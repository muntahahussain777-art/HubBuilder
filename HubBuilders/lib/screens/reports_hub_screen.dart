import 'package:flutter/material.dart';

import '../data/nav_items.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

class ReportsHubScreen extends StatelessWidget {
  const ReportsHubScreen({super.key, required this.onOpen});

  final ValueChanged<HubRoute> onOpen;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        const SectionHeader(
          title: 'Reports',
          subtitle: 'All Report Form buttons',
        ),
        const SizedBox(height: 14),
        ...reportActions.map(
          (action) => Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: SoftPanel(
              onTap: () {
                if (action.route != null) {
                  onOpen(action.route!);
                  return;
                }
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text('${action.title} — report shell (PDF later).'),
                  ),
                );
              },
              child: Row(
                children: [
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(14),
                      gradient: LinearGradient(
                        colors: [
                          AppColors.teal.withValues(alpha: 0.35),
                          AppColors.panelLift,
                        ],
                      ),
                    ),
                    child: Icon(action.icon, color: AppColors.tealBright),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          action.title,
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                        const SizedBox(height: 3),
                        Text(
                          action.subtitle,
                          style: Theme.of(context).textTheme.bodyMedium,
                        ),
                      ],
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
