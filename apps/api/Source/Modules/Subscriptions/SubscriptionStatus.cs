using System.ComponentModel;


namespace GameGuild;

public enum SubscriptionStatus {
  [Description("Subscription is currently valid and paid up")] Active,

  [Description("In free trial period before regular billing begins")] Trialing,

  [Description("Payment failed but subscription still active, pending retry")] PastDue,

  [Description("User has canceled the subscription")] Canceled,

  /// <summary> Alias for Canceled to support both spellings </summary>
  [Description("User has cancelled the subscription")]
  Cancelled = Canceled,

  [Description("Initial payment failed, subscription not fully activated")] Incomplete,

  [Description("Initial payment failed and the trial period expired")] IncompleteExpired,

  [Description("Payment failed after retries, subscription suspended")] Unpaid,

  [Description("Subscription is pending activation")] PendingActivation,

  [Description("Subscription has been suspended")] Suspended,
}
