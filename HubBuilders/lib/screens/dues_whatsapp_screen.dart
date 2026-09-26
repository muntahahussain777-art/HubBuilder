import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';

import '../data/dues_whatsapp_store.dart';
import '../data/plot_inventory_store.dart';
import '../data/whatsapp_open.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same as WinForms frmDuesWhatsAppList — dues + missed months + WhatsApp.
class DuesWhatsAppScreen extends StatefulWidget {
  const DuesWhatsAppScreen({super.key, this.onBack});

  final VoidCallback? onBack;

  @override
  State<DuesWhatsAppScreen> createState() => _DuesWhatsAppScreenState();
}

class _DuesWhatsAppScreenState extends State<DuesWhatsAppScreen> {
  final _search = TextEditingController();
  final _money = NumberFormat('#,##0');
  final _dateFmt = DateFormat('dd-MMM-yyyy');

  List<IdName> _societies = const [];
  IdName? _society;
  int _minMissed = 1;
  late DateTime _asOf;
  List<DuesWhatsAppRow> _rows = const [];
  bool _loading = false;
  bool _boot = true;

  @override
  void initState() {
    super.initState();
    _asOf = DateTime.now();
    _bootstrap();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    final socs = await DuesWhatsAppStore.societies();
    if (!mounted) return;
    setState(() {
      _societies = [const IdName(0, '(All Societies)'), ...socs];
      _society = _societies.first;
      _boot = false;
    });
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final rows = await DuesWhatsAppStore.load(
      societyId: _society?.id ?? 0,
      minMissedMonths: _minMissed,
      asOf: _asOf,
      keyword: _search.text,
    );
    if (!mounted) return;
    setState(() {
      _rows = rows;
      _loading = false;
    });
    if (rows.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Koi dues / missed-month plot nahi mila.')),
      );
    }
  }

  Future<void> _pickAsOf() async {
    final d = await showDatePicker(
      context: context,
      initialDate: _asOf,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (d != null) setState(() => _asOf = d);
  }

  Future<void> _sendWhatsApp(DuesWhatsAppRow r) async {
    if (DuesWhatsAppStore.normalizePhone(r.mobile).isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Customer ka mobile number nahi mila.')),
      );
      return;
    }
    final ok = await WhatsAppOpen.send(
      mobile: r.mobile,
      message: r.whatsAppMessage(),
    );
    if (!ok && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('WhatsApp open nahi hua.')),
      );
    }
  }

  Future<void> _sendSms(DuesWhatsAppRow r) async {
    final digits = DuesWhatsAppStore.normalizePhone(r.mobile);
    if (digits.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Customer ka mobile number nahi mila.')),
      );
      return;
    }
    final phone = digits.startsWith('92') ? '+$digits' : digits;
    final uri = Uri(
      scheme: 'sms',
      path: phone,
      queryParameters: {'body': r.whatsAppMessage()},
    );
    final ok = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (!ok && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Message app open nahi hua.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_boot) {
      return const Center(child: CircularProgressIndicator());
    }
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
      children: [
        SoftPanel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
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
                      'Dues WhatsApp Reminder',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              DropdownButtonFormField<IdName>(
                value: _society,
                decoration: const InputDecoration(labelText: 'Society'),
                items: _societies
                    .map((s) => DropdownMenuItem(value: s, child: Text(s.name)))
                    .toList(),
                onChanged: (v) => setState(() => _society = v),
              ),
              const SizedBox(height: 10),
              DropdownButtonFormField<int>(
                value: _minMissed,
                decoration: const InputDecoration(labelText: 'Min missed months'),
                items: const [
                  DropdownMenuItem(value: 1, child: Text('1 month+')),
                  DropdownMenuItem(value: 2, child: Text('2 months+')),
                  DropdownMenuItem(value: 3, child: Text('3 months+')),
                  DropdownMenuItem(value: 4, child: Text('4 months+')),
                  DropdownMenuItem(value: 5, child: Text('5 months+')),
                ],
                onChanged: (v) => setState(() => _minMissed = v ?? 1),
              ),
              const SizedBox(height: 10),
              ListTile(
                contentPadding: EdgeInsets.zero,
                title: const Text('As Of'),
                subtitle: Text(_dateFmt.format(_asOf)),
                trailing: const Icon(Icons.calendar_today),
                onTap: _pickAsOf,
              ),
              TextField(
                controller: _search,
                decoration: const InputDecoration(
                  labelText: 'Search',
                  prefixIcon: Icon(Icons.search),
                ),
              ),
              const SizedBox(height: 12),
              ElevatedButton(
                onPressed: _loading ? null : _load,
                child: Text(_loading ? 'Loading…' : 'Load Dues List'),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        if (_loading)
          const SoftPanel(
            child: Center(child: CircularProgressIndicator()),
          )
        else
          ..._rows.asMap().entries.map(
            (e) {
              final sn = e.key + 1;
              final r = e.value;
              return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: SoftPanel(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'S.N $sn  ·  ${r.customer}',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${r.society} · ${r.block} · Plot ${r.plotNo}',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      'Due Rs ${_money.format(r.due)} · Months (${r.missedCount}): ${r.missedCsv}',
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            color: AppColors.tealBright,
                          ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '30-Day Cycles (${r.cycleCount}): ${r.cyclesCsv}',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: AppColors.mist,
                          ),
                    ),
                    const SizedBox(height: 8),
                    Align(
                      alignment: Alignment.centerRight,
                      child: Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        alignment: WrapAlignment.end,
                        children: [
                          ElevatedButton.icon(
                            onPressed: () => _sendWhatsApp(r),
                            icon: const Icon(Icons.chat, size: 18),
                            label: const Text('WhatsApp'),
                          ),
                          ElevatedButton.icon(
                            onPressed: () => _sendSms(r),
                            icon: const Icon(Icons.sms, size: 18),
                            label: const Text('Message'),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            );
            },
          ),
      ],
    );
  }
}
