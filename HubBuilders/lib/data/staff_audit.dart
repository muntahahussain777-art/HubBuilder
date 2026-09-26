import 'dart:async';

import '../db/app_database.dart';
import 'dummy_data.dart';
import 'hub_sync.dart';
import 'auto_backup.dart';
import 'staff_access.dart';

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
    StaffAccess.invalidate();
  }

  static void clearSession() {
    username = null;
    displayName = null;
    role = null;
    StaffAccess.invalidate();
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
    final mod = StaffAccess.moduleForTable(table);
    if (!await StaffAccess.can(mod, 'add')) {
      throw Exception('Access denied: $mod add');
    }
    final db = await AppDatabase.instance.database;
    final id = await db.insert(table, values);
    await HubSync.pushNow(table, id);
    await log('Insert', table, detail: 'id=$id');
    HubSync.notifyLocalWrite();
    unawaited(AutoBackup.onEntry());
    return id;
  }

  static Future<int> update(
    String table,
    Map<String, Object?> values, {
    String? where,
    List<Object?>? whereArgs,
  }) async {
    final mod = StaffAccess.moduleForTable(table);
    if (!await StaffAccess.can(mod, 'edit')) {
      throw Exception('Access denied: $mod edit');
    }
    final db = await AppDatabase.instance.database;
    final n = await db.update(table, values, where: where, whereArgs: whereArgs);
    if (n > 0) {
      final detail = (whereArgs != null && whereArgs.isNotEmpty)
          ? 'id=${whereArgs.first}'
          : (where == null ? null : '$where $whereArgs');
      if (whereArgs != null && whereArgs.isNotEmpty) {
        await HubSync.pushNow(table, whereArgs.first);
      }
      await log('Update', table, detail: detail);
      HubSync.notifyLocalWrite();
      unawaited(AutoBackup.onEntry());
    }
    return n;
  }

  static Future<int> delete(
    String table, {
    String? where,
    List<Object?>? whereArgs,
  }) async {
    final mod = StaffAccess.moduleForTable(table);
    if (!await StaffAccess.can(mod, 'delete')) {
      throw Exception('Access denied: $mod delete');
    }
    final db = await AppDatabase.instance.database;
    final pkCol = HubSync.tablePk(table);
    final tombstones = <({int localPk, List<String> ids})>[];
    final userNames = <String>[];
    try {
      final rows = await db.query(
        table,
        where: where,
        whereArgs: whereArgs,
      );
      for (final r in rows) {
        final localPk = int.tryParse((r[pkCol] ?? '').toString()) ?? 0;
        if (localPk > 0) {
          final ids = await HubSync.collectCloudDeleteIds(table, localPk);
          tombstones.add((localPk: localPk, ids: ids));
        }
        if (table.toLowerCase() == 'users') {
          final un = (r['username'] ?? '').toString().trim();
          if (un.isNotEmpty) userNames.add(un);
        }
      }
    } catch (_) {}
    final n = await db.delete(table, where: where, whereArgs: whereArgs);
    if (n > 0) {
      for (final t in tombstones) {
        await HubSync.tombstoneLocalRow(table, t.localPk, t.ids);
      }
      for (final un in userNames) {
        await HubSync.pushDeletedUsername(un);
      }
      final detail = (whereArgs != null && whereArgs.isNotEmpty)
          ? 'id=${whereArgs.first}'
          : (where == null ? null : '$where $whereArgs');
      await log('Delete', table, detail: detail);
      HubSync.notifyLocalWrite();
      unawaited(AutoBackup.onEntry());
    }
    return n;
  }

  static final _idRe = RegExp(r'id\s*=\s*(\d+)', caseSensitive: false);
  static Map<String, String> _cache = {};
  static DateTime _cacheAt = DateTime.fromMillisecondsSinceEpoch(0);

  static Future<Map<String, String>> _idStaff() async {
    if (DateTime.now().difference(_cacheAt).inSeconds < 8 && _cache.isNotEmpty) {
      return _cache;
    }
    final next = <String, String>{};
    try {
      await ensureTable();
      final db = await AppDatabase.instance.database;
      final rows = await db.query('staff_entry_log', orderBy: 'id ASC');
      for (final r in rows) {
        final detail = (r['detail'] ?? '').toString();
        var name = (r['staff_name'] ?? '').toString().trim();
        if (name.isEmpty) name = (r['staff_username'] ?? '').toString().trim();
        if (name.isEmpty) continue;
        final m = _idRe.firstMatch(detail);
        if (m == null) continue;
        next['id=${m.group(1)}'] = name;
      }
    } catch (_) {}
    _cache = next;
    _cacheAt = DateTime.now();
    return next;
  }

  static Future<String> staffForId(int? id) async {
    if (id == null) return '';
    final map = await _idStaff();
    return map['id=$id'] ?? '';
  }

  static Future<List<ListRow>> stampRows(List<ListRow> rows) async {
    final map = await _idStaff();
    return rows
        .map((r) {
          if (r.id == null) return r;
          final s = map['id=${r.id}'] ?? '';
          if (s.isEmpty) return r;
          return r.copyWith(staff: s);
        })
        .toList();
  }
}
