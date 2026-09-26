import 'package:flutter/material.dart';

import '../data/biometric_settings.dart';
import '../data/staff_access.dart';
import '../data/staff_audit.dart';
import '../data/vendor_store.dart';
import '../widgets/ui_bits.dart';
import 'staff_entry_log_screen.dart';
import 'staff_role_rules_screen.dart';
import 'block_monthly_screen.dart';

class _UserPassCard {
  _UserPassCard(this.id, this.username, this.name, this.role, String password)
      : pass = TextEditingController(text: password);

  final int id;
  final String username;
  final String name;
  final String role;
  final TextEditingController pass;
  bool hide = false;

  void dispose() => pass.dispose();
}

/// Settings: password view/change (WinForms Password Add jaisa) + fingerprint.
class PasswordScreen extends StatefulWidget {
  const PasswordScreen({super.key, this.username});

  final String? username;

  @override
  State<PasswordScreen> createState() => _PasswordScreenState();
}

class _PasswordScreenState extends State<PasswordScreen> {
  final _current = TextEditingController();
  final _old = TextEditingController();
  final _next = TextEditingController();
  final _confirm = TextEditingController();
  final _fpPass = TextEditingController();
  bool _saving = false;
  bool _fpBusy = false;
  bool _fpEnabled = false;
  bool _hideCurrent = false;
  bool _hideOld = true;
  bool _hideNext = true;
  bool _hideConfirm = true;
  bool _hideFp = true;
  bool _canEditUsers = false;
  final _userCards = <_UserPassCard>[];

  String get _user =>
      (widget.username ?? StaffAudit.username ?? 'admin').trim();

  @override
  void initState() {
    super.initState();
    _loadFp();
    _loadPasswords();
  }

  Future<void> _loadFp() async {
    final on = await BiometricSettings.isEnabled();
    if (mounted) setState(() => _fpEnabled = on);
  }

  Future<void> _loadPasswords() async {
    final mine = await AuthStore.passwordOf(_user);
    final canEdit = StaffAccess.isAdmin || await StaffAccess.can('users', 'edit');
    final canOpen = StaffAccess.isAdmin || await StaffAccess.can('users', 'open');
    List<_UserPassCard> cards = const [];
    if (canOpen) {
      final rows = await AuthStore.users();
      cards = rows.map((r) {
        final id = int.tryParse('${r['userID'] ?? 0}') ?? 0;
        return _UserPassCard(
          id,
          '${r['username'] ?? ''}',
          '${r['uName'] ?? ''}',
          '${r['Role'] ?? ''}',
          '${r['upass'] ?? ''}',
        );
      }).toList();
    }
    if (!mounted) return;
    final old = List<_UserPassCard>.from(_userCards);
    setState(() {
      _current.text = mine;
      _canEditUsers = canEdit;
      _userCards
        ..clear()
        ..addAll(cards);
    });
    for (final c in old) {
      c.dispose();
    }
  }

  @override
  void dispose() {
    _current.dispose();
    _old.dispose();
    _next.dispose();
    _confirm.dispose();
    _fpPass.dispose();
    for (final c in _userCards) {
      c.dispose();
    }
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
      await _loadPasswords();
    }
  }

  Future<void> _saveUserPass(_UserPassCard card) async {
    if (!_canEditUsers) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Password change allow nahi.')),
      );
      return;
    }
    setState(() => _saving = true);
    final result = await AuthStore.setUserPassword(
      userId: card.id,
      newPassword: card.pass.text,
    );
    if (!mounted) return;
    setState(() => _saving = false);
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(result.message)),
    );
    if (result.ok) await _loadPasswords();
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
          subtitle: 'Password dekhein / change karein, fingerprint unlock',
        ),
        const SizedBox(height: 14),
        SoftPanel(
          child: Column(
            children: [
              HubPasswordField(
                controller: _current,
                label: 'Current Password (dekhein)',
                obscure: _hideCurrent,
                onToggle: () => setState(() => _hideCurrent = !_hideCurrent),
                prefixIcon: Icons.lock_open_outlined,
                readOnly: true,
              ),
              const SizedBox(height: 12),
              HubPasswordField(
                controller: _old,
                label: 'Current Password (change ke liye)',
                obscure: _hideOld,
                onToggle: () => setState(() => _hideOld = !_hideOld),
              ),
              const SizedBox(height: 12),
              HubPasswordField(
                controller: _next,
                label: 'New Password',
                obscure: _hideNext,
                onToggle: () => setState(() => _hideNext = !_hideNext),
              ),
              const SizedBox(height: 12),
              HubPasswordField(
                controller: _confirm,
                label: 'Confirm Password',
                obscure: _hideConfirm,
                onToggle: () => setState(() => _hideConfirm = !_hideConfirm),
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
        if (_userCards.isNotEmpty) ...[
          const SizedBox(height: 12),
          SoftPanel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Users / Passwords',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                Text(
                  'WinForms ki tarah password dekhein aur change karein',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
                const SizedBox(height: 12),
                for (final card in _userCards) ...[
                  Text(
                    '${card.name.isEmpty ? card.username : card.name}  ·  ${card.username}'
                    '${card.role.trim().isEmpty ? '' : '  ·  ${card.role}'}',
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 6),
                  HubPasswordField(
                    controller: card.pass,
                    label: 'Password',
                    obscure: card.hide,
                    onToggle: () => setState(() => card.hide = !card.hide),
                    readOnly: !_canEditUsers,
                  ),
                  if (_canEditUsers) ...[
                    const SizedBox(height: 8),
                    Align(
                      alignment: Alignment.centerRight,
                      child: ElevatedButton(
                        onPressed: _saving ? null : () => _saveUserPass(card),
                        child: const Text('Save Password'),
                      ),
                    ),
                  ],
                  const SizedBox(height: 14),
                ],
              ],
            ),
          ),
        ],
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
              HubPasswordField(
                controller: _fpPass,
                label: 'Password (fingerprint enable / disable)',
                obscure: _hideFp,
                onToggle: () => setState(() => _hideFp = !_hideFp),
                prefixIcon: Icons.lock_outline,
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
          onTap: () async {
            if (!await StaffAccess.can('staff_entry_log', 'open')) {
              if (!context.mounted) return;
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Staff Entry Log allow nahi.')),
              );
              return;
            }
            if (!context.mounted) return;
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
        if (StaffAccess.isAdmin) ...[
          const SizedBox(height: 12),
          SoftPanel(
            onTap: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const StaffRoleRulesScreen()),
              );
            },
            child: Row(
              children: [
                const Icon(Icons.rule, color: Colors.white70),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Staff Rules',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      Text(
                        'Manager / Operator — form open, add, edit, delete',
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
          SoftPanel(
            onTap: () {
              Navigator.of(context).push(
                MaterialPageRoute(builder: (_) => const BlockMonthlyScreen()),
              );
            },
            child: Row(
              children: [
                const Icon(Icons.payments_outlined, color: Colors.white70),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Block Monthly (VIP)',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      Text(
                        'Society block → default monthly installment',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
                const Icon(Icons.chevron_right, color: Colors.white54),
              ],
            ),
          ),
        ],
      ],
    );
  }
}
