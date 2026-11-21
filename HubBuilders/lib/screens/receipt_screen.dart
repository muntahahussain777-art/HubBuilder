import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';

import '../data/receipt_store.dart';
import '../data/hub_sync.dart';
import '../data/installment_cover.dart';
import '../data/partner_cash_store.dart';
import '../data/plot_inventory_store.dart';
import '../data/whatsapp_open.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same as C# `frmReceiptFormView` + `frmReciptfromAdd`.
class ReceiptScreen extends StatefulWidget {
  const ReceiptScreen({super.key});

  @override
  State<ReceiptScreen> createState() => _ReceiptScreenState();
}

class _ReceiptScreenState extends State<ReceiptScreen> {
  final _search = TextEditingController();
  int _customerPlotId = 0;
  String _allocationDisp = '';
  String _query = '';
  String _searchMode = ''; // '', 'ref', 'cust'
  int _societyId = 0;
  List<IdName> _societies = const [];
  List<ReceiptLedgerRow>? _globalRows;
  List<ReceiptLedgerRow> _rows = const [];
  bool _loading = true;
  bool _searching = false;

  @override
  void initState() {
    super.initState();
    HubSync.revision.addListener(_onSync);
    _bootstrap();
  }

  @override
  void dispose() {
    HubSync.revision.removeListener(_onSync);
    _search.dispose();
    super.dispose();
  }

  void _onSync() {
    if (mounted) _reloadRows();
  }

  Future<void> _bootstrap() async {
    final societies = await PlotInventoryStore.societies();
    final opts = await ReceiptStore.activeCustomerPlots(societyId: _societyId);
    if (!mounted) return;
    setState(() => _societies = societies);
    if (opts.isNotEmpty) {
      _customerPlotId = opts.first.id;
      _allocationDisp = opts.first.disp;
    }
    await _reloadRows();
  }

  Future<void> _onSocietyChanged(int? v) async {
    final sid = v ?? 0;
    setState(() {
      _societyId = sid;
      _searchMode = '';
      _globalRows = null;
      _query = '';
      _search.clear();
    });

    if (_customerPlotId > 0) {
      final cur = await ReceiptStore.customerPlotById(_customerPlotId);
      if (sid > 0 && (cur == null || cur.societyId != sid)) {
        if (!mounted) return;
        setState(() {
          _customerPlotId = 0;
          _allocationDisp = '';
          _rows = const [];
        });
      }
    }

    if (_customerPlotId <= 0) {
      final opts = await ReceiptStore.activeCustomerPlots(societyId: sid);
      if (!mounted) return;
      if (opts.length == 1) {
        setState(() {
          _customerPlotId = opts.first.id;
          _allocationDisp = opts.first.disp;
        });
      }
    }

    await _reloadRows();
  }

  Future<void> _reloadRows() async {
    setState(() => _loading = true);

    if (_globalRows != null) {
      if (!mounted) return;
      setState(() {
        _rows = _globalRows!;
        _loading = false;
      });
      return;
    }

    if (_customerPlotId <= 0) {
      if (!mounted) return;
      setState(() {
        _rows = const [];
        _loading = false;
      });
      return;
    }

    final rows = await ReceiptStore.buildLedgerView(
      _customerPlotId,
    );
    if (!mounted) return;
    setState(() {
      _rows = rows;
      _loading = false;
    });
  }

  String get _titleSuffix {
    if (_searchMode == 'ref') {
      return 'Ref: $_query  (${_rows.length} rows incl. summaries)';
    }
    if (_searchMode == 'cust') {
      return 'Customer: $_query  (${_rows.length} rows incl. summaries)';
    }
    if (_allocationDisp.isNotEmpty) return _allocationDisp;
    return '(Select Allocation)';
  }

  void _toast(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  Future<void> _applySearch(String raw, {bool enterPressed = false}) async {
    final q = raw.trim();
    setState(() {
      _query = q;
      _searchMode = '';
      _globalRows = null;
      _searching = true;
    });

    if (q.isEmpty) {
      await _reloadRows();
      if (!mounted) return;
      setState(() => _searching = false);
      return;
    }

    final result = await ReceiptStore.smartSearch(q, societyId: _societyId);
    if (!mounted) return;

    if (result.mode == 'ref' || result.mode == 'cust') {
      setState(() {
        _searchMode = result.mode;
        _globalRows = result.rows;
        _searching = false;
      });
      await _reloadRows();
      return;
    }

    if (result.mode == 'alloc' && result.customerPlotId != null) {
      setState(() {
        _customerPlotId = result.customerPlotId!;
        _allocationDisp = result.allocationDisp ?? '';
        _query = '';
        _search.clear();
        _searching = false;
      });
      await _reloadRows();
      return;
    }

    if (enterPressed) {
      final matches =
          await ReceiptStore.searchAllocations(q, societyId: _societyId);
      if (!mounted) return;
      setState(() => _searching = false);
      if (matches.length > 1) {
        await _pickAllocation(prefilter: q);
      } else {
        await _reloadRows();
      }
      return;
    }

    setState(() => _searching = false);
    await _reloadRows();
  }

  Future<void> _pickAllocation({String prefilter = ''}) async {
    final picked = await showDialog<CustomerPlotOption>(
      context: context,
      builder: (ctx) => _AllocationPickerDialog(
        prefilter: prefilter,
        societyId: _societyId,
      ),
    );
    if (picked == null || !mounted) return;
    setState(() {
      _customerPlotId = picked.id;
      _allocationDisp = picked.disp;
      _searchMode = '';
      _globalRows = null;
      _query = '';
      _search.clear();
    });
    await _reloadRows();
  }

  Future<void> _openAdd({int? editingReceiptId}) async {
    if (editingReceiptId == null && _customerPlotId <= 0) {
      _toast('Select an allocation first.');
      await _pickAllocation();
      if (!mounted || _customerPlotId <= 0) return;
    }

    final saved = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => ReceiptFormScreen(
          customerPlotId: _customerPlotId,
          editingReceiptId: editingReceiptId,
        ),
      ),
    );
    if (saved == true && mounted) {
      if (_searchMode == 'ref' || _searchMode == 'cust') {
        await _applySearch(_query);
      } else {
        await _reloadRows();
      }
    }
  }

  Future<void> _deleteReceipt(int receiptId) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.panel,
        title: const Text('Confirm'),
        content: const Text('Delete this receipt?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('No'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Yes'),
          ),
        ],
      ),
    );
    if (ok != true) return;

    final result = await ReceiptStore.deleteReceipt(receiptId);
    if (!mounted) return;
    if (!result.ok) {
      _toast(result.message);
      return;
    }
    _toast(result.message);
    if (_searchMode == 'ref' || _searchMode == 'cust') {
      await _applySearch(_query);
    } else {
      await _reloadRows();
    }
  }

  Future<void> _whatsApp(ReceiptLedgerRow row) async {
    final mobile = row.customerMobile.trim();
    if (mobile.isEmpty) {
      _toast('Customer mobile not available.');
      return;
    }
    final msg = ReceiptStore.buildWhatsAppMessage(
      rows: _rows,
      allocationDisp: _allocationDisp,
    );
    final ok = await WhatsAppOpen.send(mobile: mobile, message: msg);
    if (!mounted) return;
    if (!ok) {
      await Clipboard.setData(ClipboardData(text: msg));
      _toast('WhatsApp open nahi hua — message copied.');
    }
  }

  @override
  Widget build(BuildContext context) {
    final money = NumberFormat('#,##0.00');
    final busy = _loading || _searching;

    return Scaffold(
      backgroundColor: Colors.transparent,
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _openAdd(),
        icon: const Icon(Icons.add),
        label: const Text('Add'),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
        children: [
          SectionHeader(
            title: 'Receipt Form',
            subtitle: 'Receipt View — $_titleSuffix',
            trailing: TextButton.icon(
              onPressed: () => _pickAllocation(),
              icon: const Icon(Icons.swap_horiz, size: 18),
              label: const Text('Allocation'),
            ),
          ),
          const SizedBox(height: 12),
          SoftPanel(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            child: TextField(
              controller: _search,
              decoration: const InputDecoration(
                hintText: 'Search ref / notes / customer / plot…',
                prefixIcon: Icon(Icons.search),
                border: InputBorder.none,
                enabledBorder: InputBorder.none,
                focusedBorder: InputBorder.none,
                filled: false,
              ),
              onChanged: (v) => _applySearch(v),
              onSubmitted: (v) => _applySearch(v, enterPressed: true),
            ),
          ),
          const SizedBox(height: 8),
          SoftPanel(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            child: DropdownButtonFormField<int>(
              value: _societyId,
              decoration: const InputDecoration(
                labelText: 'Society',
                border: InputBorder.none,
                enabledBorder: InputBorder.none,
                focusedBorder: InputBorder.none,
                filled: false,
              ),
              items: [
                const DropdownMenuItem(value: 0, child: Text('All Societies')),
                ..._societies.map(
                  (s) => DropdownMenuItem(value: s.id, child: Text(s.name)),
                ),
              ],
              onChanged: _onSocietyChanged,
            ),
          ),
          const SizedBox(height: 12),
          if (busy)
            const SoftPanel(
              child: Padding(
                padding: EdgeInsets.symmetric(vertical: 24),
                child: Center(child: CircularProgressIndicator()),
              ),
            )
          else if (_customerPlotId <= 0 && _globalRows == null)
            SoftPanel(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Select an allocation first.',
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 10),
                  ElevatedButton(
                    onPressed: () => _pickAllocation(),
                    child: const Text('Select Allocation'),
                  ),
                ],
              ),
            )
          else if (_rows.isEmpty)
            SoftPanel(
              child: Text(
                'No ledger rows.',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            )
          else
            ..._rows.asMap().entries.map((entry) {
              final i = entry.key;
              final r = entry.value;
              return Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: SoftPanel(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Text(
                            'Sr# ${i + 1}',
                            style: Theme.of(context).textTheme.labelLarge,
                          ),
                          const SizedBox(width: 8),
                          if (r.date.isNotEmpty)
                            Text(
                              r.date,
                              style: Theme.of(context).textTheme.bodyMedium,
                            ),
                          const Spacer(),
                          if (r.isSummary)
                            StatusChip(
                              switch (r.rowType) {
                                ReceiptRowType.opening => 'OPENING',
                                ReceiptRowType.total => 'TOTAL',
                                ReceiptRowType.closing => 'CLOSING',
                                _ => '',
                              },
                            ),
                          if (r.mode.isNotEmpty) StatusChip(r.mode),
                        ],
                      ),
                      const SizedBox(height: 6),
                      Text(
                        r.description,
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              fontWeight:
                                  r.isSummary ? FontWeight.w700 : FontWeight.w500,
                            ),
                      ),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          _moneyCol(context, 'Debit', r.debit, money),
                          const SizedBox(width: 12),
                          _moneyCol(context, 'Credit', r.credit, money),
                          if (r.balance != null) ...[
                            const SizedBox(width: 12),
                            _moneyCol(
                              context,
                              'Balance',
                              r.balance!,
                              money,
                              negativeRed: true,
                            ),
                          ],
                        ],
                      ),
                      if (r.refNo.isNotEmpty) ...[
                        const SizedBox(height: 4),
                        Text(
                          'ReceiptNo: ${r.refNo}',
                          style: Theme.of(context).textTheme.bodyMedium,
                        ),
                      ],
                      if (r.isReceipt) ...[
                        const SizedBox(height: 10),
                        Wrap(
                          spacing: 8,
                          runSpacing: 8,
                          children: [
                            OutlinedButton(
                              onPressed: () =>
                                  _openAdd(editingReceiptId: r.receiptId),
                              child: const Text('Edit'),
                            ),
                            OutlinedButton(
                              style: OutlinedButton.styleFrom(
                                foregroundColor: const Color(0xFFFF5252),
                              ),
                              onPressed: () {
                                if (r.receiptId != null) {
                                  _deleteReceipt(r.receiptId!);
                                }
                              },
                              child: const Text('Delete'),
                            ),
                            ElevatedButton(
                              style: ElevatedButton.styleFrom(
                                backgroundColor: const Color(0xFF25D366),
                                foregroundColor: Colors.white,
                              ),
                              onPressed: () => _whatsApp(r),
                              child: const Text('WhatsApp'),
                            ),
                          ],
                        ),
                      ],
                    ],
                  ),
                ),
              );
            }),
        ],
      ),
    );
  }

  Widget _moneyCol(
    BuildContext context,
    String label,
    double value,
    NumberFormat money, {
    bool negativeRed = false,
  }) {
    final color = negativeRed && value < 0
        ? const Color(0xFFCD5C5C)
        : AppColors.sand;
    return Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: Theme.of(context).textTheme.labelLarge),
          Text(
            money.format(value),
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  color: color,
                ),
          ),
        ],
      ),
    );
  }
}

class _AllocationPickerDialog extends StatefulWidget {
  const _AllocationPickerDialog({
    this.prefilter = '',
    this.societyId = 0,
  });

  final String prefilter;
  final int societyId;

  @override
  State<_AllocationPickerDialog> createState() =>
      _AllocationPickerDialogState();
}

class _AllocationPickerDialogState extends State<_AllocationPickerDialog> {
  late final TextEditingController _q;
  late int _societyId;
  List<IdName> _societies = const [];
  List<CustomerPlotOption> _rows = const [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _q = TextEditingController(text: widget.prefilter);
    _societyId = widget.societyId;
    _bootstrap();
  }

  @override
  void dispose() {
    _q.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    final societies = await PlotInventoryStore.societies();
    if (!mounted) return;
    setState(() => _societies = societies);
    await _reload();
  }

  Future<void> _reload() async {
    setState(() => _loading = true);
    final rows = await ReceiptStore.activeCustomerPlots(
      query: _q.text,
      societyId: _societyId,
    );
    if (!mounted) return;
    setState(() {
      _rows = rows;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      backgroundColor: AppColors.panel,
      title: const Text('Select Allocation'),
      content: SizedBox(
        width: 420,
        height: 420,
        child: Column(
          children: [
            TextField(
              controller: _q,
              decoration: const InputDecoration(
                hintText: 'Search customer / plot / society / block…',
                prefixIcon: Icon(Icons.search),
              ),
              onChanged: (_) => _reload(),
            ),
            const SizedBox(height: 8),
            DropdownButtonFormField<int>(
              value: _societyId,
              decoration: const InputDecoration(
                labelText: 'Society',
                isDense: true,
              ),
              items: [
                const DropdownMenuItem(value: 0, child: Text('All Societies')),
                ..._societies.map(
                  (s) => DropdownMenuItem(value: s.id, child: Text(s.name)),
                ),
              ],
              onChanged: (v) {
                setState(() => _societyId = v ?? 0);
                _reload();
              },
            ),
            const SizedBox(height: 10),
            Expanded(
              child: _loading
                  ? const Center(child: CircularProgressIndicator())
                  : _rows.isEmpty
                      ? const Center(
                          child: Text('Active allocations not found.'),
                        )
                      : ListView.separated(
                          itemCount: _rows.length,
                          separatorBuilder: (_, _) => const Divider(height: 1),
                          itemBuilder: (context, i) {
                            final r = _rows[i];
                            return ListTile(
                              dense: true,
                              title: Text(
                                r.pickerDisp,
                                style: Theme.of(context).textTheme.titleMedium,
                              ),
                              subtitle: Text('ID ${r.id}'),
                              onTap: () => Navigator.pop(context, r),
                            );
                          },
                        ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Close'),
        ),
      ],
    );
  }
}

/// Same as C# `frmReciptfromAdd` — labels match designer (incl. "Recipt Form Add").
class ReceiptFormScreen extends StatefulWidget {
  const ReceiptFormScreen({
    super.key,
    this.customerPlotId = 0,
    this.editingReceiptId,
  });

  final int customerPlotId;
  final int? editingReceiptId;

  @override
  State<ReceiptFormScreen> createState() => _ReceiptFormScreenState();
}

class _ReceiptFormScreenState extends State<ReceiptFormScreen> {
  CustomerPlotOption? _allocation;
  List<CustomerPlotOption> _opts = const [];
  List<IdName> _partners = const [];
  IdName? _partner;
  String? _mode = 'Cash';
  final _refNo = TextEditingController();
  final _amount = TextEditingController(text: '0');
  final _notes = TextEditingController();
  late DateTime _receiptDate;
  final _dateFmt = DateFormat('yyyy-MM-dd');
  double? _remaining;
  bool _loading = true;
  bool _saving = false;
  bool _coverInstallments = false;

  bool get _isEdit => widget.editingReceiptId != null;

  @override
  void initState() {
    super.initState();
    _receiptDate = DateTime.now();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    final opts = await ReceiptStore.activeCustomerPlots();
    final partners = await PartnerCashStore.partners();
    if (!mounted) return;

    CustomerPlotOption? allocation;
    if (widget.customerPlotId > 0) {
      allocation = await ReceiptStore.customerPlotById(widget.customerPlotId) ??
          (opts.isEmpty ? null : opts.first);
    } else if (opts.isNotEmpty) {
      allocation = opts.first;
    }

    if (!mounted) return;
    setState(() {
      _opts = opts;
      _partners = partners;
      _allocation = allocation;
    });

    if (allocation != null) {
      await _loadPartnerForPlot(allocation.id);
    }

    if (_isEdit) {
      await _loadEdit(widget.editingReceiptId!);
    } else {
      final nextRef = await ReceiptStore.nextAutoRefNo();
      if (!mounted) return;
      _refNo.text = nextRef;
      await _refreshRemaining();
      if (!mounted) return;
      setState(() => _loading = false);
    }
  }

  Future<void> _loadPartnerForPlot(int cpId) async {
    final pid = await PartnerCashStore.getAllocationPartnerId(cpId);
    if (!mounted) return;
    IdName? match;
    if (pid != null) {
      for (final p in _partners) {
        if (p.id == pid) {
          match = p;
          break;
        }
      }
    }
    setState(() => _partner = match);
  }

  Future<void> _loadEdit(int receiptId) async {
    final r = await ReceiptStore.receiptById(receiptId);
    if (!mounted) return;
    if (r == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Receipt not found.')),
      );
      Navigator.pop(context);
      return;
    }
    final allocation = await ReceiptStore.customerPlotById(r.customerPlotId);
    if (!mounted) return;
    setState(() {
      _allocation = allocation;
      _mode = r.mode == 'Bank' ? 'Bank' : 'Cash';
      _refNo.text = r.refNo ?? '';
      _amount.text = r.amount.toStringAsFixed(2);
      _coverInstallments = InstallmentCover.hasToken(r.notes);
      _notes.text = InstallmentCover.stripToken(r.notes);
      try {
        _receiptDate = DateTime.parse(r.receiptDate);
      } catch (_) {
        _receiptDate = DateTime.now();
      }
    });
    await _refreshRemaining();
    if (!mounted) return;
    setState(() => _loading = false);
  }

  Future<void> _refreshRemaining() async {
    if (_allocation == null) {
      setState(() => _remaining = null);
      return;
    }
    final rm = await ReceiptStore.getRemaining(
      _allocation!.id,
      excludeReceiptId: widget.editingReceiptId,
    );
    if (!mounted) return;
    setState(() => _remaining = rm.remaining);
  }

  @override
  void dispose() {
    _refNo.dispose();
    _amount.dispose();
    _notes.dispose();
    super.dispose();
  }

  void _toast(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _receiptDate,
      firstDate: DateTime(2000),
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
    if (picked != null) setState(() => _receiptDate = picked);
  }

  Future<void> _save() async {
    if (_allocation == null) {
      _toast('Allocation select karein.');
      return;
    }
    final amount = double.tryParse(_amount.text.trim()) ?? 0;
    if (amount <= 0) {
      _toast('Amount > 0 required.');
      return;
    }
    if (_mode == null) {
      _toast('Mode select karein.');
      return;
    }

    setState(() => _saving = true);
    var notes = _notes.text;
    if (_coverInstallments) {
      try {
        final ctx = await ReceiptStore.installmentCoverContext(
          _allocation!.id,
          excludeReceiptId: widget.editingReceiptId,
        );
        final keys = InstallmentCover.computeCoverKeys(
          booking: ctx.booking,
          asOf: DateTime.now(),
          amount: amount,
          monthly: ctx.monthly,
          alreadyPaid: ctx.paidKeys,
        );
        notes = InstallmentCover.applyToken(notes, keys);
      } catch (_) {}
    } else {
      notes = InstallmentCover.stripToken(notes);
    }
    final result = await ReceiptStore.saveReceipt(
      customerPlotId: _allocation!.id,
      receiptDate: _dateFmt.format(_receiptDate),
      amount: amount,
      mode: _mode!,
      refNo: _refNo.text,
      notes: notes,
      editingReceiptId: widget.editingReceiptId,
      partnerId: _partner?.id,
    );
    if (!mounted) return;
    setState(() => _saving = false);

    if (!result.ok) {
      _toast(result.message);
      if (result.remaining > 0 &&
          result.message.contains('Amount remaining se zyada')) {
        setState(() => _amount.text = result.remaining.toStringAsFixed(2));
      }
      return;
    }

    if (result.remaining <= 0) {
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          backgroundColor: AppColors.panel,
          title: const Text('Completed'),
          content: const Text(
            'Payment completed. No further receipts allowed for this allocation.',
          ),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.pop(ctx);
                Navigator.pop(context, true);
              },
              child: const Text('OK'),
            ),
          ],
        ),
      );
      return;
    }

    Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    final title = _isEdit ? 'Edit Receipt' : 'Add Receipt';

    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: Text(_isEdit
              ? 'Edit Receipt #${widget.editingReceiptId}'
              : 'Recipt Form Add'),
        ),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
                children: [
                  SoftPanel(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text(
                          title,
                          style: Theme.of(context).textTheme.headlineMedium,
                        ),
                        const SizedBox(height: 14),
                        DropdownButtonFormField<String>(
                          key: ValueKey(_mode),
                          initialValue: _mode,
                          decoration:
                              const InputDecoration(labelText: 'Mode'),
                          dropdownColor: AppColors.panel,
                          items: const [
                            DropdownMenuItem(
                              value: 'Cash',
                              child: Text('Cash'),
                            ),
                            DropdownMenuItem(
                              value: 'Bank',
                              child: Text('Bank'),
                            ),
                          ],
                          onChanged: (v) => setState(() => _mode = v),
                        ),
                        const SizedBox(height: 10),
                        TextField(
                          controller: _refNo,
                          decoration:
                              const InputDecoration(labelText: 'Ref No'),
                        ),
                        const SizedBox(height: 10),
                        InkWell(
                          onTap: _pickDate,
                          borderRadius: BorderRadius.circular(12),
                          child: InputDecorator(
                            decoration: const InputDecoration(
                              labelText: 'Receipt Date',
                              suffixIcon: Icon(Icons.calendar_today_outlined),
                            ),
                            child: Text(
                              _dateFmt.format(_receiptDate),
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                          ),
                        ),
                        const SizedBox(height: 10),
                        DropdownButtonFormField<CustomerPlotOption>(
                          key: ValueKey(_allocation?.id ?? 'cp'),
                          initialValue: _allocation,
                          decoration: const InputDecoration(
                            labelText: 'Allocation Customer Name',
                          ),
                          dropdownColor: AppColors.panel,
                          isExpanded: true,
                          items: _opts
                              .map(
                                (o) => DropdownMenuItem(
                                  value: o,
                                  child: Text(
                                    o.disp,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                ),
                              )
                              .toList(),
                          onChanged: (v) async {
                            setState(() => _allocation = v);
                            if (v != null) await _loadPartnerForPlot(v.id);
                            await _refreshRemaining();
                          },
                        ),
                        const SizedBox(height: 10),
                        DropdownButtonFormField<IdName?>(
                          key: ValueKey(_partner?.id ?? 'partner'),
                          initialValue: _partner,
                          decoration: const InputDecoration(
                            labelText: 'Partner (Cash Book)',
                          ),
                          items: [
                            const DropdownMenuItem(
                              value: null,
                              child: Text('(None)'),
                            ),
                            ..._partners.map(
                              (p) => DropdownMenuItem(
                                value: p,
                                child: Text(p.name),
                              ),
                            ),
                          ],
                          onChanged: (v) => setState(() => _partner = v),
                        ),
                        const SizedBox(height: 10),
                        TextField(
                          controller: _amount,
                          keyboardType: const TextInputType.numberWithOptions(
                            decimal: true,
                          ),
                          decoration:
                              const InputDecoration(labelText: 'Amount'),
                        ),
                        const SizedBox(height: 10),
                        TextField(
                          controller: _notes,
                          maxLines: 3,
                          decoration:
                              const InputDecoration(labelText: 'Note'),
                        ),
                        CheckboxListTile(
                          contentPadding: EdgeInsets.zero,
                          value: _coverInstallments,
                          onChanged: (v) =>
                              setState(() => _coverInstallments = v ?? false),
                          title: const Text(
                            'Cover previous/current installments only — no upcoming (WhatsApp hide)',
                          ),
                          controlAffinity: ListTileControlAffinity.leading,
                        ),
                        if (_allocation != null && _remaining != null) ...[
                          const SizedBox(height: 8),
                          Text(
                            'Remaining: ${NumberFormat('#,##0.00').format(_remaining)}',
                            style: Theme.of(context)
                                .textTheme
                                .bodyMedium
                                ?.copyWith(color: AppColors.tealBright),
                          ),
                        ],
                        const SizedBox(height: 18),
                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton(
                                onPressed: () => Navigator.pop(context),
                                child: const Text('Close'),
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: ElevatedButton(
                                onPressed: _saving ? null : _save,
                                child: _saving
                                    ? const SizedBox(
                                        width: 20,
                                        height: 20,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                        ),
                                      )
                                    : Text(_isEdit ? 'Update' : 'Save'),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ],
              ),
      ),
    );
  }
}
