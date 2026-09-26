import '../db/app_database.dart';

/// Active allocation row for VIP name-search (same as WinForms Lasbela / Application Form).
class VipAllocSuggest {
  const VipAllocSuggest({
    required this.customerPlotId,
    required this.display,
    required this.name,
    required this.father,
    required this.mobile,
    required this.cnic,
    required this.plotNo,
    required this.size,
    required this.block,
    required this.category,
    required this.price,
    required this.remaining,
  });

  final int customerPlotId;
  final String display;
  final String name;
  final String father;
  final String mobile;
  final String cnic;
  final String plotNo;
  final String size;
  final String block;
  final String category;
  final double price;
  final double remaining;

  @override
  String toString() => display;
}

abstract final class VipAllocSuggestStore {
  static Future<List<VipAllocSuggest>> loadActive() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery('''
SELECT
  cp.id AS customer_plot_id,
  COALESCE(c.name,'') AS customer_name,
  COALESCE(c.father_name,'') AS father_name,
  COALESCE(c.mobile,'') AS mobile,
  COALESCE(c.cnic,'') AS cnic,
  CAST(p.plot_no AS TEXT) AS plot_no,
  COALESCE(pt.code,'') AS plot_size,
  COALESCE(b.name,'') AS block_name,
  COALESCE(NULLIF(TRIM(cp.deal_type),''),'Installment') AS deal_type,
  COALESCE(cp.price,0) AS plot_price,
  COALESCE((SELECT SUM(amount) FROM receipts r WHERE r.customer_plot_id = cp.id),0) AS total_paid
FROM customer_plots cp
JOIN customers c ON c.id = cp.customer_id
JOIN plots p ON p.id = cp.plot_id
LEFT JOIN blocks b ON b.id = p.block_id
LEFT JOIN plot_types pt ON pt.id = p.plot_type_id
WHERE cp.status = 'Active'
ORDER BY c.name COLLATE NOCASE, p.plot_no
''');

    final out = <VipAllocSuggest>[];
    for (final r in rows) {
      final name = (r['customer_name'] as String? ?? '').trim();
      final father = (r['father_name'] as String? ?? '').trim();
      final mobile = (r['mobile'] as String? ?? '').trim();
      final cnic = (r['cnic'] as String? ?? '').trim();
      final plot = (r['plot_no'] as String? ?? '').trim();
      final size = (r['plot_size'] as String? ?? '').trim();
      final block = (r['block_name'] as String? ?? '').trim();
      final cat = (r['deal_type'] as String? ?? 'Installment').trim();
      final price = (r['plot_price'] as num?)?.toDouble() ?? 0;
      final paid = (r['total_paid'] as num?)?.toDouble() ?? 0;
      var rem = (price - paid);
      if (rem < 0) rem = 0;
      rem = double.parse(rem.toStringAsFixed(2));

      var disp = name;
      if (father.isNotEmpty) disp += ' / $father';
      disp += ' | Plot $plot';
      if (block.isNotEmpty) disp += ' | $block';
      if (size.isNotEmpty) disp += ' | $size';

      out.add(
        VipAllocSuggest(
          customerPlotId: r['customer_plot_id'] as int,
          display: disp,
          name: name,
          father: father,
          mobile: mobile,
          cnic: cnic,
          plotNo: plot,
          size: size,
          block: block,
          category: cat,
          price: price,
          remaining: rem,
        ),
      );
    }
    return out;
  }

  static List<VipAllocSuggest> filter(
    List<VipAllocSuggest> cache,
    String query, {
    int limit = 80,
  }) {
    final q = query.trim().toLowerCase();
    final out = <VipAllocSuggest>[];
    for (final a in cache) {
      if (q.isEmpty ||
          a.name.toLowerCase().contains(q) ||
          a.father.toLowerCase().contains(q) ||
          a.plotNo.toLowerCase().contains(q) ||
          a.mobile.toLowerCase().contains(q) ||
          a.display.toLowerCase().contains(q)) {
        out.add(a);
        if (out.length >= limit) break;
      }
    }
    return out;
  }

  static String formatMoney(double v) {
    final n = v.round().toString();
    return n.replaceAllMapped(
      RegExp(r'(\d)(?=(\d{3})+(?!\d))'),
      (m) => '${m[1]},',
    );
  }
}
