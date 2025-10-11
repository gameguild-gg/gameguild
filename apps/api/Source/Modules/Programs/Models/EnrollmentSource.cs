using System.ComponentModel;

namespace GameGuild.Source.Modules.Programs.Models;

/// <summary>
/// Represents how a user was enrolled in a program
/// </summary>
public enum EnrollmentSource {
  /// <summary>Manual enrollment</summary>
  [Description("Manual enrollment")]
  Manual = 0,

  /// <summary>Enrolled through product purchase</summary>
  [Description("Product purchase")]
  ProductPurchase = 1,

  /// <summary>Free access enrollment</summary>
  [Description("Free access")]
  FreeAccess = 2,

  /// <summary>Administrative action</summary>
  [Description("Admin action")]
  AdminAction = 3,

  /// <summary>Bulk enrollment operation</summary>
  [Description("Bulk enrollment")]
  BulkEnrollment = 4,

  /// <summary>Invitation-based enrollment</summary>
  [Description("Invitation")]
  Invitation = 5,
}
