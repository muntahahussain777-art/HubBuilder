import 'dart:async';
import 'dart:io';

import 'package:path_provider/path_provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../db/app_database.dart';

/// Auto backup: HubBuilders/{yyyy-MM-dd}/ on phone storage.
abstract final class AutoBackup {
  static const _kInterval = 'auto_backup_interval'; // entry | 3h | 1d
  static const _kLast = 'auto_backup_last_utc';
  static Timer? _timer;
  static bool _busy = false;

  static Future<void> start() async {
    _timer ??= Timer.periodic(const Duration(minutes: 1), (_) {
      unawaited(tick());
    });
    unawaited(tick());
  }

  static Future<String> interval() async {
    final p = await SharedPreferences.getInstance();
    final v = p.getString(_kInterval) ?? '1d';
    if (v == 'entry' || v == '3h' || v == '1d') return v;
    return '1d';
  }

  static Future<void> setInterval(String v) async {
    final p = await SharedPreferences.getInstance();
    await p.setString(_kInterval, v);
  }

  static Future<Directory> todayDir() async {
    Directory base;
    try {
      base = await getExternalStorageDirectory() ??
          await getApplicationDocumentsDirectory();
    } catch (_) {
      base = await getApplicationDocumentsDirectory();
    }
    final date =
        '${DateTime.now().year.toString().padLeft(4, '0')}-${DateTime.now().month.toString().padLeft(2, '0')}-${DateTime.now().day.toString().padLeft(2, '0')}';
    final dir = Directory('${base.path}${Platform.pathSeparator}HubBuilders${Platform.pathSeparator}$date');
    if (!await dir.exists()) await dir.create(recursive: true);
    return dir;
  }

  /// After each entry: date/HHmmss folder. 3h/1d: date folder.
  static Future<Directory> backupDir() async {
    final today = await todayDir();
    if (await interval() != 'entry') return today;
    final n = DateTime.now();
    final stamp =
        '${n.hour.toString().padLeft(2, '0')}${n.minute.toString().padLeft(2, '0')}${n.second.toString().padLeft(2, '0')}';
    final dir = Directory('${today.path}${Platform.pathSeparator}$stamp');
    if (!await dir.exists()) await dir.create(recursive: true);
    return dir;
  }

  static Future<void> onEntry() async {
    if (await interval() != 'entry') return;
    await runSilent();
  }

  static Future<void> tick() async {
    final iv = await interval();
    if (iv == 'entry') return;
    final p = await SharedPreferences.getInstance();
    final lastRaw = p.getString(_kLast);
    DateTime last = DateTime.fromMillisecondsSinceEpoch(0, isUtc: true);
    if (lastRaw != null) last = DateTime.tryParse(lastRaw)?.toUtc() ?? last;
    final hours = iv == '3h' ? 3 : 24;
    if (DateTime.now().toUtc().difference(last).inHours < hours) return;
    await runSilent();
  }

  static Future<({bool ok, String message, String? path})> runSilent() async {
    if (_busy) return (ok: false, message: 'Busy', path: null);
    _busy = true;
    try {
      final dir = await backupDir();
      final dest =
          '${dir.path}${Platform.pathSeparator}RealEstateBuilders_backup.db';
      if (await File(dest).exists()) {
        await File(dest).delete();
      }
      final db = await AppDatabase.instance.database;
      final sqlPath = dest.replaceAll("'", "''");
      try {
        await db.execute("VACUUM INTO '$sqlPath'");
      } catch (_) {
        final src = await AppDatabase.instance.dbPath;
        await AppDatabase.instance.close();
        await File(src).copy(dest);
        await AppDatabase.instance.database;
      }
      final p = await SharedPreferences.getInstance();
      await p.setString(_kLast, DateTime.now().toUtc().toIso8601String());
      return (
        ok: true,
        message: 'Backup saved:\n$dest',
        path: dest,
      );
    } catch (e) {
      try {
        await AppDatabase.instance.database;
      } catch (_) {}
      return (ok: false, message: 'Backup failed: $e', path: null);
    } finally {
      _busy = false;
    }
  }
}
