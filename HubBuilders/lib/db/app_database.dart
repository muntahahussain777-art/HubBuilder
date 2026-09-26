import 'package:path/path.dart' as p;
import 'package:path_provider/path_provider.dart';
import 'package:sqflite/sqflite.dart';

import '../data/hub_duplicate_cleanup.dart';
import '../data/staff_access.dart';
import '../data/block_monthly_store.dart';

/// SQLite layer aligned with C# RealEstateBuilders AppData schema.
/// Empty on first run — no dummy seed data.
class AppDatabase {
  AppDatabase._();
  static final AppDatabase instance = AppDatabase._();

  static const dbName = 'RealEstateBuilders.db';
  static const version = 1;

  Database? _db;

  Future<Database> get database async {
    if (_db != null) return _db!;
    _db = await _open();
    return _db!;
  }

  /// Absolute path used on device (documents dir).
  Future<String> get dbPath async {
    final dir = await getApplicationDocumentsDirectory();
    return p.join(dir.path, dbName);
  }

  Future<Database> _open() async {
    final path = await dbPath;
    final db = await openDatabase(
      path,
      version: version,
      onConfigure: (db) async {
        await db.execute('PRAGMA foreign_keys = ON');
      },
      onCreate: (db, v) async {
        await _createSchema(db);
        await _createTriggers(db);
        // Default admin user (plaintext, same as C# users.upass).
        await db.insert('users', {
          'username': 'Admin',
          'upass': 'hubbuilder786',
          'uName': 'Administrator',
          'uphone': '',
          'Role': 'Admin',
        });
      },
    );
    // Additive only — old DB restore safe (IF NOT EXISTS).
    await db.execute('''
CREATE TABLE IF NOT EXISTS staff_entry_log (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  action_type TEXT NOT NULL,
  module_name TEXT NOT NULL,
  detail TEXT,
  staff_username TEXT,
  staff_name TEXT,
  staff_role TEXT,
  created_at TEXT DEFAULT (datetime('now','localtime'))
)''');
    await ensurePartnerMoneySchema(db);
    await ensureExpenseCategorySchema(db);
    await ensureOverpayTriggers(db);
    await ensureIndexes(db);
    await ensureCustomerPlotsDealType(db);
    await ensureCustomerPlotsMonthlyInstallment(db);
    await ensureDefaultAdmin(db);
    try {
      await HubDuplicateCleanup.run(db);
    } catch (_) {}
    _db = db;
    await StaffAccess.ensureSchema();
    await BlockMonthlyStore.ensureSchema();
    try {
      await BlockMonthlyStore.applyToEmptyActive();
    } catch (_) {}
    return db;
  }

  /// Additive: Installment | Reserve | Sold — purani rows → Installment.
  static Future<void> ensureCustomerPlotsDealType([Database? existing]) async {
    final db = existing ?? await instance.database;
    final cols = await db.rawQuery('PRAGMA table_info(customer_plots)');
    final has = cols.any(
      (c) => (c['name'] as String?)?.toLowerCase() == 'deal_type',
    );
    if (!has) {
      await db.execute(
        "ALTER TABLE customer_plots ADD COLUMN deal_type TEXT NOT NULL DEFAULT 'Installment'",
      );
    }
    await db.execute('''
UPDATE customer_plots
SET deal_type = 'Installment'
WHERE deal_type IS NULL
   OR TRIM(COALESCE(deal_type,'')) = ''
   OR LOWER(TRIM(deal_type)) NOT IN ('installment','reserve','sold')
''');
    await db.execute(
      "UPDATE customer_plots SET deal_type = 'Installment' WHERE LOWER(TRIM(deal_type)) = 'installment'",
    );
    await db.execute(
      "UPDATE customer_plots SET deal_type = 'Reserve' WHERE LOWER(TRIM(deal_type)) = 'reserve'",
    );
    await db.execute(
      "UPDATE customer_plots SET deal_type = 'Sold' WHERE LOWER(TRIM(deal_type)) = 'sold'",
    );
  }

  /// Additive: monthly installment amount. Old rows stay 0 (WhatsApp skip).
  static Future<void> ensureCustomerPlotsMonthlyInstallment([
    Database? existing,
  ]) async {
    final db = existing ?? await instance.database;
    final cols = await db.rawQuery('PRAGMA table_info(customer_plots)');
    final has = cols.any(
      (c) => (c['name'] as String?)?.toLowerCase() == 'monthly_installment',
    );
    if (!has) {
      await db.execute(
        'ALTER TABLE customer_plots ADD COLUMN monthly_installment REAL NOT NULL DEFAULT 0',
      );
    }
  }

  /// Built-in Admin — software lock. Password form / cloud overwrite nahi.
  static Future<void> ensureDefaultAdmin([Database? existing]) async {
    final db = existing ?? await instance.database;
    try {
      final rows = await db.query(
        'users',
        where: "lower(trim(username)) = 'admin'",
      );
      if (rows.isNotEmpty) {
        await db.rawUpdate(
          '''
UPDATE users
SET username = ?, upass = ?, Role = ?,
    uName = CASE WHEN IFNULL(TRIM(uName),'')='' THEN ? ELSE uName END
WHERE lower(trim(username)) = 'admin'
''',
          ['Admin', 'hubbuilder786', 'Admin', 'Administrator'],
        );
        return;
      }
      await db.insert('users', {
        'username': 'Admin',
        'upass': 'hubbuilder786',
        'uName': 'Administrator',
        'uphone': '',
        'Role': 'Admin',
      });
    } catch (_) {}
  }

  /// Same tables as WinForms MainClass.EnsurePartnerMoneySchema (additive).
  static Future<void> ensurePartnerMoneySchema([Database? existing]) async {
    final db = existing ?? await instance.database;
    await db.execute('''
CREATE TABLE IF NOT EXISTS customer_plot_partners (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  customer_plot_id INTEGER NOT NULL UNIQUE,
  partner_id INTEGER NOT NULL,
  created_at TEXT DEFAULT (datetime('now','localtime'))
)''');
    await db.execute('''
CREATE TABLE IF NOT EXISTS partner_money (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  partner_id INTEGER NOT NULL,
  txn_date TEXT NOT NULL,
  direction TEXT NOT NULL,
  amount REAL NOT NULL,
  mode TEXT,
  category TEXT,
  customer_plot_id INTEGER,
  receipt_id INTEGER,
  society_id INTEGER,
  note TEXT,
  created_at TEXT DEFAULT (datetime('now','localtime'))
)''');
    await db.execute(
      'CREATE INDEX IF NOT EXISTS idx_pm_partner ON partner_money(partner_id, txn_date)',
    );
    await db.execute(
      'CREATE INDEX IF NOT EXISTS idx_pm_receipt ON partner_money(receipt_id)',
    );
    await db.execute(
      'CREATE INDEX IF NOT EXISTS idx_cpp_cp ON customer_plot_partners(customer_plot_id)',
    );
    await db.execute('''
CREATE TABLE IF NOT EXISTS partner_money_categories (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL UNIQUE,
  direction TEXT NOT NULL DEFAULT 'Both'
)''');
    const seeds = [
      ('Receipt', 'In'),
      ('Manual In', 'In'),
      ('Broker Commission', 'In'),
      ('Zameen / Land', 'Out'),
      ('Office Expense', 'Out'),
      ('Staff Salary', 'Out'),
      ('Apne paas rakha', 'Out'),
      ('Other', 'Both'),
    ];
    for (final s in seeds) {
      await db.rawInsert(
        'INSERT OR IGNORE INTO partner_money_categories (name, direction) VALUES (?, ?)',
        [s.$1, s.$2],
      );
    }
  }

  static Future<void> ensureExpenseCategorySchema([Database? existing]) async {
    final db = existing ?? await instance.database;
    await db.execute('''
CREATE TABLE IF NOT EXISTS expense_categories (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL UNIQUE
)''');
    await db.execute('''
INSERT OR IGNORE INTO expense_categories (name)
SELECT DISTINCT TRIM(Category) FROM Expensetbl
WHERE Category IS NOT NULL AND TRIM(Category) <> ''
''');
  }

  static Future<void> ensureOverpayTriggers([Database? existing]) async {
    final db = existing ?? await instance.database;
    await db.execute('DROP TRIGGER IF EXISTS trg_receipts_no_overpay_insert');
    await db.execute('DROP TRIGGER IF EXISTS trg_receipts_no_overpay_update');
    await db.execute('''
CREATE TRIGGER trg_receipts_no_overpay_insert
BEFORE INSERT ON receipts
BEGIN
  SELECT CASE
    WHEN NEW.amount > (
      SELECT CASE
        WHEN COALESCE((
          SELECT SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge')
                          THEN debit ELSE 0 END)
          FROM ledger WHERE customer_plot_id = NEW.customer_plot_id
        ),0) > 0
        THEN COALESCE((
          SELECT SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge')
                          THEN debit ELSE 0 END)
          FROM ledger WHERE customer_plot_id = NEW.customer_plot_id
        ),0)
        ELSE COALESCE((SELECT price FROM customer_plots WHERE id = NEW.customer_plot_id),0)
      END
      - COALESCE((SELECT SUM(amount) FROM receipts WHERE customer_plot_id = NEW.customer_plot_id),0)
    )
    THEN RAISE(ABORT, 'Receipt amount exceeds remaining due')
  END;
END''');
    await db.execute('''
CREATE TRIGGER trg_receipts_no_overpay_update
BEFORE UPDATE OF amount, customer_plot_id ON receipts
BEGIN
  SELECT CASE
    WHEN NEW.amount > (
      SELECT CASE
        WHEN COALESCE((
          SELECT SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge')
                          THEN debit ELSE 0 END)
          FROM ledger WHERE customer_plot_id = NEW.customer_plot_id
        ),0) > 0
        THEN COALESCE((
          SELECT SUM(CASE WHEN txn_type IN ('Booking','InstallmentDue','Surcharge')
                          THEN debit ELSE 0 END)
          FROM ledger WHERE customer_plot_id = NEW.customer_plot_id
        ),0)
        ELSE COALESCE((SELECT price FROM customer_plots WHERE id = NEW.customer_plot_id),0)
      END
      - COALESCE((
          SELECT SUM(amount) FROM receipts
          WHERE customer_plot_id = NEW.customer_plot_id AND id != NEW.id
        ),0)
    )
    THEN RAISE(ABORT, 'Receipt amount exceeds remaining due')
  END;
END''');
  }

  static Future<void> ensureIndexes([Database? existing]) async {
    final db = existing ?? await instance.database;
    await db.execute(
      'CREATE INDEX IF NOT EXISTS idx_cp_customer ON customer_plots(customer_id)',
    );
    await db.execute(
      'CREATE INDEX IF NOT EXISTS idx_cp_status ON customer_plots(status)',
    );
    await db.execute(
      'CREATE INDEX IF NOT EXISTS idx_cp_plot ON customer_plots(plot_id)',
    );
    await db.execute(
      'CREATE INDEX IF NOT EXISTS IX_customer_plots_broker ON customer_plots(broker_id)',
    );
  }

  Future<void> _createSchema(Database db) async {
    await db.execute('''
CREATE TABLE societies (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL UNIQUE,
  city TEXT,
  created_at TEXT DEFAULT (datetime('now'))
)''');

    await db.execute('''
CREATE TABLE blocks (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  society_id INTEGER NOT NULL,
  name TEXT NOT NULL,
  UNIQUE(society_id, name),
  FOREIGN KEY (society_id) REFERENCES societies(id) ON DELETE CASCADE
)''');

    await db.execute('''
CREATE TABLE plot_types (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  society_id INTEGER NOT NULL,
  code TEXT NOT NULL,
  UNIQUE(society_id, code),
  FOREIGN KEY (society_id) REFERENCES societies(id) ON DELETE CASCADE
)''');

    await db.execute('''
CREATE TABLE plots (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  society_id INTEGER NOT NULL,
  block_id INTEGER NOT NULL,
  plot_type_id INTEGER NOT NULL,
  plot_no TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'Available'
    CHECK(status IN ('Available','Hold','Sold')),
  created_at TEXT DEFAULT (datetime('now')),
  UNIQUE(society_id, block_id, plot_type_id, plot_no),
  FOREIGN KEY (society_id) REFERENCES societies(id) ON DELETE CASCADE,
  FOREIGN KEY (block_id) REFERENCES blocks(id) ON DELETE CASCADE,
  FOREIGN KEY (plot_type_id) REFERENCES plot_types(id) ON DELETE CASCADE
)''');

    await db.execute('''
CREATE TABLE plans (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT UNIQUE,
  price REAL,
  down_payment REAL,
  installments INTEGER,
  notes TEXT
)''');

    await db.execute('''
CREATE TABLE customers (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  father_name TEXT,
  mobile TEXT,
  cnic TEXT,
  notes TEXT,
  created_at TEXT DEFAULT (datetime('now'))
)''');

    await db.execute('''
CREATE TABLE brokers (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  phone TEXT,
  is_active INTEGER NOT NULL DEFAULT 1,
  commission_amount_default REAL NOT NULL DEFAULT 0
    CHECK(commission_amount_default >= 0)
)''');

    await db.execute('''
CREATE TABLE customer_plots (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  customer_id INTEGER NOT NULL,
  plot_id INTEGER NOT NULL,
  plan_id INTEGER,
  booking_date TEXT,
  price REAL NOT NULL CHECK(price >= 0),
  down_payment REAL NOT NULL DEFAULT 0 CHECK(down_payment >= 0),
  notes TEXT,
  status TEXT NOT NULL DEFAULT 'Active'
    CHECK(status IN ('Active','Closed','Cancelled','Completed')),
  broker_id INTEGER,
  broker_commission REAL,
  cancelled_date TEXT,
  cancellation_id INTEGER,
  deal_type TEXT NOT NULL DEFAULT 'Installment',
  FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE,
  FOREIGN KEY (plot_id) REFERENCES plots(id) ON DELETE RESTRICT,
  FOREIGN KEY (plan_id) REFERENCES plans(id) ON DELETE SET NULL,
  FOREIGN KEY (broker_id) REFERENCES brokers(id) ON DELETE SET NULL
)''');

    await db.execute('''
CREATE TABLE ledger (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  customer_plot_id INTEGER NOT NULL,
  txn_date TEXT NOT NULL,
  voucher_no TEXT,
  narration TEXT,
  debit REAL NOT NULL DEFAULT 0,
  credit REAL NOT NULL DEFAULT 0,
  txn_type TEXT NOT NULL
    CHECK(txn_type IN (
      'Booking','InstallmentDue','Surcharge','Discount',
      'ReceiptCash','ReceiptBank','Adjustment'
    )),
  created_at TEXT DEFAULT (datetime('now')),
  FOREIGN KEY (customer_plot_id) REFERENCES customer_plots(id) ON DELETE CASCADE
)''');

    await db.execute('''
CREATE TABLE receipts (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  customer_plot_id INTEGER NOT NULL,
  receipt_date TEXT NOT NULL,
  amount REAL NOT NULL CHECK(amount > 0),
  mode TEXT NOT NULL CHECK(mode IN ('Cash','Bank')),
  ref_no TEXT,
  notes TEXT,
  ledger_id INTEGER,
  created_at TEXT DEFAULT (datetime('now')),
  FOREIGN KEY (customer_plot_id) REFERENCES customer_plots(id) ON DELETE CASCADE,
  FOREIGN KEY (ledger_id) REFERENCES ledger(id) ON DELETE SET NULL
)''');

    await db.execute('''
CREATE TABLE Expensetbl (
  sid INTEGER PRIMARY KEY AUTOINCREMENT,
  Name TEXT,
  Amount REAL,
  Category TEXT,
  Note TEXT,
  EDate TEXT
)''');

    await db.execute('''
CREATE TABLE Person (
  person_id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  phone TEXT,
  note TEXT,
  is_partner INTEGER NOT NULL DEFAULT 0,
  is_owner INTEGER NOT NULL DEFAULT 0,
  created_on TEXT DEFAULT (datetime('now'))
)''');

    await db.execute('''
CREATE TABLE Deal (
  deal_id INTEGER PRIMARY KEY AUTOINCREMENT,
  society_id INTEGER NOT NULL,
  owner_id INTEGER NOT NULL,
  amount REAL NOT NULL,
  deal_date TEXT,
  note TEXT,
  created_on TEXT DEFAULT (datetime('now')),
  FOREIGN KEY (society_id) REFERENCES societies(id),
  FOREIGN KEY (owner_id) REFERENCES Person(person_id)
)''');

    await db.execute('''
CREATE TABLE Credit (
  credit_id INTEGER PRIMARY KEY AUTOINCREMENT,
  deal_id INTEGER NOT NULL,
  partner_id INTEGER NOT NULL,
  amount REAL NOT NULL,
  pay_date TEXT,
  note TEXT,
  created_on TEXT DEFAULT (datetime('now')),
  FOREIGN KEY (deal_id) REFERENCES Deal(deal_id) ON DELETE CASCADE,
  FOREIGN KEY (partner_id) REFERENCES Person(person_id)
)''');

    await db.execute('''
CREATE TABLE users (
  userID INTEGER PRIMARY KEY AUTOINCREMENT,
  username TEXT NOT NULL UNIQUE,
  upass TEXT NOT NULL,
  uName TEXT,
  uphone TEXT,
  Role TEXT
)''');

    await db.execute('''
CREATE TABLE allocation_cancellations (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  customer_plot_id INTEGER NOT NULL,
  cancel_date TEXT,
  deduction_percent REAL,
  allocation_amount REAL,
  received_amount REAL,
  deduction_amount REAL,
  refundable_amount REAL,
  notes TEXT,
  created_at TEXT DEFAULT (datetime('now')),
  FOREIGN KEY (customer_plot_id) REFERENCES customer_plots(id)
)''');
  }

  Future<void> _createTriggers(Database db) async {
    await ensurePlotAllocTriggers(db);
  }

  /// Drop abort-triggers so cloud rows can land; restore after pull.
  static Future<void> suspendSyncTriggers([Database? existing]) async {
    final db = existing ?? await instance.database;
    await db.execute('DROP TRIGGER IF EXISTS trg_receipts_no_overpay_insert');
    await db.execute('DROP TRIGGER IF EXISTS trg_receipts_no_overpay_update');
    await db.execute('DROP TRIGGER IF EXISTS trg_cp_one_active_plot_ins');
    await db.execute('DROP TRIGGER IF EXISTS trg_cp_one_active_plot_upd');
  }

  static Future<void> restoreSyncTriggers([Database? existing]) async {
    final db = existing ?? await instance.database;
    await ensureOverpayTriggers(db);
    await ensurePlotAllocTriggers(db);
  }

  static Future<void> ensurePlotAllocTriggers([Database? existing]) async {
    final db = existing ?? await instance.database;
    await db.execute('DROP TRIGGER IF EXISTS trg_cp_one_active_plot_ins');
    await db.execute('DROP TRIGGER IF EXISTS trg_cp_one_active_plot_upd');
    await db.execute('''
CREATE TRIGGER trg_cp_one_active_plot_ins
BEFORE INSERT ON customer_plots
WHEN NEW.status = 'Active'
BEGIN
  SELECT CASE
    WHEN EXISTS (
      SELECT 1 FROM customer_plots
      WHERE plot_id = NEW.plot_id AND status = 'Active'
    )
    THEN RAISE(ABORT, 'Plot already has an Active allocation')
  END;
END''');
    await db.execute('''
CREATE TRIGGER trg_cp_one_active_plot_upd
BEFORE UPDATE OF status, plot_id ON customer_plots
WHEN NEW.status = 'Active'
BEGIN
  SELECT CASE
    WHEN EXISTS (
      SELECT 1 FROM customer_plots
      WHERE plot_id = NEW.plot_id AND status = 'Active' AND id != NEW.id
    )
    THEN RAISE(ABORT, 'Plot already has an Active allocation')
  END;
END''');
  }

  Future<void> close() async {
    await _db?.close();
    _db = null;
  }
}
