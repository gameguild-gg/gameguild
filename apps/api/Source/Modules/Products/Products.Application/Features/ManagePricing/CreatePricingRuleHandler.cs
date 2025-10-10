using GameGuild.Modules.Products.Domain.Entities;
using GameGuild.Database;
using GameGuild.CQRS;

namespace GameGuild.Modules.Products.Application.Features.ManagePricing;

/// <summary>Handler for creating pricing rules</summary>
public class CreatePricingRuleHandler : IRequestHandler<CreatePricingRuleCommand, Guid>
{
    private readonly ApplicationDbContext _context;

    public CreatePricingRuleHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreatePricingRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = new PricingRule
        {
            ProductId = request.ProductId,
            Name = request.Name,
            Description = request.Description,
            RuleType = (PricingRuleType)request.RuleType,
            Priority = request.Priority,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MinQuantity = request.MinQuantity,
            MaxQuantity = request.MaxQuantity,
            DiscountPercentage = request.DiscountPercentage,
            FixedPrice = request.FixedPrice,
            Region = request.Region,
            CustomerSegment = request.CustomerSegment,
            IsActive = true
        };

        _context.Set<PricingRule>().Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

        return rule.Id;
    }
}
