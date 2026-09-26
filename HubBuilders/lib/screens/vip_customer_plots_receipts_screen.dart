import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/allocate_store.dart';
import '../data/partner_cash_store.dart';
import '../data/plot_inventory_store.dart';
import '../data/png_pdf.dart';
import '../data/report_pdf.dart';
import '../data/whatsapp_open.dart';
import '../theme/app_colors.dart';
import '../widgets/type_suggest_field.dart';
import '../widgets/ui_bits.dart';

/// Customer Plots Receipts (VIP) — same as WinForms frmVipCustomerAllPlotsReceipts.
class VipCustomerPlotsReceiptsScreen extends StatefulWidget {
  const VipCustomerPlotsReceiptsScreen({super.key, this.onBack});

  final VoidCallback? onBack;

  @override
  State<VipCustomerPlotsReceiptsScreen> createState() =>
      _VipCustomerPlotsReceiptsScreenState();
}

class _VipCustomerPlotsReceiptsScreenState
    extends State<VipCustomerPlotsReceiptsScreen> {
  final _fmt = DateFormat('yyyy-MM-dd');
  final _disp = DateFormat('dd-MMM-yyyy');

  List<IdName> _customers = const [];
  IdName? _customer;
  late DateTime _from;
  late DateTime _to;
  CustomerPlotsReceiptsResult? _result;
  bool _loading = false;
  bool _boot = true;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _from = DateTime(now.year - 5, now.month, now.day);
    _to = DateTime(now.year, now.month, now.day);
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    final custs = await PartnerCashStore.customers();
    if (!mounted) return;
    setState(() {
      _customers = custs;
      if (custs.isNotEmpty) _customer = custs.first;
      _boot = false;
    });
  }

  Future<void> _preview() async {
    if (_customer == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Customer select karein.')),
      );
      return;
    }
    if (_from.isAfter(_to)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('From date To date se bara nahi ho sakta.')),
      );
      return;
    }
    setState(() => _loading = true);
    try {
      final res = await PartnerCashStore.vipCustomerPlotsReceipts(
        customerId: _customer!.id,
        from: _from,
        to: _to,
      );
      if (!mounted) return;
      setState(() => _result = res);
      if (res.rows.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Is date range mein receipts nahi mile.')),
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

  Future<void> _whatsApp() async {
    if (_customer == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Customer select karein.')),
      );
      return;
    }
    CustomerPlotsReceiptsResult? res = _result;
    if (res == null || res.rows.isEmpty) {
      await _preview();
      res = _result;
    }
    if (res == null || res.rows.isEmpty) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Is date range mein receipts nahi mile.')),
      );
      return;
    }
    final cust = await AllocateStore.customerById(_customer!.id);
    final mobile = cust?.mobile ?? '';
    if (mobile.trim().isEmpty) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Customer ka mobile number nahi mila.')),
      );
      return;
    }
    final n0 = NumberFormat('#,##0');
    final msg = '''Assalam o Alaikum,

*${res.customerName}* — Plots Receipts
Plot Prices Total: ${n0.format(res.totalPlotPrices)}
Receipts Grand Total: ${n0.format(res.grandTotal)}
*Remaining: ${n0.format(res.remaining)}*

Hub Builders''';
    final ok = await WhatsAppOpen.send(mobile: mobile, message: msg);
    if (!mounted) return;
    if (!ok) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('WhatsApp open nahi hua.')),
      );
    }
  }

  void _clear() {
    final now = DateTime.now();
    setState(() {
      _from = DateTime(now.year - 5, now.month, now.day);
      _to = DateTime(now.year, now.month, now.day);
      _result = null;
      if (_customers.isNotEmpty) _customer = _customers.first;
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
        title: 'Customer Plots Receipts (VIP)',
        summary:
            '${res.customerName}\nPlot Prices: ${n0.format(res.totalPlotPrices)}    '
            'Receipts: ${n0.format(res.grandTotal)}    Remaining: ${n0.format(res.remaining)}',
        headers: const ['S.N', 'Particulars', 'Date', 'Mode', 'Ref No', 'Amount'],
        rows: [
          for (final r in res.rows)
            [
              '${r.srNo}',
              r.rowLabel,
              r.receiptDate,
              r.mode,
              r.refNo,
              n0.format(r.amount),
            ],
        ],
        rightAlign: const {5},
      );
      final f = await PngPdf.saveBytes(
        bytes: bytes,
        fileBase: 'VipCustomerPlotsReceipts',
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

  Color _bg(String type) {
    switch (type) {
      case 'PLOT_PRICE':
        return const Color(0xFFD1ECF1).withValues(alpha: 0.25);
      case 'PLOT_TOTAL':
        return const Color(0xFFFFF3CD).withValues(alpha: 0.22);
      case 'TOTAL_PRICES':
        return const Color(0xFFD6EAF8).withValues(alpha: 0.25);
      case 'GRAND_TOTAL':
        return AppColors.success.withValues(alpha: 0.18);
      case 'REMAINING':
        return AppColors.danger.withValues(alpha: 0.16);
      default:
        return Colors.transparent;
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_boot) {
      return const Center(child: CircularProgressIndicator());
    }

    final res = _result;

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
                      'Customer Plots Receipts (VIP)',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                'All plots receipts + allocate prices.\nRemaining = Plot Prices Total − Receipts Grand Total.',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 12),
              TypeSuggestField<IdName>(
                label: 'Customer (type to search)',
                items: _customers,
                selected: _customer,
                display: (c) => c.name,
                onSelected: (v) => setState(() => _customer = v),
              ),
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
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _loading ? null : _preview,
                      child: Text(_loading ? 'Loading…' : 'Preview'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _loading ? null : _savePdf,
                      child: const Text('Save PDF'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _clear,
                      child: const Text('Clear'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: _loading ? null : _whatsApp,
                      icon: const Icon(Icons.chat_outlined, size: 18),
                      label: const Text('WA'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        if (res != null && res.rows.isNotEmpty) ...[
          const SizedBox(height: 14),
          SoftPanel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Hub Builders',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        color: AppColors.tealBright,
                      ),
                ),
                Text(
                  res.customerName,
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 6),
                Text(
                  'Plot Prices Total: ${res.totalPlotPrices.toStringAsFixed(2)}'
                  '   |   Receipts Grand Total: ${res.grandTotal.toStringAsFixed(2)}'
                  '   |   Remaining: ${res.remaining.toStringAsFixed(2)}',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: AppColors.gold,
                        fontWeight: FontWeight.w600,
                      ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Prices: ${res.totalPlotPrices.toStringAsFixed(2)}  −  Receipts: ${res.grandTotal.toStringAsFixed(2)}  =  Remaining: ${res.remaining.toStringAsFixed(2)}',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: AppColors.danger,
                        fontWeight: FontWeight.w600,
                      ),
                ),
                Text(
                  'Period: ${_disp.format(_from)}  →  ${_disp.format(_to)}',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          ...res.rows.map((r) {
            final detail = r.rowType == 'DETAIL';
            return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: SoftPanel(
                child: Container(
                  width: double.infinity,
                  decoration: BoxDecoration(
                    color: _bg(r.rowType),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  padding: const EdgeInsets.all(8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        detail ? '${r.srNo}. ${r.rowLabel}' : r.rowLabel,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      if (detail) ...[
                        const SizedBox(height: 4),
                        Text(
                          '${r.receiptDate}  ·  Rcp# ${r.receiptId}  ·  ${r.mode}'
                          '${r.refNo.isNotEmpty ? '  ·  ${r.refNo}' : ''}',
                          style: Theme.of(context).textTheme.bodyMedium,
                        ),
                        if (r.notes.isNotEmpty)
                          Text(r.notes, style: Theme.of(context).textTheme.bodySmall),
                      ],
                      const SizedBox(height: 4),
                      Align(
                        alignment: Alignment.centerRight,
                        child: Text(
                          r.amount.toStringAsFixed(2),
                          style: Theme.of(context).textTheme.titleMedium?.copyWith(
                                color: AppColors.gold,
                              ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            );
          }),
          SoftPanel(
            child: Text(
              'Blue = allocate plot price. Yellow = plot paid. Light blue = total plot prices. Green = receipts grand total. Pink = Remaining (Prices − Receipts).',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: AppColors.mist,
                  ),
            ),
          ),
        ],
      ],
    );
  }
}
