using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CreateTaxJurisdictionCommand
/// </summary>
public sealed class CreateTaxJurisdictionHandler : ICommandHandler<CreateTaxJurisdictionCommand, Guid>
{
    public Task<Guid> Handle(CreateTaxJurisdictionCommand request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would create actual jurisdiction
        return Task.FromResult(Guid.NewGuid());
    }
}
