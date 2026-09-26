import '../db/app_database.dart';
import 'dummy_data.dart';
import 'hub_sync.dart';
import 'plot_inventory_store.dart';
import 'staff_audit.dart';
import 'vendor_store.dart';
class PartnerCashRow {
  const PartnerCashRow({
    required this.id,
    required this.partnerId,
    required this.partnerName,
    required this.txnDate,
    required this.direction,
    required this.amount,
    required this.category,
    required this.mode,
    required this.note,
    this.customerName = '',
    this.plotNo = '',
    this.staff = '',
  });

  final int id;
  final int partnerId;
  final String partnerName;
  final String txnDate;
  final String direction;
  final double amount;
  final String category;
  final String mode;
  final String note;
  final String customerName;
  final String plotNo;
  final String staff;
}

class PartnerCashVipRow {
  const PartnerCashVipRow({
    required this.srNo,
    required this.rowType,
    required this.partnerId,
    required this.txnDate,
    required this.partnerName,
    required this.direction,
    required this.amountIn,
    required this.amountOut,
    required this.category,
    required this.mode,
    required this.brokerName,
    required this.customerName,
    required this.plotNo,
    required this.societyName,
    required this.note,
    required this.balance,
  });

  final int srNo;
  final String rowType;
  final int partnerId;
  final String txnDate;
  final String partnerName;
  final String direction;
  final double amountIn;
  final double amountOut;
  final String category;
  final String mode;
  final String brokerName;
  final String customerName;
  final String plotNo;
  final String societyName;
  final String note;
  final double balance;
}

class PartnerCashVipResult {
  const PartnerCashVipResult({
    required this.rows,
    required this.totalIn,
    required this.totalOut,
    required this.summary,
  });

  final List<PartnerCashVipRow> rows;
  final double totalIn;
  final double totalOut;
  final String summary;

  double get balance => totalIn - totalOut;
}

class CustomerPlotsReceiptLine {
  const CustomerPlotsReceiptLine({
    required this.srNo,
    required this.rowType,
    required this.rowLabel,
    required this.receiptDate,
    required this.receiptId,
    required this.mode,
    required this.refNo,
    required this.notes,
    required this.amount,
  });

  final int srNo;
  final String rowType;
  final String rowLabel;
  final String receiptDate;
  final String receiptId;
  final String mode;
  final String refNo;
  final String notes;
  final double amount;
}

class CustomerPlotsReceiptsResult {
  const CustomerPlotsReceiptsResult({
    required this.customerName,
    required this.rows,
    required this.grandTotal,
    required this.totalPlotPrices,
    required this.remaining,
    required this.receiptCount,
  });

  final String customerName;
  final List<CustomerPlotsReceiptLine> rows;
  final double grandTotal;
  final double totalPlotPrices;
  final double remaining;
  final int receiptCount;
}

/// Partner Cash Book + VIP reports — same schema as WinForms.
abstract final class PartnerCashStore {
  static Future<void> ensureSchema() async {
    await AppDatabase.ensurePartnerMoneySchema();
  }

  static Future<List<IdName>> partners() => VendorStore.partners();

  static Future<List<IdName>> societies() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('societies', orderBy: 'name COLLATE NOCASE');
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<List<String>> categories({String? direction}) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final rows = direction == null || direction.isEmpty
        ? await db.query(
            'partner_money_categories',
            orderBy: 'name COLLATE NOCASE',
          )
        : await db.query(
            'partner_money_categories',
            where: "direction = ? OR direction = 'Both'",
            whereArgs: [direction],
            orderBy: 'name COLLATE NOCASE',
          );
    return rows
        .map((r) => r['name'] as String? ?? '')
        .where((n) => n.isNotEmpty)
        .toList();
  }

  static Future<void> addCategory(String name, String direction) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final dir = (direction == 'In' || direction == 'Out') ? direction : 'Both';
    await db.rawInsert(
      'INSERT OR IGNORE INTO partner_money_categories (name, direction) VALUES (?, ?)',
      [name.trim(), dir],
    );
  }

  static Future<double> balance() async {
    final t = await totals();
    return t.balance;
  }

  /// Total In, Total Out, Balance — WinForms jaisa LEFT JOIN (sab rows; duplicate Person hide nahi).
  static Future<({double totalIn, double totalOut, double balance})> totals({
    String query = '',
  }) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final q = query.trim();
    const fromJoin = '''
FROM partner_money pm
LEFT JOIN Person p ON p.person_id = pm.partner_id
LEFT JOIN customer_plots cp ON cp.id = pm.customer_plot_id
LEFT JOIN customers c ON c.id = cp.customer_id
LEFT JOIN plots pl ON pl.id = cp.plot_id
LEFT JOIN societies s ON s.id = COALESCE(pm.society_id, pl.society_id)''';
    final List<Map<String, Object?>> rows;
    if (q.isEmpty) {
      rows = await db.rawQuery('''
SELECT
  COALESCE(SUM(CASE WHEN trim(lower(ifnull(pm.direction,'')))='in' THEN ifnull(pm.amount,0) ELSE 0 END), 0) AS tin,
  COALESCE(SUM(CASE WHEN trim(lower(ifnull(pm.direction,'')))='out' THEN ifnull(pm.amount,0) ELSE 0 END), 0) AS tout
$fromJoin''');
    } else {
      rows = await db.rawQuery(
        '''
SELECT
  COALESCE(SUM(CASE WHEN trim(lower(ifnull(pm.direction,'')))='in' THEN ifnull(pm.amount,0) ELSE 0 END), 0) AS tin,
  COALESCE(SUM(CASE WHEN trim(lower(ifnull(pm.direction,'')))='out' THEN ifnull(pm.amount,0) ELSE 0 END), 0) AS tout
$fromJoin
WHERE IFNULL(p.name,'') LIKE ? COLLATE NOCASE
   OR IFNULL(pm.category,'') LIKE ? COLLATE NOCASE
   OR IFNULL(pm.note,'') LIKE ? COLLATE NOCASE
   OR pm.direction LIKE ? COLLATE NOCASE
   OR CAST(pm.amount AS TEXT) LIKE ?
   OR IFNULL(c.name,'') LIKE ? COLLATE NOCASE
   OR IFNULL(c.mobile,'') LIKE ? COLLATE NOCASE
   OR CAST(IFNULL(pl.plot_no,'') AS TEXT) LIKE ? COLLATE NOCASE
   OR IFNULL(s.name,'') LIKE ? COLLATE NOCASE
   OR CAST(pm.partner_id AS TEXT) LIKE ?''',
        List.filled(10, '%$q%'),
      );
    }
    final tin = (rows.first['tin'] as num?)?.toDouble() ?? 0;
    final tout = (rows.first['tout'] as num?)?.toDouble() ?? 0;
    return (totalIn: tin, totalOut: tout, balance: tin - tout);
  }

  static Future<List<ListRow>> listRows({String query = ''}) async {
    final rows = await list(query: query);
    return rows
        .map(
          (r) => ListRow(
            id: r.id,
            title: r.partnerName,
            subtitle: '${r.txnDate}  ·  ${r.direction}  ·  ${r.category}',
            meta: 'Rs ${r.amount.toStringAsFixed(2)}',
            tag: r.mode,
            note: r.note,
            date: r.txnDate,
            staff: r.staff,
          ),
        )
        .toList();
  }

  static Future<List<PartnerCashRow>> list({String query = ''}) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final q = query.trim();
    // WinForms frmPartnerCashView: LEFT JOIN Person — rows hide mat karo
    final rows = q.isEmpty
        ? await db.rawQuery('''
SELECT pm.id, pm.partner_id,
       IFNULL(NULLIF(trim(p.name),''), 'Partner #' || pm.partner_id) AS partner_name,
       IFNULL(pm.txn_date,'') AS txn_date, IFNULL(pm.direction,'') AS direction,
       IFNULL(pm.amount,0) AS amount, IFNULL(pm.category,'') AS category,
       IFNULL(pm.mode,'') AS mode, IFNULL(pm.note,'') AS note,
       IFNULL(c.name,'') AS customer_name,
       IFNULL(CAST(pl.plot_no AS TEXT),'') AS plot_no
FROM partner_money pm
LEFT JOIN Person p ON p.person_id = pm.partner_id
LEFT JOIN customer_plots cp ON cp.id = pm.customer_plot_id
LEFT JOIN customers c ON c.id = cp.customer_id
LEFT JOIN plots pl ON pl.id = cp.plot_id
ORDER BY pm.txn_date DESC, pm.id DESC''')
        : await db.rawQuery(
            '''
SELECT pm.id, pm.partner_id,
       IFNULL(NULLIF(trim(p.name),''), 'Partner #' || pm.partner_id) AS partner_name,
       IFNULL(pm.txn_date,'') AS txn_date, IFNULL(pm.direction,'') AS direction,
       IFNULL(pm.amount,0) AS amount, IFNULL(pm.category,'') AS category,
       IFNULL(pm.mode,'') AS mode, IFNULL(pm.note,'') AS note,
       IFNULL(c.name,'') AS customer_name,
       IFNULL(CAST(pl.plot_no AS TEXT),'') AS plot_no
FROM partner_money pm
LEFT JOIN Person p ON p.person_id = pm.partner_id
LEFT JOIN customer_plots cp ON cp.id = pm.customer_plot_id
LEFT JOIN customers c ON c.id = cp.customer_id
LEFT JOIN plots pl ON pl.id = cp.plot_id
LEFT JOIN societies s ON s.id = COALESCE(pm.society_id, pl.society_id)
WHERE IFNULL(p.name,'') LIKE ? COLLATE NOCASE
   OR IFNULL(pm.category,'') LIKE ? COLLATE NOCASE
   OR IFNULL(pm.note,'') LIKE ? COLLATE NOCASE
   OR pm.direction LIKE ? COLLATE NOCASE
   OR CAST(pm.amount AS TEXT) LIKE ?
   OR IFNULL(c.name,'') LIKE ? COLLATE NOCASE
   OR IFNULL(c.mobile,'') LIKE ? COLLATE NOCASE
   OR CAST(IFNULL(pl.plot_no,'') AS TEXT) LIKE ? COLLATE NOCASE
   OR IFNULL(s.name,'') LIKE ? COLLATE NOCASE
   OR CAST(pm.partner_id AS TEXT) LIKE ?
ORDER BY pm.txn_date DESC, pm.id DESC''',
            List.filled(10, '%$q%'),
          );

    final out = <PartnerCashRow>[];
    for (final r in rows) {
      out.add(
        PartnerCashRow(
          id: r['id'] as int,
          partnerId: r['partner_id'] as int,
          partnerName: r['partner_name'] as String? ?? '',
          txnDate: r['txn_date'] as String? ?? '',
          direction: r['direction'] as String? ?? '',
          amount: (r['amount'] as num?)?.toDouble() ?? 0,
          category: r['category'] as String? ?? '',
          mode: r['mode'] as String? ?? '',
          note: r['note'] as String? ?? '',
          customerName: r['customer_name'] as String? ?? '',
          plotNo: r['plot_no'] as String? ?? '',
          staff: await StaffAudit.staffForId(r['id'] as int),
        ),
      );
    }
    return out;
  }

  static Future<PartnerCashRow?> getById(int id) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      '''
SELECT pm.id, pm.partner_id,
       IFNULL(NULLIF(trim(p.name),''), 'Partner #' || pm.partner_id) AS partner_name,
       IFNULL(pm.txn_date,'') AS txn_date, IFNULL(pm.direction,'') AS direction,
       IFNULL(pm.amount,0) AS amount, IFNULL(pm.category,'') AS category,
       IFNULL(pm.mode,'') AS mode, IFNULL(pm.note,'') AS note
FROM partner_money pm
LEFT JOIN Person p ON p.person_id = pm.partner_id
WHERE pm.id = ?''',
      [id],
    );
    if (rows.isEmpty) return null;
    final r = rows.first;
    return PartnerCashRow(
      id: r['id'] as int,
      partnerId: r['partner_id'] as int,
      partnerName: r['partner_name'] as String? ?? '',
      txnDate: r['txn_date'] as String? ?? '',
      direction: r['direction'] as String? ?? '',
      amount: (r['amount'] as num?)?.toDouble() ?? 0,
      category: r['category'] as String? ?? '',
      mode: r['mode'] as String? ?? '',
      note: r['note'] as String? ?? '',
    );
  }

  static Future<int> insert({
    required int partnerId,
    required String txnDate,
    required String direction,
    required double amount,
    required String category,
    String mode = 'Cash',
    String? note,
    int? customerPlotId,
    int? receiptId,
    int? societyId,
  }) async {
    await ensureSchema();
    final dir = direction == 'Out' ? 'Out' : 'In';
    return StaffAudit.insert('partner_money', {
      'partner_id': partnerId,
      'txn_date': txnDate,
      'direction': dir,
      'amount': amount,
      'mode': mode,
      'category': category,
      'customer_plot_id': customerPlotId,
      'receipt_id': receiptId,
      'society_id': societyId,
      'note': note?.trim(),
    });
  }

  static Future<bool> update({
    required int id,
    required int partnerId,
    required String txnDate,
    required String direction,
    required double amount,
    required String category,
    String mode = 'Cash',
    String? note,
  }) async {
    await ensureSchema();
    final dir = direction == 'Out' ? 'Out' : 'In';
    final n = await StaffAudit.update(
      'partner_money',
      {
        'partner_id': partnerId,
        'txn_date': txnDate,
        'direction': dir,
        'amount': amount,
        'mode': mode,
        'category': category,
        'note': note?.trim(),
      },
      where: 'id = ?',
      whereArgs: [id],
    );
    return n > 0;
  }

  static Future<bool> delete(int id) async {
    await ensureSchema();
    final n = await StaffAudit.delete(
      'partner_money',
      where: 'id = ?',
      whereArgs: [id],
    );
    return n > 0;
  }

  /// VIP Partner Cash Book report (same SQL as WinForms).
  static Future<PartnerCashVipResult> vipCashBook({
    required DateTime from,
    required DateTime to,
    int societyId = 0,
    int partnerId = 0,
    String direction = '',
    String keyword = '',
  }) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final dateFrom = _ymd(from);
    final dateTo = _ymd(to);
    final kw = keyword.trim();
    final kwLike = '%$kw%';

    final raw = await db.rawQuery(
      '''
SELECT
  pm.id,
  pm.partner_id,
  IFNULL(pm.txn_date,'') AS txn_date,
  IFNULL(NULLIF(trim(p.name),''), 'Partner #' || pm.partner_id) AS partner_name,
  IFNULL(pm.direction,'') AS direction,
  IFNULL(pm.amount,0) AS amount,
  IFNULL(pm.category,'') AS category,
  IFNULL(pm.mode,'') AS mode,
  IFNULL(pm.note,'') AS note,
  IFNULL(br.name,'') AS broker_name,
  IFNULL(c.name,'') AS customer_name,
  IFNULL(CAST(pl.plot_no AS TEXT),'') AS plot_no,
  IFNULL(s.name,'') AS society_name
FROM partner_money pm
LEFT JOIN Person p ON p.person_id = pm.partner_id
LEFT JOIN customer_plots cp ON cp.id = pm.customer_plot_id
LEFT JOIN brokers br ON br.id = cp.broker_id
LEFT JOIN customers c ON c.id = cp.customer_id
LEFT JOIN plots pl ON pl.id = cp.plot_id
LEFT JOIN societies s ON s.id = COALESCE(pm.society_id, pl.society_id)
WHERE DATE(pm.txn_date) >= DATE(?)
  AND DATE(pm.txn_date) <= DATE(?)
  AND (? = 0 OR COALESCE(pm.society_id, pl.society_id, 0) = ?)
  AND (? = 0 OR pm.partner_id = ?)
  AND (? = '' OR pm.direction = ?)
  AND (
       ? = ''
       OR p.name LIKE ? COLLATE NOCASE
       OR IFNULL(pm.category,'') LIKE ? COLLATE NOCASE
       OR IFNULL(pm.note,'') LIKE ? COLLATE NOCASE
       OR IFNULL(pm.mode,'') LIKE ? COLLATE NOCASE
       OR IFNULL(pm.direction,'') LIKE ? COLLATE NOCASE
       OR IFNULL(br.name,'') LIKE ? COLLATE NOCASE
       OR IFNULL(c.name,'') LIKE ? COLLATE NOCASE
       OR IFNULL(s.name,'') LIKE ? COLLATE NOCASE
       OR CAST(IFNULL(pl.plot_no,'') AS TEXT) LIKE ? COLLATE NOCASE
  )
ORDER BY DATE(pm.txn_date) ASC, pm.id ASC''',
      [
        dateFrom,
        dateTo,
        societyId,
        societyId,
        partnerId,
        partnerId,
        direction,
        direction,
        kw,
        kwLike,
        kwLike,
        kwLike,
        kwLike,
        kwLike,
        kwLike,
        kwLike,
        kwLike,
        kwLike,
      ],
    );

    final out = <PartnerCashVipRow>[];
    var sr = 0;
    var totalIn = 0.0;
    var totalOut = 0.0;
    for (final r in raw) {
      sr++;
      final dir = (r['direction'] as String? ?? '');
      final amt = (r['amount'] as num?)?.toDouble() ?? 0;
      final ain = dir.toLowerCase() == 'in' ? amt : 0.0;
      final aout = dir.toLowerCase() == 'out' ? amt : 0.0;
      totalIn += ain;
      totalOut += aout;
      out.add(
        PartnerCashVipRow(
          srNo: sr,
          rowType: 'DETAIL',
          partnerId: r['partner_id'] as int? ?? 0,
          txnDate: r['txn_date'] as String? ?? '',
          partnerName: r['partner_name'] as String? ?? '',
          direction: dir,
          amountIn: ain,
          amountOut: aout,
          category: r['category'] as String? ?? '',
          mode: r['mode'] as String? ?? '',
          brokerName: r['broker_name'] as String? ?? '',
          customerName: r['customer_name'] as String? ?? '',
          plotNo: r['plot_no'] as String? ?? '',
          societyName: r['society_name'] as String? ?? '',
          note: r['note'] as String? ?? '',
          balance: totalIn - totalOut,
        ),
      );
    }

    if (out.isNotEmpty) {
      out.add(
        PartnerCashVipRow(
          srNo: 0,
          rowType: 'TOTAL',
          partnerId: 0,
          txnDate: '',
          partnerName: 'TOTAL / BALANCE',
          direction: '',
          amountIn: totalIn,
          amountOut: totalOut,
          category: '',
          mode: '',
          brokerName: '',
          customerName: '',
          plotNo: '',
          societyName: '',
          note: '',
          balance: totalIn - totalOut,
        ),
      );
    }

    final summary =
        'Total In: ${totalIn.toStringAsFixed(2)}     Total Out: ${totalOut.toStringAsFixed(2)}     Balance (In − Out): ${(totalIn - totalOut).toStringAsFixed(2)}';

    return PartnerCashVipResult(
      rows: out,
      totalIn: totalIn,
      totalOut: totalOut,
      summary: summary,
    );
  }

  /// VIP Customer Plots Receipts — same logic as WinForms (prices − receipts).
  static Future<CustomerPlotsReceiptsResult> vipCustomerPlotsReceipts({
    required int customerId,
    required DateTime from,
    required DateTime to,
  }) async {
    final db = await AppDatabase.instance.database;
    final raw = await db.rawQuery(
      '''
SELECT
  c.name AS customer_name,
  s.name AS society_name,
  bl.name AS block_name,
  CAST(p.plot_no AS TEXT) AS plot_no,
  IFNULL(pt.code,'') AS plot_type,
  cp.id AS customer_plot_id,
  CAST(IFNULL(cp.price,0) AS REAL) AS plot_price,
  r.id AS receipt_id,
  r.receipt_date AS receipt_date,
  r.amount AS amount,
  IFNULL(r.mode,'') AS mode,
  IFNULL(r.ref_no,'') AS refno,
  IFNULL(r.notes,'') AS notes
FROM customer_plots cp
JOIN customers  c  ON c.id = cp.customer_id
JOIN plots      p  ON p.id = cp.plot_id
LEFT JOIN societies  s  ON s.id = p.society_id
LEFT JOIN blocks     bl ON bl.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
JOIN receipts   r  ON r.customer_plot_id = cp.id
WHERE cp.status = 'Active'
  AND c.id = ?
  AND DATE(r.receipt_date) >= DATE(?)
  AND DATE(r.receipt_date) <= DATE(?)
ORDER BY s.name, bl.name, CAST(p.plot_no AS INTEGER), DATE(r.receipt_date), r.id''',
      [customerId, _ymd(from), _ymd(to)],
    );

    if (raw.isEmpty) {
      return const CustomerPlotsReceiptsResult(
        customerName: '',
        rows: [],
        grandTotal: 0,
        totalPlotPrices: 0,
        remaining: 0,
        receiptCount: 0,
      );
    }

    final customerName = raw.first['customer_name'] as String? ?? '';
    final byPlot = <int, List<Map<String, Object?>>>{};
    for (final r in raw) {
      final cpid = r['customer_plot_id'] as int;
      byPlot.putIfAbsent(cpid, () => []).add(r);
    }

    final plotIds = byPlot.keys.toList()
      ..sort((a, b) {
        final ra = byPlot[a]!.first;
        final rb = byPlot[b]!.first;
        final sa = (ra['society_name'] as String? ?? '').toLowerCase();
        final sb = (rb['society_name'] as String? ?? '').toLowerCase();
        final c1 = sa.compareTo(sb);
        if (c1 != 0) return c1;
        final ba = (ra['block_name'] as String? ?? '').toLowerCase();
        final bb = (rb['block_name'] as String? ?? '').toLowerCase();
        final c2 = ba.compareTo(bb);
        if (c2 != 0) return c2;
        final pa = int.tryParse('${ra['plot_no']}') ?? 0;
        final pb = int.tryParse('${rb['plot_no']}') ?? 0;
        return pa.compareTo(pb);
      });

    final lines = <CustomerPlotsReceiptLine>[];
    var sr = 1;
    var grandTotal = 0.0;
    var totalPlotPrices = 0.0;
    var receiptCount = 0;

    for (final cpid in plotIds) {
      final group = byPlot[cpid]!;
      group.sort((a, b) {
        final da = a['receipt_date'] as String? ?? '';
        final db_ = b['receipt_date'] as String? ?? '';
        final c = da.compareTo(db_);
        if (c != 0) return c;
        return ((a['receipt_id'] as int?) ?? 0)
            .compareTo((b['receipt_id'] as int?) ?? 0);
      });

      final first = group.first;
      final plotDisp = _plotDisp(
        first['society_name'] as String?,
        first['block_name'] as String?,
        first['plot_no'] as String?,
        first['plot_type'] as String?,
      );
      final plotPrice = (first['plot_price'] as num?)?.toDouble() ?? 0;
      totalPlotPrices += plotPrice;
      var plotPaid = 0.0;

      for (final x in group) {
        final amt = (x['amount'] as num?)?.toDouble() ?? 0;
        plotPaid += amt;
        grandTotal += amt;
        receiptCount++;
        lines.add(
          CustomerPlotsReceiptLine(
            srNo: sr++,
            rowType: 'DETAIL',
            rowLabel: plotDisp,
            receiptDate: x['receipt_date'] as String? ?? '',
            receiptId: '${x['receipt_id']}',
            mode: x['mode'] as String? ?? '',
            refNo: x['refno'] as String? ?? '',
            notes: x['notes'] as String? ?? '',
            amount: amt,
          ),
        );
      }

      final plotBal = plotPrice - plotPaid;
      lines.add(
        CustomerPlotsReceiptLine(
          srNo: 0,
          rowType: 'PLOT_PRICE',
          rowLabel: 'PLOT PRICE (Allocate) - $plotDisp',
          receiptDate: '',
          receiptId: '',
          mode: '',
          refNo: '',
          notes: '',
          amount: plotPrice,
        ),
      );
      lines.add(
        CustomerPlotsReceiptLine(
          srNo: 0,
          rowType: 'PLOT_TOTAL',
          rowLabel:
              'PLOT PAID - $plotDisp  (${group.length} receipts)  |  Remaining: ${plotBal.toStringAsFixed(2)}',
          receiptDate: '',
          receiptId: '',
          mode: '',
          refNo: '',
          notes: '',
          amount: plotPaid,
        ),
      );
    }

    final remaining = totalPlotPrices - grandTotal;
    lines.add(
      CustomerPlotsReceiptLine(
        srNo: 0,
        rowType: 'TOTAL_PRICES',
        rowLabel: 'TOTAL PLOT PRICES (all plots above)',
        receiptDate: '',
        receiptId: '',
        mode: '',
        refNo: '',
        notes: '',
        amount: totalPlotPrices,
      ),
    );
    lines.add(
      CustomerPlotsReceiptLine(
        srNo: 0,
        rowType: 'GRAND_TOTAL',
        rowLabel: 'RECEIPTS GRAND TOTAL  ($receiptCount receipts)',
        receiptDate: '',
        receiptId: '',
        mode: '',
        refNo: '',
        notes: '',
        amount: grandTotal,
      ),
    );
    lines.add(
      CustomerPlotsReceiptLine(
        srNo: 0,
        rowType: 'REMAINING',
        rowLabel: 'REMAINING  (Plot Prices − Receipts)',
        receiptDate: '',
        receiptId: '',
        mode: '',
        refNo: '',
        notes: '',
        amount: remaining,
      ),
    );

    return CustomerPlotsReceiptsResult(
      customerName: customerName,
      rows: lines,
      grandTotal: grandTotal,
      totalPlotPrices: totalPlotPrices,
      remaining: remaining,
      receiptCount: receiptCount,
    );
  }

  static Future<List<IdName>> customers() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('customers', orderBy: 'name COLLATE NOCASE');
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<void> setAllocationPartner(
    int customerPlotId,
    int partnerId,
  ) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    await db.execute(
      '''
INSERT INTO customer_plot_partners (customer_plot_id, partner_id)
VALUES (?, ?)
ON CONFLICT(customer_plot_id) DO UPDATE SET partner_id=excluded.partner_id''',
      [customerPlotId, partnerId],
    );
    final rows = await db.query(
      'customer_plot_partners',
      columns: ['id'],
      where: 'customer_plot_id = ?',
      whereArgs: [customerPlotId],
      limit: 1,
    );
    if (rows.isNotEmpty) {
      await HubSync.pushNow('customer_plot_partners', rows.first['id']);
    }
    HubSync.notifyLocalWrite();
  }

  static Future<int?> getAllocationPartnerId(int customerPlotId) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'customer_plot_partners',
      columns: ['partner_id'],
      where: 'customer_plot_id = ?',
      whereArgs: [customerPlotId],
      limit: 1,
    );
    if (rows.isEmpty) return null;
    return rows.first['partner_id'] as int?;
  }

  static Future<int?> _societyIdForCp(int customerPlotId) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      '''
SELECT p.society_id FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE cp.id = ?''',
      [customerPlotId],
    );
    if (rows.isEmpty) return null;
    return rows.first['society_id'] as int?;
  }

  /// WinForms UpsertPartnerMoneyForReceipt.
  static Future<void> upsertPartnerMoneyForReceipt({
    required int receiptId,
    required int partnerId,
    required int customerPlotId,
    required String txnDate,
    required double amount,
    required String mode,
    String? note,
  }) async {
    await ensureSchema();
    await StaffAudit.delete(
      'partner_money',
      where: 'receipt_id = ?',
      whereArgs: [receiptId],
    );
    if (partnerId <= 0 || amount <= 0) return;
    final sid = await _societyIdForCp(customerPlotId);
    await insert(
      partnerId: partnerId,
      txnDate: txnDate,
      direction: 'In',
      amount: amount,
      category: 'Receipt',
      mode: mode,
      note: note,
      customerPlotId: customerPlotId,
      receiptId: receiptId,
      societyId: sid,
    );
    await setAllocationPartner(customerPlotId, partnerId);
  }

  /// WinForms SyncAllocationPartnerCommission.
  static Future<void> syncAllocationPartnerCommission({
    required int customerPlotId,
    required int partnerId,
    required double amount,
    required String txnDate,
  }) async {
    await ensureSchema();
    await StaffAudit.delete(
      'partner_money',
      where:
          "customer_plot_id = ? AND category = 'Broker Commission' AND IFNULL(receipt_id,0) = 0",
      whereArgs: [customerPlotId],
    );
    if (partnerId <= 0) return;
    await setAllocationPartner(customerPlotId, partnerId);
    if (amount <= 0) return;
    final sid = await _societyIdForCp(customerPlotId);
    await insert(
      partnerId: partnerId,
      txnDate: txnDate,
      direction: 'In',
      amount: amount,
      category: 'Broker Commission',
      mode: 'Cash',
      note: 'Allocate broker/partner commission',
      customerPlotId: customerPlotId,
      societyId: sid,
    );
  }

  static Future<void> deletePartnerMoneyForReceipt(int receiptId) async {
    await ensureSchema();
    await StaffAudit.delete(
      'partner_money',
      where: 'receipt_id = ?',
      whereArgs: [receiptId],
    );
  }

  static String _ymd(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  static String _plotDisp(
    String? society,
    String? block,
    String? plotNo,
    String? plotType,
  ) {
    final sb = (society ?? '').trim();
    final bl = (block ?? '').trim();
    final pn = (plotNo ?? '').trim();
    final pt = (plotType ?? '').trim();
    final type = pt.isEmpty ? '' : ' [$pt]';
    if (bl.isEmpty) return '$sb / Plot $pn$type';
    return '$sb / $bl / Plot $pn$type';
  }
}
