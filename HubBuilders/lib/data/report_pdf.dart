import 'dart:typed_data';

import 'package:intl/intl.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

import 'png_pdf.dart';
import 'reports_store.dart';

/// WinForms ReportViewer jaisa PDF: Hub Builders header + bordered table.
abstract final class ReportPdf {
  static const _navy = PdfColor.fromInt(0xFF273146);
  static const _gold = PdfColor.fromInt(0xFFD4A84B);
  static const _line = PdfColor.fromInt(0xFF1A1A1A);
  static final _stamp = DateFormat('dd-MMM-yyyy hh:mm a');
  static final _money = NumberFormat('#,##0.00');

  static Future<Uint8List> build({
    required String title,
    required ReportResult result,
  }) async {
    final rows = <List<String>>[
      for (var i = 0; i < result.lines.length; i++)
        [
          '${i + 1}',
          result.lines[i].title,
          [
            result.lines[i].subtitle,
            result.lines[i].meta,
          ].where((s) => s.trim().isNotEmpty).join('  ·  '),
          result.lines[i].amount == null
              ? ''
              : _money.format(result.lines[i].amount),
        ],
    ];
    return buildTable(
      title: title,
      summary: result.summary,
      headers: const ['S.N', 'Particulars', 'Details', 'Amount'],
      rows: rows,
      landscape: true,
      rightAlign: const {3},
    );
  }

  static Future<void> saveReport({
    required String title,
    required ReportResult result,
    String? fileBase,
  }) async {
    final bytes = await build(title: title, result: result);
    await PngPdf.saveBytes(
      bytes: bytes,
      fileBase: fileBase ?? _safeFileBase(title),
    );
  }

  static String _safeFileBase(String title) {
    final t = title.trim().replaceAll(RegExp(r'[^\w\-]+'), '_');
    if (t.isEmpty) return 'Report';
    return t.length > 40 ? t.substring(0, 40) : t;
  }

  static Future<Uint8List> buildPlain({
    required String title,
    required String summary,
    required List<String> lines,
  }) async {
    return buildTable(
      title: title,
      summary: summary,
      headers: const ['S.N', 'Details'],
      rows: [
        for (var i = 0; i < lines.length; i++) ['${i + 1}', lines[i]],
      ],
      landscape: true,
    );
  }

  static Future<Uint8List> buildTable({
    required String title,
    String summary = '',
    required List<String> headers,
    required List<List<String>> rows,
    bool landscape = true,
    Set<int> rightAlign = const {},
  }) async {
    final doc = pw.Document();
    final now = _stamp.format(DateTime.now());
    final pageFormat =
        landscape ? PdfPageFormat.a4.landscape : PdfPageFormat.a4;
    final alignments = <int, pw.Alignment>{
      for (var i = 0; i < headers.length; i++)
        i: rightAlign.contains(i)
            ? pw.Alignment.centerRight
            : (i == 0 ? pw.Alignment.center : pw.Alignment.centerLeft),
    };

    doc.addPage(
      pw.MultiPage(
        pageFormat: pageFormat,
        margin: const pw.EdgeInsets.fromLTRB(18, 16, 18, 16),
        header: (ctx) => pw.Column(
          crossAxisAlignment: pw.CrossAxisAlignment.stretch,
          children: [
            pw.Center(
              child: pw.Text(
                'HUB BUILDERS',
                style: pw.TextStyle(
                  fontSize: 22,
                  fontWeight: pw.FontWeight.bold,
                  letterSpacing: 1.2,
                  color: _navy,
                ),
              ),
            ),
            pw.SizedBox(height: 4),
            pw.Container(height: 3, color: _gold),
            pw.SizedBox(height: 8),
            pw.Row(
              mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
              children: [
                pw.Expanded(
                  child: pw.Text(
                    title,
                    style: pw.TextStyle(
                      fontSize: 12,
                      fontWeight: pw.FontWeight.bold,
                      color: _navy,
                    ),
                  ),
                ),
                pw.Text(
                  '$now    Page ${ctx.pageNumber} / ${ctx.pagesCount}',
                  style: const pw.TextStyle(fontSize: 8, color: PdfColors.grey700),
                ),
              ],
            ),
            if (summary.trim().isNotEmpty) ...[
              pw.SizedBox(height: 4),
              pw.Text(
                summary.trim(),
                style: pw.TextStyle(
                  fontSize: 9,
                  fontWeight: pw.FontWeight.bold,
                  color: _navy,
                ),
              ),
            ],
            pw.SizedBox(height: 8),
          ],
        ),
        build: (ctx) {
          if (rows.isEmpty) {
            return [
              pw.Container(
                padding: const pw.EdgeInsets.all(12),
                decoration: pw.BoxDecoration(
                  border: pw.Border.all(color: _line, width: 0.7),
                ),
                child: pw.Text(
                  'No records found.',
                  style: const pw.TextStyle(fontSize: 10),
                ),
              ),
            ];
          }
          return [
            pw.TableHelper.fromTextArray(
              headers: headers,
              data: rows,
              headerStyle: pw.TextStyle(
                fontSize: 8.5,
                fontWeight: pw.FontWeight.bold,
                color: PdfColors.white,
              ),
              headerDecoration: const pw.BoxDecoration(color: _navy),
              headerAlignment: pw.Alignment.center,
              cellStyle: const pw.TextStyle(fontSize: 8, color: PdfColors.black),
              cellAlignments: alignments,
              cellPadding: const pw.EdgeInsets.symmetric(
                horizontal: 4,
                vertical: 3,
              ),
              headerPadding: const pw.EdgeInsets.symmetric(
                horizontal: 4,
                vertical: 4,
              ),
              oddRowDecoration:
                  const pw.BoxDecoration(color: PdfColor.fromInt(0xFFF4F6F8)),
              border: const pw.TableBorder(
                left: pw.BorderSide(color: _line, width: 0.6),
                right: pw.BorderSide(color: _line, width: 0.6),
                top: pw.BorderSide(color: _line, width: 0.6),
                bottom: pw.BorderSide(color: _line, width: 0.6),
                horizontalInside: pw.BorderSide(color: _line, width: 0.4),
                verticalInside: pw.BorderSide(color: _line, width: 0.4),
              ),
            ),
          ];
        },
      ),
    );
    return Uint8List.fromList(await doc.save());
  }
}
