import '../db/app_database.dart';
import 'allocate_store.dart';
import 'hub_sync.dart';
import 'plot_inventory_store.dart';
import 'staff_audit.dart';

/// One row from C# `ledger` table.
class LedgerEntry {
  LedgerEntry({
    required this.id,
    required this.customerPlotId,
    required this.txnDate,
    required this.voucherNo,
    required this.narration,
    required this.debit,
    required this.credit,
    required this.txnType,
  });

  final int id;
  final int customerPlotId;
  String txnDate;
  String? voucherNo;
  String narration;
  double debit;
  double credit;
  String txnType;

  factory LedgerEntry.fromMap(Map<String, Object?> m) => LedgerEntry(
        id: m['id'] as int,
        customerPlotId: m['customer_plot_id'] as int,
        txnDate: m['txn_date'] as String? ?? '',
        voucherNo: m['voucher_no'] as String?,
        narration: m['narration'] as String? ?? '',
        debit: (m['debit'] as num?)?.toDouble() ?? 0,
        credit: (m['credit'] as num?)?.toDouble() ?? 0,
        txnType: m['txn_type'] as String? ?? '',
      );
}

class LedgerPlotOption {
  const LedgerPlotOption({required this.valueId, required this.label});

  final int valueId;
  final String label;

  @override
  bool operator ==(Object other) =>
      other is LedgerPlotOption && other.valueId == valueId;

  @override
  int get hashCode => valueId.hashCode;
}

class LedgerRow {
  LedgerRow({
    required this.txnDate,
    required this.voucherNo,
    required this.narration,
    required this.debit,
    required this.credit,
    required this.txnType,
    required this.runningBalance,
  });

  final String txnDate;
  final String? voucherNo;
  final String narration;
  final double debit;
  final double credit;
  final String txnType;
  final double runningBalance;
}

class LedgerLoadResult {
  const LedgerLoadResult({
    required this.rows,
    required this.periodDebit,
    required this.periodCredit,
    required this.balance,
  });

  final List<LedgerRow> rows;
  final double periodDebit;
  final double periodCredit;
  final double balance;
}

/// Same logic as C# `frmCustomerLedgerAdd` against SQLite.
abstract final class LedgerStore {
  static Future<void> addEntry(LedgerEntry e) async {
    final db = await AppDatabase.instance.database;
    final id = await db.insert('ledger', {
      'customer_plot_id': e.customerPlotId,
      'txn_date': e.txnDate,
      'voucher_no': e.voucherNo,
      'narration': e.narration,
      'debit': e.debit,
      'credit': e.credit,
      'txn_type': e.txnType,
    });
    await StaffAudit.log('Insert', 'ledger',
        detail: 'id=$id; cpid=${e.customerPlotId}; type=${e.txnType}');
    await HubSync.pushNow('ledger', id);
    HubSync.notifyLocalWrite();
  }

  static Future<List<CustomerItem>> customers() => AllocateStore.customers();

  static Future<List<IdName>> societies() => PlotInventoryStore.societies();

  static Future<({List<LedgerPlotOption> options, bool isAllocationIds})>
      loadPlotOptions({
    required int? customerId,
    required int? societyId,
  }) async {
    if (customerId != null) {
      final db = await AppDatabase.instance.database;
      final rows = await db.rawQuery('''
SELECT cp.id, p.plot_no, p.society_id
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE cp.customer_id = ?
ORDER BY p.plot_no COLLATE NOCASE
''', [customerId]);

      final fromAlloc = <LedgerPlotOption>[];
      for (final r in rows) {
        if (societyId != null && (r['society_id'] as int) != societyId) {
          continue;
        }
        fromAlloc.add(
          LedgerPlotOption(
            valueId: r['id'] as int,
            label: r['plot_no'] as String? ?? '',
          ),
        );
      }
      if (fromAlloc.isNotEmpty) {
        return (options: fromAlloc, isAllocationIds: true);
      }
    }

    final db = await AppDatabase.instance.database;
    final sold = await db.query(
      'plots',
      where: societyId == null ? "status = 'Sold'" : "status = 'Sold' AND society_id = ?",
      whereArgs: societyId == null ? null : [societyId],
      orderBy: 'plot_no COLLATE NOCASE',
    );
    return (
      options: sold
          .map(
            (r) => LedgerPlotOption(
              valueId: r['id'] as int,
              label: r['plot_no'] as String? ?? '',
            ),
          )
          .toList(),
      isAllocationIds: false,
    );
  }

  static Future<LedgerLoadResult> load({
    required int customerId,
    int? societyId,
    int? selectedValueId,
    required bool isAllocationIds,
    required DateTime from,
    required DateTime to,
  }) async {
    final fromStart = _dayStart(from);
    final toEnd = _dayEnd(to);
    final fromDate = _dateOnly(from);
    final toDate = _dateOnly(to);

    final raw = await _loadRows(
      fromStart: fromStart,
      toEnd: toEnd,
      fromDate: fromDate,
      toDate: toDate,
      customerId: customerId,
      societyId: societyId,
      selectedValueId: selectedValueId,
      isAllocationIds: isAllocationIds,
    );

    final opening = await _sumLedgerNetBefore(
          fromStart: fromStart,
          customerId: customerId,
          societyId: societyId,
          selectedValueId: selectedValueId,
          isAllocationIds: isAllocationIds,
        ) +
        await _sumAllocationPriceBefore(
          fromDate: fromDate,
          customerId: customerId,
          societyId: societyId,
          selectedValueId: selectedValueId,
          isAllocationIds: isAllocationIds,
        );

    var bal = opening;
    var periodDebit = 0.0;
    var periodCredit = 0.0;
    final rows = <LedgerRow>[];

    for (final r in raw) {
      periodDebit += r.debit;
      periodCredit += r.credit;
      bal += (r.debit - r.credit);
      rows.add(
        LedgerRow(
          txnDate: r.txnDate,
          voucherNo: r.voucherNo,
          narration: r.narration,
          debit: r.debit,
          credit: r.credit,
          txnType: r.txnType,
          runningBalance: bal,
        ),
      );
    }

    return LedgerLoadResult(
      rows: rows,
      periodDebit: periodDebit,
      periodCredit: periodCredit,
      balance: periodDebit - periodCredit,
    );
  }

  static Future<
      List<
          ({
            String txnDate,
            String? voucherNo,
            String narration,
            double debit,
            double credit,
            String txnType
          })>> _loadRows({
    required String fromStart,
    required String toEnd,
    required String fromDate,
    required String toDate,
    required int customerId,
    required int? societyId,
    required int? selectedValueId,
    required bool isAllocationIds,
  }) async {
    final db = await AppDatabase.instance.database;
    final out = <({
      String txnDate,
      String? voucherNo,
      String narration,
      double debit,
      double credit,
      String txnType
    })>[];

    final ledgerRows = await db.rawQuery('''
SELECT l.*, cp.customer_id, p.society_id, p.id AS plot_id, cp.id AS alloc_id
FROM ledger l
JOIN customer_plots cp ON cp.id = l.customer_plot_id
JOIN plots p ON p.id = cp.plot_id
WHERE cp.customer_id = ?
''', [customerId]);

    for (final l in ledgerRows) {
      if (societyId != null && (l['society_id'] as int) != societyId) continue;
      if (!_matchesSelection(
        selectedValueId: selectedValueId,
        isAllocationIds: isAllocationIds,
        allocationId: l['alloc_id'] as int,
        plotId: l['plot_id'] as int,
      )) {
        continue;
      }
      final txnDate = l['txn_date'] as String? ?? '';
      if (txnDate.compareTo(fromStart) < 0 || txnDate.compareTo(toEnd) > 0) {
        continue;
      }
      out.add((
        txnDate: txnDate,
        voucherNo: l['voucher_no'] as String?,
        narration: l['narration'] as String? ?? '',
        debit: (l['debit'] as num?)?.toDouble() ?? 0,
        credit: (l['credit'] as num?)?.toDouble() ?? 0,
        txnType: l['txn_type'] as String? ?? '',
      ));
    }

    final allocRows = await db.rawQuery('''
SELECT cp.id AS alloc_id, cp.price, cp.booking_date, p.id AS plot_id, p.society_id
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE cp.customer_id = ?
''', [customerId]);

    for (final a in allocRows) {
      if (societyId != null && (a['society_id'] as int) != societyId) continue;
      if (!_matchesSelection(
        selectedValueId: selectedValueId,
        isAllocationIds: isAllocationIds,
        allocationId: a['alloc_id'] as int,
        plotId: a['plot_id'] as int,
      )) {
        continue;
      }
      final booking = a['booking_date'] as String? ?? '';
      final bd = booking.length >= 10 ? booking.substring(0, 10) : booking;
      if (bd.compareTo(fromDate) < 0 || bd.compareTo(toDate) > 0) continue;
      out.add((
        txnDate: '$bd 00:00:00',
        voucherNo: null,
        narration: 'Allocation price',
        debit: (a['price'] as num?)?.toDouble() ?? 0,
        credit: 0.0,
        txnType: 'Booking',
      ));
    }

    out.sort((a, b) => a.txnDate.compareTo(b.txnDate));
    return out;
  }

  static Future<double> _sumLedgerNetBefore({
    required String fromStart,
    required int customerId,
    required int? societyId,
    required int? selectedValueId,
    required bool isAllocationIds,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery('''
SELECT l.debit, l.credit, l.txn_date, cp.id AS alloc_id, p.id AS plot_id, p.society_id
FROM ledger l
JOIN customer_plots cp ON cp.id = l.customer_plot_id
JOIN plots p ON p.id = cp.plot_id
WHERE cp.customer_id = ?
''', [customerId]);

    var sum = 0.0;
    for (final l in rows) {
      if (societyId != null && (l['society_id'] as int) != societyId) continue;
      if (!_matchesSelection(
        selectedValueId: selectedValueId,
        isAllocationIds: isAllocationIds,
        allocationId: l['alloc_id'] as int,
        plotId: l['plot_id'] as int,
      )) {
        continue;
      }
      final txnDate = l['txn_date'] as String? ?? '';
      if (txnDate.compareTo(fromStart) >= 0) continue;
      sum += ((l['debit'] as num?)?.toDouble() ?? 0) -
          ((l['credit'] as num?)?.toDouble() ?? 0);
    }
    return sum;
  }

  static Future<double> _sumAllocationPriceBefore({
    required String fromDate,
    required int customerId,
    required int? societyId,
    required int? selectedValueId,
    required bool isAllocationIds,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery('''
SELECT cp.id AS alloc_id, cp.price, cp.booking_date, p.id AS plot_id, p.society_id
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE cp.customer_id = ?
''', [customerId]);

    var sum = 0.0;
    for (final a in rows) {
      if (societyId != null && (a['society_id'] as int) != societyId) continue;
      if (!_matchesSelection(
        selectedValueId: selectedValueId,
        isAllocationIds: isAllocationIds,
        allocationId: a['alloc_id'] as int,
        plotId: a['plot_id'] as int,
      )) {
        continue;
      }
      final booking = a['booking_date'] as String? ?? '';
      final bd = booking.length >= 10 ? booking.substring(0, 10) : booking;
      if (bd.compareTo(fromDate) >= 0) continue;
      sum += (a['price'] as num?)?.toDouble() ?? 0;
    }
    return sum;
  }

  static bool _matchesSelection({
    required int? selectedValueId,
    required bool isAllocationIds,
    required int allocationId,
    required int plotId,
  }) {
    if (selectedValueId == null) return true;
    return isAllocationIds
        ? allocationId == selectedValueId
        : plotId == selectedValueId;
  }

  static String _dayStart(DateTime d) => '${_dateOnly(d)} 00:00:00';

  static String _dayEnd(DateTime d) => '${_dateOnly(d)} 23:59:59';

  static String _dateOnly(DateTime d) {
    final y = d.year.toString().padLeft(4, '0');
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '$y-$m-$day';
  }

  static String formatMoney(double v) {
    final neg = v < 0;
    final abs = neg ? -v : v;
    final whole = abs.floor();
    final frac = ((abs - whole) * 100).round().clamp(0, 99);
    final s = '${_group(whole)}.${frac.toString().padLeft(2, '0')}';
    return neg ? '-$s' : s;
  }

  static String _group(int n) {
    final raw = n.toString();
    final buf = StringBuffer();
    for (var i = 0; i < raw.length; i++) {
      if (i > 0 && (raw.length - i) % 3 == 0) buf.write(',');
      buf.write(raw[i]);
    }
    return buf.toString();
  }
}
