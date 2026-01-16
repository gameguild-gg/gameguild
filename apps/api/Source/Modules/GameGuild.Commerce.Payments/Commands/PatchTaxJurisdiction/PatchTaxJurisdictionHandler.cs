using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for PatchTaxJurisdictionCommand
/// </summary>
public sealed class PatchTaxJurisdictionHandler : ICommandHandler<PatchTaxJurisdictionCommand>
{
    public Task Handle(PatchTaxJurisdictionCommand request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would update actual jurisdiction
        return Task.CompletedTask;
    }
}
