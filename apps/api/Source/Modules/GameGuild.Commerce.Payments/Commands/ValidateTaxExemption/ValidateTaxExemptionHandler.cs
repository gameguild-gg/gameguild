using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for ValidateTaxExemptionCommand
/// </summary>
public sealed class ValidateTaxExemptionHandler(IApplicationDbContext context) : ICommandHandler<ValidateTaxExemptionCommand, TaxExemptionValidationResult>
{
    public async Task<TaxExemptionValidationResult> Handle(ValidateTaxExemptionCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.JurisdictionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExemptionType);

        var warnings = new List<string>();
        var transactionDate = request.TransactionDate ?? SystemClock.UtcNow;
        var jurisdictionCode = request.JurisdictionCode.Trim().ToUpperInvariant();
        var exemptionTypeParsed = Enum.TryParse<TaxExemptionType>(request.ExemptionType, true, out var exemptionType);

        if (!exemptionTypeParsed)
        {
            warnings.Add($"Unknown exemption type '{request.ExemptionType}'.");
        }

        if (request.CustomerId.HasValue)
        {
            var query = context.Set<CustomerTaxExemption>()
                .AsNoTracking()
                .Where(exemption => exemption.CustomerId == request.CustomerId.Value)
                .Where(exemption => exemption.Status == TaxExemptionStatus.Active)
                .Where(exemption => exemption.VerificationStatus == ExemptionVerificationStatus.Verified)
                .Where(exemption => exemption.ValidFrom <= transactionDate)
                .Where(exemption => exemption.ValidUntil == null || exemption.ValidUntil >= transactionDate)
                .Where(exemption => exemption.JurisdictionCode == jurisdictionCode
                                    || jurisdictionCode.StartsWith(exemption.JurisdictionCode + "-"));

            if (!string.IsNullOrWhiteSpace(request.ExemptionCertificateNumber))
            {
                var certificate = request.ExemptionCertificateNumber.Trim();
                query = query.Where(exemption => exemption.CertificateNumber == certificate);
            }

            if (exemptionTypeParsed)
            {
                query = query.Where(exemption => exemption.ExemptionType == exemptionType);
            }

            var exemption = await query
                .OrderByDescending(item => item.ValidFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (exemption is not null)
            {
                return new TaxExemptionValidationResult(
                    true,
                    exemption.ExemptionType.ToString(),
                    1.0m,
                    exemption.ValidFrom,
                    exemption.ValidUntil,
                    "Exemption certificate is active and verified",
                    warnings.Count == 0 ? null : warnings);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerVatNumber) && IsVatFormatValid(request.CustomerVatNumber, jurisdictionCode))
        {
            return new TaxExemptionValidationResult(
                true,
                request.ExemptionType,
                1.0m,
                transactionDate,
                null,
                "VAT number format is valid for reverse-charge handling",
                warnings.Count == 0 ? null : warnings);
        }

        warnings.Add("No active verified exemption was found for the supplied jurisdiction, customer, and certificate.");

        return new TaxExemptionValidationResult(
            false,
            request.ExemptionType,
            0m,
            null,
            null,
            "Tax exemption could not be validated",
            warnings);
    }

    private static bool IsVatFormatValid(string vatNumber, string jurisdictionCode)
    {
        var countryCode = jurisdictionCode.Split('-', 2)[0];
        var normalizedVat = vatNumber.Replace(" ", string.Empty).ToUpperInvariant();

        return normalizedVat.StartsWith(countryCode, StringComparison.Ordinal)
               && normalizedVat.Length is >= 8 and <= 12;
    }
}
