import 'package:flutter/material.dart';

import '../data/dummy_data.dart';
import '../data/nav_items.dart';
import '../data/vendor_store.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';
import 'application_form_screen.dart';
import 'customer_form_screen.dart';
import 'society_form_screen.dart';
import 'block_form_screen.dart';
import 'plot_form_screen.dart';
import 'plot_inventory_screen.dart';
import 'allocate_screen.dart';
import 'ledger_screen.dart';
import 'receipt_screen.dart';
import 'home_screen.dart';
import 'entity_list_screen.dart';
import 'password_screen.dart';
import 'reports_hub_screen.dart';
import 'agreement_design_screen.dart';
import 'info_screen.dart';
import 'vendor_forms.dart';

class AppShell extends StatefulWidget {
  const AppShell({super.key});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  HubRoute _route = HubRoute.home;
  final _scaffoldKey = GlobalKey<ScaffoldState>();
  int _listTick = 0;

  String get _title {
    for (final item in [
      ...sidebarItems,
      ...fileMenuItems,
      ...vendorMenuItems,
    ]) {
      if (item.route == _route) return item.label;
    }
    return switch (_route) {
      HubRoute.applicationForm => 'Application Form',
      HubRoute.applicationFormBlank => 'Blank Form Overlay',
      _ => 'HubBuilders',
    };
  }

  void go(HubRoute route) {
    setState(() {
      _route = route;
      _listTick++;
    });
    if (_scaffoldKey.currentState?.isDrawerOpen ?? false) {
      Navigator.of(context).pop();
    }
  }

  void _refreshLists() => setState(() => _listTick++);

  Widget get _body {
    return switch (_route) {
      HubRoute.home => HomeScreen(onOpen: go),
      HubRoute.customer => EntityListScreen(
          key: ValueKey('cust-$_listTick'),
          title: 'Customer',
          subtitle: 'Name, FatherName, Mobile, CNIC, Note, Date',
          loader: MasterListStore.customers,
          onAdd: () => go(HubRoute.newCustomer),
          addLabel: 'New Customer',
          onOpenRow: (row) => _openCustomerForm(row: row, isEdit: true),
        ),
      HubRoute.society => EntityListScreen(
          key: ValueKey('soc-$_listTick'),
          title: 'Society',
          subtitle: 'Society, City, Date',
          loader: MasterListStore.societies,
          onAdd: () => _openSocietyForm(),
          addLabel: 'Add Society',
          onOpenRow: (row) => _openSocietyForm(row: row, isEdit: true),
        ),
      HubRoute.block => EntityListScreen(
          key: ValueKey('blk-$_listTick'),
          title: 'Block',
          subtitle: 'Society, Block',
          loader: MasterListStore.blocks,
          onAdd: () => _openBlockForm(),
          addLabel: 'Add Block',
          onOpenRow: (row) => _openBlockForm(row: row, isEdit: true),
        ),
      HubRoute.plot => EntityListScreen(
          key: ValueKey('pt-$_listTick'),
          title: 'Plot',
          subtitle: 'Society, Size',
          loader: MasterListStore.plotTypes,
          onAdd: () => _openPlotForm(),
          addLabel: 'Add Plot Type',
          onOpenRow: (row) => _openPlotForm(row: row, isEdit: true),
        ),
      HubRoute.plotInventory => const PlotInventoryScreen(),
      HubRoute.allocate => const AllocateScreen(),
      HubRoute.ledger => const LedgerScreen(),
      HubRoute.receipt => const ReceiptScreen(),
      HubRoute.expense => EntityListScreen(
          key: ValueKey('exp-$_listTick'),
          title: 'Expense',
          subtitle: 'Expense entries',
          loader: VendorStore.expenseRows,
          onAdd: () => _openExpenseForm(),
          addLabel: 'Add Expense',
          onOpenRow: (row) => _openExpenseForm(row: row),
        ),
      HubRoute.reports => ReportsHubScreen(onOpen: go),
      HubRoute.password => const PasswordScreen(),
      HubRoute.newCustomer => CustomerFormScreen(
          onBack: () {
            go(HubRoute.customer);
            _refreshLists();
          },
        ),
      HubRoute.agreementDesign => const AgreementDesignScreen(),
      HubRoute.broker => EntityListScreen(
          key: ValueKey('brk-$_listTick'),
          title: 'Broker Add',
          subtitle: 'File → Broker Add',
          loader: MasterListStore.brokers,
          onAdd: () => _openBrokerForm(),
          addLabel: 'Add Broker',
          onOpenRow: (row) => _openBrokerForm(row: row),
        ),
      HubRoute.login => const InfoScreen(
          title: 'Login Form',
          body:
              'Default login: admin / 1234. Restart app to sign in again.',
        ),
      HubRoute.appInfo => const InfoScreen(
          title: 'Application Info',
          body:
              'HubBuilders\nVersion 1.0.0\n\nLocal data stored on this device.',
        ),
      HubRoute.owner => EntityListScreen(
          key: ValueKey('own-$_listTick'),
          title: 'Owner Form Add',
          subtitle: 'Owner records',
          loader: VendorStore.ownerRows,
          onAdd: () => _openPersonForm(isOwner: true),
          addLabel: 'Add Owner',
          onOpenRow: (row) => _openPersonForm(isOwner: true, row: row),
        ),
      HubRoute.partner => EntityListScreen(
          key: ValueKey('par-$_listTick'),
          title: 'Partner Form Add',
          subtitle: 'Partner records',
          loader: VendorStore.partnerRows,
          onAdd: () => _openPersonForm(isOwner: false),
          addLabel: 'Add Partner',
          onOpenRow: (row) => _openPersonForm(isOwner: false, row: row),
        ),
      HubRoute.deal => EntityListScreen(
          key: ValueKey('deal-$_listTick'),
          title: 'Deal Form',
          subtitle: 'Deal records',
          loader: VendorStore.dealRows,
          onAdd: () => _openDealForm(),
          addLabel: 'Add Deal',
        ),
      HubRoute.credit => EntityListScreen(
          key: ValueKey('crd-$_listTick'),
          title: 'Credit Form',
          subtitle: 'Credit records',
          loader: VendorStore.creditRows,
          onAdd: () => _openCreditForm(),
          addLabel: 'Add Credit',
        ),
      HubRoute.applicationForm => ApplicationFormScreen(
          blankOverlay: false,
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.applicationFormBlank => ApplicationFormScreen(
          blankOverlay: true,
          onBack: () => go(HubRoute.reports),
        ),
    };
  }

  Future<void> _openPlotForm({
    ListRow? row,
    bool isEdit = false,
  }) async {
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => PlotFormScreen(
          isEdit: isEdit,
          id: row?.id,
          plotSize: row?.title,
          societyName: row?.city,
        ),
      ),
    );
    _refreshLists();
  }

  Future<void> _openBlockForm({
    ListRow? row,
    bool isEdit = false,
  }) async {
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => BlockFormScreen(
          isEdit: isEdit,
          id: row?.id,
          blockName: row?.title,
          societyName: row?.city,
        ),
      ),
    );
    _refreshLists();
  }

  Future<void> _openSocietyForm({
    ListRow? row,
    bool isEdit = false,
  }) async {
    DateTime? date;
    if (row?.date != null) {
      date = DateTime.tryParse(row!.date!);
    }
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => SocietyFormScreen(
          isEdit: isEdit,
          id: row?.id,
          societyName: row?.title,
          city: row?.city,
          date: date,
        ),
      ),
    );
    _refreshLists();
  }

  Future<void> _openCustomerForm({
    ListRow? row,
    bool isEdit = false,
  }) async {
    DateTime? date;
    if (row?.date != null) {
      date = DateTime.tryParse(row!.date!);
    }
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => CustomerFormScreen(
          isEdit: isEdit,
          id: row?.id,
          name: row?.title,
          fatherName: row?.fatherName,
          mobile: row?.mobile,
          cnic: row?.cnic,
          note: row?.note,
          date: date,
        ),
      ),
    );
    _refreshLists();
  }

  Future<void> _openExpenseForm({ListRow? row}) async {
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => ExpenseFormScreen(
          id: row?.id,
          initial: row == null
              ? null
              : {
                  'name': row.title,
                  'category': row.subtitle.replaceFirst('Category: ', ''),
                  'amount': row.meta.replaceFirst('Rs ', ''),
                  'note': row.note ?? '',
                  'date': row.date ?? '',
                },
        ),
      ),
    );
    _refreshLists();
  }

  Future<void> _openBrokerForm({ListRow? row}) async {
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => BrokerFormScreen(
          id: row?.id,
          initial: row == null
              ? null
              : {
                  'name': row.title,
                  'phone': row.mobile ?? '',
                  'comm': row.meta.replaceFirst('Comm ', ''),
                  'active': row.tag == 'Inactive' ? '0' : '1',
                },
        ),
      ),
    );
    _refreshLists();
  }

  Future<void> _openPersonForm({
    required bool isOwner,
    ListRow? row,
  }) async {
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => PersonFormScreen(
          isOwner: isOwner,
          id: row?.id,
          initial: row == null
              ? null
              : {
                  'name': row.title,
                  'phone': row.mobile ?? '',
                  'note': row.note ?? '',
                },
        ),
      ),
    );
    _refreshLists();
  }

  Future<void> _openDealForm() async {
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => const DealFormScreen()),
    );
    _refreshLists();
  }

  Future<void> _openCreditForm() async {
    await Navigator.of(context).push(
      MaterialPageRoute(builder: (_) => const CreditFormScreen()),
    );
    _refreshLists();
  }

  @override
  Widget build(BuildContext context) {
    return AtmosphereBackground(
      child: Scaffold(
        key: _scaffoldKey,
        backgroundColor: Colors.transparent,
        appBar: AppBar(
          title: Text(_title),
          actions: [
            PopupMenuButton<String>(
              tooltip: 'File',
              icon: const Icon(Icons.folder_open_outlined),
              onSelected: (value) {
                final match = fileMenuItems.where((e) => e.label == value);
                if (match.isNotEmpty) go(match.first.route);
              },
              itemBuilder: (_) => fileMenuItems
                  .map(
                    (e) => PopupMenuItem(value: e.label, child: Text(e.label)),
                  )
                  .toList(),
            ),
            PopupMenuButton<String>(
              tooltip: 'Vendor Installment',
              icon: const Icon(Icons.account_tree_outlined),
              onSelected: (value) {
                final match = vendorMenuItems.where((e) => e.label == value);
                if (match.isNotEmpty) go(match.first.route);
              },
              itemBuilder: (_) => vendorMenuItems
                  .map(
                    (e) => PopupMenuItem(value: e.label, child: Text(e.label)),
                  )
                  .toList(),
            ),
            const SizedBox(width: 4),
          ],
        ),
        drawer: _SideDrawer(current: _route, onSelect: go),
        body: AnimatedSwitcher(
          duration: const Duration(milliseconds: 280),
          switchInCurve: Curves.easeOutCubic,
          switchOutCurve: Curves.easeInCubic,
          child: KeyedSubtree(
            key: ValueKey('$_route-$_listTick'),
            child: _body,
          ),
        ),
      ),
    );
  }
}

class _SideDrawer extends StatelessWidget {
  const _SideDrawer({required this.current, required this.onSelect});

  final HubRoute current;
  final ValueChanged<HubRoute> onSelect;

  @override
  Widget build(BuildContext context) {
    return Drawer(
      backgroundColor: AppColors.inkSoft,
      child: SafeArea(
        child: Column(
          children: [
            const Padding(
              padding: EdgeInsets.fromLTRB(18, 16, 18, 10),
              child: BrandMark(),
            ),
            const Divider(height: 1),
            Expanded(
              child: ListView.builder(
                padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 10),
                itemCount: sidebarItems.length,
                itemBuilder: (context, i) {
                  final item = sidebarItems[i];
                  final selected = item.route == current;
                  return Padding(
                    padding: const EdgeInsets.only(bottom: 6),
                    child: Material(
                      color: selected
                          ? AppColors.teal.withValues(alpha: 0.22)
                          : Colors.transparent,
                      borderRadius: BorderRadius.circular(14),
                      child: ListTile(
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14),
                          side: BorderSide(
                            color: selected
                                ? AppColors.tealBright.withValues(alpha: 0.45)
                                : Colors.transparent,
                          ),
                        ),
                        leading: Icon(
                          item.icon,
                          color: selected ? AppColors.tealBright : AppColors.mist,
                        ),
                        title: Text(
                          item.label,
                          style: TextStyle(
                            color: selected ? AppColors.sand : AppColors.mist,
                            fontWeight:
                                selected ? FontWeight.w700 : FontWeight.w500,
                          ),
                        ),
                        onTap: () => onSelect(item.route),
                      ),
                    ),
                  );
                },
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(14),
              child: Text(
                'HubBuilders · Local app',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      fontSize: 11,
                    ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
