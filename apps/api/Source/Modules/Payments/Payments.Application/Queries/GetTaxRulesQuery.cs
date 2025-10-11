using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Get tax rules for a jurisdiction
/// </summary>
public record GetTaxRulesQuery : IRequest<IEnumerable<TaxRule>>
{
    public required string JurisdictionCode { get; init; }
    public string? CustomerType { get; init; }
    public DateTime? EffectiveDate { get; init; }
}
