using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Service implementation for recommendation operations
/// </summary>
public class RecommendationService(IMediator mediator) : IRecommendationService
{
    // ===== RECOMMENDATIONS =====

    public async Task<IEnumerable<CourseRecommendation>> GetUserRecommendationsAsync(
        Guid userId,
        Guid? tenantId = null,
        RecommendationType? type = null,
        bool includeViewed = false,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetUserRecommendationsQuery(
            userId, tenantId, type, includeViewed, skip, take), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<CourseRecommendation>> GenerateRecommendationsAsync(
        Guid userId,
        Guid? tenantId = null,
        int maxResults = 10,
        RecommendationType[]? types = null,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GenerateRecommendationsCommand(
            userId, tenantId, maxResults, types), cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkRecommendationViewedAsync(
        Guid recommendationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await mediator.Send(new MarkRecommendationViewedCommand(recommendationId, userId), cancellationToken).ConfigureAwait(false);
    }

    public async Task DismissRecommendationAsync(
        Guid recommendationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await mediator.Send(new DismissRecommendationCommand(recommendationId, userId), cancellationToken).ConfigureAwait(false);
    }

    public async Task RefreshRecommendationsAsync(
        Guid userId,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        await mediator.Send(new RefreshRecommendationsCommand(userId, tenantId), cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecommendationStatisticsDto> GetStatisticsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetRecommendationStatisticsQuery(userId), cancellationToken).ConfigureAwait(false);
    }

    // ===== USER LEARNING PROFILE =====

    public async Task<UserLearningProfile?> GetUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetUserLearningProfileQuery(userId), cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserLearningProfile> GetOrCreateUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetOrCreateUserLearningProfileQuery(userId), cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserLearningProfile> UpdateUserProfileAsync(
        Guid userId,
        CreateOrUpdateLearningProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new CreateOrUpdateLearningProfileCommand(
            userId,
            dto.PreferredCategories,
            dto.PreferredDifficulty,
            dto.PreferredDuration,
            dto.LearningGoals,
            dto.Skills), cancellationToken);
    }

    public async Task<UserLearningProfile> AddSkillToProfileAsync(
        Guid userId,
        string skill,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new AddSkillToProfileCommand(userId, skill), cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserLearningProfile> RemoveSkillFromProfileAsync(
        Guid userId,
        string skill,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new RemoveSkillFromProfileCommand(userId, skill), cancellationToken).ConfigureAwait(false);
    }

    // ===== DISCOVERY =====

    public async Task<IEnumerable<PopularCourseDto>> GetPopularCoursesAsync(
        Guid? tenantId = null,
        string? category = null,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetPopularCoursesQuery(tenantId, category, skip, take), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TrendingCourseDto>> GetTrendingCoursesAsync(
        Guid? tenantId = null,
        int daysWindow = 7,
        int skip = 0,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetTrendingCoursesQuery(tenantId, daysWindow, skip, take), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<SimilarCourseDto>> GetSimilarCoursesAsync(
        Guid courseId,
        Guid? tenantId = null,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetSimilarCoursesQuery(courseId, tenantId, maxResults), cancellationToken).ConfigureAwait(false);
    }
}
