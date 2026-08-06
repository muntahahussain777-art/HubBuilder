import 'package:local_auth/local_auth.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'vendor_store.dart';

/// Fingerprint unlock — Settings se password ke baad enable/disable.
abstract final class BiometricSettings {
  static const _kEnabled = 'fingerprint_enabled';
  static const _kUser = 'fingerprint_username';

  static final LocalAuthentication _auth = LocalAuthentication();

  static Future<bool> isEnabled() async {
    final p = await SharedPreferences.getInstance();
    return p.getBool(_kEnabled) ?? false;
  }

  static Future<String?> savedUsername() async {
    final p = await SharedPreferences.getInstance();
    return p.getString(_kUser);
  }

  static Future<bool> deviceSupportsBiometrics() async {
    try {
      final can = await _auth.canCheckBiometrics;
      final supported = await _auth.isDeviceSupported();
      if (!can && !supported) return false;
      final available = await _auth.getAvailableBiometrics();
      return supported || available.isNotEmpty;
    } catch (_) {
      return false;
    }
  }

  /// Password verify + biometric confirm, phir enable.
  static Future<({bool ok, String message})> enableWithPassword({
    required String username,
    required String password,
  }) async {
    final login = await AuthStore.login(username: username, password: password);
    if (!login.ok) {
      return (ok: false, message: 'Password galat hai. Fingerprint enable nahi hua.');
    }

    final supported = await deviceSupportsBiometrics();
    if (!supported) {
      return (
        ok: false,
        message: 'Is device pe fingerprint / face unlock available nahi.',
      );
    }

    final verified = await authenticate(
      reason: 'Fingerprint enable karne ke liye confirm karein',
    );
    if (!verified) {
      return (ok: false, message: 'Fingerprint confirm nahi hua.');
    }

    final p = await SharedPreferences.getInstance();
    await p.setBool(_kEnabled, true);
    await p.setString(_kUser, username.trim());
    return (ok: true, message: 'Fingerprint enable ho gaya.');
  }

  /// Password verify ke baad disable.
  static Future<({bool ok, String message})> disableWithPassword({
    required String username,
    required String password,
  }) async {
    final login = await AuthStore.login(username: username, password: password);
    if (!login.ok) {
      return (ok: false, message: 'Password galat hai. Fingerprint disable nahi hua.');
    }
    final p = await SharedPreferences.getInstance();
    await p.setBool(_kEnabled, false);
    await p.remove(_kUser);
    return (ok: true, message: 'Fingerprint disable ho gaya.');
  }

  static Future<bool> authenticate({required String reason}) async {
    try {
      return await _auth.authenticate(
        localizedReason: reason,
        biometricOnly: true,
        persistAcrossBackgrounding: true,
      );
    } on LocalAuthException {
      return false;
    } catch (_) {
      return false;
    }
  }

  /// Login screen: fingerprint se session resume.
  static Future<({bool ok, String message})> unlockWithFingerprint() async {
    final enabled = await isEnabled();
    if (!enabled) {
      return (ok: false, message: 'Fingerprint Settings se enable karein.');
    }
    final user = await savedUsername();
    if (user == null || user.isEmpty) {
      return (
        ok: false,
        message: 'Fingerprint user nahi mila. Settings se dobara enable karein.',
      );
    }
    final ok = await authenticate(reason: 'HubBuilders login — fingerprint');
    if (!ok) return (ok: false, message: 'Fingerprint fail / cancel.');
    return AuthStore.resumeSession(user);
  }
}
