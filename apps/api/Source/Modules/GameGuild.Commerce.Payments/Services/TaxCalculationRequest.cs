namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax calculation request
/// </summary>
public class TaxCalculationRequest
{
    public required string JurisdictionCode { get; init; }

    public required decimal Amount { get; init; }

    public required string Currency { get; init; }

    public required CustomerType CustomerType { get; init; }

    public string? ProductCategory { get; init; }

    public string? CustomerVatNumber { get; init; }

    public bool IsTaxInclusive { get; init; }

    public DateTime TransactionDate { get; init; } = SystemClock.UtcNow;

    public List<string> ApplicableExemptions { get; init; } = new List<string>();
}
