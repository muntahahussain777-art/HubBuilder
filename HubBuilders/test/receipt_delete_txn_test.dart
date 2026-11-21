import 'package:flutter_test/flutter_test.dart';
import 'package:hub_builders/data/hub_sync.dart';
import 'package:hub_builders/data/receipt_store.dart';
import 'package:hub_builders/db/app_database.dart';
import 'package:shared_preferences/shared_preferences.dart';
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
        await db.execute('''
CREATE TABLE staff_entry_log(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  action_type TEXT NOT NULL,
  module_name TEXT NOT NULL,
  detail TEXT,
  staff_username TEXT,
  staff_name TEXT,
  staff_role TEXT,
  created_at TEXT DEFAULT (datetime('now','localtime'))
)''');
      },
    ),
  );
  return db;
}

Future<void> _seedOk(Database db, {bool withAlias = false}) async {
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
  if (withAlias) {
    await db.insert('sync_alias', {
      'sync_id': 'rec_alias_1',
      'table_name': 'receipts',
      'local_pk': 20,
    });
  }
}

Future<double> _ledgerCredit(Database db) async {
  final rows = await db.rawQuery(
    'SELECT COALESCE(SUM(credit),0) c FROM ledger WHERE customer_plot_id=1',
  );
  return (rows.first['c'] as num).toDouble();
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  sqfliteFfiInit();
  databaseFactory = databaseFactoryFfi;

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    HubSync.suppressCloudIo = true;
  });

  tearDown(() async {
    HubSync.suppressCloudIo = false;
    AppDatabase.debugSetDatabase(null);
  });

  test(
    'deleteReceipt: primary sync_id present but alias lookup fails → rollback, Rs10000 balance safe',
    () async {
      final db = await _openTestDb();
      AppDatabase.debugSetDatabase(db);
      await _seedOk(db);
      // Primary ID maujood; alias lookup break
      await db.execute('DROP TABLE sync_alias');

      final before = await _ledgerCredit(db);
      expect(before, 10000);

      final r = await ReceiptStore.deleteReceipt(20);
      expect(r.ok, isFalse, reason: r.message);
      expect(
        r.message.toLowerCase(),
        anyOf(contains('alias'), contains('no such table'), contains('failed')),
      );

      expect(await db.query('receipts', where: 'id = 20'), isNotEmpty);
      expect(await db.query('ledger', where: 'id = 10'), isNotEmpty);
      expect(await db.query('partner_money', where: 'id = 30'), isNotEmpty);
      expect(await db.query('sync_pending_delete'), isEmpty);
      expect(await _ledgerCredit(db), 10000);
      await db.close();
    },
  );

  test(
    'deleteReceipt success: all expected IDs (primary + alias + ledger + partner) queued',
    () async {
      final db = await _openTestDb();
      AppDatabase.debugSetDatabase(db);
      await _seedOk(db, withAlias: true);

      final r = await ReceiptStore.deleteReceipt(20);
      expect(r.ok, isTrue, reason: r.message);

      expect(await db.query('receipts', where: 'id = 20'), isEmpty);
      expect(await db.query('ledger', where: 'id = 10'), isEmpty);
      expect(await db.query('partner_money', where: 'receipt_id = 20'), isEmpty);

      final pending = await db.query('sync_pending_delete');
      final ids = pending.map((e) => (e['sync_id'] ?? '').toString()).toSet();
      expect(ids, containsAll(['rec_ok', 'rec_alias_1', 'led_ok', 'pm_ok']));
      expect(
        pending.where((e) => e['table_name'] == 'receipts').length,
        2,
        reason: 'primary + alias',
      );
      expect(
        pending.where((e) => e['table_name'] == 'ledger').length,
        1,
      );
      expect(
        pending.where((e) => e['table_name'] == 'partner_money').length,
        1,
      );
      // Balance effect removed with ledger row
      expect(await _ledgerCredit(db), 0);
      await db.close();
    },
  );

  test(
    'deleteReceipt: zero aliases with valid primary is OK (success)',
    () async {
      final db = await _openTestDb();
      AppDatabase.debugSetDatabase(db);
      await _seedOk(db); // no alias rows

      final r = await ReceiptStore.deleteReceipt(20);
      expect(r.ok, isTrue, reason: r.message);
      final pending = await db.query('sync_pending_delete');
      final ids = pending.map((e) => (e['sync_id'] ?? '').toString()).toSet();
      expect(ids, containsAll(['rec_ok', 'led_ok', 'pm_ok']));
      expect(ids.contains('rec_alias_1'), isFalse);
      await db.close();
    },
  );
}
