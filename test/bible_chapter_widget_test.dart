import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show rootBundle;
import 'package:flutter_test/flutter_test.dart';
import 'package:walk_in_the_word/widgets/bible_chapter_widget.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('Genesis 1 asset has a valid sequential verse structure', () async {
    final String source = await rootBundle.loadString(
      'assets/bible/genesis_1.json',
    );
    final Map<String, dynamic> chapter =
        jsonDecode(source) as Map<String, dynamic>;
    final List<dynamic> verses = chapter['verses'] as List<dynamic>;

    expect(chapter['book'], 'Genesis');
    expect(chapter['chapter'], 1);
    expect(verses, isNotEmpty);

    for (int index = 0; index < verses.length; index++) {
      final Map<String, dynamic> verse =
          verses[index] as Map<String, dynamic>;
      expect(verse['verse'], index + 1);
      expect(verse['text'], isA<String>());
      expect((verse['text'] as String).trim(), isNotEmpty);
    }
  });

  testWidgets('Bible chapter renders loaded verse data', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(MaterialApp(home: BibleChapterWidget()));
    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();

    expect(find.text('Genesis 1'), findsOneWidget);
    expect(find.byType(ListTile), findsNWidgets(5));
    expect(find.text('1'), findsOneWidget);
    expect(find.text('5'), findsOneWidget);
  });
}
