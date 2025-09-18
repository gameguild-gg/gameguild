using System.ComponentModel;

namespace GameGuild.Source.Modules.Programs.Models;

/// <summary>
/// Represents the completion status for program enrollment
/// </summary>
public enum CompletionStatus {
  /// <summary>Not yet started</summary>
  [Description("Not yet started")]
  NotStarted = 0,

  /// <summary>Currently in progress</summary>
  [Description("In progress")]
  InProgress = 1,

  /// <summary>Completed successfully</summary>
  [Description("Completed")]
  Completed = 2,

  /// <summary>Completed with certificate issued</summary>
  [Description("Completed with certificate")]
  CompletedWithCertificate = 3,

  /// <summary>Failed to complete</summary>
  [Description("Failed")]
  Failed = 4,

  /// <summary>Dropped out</summary>
  [Description("Dropped")]
  Dropped = 5,
}
