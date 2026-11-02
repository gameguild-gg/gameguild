# Authentication STRIDE Threat Model

Closes #1136.

Status: living document, current as of 2026-11-02 against `origin/develop` (a96679c7a).

Companion documents: [auth-security-architecture.md](auth-security-architecture.md) (#1135) for control inventory and trust boundaries, [auth-dfds.md](auth-dfds.md) (#1137) for per-flow data flow diagrams.

## 1. Methodology

STRIDE per flow: Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege. Each threat below is either mitigated in code (citation `file:line`, relative to repository root, `apps/api/Source/Modules/GameGuild.Identity.Authentication/...` abbreviated as `Auth/` where the prefix is identical) or is a tracked gap referenced by issue number. Known open gaps are tracked in #1082-#1091, #1115-#1128 and #1141-#1155; this document references them rather than re-deriving them. Paths under `apps/api/Source/GameGuild.API/` are abbreviated `Host/`.

Trust boundary model (from the architecture document, section 3): B1 internet-to-proxy, B2 proxy-to-API, B3 API-to-PostgreSQL, B4 API-to-Redis, B5 API-to-IdPs, B6 API-to-delivery-providers, B7 API-to-blockchain, B8 API-to-threat-intel.

## 2. Flow 1 — Local sign-in (email and password, polymorphic)

Entry: `POST /v1/auth/sign-in` (`Auth/Controllers/AuthController.cs:67`), `POST /v1/auth/polymorphic` (line 115). Engine: `Auth/Services/LocalAuthService.cs:50-411`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — credential stuffing | Bulk password spray across accounts | Breached-password SHA-256 digest matched against threat intel, digest never persisted (`Auth/Services/LocalAuthService.cs:66-69`); authentication rate-limit policy per IP/user (`Auth/Controllers/AuthController.cs:20`; `Host/Core/Extensions/RateLimitingServiceCollectionExtensions.cs:152-172`); risk engine step-up at High risk (`Auth/Services/LocalAuthService.cs:183-199`). Gap: IP blocklist for known attacker IPs — #1089 |
| S — targeted brute force | Password guessing on one account | Attempt forensics feed risk engine (`Auth/Services/AuthAttemptService.cs:59-102`); brute-force alerts to owner (`Auth/Services/LocalAuthService.cs:159-167`, `350-354`). Gaps: progressive delay — #1084; CAPTCHA after N failures — #1082; lockout notification — #1083 |
| S — stolen signed token replay | Bearer replay after theft | Short 60-minute access lifetime (`apps/api/Source/Modules/GameGuild.SharedKernel/Configuration/ApplicationLayer/JwtOptions.cs:33`); per-request jti revocation and token-version check (`Auth/Middleware/TokenRevocationMiddleware.cs:72-103`) |
| T — JWT payload tampering | Forging claims or swapping signature algorithm | HS256-only pinned on validation (`Host/Core/Extensions/SecurityServiceCollectionExtensions.cs:108`) and re-checked on issuance paths (`Auth/Services/JwtTokenService.cs:627-634`); reserved-claim injection refused (`Auth/Services/JwtTokenService.cs:605-625`) |
| T — tenant claim forgery | Client asserts arbitrary tenant | Caller-supplied `TenantId` is advisory; membership re-resolved server-side (`Auth/Services/LocalAuthService.cs:1066-1072`, `216`) |
| R — sign-in denial | User claims they did not sign in | Success and failure attempts persisted with IP, user agent, geo, timing (`Auth/Services/AuthAttemptService.cs:24-102`, `136-160`); JWT validation audit events (`Host/Core/Extensions/SecurityServiceCollectionExtensions.cs:116-160`); risk events audited (`Auth/Services/LocalAuthService.cs:186-195`) |
| I — account enumeration via error text | Distinguish existing vs unknown accounts | Generic failure message on all paths (`Auth/Services/LocalAuthService.cs:170`, `409`; `Auth/Services/UserEnumerationProtectionService.cs:77-84`) |
| I — account enumeration via timing | Faster rejection for unknown accounts | Monotonic timing floor with dummy credential work (`Auth/Services/UserEnumerationProtectionService.cs:32-76`, `140-150`; classification at `Auth/Services/LocalAuthService.cs:413-419`) |
| I — log leakage of credentials/PII | Passwords or raw emails in logs | Candidate password kept as transient digest only (`Auth/Services/LocalAuthService.cs:66-69`); emails hashed in audit logs (`Auth/Services/AuthAttemptService.cs:245-249`); `LogRedaction` on ids (`Auth/Services/JwtTokenService.cs:244`) |
| D — lockout abuse | Attacker locks out victims deliberately | No hard lockout on password path today (rate limiting + MFA lockout only, `Auth/Services/MfaAttemptTrackingService.cs:328`); combined with gap #1083 (no lockout notice) and #1084 (no progressive delay) this is an accepted asymmetry tracked there |
| D — sign-in endpoint flood | Exhaust API capacity | Authentication rate policy + global limiter (`Host/Core/Extensions/RateLimitingServiceCollectionExtensions.cs:87-107`, `152-172`) |
| E — sign-in as deleted or stale account | Old token version or deleted user restored | Re-fetch and `ValidateForAuthentication(version)` before issue (`Auth/Services/LocalAuthService.cs:207-213`) |
| E — bypass MFA requirement | Risk path skips challenge | MFA preparation is mandatory: ordinary credentials require explicit policy outcome (`Auth/Services/LocalAuthService.cs:278-294`) |

Polymorphic variant: identifier ambiguity and misses collapse to the same generic failure (`Auth/Controllers/AuthController.cs:113-134`).

## 3. Flow 2 — Sign-up

Entry: `POST /v1/auth/sign-up` (`Auth/Controllers/AuthController.cs:33`). Engine: `Auth/Services/LocalAuthService.cs:594-723`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — bot registration | Mass fake accounts | Turnstile enforced only at the Next.js layer today — API-level CAPTCHA gap #1150; rate limiting applies (`Auth/Controllers/AuthController.cs:20`) |
| T — weak password policy bypass | Client-side-only strength checks | Server-side strength validation (`Auth/Services/LocalAuthService.cs:612-617`); username slug rules (lines 605-610) |
| R — dispute of account origin | Attribution of creation | `UserCreatedEvent` integration event with actor id and correlation (`Auth/Services/LocalAuthService.cs:638-646`); registration attempt recorded (line 694) |
| I — existing-account enumeration | Distinct error reveals registered emails | Mitigation gap: duplicate email throws `InvalidOperationException("User already exists")` — observable enumeration signal distinct from the sign-in path's generic errors (`Auth/Services/LocalAuthService.cs:620-627`). Partially mitigated by timing floor (line 624). No dedicated issue tracks this yet — noted here for triage |
| D — registration flood | Storage exhaustion | Rate limiting as above; no CAPTCHA (gap #1150) |
| E — default privilege inflation | Fresh account gains unexpected roles | Default tenant membership provisioner assigns the standard membership (`Auth/Services/DefaultTenantMembershipProvisioner.cs` invoked at `Auth/Services/LocalAuthService.cs:655`); roles resolved from membership context (line 660) |

Guest upgrade on sign-up is handled via the guest access token (`Auth/Controllers/AuthController.cs:48`) through `Auth/Guest/GuestSessionUpgradeIntegration.cs`.

## 4. Flow 3 — MFA (TOTP, SMS, push, email, backup codes, WebAuthn sign-in challenge)

Entry points: `POST /v1/auth/mfa/sign-in/complete` (`Auth/Controllers/AuthController.cs:91`), `Auth/Controllers/MfaController.cs`, `Auth/Controllers/MfaRecoveryController.cs`, `Auth/Controllers/WebAuthnController.cs`. Engine: `Auth/Services/MfaService.cs`, `Auth/SignInMfa/SignInMfaService.cs`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — stolen first-factor completes MFA | Attacker forwards a captured MFA token | MFA completion consumes a server-side challenge bound to user, tenant, token version and auth time (`Auth/SignInMfa/PostgreSqlSignInMfaChallengeStore.cs:26-46`; binding proof `Auth/Services/JwtTokenService.cs:113-123`) |
| S — replayed TOTP code | Same 30-second code reused | Matched time-step recorded in replay store, single-use (`Auth/Services/TotpMfaService.cs:136-145`; entity `Auth/Entities/TotpReplayState.cs`) |
| S — phished OTP | Real-time relay | Out of scope of code controls; risk-engine step-up raises cost (`Auth/Services/LocalAuthService.cs:183-199`) |
| S — fake WebAuthn credential | Non-attested credential enrolled | Challenge ceremony with server-verified signature (`Auth/Services/WebAuthnService.cs`); gap: attestation conveyance `None` and persisted `UserVerified = true` regardless of authenticator flag — #1143 (`Auth/Services/WebAuthnRegistrationService.cs:64`, `155`) |
| T — MFA token payload forgery | Client asserts its own MFA claims | Claims derived only from server-side proof; reserved claim firewall blocks `amr`/`mfa_verified` injection (`Auth/Services/JwtTokenService.cs:35-48`, `169-177`) |
| T — TOTP secret theft at rest | Database leak exposes seeds | AES-256-GCM encryption with fail-closed key (`Auth/Services/TotpMfaService.cs:27-60`; `Auth/Services/EncryptionService.cs:15-37`) |
| R — MFA changes disputed | User denies disabling MFA | Disable re-verification failures logged with metric reason (`Auth/Services/MfaService.cs:114-125`); enrollment metrics recorded (`Auth/Services/MfaEnrollmentMetrics.cs`). Gap: MFA recovery emails bypass durable queue — evidence trail risk #1155 |
| I — TOTP seed disclosure to client | Secret leaked in API responses | Secret returned exactly once during enrollment for QR provisioning (`Auth/Services/MfaService.cs:30-55`) |
| D — OTP brute force | Guessing 6-digit codes | MFA lockout after `MaxFailedAttempts` (`Auth/Services/MfaAttemptTrackingService.cs:328`, `370`; default 5 at `apps/api/Source/Modules/GameGuild.SharedKernel/Configuration/ApplicationLayer/MfaOptions.cs:16`); email codes capped at five attempts (`Auth/Services/EmailCodeService.cs:29`, `116-127`); rate-limited endpoints |
| E — factor stripping | Attacker disables MFA with a stolen OTP | Disable requires account password; passwordless accounts cannot disable (`Auth/Services/MfaService.cs:107-134`). Gap: backup-code regeneration and push-device registration lack the same re-verification — #1153 |
| E — downgrade to non-MFA token | Refresh mints ordinary token after MFA session | Refresh re-reads persisted proof and reissues MFA-bound token or nothing (`Auth/Services/LocalAuthService.cs:818-857`) |
| E — enrollment races | Concurrent enrollment and sign-in | Gap: #1117 |

Recovery path: `Auth/Controllers/MfaRecoveryController.cs`, `Auth/Handlers/MfaRecoveryCommandHandlers.cs`. Email-based MFA enrollment endpoint gap: #1152. Email verification token store is process-local (survives neither restart nor scale-out) — #1151 (`Auth/Services/EmailVerificationService.cs:15`, `60-62`).

## 5. Flow 4 — OAuth and federation (Google, GitHub, Discord, OIDC, SAML)

Entry points: `POST /v1/auth/google:sign-in` (`Auth/Controllers/AuthController.cs:143`), GitHub authorize/callback (lines 227, 988), Discord (308, 338), OIDC per-slug (397, 427), SAML per-slug (534, 563). Engines: `Auth/Services/OAuthAuthService.cs`, `Auth/Services/OAuthService.cs`, `Auth/Services/GoogleIdTokenVerifier.cs`, `Auth/Services/OidcFederationService.cs`, `Auth/Services/SamlFederationService.cs`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — forged IdP token | Attacker mints a fake Google/IdP assertion | Google: `GoogleJsonWebSignature` validation with pinned audience, fail closed when client id missing (`Auth/Services/GoogleIdTokenVerifier.cs:16-50`). SAML: XML-DSig must verify against a currently-valid pinned certificate; unsigned responses refused (`Auth/Services/SamlFederationService.cs:15-18`, `132-136`, `173`). OIDC: id-token validation against discovery metadata with issuer-equals-authority check (`Auth/Services/OidcFederationService.cs:382`, discovery hardening at `397-406`) |
| S — callback CSRF / authorization-code injection | Forged callback binds attacker identity | State parameter required and validated (`Auth/Services/OAuthService.cs:61-65`; `Auth/Services/OidcFederationService.cs:56-59`). GitHub/Discord state is a GUID issued at authorize time (`Auth/Services/OAuthAuthService.cs:856-874`) |
| S — account linking confusion | Same email at different providers merges accounts | Gaps: OAuth email case sensitivity — #1118; multiple OIDC providers with same email — #1121 |
| T — assertion tampering in transit | MITM modifies code/assertion | TLS to providers; SAML signature wraps assertion and response with wrapping-detection posture (`Auth/Services/SamlFederationService.cs:161-182`) |
| T — evil discovery document | Attacker-controlled OIDC endpoints | Endpoints must be absolute HTTPS without embedded credentials; issuer must equal authority (`Auth/Services/OidcFederationService.cs:382`, `397-406`) |
| R — federated sign-in attribution | Dispute of which provider vouched | Provider recorded on external login entity (`Auth/Entities/ExternalLogin.cs`); sign-ins audited with method (`Auth/Services/AuthAttemptService.cs:216-243`) |
| I — over-broad scope disclosure | Excess profile data ingested | Minimal configured scopes per provider (`Auth/Services/OAuthAuthService.cs:862`, `873`) |
| D — provider outage cascades | Users locked out when IdP down | Gap: no local fallback — #1116 |
| E — SAML assertion without MFA used on MFA-required accounts | IdP did not perform MFA | AuthnContext class enforcement: assertion must carry a configured MFA class or sign-in is refused (`Auth/Services/OAuthAuthService.cs:485-512`) |
| E — guest/low-trust account auto-provisioning into privileged tenant | Provisioning inflates roles | Provisioning through default membership provisioner with tenant access context resolution (`Auth/Services/OAuthAuthService.cs` sign-in completion reuses `ResolveTenantAccessContextAsync` via the shared issuance path) |

Account linking endpoints: `Auth/Controllers/ExternalLoginsController.cs`, `Auth/Handlers/ExternalLoginHandlers.cs` (orphan prevention on unlink).

## 6. Flow 5 — Magic link

Entry: `POST /v1/auth/magic-link:request` and `:consume` (`Auth/Controllers/AuthController.cs:169`, `199`). Engine: `Auth/Handlers/MagicLinkCommandHandlers.cs`; store `Auth/StepUp/MagicLinkTokenStore.cs`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — guessed or forged token | Attacker guesses the link secret | 15-minute TTL (`Auth/Commands/MagicLinkCommands.cs:45`); tokens stored as hashes and consumed single-use atomically (`Auth/StepUp/MagicLinkTokenStore.cs:69`, `93`) |
| S — link replay after use | Second consumption | Single-use via `ConsumedAt` guard (`Auth/StepUp/MagicLinkTokenStore.cs:69`, `93`) |
| T — token substitution | Swapping hash rows | Hash-keyed lookups only (`Auth/StepUp/MagicLinkTokenStore.cs:57-69`) |
| R — dispute of magic-link sign-in | Attribution | Store persists issuance and consumption timestamps (`Auth/StepUp/MagicLinkTokenStore.cs:48-93`); audit via attempt recorder |
| I — account existence signal | Behavior differs for unknown emails | Request is always-acknowledged pattern (generic response contract in `Auth/Commands/MagicLinkCommands.cs:39-47`) |
| D — mail-bombing via requests | Flood a victim's inbox | Gap: no per-IP rate limit on magic-link requests — #1090 |
| E — magic link into privileged session | Bypass of MFA via link | Issuance goes through the same MFA preparation gate as password sign-in (`Auth/Handlers/MagicLinkCommandHandlers.cs` reuse of the `SignInMfa` path) |

## 7. Flow 6 — Email code

Entry: `POST /v1/auth/email-code:request` and `:consume` (`Auth/Controllers/AuthController.cs:247`, `275`). Engine: `Auth/Services/EmailCodeService.cs`, `Auth/Handlers/EmailCodeCommandHandlers.cs`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — guessed 6-digit code | Brute force | Five wrong attempts invalidate the code (`Auth/Services/EmailCodeService.cs:29`, `116-127`); 10-minute lifetime (line 23) |
| S — replayed code | Reuse after success | Single-use consumption on success (lines 63-74, consume path) |
| T — code substitution in store | Attacker DB write | Only SHA-256 digests persisted (lines 62-63); cache key is a hash of the token (line 149) |
| R — dispute of code sign-in | Attribution | Attempt recorder logs method `Email` with IP and user agent (`Auth/Services/AuthAttemptService.cs:29-57`) |
| I — timing oracle on code compare | Digest comparison leaks | Dummy-digest equalizer for unknown accounts (`Auth/Services/EmailCodeService.cs:38`, `91-92`) |
| D — request flood and mail-bombing | Exhaust inbox or API | Resend throttle per email digest (`Auth/Services/EmailVerificationService.cs:258-281`). Gap: per-IP limit — #1090 |
| E — code grants MFA-level token | Confusion of factors | Email codes establish first factor only; step-up rules unchanged (issuance through `SignInMfa` preparation, `Auth/Handlers/EmailCodeCommandHandlers.cs`) |

## 8. Flow 7 — Web3 (SIWE)

Entry: `POST /v1/auth/web3/challenge` (`Auth/Controllers/AuthController.cs:791`), `POST /v1/auth/web3:verify` (line 1046). Engine: `Auth/Services/Web3Service.cs`, `Auth/Services/Web3AuthService.cs`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — forged signature | Attacker signs for another wallet | EIP-4361 message verification with nonce, domain and chain binding (`Auth/Services/Web3Service.cs:17-121`; verify at line 128+) |
| S — challenge replay | Reuse of a signed message | Nonce single-binding: nonce maps to one wallet and expires in 5 minutes (`Auth/Services/Web3Service.cs:22`, `104-121`) |
| T — crafted SIWE fields (domain, statement, chain) | Confused-deputy signing | Domain authority enforced and chain allowlist (`Auth/Services/Web3Service.cs:29`, `77`) |
| R — dispute of wallet sign-in | Attribution | Failed attempts recorded as `Web3` method events (`Auth/Services/Web3AuthService.cs:48-54`); external login row ties wallet to account |
| I — wallet address enumeration | Probe which wallets have accounts | Generic failure responses on verify (`Auth/Services/Web3AuthService.cs:58`) |
| D — challenge flood | Nonce store exhaustion | In-memory challenge store with absolute expiry (bounded by TTL, `Auth/Services/Web3Service.cs:104-110`); endpoint rate-limited. Gap: challenge store is process-local `IMemoryCache`, not Redis — consistency risk under scale-out (consistent with #1151-class concern; no dedicated issue yet) |
| E — wallet links into privileged tenant | Auto-provision inflation | Wallet resolution re-uses user validation and tenant access context (`Auth/Services/Web3AuthService.cs:56-60`) |

## 9. Flow 8 — Password reset and change

Entries: `POST /v1/auth/password:reset-request`, `:reset`, `:change` (`Auth/Controllers/AuthController.cs:878`, `905`, `938`). Engine: `Auth/Handlers/PasswordCommandHandlers.cs`; tokens `Auth/Services/EmailVerificationService.cs`; account-recovery variant `Auth/Handlers/CompleteAccountRecoveryResetCommandHandler.cs`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — guessed reset token | Brute force the emailed secret | 128-bit random token (`Auth/Services/EmailVerificationService.cs:39`); 60-minute expiry (`Auth/Handlers/PasswordCommandHandlers.cs:91`); hash-keyed lookup (line 326 of the service) |
| S — reset token replay | Consume twice | Single-use `IsConsumed` flag (`Auth/Services/EmailVerificationService.cs:315`) |
| T — password history bypass | Reuse of old passwords | `PasswordHistoryPolicy.WasRecentlyUsed` on both reset and change (`Auth/Handlers/PasswordCommandHandlers.cs:142`, account-recovery variant at `Auth/Handlers/CompleteAccountRecoveryResetCommandHandler.cs:72`) |
| R — password change denial | User denies changing password | Full revocation battery audited with reason `Password reset` (`Auth/Handlers/PasswordCommandHandlers.cs:158-168`) |
| I — account existence via reset request | Behavior differs for unknown emails | Always-acknowledge response contract (`Auth/Handlers/PasswordCommandHandlers.cs:61-94` logs internally only) |
| I — reset token store in memory | Process-local storage weakens secrecy under scale-out | Gap: #1151 (`Auth/Services/EmailVerificationService.cs:15`, `60-62`) |
| D — reset-request mail flood | Inbox exhaustion | Per-email resend throttle 2 minutes (`Auth/Services/EmailVerificationService.cs:261-281`). Gap: per-IP — #1090 |
| E — reset on unverified email steals account | Attacker registers the victim email | Gap: #1124 (account recovery for unverified email). Post-reset elevation contained: all refresh tokens, sessions and legacy access tokens revoked via version cutoff (`Auth/Handlers/PasswordCommandHandlers.cs:158-168`; `Auth/Services/PasswordChangeRevocationService.cs`) |
| E — account recovery threshold bypass | Single approver resets MFA | Threshold evaluator and approvals (`Auth/Services/AccountRecoveryThresholdEvaluator.cs`; `Auth/Entities/RecoveryApproval.cs`; fixed-time hash compare at `Auth/Handlers/CompleteAccountRecoveryResetCommandHandler.cs:48`) |

## 10. Flow 9 — Token refresh

Entry: `POST /v1/auth/tokens:refresh` (`Auth/Controllers/AuthController.cs:675`). Engine: `Auth/Services/LocalAuthService.cs:725-932`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — stolen refresh token used | Attacker rotates first | Rotation marks predecessor `ReplacedByToken`; reuse detected as replay (`Auth/Services/LocalAuthService.cs:760-776`) and triggers containment (934-972) — family revocation by default, else account-wide, plus token-version increment killing all access tokens |
| S — replay race (two concurrent rotations) | Both requests succeed | Atomic claim via `TryRevokeForRotationAsync`; loser is contained (`Auth/Services/LocalAuthService.cs:886-901`) |
| T — token substitution | Presenting another user's token | Hash lookup binds token to user row (`Auth/Services/LocalAuthService.cs:741-758`) |
| T — lifetime inflation via rotation | Sliding renewal exceeds policy | Renewal resolves a configured lifetime class from the originating policy; non-sliding mode clamps to predecessor deadline (`Auth/Services/LocalAuthService.cs:822-845`) |
| R — rotation dispute | Claim token was not used by owner | Rejections and rotations recorded as lifecycle events with reason codes (`Auth/Services/LocalAuthService.cs:771-772`, `908-909`; metrics `Auth/Services/RefreshTokenLifecycleMetrics.cs`) |
| I — refresh response leakage | Token captured from logs | Tokens never logged; lifecycle logs reference token ids only (`Auth/Services/JwtTokenService.cs:294`) |
| D — refresh flood | DB pressure via rotation attempts | Rate-limited endpoint; replay containment is idempotent per family |
| E — refresh after account disable/deletion | Ghost session persists | User re-fetch and validation; unavailable user rejected (`Auth/Services/LocalAuthService.cs:789-796`). Gap: deleted user with still-valid refresh token row — #1119 |
| E — refresh across tenant boundary | Token rotates into unauthorized tenant | Tenant access re-resolution with explicit denial reason (`Auth/Services/LocalAuthService.cs:800-807`) |
| E — session-inactive refresh | Dead session mints new tokens | `RefreshSessionAsync` false branch triggers containment (`Auth/Services/LocalAuthService.cs:871-884`) |

## 11. Flow 10 — Session management and sign-out

Entry points: `Auth/Controllers/SessionController.cs` (list, terminate, rename, terminate-others, security analysis), `POST /v1/auth/tokens:revoke` (`Auth/Controllers/AuthController.cs:708`), `GET /v1/auth/session-state` (line 735). Engine: `Auth/Services/SessionManagementService.cs`.

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — session hijack via stolen refresh hash | DB theft enables session takeover | Only hashes stored (`Auth/Services/JwtTokenService.cs:269-282`); session row keyed by hash (`Auth/Services/SessionManagementService.cs:89`) |
| T — session row tampering | Extending expiry directly | Deadline re-asserted from policy on every mutation (`Auth/Services/AuthenticatedSessionDeadline.cs` enforced at `Auth/Services/LocalAuthService.cs:325-326`, `906-907`) |
| R — terminate-others dispute | User denies terminating sessions | Terminations carry `SessionTerminationReason` (`Auth/Services/SessionManagementService.cs:277`, `312`). Gap: notify user when all other sessions are terminated — #1091 |
| I — cross-session visibility | One session reads another's data | Sessions are owner-scoped by user id (`Auth/Services/SessionManagementService.cs:94`) |
| D — unbounded session growth | Storage exhaustion | Expired-session cleanup job (`Auth/Services/SessionManagementService.cs:622`). Gap: concurrent session limit per user — #1086 |
| E — session survives tenant deletion | Orphaned access | Gap: #1120. Guest variant: expiry during active use — #1128 |
| E — sign-out does not revoke server state | Client-side logout only | Revoke endpoint `Auth/Controllers/AuthController.cs:708` with hash-matched revocation (`Auth/Services/LocalAuthService.cs:1073`; `Auth/Services/JwtTokenService.cs:408-454`). Gap: `LogoutCommand` handler exists but no dedicated sign-out endpoint is exposed — #1154 (`Auth/Handlers/LogoutHandler.cs`) |

Trusted devices (`Auth/Services/SessionManagementService.cs:366-530`) and trusted contacts (`Auth/Controllers/TrustedContactsController.cs`): re-verification and pruning gaps tracked in #1144; contact-email failure atomicity gap in #1145.

## 12. Cross-cutting threats

| Threat | Scenario | Mitigation or gap |
| --- | --- | --- |
| S — signing key compromise | Long-lived HS256 secret exposed | Startup strength guard (`Host/Core/Setup/OperationalStartupConfiguration.cs:20-26`). Gaps: rotation catalog unwired — #1141; mass-disconnect concern during rotation — #1123 |
| E — mTLS client spoofing | Client-certificate auth surface | Scheme exists (`Host/Core/Extensions/SecurityServiceCollectionExtensions.cs:221-225`); gap: `BindCertificate` has no API/command surface — #1146 (`Auth/Services/ClientCertificateAuthenticationHandler.cs`) |
| E — API key scope creep | Keys exceeding granted scopes | Scoped keys with lifecycle options (`Auth/Configuration/ApiKeyLifecycleOptions.cs`; handler `Auth/Services/ApiKeyAuthenticationHandler.cs`). Gap: mid-request expiry semantics — #1122 |
| E — service account impersonation of user | Machine token used on user endpoints | Distinct `actor_kind=Service` claim and immutable `ActorContext` (`Auth/Services/JwtTokenService.cs:490-492`; `apps/api/Source/Modules/GameGuild.Identity.Context/Actors/`). Gap: PATCH persistence bug — #1142 |
| D — anomaly engine DoS by poisoned baselines | Attacker shapes learned statistics | Baselines are per-subject with bounded IP set and variance floor (`Auth/Services/AdaptiveAnomalyDetectionService.cs:177-183`); learning failures fail open to heuristics (lines 71-84). Gap: legitimate-travel false positives — #1115 |
| I — Redis cache disclosure | Revocation cache read | Out of module scope; Redis required in protected environments (`Host/Core/Setup/OperationalStartupConfiguration.cs:37-42`) |

## 13. Threat-to-issue index

| Issue | Flow | STRIDE class | Summary |
| --- | --- | --- | --- |
| #1082 | Sign-in | S/D | Turnstile CAPTCHA after N failed attempts |
| #1083 | Sign-in | R | Lockout notification email |
| #1084 | Sign-in | D | Progressive delay on failures |
| #1085 | Sign-in | R | New-device sign-in alert |
| #1086 | Sessions | D/E | Concurrent session limit |
| #1087 | Sign-in | E | Geo-fencing option for tenants |
| #1088 | Password | T | Password age rotation policy |
| #1089 | Sign-in | S/D | Attacker IP blocklist |
| #1090 | Magic link, email code, reset | D | Per-IP rate limit on email-bound requests |
| #1091 | Sessions | R | Notify on terminate-others |
| #1115 | Sign-in | D | GeoVelocity timezone whitelist false positives |
| #1116 | OAuth | D | Provider downtime fallback |
| #1117 | MFA | E | Concurrent enrollment/sign-in race |
| #1118 | OAuth | S/E | Email case-sensitivity conflict |
| #1119 | Refresh | E | Deleted user ghost session |
| #1120 | Sessions | E | Tenant deletion orphaned sessions |
| #1121 | OAuth | S/E | Multi-OIDC same-email conflict |
| #1122 | API keys | S | Mid-request expiry |
| #1123 | Tokens | D/S | Key rotation mass disconnect |
| #1124 | Recovery | S/E | Unverified-email account takeover |
| #1128 | Guest | D | Guest expiry during active use |
| #1141 | Tokens | S/T | Rotation catalog unwired |
| #1142 | Service accounts | T | PATCH loses changes |
| #1143 | WebAuthn | S/T/E | Attestation conveyance None, UserVerified hard-coded |
| #1144 | Trusted devices | S/E | Fresh-verification and pruning |
| #1145 | Trusted contacts | D/R | Email failures abort without outbox |
| #1146 | mTLS | S | BindCertificate has no API or command surface |
| #1150 | Sign-up | S | API-level CAPTCHA |
| #1151 | Password, email verify | I/D | Process-local token cache |
| #1152 | MFA | E | Email-MFA enrollment endpoint missing |
| #1153 | MFA | E | Re-verification on backup-code/push register |
| #1154 | Sign-out | E | No dedicated sign-out endpoint |
| #1155 | MFA recovery | R | Durable queue bypass |

New observation surfaced by this analysis (not yet tracked as an issue): sign-up returns a distinguishable error for existing accounts (`Auth/Services/LocalAuthService.cs:620-627`), an enumeration signal the sign-in path does not have. Recommend an issue if triage agrees the signal is worth removing (returning the same generic acknowledgment on both paths).
