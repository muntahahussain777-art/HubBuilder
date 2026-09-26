import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/hub_sync.dart';
import '../data/partner_cash_store.dart';
import '../data/plot_inventory_store.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Partner Cash Book — same module as WinForms frmPartnerCashView / Add.
class PartnerCashScreen extends StatefulWidget {
  const PartnerCashScreen({super.key});

  @override
  State<PartnerCashScreen> createState() => _PartnerCashScreenState();
}

class _PartnerCashScreenState extends State<PartnerCashScreen> {
  final _search = TextEditingController();
  List<PartnerCashRow> _rows = const [];
  double _totalIn = 0;
  double _totalOut = 0;
  double _balance = 0;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    HubSync.revision.addListener(_onSync);
    _reload();
  }

  @override
  void dispose() {
    HubSync.revision.removeListener(_onSync);
    _search.dispose();
    super.dispose();
  }

  void _onSync() {
    if (mounted) _reload();
  }

  Future<void> _reload() async {
    setState(() => _loading = true);
    final q = _search.text;
    final rows = await PartnerCashStore.list(query: q);
    final t = await PartnerCashStore.totals(query: q);
    if (!mounted) return;
    setState(() {
      _rows = rows;
      _totalIn = t.totalIn;
      _totalOut = t.totalOut;
      _balance = t.balance;
      _loading = false;
    });
  }

  Future<void> _openForm({PartnerCashRow? row}) async {
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => PartnerCashFormScreen(edit: row)),
    );
    if (ok == true) _reload();
  }

  Future<void> _delete(PartnerCashRow row) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Delete Confirmation'),
        content: const Text('Are you sure you want to delete?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('No')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Yes')),
        ],
      ),
    );
    if (ok != true) return;
    await PartnerCashStore.delete(row.id);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Delete Successfully')),
    );
    _reload();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        SoftPanel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Partner Cash Book',
                style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      color: AppColors.sand,
                    ),
              ),
              const SizedBox(height: 6),
              Text(
                'Total In: ${_totalIn.toStringAsFixed(2)}',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      color: AppColors.success,
                    ),
              ),
              Text(
                'Total Out: ${_totalOut.toStringAsFixed(2)}',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      color: AppColors.danger,
                    ),
              ),
              Text(
                'Balance (In − Out): ${_balance.toStringAsFixed(2)}',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      color: AppColors.tealBright,
                    ),
              ),
              const SizedBox(height: 14),
              TextField(
                controller: _search,
                decoration: const InputDecoration(
                  labelText: 'Search partner / customer / plot…',
                  prefixIcon: Icon(Icons.search),
                ),
                onChanged: (_) => _reload(),
              ),
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: () => _openForm(),
                  icon: const Icon(Icons.add),
                  label: const Text('Add Partner Cash'),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        if (_loading)
          const Padding(
            padding: EdgeInsets.all(24),
            child: Center(child: CircularProgressIndicator()),
          )
        else if (_rows.isEmpty)
          SoftPanel(
            child: Text(
              'No partner cash entries yet.',
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          )
        else
          ..._rows.map(
            (r) => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: SoftPanel(
                onTap: () => _openForm(row: r),
                child: Row(
                  children: [
                    Container(
                      width: 44,
                      height: 44,
                      decoration: BoxDecoration(
                        color: (r.direction == 'In'
                                ? AppColors.success
                                : AppColors.danger)
                            .withValues(alpha: 0.18),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Icon(
                        r.direction == 'In'
                            ? Icons.arrow_downward
                            : Icons.arrow_upward,
                        color: r.direction == 'In'
                            ? AppColors.success
                            : AppColors.danger,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            r.partnerName,
                            style: Theme.of(context).textTheme.titleMedium,
                          ),
                          const SizedBox(height: 3),
                          Text(
                            '${r.txnDate} · ${r.direction} · ${r.category} · ${r.mode}',
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                          if (r.staff.trim().isNotEmpty) ...[
                            const SizedBox(height: 2),
                            Text(
                              'Staff: ${r.staff}',
                              style: Theme.of(context).textTheme.labelSmall
                                  ?.copyWith(color: AppColors.mist),
                            ),
                          ],
                          if (r.customerName.isNotEmpty || r.plotNo.isNotEmpty) ...[
                            const SizedBox(height: 2),
                            Text(
                              [
                                if (r.customerName.isNotEmpty) r.customerName,
                                if (r.plotNo.isNotEmpty) 'Plot ${r.plotNo}',
                              ].join(' · '),
                              style: Theme.of(context).textTheme.bodySmall
                                  ?.copyWith(color: AppColors.mist),
                            ),
                          ],
                          if (r.note.isNotEmpty) ...[
                            const SizedBox(height: 2),
                            Text(
                              r.note,
                              style: Theme.of(context).textTheme.bodySmall
                                  ?.copyWith(color: AppColors.mist),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ],
                        ],
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          r.amount.toStringAsFixed(2),
                          style: Theme.of(context).textTheme.titleMedium
                              ?.copyWith(color: AppColors.gold),
                        ),
                        IconButton(
                          tooltip: 'Delete',
                          onPressed: () => _delete(r),
                          icon: const Icon(Icons.delete_outline,
                              color: AppColors.danger),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
      ],
    );
  }
}

class PartnerCashFormScreen extends StatefulWidget {
  const PartnerCashFormScreen({super.key, this.edit});

  final PartnerCashRow? edit;

  @override
  State<PartnerCashFormScreen> createState() => _PartnerCashFormScreenState();
}

class _PartnerCashFormScreenState extends State<PartnerCashFormScreen> {
  final _fmt = DateFormat('yyyy-MM-dd');
  final _amount = TextEditingController();
  final _note = TextEditingController();

  List<IdName> _partners = const [];
  List<String> _categories = const [];
  IdName? _partner;
  String _direction = 'Out';
  String? _category;
  String _mode = 'Cash';
  late DateTime _date;
  bool _loading = true;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final e = widget.edit;
    _date = DateTime.tryParse(e?.txnDate ?? '') ?? DateTime.now();
    _direction = e?.direction == 'In' ? 'In' : 'Out';
    _mode = (e?.mode == 'Bank') ? 'Bank' : 'Cash';
    _amount.text = e == null ? '' : e.amount.toStringAsFixed(2);
    _note.text = e?.note ?? '';
    _bootstrap();
  }

  @override
  void dispose() {
    _amount.dispose();
    _note.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    final partners = await PartnerCashStore.partners();
    final cats = await PartnerCashStore.categories(direction: _direction);
    if (!mounted) return;
    setState(() {
      _partners = partners;
      _categories = cats;
      if (widget.edit != null) {
        IdName? match;
        for (final p in partners) {
          if (p.id == widget.edit!.partnerId) {
            match = p;
            break;
          }
        }
        _partner = match ?? (partners.isNotEmpty ? partners.first : null);
        _category = widget.edit!.category.isEmpty
            ? (cats.isNotEmpty ? cats.first : null)
            : widget.edit!.category;
        if (_category != null && !cats.contains(_category)) {
          _categories = [...cats, _category!];
        }
      } else if (partners.isNotEmpty) {
        _partner = partners.first;
      }
      if (_category == null && _categories.isNotEmpty) {
        _category = _categories.first;
      }
      _loading = false;
    });
  }

  Future<void> _onDirection(String? v) async {
    if (v == null) return;
    setState(() => _direction = v);
    final cats = await PartnerCashStore.categories(direction: v);
    if (!mounted) return;
    setState(() {
      _categories = cats;
      if (_category == null || !cats.contains(_category)) {
        _category = cats.isNotEmpty ? cats.first : null;
      }
    });
  }

  Future<void> _addCategory() async {
    final ctrl = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('New Category'),
        content: TextField(
          controller: ctrl,
          decoration: const InputDecoration(labelText: 'Category name'),
          autofocus: true,
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Add')),
        ],
      ),
    );
    final name = ctrl.text.trim();
    ctrl.dispose();
    if (ok != true || name.isEmpty) return;
    await PartnerCashStore.addCategory(name, _direction);
    final cats = await PartnerCashStore.categories(direction: _direction);
    if (!mounted) return;
    setState(() {
      _categories = cats.contains(name) ? cats : [...cats, name];
      _category = name;
    });
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _save() async {
    if (_partner == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Partner select karein.')),
      );
      return;
    }
    final amt = double.tryParse(_amount.text.replaceAll(',', '').trim()) ?? 0;
    if (amt <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Amount > 0 likhein.')),
      );
      return;
    }
    if (_category == null || _category!.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Category select karein.')),
      );
      return;
    }

    setState(() => _saving = true);
    try {
      if (widget.edit != null) {
        await PartnerCashStore.update(
          id: widget.edit!.id,
          partnerId: _partner!.id,
          txnDate: _fmt.format(_date),
          direction: _direction,
          amount: amt,
          category: _category!,
          mode: _mode,
          note: _note.text,
        );
      } else {
        await PartnerCashStore.insert(
          partnerId: _partner!.id,
          txnDate: _fmt.format(_date),
          direction: _direction,
          amount: amt,
          category: _category!,
          mode: _mode,
          note: _note.text,
        );
      }
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Save failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: Text(widget.edit == null ? 'Add Partner Cash' : 'Edit Partner Cash'),
        ),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  SoftPanel(
                    child: Column(
                      children: [
                        DropdownButtonFormField<IdName>(
                          value: _partner,
                          decoration: const InputDecoration(labelText: 'Partner *'),
                          items: _partners
                              .map(
                                (p) => DropdownMenuItem(
                                  value: p,
                                  child: Text(p.name),
                                ),
                              )
                              .toList(),
                          onChanged: (v) => setState(() => _partner = v),
                        ),
                        const SizedBox(height: 12),
                        ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: const Text('Date'),
                          subtitle: Text(_fmt.format(_date)),
                          trailing: const Icon(Icons.calendar_today),
                          onTap: _pickDate,
                        ),
                        const SizedBox(height: 8),
                        DropdownButtonFormField<String>(
                          value: _direction,
                          decoration: const InputDecoration(
                            labelText: 'Direction *  (In = paise aaye | Out = karcha)',
                          ),
                          items: const [
                            DropdownMenuItem(value: 'In', child: Text('In')),
                            DropdownMenuItem(value: 'Out', child: Text('Out')),
                          ],
                          onChanged: _onDirection,
                        ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(
                              child: DropdownButtonFormField<String>(
                                value: _category,
                                decoration:
                                    const InputDecoration(labelText: 'Category *'),
                                items: _categories
                                    .map(
                                      (c) => DropdownMenuItem(
                                        value: c,
                                        child: Text(c),
                                      ),
                                    )
                                    .toList(),
                                onChanged: (v) => setState(() => _category = v),
                              ),
                            ),
                            const SizedBox(width: 8),
                            OutlinedButton(
                              onPressed: _addCategory,
                              child: const Text('New'),
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),
                        DropdownButtonFormField<String>(
                          value: _mode,
                          decoration: const InputDecoration(labelText: 'Mode'),
                          items: const [
                            DropdownMenuItem(value: 'Cash', child: Text('Cash')),
                            DropdownMenuItem(value: 'Bank', child: Text('Bank')),
                          ],
                          onChanged: (v) => setState(() => _mode = v ?? 'Cash'),
                        ),
                        const SizedBox(height: 12),
                        TextField(
                          controller: _amount,
                          keyboardType: const TextInputType.numberWithOptions(
                            decimal: true,
                          ),
                          decoration: const InputDecoration(labelText: 'Amount *'),
                        ),
                        const SizedBox(height: 12),
                        TextField(
                          controller: _note,
                          maxLines: 3,
                          decoration: const InputDecoration(labelText: 'Note'),
                        ),
                        const SizedBox(height: 18),
                        SizedBox(
                          width: double.infinity,
                          child: ElevatedButton(
                            onPressed: _saving ? null : _save,
                            child: const Text('Save'),
                          ),
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
