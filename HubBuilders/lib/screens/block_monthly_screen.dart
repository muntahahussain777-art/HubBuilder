import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../data/block_monthly_store.dart';
import '../data/hub_sync.dart';
import '../data/staff_access.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Admin/VIP: Society → Block → monthly installment default — same idea as Staff Rules.
class BlockMonthlyScreen extends StatefulWidget {
  const BlockMonthlyScreen({super.key});

  @override
  State<BlockMonthlyScreen> createState() => _BlockMonthlyScreenState();
}

class _BlockMonthlyScreenState extends State<BlockMonthlyScreen> {
  List<({int id, String name})> _societies = const [];
  int? _societyId;
  List<Map<String, Object?>> _rows = const [];
  final Map<int, TextEditingController> _ctrls = {};
  bool _loading = true;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _boot();
  }

  @override
  void dispose() {
    for (final c in _ctrls.values) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _boot() async {
    setState(() => _loading = true);
    final societies = await BlockMonthlyStore.societies();
    final sid = societies.isEmpty ? null : societies.first.id;
    final rows =
        sid == null ? <Map<String, Object?>>[] : await BlockMonthlyStore.loadGrid(sid);
    if (!mounted) return;
    _replaceCtrls(rows);
    setState(() {
      _societies = societies;
      _societyId = sid;
      _rows = rows;
      _loading = false;
    });
  }

  void _replaceCtrls(List<Map<String, Object?>> rows) {
    for (final c in _ctrls.values) {
      c.dispose();
    }
    _ctrls.clear();
    for (final r in rows) {
      final id = (r['block_id'] as num?)?.toInt() ?? 0;
      final m = (r['monthly'] as num?)?.toDouble() ?? 0;
      _ctrls[id] = TextEditingController(
        text: m > 0 ? m.toStringAsFixed(0) : '',
      );
    }
  }

  Future<void> _loadSociety(int? sid) async {
    if (sid == null) return;
    setState(() => _loading = true);
    final rows = await BlockMonthlyStore.loadGrid(sid);
    if (!mounted) return;
    _replaceCtrls(rows);
    setState(() {
      _societyId = sid;
      _rows = rows;
      _loading = false;
    });
  }

  List<Map<String, Object?>> _collect() {
    final out = <Map<String, Object?>>[];
    for (final r in _rows) {
      final id = (r['block_id'] as num?)?.toInt() ?? 0;
      final text = _ctrls[id]?.text.trim() ?? '';
      final monthly = double.tryParse(text) ?? 0;
      out.add({'block_id': id, 'monthly': monthly < 0 ? 0 : monthly});
    }
    return out;
  }

  Future<void> _save() async {
    if (_societyId == null) return;
    if (!StaffAccess.isAdmin) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Only Admin can save block monthly defaults.')),
      );
      return;
    }
    setState(() => _saving = true);
    await BlockMonthlyStore.saveGrid(_collect());
    HubSync.notifyLocalWrite();
    if (!mounted) return;
    setState(() => _saving = false);
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Block monthly amounts save ho gaye.')),
    );
  }

  Future<void> _applyEmpty() async {
    if (!StaffAccess.isAdmin) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Only Admin can apply block defaults.')),
      );
      return;
    }
    final ask = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Apply to empty'),
        content: const Text(
          'Active allocations jinki monthly installment 0/empty hai un par block default set ho jayega.\n\nContinue?',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('No')),
          ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Yes')),
        ],
      ),
    );
    if (ask != true) return;
    setState(() => _saving = true);
    await BlockMonthlyStore.saveGrid(_collect());
    final n = await BlockMonthlyStore.applyToEmptyActive();
    if (!mounted) return;
    setState(() => _saving = false);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('$n Active allocation(s) update ho gayi.')),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: const Text('Block Monthly (VIP)'),
          actions: [
            TextButton(
              onPressed: _saving || _loading ? null : _applyEmpty,
              child: const Text('Apply empty'),
            ),
            TextButton(
              onPressed: _saving || _loading ? null : _save,
              child: Text(_saving ? 'Saving…' : 'Save'),
            ),
          ],
        ),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
                children: [
                  SoftPanel(
                    child: DropdownButtonFormField<int>(
                      value: _societyId,
                      decoration: const InputDecoration(labelText: 'Society'),
                      dropdownColor: AppColors.panel,
                      items: _societies
                          .map(
                            (s) => DropdownMenuItem(
                              value: s.id,
                              child: Text(s.name),
                            ),
                          )
                          .toList(),
                      onChanged: _loadSociety,
                    ),
                  ),
                  const SizedBox(height: 12),
                  if (_rows.isEmpty)
                    SoftPanel(
                      child: Text(
                        'Is society ke liye blocks nahi mile.',
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                    ),
                  for (final r in _rows)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: SoftPanel(
                        child: Row(
                          children: [
                            Expanded(
                              child: Text(
                                '${r['name']}',
                                style: Theme.of(context).textTheme.titleMedium,
                              ),
                            ),
                            const SizedBox(width: 12),
                            SizedBox(
                              width: 140,
                              child: TextField(
                                controller: _ctrls[(r['block_id'] as num?)?.toInt() ?? 0],
                                keyboardType: TextInputType.number,
                                inputFormatters: [
                                  FilteringTextInputFormatter.digitsOnly,
                                ],
                                decoration: const InputDecoration(
                                  labelText: 'Monthly',
                                  isDense: true,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                ],
              ),
      ),
    );
  }
}
