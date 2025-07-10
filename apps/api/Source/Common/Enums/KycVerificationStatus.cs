using System.ComponentModel;


namespace GameGuild.Common.Enums;

public enum KycVerificationStatus {
  [Description("Verification request has been submitted but not completed")]
  Pending,

  [Description("Provider is processing the verification")]
  InProgress,

  [Description("KYC verification passed")]
  Approved,

  [Description("KYC verification failed")]
  Rejected,

  [Description("Verification was approved but later suspended")]
  Suspended,

  [Description("Verification has expired and needs renewal")]
  Expired,
}