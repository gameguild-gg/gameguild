namespace GameGuild.Modules.Programs;

/// <summary> How the user was enrolled </summary>
public enum EnrollmentSource {
  Manual = 0,

  ProductPurchase = 1,

  FreeAccess = 2,

  AdminAction = 3,

  BulkEnrollment = 4,

  Invitation = 5,
}