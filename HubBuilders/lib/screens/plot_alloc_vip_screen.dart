import 'package:flutter/material.dart';

import '../data/plot_alloc_vip_store.dart';
import '../data/whatsapp_open.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same as WinForms frmVipPlotAllocationWhatsApp — plot allocate VIP WhatsApp.
class PlotAllocVipScreen extends StatefulWidget {
  const PlotAllocVipScreen({super.key, this.onBack});

  final VoidCallback? onBack;

  @override
  State<PlotAllocVipScreen> createState() => _PlotAllocVipScreenState();
}

class _PlotAllocVipScreenState extends State<PlotAllocVipScreen> {
  final _customer = TextEditingController();
  final _number = TextEditingController();
  final _father = TextEditingController();
  final _cnic = TextEditingController();
  final _society = TextEditingController();
  final _block = TextEditingController();
  final _plotNo = TextEditingController();
  final _plotType = TextEditingController();
  final _booking = TextEditingController();
  final _dealType = TextEditingController();
  final _plotPrice = TextEditingController();
  final _message = TextEditingController();

  List<PlotAllocVipSuggest> _all = const [];
  List<PlotAllocVipSuggest> _hits = const [];
  PlotAllocVipSuggest? _selected;
  List<PlotAllocVipReceipt> _receipts = const [];
  bool _showHits = false;
  bool _filling = false;
  bool _busy = false;
  bool _messageEdited = false;

  @override
  void initState() {
    super.initState();
    _customer.addListener(_onCustomerTyped);
    for (final c in [
      _father,
      _cnic,
      _society,
      _block,
      _plotNo,
      _plotType,
      _booking,
      _dealType,
      _plotPrice,
      _number,
    ]) {
      c.addListener(_refreshMessage);
    }
    _message.addListener(_onMessageTyped);
    _fillMessage();
    _load();
  }

  @override
  void dispose() {
    _customer.removeListener(_onCustomerTyped);
    for (final c in [
      _father,
      _cnic,
      _society,
      _block,
      _plotNo,
      _plotType,
      _booking,
      _dealType,
      _plotPrice,
      _number,
    ]) {
      c.removeListener(_refreshMessage);
    }
    _message.removeListener(_onMessageTyped);
    for (final c in [
      _customer,
      _number,
      _father,
      _cnic,
      _society,
      _block,
      _plotNo,
      _plotType,
      _booking,
      _dealType,
      _plotPrice,
      _message,
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final rows = await PlotAllocVipStore.loadActiveAllocations();
      if (!mounted) return;
      setState(() => _all = rows);
    } catch (_) {}
  }

  void _onCustomerTyped() {
    if (_filling) return;
    final q = _customer.text.trim().toLowerCase();
    final hits = q.isEmpty
        ? const <PlotAllocVipSuggest>[]
        : _all
            .where(
              (a) =>
                  a.name.toLowerCase().contains(q) ||
                  a.father.toLowerCase().contains(q) ||
                  a.plotNo.toLowerCase().contains(q) ||
                  a.mobile.contains(q) ||
                  a.society.toLowerCase().contains(q) ||
                  a.display.toLowerCase().contains(q),
            )
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

  double _parsePrice() {
    final raw = _plotPrice.text.replaceAll(RegExp(r'[^0-9.\-]'), '');
    return double.tryParse(raw) ?? 0;
  }

  void _fillMessage() {
    _filling = true;
    _message.text = PlotAllocVipStore.buildVipMessage(
      customerName: _customer.text,
      father: _father.text,
      mobile: _number.text,
      cnic: _cnic.text,
      society: _society.text,
      block: _block.text,
      plotNo: _plotNo.text,
      plotType: _plotType.text,
      bookingDate: _booking.text,
      dealType: _dealType.text,
      plotPrice: _parsePrice(),
      monthly: _selected?.monthly ?? 0,
      receipts: _receipts,
    );
    _filling = false;
    _messageEdited = false;
  }

  Future<void> _apply(PlotAllocVipSuggest a) async {
    final receipts = await PlotAllocVipStore.loadReceipts(a.customerPlotId);
    if (!mounted) return;
    _filling = true;
    _selected = a;
    _receipts = receipts;
    _customer.text = a.name;
    _number.text = a.mobile;
    _father.text = a.father;
    _cnic.text = a.cnic;
    _society.text = a.society;
    _block.text = a.block;
    _plotNo.text = a.plotNo;
    _plotType.text = a.plotType;
    _booking.text = a.bookingDate;
    _dealType.text = a.dealType;
    _plotPrice.text =
        a.plotPrice > 0 ? PlotAllocVipStore.money(a.plotPrice) : '';
    _filling = false;
    setState(() {
      _hits = const [];
      _showHits = false;
      _messageEdited = false;
    });
    _fillMessage();
  }

  Future<void> _reloadMessage() async {
    if (_selected != null) {
      _receipts =
          await PlotAllocVipStore.loadReceipts(_selected!.customerPlotId);
    } else {
      _receipts = const [];
    }
    if (!mounted) return;
    setState(() => _messageEdited = false);
    _fillMessage();
  }

  void _clear() {
    _filling = true;
    _selected = null;
    _receipts = const [];
    for (final c in [
      _customer,
      _number,
      _father,
      _cnic,
      _society,
      _block,
      _plotNo,
      _plotType,
      _booking,
      _dealType,
      _plotPrice,
    ]) {
      c.clear();
    }
    _filling = false;
    setState(() {
      _hits = const [];
      _showHits = false;
      _messageEdited = false;
    });
    _fillMessage();
  }

  Future<void> _sendWhatsApp() async {
    final digits = WhatsAppOpen.normalizePhone(_number.text);
    if (digits.isEmpty || digits.length < 11) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Customer ka WhatsApp number likhein (03xx...).'),
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
                      'Plot Allocation Message (VIP)',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Text(
                'Allocate se search · ya name + number manually · receipts message mein',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: AppColors.mist,
                    ),
              ),
              const SizedBox(height: 12),
              _f(_customer, 'Customer (search / manual)'),
              if (_showHits)
                SoftPanel(
                  child: Column(
                    children: _hits
                        .map(
                          (h) => ListTile(
                            dense: true,
                            title: Text(h.display),
                            onTap: () => _apply(h),
                          ),
                        )
                        .toList(),
                  ),
                ),
              _f(
                _number,
                'WhatsApp number (03xx… — change kar sakte ho)',
                keyboard: TextInputType.phone,
              ),
              _f(_father, 'Father / Husband'),
              _f(_cnic, 'CNIC'),
              _f(_society, 'Society'),
              _f(_block, 'Block'),
              _f(_plotNo, 'Plot No'),
              _f(_plotType, 'Plot Type / Size'),
              _f(_booking, 'Booking Date'),
              _f(_dealType, 'Deal Type'),
              _f(
                _plotPrice,
                'Allocate / Plot Amount',
                keyboard: TextInputType.number,
              ),
              _f(
                _message,
                'VIP WhatsApp message (edit kar sakte ho)',
                maxLines: 14,
              ),
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  ElevatedButton(
                    onPressed: _busy ? null : _reloadMessage,
                    child: const Text('Load / Refresh Message'),
                  ),
                  ElevatedButton(
                    onPressed: _busy ? null : _sendWhatsApp,
                    child: Text(_busy ? 'Opening…' : 'WhatsApp'),
                  ),
                  OutlinedButton(
                    onPressed: _busy ? null : _clear,
                    child: const Text('Clear'),
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
