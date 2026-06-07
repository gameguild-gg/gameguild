using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetTaxJurisdictionByIdQuery
/// </summary>
public sealed class GetTaxJurisdictionByIdHandler(IApplicationDbContext context) : IQueryHandler<GetTaxJurisdictionByIdQuery, TaxJurisdictionDto?>
{
    public async Task<TaxJurisdictionDto?> Handle(GetTaxJurisdictionByIdQuery request, CancellationToken cancellationToken)
    {
        var jurisdiction = await context.Set<TaxJurisdiction>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.JurisdictionId, cancellationToken);

        if (jurisdiction is null)
        {
            return null;
        }

        var defaultRate = await context.Set<TaxRate>()
            .AsNoTracking()
            .Where(rate => rate.TaxJurisdictionId == jurisdiction.Id && rate.ProductCategory == null)
            .OrderByDescending(rate => rate.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return TaxProjectionMapper.ToJurisdictionDto(jurisdiction, defaultRate);
    }
}
