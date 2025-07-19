import 'package:flutter/material.dart';
import '../services/auth_service.dart';

class LoginScreen extends StatelessWidget {
  final AuthService _auth = AuthService();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Login')),
      body: Center(
        child: ElevatedButton(
          onPressed: () async {
            final user = await _auth.signInAnonymously();
            if (user != null) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text('Signed in as ${user.uid}')),
              );
            }
          },
          child: Text('Continue as Guest'),
        ),
      ),
    );
  }
}