namespace GameGuild.Social.Ratings;

/// <summary>
/// Domain events for the ratings system
/// </summary>
/// 
public record RatingCreatedEvent(
    Guid RatingId,
    Guid UserId,
    Guid EntityId,
    string EntityType,
    int Value,
    bool HasReview
);

public record RatingUpdatedEvent(
    Guid RatingId,
    Guid UserId,
    Guid EntityId,
    string EntityType,
    int OldValue,
    int NewValue
);

public record RatingDeletedEvent(
    Guid RatingId,
    Guid UserId,
    Guid EntityId,
    string EntityType
);

public record RatingReportedEvent(
    Guid RatingId,
    Guid ReportedByUserId,
    string Reason,
    int TotalReports
);

public record RatingModerationStatusChangedEvent(
    Guid RatingId,
    RatingModerationStatus OldStatus,
    RatingModerationStatus NewStatus
);

public record RatingSummaryRecalculatedEvent(
    Guid EntityId,
    string EntityType,
    decimal AverageRating,
    int TotalRatings
);

public record RatingHelpfulVoteEvent(
    Guid RatingId,
    Guid VoterUserId,
    bool IsHelpful
);
