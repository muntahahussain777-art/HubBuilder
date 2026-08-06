import '../db/app_database.dart';
import 'staff_audit.dart';

/// SQLite-backed receipts + ledger — same as C# `receipts` / `ledger`.
class ReceiptLedgerEntry {
  ReceiptLedgerEntry({
    required this.id,
    required this.customerPlotId,
    required this.txnDate,
    this.voucherNo,
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
}

/// Alias kept for receipt_screen imports that used LedgerEntry locally.
typedef LedgerEntry = ReceiptLedgerEntry;

class ReceiptRecord {
  ReceiptRecord({
    required this.id,
    required this.customerPlotId,
    required this.receiptDate,
    required this.amount,
    required this.mode,
    this.refNo,
    this.notes,
    this.ledgerId,
  });

  final int id;
  int customerPlotId;
  String receiptDate;
  double amount;
  String mode;
  String? refNo;
  String? notes;
  int? ledgerId;

  factory ReceiptRecord.fromMap(Map<String, Object?> m) => ReceiptRecord(
        id: m['id'] as int,
        customerPlotId: m['customer_plot_id'] as int,
        receiptDate: m['receipt_date'] as String? ?? '',
        amount: (m['amount'] as num?)?.toDouble() ?? 0,
        mode: m['mode'] as String? ?? 'Cash',
        refNo: m['ref_no'] as String?,
        notes: m['notes'] as String?,
        ledgerId: m['ledger_id'] as int?,
      );
}

class CustomerPlotOption {
  const CustomerPlotOption({
    required this.id,
    required this.disp,
    required this.custName,
    required this.plotNo,
    required this.society,
    required this.block,
    required this.mobile,
    required this.price,
  });

  final int id;
  final String disp;
  final String custName;
  final String plotNo;
  final String society;
  final String block;
  final String mobile;
  final double price;

  String get pickerDisp =>
      '$custName | Plot $plotNo | $society | $block';

  @override
  bool operator ==(Object other) =>
      other is CustomerPlotOption && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

enum ReceiptRowType { opening, receipt, total, closing }

class ReceiptLedgerRow {
  ReceiptLedgerRow({
    required this.rowType,
    this.receiptId,
    required this.date,
    required this.description,
    required this.debit,
    required this.credit,
    this.balance,
    this.mode = '',
    this.refNo = '',
    this.customerMobile = '',
  });

  final ReceiptRowType rowType;
  final int? receiptId;
  final String date;
  final String description;
  final double debit;
  final double credit;
  final double? balance;
  final String mode;
  final String refNo;
  final String customerMobile;

  bool get isSummary =>
      rowType == ReceiptRowType.opening ||
      rowType == ReceiptRowType.total ||
      rowType == ReceiptRowType.closing;

  bool get isReceipt => rowType == ReceiptRowType.receipt;
}

abstract final class ReceiptStore {
  static Future<List<CustomerPlotOption>> activeCustomerPlots({
    String query = '',
  }) async {
    final db = await AppDatabase.instance.database;
    final q = query.trim().toLowerCase();
    final rows = await db.rawQuery('''
SELECT cp.id, cp.price,
       c.name AS cust_name, c.mobile,
       p.plot_no, s.name AS society, b.name AS block
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
WHERE cp.status = 'Active'
ORDER BY c.name COLLATE NOCASE, p.plot_no COLLATE NOCASE
''');

    final out = <CustomerPlotOption>[];
    for (final r in rows) {
      final opt = CustomerPlotOption(
        id: r['id'] as int,
        disp: '${r['cust_name']} | Plot ${r['plot_no']}',
        custName: r['cust_name'] as String? ?? '',
        plotNo: r['plot_no'] as String? ?? '',
        society: r['society'] as String? ?? '',
        block: r['block'] as String? ?? '',
        mobile: r['mobile'] as String? ?? '',
        price: (r['price'] as num?)?.toDouble() ?? 0,
      );
      if (q.isEmpty ||
          opt.custName.toLowerCase().contains(q) ||
          opt.plotNo.toLowerCase().contains(q) ||
          opt.society.toLowerCase().contains(q) ||
          opt.block.toLowerCase().contains(q) ||
          opt.disp.toLowerCase().contains(q)) {
        out.add(opt);
      }
    }
    return out;
  }

  static Future<CustomerPlotOption?> customerPlotById(int id) async {
    final all = await activeCustomerPlots();
    try {
      return all.firstWhere((e) => e.id == id);
    } catch (_) {
      // Also look up non-active for opening calc fallback
      final db = await AppDatabase.instance.database;
      final rows = await db.rawQuery('''
SELECT cp.id, cp.price,
       c.name AS cust_name, c.mobile,
       p.plot_no, s.name AS society, b.name AS block
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
WHERE cp.id = ?
''', [id]);
      if (rows.isEmpty) return null;
      final r = rows.first;
      return CustomerPlotOption(
        id: r['id'] as int,
        disp: '${r['cust_name']} | Plot ${r['plot_no']}',
        custName: r['cust_name'] as String? ?? '',
        plotNo: r['plot_no'] as String? ?? '',
        society: r['society'] as String? ?? '',
        block: r['block'] as String? ?? '',
        mobile: r['mobile'] as String? ?? '',
        price: (r['price'] as num?)?.toDouble() ?? 0,
      );
    }
  }

  static Future<ReceiptRecord?> receiptById(int id) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('receipts', where: 'id = ?', whereArgs: [id]);
    if (rows.isEmpty) return null;
    return ReceiptRecord.fromMap(rows.first);
  }

  static Future<double> _sumLedgerDebit(int customerPlotId) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      'SELECT COALESCE(SUM(debit),0) AS s FROM ledger WHERE customer_plot_id = ?',
      [customerPlotId],
    );
    return (rows.first['s'] as num?)?.toDouble() ?? 0;
  }

  static Future<double> openingFor(int customerPlotId) async {
    final tDebit = await _sumLedgerDebit(customerPlotId);
    if (tDebit > 0) return tDebit;
    final cp = await customerPlotById(customerPlotId);
    if (cp != null) return cp.price;
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'customer_plots',
      columns: ['price'],
      where: 'id = ?',
      whereArgs: [customerPlotId],
    );
    if (rows.isEmpty) return 0;
    return (rows.first['price'] as num?)?.toDouble() ?? 0;
  }

  static Future<double> totalCreditFor(
    int customerPlotId, {
    int? excludeReceiptId,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      '''
SELECT COALESCE(SUM(amount),0) AS s FROM receipts
WHERE customer_plot_id = ?
  AND (? IS NULL OR id != ?)
''',
      [customerPlotId, excludeReceiptId, excludeReceiptId],
    );
    return (rows.first['s'] as num?)?.toDouble() ?? 0;
  }

  static Future<({double opening, double totalCredit, double remaining})>
      getRemaining(
    int customerPlotId, {
    int? excludeReceiptId,
  }) async {
    final opening = await openingFor(customerPlotId);
    final totalCredit = await totalCreditFor(
      customerPlotId,
      excludeReceiptId: excludeReceiptId,
    );
    var remaining = opening - totalCredit;
    if (remaining < 0) remaining = 0;
    return (opening: opening, totalCredit: totalCredit, remaining: remaining);
  }

  static Future<List<ReceiptLedgerRow>> buildLedgerView(
    int customerPlotId, {
    String filter = '',
  }) async {
    final cp = await customerPlotById(customerPlotId);
    final mobile = cp?.mobile ?? '';
    final opening = await openingFor(customerPlotId);
    final q = filter.trim().toLowerCase();

    final db = await AppDatabase.instance.database;
    final allReceipts = await db.query(
      'receipts',
      where: 'customer_plot_id = ?',
      whereArgs: [customerPlotId],
      orderBy: 'receipt_date ASC, id ASC',
    );

    final filtered = allReceipts.where((r) {
      if (q.isEmpty) return true;
      final ref = (r['ref_no'] as String? ?? '').toLowerCase();
      final notes = (r['notes'] as String? ?? '').toLowerCase();
      return ref.contains(q) || notes.contains(q);
    }).map(ReceiptRecord.fromMap).toList();

    final rows = <ReceiptLedgerRow>[];
    var running = opening;
    var totalCredit = 0.0;

    rows.add(
      ReceiptLedgerRow(
        rowType: ReceiptRowType.opening,
        date: '',
        description: 'Opening (Debit)',
        debit: opening,
        credit: 0,
        balance: running,
        customerMobile: mobile,
      ),
    );

    for (final r in filtered) {
      totalCredit += r.amount;
      running -= r.amount;
      final notes = (r.notes ?? '').trim();
      rows.add(
        ReceiptLedgerRow(
          rowType: ReceiptRowType.receipt,
          receiptId: r.id,
          date: r.receiptDate,
          description: notes.isEmpty ? 'Receipt' : notes,
          debit: 0,
          credit: r.amount,
          balance: running,
          mode: r.mode,
          refNo: r.refNo ?? '',
          customerMobile: mobile,
        ),
      );
    }

    rows.add(
      ReceiptLedgerRow(
        rowType: ReceiptRowType.total,
        date: '',
        description: 'Total Credit',
        debit: 0,
        credit: totalCredit,
        balance: null,
        customerMobile: mobile,
      ),
    );

    rows.add(
      ReceiptLedgerRow(
        rowType: ReceiptRowType.closing,
        date: '',
        description: 'Closing Balance (Debit - Credit)',
        debit: opening,
        credit: totalCredit,
        balance: opening - totalCredit,
        customerMobile: mobile,
      ),
    );

    return rows;
  }

  static Future<List<ReceiptLedgerRow>?> searchByRef(String query) async {
    final q = query.trim().toLowerCase();
    if (q.isEmpty) return null;
    final db = await AppDatabase.instance.database;
    final matches = await db.rawQuery(
      '''
SELECT * FROM receipts
WHERE ref_no IS NOT NULL AND trim(ref_no) != ''
  AND lower(ref_no) LIKE ?
''',
      ['%$q%'],
    );
    if (matches.isEmpty) return null;
    return _buildMultiAllocRows(
      matches.map(ReceiptRecord.fromMap).toList(),
      annotateReceipts: true,
    );
  }

  static Future<List<ReceiptLedgerRow>?> searchByCustomerName(
    String query,
  ) async {
    final q = query.trim().toLowerCase();
    if (q.isEmpty) return null;
    final db = await AppDatabase.instance.database;
    final allocRows = await db.rawQuery('''
SELECT cp.id FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
WHERE lower(c.name) LIKE ?
''', ['%$q%']);
    if (allocRows.isEmpty) return null;
    final allocIds = allocRows.map((r) => r['id'] as int).toSet();
    final placeholders = List.filled(allocIds.length, '?').join(',');
    final matches = await db.rawQuery(
      'SELECT * FROM receipts WHERE customer_plot_id IN ($placeholders)',
      allocIds.toList(),
    );
    return _buildMultiAllocRowsForAllocs(
      allocIds,
      matches.map(ReceiptRecord.fromMap).toList(),
    );
  }

  static Future<List<ReceiptLedgerRow>> _buildMultiAllocRows(
    List<ReceiptRecord> matches, {
    required bool annotateReceipts,
  }) async {
    final byCp = <int, List<ReceiptRecord>>{};
    for (final r in matches) {
      byCp.putIfAbsent(r.customerPlotId, () => []).add(r);
    }
    return _buildMultiAllocRowsForAllocs(
      byCp.keys.toSet(),
      matches,
      annotateReceipts: annotateReceipts,
    );
  }

  static Future<List<ReceiptLedgerRow>> _buildMultiAllocRowsForAllocs(
    Set<int> allocIds,
    List<ReceiptRecord> matches, {
    bool annotateReceipts = true,
  }) async {
    final opts = <CustomerPlotOption>[];
    for (final id in allocIds) {
      final o = await customerPlotById(id);
      if (o != null) opts.add(o);
    }
    opts.sort((a, b) {
      final n = a.custName.compareTo(b.custName);
      if (n != 0) return n;
      return a.plotNo.compareTo(b.plotNo);
    });

    final rows = <ReceiptLedgerRow>[];
    for (final opt in opts) {
      final opening = await openingFor(opt.id);
      final cpReceipts = matches.where((r) => r.customerPlotId == opt.id).toList()
        ..sort((a, b) {
          final d = a.receiptDate.compareTo(b.receiptDate);
          if (d != 0) return d;
          return a.id.compareTo(b.id);
        });
      var totalCredit = 0.0;

      rows.add(
        ReceiptLedgerRow(
          rowType: ReceiptRowType.opening,
          date: '',
          description:
              'Opening (Debit) — ${opt.custName} | Plot ${opt.plotNo}',
          debit: opening,
          credit: 0,
          balance: opening,
          customerMobile: opt.mobile,
        ),
      );

      for (final r in cpReceipts) {
        totalCredit += r.amount;
        final notes = (r.notes ?? '').trim();
        final base = notes.isEmpty ? 'Receipt' : notes;
        final ref = (r.refNo ?? '').trim();
        final desc = annotateReceipts
            ? '${opt.custName} | Plot ${opt.plotNo} : $base${ref.isEmpty ? '' : ' [Ref:$ref]'}'
            : base;
        rows.add(
          ReceiptLedgerRow(
            rowType: ReceiptRowType.receipt,
            receiptId: r.id,
            date: r.receiptDate,
            description: desc,
            debit: 0,
            credit: r.amount,
            balance: null,
            mode: r.mode,
            refNo: r.refNo ?? '',
            customerMobile: opt.mobile,
          ),
        );
      }

      rows.add(
        ReceiptLedgerRow(
          rowType: ReceiptRowType.total,
          date: '',
          description: 'Total Credit — ${opt.custName} | Plot ${opt.plotNo}',
          debit: 0,
          credit: totalCredit,
          balance: null,
          customerMobile: opt.mobile,
        ),
      );

      rows.add(
        ReceiptLedgerRow(
          rowType: ReceiptRowType.closing,
          date: '',
          description:
              'Closing Balance (Debit - Credit) — ${opt.custName} | Plot ${opt.plotNo}',
          debit: opening,
          credit: totalCredit,
          balance: opening - totalCredit,
          customerMobile: opt.mobile,
        ),
      );
    }
    return rows;
  }

  static Future<
      ({
        List<ReceiptLedgerRow>? rows,
        int? customerPlotId,
        String? allocationDisp,
        String mode,
      })> smartSearch(String query) async {
    final q = query.trim();
    if (q.isEmpty) {
      return (rows: null, customerPlotId: null, allocationDisp: null, mode: '');
    }

    final byRef = await searchByRef(q);
    if (byRef != null && byRef.isNotEmpty) {
      return (
        rows: byRef,
        customerPlotId: null,
        allocationDisp: null,
        mode: 'ref'
      );
    }

    final byCust = await searchByCustomerName(q);
    if (byCust != null && byCust.isNotEmpty) {
      return (
        rows: byCust,
        customerPlotId: null,
        allocationDisp: null,
        mode: 'cust'
      );
    }

    final matches = await activeCustomerPlots(query: q);
    if (matches.length == 1) {
      final m = matches.first;
      return (
        rows: await buildLedgerView(m.id),
        customerPlotId: m.id,
        allocationDisp: m.disp,
        mode: 'alloc',
      );
    }

    return (rows: null, customerPlotId: null, allocationDisp: null, mode: '');
  }

  static Future<List<CustomerPlotOption>> searchAllocations(String query) =>
      activeCustomerPlots(query: query);

  static Future<({bool ok, String message, double remaining})> saveReceipt({
    required int customerPlotId,
    required String receiptDate,
    required double amount,
    required String mode,
    String? refNo,
    String? notes,
    int? editingReceiptId,
  }) async {
    if (amount <= 0) {
      return (ok: false, message: 'Amount > 0 required.', remaining: 0.0);
    }
    if (mode != 'Cash' && mode != 'Bank') {
      return (ok: false, message: 'Mode select karein.', remaining: 0.0);
    }

    final rm =
        await getRemaining(customerPlotId, excludeReceiptId: editingReceiptId);
    if (rm.remaining <= 0) {
      return (
        ok: false,
        message:
            'Is allocation ka debit settle ho chuka hai. New receipt add/update nahi ho sakti.',
        remaining: 0.0,
      );
    }
    if (amount > rm.remaining) {
      return (
        ok: false,
        message:
            'Amount remaining se zyada hai.\nRemaining: ${rm.remaining.toStringAsFixed(2)}\nPlease enter equal or less.',
        remaining: rm.remaining,
      );
    }

    final cleanRef =
        (refNo == null || refNo.trim().isEmpty) ? null : refNo.trim();
    final cleanNotes =
        (notes == null || notes.trim().isEmpty) ? null : notes.trim();
    final txnType = mode == 'Bank' ? 'ReceiptBank' : 'ReceiptCash';
    final narration = cleanNotes ?? 'Receipt ($mode)';

    final db = await AppDatabase.instance.database;

    if (editingReceiptId != null) {
      final existing = await receiptById(editingReceiptId);
      if (existing == null) {
        return (ok: false, message: 'Receipt not found.', remaining: 0.0);
      }

      if (existing.ledgerId != null) {
        await db.update(
          'ledger',
          {
            'txn_date': receiptDate,
            'credit': amount,
            'txn_type': txnType,
            'narration': narration,
          },
          where: 'id = ?',
          whereArgs: [existing.ledgerId],
        );
      }

      await db.update(
        'receipts',
        {
          'customer_plot_id': customerPlotId,
          'receipt_date': receiptDate,
          'amount': amount,
          'mode': mode,
          'ref_no': cleanRef,
          'notes': cleanNotes,
        },
        where: 'id = ?',
        whereArgs: [editingReceiptId],
      );
    } else {
      final lid = await db.insert('ledger', {
        'customer_plot_id': customerPlotId,
        'txn_date': receiptDate,
        'narration': narration,
        'debit': 0,
        'credit': amount,
        'txn_type': txnType,
      });
      await db.insert('receipts', {
        'customer_plot_id': customerPlotId,
        'receipt_date': receiptDate,
        'amount': amount,
        'mode': mode,
        'ref_no': cleanRef,
        'notes': cleanNotes,
        'ledger_id': lid,
      });
    }

    final after = await getRemaining(customerPlotId);
    await StaffAudit.log(
      editingReceiptId != null ? 'Update' : 'Insert',
      'receipts',
      detail: 'amount=$amount; mode=$mode; cpid=$customerPlotId',
    );
    return (ok: true, message: 'Saved', remaining: after.remaining);
  }

  static Future<({bool ok, String message})> deleteReceipt(int receiptId) async {
    final existing = await receiptById(receiptId);
    if (existing == null) {
      return (ok: false, message: 'Receipt not found.');
    }
    final db = await AppDatabase.instance.database;
    final lid = existing.ledgerId;
    await db.delete('receipts', where: 'id = ?', whereArgs: [receiptId]);
    if (lid != null) {
      await db.delete('ledger', where: 'id = ?', whereArgs: [lid]);
    }
    await StaffAudit.log('Delete', 'receipts', detail: 'id=$receiptId');
    return (ok: true, message: 'Receipt deleted successfully.');
  }

  static String buildWhatsAppMessage({
    required List<ReceiptLedgerRow> rows,
    String allocationDisp = '',
  }) {
    final buf = StringBuffer();
    buf.writeln('Assalam o Alaikum,');
    buf.writeln(
      allocationDisp.trim().isEmpty
          ? 'Receipt Ledger:'
          : 'Receipt Ledger — $allocationDisp:',
    );
    buf.writeln('');

    var openingDebit = 0.0;
    for (final r in rows) {
      if (r.rowType == ReceiptRowType.opening) {
        openingDebit = r.debit;
        buf.writeln('Opening Debit: ${_fmtMoney(openingDebit)}');
        break;
      }
    }

    var anyReceipt = false;
    var totalCredit = 0.0;
    for (final r in rows) {
      if (r.rowType != ReceiptRowType.receipt) continue;
      anyReceipt = true;
      totalCredit += r.credit;
      var refno = r.refNo.trim();
      if (refno.isEmpty) refno = r.description;
      buf.writeln(
        'Receipt: ${r.date} | $refno | Amount: ${_fmtMoney(r.credit)}',
      );
    }
    if (!anyReceipt) buf.writeln('Receipt: —');

    for (final r in rows) {
      if (r.rowType == ReceiptRowType.total) {
        if (r.credit > 0) totalCredit = r.credit;
        break;
      }
    }
    buf.writeln('Total Credit: ${_fmtMoney(totalCredit)}');

    for (final r in rows) {
      if (r.rowType == ReceiptRowType.closing) {
        buf.writeln(
          'Closing (Debit - Credit): ${_fmtMoney(r.debit)} - ${_fmtMoney(r.credit)}',
        );
        break;
      }
    }

    final balance = openingDebit - totalCredit;
    buf.writeln('');
    buf.writeln('Summary:');
    buf.writeln('Debit(Open): ${_fmtMoney(openingDebit)}');
    buf.writeln('Credit(Total): ${_fmtMoney(totalCredit)}');
    buf.write('Balance: ${_fmtMoney(balance)}');
    return buf.toString().trimRight();
  }

  static String _fmtMoney(double v) {
    if (v == v.roundToDouble()) return v.toStringAsFixed(0);
    return v.toStringAsFixed(2);
  }
}
