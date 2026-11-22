import 'dart:async';
import 'dart:convert';
import 'dart:math' show Random, min;

import 'package:crypto/crypto.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:sqflite/sqflite.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

import '../db/app_database.dart';
import 'block_monthly_store.dart';
import 'hub_duplicate_cleanup.dart';
import 'staff_audit.dart';
import 'supabase_config.dart';
import 'vendor_store.dart';

/// Incremental sync — sirf naya/changed. Duplicate: Yes/No.
abstract final class HubSync {
  static final navKey = GlobalKey<NavigatorState>();

  static const _tables = <String>[
    'societies',
    'customers',
    'brokers',
    'plans',
    'users',
    'Person',
    'blocks',
    'plot_types',
    'plots',
    'Deal',
    'customer_plots',
    'ledger',
    'receipts',
    'Credit',
    'Expensetbl',
    'customer_plot_partners',
    'partner_money',
    'staff_entry_log',
    'staff_role_permissions',
    'block_monthly_installments',
  ];

  static const _fks = <String, Map<String, String>>{
    'blocks': {'society_id': 'societies'},
    'plot_types': {'society_id': 'societies'},
    'plots': {
      'society_id': 'societies',
      'block_id': 'blocks',
      'plot_type_id': 'plot_types',
    },
    'customer_plots': {
      'customer_id': 'customers',
      'plot_id': 'plots',
      'plan_id': 'plans',
      'broker_id': 'brokers',
    },
    'receipts': {
      'customer_plot_id': 'customer_plots',
      'ledger_id': 'ledger',
    },
    'ledger': {'customer_plot_id': 'customer_plots'},
    'customer_plot_partners': {
      'customer_plot_id': 'customer_plots',
      'partner_id': 'Person',
    },
    'partner_money': {
      'partner_id': 'Person',
      'customer_plot_id': 'customer_plots',
      'receipt_id': 'receipts',
      'society_id': 'societies',
    },
    'Deal': {'society_id': 'societies', 'owner_id': 'Person'},
    'Credit': {'deal_id': 'Deal', 'partner_id': 'Person'},
    'block_monthly_installments': {'block_id': 'blocks'},
  };

  /// Tests only — SoftMark / tick / Supabase init skip (local txn + pending queue still run).
  @visibleForTesting
  static bool suppressCloudIo = false;

  static Timer? _timer;
  static Future<void>? _inFlight;
  static bool _tickAgain = false;
  static bool _dirty = false;
  static DateTime _lastHashPush = DateTime.fromMillisecondsSinceEpoch(0);
  static DateTime _lastFullPrune = DateTime.fromMillisecondsSinceEpoch(0);
  static DateTime _lastSafetyPush = DateTime.fromMillisecondsSinceEpoch(0);
  static DateTime _lastUserMirror = DateTime.fromMillisecondsSinceEpoch(0);
  static final revision = ValueNotifier<int>(0);
  static final notice = ValueNotifier<String?>(null);
  /// Customer sync latency stages (ms) — local save / cloud ack / remote apply / UI refresh.
  static final syncTiming = ValueNotifier<Map<String, int>>({});
  static final snackKey = GlobalKey<ScaffoldMessengerState>();
  static DateTime? _custLocalSaveAt;
  static String? _custTimingSyncId;
  static final _pullNotes = <String>[];
  static DateTime _lastCatchUp = DateTime.fromMillisecondsSinceEpoch(0);
  static DateTime _lastAllocCatchUp = DateTime.fromMillisecondsSinceEpoch(0);
  static DateTime _lastForceRecoverBg = DateTime.fromMillisecondsSinceEpoch(0);
  static DateTime _lastFullIncremental = DateTime.fromMillisecondsSinceEpoch(0);
  static DateTime _lastDeepHeal = DateTime.fromMillisecondsSinceEpoch(0);
  static DateTime _lastPrune = DateTime.fromMillisecondsSinceEpoch(0);
  static final Set<String> _dirtyTables = <String>{};
  /// Rare deep heal — saari business forms.
  static const _hotTables = <String>[
    'societies',
    'blocks',
    'plot_types',
    'plots',
    'plans',
    'customers',
    'brokers',
    'users',
    'customer_plots',
    'ledger',
    'receipts',
    'Expensetbl',
    'Person',
    'Deal',
    'Credit',
    'customer_plot_partners',
    'partner_money',
    'block_monthly_installments',
  ];
  /// Har tick live forms (~sub-second).
  /// FK order — parents pehle (allocate/receipt pending_apply miss kam).
  static const _liveTables = <String>[
    'customers',
    'plots',
    'customer_plots',
    'ledger',
    'receipts',
    'partner_money',
    'customer_plot_partners',
  ];

  /// SoftMark kabhi mat — Rs 34,324 partner_money (held).
  static const _softMarkBlockedSyncIds = <String>{
    '1e5ee27ff6fc2ceec491a5406b665be6',
  };

  static void _bump() {
    revision.value++;
  }

  static void showNotice(String msg) {
    // WinForms jaisa — background/local sync pe toast nahi.
  }

  static String prettyModule(String module) {
    switch (module.toLowerCase()) {
      case 'customers':
        return 'Customer';
      case 'societies':
        return 'Society';
      case 'blocks':
        return 'Block';
      case 'plots':
        return 'Plot';
      case 'plot_types':
        return 'Plot Type';
      case 'receipts':
        return 'Receipt';
      case 'expensetbl':
        return 'Expense';
      case 'brokers':
        return 'Broker';
      case 'person':
        return 'Person';
      case 'deal':
        return 'Deal';
      case 'credit':
        return 'Credit';
      case 'partner_money':
        return 'Partner Cash';
      case 'customer_plots':
        return 'Allocation';
      default:
        return module;
    }
  }

  static void _noteRemote(Map<String, dynamic> row, String verb, String table) {
    // Pull toast disabled — WinForms NotePull no-op.
  }

  static String tablePk(String table) => _pk(table);

  static Future<void> saveSyncAlias(
    DatabaseExecutor db,
    String table,
    String syncId,
    int localPk,
  ) async {
    final sid = syncId.trim();
    if (sid.isEmpty || localPk <= 0 || table.trim().isEmpty) return;
    try {
      await db.insert(
        'sync_alias',
        {
          'sync_id': sid,
          'table_name': table,
          'local_pk': localPk,
        },
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    } catch (_) {}
  }

  /// Row + sync_alias — delete se pehle collect karein.
  /// [executor] diya ho to usi SQLite txn/connection par resolve.
  /// Lookup errors caller tak pohanchte hain (swallow nahi).
  /// Zero aliases on successful query = valid.
  static Future<List<String>> collectCloudDeleteIds(
    String table,
    int localPk, {
    DatabaseExecutor? executor,
  }) async {
    final ids = <String>{};
    if (localPk <= 0 || table.trim().isEmpty) return [];
    final db = executor ?? await AppDatabase.instance.database;
    final pk = _pk(table);
    final rows = await db.rawQuery(
      'SELECT sync_id FROM "$table" WHERE "$pk" = ? LIMIT 1',
      [localPk],
    );
    if (rows.isNotEmpty) {
      final s = (rows.first['sync_id'] ?? '').toString().trim();
      if (s.isNotEmpty) ids.add(s);
    }
    // Alias lookup must succeed; empty result is OK
    final aliases = await db.query(
      'sync_alias',
      columns: ['sync_id'],
      where: 'lower(table_name) = lower(?) AND local_pk = ?',
      whereArgs: [table, localPk],
    );
    for (final r in aliases) {
      final s = (r['sync_id'] ?? '').toString().trim();
      if (s.isNotEmpty) ids.add(s);
    }
    return ids.toList();
  }

  static Future<void> tombstoneLocalRow(
    String table,
    int localPk, [
    List<String>? preCollected,
  ]) async {
    if (localPk <= 0 || table.trim().isEmpty) return;
    final ids = <String>{};
    if (preCollected != null) {
      for (final s in preCollected) {
        if (s.trim().isNotEmpty) ids.add(s.trim());
      }
    }
    ids.addAll(await collectCloudDeleteIds(table, localPk));
    for (final sid in ids) {
      // Turant hard delete — queue sirf fail par (_pushDeletedOne andar queue karta hai).
      await _pushDeletedOne(table, sid);
    }
    notifyLocalWrite();
    try {
      final db = await AppDatabase.instance.database;
      await db.delete(
        'sync_alias',
        where: 'lower(table_name) = lower(?) AND local_pk = ?',
        whereArgs: [table, localPk],
      );
    } catch (_) {}
  }

  /// Local SQLite table → Supabase table (har form alag).
  static String _cloudTable(String local) {
    switch (local) {
      case 'Expensetbl':
        return 'expensetbl';
      case 'Person':
        return 'person';
      case 'Deal':
        return 'deal';
      case 'Credit':
        return 'credit';
      default:
        return local.toLowerCase();
    }
  }

  static String _fkCloudCol(String localFk) =>
      '${localFk.substring(0, localFk.length - 3)}_sync_id';

  static String _localColToCloud(String table, String col) {
    if (table == 'users') {
      if (col == 'uName') return 'uname';
      if (col == 'Role') return 'role';
    }
    if (table == 'Expensetbl') {
      switch (col) {
        case 'Name':
          return 'name';
        case 'Amount':
          return 'amount';
        case 'Category':
          return 'category';
        case 'Note':
          return 'note';
        case 'EDate':
          return 'edate';
      }
    }
    return col.toLowerCase();
  }

  static String _cloudColToLocal(String table, String col) {
    if (table == 'users') {
      if (col == 'uname') return 'uName';
      if (col == 'role') return 'Role';
    }
    if (table == 'Expensetbl') {
      switch (col) {
        case 'name':
          return 'Name';
        case 'amount':
          return 'Amount';
        case 'category':
          return 'Category';
        case 'note':
          return 'Note';
        case 'edate':
          return 'EDate';
      }
    }
    return col;
  }

  static Map<String, Object?> _cloudToLocalPayload(
    String localTable,
    Map<String, dynamic> cloud,
  ) {
    final p = <String, Object?>{};
    for (final e in cloud.entries) {
      final k = e.key.toString();
      if (k == 'updated_at' || k == 'is_deleted' || k == 'dup_key') {
        continue;
      }
      if (k == 'staff_name' && localTable != 'staff_entry_log') {
        continue;
      }
      if (k.endsWith('_sync_id') && k != 'sync_id') {
        final localFk = '${k.substring(0, k.length - 8)}_id';
        p['__fk_$localFk'] = e.value;
        continue;
      }
      if (k == 'sync_id') {
        p['sync_id'] = e.value;
        continue;
      }
      p[_cloudColToLocal(localTable, k)] = e.value;
    }
    return p;
  }

  static Map<String, Object?> _localToCloudRow(
    String localTable,
    Map<String, Object?> payload,
  ) {
    final out = <String, Object?>{'sync_id': payload['sync_id']};
    final pk = _pk(localTable).toLowerCase();
    final fkCols =
        _fks[localTable]?.keys.map((k) => k.toLowerCase()).toSet() ?? {};
    for (final e in payload.entries) {
      final k = e.key;
      if (k == 'sync_id' || k.startsWith('__fk_')) continue;
      final lk = k.toLowerCase();
      if (lk == pk ||
          lk == 'id' ||
          lk == 'sid' ||
          lk == 'person_id' ||
          lk == 'deal_id' ||
          lk == 'credit_id' ||
          lk == 'userid' ||
          fkCols.contains(lk)) {
        continue;
      }
      final v = _sqlVal(e.value);
      if (v == null) continue;
      out[_localColToCloud(localTable, k)] = v;
    }
    return out;
  }

  static List<Map<String, dynamic>> _asApplyRows(
    String localTable,
    List<Map<String, dynamic>> cloudRows,
  ) {
    return cloudRows
        .map(
          (r) => {
            'table_name': localTable,
            'sync_id': r['sync_id'],
            'is_deleted': r['is_deleted'],
            'staff_name': r['staff_name'],
            'payload': _cloudToLocalPayload(localTable, r),
          },
        )
        .toList();
  }

  static String _pk(String table) {
    switch (table.toLowerCase()) {
      case 'expensetbl':
        return 'sid';
      case 'person':
        return 'person_id';
      case 'deal':
        return 'deal_id';
      case 'credit':
        return 'credit_id';
      case 'users':
        return 'userID';
      default:
        return 'id';
    }
  }

  static void markDirty() {
    _dirty = true;
    _bump();
  }

  /// Restore ke baad: hashes clear + pehle push (warna cloud/mobile purana reh jata).
  static Future<void> notifyDatabaseRestored() async {
    try {
      final db = await AppDatabase.instance.database;
      try {
        await db.delete('sync_hash');
      } catch (_) {}
      await _setState('push_hashes_ready', '');
      await _setState('restore_push_first', '1');
      // Full-pull mat chalao — restored data cloud ke purane se overwrite na ho
      if (await _getState('first_pull_done') != '1') {
        await _setState('first_pull_done', '1');
      }
      if ((await _getState('last_pull_utc')).trim().isEmpty) {
        await _setState(
          'last_pull_utc',
          DateTime.now().toUtc().toIso8601String(),
        );
      }
      markDirty();
      unawaited(syncNow());
    } catch (e) {
      debugPrint('HubSync notifyDatabaseRestored: $e');
    }
  }

  static void markDirtyTable(String table) {
    markDirty();
    final t = table.trim();
    if (t.isNotEmpty) _dirtyTables.add(t);
  }

  /// WinForms NotifyLocalWrite — local save/delete ke baad turant pull/push cycle.
  /// [alreadyPushed] true jab [pushNow] pehle chal chuka — sirf pull, full hash-scan skip.
  static void notifyLocalWrite({bool alreadyPushed = false}) {
    if (suppressCloudIo) return;
    try {
      if (!alreadyPushed) markDirty();
    } catch (_) {}
    unawaited(tick());
  }

  static int _manualHold = 0;

  /// Manual Pull Now — pull + catch-up + hard-delete prune (fast).
  static Future<bool> pullNow() async {
    _manualHold++;
    try {
      if (!await _ensureSb()) return false;
      if (_inFlight != null) {
        try {
          await _inFlight;
        } catch (_) {}
      }
      _inFlight = _pullNowBody();
      try {
        await _inFlight;
        return true;
      } finally {
        _inFlight = null;
      }
    } catch (e, st) {
      debugPrint('HubSync pullNow: $e\n$st');
      return false;
    } finally {
      _manualHold--;
    }
  }

  /// Manual Sync Now = WinForms Sync Now: pull + catch-up + push + hard-delete prune.
  static Future<bool> syncNow() async {
    _manualHold++;
    try {
      markDirty();
      if (!await _ensureSb()) return false;
      if (_inFlight != null) {
        try {
          await _inFlight;
        } catch (_) {}
      }
      _inFlight = _syncNowBody();
      try {
        await _inFlight;
        return true;
      } finally {
        _inFlight = null;
      }
    } catch (e, st) {
      debugPrint('HubSync syncNow: $e\n$st');
      return false;
    } finally {
      _manualHold--;
    }
  }

  /// Force Recover UI entry — DISABLED (galati se press risk). Sync/Pull use karein.
  static Future<({bool ok, String summary})> forceRecoverFromCloud() async {
    return (
      ok: false,
      summary:
          'Force Recover band hai — Sync / Pull use karein. Galati se recover nahi hoga.',
    );
  }

  static const _allocateRecoverTables = <String>[
    'societies',
    'blocks',
    'plot_types',
    'plots',
    'plans',
    'customers',
    'brokers',
    'customer_plots',
    'ledger',
    'receipts',
  ];

  static Future<({bool ok, String summary})> _forceRecoverBody() async {
    // Kept for reference; UI entry disabled via forceRecoverFromCloud().
    await _pushPendingDeletes();
    if (await _canPush()) {
      await _pushChanged();
      _dirty = false;
    }
    await _setState('restore_push_first', '');

    final stats = await forceRecoverMissingFromCloud();
    try {
      await reconcileOneActiveCustomerPlots();
    } catch (_) {}
    _bump();

    var summary =
        'Recovered ${stats.recovered} missing rows '
        '(allocate=${stats.allocateRecovered}), '
        'skipped ${stats.skipped} duplicates (same sync_id). '
        'Local extras wipe nahi.';
    if (stats.skipReasons.trim().isNotEmpty) {
      summary += ' Skipped: ${stats.skipReasons}';
    }
    debugPrint('HubSync $summary');
    return (ok: true, summary: summary);
  }

  /// Pull-missing-only: cloud is_deleted=false + local sync_id absent → INSERT.
  /// Local same sync_id → skip. Deleted cloud rows not fetched. No prune.
  /// Multi-pass + parent ensure for customer_plots (plot chain / customer / placeholder).
  static Future<
      ({
        int recovered,
        int skipped,
        int allocateRecovered,
        String skipReasons,
      })> forceRecoverMissingFromCloud() async {
    var recovered = 0;
    var skipped = 0;
    var allocateRecovered = 0;
    final skipList = <String>[];
    try {
      final db = await AppDatabase.instance.database;
      await db.delete(
        'sync_alias',
        where:
            "lower(table_name) IN ('partner_money','receipts','customer_plots')",
      );
    } catch (_) {}

    for (var pass = 0; pass < 3; pass++) {
      for (final table in _allocateRecoverTables) {
        final isAlloc = table == 'customer_plots';
        final cloud = _cloudTable(table);
        final db = await AppDatabase.instance.database;
        final local = await _localSyncIdsForTable(db, table);
        for (var page = 0; page < 60; page++) {
          final from = page * 1000;
          final got = await _sbSelect(
            cloud,
            from: from,
            to: from + 999,
            columns: 'sync_id',
            orderBy: 'sync_id',
          );
          if (!got.ok || got.rows.isEmpty) break;
          final missing = <String>[];
          for (final row in got.rows) {
            final id = (row['sync_id'] ?? '').toString().trim().toLowerCase();
            if (id.isEmpty) continue;
            if (local.contains(id)) {
              if (pass == 0) skipped++;
              continue;
            }
            missing.add(id);
          }
          for (var i = 0; i < missing.length; i += 30) {
            final end = (i + 30 < missing.length) ? i + 30 : missing.length;
            final chunk = missing.sublist(i, end);
            final fetched = await _sbSelect(
              cloud,
              from: 0,
              to: chunk.length + 5,
              syncIds: chunk,
            );
            if (fetched.rows.isEmpty) continue;

            final rows = fetched.rows
                .map((r) => Map<String, dynamic>.from(r))
                .toList();
            if (isAlloc) {
              await _ensureRecoverParentsForCustomerPlots(rows, skipList);
            } else if (table == 'customers') {
              _filterOutRecoverPlaceholderCustomers(rows, skipList);
            } else if (table == 'receipts' || table == 'ledger') {
              await _filterRecoverChildrenToExistingAllocs(table, rows, skipList);
            }
            if (rows.isEmpty) continue;

            final before = await _localRowCount(table);
            await _applyFetched(
              _asApplyRows(table, rows),
              notify: false,
              bump: false,
            );
            final after = await _localRowCount(table);
            final added = after > before ? after - before : 0;
            recovered += added;
            if (table == 'customer_plots' || table == 'receipts') {
              allocateRecovered += added;
            }
            final db2 = await AppDatabase.instance.database;
            for (final r in rows) {
              final id = (r['sync_id'] ?? '').toString().trim().toLowerCase();
              if (id.isEmpty) continue;
              if (await _localIdBySyncExact(db2, table, id) != null) {
                local.add(id);
              }
            }
          }
          if (got.rows.length < 1000) break;
        }
      }
    }

    try {
      final db = await AppDatabase.instance.database;
      final localCp = await _localSyncIdsForTable(db, 'customer_plots');
      final still = await _sbSelect(
        'customer_plots',
        from: 0,
        to: 999,
        columns: 'sync_id,plot_sync_id,customer_sync_id',
        orderBy: 'sync_id',
      );
      for (final row in still.rows) {
        final id = (row['sync_id'] ?? '').toString().trim().toLowerCase();
        if (id.isEmpty || localCp.contains(id)) continue;
        final ps = (row['plot_sync_id'] ?? '').toString().trim();
        final cs = (row['customer_sync_id'] ?? '').toString().trim();
        final hasPlot =
            ps.isNotEmpty && await _localIdBySync(db, 'plots', ps) != null;
        final hasCust =
            cs.isNotEmpty && await _localIdBySync(db, 'customers', cs) != null;
        final short = id.length >= 8 ? id.substring(0, 8) : id;
        final why = !hasPlot
            ? '$short: plot parent missing'
            : (cs.isEmpty && !hasCust)
                ? '$short: customer null/unresolved'
                : (!hasCust
                    ? '$short: customer not in cloud/local'
                    : '$short: insert failed');
        if (!skipList.any((s) => s.startsWith(short))) skipList.add(why);
      }
    } catch (_) {}

    final uniq = <String>[];
    for (final s in skipList) {
      if (!uniq.contains(s)) uniq.add(s);
      if (uniq.length >= 6) break;
    }
    return (
      recovered: recovered,
      skipped: skipped,
      allocateRecovered: allocateRecovered,
      skipReasons: uniq.join('; '),
    );
  }

  /// Force Recover: plot chain + real cloud customer only. No Recovered placeholders.
  /// Invalid orphan rows are removed from [cloudRows] before apply.
  static Future<void> _ensureRecoverParentsForCustomerPlots(
    List<Map<String, dynamic>> cloudRows,
    List<String> skipList,
  ) async {
    final keep = <Map<String, dynamic>>[];
    for (final row in cloudRows) {
      try {
        final allocSync = (row['sync_id'] ?? '').toString().trim();
        final plotSync = (row['plot_sync_id'] ?? '').toString().trim();
        final custSync = (row['customer_sync_id'] ?? '').toString().trim();
        final planSync = (row['plan_sync_id'] ?? '').toString().trim();
        final brokerSync = (row['broker_sync_id'] ?? '').toString().trim();
        final short =
            allocSync.length >= 8 ? allocSync.substring(0, 8) : allocSync;

        if (custSync.toLowerCase().startsWith('recover_')) {
          skipList.add('$short: skip — placeholder customer_sync_id');
          continue;
        }

        if (plotSync.isNotEmpty) await _ensureRecoverPlotChain(plotSync);
        if (planSync.isNotEmpty) {
          await _fetchAndApplyMissingRows('plans', [planSync], true);
        }
        if (brokerSync.isNotEmpty) {
          await _fetchAndApplyMissingRows('brokers', [brokerSync], true);
        }
        if (custSync.isNotEmpty) {
          await _fetchAndApplyMissingRows('customers', [custSync], true);
        }

        final db = await AppDatabase.instance.database;
        final plotId = plotSync.isEmpty
            ? null
            : await _localIdBySync(db, 'plots', plotSync);
        final custId = custSync.isEmpty
            ? null
            : await _localIdBySync(db, 'customers', custSync);

        if (plotId == null) {
          skipList.add('$short: plot parent missing');
          await _savePendingApply(
            'customer_plots',
            row,
            waitParentTable: 'plots',
            waitParentSyncId: plotSync,
          );
          continue;
        }
        if (custSync.isEmpty || custId == null) {
          skipList.add('$short: skip — no valid cloud customer');
          await _savePendingApply(
            'customer_plots',
            row,
            waitParentTable: 'customers',
            waitParentSyncId: custSync,
          );
          continue;
        }
        if (await _isLocalRecoverPlaceholderCustomer(db, custId)) {
          skipList.add('$short: skip — Recovered placeholder customer');
          continue;
        }
        keep.add(row);
      } catch (e) {
        final id = (row['sync_id'] ?? '?').toString();
        final short = id.length >= 8 ? id.substring(0, 8) : id;
        skipList.add('$short: $e');
      }
    }
    cloudRows
      ..clear()
      ..addAll(keep);
  }

  static void _filterOutRecoverPlaceholderCustomers(
    List<Map<String, dynamic>> rows,
    List<String> skipList,
  ) {
    final keep = <Map<String, dynamic>>[];
    for (final row in rows) {
      if (_isForceRecoverPlaceholderCustomerPayload(row)) {
        final id = (row['sync_id'] ?? '?').toString();
        final short = id.length >= 8 ? id.substring(0, 8) : id;
        skipList.add('$short: skip — Recovered placeholder');
        continue;
      }
      keep.add(row);
    }
    rows
      ..clear()
      ..addAll(keep);
  }

  static Future<void> _filterRecoverChildrenToExistingAllocs(
    String table,
    List<Map<String, dynamic>> rows,
    List<String> skipList,
  ) async {
    final db = await AppDatabase.instance.database;
    final keep = <Map<String, dynamic>>[];
    for (final row in rows) {
      final id = (row['sync_id'] ?? '').toString();
      final short = id.length >= 8 ? id.substring(0, 8) : id;
      final cpSync = (row['customer_plot_sync_id'] ?? '').toString().trim();
      if (cpSync.isEmpty ||
          await _localIdBySync(db, 'customer_plots', cpSync) == null) {
        skipList.add('$short: skip — parent allocate missing ($table)');
        await _savePendingApply(
          table,
          row,
          waitParentTable: 'customer_plots',
          waitParentSyncId: cpSync,
        );
        continue;
      }
      keep.add(row);
    }
    rows
      ..clear()
      ..addAll(keep);
  }

  static bool _isForceRecoverPlaceholderCustomerPayload(
    Map<String, dynamic> row,
  ) {
    final sid = (row['sync_id'] ?? '').toString().trim();
    final name = (row['name'] ?? '').toString();
    final notes = (row['notes'] ?? '').toString();
    if (sid.toLowerCase().startsWith('recover_')) return true;
    if (notes.toLowerCase().contains('force-recover')) return true;
    if (name.startsWith('Recovered (')) return true;
    return false;
  }

  static Future<bool> _isLocalRecoverPlaceholderCustomer(
    DatabaseExecutor db,
    int customerId,
  ) async {
    try {
      final rows = await db.query(
        'customers',
        columns: ['name', 'notes', 'sync_id'],
        where: 'id = ?',
        whereArgs: [customerId],
        limit: 1,
      );
      if (rows.isEmpty) return true;
      final name = (rows.first['name'] ?? '').toString();
      final notes = (rows.first['notes'] ?? '').toString();
      final sid = (rows.first['sync_id'] ?? '').toString().trim();
      if (sid.toLowerCase().startsWith('recover_')) return true;
      if (notes.toLowerCase().contains('force-recover')) return true;
      if (name.startsWith('Recovered (')) return true;
    } catch (_) {
      return true;
    }
    return false;
  }

  static Future<void> _ensureRecoverPlotChain(String plotSyncId) async {
    final sid = plotSyncId.trim();
    if (sid.isEmpty) return;
    final db = await AppDatabase.instance.database;
    if (await _localIdBySync(db, 'plots', sid) != null) return;
    final got = await _sbSelect(
      'plots',
      from: 0,
      to: 5,
      syncIds: [sid],
      includeDeleted: true,
    );
    if (got.rows.isEmpty) return;
    final plot = Map<String, dynamic>.from(got.rows.first);
    plot['is_deleted'] = false; // soft-deleted plot still needed as FK parent
    final societySync = (plot['society_sync_id'] ?? '').toString().trim();
    final blockSync = (plot['block_sync_id'] ?? '').toString().trim();
    final typeSync = (plot['plot_type_sync_id'] ?? '').toString().trim();
    if (societySync.isNotEmpty) {
      await _fetchAndApplyMissingRows('societies', [societySync], true);
    }
    if (blockSync.isNotEmpty) {
      await _fetchAndApplyMissingRows('blocks', [blockSync], true);
    }
    if (typeSync.isNotEmpty) {
      await _fetchAndApplyMissingRows('plot_types', [typeSync], true);
    }
    await _applyFetched(
      _asApplyRows('plots', [plot]),
      notify: false,
      bump: false,
    );
  }

  static Future<void> _fetchAndApplyMissingRows(
    String table,
    List<String> syncIds,
    bool includeDeleted,
  ) async {
    if (syncIds.isEmpty) return;
    var ids = syncIds;
    if (table == 'customers') {
      ids = syncIds
          .where((s) => s.trim().isNotEmpty && !s.trim().toLowerCase().startsWith('recover_'))
          .toList();
      if (ids.isEmpty) return;
    }
    final db = await AppDatabase.instance.database;
    final need = <String>[];
    for (final id in ids) {
      final t = id.trim();
      if (t.isEmpty) continue;
      if (await _localIdBySync(db, table, t) == null) need.add(t);
    }
    if (need.isEmpty) return;
    for (var i = 0; i < need.length; i += 30) {
      final end = (i + 30 < need.length) ? i + 30 : need.length;
      final chunk = need.sublist(i, end);
      final got = await _sbSelect(
        _cloudTable(table),
        from: 0,
        to: chunk.length + 5,
        syncIds: chunk,
        includeDeleted: includeDeleted,
      );
      if (got.rows.isEmpty) continue;
      var rows = got.rows.map((r) {
        final m = Map<String, dynamic>.from(r);
        // Soft-deleted parents: insert as live for FK resolve (do not ApplyRow-delete)
        if (includeDeleted) m['is_deleted'] = false;
        return m;
      }).toList();
      if (table == 'customers') {
        final skip = <String>[];
        _filterOutRecoverPlaceholderCustomers(rows, skip);
        if (rows.isEmpty) continue;
      }
      await _applyFetched(
        _asApplyRows(table, rows),
        notify: false,
        bump: false,
      );
    }
  }

  /// DISABLED — Recovered placeholders caused bad hisab.
  static Future<String> _ensureRecoverPlaceholderCustomer(
    DatabaseExecutor db,
    String customerSyncId,
    String allocSyncId,
  ) async {
    return '';
  }

  /// WinForms-primary: remote soft-deletes pehle apply, phir local push.
  /// Pehle push se mobile purani row cloud delete overwrite karti thi (delete wapas aa jata).
  static Future<void> _syncCycleBody({required bool forcePrune}) async {
    // 1) Is device ke pending deletes soft-mark (apni deletes)
    await _pushPendingDeletes();

    // 2) Pull updates + deletes pehle (WinForms delete/update/transfer)
    if (await _needsFullPull()) {
      await _fullPullAllTables();
    } else {
      await _pullIncremental();
      await _pullRecentHeal(120);
    }
    await _mirrorUsersFromCloud();
    await _pruneGoneFromCloud(force: forcePrune);

    // 3) Ab push — deleted rows local se hat chuki, cloud soft-delete overwrite nahi
    if (await _getState('restore_push_first') == '1' || await _canPush()) {
      if (await _canPush()) {
        await _pushChanged();
        _dirty = false;
      }
      if (await _getState('restore_push_first') == '1') {
        await _setState('restore_push_first', '');
      }
    }

    // 4) Insert-missing only (live cloud rows) — soft-deleted catch-up mein nahi
    if (await _looksStaleLocal()) {
      await forceRecoverMissingFromCloud();
      try {
        await reconcileOneActiveCustomerPlots();
      } catch (_) {}
    } else {
      await _catchUpMissing();
    }
    _bump();
  }

  static Future<void> _pullNowBody() async {
    await _syncCycleBody(forcePrune: true);
  }

  static Future<void> _syncNowBody() async {
    await _syncCycleBody(forcePrune: true);
  }

  /// Soft-mark pending deletes + push dirty rows only (no empty destructive upload).
  static Future<void> _pushLocalFirst() async {
    await _pushPendingDeletes();
    if (await _canPush()) {
      await _pushChanged();
      _dirty = false;
    }
    if (await _getState('restore_push_first') == '1') {
      await _setState('restore_push_first', '');
    }
  }

  /// Local save ke saath outbox — late ACK purani revision clear nahi kare.
  static Future<String> enqueueOutbox(
    DatabaseExecutor db,
    String table,
    int rowId,
    String syncId, {
    String op = 'upsert',
  }) async {
    final rev = _newRevision();
    if (rowId <= 0 || table.isEmpty || syncId.trim().isEmpty) return rev;
    try {
      await db.insert('sync_outbox', {
        'table_name': table,
        'local_pk': rowId,
        'sync_id': syncId.trim(),
        'revision': rev,
        'op': op,
      });
    } catch (_) {}
    return rev;
  }

  static String _newRevision() {
    final r = Random();
    final b = List<int>.generate(16, (_) => r.nextInt(256));
    return b.map((e) => e.toRadixString(16).padLeft(2, '0')).join();
  }

  static Future<void> _clearOutboxRevision(String? revision) async {
    if (revision == null || revision.trim().isEmpty) return;
    try {
      final db = await AppDatabase.instance.database;
      await db.delete(
        'sync_outbox',
        where: 'revision = ?',
        whereArgs: [revision],
      );
    } catch (_) {}
  }

  static Future<void> _drainOutbox() async {
    try {
      final db = await AppDatabase.instance.database;
      if (!await _tableExists(db, 'sync_outbox')) return;
      final jobs = await db.query(
        'sync_outbox',
        columns: ['table_name', 'local_pk', 'revision'],
        orderBy: 'id ASC',
        limit: 40,
      );
      for (final j in jobs) {
        final t = (j['table_name'] ?? '').toString();
        final pk = int.tryParse((j['local_pk'] ?? '').toString()) ?? 0;
        final rev = (j['revision'] ?? '').toString();
        if (t.isEmpty || pk <= 0) continue;
        await pushNow(t, pk, outboxRevision: rev);
      }
    } catch (e) {
      debugPrint('HubSync drainOutbox: $e');
    }
  }

  static void noteCustomerLocalSave(String syncId) {
    _custLocalSaveAt = DateTime.now();
    _custTimingSyncId = syncId.trim().toLowerCase();
    syncTiming.value = {
      ...syncTiming.value,
      'local_save_ms': 0,
      'cloud_ack_ms': -1,
      'remote_apply_ms': -1,
      'ui_refresh_ms': -1,
    };
  }

  static void noteCustomerCloudAck(String syncId) {
    final t0 = _custLocalSaveAt;
    if (t0 == null) return;
    if (syncId.trim().toLowerCase() != (_custTimingSyncId ?? '')) return;
    syncTiming.value = {
      ...syncTiming.value,
      'cloud_ack_ms': DateTime.now().difference(t0).inMilliseconds,
    };
  }

  static void noteCustomerUiRefresh() {
    final t0 = _custLocalSaveAt;
    if (t0 == null) return;
    syncTiming.value = {
      ...syncTiming.value,
      'ui_refresh_ms': DateTime.now().difference(t0).inMilliseconds,
    };
  }

  static Future<void> pushNow(
    String table,
    Object? rowId, {
    String? outboxRevision,
  }) async {
    if (rowId == null) return;
    if (table == 'staff_entry_log' || table.startsWith('sync_')) return;
    if (!await _ensureSb()) return;
    try {
      final db = await AppDatabase.instance.database;
      if (!await _tableExists(db, table)) return;

      // Parents pehle — WinForms/mobile dono taraf list empty na rahe
      if (table == 'customer_plots') {
        final parents = await db.rawQuery(
          'SELECT customer_id, plot_id, broker_id FROM customer_plots WHERE id = ? LIMIT 1',
          [rowId],
        );
        if (parents.isNotEmpty) {
          final p = parents.first;
          final cid = p['customer_id'];
          final pid = p['plot_id'];
          final bid = p['broker_id'];
          if (cid != null) await pushNow('customers', cid);
          if (pid != null) await pushNow('plots', pid);
          if (bid != null) await pushNow('brokers', bid);
        }
      } else if (table == 'receipts' || table == 'ledger') {
        final parents = await db.rawQuery(
          'SELECT customer_plot_id FROM "$table" WHERE id = ? LIMIT 1',
          [rowId],
        );
        if (parents.isNotEmpty) {
          final aid = parents.first['customer_plot_id'];
          if (aid != null) await pushNow('customer_plots', aid);
        }
      }

      final pk = _pk(table);
      await db.execute(
        'UPDATE "$table" SET sync_id = lower(hex(randomblob(16))) WHERE "$pk" = ? AND (sync_id IS NULL OR TRIM(sync_id)=\'\')',
        [rowId],
      );
      final rows = await db.rawQuery(
        'SELECT * FROM "$table" WHERE "$pk" = ? LIMIT 1',
        [rowId],
      );
      if (rows.isEmpty) return;
      final payload = Map<String, Object?>.from(rows.first);
      final syncId = (payload['sync_id'] ?? '').toString();
      if (syncId.trim().isEmpty) return;
      final fks = _fks[table];
      if (fks != null) {
        for (final kv in fks.entries) {
          final fkVal = payload[kv.key];
          if (fkVal == null) continue;
          final parent = await _lookupSyncId(db, kv.value, fkVal);
          if (parent.isNotEmpty) payload['__fk_${kv.key}'] = parent;
        }
      }
      final hash = _rowHash(payload);
      final cloudRow = _localToCloudRow(table, payload);
      if (fks != null) {
        for (final kv in fks.entries) {
          final fkVal = payload[kv.key];
          if (fkVal == null) continue;
          final parent = await _lookupSyncId(db, kv.value, fkVal);
          if (parent.isNotEmpty) cloudRow[_fkCloudCol(kv.key)] = parent;
        }
      }
      final staff = (StaffAudit.displayName ?? StaffAudit.username ?? '').trim();
      // Pending delete / cloud tombstone — live upsert se resurrect mat karo
      final pending = await _loadPendingDeleteSyncIds();
      if (pending.contains(syncId.trim().toLowerCase())) return;
      try {
        final check = await Supabase.instance.client
            .from(_cloudTable(table))
            .select('sync_id,is_deleted')
            .eq('sync_id', syncId)
            .limit(1);
        if (check.isNotEmpty && _cloudRowIsDeleted(check.first)) return;
      } catch (_) {}
      cloudRow['dup_key'] = _dupKey(table, payload);
      cloudRow['is_deleted'] = false;
      cloudRow['staff_name'] = staff;
      cloudRow['updated_at'] = _utcFilter(DateTime.now().toUtc());
      final ok = await _httpUpsertIds(_cloudTable(table), [cloudRow]);
      if (ok.contains(syncId)) {
        // Race: delete jeet gaya to SoftMark dubara
        try {
          final again = await Supabase.instance.client
              .from(_cloudTable(table))
              .select('is_deleted')
              .eq('sync_id', syncId)
              .limit(1);
          if (again.isNotEmpty && _cloudRowIsDeleted(again.first)) {
            await _softMarkCloudDeleted(table, syncId);
            return;
          }
        } catch (_) {}
        await _saveHash(db, syncId, hash);
        await _clearOutboxRevision(outboxRevision);
        if (table == 'customers') noteCustomerCloudAck(syncId);
        _dirty = false;
        _bump();
        await _processPendingApplyQueue();
      }
      try {
        await db.insert(
          'sync_known',
          {'sync_id': syncId, 'table_name': table},
          conflictAlgorithm: ConflictAlgorithm.replace,
        );
      } catch (_) {}
    } catch (e) {
      debugPrint('HubSync pushNow $table: $e');
    }
  }

  static Future<void> _queuePendingDelete(String localTable, String syncId) async {
    final sid = syncId.trim();
    final table = localTable.trim();
    if (sid.isEmpty || table.isEmpty) return;
    try {
      final db = await AppDatabase.instance.database;
      await db.insert(
        'sync_pending_delete',
        {'sync_id': sid, 'table_name': table},
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
      markDirty();
    } catch (_) {}
  }

  static Future<void> _removePendingDelete(String syncId) async {
    if (syncId.trim().isEmpty) return;
    try {
      final db = await AppDatabase.instance.database;
      await db.delete(
        'sync_pending_delete',
        where: 'sync_id = ?',
        whereArgs: [syncId.trim()],
      );
    } catch (_) {}
  }

  static Future<void> _clearPendingDeleteMeta(String syncId) async {
    final sid = syncId.trim();
    if (sid.isEmpty) return;
    try {
      final db = await AppDatabase.instance.database;
      await db.delete('sync_known', where: 'sync_id = ?', whereArgs: [sid]);
      await db.delete('sync_hash', where: 'sync_id = ?', whereArgs: [sid]);
      await db.delete('sync_alias', where: 'sync_id = ?', whereArgs: [sid]);
    } catch (_) {}
  }

  /// Offline delete queue — soft-mark tombstone only (no hard-delete).
  static Future<void> _pushPendingDeletes() async {
    if (!SupabaseConfig.isConfigured) return;
    try {
      final db = await AppDatabase.instance.database;
      final pending = await db.query(
        'sync_pending_delete',
        columns: ['table_name', 'sync_id', 'queued_at'],
        orderBy: 'queued_at ASC',
      );
      for (final row in pending) {
        final table = (row['table_name'] ?? '').toString().trim();
        final sid = (row['sync_id'] ?? '').toString().trim();
        if (table.isEmpty || sid.isEmpty) continue;
        // Soft tombstone only — hard delete se doosri device delete signal kho jati hai
        if (await _softMarkCloudDeleted(table, sid)) {
          await _removePendingDelete(sid);
        }
      }
    } catch (e) {
      debugPrint('HubSync pending deletes: $e');
    }
  }

  static bool _pendingDeleteAged(String queuedAt, int minSec) {
    final q = queuedAt.trim();
    if (q.isEmpty) return true;
    var dt = DateTime.tryParse(q);
    dt ??= DateTime.tryParse(q.replaceFirst(' ', 'T'));
    if (dt == null) return true;
    return DateTime.now().difference(dt).inSeconds >= minSec;
  }

  /// Soft-delete tombstone for other devices. No hard-delete in normal sync.
  static Future<bool> _pushDeletedOne(String localTable, String syncId) async {
    final sid = syncId.trim();
    final table = localTable.trim();
    if (sid.isEmpty || table.isEmpty) return true;
    await _queuePendingDelete(table, sid);
    if (suppressCloudIo || !SupabaseConfig.isConfigured) return false;
    final softOk = await _softMarkCloudDeleted(table, sid);
    await _clearPendingDeleteMeta(sid);
    if (softOk) await _removePendingDelete(sid);
    return softOk;
  }

  static Map<String, Object?> _buildDeleteTombstone(String table, String syncId) {
    final tomb = <String, Object?>{
      'sync_id': syncId,
      'is_deleted': true,
      'updated_at': DateTime.now().toUtc().toIso8601String(),
      'staff_name':
          (StaffAudit.displayName ?? StaffAudit.username ?? 'system').trim(),
    };
    final t = table.toLowerCase();
    if (t == 'customer_plots') {
      tomb['booking_date'] =
          DateTime.now().toIso8601String().substring(0, 10);
      tomb['price'] = 0;
      tomb['down_payment'] = 0;
      tomb['status'] = 'Cancelled';
      tomb['deal_type'] = 'Installment';
      tomb['notes'] = '';
    } else if (t == 'customers') {
      tomb['name'] = '(deleted)';
      tomb['mobile'] = '';
      tomb['cnic'] = '';
    } else if (t == 'receipts') {
      tomb['receipt_date'] =
          DateTime.now().toIso8601String().substring(0, 10);
      tomb['amount'] = 0;
      tomb['mode'] = 'Cash';
    } else if (t == 'ledger') {
      tomb['txn_date'] = DateTime.now().toIso8601String().substring(0, 10);
      tomb['txn_type'] = 'Adjustment';
      tomb['debit'] = 0;
      tomb['credit'] = 0;
    }
    return tomb;
  }

  static bool _cloudRowIsDeleted(Map row) {
    final del = row['is_deleted'];
    return del == true ||
        del == 1 ||
        del?.toString().toLowerCase() == 'true';
  }

  static Future<bool> _softMarkCloudDeleted(
    String localTable,
    String syncId,
  ) async {
    final sid = syncId.trim();
    final table = localTable.trim();
    if (sid.isEmpty || table.isEmpty) return true;
    if (_softMarkBlockedSyncIds.contains(sid.toLowerCase())) {
      debugPrint('HubSync SoftMark blocked (protected): $sid');
      return false;
    }
    final cloud = _cloudTable(table);
    try {
      if (!await _ensureSb()) return false;
      await Supabase.instance.client.from(cloud).update({
        'is_deleted': true,
        'updated_at': DateTime.now().toUtc().toIso8601String(),
      }).eq('sync_id', sid);
    } catch (e) {
      debugPrint('HubSync soft-delete $table $e');
    }
    try {
      final left = await Supabase.instance.client
          .from(cloud)
          .select('sync_id,is_deleted')
          .eq('sync_id', sid)
          .limit(1);
      if (left.isNotEmpty && _cloudRowIsDeleted(left.first)) return true;
      // Row missing ya still live — INSERT/UPSERT tombstone (mobile delete signal)
      await Supabase.instance.client.from(cloud).upsert(
            _buildDeleteTombstone(table, sid),
            onConflict: 'sync_id',
          );
      final again = await Supabase.instance.client
          .from(cloud)
          .select('is_deleted')
          .eq('sync_id', sid)
          .limit(1);
      return again.isNotEmpty && _cloudRowIsDeleted(again.first);
    } catch (e) {
      debugPrint('HubSync soft-delete verify $table $e');
      return false;
    }
  }

  static Future<bool> _hardDeleteCloudRow(
    String localTable,
    String syncId,
  ) async {
    final sid = syncId.trim();
    final table = localTable.trim();
    if (sid.isEmpty || table.isEmpty) return true;
    try {
      if (!await _ensureSb()) return false;
      await Supabase.instance.client
          .from(_cloudTable(table))
          .delete()
          .eq('sync_id', sid);
      // Verify — RLS no-op catch
      try {
        final left = await Supabase.instance.client
            .from(_cloudTable(table))
            .select('sync_id')
            .eq('sync_id', sid)
            .limit(1);
        if (left.isNotEmpty) {
          await _softMarkCloudDeleted(table, sid);
          return false;
        }
      } catch (_) {}
      return true;
    } catch (e) {
      debugPrint('HubSync hard-delete $table $e');
      return false;
    }
  }

  /// Local delete ke baad cloud soft+hard delete queue.
  static Future<void> pushDeleted(String localTable, String syncId) async {
    await _pushDeletedOne(localTable, syncId);
  }

  /// Cloud login hard delete — username wale users Supabase se hatao.
  static Future<void> pushDeletedUsername(String username) async {
    final un = username.trim();
    if (un.isEmpty) return;
    try {
      if (!await _ensureSb()) return;
      await Supabase.instance.client
          .from('users')
          .delete()
          .filter('username', 'ilike', un);
    } catch (e) {
      debugPrint('HubSync hard-delete username $e');
    }
  }

  static Future<void> ensureLocalSchema() async {
    final db = await AppDatabase.instance.database;
    try {
      await BlockMonthlyStore.ensureSchema(db);
    } catch (_) {}
    try {
      await reconcileOneActiveCustomerPlots(db);
    } catch (_) {}
    try {
      await BlockMonthlyStore.applyToEmptyActive();
    } catch (_) {}
    await db.execute(
      'CREATE TABLE IF NOT EXISTS sync_state (k TEXT PRIMARY KEY, v TEXT)',
    );
    await db.execute(
      'CREATE TABLE IF NOT EXISTS sync_known (sync_id TEXT PRIMARY KEY, table_name TEXT)',
    );
    await db.execute(
      'CREATE TABLE IF NOT EXISTS sync_hash (sync_id TEXT PRIMARY KEY, row_hash TEXT)',
    );
    await db.execute('''
CREATE TABLE IF NOT EXISTS sync_alias (
  sync_id TEXT PRIMARY KEY,
  table_name TEXT NOT NULL,
  local_pk INTEGER NOT NULL
)''');
    await db.execute('''
CREATE TABLE IF NOT EXISTS sync_pending_delete (
  sync_id TEXT PRIMARY KEY,
  table_name TEXT NOT NULL,
  queued_at TEXT DEFAULT (datetime('now','localtime'))
)''');
    await db.execute('''
CREATE TABLE IF NOT EXISTS sync_outbox (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  table_name TEXT NOT NULL,
  local_pk INTEGER NOT NULL,
  sync_id TEXT NOT NULL,
  revision TEXT NOT NULL,
  op TEXT NOT NULL DEFAULT 'upsert',
  created_at TEXT DEFAULT (datetime('now'))
)''');
    await db.execute('''
CREATE TABLE IF NOT EXISTS sync_pending_apply (
  sync_id TEXT PRIMARY KEY,
  table_name TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  wait_parent_table TEXT,
  wait_parent_sync_id TEXT,
  cloud_updated_at TEXT,
  created_at TEXT DEFAULT (datetime('now'))
)''');
    await db.execute('''
CREATE TABLE IF NOT EXISTS sync_conflict (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  table_name TEXT NOT NULL,
  sync_id TEXT NOT NULL,
  local_json TEXT,
  cloud_json TEXT,
  created_at TEXT DEFAULT (datetime('now'))
)''');
    for (final t in _tables) {
      if (!await _tableExists(db, t)) continue;
      if (!await _hasColumn(db, t, 'sync_id')) {
        await db.execute('ALTER TABLE "$t" ADD COLUMN sync_id TEXT');
      }
      await db.execute(
        "UPDATE \"$t\" SET sync_id = lower(hex(randomblob(16))) WHERE sync_id IS NULL OR TRIM(sync_id)=''",
      );
      // Non-unique index — tez lookup, purani entries break nahi
      try {
        final idx = 'idx_${t.toLowerCase()}_sync_id';
        await db.execute('CREATE INDEX IF NOT EXISTS "$idx" ON "$t"(sync_id)');
      } catch (_) {}
    }
    await _seedHashesIfAlreadySynced();
    // Schema 11: outbox + pending_apply (entries wipe nahi)
    final sch = await _getState('sync_schema');
    if (sch != '11') {
      final populated = (await _localRowCount('customer_plots')) +
              (await _localRowCount('customers')) >=
          20;
      if (populated) {
        await _setState('first_pull_done', '1');
        if ((await _getState('last_pull_utc')).trim().isEmpty) {
          await _setState(
            'last_pull_utc',
            DateTime.now()
                .toUtc()
                .subtract(const Duration(minutes: 2))
                .toIso8601String(),
          );
        }
      } else if (sch.isEmpty) {
        await _setState('last_pull_utc', '');
        await _setState('first_pull_done', '');
      }
      await _setState('sync_schema', '11');
    }
    if (SupabaseConfig.isConfigured && await _getState('alias_backfill_done') != '1') {
      try {
        await _backfillSyncAliases();
        await _setState('alias_backfill_done', '1');
      } catch (_) {}
    }
  }

  static bool _lastGetOk = false;
  static bool _sbReady = false;

  static Future<bool> _ensureSb() async {
    if (_sbReady) return true;
    await SupabaseConfig.load();
    if (!SupabaseConfig.isConfigured) return false;
    try {
      if (Supabase.instance.isInitialized) {
        _sbReady = true;
        return true;
      }
    } catch (_) {}
    try {
      await Supabase.initialize(
        url: SupabaseConfig.url,
        anonKey: SupabaseConfig.anonKey,
      );
      _sbReady = true;
      return true;
    } catch (e) {
      try {
        if (Supabase.instance.isInitialized) {
          _sbReady = true;
          return true;
        }
      } catch (_) {}
      debugPrint('HubSync supabase init: $e');
      return false;
    }
  }

  static Future<void> start() async {
    await SupabaseConfig.load();
    await _ensureSb();
    await ensureLocalSchema();
    _timer ??= Timer.periodic(const Duration(milliseconds: 400), (_) {
      unawaited(tick());
    });
    // App start pe pura DB push mat karo — woh pehli pulls ko minutes rok deta tha.
    unawaited(tick());
  }

  static Future<void> tick() async {
    if (suppressCloudIo) return;
    if (_manualHold > 0) return;
    if (_inFlight != null) {
      // Busy tick miss mat karo — khatam hote hi dubara chalo (realtime)
      _tickAgain = true;
      return;
    }
    _inFlight = _tickBody();
    try {
      await _inFlight;
    } catch (_) {
    } finally {
      _inFlight = null;
    }
    if (_tickAgain) {
      _tickAgain = false;
      unawaited(tick());
    }
  }

  static Future<void> _tickBody() async {
    try {
      if (!await _ensureSb()) {
        debugPrint('HubSync: Supabase not ready');
        return;
      }
      await _healSyncFlagsIfLocalReady();
      await _ensureMissingSyncIds();
      // Outbox drain pehle — local saves foran cloud pe
      await _drainOutbox();
      await _processPendingApplyQueue();
      if (await _needsFullPull()) {
        await _fullPullAllTables();
      } else {
        // Cursor pehle (tez) — sync_id merge only, wipe/dup nahi
        await _pullLiveCursor();
        // Deletes har tick (light) — SoftMark signal ~1s
        await _pullRecentDeletesFor(_liveTables, 180);
        await _processPendingApplyQueue();
        _bump();

        if (await _canPush()) {
          await _pushPendingDeletes();
          await _drainOutbox();
          final pushed = await _pushChanged();
          if (pushed) {
            _dirty = false;
            _lastHashPush = DateTime.now();
          }
        }

        // Live heal + alloc catch-up ~2s (har tick heavy heal tick ko minutes late karti thi)
        if (DateTime.now().difference(_lastAllocCatchUp).inSeconds >= 2) {
          await _pullRecentHealFor(_liveTables, 120);
          await _pullRecentDeletesFor(_liveTables, 6 * 3600);
          await _catchUpRecentAllocates(180);
          await _processPendingApplyQueue();
          _lastAllocCatchUp = DateTime.now();
          _bump();
        }

        // Full incremental rare — har 3s pe 19 tables tick ko atka deti thi
        if (DateTime.now().difference(_lastFullIncremental).inSeconds >= 45) {
          await _pullIncremental();
          _lastFullIncremental = DateTime.now();
          _bump();
        }

        if (DateTime.now().difference(_lastDeepHeal).inSeconds >= 180) {
          await _pullRecentHealFor(_hotTables, 120);
          _lastDeepHeal = DateTime.now();
          _bump();
        }
        if (DateTime.now().difference(_lastPrune).inSeconds >= 300) {
          await _pruneGoneFromCloud(force: true);
          _lastPrune = DateTime.now();
          _bump();
        }
      }
      if (DateTime.now().difference(_lastUserMirror).inSeconds >= 300) {
        await _mirrorUsersFromCloud();
        _lastUserMirror = DateTime.now();
      }
      if (await _getState('restore_push_first') == '1') {
        await _setState('restore_push_first', '');
      }
      final stale = await _looksStaleLocal();
      if (stale &&
          DateTime.now().difference(_lastForceRecoverBg).inSeconds >= 300) {
        debugPrint('HubSync stale local → force recover missing (background)');
        await forceRecoverMissingFromCloud();
        try {
          await reconcileOneActiveCustomerPlots();
        } catch (_) {}
        _lastForceRecoverBg = DateTime.now();
        _bump();
      } else if (await _getState('first_pull_done') == '1' &&
          DateTime.now().difference(_lastCatchUp).inSeconds >= 300) {
        await _catchUpMissing();
        _lastCatchUp = DateTime.now();
      }
    } catch (e, st) {
      debugPrint('HubSync tick: $e\n$st');
    }
  }

  /// Per-table (updated_at, sync_id) cursor — same timestamp pe pehli 80 stuck nahi.
  /// Cursor sirf apply / durable pending_apply ke baad advance.
  static Future<void> _pullLiveCursor() async {
    var anyFail = false;
    for (final table in _liveTables) {
      final key = 'live_cur_$table';
      var raw = (await _getState(key)).trim();
      if (raw.isEmpty) {
        raw =
            '${_utcFilter(DateTime.now().toUtc().subtract(const Duration(days: 2)))}|';
      }
      final parts = raw.split('|');
      final curTs = _utcFilterIso(parts.isNotEmpty ? parts[0] : '');
      final curId = parts.length > 1 ? parts[1] : '';
      final got = await _sbSelectPaged(
        _cloudTable(table),
        afterUpdatedAt: curTs,
        afterSyncId: curId,
        limit: 80,
        includeDeleted: true,
      );
      if (!got.ok) {
        anyFail = true;
        continue; // is table ka cursor aage mat badhao
      }
      if (got.rows.isEmpty) continue;
      final ready = await _preparePulledRows(table, got.rows);
      // Parent-miss → sync_pending_apply (durable). Ready rows pehle apply.
      if (ready.isNotEmpty) {
        try {
          await _applyFetched(_asApplyRows(table, ready));
        } catch (e) {
          anyFail = true;
          debugPrint('HubSync live apply $table: $e');
          continue; // apply fail → cursor mat advance
        }
      }
      final last = got.rows.last;
      final lastTs = _utcFilterIso((last['updated_at'] ?? '').toString());
      final lastId = (last['sync_id'] ?? '').toString().trim();
      if (lastTs.isNotEmpty && lastId.isNotEmpty) {
        await _setState(key, '$lastTs|$lastId');
      }
    }
    if (!anyFail) await _processPendingApplyQueue();
  }

  /// Sirf is_deleted=true (last N sec) — allocate/receipt delete ~1s mein.
  static Future<void> _pullRecentDeletesFor(
    List<String> tables,
    int windowSec,
  ) async {
    final sec = windowSec < 30 ? 30 : windowSec;
    final since = _utcFilter(
      DateTime.now().toUtc().subtract(Duration(seconds: sec)),
    );
    final bag = <Map<String, dynamic>>[];
    final fetched = await Future.wait(tables.map((table) async {
      final cloud = _cloudTable(table);
      final all = <Map<String, dynamic>>[];
      for (var page = 0; page < 3; page++) {
        final from = page * 200;
        final got = await _sbSelect(
          cloud,
          from: from,
          to: from + 199,
          updatedAfter: since,
          deletedOnly: true,
          orderBy: 'updated_at',
        );
        if (!got.ok || got.rows.isEmpty) break;
        all.addAll(got.rows);
        if (got.rows.length < 200) break;
      }
      return (table: table, rows: all);
    }));
    for (final item in fetched) {
      if (item.rows.isEmpty) continue;
      bag.addAll(_asApplyRows(item.table, item.rows));
    }
    if (bag.isNotEmpty) await _applyFetched(bag);
  }

  /// Sirf apni save/delete — pura DB upload band (tick block = minute+ lag).
  static Future<bool> _canPush() async {
    if (!SupabaseConfig.isConfigured) return false;
    if (_dirty) return true;
    return await _hasPendingDeletes();
  }

  static Future<bool> _hasPendingDeletes() async {
    try {
      final db = await AppDatabase.instance.database;
      final rows = await db.rawQuery(
        'SELECT 1 FROM sync_pending_delete LIMIT 1',
      );
      return rows.isNotEmpty;
    } catch (_) {
      return false;
    }
  }

  /// Cloud users are the login source. One username → one local row.
  static Future<void> _mirrorUsersFromCloud() async {
    try {
      final got = await _sbSelect(
        'users',
        from: 0,
        to: 499,
        includeDeleted: false,
      );
      if (!got.ok) return;
      // Empty / failed cloud = local login wipe mat karo (WinForms early return).
      if (got.rows.isEmpty) return;
      await _applyFetched(
        _asApplyRows('users', got.rows),
        notify: false,
        bump: false,
      );
      final live = <String>{};
      for (final r in got.rows) {
        final un = (r['username'] ?? '').toString().trim().toLowerCase();
        if (un.isNotEmpty) live.add(un);
      }
      final db = await AppDatabase.instance.database;
      if (!await _tableExists(db, 'users')) return;
      final local = await db.query('users');
      for (final r in local) {
        final un = (r['username'] ?? '').toString().trim().toLowerCase();
        final sid = (r['sync_id'] ?? '').toString().trim();
        if (un.isEmpty) continue;
        if (live.contains(un)) continue;
        if (sid.isEmpty) continue;
        if (AuthStore.isBuiltInAdminUsername(un)) continue;
        await db.delete('users', where: 'userID = ?', whereArgs: [r['userID']]);
      }
    } catch (e) {
      debugPrint('HubSync mirror users: $e');
    }
    try {
      await AppDatabase.ensureDefaultAdmin();
    } catch (_) {}
  }

  /// Cloud duplicate sync_ids → local PK (WinForms BackfillSyncAliases).
  static Future<void> _backfillSyncAliases() async {
    if (!await _ensureSb()) return;
    final db = await AppDatabase.instance.database;
    await _aliasCloudTable(
      db,
      'customers',
      'customers',
      (row) async {
        final n = (row['name'] ?? '').toString().trim().toLowerCase();
        final m = (row['mobile'] ?? '').toString();
        final c = (row['cnic'] ?? '').toString();
        final found = await db.query(
          'customers',
          columns: ['id'],
          where:
              "lower(trim(ifnull(name,''))) = ? AND ifnull(mobile,'') = ? AND ifnull(cnic,'') = ?",
          whereArgs: [n, m, c],
          limit: 1,
        );
        if (found.isEmpty) return null;
        return int.tryParse(found.first['id']?.toString() ?? '');
      },
    );
    await _aliasCloudTable(
      db,
      'brokers',
      'brokers',
      (row) async {
        final n = (row['name'] ?? '').toString().trim().toLowerCase();
        final p = (row['phone'] ?? '').toString();
        final found = await db.query(
          'brokers',
          columns: ['id'],
          where:
              "lower(trim(ifnull(name,''))) = ? AND ifnull(phone,'') = ?",
          whereArgs: [n, p],
          limit: 1,
        );
        if (found.isEmpty) return null;
        return int.tryParse(found.first['id']?.toString() ?? '');
      },
    );
    await _aliasCloudTable(
      db,
      'customer_plots',
      'customer_plots',
      (row) async {
        final cs = (row['customer_sync_id'] ?? '').toString();
        final ps = (row['plot_sync_id'] ?? '').toString();
        final cust = await _localIdBySync(db, 'customers', cs);
        final plot = await _localIdBySync(db, 'plots', ps);
        if (cust == null || plot == null) return null;
        // Sirf empty sync_id — alag sync_id wali rows ko alias se merge mat karo
        final found = await db.query(
          'customer_plots',
          columns: ['id'],
          where: "customer_id = ? AND plot_id = ? AND ifnull(sync_id,'') = ''",
          whereArgs: [cust, plot],
          limit: 1,
        );
        if (found.isEmpty) return null;
        return int.tryParse(found.first['id']?.toString() ?? '');
      },
    );
  }

  static Future<void> _aliasCloudTable(
    DatabaseExecutor db,
    String cloudTable,
    String localTable,
    Future<int?> Function(Map<String, dynamic> row) findLocalPk,
  ) async {
    if (!await _tableExists(db, localTable)) return;
    for (var page = 0; page < 20; page++) {
      final got = await _sbSelect(
        cloudTable,
        from: page * 500,
        to: page * 500 + 499,
      );
      if (!got.ok || got.rows.isEmpty) break;
      for (final row in got.rows) {
        final cloudSync = (row['sync_id'] ?? '').toString().trim();
        if (cloudSync.isEmpty) continue;
        final localPk = await findLocalPk(row);
        if (localPk == null || localPk <= 0) continue;
        final bySync = await _localIdBySync(db, localTable, cloudSync);
        if (bySync == localPk) continue;
        await saveSyncAlias(db, localTable, cloudSync, localPk);
      }
      if (got.rows.length < 500) break;
    }
  }

  static Future<void> _dedupeLocalUser(
    DatabaseExecutor db,
    String username,
    String keepSyncId,
  ) async {
    final un = username.trim();
    if (un.isEmpty) return;
    if (AuthStore.isBuiltInAdminUsername(un)) return;
    try {
      await db.delete(
        'users',
        where:
            "lower(trim(ifnull(username,''))) = lower(trim(?)) AND ifnull(sync_id,'') <> ?",
        whereArgs: [un, keepSyncId],
      );
    } catch (_) {}
  }

  static Future<int> _localRowCount(String table) async {
    try {
      final db = await AppDatabase.instance.database;
      if (!await _tableExists(db, table)) return 0;
      final rows = await db.rawQuery('SELECT COUNT(*) AS c FROM "$table"');
      return int.tryParse((rows.first['c'] ?? '0').toString()) ?? 0;
    } catch (_) {
      return 0;
    }
  }

  static Future<int> _cloudRowCount(String table) async {
    final got = await _sbSelect(
      _cloudTable(table),
      from: 0,
      to: 0,
      columns: 'sync_id',
    );
    if (!got.ok) return -1;
    return got.rows.isEmpty ? 0 : 1;
  }

  static Future<void> _ensureMissingSyncIds() async {
    try {
      final db = await AppDatabase.instance.database;
      var changed = 0;
      for (final table in _liveTables) {
        if (!await _tableExists(db, table)) continue;
        final n = await db.rawUpdate(
          "UPDATE \"$table\" SET sync_id = lower(hex(randomblob(16))) WHERE sync_id IS NULL OR TRIM(sync_id)=''",
        );
        if (n > 0) {
          changed += n;
          markDirtyTable(table);
        }
      }
      if (changed > 0) markDirty();
    } catch (_) {}
  }

  static Future<bool> _isLastPullStale(int minutes) async {
    try {
      final lp = (await _getState('last_pull_utc')).trim();
      if (lp.isEmpty) return true;
      final t = DateTime.tryParse(lp);
      if (t == null) return true;
      return DateTime.now().toUtc().difference(t.toUtc()).inMinutes > minutes;
    } catch (_) {
      return true;
    }
  }

  /// Agar local pe pehle se data hai lekin flags wipe ho gaye — incremental pe lao (full-pull stuck band).
  static Future<void> _healSyncFlagsIfLocalReady() async {
    if (await _getState('first_pull_done') == '1') return;
    final n = await _localRowCount('customer_plots');
    final c = await _localRowCount('customers');
    if (n + c < 20) return;
    await _setState('first_pull_done', '1');
    if ((await _getState('last_pull_utc')).trim().isEmpty) {
      // 2 min — 6h backlog tick ko minutes laga deta tha
      await _setState(
        'last_pull_utc',
        DateTime.now()
            .toUtc()
            .subtract(const Duration(minutes: 2))
            .toIso8601String(),
      );
    }
    debugPrint('HubSync heal flags: local ready alloc=$n customers=$c');
  }

  /// WinForms NeedsFullPull — sirf pehli baar. Empty allocate → Force Recover missing, not wipe-pull.
  static Future<bool> _needsFullPull() async {
    await _healSyncFlagsIfLocalReady();
    return await _getState('first_pull_done') != '1';
  }

  /// Old/stale local (wipe risk / missing cloud rows): insert-missing Force Recover path.
  static Future<bool> _looksStaleLocal() async {
    await _healSyncFlagsIfLocalReady();
    if (await _getState('first_pull_done') != '1') return true;
    if (await _getState('sync_schema') != '10') return true;
    final last = (await _getState('last_pull_utc')).trim();
    if (last.isEmpty) return true;
    final dt = DateTime.tryParse(last);
    if (dt != null &&
        DateTime.now().toUtc().difference(dt.toUtc()).inDays >= 14) {
      return true;
    }
    for (final t in const ['customer_plots', 'customers', 'receipts']) {
      if (await _localRowCount(t) != 0) continue;
      final cloud = await _cloudRowCount(t);
      if (cloud > 0) return true;
    }
    return false;
  }

  static Future<void> _resetPullState() async {
    await _setState('first_pull_done', '');
    await _setState('last_pull_utc', '');
    await _setState('push_hashes_ready', '');
    try {
      final db = await AppDatabase.instance.database;
      await db.delete('sync_hash');
      await db.delete('sync_known');
    } catch (_) {}
  }

  static Future<void> _fullPullAllTables() async {
    var maxTs = '1970-01-01T00:00:00.000Z';
    var anyOk = false;
    var anyFail = false;
    final fetched = await Future.wait(_tables.map((table) async {
      final cloud = _cloudTable(table);
      final all = <Map<String, dynamic>>[];
      var ok = true;
      for (var page = 0; page < 60; page++) {
        final from = page * 500;
        final got = await _sbSelect(
          cloud,
          from: from,
          to: from + 499,
          includeDeleted: true, // tombstones bhi — delete miss na ho
        );
        if (!got.ok) {
          ok = false;
          break;
        }
        if (got.rows.isEmpty) break;
        all.addAll(got.rows);
        if (got.rows.length < 500) break;
      }
      return (table: table, rows: all, ok: ok);
    }));
    final bag = <Map<String, dynamic>>[];
    for (final item in fetched) {
      if (!item.ok) {
        anyFail = true;
      } else {
        anyOk = true;
      }
      if (item.rows.isEmpty) continue;
      for (final row in item.rows) {
        final ts = (row['updated_at'] ?? '').toString();
        if (ts.compareTo(maxTs) > 0) maxTs = ts;
      }
      final ready = await _preparePulledRows(item.table, item.rows);
      if (ready.isEmpty) continue;
      bag.addAll(_asApplyRows(item.table, ready));
    }
    if (bag.isNotEmpty) {
      await _applyFetched(bag, notify: false, bump: false);
    }
    if (maxTs == '1970-01-01T00:00:00.000Z') {
      maxTs = DateTime.now().toUtc().toIso8601String();
    }
    // Partial fail pe bhi stuck forever mat raho (warna prune/incremental band)
    if (anyOk) {
      await _setState('last_pull_utc', maxTs);
      await _setState('first_pull_done', '1');
    }
    _bump();
    debugPrint(
      'HubSync full pull ok=$anyOk fail=$anyFail getOk=$_lastGetOk '
      'customers=${await _localRowCount('customers')} '
      'plots=${await _localRowCount('plots')} '
      'alloc=${await _localRowCount('customer_plots')} '
      'receipts=${await _localRowCount('receipts')}',
    );
  }

  static String _base() =>
      SupabaseConfig.url.replaceAll(RegExp(r'/$'), '');

  /// Cloud pe already soft-deleted sync_ids — live upsert mat bhejo (delete overwrite band).
  static Future<Set<String>> _fetchCloudDeletedSyncIds(
    String cloud,
    List<String> syncIds,
  ) async {
    final deleted = <String>{};
    if (syncIds.isEmpty) return deleted;
    if (!await _ensureSb()) return deleted;
    for (var i = 0; i < syncIds.length; i += 40) {
      final chunk = syncIds.sublist(i, min(i + 40, syncIds.length));
      try {
        final got = await Supabase.instance.client
            .from(cloud)
            .select('sync_id')
            .eq('is_deleted', true)
            .inFilter('sync_id', chunk)
            .limit(100);
        for (final r in (got as List).whereType<Map>()) {
          final id = (r['sync_id'] ?? '').toString().trim();
          if (id.isNotEmpty) deleted.add(id);
        }
      } catch (e) {
        debugPrint('HubSync fetch deleted $cloud $e');
      }
    }
    return deleted;
  }

  static Future<void> _deleteLocalBySyncIds(
    String cloud,
    Set<String> syncIds,
  ) async {
    if (syncIds.isEmpty) return;
    String? local;
    for (final t in _tables) {
      if (_cloudTable(t) == cloud) {
        local = t;
        break;
      }
    }
    if (local == null) return;
    final db = await AppDatabase.instance.database;
    await AppDatabase.suspendSyncTriggers(db);
    try {
      await db.execute('PRAGMA foreign_keys = OFF');
      for (final sid in syncIds) {
        await db.delete(local, where: 'sync_id = ?', whereArgs: [sid]);
        await db.delete('sync_known', where: 'sync_id = ?', whereArgs: [sid]);
        await db.delete('sync_hash', where: 'sync_id = ?', whereArgs: [sid]);
      }
    } finally {
      await db.execute('PRAGMA foreign_keys = ON');
      await AppDatabase.restoreSyncTriggers(db);
    }
    _bump();
  }

  static Future<Set<String>> _httpUpsertIds(
    String cloud,
    List<Map<String, Object?>> rows,
  ) async {
    final okIds = <String>{};
    if (rows.isEmpty) return okIds;
    if (!await _ensureSb()) return okIds;
    final ids = rows
        .map((r) => (r['sync_id'] ?? '').toString().trim())
        .where((s) => s.isNotEmpty)
        .toList();
    final cloudDeleted = await _fetchCloudDeletedSyncIds(cloud, ids);
    var liveRows = rows;
    if (cloudDeleted.isNotEmpty) {
      liveRows = rows
          .where((r) =>
              !cloudDeleted.contains((r['sync_id'] ?? '').toString().trim()))
          .toList();
      await _deleteLocalBySyncIds(cloud, cloudDeleted);
    }
    if (liveRows.isEmpty) return okIds;
    try {
      final payload =
          liveRows.map((r) => Map<String, dynamic>.from(r)).toList();
      await Supabase.instance.client.from(cloud).upsert(
            payload,
            onConflict: 'sync_id',
          );
      for (final r in liveRows) {
        final id = (r['sync_id'] ?? '').toString();
        if (id.isNotEmpty) okIds.add(id);
      }
      return okIds;
    } catch (e) {
      debugPrint('HubSync UPSERT $cloud $e');
      if (liveRows.length > 1) {
        for (final row in liveRows) {
          okIds.addAll(await _httpUpsertIds(cloud, [row]));
        }
      } else if (cloud == 'users' && liveRows.length == 1) {
        final sid = await _mergeUserIntoLive(liveRows.first);
        if (sid != null && sid.isNotEmpty) okIds.add(sid);
      }
    }
    return okIds;
  }

  static Future<String?> _mergeUserIntoLive(Map<String, Object?> row) async {
    try {
      final un = (row['username'] ?? '').toString().trim();
      if (un.isEmpty) return null;
      final got = await Supabase.instance.client
          .from('users')
          .select('sync_id')
          .eq('is_deleted', false)
          .ilike('username', un)
          .limit(1);
      final list = (got as List)
          .whereType<Map>()
          .map((e) => Map<String, dynamic>.from(e))
          .toList();
      if (list.isEmpty) return null;
      final liveSid = (list.first['sync_id'] ?? '').toString().trim();
      if (liveSid.isEmpty) return null;
      final merged = Map<String, Object?>.from(row);
      merged['sync_id'] = liveSid;
      merged['is_deleted'] = false;
      await Supabase.instance.client.from('users').upsert(
            [Map<String, dynamic>.from(merged)],
            onConflict: 'sync_id',
          );
      final db = await AppDatabase.instance.database;
      await db.update(
        'users',
        {'sync_id': liveSid},
        where: "lower(trim(ifnull(username,''))) = lower(trim(?))",
        whereArgs: [un],
      );
      return liveSid;
    } catch (e) {
      debugPrint('HubSync merge user: $e');
      return null;
    }
  }

  static Future<void> _seedHashesIfAlreadySynced() async {
    final last = await _getState('last_pull_utc');
    if (last.trim().isEmpty) return;
    if (await _getState('push_hashes_ready') == '1') return;
    final db = await AppDatabase.instance.database;
    for (final table in _tables) {
      if (!await _tableExists(db, table)) continue;
      final rows = await db.rawQuery('SELECT * FROM "$table"');
      for (final r in rows) {
        final payload = Map<String, Object?>.from(r);
        final syncId = (payload['sync_id'] ?? '').toString();
        if (syncId.trim().isEmpty) continue;
        await _saveHash(db, syncId, _rowHash(payload));
      }
    }
    await _setState('push_hashes_ready', '1');
  }

  /// true = dirty clear karo; false = baad mein safety push.
  static Future<bool> _pushChanged() async {
    final db = await AppDatabase.instance.database;
    final staff = (StaffAudit.displayName ?? StaffAudit.username ?? '').trim();
    final pendingDel = await _loadPendingDeleteSyncIds();
    final batch = <Map<String, Object?>>[];

    Future<void> flush() async {
      if (batch.isEmpty) return;
      final grouped = <String, List<Map<String, Object?>>>{};
      for (final row in batch) {
        final t = (row.remove('_cloud_table') ?? '').toString();
        if (t.isEmpty) continue;
        grouped.putIfAbsent(t, () => []).add(row);
      }
      for (final e in grouped.entries) {
        final hashes = <String, String>{};
        for (final row in e.value) {
          final sid = (row['sync_id'] ?? '').toString();
          final h = (row.remove('_row_hash') ?? '').toString();
          if (sid.isNotEmpty && h.isNotEmpty) hashes[sid] = h;
        }
        final ok = await _httpUpsertIds(e.key, e.value);
        for (final id in ok) {
          final h = hashes[id];
          if (h != null) await _saveHash(db, id, h);
        }
      }
      batch.clear();
    }

    late final List<String> scanTables;
    if (_dirtyTables.isNotEmpty) {
      scanTables = List<String>.from(_dirtyTables);
      _dirtyTables.clear();
    } else if (_dirty &&
        DateTime.now().difference(_lastSafetyPush).inSeconds >= 15) {
      // Rare safety — har tick LiveTables scan = WinForms→mobile lag
      scanTables = List<String>.from(_liveTables);
      _lastSafetyPush = DateTime.now();
    } else {
      return !_dirty;
    }

    final cloudUserSync = <String, String>{};
    if (scanTables.any((t) => t.toLowerCase() == 'users')) {
      try {
        final got = await _sbSelect('users', from: 0, to: 499);
        if (got.ok) {
          for (final r in got.rows) {
            final un = (r['username'] ?? '').toString().trim().toLowerCase();
            final sid = (r['sync_id'] ?? '').toString().trim();
            if (un.isNotEmpty && sid.isNotEmpty) cloudUserSync[un] = sid;
          }
        }
      } catch (_) {}
    }

    for (final table in scanTables) {
      if (!await _tableExists(db, table)) continue;
      await db.execute(
        "UPDATE \"$table\" SET sync_id = lower(hex(randomblob(16))) WHERE sync_id IS NULL OR TRIM(sync_id)=''",
      );
      final rows = await db.rawQuery('SELECT * FROM "$table"');
      // Empty local table → kuch upload mat karo (destructive empty overwrite avoid)
      if (rows.isEmpty) continue;
      for (final r in rows) {
        final payload = Map<String, Object?>.from(r);
        var syncId = (payload['sync_id'] ?? '').toString();
        if (syncId.trim().isEmpty) continue;
        // Pending SoftMark — live upsert se tombstone overwrite mat karo
        if (pendingDel.contains(syncId.trim().toLowerCase())) continue;
        if (table.toLowerCase() == 'users') {
          final un = (payload['username'] ?? '').toString().trim().toLowerCase();
          final keep = un.isEmpty ? '' : (cloudUserSync[un] ?? '');
          if (keep.isNotEmpty && keep != syncId) {
            final old = syncId;
            await db.update(
              'users',
              {'sync_id': keep},
              where: 'sync_id = ?',
              whereArgs: [old],
            );
            payload['sync_id'] = keep;
            syncId = keep;
            unawaited(pushDeleted('users', old));
          }
        }
        final fks = _fks[table];
        if (fks != null) {
          for (final kv in fks.entries) {
            final fkVal = payload[kv.key];
            if (fkVal == null) continue;
            final parent = await _lookupSyncId(db, kv.value, fkVal);
            if (parent.isNotEmpty) payload['__fk_${kv.key}'] = parent;
          }
        }
        final hash = _rowHash(payload);
        if (await _loadHash(db, syncId) == hash) continue;

        final dupKey = _dupKey(table, payload);
        final cloudRow = _localToCloudRow(table, payload);
        if (fks != null) {
          for (final kv in fks.entries) {
            final fkVal = payload[kv.key];
            if (fkVal == null) continue;
            final parent = await _lookupSyncId(db, kv.value, fkVal);
            if (parent.isNotEmpty) cloudRow[_fkCloudCol(kv.key)] = parent;
          }
        }
        cloudRow['dup_key'] = dupKey;
        cloudRow['is_deleted'] = false;
        cloudRow['staff_name'] = staff;
        cloudRow['updated_at'] = _utcFilter(DateTime.now().toUtc());
        cloudRow['_cloud_table'] = _cloudTable(table);
        cloudRow['_row_hash'] = hash;
        batch.add(cloudRow);
        if (batch.length >= 40) await flush();
      }
    }
    await flush();
    await _setState('push_hashes_ready', '1');
    return true;
  }

  static String _utcFilterIso(String iso) {
    final dt = DateTime.tryParse(iso)?.toUtc();
    if (dt == null) return iso;
    return _utcFilter(dt);
  }

  static String _utcFilter(DateTime utc) {
    final u = utc.toUtc();
    final ms = u.millisecond.toString().padLeft(3, '0');
    return '${u.year.toString().padLeft(4, '0')}-'
        '${u.month.toString().padLeft(2, '0')}-'
        '${u.day.toString().padLeft(2, '0')}T'
        '${u.hour.toString().padLeft(2, '0')}:'
        '${u.minute.toString().padLeft(2, '0')}:'
        '${u.second.toString().padLeft(2, '0')}.'
        '${ms}Z';
  }

  /// Recent cloud allocates missing locally — light (full catch-up nahi).
  static Future<void> _catchUpRecentAllocates(int windowSec) async {
    final sec = windowSec < 60 ? 60 : windowSec;
    final since = _utcFilter(
      DateTime.now().toUtc().subtract(Duration(seconds: sec)),
    );
    final got = await _sbSelect(
      'customer_plots',
      from: 0,
      to: 199,
      updatedAfter: since,
      includeDeleted: false,
    );
    if (!got.ok || got.rows.isEmpty) return;
    final db = await AppDatabase.instance.database;
    final local = <String>{};
    try {
      final rows = await db.rawQuery(
        "SELECT lower(trim(sync_id)) AS s FROM customer_plots WHERE ifnull(sync_id,'') <> ''",
      );
      for (final r in rows) {
        final s = (r['s'] ?? '').toString();
        if (s.isNotEmpty) local.add(s);
      }
    } catch (_) {}
    final missing = <Map<String, dynamic>>[];
    for (final row in got.rows) {
      final id = (row['sync_id'] ?? '').toString().trim().toLowerCase();
      if (id.isEmpty || local.contains(id)) continue;
      missing.add(Map<String, dynamic>.from(row));
    }
    if (missing.isEmpty) return;
    final skip = <String>[];
    await _ensureRecoverParentsForCustomerPlots(missing, skip);
    if (missing.isNotEmpty) {
      await _applyFetched(_asApplyRows('customer_plots', missing));
    }
  }

  static Future<String> _lookupSyncId(Database db, String table, Object id) async {
    try {
      if (!await _tableExists(db, table)) return '';
      final pk = _pk(table);
      final rows = await db.rawQuery(
        'SELECT sync_id FROM "$table" WHERE "$pk" = ? LIMIT 1',
        [id],
      );
      if (rows.isEmpty) return '';
      return (rows.first['sync_id'] ?? '').toString();
    } catch (_) {
      return '';
    }
  }

  static Future<Set<String>> _loadPendingDeleteSyncIds() async {
    final ids = <String>{};
    try {
      final db = await AppDatabase.instance.database;
      final rows = await db.query('sync_pending_delete', columns: ['sync_id']);
      for (final r in rows) {
        final id = (r['sync_id'] ?? '').toString().trim().toLowerCase();
        if (id.isNotEmpty) ids.add(id);
      }
    } catch (_) {}
    return ids;
  }

  /// Insert-missing only (no wipe). Allocate rows: parent-ensure pehle (Force Recover jaisa).
  static Future<void> _catchUpMissing() async {
    try {
      final db = await AppDatabase.instance.database;
      // Stale content-dup aliases wipe union rows — clear union-safe tables
      await db.delete(
        'sync_alias',
        where:
            "lower(table_name) IN ('partner_money','receipts','customer_plots')",
      );
    } catch (_) {}
    final pendingDel = await _loadPendingDeleteSyncIds();
    for (final table in _tables) {
      final cloud = _cloudTable(table);
      final isAlloc = table == 'customer_plots';
      final db = await AppDatabase.instance.database;
      // Sirf isi table ke sync_id — global alias se missing skip na ho
      final local = await _localSyncIdsForTable(db, table);
      for (var page = 0; page < 60; page++) {
        final from = page * 1000;
        final got = await _sbSelect(
          cloud,
          from: from,
          to: from + 999,
          columns: 'sync_id',
          orderBy: 'sync_id',
        );
        if (!got.ok || got.rows.isEmpty) break;
        final missing = <String>[];
        for (final row in got.rows) {
          final id = (row['sync_id'] ?? '').toString().trim().toLowerCase();
          if (id.isEmpty) continue;
          if (pendingDel.contains(id)) continue; // local delete in-flight
          if (!local.contains(id)) missing.add(id);
        }
        for (var i = 0; i < missing.length; i += 30) {
          final end = (i + 30 < missing.length) ? i + 30 : missing.length;
          final chunk = missing.sublist(i, end);
          final fetched = await _sbSelect(
            cloud,
            from: 0,
            to: chunk.length + 5,
            syncIds: chunk,
          );
          if (fetched.rows.isEmpty) continue;
          final rows = fetched.rows
              .map((r) => Map<String, dynamic>.from(r))
              .toList();
          if (isAlloc) {
            final skipList = <String>[];
            await _ensureRecoverParentsForCustomerPlots(rows, skipList);
            if (skipList.isNotEmpty) {
              debugPrint('HubSync catch-up alloc skip: ${skipList.join('; ')}');
            }
          }
          if (rows.isEmpty) continue;
          await _applyFetched(
            _asApplyRows(table, rows),
            notify: false,
            bump: false,
          );
          for (final r in rows) {
            final id = (r['sync_id'] ?? '').toString().trim().toLowerCase();
            if (id.isNotEmpty) local.add(id);
          }
        }
        if (got.rows.length < 1000) break;
      }
    }
    _bump();
  }

  /// Catch-up ke liye: isi table ki rows ke sync_id only (alias missing hide nahi kare).
  static Future<Set<String>> _localSyncIdsForTable(
    DatabaseExecutor db,
    String table,
  ) async {
    final ids = <String>{};
    try {
      if (await _tableExists(db, table) &&
          await _hasColumn(db, table, 'sync_id')) {
        final rows = await db.rawQuery(
          'SELECT sync_id FROM "$table" WHERE sync_id IS NOT NULL AND TRIM(sync_id)<>\'\'',
        );
        for (final r in rows) {
          final id = (r['sync_id'] ?? '').toString().trim().toLowerCase();
          if (id.isNotEmpty) ids.add(id);
        }
      }
    } catch (_) {}
    return ids;
  }

  static Future<List<Map<String, dynamic>>> _preparePulledRows(
    String table,
    List<Map<String, dynamic>> raw,
  ) async {
    if (raw.isEmpty) return raw;
    // Tombstones parent-ensure se filter na hon — delete signal zaya na ho
    final deleted = <Map<String, dynamic>>[];
    final live = <Map<String, dynamic>>[];
    for (final r in raw) {
      final m = Map<String, dynamic>.from(r);
      if (_cloudRowIsDeleted(m)) {
        deleted.add(m);
      } else {
        live.add(m);
      }
    }
    if (live.isNotEmpty && table == 'customer_plots') {
      final skip = <String>[];
      await _ensureRecoverParentsForCustomerPlots(live, skip);
    } else if (live.isNotEmpty &&
        (table == 'receipts' || table == 'ledger')) {
      final skip = <String>[];
      await _ensureChildParentsThenFilter(table, live, skip);
    }
    return [...deleted, ...live];
  }

  /// Receipt/ledger: pehle parent allocate (+ uske parents), phir filter.
  /// Deleted allocation ko Active resurrect mat karo (late receipt race).
  static Future<void> _ensureChildParentsThenFilter(
    String table,
    List<Map<String, dynamic>> rows,
    List<String> skipList,
  ) async {
    final need = <String>{};
    for (final row in rows) {
      final cp = (row['customer_plot_sync_id'] ?? '').toString().trim();
      if (cp.isNotEmpty) need.add(cp);
    }
    final deletedParentIds = <String>{};
    if (need.isNotEmpty) {
      final got = await _sbSelect(
        'customer_plots',
        from: 0,
        to: need.length + 10,
        syncIds: need.toList(),
        includeDeleted: true,
      );
      final liveAllocs = <Map<String, dynamic>>[];
      final tombstones = <Map<String, dynamic>>[];
      for (final raw in got.rows) {
        final a = Map<String, dynamic>.from(raw);
        final sid = (a['sync_id'] ?? '').toString().trim();
        if (_cloudRowIsDeleted(a)) {
          if (sid.isNotEmpty) deletedParentIds.add(sid.toLowerCase());
          // Tombstone apply — Active list se hatao; is_deleted=false mat karo
          tombstones.add(a);
          continue;
        }
        liveAllocs.add(a);
      }
      if (tombstones.isNotEmpty) {
        await _applyFetched(
          _asApplyRows('customer_plots', tombstones),
          notify: false,
          bump: false,
        );
      }
      if (liveAllocs.isNotEmpty) {
        await _ensureRecoverParentsForCustomerPlots(liveAllocs, skipList);
        if (liveAllocs.isNotEmpty) {
          await _applyFetched(
            _asApplyRows('customer_plots', liveAllocs),
            notify: false,
            bump: false,
          );
        }
      }
    }
    // Late receipt + deleted parent: pending_apply mat banao (parent kabhi Active nahi hoga)
    if (deletedParentIds.isNotEmpty) {
      final keep = <Map<String, dynamic>>[];
      for (final row in rows) {
        final cp = (row['customer_plot_sync_id'] ?? '').toString().trim().toLowerCase();
        final id = (row['sync_id'] ?? '').toString();
        final short = id.length >= 8 ? id.substring(0, 8) : id;
        if (cp.isNotEmpty && deletedParentIds.contains(cp)) {
          skipList.add('$short: skip — parent allocate is_deleted (no resurrect)');
          continue;
        }
        keep.add(row);
      }
      rows
        ..clear()
        ..addAll(keep);
    }
    await _filterRecoverChildrenToExistingAllocs(table, rows, skipList);
  }

  /// last_pull `gt` ke liye — fail row dubara aaye (entries miss band).
  static String _justBeforeUtc(String iso) {
    final dt = DateTime.tryParse(iso)?.toUtc();
    if (dt == null) return iso;
    return dt.subtract(const Duration(milliseconds: 1)).toIso8601String();
  }

  static Future<void> _pullIncremental() async {
    var sinceRaw = await _getState('last_pull_utc');
    if (sinceRaw.trim().isEmpty) sinceRaw = '1970-01-01T00:00:00.000Z';
    final since = _utcFilterIso(sinceRaw);
    var maxApplied = since;
    String? minDropped;
    var anyFetchFail = false;
    var anyApplyFail = false;
    final bag = <Map<String, dynamic>>[];
    final fetched = await Future.wait(_tables.map((table) async {
      final cloud = _cloudTable(table);
      final all = <Map<String, dynamic>>[];
      var fail = false;
      for (var page = 0; page < 40; page++) {
        final from = page * 200;
        final got = await _sbSelect(
          cloud,
          from: from,
          to: from + 199,
          updatedAfter: since,
          includeDeleted: true,
        );
        if (!got.ok) {
          fail = true;
          break;
        }
        if (got.rows.isEmpty) break;
        all.addAll(got.rows);
        if (got.rows.length < 200) break;
      }
      return (table: table, rows: all, fail: fail);
    }));
    for (final item in fetched) {
      if (item.fail) {
        anyFetchFail = true;
        break; // doosri table success se failed table cursor aage na badhe
      }
      if (item.rows.isEmpty) continue;
      final rawLiveIds = <String>{};
      for (final row in item.rows) {
        if (_cloudRowIsDeleted(row)) continue;
        final id = (row['sync_id'] ?? '').toString().trim();
        if (id.isNotEmpty) rawLiveIds.add(id);
      }
      final ready = await _preparePulledRows(item.table, item.rows);
      final readyIds = <String>{};
      for (final row in ready) {
        final id = (row['sync_id'] ?? '').toString().trim();
        if (id.isNotEmpty) readyIds.add(id);
        final ts = (row['updated_at'] ?? '').toString();
        if (ts.compareTo(maxApplied) > 0) maxApplied = ts;
      }
      // Parent miss → prepare drop: last_pull aage mat badhao warna allocate forever miss
      for (final row in item.rows) {
        if (_cloudRowIsDeleted(row)) continue;
        final id = (row['sync_id'] ?? '').toString().trim();
        if (id.isEmpty || readyIds.contains(id)) continue;
        if (!rawLiveIds.contains(id)) continue;
        final ts = (row['updated_at'] ?? '').toString();
        if (ts.isEmpty) continue;
        if (minDropped == null || ts.compareTo(minDropped!) < 0) {
          minDropped = ts;
        }
      }
      if (ready.isEmpty) continue;
      bag.addAll(_asApplyRows(item.table, ready));
    }
    try {
      if (bag.isNotEmpty) await _applyFetched(bag);
    } catch (_) {
      anyApplyFail = true;
    }
    if (anyFetchFail || anyApplyFail) return;
    var advance = maxApplied;
    final dropped = minDropped;
    if (dropped != null) {
      final cap = _justBeforeUtc(dropped);
      if (advance.compareTo(cap) > 0) advance = cap;
    }
    if (advance.compareTo(since) > 0) await _setState('last_pull_utc', advance);
    await _processPendingApplyQueue();
  }

  /// Recent window re-apply (incl. deletes). Does NOT advance last_pull —
  /// heals missed update/delete; same sync_id = update only (no dup/wipe).
  static Future<void> _pullRecentHeal(int windowSec) async {
    await _pullRecentHealFor(_tables, windowSec);
  }

  static Future<void> _pullRecentHealFor(
    List<String> tables,
    int windowSec,
  ) async {
    final sec = windowSec < 15 ? 15 : windowSec;
    final since = _utcFilter(
      DateTime.now().toUtc().subtract(Duration(seconds: sec)),
    );
    final bag = <Map<String, dynamic>>[];
    final fetched = await Future.wait(tables.map((table) async {
      final cloud = _cloudTable(table);
      final all = <Map<String, dynamic>>[];
      for (var page = 0; page < 5; page++) {
        final from = page * 200;
        final got = await _sbSelect(
          cloud,
          from: from,
          to: from + 199,
          updatedAfter: since,
          includeDeleted: true,
        );
        if (!got.ok || got.rows.isEmpty) break;
        all.addAll(got.rows);
        if (got.rows.length < 200) break;
      }
      return (table: table, rows: all);
    }));
    for (final item in fetched) {
      if (item.rows.isEmpty) continue;
      final ready = await _preparePulledRows(item.table, item.rows);
      if (ready.isEmpty) continue;
      bag.addAll(_asApplyRows(item.table, ready));
    }
    if (bag.isNotEmpty) {
      await _applyFetched(bag);
    }
  }

  static Future<({bool ok, List<Map<String, dynamic>> rows})> _sbSelect(
    String cloud, {
    required int from,
    required int to,
    String columns = '*',
    String orderBy = 'updated_at',
    String? updatedAfter,
    List<String>? syncIds,
    bool includeDeleted = false,
    bool deletedOnly = false,
  }) async {
    try {
      if (!await _ensureSb()) {
        return (ok: false, rows: <Map<String, dynamic>>[]);
      }
      var q = Supabase.instance.client.from(cloud).select(columns);
      // deletedOnly = prune confirm path only; default sync/pull still is_deleted=false
      if (deletedOnly) {
        q = q.eq('is_deleted', true);
      } else if (!includeDeleted) {
        q = q.eq('is_deleted', false);
      }
      if (updatedAfter != null && updatedAfter.isNotEmpty) {
        q = q.gt('updated_at', updatedAfter);
      }
      if (syncIds != null && syncIds.isNotEmpty) {
        q = q.inFilter('sync_id', syncIds);
      }
      final data = await q.order(orderBy).range(from, to);
      _lastGetOk = true;
      final list = (data as List)
          .whereType<Map>()
          .map((e) => Map<String, dynamic>.from(e))
          .toList();
      return (ok: true, rows: list);
    } catch (e) {
      _lastGetOk = false;
      debugPrint('HubSync GET $cloud $e');
      return (ok: false, rows: <Map<String, dynamic>>[]);
    }
  }

  /// Stable keyset pagination — same updated_at pe stuck nahi.
  static Future<({bool ok, List<Map<String, dynamic>> rows})> _sbSelectPaged(
    String cloud, {
    required String afterUpdatedAt,
    required String afterSyncId,
    required int limit,
    bool includeDeleted = false,
  }) async {
    try {
      if (!await _ensureSb()) {
        return (ok: false, rows: <Map<String, dynamic>>[]);
      }
      final ts = afterUpdatedAt.trim();
      final sid = afterSyncId.trim();
      var q = Supabase.instance.client.from(cloud).select('*');
      if (!includeDeleted) q = q.eq('is_deleted', false);
      if (ts.isEmpty) {
        // bootstrap — nothing
      } else if (sid.isEmpty) {
        q = q.gt('updated_at', ts);
      } else {
        q = q.or(
          'updated_at.gt.$ts,and(updated_at.eq.$ts,sync_id.gt.$sid)',
        );
      }
      final data = await q
          .order('updated_at', ascending: true)
          .order('sync_id', ascending: true)
          .limit(limit);
      _lastGetOk = true;
      final list = (data as List)
          .whereType<Map>()
          .map((e) => Map<String, dynamic>.from(e))
          .toList();
      return (ok: true, rows: list);
    } catch (e) {
      _lastGetOk = false;
      debugPrint('HubSync GET paged $cloud $e');
      return (ok: false, rows: <Map<String, dynamic>>[]);
    }
  }

  static Future<void> _savePendingApply(
    String table,
    Map<String, dynamic> cloudRow, {
    String? waitParentTable,
    String? waitParentSyncId,
  }) async {
    final sid = (cloudRow['sync_id'] ?? '').toString().trim();
    if (sid.isEmpty || table.isEmpty) return;
    try {
      final db = await AppDatabase.instance.database;
      await db.insert(
        'sync_pending_apply',
        {
          'sync_id': sid,
          'table_name': table,
          'payload_json': jsonEncode(cloudRow),
          'wait_parent_table': waitParentTable ?? '',
          'wait_parent_sync_id': waitParentSyncId ?? '',
          'cloud_updated_at': (cloudRow['updated_at'] ?? '').toString(),
        },
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    } catch (_) {}
  }

  static Future<void> _processPendingApplyQueue() async {
    try {
      final db = await AppDatabase.instance.database;
      final rows = await db.query('sync_pending_apply', limit: 80);
      for (final r in rows) {
        final table = (r['table_name'] ?? '').toString();
        final sid = (r['sync_id'] ?? '').toString().trim();
        final waitT = (r['wait_parent_table'] ?? '').toString().trim();
        final waitS = (r['wait_parent_sync_id'] ?? '').toString().trim();
        if (waitT.isNotEmpty && waitS.isNotEmpty) {
          if (await _localIdBySync(db, waitT, waitS) == null) continue;
        }
        Map<String, dynamic> payload;
        try {
          final decoded = jsonDecode((r['payload_json'] ?? '{}').toString());
          if (decoded is! Map) continue;
          payload = Map<String, dynamic>.from(decoded);
        } catch (_) {
          continue;
        }
        payload['sync_id'] = sid;
        final list = [payload];
        final skip = <String>[];
        if (table == 'customer_plots') {
          await _ensureRecoverParentsForCustomerPlots(list, skip);
        } else if (table == 'receipts' || table == 'ledger') {
          await _ensureChildParentsThenFilter(table, list, skip);
        }
        if (list.isEmpty) continue;
        await _applyFetched(_asApplyRows(table, list));
        await db.delete(
          'sync_pending_apply',
          where: 'sync_id = ?',
          whereArgs: [sid],
        );
      }
    } catch (e) {
      debugPrint('HubSync pending apply: $e');
    }
  }

  static Future<Set<String>> _localSyncIds(DatabaseExecutor db) async {
    final ids = <String>{};
    for (final t in _tables) {
      if (!await _tableExists(db, t)) continue;
      final rows = await db.rawQuery(
        'SELECT sync_id FROM "$t" WHERE sync_id IS NOT NULL AND TRIM(sync_id)<>\'\'',
      );
      for (final r in rows) {
        final id = (r['sync_id'] ?? '').toString().trim().toLowerCase();
        if (id.isNotEmpty) ids.add(id);
      }
    }
    try {
      final aliases = await db.query('sync_alias', columns: ['sync_id']);
      for (final r in aliases) {
        final id = (r['sync_id'] ?? '').toString().trim().toLowerCase();
        if (id.isNotEmpty) ids.add(id);
      }
    } catch (_) {}
    return ids;
  }

  /// Prune: sirf cloud-confirmed is_deleted=true. Absence se kabhi local delete nahi.
  /// Soft-delete cleanup = ApplyRow / ye confirm path. Sync/pull logic alag, untouched.
  static Future<void> _pruneGoneFromCloud({bool force = false}) async {
    // force=false: max 2s; tick pe force → turant delete sync
    if (!force && DateTime.now().difference(_lastPrune).inSeconds < 2) return;
    _lastPrune = DateTime.now();
    final pending = <String, List<String>>{};

    // Recent tombstones har tick (fast); full scan ~30s (purani deletes)
    final fullScan =
        DateTime.now().difference(_lastFullPrune).inSeconds >= 30;
    if (fullScan) _lastFullPrune = DateTime.now();
    final since = fullScan
        ? null
        : DateTime.now()
            .toUtc()
            .subtract(const Duration(minutes: 10))
            .toIso8601String();

    // Cloud se sirf soft-deleted sync_ids (positive confirm) — live absence ignore
    final deletedByTable = <String, Set<String>>{};
    await Future.wait(_tables.map((table) async {
      final cloud = _cloudTable(table);
      final deleted = <String>{};
      var failed = false;
      final pageLimit = fullScan ? 80 : 5;
      for (var page = 0; page < pageLimit; page++) {
        final from = page * 1000;
        final got = await _sbSelect(
          cloud,
          from: from,
          to: from + 999,
          columns: 'sync_id',
          orderBy: since == null ? 'sync_id' : 'updated_at',
          updatedAfter: since,
          deletedOnly: true,
        );
        if (!got.ok) {
          failed = true;
          break;
        }
        if (got.rows.isEmpty) break;
        for (final row in got.rows) {
          final id = (row['sync_id'] ?? '').toString().trim();
          if (id.isNotEmpty) deleted.add(id);
        }
        if (got.rows.length < 1000) break;
      }
      if (!failed && deleted.isNotEmpty) deletedByTable[table] = deleted;
    }));

    final db = await AppDatabase.instance.database;
    for (final table in _tables.reversed) {
      final confirmed = deletedByTable[table];
      if (confirmed == null || confirmed.isEmpty) continue;
      if (!await _tableExists(db, table)) continue;
      if (!await _hasColumn(db, table, 'sync_id')) continue;

      final localGone = <String>[];
      for (final sid in confirmed) {
        final hit = await db.rawQuery(
          'SELECT 1 AS x FROM "$table" WHERE sync_id = ? LIMIT 1',
          [sid],
        );
        if (hit.isEmpty) continue;
        localGone.add(sid);
      }
      if (localGone.isEmpty) continue;
      pending[table] = localGone;
    }
    if (pending.isEmpty) return;
    await AppDatabase.suspendSyncTriggers(db);
    try {
      await db.execute('PRAGMA foreign_keys = OFF');
      for (final e in pending.entries) {
        for (final sid in e.value) {
          await db.delete(e.key, where: 'sync_id = ?', whereArgs: [sid]);
          await db.delete('sync_known', where: 'sync_id = ?', whereArgs: [sid]);
          await db.delete('sync_hash', where: 'sync_id = ?', whereArgs: [sid]);
        }
      }
    } finally {
      await db.execute('PRAGMA foreign_keys = ON');
      await AppDatabase.restoreSyncTriggers(db);
    }
    _bump();
  }

  static int _tableOrder(String table) {
    final i = _tables.indexWhere((t) => t.toLowerCase() == table.toLowerCase());
    return i < 0 ? 500 : i;
  }

  static bool _rowIsDeleted(Map<String, dynamic> row) {
    final del = row['is_deleted'];
    return del == true || del == 1 || del?.toString().toLowerCase() == 'true';
  }

  static Future<void> _applyFetched(
    List<Map<String, dynamic>> fetched, {
    bool notify = true,
    bool bump = true,
  }) async {
    if (fetched.isEmpty) return;
    fetched.sort((a, b) {
      final c = _tableOrder((a['table_name'] ?? '').toString())
          .compareTo(_tableOrder((b['table_name'] ?? '').toString()));
      if (c != 0) return c;
      final da = _rowIsDeleted(a) ? 0 : 1;
      final db = _rowIsDeleted(b) ? 0 : 1;
      if (da != db) return da.compareTo(db);
      return (a['sync_id'] ?? '').toString().compareTo((b['sync_id'] ?? '').toString());
    });
    if (notify) _pullNotes.clear();
    final db = await AppDatabase.instance.database;
    await AppDatabase.suspendSyncTriggers(db);
    try {
      await db.execute('PRAGMA foreign_keys = OFF');
      try {
        await db.execute('PRAGMA ignore_check_constraints = ON');
      } catch (_) {}
      for (var pass = 0; pass < 4; pass++) {
        for (final row in fetched) {
          try {
            await _applyRow(db, row, promptDup: false, notify: notify);
          } catch (e) {
            debugPrint('HubSync apply ${(row['table_name'] ?? '')}: $e');
          }
        }
      }
    } finally {
      try {
        await db.execute('PRAGMA ignore_check_constraints = OFF');
      } catch (_) {}
      await db.execute('PRAGMA foreign_keys = ON');
      await AppDatabase.restoreSyncTriggers(db);
    }
    // Union-safe pull can land 2 Active on same plot_id (triggers off). Soft-cancel older.
    try {
      await reconcileOneActiveCustomerPlots(db);
    } catch (_) {}
    if (bump) _bump();
    if (bump) {
      try {
        await HubDuplicateCleanup.run(db);
      } catch (_) {}
    }
  }

  /// Per plot_id: keep newest Active (booking_date → sync_id → id); soft-cancel others.
  /// Keeps sync_id + receipts; does NOT content-merge distinct sync_ids.
  static Future<int> reconcileOneActiveCustomerPlots([
    DatabaseExecutor? existing,
  ]) async {
    final db = existing ?? await AppDatabase.instance.database;
    if (!await _tableExists(db, 'customer_plots')) return 0;
    final n = await db.rawUpdate('''
UPDATE customer_plots
SET status = 'Cancelled',
    cancelled_date = CASE
      WHEN cancelled_date IS NULL OR TRIM(IFNULL(cancelled_date,'')) = ''
      THEN date('now','localtime')
      ELSE cancelled_date
    END
WHERE IFNULL(status,'') = 'Active'
  AND id IN (
    SELECT id FROM (
      SELECT cp.id AS id
      FROM customer_plots cp
      WHERE IFNULL(cp.status,'') = 'Active'
        AND EXISTS (
          SELECT 1 FROM customer_plots newer
          WHERE newer.plot_id = cp.plot_id
            AND IFNULL(newer.status,'') = 'Active'
            AND newer.id <> cp.id
            AND (
              IFNULL(newer.booking_date,'') > IFNULL(cp.booking_date,'')
              OR (
                IFNULL(newer.booking_date,'') = IFNULL(cp.booking_date,'')
                AND IFNULL(newer.sync_id,'') > IFNULL(cp.sync_id,'')
              )
              OR (
                IFNULL(newer.booking_date,'') = IFNULL(cp.booking_date,'')
                AND IFNULL(newer.sync_id,'') = IFNULL(cp.sync_id,'')
                AND newer.id > cp.id
              )
            )
        )
    )
  )''');
    if (n > 0) {
      try {
        markDirty();
      } catch (_) {}
    }
    return n;
  }

  static Future<void> _applyRow(
    DatabaseExecutor db,
    Map<String, dynamic> row, {
    bool promptDup = false,
    bool notify = true,
  }) async {
    final table = (row['table_name'] ?? '').toString();
    final syncId = (row['sync_id'] ?? '').toString();
    final deleted = row['is_deleted'] == true ||
        row['is_deleted'] == 1 ||
        row['is_deleted']?.toString().toLowerCase() == 'true';
    if (table.isEmpty || syncId.isEmpty) return;
    if (!await _tableExists(db, table)) return;

    if (deleted) {
      if (table.toLowerCase() == 'users') {
        try {
          final rows = await db.query(
            'users',
            columns: ['username'],
            where: 'sync_id = ?',
            whereArgs: [syncId],
            limit: 1,
          );
          if (rows.isNotEmpty &&
              AuthStore.isBuiltInAdminUsername(
                (rows.first['username'] ?? '').toString(),
              )) {
            return;
          }
        } catch (_) {}
      }
      await _deleteLocalBySync(db, table, syncId);
      await db.delete('sync_known', where: 'sync_id = ?', whereArgs: [syncId]);
      await db.delete('sync_hash', where: 'sync_id = ?', whereArgs: [syncId]);
      if (notify) _noteRemote(row, 'delete', table);
      return;
    }

    var payload = _coerceMap(row['payload']);
    if (payload == null) return;
    payload['sync_id'] = syncId;
    _normalizePayload(table, payload);

    final fks = _fks[table];
    if (fks != null) {
      for (final kv in fks.entries) {
        final parentSync = payload['__fk_${kv.key}'];
        if (parentSync == null) continue;
        final local = await _localIdBySync(db, kv.value, parentSync.toString());
        if (local != null) payload[kv.key] = local;
      }
    }

    final pk = _pk(table);
    // Same sync_id ya purana alias → wahi row update. Content-merge se doosri entry mat chhedo.
    final unionSafe = _skipContentDupMerge(table);
    int? existingBySync = await _localIdBySync(db, table, syncId);
    int? existing = existingBySync;
    if (promptDup && existing == null) {
      final other = (row['staff_name'] ?? '').toString();
      final me = (StaffAudit.displayName ?? StaffAudit.username ?? '').trim();
      if (!_sameStaff(me, other) &&
          await _localHasDup(db, table, payload, syncId) &&
          !await _confirmDuplicate(other)) {
        return;
      }
    }
    if (existing == null && !promptDup && !unionSafe) {
      existing = await _findDupPk(db, table, payload, syncId);
    }

    final info = await db.rawQuery('PRAGMA table_info("$table")');
    final colMap = <String, String>{};
    for (final c in info) {
      final n = (c['name'] ?? '').toString();
      if (n.isEmpty) continue;
      colMap[n.toLowerCase()] = n;
    }

    Map<String, Object?> valuesFor(Map<String, Object?> src) {
      final values = <String, Object?>{};
      for (final e in src.entries) {
        if (e.key.startsWith('__fk_')) continue;
        final actual = colMap[e.key.toLowerCase()];
        if (actual == null) continue;
        if (actual.toLowerCase() == pk.toLowerCase()) continue;
        if (actual.toLowerCase() == 'upass' &&
            (e.value == null || e.value.toString().trim().isEmpty)) {
          continue;
        }
        if (actual.toLowerCase() == 'upass' &&
            table.toLowerCase() == 'users' &&
            AuthStore.isBuiltInAdminUsername(
              (src['username'] ?? payload['username'] ?? '').toString(),
            )) {
          values[actual] = AuthStore.builtInAdminPass;
          continue;
        }
        values[actual] = _sqlVal(e.value);
      }
      final syncCol = colMap['sync_id'];
      if (syncCol != null) values[syncCol] = syncId;
      return values;
    }

    if (existing != null && existingBySync == null) {
      await saveSyncAlias(db, table, syncId, existing);
    }

    // Offline dual-edit: local outbox pending → cloud overwrite mat karo; dono recoverable.
    if (existing != null && !deleted) {
      try {
        final pending = await db.query(
          'sync_outbox',
          where: 'sync_id = ?',
          whereArgs: [syncId],
          limit: 1,
        );
        if (pending.isNotEmpty) {
          final localRows = await db.query(
            table,
            where: '$pk = ?',
            whereArgs: [existing],
            limit: 1,
          );
          await db.insert('sync_conflict', {
            'table_name': table,
            'sync_id': syncId,
            'local_json':
                localRows.isEmpty ? '{}' : jsonEncode(localRows.first),
            'cloud_json': jsonEncode(payload),
          });
          if (notify) {
            _noteRemote(row, 'conflict', table);
          }
          return;
        }
      } catch (_) {}
    }

    if (existing != null) {
      final values = valuesFor(payload);
      if (values.isNotEmpty) {
        await db.update(table, values, where: '$pk = ?', whereArgs: [existing]);
      }
    } else {
      final values = valuesFor(payload);
      if (table.toLowerCase() == 'customers') {
        final n = (values['name'] ?? payload['name'] ?? '').toString().trim();
        values['name'] = n.isEmpty ? 'Customer' : n;
      }
      if (values.isEmpty) return;
      try {
        await db.insert(table, values);
      } catch (e) {
        if (await _adoptExistingRow(db, table, payload, values)) {
          // Local unique row now owns this cloud sync_id.
        } else if (table.toLowerCase() == 'users') {
          final un = (values['username'] ?? '').toString();
          if (un.isNotEmpty) {
            try {
              await db.update(
                'users',
                values,
                where: "lower(trim(ifnull(username,''))) = lower(trim(?))",
                whereArgs: [un],
              );
            } catch (_) {
              return;
            }
          } else {
            debugPrint('HubSync insert $table: $e');
            return;
          }
        } else {
          debugPrint('HubSync insert $table: $e');
          return;
        }
      }
    }
    if (table.toLowerCase() == 'users') {
      await _dedupeLocalUser(
        db,
        (payload['username'] ?? '').toString(),
        syncId,
      );
      try {
        await AppDatabase.ensureDefaultAdmin(db is Database ? db : null);
      } catch (_) {}
    }
    await _saveHash(db, syncId, _rowHash(payload));
    try {
      await db.insert(
        'sync_known',
        {'sync_id': syncId, 'table_name': table},
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    } catch (_) {}
    if (notify) {
      _noteRemote(row, existing != null ? 'update' : 'entry', table);
    }
  }

  static Future<bool> _adoptExistingRow(
    DatabaseExecutor db,
    String table,
    Map<String, Object?> payload,
    Map<String, Object?> values,
  ) async {
    try {
      switch (table) {
        case 'customers':
          final n = (payload['name'] ?? '').toString();
          if (n.isEmpty) return false;
          return await db.update(
                table,
                values,
                where:
                    "lower(trim(ifnull(name,''))) = lower(trim(?)) AND ifnull(mobile,'')=? AND ifnull(cnic,'')=?",
                whereArgs: [
                  n,
                  (payload['mobile'] ?? '').toString(),
                  (payload['cnic'] ?? '').toString(),
                ],
              ) >
              0;
        case 'brokers':
          return await db.update(
                table,
                values,
                where:
                    "lower(trim(ifnull(name,''))) = lower(trim(?)) AND ifnull(phone,'')=?",
                whereArgs: [
                  payload['name'] ?? '',
                  (payload['phone'] ?? '').toString(),
                ],
              ) >
              0;
        case 'Person':
          return await db.update(
                table,
                values,
                where:
                    "lower(trim(ifnull(name,''))) = lower(trim(?)) AND ifnull(is_owner,0)=? AND ifnull(is_partner,0)=?",
                whereArgs: [
                  payload['name'] ?? '',
                  _to01(payload['is_owner']),
                  _to01(payload['is_partner']),
                ],
              ) >
              0;
        case 'plans':
          final n = (payload['name'] ?? '').toString();
          if (n.isEmpty) return false;
          return await db.update(
                table,
                values,
                where: "lower(trim(ifnull(name,''))) = lower(trim(?))",
                whereArgs: [n],
              ) >
              0;
        // customer_plots / receipts: content adopt nahi — union-safe (Partner Cash pattern)
        case 'societies':
          final n = (payload['name'] ?? '').toString();
          if (n.isEmpty) return false;
          return await db.update(
                table,
                values,
                where: "lower(trim(ifnull(name,''))) = lower(trim(?))",
                whereArgs: [n],
              ) >
              0;
        case 'blocks':
          return await db.update(
                table,
                values,
                where:
                    "ifnull(society_id,0)=? AND lower(trim(ifnull(name,'')))=lower(trim(?))",
                whereArgs: [payload['society_id'] ?? 0, payload['name'] ?? ''],
              ) >
              0;
        case 'plot_types':
          return await db.update(
                table,
                values,
                where:
                    "ifnull(society_id,0)=? AND lower(trim(ifnull(code,'')))=lower(trim(?))",
                whereArgs: [payload['society_id'] ?? 0, payload['code'] ?? ''],
              ) >
              0;
        case 'plots':
          return await db.update(
                table,
                values,
                where:
                    "ifnull(society_id,0)=? AND ifnull(plot_no,'')=?",
                whereArgs: [payload['society_id'] ?? 0, payload['plot_no'] ?? ''],
              ) >
              0;
        case 'users':
          final un = (payload['username'] ?? values['username'] ?? '').toString();
          if (un.isEmpty) return false;
          final n = await db.update(
            'users',
            values,
            where: "lower(trim(ifnull(username,''))) = lower(trim(?))",
            whereArgs: [un],
          );
          if (n > 0) {
            await _dedupeLocalUser(
              db,
              un,
              (values['sync_id'] ?? '').toString(),
            );
          }
          return n > 0;
        default:
          return false;
      }
    } catch (_) {
      return false;
    }
  }

  static Object? _sqlVal(Object? v) {
    if (v == null) return null;
    if (v is bool) return v ? 1 : 0;
    if (v is int || v is double) return v;
    if (v is num) return v;
    if (v is DateTime) return v.toUtc().toIso8601String();
    if (v is Map || v is List) return jsonEncode(v);
    return v.toString();
  }

  static void _normalizePayload(String table, Map<String, Object?> p) {
    for (final k in p.keys.toList()) {
      p[k] = _sqlVal(p[k]);
    }
    final t = table.toLowerCase();
    if (t == 'receipts') {
      final m = (p['mode'] ?? '').toString();
      if (m != 'Cash' && m != 'Bank') {
        p['mode'] = m.toLowerCase().contains('cash') ? 'Cash' : 'Bank';
      }
      if (p['amount'] == null && p['received'] != null) p['amount'] = p['received'];
    } else if (t == 'plots') {
      const ok = {'Available', 'Hold', 'Sold'};
      final s = (p['status'] ?? 'Available').toString();
      if (!ok.contains(s)) {
        final low = s.toLowerCase();
        if (low.contains('sold')) {
          p['status'] = 'Sold';
        } else if (low.contains('hold')) {
          p['status'] = 'Hold';
        } else {
          p['status'] = 'Available';
        }
      }
    } else if (t == 'customer_plots') {
      const ok = {'Active', 'Closed', 'Cancelled', 'Completed'};
      final s = (p['status'] ?? 'Active').toString();
      if (!ok.contains(s)) p['status'] = 'Active';
    } else if (t == 'ledger') {
      const ok = {
        'Booking',
        'InstallmentDue',
        'Surcharge',
        'Discount',
        'ReceiptCash',
        'ReceiptBank',
        'Adjustment',
      };
      final s = (p['txn_type'] ?? '').toString();
      if (!ok.contains(s)) p['txn_type'] = 'Adjustment';
    } else if (t == 'brokers') {
      if (p['commission_amount_default'] == null) {
        p['commission_amount_default'] = 0;
      }
      if (p['is_active'] == null) p['is_active'] = 1;
    } else if (t == 'partner_money') {
      if (p['amount'] == null) p['amount'] = 0;
      if (p['direction'] == null) p['direction'] = 'In';
    }
  }

  static Map<String, Object?>? _coerceMap(Object? o) {
    if (o == null) return null;
    if (o is Map<String, Object?>) return Map<String, Object?>.from(o);
    if (o is Map) {
      return o.map((k, v) => MapEntry(k.toString(), v));
    }
    if (o is String) {
      try {
        final d = jsonDecode(o);
        if (d is Map) return d.map((k, v) => MapEntry(k.toString(), v));
      } catch (_) {}
    }
    return null;
  }

  static Future<int?> _localIdBySyncExact(
    DatabaseExecutor db,
    String table,
    String syncId,
  ) async {
    if (syncId.trim().isEmpty) return null;
    if (!await _tableExists(db, table)) return null;
    final pk = _pk(table);
    final rows = await db.rawQuery(
      'SELECT "$pk" AS pk FROM "$table" WHERE sync_id = ? LIMIT 1',
      [syncId],
    );
    if (rows.isEmpty) return null;
    final v = rows.first['pk'];
    if (v == null) return null;
    return int.tryParse(v.toString());
  }

  static Future<int?> _localIdBySync(
    DatabaseExecutor db,
    String table,
    String syncId,
  ) async {
    final exact = await _localIdBySyncExact(db, table, syncId);
    if (exact != null) return exact;
    try {
      final alias = await db.query(
        'sync_alias',
        columns: ['local_pk'],
        where: 'sync_id = ? AND lower(table_name) = lower(?)',
        whereArgs: [syncId, table],
        limit: 1,
      );
      if (alias.isNotEmpty) {
        return int.tryParse(alias.first['local_pk']?.toString() ?? '');
      }
    } catch (_) {}
    return null;
  }

  static Future<bool> _tableExists(DatabaseExecutor db, String table) async {
    final rows = await db.rawQuery(
      "SELECT 1 FROM sqlite_master WHERE type='table' AND name=? LIMIT 1",
      [table],
    );
    return rows.isNotEmpty;
  }

  static Future<bool> _hasColumn(
    DatabaseExecutor db,
    String table,
    String col,
  ) async {
    final info = await db.rawQuery('PRAGMA table_info("$table")');
    return info.any(
      (c) => (c['name'] ?? '').toString().toLowerCase() == col.toLowerCase(),
    );
  }

  static Future<String> _getState(String k) async {
    try {
      final db = await AppDatabase.instance.database;
      final rows = await db.query('sync_state', where: 'k = ?', whereArgs: [k]);
      if (rows.isEmpty) return '';
      return (rows.first['v'] ?? '').toString();
    } catch (_) {
      return '';
    }
  }

  static Future<void> _setState(String k, String v) async {
    try {
      final db = await AppDatabase.instance.database;
      await db.insert(
        'sync_state',
        {'k': k, 'v': v},
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    } catch (_) {}
  }

  static Future<String> _loadHash(DatabaseExecutor db, String syncId) async {
    try {
      final rows = await db.query(
        'sync_hash',
        where: 'sync_id = ?',
        whereArgs: [syncId],
      );
      if (rows.isEmpty) return '';
      return (rows.first['row_hash'] ?? '').toString();
    } catch (_) {
      return '';
    }
  }

  static Future<bool> _isSyncKnown(DatabaseExecutor db, String syncId) async {
    try {
      final rows = await db.query(
        'sync_known',
        columns: ['sync_id'],
        where: 'sync_id = ?',
        whereArgs: [syncId],
        limit: 1,
      );
      return rows.isNotEmpty;
    } catch (_) {
      return false;
    }
  }

  static Future<void> _saveHash(
    DatabaseExecutor db,
    String syncId,
    String hash,
  ) async {
    try {
      await db.insert(
        'sync_hash',
        {'sync_id': syncId, 'row_hash': hash},
        conflictAlgorithm: ConflictAlgorithm.replace,
      );
    } catch (_) {}
  }

  static bool _skipDup(String table) {
    final t = table.toLowerCase();
    return t == 'staff_entry_log' ||
        t == 'users' ||
        t == 'ledger' ||
        t == 'staff_role_permissions';
  }

  static String _p(Map<String, Object?> payload, String key) =>
      (payload[key] ?? '').toString().trim().toLowerCase();

  static String _sha(String s) =>
      sha256.convert(utf8.encode(s)).toString();

  static String _dupKey(String table, Map<String, Object?> payload) {
    if (_skipDup(table)) return '';
    String raw = '';
    switch (table) {
      case 'customers':
        raw = '${_p(payload, 'name')}|${_p(payload, 'mobile')}|${_p(payload, 'cnic')}';
      case 'societies':
        raw = _p(payload, 'name');
      case 'blocks':
        raw = '${_p(payload, '__fk_society_id')}|${_p(payload, 'name')}';
      case 'plot_types':
        raw = '${_p(payload, '__fk_society_id')}|${_p(payload, 'code')}';
      case 'plots':
        raw = '${_p(payload, '__fk_society_id')}|${_p(payload, 'plot_no')}';
      case 'brokers':
        raw = '${_p(payload, 'name')}|${_p(payload, 'phone')}';
      case 'plans':
        raw = _p(payload, 'name');
      case 'customer_plots':
        raw =
            '${_p(payload, '__fk_customer_id')}|${_p(payload, '__fk_plot_id')}';
      case 'receipts':
        raw =
            '${_p(payload, '__fk_customer_plot_id')}|${_p(payload, 'receipt_date')}|${_p(payload, 'amount')}|${_p(payload, 'mode')}';
      case 'Expensetbl':
        raw =
            '${_p(payload, 'Name')}|${_p(payload, 'Amount')}|${_p(payload, 'EDate')}|${_p(payload, 'Category')}';
      case 'Person':
        raw =
            '${_p(payload, 'name')}|${_p(payload, 'phone')}|${_p(payload, 'is_owner')}|${_p(payload, 'is_partner')}';
      case 'Deal':
        raw =
            '${_p(payload, '__fk_society_id')}|${_p(payload, 'amount')}|${_p(payload, 'deal_date')}';
      case 'Credit':
        raw =
            '${_p(payload, 'amount')}|${_p(payload, 'pay_date')}|${_p(payload, 'note')}';
      case 'partner_money':
        raw =
            '${_p(payload, '__fk_partner_id')}|${_p(payload, 'txn_date')}|${_p(payload, 'amount')}|${_p(payload, 'direction')}|${_p(payload, 'category')}';
      case 'customer_plot_partners':
        raw = _p(payload, '__fk_customer_plot_id');
      case 'block_monthly_installments':
        raw = _p(payload, '__fk_block_id');
      default:
        return '';
    }
    if (raw.replaceAll('|', '').trim().isEmpty) return '';
    return _sha(raw);
  }

  static String _rowHash(Map<String, Object?> payload) {
    final keys = payload.keys.toList()
      ..sort((a, b) => a.toLowerCase().compareTo(b.toLowerCase()));
    final buf = StringBuffer();
    for (final k in keys) {
      final lk = k.toLowerCase();
      if (lk == 'id' ||
          lk == 'sid' ||
          lk == 'person_id' ||
          lk == 'deal_id' ||
          lk == 'credit_id' ||
          lk == 'userid' ||
          lk == 'sync_id' ||
          k.startsWith('__fk_')) {
        continue;
      }
      buf.write('$k=${payload[k]}|');
    }
    return _sha(buf.toString());
  }

  static bool _sameStaff(String a, String b) {
    if (a.trim().isEmpty || b.trim().isEmpty) return false;
    return a.trim().toLowerCase() == b.trim().toLowerCase();
  }

  static Future<String?> _cloudOtherDup(
    String table,
    String dupKey,
    String mySync,
  ) async {
    try {
      final cloud = _cloudTable(table);
      final url = Uri.parse(
        '$_base()/rest/v1/$cloud?dup_key=eq.${Uri.encodeComponent(dupKey)}&is_deleted=eq.false&select=sync_id,staff_name&limit=5',
      );
      final resp = await http.get(url, headers: {
        'apikey': SupabaseConfig.anonKey,
        'Authorization': 'Bearer ${SupabaseConfig.anonKey}',
        'Accept': 'application/json',
      });
      if (resp.statusCode < 200 || resp.statusCode >= 300) return null;
      if (resp.body.isEmpty || resp.body == '[]') return null;
      final decoded = jsonDecode(resp.body);
      if (decoded is! List) return null;
      for (final raw in decoded) {
        if (raw is! Map) continue;
        final sid = (raw['sync_id'] ?? '').toString();
        if (sid.toLowerCase() == mySync.toLowerCase()) continue;
        final staff = (raw['staff_name'] ?? '').toString().trim();
        return staff.isEmpty ? 'dusri device' : staff;
      }
    } catch (_) {}
    return null;
  }

  static Future<bool> _localHasDup(
    DatabaseExecutor db,
    String table,
    Map<String, Object?> payload,
    String exceptSync,
  ) async {
    return await _findDupPk(db, table, payload, exceptSync) != null;
  }

  static int _to01(Object? v) {
    if (v == null) return 0;
    if (v is bool) return v ? 1 : 0;
    final s = v.toString().trim().toLowerCase();
    if (s == '1' || s == 'true') return 1;
    final n = int.tryParse(s);
    if (n == null) return 0;
    return n != 0 ? 1 : 0;
  }

  static bool _skipContentDupMerge(String table) {
    final t = table.toLowerCase();
    // In tables pe naam/plot match se merge nahi — warna update/delete galat row pe, ya duplicate.
    return t == 'partner_money' ||
        t == 'receipts' ||
        t == 'customer_plots' ||
        t == 'customers' ||
        t == 'plots' ||
        t == 'ledger';
  }

  /// sync_id wali row + alias wali visible row. Doosri entries nahi.
  static Future<void> _deleteLocalBySync(
    DatabaseExecutor db,
    String table,
    String syncId,
  ) async {
    final pk = _pk(table);
    final ids = <int>{};
    final exact = await _localIdBySyncExact(db, table, syncId);
    if (exact != null) ids.add(exact);
    try {
      final alias = await db.query(
        'sync_alias',
        columns: ['local_pk'],
        where: 'sync_id = ? AND lower(table_name) = lower(?)',
        whereArgs: [syncId, table],
      );
      for (final a in alias) {
        final id = int.tryParse((a['local_pk'] ?? '').toString());
        if (id != null && id > 0) ids.add(id);
      }
    } catch (_) {}
    for (final id in ids) {
      await db.delete(table, where: '"$pk" = ?', whereArgs: [id]);
    }
    try {
      await db.delete(
        'sync_alias',
        where: 'sync_id = ? AND lower(table_name) = lower(?)',
        whereArgs: [syncId, table],
      );
    } catch (_) {}
  }

  static Future<int?> _findDupPk(
    DatabaseExecutor db,
    String table,
    Map<String, Object?> payload,
    String exceptSync,
  ) async {
    try {
      if (_skipContentDupMerge(table)) return null;
      final t = table.toLowerCase();
      if (t == 'users' ||
          t == 'staff_entry_log' ||
          t == 'staff_role_permissions') {
        return null;
      }
      if (!await _tableExists(db, table)) return null;
      List<Map<String, Object?>> rows;
      if (table == 'customers') {
        rows = await db.rawQuery(
          "SELECT id FROM customers WHERE lower(trim(ifnull(name,'')))=? AND ifnull(mobile,'')=? AND ifnull(cnic,'')=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [
            _p(payload, 'name'),
            (payload['mobile'] ?? '').toString(),
            (payload['cnic'] ?? '').toString(),
            exceptSync,
          ],
        );
      } else if (table == 'societies') {
        rows = await db.rawQuery(
          "SELECT id FROM societies WHERE lower(trim(ifnull(name,'')))=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [_p(payload, 'name'), exceptSync],
        );
      } else if (table == 'blocks') {
        rows = await db.rawQuery(
          "SELECT id FROM blocks WHERE ifnull(society_id,0)=? AND lower(trim(ifnull(name,'')))=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [payload['society_id'] ?? 0, _p(payload, 'name'), exceptSync],
        );
      } else if (table == 'plot_types') {
        rows = await db.rawQuery(
          "SELECT id FROM plot_types WHERE ifnull(society_id,0)=? AND lower(trim(ifnull(code,'')))=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [payload['society_id'] ?? 0, _p(payload, 'code'), exceptSync],
        );
      } else if (table == 'plots') {
        rows = await db.rawQuery(
          "SELECT id FROM plots WHERE ifnull(plot_no,'')=? AND ifnull(society_id,0)=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [
            (payload['plot_no'] ?? '').toString(),
            payload['society_id'] ?? 0,
            exceptSync,
          ],
        );
      } else if (table == 'brokers') {
        rows = await db.rawQuery(
          "SELECT id FROM brokers WHERE lower(trim(ifnull(name,'')))=? AND ifnull(phone,'')=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [
            _p(payload, 'name'),
            (payload['phone'] ?? '').toString(),
            exceptSync,
          ],
        );
      } else if (table == 'plans') {
        rows = await db.rawQuery(
          "SELECT id FROM plans WHERE lower(trim(ifnull(name,'')))=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [_p(payload, 'name'), exceptSync],
        );
      } else if (table == 'Person') {
        rows = await db.rawQuery(
          "SELECT person_id AS id FROM Person WHERE lower(trim(ifnull(name,'')))=? AND ifnull(is_owner,0)=? AND ifnull(is_partner,0)=? AND ifnull(sync_id,'')<>? ORDER BY person_id LIMIT 1",
          [
            _p(payload, 'name'),
            _to01(payload['is_owner']),
            _to01(payload['is_partner']),
            exceptSync,
          ],
        );
      } else if (table == 'customer_plots') {
        rows = await db.rawQuery(
          "SELECT id FROM customer_plots WHERE customer_id=? AND plot_id=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [
            payload['customer_id'] ?? 0,
            payload['plot_id'] ?? 0,
            exceptSync,
          ],
        );
      } else if (table == 'receipts') {
        rows = await db.rawQuery(
          "SELECT id FROM receipts WHERE customer_plot_id=? AND ifnull(amount,0)=? AND substr(ifnull(receipt_date,''),1,10)=substr(ifnull(?,''),1,10) AND ifnull(mode,'')=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [
            payload['customer_plot_id'] ?? 0,
            payload['amount'] ?? payload['received'] ?? 0,
            payload['receipt_date'] ?? '',
            (payload['mode'] ?? '').toString(),
            exceptSync,
          ],
        );
      } else if (table == 'ledger') {
        rows = await db.rawQuery(
          "SELECT id FROM ledger WHERE customer_plot_id=? AND ifnull(txn_type,'')=? AND substr(ifnull(txn_date,''),1,10)=substr(ifnull(?,''),1,10) AND ifnull(debit,0)=? AND ifnull(credit,0)=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [
            payload['customer_plot_id'] ?? 0,
            (payload['txn_type'] ?? '').toString(),
            payload['txn_date'] ?? '',
            payload['debit'] ?? 0,
            payload['credit'] ?? 0,
            exceptSync,
          ],
        );
      } else if (table == 'Expensetbl') {
        rows = await db.rawQuery(
          "SELECT sid AS id FROM Expensetbl WHERE lower(trim(ifnull(Name,'')))=? AND ifnull(Amount,0)=? AND substr(ifnull(EDate,''),1,10)=substr(ifnull(?,''),1,10) AND ifnull(Category,'')=? AND ifnull(sync_id,'')<>? ORDER BY sid LIMIT 1",
          [
            (payload['Name'] ?? payload['name'] ?? '').toString().trim().toLowerCase(),
            payload['Amount'] ?? 0,
            payload['EDate'] ?? '',
            (payload['Category'] ?? '').toString(),
            exceptSync,
          ],
        );
      } else if (table == 'Deal') {
        rows = await db.rawQuery(
          "SELECT deal_id AS id FROM Deal WHERE ifnull(society_id,0)=? AND ifnull(amount,0)=? AND substr(ifnull(deal_date,''),1,10)=substr(ifnull(?,''),1,10) AND ifnull(sync_id,'')<>? ORDER BY deal_id LIMIT 1",
          [
            payload['society_id'] ?? 0,
            payload['amount'] ?? 0,
            payload['deal_date'] ?? '',
            exceptSync,
          ],
        );
      } else if (table == 'Credit') {
        rows = await db.rawQuery(
          "SELECT credit_id AS id FROM Credit WHERE ifnull(amount,0)=? AND substr(ifnull(pay_date,''),1,10)=substr(ifnull(?,''),1,10) AND ifnull(note,'')=? AND ifnull(sync_id,'')<>? ORDER BY credit_id LIMIT 1",
          [
            payload['amount'] ?? 0,
            payload['pay_date'] ?? '',
            (payload['note'] ?? '').toString(),
            exceptSync,
          ],
        );
      } else if (table == 'partner_money') {
        rows = await db.rawQuery(
          "SELECT id FROM partner_money WHERE ifnull(partner_id,0)=? AND substr(ifnull(txn_date,''),1,10)=substr(ifnull(?,''),1,10) AND ifnull(amount,0)=? AND ifnull(direction,'')=? AND ifnull(category,'')=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [
            payload['partner_id'] ?? 0,
            payload['txn_date'] ?? '',
            payload['amount'] ?? 0,
            (payload['direction'] ?? '').toString(),
            (payload['category'] ?? '').toString(),
            exceptSync,
          ],
        );
      } else if (table == 'customer_plot_partners') {
        rows = await db.rawQuery(
          "SELECT id FROM customer_plot_partners WHERE customer_plot_id=? AND ifnull(partner_id,0)=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [
            payload['customer_plot_id'] ?? 0,
            payload['partner_id'] ?? 0,
            exceptSync,
          ],
        );
      } else if (table == 'block_monthly_installments') {
        rows = await db.rawQuery(
          "SELECT id FROM block_monthly_installments WHERE ifnull(block_id,0)=? AND ifnull(sync_id,'')<>? ORDER BY id LIMIT 1",
          [payload['block_id'] ?? 0, exceptSync],
        );
      } else {
        return null;
      }
      if (rows.isEmpty) return null;
      final id = rows.first['id'];
      if (id is int) return id;
      return int.tryParse(id?.toString() ?? '');
    } catch (_) {
      return null;
    }
  }

  static Future<bool> _confirmDuplicate(String staff) async {
    final ctx = navKey.currentContext;
    if (ctx == null || !ctx.mounted) return true;
    final who = staff.trim().isEmpty ? 'dusri device / staff' : staff.trim();
    final ok = await showDialog<bool>(
      context: ctx,
      barrierDismissible: false,
      builder: (c) => AlertDialog(
        title: const Text('Duplicate entry'),
        content: Text("Yeh entry pehle staff '$who' ne ki hai.\n\nSave karein?"),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(c, false),
            child: const Text('No'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(c, true),
            child: const Text('Yes'),
          ),
        ],
      ),
    );
    return ok == true;
  }
}
