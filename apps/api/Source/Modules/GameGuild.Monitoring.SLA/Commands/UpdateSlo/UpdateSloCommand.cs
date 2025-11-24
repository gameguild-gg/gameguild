using GameGuild.CQRS;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Commands;

public record UpdateSloCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    string ServiceName,
    double TargetPercentage,
    int TimeWindowDays,
    double ErrorBudgetPercentage,
    double AlertThresholdPercentage,
    bool IsEnabled
) : ICommand<SloDto>;
