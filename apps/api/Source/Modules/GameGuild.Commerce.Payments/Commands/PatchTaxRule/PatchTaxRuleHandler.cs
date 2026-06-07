using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for PatchTaxRuleCommand
/// </summary>
public sealed class PatchTaxRuleHandler(IApplicationDbContext context) : ICommandHandler<PatchTaxRuleCommand>
{
    public async Task<Unit> Handle(PatchTaxRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await context.Set<TaxRule>()
            .Include(item => item.DefaultTaxRate)
            .FirstOrDefaultAsync(item => item.Id == request.RuleId, cancellationToken)
            ?? throw new InvalidOperationException($"Tax rule '{request.RuleId}' not found.");

        if (request.Rate.HasValue)
        {
            var rate = TaxProjectionMapper.NormalizeRate(request.Rate.Value);
            if (rule.DefaultTaxRate is null)
            {
                rule.DefaultTaxRate = new TaxRate
                {
                    TaxJurisdictionId = rule.TaxJurisdictionId,
                    TaxType = TaxType.VAT,
                    Rate = rate,
                    EffectiveFrom = request.EffectiveFrom ?? rule.EffectiveFrom ?? SystemClock.UtcNow,
                    EffectiveTo = request.EffectiveTo ?? rule.EffectiveTo,
                    Description = request.Description ?? rule.Description
                };
            }
            else
            {
                rule.DefaultTaxRate.Rate = rate;
            }

            rule.RuleType = rate == 0m ? TaxRuleType.ZeroRated : TaxRuleType.Standard;
        }

        if (request.EffectiveFrom.HasValue)
        {
            rule.EffectiveFrom = request.EffectiveFrom.Value;
            if (rule.DefaultTaxRate is not null)
            {
                rule.DefaultTaxRate.EffectiveFrom = request.EffectiveFrom.Value;
            }
        }

        if (request.EffectiveTo.HasValue)
        {
            rule.EffectiveTo = request.EffectiveTo;
            if (rule.DefaultTaxRate is not null)
            {
                rule.DefaultTaxRate.EffectiveTo = request.EffectiveTo;
            }
        }

        if (request.Description is not null)
        {
            rule.Description = request.Description;
            if (rule.DefaultTaxRate is not null)
            {
                rule.DefaultTaxRate.Description = request.Description;
            }
        }

        if (request.IsActive.HasValue)
        {
            rule.IsActive = request.IsActive.Value;
            if (rule.DefaultTaxRate is not null)
            {
                rule.DefaultTaxRate.IsActive = request.IsActive.Value;
            }
        }

        rule.Touch();
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
