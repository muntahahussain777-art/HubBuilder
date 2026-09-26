import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/partner_cash_store.dart';
import '../data/receipt_store.dart';
import '../data/vip_alloc_suggest.dart';
import '../logic/letter_receipt_composers.dart';
import '../data/png_pdf.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

class TemporaryAllotmentScreen extends StatefulWidget {
  const TemporaryAllotmentScreen({
    super.key,
    required this.blankOverlay,
    this.onBack,
  });

  final bool blankOverlay;
  final VoidCallback? onBack;

  @override
  State<TemporaryAllotmentScreen> createState() =>
      _TemporaryAllotmentScreenState();
}

class _TemporaryAllotmentScreenState extends State<TemporaryAllotmentScreen> {
  late final _date = TextEditingController(
    text: DateFormat('dd-MM-yyyy').format(DateTime.now()),
  );
  final _name = TextEditingController();
  final _father = TextEditingController();
  final _membership = TextEditingController();
  final _plotNo = TextEditingController();
  final _area = TextEditingController();
  final _location = TextEditingController();
  final _nic = TextEditingController();
  bool _busy = false;

  @override
  void dispose() {
    for (final c in [
      _date,
      _name,
      _father,
      _membership,
      _plotNo,
      _area,
      _location,
      _nic,
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _savePdf() async {
    setState(() => _busy = true);
    try {
      final pages = await AllotmentLetterComposer.compose(
        blankOverlay: widget.blankOverlay,
        data: AllotmentLetterData(
          date: _date.text,
          name: _name.text,
          father: _father.text,
          membershipNo: _membership.text,
          plotNo: _plotNo.text,
          area: _area.text,
          location: _location.text,
          nic: _nic.text,
        ),
      );
      final f = await PngPdf.save(
        pages: [pages.front, pages.back],
        fileBase: widget.blankOverlay
            ? 'TemporaryAllotmentBlank'
            : 'TemporaryAllotmentLetter',
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
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _print() async {
    setState(() => _busy = true);
    try {
      final pages = await AllotmentLetterComposer.compose(
        blankOverlay: widget.blankOverlay,
        data: AllotmentLetterData(
          date: _date.text,
          name: _name.text,
          father: _father.text,
          membershipNo: _membership.text,
          plotNo: _plotNo.text,
          area: _area.text,
          location: _location.text,
          nic: _nic.text,
        ),
      );
      await PngPdf.printPages(
        pages: [pages.front, pages.back],
        name: widget.blankOverlay
            ? 'TemporaryAllotmentBlank'
            : 'TemporaryAllotmentLetter',
      );
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

  Future<void> _preview() async {
    setState(() => _busy = true);
    try {
      final pages = await AllotmentLetterComposer.compose(
        blankOverlay: widget.blankOverlay,
        data: AllotmentLetterData(
          date: _date.text,
          name: _name.text,
          father: _father.text,
          membershipNo: _membership.text,
          plotNo: _plotNo.text,
          area: _area.text,
          location: _location.text,
          nic: _nic.text,
        ),
      );
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => _PngPreview(
            title: widget.blankOverlay
                ? 'Temp. Allotment Blank'
                : 'Temporary Allotment Letter',
            images: [pages.front, pages.back],
            labels: const ['Front', 'Back'],
            fileBase: widget.blankOverlay
                ? 'TemporaryAllotmentBlank'
                : 'TemporaryAllotmentLetter',
          ),
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Preview failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _f(TextEditingController c, String label) => Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: TextField(
          controller: c,
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
                      widget.blankOverlay
                          ? 'Temp. Allotment Blank'
                          : 'Temporary Allotment Letter',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              _f(_date, 'Date / تاریخ'),
              _f(_name, 'Name'),
              _f(_father, 'Father-Husband'),
              _f(_membership, 'Membership No'),
              _f(_plotNo, 'Plot No'),
              _f(_area, 'Area'),
              _f(_location, 'Location'),
              _f(_nic, 'CNIC (page 2)'),
              ElevatedButton(
                onPressed: _busy ? null : _preview,
                child: Text(_busy ? 'Composing…' : 'Print Preview'),
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _savePdf,
                      child: const Text('Save PDF'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _print,
                      child: const Text('Print'),
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

class LasbelaReceiptScreen extends StatefulWidget {
  const LasbelaReceiptScreen({
    super.key,
    required this.marketBhawani,
    this.onBack,
  });

  final bool marketBhawani;
  final VoidCallback? onBack;

  @override
  State<LasbelaReceiptScreen> createState() => _LasbelaReceiptScreenState();
}

class _LasbelaReceiptScreenState extends State<LasbelaReceiptScreen> {
  static const _paymentModes = [
    'Cash',
    'Cross Cheque',
    'Bank Draft',
    'Pay Order',
  ];

  late final _receiptNo = TextEditingController();
  late final _receiptDate = TextEditingController(
    text: DateFormat('dd-MM-yyyy').format(DateTime.now()),
  );
  final _person = TextEditingController();
  final _father = TextEditingController();
  final _amount = TextEditingController();
  final _words = TextEditingController();
  final _mode = TextEditingController(text: 'Cash');
  late final _payDate = TextEditingController(
    text: DateFormat('dd-MM-yyyy').format(DateTime.now()),
  );
  final _plotNo = TextEditingController();
  final _size = TextEditingController();
  final _block = TextEditingController();
  final _category = TextEditingController();
  final _current = TextEditingController();
  final _balance = TextEditingController();

  bool _busy = false;
  bool _filling = false;
  double _remainingDue = 0;
  List<VipAllocSuggest> _allocCache = const [];
  List<VipAllocSuggest> _nameHits = const [];
  bool _showNameHits = false;
  VipAllocSuggest? _selectedAlloc;
  int? _editingReceiptId;

  @override
  void initState() {
    super.initState();
    _amount.addListener(_recalcBalance);
    _person.addListener(_onPersonTyped);
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    try {
      final next = await ReceiptStore.nextAutoRefNo();
      final cache = await VipAllocSuggestStore.loadActive();
      if (!mounted) return;
      setState(() {
        if (_receiptNo.text.trim().isEmpty) _receiptNo.text = next;
        _allocCache = cache;
      });
    } catch (_) {}
  }

  @override
  void dispose() {
    _amount.removeListener(_recalcBalance);
    _person.removeListener(_onPersonTyped);
    for (final c in [
      _receiptNo,
      _receiptDate,
      _person,
      _father,
      _amount,
      _words,
      _mode,
      _payDate,
      _plotNo,
      _size,
      _block,
      _category,
      _current,
      _balance,
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  void _onPersonTyped() {
    if (_filling) return;
    final q = _person.text.trim();
    if (_selectedAlloc != null &&
        q.toLowerCase() != _selectedAlloc!.name.trim().toLowerCase()) {
      _selectedAlloc = null;
      _editingReceiptId = null;
    }
    final hits = q.isEmpty
        ? const <VipAllocSuggest>[]
        : VipAllocSuggestStore.filter(_allocCache, q);
    setState(() {
      _nameHits = hits;
      _showNameHits = hits.isNotEmpty;
    });
  }

  void _recalcBalance() {
    if (_filling) return;
    final raw = _amount.text.trim().replaceAll(',', '');
    final paidNow = double.tryParse(raw) ?? 0;
    final paid = paidNow < 0 ? 0.0 : paidNow;
    _filling = true;
    _current.text = raw.isEmpty ? '' : VipAllocSuggestStore.formatMoney(paid);
    var bal = _remainingDue - paid;
    if (bal < 0) bal = 0;
    _balance.text = VipAllocSuggestStore.formatMoney(bal);
    _filling = false;
  }

  void _applySuggest(VipAllocSuggest a) {
    _filling = true;
    _person.text = a.name;
    _father.text = a.father;
    _plotNo.text = a.plotNo;
    _size.text = a.size;
    _block.text = a.block;
    _category.text = a.category;
    _remainingDue = a.remaining;
    _selectedAlloc = a;
    _editingReceiptId = null;
    _amount.clear();
    _current.clear();
    _balance.text = VipAllocSuggestStore.formatMoney(a.remaining);
    _filling = false;
    setState(() {
      _nameHits = const [];
      _showNameHits = false;
    });
  }

  Future<void> _clear() async {
    _filling = true;
    for (final c in [
      _person,
      _father,
      _amount,
      _words,
      _plotNo,
      _size,
      _block,
      _category,
      _current,
      _balance,
    ]) {
      c.clear();
    }
    _remainingDue = 0;
    _selectedAlloc = null;
    _editingReceiptId = null;
    final now = DateTime.now();
    _receiptDate.text = DateFormat('dd-MM-yyyy').format(now);
    _payDate.text = DateFormat('dd-MM-yyyy').format(now);
    _mode.text = 'Cash';
    _filling = false;
    try {
      _receiptNo.text = await ReceiptStore.nextAutoRefNo();
    } catch (_) {
      _receiptNo.text = '001';
    }
    setState(() {
      _nameHits = const [];
      _showNameHits = false;
    });
  }

  String _mapReceiptMode(String raw) {
    final s = raw.trim();
    if (s.isEmpty) return 'Cash';
    if (s.toLowerCase() == 'cash') return 'Cash';
    final low = s.toLowerCase();
    if (low.contains('bank') ||
        low.contains('cheque') ||
        low.contains('check') ||
        low.contains('draft') ||
        low.contains('pay order')) {
      return 'Bank';
    }
    return 'Cash';
  }

  String _parseDbDate(String raw) {
    final t = raw.trim();
    for (final fmt in ['dd-MM-yyyy', 'd-M-yyyy', 'yyyy-MM-dd', 'dd/MM/yyyy']) {
      try {
        return DateFormat('yyyy-MM-dd').format(DateFormat(fmt).parseStrict(t));
      } catch (_) {}
    }
    final fallback = DateTime.tryParse(t);
    if (fallback != null) return DateFormat('yyyy-MM-dd').format(fallback);
    return DateFormat('yyyy-MM-dd').format(DateTime.now());
  }

  Future<bool> _saveAmountToReceiptForm() async {
    final raw = _amount.text.trim().replaceAll(',', '');
    final amount = double.tryParse(raw) ?? 0;
    if (amount <= 0) return true;
    final alloc = _selectedAlloc;
    if (alloc == null) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Pehle customer name search karke select karo, phir amount do.',
            ),
          ),
        );
      }
      return false;
    }

    var notes = widget.marketBhawani
        ? 'Lasbela Market Bhawani'
        : 'Lasbela Village Receipt';
    final words = _words.text.trim();
    if (words.isNotEmpty) notes = '$notes / $words';
    final dateSrc = _payDate.text.trim().isNotEmpty
        ? _payDate.text
        : _receiptDate.text;
    int? partnerId;
    try {
      partnerId =
          await PartnerCashStore.getAllocationPartnerId(alloc.customerPlotId);
    } catch (_) {}

    final result = await ReceiptStore.saveReceipt(
      customerPlotId: alloc.customerPlotId,
      receiptDate: _parseDbDate(dateSrc),
      amount: amount,
      mode: _mapReceiptMode(_mode.text),
      refNo: _receiptNo.text,
      notes: notes,
      editingReceiptId: _editingReceiptId,
      partnerId: partnerId,
    );
    if (!result.ok) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(result.message)),
        );
      }
      return false;
    }
    if (result.receiptId != null) _editingReceiptId = result.receiptId;
    try {
      _allocCache = await VipAllocSuggestStore.loadActive();
    } catch (_) {}
    return true;
  }

  Future<void> _savePdf() async {
    setState(() => _busy = true);
    try {
      if (!await _saveAmountToReceiptForm()) return;
      final png = await LasbelaReceiptComposer.compose(
        marketBhawani: widget.marketBhawani,
        data: LasbelaReceiptData(
          receiptNo: _receiptNo.text,
          receiptDate: _receiptDate.text,
          personName: _person.text,
          father: _father.text,
          receivedAmount: _amount.text,
          amountWords: _words.text,
          paymentMode: _mode.text,
          paymentDate: _payDate.text,
          plotNo: _plotNo.text,
          plotSize: _size.text,
          block: _block.text,
          category: _category.text,
          currentAmount: _current.text,
          balanceAmount: _balance.text,
        ),
      );
      final f = await PngPdf.save(
        pages: [png],
        fileBase: widget.marketBhawani
            ? 'LasbelaMarketBhawaniReceipt'
            : 'LasbelaVillageReceipt',
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
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _print() async {
    setState(() => _busy = true);
    try {
      if (!await _saveAmountToReceiptForm()) return;
      final png = await LasbelaReceiptComposer.compose(
        marketBhawani: widget.marketBhawani,
        data: LasbelaReceiptData(
          receiptNo: _receiptNo.text,
          receiptDate: _receiptDate.text,
          personName: _person.text,
          father: _father.text,
          receivedAmount: _amount.text,
          amountWords: _words.text,
          paymentMode: _mode.text,
          paymentDate: _payDate.text,
          plotNo: _plotNo.text,
          plotSize: _size.text,
          block: _block.text,
          category: _category.text,
          currentAmount: _current.text,
          balanceAmount: _balance.text,
        ),
      );
      await PngPdf.printPages(
        pages: [png],
        name: widget.marketBhawani
            ? 'LasbelaMarketBhawaniReceipt'
            : 'LasbelaVillageReceipt',
      );
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

  Future<void> _preview() async {
    setState(() => _busy = true);
    try {
      if (!await _saveAmountToReceiptForm()) return;
      final png = await LasbelaReceiptComposer.compose(
        marketBhawani: widget.marketBhawani,
        data: LasbelaReceiptData(
          receiptNo: _receiptNo.text,
          receiptDate: _receiptDate.text,
          personName: _person.text,
          father: _father.text,
          receivedAmount: _amount.text,
          amountWords: _words.text,
          paymentMode: _mode.text,
          paymentDate: _payDate.text,
          plotNo: _plotNo.text,
          plotSize: _size.text,
          block: _block.text,
          category: _category.text,
          currentAmount: _current.text,
          balanceAmount: _balance.text,
        ),
      );
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => _PngPreview(
            title: widget.marketBhawani
                ? 'Lasbela Market Bhawani'
                : 'Lasbela Village Receipt',
            images: [png],
            labels: const ['A4 Dual Copy'],
            fileBase: widget.marketBhawani
                ? 'LasbelaMarketBhawaniReceipt'
                : 'LasbelaVillageReceipt',
          ),
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Preview failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _f(
    TextEditingController c,
    String label, {
    bool readOnly = false,
  }) =>
      Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: TextField(
          controller: c,
          readOnly: readOnly,
          decoration: InputDecoration(labelText: label),
        ),
      );

  Widget _nameField() {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextField(
            controller: _person,
            decoration: const InputDecoration(
              labelText: 'Name / محترم (type to search)',
            ),
          ),
          if (_showNameHits)
            Container(
              margin: const EdgeInsets.only(top: 4),
              constraints: const BoxConstraints(maxHeight: 180),
              decoration: BoxDecoration(
                color: const Color(0xFF25294A),
                border: Border.all(color: Colors.white24),
                borderRadius: BorderRadius.circular(8),
              ),
              child: ListView.builder(
                shrinkWrap: true,
                itemCount: _nameHits.length,
                itemBuilder: (context, i) {
                  final a = _nameHits[i];
                  return ListTile(
                    dense: true,
                    title: Text(
                      a.display,
                      style: const TextStyle(color: Colors.white, fontSize: 13),
                    ),
                    onTap: () => _applySuggest(a),
                  );
                },
              ),
            ),
        ],
      ),
    );
  }

  Widget _paymentModeField() {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextField(
            controller: _mode,
            decoration: const InputDecoration(
              labelText: 'Payment Mode (list or type)',
            ),
          ),
          const SizedBox(height: 6),
          Wrap(
            spacing: 6,
            runSpacing: 6,
            children: [
              for (final m in _paymentModes)
                ActionChip(
                  label: Text(m),
                  onPressed: () => setState(() => _mode.text = m),
                ),
            ],
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
      children: [
        SoftPanel(
          child: Column(
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
                      widget.marketBhawani
                          ? 'Lasbela Market Bhawani'
                          : 'Lasbela Village Receipt',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              _f(_receiptNo, 'Receipt No (auto / editable)'),
              _f(_receiptDate, 'Receipt Date'),
              _nameField(),
              _f(_father, 'Father / Husband', readOnly: true),
              _f(_amount, 'Received Amount'),
              _f(_words, 'Amount In Words'),
              _paymentModeField(),
              _f(_payDate, 'Payment Date'),
              _f(_plotNo, 'Plot No', readOnly: true),
              _f(_size, 'Plot Size', readOnly: true),
              _f(_block, 'Block', readOnly: true),
              _f(_category, 'Category', readOnly: true),
              _f(_current, 'Current Amount', readOnly: true),
              _f(_balance, 'Balance Amount', readOnly: true),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _clear,
                      child: const Text('Clear'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _preview,
                      child: Text(_busy ? 'Composing…' : 'Print Preview'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _savePdf,
                      child: const Text('Save PDF'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _print,
                      child: const Text('Print'),
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

class _PngPreview extends StatelessWidget {
  const _PngPreview({
    required this.title,
    required this.images,
    required this.labels,
    required this.fileBase,
  });

  final String title;
  final List<Uint8List> images;
  final List<String> labels;
  final String fileBase;

  Future<void> _save(BuildContext context) async {
    try {
      final f = await PngPdf.save(pages: images, fileBase: fileBase);
      if (!context.mounted || f == null) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('PDF saved:\n${f.path}')),
      );
    } catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('PDF failed: $e')),
      );
    }
  }

  Future<void> _print(BuildContext context) async {
    try {
      await PngPdf.printPages(pages: images, name: fileBase);
    } catch (e) {
      if (!context.mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Print failed: $e')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: Text(title),
          actions: [
            TextButton(
              onPressed: () => _save(context),
              child: const Text('Save PDF'),
            ),
            TextButton(
              onPressed: () => _print(context),
              child: const Text('Print'),
            ),
          ],
        ),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            for (var i = 0; i < images.length; i++) ...[
              Text(labels[i], style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 8),
              SoftPanel(
                padding: EdgeInsets.zero,
                child: InteractiveViewer(
                  minScale: 0.4,
                  maxScale: 4,
                  child: Image.memory(images[i], fit: BoxFit.contain),
                ),
              ),
              const SizedBox(height: 16),
            ],
          ],
        ),
      ),
    );
  }
}
