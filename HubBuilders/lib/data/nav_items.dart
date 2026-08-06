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
  // Vendor installment
  owner,
  partner,
  deal,
  credit,
  // Nested reports / forms
  applicationForm,
  applicationFormBlank,
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
];

const List<NavItem> vendorMenuItems = [
  NavItem(route: HubRoute.owner, label: 'Owner Form Add', icon: Icons.account_balance_outlined),
  NavItem(route: HubRoute.partner, label: 'Partner Form Add', icon: Icons.group_add_outlined),
  NavItem(route: HubRoute.deal, label: 'Deal Form', icon: Icons.handshake_outlined),
  NavItem(route: HubRoute.credit, label: 'Credit Form', icon: Icons.credit_score_outlined),
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
    title: 'Customer Plot Booking (Allocation List)',
    subtitle: 'Report shell — PDF later',
    icon: Icons.list_alt_outlined,
  ),
  ReportAction(
    title: 'Customer Totals Roll-Up',
    subtitle: 'Report shell — PDF later',
    icon: Icons.pie_chart_outline,
  ),
  ReportAction(
    title: 'Receipts Detail',
    subtitle: 'Report shell — PDF later',
    icon: Icons.receipt_outlined,
  ),
  ReportAction(
    title: 'Dues',
    subtitle: 'Outstanding dues preview',
    icon: Icons.pending_actions_outlined,
  ),
  ReportAction(
    title: 'Aging Reports',
    subtitle: 'Aging buckets preview',
    icon: Icons.timelapse_outlined,
  ),
  ReportAction(
    title: 'MonthlyWise Report',
    subtitle: 'Month roll-up preview',
    icon: Icons.calendar_month_outlined,
  ),
  ReportAction(
    title: 'Dailywise Report',
    subtitle: 'Day activity preview',
    icon: Icons.today_outlined,
  ),
  ReportAction(
    title: 'Inventory Status',
    subtitle: 'Plot stock snapshot',
    icon: Icons.warehouse_outlined,
  ),
  ReportAction(
    title: 'Profit & Lost',
    subtitle: 'P&L sketch',
    icon: Icons.trending_up,
  ),
  ReportAction(
    title: 'Profit – Cash Basis COGS',
    subtitle: 'Cash basis preview',
    icon: Icons.account_balance_wallet_outlined,
  ),
  ReportAction(
    title: 'Separate Customer Report',
    subtitle: 'Per-customer print UI',
    icon: Icons.person_search_outlined,
  ),
  ReportAction(
    title: 'Customer DIssnior',
    subtitle: 'Senior customer report',
    icon: Icons.elderly_outlined,
  ),
  ReportAction(
    title: 'Broker Details',
    subtitle: 'Broker commission preview',
    icon: Icons.badge_outlined,
  ),
  ReportAction(
    title: 'Backup And Restore Form',
    subtitle: 'Utility shell — PDF/backup later',
    icon: Icons.backup_outlined,
  ),
];
