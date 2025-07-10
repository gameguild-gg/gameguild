using System.ComponentModel;


namespace GameGuild.Common;

public enum CertificateTagRelationshipType {
  [Description("Tag is required for this certificate")]
  Required,

  [Description("Tag is optional but recommended for this certificate")]
  Optional,

  [Description("Tag indicates skill mastery demonstrated by this certificate")]
  Demonstrates,
}
