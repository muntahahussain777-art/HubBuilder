import 'dart:async';

import 'package:flutter/material.dart';

import '../data/dummy_data.dart';
import '../data/hub_sync.dart';
import '../data/nav_items.dart';
import '../data/vendor_store.dart';
import '../data/staff_access.dart';
import '../data/staff_audit.dart';
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
import 'partner_cash_screen.dart';
import 'vip_partner_cashbook_screen.dart';
import 'vip_customer_plots_receipts_screen.dart';
import 'report_preview_screen.dart';
import 'staff_entry_log_screen.dart';
import 'backup_restore_screen.dart';
import 'letter_receipt_screens.dart';
import 'dues_whatsapp_screen.dart';
import 'plot_status_owner_screen.dart';
import 'staff_salary_vip_screen.dart';
import 'plot_alloc_vip_screen.dart';
import '../data/reports_store.dart';

class AppShell extends StatefulWidget {
  const AppShell({super.key});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> with WidgetsBindingObserver {
  HubRoute _route = HubRoute.home;
  final _scaffoldKey = GlobalKey<ScaffoldState>();
  int _listTick = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    HubSync.revision.addListener(_onSyncRevision);
  }

  @override
  void dispose() {
    HubSync.revision.removeListener(_onSyncRevision);
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  void _onSyncRevision() {
    if (!mounted) return;
    _refreshLists();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      // Resume: push local dirty pehle, phir pull / force-recover-missing (no wipe)
      HubSync.markDirty();
      unawaited(HubSync.syncNow());
    }
  }

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
      HubRoute.partnerCash => 'Partner Cash Book',
      HubRoute.vipPartnerCashBook => 'Partner Cash Book (VIP)',
      HubRoute.vipCustomerPlotsReceipts => 'Customer Plots Receipts (VIP)',
      HubRoute.duesWhatsApp => 'Dues WhatsApp Reminder',
      HubRoute.rptPlotStatusOwner => 'Plot Status Report (Owner)',
      HubRoute.rptStaffSalaryVip => 'Staff Salary Message (VIP)',
      HubRoute.rptPlotAllocVip => 'Plot Allocation Message (VIP)',
      HubRoute.rptVipAllocateTotals => 'Allocate Totals (VIP)',
      HubRoute.rptInstallmentDetection => 'Installment Detection (VIP)',
      HubRoute.rptMonthCollectionVip => 'Month Collection (VIP)',
      HubRoute.rptVipBusinessAnalysis => 'Complete Business Analysis (VIP)',
      HubRoute.rptRoughInstallmentEstimate => 'Rough Installment Estimate (VIP)',
      HubRoute.rptExpense => 'Expense Report',
      HubRoute.staffEntryLog => 'Staff Entry Log',
      HubRoute.ownerPartnerReport => 'Owner Reports 1',
      _ => 'HubBuilders',
    };
  }

  Widget _rpt(
    ReportKind kind,
    String title, {
    bool dates = false,
    bool asOf = false,
    bool society = false,
    bool customer = false,
    bool broker = false,
    bool customerPlot = false,
    bool year = false,
    bool mode = false,
  }) {
    return ReportPreviewScreen(
      kind: kind,
      title: title,
      onBack: () => go(HubRoute.reports),
      showDates: dates,
      showAsOf: asOf,
      showSociety: society,
      showCustomer: customer,
      showBroker: broker,
      showCustomerPlot: customerPlot,
      showYear: year,
      showMode: mode,
    );
  }

  void go(HubRoute route) async {
    final mod = StaffAccess.moduleForRoute(route.name);
    if (!await StaffAccess.can(mod, 'open')) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Aap ke role (${StaffAudit.role ?? ''}) ke liye yeh form allow nahi.',
          ),
        ),
      );
      return;
    }
    if (!mounted) return;
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
      HubRoute.partnerCash => PartnerCashScreen(key: ValueKey('pc-$_listTick')),
      HubRoute.deal => EntityListScreen(
          key: ValueKey('deal-$_listTick'),
          title: 'Deal Form',
          subtitle: 'Deal records',
          loader: VendorStore.dealRows,
          onAdd: () => _openDealForm(),
          addLabel: 'Add Deal',
          onOpenRow: (row) => _openDealForm(row: row),
        ),
      HubRoute.credit => EntityListScreen(
          key: ValueKey('crd-$_listTick'),
          title: 'Credit Form',
          subtitle: 'Credit records',
          loader: VendorStore.creditRows,
          onAdd: () => _openCreditForm(),
          addLabel: 'Add Credit',
          onOpenRow: (row) => _openCreditForm(row: row),
        ),
      HubRoute.applicationForm => ApplicationFormScreen(
          blankOverlay: false,
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.applicationFormBlank => ApplicationFormScreen(
          blankOverlay: true,
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.vipPartnerCashBook => VipPartnerCashBookScreen(
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.vipCustomerPlotsReceipts => VipCustomerPlotsReceiptsScreen(
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.staffEntryLog => const StaffEntryLogScreen(),
      HubRoute.ownerPartnerReport => _rpt(
          ReportKind.ownerPartner,
          'Owner Reports 1',
        ),
      HubRoute.rptAllocationList => _rpt(
          ReportKind.allocationList,
          'Customer Plot Booking',
          society: true,
        ),
      HubRoute.rptCustomerTotals => _rpt(
          ReportKind.customerTotals,
          'Customer Totals Roll-Up',
          customer: true,
          society: true,
        ),
      HubRoute.rptReceiptsDetail => _rpt(
          ReportKind.receiptsDetail,
          'Receipts Detail',
          society: true,
        ),
      HubRoute.rptDues =>
          _rpt(ReportKind.dues, 'Dues', society: true),
      HubRoute.rptAging => _rpt(
          ReportKind.aging,
          'Aging Reports',
          asOf: true,
          society: true,
        ),
      HubRoute.rptMonthly => _rpt(
          ReportKind.monthlyWise,
          'MonthlyWise Report',
          year: true,
          society: true,
        ),
      HubRoute.rptDaily => _rpt(
          ReportKind.dailyWise,
          'Dailywise Report',
          society: true,
        ),
      HubRoute.rptInventory => _rpt(
          ReportKind.inventoryStatus,
          'Inventory Status',
          society: true,
        ),
      HubRoute.rptProfitLoss => _rpt(
          ReportKind.profitLoss,
          'Profit & Lost',
          dates: true,
          mode: true,
          society: true,
        ),
      HubRoute.rptProfitCogs => _rpt(
          ReportKind.profitCogs,
          'Profit – Cash Basis COGS',
          dates: true,
          mode: true,
          society: true,
        ),
      HubRoute.rptSeparateCustomer => _rpt(
          ReportKind.separateCustomer,
          'Separate Customer Report',
          dates: true,
          society: true,
        ),
      HubRoute.rptCustomerSenior => _rpt(
          ReportKind.customerSenior,
          'Customer DIssnior',
          society: true,
        ),
      HubRoute.rptBrokerDetails => _rpt(
          ReportKind.brokerDetails,
          'Broker Details',
          dates: true,
          society: true,
          broker: true,
        ),
      HubRoute.rptVipReceiptStatement => _rpt(
          ReportKind.vipCustomerReceiptStatement,
          'Customer Receipt Statement (VIP)',
          dates: true,
          customerPlot: true,
          society: true,
        ),
      HubRoute.rptVipDuesTracker => _rpt(
          ReportKind.vipCompleteDuesTracker,
          'Complete Dues Tracker (VIP)',
          society: true,
          asOf: true,
        ),
      HubRoute.rptVipSocietyProfit => _rpt(
          ReportKind.vipSocietyProfitClosing,
          'Society Profit Closing (VIP)',
          dates: true,
          society: true,
        ),
      HubRoute.rptVipBrokerDetails => _rpt(
          ReportKind.vipBrokerDetails,
          'Broker Details (VIP)',
          dates: true,
          society: true,
          broker: true,
        ),
      HubRoute.rptVipDuesAging => _rpt(
          ReportKind.vipDuesAging,
          'Dues Aging (VIP)',
          asOf: true,
          society: true,
        ),
      HubRoute.rptTemporaryAllotment => TemporaryAllotmentScreen(
          blankOverlay: false,
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.rptTemporaryAllotmentBlank => TemporaryAllotmentScreen(
          blankOverlay: true,
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.rptLasbelaVillage => LasbelaReceiptScreen(
          marketBhawani: false,
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.rptLasbelaMarket => LasbelaReceiptScreen(
          marketBhawani: true,
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.rptBackupRestore => BackupRestoreScreen(
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.duesWhatsApp => DuesWhatsAppScreen(
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.rptReceiptsMinusCredit => _rpt(
          ReportKind.receiptsMinusPartnerCredit,
          'Receipts − Partner Credit',
          dates: true,
          society: true,
        ),
      HubRoute.rptAvailablePlots => _rpt(
          ReportKind.availablePlots,
          'Available Plots Report',
          society: true,
        ),
      HubRoute.rptPlotStatusOwner => PlotStatusOwnerScreen(
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.rptStaffSalaryVip => StaffSalaryVipScreen(
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.rptPlotAllocVip => PlotAllocVipScreen(
          onBack: () => go(HubRoute.reports),
        ),
      HubRoute.rptVipAllocateTotals => _rpt(
          ReportKind.vipAllocateTotals,
          'Allocate Totals (VIP)',
          society: true,
        ),
      HubRoute.rptInstallmentDetection => _rpt(
          ReportKind.installmentDetection,
          'Installment Detection (VIP)',
          dates: true,
          society: true,
        ),
      HubRoute.rptMonthCollectionVip => _rpt(
          ReportKind.monthCollectionVip,
          'Month Collection (VIP)',
          dates: true,
          society: true,
        ),
      HubRoute.rptVipBusinessAnalysis => _rpt(
          ReportKind.vipBusinessAnalysis,
          'Complete Business Analysis (VIP)',
          dates: true,
          society: true,
        ),
      HubRoute.rptRoughInstallmentEstimate => _rpt(
          ReportKind.roughInstallmentEstimate,
          'Rough Installment Estimate (VIP)',
          dates: true,
          society: true,
        ),
      HubRoute.rptExpense => _rpt(
          ReportKind.expenseReport,
          'Expense Report',
          dates: true,
          society: true,
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

  Future<void> _openDealForm({ListRow? row}) async {
    Map<String, String>? initial;
    if (row?.id != null) {
      final d = await VendorStore.dealById(row!.id!);
      if (d != null) {
        initial = {
          'society_id': '${d['society_id'] ?? ''}',
          'owner_id': '${d['owner_id'] ?? ''}',
          'amount': '${d['amount'] ?? ''}',
          'date': '${d['deal_date'] ?? ''}',
          'note': '${d['note'] ?? ''}',
        };
      }
    }
    if (!mounted) return;
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => DealFormScreen(id: row?.id, initial: initial),
      ),
    );
    _refreshLists();
  }

  Future<void> _openCreditForm({ListRow? row}) async {
    Map<String, String>? initial;
    if (row?.id != null) {
      final c = await VendorStore.creditById(row!.id!);
      if (c != null) {
        initial = {
          'society_id': '${c['society_id'] ?? ''}',
          'owner_id': '${c['owner_id'] ?? ''}',
          'partner_id': '${c['partner_id'] ?? ''}',
          'amount': '${c['amount'] ?? ''}',
          'date': '${c['pay_date'] ?? ''}',
          'note': '${c['note'] ?? ''}',
        };
      }
    }
    if (!mounted) return;
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => CreditFormScreen(id: row?.id, initial: initial),
      ),
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
                if (match.isEmpty) return;
                if (match.first.route == HubRoute.staffEntryLog) {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => const StaffEntryLogScreen(),
                    ),
                  );
                  return;
                }
                go(match.first.route);
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
