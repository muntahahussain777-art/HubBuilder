import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/plot_inventory_store.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same fields as C# `frmSocietyForm` — SQLite `societies`.
class SocietyFormScreen extends StatefulWidget {
  const SocietyFormScreen({
    super.key,
    this.id,
    this.societyName,
    this.city,
    this.date,
    this.onBack,
    this.isEdit = false,
  });

  final int? id;
  final String? societyName;
  final String? city;
  final DateTime? date;
  final VoidCallback? onBack;
  final bool isEdit;

  @override
  State<SocietyFormScreen> createState() => _SocietyFormScreenState();
}

class _SocietyFormScreenState extends State<SocietyFormScreen> {
  late final TextEditingController _societyName;
  late final TextEditingController _city;
  late DateTime _date;
  bool _saving = false;
  final _dateFmt = DateFormat('yyyy-MM-dd');

  @override
  void initState() {
    super.initState();
    _societyName = TextEditingController(text: widget.societyName ?? '');
    _city = TextEditingController(text: widget.city ?? '');
    _date = widget.date ?? DateTime.now();
  }

  @override
  void dispose() {
    _societyName.dispose();
    _city.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(1753),
      lastDate: DateTime(9998),
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.dark(
              primary: AppColors.teal,
              onPrimary: Colors.white,
              surface: AppColors.panel,
              onSurface: AppColors.sand,
            ),
          ),
          child: child!,
        );
      },
    );
    if (picked != null) setState(() => _date = picked);
  }

  void _clear() {
    _societyName.clear();
    _city.clear();
    setState(() => _date = DateTime.now());
  }

  Future<void> _save() async {
    final name = _societyName.text.trim();
    if (name.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Society Name required hai.')),
      );
      return;
    }
    setState(() => _saving = true);
    try {
      if (widget.isEdit && widget.id != null) {
        await PlotInventoryStore.updateSociety(
          id: widget.id!,
          name: name,
          city: _city.text,
        );
      } else {
        await PlotInventoryStore.insertSociety(
          name: name,
          city: _city.text,
          createdAt: '${_dateFmt.format(_date)} 00:00:00',
        );
      }
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Society saved.')),
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
    final title = widget.isEdit ? 'Society Edit' : 'SocietyFormAdd';
    final content = ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        SectionHeader(
          title: title,
          subtitle: 'Society, City, Date',
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
              TextField(
                controller: _societyName,
                decoration: const InputDecoration(labelText: 'Society Name'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _city,
                decoration: const InputDecoration(labelText: 'City'),
              ),
              const SizedBox(height: 12),
              InkWell(
                onTap: _pickDate,
                borderRadius: BorderRadius.circular(12),
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Date',
                    suffixIcon: Icon(Icons.calendar_today_outlined),
                  ),
                  child: Text(
                    _dateFmt.format(_date),
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
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
