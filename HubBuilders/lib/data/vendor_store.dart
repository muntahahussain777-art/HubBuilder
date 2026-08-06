import '../db/app_database.dart';
import 'dummy_data.dart';
import 'plot_inventory_store.dart';
import 'staff_audit.dart';

/// Expense / Person / Deal / Credit / Users — SQLite repositories.
abstract final class VendorStore {
  // ── Expenses (Expensetbl) ────────────────────────────────────

  static Future<List<ListRow>> expenseRows() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('Expensetbl', orderBy: 'sid DESC');
    return rows.map((r) {
      final amount = (r['Amount'] as num?)?.toDouble() ?? 0;
      return ListRow(
        id: r['sid'] as int?,
        title: r['Name'] as String? ?? '',
        subtitle: 'Category: ${r['Category'] ?? ''}',
        meta: 'Rs ${amount.toStringAsFixed(0)}',
        tag: r['EDate'] as String?,
        note: r['Note'] as String?,
        date: r['EDate'] as String?,
      );
    }).toList();
  }

  static Future<int> insertExpense({
    required String name,
    required double amount,
    String? category,
    String? note,
    String? date,
  }) async {
    return StaffAudit.insert('Expensetbl', {
      'Name': name.trim(),
      'Amount': amount,
      'Category': category?.trim(),
      'Note': note?.trim(),
      'EDate': date ??
          DateTime.now().toIso8601String().split('T').first,
    });
  }

  static Future<bool> updateExpense({
    required int id,
    required String name,
    required double amount,
    String? category,
    String? note,
    String? date,
  }) async {
    final n = await StaffAudit.update(
      'Expensetbl',
      {
        'Name': name.trim(),
        'Amount': amount,
        'Category': category?.trim(),
        'Note': note?.trim(),
        'EDate': date,
      },
      where: 'sid = ?',
      whereArgs: [id],
    );
    return n > 0;
  }

  // ── Person (owners / partners) ───────────────────────────────

  static Future<List<ListRow>> ownerRows() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'Person',
      where: 'is_owner = 1',
      orderBy: 'person_id DESC',
    );
    return rows
        .map(
          (r) => ListRow(
            id: r['person_id'] as int?,
            title: r['name'] as String? ?? '',
            subtitle: 'Phone: ${r['phone'] ?? ''}',
            meta: r['note'] as String? ?? '',
            tag: 'Owner',
            mobile: r['phone'] as String?,
            note: r['note'] as String?,
          ),
        )
        .toList();
  }

  static Future<List<ListRow>> partnerRows() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'Person',
      where: 'is_partner = 1',
      orderBy: 'person_id DESC',
    );
    return rows
        .map(
          (r) => ListRow(
            id: r['person_id'] as int?,
            title: r['name'] as String? ?? '',
            subtitle: 'Phone: ${r['phone'] ?? ''}',
            meta: r['note'] as String? ?? '',
            tag: 'Partner',
            mobile: r['phone'] as String?,
            note: r['note'] as String?,
          ),
        )
        .toList();
  }

  static Future<List<IdName>> owners() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'Person',
      where: 'is_owner = 1',
      orderBy: 'name COLLATE NOCASE',
    );
    return rows
        .map((r) => IdName(r['person_id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<List<IdName>> partners() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'Person',
      where: 'is_partner = 1',
      orderBy: 'name COLLATE NOCASE',
    );
    return rows
        .map((r) => IdName(r['person_id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<int> insertPerson({
    required String name,
    String? phone,
    String? note,
    required bool isOwner,
    required bool isPartner,
  }) async {
    return StaffAudit.insert('Person', {
      'name': name.trim(),
      'phone': phone?.trim(),
      'note': note?.trim(),
      'is_owner': isOwner ? 1 : 0,
      'is_partner': isPartner ? 1 : 0,
    });
  }

  static Future<bool> updatePerson({
    required int id,
    required String name,
    String? phone,
    String? note,
    required bool isOwner,
    required bool isPartner,
  }) async {
    final n = await StaffAudit.update(
      'Person',
      {
        'name': name.trim(),
        'phone': phone?.trim(),
        'note': note?.trim(),
        'is_owner': isOwner ? 1 : 0,
        'is_partner': isPartner ? 1 : 0,
      },
      where: 'person_id = ?',
      whereArgs: [id],
    );
    return n > 0;
  }

  // ── Deal ─────────────────────────────────────────────────────

  static Future<List<ListRow>> dealRows() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery('''
SELECT d.deal_id, d.amount, d.deal_date, d.note,
       s.name AS society, p.name AS owner_name
FROM Deal d
JOIN societies s ON s.id = d.society_id
JOIN Person p ON p.person_id = d.owner_id
ORDER BY d.deal_id DESC
''');
    return rows.map((r) {
      final amount = (r['amount'] as num?)?.toDouble() ?? 0;
      return ListRow(
        id: r['deal_id'] as int?,
        title: r['society'] as String? ?? '',
        subtitle: 'Owner: ${r['owner_name'] ?? ''}',
        meta: 'Rs ${amount.toStringAsFixed(0)}',
        tag: r['deal_date'] as String?,
        note: r['note'] as String?,
        date: r['deal_date'] as String?,
      );
    }).toList();
  }

  static Future<int> insertDeal({
    required int societyId,
    required int ownerId,
    required double amount,
    String? dealDate,
    String? note,
  }) async {
    return StaffAudit.insert('Deal', {
      'society_id': societyId,
      'owner_id': ownerId,
      'amount': amount,
      'deal_date': dealDate ??
          DateTime.now().toIso8601String().split('T').first,
      'note': note?.trim(),
    });
  }

  // ── Credit ───────────────────────────────────────────────────

  static Future<List<ListRow>> creditRows() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery('''
SELECT c.credit_id, c.amount, c.pay_date, c.note,
       p.name AS partner_name, d.deal_id,
       s.name AS society, o.name AS owner_name
FROM Credit c
JOIN Deal d ON d.deal_id = c.deal_id
JOIN Person p ON p.person_id = c.partner_id
JOIN Person o ON o.person_id = d.owner_id
JOIN societies s ON s.id = d.society_id
ORDER BY c.credit_id DESC
''');
    return rows.map((r) {
      final amount = (r['amount'] as num?)?.toDouble() ?? 0;
      return ListRow(
        id: r['credit_id'] as int?,
        title: r['partner_name'] as String? ?? '',
        subtitle: '${r['society']} · Owner ${r['owner_name']}',
        meta: 'Rs ${amount.toStringAsFixed(0)}',
        tag: r['pay_date'] as String?,
        note: r['note'] as String?,
        date: r['pay_date'] as String?,
      );
    }).toList();
  }

  /// Resolves latest Deal for society+owner (C# Credit form behavior).
  static Future<int?> latestDealId({
    required int societyId,
    required int ownerId,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'Deal',
      columns: ['deal_id'],
      where: 'society_id = ? AND owner_id = ?',
      whereArgs: [societyId, ownerId],
      orderBy: 'deal_id DESC',
      limit: 1,
    );
    if (rows.isEmpty) return null;
    return rows.first['deal_id'] as int?;
  }

  static Future<int> insertCredit({
    required int dealId,
    required int partnerId,
    required double amount,
    String? payDate,
    String? note,
  }) async {
    return StaffAudit.insert('Credit', {
      'deal_id': dealId,
      'partner_id': partnerId,
      'amount': amount,
      'pay_date': payDate ??
          DateTime.now().toIso8601String().split('T').first,
      'note': note?.trim(),
    });
  }
}

abstract final class AuthStore {
  static Future<({bool ok, String? role, String message})> login({
    required String username,
    required String password,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'users',
      where: 'username = ? AND upass = ?',
      whereArgs: [username.trim(), password],
    );
    if (rows.isEmpty) {
      return (ok: false, role: null, message: 'Invalid username or password.');
    }
    final row = rows.first;
    final role = row['Role'] as String? ?? '';
    final uName = row['uName'] as String? ?? '';
    StaffAudit.setSession(
      username: row['username'] as String? ?? username.trim(),
      displayName: uName,
      role: role,
    );
    await StaffAudit.ensureTable();
    return (
      ok: true,
      role: role,
      message: 'OK',
    );
  }

  /// Fingerprint unlock ke baad — password ke baghair session set.
  static Future<({bool ok, String message})> resumeSession(String username) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'users',
      where: 'username = ?',
      whereArgs: [username.trim()],
    );
    if (rows.isEmpty) {
      return (ok: false, message: 'User nahi mila. Settings se fingerprint dobara enable karein.');
    }
    final row = rows.first;
    StaffAudit.setSession(
      username: row['username'] as String? ?? username.trim(),
      displayName: row['uName'] as String? ?? '',
      role: row['Role'] as String? ?? '',
    );
    await StaffAudit.ensureTable();
    return (ok: true, message: 'OK');
  }

  static Future<({bool ok, String message})> changePassword({
    required String username,
    required String currentPassword,
    required String newPassword,
  }) async {
    if (newPassword.trim().isEmpty) {
      return (ok: false, message: 'New password required.');
    }
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'users',
      where: 'username = ? AND upass = ?',
      whereArgs: [username.trim(), currentPassword],
    );
    if (rows.isEmpty) {
      return (ok: false, message: 'Current password incorrect.');
    }
    final n = await StaffAudit.update(
      'users',
      {'upass': newPassword},
      where: 'username = ?',
      whereArgs: [username.trim()],
    );
    if (n <= 0) return (ok: false, message: 'Update failed.');
    return (ok: true, message: 'Password updated.');
  }

  static Future<List<Map<String, Object?>>> users() async {
    final db = await AppDatabase.instance.database;
    return db.query('users', orderBy: 'userID');
  }
}

/// List helpers for master data modules.
abstract final class MasterListStore {
  static Future<List<ListRow>> customers() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('customers', orderBy: 'id DESC');
    return rows.map((r) {
      final name = r['name'] as String? ?? '';
      final father = r['father_name'] as String? ?? '';
      final mobile = r['mobile'] as String? ?? '';
      final cnic = r['cnic'] as String? ?? '';
      final created = r['created_at'] as String? ?? '';
      final date = created.length >= 10 ? created.substring(0, 10) : created;
      return ListRow(
        id: r['id'] as int?,
        title: name,
        fatherName: father,
        mobile: mobile,
        cnic: cnic,
        note: r['notes'] as String?,
        date: date,
        subtitle: 'FatherName: $father · Mobile: $mobile',
        meta: 'CNIC $cnic',
        tag: date,
      );
    }).toList();
  }

  static Future<List<ListRow>> societies() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('societies', orderBy: 'id DESC');
    return rows.map((r) {
      final created = r['created_at'] as String? ?? '';
      final date = created.length >= 10 ? created.substring(0, 10) : created;
      final city = r['city'] as String? ?? '';
      return ListRow(
        id: r['id'] as int?,
        title: r['name'] as String? ?? '',
        city: city,
        date: date,
        subtitle: 'City: $city',
        meta: 'Date $date',
        tag: date,
      );
    }).toList();
  }

  static Future<List<ListRow>> blocks() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery('''
SELECT b.id, b.name, s.name AS society
FROM blocks b
JOIN societies s ON s.id = b.society_id
ORDER BY b.id DESC
''');
    return rows
        .map(
          (r) => ListRow(
            id: r['id'] as int?,
            title: r['name'] as String? ?? '',
            city: r['society'] as String?,
            subtitle: 'Society: ${r['society']}',
            meta: r['name'] as String? ?? '',
            tag: 'Block',
          ),
        )
        .toList();
  }

  static Future<List<ListRow>> plotTypes() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery('''
SELECT pt.id, pt.code, s.name AS society
FROM plot_types pt
JOIN societies s ON s.id = pt.society_id
ORDER BY pt.id DESC
''');
    return rows
        .map(
          (r) => ListRow(
            id: r['id'] as int?,
            title: r['code'] as String? ?? '',
            city: r['society'] as String?,
            subtitle: 'Society: ${r['society']}',
            meta: 'Size: ${r['code']}',
            tag: 'Size',
          ),
        )
        .toList();
  }

  static Future<List<ListRow>> brokers() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('brokers', orderBy: 'id DESC');
    return rows.map((r) {
      final active = (r['is_active'] as int? ?? 1) == 1;
      final comm = (r['commission_amount_default'] as num?)?.toDouble() ?? 0;
      return ListRow(
        id: r['id'] as int?,
        title: r['name'] as String? ?? '',
        subtitle: 'Phone: ${r['phone'] ?? ''}',
        meta: 'Comm ${comm.toStringAsFixed(0)}',
        tag: active ? 'Active' : 'Inactive',
        mobile: r['phone'] as String?,
      );
    }).toList();
  }
}
