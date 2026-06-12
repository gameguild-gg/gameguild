using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.LaunchPad;

public sealed class LaunchPadHandlers(IApplicationDbContext context, IActorContextAccessor actorContextAccessor, ILogger<LaunchPadHandlers> logger)
    : ICommandHandler<CreateLaunchPlanCommand, Result<LaunchPlan>>,
      ICommandHandler<CompleteLaunchChecklistItemCommand, Result<LaunchPlan>>,
      ICommandHandler<PublishLaunchCommand, Result<LaunchPlan>>,
      IQueryHandler<GetLaunchPlanQuery, Result<LaunchPlan?>>,
      IQueryHandler<GetLaunchPlanByProjectQuery, Result<LaunchPlan?>>,
      IQueryHandler<GetLaunchPadDashboardQuery, Result<IReadOnlyList<LaunchPlan>>>
{
    public async Task<Result<LaunchPlan>> Handle(CreateLaunchPlanCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await context.Set<Project>()
            .AnyAsync(project => project.Id == request.ProjectId && project.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (!projectExists) return Result.Failure<LaunchPlan>(Error.NotFound("LaunchPad.ProjectNotFound", "Project not found."));

        var existing = await context.Set<LaunchPlan>()
            .Include(plan => plan.ChecklistItems)
            .FirstOrDefaultAsync(plan => plan.ProjectId == request.ProjectId && plan.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (existing != null) return Result.Failure<LaunchPlan>(Error.Conflict("LaunchPad.PlanExists", "A launch plan already exists for this project."));

        var plan = new LaunchPlan
        {
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            Positioning = request.Positioning?.Trim(),
            TargetLaunchAt = request.TargetLaunchAt,
            Channels = NormalizeChannels(request.Channels)
        };

        foreach (var item in request.ChecklistItems)
        {
            var checklistItem = new LaunchChecklistItem
            {
                Title = item.Title.Trim(),
                Category = string.IsNullOrWhiteSpace(item.Category) ? "Readiness" : item.Category.Trim(),
                IsRequired = item.IsRequired,
                IsComplete = item.IsComplete,
                CompletedAt = item.IsComplete ? SystemClock.UtcNow : null
            };
            plan.ChecklistItems.Add(checklistItem);
        }

        plan.RecalculateStatus();
        context.Set<LaunchPlan>().Add(plan);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Actor {ActorId} created launch plan {LaunchPlanId} for project {ProjectId}",
            actorContextAccessor.ActorContext.SubjectId,
            plan.Id,
            plan.ProjectId);

        return Result.Success(plan);
    }

    public async Task<Result<LaunchPlan>> Handle(CompleteLaunchChecklistItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await LoadPlan(request.LaunchPlanId, cancellationToken).ConfigureAwait(false);
        if (plan == null) return Result.Failure<LaunchPlan>(Error.NotFound("LaunchPad.PlanNotFound", "Launch plan not found."));

        var item = plan.ChecklistItems.FirstOrDefault(candidate => candidate.Id == request.ChecklistItemId);
        if (item == null) return Result.Failure<LaunchPlan>(Error.NotFound("LaunchPad.ChecklistItemNotFound", "Checklist item not found."));

        item.Complete();
        plan.RecalculateStatus();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(plan);
    }

    public async Task<Result<LaunchPlan>> Handle(PublishLaunchCommand request, CancellationToken cancellationToken)
    {
        var plan = await LoadPlan(request.LaunchPlanId, cancellationToken).ConfigureAwait(false);
        if (plan == null) return Result.Failure<LaunchPlan>(Error.NotFound("LaunchPad.PlanNotFound", "Launch plan not found."));

        try
        {
            plan.Publish();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<LaunchPlan>(Error.Validation("LaunchPad.NotReady", ex.Message));
        }

        var project = await context.Set<Project>().FirstOrDefaultAsync(candidate => candidate.Id == plan.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project != null)
        {
            project.Status = ContentStatus.Published;
            project.Visibility = ContentVisibility.Public;
            project.PublishedAt ??= SystemClock.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(plan);
    }

    public async Task<Result<LaunchPlan?>> Handle(GetLaunchPlanQuery request, CancellationToken cancellationToken)
        => Result.Success(await LoadPlan(request.LaunchPlanId, cancellationToken).ConfigureAwait(false));

    public async Task<Result<LaunchPlan?>> Handle(GetLaunchPlanByProjectQuery request, CancellationToken cancellationToken)
        => Result.Success(await context.Set<LaunchPlan>()
            .AsNoTracking()
            .Include(plan => plan.Project)
            .Include(plan => plan.ChecklistItems)
            .FirstOrDefaultAsync(plan => plan.ProjectId == request.ProjectId && plan.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false));

    public async Task<Result<IReadOnlyList<LaunchPlan>>> Handle(GetLaunchPadDashboardQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<LaunchPlan>()
            .AsNoTracking()
            .Include(plan => plan.Project)
            .Include(plan => plan.ChecklistItems)
            .Where(plan => plan.DeletedAt == null);

        if (request.Status.HasValue) query = query.Where(plan => plan.Status == request.Status.Value);

        var plans = await query
            .OrderBy(plan => plan.TargetLaunchAt ?? DateTime.MaxValue)
            .ThenBy(plan => plan.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<LaunchPlan>>(plans);
    }

    private async Task<LaunchPlan?> LoadPlan(Guid launchPlanId, CancellationToken cancellationToken)
        => await context.Set<LaunchPlan>()
            .Include(plan => plan.Project)
            .Include(plan => plan.ChecklistItems)
            .FirstOrDefaultAsync(plan => plan.Id == launchPlanId && plan.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);

    private static string[] NormalizeChannels(IEnumerable<string> channels)
        => channels
            .Select(channel => channel.Trim().ToLowerInvariant())
            .Where(channel => channel.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(channel => channel, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
