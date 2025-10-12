using System.ComponentModel;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Learning difficulty levels for appropriate program selection and progression
/// </summary>
/// <remarks>
/// Difficulty levels help learners choose appropriate programs based on their
/// current knowledge and experience. This enables proper learning progression
/// and prevents frustration from content that is too advanced or too basic.
/// </remarks>
public enum ProgramDifficulty {
  [Description("Suitable for complete beginners")] Beginner,

  [Description("Requires basic understanding of the subject")] Intermediate,

  [Description("Requires significant prior knowledge and experience")] Advanced,

  [Description("Requires expert-level knowledge")] Expert,
}
