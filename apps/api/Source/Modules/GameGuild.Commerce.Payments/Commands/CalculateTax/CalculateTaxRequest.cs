namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax calculation request DTO
/// </summary>
public class CalculateTaxRequest
{
    public required string JurisdictionCode { get; init; }

    public required decimal Amount { get; init; }

    public required string Currency { get; init; }

    public required string CustomerType { get; init; }

    public string? ProductCategory { get; init; }

    public string? CustomerVatNumber { get; init; }

    public bool IsTaxInclusive { get; init; }

    public DateTime? TransactionDate { get; init; }

    public IReadOnlyList<string>? ApplicableExemptions { get; init; }
}
