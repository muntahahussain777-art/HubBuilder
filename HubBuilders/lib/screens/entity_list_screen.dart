import 'package:flutter/material.dart';

import '../data/dummy_data.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Async list screen fed by SQLite (replaces DummyData lists).
class EntityListScreen extends StatefulWidget {
  const EntityListScreen({
    super.key,
    required this.title,
    required this.subtitle,
    required this.loader,
    this.onAdd,
    this.onOpenRow,
    this.addLabel = 'Add',
    this.showFab = true,
  });

  final String title;
  final String subtitle;
  final Future<List<ListRow>> Function() loader;
  final VoidCallback? onAdd;
  final ValueChanged<ListRow>? onOpenRow;
  final String addLabel;
  final bool showFab;

  @override
  State<EntityListScreen> createState() => _EntityListScreenState();
}

class _EntityListScreenState extends State<EntityListScreen> {
  List<ListRow> _rows = const [];
  List<ListRow> _filtered = const [];
  bool _loading = true;
  String _query = '';

  @override
  void initState() {
    super.initState();
    _reload();
  }

  @override
  void didUpdateWidget(covariant EntityListScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.title != widget.title) {
      _reload();
    }
  }

  Future<void> _reload() async {
    setState(() => _loading = true);
    final rows = await widget.loader();
    if (!mounted) return;
    setState(() {
      _rows = rows;
      _applyFilter();
      _loading = false;
    });
  }

  void _applyFilter() {
    final q = _query.trim().toLowerCase();
    if (q.isEmpty) {
      _filtered = _rows;
      return;
    }
    _filtered = _rows
        .where(
          (r) =>
              r.title.toLowerCase().contains(q) ||
              r.subtitle.toLowerCase().contains(q) ||
              r.meta.toLowerCase().contains(q) ||
              (r.mobile ?? '').contains(q) ||
              (r.cnic ?? '').toLowerCase().contains(q),
        )
        .toList();
  }

  Future<void> _handleAdd() async {
    widget.onAdd?.call();
    // Give form time to pop then refresh — parent should call setState on route change.
    await Future<void>.delayed(const Duration(milliseconds: 300));
    if (mounted) await _reload();
  }

  Future<void> _handleOpen(ListRow row) async {
    widget.onOpenRow?.call(row);
    await Future<void>.delayed(const Duration(milliseconds: 300));
    if (mounted) await _reload();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      floatingActionButton: widget.showFab && widget.onAdd != null
          ? FloatingActionButton.extended(
              onPressed: _handleAdd,
              icon: const Icon(Icons.add),
              label: Text(widget.addLabel),
            )
          : null,
      body: RefreshIndicator(
        onRefresh: _reload,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 100),
          children: [
            SectionHeader(title: widget.title, subtitle: widget.subtitle),
            const SizedBox(height: 14),
            SoftPanel(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
              child: TextField(
                decoration: const InputDecoration(
                  hintText: 'Search records…',
                  prefixIcon: Icon(Icons.search),
                  border: InputBorder.none,
                  enabledBorder: InputBorder.none,
                  focusedBorder: InputBorder.none,
                  filled: false,
                ),
                onChanged: (v) => setState(() {
                  _query = v;
                  _applyFilter();
                }),
              ),
            ),
            const SizedBox(height: 14),
            if (_loading)
              const Padding(
                padding: EdgeInsets.all(32),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (_filtered.isEmpty)
              SoftPanel(
                child: Text(
                  'No records yet. Tap ${widget.addLabel} to add.',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              )
            else
              ..._filtered.map(
                (row) => Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: SoftPanel(
                    onTap: widget.onOpenRow == null
                        ? null
                        : () => _handleOpen(row),
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
                            row.title.isEmpty
                                ? '?'
                                : row.title[0].toUpperCase(),
                            style: Theme.of(context)
                                .textTheme
                                .titleLarge
                                ?.copyWith(color: AppColors.tealBright),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                row.title,
                                style: Theme.of(context).textTheme.titleMedium,
                              ),
                              const SizedBox(height: 2),
                              Text(
                                row.subtitle,
                                style: Theme.of(context).textTheme.bodyMedium,
                              ),
                            ],
                          ),
                        ),
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            Text(
                              row.meta,
                              style: Theme.of(context)
                                  .textTheme
                                  .labelLarge
                                  ?.copyWith(color: AppColors.sand),
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
      ),
    );
  }
}
