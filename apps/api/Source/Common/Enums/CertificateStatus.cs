using System.ComponentModel;


namespace GameGuild.Common.Enums;

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

public enum VerificationMethod {
  [Description("Certificate can be verified using a unique code")]
  Code,

  [Description("Certificate is verified through blockchain anchoring")]
  Blockchain,

  [Description("Certificate uses both code and blockchain verification")]
  Both,
}

public enum SkillProficiencyLevel {
  [Description("Basic awareness and understanding of concepts")]
  Awareness,

  [Description("Can perform basic tasks with guidance")]
  Novice,

  [Description("Can perform routine tasks independently")]
  Beginner,

  [Description("Can handle most tasks and some complex scenarios")]
  Intermediate,

  [Description("Can handle complex tasks and mentor others")]
  Advanced,

  [Description("Deep expertise, can innovate and teach")]
  Expert,

  [Description("Recognized authority, can define best practices")]
  Master,
}
