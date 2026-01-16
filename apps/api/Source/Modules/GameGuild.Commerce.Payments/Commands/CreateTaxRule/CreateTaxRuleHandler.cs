using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CreateTaxRuleCommand
/// </summary>
public sealed class CreateTaxRuleHandler : ICommandHandler<CreateTaxRuleCommand, Guid>
{
    public Task<Guid> Handle(CreateTaxRuleCommand request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would create actual rule
        return Task.FromResult(Guid.NewGuid());
    }
}
