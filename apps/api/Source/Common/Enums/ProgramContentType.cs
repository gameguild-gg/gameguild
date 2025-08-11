using System.ComponentModel;


namespace GameGuild.Common;

public enum ProgramContentType {
  [Description("Instructional content page")]
  Page,

  [Description("General assignment where students submit work for evaluation")]
  Assignment,

  [Description("A sequence of questions with expected answers, similar to a quiz or test")]
  Questionnaire,

  [Description("Discussion forum for collaborative learning and sharing ideas")]
  Discussion,

  [Description("Programming assignment requiring code submission")]
  Code,

  [Description("Competition-style activity with rankings or achievements")]
  Challenge,

  [Description("Student reflections on learning or experiences")]
  Reflection,

  [Description("Data collection activity without grading")]
  Survey,
}
