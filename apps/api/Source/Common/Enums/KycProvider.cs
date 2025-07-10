using System.ComponentModel;


namespace GameGuild.Common;

public enum KycProvider {
  [Description("SumSub KYC verification provider")]
  Sumsub,

  [Description("Shufti Pro KYC verification provider")]
  Shufti,

  [Description("Onfido KYC verification provider")]
  Onfido,

  [Description("Jumio KYC verification provider")]
  Jumio,

  [Description("Custom or internal KYC verification process")]
  Custom,
}
