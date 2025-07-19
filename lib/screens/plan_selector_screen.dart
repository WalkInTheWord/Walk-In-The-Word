import 'package:flutter/material.dart';

class PlanSelectorScreen extends StatelessWidget {
  final plans = ['30-Day Psalms', '6-Month Bible', '1-Year Chronological'];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Reading Plans')),
      body: ListView.builder(
        itemCount: plans.length,
        itemBuilder: (context, index) {
          return ListTile(
            title: Text(plans[index]),
            trailing: Icon(Icons.chevron_right),
            onTap: () {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text('Selected: ${plans[index]}')),
              );
            },
          );
        },
      ),
    );
  }
}