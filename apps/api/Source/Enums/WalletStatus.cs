using System.ComponentModel;


namespace GameGuild.Common;

public enum WalletStatus {
  [Description("Wallet is operational and can process transactions")]
  Active,

  [Description("Wallet temporarily suspended, cannot process transactions")]
  Frozen,

  [Description("Wallet permanently deactivated")]
  Closed,
}
