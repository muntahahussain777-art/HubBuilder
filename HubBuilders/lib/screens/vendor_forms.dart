import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../data/allocate_store.dart';
import '../data/plot_inventory_store.dart';
import '../data/vendor_store.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Expense form — SQLite Expensetbl.
class ExpenseFormScreen extends StatefulWidget {
  const ExpenseFormScreen({super.key, this.id, this.initial});

  final int? id;
  final Map<String, String>? initial;

  @override
  State<ExpenseFormScreen> createState() => _ExpenseFormScreenState();
}

class _ExpenseFormScreenState extends State<ExpenseFormScreen> {
  late final TextEditingController _name;
  late final TextEditingController _amount;
  late final TextEditingController _category;
  late final TextEditingController _note;
  late DateTime _date;
  final _fmt = DateFormat('yyyy-MM-dd');
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final i = widget.initial ?? {};
    _name = TextEditingController(text: i['name'] ?? '');
    _amount = TextEditingController(text: i['amount'] ?? '');
    _category = TextEditingController(text: i['category'] ?? '');
    _note = TextEditingController(text: i['note'] ?? '');
    _date = DateTime.tryParse(i['date'] ?? '') ?? DateTime.now();
  }

  @override
  void dispose() {
    _name.dispose();
    _amount.dispose();
    _category.dispose();
    _note.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final name = _name.text.trim();
    final amount = double.tryParse(_amount.text.trim()) ?? 0;
    if (name.isEmpty || amount <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Name aur Amount (>0) required.')),
      );
      return;
    }
    setState(() => _saving = true);
    try {
      if (widget.id != null) {
        await VendorStore.updateExpense(
          id: widget.id!,
          name: name,
          amount: amount,
          category: _category.text,
          note: _note.text,
          date: _fmt.format(_date),
        );
      } else {
        await VendorStore.insertExpense(
          name: name,
          amount: amount,
          category: _category.text,
          note: _note.text,
          date: _fmt.format(_date),
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
        appBar: AppBar(title: Text(widget.id == null ? 'Add Expense' : 'Edit Expense')),
        body: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            SoftPanel(
              child: Column(
                children: [
                  TextField(
                    controller: _name,
                    decoration: const InputDecoration(labelText: 'Name'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _category,
                    decoration: const InputDecoration(labelText: 'Category'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _amount,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Amount'),
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

/// Broker form — SQLite brokers.
class BrokerFormScreen extends StatefulWidget {
  const BrokerFormScreen({super.key, this.id, this.initial});

  final int? id;
  final Map<String, String>? initial;

  @override
  State<BrokerFormScreen> createState() => _BrokerFormScreenState();
}

class _BrokerFormScreenState extends State<BrokerFormScreen> {
  late final TextEditingController _name;
  late final TextEditingController _phone;
  late final TextEditingController _comm;
  bool _active = true;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final i = widget.initial ?? {};
    _name = TextEditingController(text: i['name'] ?? '');
    _phone = TextEditingController(text: i['phone'] ?? '');
    _comm = TextEditingController(text: i['comm'] ?? '0');
    _active = (i['active'] ?? '1') != '0';
  }

  @override
  void dispose() {
    _name.dispose();
    _phone.dispose();
    _comm.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_name.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Broker name required.')),
      );
      return;
    }
    setState(() => _saving = true);
    try {
      final comm = double.tryParse(_comm.text.trim()) ?? 0;
      if (widget.id != null) {
        await AllocateStore.updateBroker(
          id: widget.id!,
          name: _name.text,
          phone: _phone.text,
          commissionDefault: comm,
          isActive: _active,
        );
      } else {
        await AllocateStore.insertBroker(
          name: _name.text,
          phone: _phone.text,
          commissionDefault: comm,
          isActive: _active,
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
        appBar: AppBar(title: Text(widget.id == null ? 'Add Broker' : 'Edit Broker')),
        body: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            SoftPanel(
              child: Column(
                children: [
                  TextField(
                    controller: _name,
                    decoration: const InputDecoration(labelText: 'Broker Name'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _phone,
                    decoration: const InputDecoration(labelText: 'Phone'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _comm,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Commission Amount Default',
                    ),
                  ),
                  SwitchListTile(
                    title: const Text('Active'),
                    value: _active,
                    onChanged: (v) => setState(() => _active = v),
                  ),
                  const SizedBox(height: 12),
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

/// Owner / Partner person form — SQLite Person.
class PersonFormScreen extends StatefulWidget {
  const PersonFormScreen({
    super.key,
    required this.isOwner,
    this.id,
    this.initial,
  });

  final bool isOwner;
  final int? id;
  final Map<String, String>? initial;

  @override
  State<PersonFormScreen> createState() => _PersonFormScreenState();
}

class _PersonFormScreenState extends State<PersonFormScreen> {
  late final TextEditingController _name;
  late final TextEditingController _phone;
  late final TextEditingController _note;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final i = widget.initial ?? {};
    _name = TextEditingController(text: i['name'] ?? '');
    _phone = TextEditingController(text: i['phone'] ?? '');
    _note = TextEditingController(text: i['note'] ?? '');
  }

  @override
  void dispose() {
    _name.dispose();
    _phone.dispose();
    _note.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_name.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Name required.')),
      );
      return;
    }
    setState(() => _saving = true);
    try {
      if (widget.id != null) {
        await VendorStore.updatePerson(
          id: widget.id!,
          name: _name.text,
          phone: _phone.text,
          note: _note.text,
          isOwner: widget.isOwner,
          isPartner: !widget.isOwner,
        );
      } else {
        await VendorStore.insertPerson(
          name: _name.text,
          phone: _phone.text,
          note: _note.text,
          isOwner: widget.isOwner,
          isPartner: !widget.isOwner,
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
    final label = widget.isOwner ? 'Owner' : 'Partner';
    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: Text(widget.id == null ? 'Add $label' : 'Edit $label'),
        ),
        body: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            SoftPanel(
              child: Column(
                children: [
                  TextField(
                    controller: _name,
                    decoration: InputDecoration(labelText: '$label Name'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _phone,
                    decoration: const InputDecoration(labelText: 'Phone'),
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

/// Deal form — SQLite Deal.
class DealFormScreen extends StatefulWidget {
  const DealFormScreen({super.key});

  @override
  State<DealFormScreen> createState() => _DealFormScreenState();
}

class _DealFormScreenState extends State<DealFormScreen> {
  IdName? _society;
  IdName? _owner;
  final _amount = TextEditingController();
  final _note = TextEditingController();
  List<IdName> _societies = const [];
  List<IdName> _owners = const [];
  bool _loading = true;
  bool _saving = false;
  final _fmt = DateFormat('yyyy-MM-dd');
  late DateTime _date;

  @override
  void initState() {
    super.initState();
    _date = DateTime.now();
    _load();
  }

  Future<void> _load() async {
    final s = await PlotInventoryStore.societies();
    final o = await VendorStore.owners();
    if (!mounted) return;
    setState(() {
      _societies = s;
      _owners = o;
      _loading = false;
    });
  }

  @override
  void dispose() {
    _amount.dispose();
    _note.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final amount = double.tryParse(_amount.text.trim()) ?? 0;
    if (_society == null || _owner == null || amount <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Society, Owner aur Amount required.')),
      );
      return;
    }
    setState(() => _saving = true);
    try {
      await VendorStore.insertDeal(
        societyId: _society!.id,
        ownerId: _owner!.id,
        amount: amount,
        dealDate: _fmt.format(_date),
        note: _note.text,
      );
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
        appBar: AppBar(title: const Text('Add Deal')),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  SoftPanel(
                    child: Column(
                      children: [
                        DropdownButtonFormField<IdName>(
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
                          onChanged: (v) => setState(() => _society = v),
                        ),
                        const SizedBox(height: 12),
                        DropdownButtonFormField<IdName>(
                          initialValue: _owner,
                          decoration: const InputDecoration(labelText: 'Owner'),
                          dropdownColor: AppColors.panel,
                          items: _owners
                              .map(
                                (o) => DropdownMenuItem(
                                  value: o,
                                  child: Text(o.name),
                                ),
                              )
                              .toList(),
                          onChanged: (v) => setState(() => _owner = v),
                        ),
                        const SizedBox(height: 12),
                        TextField(
                          controller: _amount,
                          keyboardType: TextInputType.number,
                          decoration:
                              const InputDecoration(labelText: 'Amount'),
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

/// Credit form — SQLite Credit (resolves latest Deal).
class CreditFormScreen extends StatefulWidget {
  const CreditFormScreen({super.key});

  @override
  State<CreditFormScreen> createState() => _CreditFormScreenState();
}

class _CreditFormScreenState extends State<CreditFormScreen> {
  IdName? _society;
  IdName? _owner;
  IdName? _partner;
  final _amount = TextEditingController();
  final _note = TextEditingController();
  List<IdName> _societies = const [];
  List<IdName> _owners = const [];
  List<IdName> _partners = const [];
  bool _loading = true;
  bool _saving = false;
  final _fmt = DateFormat('yyyy-MM-dd');

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final s = await PlotInventoryStore.societies();
    final o = await VendorStore.owners();
    final p = await VendorStore.partners();
    if (!mounted) return;
    setState(() {
      _societies = s;
      _owners = o;
      _partners = p;
      _loading = false;
    });
  }

  @override
  void dispose() {
    _amount.dispose();
    _note.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final amount = double.tryParse(_amount.text.trim()) ?? 0;
    if (_society == null ||
        _owner == null ||
        _partner == null ||
        amount <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Society, Owner, Partner aur Amount required.'),
        ),
      );
      return;
    }
    setState(() => _saving = true);
    try {
      final dealId = await VendorStore.latestDealId(
        societyId: _society!.id,
        ownerId: _owner!.id,
      );
      if (dealId == null) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Is Society/Owner ke liye Deal nahi mili.'),
            ),
          );
        }
        return;
      }
      await VendorStore.insertCredit(
        dealId: dealId,
        partnerId: _partner!.id,
        amount: amount,
        payDate: _fmt.format(DateTime.now()),
        note: _note.text,
      );
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
        appBar: AppBar(title: const Text('Add Credit')),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  SoftPanel(
                    child: Column(
                      children: [
                        DropdownButtonFormField<IdName>(
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
                          onChanged: (v) => setState(() => _society = v),
                        ),
                        const SizedBox(height: 12),
                        DropdownButtonFormField<IdName>(
                          initialValue: _owner,
                          decoration: const InputDecoration(labelText: 'Owner'),
                          dropdownColor: AppColors.panel,
                          items: _owners
                              .map(
                                (o) => DropdownMenuItem(
                                  value: o,
                                  child: Text(o.name),
                                ),
                              )
                              .toList(),
                          onChanged: (v) => setState(() => _owner = v),
                        ),
                        const SizedBox(height: 12),
                        DropdownButtonFormField<IdName>(
                          initialValue: _partner,
                          decoration:
                              const InputDecoration(labelText: 'Partner'),
                          dropdownColor: AppColors.panel,
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
                        TextField(
                          controller: _amount,
                          keyboardType: TextInputType.number,
                          decoration:
                              const InputDecoration(labelText: 'Amount'),
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
