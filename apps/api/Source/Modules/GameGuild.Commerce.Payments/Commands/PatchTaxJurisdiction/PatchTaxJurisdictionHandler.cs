using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for PatchTaxJurisdictionCommand
/// </summary>
public sealed class PatchTaxJurisdictionHandler : ICommandHandler<PatchTaxJurisdictionCommand>
{
    public Task<Unit> Handle(PatchTaxJurisdictionCommand request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would update actual jurisdiction
        return Unit.Task;
    }
}
