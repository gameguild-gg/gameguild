using System.ComponentModel;


namespace GameGuild.Common;

public enum TagType {
  [Description("Tag represents a learnable or teachable skill")]
  Skill,

  [Description("General subject matter or area of knowledge")]
  Topic,

  [Description("Specific technology, software, framework, or tool")]
  Technology,

  [Description("Indicates content difficulty level")]
  Difficulty,

  [Description("High-level grouping of content")]
  Category,

  [Description("Specific industry or sector")]
  Industry,

  [Description("Professional or industry certification")]
  Certification,
}
