using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to calculate tax for a transaction
/// </summary>
public sealed record CalculateTaxCommand(
    string JurisdictionCode,
    decimal Amount,
    string Currency,
    string CustomerType,
    string? ProductCategory = null,
    string? CustomerVatNumber = null,
    bool IsTaxInclusive = false,
    DateTime? TransactionDate = null,
    IReadOnlyList<string>? ApplicableExemptions = null
) : ICommand<TaxCalculationResult>;
