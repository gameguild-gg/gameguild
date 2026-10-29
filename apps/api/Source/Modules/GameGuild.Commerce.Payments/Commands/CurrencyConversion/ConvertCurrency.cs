using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Business rule identifiers raised by the currency conversion query.</summary>
public static class CurrencyConversionRules
{
    /// <summary>The target currency is not enabled for the tenant.</summary>
    public const string TargetCurrencyNotEnabled = "TargetCurrencyNotEnabled";
}

/// <summary>One conversion request entry: an amount denominated in <paramref name="Currency"/>.</summary>
/// <param name="Amount">Amount to convert (≥ 0).</param>
/// <param name="Currency">ISO 4217 code the amount is denominated in (any casing).</param>
public sealed record CurrencyConversionItemInput(decimal Amount, string Currency);

/// <summary>
///     Per-item conversion outcome (issue #412 spec D). Successful items carry the converted
///     amount rounded to the target currency's ISO 4217 minor-unit exponent and the quotation
///     metadata that produced it; failed items carry <paramref name="Error"/> instead so one
///     bad entry never aborts a batch.
/// </summary>
/// <param name="SourceAmount">Requested amount, unchanged.</param>
/// <param name="SourceCurrency">Normalized source ISO 4217 code.</param>
/// <param name="ConvertedAmount">Converted amount in <paramref name="TargetCurrency"/> (minor-unit rounded), when the item succeeded.</param>
/// <param name="TargetCurrency">Normalized target ISO 4217 code.</param>
/// <param name="Rate">Quotation used (quote units per one source unit); null for same-currency identity items.</param>
/// <param name="RateSource">Provenance label of the quotation (e.g. "manual-override", provider label); null for identity items.</param>
/// <param name="RateAsOfUtc">Moment the quotation was published; null for identity items.</param>
/// <param name="RateEffectiveDateUtc">Effective (data) date of the quotation, when known (historical lookups); otherwise null.</param>
/// <param name="RateProvider">Provider that produced the quotation, when known (historical lookups); otherwise null.</param>
/// <param name="IsIdentity">True when source equals target and no FX quotation was involved.</param>
/// <param name="Error">Failure reason for this item, when it failed; null on success.</param>
public sealed record CurrencyConversionItemResult(
    decimal SourceAmount,
    string SourceCurrency,
    decimal? ConvertedAmount,
    string TargetCurrency,
    decimal? Rate,
    string? RateSource,
    DateTime? RateAsOfUtc,
    DateTime? RateEffectiveDateUtc,
    string? RateProvider,
    bool IsIdentity,
    string? Error);

/// <summary>Batch conversion response.</summary>
/// <param name="TargetCurrency">Normalized target ISO 4217 code shared by every item.</param>
/// <param name="AsOfUtc">Historical lookup date, when requested; null means current quotations.</param>
/// <param name="Items">One outcome per requested entry, in request order.</param>
public sealed record CurrencyConversionResult(
    string TargetCurrency,
    DateTime? AsOfUtc,
    IReadOnlyList<CurrencyConversionItemResult> Items);

/// <summary>
///     Command (issue #412 spec D): convert one or many amounts into a target currency. Current
///     requests resolve quotations through the platform FX seam (operator overrides and synced
///     market grids behind <see cref="PersistedFxRateProvider"/>, TTL-cached); requests with
///     <paramref name="AsOfUtc"/> resolve the newest persisted snapshot whose effective date is
///     at or before that date — the exact direction first, then the inverse of the opposite
///     pair (derived arithmetic, but the documented inverse of an auditable quotation). Both
///     currencies must be enabled for the tenant. Same-currency entries are identity
///     conversions and need no quotation.
/// </summary>
/// <param name="TargetCurrency">ISO 4217 code every amount is converted into (any casing).</param>
/// <param name="Items">Entries to convert (1..50).</param>
/// <param name="AsOfUtc">Optional historical lookup instant (UTC). Null = current quotations.</param>
public sealed record ConvertCurrencyCommand(
    string TargetCurrency,
    IReadOnlyList<CurrencyConversionItemInput> Items,
    DateTime? AsOfUtc = null) : ICommand<CurrencyConversionResult>;

/// <summary>Validator for <see cref="ConvertCurrencyCommand"/>.</summary>
public sealed class ConvertCurrencyCommandValidator : AbstractValidator<ConvertCurrencyCommand>
{
    /// <summary>Initializes the validator.</summary>
    public ConvertCurrencyCommandValidator()
    {
        RuleFor(query => query.TargetCurrency)
            .NotEmpty().WithMessage("Target currency is required.")
            .Must(code => !string.IsNullOrWhiteSpace(code) && code.Trim().Length == 3)
            .WithMessage("Target currency must be a three-letter ISO 4217 code.");

        RuleFor(query => query.Items)
            .NotEmpty().WithMessage("At least one conversion item is required.")
            .Must(items => items.Count <= 50).WithMessage("At most 50 conversion items per request.");

        RuleForEach(query => query.Items).ChildRules(item =>
        {
            item.RuleFor(entry => entry.Amount).GreaterThanOrEqualTo(0m);
            item.RuleFor(entry => entry.Currency)
                .NotEmpty().WithMessage("Item currency is required.")
                .Must(code => !string.IsNullOrWhiteSpace(code) && code.Trim().Length == 3)
                .WithMessage("Item currency must be a three-letter ISO 4217 code.");
        });

        RuleFor(query => query.AsOfUtc)
            .Must(asOf => asOf is null || asOf.Value <= SystemClock.UtcNow)
            .WithMessage("Historical conversion date cannot be in the future.");
    }
}

/// <summary>Handler for <see cref="ConvertCurrencyCommand"/>.</summary>
public sealed class ConvertCurrencyCommandHandler(
    IApplicationDbContext context,
    IExchangeRateProvider rates,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<ConvertCurrencyCommand, CurrencyConversionResult>
{
    private const int MaxItems = 50;

    /// <inheritdoc />
    public async Task<CurrencyConversionResult> Handle(ConvertCurrencyCommand request, CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.TenantId is not { } tenantId || tenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Currency conversion requires an authenticated tenant context.");
        }

        var target = NormalizeCurrency(request.TargetCurrency);
        if (!Iso4217CurrencyCatalog.Exists(target))
        {
            throw new BusinessRuleViolationException(
                CurrencyConfigurationRules.UnknownCurrency,
                $"Currency '{target}' is not a known ISO 4217 code.",
                new { Currency = target });
        }

        // Both legs must be enabled currencies for the tenant (issue #412 spec D: "any two
        // enabled currencies"). The target gate is request-wide; per-item source gates are
        // per-item outcomes so a batch keeps its good entries.
        var enabled = await context.Set<CurrencyConfiguration>()
            .AsNoTracking()
            .Where(configuration => configuration.TenantId == tenantId && configuration.IsEnabled)
            .Select(configuration => configuration.CurrencyCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var enabledSet = new HashSet<string>(enabled, StringComparer.Ordinal);
        if (!enabledSet.Contains(target))
        {
            throw new BusinessRuleViolationException(
                CurrencyConversionRules.TargetCurrencyNotEnabled,
                $"Currency '{target}' is not enabled for this tenant; conversion targets must be enabled currencies.",
                new { Currency = target, tenantId });
        }

        var items = request.Items is { Count: > 0 } list ? list.Take(MaxItems).ToList() : [];
        var asOfUtc = request.AsOfUtc;
        var results = new List<CurrencyConversionItemResult>(items.Count);
        foreach (var item in items)
        {
            results.Add(await ConvertItemAsync(item, target, asOfUtc, enabledSet, cancellationToken).ConfigureAwait(false));
        }

        return new CurrencyConversionResult(target, asOfUtc, results);
    }

    private async Task<CurrencyConversionItemResult> ConvertItemAsync(
        CurrencyConversionItemInput item,
        string target,
        DateTime? asOfUtc,
        HashSet<string> enabledCurrencies,
        CancellationToken cancellationToken)
    {
        string source;
        try
        {
            source = NormalizeCurrency(item.Currency);
        }
        catch (ArgumentException)
        {
            return Failed(item, target, "Item currency must be a three-letter ISO 4217 code.");
        }

        if (!Iso4217CurrencyCatalog.Exists(source))
        {
            return Failed(item, target, $"Currency '{source}' is not a known ISO 4217 code.");
        }

        if (!enabledCurrencies.Contains(source))
        {
            return Failed(item, target, $"Currency '{source}' is not enabled for this tenant.");
        }

        if (source == target)
        {
            // Identity is exact arithmetic, not an FX assumption: no quotation involved.
            return new CurrencyConversionItemResult(
                item.Amount, source, item.Amount, target,
                Rate: null, RateSource: null, RateAsOfUtc: null, RateEffectiveDateUtc: null, RateProvider: null,
                IsIdentity: true, Error: null);
        }

        return asOfUtc is { } historical
            ? await ConvertHistoricalAsync(item, source, target, historical, cancellationToken).ConfigureAwait(false)
            : await ConvertCurrentAsync(item, source, target, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CurrencyConversionItemResult> ConvertCurrentAsync(
        CurrencyConversionItemInput item,
        string source,
        string target,
        CancellationToken cancellationToken)
    {
        var rate = await rates.GetRateAsync(source, target, cancellationToken).ConfigureAwait(false);
        if (rate is null)
        {
            return Failed(item, target, NoRateMessage(source, target));
        }

        // Same math as CurrencyConversionService.ConvertAsync: multiply, then let Money round
        // to the target currency's ISO 4217 minor-unit exponent (platform AwayFromZero).
        var converted = new Money(item.Amount * rate.Rate, target);
        return new CurrencyConversionItemResult(
            item.Amount, source, converted.Amount, target,
            rate.Rate, rate.Source, rate.AsOfUtc.UtcDateTime, null, null,
            IsIdentity: false, Error: null);
    }

    private async Task<CurrencyConversionItemResult> ConvertHistoricalAsync(
        CurrencyConversionItemInput item,
        string source,
        string target,
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        // Newest persisted snapshot whose effective (data) date is at or before the requested
        // instant: exact direction first; only then the inverse of the opposite pair.
        var effectiveDate = asOfUtc.Date;
        var direct = await context.Set<FxRateSnapshot>()
            .AsNoTracking()
            .Where(snapshot => snapshot.BaseCurrency == source
                && snapshot.QuoteCurrency == target
                && snapshot.EffectiveDateUtc <= effectiveDate)
            .OrderByDescending(snapshot => snapshot.EffectiveDateUtc)
            .ThenByDescending(snapshot => snapshot.AsOfUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal rate;
        string rateSource;
        DateTime rateAsOf;
        DateTime effective;
        string provider;
        if (direct is not null)
        {
            rate = direct.Rate;
            rateSource = direct.Source;
            rateAsOf = direct.AsOfUtc;
            effective = direct.EffectiveDateUtc;
            provider = direct.ProviderName;
        }
        else
        {
            var inverse = await context.Set<FxRateSnapshot>()
                .AsNoTracking()
                .Where(snapshot => snapshot.BaseCurrency == target
                    && snapshot.QuoteCurrency == source
                    && snapshot.EffectiveDateUtc <= effectiveDate)
                .OrderByDescending(snapshot => snapshot.EffectiveDateUtc)
                .ThenByDescending(snapshot => snapshot.AsOfUtc)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (inverse is null)
            {
                return Failed(item, target, $"No historical exchange rate available for {source} → {target} at {asOfUtc:yyyy-MM-dd}.");
            }

            rate = decimal.Round(1m / inverse.Rate, 12, MidpointRounding.AwayFromZero);
            rateSource = $"{inverse.Source} (inverted {target}→{source})";
            rateAsOf = inverse.AsOfUtc;
            effective = inverse.EffectiveDateUtc;
            provider = inverse.ProviderName;
        }

        var converted = new Money(item.Amount * rate, target);
        return new CurrencyConversionItemResult(
            item.Amount, source, converted.Amount, target,
            rate, rateSource, rateAsOf, effective, provider,
            IsIdentity: false, Error: null);
    }

    private static string NoRateMessage(string source, string target)
        => $"No exchange rate available for {source} → {target}: the configured provider has no quotation for the pair.";

    private static CurrencyConversionItemResult Failed(CurrencyConversionItemInput item, string target, string error)
        => new(item.Amount, item.Currency?.Trim().ToUpperInvariant() ?? string.Empty, null, target, null, null, null, null, null, false, error);

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("Currency must be a three-letter ISO 4217 code.", nameof(currency));
        }

        return currency.Trim().ToUpperInvariant();
    }
}
