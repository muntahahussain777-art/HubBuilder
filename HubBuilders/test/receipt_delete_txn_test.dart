import 'package:flutter_test/flutter_test.dart';
import 'package:hub_builders/data/receipt_store.dart';
import 'package:hub_builders/db/app_database.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

Future<Database> _openTestDb() async {
  final db = await databaseFactoryFfi.openDatabase(
    inMemoryDatabasePath,
    options: OpenDatabaseOptions(
      version: 1,
      onConfigure: (db) async {
        await db.execute('PRAGMA foreign_keys = ON');
      },
      onCreate: (db, _) async {
        await db.execute('''
CREATE TABLE customer_plots(
  id INTEGER PRIMARY KEY, customer_id INT, plot_id INT, status TEXT, sync_id TEXT, price REAL
)''');
        await db.execute('''
CREATE TABLE ledger(
  id INTEGER PRIMARY KEY, customer_plot_id INT, debit REAL, credit REAL, sync_id TEXT
)''');
        await db.execute('''
CREATE TABLE receipts(
  id INTEGER PRIMARY KEY, customer_plot_id INT, amount REAL, sync_id TEXT, ledger_id INT,
  receipt_date TEXT, mode TEXT, ref_no TEXT, notes TEXT
)''');
        await db.execute('''
CREATE TABLE partner_money(
  id INTEGER PRIMARY KEY, partner_id INT, receipt_id INT, customer_plot_id INT,
  amount REAL, sync_id TEXT, category TEXT, txn_date TEXT
)''');
        await db.execute('''
CREATE TABLE sync_pending_delete(
  sync_id TEXT PRIMARY KEY, table_name TEXT NOT NULL,
  queued_at TEXT DEFAULT (datetime('now','localtime'))
)''');
        await db.execute('''
CREATE TABLE sync_alias(
  sync_id TEXT PRIMARY KEY, table_name TEXT NOT NULL, local_pk INTEGER NOT NULL
)''');
      },
    ),
  );
  return db;
}

Future<void> _seedOk(Database db) async {
  await db.insert('customer_plots', {
    'id': 1,
    'customer_id': 1,
    'plot_id': 1,
    'status': 'Active',
    'sync_id': 'alloc_ok',
    'price': 100000,
  });
  await db.insert('ledger', {
    'id': 10,
    'customer_plot_id': 1,
    'debit': 100000,
    'credit': 10000,
    'sync_id': 'led_ok',
  });
  await db.insert('receipts', {
    'id': 20,
    'customer_plot_id': 1,
    'amount': 10000,
    'sync_id': 'rec_ok',
    'ledger_id': 10,
    'receipt_date': '2026-11-22',
    'mode': 'Cash',
  });
  await db.insert('partner_money', {
    'id': 30,
    'partner_id': 1,
    'receipt_id': 20,
    'customer_plot_id': 1,
    'amount': 1000,
    'sync_id': 'pm_ok',
    'category': 'Receipt',
    'txn_date': '2026-11-22',
  });
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  sqfliteFfiInit();
  databaseFactory = databaseFactoryFfi;

  tearDown(() async {
    AppDatabase.debugSetDatabase(null);
  });

  test('deleteReceipt failure: missing receipt sync_id rolls back — data preserved',
      () async {
    final db = await _openTestDb();
    AppDatabase.debugSetDatabase(db);
    await _seedOk(db);
    await db.update('receipts', {'sync_id': null}, where: 'id = 20');

    final balBefore = await db.rawQuery(
      'SELECT COALESCE(SUM(credit),0) c FROM ledger WHERE customer_plot_id=1',
    );
    final beforeCredit = (balBefore.first['c'] as num).toDouble();

    final r = await ReceiptStore.deleteReceipt(20);
    expect(r.ok, isFalse, reason: r.message);
    expect(r.message.toLowerCase(), contains('sync_id'));

    expect(
      await db.query('receipts', where: 'id = 20'),
      isNotEmpty,
      reason: 'receipt preserved',
    );
    expect(
      await db.query('ledger', where: 'id = 10'),
      isNotEmpty,
      reason: 'ledger preserved',
    );
    expect(
      await db.query('partner_money', where: 'id = 30'),
      isNotEmpty,
      reason: 'partner cash preserved',
    );
    expect(
      await db.query('sync_pending_delete'),
      isEmpty,
      reason: 'no pending after rollback',
    );
    final balAfter = await db.rawQuery(
      'SELECT COALESCE(SUM(credit),0) c FROM ledger WHERE customer_plot_id=1',
    );
    expect((balAfter.first['c'] as num).toDouble(), beforeCredit);
    await db.close();
  });

  test('deleteReceipt failure: pending_delete queue insert fail rolls back', () async {
    final db = await _openTestDb();
    AppDatabase.debugSetDatabase(db);
    await _seedOk(db);
    // Break pending_delete so INSERT of required queue fails
    await db.execute('DROP TABLE sync_pending_delete');
    await db.execute(
      'CREATE TABLE sync_pending_delete (sync_id TEXT PRIMARY KEY)',
    ); // missing table_name NOT NULL → insert fails

    final r = await ReceiptStore.deleteReceipt(20);
    expect(r.ok, isFalse, reason: r.message);

    expect(await db.query('receipts', where: 'id = 20'), isNotEmpty);
    expect(await db.query('ledger', where: 'id = 10'), isNotEmpty);
    expect(await db.query('partner_money', where: 'id = 30'), isNotEmpty);
    final credit = await db.rawQuery(
      'SELECT credit FROM ledger WHERE id=10',
    );
    expect((credit.first['credit'] as num).toDouble(), 10000);
    await db.close();
  });
}
