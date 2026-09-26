import '../db/app_database.dart';
import 'hub_sync.dart';

/// VIP/Admin: default monthly installment per society block.
/// Local table block_monthly_installments — syncs via HubSync catalog.
abstract final class BlockMonthlyStore {
  static Future<void> ensureSchema([dynamic existing]) async {
    final db = existing ?? await AppDatabase.instance.database;
    await db.execute('''
CREATE TABLE IF NOT EXISTS block_monthly_installments (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  block_id INTEGER NOT NULL UNIQUE,
  monthly_amount REAL NOT NULL DEFAULT 0,
  sync_id TEXT
)''');
  }

  static Future<List<({int id, String name})>> societies() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      'SELECT id, name FROM societies ORDER BY name',
    );
    return rows
        .map((r) => (
              id: (r['id'] as num).toInt(),
              name: (r['name'] ?? '').toString(),
            ))
        .toList();
  }

  /// One row per block in society: block_id, name, monthly.
  static Future<List<Map<String, Object?>>> loadGrid(int societyId) async {
    await ensureSchema();
    if (societyId <= 0) return const [];
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      '''
SELECT b.id AS block_id, b.name AS name,
  COALESCE(bmi.monthly_amount, 0) AS monthly
FROM blocks b
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = b.id
WHERE b.society_id = ?
ORDER BY b.name''',
      [societyId],
    );
    return rows.map((e) => Map<String, Object?>.from(e)).toList();
  }

  static Future<void> saveGrid(List<Map<String, Object?>> rows) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    await db.transaction((txn) async {
      for (final row in rows) {
        final blockId = (row['block_id'] as num?)?.toInt() ?? 0;
        if (blockId <= 0) continue;
        var amount = (row['monthly'] as num?)?.toDouble() ?? 0;
        if (amount < 0) amount = 0;
        await txn.rawInsert(
          '''
INSERT INTO block_monthly_installments(block_id, monthly_amount, sync_id)
VALUES(?, ?, lower(hex(randomblob(16))))
ON CONFLICT(block_id) DO UPDATE SET
  monthly_amount = excluded.monthly_amount''',
          [blockId, amount],
        );
      }
    });
    try {
      await applyToEmptyActive();
    } catch (_) {}
    HubSync.notifyLocalWrite();
  }

  static Future<double> amountForBlock(int blockId) async {
    if (blockId <= 0) return 0;
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      'SELECT COALESCE(monthly_amount,0) AS m FROM block_monthly_installments WHERE block_id = ? LIMIT 1',
      [blockId],
    );
    if (rows.isEmpty) return 0;
    final m = (rows.first['m'] as num?)?.toDouble() ?? 0;
    return m > 0 ? m : 0;
  }

  /// Upsert one block default (Block Form + VIP grid stay consistent).
  static Future<void> upsertForBlock(int blockId, double monthlyAmount) async {
    if (blockId <= 0) return;
    await ensureSchema();
    var amount = monthlyAmount;
    if (amount < 0) amount = 0;
    final db = await AppDatabase.instance.database;
    await db.rawInsert(
      '''
INSERT INTO block_monthly_installments(block_id, monthly_amount, sync_id)
VALUES(?, ?, lower(hex(randomblob(16))))
ON CONFLICT(block_id) DO UPDATE SET
  monthly_amount = excluded.monthly_amount''',
      [blockId, amount],
    );
    HubSync.notifyLocalWrite();
    try {
      await applyToEmptyActive();
    } catch (_) {}
  }

  /// Active allocations with empty monthly → block default. Returns count updated.
  static Future<int> applyToEmptyActive() async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final n = await db.rawUpdate('''
UPDATE customer_plots
SET monthly_installment = (
  SELECT bmi.monthly_amount
  FROM plots p
  JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
  WHERE p.id = customer_plots.plot_id
    AND IFNULL(bmi.monthly_amount, 0) > 0
  LIMIT 1
)
WHERE IFNULL(status,'') = 'Active'
  AND IFNULL(monthly_installment, 0) <= 0
  AND EXISTS (
    SELECT 1
    FROM plots p
    JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
    WHERE p.id = customer_plots.plot_id
      AND IFNULL(bmi.monthly_amount, 0) > 0
  )''');
    if (n > 0) HubSync.notifyLocalWrite();
    return n;
  }
}
