using GameGuild.CQRS;

namespace GameGuild.LaunchPad;

public sealed record LaunchChecklistItemInput(string Title, string Category, bool IsComplete = false, bool IsRequired = true);

public sealed record CreateLaunchPlanCommand : ICommand<Result<LaunchPlan>>
{
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Positioning { get; init; }
    public DateTime? TargetLaunchAt { get; init; }
    public IReadOnlyList<string> Channels { get; init; } = [];
    public IReadOnlyList<LaunchChecklistItemInput> ChecklistItems { get; init; } = [];
}

public sealed record CompleteLaunchChecklistItemCommand : ICommand<Result<LaunchPlan>>
{
    public Guid LaunchPlanId { get; init; }
    public Guid ChecklistItemId { get; init; }
}

public sealed record PublishLaunchCommand : ICommand<Result<LaunchPlan>>
{
    public Guid LaunchPlanId { get; init; }
}

public sealed record GetLaunchPlanQuery : IQuery<Result<LaunchPlan?>>
{
    public Guid LaunchPlanId { get; init; }
}

public sealed record GetLaunchPlanByProjectQuery : IQuery<Result<LaunchPlan?>>
{
    public Guid ProjectId { get; init; }
}

public sealed record GetLaunchPadDashboardQuery : IQuery<Result<IReadOnlyList<LaunchPlan>>>
{
    public LaunchPlanStatus? Status { get; init; }
}

public sealed record CreateLaunchPlanRequest
{
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Positioning { get; init; }
    public DateTime? TargetLaunchAt { get; init; }
    public IReadOnlyList<string> Channels { get; init; } = [];
    public IReadOnlyList<LaunchChecklistItemInput> ChecklistItems { get; init; } = [];
}
