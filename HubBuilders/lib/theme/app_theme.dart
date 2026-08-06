import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'app_colors.dart';

abstract final class AppTheme {
  static ThemeData dark() {
    final base = ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      scaffoldBackgroundColor: AppColors.ink,
      colorScheme: const ColorScheme.dark(
        primary: AppColors.teal,
        secondary: AppColors.gold,
        surface: AppColors.panel,
        error: AppColors.danger,
        onPrimary: Colors.white,
        onSecondary: AppColors.ink,
        onSurface: AppColors.sand,
      ),
    );

    final display = GoogleFonts.fraunces(color: AppColors.sand);
    final body = GoogleFonts.manrope(color: AppColors.sand);

    return base.copyWith(
      textTheme: TextTheme(
        displayLarge: display.copyWith(fontSize: 42, fontWeight: FontWeight.w600),
        displayMedium: display.copyWith(fontSize: 32, fontWeight: FontWeight.w600),
        headlineLarge: display.copyWith(fontSize: 28, fontWeight: FontWeight.w600),
        headlineMedium: display.copyWith(fontSize: 22, fontWeight: FontWeight.w600),
        titleLarge: body.copyWith(fontSize: 18, fontWeight: FontWeight.w700),
        titleMedium: body.copyWith(fontSize: 16, fontWeight: FontWeight.w600),
        bodyLarge: body.copyWith(fontSize: 15, height: 1.4),
        bodyMedium: body.copyWith(fontSize: 13.5, height: 1.4, color: AppColors.mist),
        labelLarge: body.copyWith(fontSize: 13, fontWeight: FontWeight.w700, letterSpacing: 0.4),
      ),
      appBarTheme: AppBarTheme(
        backgroundColor: Colors.transparent,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: body.copyWith(
          fontSize: 18,
          fontWeight: FontWeight.w700,
          color: AppColors.sand,
        ),
        iconTheme: const IconThemeData(color: AppColors.sand),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.inkSoft,
        hintStyle: body.copyWith(color: AppColors.mist.withValues(alpha: 0.7)),
        labelStyle: body.copyWith(color: AppColors.mist),
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.line),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.line),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.tealBright, width: 1.4),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.teal,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          textStyle: body.copyWith(fontWeight: FontWeight.w700, fontSize: 14),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.sand,
          side: const BorderSide(color: AppColors.line),
          padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      ),
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: AppColors.teal,
        foregroundColor: Colors.white,
      ),
      dividerTheme: const DividerThemeData(color: AppColors.line, thickness: 1),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: AppColors.panelLift,
        contentTextStyle: body.copyWith(color: AppColors.sand),
        behavior: SnackBarBehavior.floating,
      ),
    );
  }
}
