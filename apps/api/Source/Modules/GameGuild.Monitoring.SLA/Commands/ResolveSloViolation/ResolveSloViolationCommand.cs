using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Command to resolve an SLO violation.
///     Concrete (not abstract): this record is bound from the request body, and
///     System.Text.Json can never deserialize an abstract type (E9 unbindable-body
///     class, issue #1278).
/// </summary>
public sealed record ResolveSloViolationCommand(Guid ViolationId, Guid TenantId, string? ResolutionNotes = null) : ICommand<Unit>;
