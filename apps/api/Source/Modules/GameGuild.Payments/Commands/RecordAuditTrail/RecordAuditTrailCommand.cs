using GameGuild.CQRS;

namespace GameGuild.Payments.Commands;

/// <summary>
///     Command to record an audit trail entry
/// </summary>
public record RecordAuditTrailCommand(
    string EntityType,
    Guid EntityId,
    string Action,
    Guid ChangedBy,
    string? OldValue = null,
    string? NewValue = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? Reason = null
) : ICommand;
