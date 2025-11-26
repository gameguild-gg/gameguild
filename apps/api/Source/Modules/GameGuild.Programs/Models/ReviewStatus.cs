namespace GameGuild.Modules.Programs;

/// <summary>
/// Status of a review
/// </summary>
public enum ReviewStatus {
    Pending = 0,

    InProgress = 1,

    Submitted = 2,

    Approved = 3,

    Rejected = 4,

    RequiresRevision = 5,

    Escalated = 6,
}