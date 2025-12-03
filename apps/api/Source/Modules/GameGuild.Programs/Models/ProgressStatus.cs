using GameGuild.SharedKernel.Enums;
namespace GameGuild.Modules.Programs.Models;

/// <summary>
/// Represents the progress status of a content item
/// </summary>
public enum ProgressStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Submitted = 3
}
