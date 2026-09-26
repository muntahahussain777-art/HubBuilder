import 'package:flutter/material.dart';

import '../data/plot_inventory_store.dart';
import '../data/hub_sync.dart';
import '../logic/plot_no_helper.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same logic as C# `frmPlotInventoryFormAdd` — SQLite-backed.
class PlotInventoryScreen extends StatefulWidget {
  const PlotInventoryScreen({super.key});

  @override
  State<PlotInventoryScreen> createState() => _PlotInventoryScreenState();
}

class _PlotInventoryScreenState extends State<PlotInventoryScreen> {
  static const _statuses = ['Available', 'Hold', 'Sold'];

  IdName? _society;
  BlockItem? _block;
  PlotTypeItem? _plotType;
  String _status = 'Available';
  int _count = 1;

  final _startPlot = TextEditingController(text: '1');
  final List<PreviewPlotRow> _preview = [];

  List<IdName> _societies = const [];
  List<BlockItem> _blocks = const [];
  List<PlotTypeItem> _types = const [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    HubSync.revision.addListener(_onSync);
    _loadSocieties();
  }

  @override
  void dispose() {
    HubSync.revision.removeListener(_onSync);
    _startPlot.dispose();
    super.dispose();
  }

  void _onSync() {
    if (mounted) _loadSocieties();
  }

  Future<void> _loadSocieties() async {
    final list = await PlotInventoryStore.societies();
    if (!mounted) return;
    setState(() {
      _societies = list;
      _loading = false;
    });
  }

  Future<void> _onSocietyChanged(IdName? v) async {
    setState(() {
      _society = v;
      _block = null;
      _plotType = null;
      _blocks = const [];
      _types = const [];
    });
    if (v == null) return;
    final blocks = await PlotInventoryStore.blocksFor(v.id);
    final types = await PlotInventoryStore.typesFor(v.id);
    if (!mounted) return;
    setState(() {
      _blocks = blocks;
      _types = types;
    });
  }

  void _toast(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  bool _validateCombo() {
    if (_society == null) {
      _toast('Society select karo.');
      return false;
    }
    if (_block == null) {
      _toast('Block select karo.');
      return false;
    }
    if (_plotType == null) {
      _toast('Plot Type select karo.');
      return false;
    }
    return true;
  }

  void _generatePreview() {
    if (!_validateCombo()) return;
    final start = _startPlot.text.trim();
    if (start.isEmpty) {
      _toast('Start Plot No required hai.');
      return;
    }
    if (_count < 1) {
      setState(() => _count = 1);
      _toast('Count kam az kam 1.');
      return;
    }

    var status = _status;
    if (!_statuses.contains(status)) status = 'Available';

    final rows = <PreviewPlotRow>[];
    var cur = start;
    for (var i = 0; i < _count; i++) {
      rows.add(
        PreviewPlotRow(
          plotId: 0,
          isDb: false,
          societyId: _society!.id,
          blockId: _block!.id,
          plotTypeId: _plotType!.id,
          plotNo: cur,
          society: _society!.name,
          block: _block!.name,
          type: _plotType!.code,
          status: status,
        ),
      );
      cur = nextPlotNo(cur);
    }
    setState(() {
      _preview
        ..clear()
        ..addAll(rows);
    });
  }

  Future<void> _loadExisting() async {
    if (!_validateCombo()) return;
    final stored = await PlotInventoryStore.loadExisting(
      societyId: _society!.id,
      blockId: _block!.id,
      plotTypeId: _plotType!.id,
    );
    if (!mounted) return;
    setState(() {
      _preview
        ..clear()
        ..addAll(
          stored.map(
            (p) => PreviewPlotRow(
              plotId: p.id,
              isDb: true,
              societyId: p.societyId,
              blockId: p.blockId,
              plotTypeId: p.plotTypeId,
              plotNo: p.plotNo,
              society: _society!.name,
              block: _block!.name,
              type: _plotType!.code,
              status: p.status,
            ),
          ),
        );
    });
    if (stored.isEmpty) {
      _toast('Is combo pe DB mein plot nahi mile.');
    }
  }

  Future<void> _commit() async {
    if (_preview.isEmpty) {
      _toast('Preview me kuch rows nahi hain.');
      return;
    }

    var inserted = 0;
    var duplicated = 0;
    var failed = 0;

    for (final r in _preview) {
      if (r.isDb) continue;

      if (await PlotInventoryStore.plotNoExists(
        societyId: r.societyId,
        blockId: r.blockId,
        plotTypeId: r.plotTypeId,
        plotNo: r.plotNo,
      )) {
        duplicated++;
        continue;
      }

      try {
        final id = await PlotInventoryStore.insertPlot(
          societyId: r.societyId,
          blockId: r.blockId,
          plotTypeId: r.plotTypeId,
          plotNo: r.plotNo,
          status: r.status,
        );
        r.plotId = id;
        r.isDb = true;
        inserted++;
      } catch (_) {
        failed++;
      }
    }

    if (!mounted) return;
    setState(() {});
    showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.panel,
        title: const Text('RealEstate Builders'),
        content: Text(
          'Commit summary:\nInserted: $inserted\nDuplicates skipped: $duplicated\nFailed: $failed',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('OK')),
        ],
      ),
    );
  }

  Future<void> _editRow(int index) async {
    final row = _preview[index];
    final plotCtrl = TextEditingController(text: row.plotNo);
    var status = _statuses.contains(row.status) ? row.status : 'Available';

    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setLocal) {
            return AlertDialog(
              backgroundColor: AppColors.panel,
              title: const Text('Edit Plot'),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextField(
                    controller: plotCtrl,
                    decoration: const InputDecoration(labelText: 'Plot No'),
                  ),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<String>(
                    key: ValueKey(status),
                    initialValue: status,
                    decoration: const InputDecoration(labelText: 'Status'),
                    dropdownColor: AppColors.panel,
                    items: _statuses
                        .map((s) => DropdownMenuItem(value: s, child: Text(s)))
                        .toList(),
                    onChanged: (v) {
                      if (v != null) setLocal(() => status = v);
                    },
                  ),
                ],
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(ctx, false),
                  child: const Text('Cancel'),
                ),
                ElevatedButton(
                  onPressed: () => Navigator.pop(ctx, true),
                  child: const Text('OK'),
                ),
              ],
            );
          },
        );
      },
    );

    if (ok != true) {
      plotCtrl.dispose();
      return;
    }

    final newPlot = plotCtrl.text.trim();
    plotCtrl.dispose();
    if (newPlot.isEmpty) {
      _toast('PlotNo khaali nahi ho sakta.');
      return;
    }
    if (!_statuses.contains(status)) {
      _toast('Status sirf Available/Hold/Sold.');
      return;
    }

    if (row.isDb) {
      if (await PlotInventoryStore.plotNoExists(
        societyId: row.societyId,
        blockId: row.blockId,
        plotTypeId: row.plotTypeId,
        plotNo: newPlot,
        excludeId: row.plotId,
      )) {
        _toast('Isi combo me yeh PlotNo pehle se mojood hai.');
        return;
      }
      final up = await PlotInventoryStore.updatePlot(
        id: row.plotId,
        plotNo: newPlot,
        status: status,
      );
      if (!up) {
        _toast('DB update fail.');
        return;
      }
    }

    setState(() {
      row.plotNo = newPlot;
      row.status = status;
    });
  }

  Future<void> _deleteRow(int index) async {
    final row = _preview[index];
    final msg = row.isDb
        ? 'Delete this plot from DATABASE?'
        : 'Remove this row from PREVIEW?';

    final yes = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.panel,
        title: const Text('Delete Confirmation'),
        content: Text(msg),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('No'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Yes'),
          ),
        ],
      ),
    );
    if (yes != true) return;

    if (row.isDb) {
      final del = await PlotInventoryStore.deletePlot(row.plotId);
      if (!del) {
        _toast('DB delete fail ya FK constraint.');
        return;
      }
    }

    setState(() => _preview.removeAt(index));
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
      children: [
        const SectionHeader(
          title: 'Plot Inventory',
          subtitle: 'Preview / Load / Commit',
        ),
        const SizedBox(height: 12),
        SoftPanel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              DropdownButtonFormField<IdName>(
                key: ValueKey(_society?.id ?? 'soc'),
                initialValue: _society,
                decoration: const InputDecoration(labelText: 'Society'),
                dropdownColor: AppColors.panel,
                items: _societies
                    .map(
                      (s) => DropdownMenuItem(value: s, child: Text(s.name)),
                    )
                    .toList(),
                onChanged: _onSocietyChanged,
              ),
              const SizedBox(height: 10),
              DropdownButtonFormField<BlockItem>(
                key: ValueKey('blk-${_society?.id}-${_block?.id}'),
                initialValue: _block,
                decoration: const InputDecoration(labelText: 'Block'),
                dropdownColor: AppColors.panel,
                items: _blocks
                    .map(
                      (b) => DropdownMenuItem(value: b, child: Text(b.name)),
                    )
                    .toList(),
                onChanged: (v) => setState(() => _block = v),
              ),
              const SizedBox(height: 10),
              DropdownButtonFormField<PlotTypeItem>(
                key: ValueKey('pt-${_society?.id}-${_plotType?.id}'),
                initialValue: _plotType,
                decoration: const InputDecoration(labelText: 'Plot Type'),
                dropdownColor: AppColors.panel,
                items: _types
                    .map(
                      (t) => DropdownMenuItem(value: t, child: Text(t.code)),
                    )
                    .toList(),
                onChanged: (v) => setState(() => _plotType = v),
              ),
              const SizedBox(height: 10),
              TextField(
                controller: _startPlot,
                decoration: const InputDecoration(labelText: 'Start Plot No'),
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: InputDecorator(
                      decoration: const InputDecoration(labelText: 'Count'),
                      child: Row(
                        children: [
                          IconButton(
                            onPressed: () {
                              if (_count > 1) setState(() => _count--);
                            },
                            icon: const Icon(Icons.remove_circle_outline),
                          ),
                          Expanded(
                            child: Text(
                              '$_count',
                              textAlign: TextAlign.center,
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                          ),
                          IconButton(
                            onPressed: () {
                              if (_count < 500) setState(() => _count++);
                            },
                            icon: const Icon(Icons.add_circle_outline),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      key: ValueKey(_status),
                      initialValue: _status,
                      decoration: const InputDecoration(labelText: 'Status'),
                      dropdownColor: AppColors.panel,
                      items: _statuses
                          .map(
                            (s) => DropdownMenuItem(value: s, child: Text(s)),
                          )
                          .toList(),
                      onChanged: (v) {
                        if (v != null) setState(() => _status = v);
                      },
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF1E90FF),
                    ),
                    onPressed: _generatePreview,
                    child: const Text('Preview'),
                  ),
                  ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF9370DB),
                    ),
                    onPressed: _loadExisting,
                    child: const Text('Load'),
                  ),
                  ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF2E8B57),
                    ),
                    onPressed: _commit,
                    child: const Text('Commit'),
                  ),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        SectionHeader(
          title: 'Preview grid',
          subtitle:
              '${_preview.length} row(s) · PlotNo / Society / Block / Type / Status',
        ),
        const SizedBox(height: 10),
        if (_preview.isEmpty)
          SoftPanel(
            child: Text(
              'Preview empty — add Society/Block/Plot Type first, then Preview or Load.',
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          )
        else
          ...List.generate(_preview.length, (i) {
            final r = _preview[i];
            return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: SoftPanel(
                padding: const EdgeInsets.fromLTRB(14, 12, 8, 12),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            r.plotNo,
                            style: Theme.of(context).textTheme.titleMedium,
                          ),
                          const SizedBox(height: 2),
                          Text(
                            '${r.society} · ${r.block} · ${r.type}',
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                          const SizedBox(height: 6),
                          Row(
                            children: [
                              StatusChip(r.status),
                              const SizedBox(width: 8),
                              StatusChip(r.isDb ? 'DB' : 'Preview'),
                            ],
                          ),
                        ],
                      ),
                    ),
                    IconButton(
                      tooltip: 'Edit',
                      style: IconButton.styleFrom(
                        backgroundColor: const Color(0xFF1E90FF),
                        foregroundColor: Colors.white,
                      ),
                      onPressed: () => _editRow(i),
                      icon: const Icon(Icons.edit, size: 18),
                    ),
                    const SizedBox(width: 4),
                    IconButton(
                      tooltip: 'Delete',
                      style: IconButton.styleFrom(
                        backgroundColor: const Color(0xFFCD5C5C),
                        foregroundColor: Colors.white,
                      ),
                      onPressed: () => _deleteRow(i),
                      icon: const Icon(Icons.delete_outline, size: 18),
                    ),
                  ],
                ),
              ),
            );
          }),
      ],
    );
  }
}
