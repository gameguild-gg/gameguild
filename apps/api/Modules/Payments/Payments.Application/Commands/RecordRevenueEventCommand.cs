using GameGuild.Modules.Payments.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Commands;

public record RecordRevenueEventCommand(
    RevenueEventType EventType,
    decimal Amount,
    string Currency,
    RevenueSource Source,
    string ReferenceId,
    Guid? UserId = null,
    Guid? TenantId = null,
    string? Metadata = null
) : IRequest<RevenueEvent>;
