import 'package:flutter/material.dart';

import '../data/biometric_settings.dart';
import '../data/staff_audit.dart';
import '../data/vendor_store.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';
import 'staff_entry_log_screen.dart';

/// Settings: password change + fingerprint (password ke baad enable/disable).
class PasswordScreen extends StatefulWidget {
  const PasswordScreen({super.key, this.username});

  final String? username;

  @override
  State<PasswordScreen> createState() => _PasswordScreenState();
}

class _PasswordScreenState extends State<PasswordScreen> {
  final _old = TextEditingController();
  final _next = TextEditingController();
  final _confirm = TextEditingController();
  final _fpPass = TextEditingController();
  bool _saving = false;
  bool _fpBusy = false;
  bool _fpEnabled = false;

  String get _user =>
      (widget.username ?? StaffAudit.username ?? 'admin').trim();

  @override
  void initState() {
    super.initState();
    _loadFp();
  }

  Future<void> _loadFp() async {
    final on = await BiometricSettings.isEnabled();
    if (mounted) setState(() => _fpEnabled = on);
  }

  @override
  void dispose() {
    _old.dispose();
    _next.dispose();
    _confirm.dispose();
    _fpPass.dispose();
    super.dispose();
  }

  Future<void> _update() async {
    if (_next.text != _confirm.text) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('New password aur confirm match nahi karte.')),
      );
      return;
    }
    setState(() => _saving = true);
    final result = await AuthStore.changePassword(
      username: _user,
      currentPassword: _old.text,
      newPassword: _next.text,
    );
    if (!mounted) return;
    setState(() => _saving = false);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(result.message)),
    );
    if (result.ok) {
      _old.clear();
      _next.clear();
      _confirm.clear();
    }
  }

  Future<void> _toggleFingerprint() async {
    if (_fpPass.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Fingerprint change ke liye pehle password dalein.'),
        ),
      );
      return;
    }
    setState(() => _fpBusy = true);
    final result = _fpEnabled
        ? await BiometricSettings.disableWithPassword(
            username: _user,
            password: _fpPass.text,
          )
        : await BiometricSettings.enableWithPassword(
            username: _user,
            password: _fpPass.text,
          );
    if (!mounted) return;
    setState(() {
      _fpBusy = false;
      if (result.ok) _fpEnabled = !_fpEnabled;
    });
    if (result.ok) _fpPass.clear();
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(result.message)),
    );
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        const SectionHeader(
          title: 'Settings',
          subtitle: 'Password change aur fingerprint unlock',
        ),
        const SizedBox(height: 14),
        SoftPanel(
          child: Column(
            children: [
              TextField(
                controller: _old,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Current Password'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _next,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'New Password'),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: _confirm,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Confirm Password'),
              ),
              const SizedBox(height: 18),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _saving ? null : _update,
                  child: _saving
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : const Text('Update Password'),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        SoftPanel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  const Icon(Icons.fingerprint, color: Colors.white70),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Fingerprint Login',
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                        Text(
                          _fpEnabled
                              ? 'Enabled — login pe fingerprint use ho sakta hai'
                              : 'Disabled — password dalein, phir Enable dabain',
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              TextField(
                controller: _fpPass,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'Password (fingerprint enable / disable)',
                  prefixIcon: Icon(Icons.lock_outline),
                ),
              ),
              const SizedBox(height: 14),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: _fpBusy ? null : _toggleFingerprint,
                  icon: _fpBusy
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : Icon(_fpEnabled ? Icons.toggle_on : Icons.toggle_off),
                  label: Text(
                    _fpEnabled ? 'Disable Fingerprint' : 'Enable Fingerprint',
                  ),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        SoftPanel(
          onTap: () {
            Navigator.of(context).push(
              MaterialPageRoute(builder: (_) => const StaffEntryLogScreen()),
            );
          },
          child: Row(
            children: [
              const Icon(Icons.history, color: Colors.white70),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Staff Entry Log',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    Text(
                      'Kis staff ne konsi entry ki — yahan dekho',
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right, color: Colors.white54),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Text(
          'Default login: admin / 1234',
          style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                color: AppColors.mist.withValues(alpha: 0.85),
              ),
        ),
      ],
    );
  }
}
