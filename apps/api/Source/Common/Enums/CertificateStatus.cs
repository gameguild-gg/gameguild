using System.ComponentModel;


namespace GameGuild.Common;

public enum CertificateStatus {
  [Description("Certificate is valid and can be verified")]
  Active,

  [Description("Certificate has reached its expiration date")]
  Expired,

  [Description("Certificate has been manually invalidated")]
  Revoked,

  [Description("Certificate is in the process of being issued")]
  Pending,
}
