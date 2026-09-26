import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

import '../data/backup_store.dart';
import '../data/auto_backup.dart';
import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

/// Same as WinForms Backup_RestoreForm.
class BackupRestoreScreen extends StatefulWidget {
  const BackupRestoreScreen({super.key, this.onBack});

  final VoidCallback? onBack;

  @override
  State<BackupRestoreScreen> createState() => _BackupRestoreScreenState();
}

class _BackupRestoreScreenState extends State<BackupRestoreScreen> {
  String? _backupFolder;
  String? _restoreFile;
  String _dbPath = '';
  String _interval = '1d';
  String _autoPath = '';
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    BackupStore.currentDbPath().then((p) {
      if (mounted) setState(() => _dbPath = p);
    });
    AutoBackup.interval().then((v) {
      if (mounted) setState(() => _interval = v);
    });
    AutoBackup.todayDir().then((d) {
      if (mounted) setState(() => _autoPath = d.path);
    });
  }

  void _toast(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
  }

  Future<void> _pickBackupFolder() async {
    final path = await FilePicker.getDirectoryPath(
      dialogTitle: 'Select Backup Folder',
    );
    if (path != null) setState(() => _backupFolder = path);
  }

  Future<void> _pickRestoreFile() async {
    final res = await FilePicker.pickFiles(
      dialogTitle: 'Select Restore .db File',
      type: FileType.custom,
      allowedExtensions: const ['db'],
    );
    if (res != null && res.files.single.path != null) {
      setState(() => _restoreFile = res.files.single.path);
    }
  }

  Future<void> _backup() async {
    if (_backupFolder == null || _backupFolder!.isEmpty) {
      _toast('Please select a valid backup path.');
      return;
    }
    setState(() => _busy = true);
    final r = await BackupStore.backupToFolder(_backupFolder!);
    if (!mounted) return;
    setState(() => _busy = false);
    _toast(r.message);
  }

  Future<void> _restore() async {
    if (_restoreFile == null || _restoreFile!.isEmpty) {
      _toast('Please select a valid restore file.');
      return;
    }
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirm Restore'),
        content: const Text(
          'Client ka purana data restore hoga (RealEstateBuilders.db).\n'
          'Current data overwrite ho jayega. Continue?',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('No')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Yes')),
        ],
      ),
    );
    if (ok != true) return;
    setState(() => _busy = true);
    final r = await BackupStore.restoreFromFile(_restoreFile!);
    if (!mounted) return;
    setState(() => _busy = false);
    _toast(r.message);
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
      children: [
        SoftPanel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  if (widget.onBack != null)
                    IconButton(
                      onPressed: widget.onBack,
                      icon: const Icon(Icons.arrow_back),
                    ),
                  Expanded(
                    child: Text(
                      'Backup And Restore',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            color: AppColors.sand,
                          ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Text(
                'Live DB:\n$_dbPath',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 16),
              Text('Backup folder', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 6),
              Text(
                _backupFolder ?? '(not selected)',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _pickBackupFolder,
                      child: const Text('Browse Backup Path'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _backup,
                      child: const Text('Backup'),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Text('Auto backup', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 6),
              Text(
                _autoPath.isEmpty ? '(phone storage / HubBuilders / date)' : _autoPath,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 8),
              DropdownButton<String>(
                isExpanded: true,
                value: _interval,
                items: const [
                  DropdownMenuItem(value: 'entry', child: Text('After each entry')),
                  DropdownMenuItem(value: '3h', child: Text('Every 3 hours')),
                  DropdownMenuItem(value: '1d', child: Text('Every 1 day')),
                ],
                onChanged: _busy
                    ? null
                    : (v) async {
                        if (v == null) return;
                        await AutoBackup.setInterval(v);
                        setState(() => _interval = v);
                      },
              ),
              const SizedBox(height: 22),
              Text('Restore .db file', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 6),
              Text(
                _restoreFile ?? '(not selected)',
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _busy ? null : _pickRestoreFile,
                      child: const Text('Browse Restore File'),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _busy ? null : _restore,
                      child: const Text('Restore'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ],
    );
  }
}
