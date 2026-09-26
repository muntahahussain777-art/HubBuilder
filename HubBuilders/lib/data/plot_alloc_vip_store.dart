import '../db/app_database.dart';

class PlotAllocVipReceipt {
  const PlotAllocVipReceipt({
    required this.date,
    required this.amount,
    required this.mode,
    this.refNo = '',
    this.notes = '',
  });

  final String date;
  final double amount;
  final String mode;
  final String refNo;
  final String notes;
}

class PlotAllocVipSuggest {
  const PlotAllocVipSuggest({
    required this.customerPlotId,
    required this.customerId,
    required this.display,
    required this.name,
    required this.father,
    required this.mobile,
    required this.cnic,
    required this.society,
    required this.block,
    required this.plotNo,
    required this.plotType,
    required this.bookingDate,
    required this.dealType,
    required this.plotPrice,
    this.monthly = 0,
  });

  final int customerPlotId;
  final int customerId;
  final String display;
  final String name;
  final String father;
  final String mobile;
  final String cnic;
  final String society;
  final String block;
  final String plotNo;
  final String plotType;
  final String bookingDate;
  final String dealType;
  final double plotPrice;
  final double monthly;
}

/// Same rules as WinForms frmVipPlotAllocationWhatsApp.
abstract final class PlotAllocVipStore {
  static Future<List<PlotAllocVipSuggest>> loadActiveAllocations() async {
    await AppDatabase.ensureCustomerPlotsMonthlyInstallment();
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery('''
SELECT
  cp.id AS customer_plot_id,
  cp.customer_id AS customer_id,
  COALESCE(c.name,'') AS customer_name,
  COALESCE(c.father_name,'') AS father_name,
  COALESCE(c.mobile,'') AS mobile,
  COALESCE(c.cnic,'') AS cnic,
  COALESCE(s.name,'') AS society_name,
  COALESCE(b.name,'') AS block_name,
  CAST(p.plot_no AS TEXT) AS plot_no,
  COALESCE(pt.code,'') AS plot_type,
  COALESCE(cp.booking_date,'') AS booking_date,
  COALESCE(NULLIF(TRIM(cp.deal_type),''),'Installment') AS deal_type,
  COALESCE(cp.price,0) AS plot_price,
  COALESCE(cp.monthly_installment,0) AS monthly_installment
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN societies s ON s.id = p.society_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE cp.status = 'Active'
ORDER BY c.name COLLATE NOCASE, p.plot_no''');

    final out = <PlotAllocVipSuggest>[];
    for (final r in rows) {
      final name = (r['customer_name'] as String? ?? '').trim();
      final father = (r['father_name'] as String? ?? '').trim();
      final plotNo = '${r['plot_no'] ?? ''}'.trim();
      final block = (r['block_name'] as String? ?? '').trim();
      final society = (r['society_name'] as String? ?? '').trim();
      var booking = (r['booking_date'] as String? ?? '').trim();
      if (booking.length >= 10) booking = booking.substring(0, 10);

      var disp = name;
      if (father.isNotEmpty) disp += ' / $father';
      disp += ' | Plot $plotNo';
      if (block.isNotEmpty) disp += ' | $block';
      if (society.isNotEmpty) disp += ' | $society';

      out.add(
        PlotAllocVipSuggest(
          customerPlotId: r['customer_plot_id'] as int,
          customerId: r['customer_id'] as int,
          display: disp,
          name: name,
          father: father,
          mobile: (r['mobile'] as String? ?? '').trim(),
          cnic: (r['cnic'] as String? ?? '').trim(),
          society: society,
          block: block,
          plotNo: plotNo,
          plotType: (r['plot_type'] as String? ?? '').trim(),
          bookingDate: booking,
          dealType: (r['deal_type'] as String? ?? 'Installment').trim(),
          plotPrice: (r['plot_price'] as num?)?.toDouble() ?? 0,
          monthly: (r['monthly_installment'] as num?)?.toDouble() ?? 0,
        ),
      );
    }
    return out;
  }

  static Future<List<PlotAllocVipReceipt>> loadReceipts(int customerPlotId) async {
    if (customerPlotId <= 0) return const [];
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      '''
SELECT receipt_date, amount, IFNULL(mode,'') AS mode,
       IFNULL(ref_no,'') AS ref_no, IFNULL(notes,'') AS notes
FROM receipts
WHERE customer_plot_id = ? AND amount > 0
ORDER BY receipt_date ASC, id ASC''',
      [customerPlotId],
    );
    return rows
        .map(
          (r) => PlotAllocVipReceipt(
            date: (r['receipt_date'] as String? ?? '').trim(),
            amount: (r['amount'] as num?)?.toDouble() ?? 0,
            mode: (r['mode'] as String? ?? '').trim(),
            refNo: (r['ref_no'] as String? ?? '').trim(),
            notes: (r['notes'] as String? ?? '').trim(),
          ),
        )
        .toList();
  }

  static String money(double v) {
    final n = v.round();
    final s = n.abs().toString();
    final buf = StringBuffer();
    for (var i = 0; i < s.length; i++) {
      final fromEnd = s.length - i;
      buf.write(s[i]);
      if (fromEnd > 1 && fromEnd % 3 == 1) buf.write(',');
    }
    return n < 0 ? '-${buf.toString()}' : buf.toString();
  }

  static String buildVipMessage({
    required String customerName,
    String father = '',
    String mobile = '',
    String cnic = '',
    String society = '',
    String block = '',
    String plotNo = '',
    String plotType = '',
    String bookingDate = '',
    String dealType = '',
    double plotPrice = 0,
    double monthly = 0,
    List<PlotAllocVipReceipt> receipts = const [],
    DateTime? now,
  }) {
    final name = customerName.trim().isEmpty ? 'Janab' : customerName.trim();
    final when = now ?? DateTime.now();
    final date =
        '${when.day.toString().padLeft(2, '0')}-${_mon(when.month)}-${when.year}';
    final totalPaid = receipts.fold<double>(0, (s, r) => s + r.amount);
    var remaining = plotPrice - totalPaid;
    if (remaining < 0) remaining = 0;

    final buf = StringBuffer()
      ..writeln('Assalam o Alaikum *$name*,')
      ..writeln()
      ..writeln('*HUB BUILDERS*')
      ..writeln('Plot Allocation Confirmation')
      ..writeln('Date: $date')
      ..writeln()
      ..writeln('Mubarak ho!')
      ..writeln(
        'Aap ko Hub Builders ki taraf se *plot allocate* kar diya gaya hai.',
      )
      ..writeln(
        "Allah ta'ala aap ke liye is plot mein barkat aur sukoon ata farmaye. Aameen.",
      )
      ..writeln()
      ..writeln('*Customer Details:*')
      ..writeln('Name: $name');
    if (father.trim().isNotEmpty) {
      buf.writeln('Father / Husband: ${father.trim()}');
    }
    if (mobile.trim().isNotEmpty) buf.writeln('Mobile: ${mobile.trim()}');
    if (cnic.trim().isNotEmpty) buf.writeln('CNIC: ${cnic.trim()}');
    buf
      ..writeln()
      ..writeln('*Plot Details:*');
    if (society.trim().isNotEmpty) buf.writeln('Society: ${society.trim()}');
    if (block.trim().isNotEmpty) buf.writeln('Block: ${block.trim()}');
    if (plotNo.trim().isNotEmpty) buf.writeln('Plot No: ${plotNo.trim()}');
    if (plotType.trim().isNotEmpty) {
      buf.writeln('Plot Type / Size: ${plotType.trim()}');
    }
    if (bookingDate.trim().isNotEmpty) {
      buf.writeln('Booking Date: ${bookingDate.trim()}');
    }
    if (dealType.trim().isNotEmpty) buf.writeln('Deal Type: ${dealType.trim()}');
    buf
      ..writeln()
      ..writeln('*Account Summary:*')
      ..writeln('Allocate / Plot Amount: Rs ${money(plotPrice)}');
    if (monthly > 0) {
      buf.writeln('Monthly Installment: Rs ${money(monthly)}');
    }

    if (receipts.isNotEmpty) {
      buf
        ..writeln()
        ..writeln('*Receipt Form — Credit Payments:*');
      for (var i = 0; i < receipts.length; i++) {
        final r = receipts[i];
        final label = i == 0 ? 'Advance / 1st Receipt' : 'Receipt ${i + 1}';
        var d = r.date;
        if (d.length >= 10) d = d.substring(0, 10);
        var line = '${i + 1}. $label — $d';
        if (r.mode.isNotEmpty) line += ' | ${r.mode}';
        if (r.refNo.isNotEmpty) line += ' | Ref: ${r.refNo}';
        line += ' | Credit: Rs ${money(r.amount)}';
        buf.writeln(line);
      }
      buf.writeln('*Total Paid (Credit): Rs ${money(totalPaid)}*');
    } else {
      buf
        ..writeln('Receipts (Credit): Abhi koi receipt save nahi hui.')
        ..writeln('Total Paid (Credit): Rs 0');
    }

    buf
      ..writeln('*Remaining Due: Rs ${money(remaining)}*')
      ..writeln()
      ..writeln('Aap ka plot Hub Builders ke paas secure hai.')
      ..writeln(
        'Barah-e-karam apni installment / dues time par clear karte rahein.',
      )
      ..writeln(
        'Kisi bhi sawal ya madad ke liye Hub Builders office se rabta karein.',
      )
      ..writeln()
      ..writeln('Shukriya — aap ka bharosa humari zimmedari hai.')
      ..writeln('*Team Hub Builders*');
    return buf.toString().trimRight();
  }

  static String _mon(int m) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    return months[(m - 1).clamp(0, 11)];
  }
}
