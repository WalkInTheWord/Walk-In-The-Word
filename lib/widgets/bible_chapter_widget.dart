import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show rootBundle;

class BibleChapterWidget extends StatefulWidget {
  @override
  _BibleChapterWidgetState createState() => _BibleChapterWidgetState();
}

class _BibleChapterWidgetState extends State<BibleChapterWidget> {
  List<Map<String, dynamic>> _verses = [];

  @override
  void initState() {
    super.initState();
    loadChapter();
  }

  Future<void> loadChapter() async {
    final String response = await rootBundle.loadString('assets/bible/genesis_1.json');
    final data = json.decode(response) as Map<String, dynamic>;
    final verses = (data['verses'] as List<dynamic>)
        .cast<Map<String, dynamic>>();
    setState(() {
      _verses = verses;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Genesis 1')),
      body: ListView.builder(
        itemCount: _verses.length,
        itemBuilder: (context, index) {
          final verse = _verses[index];
          return ListTile(
            leading: Text(verse['verse'].toString()),
            title: Text(verse['text'] as String),
          );
        },
      ),
    );
  }
}