import 'package:url_launcher/url_launcher.dart';

import 'dues_whatsapp_store.dart';

/// Same as WinForms OpenWhatsAppWithPrefill — wa.me / whatsapp:// send.
abstract final class WhatsAppOpen {
  static String normalizePhone(String input) =>
      DuesWhatsAppStore.normalizePhone(input);

  static Future<bool> send({
    required String mobile,
    required String message,
  }) async {
    final digits = normalizePhone(mobile);
    if (digits.isEmpty) return false;
    final encoded = Uri.encodeComponent(message);
    final app = Uri.parse('whatsapp://send?phone=$digits&text=$encoded');
    try {
      if (await launchUrl(app, mode: LaunchMode.externalApplication)) {
        return true;
      }
    } catch (_) {}
    final web = Uri.parse('https://wa.me/$digits?text=$encoded');
    return launchUrl(web, mode: LaunchMode.externalApplication);
  }
}
