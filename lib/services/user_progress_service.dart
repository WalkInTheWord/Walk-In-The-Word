import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:firebase_auth/firebase_auth.dart';

class UserProgressService {
  final FirebaseFirestore _db = FirebaseFirestore.instance;
  final FirebaseAuth _auth = FirebaseAuth.instance;

  Future<void> saveProgress(String book, int chapter, int verse) async {
    final uid = _auth.currentUser?.uid;
    if (uid == null) return;

    await _db
        .collection('users')
        .doc(uid)
        .collection('progress')
        .add({
          'book': book,
          'chapter': chapter,
          'verse': verse,
          'timestamp': FieldValue.serverTimestamp(),
        });
  }

  Stream<QuerySnapshot> getUserProgress() {
    final uid = _auth.currentUser?.uid;
    return _db.collection('users').doc(uid).collection('progress').snapshots();
  }
}