using System.ComponentModel;


namespace GameGuild.Common;

public enum VerificationMethod {
  [Description("Certificate can be verified using a unique code")]
  Code,

  [Description("Certificate is verified through blockchain anchoring")]
  Blockchain,

  [Description("Certificate uses both code and blockchain verification")]
  Both,
}
