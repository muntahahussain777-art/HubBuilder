import 'dart:io';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';

abstract final class PngPdf {
  static Future<Uint8List> buildBytes({
    required List<Uint8List> pages,
  }) async {
    final doc = pw.Document();
    for (final bytes in pages) {
      final img = pw.MemoryImage(bytes);
      doc.addPage(
        pw.Page(
          pageFormat: PdfPageFormat.a4,
          margin: const pw.EdgeInsets.all(0),
          build: (_) => pw.Center(
            child: pw.Image(img, fit: pw.BoxFit.contain),
          ),
        ),
      );
    }
    return Uint8List.fromList(await doc.save());
  }

  static Future<File?> save({
    required List<Uint8List> pages,
    required String fileBase,
  }) async {
    final bytes = await buildBytes(pages: pages);
    final stamp = DateTime.now()
        .toIso8601String()
        .replaceAll(':', '')
        .replaceAll('.', '')
        .replaceAll('-', '');
    final name = '${fileBase}_$stamp.pdf';
    String? path;
    try {
      path = await FilePicker.saveFile(
        dialogTitle: 'Save PDF — folder choose karein',
        fileName: name,
        type: FileType.custom,
        allowedExtensions: const ['pdf'],
        bytes: bytes,
      );
    } catch (_) {
      path = null;
    }
    if (path == null || path.trim().isEmpty) {
      final dir = await FilePicker.getDirectoryPath(
        dialogTitle: 'PDF save karne ka folder choose karein',
      );
      if (dir == null || dir.trim().isEmpty) return null;
      path = '$dir${Platform.pathSeparator}$name';
      await File(path).writeAsBytes(bytes, flush: true);
      return File(path);
    }
    final f = File(path);
    try {
      if (!await f.exists() || await f.length() == 0) {
        await f.writeAsBytes(bytes, flush: true);
      }
    } catch (_) {
      try {
        await f.writeAsBytes(bytes, flush: true);
      } catch (_) {}
    }
    return f;
  }

  /// System print dialog — koi bhi printer.
  static Future<void> printPages({
    required List<Uint8List> pages,
    String name = 'HubBuilders',
  }) async {
    final bytes = await buildBytes(pages: pages);
    await Printing.layoutPdf(
      name: name,
      onLayout: (_) async => bytes,
    );
  }

  static Future<File?> saveBytes({
    required Uint8List bytes,
    required String fileBase,
  }) async {
    final stamp = DateTime.now()
        .toIso8601String()
        .replaceAll(':', '')
        .replaceAll('.', '')
        .replaceAll('-', '');
    final name = '${fileBase}_$stamp.pdf';
    String? path;
    try {
      path = await FilePicker.saveFile(
        dialogTitle: 'Save PDF — folder choose karein',
        fileName: name,
        type: FileType.custom,
        allowedExtensions: const ['pdf'],
        bytes: bytes,
      );
    } catch (_) {
      path = null;
    }
    if (path == null || path.trim().isEmpty) {
      final dir = await FilePicker.getDirectoryPath(
        dialogTitle: 'PDF save karne ka folder choose karein',
      );
      if (dir == null || dir.trim().isEmpty) return null;
      path = '$dir${Platform.pathSeparator}$name';
      await File(path).writeAsBytes(bytes, flush: true);
      return File(path);
    }
    final f = File(path);
    try {
      if (!await f.exists() || await f.length() == 0) {
        await f.writeAsBytes(bytes, flush: true);
      }
    } catch (_) {
      try {
        await f.writeAsBytes(bytes, flush: true);
      } catch (_) {}
    }
    return f;
  }

  static Future<void> printBytes({
    required Uint8List bytes,
    String name = 'HubBuilders',
  }) async {
    await Printing.layoutPdf(
      name: name,
      onLayout: (_) async => bytes,
    );
  }
}
