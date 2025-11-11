using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA.Commands;

/// <summary>
///     Command to resolve an SLO violation.
/// </summary>
public abstract record ResolveSloViolationCommand(Guid ViolationId, Guid TenantId, string? ResolutionNotes = null) : ICommand<Unit>;
