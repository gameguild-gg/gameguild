using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CreateTaxRuleCommand
/// </summary>
public sealed class CreateTaxRuleHandler(IApplicationDbContext context) : ICommandHandler<CreateTaxRuleCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaxRuleCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.JurisdictionCode);

        var jurisdictionCode = request.JurisdictionCode.Trim().ToUpperInvariant();
        var jurisdiction = await context.Set<TaxJurisdiction>()
            .FirstOrDefaultAsync(item => item.Code == jurisdictionCode && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Tax jurisdiction '{jurisdictionCode}' not found.");

        var rate = TaxProjectionMapper.NormalizeRate(request.Rate);
        var customerType = TaxProjectionMapper.ParseCustomerType(request.CustomerType);
        var taxRate = new TaxRate
        {
            TaxJurisdictionId = jurisdiction.Id,
            TaxType = TaxType.VAT,
            Rate = rate,
            ProductCategory = string.IsNullOrWhiteSpace(request.ProductCategory) ? null : request.ProductCategory.Trim(),
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Description = request.Description
        };

        var rule = new TaxRule
        {
            Name = $"{jurisdiction.Code} {(request.ProductCategory ?? "default")} tax rule",
            Description = request.Description,
            TaxJurisdictionId = jurisdiction.Id,
            RuleType = rate == 0m ? TaxRuleType.ZeroRated : TaxRuleType.Standard,
            Priority = 0,
            IsActive = true,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            CustomerTypeFilter = customerType,
            ProductCategories = TaxProjectionMapper.SerializeProductCategory(request.ProductCategory),
            DefaultTaxRateId = taxRate.Id
        };

        await context.Set<TaxRate>().AddAsync(taxRate, cancellationToken);
        await context.Set<TaxRule>().AddAsync(rule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return rule.Id;
    }
}
