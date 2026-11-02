# Sign-Up Flow (Email + Password Registration, Verification Email, Guest Upgrade) — User Flow Diagram

> Issue #1096 · Module `GameGuild.Identity.Authentication` (+ `GameGuild.Identity.Users`, web anti-bot edge) · Documented against `develop` commit `74e9bba4c`.
> Scope: email+password registration, Turnstile CAPTCHA gating at the web edge, username/password validation, duplicate-account handling, first-session issuance, the verification email pipeline, token-based email verification, guest-session upgrade on signup (issue #213), and the compliance gates that do (and do not) apply at registration.

## 1. Overview

Registration starts at `POST v1/auth/sign-up` (`Controllers/AuthController.cs:33-54`, `[AllowAnonymous]`, `Authentication` rate-limit policy at `:20`). Anti-bot gating (Cloudflare Turnstile) is enforced **unconditionally at the Next.js web edge** — `apps/web/src/auth.ts:98-112` wraps every password sign-up with `verifyTurnstileToken` (`apps/web/src/lib/antibot/turnstile.ts:31-77`: fail-open when `TURNSTILE_SECRET_KEY` is unset, fail-closed on `missing-token`/`verify-failed`, fail-open on Cloudflare outage) — the .NET API itself has no CAPTCHA check on this route.

The command is `LocalSignUpCommand` (`Commands/LocalSignUpCommand.cs:8-26`, carries optional `GuestAccessToken`). `ValidationBehavior` runs `LocalSignUpCommandValidator` (`Validators/LocalSignUpCommandValidator.cs:13-58`, email format + RFC 5321 length, password strength via `IPasswordHasher`, username slug rules) and throws `RequestValidationException` → 400 (`SharedKernel/Middlewares/ExceptionHandlingMiddleware.cs:43-58`). `LocalSignUpHandler` (`Handlers/LocalSignUpHandler.cs:10-63`) delegates to `LocalAuthService.LocalSignUpAsync` (`Services/LocalAuthService.cs:594-723`): duplicate-email guard (`:620-628`), `User.CreateWithPassword` (`GameGuild.Identity.Users/Entities/User.cs:563-585`), `UserCreatedEvent` durable integration event (`:638-646`), user row + unique `Users.Email`/`Users.Username` indexes (`GameGuild.API/Database/Migrations/ApplicationDbContextModelSnapshot.cs:23697-23703`), default tenant provisioning (`:655`), then immediate session issuance (refresh + access token + `usersession` row, `:662-691`) — **sign-up signs the user in** (201 with tokens). Back in the handler, the verification email is dispatched via `SendEmailVerificationCommand` (`Handlers/LocalSignUpHandler.cs:42-48`) and an email-delivery failure never fails the committed account (`:50-56`). Finally `GuestSessionUpgradeIntegration.ApplyGuestUpgradeAsync` (`Guest/GuestSessionUpgradeIntegration.cs:13-40`) upgrades the anonymous session into the new authenticated session; an invalid guest token never blocks registration.

Verification tokens are 16 random bytes, hex, 24-hour lifetime, stored in **`IMemoryCache`** (`Services/EmailVerificationService.cs:27-30`, `:47-78`) — see Gaps. `POST v1/auth/email:verify` (`Controllers/AuthController.cs:882-905`) consumes the token single-use (`EmailVerificationService.cs:163-246`) and flips `User.IsEmailVerified` (`Entities/User.cs:267-271`). KYC/compliance gates do **not** run at sign-up: `KycAndHoldSignInCompliancePolicy` (`GameGuild.API/Core/Compliance/KycAndHoldSignInCompliancePolicy.cs:58-91`) evaluates at sign-in, and nothing at registration blocks an unverified email from receiving working tokens.

## 2. End-to-End Flowchart — happy path and every edge case

```mermaid
flowchart TD
    subgraph WEB["Web edge - Turnstile gate"]
        W1["Signup form renders TurnstileWidget<br/>apps web src components signup-form.tsx L234"] --> W2["Client submits email password username<br/>plus turnstileToken and optional guestAccessToken<br/>signup-form.tsx L104"]
        W2 --> W3{"verifyTurnstileToken<br/>apps web src auth.ts L104 and lib antibot turnstile.ts L31"}
        W3 -- "secret unset - disabled" --> W4["fail-open success true reason disabled<br/>turnstile.ts L34"]
        W3 -- "token missing or over 2048 chars" --> W5["fail-closed CredentialsSignInError Bot verification failed<br/>turnstile.ts L26 and auth.ts L106"]
        W3 -- "Cloudflare rejects" --> W5
        W3 -- "siteverify network error" --> W4b["fail-open success true reason verify-unavailable<br/>turnstile.ts L70"]
        W4 --> API1
        W4b --> API1
        W5 --> W6["token reset so widget re-issues<br/>signup-form.tsx L118"]
    end

    API1["POST v1 auth sign-up - AllowAnonymous<br/>AuthController.cs L33"] --> V1{"ValidationBehavior runs<br/>LocalSignUpCommandValidator<br/>ValidationBehavior.cs L32"}
    V1 -- "bad email or weak password or bad username slug" --> V400["RequestValidationException<br/>400 Validation Failed problem details<br/>ExceptionHandlingMiddleware.cs L43"]
    V1 -- "valid" --> H1["LocalSignUpHandler Handle<br/>LocalSignUpHandler.cs L20"]
    H1 --> S1["LocalAuthService LocalSignUpAsync<br/>LocalAuthService.cs L594"]
    S1 --> S2{"UsernameSlug.Normalize produces<br/>3 to 50 char handle - L605"}
    S2 -- "no" --> S400b["RequestValidationException 400 - L608"]
    S2 -- "yes" --> S3{"passwordHasher ValidatePasswordStrength<br/>L612"}
    S3 -- "fails" --> S400b
    S3 -- "passes" --> S4{"userRepository ExistsByEmailAsync<br/>lowercased email - L620"}
    S4 -- "email already registered" --> S500a["InvalidOperationException User already exists - L627<br/>generic catch maps to 500 - ExceptionHandlingMiddleware.cs L74<br/>declared 409 never produced - see Gaps"]
    S4 -- "new email" --> S5["passwordHasher HashPassword - L630"]
    S5 --> S6["User CreateWithPassword<br/>email lowercased IsActive true<br/>UsernameSlug FromExplicit - User.cs L563"]
    S6 --> S7["UserCreatedEvent added as integration event<br/>LocalAuthService.cs L638"]
    S7 --> S8["AddAsync then SaveChangesAsync - L649"]
    S8 -- "concurrent signup same email or username<br/>unique index race" --> S500b["DbUpdateException surfaces as 500<br/>Users Email and Username unique indexes<br/>ApplicationDbContextModelSnapshot.cs L23697"]
    S8 -- "saved" --> S9["DefaultTenantMembershipProvisioner EnsureAsync<br/>LocalAuthService.cs L655"]
    S9 --> S10["ResolveTenantAccessContextAsync - L660"]
    S10 -- "tenant denied" --> S500c["AccessDeniedException - see Gaps"]
    S10 -- "active tenant" --> S11["GenerateRefreshTokenAsync L666 plus<br/>GenerateAccessTokenAsync L672"]
    S11 --> S12["sessionManagementService CreateSessionAsync<br/>usersession row - L681"]
    S12 --> S13["AuthenticatedSessionDeadline Require - L690"]
    S13 --> S14["authAttemptService RecordSuccessfulAttemptAsync<br/>Registration - L694"]
    S14 --> S15["SignInResponse with tokens session tenant - L700"]

    S15 --> EV1["SendEmailVerificationCommand<br/>LocalSignUpHandler.cs L42"]
    EV1 --> EV2{"SendEmailVerificationCommandHandler<br/>resolve user by id or email<br/>SendEmailVerificationCommandHandler.cs L23"}
    EV2 -- "user missing" --> EV3["generic response - no enumeration oracle - L33"]
    EV2 -- "user found" --> EV4{"delivery retry loop up to 2 attempts - L42"}
    EV4 -- "attempt fails" --> EV5["retry then LogCritical plus<br/>Authentication.EmailVerificationDeliveryFailed audit - L63"]
    EV4 -- "token generated and email queued" --> EV6["EmailVerificationService<br/>GenerateVerificationTokenAsync 24h - L27<br/>SendVerificationEmailAsync publishes notification - L80"]
    EV4 -- "queue unavailable" --> EV7["exception swallowed - signup stays successful<br/>message set to verification pending - LocalSignUpHandler.cs L50"]
    EV5 --> EV8["account is committed - response 201 either way"]
    EV6 --> EV8
    EV3 --> EV8
    EV7 --> EV8

    EV8 --> G1{"GuestAccessToken presented<br/>GuestSessionUpgradeIntegration.cs L22"}
    G1 -- "absent or signup not successful" --> R1["201 Created with SignInResponse"]
    G1 -- "present" --> G2["GuestSessionService UpgradeAsync<br/>GuestSessionService.cs L97"]
    G2 -- "token invalid or unparsable" --> G3["denied InvalidGuestToken plus audit<br/>Authentication.GuestSessionUpgradeRejected - L112"]
    G2 -- "guest session not found or already upgraded" --> G4["denied GuestSessionUnavailable plus audit - L119"]
    G2 -- "TryMarkUpgradedAsync wins" --> G5["audit Authentication.GuestSessionUpgraded - L130<br/>UpgradedFromGuestSessionId set on response - L36"]
    G3 --> R1
    G4 --> R1
    G5 --> R1

    subgraph VERIFY["Email verification - POST v1 auth email verify"]
        VE1["Client POST token from email link<br/>AuthController.cs L882"] --> VE2["VerifyEmailCommandHandler<br/>PasswordCommandHandlers.cs L13"]
        VE2 --> VE3{"ValidateAndConsumeTokenAsync<br/>EmailVerificationService.cs L163"}
        VE3 -- "token empty or unknown or wrong type<br/>or expired or already consumed" --> VE400["Success false<br/>400 BadRequest from controller - AuthController.cs L899"]
        VE3 -- "valid first use" --> VE4["cache verified flag then remove token - L227"]
        VE4 --> VE5{"user row found by id - L25"}
        VE5 -- "missing" --> VE400b["Success false User not found - 400"]
        VE5 -- "found and not yet verified" --> VE6["user VerifyEmail then UpdateAsync SaveChanges<br/>User.cs L267"]
        VE5 -- "already verified" --> VE7["skip write - idempotent - L35"]
        VE6 --> VE200["200 EmailVerificationResult with VerifiedAt"]
        VE7 --> VE200
    end

    subgraph CONSUME["UserCreatedEvent consumers - async"]
        UC1["identity.user.created.v1 durable event<br/>ResourceLifecycleEvents.cs L3"] --> UC2["SendWelcomeEmailHandler HandleAsync<br/>SendWelcomeEmailHandler.cs L18"]
        UC2 -- "user deleted" --> UC3["skip - no welcome email - L23"]
        UC2 -- "active user" --> UC4["notification Onboarding welcome - L47"]
    end
```

## 3. CQRS Chain Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    participant B as Browser signup form
    participant T as Turnstile verify web edge
    participant C as AuthController
    participant S as ISender CQRS pipeline
    participant H as LocalSignUpHandler
    participant A as LocalAuthService
    participant U as UserRepository
    participant E as SendEmailVerificationCommandHandler
    participant G as GuestSessionService
    participant DB as Users table

    B->>T: signUp credentials plus turnstileToken
    T->>T: verifyTurnstileToken - auth.ts L104
    T-->>B: rejected CredentialsSignInError when Cloudflare rejects
    B->>C: POST v1 auth sign-up
    C->>S: LocalSignUpCommand
    S->>S: ValidationBehavior LocalSignUpCommandValidator
    S->>H: Handle - LocalSignUpHandler.cs L20
    H->>A: LocalSignUpAsync request
    A->>U: ExistsByEmailAsync lowercased email - L620
    U-->>A: false
    A->>A: User CreateWithPassword plus UserCreatedEvent - L633 L638
    A->>U: AddAsync SaveChangesAsync - L649
    U->>DB: INSERT Users row
    DB-->>U: ok
    A->>A: refresh token plus access token plus usersession - L666 L672 L681
    A-->>H: SignInResponse Success true
    H->>E: SendEmailVerificationCommand - L42
    E->>E: generate 24h token in memory cache plus queue email
    E-->>H: generic response
    H->>G: UpgradeAsync guestAccessToken userId sessionId - L62
    G-->>H: upgrade result - never blocks signup
    H-->>S: SignInResponse
    S-->>C: SignInResponse
    C-->>B: 201 Created with tokens
```

## 4. Data Flow Diagram

```mermaid
flowchart TD
    DF1["Browser signup form"] -->|"turnstileToken"| DF2["Next.js web edge verifyTurnstileToken"]
    DF2 -->|"Cloudflare siteverify call with secret and remoteip"| DF3["challenges.cloudflare.com"]
    DF1 -->|"email password username guestAccessToken"| DF4["AuthController POST auth sign-up"]
    DF4 -->|"LocalSignUpCommand via ISender"| DF5["LocalSignUpHandler then LocalAuthService"]
    DF5 -->|"hashed lookup plus insert on email and username"| DF6["Users table - unique Email and Username"]
    DF5 -->|"refresh token hash and session row"| DF7["refreshtoken and usersession tables"]
    DF5 -->|"tenant membership provisioning"| DF8["TenantMembership"]
    DF5 -->|"verification token - 16 bytes hex 24h"| DF9["IMemoryCache emailverify token keys"]
    DF5 -->|"EmailVerificationRequestedNotification"| DF10["Notifications email pipeline"]
    DF10 -->|"verification link with token"| DF1
    DF1 -->|"token from email"| DF11["POST auth email verify"]
    DF11 -->|"IsEmailVerified flag update"| DF6
    DF5 -->|"identity.user.created.v1 durable event"| DF12["Integration event outbox"]
    DF12 -->|"welcome onboarding notification"| DF13["NotificationService"]
    DF5 -->|"Registration attempt record plus guest upgrade audits"| DF14["audit tables"]
```

## 5. Edge-Case Matrix

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Turnstile secret not configured | fail-open branch (`apps/web/src/lib/antibot/turnstile.ts:34-40`) | signup allowed, one warning per process |
| 2 | Turnstile token missing / > 2048 chars | fail-closed (`turnstile.ts:26`, `:42-45`) | `CredentialsSignInError` "Bot verification failed"; widget token reset for re-issue (`signup-form.tsx:118`) |
| 3 | Cloudflare siteverify outage | fail-open `verify-unavailable` (`turnstile.ts:70-73`) | signup allowed (availability over strictness) |
| 4 | Malformed email / email > 254 chars | validator (`Validators/LocalSignUpCommandValidator.cs:13-21`) + `ValidationBehavior` (`SharedKernel/CQRS/Behaviors/ValidationBehavior.cs:50-54`) | 400 Validation Failed with property errors |
| 5 | Weak password | `IPasswordHasher.ValidatePasswordStrength` via validator (`LocalSignUpCommandValidator.cs:28-43`) and re-check in service (`LocalAuthService.cs:612-617`) | 400 with per-rule failures |
| 6 | Username does not slugify to 3–50 chars | validator `Must` clause (`LocalSignUpCommandValidator.cs:54-55`) + service guard (`LocalAuthService.cs:605-610`) | 400 |
| 7 | Email already registered | `ExistsByEmailAsync` → `InvalidOperationException("User already exists")` (`LocalAuthService.cs:620-628`); generic catch (`ExceptionHandlingMiddleware.cs:74-85`, `:141-155`) | **500** (declared 409 at `AuthController.cs:38` is never produced — see Gaps) |
| 8 | Two concurrent signups with same email (TOCTOU) | both pass `ExistsByEmailAsync`, loser hits unique `Users.Email` index (`ApplicationDbContextModelSnapshot.cs:23697-23698`) | loser gets unhandled `DbUpdateException` → 500 |
| 9 | Duplicate username | unique `Users.Username` index (`ApplicationDbContextModelSnapshot.cs:23700-23701`); no pre-check in service | 500, no friendly duplicate-username message |
| 10 | Guest token invalid / expired / already upgraded | `GuestSessionService.UpgradeAsync` denies with audited reason (`Guest/GuestSessionService.cs:109-127`) | registration still succeeds; continuity link dropped; `Authentication.GuestSessionUpgradeRejected` audit |
| 11 | Guest token missing / response not successful | early return (`Guest/GuestSessionUpgradeIntegration.cs:22-28`) | plain 201, no upgrade attempted |
| 12 | Verification email pipeline unavailable after account commit | catch-all in handler (`Handlers/LocalSignUpHandler.cs:50-56`) | signup still 201; message says "request again"; resend endpoint available |
| 13 | Verification delivery fails twice | 2-attempt loop + `LogCritical` + `Authentication.EmailVerificationDeliveryFailed` audit (`Handlers/SendEmailVerificationCommandHandler.cs:42-70`) | enumeration-safe generic 200; failure visible in logs/audit only |
| 14 | `POST v1/auth/email:send-verification` for unknown email | unknown-account branch (`SendEmailVerificationCommandHandler.cs:30-34`) | identical generic message — no account enumeration |
| 15 | Verification token expired (24 h) | `ExpiresAt < now` → remove + fail (`Services/EmailVerificationService.cs:198-205`) | 400 "Expired token"; user must resend |
| 16 | Verification token reused (already consumed) | `TryConsume()` fails (`EmailVerificationService.cs:219-225`; token removed after consume `:232`) | 400 "Invalid token" |
| 17 | Password-reset token presented to email verify (type confusion) | `tokenInfo.Type != expectedType` (`EmailVerificationService.cs:207-217`) | 400 "Invalid token type" |
| 18 | Token belongs to different user (user-scoped verify overload) | `expectedUserId` mismatch (`EmailVerificationService.cs:188-196`) | 400 "Token does not belong to the requested user" |
| 19 | Verify token valid but user row deleted | `user is null` (`Handlers/PasswordCommandHandlers.cs:26-33`) | 400 "User not found" |
| 20 | Verify called when already verified | `if (!user.IsEmailVerified)` skip (`PasswordCommandHandlers.cs:35-40`) | idempotent 200 success |
| 21 | API restart / multi-node between send and verify | tokens in `IMemoryCache` only (`EmailVerificationService.cs:60`) | all pending tokens lost (or invisible to other node) — see Gaps |
| 22 | Resend verification spam | endpoint-level `Authentication` rate limit (`AuthController.cs:20`); the 2-minute per-address limiter in `ResendVerificationEmailAsync` (`EmailVerificationService.cs:281-294`) is **not** called by the CQRS handler | only global rate limit applies (see Gaps) |
| 23 | Compliance hold / KYC state at sign-up | `KycAndHoldSignInCompliancePolicy` evaluates at sign-in (`GameGuild.API/Core/Compliance/KycAndHoldSignInCompliancePolicy.cs:58-91`); no call in `LocalSignUpAsync` | sign-up itself has no compliance gate; tokens are issued before any KYC challenge |
| 24 | Unverified email signs in | `RequiresEmailVerification` (`GameGuild.Identity.Users/Entities/User.cs:508-512`) has no callers in the sign-in path | unverified accounts can authenticate (by design; verification is not a login gate) |

## 6. Endpoint Inventory

| Method + Route | Handler chain | Controller ref |
|----------------|---------------|----------------|
| POST `v{v}/auth/sign-up` | `LocalSignUpCommand` → `LocalSignUpHandler` → `LocalAuthService.LocalSignUpAsync` (+ nested `SendEmailVerificationCommand`, guest upgrade) | `Controllers/AuthController.cs:33-54` |
| POST `v{v}/auth/email:send-verification` | `SendEmailVerificationCommand` → `SendEmailVerificationCommandHandler` | `Controllers/AuthController.cs:861-873` |
| POST `v{v}/auth/email:verify` | `VerifyEmailCommand` → `VerifyEmailCommandHandler` | `Controllers/AuthController.cs:882-905` |

All three are `[AllowAnonymous]` under the `Authentication` rate-limit policy (`Controllers/AuthController.cs:20`).

## 7. Events Published / Consumed

- **Published:**
  - `UserCreatedEvent` (`identity.user.created.v1`, durable integration event) — attached to the aggregate in `LocalAuthService.cs:638-646`, defined at `GameGuild.SharedKernel/Events/ResourceLifecycleEvents.cs:3-10`.
  - `EmailVerificationRequestedNotification` (in-process MediatR notification carrying the token) — `Services/EmailVerificationService.cs:84-90`.
  - `AuthenticationAuditEvent` rows: successful "Registration" attempt (`LocalAuthService.cs:694`), email-verification delivery failure (`Handlers/SendEmailVerificationCommandHandler.cs:87-93`), guest upgrade accepted/rejected (`Guest/GuestSessionService.cs:130-137`, `:146-155`).
- **Consumed:**
  - `SendWelcomeEmailHandler` handles `UserCreatedEvent` (`IIntegrationEventHandler<UserCreatedEvent>`, `Handlers/SendWelcomeEmailHandler.cs:14-30`) — records an Onboarding welcome notification, skipping deleted users.
  - The notifications module's email sender handles `EmailVerificationRequestedNotification` (delivery of the actual mail).

## 8. Entities and Tables Touched

| Entity / Table | Mapping | Writes |
|----------------|---------|--------|
| `User` → `Users` (unique `Email`, unique `Username` indexes) | `GameGuild.Identity.Users/Entities/UserConfiguration.cs:14-66`; indexes visible in `ApplicationDbContextModelSnapshot.cs:23695-23703` | INSERT (`UserRepository.AddAsync` via `LocalAuthService.cs:649`); UPDATE `IsEmailVerified` (`Handlers/PasswordCommandHandlers.cs:38-39`) |
| `UserSession` → `gameguild.authentication.usersession` | snapshot `:20694` | INSERT at sign-up session creation (`LocalAuthService.cs:681`) |
| `RefreshToken` → `gameguild.authentication.refreshtoken` | `JwtTokenService.GenerateRefreshTokenAsync` (`LocalAuthService.cs:666`) | INSERT (hash only) |
| `TenantMembership` | `DefaultTenantMembershipProvisioner.EnsureAsync` (`Services/DefaultTenantMembershipProvisioner.cs:9`, called `LocalAuthService.cs:655`) | INSERT (default membership) |
| `AuthenticationAttempt` | `AuthenticationAttemptConfiguration` (index `ix_authenticationattempt_email`) | INSERT "Registration" success (`LocalAuthService.cs:694`) |
| Guest session store | `GuestSessionService` / `IGuestSessionStore.TryMarkUpgradedAsync` (`Guest/GuestSessionService.cs:124`) | UPDATE upgraded-by/session link |
| `IMemoryCache` (not a table) | `EmailVerificationService.cs:60`, `:229` | verification token + verified flag (volatile) |

## 9. Gaps — realistic scenarios the code does not handle

1. **Duplicate email returns 500, not the declared 409.** `AuthController.LocalSignUp` declares `[ProducesResponseType(StatusCodes.Status409Conflict)]` (`Controllers/AuthController.cs:38`) but `LocalAuthService` throws plain `InvalidOperationException("User already exists")` (`Services/LocalAuthService.cs:627`), which falls into the generic 500 bucket (`SharedKernel/Middlewares/ExceptionHandlingMiddleware.cs:74-85`). Clients cannot distinguish "already registered" from a server fault; `GameGuild.Identity.Users/Models/UserAlreadyExistsException.cs:11` exists but is never thrown on this path.
2. **TOCTOU duplicate registration race.** The duplicate check (`ExistsByEmailAsync`, `LocalAuthService.cs:620`) and the insert (`:649`) are not atomic; two concurrent signups with the same email (or username) rely on the DB unique indexes (`ApplicationDbContextModelSnapshot.cs:23697-23701`), and the loser surfaces an unhandled `DbUpdateException` → 500 with no retry or friendly conflict mapping. Unlike the external-login link path, there is no `DbUpdateException` refetch/retry branch here.
3. **Verification tokens live only in `IMemoryCache`.** `EmailVerificationService.GenerateTokenAsync` stores the token with a 24-hour absolute expiry in process memory (`Services/EmailVerificationService.cs:60-63`). An API restart, scale-out to a second node (token created on node A, verify hits node B), or memory pressure eviction silently invalidates every in-flight verification email; the user just sees "Invalid token" (`:184-186`). The magic-link flow already has a durable `IMagicLinkTokenStore` (`:41`) — email-verification tokens do not.
4. **Sign-up does not verify email before issuing tokens.** `LocalSignUpAsync` issues access + refresh tokens and creates a session (`LocalAuthService.cs:662-691`) before any email verification. `User.RequiresEmailVerification` (`GameGuild.Identity.Users/Entities/User.cs:508-512`) has no callers, so unverified registrants can authenticate indefinitely; there is no re-verification challenge, no lockout for never-verified accounts, and no cleanup job for them.
5. **No server-side CAPTCHA on the API route.** Turnstile is enforced only in the Next.js web wrapper (`apps/web/src/auth.ts:98-112`); `POST /v1/auth/sign-up` itself accepts any request (`[AllowAnonymous]`, no captcha claim checked). A direct API caller bypasses anti-bot entirely; `AuthenticationSecurityOptions.EnableCaptchaOnSuspiciousActivity` (`GameGuild.SharedKernel/Configuration/ApplicationLayer/AuthenticationSecurityOptions.cs:71`, default false) is not wired to this endpoint.
6. **Resend endpoint bypasses the per-address cooldown.** `EmailVerificationService.ResendVerificationEmailAsync` implements a 2-minute rate limit keyed on user + email digest (`Services/EmailVerificationService.cs:271-294`), but `SendEmailVerificationCommandHandler` calls `GenerateVerificationTokenAsync` + `SendVerificationEmailAsync` directly (`Handlers/SendEmailVerificationCommandHandler.cs:46-47`) and never invokes the cooldown method — an attacker (within the global policy limit) can trigger unbounded verification mails to a victim's address.
7. **Sign-up has no compliance gate.** The KYC/compliance-hold gate exists only on the sign-in path (`GameGuild.API/Core/Compliance/KycAndHoldSignInCompliancePolicy.cs:58-91`); a signup that will immediately be challenged/denied at next sign-in still provisions the user, tenant membership, session and welcome email. Account enumeration via timing (existing-email path returns quickly with 500; new-email path does hashing + inserts) is also unmitigated.
8. **Guest-upgrade failure is silent to the client.** `GuestSessionUpgradeIntegration` only sets `UpgradedFromGuestSessionId` on success (`Guest/GuestSessionUpgradeIntegration.cs:34-37`); a rejected upgrade (invalid/expired/already-used guest token) produces no response field or warning — the web form unconditionally clears the stored guest session after signup (`apps/web/src/components/signup-form.tsx:116-117`), so a failed upgrade permanently discards the guest cart/progress with no recovery prompt.
