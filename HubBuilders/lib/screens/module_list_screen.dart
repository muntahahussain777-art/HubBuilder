import 'package:flutter/material.dart';

import '../data/dummy_data.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

class ModuleListScreen extends StatelessWidget {
  const ModuleListScreen({
    super.key,
    required this.title,
    required this.subtitle,
    required this.rows,
    this.onAdd,
    this.onOpenRow,
    this.addLabel = 'Add',
    this.showFab = true,
  });

  final String title;
  final String subtitle;
  final List<DummyRow> rows;
  final VoidCallback? onAdd;
  final ValueChanged<DummyRow>? onOpenRow;
  final String addLabel;
  final bool showFab;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      floatingActionButton: showFab && onAdd != null
          ? FloatingActionButton.extended(
              onPressed: onAdd,
              icon: const Icon(Icons.add),
              label: Text(addLabel),
            )
          : null,
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 100),
        children: [
          SectionHeader(title: title, subtitle: subtitle),
          const SizedBox(height: 14),
          SoftPanel(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
            child: TextField(
              decoration: const InputDecoration(
                hintText: 'Search dummy records…',
                prefixIcon: Icon(Icons.search),
                border: InputBorder.none,
                enabledBorder: InputBorder.none,
                focusedBorder: InputBorder.none,
                filled: false,
              ),
            ),
          ),
          const SizedBox(height: 14),
          ...rows.map(
            (row) => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: SoftPanel(
                onTap: onOpenRow == null ? null : () => onOpenRow!(row),
                child: Row(
                  children: [
                    Container(
                      width: 46,
                      height: 46,
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(12),
                        color: AppColors.panelLift,
                      ),
                      alignment: Alignment.center,
                      child: Text(
                        row.title.isEmpty ? '?' : row.title[0].toUpperCase(),
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              color: AppColors.tealBright,
                            ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(row.title, style: Theme.of(context).textTheme.titleMedium),
                          const SizedBox(height: 2),
                          Text(row.subtitle, style: Theme.of(context).textTheme.bodyMedium),
                        ],
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          row.meta,
                          style: Theme.of(context).textTheme.labelLarge?.copyWith(
                                color: AppColors.sand,
                              ),
                        ),
                        if (row.tag != null) ...[
                          const SizedBox(height: 6),
                          StatusChip(row.tag!),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
