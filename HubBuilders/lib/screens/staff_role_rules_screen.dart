import 'package:flutter/material.dart';

import '../data/hub_sync.dart';
import '../data/staff_access.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Admin: set Manager/Operator form open/add/edit/delete — same as WinForms Staff Rules.
class StaffRoleRulesScreen extends StatefulWidget {
  const StaffRoleRulesScreen({super.key});

  @override
  State<StaffRoleRulesScreen> createState() => _StaffRoleRulesScreenState();
}

class _StaffRoleRulesScreenState extends State<StaffRoleRulesScreen> {
  List<String> _roles = const [];
  String? _role;
  List<Map<String, Object?>> _rows = const [];
  bool _loading = true;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _boot();
  }

  Future<void> _boot() async {
    setState(() => _loading = true);
    final roles = await StaffAccess.distinctRoles();
    final role = roles.isEmpty ? null : roles.first;
    final rows = role == null ? <Map<String, Object?>>[] : await StaffAccess.loadRoleGrid(role);
    if (!mounted) return;
    setState(() {
      _roles = roles;
      _role = role;
      _rows = rows.map((e) => Map<String, Object?>.from(e)).toList();
      _loading = false;
    });
  }

  Future<void> _loadRole(String? role) async {
    if (role == null) return;
    setState(() => _loading = true);
    final rows = await StaffAccess.loadRoleGrid(role);
    if (!mounted) return;
    setState(() {
      _role = role;
      _rows = rows.map((e) => Map<String, Object?>.from(e)).toList();
      _loading = false;
    });
  }

  Future<void> _save() async {
    if (_role == null) return;
    if (!StaffAccess.isAdmin) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Only Admin can save staff rules.')),
      );
      return;
    }
    setState(() => _saving = true);
    await StaffAccess.saveRoleGrid(_role!, _rows);
    HubSync.notifyLocalWrite();
    if (!mounted) return;
    setState(() => _saving = false);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Rules save ho gaye: $_role')),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: const Text('Staff Rules'),
          actions: [
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
                    child: DropdownButtonFormField<String>(
                      value: _role,
                      decoration: const InputDecoration(labelText: 'Role'),
                      dropdownColor: AppColors.panel,
                      items: _roles
                          .map(
                            (r) => DropdownMenuItem(value: r, child: Text(r)),
                          )
                          .toList(),
                      onChanged: _loadRole,
                    ),
                  ),
                  const SizedBox(height: 12),
                  for (var i = 0; i < _rows.length; i++)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: SoftPanel(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              '${_rows[i]['title']}',
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                            const SizedBox(height: 6),
                            Wrap(
                              spacing: 8,
                              children: [
                                FilterChip(
                                  label: const Text('Open'),
                                  selected: _rows[i]['open'] == true,
                                  onSelected: (v) =>
                                      setState(() => _rows[i]['open'] = v),
                                ),
                                FilterChip(
                                  label: const Text('Add'),
                                  selected: _rows[i]['add'] == true,
                                  onSelected: (v) =>
                                      setState(() => _rows[i]['add'] = v),
                                ),
                                FilterChip(
                                  label: const Text('Edit'),
                                  selected: _rows[i]['edit'] == true,
                                  onSelected: (v) =>
                                      setState(() => _rows[i]['edit'] = v),
                                ),
                                FilterChip(
                                  label: const Text('Delete'),
                                  selected: _rows[i]['delete'] == true,
                                  onSelected: (v) =>
                                      setState(() => _rows[i]['delete'] = v),
                                ),
                              ],
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
