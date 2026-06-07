using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for PatchTaxJurisdictionCommand
/// </summary>
public sealed class PatchTaxJurisdictionHandler(IApplicationDbContext context) : ICommandHandler<PatchTaxJurisdictionCommand>
{
    public async Task<Unit> Handle(PatchTaxJurisdictionCommand request, CancellationToken cancellationToken)
    {
        var jurisdiction = await context.Set<TaxJurisdiction>()
            .FirstOrDefaultAsync(item => item.Id == request.JurisdictionId, cancellationToken)
            ?? throw new InvalidOperationException($"Tax jurisdiction '{request.JurisdictionId}' not found.");

        if (request.Name is not null)
        {
            jurisdiction.Name = request.Name.Trim();
        }

        if (request.IsActive.HasValue)
        {
            jurisdiction.IsActive = request.IsActive.Value;
        }

        TaxRate? defaultRate = null;
        if (request.DefaultRate.HasValue || !string.IsNullOrWhiteSpace(request.TaxType))
        {
            defaultRate = await context.Set<TaxRate>()
                .Where(rate => rate.TaxJurisdictionId == jurisdiction.Id && rate.ProductCategory == null)
                .OrderByDescending(rate => rate.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultRate is null)
            {
                defaultRate = new TaxRate
                {
                    TaxJurisdictionId = jurisdiction.Id,
                    TaxType = TaxProjectionMapper.ParseTaxType(request.TaxType),
                    Rate = TaxProjectionMapper.NormalizeRate(request.DefaultRate ?? 0m),
                    EffectiveFrom = SystemClock.UtcNow,
                    IsActive = jurisdiction.IsActive,
                    Description = $"Default rate for {jurisdiction.Code}"
                };
                await context.Set<TaxRate>().AddAsync(defaultRate, cancellationToken);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(request.TaxType))
                {
                    defaultRate.TaxType = TaxProjectionMapper.ParseTaxType(request.TaxType);
                }

                if (request.DefaultRate.HasValue)
                {
                    defaultRate.Rate = TaxProjectionMapper.NormalizeRate(request.DefaultRate.Value);
                }

                defaultRate.IsActive = jurisdiction.IsActive;
            }
        }

        if (request.IsActive.HasValue)
        {
            var rules = await context.Set<TaxRule>()
                .Where(rule => rule.TaxJurisdictionId == jurisdiction.Id)
                .ToListAsync(cancellationToken);
            var rates = await context.Set<TaxRate>()
                .Where(rate => rate.TaxJurisdictionId == jurisdiction.Id)
                .ToListAsync(cancellationToken);

            foreach (var rule in rules)
            {
                rule.IsActive = request.IsActive.Value;
                rule.Touch();
            }

            foreach (var rate in rates)
            {
                rate.IsActive = request.IsActive.Value;
            }
        }

        jurisdiction.Touch();
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
