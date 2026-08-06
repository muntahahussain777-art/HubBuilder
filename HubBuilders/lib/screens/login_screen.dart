import 'package:flutter/material.dart';

import '../data/biometric_settings.dart';
import '../data/vendor_store.dart';
import '../db/app_database.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';
import 'app_shell.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen>
    with SingleTickerProviderStateMixin {
  final _user = TextEditingController(text: 'admin');
  final _pass = TextEditingController(text: '1234');
  late final AnimationController _pulse;
  bool _obscure = true;
  bool _loading = false;
  bool _fpAvailable = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _pulse = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1600),
    )..repeat(reverse: true);
    AppDatabase.instance.database;
    _checkFingerprint();
  }

  Future<void> _checkFingerprint() async {
    final on = await BiometricSettings.isEnabled();
    if (mounted) setState(() => _fpAvailable = on);
  }

  @override
  void dispose() {
    _pulse.dispose();
    _user.dispose();
    _pass.dispose();
    super.dispose();
  }

  void _goHome() {
    Navigator.of(context).pushReplacement(
      PageRouteBuilder(
        pageBuilder: (context, a, secondaryAnimation) => const AppShell(),
        transitionsBuilder: (context, a, secondaryAnimation, child) =>
            FadeTransition(opacity: a, child: child),
        transitionDuration: const Duration(milliseconds: 450),
      ),
    );
  }

  Future<void> _enter() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final result = await AuthStore.login(
      username: _user.text,
      password: _pass.text,
    );
    if (!mounted) return;
    if (!result.ok) {
      setState(() {
        _loading = false;
        _error = result.message;
      });
      return;
    }
    _goHome();
  }

  Future<void> _fingerprintLogin() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    final result = await BiometricSettings.unlockWithFingerprint();
    if (!mounted) return;
    if (!result.ok) {
      setState(() {
        _loading = false;
        _error = result.message;
      });
      return;
    }
    _goHome();
  }

  @override
  Widget build(BuildContext context) {
    return AtmosphereBackground(
      child: Scaffold(
        backgroundColor: Colors.transparent,
        body: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 420),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    ScaleTransition(
                      scale: Tween(begin: 0.96, end: 1.0).animate(
                        CurvedAnimation(parent: _pulse, curve: Curves.easeInOut),
                      ),
                      child: const BrandMark(),
                    ),
                    const SizedBox(height: 28),
                    SoftPanel(
                      padding: const EdgeInsets.fromLTRB(22, 26, 22, 22),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Text(
                            'Sign in',
                            style: Theme.of(context).textTheme.headlineMedium,
                          ),
                          const SizedBox(height: 6),
                          Text(
                            'Username aur password se login karein.',
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                          const SizedBox(height: 22),
                          TextField(
                            controller: _user,
                            decoration: const InputDecoration(
                              labelText: 'Username',
                              prefixIcon: Icon(Icons.person_outline),
                            ),
                          ),
                          const SizedBox(height: 14),
                          TextField(
                            controller: _pass,
                            obscureText: _obscure,
                            onSubmitted: (_) => _enter(),
                            decoration: InputDecoration(
                              labelText: 'Password',
                              prefixIcon: const Icon(Icons.lock_outline),
                              suffixIcon: IconButton(
                                onPressed: () =>
                                    setState(() => _obscure = !_obscure),
                                icon: Icon(
                                  _obscure
                                      ? Icons.visibility_outlined
                                      : Icons.visibility_off_outlined,
                                ),
                              ),
                            ),
                          ),
                          if (_error != null) ...[
                            const SizedBox(height: 10),
                            Text(
                              _error!,
                              style: const TextStyle(color: AppColors.danger),
                            ),
                          ],
                          const SizedBox(height: 22),
                          ElevatedButton(
                            onPressed: _loading ? null : _enter,
                            child: _loading
                                ? const SizedBox(
                                    height: 18,
                                    width: 18,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                      color: Colors.white,
                                    ),
                                  )
                                : const Text('Enter HubBuilders'),
                          ),
                          if (_fpAvailable) ...[
                            const SizedBox(height: 12),
                            OutlinedButton.icon(
                              onPressed: _loading ? null : _fingerprintLogin,
                              icon: const Icon(Icons.fingerprint),
                              label: const Text('Fingerprint se login'),
                            ),
                          ],
                          const SizedBox(height: 12),
                          Text(
                            'Default: admin / 1234',
                            textAlign: TextAlign.center,
                            style: Theme.of(context)
                                .textTheme
                                .bodyMedium
                                ?.copyWith(fontSize: 12),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 18),
                    Text(
                      'HubBuilders · Local app',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            color: AppColors.mist.withValues(alpha: 0.8),
                          ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
