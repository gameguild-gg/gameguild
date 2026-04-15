using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Tax calculation service implementation with in-memory caching for tax rate lookups.
///     Caching strategy: Tax rates and jurisdictions change infrequently, so we use a sliding
///     expiration of 30 minutes with an absolute expiration of 2 hours to balance freshness
///     with database load reduction.
/// </summary>
public class TaxCalculationService : ITaxCalculationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TaxCalculationService> _logger;
    private readonly IMemoryCache _cache;

    // Cache configuration constants
    private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CacheAbsoluteExpiration = TimeSpan.FromHours(2);

    // Cache key prefixes
    private const string TaxRateCacheKeyPrefix = "TaxRate:";
    private const string JurisdictionCacheKeyPrefix = "TaxJurisdiction:";
    private const string ExemptionCacheKeyPrefix = "TaxExemption:";
    private const string AllJurisdictionsCacheKey = "TaxJurisdictions:All";

    public TaxCalculationService(
        IApplicationDbContext context,
        ILogger<TaxCalculationService> logger,
        IMemoryCache cache)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    private DbSet<TaxJurisdiction> TaxJurisdictions => _context.Set<TaxJurisdiction>();

    // ReSharper disable once UnusedMember.Local - Reserved for direct TaxRule queries if needed
    private DbSet<TaxRule> TaxRules => _context.Set<TaxRule>();

    private DbSet<TaxRate> TaxRates => _context.Set<TaxRate>();

    private DbSet<CustomerTaxExemption> CustomerTaxExemptions => _context.Set<CustomerTaxExemption>();

    /// <summary>
    ///     Creates cache entry options with standard sliding and absolute expiration.
    /// </summary>
    private static MemoryCacheEntryOptions CreateCacheEntryOptions() => new()
    {
        SlidingExpiration = CacheSlidingExpiration,
        AbsoluteExpirationRelativeToNow = CacheAbsoluteExpiration,
        Size = 1
    };

    public async Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating tax for jurisdiction {Jurisdiction}, amount {Amount}, customer type {CustomerType}", request.JurisdictionCode, request.Amount, request.CustomerType);

        // Get jurisdiction with caching
        var jurisdiction = await GetCachedJurisdictionAsync(request.JurisdictionCode, cancellationToken).ConfigureAwait(false);

        if (jurisdiction == null)
        {
            _logger.LogWarning("Tax jurisdiction {Jurisdiction} not found", request.JurisdictionCode);

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
            _logger.LogWarning("No applicable tax rules found for jurisdiction {Jurisdiction}", request.JurisdictionCode);

            return CreateZeroTaxResult(request);
        }

        // Get tax rate with caching
        var taxRate = await GetTaxRateAsync(request.JurisdictionCode, TaxType.VAT, request.ProductCategory, request.TransactionDate, cancellationToken).ConfigureAwait(false);

        if (taxRate == null)
        {
            _logger.LogWarning("No tax rate found for jurisdiction {Jurisdiction}", request.JurisdictionCode);

            return CreateZeroTaxResult(request);
        }

        // Calculate tax
        return CalculateTax(request, jurisdiction, taxRate, applicableRules.First());
    }

    /// <summary>
    ///     Gets a tax jurisdiction from cache or database.
    /// </summary>
    private async Task<TaxJurisdiction?> GetCachedJurisdictionAsync(string jurisdictionCode, CancellationToken cancellationToken)
    {
        var cacheKey = $"{JurisdictionCacheKeyPrefix}{jurisdictionCode.ToUpperInvariant()}";

        if (_cache.TryGetValue(cacheKey, out TaxJurisdiction? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for jurisdiction {JurisdictionCode}", jurisdictionCode);
            return cached;
        }

        _logger.LogDebug("Cache miss for jurisdiction {JurisdictionCode}, loading from database", jurisdictionCode);

        var jurisdiction = await TaxJurisdictions
            .Include(j => j.TaxRules)
            .ThenInclude(r => r.DefaultTaxRate)
            .FirstOrDefaultAsync(j => j.Code == jurisdictionCode && j.IsActive, cancellationToken).ConfigureAwait(false);

        if (jurisdiction != null)
        {
            _cache.Set(cacheKey, jurisdiction, CreateCacheEntryOptions());
        }

        return jurisdiction;
    }

    public async Task<TaxRate?> GetTaxRateAsync(string jurisdictionCode, TaxType taxType, string? productCategory = null, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var date = effectiveDate ?? SystemClock.UtcNow;

        // Create a cache key that includes all relevant parameters
        // Note: We cache based on date truncated to the day to allow reasonable caching while still
        // respecting effective date ranges
        var cacheKey = $"{TaxRateCacheKeyPrefix}{jurisdictionCode.ToUpperInvariant()}:{taxType}:{productCategory ?? "default"}:{date:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out TaxRate? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for tax rate: {JurisdictionCode}, {TaxType}, {Category}", jurisdictionCode, taxType, productCategory);
            return cached;
        }

        _logger.LogDebug("Cache miss for tax rate: {JurisdictionCode}, {TaxType}, {Category}, loading from database", jurisdictionCode, taxType, productCategory);

        var query = TaxRates.Include(r => r.TaxJurisdiction)
            .Where(r => r.TaxJurisdiction.Code == jurisdictionCode && r.TaxType == taxType && r.IsActive && r.EffectiveFrom <= date && (r.EffectiveTo == null || r.EffectiveTo >= date));

        TaxRate? result = null;

        // Try exact product category match first
        if (!string.IsNullOrEmpty(productCategory))
        {
            result = await query.FirstOrDefaultAsync(r => r.ProductCategory == productCategory, cancellationToken);
        }

        // Fall back to default (null category) if no category match
        result ??= await query.FirstOrDefaultAsync(r => r.ProductCategory == null, cancellationToken);

        if (result != null)
        {
            _cache.Set(cacheKey, result, CreateCacheEntryOptions());
        }

        return result;
    }

    public async Task<bool> ValidateTaxExemptionAsync(Guid customerId, string jurisdictionCode, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating tax exemption for customer {CustomerId} in jurisdiction {JurisdictionCode}",
            customerId, jurisdictionCode);

        var now = SystemClock.UtcNow;
        var normalizedJurisdiction = jurisdictionCode.ToUpperInvariant();

        // Check cache first for tax exemption status
        var cacheKey = $"{ExemptionCacheKeyPrefix}{customerId}:{normalizedJurisdiction}";
        if (_cache.TryGetValue(cacheKey, out bool cachedResult))
        {
            _logger.LogDebug("Cache hit for tax exemption: customer {CustomerId}, jurisdiction {JurisdictionCode}", customerId, jurisdictionCode);
            return cachedResult;
        }

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
            _logger.LogInformation(
                "Valid tax exemption found for customer {CustomerId} in jurisdiction {JurisdictionCode}: " +
                "Certificate {CertificateNumber}, Type {ExemptionType}",
                customerId, jurisdictionCode, exemption.CertificateNumber, exemption.ExemptionType);
            _cache.Set(cacheKey, true, CreateCacheEntryOptions());
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
                _logger.LogInformation(
                    "Valid parent jurisdiction tax exemption found for customer {CustomerId}: " +
                    "Parent {ParentJurisdiction} covers {ChildJurisdiction}",
                    customerId, parentJurisdiction, jurisdictionCode);
                _cache.Set(cacheKey, true, CreateCacheEntryOptions());
                return true;
            }
        }

        _logger.LogDebug("No valid tax exemption found for customer {CustomerId} in jurisdiction {JurisdictionCode}",
            customerId, jurisdictionCode);
        _cache.Set(cacheKey, false, CreateCacheEntryOptions());
        return false;
    }

    public async Task<IEnumerable<TaxJurisdiction>> GetTaxJurisdictionsAsync(CancellationToken cancellationToken = default)
    {
        // Check cache for all jurisdictions
        if (_cache.TryGetValue(AllJurisdictionsCacheKey, out List<TaxJurisdiction>? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for all tax jurisdictions");
            return cached;
        }

        _logger.LogDebug("Cache miss for all tax jurisdictions, loading from database");

        var jurisdictions = await TaxJurisdictions
            .Include(j => j.ParentJurisdiction)
            .Include(j => j.ChildJurisdictions)
            .Where(j => j.IsActive)
            .OrderBy(j => j.Type)
            .ThenBy(j => j.Name)
            .ToListAsync(cancellationToken);

        _cache.Set(AllJurisdictionsCacheKey, jurisdictions, CreateCacheEntryOptions());

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

        _logger.LogDebug("Validating VAT number {VatNumber} for country {CountryCode}",
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
            // Tax is included in the amount; Rate is stored as a decimal fraction (e.g. 0.19 for 19%)
            total = request.Amount;
            subtotal = total / (1 + taxRate.Rate);
            taxAmount = total - subtotal;
        }
        else
        {
            // Tax is added to the amount; Rate is stored as a decimal fraction (e.g. 0.19 for 19%)
            subtotal = request.Amount;
            taxAmount = subtotal * taxRate.Rate;
            total = subtotal + taxAmount;
        }

        var breakdown = new TaxBreakdown
        {
            TaxType = taxRate.TaxType,
            Description = taxRate.Description ?? string.Empty,
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
        var date = effectiveDate ?? SystemClock.UtcNow;

        return await _context.Set<TaxRule>()
            .Include(tr => tr.TaxJurisdiction)
            .Where(tr => tr.TaxJurisdiction.Code == jurisdictionCode &&
                         (tr.CustomerTypeFilter == null || tr.CustomerTypeFilter == customerType) &&
                         tr.IsActive &&
                         (tr.EffectiveFrom == null || tr.EffectiveFrom <= date) &&
                         (tr.EffectiveTo == null || tr.EffectiveTo >= date)
            )
            .OrderBy(tr => tr.Priority)
            .ToListAsync(cancellationToken)
            ;
    }

    #endregion
}
