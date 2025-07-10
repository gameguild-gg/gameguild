using System.ComponentModel;


namespace GameGuild.Common;

public enum ProgramRoleType {
  [Description("Standard learner role with access to content and ability to submit assignments")]
  Student,

  [Description("Can create content, grade submissions, and manage the program")]
  Instructor,

  [Description("Can edit program content but cannot grade or manage users")]
  Editor,

  [Description("Full control over the program, including user management and settings")]
  Administrator,

  [Description("Can grade submissions and assist students but with limited content editing abilities")]
  TeachingAssistant,
}
