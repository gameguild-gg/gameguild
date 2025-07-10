using System.ComponentModel;


namespace GameGuild.Common.Enums;

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