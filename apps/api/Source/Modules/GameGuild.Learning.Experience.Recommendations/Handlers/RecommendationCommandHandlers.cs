using System.Text.Json;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Recommendations;

// ===== USER LEARNING PROFILE HANDLERS =====

public sealed class CreateOrUpdateLearningProfileCommandHandler(
    IApplicationDbContext context,
    ILogger<CreateOrUpdateLearningProfileCommandHandler> logger)
    : ICommandHandler<CreateOrUpdateLearningProfileCommand, UserLearningProfile>
{
    public async Task<UserLearningProfile> Handle(CreateOrUpdateLearningProfileCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating/updating learning profile for user {UserId}", request.UserId);

        var profile = await context.Set<UserLearningProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile == null)
        {
            profile = UserLearningProfile.Create(request.UserId);
            context.Set<UserLearningProfile>().Add(profile);
        }

        profile.UpdatePreferences(
            preferredCategories: request.PreferredCategories != null 
                ? JsonSerializer.Serialize(request.PreferredCategories) 
                : null,
            preferredDifficulty: request.PreferredDifficulty,
            preferredDuration: request.PreferredDuration,
            learningGoals: request.LearningGoals != null 
                ? JsonSerializer.Serialize(request.LearningGoals) 
                : null,
            skills: request.Skills != null 
                ? JsonSerializer.Serialize(request.Skills) 
                : null);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return profile;
    }
}

public sealed class AddSkillToProfileCommandHandler(
    IApplicationDbContext context,
    ILogger<AddSkillToProfileCommandHandler> logger,
    GameGuild.CQRS.IPublisher? publisher = null)
    : ICommandHandler<AddSkillToProfileCommand, UserLearningProfile>
{
    public async Task<UserLearningProfile> Handle(AddSkillToProfileCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding skill {Skill} to profile for user {UserId}", request.Skill, request.UserId);

        var profile = await context.Set<UserLearningProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile == null)
        {
            profile = UserLearningProfile.Create(request.UserId);
            context.Set<UserLearningProfile>().Add(profile);
        }

        profile.AddSkill(request.Skill);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Issue #1184 G4: UserSkillUpdatedEvent was declared but never raised; adding a
        // skill interest to the learning profile is its producing site (the profile-skill
        // consumer is idempotent — the skill is already in the set at this point).
        // Separate method (GGARCH003).
        await PublishSkillUpdatedAsync(request.UserId, request.Skill, cancellationToken).ConfigureAwait(false);

        return profile;
    }

    private async Task PublishSkillUpdatedAsync(Guid userId, string skill, CancellationToken cancellationToken)
    {
        if (publisher is null)
        {
            return;
        }

        await publisher.Publish(new UserSkillUpdatedEvent(
            userId, skill, null, null, null), cancellationToken).ConfigureAwait(false);
    }
}

public sealed class RemoveSkillFromProfileCommandHandler(
    IApplicationDbContext context,
    ILogger<RemoveSkillFromProfileCommandHandler> logger)
    : ICommandHandler<RemoveSkillFromProfileCommand, UserLearningProfile>
{
    public async Task<UserLearningProfile> Handle(RemoveSkillFromProfileCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing skill {Skill} from profile for user {UserId}", request.Skill, request.UserId);

        var profile = await context.Set<UserLearningProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile == null)
        {
            throw new EntityNotFoundException("UserLearningProfile", request.UserId, $"Learning profile not found for user {request.UserId}");
        }

        profile.RemoveSkill(request.Skill);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return profile;
    }
}

public sealed class UpdateUserActivityCommandHandler(
    IApplicationDbContext context,
    ILogger<UpdateUserActivityCommandHandler> logger)
    : ICommandHandler<UpdateUserActivityCommand>
{
    public async Task<Unit> Handle(UpdateUserActivityCommand request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Updating activity for user {UserId}", request.UserId);

        var profile = await context.Set<UserLearningProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile != null)
        {
            profile.UpdateActivity();
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        return Unit.Value;
    }
}

public sealed class IncrementCompletedCoursesCommandHandler(
    IApplicationDbContext context,
    ILogger<IncrementCompletedCoursesCommandHandler> logger)
    : ICommandHandler<IncrementCompletedCoursesCommand>
{
    public async Task<Unit> Handle(IncrementCompletedCoursesCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Incrementing completed courses for user {UserId} by {Hours} hours", request.UserId, request.Hours);

        var profile = await context.Set<UserLearningProfile>()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (profile == null)
        {
            profile = UserLearningProfile.Create(request.UserId);
            context.Set<UserLearningProfile>().Add(profile);
        }

        profile.IncrementCoursesCompleted(request.Hours);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

// ===== RECOMMENDATION HANDLERS =====

public sealed class GenerateRecommendationsCommandHandler(
    IRecommendationEngine engine,
    ILogger<GenerateRecommendationsCommandHandler> logger)
    : ICommandHandler<GenerateRecommendationsCommand, IEnumerable<CourseRecommendation>>
{
    public async Task<IEnumerable<CourseRecommendation>> Handle(GenerateRecommendationsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating recommendations for user {UserId}", request.UserId);

        return await engine.GenerateRecommendationsAsync(
            request.UserId,
            request.TenantId,
            request.MaxResults,
            request.Types,
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class MarkRecommendationViewedCommandHandler(
    IApplicationDbContext context,
    ILogger<MarkRecommendationViewedCommandHandler> logger,
    GameGuild.CQRS.IPublisher? publisher = null)
    : ICommandHandler<MarkRecommendationViewedCommand>
{
    public async Task<Unit> Handle(MarkRecommendationViewedCommand request, CancellationToken cancellationToken)
    {
        var recommendation = await context.Set<CourseRecommendation>()
            .FirstOrDefaultAsync(r => r.Id == request.RecommendationId && r.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {Id} not found for user {UserId}", request.RecommendationId, request.UserId);
            return Unit.Value;
        }

        recommendation.MarkViewed();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Marked recommendation {Id} as viewed", request.RecommendationId);

        // Issue #1184 G4: RecommendationViewedEvent was declared but never raised; the
        // explicit viewed-tracking command is its producing site. Separate method (GGARCH003).
        await PublishViewedAsync(request.UserId, recommendation, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }

    private async Task PublishViewedAsync(Guid userId, CourseRecommendation recommendation, CancellationToken cancellationToken)
    {
        if (publisher is null)
        {
            return;
        }

        await publisher.Publish(new RecommendationViewedEvent(
            userId,
            recommendation.Id,
            recommendation.CourseId,
            recommendation.Type.ToString(),
            0,
            null), cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
///     Records a recommendation click (issue #1184 G4): the user navigated to the recommended
///     course from the recommendation surface. When the user is already actively enrolled in
///     that course, the click also publishes <see cref="GameGuild.Learning.RecommendationConvertedEvent"/>
///     — the demonstrable recommendation-led enrollment signal.
/// </summary>
public sealed record RecordRecommendationClickCommand(Guid RecommendationId, Guid UserId) : ICommand;

public sealed class RecordRecommendationClickCommandHandler(
    IApplicationDbContext context,
    GameGuild.CQRS.IPublisher publisher,
    ILogger<RecordRecommendationClickCommandHandler> logger)
    : ICommandHandler<RecordRecommendationClickCommand>
{
    public async Task<Unit> Handle(RecordRecommendationClickCommand request, CancellationToken cancellationToken)
    {
        var recommendation = await context.Set<CourseRecommendation>()
            .FirstOrDefaultAsync(r => r.Id == request.RecommendationId && r.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {Id} not found for user {UserId}", request.RecommendationId, request.UserId);
            return Unit.Value;
        }

        // Issue #1184 G4: RecommendationClickedEvent was declared but never raised; this
        // command (mirroring MarkRecommendationViewed) is its producing site.
        await publisher.Publish(new RecommendationClickedEvent(
            request.UserId,
            recommendation.Id,
            recommendation.CourseId,
            recommendation.Type.ToString(),
            0,
            null), cancellationToken).ConfigureAwait(false);

        // Conversion signal: the click landed on a course the user is actively enrolled in
        // (courses enrollment surface — ProgramUser rows), i.e. the recommendation led to
        // (or already produced) an enrollment.
        var enrolled = await context.Set<Learning.Courses.ProgramUser>()
            .AnyAsync(pu => pu.ProgramId == recommendation.CourseId && pu.UserId == request.UserId && pu.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (enrolled)
        {
            await publisher.Publish(new RecommendationConvertedEvent(
                request.UserId,
                recommendation.Id,
                recommendation.CourseId,
                recommendation.Type.ToString(),
                null), cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Recorded click on recommendation {Id}", request.RecommendationId);
        return Unit.Value;
    }
}

public sealed class DismissRecommendationCommandHandler(
    IApplicationDbContext context,
    ILogger<DismissRecommendationCommandHandler> logger)
    : ICommandHandler<DismissRecommendationCommand>
{
    public async Task<Unit> Handle(DismissRecommendationCommand request, CancellationToken cancellationToken)
    {
        var recommendation = await context.Set<CourseRecommendation>()
            .FirstOrDefaultAsync(r => r.Id == request.RecommendationId && r.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (recommendation == null)
        {
            logger.LogWarning("Recommendation {Id} not found for user {UserId}", request.RecommendationId, request.UserId);
            return Unit.Value;
        }

        recommendation.Dismiss();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Dismissed recommendation {Id}", request.RecommendationId);
        return Unit.Value;
    }
}

public sealed class RefreshRecommendationsCommandHandler(
    IRecommendationEngine engine,
    ILogger<RefreshRecommendationsCommandHandler> logger)
    : ICommandHandler<RefreshRecommendationsCommand>
{
    public async Task<Unit> Handle(RefreshRecommendationsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Refreshing recommendations for user {UserId}", request.UserId);
        await engine.RefreshRecommendationsAsync(request.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class ClearUserRecommendationsCommandHandler(
    IApplicationDbContext context,
    ILogger<ClearUserRecommendationsCommandHandler> logger)
    : ICommandHandler<ClearUserRecommendationsCommand, int>
{
    public async Task<int> Handle(ClearUserRecommendationsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Clearing all recommendations for user {UserId}", request.UserId);

        var recommendations = await context.Set<CourseRecommendation>()
            .Where(r => r.UserId == request.UserId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        context.Set<CourseRecommendation>().RemoveRange(recommendations);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return recommendations.Count;
    }
}
