using System.ComponentModel;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Content types defining the nature and interaction model of program materials
/// </summary>
/// <remarks>
/// Content types determine how learners interact with materials and what
/// features are available (submissions, grading, discussions, etc.).
/// Each type has specific UI rendering and business logic requirements.
/// </remarks>
public enum ProgramContentType {
  [Description("Instructional lesson content")] Lesson = 0,

  [Description("Legacy instructional content page. Professor-facing APIs normalize this to Lesson.")] Page = 1,

  [Description("General assignment where students submit work for evaluation")] Assignment = 2,

  [Description("A sequence of questions with expected answers, similar to a quiz or test")] Questionnaire = 3,

  [Description("Discussion forum for collaborative learning and sharing ideas")] Discussion = 4,

  [Description("Programming assignment requiring code submission")] Code = 5,

  [Description("Legacy competition-style activity. Professor-facing APIs normalize this to Assignment.")] Challenge = 6,

  [Description("Student reflections on learning or experiences")] Reflection = 7,

  [Description("Data collection activity without grading")] Survey = 8,

  [Description("Project activity used for milestone or final-project delivery")] Project = 9,
}
