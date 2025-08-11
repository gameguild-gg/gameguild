using System.ComponentModel;


namespace GameGuild.Common;

public enum SubscriptionType {
  [Description("Subscription billed on a monthly basis")]
  Monthly,

  [Description("Subscription billed every three months")]
  Quarterly,

  [Description("Subscription billed once per year")]
  Annual,

  [Description("One-time payment for permanent access")]
  Lifetime,
}
