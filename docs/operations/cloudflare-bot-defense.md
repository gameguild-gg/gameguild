# Cloudflare bot defense and trusted-proxy operations

This runbook covers the client-IP attribution and anti-bot controls deployed on the API and Web apps: trusted-proxy CIDRs for real client IPs behind Cloudflare and Traefik, the deployment blast radius of that change, and the Cloudflare Turnstile challenge on public write surfaces. Everything here is already deployed in code; this document explains how it behaves and what an operator must check when touching related configuration.

## 1. Why per-IP limiting needed real client IPs

Rate-limit policies partition anonymous traffic by IP (`PerIp`, `Webhook`, `GuestIssuance`, and the anonymous fallback of the global/API policies). Behind a reverse proxy that key is `HttpContext.Connection.RemoteIpAddress`. If the forwarded-header middleware does not trust the proxy, that address stays the proxy's address, so every visitor shares one bucket: one abusive client (or one neighbor container) can exhaust the quota for everyone.

Root cause: `SetupRateLimiting` **replaces** the hosting-level forwarded-header settings instead of merging with them (`apps/api/Source/GameGuild.API/Core/Extensions/RateLimitingServiceCollectionExtensions.cs:37-53`). With an empty trusted-proxy configuration it sets `ForwardedHeaders.None`, so `X-Forwarded-For` is ignored entirely and the proxy IP becomes the bucket key. The fix is the trusted-proxy model below: the API now knows which upstream hops are infrastructure and reads the real client IP from `X-Forwarded-For`.

## 2. Trusted-proxy model

Configuration lives in `apps/api/Source/GameGuild.API/appsettings.json` under `RateLimiting`:

- `TrustedProxyNetworks`: 23 CIDRs, which are the 15 published Cloudflare IPv4 ranges, the 7 published Cloudflare IPv6 ranges, and `172.16.0.0/12` for the internal container network on the Coolify host.
- `TrustedProxyForwardLimit: 3`: how many proxy hops the API accepts in the `X-Forwarded-For` chain (see "Ingress modes" below for why 3 fits both ingress paths).

**Single source of truth.** `TrustedProxyNetworks` is configured ONLY in `appsettings.json`. Never also set `RateLimiting__TrustedProxyNetworks__*` environment variables. ASP.NET Core configuration is last-provider-wins per array index: an env var `RateLimiting__TrustedProxyNetworks__0=...` overwrites only the first JSON entry, silently corrupting the list (the remaining 22 entries shift meaning). `compose.coolify.yaml` carries a comment next to the allowlist env var spelling this out; keep it there. If you need deployment-specific proxy ranges, edit `appsettings.json` (or an environment-specific appsettings file), not compose env vars.

The asymmetry with `AllowlistedIpNetworks` is intentional: `appsettings.json` has **no** `RateLimiting:AccessControl` section, so the compose env var `RateLimiting__AccessControl__AllowlistedIpNetworks__0=172.16.0.0/12` is the _only_ source for that array and a scalar-indexed env override is safe. For `TrustedProxyNetworks` the JSON array is the only source, and the two directions must not be mixed.

**Refreshing the Cloudflare ranges.** The published ranges change rarely, but check after any Cloudflare announcement:

1. Fetch <https://www.cloudflare.com/ips-v4> and <https://www.cloudflare.com/ips-v6>.
2. Diff against the `TrustedProxyNetworks` entries in `appsettings.json` (the first 15 IPv4 and next 7 IPv6 entries; the trailing `172.16.0.0/12` is ours and stays).
3. Add or remove ranges accordingly and redeploy the API.

Two layers of validation back this list: `RateLimitingOptions.Validate()` throws at startup when any entry is not a well-formed IPv4/IPv6 CIDR (`GameGuild.SharedKernel/Configuration/PresentationLayer/RateLimiting/RateLimitingOptions.cs:380-385`), and the forwarded-headers setup additionally skips entries that fail `IPNetwork.TryParse` as a belt-and-braces guard against hosting-level configuration being mutated after startup (`RateLimitingServiceCollectionExtensions.cs:55-63`). In practice Validate() is the gate you will see; the skip is defense in depth.

## 3. Ingress modes

**Cloudflare Tunnel (current).** Traffic path: Cloudflare edge → cloudflared connector → Coolify's Traefik → API container on the shared Coolify network (`compose.coolify.yaml:3-8`). The API's TCP peers here are internal containers (cloudflared, Traefik), not Cloudflare edges, so the CF ranges in `TrustedProxyNetworks` are defense-in-depth: they make the API correctly attribute `X-Forwarded-For` if a path ever presents a CF edge address directly. The `172.16.0.0/12` entry is what marks the container peers as trusted today.

**Direct-origin behind the CF proxy (planned).** The migration plan (`.omo/plans/drop-cloudflared-tunnel-direct-origin.md`, local planning notes) removes the tunnel and points DNS A-records at origins fronted by Traefik with Cloudflare orange-cloud proxying. In that mode Traefik's TCP peer IS a Cloudflare edge IP, so the CF ranges become REQUIRED for correct attribution, not optional.

`TrustedProxyForwardLimit: 3` covers both modes: `client → [CF edge] → cloudflared → Traefik → API` yields at most three proxy hops appending to `X-Forwarded-For`. Setting the limit higher would let attackers append fake hops; lower breaks the chain.

## 4. Deploy checklist: RemoteIpAddress blast radius

Once trusted-proxy parsing is active, `RemoteIpAddress` changes from a proxy/container IP to the visitor's real address. This affects roughly two dozen consumers across the API (33 files, 48 references), not just rate limiting. Review these before and after any deploy that changes proxy trust:

**(a) `WebhookSourceIpAllowlist` (billing webhooks).** The filter enforces a CIDR allowlist on Stripe callback endpoints and fails closed with 403 (`apps/api/Source/Modules/GameGuild.Commerce.Billing/Security/WebhookSourceSecurityFilter.cs`; locate references with `rg -l "WebhookSourceSecurityFilter" apps/api/Source`). If the configured allowlist entries name the old observed proxy or container IP (for example `172.16.0.0/12` when traffic arrived via the tunnel), replace them with the provider's published CIDRs, i.e. Stripe's webhook IP ranges. A mismatch after deploy means legitimate webhooks start getting 403, so verify this first on a payment-touching release.

**(b) `apiKey.IpWhitelist`.** `ApiKeyAuthenticationHandler` compares `Context.Connection.RemoteIpAddress` against the key's comma-separated whitelist by exact string match (`apps/api/Source/Modules/GameGuild.Identity.Authentication/Services/ApiKeyAuthenticationHandler.cs:105-112`). Keys pinned to the old proxy IP will now fail ("ApiKeyIpNotAllowed"). Update those keys to the real caller IPs, and mind representation: after forwarded-header processing, IPv4 addresses can appear in IPv4-mapped IPv6 form (`::ffff:a.b.c.d`). Test both spellings or normalize before comparing.

**(c) Tenant IP allowlist rules.** `RequireIpAllowListRuleEvaluator` evaluates `HttpContext.Connection.RemoteIpAddress` against tenant allowlist rules (`apps/api/Source/Modules/GameGuild.Identity.Authorization/Rules/Evaluators/RequireIpAllowListRuleEvaluator.cs:45`). Semantics change: rules written against the old proxy/container IP now match the actual visitor address. Re-review tenant rule sets after deploy; a rule that allowed "everything from the proxy" now allows only real client IPs that match, which is usually the intent but is a behavior change.

**(d) Audit and security logs.** `AuthAttemptService`, `SecurityEventLogger`, and `MfaAttemptTrackingService` now record the real client IP instead of the shared proxy address. This is the desired outcome: per-IP correlation, velocity checks, and lockout analytics become meaningful. Expect historical dashboards to show a shift in the logged IP distribution at deploy time; that is the fix landing, not a regression.

## 5. Blast radius of 172.16.0.0/12 on shared Coolify hosts

`compose.coolify.yaml` joins the external, provider-wide `coolify` network (networks block at `compose.coolify.yaml:223-225`, wired to both services at lines 148-153 and 216-217). That means _other tenants' containers on the same Coolify host_ share a network with this API, and `172.16.0.0/12` is trusted for forwarded-header parsing and exempt via `AllowlistedIpNetworks__0`. Concretely, a neighbor container could:

- bypass per-IP rate limiting entirely (allowlist membership), or
- spoof arbitrary `X-Forwarded-For` values that the API will trust (proxy trust).

On a single-tenant host this is fine. On a shared Coolify host, decide explicitly whether to accept it, or narrow the exposure per deployment by overriding `RateLimiting__AccessControl__AllowlistedIpNetworks__0` with a tighter CIDR (for example the specific compose network's subnet). That scalar-indexed env override is safe precisely because `appsettings.json` has no `AccessControl` array (see section 2); do NOT apply the same override trick to `TrustedProxyNetworks`.

## 6. Residual: visitor fan-out

A visitor's own browser still fans out 5-15 API calls per page render (RSC fetches, data loads) into _their own_ bucket under the global default limit of 100 requests/minute. That is strictly better than the old shared bucket, where any one visitor's fan-out counted against everyone. If real visitors start hitting 429s, tune `RateLimiting:Limit` (and per-policy budgets) via environment variables, e.g. `RateLimiting__Limit`; no code change needed.

## 7. Cloudflare edge defenses (zero code)

For the API hostname, enable at the Cloudflare edge, in addition to the application controls above:

- **WAF managed rules** and/or **Bot Fight Mode** on the API zone.
- A **Cloudflare rate-limiting rule** on sensitive paths such as `/v1/auth/*`, applied before traffic ever reaches the origin.

**These are human actions performed in the Cloudflare dashboard. They are out of scope for CI and agents**: no repository change enables them, and no pipeline should attempt to. This document is the pointer; the zone administrator applies the settings.

References: WAF managed rules <https://developers.cloudflare.com/waf/managed-rules/>, rate-limiting rules <https://developers.cloudflare.com/waf/rate-limiting-rules/>.

## 8. Turnstile operations

**How it works.** Public write surfaces render an invisible Turnstile widget that returns a short-lived token; the server-side action verifies the token against Cloudflare's `siteverify` endpoint with the secret key, passing the client IP for extra validation (`apps/web/src/lib/antibot/turnstile.ts`). Constraints enforced in code: tokens are single-use (a replayed token fails verification), max token length 2048 characters, verification timeout 10 seconds.

**Gated surfaces.** The following public write surfaces verify a Turnstile token before doing work:

- sign-in password path (wrapped via `withAntibotVerification` in `apps/web/src/auth.ts`; magic-link, MFA, and web3 sign-in flows are deliberately not gated)
- sign-up
- forgot-password and reset-password (`requestPasswordResetAction` / `completePasswordResetAction` in `apps/web/src/lib/auth/password-actions.ts`)
- contact form (`apps/web/src/lib/contact/actions.ts`)
- blog comments (`apps/web/src/lib/blogs/actions.ts`)
- feed comments (`apps/web/src/lib/feed/actions.ts`)
- web3 auth challenge (`apps/web/src/app/api/auth/web3/challenge/route.ts`)
- content reports (`apps/web/src/app/api/courses/content-reports/route.ts`)

**Failure modes.**

- `TURNSTILE_SECRET_KEY` unset/empty: fail-open, verification skipped with a once-per-process warning (dev/e2e path).
- Cloudflare returns `success: false`: fail-closed, the action rejects.
- Verify endpoint unreachable or unparseable: fail-open with `verify-unavailable`; an outage must not lock everyone out, an explicit rejection does.

**Keys and where they live.**

- Site key: `NEXT_PUBLIC_TURNSTILE_SITE_KEY`. This is a **build-time** variable for the web app because Next.js inlines `NEXT_PUBLIC_*` values at build. It is passed as a Docker build arg in `compose.coolify.yaml` (with the visible test key as fallback) AND as a runtime env var.
- Secret key: `TURNSTILE_SECRET_KEY`, runtime env only, never inlined, never committed. Production values are set through the Coolify environment-variable UI for the resource; the compose file ships only test-key fallbacks.

**Test keys** (from <https://developers.cloudflare.com/turnstile/troubleshooting/testing/>; always use these in non-production, never commit real keys):

| Purpose             | Site key (visible)         | Secret key                            | Behavior                     |
| ------------------- | -------------------------- | ------------------------------------- | ---------------------------- |
| Always passes       | `1x00000000000000000000AA` | `1x0000000000000000000000000000000AA` | Verification always succeeds |
| Always fails        | `2x00000000000000000000AA` | `2x0000000000000000000000000000000AA` | Verification always fails    |
| Token already spent | `3x00000000000000000000AA` | `3x0000000000000000000000000000000AA` | Passes once, fails on replay |

Server-side validation contract: <https://developers.cloudflare.com/turnstile/get-started/server-side-validation/>.
