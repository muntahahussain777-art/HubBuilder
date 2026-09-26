import '../db/app_database.dart';
import 'plot_inventory_store.dart';

class PlotStatusRow {
  const PlotStatusRow({
    required this.kind,
    required this.society,
    required this.block,
    required this.plotType,
    required this.plotNo,
    required this.customer,
  });

  final String kind;
  final String society;
  final String block;
  final String plotType;
  final String plotNo;
  final String customer;
}

class PlotStatusCounts {
  const PlotStatusCounts({
    this.available = 0,
    this.installment = 0,
    this.reserve = 0,
    this.sold = 0,
  });

  final int available;
  final int installment;
  final int reserve;
  final int sold;
  int get total => available + installment + reserve + sold;
}

class PlotStatusResult {
  const PlotStatusResult({
    required this.rows,
    required this.counts,
    required this.societyName,
  });

  final List<PlotStatusRow> rows;
  final PlotStatusCounts counts;
  final String societyName;
}

abstract final class PlotStatusStore {
  static Future<List<IdName>> societies() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('societies', orderBy: 'name COLLATE NOCASE');
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<PlotStatusResult> load({int societyId = 0}) async {
    final db = await AppDatabase.instance.database;
    String societyName = '(All Societies)';
    if (societyId > 0) {
      final s = await db.query(
        'societies',
        columns: ['name'],
        where: 'id = ?',
        whereArgs: [societyId],
        limit: 1,
      );
      if (s.isNotEmpty) societyName = s.first['name'] as String? ?? societyName;
    }

    // Auto-cancel (bina date) wapas Active + Available repair
    try {
      await db.execute('''
UPDATE customer_plots
SET status = 'Active'
WHERE IFNULL(status,'') = 'Cancelled'
  AND (cancelled_date IS NULL OR TRIM(IFNULL(cancelled_date,'')) = '')
  AND IFNULL(cancellation_id,0) = 0''');
      await db.execute('''
UPDATE plots SET status = CASE
  WHEN EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = plots.id
      AND IFNULL(cp.status,'') <> 'Cancelled'
      AND IFNULL(cp.deal_type,'') = 'Sold'
  ) THEN 'Sold'
  ELSE 'Hold'
END
WHERE status = 'Available'
  AND EXISTS (
    SELECT 1 FROM customer_plots cp
    WHERE cp.plot_id = plots.id
      AND IFNULL(cp.status,'') <> 'Cancelled'
  )''');
    } catch (_) {}

    final raw = await db.rawQuery(
      '''
SELECT 'Available' AS kind, s.name AS society, b.name AS block,
       IFNULL(pt.code,'') AS plot_type, p.plot_no AS plot_no, '' AS customer
FROM plots p
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE p.status = 'Available'
  AND (? = 0 OR p.society_id = ?)
  AND NOT EXISTS (
    SELECT 1 FROM customer_plots cp0
    WHERE cp0.plot_id = p.id
      AND IFNULL(cp0.status,'') <> 'Cancelled'
  )

UNION ALL

SELECT
  CASE
    WHEN LOWER(TRIM(IFNULL(cp.deal_type,''))) = 'sold' THEN 'Sold'
    WHEN LOWER(TRIM(IFNULL(cp.deal_type,''))) = 'reserve' THEN 'Reserve'
    ELSE 'Installment'
  END AS kind,
  s.name AS society, b.name AS block,
  IFNULL(pt.code,'') AS plot_type, p.plot_no AS plot_no,
  IFNULL(c.name,'') AS customer
FROM customer_plots cp
JOIN plots p ON p.id = cp.plot_id
JOIN customers c ON c.id = cp.customer_id
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE IFNULL(cp.status,'') <> 'Cancelled'
  AND (? = 0 OR p.society_id = ?)
''',
      [societyId, societyId, societyId, societyId],
    );

    int rank(String k) {
      switch (k) {
        case 'Available':
          return 0;
        case 'Installment':
          return 1;
        case 'Reserve':
          return 2;
        default:
          return 3;
      }
    }

    final rows = raw
        .map(
          (r) => PlotStatusRow(
            kind: (r['kind'] ?? '').toString(),
            society: (r['society'] ?? '').toString(),
            block: (r['block'] ?? '').toString(),
            plotType: (r['plot_type'] ?? '').toString(),
            plotNo: (r['plot_no'] ?? '').toString(),
            customer: (r['customer'] ?? '').toString(),
          ),
        )
        .toList()
      ..sort((a, b) {
        final c = rank(a.kind).compareTo(rank(b.kind));
        if (c != 0) return c;
        final s = a.society.toLowerCase().compareTo(b.society.toLowerCase());
        if (s != 0) return s;
        final bl = a.block.toLowerCase().compareTo(b.block.toLowerCase());
        if (bl != 0) return bl;
        return a.plotNo.toLowerCase().compareTo(b.plotNo.toLowerCase());
      });

    var avail = 0, inst = 0, res = 0, sold = 0;
    for (final r in rows) {
      switch (r.kind) {
        case 'Available':
          avail++;
          break;
        case 'Installment':
          inst++;
          break;
        case 'Reserve':
          res++;
          break;
        case 'Sold':
          sold++;
          break;
      }
    }

    return PlotStatusResult(
      rows: rows,
      counts: PlotStatusCounts(
        available: avail,
        installment: inst,
        reserve: res,
        sold: sold,
      ),
      societyName: societyName,
    );
  }
}
