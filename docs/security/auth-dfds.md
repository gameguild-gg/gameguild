# Authentication Data Flow Diagrams

Closes #1137.

Status: living document, current as of 2026-11-02 against `origin/develop` (a96679c7a).

Companion documents: [auth-security-architecture.md](auth-security-architecture.md) (#1135) for trust boundaries B1-B8 and the controls catalog, [auth-stride-threat-model.md](auth-stride-threat-model.md) (#1136) for per-flow STRIDE analysis.

## 1. Notation and conventions

Mermaid flowchart is used with standard DFD shapes:

- External entity — rectangle: `E1["User browser"]`
- Process — stadium: `P1(["1.0 Sign in"])`
- Data store — cylinder: `D1[("PostgreSQL auth database")]`
- Data flow — labeled arrow: `P1 -->|"credentials"| D1`
- Trust boundary — subgraph per zone (client, API, external services)

Processes are numbered per flow family F1-F12, matching the twelve user-flow issues #1096-#1107 (batch 1 and 2 of the authentication user-flow documentation effort). Those user-flow documents are not yet merged to `develop` (issues open at time of writing); this document is the first data-flow rendering of the same twelve families and should be kept consistent with them when they land. Credential-only families analyzed in the STRIDE document (password sign-in, federated sign-in, passwordless, password reset, token refresh) get DFDs in section 4 because they carry the highest-risk data flows; their flow-issue coverage is the STRIDE document's scope.

Paths are relative to the repository root. `Auth/` abbreviates `apps/api/Source/Modules/GameGuild.Identity.Authentication/`; `Host/` abbreviates `apps/api/Source/GameGuild.API/`.

## 2. Level 0 — context diagram

The authentication module as a single process with all external actors and stores.

```mermaid
flowchart LR
    USER["User with browser or app"]
    PROXY["Reverse proxy and CDN"]
    MAIL["Email provider"]
    SMS["SMS provider"]
    PUSH["Push provider"]
    IDP["Federated identity providers"]
    CHAIN["EVM blockchain"]
    TIF["Threat intel feed"]

    AUTH(["0.0 GameGuild authentication module"])

    PG[("PostgreSQL auth database")]
    REDIS[("Redis cache")]

    USER -->|"credentials and tokens"| PROXY
    PROXY -->|"HTTPS requests"| AUTH
    AUTH -->|"verification emails, codes, magic links, alerts"| MAIL
    AUTH -->|"SMS OTP"| SMS
    AUTH -->|"push challenges"| PUSH
    AUTH -->|"authorization redirects"| IDP
    IDP -->|"codes and assertions"| AUTH
    AUTH -->|"certificate anchors"| CHAIN
    CHAIN -->|"confirmations"| AUTH
    AUTH -->|"password digest queries"| TIF
    AUTH -->|"credentials, sessions, MFA state"| PG
    AUTH -->|"revocation and guest state"| REDIS
    AUTH -->|"access and refresh tokens"| PROXY
```

Trust boundaries crossed at level 0: B1 internet to proxy, B2 proxy to API, B3 PostgreSQL, B4 Redis, B5 IdPs, B6 delivery providers, B7 blockchain, B8 threat intel (architecture document, section 3).

## 3. Level 1 — the twelve flow families of issues #1096-#1107

### F1 — Sign-Up flow (issue #1096)

Registration with email and password, email verification, guest upgrade. Entry `POST /v1/auth/sign-up` (`Auth/Controllers/AuthController.cs:33`); engine `Auth/Services/LocalAuthService.cs:594-723`; verification `Auth/Services/EmailVerificationService.cs`; guest upgrade `Auth/Guest/GuestSessionUpgradeIntegration.cs`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["User browser"]
        E2["Next.js web app"]
    end

    subgraph API["API trust zone"]
        P1(["1.1 Validate sign up request"])
        P2(["1.2 Create user and hash password"])
        P3(["1.3 Provision default tenant membership"])
        P4(["1.4 Issue tokens and create session"])
        P5(["1.5 Send email verification"])
        P6(["1.6 Upgrade guest session"])
    end

    subgraph EXTSVC["External services zone"]
        E3["Email provider"]
    end

    D1[("PostgreSQL users and sessions")]
    D2[("Redis guest session store")]
    D3[("Process memory verification tokens")]

    E1 -->|"email, password, username, guest token"| E2
    E2 -->|"sign up request"| P1
    P1 -->|"normalized input"| P2
    P2 -->|"user row with bcrypt hash"| D1
    P2 -->|"user id"| P3
    P3 -->|"membership"| D1
    P2 -->|"credentials issued"| P4
    P4 -->|"refresh token hash, session row"| D1
    P4 -->|"tokens"| E1
    P5 -->|"verification token"| D3
    P5 -->|"verification email"| E3
    E3 -->|"email to inbox"| E1
    E1 -->|"verification token"| P5
    P1 -->|"guest access token"| P6
    P6 -->|"upgrade snapshot"| D2
```

Notes: strength validation and duplicate handling at `Auth/Services/LocalAuthService.cs:605-628`; token store is process-local (gap #1151); CAPTCHA is enforced only in the web layer (gap #1150).

### F2 — MFA Enrollment flow (issue #1097)

TOTP, SMS, push enablement, QR provisioning, backup codes. Controller `Auth/Controllers/MfaController.cs`; coordinator `Auth/Services/MfaService.cs:30-64`; TOTP `Auth/Services/TotpMfaService.cs:27-70`; backup codes hashed at `Auth/Services/MfaService.cs:35-44`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["Authenticated user"]
        E2["Authenticator app"]
    end

    subgraph API["API trust zone"]
        P1(["2.1 Start enrollment"])
        P2(["2.2 Generate and encrypt TOTP secret"])
        P3(["2.3 Return QR provisioning URI"])
        P4(["2.4 Verify first code and enable"])
        P5(["2.5 Generate and hash backup codes"])
    end

    D1[("PostgreSQL MFA configuration")]
    D2[("PostgreSQL backup codes")]

    E1 -->|"start enrollment"| P1
    P1 -->|"user id"| P2
    P2 -->|"encrypted secret"| D1
    P2 -->|"secret and QR URI"| P3
    P3 -->|"QR URI"| E1
    E1 -->|"scans QR"| E2
    E2 -->|"TOTP code"| E1
    E1 -->|"code to confirm"| P4
    P4 -->|"enabled flag"| D1
    P4 -->|"request backup codes"| P5
    P5 -->|"hashed codes"| D2
    P5 -->|"plaintext once"| E1
```

Notes: secrets encrypted AES-256-GCM (`Auth/Services/EncryptionService.cs`); disable requires password re-verification (`Auth/Services/MfaService.cs:107-134`); email-method enrollment endpoint gap #1152; push-device registration without re-verification gap #1153.

### F3 — MFA Recovery flow (issue #1098)

Lost device, recovery codes usage, re-enrollment, account recovery via contacts. Controllers `Auth/Controllers/MfaRecoveryController.cs`, `Auth/Controllers/AccountRecoveryController.cs`; handlers `Auth/Handlers/MfaRecoveryCommandHandlers.cs`, `Auth/Handlers/AccountRecoveryRequestHandlers.cs`, `Auth/Handlers/CompleteAccountRecoveryResetCommandHandler.cs`; threshold `Auth/Services/AccountRecoveryThresholdEvaluator.cs`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["User with lost device"]
        E2["Trusted contacts"]
    end

    subgraph API["API trust zone"]
        P1(["3.1 Submit recovery request"])
        P2(["3.2 Verify identity evidence"])
        P3(["3.3 Threshold evaluation and approvals"])
        P4(["3.4 Issue single use reset token"])
        P5(["3.5 Reset MFA and password"])
        P6(["3.6 Notify owner"])
    end

    subgraph EXTSVC["External services zone"]
        E3["Email provider"]
    end

    D1[("PostgreSQL recovery requests and approvals")]
    D2[("PostgreSQL users and MFA configuration")]
    D3[("PostgreSQL sessions and refresh tokens")]

    E1 -->|"recovery request"| P1
    P1 -->|"request row"| D1
    P1 -->|"evidence"| P2
    P2 -->|"verified claims"| P3
    E2 -->|"approval"| P3
    P3 -->|"decision"| D1
    P3 -->|"unlocked"| P4
    P4 -->|"token hash and expiry"| D1
    P4 -->|"reset link"| E3
    E3 -->|"email"| E1
    E1 -->|"reset token and new password"| P5
    P5 -->|"clear MFA, new password hash"| D2
    P5 -->|"revoke sessions and tokens"| D3
    P5 -->|"notification"| P6
    P6 -->|"alert"| E3
```

Notes: reset tokens hashed with fixed-time compare (`Auth/Handlers/CompleteAccountRecoveryResetCommandHandler.cs:46-56`); recovery for unverified email is a takeover vector (gap #1124); recovery emails bypass the durable queue (gap #1155).

### F4 — WebAuthn FIDO2 flow (issue #1099)

Biometric and security-key registration and authentication. Controller `Auth/Controllers/WebAuthnController.cs`; registration `Auth/Services/WebAuthnRegistrationService.cs`; authentication `Auth/Services/WebAuthnAuthenticationSubService.cs`; ceremony sessions `Auth/Services/DistributedCacheWebAuthnSessionStore.cs`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["User browser"]
        E2["Authenticator device"]
    end

    subgraph API["API trust zone"]
        P1(["4.1 Begin registration ceremony"])
        P2(["4.2 Store ceremony challenge"])
        P3(["4.3 Verify attestation and persist credential"])
        P4(["4.4 Begin authentication ceremony"])
        P5(["4.5 Verify assertion and sign in"])
    end

    D1[("Redis WebAuthn ceremony store")]
    D2[("PostgreSQL WebAuthn credentials")]

    E1 -->|"register request"| P1
    P1 -->|"challenge"| P2
    P2 -->|"ceremony state"| D1
    P2 -->|"challenge"| E1
    E1 -->|"credential creation"| E2
    E2 -->|"attestation response"| P3
    P3 -->|"credential row"| D2
    P3 -->|"status"| E1
    E1 -->|"sign in request"| P4
    P4 -->|"assertion challenge"| D1
    P4 -->|"challenge"| E1
    E2 -->|"assertion signature"| P5
    P5 -->|"verified identity"| D2
    P5 -->|"tokens"| E1
```

Notes: attestation conveyance `None` and persisted `UserVerified = true` (gap #1143, `Auth/Services/WebAuthnRegistrationService.cs:64`, `155`).

### F5 — Session Management flow (issue #1100)

List, terminate, rename, terminate-others, per-session security analysis. Controller `Auth/Controllers/SessionController.cs`; engine `Auth/Services/SessionManagementService.cs` (create 41, refresh 155, terminate 277, terminate-others 312, analysis 629).

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["Authenticated user"]
    end

    subgraph API["API trust zone"]
        P1(["5.1 List sessions with analysis"])
        P2(["5.2 Rename session"])
        P3(["5.3 Terminate one session"])
        P4(["5.4 Terminate other sessions"])
        P5(["5.5 Cleanup expired sessions"])
    end

    D1[("PostgreSQL sessions")]
    D2[("PostgreSQL refresh tokens")]
    D3[("PostgreSQL authentication attempts")]

    E1 -->|"list request"| P1
    P1 -->|"session rows"| D1
    P1 -->|"forensic history"| D3
    P1 -->|"session inventory with risk notes"| E1
    E1 -->|"new name"| P2
    P2 -->|"updated row"| D1
    E1 -->|"terminate session"| P3
    P3 -->|"termination reason"| D1
    P3 -->|"revoked tokens"| D2
    E1 -->|"terminate others"| P4
    P4 -->|"bulk termination"| D1
    P4 -->|"bulk token revocation"| D2
    P5 -->|"purge expired"| D1
```

Notes: terminate-others notification gap #1091; concurrent session limit gap #1086; tenant deletion orphaning gap #1120.

### F6 — Sign-Out flow (issue #1101)

Single-session and all-sessions sign-out, token cleanup. Revoke endpoint `POST /v1/auth/tokens:revoke` (`Auth/Controllers/AuthController.cs:708`); revocation `Auth/Services/LocalAuthService.cs:1073`, `Auth/Services/JwtTokenService.cs:408-454`; handler `Auth/Handlers/LogoutHandler.cs` (dead code, gap #1154).

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["User browser"]
    end

    subgraph API["API trust zone"]
        P1(["6.1 Revoke refresh token"])
        P2(["6.2 Terminate session row"])
        P3(["6.3 Increment token version for all sessions"])
        P4(["6.4 Drop client credentials"])
    end

    D1[("PostgreSQL refresh tokens")]
    D2[("PostgreSQL sessions and users")]
    D3[("Redis jti revocation cache")]

    E1 -->|"refresh token"| P1
    P1 -->|"hash match and revoke"| D1
    P1 -->|"session teardown"| P2
    P2 -->|"terminated row"| D2
    E1 -->|"sign out all"| P3
    P3 -->|"token version bump"| D2
    P3 -->|"deny list entries"| D3
    E1 -->|"discard tokens"| P4
```

Notes: no dedicated sign-out endpoint on the API today (gap #1154); access-token invalidation for other sessions relies on the token-version middleware (`Auth/Middleware/TokenRevocationMiddleware.cs:91-103`).

### F7 — API Key flow (issue #1102)

Creation, listing, revocation, scope management, usage tracking. Controller `Auth/Controllers/ApiKeyController.cs`; handler `Auth/Services/ApiKeyAuthenticationHandler.cs`; entity `Auth/Entities/ApiKey.cs`; lifecycle `Auth/Configuration/ApiKeyLifecycleOptions.cs`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["Owner user"]
        E2["Machine client"]
    end

    subgraph API["API trust zone"]
        P1(["7.1 Create scoped key"])
        P2(["7.2 Hash and store key"])
        P3(["7.3 Authenticate request by key"])
        P4(["7.4 Track usage and audit"])
        P5(["7.5 Revoke or rotate key"])
    end

    D1[("PostgreSQL API keys")]
    D2[("PostgreSQL API key audit events")]

    E1 -->|"scopes and expiry"| P1
    P1 -->|"key material"| P2
    P2 -->|"hashed key row"| D1
    P2 -->|"plaintext key once"| E1
    E2 -->|"API key header"| P3
    P3 -->|"hash lookup"| D1
    P3 -->|"principal"| P4
    P4 -->|"usage and audit rows"| D2
    E1 -->|"revoke"| P5
    P5 -->|"revoked row"| D1
```

Notes: mid-request expiry semantics gap #1122; audit sink abstraction `Auth/Abstractions/IApiKeyAuditEventSink.cs`.

### F8 — Key Rotation flow (issue #1103)

Manual and scheduled JWT signing-key rotation. Controller `Auth/Controllers/KeyRotationController.cs`; engine `Auth/Services/KeyRotationService.cs` (catalog at line 23, rotation at line 40); background cleanup `Auth/Services/KeyRotationBackgroundService.cs`; entity `Auth/Entities/JwtSigningKey.cs`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["Operator or admin"]
    end

    subgraph API["API trust zone"]
        P1(["8.1 Request rotation with reason"])
        P2(["8.2 Create new signing key row"])
        P3(["8.3 Publish validation key set"])
        P4(["8.4 Retire and purge expired keys"])
        P5(["8.5 Issue and validate tokens"])
    end

    D1[("PostgreSQL JWT signing keys")]

    E1 -->|"rotate now"| P1
    P1 -->|"reason"| P2
    P2 -->|"new key row"| D1
    P2 -->|"key catalog"| P3
    P3 -->|"validation keys"| P5
    P4 -->|"purge expired"| D1
    P5 -->|"single static HS256 key"| D1
```

Notes: the issuance and validation paths currently use the single static symmetric key and do not consume the rotation catalog (`Auth/Services/JwtTokenService.cs:193-196`; `Host/Core/Extensions/SecurityServiceCollectionExtensions.cs:84-114`) — wiring gap #1141, mass-disconnect concern #1123. Diagram shows the catalog path as designed; the wired path today is P5 with the static key only.

### F9 — Service Account flow (issue #1104)

Machine-to-machine token issuance and scope management. Controllers `Auth/Controllers/ServiceAccountCrudController.cs`, `Auth/Controllers/ServiceAccountTokenController.cs`, `Auth/Controllers/ServiceAccountOperationsController.cs`; engine `Auth/Services/ServiceAccountService.cs`; token issuance `Auth/Services/JwtTokenService.cs:459-537`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["Admin operator"]
        E2["Backend service"]
    end

    subgraph API["API trust zone"]
        P1(["9.1 Create service account with scopes"])
        P2(["9.2 Issue client credentials token"])
        P3(["9.3 Authenticate service request"])
        P4(["9.4 Update account or scopes"])
    end

    D1[("PostgreSQL service accounts")]

    E1 -->|"name, scopes, expiry"| P1
    P1 -->|"account row"| D1
    E2 -->|"client credentials grant"| P2
    P2 -->|"actor kind Service token"| E2
    E2 -->|"bearer token"| P3
    P3 -->|"actor context"| D1
    E1 -->|"patch account"| P4
    P4 -->|"updated row"| D1
```

Notes: PATCH persistence bug gap #1142; service tokens are distinguishable from user tokens via `actor_kind` claim (`Auth/Services/JwtTokenService.cs:490-492`) and immutable actor construction (`apps/api/Source/Modules/GameGuild.Identity.Context/Actors/ActorContextBuilder.cs`).

### F10 — Step-Up Authentication flow (issue #1105)

High-risk challenge with timeout, retry and cancellation. Controller `Auth/Controllers/StepUpController.cs`; contracts `Auth/StepUp/StepUpContracts.cs:52-73`; trigger `Auth/Services/LocalAuthService.cs:183-199`; challenge reuse in sign-in MFA (`Auth/SignInMfa/SignInMfaService.cs`).

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["User browser"]
        E2["Authenticator app or email"]
    end

    subgraph API["API trust zone"]
        P1(["10.1 Risk engine flags high risk"])
        P2(["10.2 Create step up challenge"])
        P3(["10.3 Deliver secondary factor"])
        P4(["10.4 Verify factor within TTL"])
        P5(["10.5 Issue receipt or deny"])
        P6(["10.6 Re-verify on sensitive action"])
    end

    D1[("PostgreSQL step up challenges and MFA evidence")]
    D2[("PostgreSQL adaptive baselines")]

    P1 -->|"risk level"| P2
    P1 -->|"baseline learning"| D2
    P2 -->|"challenge with expiry"| D1
    P2 -->|"challenge"| E1
    E1 -->|"factor delivery"| E2
    E2 -->|"code"| E1
    E1 -->|"proof"| P4
    P4 -->|"consumed challenge"| D1
    P4 -->|"outcome"| P5
    P5 -->|"receipt with expiry"| E1
    E1 -->|"receipt on sensitive action"| P6
    P6 -->|"verify receipt"| D1
```

Notes: risk thresholds from `Auth/Services/AdaptiveAnomalyDetectionService.cs:122-140`; heuristic layer `Auth/Services/AuthenticationAnomalyDetectionService.cs`; timezone false-positive gap #1115.

### F11 — Trusted Devices flow (issue #1106)

Device registration, trust decision, expiry, management. `Auth/Services/SessionManagementService.cs:366-530` (trust 366, check 432, list 454, revoke 506, rename 530); entity `Auth/Entities/TrustedDevice.cs`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["Authenticated user"]
    end

    subgraph API["API trust zone"]
        P1(["11.1 Register device fingerprint"])
        P2(["11.2 Evaluate trust on sign in"])
        P3(["11.3 Extend or expire trust"])
        P4(["11.4 List and revoke devices"])
    end

    D1[("PostgreSQL trusted devices")]
    D2[("PostgreSQL authentication attempts")]

    E1 -->|"fingerprint and name"| P1
    P1 -->|"device row"| D1
    P2 -->|"trust check"| D1
    P2 -->|"history"| D2
    P2 -->|"trust decision"| E1
    P3 -->|"expiry updates"| D1
    E1 -->|"manage devices"| P4
    P4 -->|"revocation"| D1
```

Notes: fresh-verification prerequisite and expired-row pruning gap #1144.

### F12 — Trusted Contacts flow (issue #1107)

Contact addition, verification, recovery via contacts. Controller `Auth/Controllers/TrustedContactsController.cs`; entity `Auth/Entities/TrustedContact.cs`; recovery interaction with F3.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["Account owner"]
        E2["Trusted contact"]
    end

    subgraph API["API trust zone"]
        P1(["12.1 Add trusted contact"])
        P2(["12.2 Send verification email"])
        P3(["12.3 Confirm contact"])
        P4(["12.4 Recovery approval vote"])
    end

    subgraph EXTSVC["External services zone"]
        E3["Email provider"]
    end

    D1[("PostgreSQL trusted contacts")]
    D2[("PostgreSQL recovery approvals")]

    E1 -->|"contact email"| P1
    P1 -->|"contact row"| D1
    P2 -->|"verification email"| E3
    E3 -->|"email"| E2
    E2 -->|"confirmation"| P3
    P3 -->|"verified flag"| D1
    E2 -->|"approval for recovery"| P4
    P4 -->|"approval rows"| D2
    P2 -->|"failure signal"| E1
```

Notes: email delivery failures abort the command without an outbox (gap #1145).

## 4. Credential flow DFDs (STRIDE scope cross-reference)

These families are the STRIDE document's flow list (its sections 2, 5-10); DFDs are included here because they carry the primary credential flows.

### C1 — Password and federated sign-in (STRIDE flows 1, 2, 4)

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["User browser"]
    end

    subgraph API["API trust zone"]
        P1(["C1.1 Rate limit and validate"])
        P2(["C1.2 Resolve account"])
        P3(["C1.3 Verify password with timing floor"])
        P4(["C1.4 Risk and anomaly analysis"])
        P5(["C1.5 MFA preparation gate"])
        P6(["C1.6 Federated callback validation"])
        P7(["C1.7 Issue tokens and create session"])
    end

    subgraph EXTSVC["External services zone"]
        E2["Identity providers"]
        E3["Threat intel feed"]
    end

    D1[("PostgreSQL users")]
    D2[("PostgreSQL authentication attempts")]
    D3[("PostgreSQL adaptive baselines")]
    D4[("PostgreSQL external logins")]
    D5[("PostgreSQL sessions and refresh tokens")]

    E1 -->|"credentials"| P1
    P1 -->|"throttled request"| P2
    P2 -->|"account or none"| D1
    P3 -->|"verify"| D1
    P3 -->|"password digest"| E3
    P4 -->|"attempt forensics"| D2
    P4 -->|"baseline update"| D3
    E1 -->|"provider callback code and state"| P6
    E2 -->|"code exchange and id token"| P6
    P6 -->|"external login row"| D4
    P3 -->|"verified identity"| P5
    P6 -->|"verified identity"| P5
    P5 -->|"outcome"| P7
    P7 -->|"session and token rows"| D5
    P7 -->|"tokens"| E1
```

### C2 — Passwordless entry (magic link, email code, Web3; STRIDE flows 5-7)

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["User browser"]
        E2["Crypto wallet"]
    end

    subgraph API["API trust zone"]
        P1(["C2.1 Magic link issue and consume"])
        P2(["C2.2 Email code issue and consume"])
        P3(["C2.3 SIWE challenge issue"])
        P4(["C2.4 Signature verification"])
        P5(["C2.5 Shared issuance path"])
    end

    subgraph EXTSVC["External services zone"]
        E3["Email provider"]
    end

    D1[("PostgreSQL magic link tokens")]
    D2[("Process memory email codes")]
    D3[("Process memory SIWE challenges")]
    D4[("PostgreSQL users and external logins")]

    E1 -->|"request link"| P1
    P1 -->|"hashed single use token"| D1
    P1 -->|"link"| E3
    E3 -->|"link"| E1
    E1 -->|"token"| P1
    E1 -->|"request code"| P2
    P2 -->|"hashed code"| D2
    P2 -->|"code"| E3
    E1 -->|"code"| P2
    E1 -->|"wallet address"| P3
    P3 -->|"nonce challenge"| D3
    P3 -->|"challenge"| E1
    E1 -->|"SIWE message"| E2
    E2 -->|"signature"| P4
    P4 -->|"verify nonce binding"| D3
    P4 -->|"wallet identity"| D4
    P1 -->|"identity"| P5
    P2 -->|"identity"| P5
    P4 -->|"identity"| P5
    P5 -->|"tokens"| E1
```

### C3 — Password reset and token refresh (STRIDE flows 8, 9)

```mermaid
flowchart TB
    subgraph CLIENT["Client zone"]
        E1["User browser"]
    end

    subgraph API["API trust zone"]
        P1(["C3.1 Reset request with generic ack"])
        P2(["C3.2 Token issue and verification"])
        P3(["C3.3 Reset password with history check"])
        P4(["C3.4 Revoke all sessions and bump version"])
        P5(["C3.5 Refresh rotation with replay containment"])
    end

    subgraph EXTSVC["External services zone"]
        E2["Email provider"]
    end

    D1[("Process memory reset tokens")]
    D2[("PostgreSQL users")]
    D3[("PostgreSQL refresh token lineage")]
    D4[("PostgreSQL sessions")]
    D5[("Redis jti revocation cache")]

    E1 -->|"email"| P1
    P1 -->|"token"| D1
    P1 -->|"reset email"| E2
    E2 -->|"link"| E1
    E1 -->|"token and new password"| P2
    P2 -->|"verify single use"| D1
    P2 -->|"new credential"| P3
    P3 -->|"password hash"| D2
    P3 -->|"revocation"| P4
    P4 -->|"token version bump"| D2
    P4 -->|"terminate"| D4
    P4 -->|"deny list"| D5
    E1 -->|"refresh token"| P5
    P5 -->|"rotation claim"| D3
    P5 -->|"session renewal"| D4
    P5 -->|"replay containment"| D3
    P5 -->|"new token pair"| E1
```

## 5. Coverage matrix

| Authentication flow | DFD section | Flow issue | STRIDE section |
| --- | --- | --- | --- |
| Sign-up | F1 | #1096 | 3 |
| MFA enrollment | F2 | #1097 | 4 |
| MFA recovery | F3 | #1098 | 4 |
| WebAuthn FIDO2 | F4 | #1099 | 4 |
| Session management | F5 | #1100 | 11 |
| Sign-out | F6 | #1101 | 11 |
| API key | F7 | #1102 | 12 cross-cutting |
| Key rotation | F8 | #1103 | 12 cross-cutting |
| Service account | F9 | #1104 | 12 cross-cutting |
| Step-up authentication | F10 | #1105 | 2, 4 |
| Trusted devices | F11 | #1106 | 11 |
| Trusted contacts | F12 | #1107 | 3 of STRIDE doc context |
| Password and federated sign-in | C1 | none yet | 2, 4 |
| Passwordless entry | C2 | none yet | 5, 6, 7 |
| Password reset and token refresh | C3 | none yet | 8, 9 |

Flow families #1108-#1114 (batch 3: password change, external login link/unlink, permission evaluation, orchestrated auth, guest lifecycle, token lifecycle, account provisioning) are open issues; their DFDs should extend this document when those issues are picked up. Permission evaluation itself is documented in [PERMISSION_EVALUATION_POLICY.md](PERMISSION_EVALUATION_POLICY.md) and the authorization audit trail in [AUTHORIZATION_PRECEDENCE_AUDIT.md](AUTHORIZATION_PRECEDENCE_AUDIT.md).

## 6. Maintenance rules

1. When a flow's code path moves (controller, handler, service split or merge), update the affected diagram and its citations in the same PR.
2. When a gap issue closes (for example #1141 wiring the rotation catalog), re-draw the affected diagram to show the wired path rather than the designed path, and drop the gap note.
3. New flows get a new family letter and an entry in the coverage matrix; do not silently redraw an existing family to cover two families.
4. Keep Mermaid labels free of parentheses, colons and slashes — the renderer used by the docs site rejects them.
