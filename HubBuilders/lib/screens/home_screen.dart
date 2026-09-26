import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/dashboard_store.dart';
import '../data/hub_sync.dart';
import '../data/nav_items.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.onOpen});

  final ValueChanged<HubRoute> onOpen;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  DashboardStats? _stats;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    HubSync.revision.addListener(_onSync);
    _load();
  }

  @override
  void dispose() {
    HubSync.revision.removeListener(_onSync);
    super.dispose();
  }

  void _onSync() {
    if (mounted) _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final s = await DashboardStore.load();
    if (!mounted) return;
    setState(() {
      _stats = s;
      _loading = false;
    });
  }

  String _fmtReceipts(double v) {
    if (v >= 1000000) return '${(v / 1000000).toStringAsFixed(1)}M';
    if (v >= 1000) return NumberFormat('#,##0').format(v);
    return v.toStringAsFixed(0);
  }

  @override
  Widget build(BuildContext context) {
    final shortcuts =
        sidebarItems.where((e) => e.route != HubRoute.home).take(8);
    final s = _stats;

    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        SoftPanel(
          padding: const EdgeInsets.fromLTRB(20, 22, 20, 22),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'HubBuilders',
                style: Theme.of(context).textTheme.displayMedium?.copyWith(
                      color: AppColors.sand,
                    ),
              ),
              const SizedBox(height: 8),
              Text(
                'Builders control desk for societies, plots, allocations & cash.',
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                      color: AppColors.mist,
                    ),
              ),
              const SizedBox(height: 18),
              Wrap(
                spacing: 10,
                runSpacing: 10,
                children: [
                  ElevatedButton.icon(
                    onPressed: () => widget.onOpen(HubRoute.customer),
                    icon: const Icon(Icons.people_outline, size: 18),
                    label: const Text('Customers'),
                  ),
                  OutlinedButton.icon(
                    onPressed: () => widget.onOpen(HubRoute.allocate),
                    icon: const Icon(Icons.assignment_outlined, size: 18),
                    label: const Text('Allocate'),
                  ),
                  OutlinedButton.icon(
                    onPressed: () => widget.onOpen(HubRoute.reports),
                    icon: const Icon(Icons.assessment_outlined, size: 18),
                    label: const Text('Reports'),
                  ),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 18),
        const SectionHeader(
          title: 'Today at a glance',
          subtitle: 'Live counts from database',
        ),
        const SizedBox(height: 12),
        if (_loading)
          const SoftPanel(
            child: Center(child: CircularProgressIndicator()),
          )
        else
          Row(
            children: [
              Expanded(
                child: _StatTile(
                  label: 'Available',
                  value: '${s?.availablePlots ?? 0}',
                  tip: 'Plots',
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _StatTile(
                  label: 'Bookings',
                  value: '${s?.activeBookings ?? 0}',
                  tip: 'Allocated',
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _StatTile(
                  label: 'Receipts',
                  value: _fmtReceipts(s?.monthReceipts ?? 0),
                  tip: 'This month',
                ),
              ),
            ],
          ),
        const SizedBox(height: 22),
        const SectionHeader(
          title: 'Modules',
          subtitle: 'Open any module from the sidebar',
        ),
        const SizedBox(height: 12),
        ...shortcuts.map(
          (item) => Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: SoftPanel(
              onTap: () => widget.onOpen(item.route),
              child: Row(
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(
                      color: AppColors.teal.withValues(alpha: 0.16),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(item.icon, color: AppColors.tealBright),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Text(
                      item.label,
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                  ),
                  const Icon(Icons.chevron_right, color: AppColors.mist),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _StatTile extends StatelessWidget {
  const _StatTile({
    required this.label,
    required this.value,
    required this.tip,
  });

  final String label;
  final String value;
  final String tip;

  @override
  Widget build(BuildContext context) {
    return SoftPanel(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: Theme.of(context).textTheme.bodyMedium),
          const SizedBox(height: 6),
          Text(
            value,
            style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  color: AppColors.tealBright,
                ),
          ),
          Text(
            tip,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontSize: 11),
          ),
        ],
      ),
    );
  }
}
