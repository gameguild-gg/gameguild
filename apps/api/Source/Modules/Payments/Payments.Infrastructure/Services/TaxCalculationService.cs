using GameGuild.Modules.Payments.Entities;
using GameGuild.Modules.Payments.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Infrastructure.Services;

/// <summary>
///     Tax calculation service implementation
/// </summary>
public class TaxCalculationService : ITaxCalculationService
{
    private readonly DbContext _dbContext;
    private readonly ILogger<TaxCalculationService> _logger;

    public TaxCalculationService(DbContext dbContext, ILogger<TaxCalculationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TaxCalculationResult> CalculateTaxAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating tax for jurisdiction {Jurisdiction}, amount {Amount}, customer type {CustomerType}",
            request.JurisdictionCode, request.Amount, request.CustomerType);

        // Get jurisdiction
        var jurisdiction = await _dbContext.Set<TaxJurisdiction>()
            .Include(j => j.TaxRules)
            .ThenInclude(r => r.DefaultTaxRate)
            .FirstOrDefaultAsync(j => j.Code == request.JurisdictionCode && j.IsActive, cancellationToken);

        if (jurisdiction == null)
        {
            _logger.LogWarning("Tax jurisdiction {Jurisdiction} not found", request.JurisdictionCode);
            return CreateZeroTaxResult(request);
        }

        // Check for tax exemptions
        if (request.ApplicableExemptions.Count > 0)
        {
            return CreateExemptResult(request, jurisdiction, "Customer tax exemption");
        }

        // Check for reverse charge (EU B2B with valid VAT)
        if (request.CustomerType == CustomerType.B2B
            && !string.IsNullOrEmpty(request.CustomerVatNumber)
            && jurisdiction.IsReverseChargeApplicable)
        {
            return CreateReverseChargeResult(request, jurisdiction);
        }

        // Get applicable tax rules
        var applicableRules = jurisdiction.TaxRules
            .Where(r => r.IsEffective(request.TransactionDate)
                        && r.AppliesToTransaction(request.Amount, request.CustomerType))
            .OrderByDescending(r => r.Priority)
            .ToList();

        if (applicableRules.Count == 0)
        {
            _logger.LogWarning("No applicable tax rules found for jurisdiction {Jurisdiction}", request.JurisdictionCode);
            return CreateZeroTaxResult(request);
        }

        // Get tax rate
        var taxRate = await GetTaxRateAsync(
            request.JurisdictionCode,
            TaxType.VAT,
            request.ProductCategory,
            request.TransactionDate,
            cancellationToken);

        if (taxRate == null)
        {
            _logger.LogWarning("No tax rate found for jurisdiction {Jurisdiction}", request.JurisdictionCode);
            return CreateZeroTaxResult(request);
        }

        // Calculate tax
        return CalculateTax(request, jurisdiction, taxRate, applicableRules.First());
    }

    public async Task<IEnumerable<TaxRule>> GetApplicableTaxRulesAsync(
        string jurisdictionCode,
        CustomerType customerType,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        var date = effectiveDate ?? DateTime.UtcNow;

        return await _dbContext.Set<TaxRule>()
            .Include(r => r.TaxJurisdiction)
            .Include(r => r.DefaultTaxRate)
            .Where(r => r.TaxJurisdiction.Code == jurisdictionCode
                        && r.IsActive
                        && r.EffectiveFrom <= date
                        && (r.EffectiveTo == null || r.EffectiveTo >= date)
                        && (!r.CustomerTypeFilter.HasValue || r.CustomerTypeFilter == customerType))
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaxRate?> GetTaxRateAsync(
        string jurisdictionCode,
        TaxType taxType,
        string? productCategory = null,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        var date = effectiveDate ?? DateTime.UtcNow;

        var query = _dbContext.Set<TaxRate>()
            .Include(r => r.TaxJurisdiction)
            .Where(r => r.TaxJurisdiction.Code == jurisdictionCode
                        && r.TaxType == taxType
                        && r.IsActive
                        && r.EffectiveFrom <= date
                        && (r.EffectiveTo == null || r.EffectiveTo >= date));

        // Try exact product category match first
        if (!string.IsNullOrEmpty(productCategory))
        {
            var categoryMatch = await query
                .FirstOrDefaultAsync(r => r.ProductCategory == productCategory, cancellationToken);
            if (categoryMatch != null)
                return categoryMatch;
        }

        // Fall back to default (null category)
        return await query
            .FirstOrDefaultAsync(r => r.ProductCategory == null, cancellationToken);
    }

    public async Task<bool> ValidateTaxExemptionAsync(
        Guid customerId,
        string jurisdictionCode,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement customer tax exemption validation
        // This would check against a customer exemption registry
        await Task.CompletedTask;
        return false;
    }

    public async Task<IEnumerable<TaxJurisdiction>> GetTaxJurisdictionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<TaxJurisdiction>()
            .Include(j => j.ParentJurisdiction)
            .Include(j => j.ChildJurisdictions)
            .Where(j => j.IsActive)
            .OrderBy(j => j.Type)
            .ThenBy(j => j.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ValidateVatNumberAsync(
        string vatNumber,
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        // TODO: Integrate with EU VIES VAT validation service
        // For now, basic format validation
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(vatNumber))
            return false;

        // Remove spaces and convert to uppercase
        vatNumber = vatNumber.Replace(" ", "").ToUpperInvariant();

        // Check if starts with country code
        if (!vatNumber.StartsWith(countryCode.ToUpperInvariant()))
            return false;

        // Basic length check (VAT numbers are typically 8-12 characters)
        return vatNumber.Length >= 8 && vatNumber.Length <= 12;
    }

    #region Private Helper Methods

    private TaxCalculationResult CalculateTax(
        TaxCalculationRequest request,
        TaxJurisdiction jurisdiction,
        TaxRate taxRate,
        TaxRule taxRule)
    {
        decimal subtotal;
        decimal taxAmount;
        decimal total;

        if (request.IsTaxInclusive || taxRule.IsTaxInclusive)
        {
            // Tax is included in the amount
            total = request.Amount;
            subtotal = total / (1 + (taxRate.Rate / 100));
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
            TaxType = taxRate.TaxType,
            Description = taxRate.Description,
            Rate = taxRate.Rate,
            TaxableAmount = subtotal,
            TaxAmount = taxAmount,
            JurisdictionCode = jurisdiction.Code
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
            TaxDescription = taxRate.Description,
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

    private TaxCalculationResult CreateExemptResult(
        TaxCalculationRequest request,
        TaxJurisdiction jurisdiction,
        string exemptionReason)
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

    private TaxCalculationResult CreateReverseChargeResult(
        TaxCalculationRequest request,
        TaxJurisdiction jurisdiction)
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

    #endregion
}
