using System.ComponentModel;


namespace GameGuild.Common;

public enum GradingMethod {
  [Description("Graded manually by an instructor or teaching assistant")]
  Instructor,

  [Description("Peer review-based grading by other students")]
  Peer,

  [Description("Automated grading using AI algorithms")]
  Ai,

  [Description("Graded automatically using predefined test cases")]
  AutomatedTests,
}
