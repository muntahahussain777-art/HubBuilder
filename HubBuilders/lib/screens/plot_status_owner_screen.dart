import 'package:flutter/material.dart';
import 'package:share_plus/share_plus.dart';

import '../data/plot_inventory_store.dart';
import '../data/plot_status_pdf.dart';
import '../data/plot_status_store.dart';
import '../data/png_pdf.dart';
import '../data/whatsapp_open.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

class PlotStatusOwnerScreen extends StatefulWidget {
  const PlotStatusOwnerScreen({super.key, this.onBack});

  final VoidCallback? onBack;

  @override
  State<PlotStatusOwnerScreen> createState() => _PlotStatusOwnerScreenState();
}

class _PlotStatusOwnerScreenState extends State<PlotStatusOwnerScreen> {
  final _phone = TextEditingController();
  List<IdName> _societies = const [];
  IdName? _society;
  String _status = '(All Status)';
  PlotStatusResult? _result;
  bool _boot = true;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  @override
  void dispose() {
    _phone.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    final socs = await PlotStatusStore.societies();
    if (!mounted) return;
    setState(() {
      _societies = [const IdName(0, '(All Societies)'), ...socs];
      _society = _societies.first;
      _boot = false;
    });
  }

  Future<void> _load() async {
    setState(() => _busy = true);
    try {
      final res = await PlotStatusStore.load(societyId: _society?.id ?? 0);
      if (!mounted) return;
      setState(() => _result = res);
      if (res.rows.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Koi plot nahi mila.')),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Report failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  List<PlotStatusRow> _shown(PlotStatusResult res) {
    if (_status == 'Available' ||
        _status == 'Installment' ||
        _status == 'Reserve' ||
        _status == 'Sold') {
      return res.rows.where((r) => r.kind == _status).toList();
    }
    return res.rows;
  }

  PlotStatusResult _forPdf(PlotStatusResult res) {
    return PlotStatusResult(
      rows: _shown(res),
      counts: res.counts,
      societyName: res.societyName,
    );
  }

  Future<PlotStatusResult?> _ensure() async {
    var res = _result;
    if (res == null || res.rows.isEmpty) {
      await _load();
      res = _result;
    }
    return res;
  }

  Future<void> _savePdf() async {
    final res = await _ensure();
    if (res == null || res.rows.isEmpty) return;
    setState(() => _busy = true);
    try {
      final pdfRes = _forPdf(res);
      if (pdfRes.rows.isEmpty) return;
      final bytes = await PlotStatusPdf.build(
        pdfRes,
        statusFilter: _status == '(All Status)' ? '' : _status,
      );
      final f = await PngPdf.saveBytes(bytes: bytes, fileBase: 'HubBuilders_PlotStatus');
      if (!mounted || f == null) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('PDF saved:\n${f.path}')),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('PDF failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _print() async {
    final res = await _ensure();
    if (res == null || res.rows.isEmpty) return;
    setState(() => _busy = true);
    try {
      final pdfRes = _forPdf(res);
      if (pdfRes.rows.isEmpty) return;
      final bytes = await PlotStatusPdf.build(
        pdfRes,
        statusFilter: _status == '(All Status)' ? '' : _status,
      );
      await PngPdf.printBytes(bytes: bytes, name: 'HubBuilders Plot Status');
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Print failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _whatsApp() async {
    final res = await _ensure();
    if (res == null || res.rows.isEmpty) return;
    final digits = WhatsAppOpen.normalizePhone(_phone.text);
    if (digits.isEmpty) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Owner ka WhatsApp number likhein (03xx...).')),
      );
      return;
    }
    setState(() => _busy = true);
    try {
      final pdfRes = _forPdf(res);
      if (pdfRes.rows.isEmpty) return;
      final bytes = await PlotStatusPdf.build(
        pdfRes,
        statusFilter: _status == '(All Status)' ? '' : _status,
      );
      final f = await PngPdf.saveBytes(
        bytes: bytes,
        fileBase: 'HubBuilders_PlotStatus',
      );
      if (f == null) return;
      final c = res.counts;
      final msg =
          'HUB BUILDERS — Plot Status Report\n'
          'Society: ${res.societyName}\n'
          'Available: ${c.available} | Installment: ${c.installment} | '
          'Reserve: ${c.reserve} | Sold: ${c.sold}';
      await WhatsAppOpen.send(mobile: digits, message: msg);
      await SharePlus.instance.share(
        ShareParams(
          files: [XFile(f.path, mimeType: 'application/pdf')],
          text: msg,
        ),
      );
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

  @override
  Widget build(BuildContext context) {
    if (_boot) return const Center(child: CircularProgressIndicator());
    final rows = _result == null ? const <PlotStatusRow>[] : _shown(_result!);
    final c = _result?.counts;

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
                      'Plot Status Report (Owner)',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              DropdownButtonFormField<IdName>(
                initialValue: _society,
                decoration: const InputDecoration(labelText: 'Society'),
                items: [
                  for (final s in _societies)
                    DropdownMenuItem(value: s, child: Text(s.name)),
                ],
                onChanged: (v) => setState(() => _society = v),
              ),
              const SizedBox(height: 10),
              DropdownButtonFormField<String>(
                initialValue: _status,
                decoration: const InputDecoration(labelText: 'Status'),
                items: const [
                  DropdownMenuItem(value: '(All Status)', child: Text('(All Status)')),
                  DropdownMenuItem(value: 'Available', child: Text('Available')),
                  DropdownMenuItem(value: 'Installment', child: Text('Installment')),
                  DropdownMenuItem(value: 'Reserve', child: Text('Reserve')),
                  DropdownMenuItem(value: 'Sold', child: Text('Sold')),
                ],
                onChanged: (v) => setState(() => _status = v ?? '(All Status)'),
              ),
              const SizedBox(height: 10),
              TextField(
                controller: _phone,
                keyboardType: TextInputType.phone,
                decoration: const InputDecoration(
                  labelText: 'Owner WhatsApp number',
                ),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _load,
                      child: Text(_busy ? 'Loading…' : 'Show'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _savePdf,
                      child: const Text('Save PDF'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _print,
                      child: const Text('Print'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _whatsApp,
                      child: const Text('WhatsApp PDF'),
                    ),
                  ),
                ],
              ),
              if (c != null) ...[
                const SizedBox(height: 12),
                Builder(
                  builder: (_) {
                    final allocated = c.installment + c.reserve + c.sold;
                    return Text(
                      'Allocate jaisi entries: $allocated  '
                      '(Installment ${c.installment} + Reserve ${c.reserve} + Sold ${c.sold})'
                      '  |  Available ${c.available}  |  Showing ${rows.length}',
                      style: Theme.of(context).textTheme.titleSmall?.copyWith(
                            color: AppColors.gold,
                          ),
                    );
                  },
                ),
              ],
            ],
          ),
        ),
        const SizedBox(height: 10),
        for (final r in rows)
          Padding(
            padding: const EdgeInsets.only(bottom: 6),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Row(
                children: [
                  SizedBox(
                    width: 88,
                    child: Text(
                      r.kind,
                      style: const TextStyle(
                        color: Colors.black,
                        fontWeight: FontWeight.w700,
                        fontSize: 13,
                      ),
                    ),
                  ),
                  Expanded(
                    child: Text(
                      '${r.society}  ·  ${r.block}  ·  Plot ${r.plotNo}'
                      '${r.plotType.isEmpty ? '' : '  ·  ${r.plotType}'}'
                      '${r.customer.isEmpty ? '' : '\n${r.customer}'}',
                      style: const TextStyle(
                        color: Colors.black,
                        fontSize: 13,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
      ],
    );
  }
}
