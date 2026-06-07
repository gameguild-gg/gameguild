using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetTaxRuleByIdQuery
/// </summary>
public sealed class GetTaxRuleByIdHandler(IApplicationDbContext context) : IQueryHandler<GetTaxRuleByIdQuery, TaxRuleDto?>
{
    public async Task<TaxRuleDto?> Handle(GetTaxRuleByIdQuery request, CancellationToken cancellationToken)
    {
        var rule = await context.Set<TaxRule>()
            .AsNoTracking()
            .Include(item => item.TaxJurisdiction)
            .Include(item => item.DefaultTaxRate)
            .FirstOrDefaultAsync(item => item.Id == request.RuleId, cancellationToken);

        return rule is null ? null : TaxProjectionMapper.ToRuleDto(rule);
    }
}
