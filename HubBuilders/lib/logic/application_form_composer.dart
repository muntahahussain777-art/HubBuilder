import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

/// Same seats / scale as WinForms `frmApplicationForm` + `frmApplicationFormBlank`.
class ApplicationFormData {
  const ApplicationFormData({
    required this.plotNo,
    required this.size,
    required this.commercialPlot,
    required this.residentialPlot,
    required this.name,
    required this.father,
    required this.postal,
    required this.residential,
    required this.phoneOffice,
    required this.phoneHome,
    required this.careOf,
    required this.mobile,
    required this.overseas,
    required this.nationality,
    required this.nic,
    required this.heir,
    required this.place,
    required this.date,
    required this.day,
    required this.month,
    required this.year,
    required this.acceptDate,
    required this.booker,
    required this.plotPrice,
    required this.corner,
    required this.roadside,
    required this.otherCharges,
    required this.grandTotal,
  });

  final String plotNo;
  final String size;
  final String commercialPlot;
  final String residentialPlot;
  final String name;
  final String father;
  final String postal;
  final String residential;
  final String phoneOffice;
  final String phoneHome;
  final String careOf;
  final String mobile;
  final String overseas;
  final String nationality;
  final String nic;
  final String heir;
  final String place;
  final String date;
  final String day;
  final String month;
  final String year;
  final String acceptDate;
  final String booker;
  final String plotPrice;
  final String corner;
  final String roadside;
  final String otherCharges;
  final String grandTotal;
}

class ApplicationFormComposer {
  static const frontAsset = 'assets/reports/ApplicationFormFront.png';
  static const backAsset = 'assets/reports/ApplicationFormBack.png';
  static const bottomPadPct = 0.048;
  static const ink = Color.fromARGB(255, 20, 15, 8);

  static Future<ui.Image> _loadAssetImage(String asset) async {
    final data = await rootBundle.load(asset);
    final codec = await ui.instantiateImageCodec(data.buffer.asUint8List());
    final frame = await codec.getNextFrame();
    return frame.image;
  }

  static Future<({Uint8List front, Uint8List back})> compose({
    required ApplicationFormData data,
    required bool blankOverlay,
  }) async {
    final frontTpl = await _loadAssetImage(frontAsset);
    final backTpl = await _loadAssetImage(backAsset);
    try {
      final front = await _composePage(
        tpl: frontTpl,
        blankOverlay: blankOverlay,
        paint: (c, w, h, fs, fsSm, fsDate) {
          if (blankOverlay) {
            _drawFrontBlank(c, data, w, h, fs, fsSm);
          } else {
            _drawFrontFilled(c, data, w, h, fs, fsSm);
          }
        },
      );
      final back = await _composePage(
        tpl: backTpl,
        blankOverlay: blankOverlay,
        paint: (c, w, h, fs, fsSm, fsDate) {
          if (blankOverlay) {
            _drawBackBlank(c, data, w, h, fs, fsSm, fsDate);
          } else {
            _drawBackFilled(c, data, w, h, fs, fsSm, fsDate);
          }
        },
      );
      return (front: front, back: back);
    } finally {
      frontTpl.dispose();
      backTpl.dispose();
    }
  }

  static Future<Uint8List> _composePage({
    required ui.Image tpl,
    required bool blankOverlay,
    required void Function(
      Canvas c,
      double w,
      double h,
      double fs,
      double fsSm,
      double fsDate,
    ) paint,
  }) async {
    final w = tpl.width.toDouble();
    final h = tpl.height.toDouble();
    final recorder = ui.PictureRecorder();
    final canvas = Canvas(recorder);

    if (blankOverlay) {
      canvas.drawRect(Rect.fromLTWH(0, 0, w, h), Paint()..color = Colors.white);
    } else {
      canvas.drawImage(tpl, Offset.zero, Paint());
    }

    final fs = (h * 0.0145).clamp(13.0, 48.0);
    final fsSm = (h * 0.0125).clamp(11.0, 40.0);
    final fsDate = (h * 0.0165).clamp(14.0, 52.0);
    paint(canvas, w, h, fs, fsSm, fsDate);

    final picture = recorder.endRecording();
    final composed = await picture.toImage(tpl.width, tpl.height);
    picture.dispose();
    try {
      return await _printerSafeBottom(composed, bottomPadPct);
    } finally {
      composed.dispose();
    }
  }

  static void _drawFrontFilled(
    Canvas c,
    ApplicationFormData d,
    double w,
    double h,
    double fs,
    double fsSm,
  ) {
    _onLine(c, d.plotNo, 0.70, 0.320, 0.12, w, h, fs);
    _onLine(c, d.size, 0.50, 0.320, 0.12, w, h, fs);
    _onLine(c, d.commercialPlot, 0.28, 0.320, 0.12, w, h, fs);
    _onLine(c, d.residentialPlot, 0.08, 0.320, 0.12, w, h, fs);
    _onLine(c, d.name, 0.10, 0.399, 0.60, w, h, fs);
    _onLine(c, d.father, 0.10, 0.440, 0.52, w, h, fs);
    _onLine(c, d.postal, 0.10, 0.477, 0.52, w, h, fs);
    _onLine(c, d.residential, 0.10, 0.517, 0.52, w, h, fs);
    _onLine(c, d.phoneOffice, 0.70, 0.556, 0.14, w, h, fsSm);
    _onLine(c, d.phoneHome, 0.42, 0.556, 0.14, w, h, fsSm);
    _onLine(c, d.careOf, 0.10, 0.556, 0.14, w, h, fsSm);
    _onLine(c, d.mobile, 0.68, 0.596, 0.16, w, h, fs);
    _onLine(c, d.overseas, 0.12, 0.596, 0.28, w, h, fsSm);
    _onLine(c, d.nationality, 0.68, 0.635, 0.14, w, h, fs);
    _onLine(c, d.nic, 0.12, 0.635, 0.32, w, h, fs);
    _onLine(c, d.heir, 0.10, 0.674, 0.48, w, h, fsSm);
    _onLine(c, d.place, 0.55, 0.908, 0.29, w, h, fsSm);
    _onLine(c, d.date, 0.68, 0.948, 0.16, w, h, fsSm);
  }

  static void _drawFrontBlank(
    Canvas c,
    ApplicationFormData d,
    double w,
    double h,
    double fs,
    double fsSm,
  ) {
    _onLine(c, d.plotNo, 0.70, 0.320, 0.12, w, h, fs);
    _onLine(c, d.size, 0.50, 0.320, 0.12, w, h, fs);
    _onLine(c, d.commercialPlot, 0.28, 0.320, 0.12, w, h, fs);
    _onLine(c, d.residentialPlot, 0.08, 0.3166, 0.12, w, h, fs);
    _onLine(c, d.name, 0.10, 0.3939, 0.60, w, h, fs);
    _onLine(c, d.father, 0.10, 0.4339, 0.52, w, h, fs);
    _onLine(c, d.postal, 0.10, 0.4709, 0.52, w, h, fs);
    _onLine(c, d.residential, 0.10, 0.5109, 0.52, w, h, fs);
    _onLine(c, d.phoneOffice, 0.70, 0.5482, 0.14, w, h, fsSm);
    _onLine(c, d.phoneHome, 0.42, 0.5482, 0.14, w, h, fsSm);
    _onLineCentered(c, d.careOf, 0.10 + (20 / 210), 0.5482, 0.14, w, h, fsSm);
    _onLine(c, d.mobile, 0.68, 0.5882, 0.16, w, h, fs);
    _onLine(c, d.overseas, 0.12, 0.5892, 0.28, w, h, fsSm);
    _onLine(c, d.nationality, 0.68, 0.6299, 0.14, w, h, fs);
    _onLine(c, d.nic, 0.12, 0.6289, 0.32, w, h, fs);
    _onLine(c, d.heir, 0.10, 0.6645, 0.48, w, h, fsSm);
    _onLineCentered(c, d.place, 0.55 + (20 / 210), 0.9019, 0.29, w, h, fsSm);
    _onLine(c, d.date, 0.68, 0.9419, 0.16, w, h, fsSm);
  }

  static void _drawBackFilled(
    Canvas c,
    ApplicationFormData d,
    double w,
    double h,
    double fs,
    double fsSm,
    double fsDate,
  ) {
    _onLine(c, d.day, 0.76, 0.340, 0.08, w, h, fsDate);
    _onLine(c, d.month, 0.52, 0.340, 0.10, w, h, fsDate);
    _onLine(c, d.year, 0.24, 0.340, 0.12, w, h, fsDate);
    _onLine(c, d.acceptDate, 0.68, 0.508, 0.16, w, h, fsDate);
    _onLine(c, d.booker, 0.18, 0.680, 0.52, w, h, fs);
    _onLine(c, d.plotPrice, 0.18, 0.722, 0.52, w, h, fs);
    _onLine(c, d.corner, 0.18, 0.764, 0.52, w, h, fs);
    _onLine(c, d.roadside, 0.18, 0.805, 0.52, w, h, fs);
    _onLine(c, d.otherCharges, 0.18, 0.847, 0.52, w, h, fs);
    _onLine(c, d.grandTotal, 0.18, 0.889, 0.52, w, h, fs);
  }

  static void _drawBackBlank(
    Canvas c,
    ApplicationFormData d,
    double w,
    double h,
    double fs,
    double fsSm,
    double fsDate,
  ) {
    _onLine(c, d.day, 0.76, 0.3434, 0.08, w, h, fsDate);
    _onLine(c, d.month, 0.52, 0.3434, 0.10, w, h, fsDate);
    _onLine(c, d.year, 0.24, 0.3434, 0.12, w, h, fsDate);
    _onLine(c, d.acceptDate, 0.68, 0.5114, 0.16, w, h, fsDate);
    _onLine(c, d.booker, 0.18, 0.680, 0.52, w, h, fs);
    _onLine(c, d.plotPrice, 0.18, 0.722, 0.52, w, h, fs);
    _onLine(c, d.corner, 0.18, 0.764, 0.52, w, h, fs);
    _onLine(c, d.roadside, 0.18, 0.805, 0.52, w, h, fs);
    _onLine(c, d.otherCharges, 0.18, 0.847, 0.52, w, h, fs);
    _onLine(c, d.grandTotal, 0.18, 0.889, 0.52, w, h, fs);
  }

  /// WinForms DrawOnLine — right aligned, shrink to fit.
  static void _onLine(
    Canvas canvas,
    String text,
    double xPct,
    double lineYPct,
    double wPct,
    double imgW,
    double imgH,
    double fontSize, {
    TextAlign align = TextAlign.right,
  }) {
    final t = text.trim();
    if (t.isEmpty) return;

    final lineY = lineYPct * imgH;
    final maxW = wPct * imgW;
    var size = fontSize;
    TextPainter? tp;
    while (true) {
      tp?.dispose();
      tp = TextPainter(
        text: TextSpan(
          text: t,
          style: TextStyle(
            color: ink,
            fontSize: size,
            fontWeight: FontWeight.w700,
            fontFamily: 'Segoe UI',
          ),
        ),
        textAlign: align,
        textDirection: TextDirection.ltr,
        maxLines: 1,
      )..layout(maxWidth: maxW * 4);
      if (tp.width <= maxW || size <= 7.5) break;
      size -= 0.5;
    }

    final boxH = (tp.height * 1.15).clamp(imgH * 0.018, imgH);
    var top = lineY - boxH - 2;
    if (top < 0) top = 0;
    tp.layout(maxWidth: maxW);
    final dx = align == TextAlign.center
        ? (xPct * imgW) + (maxW - tp.width) / 2
        : (xPct * imgW) + (maxW - tp.width); // right
    tp.paint(canvas, Offset(dx, top + (boxH - tp.height)));
    tp.dispose();
  }

  static void _onLineCentered(
    Canvas canvas,
    String text,
    double xPct,
    double lineYPct,
    double wPct,
    double imgW,
    double imgH,
    double fontSize,
  ) {
    _onLine(
      canvas,
      text,
      xPct,
      lineYPct,
      wPct,
      imgW,
      imgH,
      fontSize,
      align: TextAlign.center,
    );
  }

  /// WinForms ToPngBytesWithPrinterSafeBottom.
  static Future<Uint8List> _printerSafeBottom(
    ui.Image composed,
    double bottomPadPct,
  ) async {
    final w = composed.width.toDouble();
    final h = composed.height.toDouble();
    final recorder = ui.PictureRecorder();
    final canvas = Canvas(recorder);
    canvas.drawRect(Rect.fromLTWH(0, 0, w, h), Paint()..color = Colors.white);
    final pad = h * bottomPadPct;
    final drawH = h - pad;
    final drawW = w * (drawH / h);
    final left = (w - drawW) * 0.5;
    paintImage(
      canvas: canvas,
      rect: Rect.fromLTWH(left, 0, drawW, drawH),
      image: composed,
      fit: BoxFit.fill,
      filterQuality: FilterQuality.high,
    );
    final picture = recorder.endRecording();
    final finalImg = await picture.toImage(composed.width, composed.height);
    picture.dispose();
    try {
      final bytes =
          await finalImg.toByteData(format: ui.ImageByteFormat.png);
      return bytes!.buffer.asUint8List();
    } finally {
      finalImg.dispose();
    }
  }
}
