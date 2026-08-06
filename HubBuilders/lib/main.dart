import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';

import 'db/app_database.dart';
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
  // Open empty SQLite DB (creates schema + default admin user only).
  await AppDatabase.instance.database;
  runApp(const HubBuildersApp());
}

class HubBuildersApp extends StatelessWidget {
  const HubBuildersApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'HubBuilders',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.dark(),
      home: const LoginScreen(),
    );
  }
}
