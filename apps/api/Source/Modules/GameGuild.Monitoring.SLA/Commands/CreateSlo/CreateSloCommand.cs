using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA;

public sealed record CreateSloCommand(
    Guid TenantId,
    string Name,
    string? Description,
    string ServiceName,
    double TargetPercentage,
    int TimeWindowDays,
    double ErrorBudgetPercentage,
    double AlertThresholdPercentage
) : ICommand<SloDto>;
