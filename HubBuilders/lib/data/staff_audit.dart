import '../db/app_database.dart';

/// Tracks which staff created/updated/deleted rows.
/// Uses only additive table [staff_entry_log] — never alters old tables.
abstract final class StaffAudit {
  static String? username;
  static String? displayName;
  static String? role;

  static void setSession({
    required String username,
    String? displayName,
    String? role,
  }) {
    StaffAudit.username = username.trim();
    StaffAudit.displayName =
        (displayName == null || displayName.trim().isEmpty)
            ? username.trim()
            : displayName.trim();
    StaffAudit.role = role ?? '';
  }

  static void clearSession() {
    username = null;
    displayName = null;
    role = null;
  }

  static Future<void> ensureTable() async {
    final db = await AppDatabase.instance.database;
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
  }

  static Future<void> log(
    String actionType,
    String moduleName, {
    String? detail,
  }) async {
    if ((username == null || username!.isEmpty) &&
        (role == null || role!.isEmpty)) {
      return;
    }
    try {
      await ensureTable();
      final db = await AppDatabase.instance.database;
      await db.insert('staff_entry_log', {
        'action_type': actionType,
        'module_name': moduleName,
        'detail': detail,
        'staff_username': username ?? '',
        'staff_name': displayName ?? '',
        'staff_role': role ?? '',
      });
    } catch (_) {
      // Never block business save on audit failure.
    }
  }

  static Future<List<Map<String, Object?>>> recentEntries({
    String? search,
    int limit = 500,
  }) async {
    await ensureTable();
    final db = await AppDatabase.instance.database;
    final s = (search ?? '').trim();
    if (s.isEmpty) {
      return db.query(
        'staff_entry_log',
        orderBy: 'id DESC',
        limit: limit,
      );
    }
    final like = '%$s%';
    return db.query(
      'staff_entry_log',
      where:
          "staff_username LIKE ? OR staff_name LIKE ? OR module_name LIKE ? OR action_type LIKE ? OR IFNULL(detail,'') LIKE ?",
      whereArgs: [like, like, like, like, like],
      orderBy: 'id DESC',
      limit: limit,
    );
  }

  static Future<int> insert(
    String table,
    Map<String, Object?> values,
  ) async {
    final db = await AppDatabase.instance.database;
    final id = await db.insert(table, values);
    await log('Insert', table, detail: 'id=$id');
    return id;
  }

  static Future<int> update(
    String table,
    Map<String, Object?> values, {
    String? where,
    List<Object?>? whereArgs,
  }) async {
    final db = await AppDatabase.instance.database;
    final n = await db.update(table, values, where: where, whereArgs: whereArgs);
    if (n > 0) {
      await log('Update', table, detail: where == null ? null : '$where $whereArgs');
    }
    return n;
  }

  static Future<int> delete(
    String table, {
    String? where,
    List<Object?>? whereArgs,
  }) async {
    final db = await AppDatabase.instance.database;
    final n = await db.delete(table, where: where, whereArgs: whereArgs);
    if (n > 0) {
      await log('Delete', table, detail: where == null ? null : '$where $whereArgs');
    }
    return n;
  }
}
