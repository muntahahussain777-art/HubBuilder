import 'package:intl/intl.dart';

import '../db/app_database.dart';
import 'block_monthly_store.dart';
import 'plot_inventory_store.dart';

class DuesWhatsAppRow {
  const DuesWhatsAppRow({
    required this.customerPlotId,
    required this.customer,
    required this.mobile,
    required this.society,
    required this.block,
    required this.plotNo,
    required this.bookingDate,
    required this.price,
    required this.paid,
    required this.due,
    required this.missedCsv,
    required this.missedCount,
    required this.cyclesCsv,
    required this.cycleCount,
    required this.lastPayment,
    this.monthlyInstallment = 0,
  });

  final int customerPlotId;
  final String customer;
  final String mobile;
  final String society;
  final String block;
  final String plotNo;
  final String bookingDate;
  final double price;
  final double paid;
  final double due;
  final String missedCsv;
  final int missedCount;
  final String cyclesCsv;
  final int cycleCount;
  final String lastPayment;
  final double monthlyInstallment;

  /// Same text as WinForms frmDuesWhatsAppList.BuildWhatsAppMessage.
  String whatsAppMessage() {
    final buf = StringBuffer()
      ..writeln('Assalam o Alaikum,')
      ..writeln()
      ..writeln('*$customer* — Dues Reminder')
      ..writeln('Society: $society')
      ..writeln('Block/Plot: $block / $plotNo')
      ..writeln('Booking: $bookingDate')
      ..writeln('Plot Price: ${_money(price)}')
      ..writeln('Total Paid: ${_money(paid)}')
      ..writeln('*Remaining Due: ${_money(due)}*');
    if (monthlyInstallment > 0) {
      buf
        ..writeln('Is month ki installment: ${_money(monthlyInstallment)}')
        ..writeln(
          'Is month apko ${_money(monthlyInstallment)} installment deni hai.',
        );
    }
    buf
      ..writeln('Missed Months ($missedCount): $missedCsv')
      ..writeln('30-Day Cycles Missed ($cycleCount): $cyclesCsv');
    if (lastPayment.trim().isNotEmpty) {
      buf.writeln('Last Payment: $lastPayment');
    }
    buf
      ..writeln()
      ..writeln('Barah-e-karam apni dues clear kar dein. Shukriya.')
      ..writeln('Hub Builders');
    return buf.toString();
  }

  static final _moneyFmt = NumberFormat('#,##0');

  static String _money(double v) => _moneyFmt.format(v.round());
}

/// Same rules as WinForms frmDuesWhatsAppList — 30-day cycles + calendar months.
abstract final class DuesWhatsAppStore {
  static Future<List<IdName>> societies() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('societies', orderBy: 'name COLLATE NOCASE');
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<List<DuesWhatsAppRow>> load({
    int societyId = 0,
    int minMissedMonths = 1,
    DateTime? asOf,
    String keyword = '',
  }) async {
    await AppDatabase.ensureCustomerPlotsMonthlyInstallment();
    try {
      await BlockMonthlyStore.ensureSchema();
    } catch (_) {}
    final db = await AppDatabase.instance.database;
    final asOfDate = DateTime(
      (asOf ?? DateTime.now()).year,
      (asOf ?? DateTime.now()).month,
      (asOf ?? DateTime.now()).day,
    );
    final kw = keyword.trim();
    final kwLike = '%$kw%';

    final rows = await db.rawQuery(
      '''
SELECT cp.id AS customer_plot_id, c.name AS customer_name, c.mobile,
  s.name AS society_name, b.name AS block_name, p.plot_no, cp.booking_date,
  COALESCE(cp.price,0) AS plot_price,
  COALESCE(cp.monthly_installment,0) AS monthly_installment,
  COALESCE(bmi.monthly_amount,0) AS block_monthly,
  COALESCE((SELECT SUM(amount) FROM receipts r WHERE r.customer_plot_id = cp.id),0) AS total_paid,
  COALESCE((SELECT MAX(receipt_date) FROM receipts r2 WHERE r2.customer_plot_id = cp.id),'') AS last_payment_date
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN block_monthly_installments bmi ON bmi.block_id = p.block_id
WHERE cp.status = 'Active'
  AND (? = 0 OR p.society_id = ?)
  AND (? = '' OR c.name LIKE ? COLLATE NOCASE OR c.mobile LIKE ? COLLATE NOCASE
       OR CAST(p.plot_no AS TEXT) LIKE ? COLLATE NOCASE OR s.name LIKE ? COLLATE NOCASE)
ORDER BY s.name, c.name, p.plot_no''',
      [societyId, societyId, kw, kwLike, kwLike, kwLike, kwLike],
    );

    final out = <DuesWhatsAppRow>[];
    for (final r in rows) {
      final price = (r['plot_price'] as num?)?.toDouble() ?? 0;
      final paid = (r['total_paid'] as num?)?.toDouble() ?? 0;
      final due = price - paid;
      // Plot rate complete (receipt se) → reminder mein bilkul nahi
      if (due <= 0.009) continue;

      final cpId = r['customer_plot_id'] as int;
      final bookingStr = r['booking_date'] as String? ?? '';
      // WinForms ParseDateSafe: unparseable booking → fall back to asOf
      var booking = DateTime.tryParse(
        bookingStr.length >= 10 ? bookingStr.substring(0, 10) : bookingStr,
      );
      booking ??= DateTime.tryParse(bookingStr.replaceFirst(' ', 'T'));
      booking ??= asOfDate;

      final bookingDay = DateTime(booking.year, booking.month, booking.day);
      final days = asOfDate.difference(bookingDay).inDays;
      if (days < 30) continue;

      final paidRows = await db.rawQuery(
        '''
SELECT receipt_date, amount
FROM receipts
WHERE customer_plot_id = ? AND amount > 0 AND receipt_date IS NOT NULL
  AND TRIM(receipt_date) <> ''
''',
        [cpId],
      );
      // ADVANCE = receipt_date < booking+7 → ignore for installment clear only
      // (30-day cycle still starts on booking; list appear only after day 30)
      final advanceCutoff = bookingDay.add(const Duration(days: 7));
      final paidMonths = <String>{};
      final paidDates = <DateTime>[];
      var installmentPaid = 0.0;
      for (final x in paidRows) {
        final raw = (x['receipt_date'] ?? '').toString().trim();
        DateTime? d;
        if (raw.length >= 10) {
          d = DateTime.tryParse(raw.substring(0, 10));
        }
        d ??= DateTime.tryParse(raw.replaceFirst(' ', 'T'));
        if (d == null) continue;
        final day = DateTime(d.year, d.month, d.day);
        // ADVANCE window: date-only compare (< booking+7)
        if (day.isBefore(advanceCutoff)) continue;
        paidDates.add(day);
        paidMonths.add(
          '${day.year.toString().padLeft(4, '0')}-${day.month.toString().padLeft(2, '0')}',
        );
        installmentPaid += (x['amount'] as num?)?.toDouble() ?? 0;
      }

      var monthly = (r['monthly_installment'] as num?)?.toDouble() ?? 0;
      if (monthly <= 0) {
        monthly = (r['block_monthly'] as num?)?.toDouble() ?? 0;
      }
      // FIFO/cover uses post-advance paid only; Due display still uses all receipts
      final missed = _missedMonths(
        bookingDay,
        asOfDate,
        paidMonths,
        monthly,
        installmentPaid,
        paidDates.length,
      );
      final cycles = _missed30DayCycles(
        bookingDay,
        asOfDate,
        paidDates,
        missed.clearedKeys,
      );
      // Dues clear / sab months+cycles paid → hide
      if (missed.count == 0 && cycles.count == 0) continue;
      if (missed.count < minMissedMonths && cycles.count < minMissedMonths) {
        continue;
      }

      out.add(
        DuesWhatsAppRow(
          customerPlotId: cpId,
          customer: r['customer_name'] as String? ?? '',
          mobile: r['mobile'] as String? ?? '',
          society: r['society_name'] as String? ?? '',
          block: r['block_name'] as String? ?? '',
          plotNo: '${r['plot_no'] ?? ''}',
          bookingDate:
              '${bookingDay.year.toString().padLeft(4, '0')}-'
              '${bookingDay.month.toString().padLeft(2, '0')}-'
              '${bookingDay.day.toString().padLeft(2, '0')}',
          price: price,
          paid: paid,
          due: due,
          missedCsv: missed.csv,
          missedCount: missed.count,
          cyclesCsv: cycles.csv,
          cycleCount: cycles.count,
          lastPayment: r['last_payment_date'] as String? ?? '',
          monthlyInstallment: monthly,
        ),
      );
    }
    return out;
  }

  /// PK digits for wa.me (03xxxxxxxxx → 92xxxxxxxxxx).
  static String normalizePhone(String input) {
    final digits = input.replaceAll(RegExp(r'\D'), '');
    if (digits.startsWith('03') && digits.length == 11) {
      return '92${digits.substring(1)}';
    }
    return digits;
  }

  /// Receipt date us month → clear. Installment/receipt count FIFO purane pehle.
  static ({String csv, int count, Set<String> clearedKeys}) _missedMonths(
    DateTime booking,
    DateTime asOf,
    Set<String> paidMonths,
    double monthlyInstallment,
    double totalPaid,
    int receiptCount,
  ) {
    var cursor = DateTime(booking.year, booking.month + 1, 1);
    final end = DateTime(asOf.year, asOf.month, 1);
    if (cursor.isAfter(end)) {
      return (csv: '—', count: 0, clearedKeys: <String>{});
    }

    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    final due = <DateTime>[];
    while (!cursor.isAfter(end)) {
      due.add(cursor);
      cursor = DateTime(cursor.year, cursor.month + 1, 1);
    }

    var coverLeft = _coverSlots(monthlyInstallment, totalPaid, receiptCount);
    final cleared = <String>{};
    final missed = <String>[];
    for (final m in due) {
      final key =
          '${m.year.toString().padLeft(4, '0')}-${m.month.toString().padLeft(2, '0')}';
      if (paidMonths.contains(key)) {
        cleared.add(key);
        continue;
      }
      if (coverLeft > 0) {
        coverLeft--;
        cleared.add(key);
        continue;
      }
      missed.add('${months[m.month - 1]}-${m.year}');
    }
    if (missed.isEmpty) {
      return (csv: 'None', count: 0, clearedKeys: cleared);
    }
    return (csv: missed.join(', '), count: missed.length, clearedKeys: cleared);
  }

  static int _coverSlots(
    double monthlyInstallment,
    double totalPaid,
    int receiptCount,
  ) {
    if (monthlyInstallment > 0.009) {
      return (totalPaid / monthlyInstallment).floor();
    }
    // Monthly set nahi → har receipt 1 period clear
    return receiptCount < 0 ? 0 : receiptCount;
  }

  /// Cycle paid if: receipt date in range, OR overlaps a cleared calendar month.
  /// No independent FIFO on cycles (avoids double-counting totalPaid).
  static ({String csv, int count}) _missed30DayCycles(
    DateTime booking,
    DateTime asOf,
    List<DateTime> paidDates,
    Set<String> clearedMonthKeys,
  ) {
    final completed = asOf.difference(booking).inDays ~/ 30;
    if (completed < 1) return (csv: '—', count: 0);

    // WinForms: start dd-MMM (no year), endInclusive dd-MMM-yyyy
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    String fmtStart(DateTime d) =>
        '${d.day.toString().padLeft(2, '0')}-${months[d.month - 1]}';
    String fmtEnd(DateTime d) =>
        '${d.day.toString().padLeft(2, '0')}-${months[d.month - 1]}-${d.year}';

    final missed = <String>[];
    for (var i = 0; i < completed; i++) {
      final start = booking.add(Duration(days: i * 30));
      final endExclusive = booking.add(Duration(days: (i + 1) * 30));
      final endInclusive = endExclusive.subtract(const Duration(days: 1));
      final paidByDate = paidDates.any(
        (pd) => !pd.isBefore(start) && pd.isBefore(endExclusive),
      );
      if (paidByDate) continue;
      if (_cycleOverlapsClearedMonth(start, endInclusive, clearedMonthKeys)) {
        continue;
      }
      missed.add(
        'C${i + 1} (${fmtStart(start)} → ${fmtEnd(endInclusive)})',
      );
    }
    if (missed.isEmpty) return (csv: 'None', count: 0);
    return (csv: missed.join(', '), count: missed.length);
  }

  static bool _cycleOverlapsClearedMonth(
    DateTime start,
    DateTime endInclusive,
    Set<String> clearedMonthKeys,
  ) {
    if (clearedMonthKeys.isEmpty) return false;
    var cur = DateTime(start.year, start.month, 1);
    final end = DateTime(endInclusive.year, endInclusive.month, 1);
    while (!cur.isAfter(end)) {
      final key =
          '${cur.year.toString().padLeft(4, '0')}-${cur.month.toString().padLeft(2, '0')}';
      if (clearedMonthKeys.contains(key)) return true;
      cur = DateTime(cur.year, cur.month + 1, 1);
    }
    return false;
  }
}
