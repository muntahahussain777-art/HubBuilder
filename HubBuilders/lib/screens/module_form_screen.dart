import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

class FormFieldSpec {
  const FormFieldSpec(
    this.label, {
    this.initial,
    this.maxLines = 1,
    this.keyboard,
  });

  final String label;
  final String? initial;
  final int maxLines;
  final TextInputType? keyboard;
}

class ModuleFormScreen extends StatefulWidget {
  const ModuleFormScreen({
    super.key,
    required this.title,
    required this.fields,
    this.subtitle,
    this.onBack,
  });

  final String title;
  final String? subtitle;
  final List<FormFieldSpec> fields;
  final VoidCallback? onBack;

  @override
  State<ModuleFormScreen> createState() => _ModuleFormScreenState();
}

class _ModuleFormScreenState extends State<ModuleFormScreen> {
  late final List<TextEditingController> _controllers;

  @override
  void initState() {
    super.initState();
    _controllers = widget.fields
        .map((f) => TextEditingController(text: f.initial ?? ''))
        .toList();
  }

  @override
  void dispose() {
    for (final c in _controllers) {
      c.dispose();
    }
    super.dispose();
  }

  void _save() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Saved.'),
      ),
    );
    if (widget.onBack != null) {
      widget.onBack!();
    } else if (Navigator.of(context).canPop()) {
      Navigator.of(context).pop();
    }
  }

  void _clear() {
    for (final c in _controllers) {
      c.clear();
    }
    // Restore nationality-style defaults if present
    for (var i = 0; i < widget.fields.length; i++) {
      if (widget.fields[i].initial != null) {
        _controllers[i].text = widget.fields[i].initial!;
      }
    }
    setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final embedded = widget.onBack != null;

    final content = ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        SectionHeader(
          title: widget.title,
          subtitle: widget.subtitle ?? 'Dummy form for UI review',
          trailing: embedded
              ? IconButton(
                  onPressed: widget.onBack,
                  icon: const Icon(Icons.close),
                )
              : null,
        ),
        const SizedBox(height: 14),
        SoftPanel(
          child: Column(
            children: [
              for (var i = 0; i < widget.fields.length; i++) ...[
                if (i > 0) const SizedBox(height: 12),
                TextField(
                  controller: _controllers[i],
                  maxLines: widget.fields[i].maxLines,
                  keyboardType: widget.fields[i].keyboard,
                  decoration: InputDecoration(
                    labelText: widget.fields[i].label,
                  ),
                ),
              ],
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
                      onPressed: _save,
                      child: const Text('Save'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Text(
          'Form preview — data save next step.',
          style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                color: AppColors.mist.withValues(alpha: 0.85),
              ),
        ),
      ],
    );

    if (embedded) {
      return content;
    }

    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: Text(widget.title),
        ),
        body: content,
      ),
    );
  }
}
