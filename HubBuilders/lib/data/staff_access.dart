import '../db/app_database.dart';
import 'staff_audit.dart';

class StaffModule {
  const StaffModule(this.key, this.title);
  final String key;
  final String title;
}

class StaffPerm {
  const StaffPerm({
    this.open = true,
    this.add = true,
    this.edit = true,
    this.delete = true,
  });
  final bool open;
  final bool add;
  final bool edit;
  final bool delete;
}

/// Admin-defined role → form open/add/edit/delete. Syncs via staff_role_permissions.
abstract final class StaffAccess {
  static const modules = <StaffModule>[
    StaffModule('home', 'Home'),
    StaffModule('customers', 'Customer'),
    StaffModule('societies', 'Society'),
    StaffModule('blocks', 'Block'),
    StaffModule('plot_types', 'Plot Type'),
    StaffModule('plots', 'Plot Inventory'),
    StaffModule('customer_plots', 'Allocate'),
    StaffModule('ledger', 'Ledger'),
    StaffModule('receipts', 'Receipt Form'),
    StaffModule('expensetbl', 'Expense'),
    StaffModule('reports', 'Reports'),
    StaffModule('users', 'Settings / Password'),
    StaffModule('brokers', 'Broker'),
    StaffModule('person', 'Owner / Partner'),
    StaffModule('deal', 'Deal'),
    StaffModule('credit', 'Credit'),
    StaffModule('partner_money', 'Partner Cash'),
    StaffModule('staff_entry_log', 'Staff Entry Log'),
    StaffModule('backup', 'Backup Restore'),
    StaffModule('agreement', 'Agreement Design'),
    StaffModule('dues_whatsapp', 'Dues WhatsApp'),
  ];

  static Map<String, StaffPerm> _cache = {};
  static String _cacheRole = '';
  static DateTime _cacheAt = DateTime.fromMillisecondsSinceEpoch(0);

  static Future<void> ensureSchema() async {
    final db = await AppDatabase.instance.database;
    await db.execute('''
CREATE TABLE IF NOT EXISTS staff_role_permissions (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  role_name TEXT NOT NULL,
  module_key TEXT NOT NULL,
  can_open INTEGER NOT NULL DEFAULT 1,
  can_add INTEGER NOT NULL DEFAULT 1,
  can_edit INTEGER NOT NULL DEFAULT 1,
  can_delete INTEGER NOT NULL DEFAULT 1,
  sync_id TEXT,
  UNIQUE(role_name, module_key)
)''');
  }

  static void invalidate() {
    _cache = {};
    _cacheRole = '';
    _cacheAt = DateTime.fromMillisecondsSinceEpoch(0);
  }

  static bool get isAdmin {
    final r = (StaffAudit.role ?? '').trim();
    return r.toLowerCase() == 'admin';
  }

  static Future<bool> can(String moduleKey, String action) async {
    try {
      if (isAdmin) return true;
      final role = (StaffAudit.role ?? '').trim();
      if (role.isEmpty) return true;
      await ensureSchema();
      await _refresh(role);
      final p = _cache[moduleKey] ?? _default(moduleKey);
      switch (action.toLowerCase()) {
        case 'open':
        case 'view':
          return p.open;
        case 'add':
        case 'insert':
          return p.add;
        case 'edit':
        case 'update':
          return p.edit;
        case 'delete':
        case 'del':
          return p.delete;
        default:
          return p.open;
      }
    } catch (_) {
      return true;
    }
  }

  static String moduleForRoute(String routeName) {
    switch (routeName) {
      case 'home':
        return 'home';
      case 'customer':
      case 'newCustomer':
        return 'customers';
      case 'society':
        return 'societies';
      case 'block':
        return 'blocks';
      case 'plot':
        return 'plot_types';
      case 'plotInventory':
        return 'plots';
      case 'allocate':
        return 'customer_plots';
      case 'ledger':
        return 'ledger';
      case 'receipt':
        return 'receipts';
      case 'expense':
        return 'expensetbl';
      case 'reports':
      case 'applicationForm':
      case 'applicationFormBlank':
      case 'vipPartnerCashBook':
      case 'vipCustomerPlotsReceipts':
      case 'rptVipAllocateTotals':
        return 'reports';
      case 'password':
        return 'users';
      case 'broker':
        return 'brokers';
      case 'owner':
      case 'partner':
        return 'person';
      case 'partnerCash':
        return 'partner_money';
      case 'deal':
        return 'deal';
      case 'credit':
        return 'credit';
      case 'staffEntryLog':
        return 'staff_entry_log';
      case 'rptBackupRestore':
        return 'backup';
      case 'agreementDesign':
        return 'agreement';
      case 'duesWhatsApp':
        return 'dues_whatsapp';
      default:
        if (routeName.startsWith('rpt')) return 'reports';
        return routeName;
    }
  }

  static String moduleForTable(String table) {
    final t = table.toLowerCase();
    if (t == 'allocation_cancellations') return 'customer_plots';
    if (t == 'customer_plot_partners') return 'partner_money';
    return t;
  }

  static Future<List<String>> distinctRoles() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      "SELECT DISTINCT Role FROM users WHERE Role IS NOT NULL AND TRIM(Role)<>'' ORDER BY Role",
    );
    final out = <String>[];
    for (final r in rows) {
      final s = (r['Role'] ?? '').toString().trim();
      if (s.isEmpty) continue;
      if (s.toLowerCase() == 'admin') continue;
      if (!out.any((x) => x.toLowerCase() == s.toLowerCase())) out.add(s);
    }
    if (!out.any((x) => x.toLowerCase() == 'manager')) out.insert(0, 'Manager');
    if (!out.any((x) => x.toLowerCase() == 'operator')) out.add('Operator');
    return out;
  }

  static Future<List<Map<String, Object?>>> loadRoleGrid(String role) async {
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'staff_role_permissions',
      where: 'lower(trim(role_name)) = lower(trim(?))',
      whereArgs: [role],
    );
    final map = <String, StaffPerm>{};
    for (final r in rows) {
      map[(r['module_key'] ?? '').toString()] = StaffPerm(
        open: (r['can_open'] as int? ?? 1) != 0,
        add: (r['can_add'] as int? ?? 1) != 0,
        edit: (r['can_edit'] as int? ?? 1) != 0,
        delete: (r['can_delete'] as int? ?? 1) != 0,
      );
    }
    return [
      for (final m in modules)
        {
          'module_key': m.key,
          'title': m.title,
          'open': (map[m.key] ?? _default(m.key)).open,
          'add': (map[m.key] ?? _default(m.key)).add,
          'edit': (map[m.key] ?? _default(m.key)).edit,
          'delete': (map[m.key] ?? _default(m.key)).delete,
        },
    ];
  }

  static Future<void> saveRoleGrid(
    String role,
    List<Map<String, Object?>> rows,
  ) async {
    final r = role.trim();
    if (r.isEmpty || r.toLowerCase() == 'admin') return;
    await ensureSchema();
    final db = await AppDatabase.instance.database;
    final batch = db.batch();
    for (final row in rows) {
      final key = (row['module_key'] ?? '').toString();
      batch.rawInsert(
        '''
INSERT INTO staff_role_permissions(role_name, module_key, can_open, can_add, can_edit, can_delete, sync_id)
VALUES(?,?,?,?,?,?, lower(hex(randomblob(16))))
ON CONFLICT(role_name, module_key) DO UPDATE SET
  can_open=excluded.can_open,
  can_add=excluded.can_add,
  can_edit=excluded.can_edit,
  can_delete=excluded.can_delete
''',
        [
          r,
          key,
          (row['open'] == true) ? 1 : 0,
          (row['add'] == true) ? 1 : 0,
          (row['edit'] == true) ? 1 : 0,
          (row['delete'] == true) ? 1 : 0,
        ],
      );
    }
    await batch.commit(noResult: true);
    invalidate();
  }

  static Future<void> _refresh(String role) async {
    if (_cacheRole.toLowerCase() == role.toLowerCase() &&
        DateTime.now().difference(_cacheAt).inSeconds < 8 &&
        _cache.isNotEmpty) {
      return;
    }
    _cache = {};
    _cacheRole = role;
    _cacheAt = DateTime.now();
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'staff_role_permissions',
      where: 'lower(trim(role_name)) = lower(trim(?))',
      whereArgs: [role],
    );
    for (final r in rows) {
      _cache[(r['module_key'] ?? '').toString()] = StaffPerm(
        open: (r['can_open'] as int? ?? 1) != 0,
        add: (r['can_add'] as int? ?? 1) != 0,
        edit: (r['can_edit'] as int? ?? 1) != 0,
        delete: (r['can_delete'] as int? ?? 1) != 0,
      );
    }
  }

  static StaffPerm _default(String moduleKey) {
    final k = moduleKey.toLowerCase();
    if (k == 'users' || k == 'staff_entry_log') {
      return const StaffPerm(
        open: false,
        add: false,
        edit: false,
        delete: false,
      );
    }
    return const StaffPerm();
  }
}
