import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/staff_audit.dart';
import '../data/vendor_store.dart';
import '../data/whatsapp_open.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same as WinForms frmVipStaffSalaryMessage — staff salary WhatsApp to owner.
class StaffSalaryVipScreen extends StatefulWidget {
  const StaffSalaryVipScreen({super.key, this.onBack});

  final VoidCallback? onBack;

  @override
  State<StaffSalaryVipScreen> createState() => _StaffSalaryVipScreenState();
}

class _StaffSalaryVipScreenState extends State<StaffSalaryVipScreen> {
  final _owner = TextEditingController();
  final _number = TextEditingController();
  late final _staff = TextEditingController(
    text: (StaffAudit.displayName ?? StaffAudit.username ?? '').trim(),
  );
  late final _month = TextEditingController(
    text: DateFormat('MMMM yyyy').format(DateTime.now()),
  );
  final _message = TextEditingController();

  List<String> _owners = const [];
  List<String> _hits = const [];
  bool _showHits = false;
  bool _filling = false;
  bool _busy = false;
  bool _messageEdited = false;

  @override
  void initState() {
    super.initState();
    _owner.addListener(_onOwnerTyped);
    _staff.addListener(_refreshMessage);
    _month.addListener(_refreshMessage);
    _message.addListener(_onMessageTyped);
    _fillMessage();
    _loadOwners();
  }

  @override
  void dispose() {
    _owner.removeListener(_onOwnerTyped);
    _staff.removeListener(_refreshMessage);
    _month.removeListener(_refreshMessage);
    _message.removeListener(_onMessageTyped);
    for (final c in [_owner, _number, _staff, _month, _message]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _loadOwners() async {
    try {
      final rows = await VendorStore.ownerRows();
      if (!mounted) return;
      setState(() {
        _owners = rows
            .map((r) => r.title.trim())
            .where((n) => n.isNotEmpty)
            .toList();
      });
    } catch (_) {}
  }

  void _onOwnerTyped() {
    if (_filling) return;
    final q = _owner.text.trim().toLowerCase();
    final hits = q.isEmpty
        ? const <String>[]
        : _owners
            .where((n) => n.toLowerCase().contains(q))
            .take(8)
            .toList();
    setState(() {
      _hits = hits;
      _showHits = hits.isNotEmpty;
    });
    _refreshMessage();
  }

  void _onMessageTyped() {
    if (_filling) return;
    _messageEdited = true;
  }

  void _refreshMessage() {
    if (_filling || _messageEdited) return;
    _fillMessage();
  }

  void _fillMessage() {
    _filling = true;
    _message.text = buildVipStaffSalaryMessage(
      ownerName: _owner.text,
      staffName: _staff.text,
      month: _month.text,
    );
    _filling = false;
    _messageEdited = false;
  }

  void _applyOwner(String name) {
    _filling = true;
    _owner.text = name;
    _filling = false;
    setState(() {
      _hits = const [];
      _showHits = false;
    });
    _fillMessage();
  }

  void _clear() {
    _filling = true;
    _owner.clear();
    _number.clear();
    _month.text = DateFormat('MMMM yyyy').format(DateTime.now());
    _filling = false;
    _fillMessage();
    setState(() {
      _hits = const [];
      _showHits = false;
      _messageEdited = false;
    });
  }

  Future<void> _sendWhatsApp() async {
    final digits = WhatsAppOpen.normalizePhone(_number.text);
    if (digits.isEmpty || digits.length < 11) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Owner ka WhatsApp number likhein (03xx...).'),
        ),
      );
      return;
    }
    var msg = _message.text.trim();
    if (msg.isEmpty) {
      _fillMessage();
      msg = _message.text.trim();
    }
    setState(() => _busy = true);
    try {
      final ok = await WhatsAppOpen.send(mobile: digits, message: msg);
      if (!mounted) return;
      if (!ok) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('WhatsApp open nahi hua.')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('WhatsApp failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _f(
    TextEditingController c,
    String label, {
    int maxLines = 1,
    TextInputType? keyboard,
  }) =>
      Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: TextField(
          controller: c,
          maxLines: maxLines,
          keyboardType: keyboard,
          decoration: InputDecoration(labelText: label),
        ),
      );

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
      children: [
        SoftPanel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  if (widget.onBack != null)
                    IconButton(
                      onPressed: widget.onBack,
                      icon: const Icon(Icons.arrow_back),
                    ),
                  Expanded(
                    child: Text(
                      'Staff Salary Message (VIP)',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    TextField(
                      controller: _owner,
                      decoration: const InputDecoration(
                        labelText: 'Owner name (type to search)',
                      ),
                    ),
                    if (_showHits)
                      ..._hits.map(
                        (n) => ListTile(
                          dense: true,
                          title: Text(n),
                          onTap: () => _applyOwner(n),
                        ),
                      ),
                  ],
                ),
              ),
              _f(
                _number,
                'Owner WhatsApp number (03xx…)',
                keyboard: TextInputType.phone,
              ),
              _f(_staff, 'Staff name (aap ka naam)'),
              _f(_month, 'Salary month'),
              _f(
                _message,
                'VIP WhatsApp message (edit kar sakte ho)',
                maxLines: 14,
              ),
              Text(
                'Message Hub Builders ke naam se hai. Number khud likho, phir WhatsApp.',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      color: AppColors.mist,
                    ),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy
                          ? null
                          : () {
                              _messageEdited = false;
                              _fillMessage();
                              setState(() {});
                            },
                      child: const Text('Default VIP Message'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _clear,
                      child: const Text('Clear'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              ElevatedButton(
                onPressed: _busy ? null : _sendWhatsApp,
                child: Text(_busy ? 'Opening…' : 'WhatsApp'),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

String buildVipStaffSalaryMessage({
  required String ownerName,
  required String staffName,
  required String month,
  DateTime? when,
}) {
  final now = when ?? DateTime.now();
  var owner = ownerName.trim();
  var staff = staffName.trim();
  var mon = month.trim();
  if (owner.isEmpty) owner = 'Janab';
  if (staff.isEmpty) staff = 'Hub Builders Staff';
  if (mon.isEmpty) mon = DateFormat('MMMM yyyy').format(now);
  final date = DateFormat('dd-MMM-yyyy').format(now);
  return 'Assalam o Alaikum $owner,\n'
      '\n'
      '*HUB BUILDERS*\n'
      'Staff Salary Request\n'
      'Date: $date\n'
      '\n'
      'Ehtiram ke sath darkhwast hai.\n'
      '\n'
      'Main *$staff*, Hub Builders ka staff hoon.\n'
      'Barah-e-karam arz hai ke meri *$mon* ki salary abhi tak receive nahi hui.\n'
      '\n'
      'Allah ta\'ala aap ko sehat, khushhali aur barkat ata farmaye.\n'
      'Agar is mahine ki salary release farma dein to mein dil se shukrguzar rahunga.\n'
      '\n'
      'Shukriya.\n'
      'Wassalam,\n'
      '$staff\n'
      'Hub Builders\n';
}
