import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

class ApplicationFormScreen extends StatefulWidget {
  const ApplicationFormScreen({
    super.key,
    required this.blankOverlay,
    this.onBack,
  });

  final bool blankOverlay;
  final VoidCallback? onBack;

  @override
  State<ApplicationFormScreen> createState() => _ApplicationFormScreenState();
}

class _ApplicationFormScreenState extends State<ApplicationFormScreen> {
  final _name = TextEditingController(text: 'Ahmed Khan');
  final _father = TextEditingController(text: 'Muhammad Khan');
  final _cnic = TextEditingController(text: '35202-1234567-1');
  final _phone = TextEditingController(text: '0300-1234567');
  final _address = TextEditingController(text: 'Street 4, Model Town');
  final _city = TextEditingController(text: 'Sargodha');
  final _nationality = TextEditingController(text: 'Pakistani');
  final _plot = TextEditingController(text: 'A-12');
  final _size = TextEditingController(text: '5 Marla');
  final _place = TextEditingController(text: 'Sargodha');
  final _day = TextEditingController(text: '29');
  final _month = TextEditingController(text: '07');
  final _year = TextEditingController(text: '2026');

  @override
  void dispose() {
    for (final c in [
      _name,
      _father,
      _cnic,
      _phone,
      _address,
      _city,
      _nationality,
      _plot,
      _size,
      _place,
      _day,
      _month,
      _year,
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  void _clear() {
    for (final c in [
      _name,
      _father,
      _cnic,
      _phone,
      _address,
      _city,
      _plot,
      _size,
      _place,
      _day,
      _month,
      _year,
    ]) {
      c.clear();
    }
    _nationality.text = 'Pakistani';
    setState(() {});
  }

  void _preview() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: AppColors.inkSoft,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(22)),
      ),
      builder: (context) {
        return Padding(
          padding: EdgeInsets.fromLTRB(
            20,
            16,
            20,
            20 + MediaQuery.paddingOf(context).bottom,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Center(
                child: Container(
                  width: 42,
                  height: 4,
                  decoration: BoxDecoration(
                    color: AppColors.line,
                    borderRadius: BorderRadius.circular(99),
                  ),
                ),
              ),
              const SizedBox(height: 14),
              Text(
                widget.blankOverlay
                    ? 'Blank overlay preview'
                    : 'Filled form preview',
                style: Theme.of(context).textTheme.headlineMedium,
              ),
              const SizedBox(height: 6),
              Text(
                widget.blankOverlay
                    ? 'White page + field text only (Text Only print).'
                    : 'Designed application sheet with filled applicant data.',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 16),
              SoftPanel(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _line('Applicant', _name.text),
                    _line('S/O / W/O', _father.text),
                    _line('CNIC', _cnic.text),
                    _line('Phone', _phone.text),
                    _line('Address', _address.text),
                    _line('City', _city.text),
                    _line('Nationality', _nationality.text),
                    _line('Plot', '${_plot.text} · ${_size.text}'),
                    _line('Place / Date',
                        '${_place.text} · ${_day.text}/${_month.text}/${_year.text}'),
                  ],
                ),
              ),
              const SizedBox(height: 14),
              ElevatedButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Close preview'),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _line(String k, String v) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100,
            child: Text(k, style: Theme.of(context).textTheme.bodyMedium),
          ),
          Expanded(
            child: Text(
              v.isEmpty ? '—' : v,
              style: Theme.of(context).textTheme.titleMedium,
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        SectionHeader(
          title: widget.blankOverlay
              ? 'Fill on Blank Form'
              : 'Application Form',
          subtitle: widget.blankOverlay
              ? 'Text-only overlay seats · درخواست فارم'
              : 'درخواست فارم',
          trailing: widget.onBack == null
              ? null
              : IconButton(
                  onPressed: widget.onBack,
                  icon: const Icon(Icons.arrow_back),
                ),
        ),
        const SizedBox(height: 12),
        SoftPanel(
          child: Column(
            children: [
              TextField(controller: _name, decoration: const InputDecoration(labelText: 'Applicant Name')),
              const SizedBox(height: 12),
              TextField(controller: _father, decoration: const InputDecoration(labelText: 'Father / Husband')),
              const SizedBox(height: 12),
              TextField(controller: _cnic, decoration: const InputDecoration(labelText: 'CNIC')),
              const SizedBox(height: 12),
              TextField(controller: _phone, keyboardType: TextInputType.phone, decoration: const InputDecoration(labelText: 'Phone')),
              const SizedBox(height: 12),
              TextField(controller: _address, maxLines: 2, decoration: const InputDecoration(labelText: 'Address')),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(child: TextField(controller: _city, decoration: const InputDecoration(labelText: 'City'))),
                  const SizedBox(width: 10),
                  Expanded(child: TextField(controller: _nationality, decoration: const InputDecoration(labelText: 'Nationality'))),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(child: TextField(controller: _plot, decoration: const InputDecoration(labelText: 'Plot No'))),
                  const SizedBox(width: 10),
                  Expanded(child: TextField(controller: _size, decoration: const InputDecoration(labelText: 'Size'))),
                ],
              ),
              const SizedBox(height: 12),
              TextField(controller: _place, decoration: const InputDecoration(labelText: 'Place')),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(child: TextField(controller: _day, decoration: const InputDecoration(labelText: 'Day'))),
                  const SizedBox(width: 8),
                  Expanded(child: TextField(controller: _month, decoration: const InputDecoration(labelText: 'Month'))),
                  const SizedBox(width: 8),
                  Expanded(child: TextField(controller: _year, decoration: const InputDecoration(labelText: 'Year'))),
                ],
              ),
              const SizedBox(height: 18),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _clear,
                      child: const Text('Clear'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _preview,
                      child: Text(widget.blankOverlay ? 'Preview Overlay' : 'Print Preview'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}
