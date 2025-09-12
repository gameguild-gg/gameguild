using System.ComponentModel;


namespace GameGuild.Common;

public enum ProgramDifficulty {
  [Description("Suitable for complete beginners")]
  Beginner,

  [Description("Requires basic understanding of the subject")]
  Intermediate,

  [Description("Requires significant prior knowledge and experience")]
  Advanced,

  [Description("Requires expert-level knowledge")]
  Expert,
}
