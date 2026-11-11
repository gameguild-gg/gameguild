# Authentication docs citation spot-audit — 2026-11

> Row-1 audit of the `docs/auth/*` + `docs/security/auth-*.md` packages (both claim 8/8 against commit `74e9bba4c`) against current `develop` `7ae5a28a65` — i.e. **after** #1160 (auth edge-cases batch 2), #1227 (auth dead-event wiring), #1263 (sub-flow docs), #1269 (security batch 3).
> Method: 29 file:line citations sampled across 16 distinct source files and 12 distinct doc files (flows, wiring-matrix, state-machines, event-flows, cross-module-sequences, stride, dfds, security-architecture). Each checked for (a) line-content match at the cited number, (b) if drifted, the claim's anchor relocated and re-verified, (c) semantic truth of the claim at the new location.

## 1. Results

| Verdict | Count | Rate |
|---|---|---|
| Exact — cited line still shows the documented content | 21 | 72% |
| Drifted — content moved, claim re-verified TRUE at new location | 8 | 28% |
| **Claim now false** | **0** | 0% |

## 2. Exact citations (sample)

| Citation (doc) | Claim | Current content at line |
|---|---|---|
| `Controllers/AuthController.cs:20` (magic-link/web3 flows) | class-level `Authentication` rate-limit policy | `[EnableRateLimiting(RateLimitPolicies.Authentication)]` |
| `Services/ApiKeyAuthenticationHandler.cs:96-102` (api-key flow) | lazy rotation finalize + inactive/expired fail | `FinalizeRotationRevocationAsync` + `FailAsync("API key is inactive or expired"…)` |
| `SignInMfa/SignInMfaService.cs:50-53` / `:121-126` | MFA policy split; live-recovery cancellation | both verbatim |
| `StepUp/MagicLinkTokenStore.cs:36` (state-machines) | `DatabaseMagicLinkTokenStore` | class declaration |
| `SessionOptions.cs:84` (trusted-devices) | `RequireTrustedDeviceForSensitiveOps` default false | the property with `= false` |
| `Services/ServiceAccountService.cs:206-222` / `:411-476` | secret rotation NO-overlap; derived audit entries | `RotateSecretAsync`, `BuildDerivedAuditEntries` |
| `Handlers/PolymorphicSignInHandler.cs:87-89`, `Guest/GuestSessionUpgradeIntegration.cs:13-40`, `Guest/GuestAccessDefaults.cs:22`, `Guest/DistributedCacheGuestSessionStore.cs:93-96` | guest-upgrade wiring + audience + concurrency-window comment | all verbatim |
| `Services/JwtTokenService.cs:193-194` (see D7 below for the +2) + `:524-528`, `:617-618` (key-rotation flow) | static `JwtOptions.SecretKey` HS256 everywhere | still true (re-verified at new lines) |
| `GameGuild.API/Core/Setup/InfrastructureLayerExtensions.cs:246,:406` (key-rotation flow) | only outbox/quota/refresh-token/economy workers hosted — **key-rotation service unwired** | still `QuotaReconciliationBackgroundService` / `RefreshTokenCleanupWorker`; no `KeyRotationBackgroundService`/`IKeyRotationService` registration anywhere — **the headline #1141 finding remains true** |
| `…/Provisioning/Services/ScimUserService.cs:252` | SCIM delete → terminate sessions + token-version bump | verbatim |
| `Users/Entities/User.cs:603-618` (account-provisioning) | `CreateOAuthUser` | verbatim (line still exact) |
| `Web3Challenge.cs:53-58`, `Web3Service.cs:104-111` | atomic challenge claim; challenge cache | verbatim |

## 3. Drifted citations (the +N class)

All drift is **additive** (post-#1227/#1160/#1269 insertions pushing later content down); no cited content was deleted or rewritten, and every claim re-verified TRUE at its new anchor.

| Citation (doc) | Claim | Old → new | Δ |
|---|---|---|---|
| `Services/LocalAuthService.cs:60-63` (trusted-devices flow) | `X-Device-Fingerprint` capture | `:60-63` → `:78-81` | **+18** |
| `Services/LocalAuthService.cs:501` (event-flows) | `SuspiciousLoginDetectedV1` published only from the local sign-in risk path | `:501` → `:719-724` | **+218** |
| `Services/LocalAuthService.cs:621-628` (account-provisioning) | `UserCreatedEvent` attached only in local sign-up | `:621-628` → `:861` | **+233** |
| `Services/LocalAuthService.cs:952-971` (state-machines, token-lifecycle) | replay containment fallback to account scope | `:952-971` → `:1172-1189` | **+220** |
| `Services/JwtTokenService.cs:193` (key-rotation) | static SecretKey signing | `:193` → `:195-196` | **+2** |
| `Controllers/AuthController.cs:708-727` (sign-out flow) | `tokens:revoke` endpoint | `:708` → `:721` | **+13** |
| `Controllers/AuthController.cs:736-767` (sign-out flow, wiring-matrix) | `sign-out` endpoint | `:736` → `:749` | **+13** |
| `Entities/User.cs:563` (account-provisioning; Users module) | `CreateWithPassword` | `:563` → `:571` | **+8** |

Drift is concentrated in **one file** (`LocalAuthService.cs` — 4 of 8, deltas +18/+218/+220/+233 — #1227's sign-in telemetry/dead-event insertions plus the #1160/#1269 security batches) and `AuthController.cs` (+13, new endpoints added above the cited ones). Everything else audited byte-stable.

## 4. Verdict for row 1

The 8/8 claim **stands**. No citation in the sample points at content that disappeared; no load-bearing claim (static-secret signing, unwired key rotation, guest-upgrade wiring, MFA policy split, lazy API-key rotation finalization) is now false. The one hygiene item: the eight drifted citations above should be re-anchored when the auth package is next touched — the whole drift class lives in two files, so a single mechanical pass fixes it.
