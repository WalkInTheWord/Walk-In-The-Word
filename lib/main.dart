import 'package:flutter/material.dart';

void main() => runApp(WalkInTheWordApp());

class WalkInTheWordApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Walk in the Word',
      theme: ThemeData(primarySwatch: Colors.blue),
      home: Scaffold(
        appBar: AppBar(title: Text('Walk in the Word')),
        body: Center(child: Text('Welcome to Walk in the Word Bible App')),
      ),
    );
  }
}