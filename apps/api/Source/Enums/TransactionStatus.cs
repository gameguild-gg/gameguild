using System.ComponentModel;


namespace GameGuild.Common;

public enum TransactionStatus {
  [Description("Transaction has been initiated but not completed")]
  Pending,

  [Description("Transaction is being processed by payment provider")]
  Processing,

  [Description("Transaction has been successfully completed")]
  Completed,

  [Description("Transaction failed to process")]
  Failed,

  [Description("Transaction was completed but has been refunded")]
  Refunded,

  [Description("Transaction was cancelled before processing")]
  Cancelled,
}
