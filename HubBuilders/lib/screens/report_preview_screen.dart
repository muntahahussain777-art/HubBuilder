import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/plot_inventory_store.dart';
import '../data/png_pdf.dart';
import '../data/report_pdf.dart';
import '../data/reports_store.dart';
import '../data/whatsapp_open.dart';
import '../theme/app_colors.dart';
import '../widgets/type_suggest_field.dart';
import '../widgets/ui_bits.dart';

/// Generic WinForms-parity report preview (list + filters).
class ReportPreviewScreen extends StatefulWidget {
  const ReportPreviewScreen({
    super.key,
    required this.kind,
    required this.title,
    this.onBack,
    this.showDates = false,
    this.showAsOf = false,
    this.showSociety = false,
    this.showCustomer = false,
    this.showBroker = false,
    this.showCustomerPlot = false,
    this.showYear = false,
    this.showMode = false,
  });

  final ReportKind kind;
  final String title;
  final VoidCallback? onBack;
  final bool showDates;
  final bool showAsOf;
  final bool showSociety;
  final bool showCustomer;
  final bool showBroker;
  final bool showCustomerPlot;
  final bool showYear;
  final bool showMode;

  @override
  State<ReportPreviewScreen> createState() => _ReportPreviewScreenState();
}

class _ReportPreviewScreenState extends State<ReportPreviewScreen> {
  final _fmt = DateFormat('yyyy-MM-dd');
  final _search = TextEditingController();
  late final TextEditingController _yearCtrl;

  List<IdName> _societies = const [];
  List<IdName> _customers = const [];
  List<IdName> _brokers = const [];
  List<IdName> _plots = const [];
  List<IdName> _blocks = const [];
  List<IdName> _plotTypes = const [];

  IdName? _society;
  IdName? _customer;
  IdName? _broker;
  IdName? _plot;
  IdName? _block;
  IdName? _plotType;
  String? _mode;
  String? _year;
  late DateTime _from;
  late DateTime _to;
  late DateTime _asOf;
  /// Month Collection VIP: 0=Current, 1=Previous, 2=Custom (WinForms cmbScope).
  int _monthScope = 0;

  ReportResult? _result;
  bool _loading = false;
  bool _boot = true;

  bool get _isAvailablePlots => widget.kind == ReportKind.availablePlots;
  bool get _isMonthCollection => widget.kind == ReportKind.monthCollectionVip;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    // Month Collection / Business Analysis / Rough Estimate (VIP): default = current month.
    if (widget.kind == ReportKind.monthCollectionVip ||
        widget.kind == ReportKind.vipBusinessAnalysis ||
        widget.kind == ReportKind.installmentDetection ||
        widget.kind == ReportKind.roughInstallmentEstimate) {
      _from = DateTime(now.year, now.month, 1);
    } else {
      _from = DateTime(now.year, 1, 1);
    }
    _to = DateTime(now.year, now.month, now.day);
    _asOf = _to;
    _year = '${now.year}';
    _yearCtrl = TextEditingController(text: _year);
    _bootstrap();
  }

  @override
  void dispose() {
    _search.dispose();
    _yearCtrl.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    final socs = widget.showSociety ? await ReportsStore.societies() : <IdName>[];
    final custs =
        widget.showCustomer ? await ReportsStore.customers() : <IdName>[];
    final brks = widget.showBroker ? await ReportsStore.brokers() : <IdName>[];
    final plots =
        widget.showCustomerPlot ? await ReportsStore.customerPlots() : <IdName>[];
    final types =
        _isAvailablePlots ? await ReportsStore.plotTypes() : <IdName>[];
    if (!mounted) return;
    setState(() {
      _societies = socs;
      _customers = custs;
      _brokers = brks;
      _plots = plots;
      _plotTypes = types;
      if (plots.isNotEmpty) _plot = plots.first;
      if (_isMonthCollection) _applyMonthScope(silent: true);
      _boot = false;
    });
  }

  void _applyMonthScope({bool silent = false}) {
    final today = DateTime.now();
    final curStart = DateTime(today.year, today.month, 1);
    final prevStart = DateTime(today.year, today.month - 1, 1);
    final prevEnd = curStart.subtract(const Duration(days: 1));
    if (_monthScope == 0) {
      _from = curStart;
      _to = DateTime(today.year, today.month, today.day);
    } else if (_monthScope == 1) {
      _from = prevStart;
      _to = prevEnd;
    }
    if (!silent) setState(() {});
  }

  Future<void> _onSocietyChanged(IdName? v) async {
    setState(() {
      _society = v == null || v.id == 0 ? null : v;
      _block = null;
      _blocks = const [];
    });
    if (_isAvailablePlots && (_society?.id ?? 0) > 0) {
      final blks = await ReportsStore.blocksForSociety(_society!.id);
      if (!mounted) return;
      setState(() => _blocks = blks);
    }
  }

  Future<void> _preview() async {
    if (_isAvailablePlots && (_society?.id ?? 0) <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Pehle Society select karein.')),
      );
      return;
    }
    setState(() => _loading = true);
    try {
      final res = await ReportsStore.run(
        widget.kind,
        keyword: _search.text,
        from: widget.showDates ? _from : null,
        to: widget.showDates ? _to : null,
        asOf: widget.showAsOf ? _asOf : null,
        societyId: _society?.id ?? 0,
        blockId: _block?.id ?? 0,
        plotTypeId: _plotType?.id ?? 0,
        customerId: _customer?.id ?? 0,
        brokerId: _broker?.id ?? 0,
        customerPlotId: _plot?.id ?? 0,
        year: widget.showYear ? _year : null,
        mode: widget.showMode ? _mode : null,
      );
      if (!mounted) return;
      setState(() => _result = res);
      if (res.lines.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(res.summary.isEmpty ? 'No records found.' : res.summary),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Report failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _savePdf() async {
    var res = _result;
    if (res == null) {
      await _preview();
      res = _result;
    }
    if (res == null) return;
    setState(() => _loading = true);
    try {
      final f = await PngPdf.saveBytes(
        bytes: await ReportPdf.build(title: widget.title, result: res),
        fileBase: widget.title.replaceAll(RegExp(r'[^\w\-]+'), '_'),
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

  Future<void> _pick(DateTime initial, ValueChanged<DateTime> set) async {
    final p = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (p != null) setState(() => set(p));
  }

  @override
  Widget build(BuildContext context) {
    if (_boot) return const Center(child: CircularProgressIndicator());

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
                      widget.title,
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              TextField(
                controller: _search,
                decoration: const InputDecoration(
                  labelText: 'Search',
                  prefixIcon: Icon(Icons.search),
                ),
              ),
              if (widget.showSociety)
                TypeSuggestField<IdName>(
                  label: _isAvailablePlots
                      ? 'Society (required)'
                      : 'Society (type to search)',
                  items: [
                    if (!_isAvailablePlots) const IdName(0, '(All)'),
                    ..._societies,
                  ],
                  selected: _society ??
                      (_isAvailablePlots
                          ? null
                          : const IdName(0, '(All)')),
                  display: (s) => s.name,
                  onSelected: (v) {
                    _onSocietyChanged(v);
                  },
                ),
              if (_isAvailablePlots) ...[
                TypeSuggestField<IdName>(
                  label: 'Block (optional)',
                  items: [const IdName(0, '(All Blocks)'), ..._blocks],
                  selected: _block ?? const IdName(0, '(All Blocks)'),
                  display: (b) => b.name,
                  onSelected: (v) =>
                      setState(() => _block = v.id == 0 ? null : v),
                ),
                TypeSuggestField<IdName>(
                  label: 'Plot Type (optional)',
                  items: [const IdName(0, '(All Types)'), ..._plotTypes],
                  selected: _plotType ?? const IdName(0, '(All Types)'),
                  display: (t) => t.name,
                  onSelected: (v) =>
                      setState(() => _plotType = v.id == 0 ? null : v),
                ),
              ],
              if (widget.showCustomer)
                TypeSuggestField<IdName>(
                  label: 'Customer (type to search)',
                  items: [const IdName(0, '(All)'), ..._customers],
                  selected: _customer ?? const IdName(0, '(All)'),
                  display: (c) => c.name,
                  onSelected: (v) =>
                      setState(() => _customer = v.id == 0 ? null : v),
                ),
              if (widget.showBroker)
                TypeSuggestField<IdName>(
                  label: 'Broker (type to search)',
                  items: [const IdName(0, '(All)'), ..._brokers],
                  selected: _broker ?? const IdName(0, '(All)'),
                  display: (b) => b.name,
                  onSelected: (v) =>
                      setState(() => _broker = v.id == 0 ? null : v),
                ),
              if (widget.showCustomerPlot)
                TypeSuggestField<IdName>(
                  label: 'Allocation (type to search)',
                  items: _plots,
                  selected: _plot,
                  display: (p) => p.name,
                  onSelected: (v) => setState(() => _plot = v),
                ),
              if (widget.showMode) ...[
                const SizedBox(height: 10),
                DropdownButtonFormField<String?>(
                  initialValue: _mode,
                  decoration: const InputDecoration(labelText: 'Mode'),
                  items: const [
                    DropdownMenuItem(value: null, child: Text('(All)')),
                    DropdownMenuItem(value: 'Cash', child: Text('Cash')),
                    DropdownMenuItem(value: 'Bank', child: Text('Bank')),
                  ],
                  onChanged: (v) => setState(() => _mode = v),
                ),
              ],
              if (widget.showYear) ...[
                const SizedBox(height: 10),
                TextField(
                  decoration: const InputDecoration(labelText: 'Year (blank = all)'),
                  controller: _yearCtrl,
                  onChanged: (v) => _year = v.trim().isEmpty ? null : v.trim(),
                ),
              ],
              if (_isMonthCollection) ...[
                const SizedBox(height: 10),
                DropdownButtonFormField<int>(
                  initialValue: _monthScope,
                  decoration: const InputDecoration(labelText: 'Detail'),
                  items: const [
                    DropdownMenuItem(value: 0, child: Text('Current month')),
                    DropdownMenuItem(value: 1, child: Text('Previous month')),
                    DropdownMenuItem(value: 2, child: Text('Custom From–To')),
                  ],
                  onChanged: (v) {
                    _monthScope = v ?? 0;
                    _applyMonthScope();
                  },
                ),
              ],
              if (widget.showDates)
                Row(
                  children: [
                    Expanded(
                      child: ListTile(
                        contentPadding: EdgeInsets.zero,
                        title: const Text('From'),
                        subtitle: Text(_fmt.format(_from)),
                        onTap: _isMonthCollection && _monthScope < 2
                            ? null
                            : () => _pick(_from, (d) => _from = d),
                      ),
                    ),
                    Expanded(
                      child: ListTile(
                        contentPadding: EdgeInsets.zero,
                        title: const Text('To'),
                        subtitle: Text(_fmt.format(_to)),
                        onTap: _isMonthCollection && _monthScope < 2
                            ? null
                            : () => _pick(_to, (d) => _to = d),
                      ),
                    ),
                  ],
                ),
              if (widget.showAsOf)
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('As of'),
                  subtitle: Text(_fmt.format(_asOf)),
                  onTap: () => _pick(_asOf, (d) => _asOf = d),
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
                ],
              ),
            ],
          ),
        ),
        if (_result != null) ...[
          const SizedBox(height: 12),
          if (_result!.summary.isNotEmpty)
            SoftPanel(
              child: Text(
                _result!.summary,
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      color: AppColors.gold,
                    ),
              ),
            ),
          if (widget.kind == ReportKind.installmentDetection) ...[
            const SizedBox(height: 8),
            SoftPanel(
              child: Text(
                'Partial = some payment, but less than installment due\n'
                'Partial = thori payment mili, poori installment nahi',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: AppColors.sand,
                    ),
              ),
            ),
          ],
          if (widget.kind == ReportKind.roughInstallmentEstimate) ...[
            const SizedBox(height: 8),
            SoftPanel(
              child: Text(
                'ROUGH / Estimate (andaza — exact contract nahi).\n'
                'Monthly: Exact = plot pe set · Block = block default · '
                'Estimate = Society+Block(+Gaz) average, warna Society average, '
                'warna (Price−Paid)/12.\n'
                'Expected = Monthly × due months · Le chuke = receipts in range · '
                'Baqi = Expected − Le chuke.',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: AppColors.sand,
                    ),
              ),
            ),
          ],
          ..._result!.lines.map(
            (r) => Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: SoftPanel(
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(r.title, style: Theme.of(context).textTheme.titleMedium),
                          if (r.subtitle.isNotEmpty) ...[
                            const SizedBox(height: 3),
                            Text(r.subtitle, style: Theme.of(context).textTheme.bodyMedium),
                          ],
                          if (r.meta.isNotEmpty)
                            Text(r.meta, style: Theme.of(context).textTheme.bodySmall),
                        ],
                      ),
                    ),
                    if (r.amount != null)
                      Text(
                        r.amount!.toStringAsFixed(2),
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              color: AppColors.gold,
                            ),
                      ),
                    if (r.whatsAppMessage.trim().isNotEmpty)
                      IconButton(
                        tooltip: 'WhatsApp',
                        icon: const Icon(Icons.chat_outlined, size: 20),
                        onPressed: () async {
                          if (r.mobile.trim().isEmpty) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(
                                content: Text('Customer ka mobile number nahi mila.'),
                              ),
                            );
                            return;
                          }
                          final ok = await WhatsAppOpen.send(
                            mobile: r.mobile,
                            message: r.whatsAppMessage,
                          );
                          if (!context.mounted) return;
                          if (!ok) {
                            ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(
                                content: Text('WhatsApp open nahi hua.'),
                              ),
                            );
                          }
                        },
                      ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ],
    );
  }
}
