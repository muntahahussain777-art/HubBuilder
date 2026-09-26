import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/partner_cash_store.dart';
import '../data/plot_inventory_store.dart';
import '../data/png_pdf.dart';
import '../data/report_pdf.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Partner Cash Book (VIP) — same filters/logic as WinForms frmVipPartnerCashBook.
class VipPartnerCashBookScreen extends StatefulWidget {
  const VipPartnerCashBookScreen({super.key, this.onBack});

  final VoidCallback? onBack;

  @override
  State<VipPartnerCashBookScreen> createState() =>
      _VipPartnerCashBookScreenState();
}

class _VipPartnerCashBookScreenState extends State<VipPartnerCashBookScreen> {
  final _fmt = DateFormat('yyyy-MM-dd');
  final _disp = DateFormat('dd-MMM-yyyy');
  final _search = TextEditingController();

  List<IdName> _societies = const [];
  List<IdName> _partners = const [];
  IdName? _filterSociety;
  IdName? _headerSociety;
  IdName? _partner;
  String _direction = ''; // '' = all
  late DateTime _from;
  late DateTime _to;

  PartnerCashVipResult? _result;
  bool _loading = false;
  bool _boot = true;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _from = DateTime(now.year, 1, 1);
    _to = DateTime(now.year, now.month, now.day);
    _bootstrap();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    final socs = await PartnerCashStore.societies();
    final parts = await PartnerCashStore.partners();
    if (!mounted) return;
    setState(() {
      _societies = socs;
      _partners = parts;
      _boot = false;
    });
  }

  Future<void> _preview() async {
    if (_from.isAfter(_to)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('From date To date se bara nahi ho sakta.')),
      );
      return;
    }
    setState(() => _loading = true);
    try {
      final res = await PartnerCashStore.vipCashBook(
        from: _from,
        to: _to,
        societyId: _filterSociety?.id ?? 0,
        partnerId: _partner?.id ?? 0,
        direction: _direction,
        keyword: _search.text,
      );
      if (!mounted) return;
      setState(() => _result = res);
      if (res.rows.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Is filter mein Partner Cash Book records nahi mile.'),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Preview failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _clear() {
    final now = DateTime.now();
    setState(() {
      _from = DateTime(now.year, 1, 1);
      _to = DateTime(now.year, now.month, now.day);
      _filterSociety = null;
      _headerSociety = null;
      _partner = null;
      _direction = '';
      _search.clear();
      _result = null;
    });
  }

  Future<void> _savePdf() async {
    var res = _result;
    if (res == null || res.rows.isEmpty) {
      await _preview();
      res = _result;
    }
    if (res == null || res.rows.isEmpty) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('PDF ke liye pehle Preview karein.')),
      );
      return;
    }
    setState(() => _loading = true);
    try {
      final n0 = NumberFormat('#,##0.##');
      final bytes = await ReportPdf.buildTable(
        title: 'Partner Cash Book (VIP)',
        summary:
            '${res.summary}\nIn: ${n0.format(res.totalIn)}    Out: ${n0.format(res.totalOut)}    Balance: ${n0.format(res.balance)}',
        headers: const [
          'S.N',
          'Date',
          'Partner',
          'Dir',
          'In',
          'Out',
          'Category',
          'Society',
          'Balance',
        ],
        rows: [
          for (final r in res.rows)
            [
              '${r.srNo}',
              r.txnDate,
              r.partnerName,
              r.direction,
              n0.format(r.amountIn),
              n0.format(r.amountOut),
              r.category,
              r.societyName,
              n0.format(r.balance),
            ],
        ],
        rightAlign: const {4, 5, 8},
      );
      final f = await PngPdf.saveBytes(
        bytes: bytes,
        fileBase: 'VipPartnerCashBook',
      );
      if (!mounted) return;
      if (f == null) return;
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
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _pickFrom() async {
    final p = await showDatePicker(
      context: context,
      initialDate: _from,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (p != null) setState(() => _from = p);
  }

  Future<void> _pickTo() async {
    final p = await showDatePicker(
      context: context,
      initialDate: _to,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (p != null) setState(() => _to = p);
  }

  String get _brand {
    final n = _headerSociety?.name.trim() ?? '';
    if (n.isEmpty) return 'HUB BUILDERS';
    return n.toUpperCase();
  }

  Color _rowColor(String type) {
    if (type == 'TOTAL') return AppColors.success.withValues(alpha: 0.18);
    return Colors.transparent;
  }

  @override
  Widget build(BuildContext context) {
    if (_boot) {
      return const Center(child: CircularProgressIndicator());
    }

    final rows = _result?.rows ?? const <PartnerCashVipRow>[];

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
                      'Partner Cash Book (VIP)',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              DropdownButtonFormField<IdName?>(
                value: _headerSociety,
                decoration: const InputDecoration(labelText: 'Header brand / Society'),
                items: [
                  const DropdownMenuItem<IdName?>(
                    value: null,
                    child: Text('(Hub Builders — default)'),
                  ),
                  ..._societies.map(
                    (s) => DropdownMenuItem<IdName?>(value: s, child: Text(s.name)),
                  ),
                ],
                onChanged: (v) => setState(() => _headerSociety = v),
              ),
              const SizedBox(height: 10),
              DropdownButtonFormField<IdName?>(
                value: _filterSociety,
                decoration: const InputDecoration(labelText: 'Filter Society'),
                items: [
                  const DropdownMenuItem<IdName?>(
                    value: null,
                    child: Text('(All Societies)'),
                  ),
                  ..._societies.map(
                    (s) => DropdownMenuItem<IdName?>(value: s, child: Text(s.name)),
                  ),
                ],
                onChanged: (v) => setState(() => _filterSociety = v),
              ),
              const SizedBox(height: 10),
              DropdownButtonFormField<IdName?>(
                value: _partner,
                decoration: const InputDecoration(labelText: 'Partner'),
                items: [
                  const DropdownMenuItem<IdName?>(
                    value: null,
                    child: Text('(All Partners)'),
                  ),
                  ..._partners.map(
                    (p) => DropdownMenuItem<IdName?>(value: p, child: Text(p.name)),
                  ),
                ],
                onChanged: (v) => setState(() => _partner = v),
              ),
              const SizedBox(height: 10),
              DropdownButtonFormField<String>(
                value: _direction,
                decoration: const InputDecoration(labelText: 'Direction'),
                items: const [
                  DropdownMenuItem(value: '', child: Text('(All In / Out)')),
                  DropdownMenuItem(value: 'In', child: Text('In')),
                  DropdownMenuItem(value: 'Out', child: Text('Out')),
                ],
                onChanged: (v) => setState(() => _direction = v ?? ''),
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: const Text('From'),
                      subtitle: Text(_fmt.format(_from)),
                      onTap: _pickFrom,
                    ),
                  ),
                  Expanded(
                    child: ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: const Text('To'),
                      subtitle: Text(_fmt.format(_to)),
                      onTap: _pickTo,
                    ),
                  ),
                ],
              ),
              TextField(
                controller: _search,
                decoration: const InputDecoration(
                  labelText: 'Search',
                  prefixIcon: Icon(Icons.search),
                ),
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _loading ? null : _preview,
                      child: Text(_loading ? 'Loading…' : 'Preview'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _loading ? null : _savePdf,
                      child: const Text('Save PDF'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _clear,
                      child: const Text('Clear'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        if (_result != null && rows.isNotEmpty) ...[
          const SizedBox(height: 14),
          SoftPanel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  _brand,
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        color: AppColors.tealBright,
                      ),
                ),
                Text(
                  'Period: ${_disp.format(_from)}  →  ${_disp.format(_to)}',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                const SizedBox(height: 6),
                Text(
                  _result!.summary,
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: AppColors.gold,
                        fontWeight: FontWeight.w600,
                      ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          ...rows.map((r) {
            final isTotal = r.rowType == 'TOTAL';
            return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: SoftPanel(
                child: Container(
                  decoration: BoxDecoration(
                    color: _rowColor(r.rowType),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  padding: const EdgeInsets.all(4),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        isTotal
                            ? r.partnerName
                            : '${r.srNo}. ${r.partnerName}  ·  ${r.txnDate}  ·  ${r.direction}',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: 4),
                      Text(
                        isTotal
                            ? 'In ${r.amountIn.toStringAsFixed(2)}  |  Out ${r.amountOut.toStringAsFixed(2)}  |  Bal ${r.balance.toStringAsFixed(2)}'
                            : '${r.category} · ${r.mode}'
                                '${r.societyName.isNotEmpty ? ' · ${r.societyName}' : ''}'
                                '${r.customerName.isNotEmpty ? ' · ${r.customerName}' : ''}'
                                '${r.plotNo.isNotEmpty ? ' · Plot ${r.plotNo}' : ''}'
                                '${r.brokerName.isNotEmpty ? ' · Broker ${r.brokerName}' : ''}',
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                      if (!isTotal && r.note.isNotEmpty)
                        Text(r.note, style: Theme.of(context).textTheme.bodySmall),
                      const SizedBox(height: 4),
                      if (!isTotal)
                        Text(
                          'In ${r.amountIn.toStringAsFixed(2)}  |  Out ${r.amountOut.toStringAsFixed(2)}  |  Running Bal ${r.balance.toStringAsFixed(2)}',
                          style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                                color: AppColors.gold,
                              ),
                        ),
                    ],
                  ),
                ),
              ),
            );
          }),
        ],
      ],
    );
  }
}
