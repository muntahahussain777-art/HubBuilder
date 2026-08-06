import '../db/app_database.dart';
import 'staff_audit.dart';
import 'plot_inventory_store.dart';

class CustomerItem {
  const CustomerItem(
    this.id,
    this.name,
    this.mobile, {
    this.fatherName = '',
    this.cnic = '',
    this.notes = '',
    this.createdAt = '',
  });

  final int id;
  final String name;
  final String mobile;
  final String fatherName;
  final String cnic;
  final String notes;
  final String createdAt;

  @override
  bool operator ==(Object other) => other is CustomerItem && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class BrokerItem {
  const BrokerItem(
    this.id,
    this.name,
    this.commissionDefault, {
    this.phone = '',
    this.isActive = true,
  });

  final int id;
  final String name;
  final double commissionDefault;
  final String phone;
  final bool isActive;

  @override
  bool operator ==(Object other) => other is BrokerItem && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class AllocationRecord {
  AllocationRecord({
    required this.id,
    required this.customerId,
    required this.plotId,
    required this.price,
    required this.bookingDate,
    required this.notes,
    required this.status,
    required this.brokerId,
    required this.brokerCommission,
  });

  final int id;
  final int customerId;
  final int plotId;
  double price;
  String bookingDate;
  String notes;
  String status;
  int brokerId;
  double brokerCommission;

  factory AllocationRecord.fromMap(Map<String, Object?> m) => AllocationRecord(
        id: m['id'] as int,
        customerId: m['customer_id'] as int,
        plotId: m['plot_id'] as int,
        price: (m['price'] as num?)?.toDouble() ?? 0,
        bookingDate: m['booking_date'] as String? ?? '',
        notes: m['notes'] as String? ?? '',
        status: m['status'] as String? ?? 'Active',
        brokerId: m['broker_id'] as int? ?? 0,
        brokerCommission: (m['broker_commission'] as num?)?.toDouble() ?? 0,
      );
}

class AllocationListRow {
  AllocationListRow({
    required this.id,
    required this.customer,
    required this.phone,
    required this.society,
    required this.block,
    required this.type,
    required this.plotNo,
    required this.price,
    required this.bookingDate,
    required this.status,
  });

  final int id;
  final String customer;
  final String phone;
  final String society;
  final String block;
  final String type;
  final String plotNo;
  final double price;
  final String bookingDate;
  final String status;
}

/// SQLite-backed customers / brokers / customer_plots.
abstract final class AllocateStore {
  static Future<List<CustomerItem>> customers() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('customers', orderBy: 'name COLLATE NOCASE');
    return rows
        .map(
          (r) => CustomerItem(
            r['id'] as int,
            r['name'] as String? ?? '',
            r['mobile'] as String? ?? '',
            fatherName: r['father_name'] as String? ?? '',
            cnic: r['cnic'] as String? ?? '',
            notes: r['notes'] as String? ?? '',
            createdAt: r['created_at'] as String? ?? '',
          ),
        )
        .toList();
  }

  static Future<CustomerItem?> customerById(int id) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('customers', where: 'id = ?', whereArgs: [id]);
    if (rows.isEmpty) return null;
    final r = rows.first;
    return CustomerItem(
      r['id'] as int,
      r['name'] as String? ?? '',
      r['mobile'] as String? ?? '',
      fatherName: r['father_name'] as String? ?? '',
      cnic: r['cnic'] as String? ?? '',
      notes: r['notes'] as String? ?? '',
      createdAt: r['created_at'] as String? ?? '',
    );
  }

  static Future<int> insertCustomer({
    required String name,
    String? fatherName,
    String? mobile,
    String? cnic,
    String? notes,
    String? createdAt,
  }) async {
    final db = await AppDatabase.instance.database;
    return StaffAudit.insert('customers', {
      'name': name.trim(),
      'father_name': fatherName?.trim(),
      'mobile': mobile?.trim(),
      'cnic': (cnic == null || cnic.trim().isEmpty) ? null : cnic.trim(),
      'notes': notes?.trim(),
      'created_at': createdAt ??
          DateTime.now()
              .toIso8601String()
              .replaceFirst('T', ' ')
              .split('.')
              .first,
    });
  }

  static Future<bool> updateCustomer({
    required int id,
    required String name,
    String? fatherName,
    String? mobile,
    String? cnic,
    String? notes,
  }) async {
    final db = await AppDatabase.instance.database;
    final n = await StaffAudit.update(
      'customers',
      {
        'name': name.trim(),
        'father_name': fatherName?.trim(),
        'mobile': mobile?.trim(),
        'cnic': (cnic == null || cnic.trim().isEmpty) ? null : cnic.trim(),
        'notes': notes?.trim(),
      },
      where: 'id = ?',
      whereArgs: [id],
    );
    return n > 0;
  }

  static Future<List<BrokerItem>> brokers({bool activeOnly = false}) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'brokers',
      where: activeOnly ? 'is_active = 1' : null,
      orderBy: 'name COLLATE NOCASE',
    );
    return rows
        .map(
          (r) => BrokerItem(
            r['id'] as int,
            r['name'] as String? ?? '',
            (r['commission_amount_default'] as num?)?.toDouble() ?? 0,
            phone: r['phone'] as String? ?? '',
            isActive: (r['is_active'] as int? ?? 1) == 1,
          ),
        )
        .toList();
  }

  static Future<BrokerItem?> brokerById(int id) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('brokers', where: 'id = ?', whereArgs: [id]);
    if (rows.isEmpty) return null;
    final r = rows.first;
    return BrokerItem(
      r['id'] as int,
      r['name'] as String? ?? '',
      (r['commission_amount_default'] as num?)?.toDouble() ?? 0,
      phone: r['phone'] as String? ?? '',
      isActive: (r['is_active'] as int? ?? 1) == 1,
    );
  }

  static Future<int> insertBroker({
    required String name,
    String? phone,
    double commissionDefault = 0,
    bool isActive = true,
  }) async {
    final db = await AppDatabase.instance.database;
    return StaffAudit.insert('brokers', {
      'name': name.trim(),
      'phone': phone?.trim(),
      'is_active': isActive ? 1 : 0,
      'commission_amount_default': commissionDefault,
    });
  }

  static Future<bool> updateBroker({
    required int id,
    required String name,
    String? phone,
    double commissionDefault = 0,
    bool isActive = true,
  }) async {
    final db = await AppDatabase.instance.database;
    final n = await StaffAudit.update(
      'brokers',
      {
        'name': name.trim(),
        'phone': phone?.trim(),
        'is_active': isActive ? 1 : 0,
        'commission_amount_default': commissionDefault,
      },
      where: 'id = ?',
      whereArgs: [id],
    );
    return n > 0;
  }

  static Future<AllocationRecord?> allocationById(int id) async {
    final db = await AppDatabase.instance.database;
    final rows =
        await db.query('customer_plots', where: 'id = ?', whereArgs: [id]);
    if (rows.isEmpty) return null;
    return AllocationRecord.fromMap(rows.first);
  }

  static Future<List<AllocationRecord>> allocations() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('customer_plots', orderBy: 'id DESC');
    return rows.map(AllocationRecord.fromMap).toList();
  }

  static Future<List<AllocationListRow>> list({String query = ''}) async {
    final db = await AppDatabase.instance.database;
    final q = query.trim().toLowerCase();
    final rows = await db.rawQuery('''
SELECT cp.id, cp.price, cp.booking_date, cp.status,
       c.name AS customer, c.mobile AS phone,
       s.name AS society, b.name AS block, pt.code AS type,
       p.plot_no
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
JOIN societies s ON s.id = p.society_id
JOIN blocks b ON b.id = p.block_id
JOIN plot_types pt ON pt.id = p.plot_type_id
ORDER BY cp.id DESC
''');

    final out = <AllocationListRow>[];
    for (final r in rows) {
      final row = AllocationListRow(
        id: r['id'] as int,
        customer: r['customer'] as String? ?? '',
        phone: r['phone'] as String? ?? '',
        society: r['society'] as String? ?? '',
        block: r['block'] as String? ?? '',
        type: r['type'] as String? ?? '',
        plotNo: r['plot_no'] as String? ?? '',
        price: (r['price'] as num?)?.toDouble() ?? 0,
        bookingDate: r['booking_date'] as String? ?? '',
        status: r['status'] as String? ?? '',
      );
      if (q.isEmpty ||
          row.customer.toLowerCase().contains(q) ||
          row.phone.contains(q) ||
          row.plotNo.toLowerCase().contains(q) ||
          row.society.toLowerCase().contains(q) ||
          row.block.toLowerCase().contains(q)) {
        out.add(row);
      }
    }
    return out;
  }

  /// Same as C# allocate: Hold Available plot → insert customer_plots Active.
  static Future<({bool ok, String message, int allocId})> allocate({
    required int customerId,
    required int societyId,
    required int blockId,
    required int plotTypeId,
    required int plotId,
    required double price,
    required String bookingDate,
    required String notes,
    required int brokerId,
    required double brokerCommission,
  }) async {
    if (price <= 0) {
      return (ok: false, message: 'Price required hai.', allocId: 0);
    }
    if (brokerCommission < 0) {
      return (
        ok: false,
        message: 'Broker commission 0 ya us se zyada honi chahiye.',
        allocId: 0,
      );
    }

    final plot = await PlotInventoryStore.plotById(plotId);
    if (plot == null ||
        plot.societyId != societyId ||
        plot.blockId != blockId ||
        plot.plotTypeId != plotTypeId) {
      return (
        ok: false,
        message: 'Selected plot ab Available nahi raha.',
        allocId: 0,
      );
    }

    if (!await PlotInventoryStore.holdIfAvailable(plotId)) {
      return (
        ok: false,
        message: 'Selected plot ab Available nahi raha.',
        allocId: 0,
      );
    }

    try {
      final db = await AppDatabase.instance.database;
      final id = await db.insert('customer_plots', {
        'customer_id': customerId,
        'plot_id': plotId,
        'price': price,
        'booking_date': bookingDate,
        'notes': notes,
        'status': 'Active',
        'broker_id': brokerId > 0 ? brokerId : null,
        'broker_commission': brokerCommission,
        'down_payment': 0,
      });
      await StaffAudit.log('Insert', 'customer_plots',
          detail: 'id=$id; plotId=$plotId; price=$price');
      return (
        ok: true,
        message: 'Plot allocate ho gaya (plot status: Hold).',
        allocId: id,
      );
    } catch (e) {
      await PlotInventoryStore.releaseHold(plotId);
      return (
        ok: false,
        message: 'Allocation save fail: $e',
        allocId: 0,
      );
    }
  }
}
