using System.ComponentModel;


namespace GameGuild.Common;

public enum ProgressStatus {
  [Description("User has not started this content yet")]
  NotStarted = 0,

  [Description("User has started but not completed this content")]
  InProgress = 1,

  [Description("User has successfully completed this content")]
  Completed = 2,

  // alias for Completed
  [Description("User has submitted their work for evaluation, alias for Completed")]
  Submitted = 2,

  [Description("User has opted to skip this optional content")]
  Skipped = 3,
}
