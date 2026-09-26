import '../db/app_database.dart';
import 'block_monthly_store.dart';
import 'plot_inventory_store.dart';

enum ReportKind {
  allocationList,
  customerTotals,
  receiptsDetail,
  dues,
  aging,
  monthlyWise,
  dailyWise,
  inventoryStatus,
  profitLoss,
  profitCogs,
  separateCustomer,
  customerSenior,
  brokerDetails,
  vipCustomerReceiptStatement,
  vipCompleteDuesTracker,
  vipSocietyProfitClosing,
  vipBrokerDetails,
  vipDuesAging,
  ownerPartner,
  temporaryAllotment,
  temporaryAllotmentBlank,
  lasbelaVillage,
  lasbelaMarket,
  backupRestore,
  receiptsMinusPartnerCredit,
  availablePlots,
  expenseReport,
  vipAllocateTotals,
  installmentDetection,
  monthCollectionVip,
  vipBusinessAnalysis,
  roughInstallmentEstimate,
}

class ReportLine {
  const ReportLine({
    required this.title,
    required this.subtitle,
    this.meta = '',
    this.amount,
    this.mobile = '',
    this.whatsAppMessage = '',
  });

  final String title;
  final String subtitle;
  final String meta;
  final double? amount;
  final String mobile;
  final String whatsAppMessage;
}

class ReportResult {
  const ReportResult({
    required this.lines,
    this.summary = '',
  });

  final List<ReportLine> lines;
  final String summary;
}

/// WinForms AllReportForm queries — mobile preview (same SQLite schema).
abstract final class ReportsStore {
  static Future<List<IdName>> societies() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('societies', orderBy: 'name COLLATE NOCASE');
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<List<IdName>> customers() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('customers', orderBy: 'name COLLATE NOCASE');
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<List<IdName>> brokers() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('brokers', orderBy: 'name COLLATE NOCASE');
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<List<IdName>> customerPlots() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery('''
SELECT cp.id,
  c.name || ' / ' || IFNULL(s.name,'') || ' / Plot ' || IFNULL(p.plot_no,'') AS name
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
WHERE cp.status = 'Active'
ORDER BY c.name, p.plot_no''');
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<List<IdName>> blocksForSociety(int societyId) async {
    if (societyId <= 0) return const [];
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      'SELECT id, name FROM blocks WHERE society_id = ? ORDER BY name COLLATE NOCASE',
      [societyId],
    );
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<List<IdName>> plotTypes() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('plot_types', orderBy: 'code COLLATE NOCASE');
    return rows
        .map((r) => IdName(r['id'] as int, (r['code'] as String? ?? '').toString()))
        .toList();
  }

  static String _ymd(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

  static const _monthNames = <String>[
    'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
    'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
  ];

  static String _monthLabel(DateTime d) =>
      '${_monthNames[d.month - 1]} ${d.year}';

  static String _fmtInt(double v) {
    final n = v.round();
    final s = n.abs().toString();
    final buf = StringBuffer();
    for (var i = 0; i < s.length; i++) {
      if (i > 0 && (s.length - i) % 3 == 0) buf.write(',');
      buf.write(s[i]);
    }
    return n < 0 ? '-$buf' : buf.toString();
  }

  static Future<ReportResult> run(
    ReportKind kind, {
    String keyword = '',
    DateTime? from,
    DateTime? to,
    DateTime? asOf,
    int societyId = 0,
    int blockId = 0,
    int plotTypeId = 0,
    int customerId = 0,
    int brokerId = 0,
    int customerPlotId = 0,
    String? year,
    String? mode,
  }) async {
    final db = await AppDatabase.instance.database;
    final kw = keyword.trim();
    final kwLike = '%$kw%';
    final df = from != null ? _ymd(from) : null;
    final dt = to != null ? _ymd(to) : null;
    final asOfStr = _ymd(asOf ?? DateTime.now());

    switch (kind) {
      case ReportKind.allocationList:
        final rows = await db.rawQuery(
          '''
SELECT cp.booking_date, c.name AS customer_name, s.name AS society_name,
  b.name AS block_name, pt.code AS plot_type_code, p.plot_no,
  cp.price, cp.status
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
JOIN blocks b ON b.id = p.block_id
JOIN societies s ON s.id = p.society_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE IFNULL(cp.status,'') <> 'Cancelled'
  AND (? = '' OR c.name LIKE ? COLLATE NOCASE
  OR pt.code LIKE ? COLLATE NOCASE OR p.plot_no LIKE ? COLLATE NOCASE)
ORDER BY cp.booking_date, cp.id''',
          [kw, kwLike, kwLike, kwLike],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: '${r['customer_name']} · Plot ${r['plot_no']}',
                  subtitle:
                      '${r['society_name']} / ${r['block_name']} / ${r['plot_type_code']} · ${r['booking_date']} · ${r['status']}',
                  amount: (r['price'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.customerTotals:
        final rows = await db.rawQuery(
          '''
WITH bal AS (
  WITH bill AS (
    SELECT customer_plot_id,
           SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge') THEN debit ELSE 0 END) AS billed_debit
    FROM ledger GROUP BY customer_plot_id
  ),
  rc AS (
    SELECT customer_plot_id, SUM(amount) AS total_receipts
    FROM receipts GROUP BY customer_plot_id
  )
  SELECT cp.customer_id,
         COALESCE(bill.billed_debit,0) AS billed_debit,
         COALESCE(rc.total_receipts,0) AS total_receipts,
         CASE WHEN COALESCE(bill.billed_debit,0)>0
              THEN COALESCE(bill.billed_debit,0)-COALESCE(rc.total_receipts,0)
              ELSE COALESCE(cp.price,0)-COALESCE(rc.total_receipts,0)
         END AS due_amount
  FROM customer_plots cp
  LEFT JOIN bill ON bill.customer_plot_id=cp.id
  LEFT JOIN rc   ON rc.customer_plot_id=cp.id
)
SELECT c.id AS customer_id, c.name, c.mobile, c.cnic,
       SUM(bal.billed_debit) AS billed_total,
       SUM(bal.total_receipts) AS receipts_total,
       SUM(bal.due_amount) AS due_total
FROM customers c
LEFT JOIN bal ON bal.customer_id=c.id
WHERE (? = 0 OR c.id = ?)
GROUP BY c.id
ORDER BY c.name''',
          [customerId, customerId],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: r['name'] as String? ?? '',
                  subtitle:
                      'Mobile ${r['mobile'] ?? ''} · CNIC ${r['cnic'] ?? ''}',
                  meta:
                      'Billed ${(r['billed_total'] as num?)?.toStringAsFixed(2) ?? '0'} · Paid ${(r['receipts_total'] as num?)?.toStringAsFixed(2) ?? '0'}',
                  amount: (r['due_total'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.receiptsDetail:
        final rows = await db.rawQuery(
          '''
SELECT r.receipt_date, r.amount, r.mode, r.ref_no, c.name AS customer_name, p.plot_no
FROM receipts r
LEFT JOIN customer_plots cp ON cp.id = r.customer_plot_id
LEFT JOIN customers c ON c.id = cp.customer_id
LEFT JOIN plots p ON p.id = cp.plot_id
WHERE (? = '' OR c.name LIKE ? COLLATE NOCASE OR p.plot_no LIKE ? COLLATE NOCASE)
ORDER BY r.receipt_date, r.id''',
          [kw, kwLike, kwLike],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: '${r['customer_name']} · Plot ${r['plot_no']}',
                  subtitle:
                      '${r['receipt_date']} · ${r['mode']} · Ref ${r['ref_no'] ?? ''}',
                  amount: (r['amount'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.dues:
        final rows = await db.rawQuery('''
WITH bill AS (
  SELECT customer_plot_id,
         SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge')
                  THEN debit ELSE 0 END) AS billed_debit
  FROM ledger GROUP BY customer_plot_id
),
rc AS (
  SELECT customer_plot_id, SUM(amount) AS total_receipts
  FROM receipts GROUP BY customer_plot_id
)
SELECT c.name AS customer_name, cp.booking_date, cp.price,
  COALESCE(bill.billed_debit,0) AS billed_debit,
  COALESCE(rc.total_receipts,0) AS collected_cash,
  CASE WHEN COALESCE(bill.billed_debit,0) > 0
       THEN ROUND(COALESCE(bill.billed_debit,0) - COALESCE(rc.total_receipts,0), 2)
       ELSE ROUND(COALESCE(cp.price,0) - COALESCE(rc.total_receipts,0), 2)
  END AS due_amount
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
LEFT JOIN bill ON bill.customer_plot_id = cp.id
LEFT JOIN rc ON rc.customer_plot_id = cp.id
ORDER BY cp.booking_date, cp.id''');
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: r['customer_name'] as String? ?? '',
                  subtitle:
                      'Booking ${r['booking_date']} · Paid ${(r['collected_cash'] as num?)?.toStringAsFixed(2)}',
                  amount: (r['due_amount'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.aging:
      case ReportKind.vipDuesAging:
        final rows = await db.rawQuery(
          '''
WITH deb AS (
  SELECT l.customer_plot_id,
    MIN(l.txn_date) AS oldest_unpaid_date,
    SUM(CASE WHEN l.txn_type IN ('Booking','InstallmentDue','Surcharge')
             THEN l.debit ELSE 0 END) AS billed
  FROM ledger l
  WHERE date(l.txn_date) <= date(?)
  GROUP BY l.customer_plot_id
),
rc AS (
  SELECT r.customer_plot_id, SUM(r.amount) AS paid
  FROM receipts r
  WHERE date(r.receipt_date) <= date(?)
  GROUP BY r.customer_plot_id
),
q AS (
  SELECT cp.id AS customer_plot_id, cp.customer_id, cp.plot_id,
    COALESCE(deb.billed,0) AS billed, COALESCE(rc.paid,0) AS paid,
    CASE WHEN COALESCE(deb.billed,0) > 0
      THEN COALESCE(deb.billed,0) - COALESCE(rc.paid,0)
      ELSE COALESCE(cp.price,0) - COALESCE(rc.paid,0)
    END AS due_amount,
    COALESCE(deb.oldest_unpaid_date, cp.booking_date) AS oldest_unpaid_date
  FROM customer_plots cp
  LEFT JOIN deb ON deb.customer_plot_id = cp.id
  LEFT JOIN rc ON rc.customer_plot_id = cp.id
),
base AS (
  SELECT q.*,
    CASE WHEN p.plot_no IS NOT NULL AND p.plot_no <> ''
      THEN c.name || '/' || p.plot_no ELSE c.name END AS customer_name,
    p.plot_no, pt.code AS plot_type_code,
    CASE
      WHEN q.due_amount <= 0 THEN 'Current'
      WHEN (julianday(?) - julianday(q.oldest_unpaid_date)) <= 30 THEN '0-30'
      WHEN (julianday(?) - julianday(q.oldest_unpaid_date)) <= 60 THEN '31-60'
      WHEN (julianday(?) - julianday(q.oldest_unpaid_date)) <= 90 THEN '61-90'
      ELSE '>90'
    END AS aging_bucket
  FROM q
  LEFT JOIN customers c ON c.id = q.customer_id
  LEFT JOIN plots p ON p.id = q.plot_id
  LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
)
SELECT * FROM base
WHERE (? = '' OR customer_name LIKE ? COLLATE NOCASE
  OR plot_no LIKE ? COLLATE NOCASE OR plot_type_code LIKE ? COLLATE NOCASE)
ORDER BY aging_bucket DESC, due_amount DESC, customer_name''',
          [
            asOfStr,
            asOfStr,
            asOfStr,
            asOfStr,
            asOfStr,
            asOfStr,
            kw,
            kwLike,
            kwLike,
            kwLike,
          ],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: '${r['customer_name']} · ${r['aging_bucket']}',
                  subtitle: 'As of $asOfStr · Plot ${r['plot_no']}',
                  amount: (r['due_amount'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.monthlyWise:
        final rows = await db.rawQuery(
          '''
SELECT strftime('%Y-%m', r.receipt_date) AS yyyymm, SUM(r.amount) AS total_amount
FROM receipts r
WHERE (? IS NULL OR strftime('%Y', r.receipt_date) = ?)
GROUP BY strftime('%Y-%m', r.receipt_date)
ORDER BY yyyymm''',
          [year, year],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: r['yyyymm'] as String? ?? '',
                  subtitle: 'Monthly receipts',
                  amount: (r['total_amount'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.dailyWise:
        final rows = await db.rawQuery(
          '''
SELECT DATE(r.receipt_date) AS receipt_date, c.name AS customer_name,
  SUM(r.amount) AS total_amount,
  SUM(CASE WHEN r.mode='Cash' THEN r.amount ELSE 0 END) AS cash_amount,
  SUM(CASE WHEN r.mode='Bank' THEN r.amount ELSE 0 END) AS bank_amount,
  COUNT(*) AS receipt_count
FROM receipts r
LEFT JOIN customer_plots cp ON cp.id = r.customer_plot_id
LEFT JOIN customers c ON c.id = cp.customer_id
WHERE (? = '' OR c.name LIKE ? COLLATE NOCASE)
GROUP BY DATE(r.receipt_date), c.name
ORDER BY DATE(r.receipt_date), c.name''',
          [kw, kwLike],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: '${r['receipt_date']} · ${r['customer_name']}',
                  subtitle:
                      'Cash ${(r['cash_amount'] as num?)?.toStringAsFixed(2)} · Bank ${(r['bank_amount'] as num?)?.toStringAsFixed(2)} · ${r['receipt_count']} rcp',
                  amount: (r['total_amount'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.inventoryStatus:
        final rows = await db.rawQuery('''
SELECT s.name AS society, b.name AS block, pt.code AS plot_type,
  SUM(CASE WHEN p.status='Available' THEN 1 ELSE 0 END) AS available,
  SUM(CASE WHEN p.status='Hold' THEN 1 ELSE 0 END) AS hold,
  SUM(CASE WHEN p.status='Sold' THEN 1 ELSE 0 END) AS sold
FROM plots p
JOIN blocks b ON b.id = p.block_id
JOIN societies s ON s.id = p.society_id
JOIN plot_types pt ON pt.id = p.plot_type_id
GROUP BY s.name, b.name, pt.code
ORDER BY s.name, b.name, pt.code''');
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: '${r['society']} / ${r['block']} / ${r['plot_type']}',
                  subtitle:
                      'Available ${r['available']} · Hold ${r['hold']} · Sold ${r['sold']}',
                ),
              )
              .toList(),
        );

      case ReportKind.profitLoss:
      case ReportKind.profitCogs:
        final rows = await db.rawQuery(
          '''
WITH ex AS (
  SELECT
    SUM(CASE
      WHEN LOWER(TRIM(IFNULL(Category,''))) LIKE 'cogs%'
        OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'cost of sales%'
        OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'material%'
        OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'labor%'
        OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'wage%'
      THEN COALESCE(Amount,0) ELSE 0 END) AS cogs_sum,
    SUM(CASE
      WHEN LOWER(TRIM(IFNULL(Category,''))) LIKE 'cogs%'
        OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'cost of sales%'
        OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'material%'
        OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'labor%'
        OR LOWER(TRIM(IFNULL(Category,''))) LIKE 'wage%'
      THEN 0 ELSE COALESCE(Amount,0) END) AS opex_sum
  FROM Expensetbl e
  WHERE (? IS NULL OR DATE(e.EDate) >= DATE(?))
    AND (? IS NULL OR DATE(e.EDate) < DATE(?, '+1 day'))
),
rc AS (
  SELECT SUM(COALESCE(r.amount,0)) AS cash_in
  FROM receipts r
  WHERE (? IS NULL OR r.mode = ?)
    AND (? IS NULL OR DATE(r.receipt_date) >= DATE(?))
    AND (? IS NULL OR DATE(r.receipt_date) < DATE(?, '+1 day'))
),
bc AS (
  SELECT SUM(COALESCE(cp.broker_commission,0)) AS broker_sum
  FROM customer_plots cp
  WHERE IFNULL(cp.status,'Active') = 'Active'
    AND (? IS NULL OR DATE(cp.booking_date) >= DATE(?))
    AND (? IS NULL OR DATE(cp.booking_date) < DATE(?, '+1 day'))
)
SELECT
  ROUND(COALESCE(rc.cash_in,0), 2) AS revenue_cash,
  ROUND(COALESCE(ex.cogs_sum,0), 2) AS cogs_cash,
  ROUND(COALESCE(rc.cash_in,0) - COALESCE(ex.cogs_sum,0), 2) AS gross_profit_cash,
  ROUND(COALESCE(ex.opex_sum,0), 2) AS operating_expense,
  ROUND(COALESCE(bc.broker_sum,0), 2) AS broker_commission,
  ROUND((COALESCE(rc.cash_in,0) - COALESCE(ex.cogs_sum,0))
        - (COALESCE(ex.opex_sum,0) + COALESCE(bc.broker_sum,0)), 2) AS net_profit_cash
FROM ex CROSS JOIN rc CROSS JOIN bc''',
          [df, df, dt, dt, mode, mode, df, df, dt, dt, df, df, dt, dt],
        );
        final r = rows.isEmpty ? <String, Object?>{} : rows.first;
        return ReportResult(
          summary:
              'Net ${(r['net_profit_cash'] as num?)?.toStringAsFixed(2) ?? '0'}',
          lines: [
            ReportLine(
              title: 'Revenue (Cash)',
              subtitle: 'Receipts',
              amount: (r['revenue_cash'] as num?)?.toDouble(),
            ),
            ReportLine(
              title: 'COGS',
              subtitle: 'Cash basis',
              amount: (r['cogs_cash'] as num?)?.toDouble(),
            ),
            ReportLine(
              title: 'Gross Profit',
              subtitle: 'Revenue − COGS',
              amount: (r['gross_profit_cash'] as num?)?.toDouble(),
            ),
            ReportLine(
              title: 'Operating Expense',
              subtitle: 'OPEX',
              amount: (r['operating_expense'] as num?)?.toDouble(),
            ),
            ReportLine(
              title: 'Broker Commission',
              subtitle: 'Allocate form',
              amount: (r['broker_commission'] as num?)?.toDouble(),
            ),
            ReportLine(
              title: 'Net Profit',
              subtitle: 'Revenue − COGS − Expense − Broker',
              amount: (r['net_profit_cash'] as num?)?.toDouble(),
            ),
          ],
        );

      case ReportKind.separateCustomer:
        final rows = await db.rawQuery(
          '''
SELECT r.id, r.receipt_date, r.amount, r.mode, r.ref_no,
  c.name AS customer_name, s.name AS society, p.plot_no, cp.price
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
JOIN societies s ON s.id = p.society_id
WHERE (? IS NULL OR DATE(r.receipt_date) >= DATE(?))
  AND (? IS NULL OR DATE(r.receipt_date) < DATE(?, '+1 day'))
  AND (? = '' OR r.ref_no LIKE ? COLLATE NOCASE OR c.name LIKE ? COLLATE NOCASE
       OR s.name LIKE ? COLLATE NOCASE OR p.plot_no LIKE ? COLLATE NOCASE)
ORDER BY r.receipt_date DESC, r.id DESC''',
          [df, df, dt, dt, kw, kwLike, kwLike, kwLike, kwLike],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: '${r['customer_name']} · Plot ${r['plot_no']}',
                  subtitle:
                      '${r['receipt_date']} · ${r['society']} · ${r['mode']} · ${r['ref_no'] ?? ''}',
                  amount: (r['amount'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.customerSenior:
        final rows = await db.rawQuery(
          '''
SELECT c.name, c.mobile, c.cnic, COUNT(cp.id) AS plots_count,
  SUM(COALESCE(r.total_receipts,0)) AS total_received,
  SUM(CASE WHEN COALESCE(l_bill.billed_debit,0) > 0
        THEN COALESCE(l_bill.billed_debit,0) - COALESCE(r.total_receipts,0)
        ELSE COALESCE(cp.price,0) - COALESCE(r.total_receipts,0) END) AS total_due,
  GROUP_CONCAT(p.plot_no, ', ') AS plots
FROM customers c
LEFT JOIN customer_plots cp ON cp.customer_id=c.id
LEFT JOIN plots p ON p.id=cp.plot_id
LEFT JOIN plot_types pt ON pt.id=p.plot_type_id
LEFT JOIN (
  SELECT customer_plot_id,
         SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge')
                  THEN debit ELSE 0 END) AS billed_debit
  FROM ledger GROUP BY customer_plot_id
) l_bill ON l_bill.customer_plot_id=cp.id
LEFT JOIN (
  SELECT customer_plot_id, SUM(amount) AS total_receipts
  FROM receipts GROUP BY customer_plot_id
) r ON r.customer_plot_id=cp.id
WHERE (? = '' OR c.name LIKE ? COLLATE NOCASE
  OR pt.code LIKE ? COLLATE NOCASE OR p.plot_no LIKE ? COLLATE NOCASE)
GROUP BY c.id
ORDER BY c.name''',
          [kw, kwLike, kwLike, kwLike],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: r['name'] as String? ?? '',
                  subtitle:
                      'Plots ${r['plots'] ?? ''} · Paid ${(r['total_received'] as num?)?.toStringAsFixed(2)}',
                  amount: (r['total_due'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.brokerDetails:
      case ReportKind.vipBrokerDetails:
        final rows = await db.rawQuery(
          '''
SELECT IFNULL(b.name,'(No Broker)') AS broker_name, c.name AS customer_name, s.name AS society_name,
  IFNULL(bl.name,'') AS block_name, IFNULL(pt.code,'') AS plot_type,
  p.plot_no, cp.price, cp.broker_commission, cp.booking_date
FROM customer_plots cp
LEFT JOIN brokers b ON b.id = cp.broker_id
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks bl ON bl.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE cp.status = 'Active'
  AND (IFNULL(cp.broker_id,0) > 0 OR IFNULL(cp.broker_commission,0) <> 0)
  AND (? IS NULL OR DATE(cp.booking_date) >= DATE(?))
  AND (? IS NULL OR DATE(cp.booking_date) <= DATE(?))
  AND (? = 0 OR p.society_id = ?)
  AND (? = 0 OR cp.broker_id = ?)
  AND (? = '' OR IFNULL(b.name,'') LIKE ? COLLATE NOCASE OR c.name LIKE ? COLLATE NOCASE
       OR s.name LIKE ? COLLATE NOCASE OR p.plot_no LIKE ? COLLATE NOCASE)
ORDER BY b.name, s.name, c.name''',
          [
            df,
            df,
            dt,
            dt,
            societyId,
            societyId,
            brokerId,
            brokerId,
            kw,
            kwLike,
            kwLike,
            kwLike,
            kwLike,
          ],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: '${r['broker_name']} · ${r['customer_name']}',
                  subtitle:
                      '${r['society_name']} / ${r['block_name']} / ${r['plot_type']} Plot ${r['plot_no']} · ${r['booking_date']}',
                  meta:
                      'Price ${(r['price'] as num?)?.toStringAsFixed(2)}',
                  amount: (r['broker_commission'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.vipCompleteDuesTracker:
        final rows = await db.rawQuery(
          '''
SELECT cp.id AS customer_plot_id, c.name AS customer_name, c.mobile, s.name AS society_name,
  b.name AS block_name, p.plot_no, cp.booking_date, COALESCE(cp.price,0) AS plot_price,
  COALESCE((SELECT SUM(amount) FROM receipts r WHERE r.customer_plot_id = cp.id),0) AS total_paid,
  COALESCE((SELECT MAX(receipt_date) FROM receipts r2 WHERE r2.customer_plot_id = cp.id),'') AS last_payment_date
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
WHERE cp.status = 'Active'
  AND (? = 0 OR p.society_id = ?)
  AND (? = '' OR c.name LIKE ? COLLATE NOCASE OR c.mobile LIKE ? COLLATE NOCASE
       OR CAST(p.plot_no AS TEXT) LIKE ? COLLATE NOCASE OR s.name LIKE ? COLLATE NOCASE)
ORDER BY s.name, c.name, p.plot_no''',
          [societyId, societyId, kw, kwLike, kwLike, kwLike, kwLike],
        );
        final asOfDate = asOf ?? DateTime.now();
        final lines = <ReportLine>[];
        for (final r in rows) {
          final price = (r['plot_price'] as num?)?.toDouble() ?? 0;
          final paid = (r['total_paid'] as num?)?.toDouble() ?? 0;
          var due = price - paid;
          if (due < 0) due = 0;
          final cpId = r['customer_plot_id'] as int;
          final bookingStr = r['booking_date'] as String? ?? '';
          final paidMonthsRows = await db.rawQuery(
            '''
SELECT DISTINCT strftime('%Y-%m', receipt_date) AS ym
FROM receipts WHERE customer_plot_id = ? AND receipt_date IS NOT NULL''',
            [cpId],
          );
          final paidMonths = paidMonthsRows
              .map((x) => x['ym'] as String? ?? '')
              .where((x) => x.isNotEmpty)
              .toSet();
          final missed = _missedMonths(bookingStr, asOfDate, paidMonths);
          final name = '${r['customer_name'] ?? ''}';
          final society = '${r['society_name'] ?? ''}';
          final block = '${r['block_name'] ?? ''}';
          final plot = '${r['plot_no'] ?? ''}';
          final last = '${r['last_payment_date'] ?? ''}';
          final n0 = due.round();
          final paidN = paid.round();
          final priceN = price.round();
          final wa = StringBuffer()
            ..writeln('Assalam o Alaikum,')
            ..writeln()
            ..writeln('*$name* — Dues Summary')
            ..writeln('Society: $society')
            ..writeln('Block/Plot: $block / $plot')
            ..writeln('Booking: $bookingStr')
            ..writeln('Plot Price: $priceN')
            ..writeln('Total Paid: $paidN')
            ..writeln('*Remaining Due: $n0*')
            ..writeln('Missed Months (${missed.count}): ${missed.csv}');
          if (last.trim().isNotEmpty) wa.writeln('Last Payment: $last');
          wa
            ..writeln()
            ..writeln('Hub Builders');
          lines.add(
            ReportLine(
              title: '$name · Plot $plot',
              subtitle:
                  '$society / $block · Paid $paid · Last ${r['last_payment_date']}'
                  '\nMissed (${missed.count}): ${missed.csv}',
              amount: due,
              mobile: r['mobile'] as String? ?? '',
              whatsAppMessage: wa.toString(),
            ),
          );
        }
        return ReportResult(lines: lines);

      case ReportKind.vipSocietyProfitClosing:
        final rows = await db.rawQuery(
          '''
SELECT
  COALESCE(s.name, '(Unknown)') AS society_name,
  COALESCE((SELECT COUNT(1) FROM plots px WHERE px.society_id = s.id),0) AS plots_total,
  COALESCE((SELECT COUNT(1) FROM plots px WHERE px.society_id = s.id AND px.status='Available'),0) AS plots_available,
  COALESCE((SELECT COUNT(1) FROM plots px WHERE px.society_id = s.id AND px.status='Hold'),0) AS plots_booked,
  COALESCE((SELECT COUNT(1) FROM plots px WHERE px.society_id = s.id AND px.status='Sold'),0) AS plots_sold,
  COALESCE((
    SELECT SUM(COALESCE(r.amount,0)) FROM receipts r
    JOIN customer_plots cp ON cp.id = r.customer_plot_id
    JOIN plots p ON p.id = cp.plot_id
    WHERE p.society_id = s.id
      AND (? IS NULL OR DATE(r.receipt_date) >= DATE(?))
      AND (? IS NULL OR DATE(r.receipt_date) < DATE(?, '+1 day'))
  ),0) AS receipts_amount
FROM societies s
WHERE (? = 0 OR s.id = ?)
ORDER BY s.name''',
          [df, df, dt, dt, societyId, societyId],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: r['society_name'] as String? ?? '',
                  subtitle:
                      'Plots ${r['plots_total']} (A ${r['plots_available']} / H ${r['plots_booked']} / S ${r['plots_sold']})',
                  amount: (r['receipts_amount'] as num?)?.toDouble(),
                ),
              )
              .toList(),
        );

      case ReportKind.vipCustomerReceiptStatement:
        if (customerPlotId <= 0) {
          return const ReportResult(
            lines: [],
            summary: 'Allocation select karein.',
          );
        }
        final hdr = await db.rawQuery(
          '''
SELECT c.name, c.father_name, c.mobile, c.cnic, s.name AS society, b.name AS block,
  p.plot_no, pt.code AS plot_type, cp.price
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE cp.id = ?''',
          [customerPlotId],
        );
        final ledger = await db.rawQuery(
          '''
SELECT txn_date, narration, debit, credit, txn_type
FROM ledger WHERE customer_plot_id = ?
  AND (? IS NULL OR DATE(txn_date) >= DATE(?))
  AND (? IS NULL OR DATE(txn_date) <= DATE(?))
ORDER BY DATE(txn_date), id''',
          [customerPlotId, df, df, dt, dt],
        );
        final h = hdr.isEmpty ? <String, Object?>{} : hdr.first;
        final lines = <ReportLine>[
          ReportLine(
            title: h['name'] as String? ?? '',
            subtitle:
                'S/O ${h['father_name'] ?? ''} · ${h['mobile'] ?? ''} · ${h['cnic'] ?? ''}',
            meta:
                '${h['society']} / ${h['block']} / Plot ${h['plot_no']} [${h['plot_type']}]',
            amount: (h['price'] as num?)?.toDouble(),
          ),
          ...ledger.map(
            (r) => ReportLine(
              title: '${r['txn_date']} · ${r['txn_type']}',
              subtitle: r['narration'] as String? ?? '',
              meta:
                  'Dr ${(r['debit'] as num?)?.toStringAsFixed(2)} · Cr ${(r['credit'] as num?)?.toStringAsFixed(2)}',
            ),
          ),
        ];
        return ReportResult(lines: lines);

      case ReportKind.ownerPartner:
        final rows = await db.rawQuery('''
SELECT name, phone, note,
  CASE WHEN is_owner=1 AND is_partner=1 THEN 'Owner+Partner'
       WHEN is_owner=1 THEN 'Owner'
       WHEN is_partner=1 THEN 'Partner'
       ELSE 'Person' END AS role
FROM Person
ORDER BY name''');
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: r['name'] as String? ?? '',
                  subtitle: '${r['role']} · ${r['phone'] ?? ''}',
                  meta: r['note'] as String? ?? '',
                ),
              )
              .toList(),
        );

      case ReportKind.temporaryAllotment:
      case ReportKind.temporaryAllotmentBlank:
      case ReportKind.lasbelaVillage:
      case ReportKind.lasbelaMarket:
        // Same data basis as allocation list — print templates are UI/PDF on WinForms.
        return run(
          ReportKind.allocationList,
          keyword: keyword,
          from: from,
          to: to,
        );

      case ReportKind.backupRestore:
        final path = await AppDatabase.instance.dbPath;
        return ReportResult(
          summary: path,
          lines: [
            ReportLine(
              title: 'Local SQLite database',
              subtitle: path,
              meta: 'Copy/restore this file (same as WinForms Backup).',
            ),
          ],
        );

      case ReportKind.receiptsMinusPartnerCredit:
        final rec = await db.rawQuery(
          '''
SELECT COALESCE(SUM(amount),0) AS t FROM receipts
WHERE (? IS NULL OR DATE(receipt_date) >= DATE(?))
  AND (? IS NULL OR DATE(receipt_date) < DATE(?, '+1 day'))''',
          [df, df, dt, dt],
        );
        double partnerIn = 0, partnerOut = 0, creditForm = 0;
        try {
          final pin = await db.rawQuery(
            '''
SELECT COALESCE(SUM(amount),0) AS t FROM partner_money
WHERE UPPER(IFNULL(direction,'')) = 'IN'
  AND (IFNULL(TRIM(txn_date),'') = ''
       OR ((? IS NULL OR DATE(txn_date) >= DATE(?))
           AND (? IS NULL OR DATE(txn_date) < DATE(?, '+1 day'))))''',
            [df, df, dt, dt],
          );
          partnerIn = (pin.first['t'] as num?)?.toDouble() ?? 0;
          final pout = await db.rawQuery(
            '''
SELECT COALESCE(SUM(amount),0) AS t FROM partner_money
WHERE UPPER(IFNULL(direction,'')) = 'OUT'
  AND (IFNULL(TRIM(txn_date),'') = ''
       OR ((? IS NULL OR DATE(txn_date) >= DATE(?))
           AND (? IS NULL OR DATE(txn_date) < DATE(?, '+1 day'))))''',
            [df, df, dt, dt],
          );
          partnerOut = (pout.first['t'] as num?)?.toDouble() ?? 0;
        } catch (_) {}
        try {
          final cr = await db.rawQuery(
            '''
SELECT COALESCE(SUM(amount),0) AS t FROM Credit
WHERE (IFNULL(TRIM(pay_date),'') = ''
       OR ((? IS NULL OR DATE(pay_date) >= DATE(?))
           AND (? IS NULL OR DATE(pay_date) < DATE(?, '+1 day'))))''',
            [df, df, dt, dt],
          );
          creditForm = (cr.first['t'] as num?)?.toDouble() ?? 0;
        } catch (_) {}
        final receipts = (rec.first['t'] as num?)?.toDouble() ?? 0;
        final partnerCash = partnerIn + partnerOut;
        final partnerTotal = partnerCash + creditForm;
        final bal = receipts - partnerTotal;
        return ReportResult(
          summary:
              'Receipts $receipts − Partner Cash $partnerCash = Balance $bal',
          lines: [
            ReportLine(
              title: 'Total Receipts (Receipt Form)',
              subtitle: 'Date filter applied',
              amount: receipts,
            ),
            ReportLine(
              title: 'Partner Cash Book — In',
              subtitle: 'partner_money In',
              amount: partnerIn,
            ),
            ReportLine(
              title: 'Partner Cash Book — Out',
              subtitle: 'partner_money Out',
              amount: partnerOut,
            ),
            ReportLine(
              title: 'Partner Cash Book Total',
              subtitle: 'In + Out',
              amount: partnerCash,
            ),
            if (creditForm > 0)
              ReportLine(
                title: 'Credit Form (Partner)',
                subtitle: 'Credit table',
                amount: creditForm,
              ),
            ReportLine(
              title: 'Balance (Receipts − Partner Credit)',
              subtitle: 'Receipts minus Partner Cash Book',
              amount: bal,
            ),
          ],
        );

      case ReportKind.availablePlots:
        // WinForms: society required. Block / plot type optional (0 = all).
        if (societyId <= 0) {
          return const ReportResult(
            summary: 'Pehle Society select karein.',
            lines: [],
          );
        }
        // Same hygiene as Allocate picker (PlotInventoryStore.availablePlots),
        // hardened case-insensitive so Installment/Sold never linger as Available.
        try {
          await db.execute('''
UPDATE customer_plots
SET status = 'Active'
WHERE LOWER(TRIM(IFNULL(status,''))) = 'cancelled'
  AND (cancelled_date IS NULL OR TRIM(IFNULL(cancelled_date,'')) = '')
  AND IFNULL(cancellation_id,0) = 0''');
          await db.execute('''
UPDATE plots SET status = CASE
  WHEN EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = plots.id
      AND LOWER(TRIM(IFNULL(cp.status,''))) <> 'cancelled'
      AND LOWER(TRIM(IFNULL(cp.deal_type,''))) = 'sold'
  ) THEN 'Sold'
  ELSE 'Hold'
END
WHERE LOWER(TRIM(IFNULL(status,''))) = 'available'
  AND EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = plots.id
      AND LOWER(TRIM(IFNULL(cp.status,''))) <> 'cancelled'
  )''');
        } catch (_) {}
        // At least as strict as Allocate: Available only + no Active allocation.
        // Hold/Sold never listed. deal_type irrelevant. Cancelled does not block.
        final rows = await db.rawQuery(
          '''
SELECT s.name AS society, b.name AS block, pt.code AS plot_type, p.plot_no
FROM plots p
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE LOWER(TRIM(IFNULL(p.status,''))) = 'available'
  AND LOWER(TRIM(IFNULL(p.status,''))) NOT IN ('hold','sold')
  AND NOT EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = p.id
      AND LOWER(TRIM(IFNULL(cp.status,''))) = 'active'
  )
  AND p.society_id = ?
  AND (? = 0 OR p.block_id = ?)
  AND (? = 0 OR p.plot_type_id = ?)
ORDER BY s.name, b.name, pt.code, p.plot_no''',
          [societyId, blockId, blockId, plotTypeId, plotTypeId],
        );
        return ReportResult(
          lines: rows
              .map(
                (r) => ReportLine(
                  title: 'Plot ${r['plot_no']}',
                  subtitle:
                      '${r['society']} / ${r['block']} / ${r['plot_type']} · Available',
                ),
              )
              .toList(),
        );

      case ReportKind.expenseReport:
        final rows = await db.rawQuery(
          '''
SELECT
  COALESCE(e.EDate,'') AS edate,
  COALESCE(e.Name,'') AS name,
  COALESCE(e.Category,'') AS category,
  COALESCE(e.Amount,0) AS amount,
  COALESCE(e.Note,'') AS note
FROM Expensetbl e
WHERE
  (? IS NULL OR DATE(e.EDate) >= DATE(?))
  AND (? IS NULL OR DATE(e.EDate) < DATE(?, '+1 day'))
  AND (
       ? = ''
    OR e.Name LIKE ? COLLATE NOCASE
    OR IFNULL(e.Category,'') LIKE ? COLLATE NOCASE
    OR IFNULL(e.Note,'') LIKE ? COLLATE NOCASE
  )
ORDER BY DATE(e.EDate) ASC, e.sid ASC''',
          [df, df, dt, dt, kw, kwLike, kwLike, kwLike],
        );
        var total = 0.0;
        final lines = <ReportLine>[];
        for (final r in rows) {
          final amt = (r['amount'] as num?)?.toDouble() ?? 0;
          total += amt;
          var d = (r['edate'] as String? ?? '').trim();
          if (d.length >= 10) d = d.substring(0, 10);
          final cat = (r['category'] as String? ?? '').trim();
          final note = (r['note'] as String? ?? '').trim();
          lines.add(
            ReportLine(
              title: (r['name'] as String? ?? '').trim(),
              subtitle: [
                if (d.isNotEmpty) d,
                if (cat.isNotEmpty) cat,
                if (note.isNotEmpty) note,
              ].join(' · '),
              amount: amt,
            ),
          );
        }
        lines.add(
          ReportLine(
            title: 'TOTAL',
            subtitle: '${rows.length} expense entries',
            amount: total,
          ),
        );
        return ReportResult(
          lines: lines,
          summary: 'Expense Report · Total Rs ${total.toStringAsFixed(2)}',
        );

      case ReportKind.vipAllocateTotals:
        final rows = await db.rawQuery(
          '''
SELECT
  COUNT(*) AS plot_count,
  COALESCE(SUM(COALESCE(cp.price,0)),0) AS total_allocate,
  COALESCE(SUM((
    SELECT COALESCE(SUM(r.amount),0)
    FROM receipts r
    WHERE r.customer_plot_id = cp.id
  )),0) AS total_paid
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE cp.status = 'Active'
  AND (? = 0 OR p.society_id = ?)''',
          [societyId, societyId],
        );
        final row = rows.isEmpty ? <String, Object?>{} : rows.first;
        final plotCount = (row['plot_count'] as num?)?.toInt() ?? 0;
        final totalAllocate =
            (row['total_allocate'] as num?)?.toDouble() ?? 0;
        final totalPaid = (row['total_paid'] as num?)?.toDouble() ?? 0;
        final remaining = totalAllocate - totalPaid;
        return ReportResult(
          summary:
              'Plots $plotCount · Allocate ${totalAllocate.toStringAsFixed(2)} · Paid ${totalPaid.toStringAsFixed(2)} · Remaining ${remaining.toStringAsFixed(2)}',
          lines: [
            ReportLine(
              title: 'Allocated Plots (count)',
              subtitle: societyId > 0
                  ? 'Active allocations (filtered society)'
                  : 'All Active allocations',
              amount: plotCount.toDouble(),
            ),
            ReportLine(
              title: 'Total Allocate Amount',
              subtitle: 'Sum of customer_plots.price',
              amount: totalAllocate,
            ),
            ReportLine(
              title: 'Total Paid (Receipts)',
              subtitle: 'Sum of receipts for those allocations',
              amount: totalPaid,
            ),
            ReportLine(
              title: 'Remaining / Baqi Lena',
              subtitle: 'Allocate − Paid',
              amount: remaining,
            ),
          ],
        );

      case ReportKind.installmentDetection:
        final fromDt =
            from ?? DateTime(DateTime.now().year, DateTime.now().month, 1);
        final toDt = to ?? DateTime.now();
        final fromY = _ymd(DateTime(fromDt.year, fromDt.month, fromDt.day));
        final toY = _ymd(DateTime(toDt.year, toDt.month, toDt.day));
        final detRows = await db.rawQuery(
          '''
SELECT cp.id AS customer_plot_id, cp.customer_id, c.name AS customer_name,
  s.name AS society_name, b.name AS block_name, p.plot_no, cp.booking_date,
  COALESCE(cp.monthly_installment, 0) AS monthly_installment,
  COALESCE(bmi.monthly_amount, 0) AS block_monthly,
  COALESCE((
    SELECT SUM(r.amount) FROM receipts r
    WHERE r.customer_plot_id = cp.id
      AND r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
      AND substr(r.receipt_date,1,10) >= ?
      AND substr(r.receipt_date,1,10) <= ?
  ), 0) AS paid_in_period
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
WHERE IFNULL(cp.status,'') = 'Active'
  AND (? = 0 OR p.society_id = ?)
ORDER BY s.name, b.name, c.name, p.plot_no''',
          [fromY, toY, societyId, societyId],
        );
        final detailLines = <ReportLine>[];
        final seenCust = <int>{};
        var sn = 0;
        var totalActive = 0;
        var sumExpected = 0.0;
        var sumPaid = 0.0;
        var unpaid = 0;
        var partial = 0;
        var paidOk = 0;
        var notSet = 0;
        final fromDay = DateTime(fromDt.year, fromDt.month, fromDt.day);
        final toDay = DateTime(toDt.year, toDt.month, toDt.day);
        for (final r in detRows) {
          totalActive++;
          var monthly = (r['monthly_installment'] as num?)?.toDouble() ?? 0;
          if (monthly <= 0) {
            monthly = (r['block_monthly'] as num?)?.toDouble() ?? 0;
          }
          final paidAmt = (r['paid_in_period'] as num?)?.toDouble() ?? 0;
          var dueMonths = 0;
          var expected = 0.0;
          String status;
          if (monthly <= 0) {
            // List Active even when monthly resolves to 0.
            status = 'Not set';
            notSet++;
          } else {
            final bookingStr = (r['booking_date'] ?? '').toString();
            DateTime? booking;
            if (bookingStr.length >= 10) {
              booking = DateTime.tryParse(bookingStr.substring(0, 10));
            }
            booking ??= DateTime.tryParse(bookingStr);
            if (booking != null) {
              dueMonths = _dueMonthsInRange(booking, fromDay, toDay);
            }
            expected = monthly * dueMonths;
            if (expected <= 0.009) {
              status = 'No due in range';
              notSet++;
              expected = 0;
            } else if (paidAmt >= expected - 0.009) {
              status = 'Paid';
              paidOk++;
            } else if (paidAmt > 0.009) {
              status = 'Partial';
              partial++;
            } else {
              status = 'Unpaid';
              unpaid++;
            }
          }
          final rem = expected - paidAmt;
          final custId = (r['customer_id'] as num?)?.toInt() ?? 0;
          if (custId > 0) seenCust.add(custId);
          sumExpected += expected;
          sumPaid += paidAmt;
          sn++;
          final cust = (r['customer_name'] ?? '').toString();
          final plot = (r['plot_no'] ?? '').toString();
          final block = (r['block_name'] ?? '').toString();
          final soc = (r['society_name'] ?? '').toString();
          detailLines.add(
            ReportLine(
              title: '$sn. $cust · Plot $plot',
              subtitle:
                  '$soc / $block · Monthly ${monthly.toStringAsFixed(0)} × $dueMonths',
              meta:
                  'Expected ${expected.toStringAsFixed(2)} · Paid ${paidAmt.toStringAsFixed(2)} · $status',
              amount: rem,
            ),
          );
        }
        final sumRem =
            (sumExpected - sumPaid) < 0 ? 0.0 : (sumExpected - sumPaid);
        return ReportResult(
          lines: detailLines,
          summary:
              'Total Active $totalActive · Shown ${detailLines.length} · '
              'Customers ${seenCust.length} · '
              'Total installment ${sumExpected.toStringAsFixed(2)} · '
              'Already collected (Le chuke) ${sumPaid.toStringAsFixed(2)} · '
              'Remaining (Baqi) ${sumRem.toStringAsFixed(2)} · '
              'Paid/Partial/Unpaid/Not set $paidOk/$partial/$unpaid/$notSet',
        );

      case ReportKind.monthCollectionVip:
        await AppDatabase.ensurePartnerMoneySchema();
        await BlockMonthlyStore.ensureSchema();
        final now = DateTime.now();
        final curStart = DateTime(now.year, now.month, 1);
        final curEnd = DateTime(now.year, now.month, now.day);
        final prevStart = DateTime(now.year, now.month - 1, 1);
        final prevEnd = curStart.subtract(const Duration(days: 1));
        final fromDt = from ?? curStart;
        final toDt = to ?? curEnd;
        final fromY = _ymd(DateTime(fromDt.year, fromDt.month, fromDt.day));
        final toY = _ymd(DateTime(toDt.year, toDt.month, toDt.day));
        final curFrom = _ymd(curStart);
        final curTo = _ymd(curEnd);
        final prevFrom = _ymd(prevStart);
        final prevTo = _ymd(prevEnd);

        Future<double> sumRange(String a, String b) async {
          final rows = await db.rawQuery(
            '''
SELECT COALESCE(SUM(r.amount),0) AS t
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
JOIN plots p ON p.id = cp.plot_id
WHERE r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
  AND substr(r.receipt_date,1,10) >= ?
  AND substr(r.receipt_date,1,10) <= ?
  AND (? = 0 OR p.society_id = ?)''',
            [a, b, societyId, societyId],
          );
          return (rows.first['t'] as num?)?.toDouble() ?? 0;
        }

        // Group by Society | Block | monthly only (gaz dropped from key —
        // distinct plot-type sizes joined in Gaz column so Block A ×2 does not appear).
        final groupRows = await db.rawQuery(
          '''
SELECT society_name, block_name,
  REPLACE(GROUP_CONCAT(DISTINCT gaz), ',', ', ') AS gaz,
  monthly, COUNT(*) AS customers
FROM (
  SELECT
    COALESCE(NULLIF(TRIM(s.name),''), '—') AS society_name,
    COALESCE(NULLIF(TRIM(b.name),''), '—') AS block_name,
    COALESCE(NULLIF(TRIM(pt.code),''), '—') AS gaz,
    CASE
      WHEN COALESCE(cp.monthly_installment, 0) > 0 THEN COALESCE(cp.monthly_installment, 0)
      ELSE COALESCE(bmi.monthly_amount, 0)
    END AS monthly
  FROM customer_plots cp
  JOIN plots p ON p.id = cp.plot_id
  LEFT JOIN societies s ON s.id = p.society_id
  LEFT JOIN blocks b ON b.id = p.block_id
  LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
  LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
  WHERE IFNULL(cp.status,'') = 'Active'
    AND (? = 0 OR p.society_id = ?)
) t
GROUP BY society_name, block_name, monthly
ORDER BY society_name COLLATE NOCASE, block_name COLLATE NOCASE, monthly''',
          [societyId, societyId],
        );

        var activeCount = 0;
        var setCount = 0;
        var notSetCount = 0;
        var expectedTotal = 0.0;
        final groupLines = <ReportLine>[];
        var gSn = 0;
        // Grid: only Society|Block|Monthly where resolved monthly > 0.
        // Not set (monthly=0 after block-default resolve) = one summary, not per block.
        for (final r in groupRows) {
          final monthly = (r['monthly'] as num?)?.toDouble() ?? 0;
          final count = (r['customers'] as num?)?.toInt() ?? 0;
          final soc = (r['society_name'] ?? '').toString();
          final block = (r['block_name'] ?? '').toString();
          final gaz = (r['gaz'] ?? '').toString();
          activeCount += count;
          if (monthly > 0.009) {
            gSn++;
            setCount += count;
            final total = monthly * count;
            expectedTotal += total;
            groupLines.add(
              ReportLine(
                title:
                    '$gSn. $soc | $block | Gaz $gaz · $count × ${_fmtInt(monthly)} = ${_fmtInt(total)}',
                subtitle: 'Society · Block · monthly ${_fmtInt(monthly)}',
                meta: 'Active allocations with this society/block/monthly',
                amount: total,
              ),
            );
          } else {
            notSetCount += count;
          }
        }
        if (notSetCount > 0) {
          groupLines.add(
            ReportLine(
              title: 'Not set · $notSetCount customers',
              subtitle: 'Active allocations with monthly not set (after block default)',
              meta: 'Count only — no fake total · not repeated per block',
            ),
          );
        }

        final curTotal = await sumRange(curFrom, curTo);
        final prevTotal = await sumRange(prevFrom, prevTo);

        final detailRows = await db.rawQuery(
          '''
SELECT r.id AS receipt_id, r.receipt_date, r.amount, r.mode,
  COALESCE(NULLIF(TRIM(r.ref_no),''), CAST(r.id AS TEXT)) AS receipt_no,
  c.name AS customer_name, p.plot_no, b.name AS block_name, s.name AS society_name,
  COALESCE(NULLIF(TRIM(per.name),''), '') AS collected_by
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN partner_money pm ON pm.receipt_id = r.id
LEFT JOIN Person per ON per.person_id = pm.partner_id
WHERE r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
  AND substr(r.receipt_date,1,10) >= ?
  AND substr(r.receipt_date,1,10) <= ?
  AND (? = 0 OR p.society_id = ?)
ORDER BY substr(r.receipt_date,1,10) DESC, r.id DESC''',
          [fromY, toY, societyId, societyId],
        );

        final curLabel = _monthLabel(curStart);
        final prevLabel = _monthLabel(prevStart);
        var sn = 0;
        var detailTotal = 0.0;
        final receiptLines = <ReportLine>[];
        for (final r in detailRows) {
          final amt = (r['amount'] as num?)?.toDouble() ?? 0;
          detailTotal += amt;
          sn++;
          final dateRaw = (r['receipt_date'] ?? '').toString();
          final dateDisp =
              dateRaw.length >= 10 ? dateRaw.substring(0, 10) : dateRaw;
          var collected = (r['collected_by'] ?? '').toString().trim();
          if (collected.isEmpty) collected = '—';
          final cust = (r['customer_name'] ?? '').toString();
          final plot = (r['plot_no'] ?? '').toString();
          final block = (r['block_name'] ?? '').toString();
          final soc = (r['society_name'] ?? '').toString();
          final receiptNo = (r['receipt_no'] ?? '').toString();
          final mode = (r['mode'] ?? '').toString();
          receiptLines.add(
            ReportLine(
              title: '$sn. $cust · $amt',
              subtitle: '$dateDisp · Society: $soc · Block: $block · Plot: $plot · $mode',
              meta: 'Collected by: $collected · Receipt $receiptNo',
              amount: amt,
            ),
          );
        }

        final baqiLena =
            expectedTotal > detailTotal ? expectedTotal - detailTotal : 0.0;

        final lines = <ReportLine>[
          ReportLine(
            title: 'Month installment TOTAL (Gross)',
            subtitle: 'Sum of Installment Groups (all customers × monthly)',
            meta: 'Active $activeCount · Set $setCount · Not set $notSetCount',
            amount: expectedTotal,
          ),
          ReportLine(
            title: 'Collection',
            subtitle: 'Wasool in detail period $fromY → $toY',
            meta:
                '$curLabel ${curTotal.toStringAsFixed(0)} · $prevLabel ${prevTotal.toStringAsFixed(0)} · ${receiptLines.length} receipts',
            amount: detailTotal,
          ),
          ReportLine(
            title: 'Still to collect (Baqi Lena)',
            subtitle: 'Month installment TOTAL − Collection (min 0)',
            meta: 'Expected vs Collected vs Remaining',
            amount: baqiLena,
          ),
          ReportLine(
            title: '— Installment Groups (Active) —',
            subtitle:
                'Active $activeCount · Monthly set $setCount · Not set $notSetCount',
            meta: 'Month installment TOTAL (Gross) ${_fmtInt(expectedTotal)}',
            amount: expectedTotal > 0 ? expectedTotal : null,
          ),
          ...groupLines,
          ReportLine(
            title: 'GROSS TOTAL',
            subtitle:
                '$setCount set · Not set $notSetCount · sum of set groups only',
            meta: 'Sum of Society | Block | Monthly (monthly > 0) totals',
            amount: expectedTotal > 0 ? expectedTotal : 0,
          ),
          ReportLine(
            title: '— Collection receipts —',
            subtitle: 'Detail $fromY → $toY · ${receiptLines.length} receipts',
            meta:
                '$curLabel ${curTotal.toStringAsFixed(2)} · $prevLabel ${prevTotal.toStringAsFixed(2)}',
            amount: detailTotal,
          ),
          ...receiptLines,
        ];

        return ReportResult(
          lines: lines,
          summary:
              'Month installment TOTAL (Gross): ${_fmtInt(expectedTotal)}   |   '
              'Collection: ${_fmtInt(detailTotal)}   |   '
              'Still to collect (Baqi Lena): ${_fmtInt(baqiLena)}\n'
              'Active $activeCount · Set $setCount · Not set $notSetCount   ·   '
              'Detail ($fromY → $toY): ${detailTotal.toStringAsFixed(0)}'
              ' · ${receiptLines.length} receipts   ·   '
              '$curLabel: ${curTotal.toStringAsFixed(0)}   ·   '
              '$prevLabel: ${prevTotal.toStringAsFixed(0)}',
        );

      case ReportKind.vipBusinessAnalysis:
        final fromDt =
            from ?? DateTime(DateTime.now().year, DateTime.now().month, 1);
        final toDt = to ?? DateTime.now();
        final fromDay = DateTime(fromDt.year, fromDt.month, fromDt.day);
        final toDay = DateTime(toDt.year, toDt.month, toDt.day);
        final fromY = _ymd(fromDay);
        final toY = _ymd(toDay);

        final lifeRows = await db.rawQuery(
          '''
SELECT
  cp.id AS customer_plot_id,
  cp.customer_id,
  COALESCE(cp.price,0) AS price,
  COALESCE((
    SELECT SUM(r.amount) FROM receipts r WHERE r.customer_plot_id = cp.id
  ), 0) AS paid_life
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
WHERE IFNULL(cp.status,'') = 'Active'
  AND (? = 0 OR p.society_id = ?)''',
          [societyId, societyId],
        );
        var activePlots = 0;
        var totalAllocate = 0.0;
        var lifetimePaid = 0.0;
        final duesCust = <int>{};
        for (final r in lifeRows) {
          activePlots++;
          final price = (r['price'] as num?)?.toDouble() ?? 0;
          final paid = (r['paid_life'] as num?)?.toDouble() ?? 0;
          totalAllocate += price;
          lifetimePaid += paid;
          if (price - paid > 0.009) {
            final cid = (r['customer_id'] as num?)?.toInt() ?? 0;
            if (cid > 0) duesCust.add(cid);
          }
        }
        final baqiLena = totalAllocate - lifetimePaid;

        final periodRows = await db.rawQuery(
          '''
SELECT COALESCE(SUM(r.amount),0) AS t
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
JOIN plots p ON p.id = cp.plot_id
WHERE IFNULL(cp.status,'') = 'Active'
  AND r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
  AND substr(r.receipt_date,1,10) >= ?
  AND substr(r.receipt_date,1,10) <= ?
  AND (? = 0 OR p.society_id = ?)''',
          [fromY, toY, societyId, societyId],
        );
        final periodCollection =
            (periodRows.first['t'] as num?)?.toDouble() ?? 0;

        final expRows = await db.rawQuery(
          '''
SELECT COALESCE(SUM(COALESCE(e.Amount,0)),0) AS t
FROM Expensetbl e
WHERE DATE(e.EDate) >= DATE(?)
  AND DATE(e.EDate) < DATE(?, '+1 day')''',
          [fromY, toY],
        );
        final periodExpense = (expRows.first['t'] as num?)?.toDouble() ?? 0;
        final periodNet = periodCollection - periodExpense;

        final instRows = await db.rawQuery(
          '''
SELECT cp.id AS customer_plot_id, cp.booking_date,
  COALESCE(cp.monthly_installment, 0) AS monthly_installment,
  COALESCE(bmi.monthly_amount, 0) AS block_monthly,
  COALESCE((
    SELECT SUM(r.amount) FROM receipts r
    WHERE r.customer_plot_id = cp.id
      AND r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
      AND substr(r.receipt_date,1,10) >= ?
      AND substr(r.receipt_date,1,10) <= ?
  ), 0) AS paid_in_period
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
WHERE IFNULL(cp.status,'') = 'Active'
  AND (? = 0 OR p.society_id = ?)''',
          [fromY, toY, societyId, societyId],
        );
        var expectedTarget = 0.0;
        var targetCollected = 0.0;
        var remainingTarget = 0.0;
        var installmentDuePlots = 0;
        var paidOk = 0;
        var partial = 0;
        var unpaid = 0;
        for (final r in instRows) {
          var monthly = (r['monthly_installment'] as num?)?.toDouble() ?? 0;
          if (monthly <= 0) {
            monthly = (r['block_monthly'] as num?)?.toDouble() ?? 0;
          }
          if (monthly <= 0) continue;
          final bookingStr = (r['booking_date'] ?? '').toString();
          DateTime? booking;
          if (bookingStr.length >= 10) {
            booking = DateTime.tryParse(bookingStr.substring(0, 10));
          }
          booking ??= DateTime.tryParse(bookingStr);
          if (booking == null) continue;
          final dueMonths = _dueMonthsInRange(booking, fromDay, toDay);
          if (dueMonths <= 0) continue;
          final expected = monthly * dueMonths;
          final paidAmt = (r['paid_in_period'] as num?)?.toDouble() ?? 0;
          installmentDuePlots++;
          expectedTarget += expected;
          targetCollected += paidAmt;
          final rem = expected - paidAmt;
          if (rem > 0.009) remainingTarget += rem;
          if (paidAmt >= expected - 0.009) {
            paidOk++;
          } else if (paidAmt > 0.009) {
            partial++;
          } else {
            unpaid++;
          }
        }

        return ReportResult(
          summary:
              'Allocate ${totalAllocate.toStringAsFixed(2)} · Paid ${lifetimePaid.toStringAsFixed(2)} · '
              'Baqi ${baqiLena.toStringAsFixed(2)} · Period wasool ${periodCollection.toStringAsFixed(2)} · '
              'Target rem ${remainingTarget.toStringAsFixed(2)} · Net ${periodNet.toStringAsFixed(2)}',
          lines: [
            ReportLine(
              title: 'Total Allocate Amount',
              subtitle: '1. Lifetime (Ab tak) · Active plots ki total price',
              amount: totalAllocate,
            ),
            ReportLine(
              title: 'Total Received from Customers',
              subtitle: '1. Lifetime · Ab tak jitna customer ne diya',
              amount: lifetimePaid,
            ),
            ReportLine(
              title: 'Still to Collect (Baqi Lena)',
              subtitle: '1. Lifetime · Allocate − Paid',
              amount: baqiLena,
            ),
            ReportLine(
              title: 'This Period Collection',
              subtitle: '2. Period · Is date range mein kitna wasool',
              amount: periodCollection,
            ),
            ReportLine(
              title: 'Total Expenses (Period)',
              subtitle: '2. Period · Is range ka total kharcha',
              amount: periodExpense,
            ),
            ReportLine(
              title: 'Net (Collection − Expense)',
              subtitle: '2. Period · Wasool minus kharcha',
              amount: periodNet,
            ),
            ReportLine(
              title: 'Expected Target (Installment)',
              subtitle: '3. Installment · Due months × monthly',
              amount: expectedTarget,
            ),
            ReportLine(
              title: 'Already Collected (toward target)',
              subtitle: '3. Installment · Period wasool on due plots',
              amount: targetCollected,
            ),
            ReportLine(
              title: 'Remaining Target',
              subtitle:
                  '3. Installment · Fully paid plots is mein nahi · Paid/Partial/Unpaid $paidOk/$partial/$unpaid',
              amount: remainingTarget,
            ),
            ReportLine(
              title: 'Active Plots',
              subtitle: '4. Counts',
              amount: activePlots.toDouble(),
            ),
            ReportLine(
              title: 'Customers with Dues (Baqi)',
              subtitle: '4. Counts · Jin ka abhi baqi lena hai',
              amount: duesCust.length.toDouble(),
            ),
            ReportLine(
              title: 'Installment-due Plots (Period)',
              subtitle: '4. Counts · Is range mein jinke installment due thay',
              amount: installmentDuePlots.toDouble(),
            ),
          ],
        );

      case ReportKind.roughInstallmentEstimate:
        await BlockMonthlyStore.ensureSchema();
        final fromDt =
            from ?? DateTime(DateTime.now().year, DateTime.now().month, 1);
        final toDt = to ?? DateTime.now();
        final fromDay = DateTime(fromDt.year, fromDt.month, fromDt.day);
        final toDay = DateTime(toDt.year, toDt.month, toDt.day);
        final fromY = _ymd(fromDay);
        final toY = _ymd(toDay);
        final roughRows = await db.rawQuery(
          '''
SELECT cp.id AS customer_plot_id, cp.customer_id, c.name AS customer_name,
  s.name AS society_name, b.name AS block_name, p.plot_no,
  COALESCE(NULLIF(TRIM(pt.code),''), '') AS gaz,
  cp.booking_date,
  COALESCE(cp.price, 0) AS price,
  COALESCE(cp.monthly_installment, 0) AS monthly_installment,
  COALESCE(bmi.monthly_amount, 0) AS block_monthly,
  COALESCE((
    SELECT SUM(r.amount) FROM receipts r WHERE r.customer_plot_id = cp.id
  ), 0) AS paid_life,
  COALESCE((
    SELECT SUM(r.amount) FROM receipts r
    WHERE r.customer_plot_id = cp.id
      AND r.receipt_date IS NOT NULL AND TRIM(r.receipt_date) <> ''
      AND substr(r.receipt_date,1,10) >= ?
      AND substr(r.receipt_date,1,10) <= ?
  ), 0) AS paid_in_period
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
WHERE IFNULL(cp.status,'') = 'Active'
  AND (? = 0 OR p.society_id = ?)
ORDER BY s.name, b.name, c.name, p.plot_no''',
          [fromY, toY, societyId, societyId],
        );

        final avgSocBlockGaz = <String, _AvgAcc>{};
        final avgSocBlock = <String, _AvgAcc>{};
        final avgSoc = <String, _AvgAcc>{};
        for (final r in roughRows) {
          final exact = (r['monthly_installment'] as num?)?.toDouble() ?? 0;
          if (exact <= 0.009) continue;
          final soc = _normKey((r['society_name'] ?? '').toString());
          final blk = _normKey((r['block_name'] ?? '').toString());
          final gaz = _normKey((r['gaz'] ?? '').toString());
          _accAdd(avgSoc, soc, exact);
          _accAdd(avgSocBlock, '$soc|$blk', exact);
          if (gaz.isNotEmpty) _accAdd(avgSocBlockGaz, '$soc|$blk|$gaz', exact);
        }

        final detailLines = <ReportLine>[];
        var sn = 0;
        var totalActive = 0;
        var cntExact = 0;
        var cntBlock = 0;
        var cntEstimate = 0;
        var sumExpected = 0.0;
        var sumPaid = 0.0;

        for (final r in roughRows) {
          totalActive++;
          final price = (r['price'] as num?)?.toDouble() ?? 0;
          final paidLife = (r['paid_life'] as num?)?.toDouble() ?? 0;
          final paidAmt = (r['paid_in_period'] as num?)?.toDouble() ?? 0;
          var due = price - paidLife;
          if (due < 0) due = 0;

          final exact = (r['monthly_installment'] as num?)?.toDouble() ?? 0;
          final blockM = (r['block_monthly'] as num?)?.toDouble() ?? 0;
          late double monthly;
          late String source;
          if (exact > 0.009) {
            monthly = exact;
            source = 'Exact';
            cntExact++;
          } else if (blockM > 0.009) {
            monthly = blockM;
            source = 'Block';
            cntBlock++;
          } else {
            final soc = _normKey((r['society_name'] ?? '').toString());
            final blk = _normKey((r['block_name'] ?? '').toString());
            final gaz = _normKey((r['gaz'] ?? '').toString());
            monthly = _resolveEstimate(
              due: due,
              soc: soc,
              blk: blk,
              gaz: gaz,
              avgSocBlockGaz: avgSocBlockGaz,
              avgSocBlock: avgSocBlock,
              avgSoc: avgSoc,
            );
            source = 'Estimate';
            cntEstimate++;
          }

          var dueMonths = 0;
          var expected = 0.0;
          String status;
          if (monthly <= 0.009) {
            status = 'No estimate';
            monthly = 0;
          } else {
            final bookingStr = (r['booking_date'] ?? '').toString();
            DateTime? booking;
            if (bookingStr.length >= 10) {
              booking = DateTime.tryParse(bookingStr.substring(0, 10));
            }
            booking ??= DateTime.tryParse(bookingStr);
            if (booking != null) {
              dueMonths = _dueMonthsInRange(booking, fromDay, toDay);
            }
            expected = monthly * dueMonths;
            if (expected <= 0.009) {
              status = 'No due in range';
              expected = 0;
            } else if (paidAmt >= expected - 0.009) {
              status = 'Paid';
            } else if (paidAmt > 0.009) {
              status = 'Partial';
            } else {
              status = 'Unpaid';
            }
          }

          var baqi = expected - paidAmt;
          if (baqi < 0) baqi = 0;
          sumExpected += expected;
          sumPaid += paidAmt;
          sn++;
          final cust = (r['customer_name'] ?? '').toString();
          final plot = (r['plot_no'] ?? '').toString();
          final block = (r['block_name'] ?? '').toString();
          final socName = (r['society_name'] ?? '').toString();
          final gazDisp = (r['gaz'] ?? '').toString().trim();
          detailLines.add(
            ReportLine(
              title: '$sn. $cust · Plot $plot · $source',
              subtitle:
                  '$socName / $block · Gaz ${gazDisp.isEmpty ? '—' : gazDisp} · '
                  'Price ${_fmtInt(price)} · Paid ${_fmtInt(paidLife)} · Due ${_fmtInt(due)}',
              meta:
                  'Monthly ${_fmtInt(monthly)} × $dueMonths · '
                  'Expected ${expected.toStringAsFixed(0)} · '
                  'Le chuke ${paidAmt.toStringAsFixed(0)} · '
                  'Baqi ${baqi.toStringAsFixed(0)} · $status',
              amount: baqi,
            ),
          );
        }

        final sumRem =
            (sumExpected - sumPaid) < 0 ? 0.0 : (sumExpected - sumPaid);
        return ReportResult(
          lines: detailLines,
          summary:
              'ROUGH / Estimate · Active $totalActive · '
              'Exact/Block/Estimate $cntExact/$cntBlock/$cntEstimate · '
              'Total installment ${sumExpected.toStringAsFixed(0)} · '
              'Le chuke ${sumPaid.toStringAsFixed(0)} · '
              'Baqi ${sumRem.toStringAsFixed(0)}',
        );
    }
  }

  static String _normKey(String s) => s.trim().toUpperCase();

  static void _accAdd(Map<String, _AvgAcc> map, String key, double monthly) {
    if (key.isEmpty) return;
    final acc = map.putIfAbsent(key, _AvgAcc.new);
    acc.sum += monthly;
    acc.count++;
  }

  static double? _accAvg(Map<String, _AvgAcc> map, String key) {
    final acc = map[key];
    if (acc == null || acc.count < 1) return null;
    return acc.sum / acc.count;
  }

  static double _resolveEstimate({
    required double due,
    required String soc,
    required String blk,
    required String gaz,
    required Map<String, _AvgAcc> avgSocBlockGaz,
    required Map<String, _AvgAcc> avgSocBlock,
    required Map<String, _AvgAcc> avgSoc,
  }) {
    double? avg;
    if (gaz.isNotEmpty) {
      avg = _accAvg(avgSocBlockGaz, '$soc|$blk|$gaz');
    }
    if (avg == null || avg <= 0.009) {
      avg = _accAvg(avgSocBlock, '$soc|$blk');
    }
    if (avg == null || avg <= 0.009) {
      avg = _accAvg(avgSoc, soc);
    }
    if (avg != null && avg > 0.009) {
      return avg.roundToDouble();
    }
    if (due <= 0.009) return 0;
    var fallback = (due / 12.0).roundToDouble();
    if (fallback < 1) fallback = 1;
    return fallback;
  }

  /// Calendar months after booking overlapping [from,to]; needs booking+30d <= to.
  static int _dueMonthsInRange(DateTime booking, DateTime from, DateTime to) {
    final b = DateTime(booking.year, booking.month, booking.day);
    final f = DateTime(from.year, from.month, from.day);
    final t = DateTime(to.year, to.month, to.day);
    if (t.isBefore(f)) return 0;
    if (t.difference(b).inDays < 30) return 0;
    final firstDue = DateTime(b.year, b.month + 1, 1);
    final fromMonth = DateTime(f.year, f.month, 1);
    final toMonth = DateTime(t.year, t.month, 1);
    var start = firstDue.isAfter(fromMonth) ? firstDue : fromMonth;
    if (start.isAfter(toMonth)) return 0;
    var n = 0;
    var m = start;
    while (!m.isAfter(toMonth)) {
      n++;
      m = DateTime(m.year, m.month + 1, 1);
    }
    return n;
  }

  /// WinForms Mode A: months AFTER booking month through AsOf with no receipt.
  static ({String csv, int count}) _missedMonths(
    String bookingStr,
    DateTime asOf,
    Set<String> paidMonths,
  ) {
    DateTime? booking;
    try {
      if (bookingStr.length >= 10) {
        booking = DateTime.tryParse(bookingStr.substring(0, 10));
      }
      booking ??= DateTime.tryParse(bookingStr);
    } catch (_) {}
    if (booking == null) return (csv: '—', count: 0);

    var cursor = DateTime(booking.year, booking.month + 1, 1);
    final end = DateTime(asOf.year, asOf.month, 1);
    if (cursor.isAfter(end)) return (csv: '—', count: 0);

    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    final missed = <String>[];
    while (!cursor.isAfter(end)) {
      final key =
          '${cursor.year.toString().padLeft(4, '0')}-${cursor.month.toString().padLeft(2, '0')}';
      if (!paidMonths.contains(key)) {
        missed.add('${months[cursor.month - 1]}-${cursor.year}');
      }
      cursor = DateTime(cursor.year, cursor.month + 1, 1);
    }
    if (missed.isEmpty) return (csv: 'None', count: 0);
    return (csv: missed.join(', '), count: missed.length);
  }
}

class _AvgAcc {
  double sum = 0;
  int count = 0;
}
