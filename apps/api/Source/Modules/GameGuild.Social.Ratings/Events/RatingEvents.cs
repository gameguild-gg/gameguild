namespace GameGuild.Social.Ratings;

/// <summary>
/// Domain events for the ratings system
/// </summary>
/// 
public sealed record RatingCreatedEvent(
    Guid RatingId,
    Guid UserId,
    Guid EntityId,
    string EntityType,
    int Value,
    bool HasReview
);

public sealed record RatingUpdatedEvent(
    Guid RatingId,
    Guid UserId,
    Guid EntityId,
    string EntityType,
    int OldValue,
    int NewValue
);

public sealed record RatingDeletedEvent(
    Guid RatingId,
    Guid UserId,
    Guid EntityId,
    string EntityType
);

public sealed record RatingReportedEvent(
    Guid RatingId,
    Guid ReportedByUserId,
    string Reason,
    int TotalReports
);

public sealed record RatingModerationStatusChangedEvent(
    Guid RatingId,
    RatingModerationStatus OldStatus,
    RatingModerationStatus NewStatus
);

public sealed record RatingSummaryRecalculatedEvent(
    Guid EntityId,
    string EntityType,
    decimal AverageRating,
    int TotalRatings
);

public sealed record RatingHelpfulVoteEvent(
    Guid RatingId,
    Guid VoterUserId,
    bool IsHelpful
);
