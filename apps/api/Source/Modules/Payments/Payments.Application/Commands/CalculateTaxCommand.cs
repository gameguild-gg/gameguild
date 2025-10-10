using GameGuild.Modules.Payments.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Commands;

/// <summary>
///     Calculate tax for a transaction
/// </summary>
public record CalculateTaxCommand : IRequest<TaxCalculationResult>
{
    public required string JurisdictionCode { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string CustomerType { get; init; }
    public string? ProductCategory { get; init; }
    public string? CustomerVatNumber { get; init; }
    public bool IsTaxInclusive { get; init; }
    public DateTime? TransactionDate { get; init; }
}
