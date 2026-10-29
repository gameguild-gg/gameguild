# Currency & FX compliance notes (issue #412)

Compliance posture for the multi-currency pricing and FX rate synchronization feature
(specs C/E/H of issue #412). This is an engineering statement of the controls in code;
it is not a legal opinion.

## PCI DSS — storing currency data

**Position: currency data is not cardholder data (CHD) and is out of PCI DSS CDE scope.**

What the currency/FX surfaces store (`currency_configurations`, `fx_rate_snapshots`,
`fx_rate_sync_runs`, `price_localizations`, `price_recalculation_runs` under
`apps/api/Source/Modules/GameGuild.Commerce.Payments/Entities/`):

- ISO 4217 currency codes, amounts, minor-unit exponents;
- FX quotations with provenance (provider, source, effective dates, raw provider responses);
- per-tenant enablement/base-currency configuration and localized plan prices.

None of these are PAN, CVV, expiry or any CHD element defined by PCI DSS v4.x. The tables
never co-locate with cardholder data, and the raw provider responses persisted for audit
(`FxRateSyncRun.RawResponse`) contain market rates only — the providers are called
server-to-server with an API key and no payment data is exchanged.

Controls that keep it that way:

1. **No PAN storage anywhere in the payments module.** Card data goes directly to the
   gateway SDKs; the platform stores only gateway tokens (see `GatewayPaymentMethodIdProtector`).
2. **FX provider API keys are encrypted at rest** with ASP.NET DataProtection (AES-256
   under the application key ring), in the same audited credential store as the payment
   gateway credentials (`fx:openexchangerates/app-id` slot, `GatewayCredentialStore`).
   Plaintext keys exist only in memory during a fetch and are never logged or returned;
   provisioning is system-admin only and audit-logged with a SHA-256 fingerprint
   (`ProvisionFxProviderApiKeyCommand`).
3. **Rate-reasonableness guard** (spec H): quotations moving more than
   `FxRates:MaxRateChangePercentPerDay` (default 10%) against the previously persisted
   value are quarantined — not persisted — audited, and escalated to operators, so a
   compromised or glitching feed cannot silently reprice the platform.
4. **Bounded outbound call rate** (`FxRates:ProviderMaxCallsPerMinute`) protects provider
   accounts from runaway sync loops.

## GDPR — location-based currency detection

**Position: the currency suggestion is data-minimized, ephemeral and non-profiling.**

How `GET /api/v{v}/currencies/suggestion` works (`CurrenciesController` / spec E):

1. The request's country is read from edge-proxy headers (`CF-IPCountry`, then
   `X-Country-Code`/`X-Vercel-IP-Country`) that carry a country code, not an IP address
   and not any personal identifier.
2. The country code is mapped to an ISO 4217 currency through the platform catalog
   (`.NET RegionInfo` mapping) and intersected with the tenant's enabled currencies.
3. **Nothing is persisted.** No IP address, no country, no geolocation database lookup
   and no suggestion event is stored by the endpoint. The response carries the suggested
   currency, its source, and a short privacy notice stating exactly this.
4. Selecting a currency stores only the user's explicit choice (the `Currency` key of the
   account's localization preferences, or a client-side preference cookie for anonymous
   visitors). Users can change or clear it at any time in workspace settings.

This makes the feature a **non-profiling, transient processing** of a country code for
the sole purpose of pre-selecting a display currency — compatible with GDPR's data
minimization (Art. 5(1)(c)) and storage limitation (Art. 5(1)(e)) principles. No
MaxMind/GeoIP database lookup is performed for currency suggestion; the existing MaxMind
enrichment of authentication sessions (issue #180 scope) is a separate, independently
documented process.
