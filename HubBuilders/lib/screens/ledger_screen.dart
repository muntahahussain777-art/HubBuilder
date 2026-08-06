import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';

import '../data/allocate_store.dart';
import '../data/ledger_store.dart';
import '../data/plot_inventory_store.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same as C# `frmCustomerLedgerAdd` — Filter Panel Ledger Form.
class LedgerScreen extends StatefulWidget {
  const LedgerScreen({super.key});

  @override
  State<LedgerScreen> createState() => _LedgerScreenState();
}

class _LedgerScreenState extends State<LedgerScreen> {
  final _dateFmt = DateFormat('yyyy-MM-dd');

  List<CustomerItem> _customers = const [];
  List<IdName> _societies = const [];

  CustomerItem? _customer;
  IdName? _society;
  LedgerPlotOption? _plot;
  bool _plotOptionsAreAllocationIds = true;
  List<LedgerPlotOption> _plotOptions = const [];

  late DateTime _from;
  late DateTime _to;

  List<LedgerRow> _rows = const [];
  double _periodDebit = 0;
  double _periodCredit = 0;
  double _balance = 0;
  bool _loaded = false;
  bool _bootstrapping = true;
  bool _loadingPlots = false;
  bool _loadingLedger = false;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _from = DateTime(now.year, now.month, 1);
    _to = DateTime(now.year, now.month, now.day);
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    final custs = await LedgerStore.customers();
    final socs = await LedgerStore.societies();
    if (!mounted) return;
    setState(() {
      _customers = custs;
      _societies = socs;
      if (custs.isNotEmpty) _customer = custs.first;
      if (socs.isNotEmpty) _society = socs.first;
      _bootstrapping = false;
    });
    await _refreshPlotOptions();
  }

  void _toast(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  Future<void> _refreshPlotOptions() async {
    setState(() => _loadingPlots = true);
    final result = await LedgerStore.loadPlotOptions(
      customerId: _customer?.id,
      societyId: _society?.id,
    );
    if (!mounted) return;
    setState(() {
      _plotOptionsAreAllocationIds = result.isAllocationIds;
      _plotOptions = result.options;
      if (_plotOptions.isEmpty) {
        _plot = null;
      } else if (_plot == null ||
          !_plotOptions.any((o) => o.valueId == _plot!.valueId)) {
        _plot = _plotOptions.first;
      } else {
        _plot = _plotOptions.firstWhere((o) => o.valueId == _plot!.valueId);
      }
      _loadingPlots = false;
    });
  }

  void _onCustomer(CustomerItem? v) {
    setState(() => _customer = v);
    _refreshPlotOptions();
  }

  void _onSociety(IdName? v) {
    setState(() => _society = v);
    _refreshPlotOptions();
  }

  Future<void> _pickFrom() async {
    final picked = await _pickDate(_from);
    if (picked != null) setState(() => _from = picked);
  }

  Future<void> _pickTo() async {
    final picked = await _pickDate(_to);
    if (picked != null) setState(() => _to = picked);
  }

  Future<DateTime?> _pickDate(DateTime initial) {
    return showDatePicker(
      context: context,
      initialDate: initial,
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
  }

  Future<void> _load() async {
    if (_customer == null) {
      _toast('Customer required.');
      return;
    }

    setState(() => _loadingLedger = true);
    final result = await LedgerStore.load(
      customerId: _customer!.id,
      societyId: _society?.id,
      selectedValueId: _plot?.valueId,
      isAllocationIds: _plotOptionsAreAllocationIds,
      from: _from,
      to: _to,
    );
    if (!mounted) return;

    setState(() {
      _rows = result.rows;
      _periodDebit = result.periodDebit;
      _periodCredit = result.periodCredit;
      _balance = result.balance;
      _loaded = true;
      _loadingLedger = false;
    });
  }

  Future<void> _print() async {
    if (!_loaded) {
      await _load();
      if (!_loaded) return;
    }

    final cust = _customer?.name ?? '';
    final soc = _society?.name ?? '';
    final plot = _plot?.label ?? '';
    final period =
        '${_dateFmt.format(_from)} → ${_dateFmt.format(_to)}';
    final subtitle =
        'Customer: $cust    Society: $soc    Plot: $plot    Period: $period';

    final buf = StringBuffer()
      ..writeln('Customer Ledger')
      ..writeln(subtitle)
      ..writeln()
      ..writeln('Debit (Period): ${LedgerStore.formatMoney(_periodDebit)}')
      ..writeln('Credit (Period): ${LedgerStore.formatMoney(_periodCredit)}')
      ..writeln('Balance (D − C): ${LedgerStore.formatMoney(_balance)}')
      ..writeln()
      ..writeln(
        'Date | VoucherNo | Narration | Debit | Credit | Type | RunningBalance',
      );

    for (final r in _rows) {
      final date = r.txnDate.length >= 10 ? r.txnDate.substring(0, 10) : r.txnDate;
      buf.writeln(
        '$date | ${r.voucherNo ?? ''} | ${r.narration} | '
        '${LedgerStore.formatMoney(r.debit)} | '
        '${LedgerStore.formatMoney(r.credit)} | '
        '${r.txnType} | ${LedgerStore.formatMoney(r.runningBalance)}',
      );
    }

    final text = buf.toString();
    await Clipboard.setData(ClipboardData(text: text));

    if (!mounted) return;
    await showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.panel,
        title: const Text('Customer Ledger'),
        content: SizedBox(
          width: double.maxFinite,
          child: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(subtitle, style: Theme.of(ctx).textTheme.bodyMedium),
                const SizedBox(height: 12),
                Text(
                  'Debit (Period): ${LedgerStore.formatMoney(_periodDebit)}\n'
                  'Credit (Period): ${LedgerStore.formatMoney(_periodCredit)}\n'
                  'Balance (D − C): ${LedgerStore.formatMoney(_balance)}',
                  style: Theme.of(ctx).textTheme.titleMedium,
                ),
                const SizedBox(height: 12),
                Text(
                  _rows.isEmpty
                      ? 'No data.'
                      : '${_rows.length} row(s) — copied to clipboard.',
                  style: Theme.of(ctx).textTheme.bodyMedium,
                ),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('OK'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      body: Column(
        children: [
          Expanded(
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
              children: [
                const SectionHeader(
                  title: 'Filter Panel Ledger Form',
                  subtitle:
                      'Customer Ledger — Customer, Society, Plot NO, From, To',
                ),
                const SizedBox(height: 12),
                SoftPanel(
                  child: _bootstrapping
                      ? const Padding(
                          padding: EdgeInsets.symmetric(vertical: 24),
                          child: Center(child: CircularProgressIndicator()),
                        )
                      : Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            DropdownButtonFormField<CustomerItem>(
                              key: ValueKey(_customer?.id ?? 'cust'),
                              initialValue: _customer,
                              decoration:
                                  const InputDecoration(labelText: 'Customer'),
                              dropdownColor: AppColors.panel,
                              items: _customers
                                  .map(
                                    (c) => DropdownMenuItem(
                                      value: c,
                                      child: Text(c.name),
                                    ),
                                  )
                                  .toList(),
                              onChanged: _onCustomer,
                            ),
                            const SizedBox(height: 10),
                            DropdownButtonFormField<IdName>(
                              key: ValueKey(_society?.id ?? 'soc'),
                              initialValue: _society,
                              decoration:
                                  const InputDecoration(labelText: 'Society'),
                              dropdownColor: AppColors.panel,
                              items: _societies
                                  .map(
                                    (s) => DropdownMenuItem(
                                      value: s,
                                      child: Text(s.name),
                                    ),
                                  )
                                  .toList(),
                              onChanged: _onSociety,
                            ),
                            const SizedBox(height: 10),
                            if (_loadingPlots)
                              const Padding(
                                padding: EdgeInsets.symmetric(vertical: 12),
                                child: Center(
                                  child: SizedBox(
                                    width: 24,
                                    height: 24,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                    ),
                                  ),
                                ),
                              )
                            else
                              DropdownButtonFormField<LedgerPlotOption>(
                                key: ValueKey(
                                  'plot-${_customer?.id}-${_society?.id}-'
                                  '${_plot?.valueId}-${_plotOptions.length}',
                                ),
                                initialValue: _plot,
                                decoration: const InputDecoration(
                                  labelText: 'Plot NO',
                                ),
                                dropdownColor: AppColors.panel,
                                items: _plotOptions
                                    .map(
                                      (p) => DropdownMenuItem(
                                        value: p,
                                        child: Text(p.label),
                                      ),
                                    )
                                    .toList(),
                                onChanged: (v) => setState(() => _plot = v),
                              ),
                            const SizedBox(height: 10),
                            InkWell(
                              onTap: _pickFrom,
                              borderRadius: BorderRadius.circular(12),
                              child: InputDecorator(
                                decoration: const InputDecoration(
                                  labelText: 'From',
                                  suffixIcon:
                                      Icon(Icons.calendar_today_outlined),
                                ),
                                child: Text(
                                  _dateFmt.format(_from),
                                  style:
                                      Theme.of(context).textTheme.titleMedium,
                                ),
                              ),
                            ),
                            const SizedBox(height: 10),
                            InkWell(
                              onTap: _pickTo,
                              borderRadius: BorderRadius.circular(12),
                              child: InputDecorator(
                                decoration: const InputDecoration(
                                  labelText: 'To',
                                  suffixIcon:
                                      Icon(Icons.calendar_today_outlined),
                                ),
                                child: Text(
                                  _dateFmt.format(_to),
                                  style:
                                      Theme.of(context).textTheme.titleMedium,
                                ),
                              ),
                            ),
                            const SizedBox(height: 16),
                            Row(
                              children: [
                                Expanded(
                                  child: ElevatedButton(
                                    onPressed:
                                        _loadingLedger ? null : _load,
                                    child: _loadingLedger
                                        ? const SizedBox(
                                            width: 20,
                                            height: 20,
                                            child: CircularProgressIndicator(
                                              strokeWidth: 2,
                                            ),
                                          )
                                        : const Text('Load'),
                                  ),
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: OutlinedButton(
                                    onPressed: _print,
                                    child: const Text('Print'),
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                ),
                const SizedBox(height: 12),
                if (_loadingLedger)
                  const SoftPanel(
                    child: Padding(
                      padding: EdgeInsets.symmetric(vertical: 16),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  )
                else if (!_loaded)
                  SoftPanel(
                    child: Text(
                      'Select filters and tap Load.',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  )
                else if (_rows.isEmpty)
                  SoftPanel(
                    child: Text(
                      'No data.',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  )
                else
                  ..._rows.map((r) {
                    final date = r.txnDate.length >= 10
                        ? r.txnDate.substring(0, 10)
                        : r.txnDate;
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 8),
                      child: SoftPanel(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Expanded(
                                  child: Text(
                                    date,
                                    style:
                                        Theme.of(context).textTheme.titleMedium,
                                  ),
                                ),
                                StatusChip(r.txnType),
                              ],
                            ),
                            if (r.voucherNo != null &&
                                r.voucherNo!.trim().isNotEmpty) ...[
                              const SizedBox(height: 4),
                              Text(
                                'VoucherNo: ${r.voucherNo}',
                                style: Theme.of(context).textTheme.bodyMedium,
                              ),
                            ],
                            const SizedBox(height: 4),
                            Text(
                              r.narration,
                              style: Theme.of(context).textTheme.bodyMedium,
                            ),
                            const SizedBox(height: 8),
                            Row(
                              children: [
                                Expanded(
                                  child: Text(
                                    'Debit: ${LedgerStore.formatMoney(r.debit)}',
                                    style: Theme.of(context)
                                        .textTheme
                                        .labelLarge
                                        ?.copyWith(color: AppColors.mist),
                                  ),
                                ),
                                Expanded(
                                  child: Text(
                                    'Credit: ${LedgerStore.formatMoney(r.credit)}',
                                    style: Theme.of(context)
                                        .textTheme
                                        .labelLarge
                                        ?.copyWith(color: AppColors.tealBright),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 4),
                            Text(
                              'RunningBalance: ${LedgerStore.formatMoney(r.runningBalance)}',
                              style: Theme.of(context)
                                  .textTheme
                                  .labelLarge
                                  ?.copyWith(color: AppColors.gold),
                            ),
                          ],
                        ),
                      ),
                    );
                  }),
              ],
            ),
          ),
          // Bottom summary — same label names as C# designer;
          // values follow C# rule (period Debit / Credit / net).
          SoftPanel(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
            child: SafeArea(
              top: false,
              child: Column(
                children: [
                  _SummaryLine(
                    label: 'Opening Balance',
                    value: LedgerStore.formatMoney(_periodDebit),
                  ),
                  const SizedBox(height: 6),
                  _SummaryLine(
                    label: 'Closing Balance',
                    value: LedgerStore.formatMoney(_periodCredit),
                  ),
                  const SizedBox(height: 6),
                  // Designer lblBalance also says "Closing Balance"; print uses Balance (D − C).
                  _SummaryLine(
                    label: 'Balance (D − C)',
                    value: LedgerStore.formatMoney(_balance),
                    emphasize: true,
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _SummaryLine extends StatelessWidget {
  const _SummaryLine({
    required this.label,
    required this.value,
    this.emphasize = false,
  });

  final String label;
  final String value;
  final bool emphasize;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: emphasize ? AppColors.sand : AppColors.mist,
                  fontWeight: emphasize ? FontWeight.w700 : FontWeight.w500,
                ),
          ),
        ),
        Text(
          value,
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
                color: emphasize ? AppColors.tealBright : AppColors.sand,
                fontWeight: FontWeight.w700,
              ),
        ),
      ],
    );
  }
}
