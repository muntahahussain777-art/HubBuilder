import '../db/app_database.dart';
import 'staff_audit.dart';

class IdName {
  const IdName(this.id, this.name);
  final int id;
  final String name;

  @override
  bool operator ==(Object other) => other is IdName && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class PlotTypeItem {
  const PlotTypeItem(this.id, this.societyId, this.code);
  final int id;
  final int societyId;
  final String code;

  @override
  bool operator ==(Object other) => other is PlotTypeItem && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class BlockItem {
  const BlockItem(this.id, this.societyId, this.name);
  final int id;
  final int societyId;
  final String name;

  @override
  bool operator ==(Object other) => other is BlockItem && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class StoredPlot {
  StoredPlot({
    required this.id,
    required this.societyId,
    required this.blockId,
    required this.plotTypeId,
    required this.plotNo,
    required this.status,
    required this.createdAt,
  });

  final int id;
  final int societyId;
  final int blockId;
  final int plotTypeId;
  String plotNo;
  String status;
  final String createdAt;

  factory StoredPlot.fromMap(Map<String, Object?> m) => StoredPlot(
        id: m['id'] as int,
        societyId: m['society_id'] as int,
        blockId: m['block_id'] as int,
        plotTypeId: m['plot_type_id'] as int,
        plotNo: m['plot_no'] as String? ?? '',
        status: m['status'] as String? ?? 'Available',
        createdAt: m['created_at'] as String? ?? '',
      );

  @override
  bool operator ==(Object other) => other is StoredPlot && other.id == id;

  @override
  int get hashCode => id.hashCode;
}

class PreviewPlotRow {
  PreviewPlotRow({
    required this.plotId,
    required this.isDb,
    required this.societyId,
    required this.blockId,
    required this.plotTypeId,
    required this.plotNo,
    required this.society,
    required this.block,
    required this.type,
    required this.status,
  });

  int plotId;
  bool isDb;
  int societyId;
  int blockId;
  int plotTypeId;
  String plotNo;
  String society;
  String block;
  String type;
  String status;
}

/// SQLite-backed plot inventory — same shape as C# societies / blocks / plot_types / plots.
abstract final class PlotInventoryStore {
  static Future<List<IdName>> societies() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('societies', orderBy: 'name COLLATE NOCASE');
    return rows
        .map((r) => IdName(r['id'] as int, r['name'] as String? ?? ''))
        .toList();
  }

  static Future<List<BlockItem>> blocksFor(int societyId) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'blocks',
      where: 'society_id = ?',
      whereArgs: [societyId],
      orderBy: 'name COLLATE NOCASE',
    );
    return rows
        .map(
          (r) => BlockItem(
            r['id'] as int,
            r['society_id'] as int,
            r['name'] as String? ?? '',
          ),
        )
        .toList();
  }

  static Future<List<PlotTypeItem>> typesFor(int societyId) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'plot_types',
      where: 'society_id = ?',
      whereArgs: [societyId],
      orderBy: 'code COLLATE NOCASE',
    );
    return rows
        .map(
          (r) => PlotTypeItem(
            r['id'] as int,
            r['society_id'] as int,
            r['code'] as String? ?? '',
          ),
        )
        .toList();
  }

  static Future<List<BlockItem>> allBlocks() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('blocks', orderBy: 'name COLLATE NOCASE');
    return rows
        .map(
          (r) => BlockItem(
            r['id'] as int,
            r['society_id'] as int,
            r['name'] as String? ?? '',
          ),
        )
        .toList();
  }

  static Future<List<PlotTypeItem>> allPlotTypes() async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('plot_types', orderBy: 'code COLLATE NOCASE');
    return rows
        .map(
          (r) => PlotTypeItem(
            r['id'] as int,
            r['society_id'] as int,
            r['code'] as String? ?? '',
          ),
        )
        .toList();
  }

  static Future<StoredPlot?> plotById(int id) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query('plots', where: 'id = ?', whereArgs: [id]);
    if (rows.isEmpty) return null;
    return StoredPlot.fromMap(rows.first);
  }

  static Future<String> societyName(int id) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'societies',
      columns: ['name'],
      where: 'id = ?',
      whereArgs: [id],
    );
    if (rows.isEmpty) return '';
    return rows.first['name'] as String? ?? '';
  }

  static Future<String> blockName(int id) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'blocks',
      columns: ['name'],
      where: 'id = ?',
      whereArgs: [id],
    );
    if (rows.isEmpty) return '';
    return rows.first['name'] as String? ?? '';
  }

  static Future<String> typeCode(int id) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'plot_types',
      columns: ['code'],
      where: 'id = ?',
      whereArgs: [id],
    );
    if (rows.isEmpty) return '';
    return rows.first['code'] as String? ?? '';
  }

  static Future<List<StoredPlot>> availablePlots({
    required int societyId,
    required int blockId,
    required int plotTypeId,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'plots',
      where:
          'society_id = ? AND block_id = ? AND plot_type_id = ? AND status = ?',
      whereArgs: [societyId, blockId, plotTypeId, 'Available'],
      orderBy: 'plot_no COLLATE NOCASE',
    );
    return rows.map(StoredPlot.fromMap).toList();
  }

  /// UPDATE plots SET status='Hold' WHERE id=@pid AND status='Available'
  static Future<bool> holdIfAvailable(int plotId) async {
    final db = await AppDatabase.instance.database;
    final n = await db.rawUpdate(
      "UPDATE plots SET status = 'Hold' WHERE id = ? AND status = 'Available'",
      [plotId],
    );
    return n > 0;
  }

  static Future<bool> releaseHold(int plotId) async {
    final db = await AppDatabase.instance.database;
    final n = await db.rawUpdate(
      "UPDATE plots SET status = 'Available' WHERE id = ? AND status = 'Hold'",
      [plotId],
    );
    return n > 0;
  }

  static Future<List<StoredPlot>> loadExisting({
    required int societyId,
    required int blockId,
    required int plotTypeId,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.query(
      'plots',
      where: 'society_id = ? AND block_id = ? AND plot_type_id = ?',
      whereArgs: [societyId, blockId, plotTypeId],
      orderBy: 'id DESC',
    );
    return rows.map(StoredPlot.fromMap).toList();
  }

  static Future<bool> plotNoExists({
    required int societyId,
    required int blockId,
    required int plotTypeId,
    required String plotNo,
    int? excludeId,
  }) async {
    final db = await AppDatabase.instance.database;
    final rows = await db.rawQuery(
      '''
SELECT id FROM plots
WHERE society_id = ? AND block_id = ? AND plot_type_id = ?
  AND lower(plot_no) = lower(?)
  AND (? IS NULL OR id != ?)
LIMIT 1''',
      [societyId, blockId, plotTypeId, plotNo, excludeId, excludeId],
    );
    return rows.isNotEmpty;
  }

  static Future<int> insertPlot({
    required int societyId,
    required int blockId,
    required int plotTypeId,
    required String plotNo,
    required String status,
  }) async {
    final db = await AppDatabase.instance.database;
    final createdAt = DateTime.now()
        .toIso8601String()
        .replaceFirst('T', ' ')
        .split('.')
        .first;
    return StaffAudit.insert('plots', {
      'society_id': societyId,
      'block_id': blockId,
      'plot_type_id': plotTypeId,
      'plot_no': plotNo,
      'status': status,
      'created_at': createdAt,
    });
  }

  static Future<bool> updatePlot({
    required int id,
    required String plotNo,
    required String status,
  }) async {
    final n = await StaffAudit.update(
      'plots',
      {'plot_no': plotNo, 'status': status},
      where: 'id = ?',
      whereArgs: [id],
    );
    return n > 0;
  }

  static Future<bool> deletePlot(int id) async {
    final n = await StaffAudit.delete('plots', where: 'id = ?', whereArgs: [id]);
    return n > 0;
  }

  // ── Society / Block / PlotType CRUD ──────────────────────────

  static Future<int> insertSociety({
    required String name,
    String? city,
    String? createdAt,
  }) async {
    final db = await AppDatabase.instance.database;
    return StaffAudit.insert('societies', {
      'name': name.trim(),
      'city': city?.trim(),
      'created_at': createdAt ??
          DateTime.now()
              .toIso8601String()
              .replaceFirst('T', ' ')
              .split('.')
              .first,
    });
  }

  static Future<bool> updateSociety({
    required int id,
    required String name,
    String? city,
  }) async {
    final n = await StaffAudit.update(
      'societies',
      {'name': name.trim(), 'city': city?.trim()},
      where: 'id = ?',
      whereArgs: [id],
    );
    return n > 0;
  }

  static Future<List<Map<String, Object?>>> societyRows() async {
    final db = await AppDatabase.instance.database;
    return db.query('societies', orderBy: 'id DESC');
  }

  static Future<int> insertBlock({
    required int societyId,
    required String name,
  }) async {
    final db = await AppDatabase.instance.database;
    return StaffAudit.insert('blocks', {
      'society_id': societyId,
      'name': name.trim(),
    });
  }

  static Future<bool> updateBlock({
    required int id,
    required int societyId,
    required String name,
  }) async {
    final n = await StaffAudit.update(
      'blocks',
      {'society_id': societyId, 'name': name.trim()},
      where: 'id = ?',
      whereArgs: [id],
    );
    return n > 0;
  }

  static Future<int> insertPlotType({
    required int societyId,
    required String code,
  }) async {
    return StaffAudit.insert('plot_types', {
      'society_id': societyId,
      'code': code.trim(),
    });
  }

  static Future<bool> updatePlotType({
    required int id,
    required int societyId,
    required String code,
  }) async {
    final n = await StaffAudit.update(
      'plot_types',
      {'society_id': societyId, 'code': code.trim()},
      where: 'id = ?',
      whereArgs: [id],
    );
    return n > 0;
  }
}
