import '../db/app_database.dart';

class DashboardStats {
  const DashboardStats({
    required this.availablePlots,
    required this.activeBookings,
    required this.monthReceipts,
  });

  final int availablePlots;
  final int activeBookings;
  final double monthReceipts;
}

/// Same idea as WinForms frmDashboard — live SQLite counts.
abstract final class DashboardStore {
  static Future<DashboardStats> load() async {
    final db = await AppDatabase.instance.database;
    final now = DateTime.now();
    final monthStart =
        '${now.year.toString().padLeft(4, '0')}-${now.month.toString().padLeft(2, '0')}-01';

    final avail = await db.rawQuery(
      "SELECT COUNT(1) AS c FROM plots WHERE status = 'Available'",
    );
    final booked = await db.rawQuery(
      "SELECT COUNT(1) AS c FROM customer_plots WHERE status = 'Active'",
    );
    final receipts = await db.rawQuery(
      '''
SELECT COALESCE(SUM(amount),0) AS t FROM receipts
WHERE DATE(receipt_date) >= DATE(?)''',
      [monthStart],
    );

    return DashboardStats(
      availablePlots: (avail.first['c'] as num?)?.toInt() ?? 0,
      activeBookings: (booked.first['c'] as num?)?.toInt() ?? 0,
      monthReceipts: (receipts.first['t'] as num?)?.toDouble() ?? 0,
    );
  }
}
