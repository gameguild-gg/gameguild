using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for DeleteTaxJurisdictionCommand
/// </summary>
public sealed class DeleteTaxJurisdictionHandler : ICommandHandler<DeleteTaxJurisdictionCommand>
{
    public Task<Unit> Handle(DeleteTaxJurisdictionCommand request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would delete actual jurisdiction
        return Unit.Task;
    }
}
