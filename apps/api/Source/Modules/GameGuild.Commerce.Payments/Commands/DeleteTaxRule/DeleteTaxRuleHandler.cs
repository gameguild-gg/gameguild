using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for DeleteTaxRuleCommand
/// </summary>
public sealed class DeleteTaxRuleHandler : ICommandHandler<DeleteTaxRuleCommand>
{
    public Task<Unit> Handle(DeleteTaxRuleCommand request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would delete actual rule
        return Unit.Task;
    }
}
