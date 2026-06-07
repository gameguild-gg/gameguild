namespace GameGuild.Identity.Authentication;

[Flags]
public enum ComplianceFlags { None = 0, Aml = 1, Kyc = 2, Pep = 4, Sanctions = 8 }
