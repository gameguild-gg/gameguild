using System.ComponentModel;

namespace GameGuild.Source.Modules.Programs.Models;

/// <summary>
/// Represents different methods for grading program content
/// </summary>
public enum GradingMethod {
  /// <summary>Graded manually by an instructor or teaching assistant</summary>
  [Description("Graded manually by an instructor or teaching assistant")]
  Instructor,

  /// <summary>Peer review-based grading by other students</summary>
  [Description("Peer review-based grading by other students")]
  Peer,

  /// <summary>Automated grading using AI algorithms</summary>
  [Description("Automated grading using AI algorithms")]
  Ai,

  /// <summary>Graded automatically using predefined test cases</summary>
  [Description("Graded automatically using predefined test cases")]
  AutomatedTests,
}
