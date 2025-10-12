using System.ComponentModel;


namespace GameGuild;

/// <summary>
/// Represents the various states of a subscription throughout its lifecycle
/// </summary>
/// <remarks>
/// These statuses track the subscription from initial creation through cancellation,
/// including payment-related states and administrative actions.
/// </remarks>
public enum SubscriptionStatus {
  /// <summary>
  /// Subscription is currently valid and paid up, providing full access to services
  /// </summary>
  [Description("Subscription is currently valid and paid up")]
  Active,

  /// <summary>
  /// In free trial period before regular billing begins, providing full access
  /// </summary>
  [Description("In free trial period before regular billing begins")]
  Trialing,

  /// <summary>
  /// Payment failed but subscription still active, pending retry attempts
  /// </summary>
  [Description("Payment failed but subscription still active, pending retry")]
  PastDue,

  /// <summary>
  /// User has canceled the subscription, may remain active until period end
  /// </summary>
  [Description("User has canceled the subscription")]
  Canceled,

  /// <summary>
  /// Alias for Canceled to support both spellings (British English variant)
  /// </summary>
  [Description("User has cancelled the subscription")]
  Cancelled = Canceled,

  /// <summary>
  /// Initial payment failed, subscription not fully activated, access may be limited
  /// </summary>
  [Description("Initial payment failed, subscription not fully activated")]
  Incomplete,

  /// <summary>
  /// Initial payment failed and the trial period expired, no access provided
  /// </summary>
  [Description("Initial payment failed and the trial period expired")]
  IncompleteExpired,

  /// <summary>
  /// Payment failed after retries, subscription suspended, access revoked
  /// </summary>
  [Description("Payment failed after retries, subscription suspended")]
  Unpaid,

  /// <summary>
  /// Subscription is pending activation, awaiting payment or administrative approval
  /// </summary>
  [Description("Subscription is pending activation")]
  PendingActivation,

  /// <summary>
  /// Subscription has been suspended by administrative action, access temporarily revoked
  /// </summary>
  [Description("Subscription has been suspended")]
  Suspended,
}
