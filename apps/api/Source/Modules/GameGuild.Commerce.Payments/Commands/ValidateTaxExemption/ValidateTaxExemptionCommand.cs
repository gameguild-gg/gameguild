using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to validate a tax exemption
/// </summary>
public record ValidateTaxExemptionCommand(
    string JurisdictionCode,
    string ExemptionType,
    string? ExemptionCertificateNumber,
    string? CustomerVatNumber,
    Guid? CustomerId,
    DateTime? TransactionDate) : ICommand<TaxExemptionValidationResult>;
