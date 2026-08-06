import 'package:flutter/material.dart';

import '../widgets/ui_bits.dart';

class InfoScreen extends StatelessWidget {
  const InfoScreen({super.key, required this.title, required this.body});

  final String title;
  final String body;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
      children: [
        SectionHeader(title: title),
        const SizedBox(height: 14),
        SoftPanel(
          child: Text(body, style: Theme.of(context).textTheme.bodyLarge),
        ),
      ],
    );
  }
}
