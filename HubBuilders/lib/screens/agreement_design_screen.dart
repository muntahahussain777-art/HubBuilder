import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../widgets/ui_bits.dart';

class AgreementDesignScreen extends StatelessWidget {
  const AgreementDesignScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        const SectionHeader(
          title: 'Agreement Design',
          subtitle: 'File → Agreement Design (Form Filler UI)',
        ),
        const SizedBox(height: 14),
        SoftPanel(
          padding: EdgeInsets.zero,
          child: AspectRatio(
            aspectRatio: 3 / 4,
            child: Container(
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(18),
                gradient: const LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    Color(0xFF203048),
                    Color(0xFF152433),
                  ],
                ),
              ),
              child: Stack(
                children: [
                  Positioned.fill(
                    child: CustomPaint(painter: _PaperGuidesPainter()),
                  ),
                  Padding(
                    padding: const EdgeInsets.all(22),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'FORM FILLER',
                          style: Theme.of(context).textTheme.labelLarge?.copyWith(
                                color: AppColors.tealBright,
                                letterSpacing: 1.6,
                              ),
                        ),
                        const SizedBox(height: 10),
                        Text(
                          'Agreement canvas',
                          style: Theme.of(context).textTheme.headlineMedium,
                        ),
                        const SizedBox(height: 8),
                        Text(
                          'Upload template · place text / ✔ / photos · save layout.',
                          style: Theme.of(context).textTheme.bodyMedium,
                        ),
                        const Spacer(),
                        SoftPanel(
                          padding: const EdgeInsets.all(12),
                          child: Row(
                            children: [
                              const Icon(Icons.touch_app_outlined,
                                  color: AppColors.gold),
                              const SizedBox(width: 10),
                              Expanded(
                                child: Text(
                                  'Dummy drag seats ready for template sync',
                                  style: Theme.of(context).textTheme.bodyMedium,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
        const SizedBox(height: 14),
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Dummy: Load Layout (.xml)')),
                  );
                },
                icon: const Icon(Icons.folder_open),
                label: const Text('Load Layout'),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: ElevatedButton.icon(
                onPressed: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Dummy: Save Layout (.xml)')),
                  );
                },
                icon: const Icon(Icons.save_outlined),
                label: const Text('Save Layout'),
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _PaperGuidesPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = AppColors.line.withValues(alpha: 0.55)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1;
    final rect = RRect.fromRectAndRadius(
      Rect.fromLTWH(18, 18, size.width - 36, size.height - 36),
      const Radius.circular(12),
    );
    canvas.drawRRect(rect, paint);
    for (var i = 0; i < 6; i++) {
      final y = 90.0 + i * 46;
      canvas.drawLine(Offset(40, y), Offset(size.width - 40, y), paint);
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
