import 'package:flutter_test/flutter_test.dart';

import 'package:walk_in_the_word/main.dart';

void main() {
  testWidgets('Walk in the Word app loads', (WidgetTester tester) async {
    await tester.pumpWidget(WalkInTheWordApp());

    expect(find.text('Walk in the Word'), findsOneWidget);
    expect(find.text('Welcome to Walk in the Word Bible App'), findsOneWidget);
  });
}
