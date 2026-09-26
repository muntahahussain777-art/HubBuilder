import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';

import 'db/app_database.dart';
import 'data/hub_sync.dart';
import 'data/auto_backup.dart';
import 'screens/login_screen.dart';
import 'theme/app_theme.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  GoogleFonts.config.allowRuntimeFetching = true;
  SystemChrome.setSystemUIOverlayStyle(
    const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ),
  );
  // Open local SQLite only — login internet / Supabase pe depend nahi.
  await AppDatabase.instance.database;
  try {
    await HubSync.reconcileOneActiveCustomerPlots();
  } catch (_) {}
  unawaited(AutoBackup.start());
  runApp(const HubBuildersApp());
}

class HubBuildersApp extends StatefulWidget {
  const HubBuildersApp({super.key});

  @override
  State<HubBuildersApp> createState() => _HubBuildersAppState();
}

class _HubBuildersAppState extends State<HubBuildersApp> {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'HubBuilders',
      debugShowCheckedModeBanner: false,
      navigatorKey: HubSync.navKey,
      scaffoldMessengerKey: HubSync.snackKey,
      theme: AppTheme.dark(),
      home: const LoginScreen(),
    );
  }
}
