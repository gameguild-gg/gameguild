using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to reconcile a ledger entry
/// </summary>
public record ReconcileLedgerCommand(Guid EntryId, Guid ReconciledBy, string? Notes = null) : ICommand;
