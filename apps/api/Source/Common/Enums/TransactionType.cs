using System.ComponentModel;


namespace GameGuild.Common;

public enum TransactionType {
  [Description("Payment for acquiring a product")]
  Purchase,

  [Description("Return of funds to the customer")]
  Refund,

  [Description("Removal of funds from the platform")]
  Withdrawal,

  [Description("Addition of funds to the platform")]
  Deposit,

  [Description("Movement of funds between wallets")]
  Transfer,

  [Description("Platform or processing fees")]
  Fee,

  [Description("Manual correction to balance")]
  Adjustment,
}
