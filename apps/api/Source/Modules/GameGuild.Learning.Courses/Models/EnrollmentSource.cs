using GameGuild.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Identity.Users;
﻿using System.ComponentModel;

namespace GameGuild.Learning.Courses;

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
