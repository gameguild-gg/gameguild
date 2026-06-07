using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for CreateTaxJurisdictionCommand
/// </summary>
public sealed class CreateTaxJurisdictionHandler(IApplicationDbContext context) : ICommandHandler<CreateTaxJurisdictionCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaxJurisdictionCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Code);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Country);

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await context.Set<TaxJurisdiction>()
            .AnyAsync(jurisdiction => jurisdiction.Code == code, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Tax jurisdiction '{code}' already exists.");
        }

        var jurisdiction = new TaxJurisdiction
        {
            Code = code,
            Name = request.Name.Trim(),
            Type = string.IsNullOrWhiteSpace(request.State)
                ? TaxJurisdictionType.Country
                : TaxJurisdictionType.State,
            IsActive = true
        };

        var defaultRate = new TaxRate
        {
            TaxJurisdictionId = jurisdiction.Id,
            TaxType = TaxProjectionMapper.ParseTaxType(request.TaxType),
            Rate = TaxProjectionMapper.NormalizeRate(request.DefaultRate),
            EffectiveFrom = SystemClock.UtcNow,
            Description = $"Default {request.TaxType} rate for {code}"
        };

        await context.Set<TaxJurisdiction>().AddAsync(jurisdiction, cancellationToken);
        await context.Set<TaxRate>().AddAsync(defaultRate, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return jurisdiction.Id;
    }
}
