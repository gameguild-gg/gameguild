using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Ratings;

/// <summary>
/// Represents a polymorphic rating that can be attached to any entity type.
/// Supports star ratings (1-5) with optional review comments.
/// </summary>
[Table("ratings")]
[Index(nameof(EntityId), nameof(EntityType))]
[Index(nameof(UserId))]
[Index(nameof(EntityId), nameof(EntityType), nameof(UserId), IsUnique = true)]
[Index(nameof(Value))]
[Index(nameof(CreatedAt))]
public class Rating : EntityBase
{
    /// <summary>The user who provided the rating</summary>
    public Guid UserId { get; private set; }

    /// <summary>The ID of the entity being rated</summary>
    public Guid EntityId { get; private set; }

    /// <summary>The type of entity being rated (e.g., "Course", "Project", "Post", "Resource")</summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>The rating value (1-5 stars)</summary>
    [Range(1, 5)]
    public int Value { get; private set; }

    /// <summary>Optional review text</summary>
    [MaxLength(2000)]
    public string? ReviewText { get; private set; }

    /// <summary>Optional title for the review</summary>
    [MaxLength(200)]
    public string? ReviewTitle { get; private set; }

    /// <summary>Whether the rating is verified (user completed/purchased the item)</summary>
    public bool IsVerified { get; private set; }

    /// <summary>Number of users who found this review helpful</summary>
    public int HelpfulCount { get; private set; }

    /// <summary>Number of users who reported this review</summary>
    public int ReportCount { get; private set; }

    /// <summary>Moderation status for reviews</summary>
    public RatingModerationStatus ModerationStatus { get; private set; } = RatingModerationStatus.Approved;

    /// <summary>When the user edited the rating (if edited)</summary>
    public DateTime? EditedAt { get; private set; }

    private Rating() { } // EF Core

    public static Rating Create(
        Guid userId,
        Guid entityId,
        string entityType,
        int value,
        string? reviewText = null,
        string? reviewTitle = null,
        bool isVerified = false)
    {
        if (value < 1 || value > 5)
            throw new ArgumentOutOfRangeException(nameof(value), "Rating value must be between 1 and 5");

        return new Rating
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntityId = entityId,
            EntityType = entityType.Trim(),
            Value = value,
            ReviewText = reviewText?.Trim(),
            ReviewTitle = reviewTitle?.Trim(),
            IsVerified = isVerified,
            HelpfulCount = 0,
            ReportCount = 0,
            ModerationStatus = RatingModerationStatus.Approved
        };
    }

    public void Update(int value, string? reviewText = null, string? reviewTitle = null)
    {
        if (value < 1 || value > 5)
            throw new ArgumentOutOfRangeException(nameof(value), "Rating value must be between 1 and 5");

        Value = value;
        ReviewText = reviewText?.Trim();
        ReviewTitle = reviewTitle?.Trim();
        EditedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    public void MarkAsVerified() { IsVerified = true; UpdatedAt = SystemClock.UtcNow; }

    public void IncrementHelpful() { HelpfulCount++; UpdatedAt = SystemClock.UtcNow; }
    public void DecrementHelpful() { if (HelpfulCount > 0) HelpfulCount--; UpdatedAt = SystemClock.UtcNow; }

    public void IncrementReport() { ReportCount++; UpdatedAt = SystemClock.UtcNow; }

    public void SetModerationStatus(RatingModerationStatus status)
    {
        ModerationStatus = status;
        UpdatedAt = SystemClock.UtcNow;
    }
}

/// <summary>
/// Moderation status for ratings/reviews
/// </summary>
public enum RatingModerationStatus
{
    /// <summary>Pending review by moderator</summary>
    Pending = 0,

    /// <summary>Approved and visible</summary>
    Approved = 1,

    /// <summary>Rejected and hidden</summary>
    Rejected = 2,

    /// <summary>Flagged for review due to reports</summary>
    Flagged = 3
}

/// <summary>
/// Tracks whether a user found a rating helpful
/// </summary>
[Table("rating_helpful_votes")]
[Index(nameof(RatingId))]
[Index(nameof(UserId))]
[Index(nameof(RatingId), nameof(UserId), IsUnique = true)]
public class RatingHelpfulVote : EntityBase
{
    public Guid RatingId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsHelpful { get; private set; }

    private RatingHelpfulVote() { } // EF Core

    public static RatingHelpfulVote Create(Guid ratingId, Guid userId, bool isHelpful)
    {
        return new RatingHelpfulVote
        {
            Id = Guid.NewGuid(),
            RatingId = ratingId,
            UserId = userId,
            IsHelpful = isHelpful
        };
    }

    public void UpdateVote(bool isHelpful)
    {
        IsHelpful = isHelpful;
        UpdatedAt = SystemClock.UtcNow;
    }
}

/// <summary>
/// Aggregated rating statistics for an entity
/// </summary>
[Table("rating_summaries")]
[Index(nameof(EntityId), nameof(EntityType), IsUnique = true)]
[Index(nameof(AverageRating))]
[Index(nameof(TotalRatings))]
public class RatingSummary : EntityBase
{
    public Guid EntityId { get; private set; }

    [Required]
    [MaxLength(100)]
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Weighted average rating (1.0 - 5.0)</summary>
    [Column(TypeName = "decimal(3,2)")]
    public decimal AverageRating { get; private set; }

    /// <summary>Total number of ratings</summary>
    public int TotalRatings { get; private set; }

    /// <summary>Count of 1-star ratings</summary>
    public int OneStar { get; private set; }

    /// <summary>Count of 2-star ratings</summary>
    public int TwoStar { get; private set; }

    /// <summary>Count of 3-star ratings</summary>
    public int ThreeStar { get; private set; }

    /// <summary>Count of 4-star ratings</summary>
    public int FourStar { get; private set; }

    /// <summary>Count of 5-star ratings</summary>
    public int FiveStar { get; private set; }

    /// <summary>Total number of reviews (ratings with text)</summary>
    public int TotalReviews { get; private set; }

    /// <summary>When statistics were last recalculated</summary>
    public DateTime LastCalculatedAt { get; private set; } = SystemClock.UtcNow;

    private RatingSummary() { } // EF Core

    public static RatingSummary Create(Guid entityId, string entityType)
    {
        return new RatingSummary
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntityType = entityType.Trim(),
            AverageRating = 0,
            TotalRatings = 0,
            OneStar = 0,
            TwoStar = 0,
            ThreeStar = 0,
            FourStar = 0,
            FiveStar = 0,
            TotalReviews = 0,
            LastCalculatedAt = SystemClock.UtcNow
        };
    }

    public void Recalculate(IEnumerable<Rating> ratings)
    {
        var ratingsList = ratings.Where(r => r.ModerationStatus == RatingModerationStatus.Approved).ToList();

        TotalRatings = ratingsList.Count;
        OneStar = ratingsList.Count(r => r.Value == 1);
        TwoStar = ratingsList.Count(r => r.Value == 2);
        ThreeStar = ratingsList.Count(r => r.Value == 3);
        FourStar = ratingsList.Count(r => r.Value == 4);
        FiveStar = ratingsList.Count(r => r.Value == 5);
        TotalReviews = ratingsList.Count(r => !string.IsNullOrWhiteSpace(r.ReviewText));

        AverageRating = TotalRatings > 0
            ? Math.Round((decimal)ratingsList.Average(r => r.Value), 2)
            : 0;

        LastCalculatedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>Gets the distribution as percentages</summary>
    public Dictionary<int, double> GetDistributionPercentages()
    {
        if (TotalRatings == 0)
            return new Dictionary<int, double> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };

        return new Dictionary<int, double>
        {
            { 1, Math.Round(OneStar * 100.0 / TotalRatings, 1) },
            { 2, Math.Round(TwoStar * 100.0 / TotalRatings, 1) },
            { 3, Math.Round(ThreeStar * 100.0 / TotalRatings, 1) },
            { 4, Math.Round(FourStar * 100.0 / TotalRatings, 1) },
            { 5, Math.Round(FiveStar * 100.0 / TotalRatings, 1) }
        };
    }
}

/// <summary>
/// Interface for entities that support ratings
/// </summary>
public interface IRateable
{
    Guid Id { get; }
    string GetRatingEntityType();
}
