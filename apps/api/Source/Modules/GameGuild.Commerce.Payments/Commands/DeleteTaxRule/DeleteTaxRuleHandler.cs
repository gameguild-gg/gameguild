using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for DeleteTaxRuleCommand
/// </summary>
public sealed class DeleteTaxRuleHandler(IApplicationDbContext context) : ICommandHandler<DeleteTaxRuleCommand>
{
    public async Task<Unit> Handle(DeleteTaxRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await context.Set<TaxRule>()
            .Include(item => item.DefaultTaxRate)
            .FirstOrDefaultAsync(item => item.Id == request.RuleId, cancellationToken)
            ?? throw new InvalidOperationException($"Tax rule '{request.RuleId}' not found.");

        rule.IsActive = false;
        rule.Touch();

        if (rule.DefaultTaxRate is not null)
        {
            rule.DefaultTaxRate.IsActive = false;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
