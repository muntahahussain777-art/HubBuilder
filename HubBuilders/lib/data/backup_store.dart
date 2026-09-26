import 'dart:io';

import '../db/app_database.dart';
import 'hub_sync.dart';

/// Same as WinForms Backup_RestoreForm — copy RealEstateBuilders.db.
abstract final class BackupStore {
  static Future<String> currentDbPath() => AppDatabase.instance.dbPath;

  /// Backup via VACUUM INTO (SQLite backup API equivalent).
  static Future<({bool ok, String message, String? path})> backupToFolder(
    String folderPath,
  ) async {
    try {
      final dir = Directory(folderPath);
      if (!await dir.exists()) {
        return (ok: false, message: 'Folder not found.', path: null);
      }
      final dest = '${dir.path}${Platform.pathSeparator}RealEstateBuilders_backup.db';
      if (await File(dest).exists()) {
        await File(dest).delete();
      }
      final db = await AppDatabase.instance.database;
      // Escape single quotes in path for SQL string literal.
      final sqlPath = dest.replaceAll("'", "''");
      await db.execute("VACUUM INTO '$sqlPath'");
      return (
        ok: true,
        message: 'Backup successfully created at:\n$dest',
        path: dest,
      );
    } catch (e) {
      // Fallback: close + File.copy
      try {
        final src = await AppDatabase.instance.dbPath;
        final dest =
            '$folderPath${Platform.pathSeparator}RealEstateBuilders_backup.db';
        await AppDatabase.instance.close();
        await File(src).copy(dest);
        await AppDatabase.instance.database; // reopen
        return (
          ok: true,
          message: 'Backup successfully created at:\n$dest',
          path: dest,
        );
      } catch (e2) {
        try {
          await AppDatabase.instance.database;
        } catch (_) {}
        return (ok: false, message: 'Backup failed: $e2', path: null);
      }
    }
  }

  /// Restore: close DB, overwrite live file, reopen + ensure schemas.
  static Future<({bool ok, String message})> restoreFromFile(
    String restoreFilePath,
  ) async {
    try {
      final src = File(restoreFilePath);
      if (!await src.exists()) {
        return (ok: false, message: 'Restore file not found.');
      }
      final destPath = await AppDatabase.instance.dbPath;
      await AppDatabase.instance.close();
      await File(restoreFilePath).copy(destPath);
      // Reopen + additive schemas (same as EnsureAdditiveSchemasAfterRestore)
      final db = await AppDatabase.instance.database;
      await AppDatabase.ensurePartnerMoneySchema(db);
      await AppDatabase.ensureExpenseCategorySchema(db);
      await AppDatabase.ensureOverpayTriggers(db);
      await AppDatabase.ensureIndexes(db);
      await HubSync.ensureLocalSchema();
      await HubSync.notifyDatabaseRestored();
      return (
        ok: true,
        message:
            'Database restored successfully.\nPlease restart the app (cold start).',
      );
    } catch (e) {
      try {
        await AppDatabase.instance.database;
      } catch (_) {}
      return (ok: false, message: 'Restore failed: $e');
    }
  }
}
