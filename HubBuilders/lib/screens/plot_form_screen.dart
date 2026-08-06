import 'package:flutter/material.dart';

import '../data/plot_inventory_store.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same fields as C# `frmPlotTypeFormAdd` — SQLite `plot_types`.
class PlotFormScreen extends StatefulWidget {
  const PlotFormScreen({
    super.key,
    this.id,
    this.societyId,
    this.societyName,
    this.plotSize,
    this.onBack,
    this.isEdit = false,
  });

  final int? id;
  final int? societyId;
  final String? societyName;
  final String? plotSize;
  final VoidCallback? onBack;
  final bool isEdit;

  @override
  State<PlotFormScreen> createState() => _PlotFormScreenState();
}

class _PlotFormScreenState extends State<PlotFormScreen> {
  late final TextEditingController _plotSize;
  IdName? _society;
  List<IdName> _societies = const [];
  bool _loading = true;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _plotSize = TextEditingController(text: widget.plotSize ?? '');
    _load();
  }

  Future<void> _load() async {
    final list = await PlotInventoryStore.societies();
    IdName? selected;
    if (widget.societyId != null) {
      try {
        selected = list.firstWhere((s) => s.id == widget.societyId);
      } catch (_) {}
    }
    if (selected == null && widget.societyName != null) {
      try {
        selected = list.firstWhere((s) => s.name == widget.societyName);
      } catch (_) {}
    }
    if (!mounted) return;
    setState(() {
      _societies = list;
      _society = selected;
      _loading = false;
    });
  }

  @override
  void dispose() {
    _plotSize.dispose();
    super.dispose();
  }

  void _clear() {
    _plotSize.clear();
    setState(() => _society = null);
  }

  Future<void> _save() async {
    if (_society == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Society select karo.')),
      );
      return;
    }
    if (_plotSize.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Plot Size required hai.')),
      );
      return;
    }
    setState(() => _saving = true);
    try {
      if (widget.isEdit && widget.id != null) {
        await PlotInventoryStore.updatePlotType(
          id: widget.id!,
          societyId: _society!.id,
          code: _plotSize.text,
        );
      } else {
        await PlotInventoryStore.insertPlotType(
          societyId: _society!.id,
          code: _plotSize.text,
        );
      }
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Plot type saved.')),
      );
      if (widget.onBack != null) {
        widget.onBack!();
      } else if (Navigator.of(context).canPop()) {
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Save failed: $e')),
      );
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final title = widget.isEdit ? 'Plot Type Edit' : 'PlotTypeForm';
    final content = _loading
        ? const Center(child: CircularProgressIndicator())
        : ListView(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
            children: [
              SectionHeader(
                title: title,
                subtitle: 'Society, Size',
                trailing: widget.onBack == null
                    ? null
                    : IconButton(
                        onPressed: widget.onBack,
                        icon: const Icon(Icons.close),
                      ),
              ),
              const SizedBox(height: 14),
              SoftPanel(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    DropdownButtonFormField<IdName>(
                      key: ValueKey(_society?.id ?? 'none'),
                      initialValue: _society,
                      decoration: const InputDecoration(labelText: 'Society'),
                      dropdownColor: AppColors.panel,
                      items: _societies
                          .map(
                            (s) => DropdownMenuItem(
                              value: s,
                              child: Text(s.name),
                            ),
                          )
                          .toList(),
                      onChanged: (v) => setState(() => _society = v),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _plotSize,
                      decoration:
                          const InputDecoration(labelText: 'Plot Size'),
                    ),
                    const SizedBox(height: 20),
                    Row(
                      children: [
                        Expanded(
                          child: OutlinedButton(
                            onPressed: _clear,
                            child: const Text('Clear'),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: ElevatedButton(
                            onPressed: _saving ? null : _save,
                            child: _saving
                                ? const SizedBox(
                                    height: 18,
                                    width: 18,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                      color: Colors.white,
                                    ),
                                  )
                                : const Text('Save'),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          );

    if (widget.onBack != null) return content;

    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(title: Text(title)),
        body: content,
      ),
    );
  }
}
