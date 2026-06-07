using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for DeleteTaxJurisdictionCommand
/// </summary>
public sealed class DeleteTaxJurisdictionHandler(IApplicationDbContext context) : ICommandHandler<DeleteTaxJurisdictionCommand>
{
    public async Task<Unit> Handle(DeleteTaxJurisdictionCommand request, CancellationToken cancellationToken)
    {
        var jurisdiction = await context.Set<TaxJurisdiction>()
            .FirstOrDefaultAsync(item => item.Id == request.JurisdictionId, cancellationToken)
            ?? throw new InvalidOperationException($"Tax jurisdiction '{request.JurisdictionId}' not found.");

        jurisdiction.IsActive = false;
        jurisdiction.Touch();

        var rules = await context.Set<TaxRule>()
            .Where(rule => rule.TaxJurisdictionId == jurisdiction.Id)
            .ToListAsync(cancellationToken);
        var rates = await context.Set<TaxRate>()
            .Where(rate => rate.TaxJurisdictionId == jurisdiction.Id)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            rule.IsActive = false;
            rule.Touch();
        }

        foreach (var rate in rates)
        {
            rate.IsActive = false;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
