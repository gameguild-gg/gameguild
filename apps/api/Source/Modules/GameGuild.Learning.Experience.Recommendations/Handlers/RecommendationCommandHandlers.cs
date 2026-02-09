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
    ILogger<AddSkillToProfileCommandHandler> logger)
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
        return profile;
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
            throw new InvalidOperationException($"Learning profile not found for user {request.UserId}");
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
    ILogger<MarkRecommendationViewedCommandHandler> logger)
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
