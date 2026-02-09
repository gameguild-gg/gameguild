using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA;

public sealed record DeleteSloCommand(Guid Id, Guid TenantId) : ICommand<Unit>;
