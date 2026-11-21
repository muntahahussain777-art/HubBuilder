/// Additive receipt-form option token. WhatsApp reminder reads it.
/// Does not change receipt totals or sync payload shape.
abstract final class InstallmentCover {
  static final _token = RegExp(r'\[INST-COVER:([0-9,\-]+)\]', caseSensitive: false);

  static bool hasToken(String? notes) =>
      notes != null && notes.toUpperCase().contains('[INST-COVER:');

  static String stripToken(String? notes) {
    if (notes == null || notes.trim().isEmpty) return '';
    return notes.replaceAll(_token, '').trim();
  }

  static Set<String> parseKeys(String? notes) {
    final out = <String>{};
    if (notes == null || notes.isEmpty) return out;
    final m = _token.firstMatch(notes);
    if (m == null) return out;
    for (final part in m.group(1)!.split(',')) {
      final k = part.trim();
      if (k.length == 7 && k[4] == '-') out.add(k);
    }
    return out;
  }

  static String applyToken(String? notes, List<String> keys) {
    final base = stripToken(notes);
    if (keys.isEmpty) return base;
    final token = '[INST-COVER:${keys.join(',')}]';
    if (base.isEmpty) return token;
    return '$base $token';
  }

  static String monthKey(DateTime d) =>
      '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}';

  /// monthly>0 → floor(amount/monthly) unpaid previous/current only (never upcoming).
  /// Extra slots beyond current dues are unused.
  /// monthly==0 → all unpaid months through asOf.
  static List<String> computeCoverKeys({
    required DateTime booking,
    required DateTime asOf,
    required double amount,
    required double monthly,
    Set<String> alreadyPaid = const {},
  }) {
    final keys = <String>[];
    var start = DateTime(booking.year, booking.month + 1, 1);
    final asOfMonth = DateTime(asOf.year, asOf.month, 1);
    if (start.isAfter(asOfMonth)) return keys;

    late final int slots;
    if (monthly > 0.009) {
      slots = (amount / monthly).floor();
    } else {
      slots = 240;
    }
    final useSlots = monthly > 0.009 && slots < 1 ? 1 : slots;
    // Stop at current month — upcoming never covered (client rule).
    var cursor = start;
    while (!cursor.isAfter(asOfMonth) && keys.length < useSlots) {
      final key = monthKey(cursor);
      if (!alreadyPaid.contains(key)) keys.add(key);
      cursor = DateTime(cursor.year, cursor.month + 1, 1);
    }
    return keys;
  }
}
