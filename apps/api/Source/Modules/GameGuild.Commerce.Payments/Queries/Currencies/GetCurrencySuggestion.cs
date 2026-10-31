using System.Globalization;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Result of the location-based currency suggestion (issue #412 spec E).</summary>
/// <param name="SuggestedCurrency">ISO 4217 code suggested for the caller.</param>
/// <param name="Country">Detected ISO 3166-1 alpha-2 country, when available.</param>
/// <param name="Source">How the suggestion was produced ("edge-header", "tenant-base", "first-enabled", "default").</param>
/// <param name="IsEnabledForTenant">Whether the suggested currency is enabled for the caller's tenant (false when anonymous).</param>
/// <param name="FallbackReason">Why a non-country currency was suggested, when applicable.</param>
/// <param name="PrivacyNotice">GDPR data-minimization notice: nothing about the caller is stored.</param>
public sealed record CurrencySuggestionDto(
    string SuggestedCurrency,
    string? Country,
    string Source,
    bool IsEnabledForTenant,
    string? FallbackReason,
    string PrivacyNotice);

/// <summary>
///     Query (issue #412 spec E): suggest a display currency for the caller from the
///     country code delivered by the edge proxy (GDPR data-minimized: a country code, not
///     an IP; nothing is persisted, no geolocation database is consulted). The suggestion
///     respects the tenant's enablement: a disabled currency is never suggested; the tenant
///     base currency (or first enabled currency) is offered instead.
/// </summary>
/// <param name="CountryCode">ISO 3166-1 alpha-2 country from the edge headers; null/unknown falls back.</param>
[AuthorizeRequest("")]
public sealed record GetCurrencySuggestionQuery(string? CountryCode) : IQuery<CurrencySuggestionDto>;

/// <summary>Handler for <see cref="GetCurrencySuggestionQuery"/>.</summary>
public sealed class GetCurrencySuggestionQueryHandler(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor) : IQueryHandler<GetCurrencySuggestionQuery, CurrencySuggestionDto>
{
    /// <summary>Fixed GDPR notice returned with every suggestion.</summary>
    public const string PrivacyNotice =
        "Currency suggestion uses only the country code delivered by the network edge. "
        + "No IP address, location or suggestion event is stored; the chosen currency is saved only when you select it.";

    /// <inheritdoc />
    public async Task<CurrencySuggestionDto> Handle(GetCurrencySuggestionQuery request, CancellationToken cancellationToken)
    {
        var country = (request.CountryCode ?? string.Empty).Trim().ToUpperInvariant();
        var countryCurrency = MapCountryToCurrency(country);

        // Tenant context is optional: anonymous visitors get the catalog mapping and the
        // client intersects it with the enabled list once authenticated.
        var actor = actorContextAccessor.ActorContext;
        Guid? tenantId = actor.IsAuthenticated ? actor.TenantId : null;
        if (tenantId is not { } tenant || tenant == Guid.Empty)
        {
            tenantId = null;
        }

        if (tenantId is null)
        {
            return new CurrencySuggestionDto(
                countryCurrency ?? "USD",
                countryCurrency is null ? null : country,
                countryCurrency is null ? "default" : "edge-header",
                IsEnabledForTenant: false,
                FallbackReason: countryCurrency is null ? "country-unknown" : null,
                PrivacyNotice);
        }

        var configurations = await context.Set<CurrencyConfiguration>()
            .AsNoTracking()
            .Where(configuration => configuration.TenantId == tenantId && configuration.IsEnabled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var baseCurrency = configurations
            .Where(configuration => configuration.IsBaseCurrency)
            .Select(configuration => configuration.CurrencyCode)
            .FirstOrDefault();

        if (countryCurrency is not null && configurations.Any(configuration => configuration.CurrencyCode == countryCurrency))
        {
            return new CurrencySuggestionDto(countryCurrency, country, "edge-header", true, null, PrivacyNotice);
        }

        // The country's currency is unavailable for this tenant: fall back to the tenant
        // base currency, then the first enabled currency — never a disabled currency.
        var fallback = baseCurrency
            ?? configurations
                .OrderBy(configuration => configuration.DisplayOrder)
                .ThenBy(configuration => configuration.CurrencyCode, StringComparer.Ordinal)
                .Select(configuration => configuration.CurrencyCode)
                .FirstOrDefault();

        return new CurrencySuggestionDto(
            fallback ?? countryCurrency ?? "USD",
            countryCurrency is null ? null : country,
            fallback is null ? "default" : baseCurrency == fallback ? "tenant-base" : "first-enabled",
            fallback is not null,
            FallbackReason: countryCurrency is null ? "country-unknown" : "currency-not-enabled",
            PrivacyNotice);
    }

    /// <summary>
    ///     Maps an ISO 3166-1 alpha-2 country to its ISO 4217 currency through the BCL
    ///     region data (no geolocation database involved). Unknown codes return null.
    /// </summary>
    internal static string? MapCountryToCurrency(string countryCode)
    {
        if (countryCode.Length != 2 || !char.IsLetter(countryCode[0]) || !char.IsLetter(countryCode[1]))
        {
            return null;
        }

        try
        {
            var region = new RegionInfo(countryCode);
            var currency = region.ISOCurrencySymbol?.Trim().ToUpperInvariant();
            return currency is { Length: 3 } && Iso4217CurrencyCatalog.Exists(currency) ? currency : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
