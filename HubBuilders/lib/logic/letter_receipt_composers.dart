import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

class AllotmentLetterData {
  const AllotmentLetterData({
    required this.date,
    required this.name,
    required this.father,
    required this.membershipNo,
    required this.plotNo,
    required this.area,
    required this.location,
    required this.nic,
  });

  final String date;
  final String name;
  final String father;
  final String membershipNo;
  final String plotNo;
  final String area;
  final String location;
  final String nic;
}

/// WinForms Temporary Allotment Letter compose seats.
abstract final class AllotmentLetterComposer {
  static const frontAsset = 'assets/reports/TemporaryAllotmentLetterFront.png';
  static const backAsset = 'assets/reports/TemporaryAllotmentLetterBack.png';
  static const ink = Color.fromARGB(255, 25, 18, 10);

  static Future<({Uint8List front, Uint8List back})> compose({
    required AllotmentLetterData data,
    required bool blankOverlay,
  }) async {
    final frontTpl = await _load(frontAsset);
    final backTpl = await _load(backAsset);
    try {
      final front = await _page(
        tpl: frontTpl,
        blank: blankOverlay,
        paint: (c, w, h) {
          final fs = (h * 0.0145).clamp(28.0, 72.0);
          final fsArea = (h * 0.0175).clamp(34.0, 80.0);
          _line(c, data.date, 0.10, 0.270, 0.18, w, h, fs);
          _line(c, data.name, 0.24, 0.325, 0.26, w, h, fs);
          _line(c, data.father, 0.62, 0.360, 0.18, w, h, fs);
          _line(c, data.membershipNo, 0.18, 0.360, 0.30, w, h, fs);
          _line(c, data.plotNo, 0.70, 0.395, 0.11, w, h, fs);
          _line(c, data.area, 0.50, 0.395, 0.14, w, h, fsArea);
          _line(c, data.location, 0.52, 0.438, 0.22, w, h, fs);
        },
      );
      final back = await _page(
        tpl: backTpl,
        blank: blankOverlay,
        paint: (c, w, h) {
          final fs = (h * 0.0145).clamp(28.0, 72.0);
          _line(c, data.nic, 0.18, 0.515, 0.48, w, h, fs);
        },
      );
      return (front: front, back: back);
    } finally {
      frontTpl.dispose();
      backTpl.dispose();
    }
  }

  static Future<ui.Image> _load(String asset) async {
    final data = await rootBundle.load(asset);
    final codec = await ui.instantiateImageCodec(data.buffer.asUint8List());
    final frame = await codec.getNextFrame();
    return frame.image;
  }

  static Future<Uint8List> _page({
    required ui.Image tpl,
    required bool blank,
    required void Function(Canvas c, double w, double h) paint,
  }) async {
    final w = tpl.width.toDouble();
    final h = tpl.height.toDouble();
    final recorder = ui.PictureRecorder();
    final canvas = Canvas(recorder);
    if (blank) {
      canvas.drawRect(Rect.fromLTWH(0, 0, w, h), Paint()..color = Colors.white);
    } else {
      canvas.drawImage(tpl, Offset.zero, Paint());
    }
    paint(canvas, w, h);
    final picture = recorder.endRecording();
    final img = await picture.toImage(tpl.width, tpl.height);
    picture.dispose();
    try {
      final bytes = await img.toByteData(format: ui.ImageByteFormat.png);
      return bytes!.buffer.asUint8List();
    } finally {
      img.dispose();
    }
  }

  static void _line(
    Canvas canvas,
    String text,
    double xPct,
    double lineYPct,
    double wPct,
    double imgW,
    double imgH,
    double fontSize,
  ) {
    final t = text.trim();
    if (t.isEmpty) return;
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
          ),
        ),
        textAlign: TextAlign.center,
        textDirection: TextDirection.ltr,
        maxLines: 1,
      )..layout(maxWidth: maxW * 4);
      if (tp.width <= maxW || size <= 14) break;
      size -= 0.5;
    }
    final boxH = (tp.height * 1.15).clamp(imgH * 0.018, imgH);
    var top = lineYPct * imgH - boxH;
    if (top < 0) top = 0;
    tp.layout(maxWidth: maxW);
    final dx = xPct * imgW + (maxW - tp.width) / 2;
    tp.paint(canvas, Offset(dx, top + (boxH - tp.height)));
    tp.dispose();
  }
}

class LasbelaReceiptData {
  const LasbelaReceiptData({
    required this.receiptNo,
    required this.receiptDate,
    required this.personName,
    required this.father,
    required this.receivedAmount,
    required this.amountWords,
    required this.paymentMode,
    required this.paymentDate,
    required this.plotNo,
    required this.plotSize,
    required this.block,
    required this.category,
    required this.currentAmount,
    required this.balanceAmount,
  });

  final String receiptNo;
  final String receiptDate;
  final String personName;
  final String father;
  final String receivedAmount;
  final String amountWords;
  final String paymentMode;
  final String paymentDate;
  final String plotNo;
  final String plotSize;
  final String block;
  final String category;
  final String currentAmount;
  final String balanceAmount;
}

/// WinForms Lasbela dual A4 portrait receipt compose.
abstract final class LasbelaReceiptComposer {
  static const villageAsset = 'assets/reports/LasbelaVillageReceipt.png';
  static const marketAsset = 'assets/reports/LasbelaMarketBhawaniReceipt.png';
  static const a4W = 1654;
  static const a4H = 2339;
  static const ink = Color.fromARGB(255, 15, 20, 90);

  static Future<Uint8List> compose({
    required LasbelaReceiptData data,
    required bool marketBhawani,
  }) async {
    final asset = marketBhawani ? marketAsset : villageAsset;
    final tpl = await AllotmentLetterComposer._load(asset);
    try {
      final one = await _composeOne(tpl, data, marketBhawani);
      return _dualPortrait(one);
    } finally {
      tpl.dispose();
    }
  }

  static Future<ui.Image> _composeOne(
    ui.Image tpl,
    LasbelaReceiptData d,
    bool market,
  ) async {
    final w = tpl.width.toDouble();
    final h = tpl.height.toDouble();
    final recorder = ui.PictureRecorder();
    final canvas = Canvas(recorder);
    canvas.drawImage(tpl, Offset.zero, Paint());
    final fs = (h * 0.028).clamp(18.0, 48.0) + 4; // match WinForms +4 font bump; seats unchanged

    void seat(
      String text,
      double x,
      double y,
      double ww, {
      bool rtl = false,
      TextAlign align = TextAlign.center,
    }) {
      _draw(canvas, text, x, y, ww, w, h, fs, rtl: rtl, align: align);
    }

    if (market) {
      // Exact WinForms frmLasbelaMarketBhawaniReceipt seats.
      seat(d.receiptDate, 0.04, 0.293, 0.18);
      seat(d.receiptNo, 0.72, 0.293, 0.17);
      seat(d.personName, 0.34, 0.368, 0.24, rtl: true);
      seat(d.father, 0.44, 0.438, 0.22, rtl: true);
      seat(d.amountWords, 0.20, 0.506, 0.32, rtl: true, align: TextAlign.right);
      seat(d.receivedAmount, 0.70, 0.506, 0.22);
      seat(d.paymentDate, 0.05, 0.584, 0.20);
      seat(d.paymentMode, 0.34, 0.584, 0.28, rtl: true);
      seat(d.category, 0.06, 0.663, 0.14);
      seat(d.block, 0.30, 0.663, 0.14);
      seat(d.plotSize, 0.50, 0.663, 0.14);
      seat(d.plotNo, 0.76, 0.663, 0.15);
      seat(d.balanceAmount, 0.40, 0.750, 0.18);
      seat(d.currentAmount, 0.74, 0.750, 0.12);
    } else {
      seat(d.receiptDate, 0.04, 0.318, 0.18);
      seat(d.receiptNo, 0.72, 0.306, 0.17);
      seat(d.personName, 0.34, 0.395, 0.24, rtl: true);
      seat(d.father, 0.44, 0.466, 0.22, rtl: true);
      seat(d.amountWords, 0.20, 0.541, 0.32, rtl: true, align: TextAlign.right);
      seat(d.receivedAmount, 0.68, 0.541, 0.18);
      seat(d.paymentDate, 0.05, 0.627, 0.20);
      seat(d.paymentMode, 0.34, 0.627, 0.28, rtl: true);
      seat(d.category, 0.06, 0.708, 0.14);
      seat(d.block, 0.30, 0.708, 0.14);
      seat(d.plotSize, 0.50, 0.708, 0.14);
      seat(d.plotNo, 0.70, 0.708, 0.14);
      seat(d.balanceAmount, 0.40, 0.800, 0.18);
      seat(d.currentAmount, 0.68, 0.800, 0.18);
    }

    final picture = recorder.endRecording();
    final img = await picture.toImage(tpl.width, tpl.height);
    picture.dispose();
    return img;
  }

  static Future<Uint8List> _dualPortrait(ui.Image one) async {
    final recorder = ui.PictureRecorder();
    final canvas = Canvas(recorder);
    canvas.drawRect(
      Rect.fromLTWH(0, 0, a4W.toDouble(), a4H.toDouble()),
      Paint()..color = Colors.white,
    );
    final half = a4H / 2.0;
    final scale = (half / one.height).clamp(0.0, a4W / one.width);
    final dw = one.width * scale;
    final dh = one.height * scale;
    final left = (a4W - dw) / 2;
    paintImage(
      canvas: canvas,
      rect: Rect.fromLTWH(left, 8, dw, dh),
      image: one,
      fit: BoxFit.fill,
    );
    paintImage(
      canvas: canvas,
      rect: Rect.fromLTWH(left, half + 8, dw, dh),
      image: one,
      fit: BoxFit.fill,
    );
    final paint = Paint()
      ..color = Colors.grey
      ..strokeWidth = 2
      ..style = PaintingStyle.stroke;
    const dash = 12.0;
    var x = 40.0;
    while (x < a4W - 40) {
      canvas.drawLine(Offset(x, half), Offset(x + dash, half), paint);
      x += dash * 2;
    }
    final picture = recorder.endRecording();
    final img = await picture.toImage(a4W, a4H);
    picture.dispose();
    one.dispose();
    try {
      final bytes = await img.toByteData(format: ui.ImageByteFormat.png);
      return bytes!.buffer.asUint8List();
    } finally {
      img.dispose();
    }
  }

  static void _draw(
    Canvas canvas,
    String text,
    double xPct,
    double lineYPct,
    double wPct,
    double imgW,
    double imgH,
    double fontSize, {
    bool rtl = false,
    TextAlign align = TextAlign.center,
  }) {
    final t = text.trim();
    if (t.isEmpty) return;
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
          ),
        ),
        textAlign: align,
        textDirection: rtl ? TextDirection.rtl : TextDirection.ltr,
        maxLines: 1,
      )..layout(maxWidth: maxW * 4);
      if (tp.width <= maxW || size <= 10) break;
      size -= 0.5;
    }
    final boxH = (tp.height * 1.15).clamp(imgH * 0.02, imgH);
    var top = lineYPct * imgH - boxH;
    if (top < 0) top = 0;
    tp.layout(maxWidth: maxW);
    double dx;
    if (align == TextAlign.right || align == TextAlign.end) {
      dx = xPct * imgW + (maxW - tp.width);
    } else {
      dx = xPct * imgW + (maxW - tp.width) / 2;
    }
    tp.paint(canvas, Offset(dx, top + (boxH - tp.height)));
    tp.dispose();
  }
}
