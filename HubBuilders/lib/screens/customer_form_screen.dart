import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/allocate_store.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same fields as C# `frmCustomerAdd` — persists to SQLite `customers`.
class CustomerFormScreen extends StatefulWidget {
  const CustomerFormScreen({
    super.key,
    this.id,
    this.name,
    this.fatherName,
    this.mobile,
    this.cnic,
    this.note,
    this.date,
    this.onBack,
    this.isEdit = false,
  });

  final int? id;
  final String? name;
  final String? fatherName;
  final String? mobile;
  final String? cnic;
  final String? note;
  final DateTime? date;
  final VoidCallback? onBack;
  final bool isEdit;

  @override
  State<CustomerFormScreen> createState() => _CustomerFormScreenState();
}

class _CustomerFormScreenState extends State<CustomerFormScreen> {
  late final TextEditingController _name;
  late final TextEditingController _fatherName;
  late final TextEditingController _mobile;
  late final TextEditingController _cnic;
  late final TextEditingController _note;
  late DateTime _date;
  bool _saving = false;

  final _dateFmt = DateFormat('yyyy-MM-dd');

  @override
  void initState() {
    super.initState();
    _name = TextEditingController(text: widget.name ?? '');
    _fatherName = TextEditingController(text: widget.fatherName ?? '');
    _mobile = TextEditingController(text: widget.mobile ?? '');
    _cnic = TextEditingController(text: widget.cnic ?? '');
    _note = TextEditingController(text: widget.note ?? '');
    _date = widget.date ?? DateTime.now();
  }

  @override
  void dispose() {
    _name.dispose();
    _fatherName.dispose();
    _mobile.dispose();
    _cnic.dispose();
    _note.dispose();
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
    _name.clear();
    _fatherName.clear();
    _mobile.clear();
    _cnic.clear();
    _note.clear();
    setState(() => _date = DateTime.now());
  }

  Future<void> _save() async {
    final name = _name.text.trim();
    if (name.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Name required hai.')),
      );
      return;
    }
    setState(() => _saving = true);
    try {
      if (widget.isEdit && widget.id != null) {
        await AllocateStore.updateCustomer(
          id: widget.id!,
          name: name,
          fatherName: _fatherName.text,
          mobile: _mobile.text,
          cnic: _cnic.text,
          notes: _note.text,
        );
      } else {
        await AllocateStore.insertCustomer(
          name: name,
          fatherName: _fatherName.text,
          mobile: _mobile.text,
          cnic: _cnic.text,
          notes: _note.text,
          createdAt: '${_dateFmt.format(_date)} 00:00:00',
        );
      }
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Customer saved.')),
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

  Future<void> _delete() async {
    if (widget.id == null) return;
    final ok = await confirmDelete(context, 'Delete this customer?');
    if (!ok || !mounted) return;
    setState(() => _saving = true);
    try {
      final res = await AllocateStore.deleteCustomer(widget.id!);
      if (!mounted) return;
      if (!res.ok) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(res.message)),
        );
        return;
      }
      if (widget.onBack != null) {
        widget.onBack!();
      } else if (Navigator.of(context).canPop()) {
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Delete failed: $e')),
      );
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final title = widget.isEdit ? 'Customer Edit' : 'Customer Add';
    final content = ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        SectionHeader(
          title: title,
          subtitle: 'Name, FatherName, Mobile, CNIC, Note, Date',
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
                controller: _name,
                decoration: const InputDecoration(labelText: 'Name'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _fatherName,
                decoration: const InputDecoration(labelText: 'FatherName'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _mobile,
                keyboardType: TextInputType.phone,
                decoration: const InputDecoration(labelText: 'Mobile'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _cnic,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'CNIC No'),
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
              const SizedBox(height: 12),
              TextField(
                controller: _note,
                maxLines: 4,
                decoration: const InputDecoration(labelText: 'Note'),
              ),
              const SizedBox(height: 20),
              if (widget.isEdit && widget.id != null) ...[
                FormDeleteButton(onPressed: _saving ? null : _delete),
                const SizedBox(height: 12),
              ],
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
