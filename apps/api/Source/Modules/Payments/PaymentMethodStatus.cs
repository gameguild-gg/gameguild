using System.ComponentModel;


namespace GameGuild.Common;

public enum PaymentMethodStatus {
  [Description("Payment method is valid and can be used")]
  Active,

  [Description("Payment method temporarily disabled")]
  Inactive,

  [Description("Payment method has expired (e.g., expired card)")]
  Expired,

  [Description("Payment method has been deleted by the user")]
  Removed,
}
