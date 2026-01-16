using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for PatchTaxRuleCommand
/// </summary>
public sealed class PatchTaxRuleHandler : ICommandHandler<PatchTaxRuleCommand>
{
    public Task Handle(PatchTaxRuleCommand request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would update actual rule
        return Task.CompletedTask;
    }
}
