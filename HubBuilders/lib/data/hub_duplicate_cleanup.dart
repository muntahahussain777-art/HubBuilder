import 'package:sqflite/sqflite.dart';

import '../db/app_database.dart';

/// Sync ne same name do dafa insert kar diye. UI nahi chhedta — extra rows merge.
class HubDuplicateCleanup {
  static Future<void> run([Database? existing]) async {
    final db = existing ?? await AppDatabase.instance.database;
    try {
      await db.execute('PRAGMA foreign_keys=OFF');
      await AppDatabase.suspendSyncTriggers(db);
      for (var i = 0; i < 3; i++) {
        await _mergeNamed(
          db,
          'societies',
          'id',
          'lower(trim(ifnull(name,\'\')))',
          'lower(trim(ifnull(name,\'\')))=?',
        );
        await _mergeNamed(
          db,
          'blocks',
          'id',
          'ifnull(society_id,0), lower(trim(ifnull(name,\'\')))',
          'ifnull(society_id,0)=? AND lower(trim(ifnull(name,\'\')))=?',
        );
        await _mergeNamed(
          db,
          'plot_types',
          'id',
          'ifnull(society_id,0), lower(trim(ifnull(code,\'\')))',
          'ifnull(society_id,0)=? AND lower(trim(ifnull(code,\'\')))=?',
        );
        await _mergeNamed(
          db,
          'plans',
          'id',
          'lower(trim(ifnull(name,\'\')))',
          'lower(trim(ifnull(name,\'\')))=?',
        );
        await _mergeNamed(
          db,
          'customers',
          'id',
          'lower(trim(ifnull(name,\'\'))), ifnull(mobile,\'\'), ifnull(cnic,\'\')',
          'lower(trim(ifnull(name,\'\')))=? AND ifnull(mobile,\'\')=? AND ifnull(cnic,\'\')=?',
        );
        await _mergeNamed(
          db,
          'brokers',
          'id',
          'lower(trim(ifnull(name,\'\'))), ifnull(phone,\'\')',
          'lower(trim(ifnull(name,\'\')))=? AND ifnull(phone,\'\')=?',
        );
        await _mergeNamed(
          db,
          'Person',
          'person_id',
          'lower(trim(ifnull(name,\'\'))), ifnull(is_owner,0), ifnull(is_partner,0)',
          'lower(trim(ifnull(name,\'\')))=? AND ifnull(is_owner,0)=? AND ifnull(is_partner,0)=?',
        );
        await _mergeNamed(
          db,
          'plots',
          'id',
          'ifnull(society_id,0), ifnull(plot_no,\'\')',
          'ifnull(society_id,0)=? AND ifnull(plot_no,\'\')=?',
        );
        // customer_plots: NEVER content-merge — cancel→reallocate / do devices
        // receipts: NEVER content-merge — same day+amount alag entries (Partner Cash pattern)
        // partner_money: NEVER auto-merge/delete — Partner Cash Book max-safe
        await _mergeNamed(
          db,
          'ledger',
          'id',
          'ifnull(customer_plot_id,0), ifnull(txn_type,\'\'), substr(ifnull(txn_date,\'\'),1,10), ifnull(debit,0), ifnull(credit,0)',
          'ifnull(customer_plot_id,0)=? AND ifnull(txn_type,\'\')=? AND substr(ifnull(txn_date,\'\'),1,10)=? AND ifnull(debit,0)=? AND ifnull(credit,0)=?',
        );
        await _mergeNamed(
          db,
          'Deal',
          'deal_id',
          'ifnull(society_id,0), ifnull(amount,0), substr(ifnull(deal_date,\'\'),1,10)',
          'ifnull(society_id,0)=? AND ifnull(amount,0)=? AND substr(ifnull(deal_date,\'\'),1,10)=?',
        );
        await _mergeNamed(
          db,
          'Credit',
          'credit_id',
          'ifnull(amount,0), substr(ifnull(pay_date,\'\'),1,10), ifnull(note,\'\')',
          'ifnull(amount,0)=? AND substr(ifnull(pay_date,\'\'),1,10)=? AND ifnull(note,\'\')=?',
        );
        await _mergeNamed(
          db,
          'customer_plot_partners',
          'id',
          'ifnull(customer_plot_id,0), ifnull(partner_id,0)',
          'ifnull(customer_plot_id,0)=? AND ifnull(partner_id,0)=?',
        );
        await _mergeNamed(
          db,
          'Expensetbl',
          'sid',
          'lower(trim(ifnull(Name,\'\'))), ifnull(Amount,0), substr(ifnull(EDate,\'\'),1,10), ifnull(Category,\'\')',
          'lower(trim(ifnull(Name,\'\')))=? AND ifnull(Amount,0)=? AND substr(ifnull(EDate,\'\'),1,10)=? AND ifnull(Category,\'\')=?',
        );
      }
    } catch (_) {
    } finally {
      try {
        await AppDatabase.restoreSyncTriggers(db);
      } catch (_) {}
      try {
        await db.execute('PRAGMA foreign_keys=ON');
      } catch (_) {}
    }
  }

  static Future<bool> _tableExists(Database db, String table) async {
    final rows = await db.rawQuery(
      "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
      [table],
    );
    return rows.isNotEmpty;
  }

  static Future<void> _mergeNamed(
    Database db,
    String table,
    String pk,
    String groupCols,
    String whereSql,
  ) async {
    if (!await _tableExists(db, table)) return;
    List<Map<String, Object?>> groups;
    try {
      groups = await db.rawQuery(
        'SELECT $groupCols FROM "$table" GROUP BY $groupCols HAVING COUNT(*)>1',
      );
    } catch (_) {
      return;
    }
    for (final g in groups) {
      final keys = g.values.toList();
      if (keys.isEmpty) continue;
      List<Map<String, Object?>> idRows;
      try {
        idRows = await db.rawQuery(
          'SELECT "$pk" AS id FROM "$table" WHERE $whereSql '
          "ORDER BY CASE WHEN ifnull(trim(sync_id),'')='' THEN 1 ELSE 0 END, \"$pk\" ASC",
          keys,
        );
      } catch (_) {
        continue;
      }
      if (idRows.length < 2) continue;
      final win = _asInt(idRows.first['id']);
      if (win == null) continue;
      for (var i = 1; i < idRows.length; i++) {
        final lose = _asInt(idRows[i]['id']);
        if (lose == null) continue;
        await _mergePair(db, table, pk, win, lose);
      }
    }
  }

  static int? _asInt(Object? v) {
    if (v == null) return null;
    if (v is int) return v;
    return int.tryParse(v.toString());
  }

  static Future<void> _mergePair(
    Database db,
    String table,
    String pk,
    int winner,
    int loser,
  ) async {
    if (winner == loser) return;
    await _saveLoserAlias(db, table, pk, winner, loser);
    await _copySync(db, table, pk, winner, loser);
    final t = table.toLowerCase();
    if (t == 'societies') {
      await _remapSimple(db, 'blocks', 'id', 'society_id', loser, winner);
      await _remapSimple(db, 'plot_types', 'id', 'society_id', loser, winner);
      await _remapSimple(db, 'plots', 'id', 'society_id', loser, winner);
      await _remapSimple(db, 'Deal', 'deal_id', 'society_id', loser, winner);
      await _remapSimple(db, 'partner_money', 'id', 'society_id', loser, winner);
    } else if (t == 'blocks') {
      await _remapSimple(db, 'plots', 'id', 'block_id', loser, winner);
    } else if (t == 'plot_types') {
      await _remapSimple(db, 'plots', 'id', 'plot_type_id', loser, winner);
    } else if (t == 'plots') {
      await _remapSimple(db, 'customer_plots', 'id', 'plot_id', loser, winner);
    } else if (t == 'customers') {
      await _remapSimple(db, 'customer_plots', 'id', 'customer_id', loser, winner);
    } else if (t == 'brokers') {
      await _remapSimple(db, 'customer_plots', 'id', 'broker_id', loser, winner);
    } else if (t == 'plans') {
      await _remapSimple(db, 'customer_plots', 'id', 'plan_id', loser, winner);
    } else if (t == 'person') {
      await _remapSimple(db, 'Deal', 'deal_id', 'owner_id', loser, winner);
      await _remapSimple(db, 'Credit', 'credit_id', 'partner_id', loser, winner);
      await _remapSimple(
        db,
        'customer_plot_partners',
        'id',
        'partner_id',
        loser,
        winner,
      );
      await _remapSimple(db, 'partner_money', 'id', 'partner_id', loser, winner);
    } else if (t == 'customer_plots') {
      await _remapSimple(db, 'receipts', 'id', 'customer_plot_id', loser, winner);
      await _remapSimple(db, 'ledger', 'id', 'customer_plot_id', loser, winner);
      await _remapSimple(
        db,
        'customer_plot_partners',
        'id',
        'customer_plot_id',
        loser,
        winner,
      );
      await _remapSimple(
        db,
        'partner_money',
        'id',
        'customer_plot_id',
        loser,
        winner,
      );
    } else if (t == 'ledger') {
      await _remapSimple(db, 'receipts', 'id', 'ledger_id', loser, winner);
    } else if (t == 'receipts') {
      await _remapSimple(db, 'partner_money', 'id', 'receipt_id', loser, winner);
    } else if (t == 'deal') {
      await _remapSimple(db, 'Credit', 'credit_id', 'deal_id', loser, winner);
    }
    try {
      await db.delete(table, where: '"$pk"=?', whereArgs: [loser]);
    } catch (_) {}
  }

  static Future<void> _saveLoserAlias(
    Database db,
    String table,
    String pk,
    int winner,
    int loser,
  ) async {
    try {
      final rows = await db.query(
        table,
        columns: ['sync_id'],
        where: '"$pk" = ?',
        whereArgs: [loser],
        limit: 1,
      );
      if (rows.isEmpty) return;
      final sid = (rows.first['sync_id'] ?? '').toString().trim();
      if (sid.isEmpty) return;
      await db.insert(
        'sync_alias',
        {
          'sync_id': sid,
          'table_name': table,
          'local_pk': winner,
        },
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    } catch (_) {}
  }

  static Future<void> _copySync(
    Database db,
    String table,
    String pk,
    int winner,
    int loser,
  ) async {
    try {
      await db.rawUpdate(
        'UPDATE "$table" SET sync_id=(SELECT sync_id FROM "$table" WHERE "$pk"=?) '
        'WHERE "$pk"=? AND ifnull(trim(sync_id),\'\')=\'\' '
        'AND ifnull((SELECT trim(ifnull(sync_id,\'\')) FROM "$table" WHERE "$pk"=?),\'\')<>\'\'',
        [loser, winner, loser],
      );
    } catch (_) {}
  }

  static Future<void> _remapSimple(
    Database db,
    String child,
    String childPk,
    String col,
    int from,
    int to,
  ) async {
    if (!await _tableExists(db, child)) return;
    try {
      await db.rawUpdate(
        'UPDATE "$child" SET "$col"=? WHERE "$col"=?',
        [to, from],
      );
      return;
    } catch (_) {
      try {
        await db.rawDelete(
          'DELETE FROM "$child" WHERE "$col"=? AND EXISTS (SELECT 1 FROM "$child" x WHERE x."$col"=?)',
          [from, to],
        );
        await db.rawUpdate(
          'UPDATE "$child" SET "$col"=? WHERE "$col"=?',
          [to, from],
        );
        return;
      } catch (_) {}
    }
    List<Map<String, Object?>> leftover;
    try {
      leftover = await db.rawQuery(
        'SELECT "$childPk" AS id FROM "$child" WHERE "$col"=?',
        [from],
      );
    } catch (_) {
      return;
    }
    for (final row in leftover) {
      final lid = _asInt(row['id']);
      if (lid == null) continue;
      try {
        await db.rawUpdate(
          'UPDATE "$child" SET "$col"=? WHERE "$childPk"=?',
          [to, lid],
        );
      } catch (_) {
        // Unique clash: merge this child into the winner's matching row next pass.
      }
    }
  }
}
