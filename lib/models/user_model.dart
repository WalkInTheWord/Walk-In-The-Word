class UserModel {
  final String uid;
  final String email;
  final bool premium;

  UserModel({required this.uid, required this.email, this.premium = false});
}