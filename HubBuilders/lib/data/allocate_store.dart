import 'package:sqflite/sqflite.dart';

import '../db/app_database.dart';
import 'hub_sync.dart';
import 'partner_cash_store.dart';
import 'plot_inventory_store.dart';
import 'staff_audit.dart';

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
    required     this.bookingDate,
    required this.status,
    this.staff = '',
    this.monthlyInstallment = 0,
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
  final String staff;
  final double monthlyInstallment;
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

  /// Cascade: pehle receipts → phir allocate → phir customer.
  static Future<({bool ok, String message})> deleteCustomer(int id) async {
    final db = await AppDatabase.instance.database;
    final recObj = await db.rawQuery(
      '''
SELECT COUNT(*) AS c
FROM receipts r
JOIN customer_plots cp ON cp.id = r.customer_plot_id
WHERE cp.customer_id = ?''',
      [id],
    );
    final recCount = (recObj.first['c'] as num?)?.toInt() ?? 0;
    if (recCount > 0) {
      return (
        ok: false,
        message:
            'Customer delete allow nahi.\nPehle Receipt Form se is customer ki $recCount receipt(s) delete karein.\nPhir Allocate se delete, phir customer.',
      );
    }

    final allocObj = await db.rawQuery(
      'SELECT COUNT(*) AS c FROM customer_plots WHERE customer_id = ?',
      [id],
    );
    final allocCount = (allocObj.first['c'] as num?)?.toInt() ?? 0;
    if (allocCount > 0) {
      return (
        ok: false,
        message:
            'Customer delete allow nahi.\nPehle Allocate se is customer ki $allocCount allocation(s) delete karein.\nPhir customer delete hoga.',
      );
    }

    final n = await StaffAudit.delete(
      'customers',
      where: 'id = ?',
      whereArgs: [id],
    );
    if (n <= 0) {
      return (ok: false, message: 'Customer delete failed / not found.');
    }
    return (ok: true, message: 'Customer delete ho gaya.');
  }

  /// Hard delete allocation — linked receipts/ledger/partner cash bhi (WinForms cascade).
  /// Local deletes + exact sync_id pending_delete EK transaction; SoftMark HTTP bahar.
  static Future<({bool ok, String message})> deleteAllocation(
    int customerPlotId,
  ) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'customer_plots',
      where: 'id = ?',
      whereArgs: [customerPlotId],
      limit: 1,
    );
    if (rows.isEmpty) {
      return (ok: false, message: 'Allocation not found.');
    }
    final plotId = rows.first['plot_id'] as int;
    final status = (rows.first['status'] as String?) ?? '';

    Future<void> collect(
      String table,
      String where,
      List<Object?> args,
      List<({String table, String sid})> out,
    ) async {
      try {
        final found = await db.query(table, columns: ['id'], where: where, whereArgs: args);
        for (final r in found) {
          final id = int.tryParse((r['id'] ?? '').toString()) ?? 0;
          if (id <= 0) continue;
          for (final sid in await HubSync.collectCloudDeleteIds(table, id)) {
            if (sid.trim().isNotEmpty) {
              out.add((table: table, sid: sid.trim()));
            }
          }
        }
      } catch (_) {}
    }

    final tombstones = <({String table, String sid})>[];
    await collect('partner_money', 'customer_plot_id = ?', [customerPlotId], tombstones);
    await collect('receipts', 'customer_plot_id = ?', [customerPlotId], tombstones);
    await collect('ledger', 'customer_plot_id = ?', [customerPlotId], tombstones);
    await collect(
      'customer_plot_partners',
      'customer_plot_id = ?',
      [customerPlotId],
      tombstones,
    );
    for (final sid
        in await HubSync.collectCloudDeleteIds('customer_plots', customerPlotId)) {
      if (sid.trim().isNotEmpty) {
        tombstones.add((table: 'customer_plots', sid: sid.trim()));
      }
    }

    try {
      var deleted = 0;
      await db.transaction((txn) async {
        try {
          await txn.delete(
            'allocation_cancellations',
            where: 'customer_plot_id = ?',
            whereArgs: [customerPlotId],
          );
        } catch (_) {}
        for (final t in tombstones) {
          await txn.insert(
            'sync_pending_delete',
            {'sync_id': t.sid, 'table_name': t.table},
            conflictAlgorithm: ConflictAlgorithm.replace,
          );
        }
        await txn.delete(
          'partner_money',
          where: 'customer_plot_id = ?',
          whereArgs: [customerPlotId],
        );
        await txn.delete(
          'receipts',
          where: 'customer_plot_id = ?',
          whereArgs: [customerPlotId],
        );
        await txn.delete(
          'ledger',
          where: 'customer_plot_id = ?',
          whereArgs: [customerPlotId],
        );
        try {
          await txn.delete(
            'customer_plot_partners',
            where: 'customer_plot_id = ?',
            whereArgs: [customerPlotId],
          );
        } catch (_) {}
        deleted = await txn.delete(
          'customer_plots',
          where: 'id = ?',
          whereArgs: [customerPlotId],
        );
        if (deleted <= 0) {
          throw StateError('Allocation delete failed.');
        }
        if (status == 'Active' || status == 'Hold') {
          await txn.update(
            'plots',
            {'status': 'Available'},
            where: "id = ? AND status IN ('Hold','Sold')",
            whereArgs: [plotId],
          );
        }
      });

      for (final t in tombstones) {
        try {
          await HubSync.pushDeleted(t.table, t.sid);
        } catch (_) {}
      }
      if (status == 'Active' || status == 'Hold') {
        await HubSync.pushNow('plots', plotId);
      }
      await StaffAudit.log('Delete', 'customer_plots', detail: 'id=$customerPlotId');
      HubSync.notifyLocalWrite(alreadyPushed: true);
      return (ok: true, message: 'Allocation delete ho gaya.');
    } catch (e) {
      return (ok: false, message: 'Allocation delete failed: $e');
    }
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

  static Future<bool> deleteBroker(int id) async {
    final n = await StaffAudit.delete(
      'brokers',
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

  static Future<List<AllocationListRow>> list({
    String query = '',
    int societyId = 0,
  }) async {
    final db = await AppDatabase.instance.database;
    final q = query.trim().toLowerCase();
    final rows = await db.rawQuery(
      '''
SELECT cp.id, cp.price, cp.booking_date, cp.status,
       COALESCE(cp.monthly_installment,0) AS monthly_installment,
       c.name AS customer, c.mobile AS phone,
       s.name AS society, b.name AS block, pt.code AS type,
       p.plot_no, s.id AS society_id
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE IFNULL(cp.status,'') <> 'Cancelled'
  AND (? = 0 OR IFNULL(s.id,0) = ?)
ORDER BY cp.id DESC
''',
      [societyId, societyId],
    );

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
        monthlyInstallment: (r['monthly_installment'] as num?)?.toDouble() ?? 0,
        staff: await StaffAudit.staffForId(r['id'] as int),
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
    int partnerId = 0,
    String dealType = 'Installment',
    double monthlyInstallment = 0,
  }) async {
    // Price / broker / commission optional — 0 = reserve without money
    if (price < 0) price = 0;
    if (brokerCommission < 0) brokerCommission = 0;
    final deal = _normalizeDealType(dealType);
    final plotStatus = deal == 'Sold' ? 'Sold' : 'Hold';

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

    if (!await PlotInventoryStore.holdIfAvailable(plotId, status: plotStatus)) {
      return (
        ok: false,
        message: 'Selected plot ab Available nahi raha.',
        allocId: 0,
      );
    }

    try {
      final db = await AppDatabase.instance.database;
      await AppDatabase.ensureCustomerPlotsDealType(db);
      await AppDatabase.ensureCustomerPlotsMonthlyInstallment(db);
      final syncIdRows = await db.rawQuery(
        "SELECT lower(hex(randomblob(16))) AS s",
      );
      final syncId = (syncIdRows.first['s'] ?? '').toString();
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
        'deal_type': deal,
        'monthly_installment': monthlyInstallment < 0 ? 0 : monthlyInstallment,
        if (syncId.isNotEmpty) 'sync_id': syncId,
      });
      if (partnerId > 0) {
        try {
          await PartnerCashStore.syncAllocationPartnerCommission(
            customerPlotId: id,
            partnerId: partnerId,
            amount: brokerCommission,
            txnDate: bookingDate,
          );
        } catch (_) {}
      }
      await StaffAudit.log('Insert', 'customer_plots',
          detail: 'id=$id; plotId=$plotId; price=$price; deal=$deal; partner=$partnerId');
      await HubSync.pushNow('plots', plotId);
      await HubSync.pushNow('customer_plots', id);
      HubSync.notifyLocalWrite(alreadyPushed: true);
      return (
        ok: true,
        message: 'Plot allocate ho gaya ($deal — plot: $plotStatus).',
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

  static String _normalizeDealType(String? raw) {
    final s = (raw ?? '').trim().toLowerCase();
    if (s == 'reserve') return 'Reserve';
    if (s == 'sold') return 'Sold';
    return 'Installment';
  }

  static Future<({bool ok, String message})> updateAllocation({
    required int customerPlotId,
    required double price,
    required String bookingDate,
    required String notes,
    int brokerId = 0,
    double brokerCommission = 0,
    double monthlyInstallment = 0,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'customer_plots',
      where: 'id = ?',
      whereArgs: [customerPlotId],
      limit: 1,
    );
    if (rows.isEmpty) return (ok: false, message: 'Allocation not found.');
    if ((rows.first['status'] as String?) != 'Active') {
      return (ok: false, message: 'Only Active allocation edit ho sakti hai.');
    }
    await db.update(
      'customer_plots',
      {
        'price': price,
        'booking_date': bookingDate,
        'notes': notes,
        'broker_id': brokerId > 0 ? brokerId : null,
        'broker_commission': brokerCommission,
        'monthly_installment': monthlyInstallment < 0 ? 0 : monthlyInstallment,
      },
      where: 'id = ?',
      whereArgs: [customerPlotId],
    );
    await StaffAudit.log('Update', 'customer_plots',
        detail: 'id=$customerPlotId; price=$price');
    await HubSync.pushNow('customer_plots', customerPlotId);
    HubSync.notifyLocalWrite(alreadyPushed: true);
    return (ok: true, message: 'Allocation updated.');
  }

  /// WinForms cancel — deduction % on received, free plot to Available.
  static Future<({bool ok, String message})> cancelAllocation({
    required int customerPlotId,
    double deductionPercent = 30,
    String? notes,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      '''
SELECT cp.id, cp.plot_id, cp.status, cp.notes,
  COALESCE((SELECT SUM(amount) FROM receipts r WHERE r.customer_plot_id = cp.id),0) AS received
FROM customer_plots cp WHERE cp.id = ?''',
      [customerPlotId],
    );
    if (rows.isEmpty) return (ok: false, message: 'Allocation not found.');
    final r = rows.first;
    if ((r['status'] as String?) != 'Active') {
      return (ok: false, message: 'Only Active allocation cancel ho sakti hai.');
    }
    final received = (r['received'] as num?)?.toDouble() ?? 0;
    final plotId = r['plot_id'] as int;
    final pct = deductionPercent.clamp(0, 100);
    final deduction = received * pct / 100;
    final refundable = received - deduction;
    final cancelDate = DateTime.now().toIso8601String().split('T').first;

    final cancelId = await db.insert('allocation_cancellations', {
      'customer_plot_id': customerPlotId,
      'cancel_date': cancelDate,
      'deduction_percent': pct,
      'allocation_amount': 0,
      'received_amount': received,
      'deduction_amount': deduction,
      'refundable_amount': refundable,
      'notes': notes,
    });

    final oldNotes = (r['notes'] as String?) ?? '';
    final append =
        ' [Cancelled $cancelDate; ded=$pct%; refund=${refundable.toStringAsFixed(2)}]';
    await db.update(
      'customer_plots',
      {
        'status': 'Cancelled',
        'cancelled_date': cancelDate,
        'cancellation_id': cancelId,
        'notes': oldNotes + append,
      },
      where: 'id = ?',
      whereArgs: [customerPlotId],
    );
    await db.update(
      'plots',
      {'status': 'Available'},
      where: 'id = ?',
      whereArgs: [plotId],
    );
    await StaffAudit.log('Cancel', 'customer_plots',
        detail: 'id=$customerPlotId; refund=$refundable');
    await HubSync.pushNow('customer_plots', customerPlotId);
    await HubSync.pushNow('plots', plotId);
    HubSync.notifyLocalWrite(alreadyPushed: true);
    return (ok: true, message: 'Allocation cancelled. Plot Available.');
  }

  static Future<({bool ok, String message})> setStatus({
    required int customerPlotId,
    required String status,
  }) async {
    if (status != 'Active' && status != 'Cancelled' && status != 'Completed') {
      return (ok: false, message: 'Invalid status.');
    }
    if (status == 'Cancelled') {
      return cancelAllocation(customerPlotId: customerPlotId);
    }
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'customer_plots',
      where: 'id = ?',
      whereArgs: [customerPlotId],
      limit: 1,
    );
    if (rows.isEmpty) return (ok: false, message: 'Not found.');
    final plotId = rows.first['plot_id'] as int;
    final cur = rows.first['status'] as String? ?? '';
    if (cur == 'Completed' && status != 'Completed') {
      return (ok: false, message: 'Completed allocation status change nahi.');
    }
    await db.update(
      'customer_plots',
      {'status': status},
      where: 'id = ?',
      whereArgs: [customerPlotId],
    );
    if (status == 'Completed') {
      await db.update(
        'plots',
        {'status': 'Sold'},
        where: 'id = ?',
        whereArgs: [plotId],
      );
    }
    await StaffAudit.log('Update', 'customer_plots',
        detail: 'id=$customerPlotId; status=$status');
    await HubSync.pushNow('customer_plots', customerPlotId);
    if (status == 'Completed') {
      await HubSync.pushNow('plots', plotId);
    }
    HubSync.notifyLocalWrite(alreadyPushed: true);
    return (ok: true, message: 'Status updated.');
  }
}
