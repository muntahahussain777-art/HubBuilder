import 'dart:typed_data';

import 'package:intl/intl.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

import 'plot_status_store.dart';

abstract final class PlotStatusPdf {
  static Future<Uint8List> build(PlotStatusResult data, {String statusFilter = ''}) async {
    final doc = pw.Document();
    final now = DateFormat('dd-MMM-yyyy hh:mm a').format(DateTime.now());
    final c = data.counts;
    final filter = statusFilter.trim().isEmpty ? 'All Status' : '$statusFilter only';
    const black = PdfColors.black;

    doc.addPage(
      pw.MultiPage(
        pageFormat: PdfPageFormat.a4.landscape,
        margin: const pw.EdgeInsets.fromLTRB(16, 14, 16, 14),
        header: (ctx) => pw.Column(
          crossAxisAlignment: pw.CrossAxisAlignment.start,
          children: [
            pw.Row(
              mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
              children: [
                pw.Text(
                  'HUB BUILDERS',
                  style: pw.TextStyle(
                    fontSize: 16,
                    fontWeight: pw.FontWeight.bold,
                    color: black,
                  ),
                ),
                pw.Text(
                  '$now    Page ${ctx.pageNumber} / ${ctx.pagesCount}',
                  style: const pw.TextStyle(fontSize: 8, color: black),
                ),
              ],
            ),
            pw.SizedBox(height: 2),
            pw.Text(
              'Plot Status Report  —  ${data.societyName}  —  $filter',
              style: pw.TextStyle(
                fontSize: 10,
                fontWeight: pw.FontWeight.bold,
                color: black,
              ),
            ),
            pw.SizedBox(height: 4),
            pw.Text(
              'Available: ${c.available}     Installment: ${c.installment}     '
              'Reserve: ${c.reserve}     Sold: ${c.sold}     Showing: ${data.rows.length}',
              style: const pw.TextStyle(fontSize: 9, color: black),
            ),
            pw.SizedBox(height: 6),
          ],
        ),
        build: (ctx) => [
          pw.TableHelper.fromTextArray(
            headerStyle: pw.TextStyle(
              fontSize: 8.5,
              fontWeight: pw.FontWeight.bold,
              color: PdfColors.white,
            ),
            headerDecoration: const pw.BoxDecoration(color: PdfColors.blueGrey800),
            cellStyle: const pw.TextStyle(fontSize: 8.5, color: black),
            cellAlignments: {
              0: pw.Alignment.centerLeft,
              1: pw.Alignment.centerLeft,
              2: pw.Alignment.centerLeft,
              3: pw.Alignment.centerLeft,
              4: pw.Alignment.centerLeft,
              5: pw.Alignment.centerLeft,
              6: pw.Alignment.centerLeft,
            },
            cellPadding: const pw.EdgeInsets.symmetric(horizontal: 3, vertical: 2.5),
            headerPadding: const pw.EdgeInsets.symmetric(horizontal: 3, vertical: 3),
            oddRowDecoration: const pw.BoxDecoration(color: PdfColors.grey200),
            columnWidths: {
              0: const pw.FixedColumnWidth(28),
              1: const pw.FixedColumnWidth(72),
              2: const pw.FlexColumnWidth(2.2),
              3: const pw.FlexColumnWidth(1.2),
              4: const pw.FixedColumnWidth(62),
              5: const pw.FixedColumnWidth(70),
              6: const pw.FlexColumnWidth(2.4),
            },
            headers: const [
              '#',
              'Status',
              'Society',
              'Block',
              'Plot No',
              'Type',
              'Customer',
            ],
            data: [
              for (var i = 0; i < data.rows.length; i++)
                [
                  '${i + 1}',
                  data.rows[i].kind,
                  data.rows[i].society,
                  data.rows[i].block,
                  data.rows[i].plotNo,
                  data.rows[i].plotType,
                  data.rows[i].customer,
                ],
            ],
          ),
        ],
      ),
    );
    return Uint8List.fromList(await doc.save());
  }
}
