import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/allocate_store.dart';
import '../data/plot_inventory_store.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same as C# Allocate view + form — SQLite-backed.
class AllocateScreen extends StatefulWidget {
  const AllocateScreen({super.key});

  @override
  State<AllocateScreen> createState() => _AllocateScreenState();
}

class _AllocateScreenState extends State<AllocateScreen> {
  final _search = TextEditingController();
  String _query = '';
  List<AllocationListRow> _rows = const [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _reload() async {
    setState(() => _loading = true);
    final rows = await AllocateStore.list(query: _query);
    if (!mounted) return;
    setState(() {
      _rows = rows;
      _loading = false;
    });
  }

  Future<void> _openAdd() async {
    final saved = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const AllocateFormScreen()),
    );
    if (saved == true && mounted) await _reload();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _openAdd,
        icon: const Icon(Icons.add),
        label: const Text('Allocate'),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
        children: [
          const SectionHeader(
            title: 'Allocate',
            subtitle:
                'Customer, Society, Block, Plot, Price, Broker',
          ),
          const SizedBox(height: 12),
          SoftPanel(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            child: TextField(
              controller: _search,
              decoration: const InputDecoration(
                hintText: 'Search customer / mobile / plot / society…',
                prefixIcon: Icon(Icons.search),
                border: InputBorder.none,
                enabledBorder: InputBorder.none,
                focusedBorder: InputBorder.none,
                filled: false,
              ),
              onChanged: (v) {
                _query = v;
                _reload();
              },
            ),
          ),
          const SizedBox(height: 12),
          if (_loading)
            const Center(child: Padding(
              padding: EdgeInsets.all(24),
              child: CircularProgressIndicator(),
            ))
          else if (_rows.isEmpty)
            SoftPanel(
              child: Text(
                'No allocations found. Add customers, plots, then allocate.',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            )
          else
            ..._rows.map(
              (r) => Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: SoftPanel(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              r.customer,
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                          ),
                          StatusChip(r.status),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text(
                        '${r.society} · ${r.block} · ${r.plotNo} · ${r.type}',
                        style: Theme.of(context).textTheme.bodyMedium,
                      ),
                      const SizedBox(height: 6),
                      Row(
                        children: [
                          Text(
                            'Rs ${NumberFormat('#,##0').format(r.price)}',
                            style: Theme.of(context)
                                .textTheme
                                .labelLarge
                                ?.copyWith(color: AppColors.tealBright),
                          ),
                          const SizedBox(width: 12),
                          Text(
                            r.bookingDate,
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                          const Spacer(),
                          Text(
                            r.phone,
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class AllocateFormScreen extends StatefulWidget {
  const AllocateFormScreen({super.key});

  @override
  State<AllocateFormScreen> createState() => _AllocateFormScreenState();
}

class _AllocateFormScreenState extends State<AllocateFormScreen> {
  CustomerItem? _customer;
  IdName? _society;
  BlockItem? _block;
  PlotTypeItem? _plotType;
  StoredPlot? _plot;
  BrokerItem? _broker;

  final _price = TextEditingController();
  final _notes = TextEditingController();
  final _commission = TextEditingController(text: '0');
  late DateTime _bookingDate;
  final _dateFmt = DateFormat('yyyy-MM-dd');

  List<CustomerItem> _customers = const [];
  List<IdName> _societies = const [];
  List<BlockItem> _blocks = const [];
  List<PlotTypeItem> _types = const [];
  List<StoredPlot> _availablePlots = const [];
  List<BrokerItem> _brokers = const [];
  bool _loading = true;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _bookingDate = DateTime.now();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    final customers = await AllocateStore.customers();
    final societies = await PlotInventoryStore.societies();
    final brokers = await AllocateStore.brokers(activeOnly: true);
    if (!mounted) return;
    setState(() {
      _customers = customers;
      _societies = societies;
      _brokers = brokers;
      _loading = false;
    });
  }

  Future<void> _refreshCascade() async {
    if (_society == null) {
      setState(() {
        _blocks = const [];
        _types = const [];
        _availablePlots = const [];
      });
      return;
    }
    final blocks = await PlotInventoryStore.blocksFor(_society!.id);
    final types = await PlotInventoryStore.typesFor(_society!.id);
    List<StoredPlot> plots = const [];
    if (_block != null && _plotType != null) {
      plots = await PlotInventoryStore.availablePlots(
        societyId: _society!.id,
        blockId: _block!.id,
        plotTypeId: _plotType!.id,
      );
    }
    if (!mounted) return;
    setState(() {
      _blocks = blocks;
      _types = types;
      _availablePlots = plots;
    });
  }

  Future<void> _refreshPlots() async {
    if (_society == null || _block == null || _plotType == null) {
      setState(() => _availablePlots = const []);
      return;
    }
    final plots = await PlotInventoryStore.availablePlots(
      societyId: _society!.id,
      blockId: _block!.id,
      plotTypeId: _plotType!.id,
    );
    if (!mounted) return;
    setState(() {
      _availablePlots = plots;
      if (_plot != null && !plots.any((p) => p.id == _plot!.id)) {
        _plot = null;
      }
    });
  }

  @override
  void dispose() {
    _price.dispose();
    _notes.dispose();
    _commission.dispose();
    super.dispose();
  }

  void _toast(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  Future<void> _onSociety(IdName? v) async {
    setState(() {
      _society = v;
      _block = null;
      _plotType = null;
      _plot = null;
    });
    await _refreshCascade();
  }

  void _onBroker(BrokerItem? v) {
    setState(() {
      _broker = v;
      if (v != null) {
        _commission.text = v.commissionDefault.toStringAsFixed(0);
      }
    });
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _bookingDate,
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
    if (picked != null) setState(() => _bookingDate = picked);
  }

  void _clearAfterSave() {
    setState(() {
      _plot = null;
      _broker = null;
      _price.text = '';
      _notes.clear();
      _commission.text = '0';
      _bookingDate = DateTime.now();
    });
  }

  Future<void> _save() async {
    if (_customer == null) {
      _toast('Customer select karein.');
      return;
    }
    if (_society == null) {
      _toast('Society select karein.');
      return;
    }
    if (_block == null) {
      _toast('Block select karein.');
      return;
    }
    if (_plotType == null) {
      _toast('Plot Type select karein.');
      return;
    }
    if (_plot == null) {
      _toast('Plot No select karein (sirf Available dikhte hain).');
      return;
    }
    final price = double.tryParse(_price.text.trim()) ?? 0;
    if (price <= 0) {
      _toast('Price required hai.');
      return;
    }
    if (_broker == null) {
      _toast('Broker select karein.');
      return;
    }
    final comm = double.tryParse(_commission.text.trim()) ?? -1;
    if (comm < 0) {
      _toast('Broker commission 0 ya us se zyada honi chahiye.');
      return;
    }

    setState(() => _saving = true);
    final result = await AllocateStore.allocate(
      customerId: _customer!.id,
      societyId: _society!.id,
      blockId: _block!.id,
      plotTypeId: _plotType!.id,
      plotId: _plot!.id,
      price: price,
      bookingDate: _dateFmt.format(_bookingDate),
      notes: _notes.text.trim(),
      brokerId: _broker!.id,
      brokerCommission: comm,
    );
    if (!mounted) return;
    setState(() => _saving = false);

    if (!result.ok) {
      _toast(result.message);
      await _refreshPlots();
      return;
    }

    showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.panel,
        title: const Text('RealEstate Builders'),
        content: Text(result.message),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(ctx);
              _clearAfterSave();
              Navigator.pop(context, true);
            },
            child: const Text('OK'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(title: const Text('AllocatePlotForm')),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
                children: [
                  SoftPanel(
                    child: Column(
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
                          onChanged: (v) => setState(() => _customer = v),
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
                        DropdownButtonFormField<BlockItem>(
                          key: ValueKey('blk-${_society?.id}-${_block?.id}'),
                          initialValue: _block,
                          decoration: const InputDecoration(labelText: 'Block'),
                          dropdownColor: AppColors.panel,
                          items: _blocks
                              .map(
                                (b) => DropdownMenuItem(
                                  value: b,
                                  child: Text(b.name),
                                ),
                              )
                              .toList(),
                          onChanged: (v) async {
                            setState(() {
                              _block = v;
                              _plot = null;
                            });
                            await _refreshPlots();
                          },
                        ),
                        const SizedBox(height: 10),
                        DropdownButtonFormField<PlotTypeItem>(
                          key: ValueKey('pt-${_society?.id}-${_plotType?.id}'),
                          initialValue: _plotType,
                          decoration:
                              const InputDecoration(labelText: 'Plot Type'),
                          dropdownColor: AppColors.panel,
                          items: _types
                              .map(
                                (t) => DropdownMenuItem(
                                  value: t,
                                  child: Text(t.code),
                                ),
                              )
                              .toList(),
                          onChanged: (v) async {
                            setState(() {
                              _plotType = v;
                              _plot = null;
                            });
                            await _refreshPlots();
                          },
                        ),
                        const SizedBox(height: 10),
                        DropdownButtonFormField<StoredPlot>(
                          key: ValueKey(
                            'plot-${_society?.id}-${_block?.id}-${_plotType?.id}-${_plot?.id}-${_availablePlots.length}',
                          ),
                          initialValue: _plot,
                          decoration: const InputDecoration(
                            labelText: 'Plot NO',
                            helperText: 'Sirf Available plots',
                          ),
                          dropdownColor: AppColors.panel,
                          items: _availablePlots
                              .map(
                                (p) => DropdownMenuItem(
                                  value: p,
                                  child: Text(p.plotNo),
                                ),
                              )
                              .toList(),
                          onChanged: (v) => setState(() => _plot = v),
                        ),
                        const SizedBox(height: 10),
                        TextField(
                          controller: _price,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(labelText: 'Price'),
                        ),
                        const SizedBox(height: 10),
                        InkWell(
                          onTap: _pickDate,
                          borderRadius: BorderRadius.circular(12),
                          child: InputDecorator(
                            decoration: const InputDecoration(
                              labelText: 'Date',
                              suffixIcon: Icon(Icons.calendar_today_outlined),
                            ),
                            child: Text(
                              _dateFmt.format(_bookingDate),
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                          ),
                        ),
                        const SizedBox(height: 10),
                        TextField(
                          controller: _notes,
                          maxLines: 3,
                          decoration: const InputDecoration(labelText: 'Note'),
                        ),
                        const SizedBox(height: 10),
                        DropdownButtonFormField<BrokerItem>(
                          key: ValueKey(_broker?.id ?? 'broker'),
                          initialValue: _broker,
                          decoration: const InputDecoration(
                            labelText: 'Broker Name',
                          ),
                          dropdownColor: AppColors.panel,
                          items: _brokers
                              .map(
                                (b) => DropdownMenuItem(
                                  value: b,
                                  child: Text(b.name),
                                ),
                              )
                              .toList(),
                          onChanged: _onBroker,
                        ),
                        const SizedBox(height: 10),
                        TextField(
                          controller: _commission,
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            labelText: 'BrokerCommissionAmount',
                          ),
                        ),
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
                                        height: 18,
                                        width: 18,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                          color: Colors.white,
                                        ),
                                      )
                                    : const Text('Save'),
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
