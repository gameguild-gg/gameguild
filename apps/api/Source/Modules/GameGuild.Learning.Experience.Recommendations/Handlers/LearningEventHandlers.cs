using GameGuild.CQRS;
using GameGuild.Learning;
using GameGuild.Learning.Experience.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Handles CourseCompletedEvent to update user learning profile statistics
/// </summary>
public sealed class CourseCompletedLearningProfileHandler(
    IApplicationDbContext context,
    ILogger<CourseCompletedLearningProfileHandler> logger)
    : IDomainEventHandler<CourseCompletedEvent>
{
    public async Task Handle(CourseCompletedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "User {UserId} completed course {CourseId}, updating learning profile",
            notification.UserId, notification.CourseId);

        var profile = await context.Set<UserLearningProfile>()
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken).ConfigureAwait(false);

        if (profile == null)
        {
            profile = UserLearningProfile.Create(notification.UserId);
            context.Set<UserLearningProfile>().Add(profile);
        }

        var hours = notification.TotalTimeSpentSeconds / 3600;
        profile.IncrementCoursesCompleted(hours > 0 ? hours : 1);
        profile.UpdateActivity();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Updated learning profile for user {UserId}: {TotalCourses} courses, {TotalHours} hours",
            notification.UserId, profile.TotalCoursesCompleted, profile.TotalHoursLearned);
    }
}

/// <summary>
/// Handles CourseViewedEvent to update user activity
/// </summary>
public sealed class CourseViewedActivityHandler(
    IApplicationDbContext context,
    ILogger<CourseViewedActivityHandler> logger)
    : IDomainEventHandler<CourseViewedEvent>
{
    public async Task Handle(CourseViewedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating activity for user {UserId} who viewed course", notification.UserId);

        var profile = await context.Set<UserLearningProfile>()
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken).ConfigureAwait(false);

        if (profile != null)
        {
            profile.UpdateActivity();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Handles RecommendationConvertedEvent to track recommendation effectiveness
/// </summary>
public sealed class RecommendationConvertedHandler(
    IApplicationDbContext context,
    ILogger<RecommendationConvertedHandler> logger)
    : IDomainEventHandler<RecommendationConvertedEvent>
{
    public async Task Handle(RecommendationConvertedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Recommendation {RecommendationId} converted for user {UserId} on course {CourseId}",
            notification.RecommendationId, notification.UserId, notification.CourseId);

        var recommendation = await context.Set<CourseRecommendation>()
            .FirstOrDefaultAsync(r => r.Id == notification.RecommendationId, cancellationToken).ConfigureAwait(false);

        if (recommendation != null)
        {
            recommendation.MarkViewed();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Handles SearchPerformedEvent to record search history for analytics
/// </summary>
public sealed class SearchPerformedHistoryHandler(
    IApplicationDbContext context,
    ILogger<SearchPerformedHistoryHandler> logger)
    : IDomainEventHandler<SearchPerformedEvent>
{
    public async Task Handle(SearchPerformedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Recording search for query '{Query}' with {ResultCount} results",
            notification.Query, notification.ResultCount);

        var searchHistory = SearchHistory.Create(
            userId: notification.UserId,
            query: notification.Query,
            resultCount: notification.ResultCount,
            filters: notification.Filters,
            tenantId: notification.TenantId);

        context.Set<SearchHistory>().Add(searchHistory);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Handles SearchResultClickedEvent to update search history with click data
/// </summary>
public sealed class SearchResultClickedHandler(
    IApplicationDbContext context,
    ILogger<SearchResultClickedHandler> logger)
    : IDomainEventHandler<SearchResultClickedEvent>
{
    public async Task Handle(SearchResultClickedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Recording search result click for user {UserId}, query '{Query}'", notification.UserId, notification.Query);

        // Find recent search history for this user/query
        var recentCutoff = SystemClock.UtcNow.AddMinutes(-30);
        var searchHistory = await context.Set<SearchHistory>()
            .Where(s => s.UserId == notification.UserId)
            .Where(s => s.Query == notification.Query)
            .Where(s => s.CreatedAt >= recentCutoff)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (searchHistory != null)
        {
            searchHistory.RecordClick(notification.ClickedCourseId, notification.Position);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Handles LearningProgressUpdatedEvent to refresh recommendations when significant progress is made
/// </summary>
public sealed class LearningProgressRecommendationRefreshHandler(
    IRecommendationEngine engine,
    ILogger<LearningProgressRecommendationRefreshHandler> logger)
    : IDomainEventHandler<LearningProgressUpdatedEvent>
{
    public async Task Handle(LearningProgressUpdatedEvent notification, CancellationToken cancellationToken)
    {
        // Only refresh recommendations when significant progress milestones are reached
        var progressMilestones = new[] { 25, 50, 75, 100 };
        
        var crossedMilestone = progressMilestones.Any(m => 
            notification.OldProgress < m && notification.NewProgress >= m);

        if (crossedMilestone)
        {
            logger.LogInformation(
                "User {UserId} crossed progress milestone in course {CourseId}, refreshing recommendations",
                notification.UserId, notification.CourseId);

            await engine.RefreshRecommendationsAsync(
                notification.UserId, 
                notification.TenantId, 
                cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Handles UserSkillUpdatedEvent to update user learning profile skills
/// </summary>
public sealed class UserSkillUpdatedProfileHandler(
    IApplicationDbContext context,
    ILogger<UserSkillUpdatedProfileHandler> logger)
    : IDomainEventHandler<UserSkillUpdatedEvent>
{
    public async Task Handle(UserSkillUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Updating skill {Skill} for user {UserId}",
            notification.SkillName, notification.UserId);

        var profile = await context.Set<UserLearningProfile>()
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken).ConfigureAwait(false);

        if (profile == null)
        {
            profile = UserLearningProfile.Create(notification.UserId);
            context.Set<UserLearningProfile>().Add(profile);
        }

        profile.AddSkill(notification.SkillName);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
