using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Commands;

public record RecordAuditTrailCommand(
    string EntityType,
    Guid EntityId,
    AuditAction Action,
    Guid ChangedBy,
    string? OldValue = null,
    string? NewValue = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? Reason = null,
    Guid? TenantId = null
) : IRequest<AuditTrail>;
