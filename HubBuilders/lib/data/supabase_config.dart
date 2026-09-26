import 'dart:convert';
import 'dart:io';

import 'package:flutter/services.dart';
import 'package:path_provider/path_provider.dart';

/// Same keys as WinForms supabase_config.json.
abstract final class SupabaseConfig {
  // Built-in HubBuilders project — asset/override se na tootay.
  static const String _fallbackUrl =
      'https://fkvinhehqajjnxnducgc.supabase.co';
  static const String _fallbackKey =
      'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZrdmluaGVocWFqam54bmR1Y2djIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODY3MzE0MjYsImV4cCI6MjEwMjMwNzQyNn0.kLtyV7LeNQsPQOu344O5wy5g5b7LsNQHsxulclJDTXo';

  static String url = _fallbackUrl;
  static String anonKey = _fallbackKey;

  static bool get isConfigured {
    final u = url.trim();
    final k = anonKey.trim();
    return u.isNotEmpty &&
        !u.contains('YOUR-PROJECT') &&
        k.isNotEmpty &&
        !k.contains('YOUR-ANON-KEY');
  }

  static void _useFallback() {
    url = _fallbackUrl;
    anonKey = _fallbackKey;
  }

  static Future<void> load() async {
    _useFallback();
    try {
      final asset = await rootBundle.loadString('assets/supabase_config.json');
      _applyJson(asset);
    } catch (_) {}
    if (!isConfigured) _useFallback();

    try {
      final envUrl = Platform.environment['HUBBUILDERS_SUPABASE_URL'];
      final envKey = Platform.environment['HUBBUILDERS_SUPABASE_ANON_KEY'];
      if (envUrl != null &&
          envKey != null &&
          envUrl.trim().isNotEmpty &&
          envKey.trim().isNotEmpty) {
        url = envUrl.trim();
        anonKey = envKey.trim();
      }
    } catch (_) {}
    if (!isConfigured) _useFallback();

    try {
      final docs = await getApplicationDocumentsDirectory();
      final override = File('${docs.path}/supabase_config.json');
      if (await override.exists()) {
        await override.delete();
      }
    } catch (_) {}

    if (!isConfigured) _useFallback();
  }

  static void _applyJson(String raw) {
    final map = jsonDecode(raw);
    if (map is! Map) return;
    final nextUrl = (map['url'] ?? '').toString().trim();
    final nextKey =
        (map['anonKey'] ?? map['anon_key'] ?? '').toString().trim();
    if (nextUrl.contains('YOUR-PROJECT') ||
        nextKey.contains('YOUR-ANON-KEY') ||
        nextUrl.isEmpty ||
        nextKey.isEmpty) {
      return;
    }
    // Never follow leftover old-project override.
    if (nextUrl.contains('zdlsxpouflytzlqdfqfb')) return;
    url = nextUrl;
    anonKey = nextKey;
  }
}
