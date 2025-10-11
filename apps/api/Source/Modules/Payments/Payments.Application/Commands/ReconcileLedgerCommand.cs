using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Payments.Application.Commands;

public record ReconcileLedgerCommand(
    Guid EntryId,
    Guid ReconciledBy,
    string? Notes = null
) : ICommand<Unit>;
