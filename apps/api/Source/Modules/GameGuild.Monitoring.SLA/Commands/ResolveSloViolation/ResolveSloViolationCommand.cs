using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Command to resolve an SLO violation.
/// </summary>
/// <remarks>
///     Concrete (not abstract): this record is bound from the request body, and
///     System.Text.Json can never deserialize an abstract type (E9 unbindable-body
///     class, issue #1278). Kept out of the summary so the generated OpenAPI/client
///     descriptions stay stable.
/// </remarks>
public sealed record ResolveSloViolationCommand(Guid ViolationId, Guid TenantId, string? ResolutionNotes = null) : ICommand<Unit>;
