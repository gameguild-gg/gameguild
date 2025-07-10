using System.ComponentModel;


namespace GameGuild.Common;

public enum SubscriptionStatus {
  [Description("Subscription is currently valid and paid up")]
  Active,

  [Description("In free trial period before regular billing begins")]
  Trialing,

  [Description("Payment failed but subscription still active, pending retry")]
  PastDue,

  [Description("User has canceled the subscription")]
  Canceled,

  [Description("Initial payment failed, subscription not fully activated")]
  Incomplete,

  [Description("Initial payment failed and the trial period expired")]
  IncompleteExpired,

  [Description("Payment failed after retries, subscription suspended")]
  Unpaid,
}
