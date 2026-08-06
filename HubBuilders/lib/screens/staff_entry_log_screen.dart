import 'package:flutter/material.dart';

import '../data/staff_audit.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Kis staff ne konsi entry ki — same staff_entry_log as WinForms.
class StaffEntryLogScreen extends StatefulWidget {
  const StaffEntryLogScreen({super.key});

  @override
  State<StaffEntryLogScreen> createState() => _StaffEntryLogScreenState();
}

class _StaffEntryLogScreenState extends State<StaffEntryLogScreen> {
  final _search = TextEditingController();
  List<Map<String, Object?>> _rows = const [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final rows = await StaffAudit.recentEntries(search: _search.text);
    if (!mounted) return;
    setState(() {
      _rows = rows;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.ink,
      appBar: AppBar(
        title: const Text('Staff Entry Log'),
        actions: [
          IconButton(
            tooltip: 'Refresh',
            onPressed: _load,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: TextField(
              controller: _search,
              decoration: const InputDecoration(
                hintText: 'Search staff / module / action',
                prefixIcon: Icon(Icons.search),
              ),
              onChanged: (_) => _load(),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text(
              _rows.isEmpty
                  ? 'Abhi log khali hai. Staff login karke entry save kare — yahan dikhegi.'
                  : 'Staff User / Name se pata chalega kis ne Insert/Update/Delete ki.',
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: AppColors.mist.withValues(alpha: 0.9),
                  ),
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : ListView.separated(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                    itemCount: _rows.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 8),
                    itemBuilder: (context, i) {
                      final r = _rows[i];
                      final staff = (r['staff_name'] as String?)?.trim().isNotEmpty == true
                          ? r['staff_name']
                          : r['staff_username'];
                      final action = r['action_type'] ?? '';
                      final module = r['module_name'] ?? '';
                      final when = r['created_at'] ?? '';
                      final detail = r['detail'] ?? '';
                      final role = r['staff_role'] ?? '';
                      return SoftCard(
                        child: ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(
                            '$staff  ·  $action  ·  $module',
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                          subtitle: Text(
                            [
                              if (role.toString().isNotEmpty) 'Role: $role',
                              'Time: $when',
                              if (detail.toString().trim().isNotEmpty) '$detail',
                            ].join('\n'),
                          ),
                          isThreeLine: true,
                        ),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}
