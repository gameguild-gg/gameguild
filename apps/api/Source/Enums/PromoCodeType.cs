using System.ComponentModel;


namespace GameGuild.Common;

public enum PromoCodeType {
  [Description("Applies a percentage discount to the total price")]
  PercentageOff,

  [Description("Deducts a specific amount from the total price")]
  FixedAmountOff,

  [Description("Purchase one product and receive another free")]
  BuyOneGetOne,

  [Description("No charge for the first month of a subscription")]
  FirstMonthFree,
}
