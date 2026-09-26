import 'package:flutter/material.dart';

enum HubRoute {
  home,
  customer,
  society,
  block,
  plot,
  plotInventory,
  allocate,
  ledger,
  receipt,
  expense,
  reports,
  password,
  // File menu
  newCustomer,
  agreementDesign,
  broker,
  login,
  appInfo,
  staffEntryLog,
  // Vendor installment
  owner,
  partner,
  partnerCash,
  deal,
  credit,
  ownerPartnerReport,
  // Nested reports / forms
  applicationForm,
  applicationFormBlank,
  vipPartnerCashBook,
  vipCustomerPlotsReceipts,
  rptAllocationList,
  rptCustomerTotals,
  rptReceiptsDetail,
  rptDues,
  rptAging,
  rptMonthly,
  rptDaily,
  rptInventory,
  rptProfitLoss,
  rptProfitCogs,
  rptSeparateCustomer,
  rptCustomerSenior,
  rptBrokerDetails,
  rptVipReceiptStatement,
  rptVipDuesTracker,
  rptVipSocietyProfit,
  rptVipBrokerDetails,
  rptVipDuesAging,
  rptTemporaryAllotment,
  rptTemporaryAllotmentBlank,
  rptLasbelaVillage,
  rptLasbelaMarket,
  rptBackupRestore,
  duesWhatsApp,
  rptReceiptsMinusCredit,
  rptAvailablePlots,
  rptPlotStatusOwner,
  rptStaffSalaryVip,
  rptPlotAllocVip,
  rptExpense,
  rptVipAllocateTotals,
  rptInstallmentDetection,
  rptMonthCollectionVip,
  rptVipBusinessAnalysis,
  rptRoughInstallmentEstimate,
}

class NavItem {
  const NavItem({
    required this.route,
    required this.label,
    required this.icon,
    this.section,
  });

  final HubRoute route;
  final String label;
  final IconData icon;
  final String? section;
}

/// Sidebar mirrors C# MainForm flowLayout buttons.
const List<NavItem> sidebarItems = [
  NavItem(route: HubRoute.home, label: 'Home', icon: Icons.home_outlined),
  NavItem(route: HubRoute.customer, label: 'Customer', icon: Icons.people_outline),
  NavItem(route: HubRoute.society, label: 'Society', icon: Icons.apartment_outlined),
  NavItem(route: HubRoute.block, label: 'Block', icon: Icons.grid_view_outlined),
  NavItem(route: HubRoute.plot, label: 'Plot', icon: Icons.map_outlined),
  NavItem(
    route: HubRoute.plotInventory,
    label: 'Plot Inventory',
    icon: Icons.inventory_2_outlined,
  ),
  NavItem(route: HubRoute.allocate, label: 'Allocate', icon: Icons.assignment_outlined),
  NavItem(route: HubRoute.ledger, label: 'Ledger', icon: Icons.menu_book_outlined),
  NavItem(route: HubRoute.receipt, label: 'Receipt Form', icon: Icons.receipt_long_outlined),
  NavItem(route: HubRoute.expense, label: 'Expense', icon: Icons.payments_outlined),
  NavItem(route: HubRoute.reports, label: 'Reports', icon: Icons.assessment_outlined),
  NavItem(route: HubRoute.password, label: 'Settings', icon: Icons.settings_outlined),
];

const List<NavItem> fileMenuItems = [
  NavItem(route: HubRoute.newCustomer, label: 'New Customer Add', icon: Icons.person_add_alt),
  NavItem(
    route: HubRoute.agreementDesign,
    label: 'Agreement Design',
    icon: Icons.design_services_outlined,
  ),
  NavItem(route: HubRoute.broker, label: 'Broker Add', icon: Icons.handshake_outlined),
  NavItem(route: HubRoute.login, label: 'Login Form', icon: Icons.login),
  NavItem(route: HubRoute.appInfo, label: 'Application Info', icon: Icons.info_outline),
  NavItem(
    route: HubRoute.staffEntryLog,
    label: 'Staff Entry Log',
    icon: Icons.history,
  ),
];

const List<NavItem> vendorMenuItems = [
  NavItem(route: HubRoute.owner, label: 'Owner Form Add', icon: Icons.account_balance_outlined),
  NavItem(route: HubRoute.partner, label: 'Partner Form Add', icon: Icons.group_add_outlined),
  NavItem(route: HubRoute.partnerCash, label: 'Partner Cash Book', icon: Icons.account_balance_wallet_outlined),
  NavItem(route: HubRoute.deal, label: 'Deal Form', icon: Icons.handshake_outlined),
  NavItem(route: HubRoute.credit, label: 'Credit Form', icon: Icons.credit_score_outlined),
  NavItem(
    route: HubRoute.ownerPartnerReport,
    label: 'Owner Reports 1',
    icon: Icons.groups_outlined,
  ),
  NavItem(route: HubRoute.reports, label: 'Reports', icon: Icons.assessment_outlined),
];

class ReportAction {
  const ReportAction({
    required this.title,
    required this.subtitle,
    required this.icon,
    this.route,
  });

  final String title;
  final String subtitle;
  final IconData icon;
  final HubRoute? route;
}

const List<ReportAction> reportActions = [
  ReportAction(
    title: 'Application Form (درخواست فارم)',
    subtitle: 'Fill & preview application PDF UI',
    icon: Icons.description_outlined,
    route: HubRoute.applicationForm,
  ),
  ReportAction(
    title: 'Fill on Blank Form (Text Only)',
    subtitle: 'Overlay text seats for pre-printed stock',
    icon: Icons.layers_outlined,
    route: HubRoute.applicationFormBlank,
  ),
  ReportAction(
    title: 'Temporary Allotment Letter',
    subtitle: 'Allocation list for allotment letter',
    icon: Icons.mail_outline,
    route: HubRoute.rptTemporaryAllotment,
  ),
  ReportAction(
    title: 'Temp. Allotment Blank',
    subtitle: 'Blank allotment letter data',
    icon: Icons.layers_clear_outlined,
    route: HubRoute.rptTemporaryAllotmentBlank,
  ),
  ReportAction(
    title: 'Lasbela Village Receipt',
    subtitle: 'Lasbela village receipt data',
    icon: Icons.receipt_outlined,
    route: HubRoute.rptLasbelaVillage,
  ),
  ReportAction(
    title: 'Lasbela Market Bhawani',
    subtitle: 'Lasbela market receipt data',
    icon: Icons.storefront_outlined,
    route: HubRoute.rptLasbelaMarket,
  ),
  ReportAction(
    title: 'Customer Plot Booking (Allocation List)',
    subtitle: 'Active allocations list',
    icon: Icons.list_alt_outlined,
    route: HubRoute.rptAllocationList,
  ),
  ReportAction(
    title: 'Customer Totals Roll-Up',
    subtitle: 'Billed / paid / due per customer',
    icon: Icons.pie_chart_outline,
    route: HubRoute.rptCustomerTotals,
  ),
  ReportAction(
    title: 'Receipts Detail',
    subtitle: 'All receipts with customer/plot',
    icon: Icons.receipt_outlined,
    route: HubRoute.rptReceiptsDetail,
  ),
  ReportAction(
    title: 'Dues',
    subtitle: 'Outstanding dues per plot',
    icon: Icons.pending_actions_outlined,
    route: HubRoute.rptDues,
  ),
  ReportAction(
    title: 'Aging Reports',
    subtitle: 'Aging buckets preview',
    icon: Icons.timelapse_outlined,
    route: HubRoute.rptAging,
  ),
  ReportAction(
    title: 'MonthlyWise Report',
    subtitle: 'Month roll-up of receipts',
    icon: Icons.calendar_month_outlined,
    route: HubRoute.rptMonthly,
  ),
  ReportAction(
    title: 'Dailywise Report',
    subtitle: 'Day activity by customer',
    icon: Icons.today_outlined,
    route: HubRoute.rptDaily,
  ),
  ReportAction(
    title: 'Inventory Status',
    subtitle: 'Available / Hold / Sold',
    icon: Icons.warehouse_outlined,
    route: HubRoute.rptInventory,
  ),
  ReportAction(
    title: 'Profit & Lost',
    subtitle: 'Cash P&L',
    icon: Icons.trending_up,
    route: HubRoute.rptProfitLoss,
  ),
  ReportAction(
    title: 'Profit – Cash Basis COGS',
    subtitle: 'Cash basis COGS P&L',
    icon: Icons.account_balance_wallet_outlined,
    route: HubRoute.rptProfitCogs,
  ),
  ReportAction(
    title: 'Separate Customer Report',
    subtitle: 'Per-customer receipts',
    icon: Icons.person_search_outlined,
    route: HubRoute.rptSeparateCustomer,
  ),
  ReportAction(
    title: 'Customer DIssnior',
    subtitle: 'Customer senior / complete',
    icon: Icons.elderly_outlined,
    route: HubRoute.rptCustomerSenior,
  ),
  ReportAction(
    title: 'Broker Details',
    subtitle: 'Broker commission preview',
    icon: Icons.badge_outlined,
    route: HubRoute.rptBrokerDetails,
  ),
  ReportAction(
    title: 'Customer Receipt Statement (VIP)',
    subtitle: 'VIP A4 customer receipt statement',
    icon: Icons.description_outlined,
    route: HubRoute.rptVipReceiptStatement,
  ),
  ReportAction(
    title: 'Complete Dues Tracker (VIP)',
    subtitle: 'VIP dues tracker',
    icon: Icons.pending_actions_outlined,
    route: HubRoute.rptVipDuesTracker,
  ),
  ReportAction(
    title: 'Society Profit Closing (VIP)',
    subtitle: 'VIP society receipts closing',
    icon: Icons.trending_up,
    route: HubRoute.rptVipSocietyProfit,
  ),
  ReportAction(
    title: 'Broker Details (VIP)',
    subtitle: 'VIP broker commission details',
    icon: Icons.badge_outlined,
    route: HubRoute.rptVipBrokerDetails,
  ),
  ReportAction(
    title: 'Dues Aging (VIP)',
    subtitle: 'VIP aging buckets',
    icon: Icons.timelapse_outlined,
    route: HubRoute.rptVipDuesAging,
  ),
  ReportAction(
    title: 'Partner Cash Book (VIP)',
    subtitle: 'Partner In/Out cash book with filters',
    icon: Icons.account_balance_wallet_outlined,
    route: HubRoute.vipPartnerCashBook,
  ),
  ReportAction(
    title: 'Customer Plots Receipts (VIP)',
    subtitle: 'All plots receipts + allocate prices − remaining',
    icon: Icons.receipt_long_outlined,
    route: HubRoute.vipCustomerPlotsReceipts,
  ),
  ReportAction(
    title: 'Receipts − Partner Credit',
    subtitle: 'Receipt totals minus Credit form',
    icon: Icons.account_balance_outlined,
    route: HubRoute.rptReceiptsMinusCredit,
  ),
  ReportAction(
    title: 'Available Plots Report',
    subtitle: 'Society → Block → Plot Type → Available list',
    icon: Icons.map_outlined,
    route: HubRoute.rptAvailablePlots,
  ),
  ReportAction(
    title: 'Plot Status Report (Owner)',
    subtitle: 'Available / Installment / Reserve / Sold — PDF + WhatsApp',
    icon: Icons.table_chart_outlined,
    route: HubRoute.rptPlotStatusOwner,
  ),
  ReportAction(
    title: 'Staff Salary Message (VIP)',
    subtitle: 'Owner ko salary request — Hub Builders WhatsApp',
    icon: Icons.chat_outlined,
    route: HubRoute.rptStaffSalaryVip,
  ),
  ReportAction(
    title: 'Plot Allocation Message (VIP)',
    subtitle: 'Plot allocate confirmation + receipts — WhatsApp',
    icon: Icons.celebration_outlined,
    route: HubRoute.rptPlotAllocVip,
  ),
  ReportAction(
    title: 'Allocate Totals (VIP)',
    subtitle: 'Total allocate · Total paid · Remaining (Baqi Lena)',
    icon: Icons.summarize_outlined,
    route: HubRoute.rptVipAllocateTotals,
  ),
  ReportAction(
    title: 'Installment Detection (VIP)',
    subtitle: 'From–To: expected vs collected monthly installments',
    icon: Icons.fact_check_outlined,
    route: HubRoute.rptInstallmentDetection,
  ),
  ReportAction(
    title: 'Month Collection (VIP)',
    subtitle: 'This month + previous wasool · who collected (partner)',
    icon: Icons.savings_outlined,
    route: HubRoute.rptMonthCollectionVip,
  ),
  ReportAction(
    title: 'Complete Business Analysis (VIP)',
    subtitle: 'Allocate · Paid · Baqi · Period wasool · Target · Kharcha · Net',
    icon: Icons.analytics_outlined,
    route: HubRoute.rptVipBusinessAnalysis,
  ),
  ReportAction(
    title: 'Rough Installment Estimate (VIP)',
    subtitle: 'Andaza monthly when not set · Expected · Le chuke · Baqi',
    icon: Icons.calculate_outlined,
    route: HubRoute.rptRoughInstallmentEstimate,
  ),
  ReportAction(
    title: 'Expense Report',
    subtitle: 'Expense Form list — date filter + total',
    icon: Icons.payments_outlined,
    route: HubRoute.rptExpense,
  ),
  ReportAction(
    title: 'Dues WhatsApp Reminder',
    subtitle: 'Only dues + missed months · WhatsApp message',
    icon: Icons.chat_outlined,
    route: HubRoute.duesWhatsApp,
  ),
  ReportAction(
    title: 'Backup And Restore Form',
    subtitle: 'Local DB path for backup/restore',
    icon: Icons.backup_outlined,
    route: HubRoute.rptBackupRestore,
  ),
];
