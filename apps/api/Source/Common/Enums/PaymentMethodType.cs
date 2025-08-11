using System.ComponentModel;


namespace GameGuild.Common;

public enum PaymentMethodType {
  [Description("Credit card payment method")]
  CreditCard,

  [Description("Debit card payment method")]
  DebitCard,

  [Description("Cryptocurrency wallet for blockchain payments")]
  CryptoWallet,

  [Description("Platform internal wallet balance")]
  WalletBalance,

  [Description("Direct bank transfer or wire payment")]
  BankTransfer,
}
