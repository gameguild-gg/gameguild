using System.ComponentModel;


namespace GameGuild;

public enum SubscriptionBillingInterval {
  [Description("Billing occurs daily")] Day,
  [Description("Billing occurs weekly")] Week,

  [Description("Billing occurs monthly")]
  Month,
  [Description("Billing occurs yearly")] Year,
}
