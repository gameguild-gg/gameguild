using GameGuild.CQRS;

namespace GameGuild.Monitoring.SLA.Commands;

public record DeleteSloCommand(Guid Id, Guid TenantId) : ICommand<Unit>;
