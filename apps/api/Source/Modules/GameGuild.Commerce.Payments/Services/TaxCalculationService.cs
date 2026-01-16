using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax calculation service implementation
/// </summary>
public class TaxCalculationService(IApplicationDbContext context, ILogger<TaxCalculationService> logger) : ITaxCalculationService
{
    private DbSet<TaxJurisdiction> TaxJurisdictions { get => context.Set<TaxJurisdiction>(); }

    // ReSharper disable once UnusedMember.Local - Reserved for direct TaxRule queries if needed
    private DbSet<TaxRule> TaxRules { get => context.Set<TaxRule>(); }

    private DbSet<TaxRate> TaxRates { get => context.Set<TaxRate>(); }
    
    private DbSet<CustomerTaxExemption> CustomerTaxExemptions { get => context.Set<CustomerTaxExemption>(); }

    public async Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Calculating tax for jurisdiction {Jurisdiction}, amount {Amount}, customer type {CustomerType}", request.JurisdictionCode, request.Amount, request.CustomerType);

        // Get jurisdiction
        var jurisdiction = await TaxJurisdictions.Include(j => j.TaxRules).ThenInclude(r => r.DefaultTaxRate).FirstOrDefaultAsync(j => j.Code == request.JurisdictionCode && j.IsActive, cancellationToken);

        if (jurisdiction == null)
        {
            logger.LogWarning("Tax jurisdiction {Jurisdiction} not found", request.JurisdictionCode);

            return CreateZeroTaxResult(request);
        }

        // Check for tax exemptions
        if (request.ApplicableExemptions.Count > 0) { return CreateExemptResult(request, jurisdiction, "Customer tax exemption"); }

        // Check for reverse charge (EU B2B with valid VAT)
        if (request.CustomerType == CustomerType.B2B && !string.IsNullOrEmpty(request.CustomerVatNumber) && jurisdiction.IsReverseChargeApplicable) { return CreateReverseChargeResult(request, jurisdiction); }

        // Get applicable tax rules
        var applicableRules = jurisdiction.TaxRules.Where(r => r.IsEffective(request.TransactionDate) && r.AppliesToTransaction(request.Amount, request.CustomerType)).OrderByDescending(r => r.Priority).ToList();

        if (applicableRules.Count == 0)
        {
            logger.LogWarning("No applicable tax rules found for jurisdiction {Jurisdiction}", request.JurisdictionCode);

            return CreateZeroTaxResult(request);
        }

        // Get tax rate
        var taxRate = await GetTaxRateAsync(request.JurisdictionCode, TaxType.VAT, request.ProductCategory, request.TransactionDate, cancellationToken);

        if (taxRate == null)
        {
            logger.LogWarning("No tax rate found for jurisdiction {Jurisdiction}", request.JurisdictionCode);

            return CreateZeroTaxResult(request);
        }

        // Calculate tax
        return CalculateTax(request, jurisdiction, taxRate, applicableRules.First());
    }

    public async Task<TaxRate?> GetTaxRateAsync(string jurisdictionCode, TaxType taxType, string? productCategory = null, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var date = effectiveDate ?? DateTime.UtcNow;

        var query = TaxRates.Include(r => r.TaxJurisdiction)
            .Where(r => r.TaxJurisdiction.Code == jurisdictionCode && r.TaxType == taxType && r.IsActive && r.EffectiveFrom <= date && (r.EffectiveTo == null || r.EffectiveTo >= date));

        // Try exact product category match first
        if (!string.IsNullOrEmpty(productCategory))
        {
            var categoryMatch = await query.FirstOrDefaultAsync(r => r.ProductCategory == productCategory, cancellationToken);

            if (categoryMatch != null) return categoryMatch;
        }

        // Fall back to default (null category)
        return await query.FirstOrDefaultAsync(r => r.ProductCategory == null, cancellationToken);
    }

    public async Task<bool> ValidateTaxExemptionAsync(Guid customerId, string jurisdictionCode, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Validating tax exemption for customer {CustomerId} in jurisdiction {JurisdictionCode}", 
            customerId, jurisdictionCode);

        var now = DateTime.UtcNow;
        var normalizedJurisdiction = jurisdictionCode.ToUpperInvariant();

        // Query the customer tax exemption registry for valid exemptions
        var exemption = await CustomerTaxExemptions
            .Where(e => e.CustomerId == customerId)
            .Where(e => e.JurisdictionCode == normalizedJurisdiction)
            .Where(e => e.Status == TaxExemptionStatus.Active)
            .Where(e => e.VerificationStatus == ExemptionVerificationStatus.Verified)
            .Where(e => e.ValidFrom <= now)
            .Where(e => e.ValidUntil == null || e.ValidUntil >= now)
            .FirstOrDefaultAsync(cancellationToken);

        if (exemption != null)
        {
            logger.LogInformation(
                "Valid tax exemption found for customer {CustomerId} in jurisdiction {JurisdictionCode}: " +
                "Certificate {CertificateNumber}, Type {ExemptionType}",
                customerId, jurisdictionCode, exemption.CertificateNumber, exemption.ExemptionType);
            return true;
        }

        // Check for parent jurisdiction exemptions (e.g., US exemption may cover US-CA)
        if (normalizedJurisdiction.Contains('-'))
        {
            var parentJurisdiction = normalizedJurisdiction.Split('-')[0];
            var parentExemption = await CustomerTaxExemptions
                .Where(e => e.CustomerId == customerId)
                .Where(e => e.JurisdictionCode == parentJurisdiction)
                .Where(e => e.Status == TaxExemptionStatus.Active)
                .Where(e => e.VerificationStatus == ExemptionVerificationStatus.Verified)
                .Where(e => e.ValidFrom <= now)
                .Where(e => e.ValidUntil == null || e.ValidUntil >= now)
                .FirstOrDefaultAsync(cancellationToken);

            if (parentExemption != null)
            {
                logger.LogInformation(
                    "Valid parent jurisdiction tax exemption found for customer {CustomerId}: " +
                    "Parent {ParentJurisdiction} covers {ChildJurisdiction}",
                    customerId, parentJurisdiction, jurisdictionCode);
                return true;
            }
        }

        logger.LogDebug("No valid tax exemption found for customer {CustomerId} in jurisdiction {JurisdictionCode}", 
            customerId, jurisdictionCode);
        return false;
    }

    public async Task<IEnumerable<TaxJurisdiction>> GetTaxJurisdictionsAsync(CancellationToken cancellationToken = default)
    {
        var jurisdictions = await TaxJurisdictions.Include(j => j.ParentJurisdiction).Include(j => j.ChildJurisdictions).Where(j => j.IsActive).OrderBy(j => j.Type).ThenBy(j => j.Name).ToListAsync(cancellationToken);

        return jurisdictions;
    }

    public async Task<bool> ValidateVatNumberAsync(string vatNumber, string countryCode, CancellationToken cancellationToken = default)
    {
        // VAT number validation with basic format checking
        // 
        // Implementation notes:
        // - For production, integrate with EU VIES (VAT Information Exchange System) API
        // - VIES endpoint: https://ec.europa.eu/taxation_customs/vies/services/checkVatService
        // - UK VAT numbers use HMRC API after Brexit
        //
        // Current implementation: Basic format validation (safe fallback)
        // This allows B2B transactions to proceed with format-valid VAT numbers.
        // Invalid formats are rejected, preventing obvious data entry errors.
        
        logger.LogDebug("Validating VAT number {VatNumber} for country {CountryCode}", 
            vatNumber, countryCode);
            
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(vatNumber)) return false;

        // Remove spaces and convert to uppercase
        vatNumber = vatNumber.Replace(" ", "").ToUpperInvariant();

        // Check if starts with country code
        if (!vatNumber.StartsWith(countryCode.ToUpperInvariant())) return false;

        // Basic length check (VAT numbers are typically 8-12 characters)
        return vatNumber.Length >= 8 && vatNumber.Length <= 12;
    }

    #region Private Helper Methods

    private TaxCalculationResult CalculateTax(TaxCalculationRequest request, TaxJurisdiction jurisdiction, TaxRate taxRate, TaxRule taxRule)
    {
        decimal subtotal;
        decimal taxAmount;
        decimal total;

        if (request.IsTaxInclusive || taxRule.IsTaxInclusive)
        {
            // Tax is included in the amount
            total = request.Amount;
            subtotal = total / (1 + taxRate.Rate / 100);
            taxAmount = total - subtotal;
        }
        else
        {
            // Tax is added to the amount
            subtotal = request.Amount;
            taxAmount = subtotal * (taxRate.Rate / 100);
            total = subtotal + taxAmount;
        }

        var breakdown = new TaxBreakdown
        {
            TaxType = taxRate.TaxType, Description = taxRate.Description ?? string.Empty, Rate = taxRate.Rate, TaxableAmount = subtotal, TaxAmount = taxAmount, JurisdictionCode = jurisdiction.Code
        };

        return new TaxCalculationResult
        {
            SubtotalAmount = Math.Round(subtotal, 2),
            TaxAmount = Math.Round(taxAmount, 2),
            TotalAmount = Math.Round(total, 2),
            EffectiveTaxRate = taxRate.Rate,
            JurisdictionCode = jurisdiction.Code,
            JurisdictionName = jurisdiction.Name,
            TaxType = taxRate.TaxType,
            TaxDescription = taxRate.Description ?? string.Empty,
            IsTaxExempt = false,
            IsReverseCharge = false,
            TaxBreakdowns = new List<TaxBreakdown> { breakdown }
        };
    }

    private TaxCalculationResult CreateZeroTaxResult(TaxCalculationRequest request)
    {
        return new TaxCalculationResult
        {
            SubtotalAmount = request.Amount,
            TaxAmount = 0,
            TotalAmount = request.Amount,
            EffectiveTaxRate = 0,
            JurisdictionCode = request.JurisdictionCode,
            JurisdictionName = "Unknown",
            TaxType = TaxType.Other,
            TaxDescription = "No tax applicable",
            IsTaxExempt = false,
            IsReverseCharge = false,
            TaxBreakdowns = new List<TaxBreakdown>()
        };
    }

    private TaxCalculationResult CreateExemptResult(TaxCalculationRequest request, TaxJurisdiction jurisdiction, string exemptionReason)
    {
        return new TaxCalculationResult
        {
            SubtotalAmount = request.Amount,
            TaxAmount = 0,
            TotalAmount = request.Amount,
            EffectiveTaxRate = 0,
            JurisdictionCode = jurisdiction.Code,
            JurisdictionName = jurisdiction.Name,
            TaxType = TaxType.VAT,
            TaxDescription = "Tax exempt",
            IsTaxExempt = true,
            IsReverseCharge = false,
            ExemptionReason = exemptionReason,
            TaxBreakdowns = new List<TaxBreakdown>()
        };
    }

    private TaxCalculationResult CreateReverseChargeResult(TaxCalculationRequest request, TaxJurisdiction jurisdiction)
    {
        return new TaxCalculationResult
        {
            SubtotalAmount = request.Amount,
            TaxAmount = 0,
            TotalAmount = request.Amount,
            EffectiveTaxRate = 0,
            JurisdictionCode = jurisdiction.Code,
            JurisdictionName = jurisdiction.Name,
            TaxType = TaxType.VAT,
            TaxDescription = "Reverse charge (B2B)",
            IsTaxExempt = false,
            IsReverseCharge = true,
            ExemptionReason = "EU B2B reverse charge mechanism",
            TaxBreakdowns = new List<TaxBreakdown>()
        };
    }

    public async Task<IEnumerable<TaxRule>> GetApplicableTaxRulesAsync(string jurisdictionCode, CustomerType customerType, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var date = effectiveDate ?? DateTime.UtcNow;

        return await context.Set<TaxRule>()
            .Include(tr => tr.TaxJurisdiction)
            .Where(tr => tr.TaxJurisdiction.Code == jurisdictionCode &&
                         (tr.CustomerTypeFilter == null || tr.CustomerTypeFilter == customerType) &&
                         tr.IsActive &&
                         (tr.EffectiveFrom == null || tr.EffectiveFrom <= date) &&
                         (tr.EffectiveTo == null || tr.EffectiveTo >= date)
            )
            .OrderBy(tr => tr.Priority)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion
}
