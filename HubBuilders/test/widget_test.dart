import 'package:flutter_test/flutter_test.dart';
import 'package:hub_builders/main.dart';

void main() {
  testWidgets('HubBuilders app boots', (tester) async {
    await tester.pumpWidget(const HubBuildersApp());
    expect(find.text('HubBuilders'), findsWidgets);
  });
}
