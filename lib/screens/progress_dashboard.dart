import 'package:flutter/material.dart';
import 'package:cloud_firestore/cloud_firestore.dart';
import '../services/user_progress_service.dart';

class ProgressDashboard extends StatelessWidget {
  final _progressService = UserProgressService();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Reading Progress')),
      body: StreamBuilder<QuerySnapshot>(
        stream: _progressService.getUserProgress(),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return Center(child: CircularProgressIndicator());
          }
          if (!snapshot.hasData || snapshot.data!.docs.isEmpty) {
            return Center(child: Text('No reading progress found.'));
          }
          final docs = snapshot.data!.docs;
          return ListView.builder(
            itemCount: docs.length,
            itemBuilder: (context, index) {
              final data = docs[index].data() as Map<String, dynamic>;
              return ListTile(
                title: Text('${data['book']} ${data['chapter']}:${data['verse']}'),
                subtitle: data['timestamp'] != null
                    ? Text('On ${data['timestamp'].toDate()}')
                    : Text('Time not recorded'),
              );
            },
          );
        },
      ),
    );
  }
}