namespace GameGuild.Modules.Programs;

/// <summary> Completion status enumeration </summary>
public enum CompletionStatus {
  NotStarted = 0,

  InProgress = 1,

  Completed = 2,

  CompletedWithCertificate = 3,

  Failed = 4,

  Dropped = 5,
}