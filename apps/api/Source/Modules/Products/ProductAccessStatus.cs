using System.ComponentModel;


namespace GameGuild.Common;

public enum ProductAccessStatus {
  [Description("User has full access to the product")]
  Active,

  [Description("Access period has ended")]
  Expired,

  [Description("Access manually removed by admin")]
  Revoked,

  [Description("Temporary hold on access that may be restored")]
  Suspended,
}
