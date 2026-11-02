# Password Change Flow — User Flow Diagram

> Issue #1109 · Module `GameGuild.Identity.Authentication` (+ `GameGuild.Identity.Users`, `GameGuild.SharedKernel`) · Documented against `develop` commit `a96679c7a`.
> Scope: authenticated password change (current-password verification, strength + history policy, optimistic-concurrency write, token-version bump), the all-session revocation battery, the related token-based password reset, and the password-history mechanism.

## 1. Overview

An authenticated user changes their password via `POST /v{version}/auth/password:change` (`Controllers/AuthController.cs:938-975`). The controller resolves the user id from claims (and the `session_id` claim for keep-current-session semantics), then sends `ChangePasswordCommand`. `ChangePasswordCommandHandler` (`Handlers/PasswordCommandHandlers.cs:183-285`) verifies the current password (or allows set-initial mode for OAuth-only accounts), enforces strength and the last-5-passwords history, and writes the new hash through `UserRepository.UpdatePasswordHashAsync` (`Repositories/UserRepository.cs:334-346`), which compares the observed hash before applying `User.SetPasswordHash` — the write that records history and bumps `TokenVersion` (`Entities/User.cs:206-223`). With the default `SessionOptions.TerminateSessionsOnPasswordChange = true` (`SharedKernel/Configuration/ApplicationLayer/SessionOptions.cs:64`) the full revocation battery then runs through `PasswordChangeRevocationService` (`Services/PasswordChangeRevocationService.cs:16-55`): revoke all refresh tokens, terminate all sessions (audited), and record a versioned token cutoff that rejects pre-bump access tokens.

## 2. End-to-End Flowchart — happy path and every edge case

```mermaid
flowchart TD
    A1["Client POST v1 auth password change<br/>with current and new password<br/>AuthController.cs L938"] --> A2{"UserId claim parse<br/>NameIdentifier or sub or user_id<br/>AuthController.cs L949"}
    A2 -- "missing or invalid" --> A401["401 Invalid user context<br/>AuthController.cs L954"]
    A2 -- "ok" --> A3["ChangePasswordCommand with<br/>UserId CurrentPassword NewPassword<br/>RevokeOtherSessions and CurrentSessionId from sid claim<br/>AuthController.cs L957"]
    A3 --> H1["ChangePasswordCommandHandler Handle<br/>PasswordCommandHandlers.cs L191"]

    H1 --> E1{"NewPassword equals ConfirmPassword<br/>PasswordCommandHandlers.cs L193"}
    E1 -- "no" --> E400a["400 Password confirmation does not match"]
    E1 -- "yes" --> E2["UserRepository GetByIdAsync<br/>PasswordCommandHandlers.cs L198"]
    E2 -- "user not found" --> E400b["400 User not found<br/>PasswordCommandHandlers.cs L201"]
    E2 -- "user found" --> E3{"Account has PasswordHash<br/>PasswordCommandHandlers.cs L204"}
    E3 -- "yes - normal account" --> E4{"VerifyPassword current<br/>PasswordCommandHandlers.cs L213"}
    E4 -- "wrong current password" --> E400c["400 Current password is incorrect<br/>PasswordCommandHandlers.cs L215"]
    E4 -- "correct" --> E5
    E3 -- "no - OAuth-only account" --> E4b{"CurrentPassword supplied<br/>PasswordCommandHandlers.cs L208"}
    E4b -- "any value supplied" --> E400d["400 Current password is incorrect<br/>set-initial mode requires empty current<br/>PasswordCommandHandlers.cs L210"]
    E4b -- "empty - set initial password" --> E5["ValidatePasswordStrength<br/>PasswordCommandHandlers.cs L218"]
    E5 -- "weak password" --> E400e["400 joined validation failures<br/>PasswordCommandHandlers.cs L221"]
    E5 -- "strong" --> E6{"PasswordHistoryPolicy WasRecentlyUsed<br/>current plus last 5 hashes<br/>PasswordHistoryPolicy.cs L10"}
    E6 -- "reuse detected" --> E400f["400 You cannot reuse any of your last 5 passwords<br/>PasswordCommandHandlers.cs L230"]
    E6 -- "not reused" --> E7["HashPassword new<br/>PasswordCommandHandlers.cs L233"]
    E7 --> E8["UpdatePasswordHashAsync with expected current hash<br/>PasswordCommandHandlers.cs L234"]
    E8 --> E9{"UserRepository compares stored hash<br/>UserRepository.cs L337"}
    E9 -- "hash changed concurrently" --> E400g["400 Your password changed during this request<br/>PasswordCommandHandlers.cs L237"]
    E9 -- "matches" --> E10["User SetPasswordHash<br/>history recorded and TokenVersion incremented<br/>Entities User.cs L206"]
    E10 --> R0{"SessionOptions TerminateSessionsOnPasswordChange<br/>default true - SessionOptions.cs L64"}

    R0 -- "true - default" --> R1["PasswordChangeRevocationService RevokeAllSessionsAndTokensAsync<br/>reason Password changed<br/>PasswordCommandHandlers.cs L247"]
    R1 --> R2["RefreshTokenRepository RevokeAllForUserAsync<br/>PasswordChangeRevocationService.cs L30"]
    R1 --> R3["SessionManagementService TerminateAllUserSessionsAsync<br/>reason PasswordChanged - audited per session<br/>PasswordChangeRevocationService.cs L32"]
    R1 --> R4["LifecycleRecorder AllRevoked event<br/>PasswordChangeRevocationService.cs L35"]
    R1 --> R5{"GetTokenVersionAsync<br/>missing user resolves to 0 - fail closed<br/>PasswordChangeRevocationService.cs L44"}
    R5 --> R6["VersionedUserTokenRevocationService RevokeAllUserTokensAsync<br/>cutoff at current version<br/>PasswordChangeRevocationService.cs L46"]
    R2 --> ROK["200 Password changed - SessionsRevoked count<br/>PasswordCommandHandlers.cs L278"]
    R3 --> ROK
    R4 --> ROK
    R6 --> ROK

    R0 -- "false - admin opt-out" --> R1b{"RevokeOtherSessions requested<br/>and CurrentSessionId present<br/>PasswordCommandHandlers.cs L261"}
    R1b -- "yes" --> R2b["TerminateAllExceptAsync keep session<br/>reason password_changed<br/>PasswordCommandHandlers.cs L265"]
    R1b -- "no session id or not requested" --> R3b["Skip with log - sessions stay active<br/>PasswordCommandHandlers.cs L271"]
    R2b --> ROK
    R3b --> ROK

    ROK --> MW["Next request with old access token<br/>TokenRevocationMiddleware.cs L91"]
    MW --> MW1{"token_version below current<br/>TokenRevocationMiddleware.cs L100"}
    MW1 -- "yes" --> MW401["401 Invalid access token<br/>TokenRevocationMiddleware.cs L105"]
    MW1 -- "no - freshly issued token" --> MW2["Request proceeds"]

    subgraph RESET["Related - token-based password reset"]
        RS1["Client POST v1 auth password reset<br/>AuthController.cs L905"] --> RS2["ResetPasswordCommandHandler<br/>PasswordCommandHandlers.cs L105"]
        RS2 --> RS3{"VerifyPasswordResetTokenAsync<br/>EmailVerificationService<br/>PasswordCommandHandlers.cs L122"}
        RS3 -- "invalid or expired token" --> RS400["400 Invalid or expired reset token"]
        RS3 -- "valid" --> RS4["same strength and history checks<br/>PasswordCommandHandlers.cs L112 and L142"]
        RS4 --> RS5["UpdatePasswordHashAsync then<br/>revocation battery when TerminateSessionsOnPasswordChange<br/>PasswordCommandHandlers.cs L148 and L154"]
        RS5 --> RS200["200 Password reset successfully"]
    end
```

## 3. CQRS Chain Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    participant C as Client settings page
    participant API as AuthController
    participant S as ISender
    participant H as ChangePasswordCommandHandler
    participant U as UserRepository
    participant E as User entity
    participant P as PasswordChangeRevocationService
    participant M as TokenRevocationMiddleware

    C->>API: POST v1 auth password change
    API->>API: parse sub claim and session_id claim - AuthController.cs L949
    API->>S: ChangePasswordCommand
    S->>H: Handle - PasswordCommandHandlers.cs L191
    H->>U: GetByIdAsync userId
    U-->>H: User with PasswordHash and TokenVersion
    H->>H: VerifyPassword current - L213
    H->>H: ValidatePasswordStrength - L218
    H->>H: PasswordHistoryPolicy check - L228
    H->>U: UpdatePasswordHashAsync with expected current hash - L234
    U->>E: SetPasswordHash - User.cs L206
    E->>E: push old hash onto history of 5 - User.cs L210
    E->>E: IncrementTokenVersion - User.cs L221
    U-->>H: true
    H->>P: RevokeAllSessionsAndTokensAsync - L247
    P->>P: RevokeAllForUserAsync refresh tokens - L30
    P->>P: TerminateAllUserSessionsAsync audited - L32
    P->>P: versioned token cutoff at new version - L46
    P-->>H: terminated session count
    H-->>S: PasswordChangeResult success
    S-->>API: result
    API-->>C: 200 with SessionsRevoked
    C->>M: next request with pre-change access token
    M->>M: token_version below current - TokenRevocationMiddleware.cs L100
    M-->>C: 401 Invalid access token
```

## 4. Data Flow Diagram

```mermaid
flowchart TD
    DF1["Browser account settings"] -->|"current and new password over TLS"| DF2["AuthController password change"]
    DF2 -->|"ChangePasswordCommand"| DF3["ChangePasswordCommandHandler"]
    DF3 -->|"read and compare-and-swap write"| DF4["users table - PasswordHash<br/>PasswordHistoryHashes TokenVersion"]
    DF3 -->|"BCrypt verify and hash"| DF5["IPasswordHasher"]
    DF3 -->|"revoke all"| DF6["refreshtokens table"]
    DF3 -->|"terminate all - audit events"| DF7["usersessions table"]
    DF3 -->|"versioned cutoff row"| DF8["user token revocation store"]
    DF6 --> DF9["401 for every old refresh token"]
    DF7 --> DF9
    DF8 -->|"rejects legacy access tokens"| DF10["TokenRevocationMiddleware"]
    DF10 -->|"401 Invalid access token"| DF1
```

## 5. Edge-Case Matrix

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Confirmation mismatch | `PasswordCommandHandlers.cs:193-196` | 400, message from handler |
| 2 | User record vanished mid-request | `PasswordCommandHandlers.cs:199-202` | 400 "User not found" |
| 3 | Wrong current password | `PasswordCommandHandlers.cs:213-216` | 400 "Current password is incorrect" |
| 4 | OAuth-only account (no `PasswordHash`) supplies any current password | set-initial mode guard (`PasswordCommandHandlers.cs:204-212`) | 400 — supplied password can never match; empty current password proceeds to set initial |
| 5 | Weak new password | `IPasswordHasher.ValidatePasswordStrength` (`PasswordCommandHandlers.cs:218-226`) | 400 with joined failure list |
| 6 | New password equals current or one of last 5 | `PasswordHistoryPolicy.WasRecentlyUsed` (`Services/PasswordHistoryPolicy.cs:10-25`) checks current hash + `GetPasswordHistoryHashes()` with legacy-hash fallback | 400 "cannot reuse any of your last 5 passwords" |
| 7 | Legacy long BCrypt hashes in history | `PasswordHasher.MatchesLongLegacyHashForHistory` (`Services/PasswordHistoryPolicy.cs:15-16`) | still detected, no false negative |
| 8 | Two concurrent change requests | `UpdatePasswordHashAsync` compares observed hash (`Repositories/UserRepository.cs:337-340`) | loser gets 400 "Your password changed during this request", no double history push |
| 9 | Password reset races a change | same compare-and-swap in `ResetPasswordCommandHandler` (`PasswordCommandHandlers.cs:148-152`) | 400 with retry guidance |
| 10 | All-session revocation | default `TerminateSessionsOnPasswordChange = true` (`SharedKernel/.../SessionOptions.cs:64`) + `PasswordChangeRevocationService` (`Services/PasswordChangeRevocationService.cs:24-54`) | refresh tokens revoked, sessions terminated with audit, versioned cutoff recorded |
| 11 | Versioned cutoff store unavailable | store failure propagates (`PasswordChangeRevocationService.cs:45-47` comment) | handler throws — revocation never reported complete |
| 12 | User row disappears between write and revocation | `GetTokenVersionAsync` returns null → resolves to 0 (`PasswordChangeRevocationService.cs:44`) | fails closed, revokes everything |
| 13 | `TerminateSessionsOnPasswordChange` set to false + `RevokeOtherSessions` + valid `session_id` claim | keep-current branch (`PasswordCommandHandlers.cs:261-270`) | all sessions except current terminated (`TerminateAllExceptAsync`) |
| 14 | `RevokeOtherSessions` requested but no session repository / no session id | skip branch (`PasswordCommandHandlers.cs:271-274`) | sessions stay active, logged |
| 15 | Pre-change access tokens after revocation | `TokenRevocationMiddleware` version check (`Middleware/TokenRevocationMiddleware.cs:91-109`) | 401 Invalid access token |
| 16 | Anonymous call to change endpoint | `[Authorize]` (`Controllers/AuthController.cs:937`) | 401 before handler |
| 17 | Expired reset token on the reset path | `VerifyPasswordResetTokenAsync` (`PasswordCommandHandlers.cs:122-130`) | 400 Invalid or expired reset token |

## 6. Endpoint Inventory

| Method + Route | Handler chain | Controller ref |
|----------------|---------------|----------------|
| POST `v{v}/auth/password:change` | `ChangePasswordCommand` → `ChangePasswordCommandHandler` | `Controllers/AuthController.cs:937-975` |
| POST `v{v}/auth/password:reset` | `ResetPasswordCommand` → `ResetPasswordCommandHandler` (related completion path) | `Controllers/AuthController.cs:904-929` |
| POST `v{v}/auth/password:reset-request` (forgot password) | `RequestPasswordResetCommand` → `RequestPasswordResetCommandHandler` (`Handlers/PasswordCommandHandlers.cs:55-94`) | `Controllers/AuthController.cs:878` |

## 7. Events Published / Consumed

- **Published**
  - `PasswordResetRequestedNotification` on the forgot-password path only (`Handlers/PasswordCommandHandlers.cs:68-75`) → consumed by `SendPasswordResetRequestedHandler` (email pipeline).
  - Refresh-token lifecycle events `AllRevoked` (`Services/PasswordChangeRevocationService.cs:35-38`) via `IRefreshTokenLifecycleRecorder`, with `RefreshTokenLifecycleMetrics.RecordAttempt` (`:29`).
  - Session termination audit events per session via `ISessionManagementService.TerminateAllUserSessionsAsync` (`:32-34`).
  - Structured log "Password changed for user ... revoked sessions" (`Handlers/PasswordCommandHandlers.cs:276`).
- **Consumed:** none (no `PasswordChanged` domain event exists — see Gaps).

## 8. Entities and Tables Touched

| Entity / Table | Fields | Write ref |
|----------------|--------|-----------|
| `User` → users table | `PasswordHash`, `PasswordHistoryHashes` (max 2600, newline-joined 5 hashes, `Entities/UserConfiguration.cs:66`), `TokenVersion` | `Repositories/UserRepository.cs:334-346` via `Entities/User.cs:206-223` |
| `RefreshToken` → refreshtokens | `IsRevoked`, `RevokedAt`, `RevokedByIp` | `IRefreshTokenRepository.RevokeAllForUserAsync` (`Services/PasswordChangeRevocationService.cs:30`) |
| `UserSession` → usersessions | `IsActive`, `TerminatedAt`, `TerminationReason = PasswordChanged` | `ISessionManagementService.TerminateAllUserSessionsAsync` (`:32`) |
| Versioned user-token revocation store | per-user version cutoff | `IVersionedUserTokenRevocationService.RevokeAllUserTokensAsync` (`:46`) |
| Refresh-token lifecycle metrics/events | operation counters + audit rows | `IRefreshTokenLifecycleRecorder.RecordMutationAsync` (`:35`) |

## 9. Gaps — realistic scenarios the code does not handle

1. **No email notification on password change.** The issue names "email notification"; only the *reset request* sends email (`PasswordResetRequestedNotification`). A successful authenticated change (or reset) sends no confirmation email, so a silent attacker-driven change is invisible to the account owner. `PasswordChanged` exists only as a session-termination reason and revocation-service reason string, not as a notification.
2. **`RevokeOtherSessions` is dead in the default configuration.** The keep-current-session branch (`Handlers/PasswordCommandHandlers.cs:261-270`) is reachable only when `TerminateSessionsOnPasswordChange` is false; with the default true (`SessionOptions.cs:64`) the request flag is ignored and the caller's own session is also terminated — the client gets a 200 but its tokens are immediately invalid. There is no default path that keeps the current session.
3. **No step-up / re-authentication cadence beyond the current password.** A stolen access token can change the password (the password itself is the only second factor); no MFA proof or recent-auth (`auth_time`) requirement gates the endpoint.
4. **History truncation before the write can lose an entry under concurrency.** `User.SetPasswordHash` keeps old current + first 4 history (`Entities/User.cs:210-217`); concurrent writers are stopped by the compare-and-swap, but a successful change immediately followed by another valid change with a stale UI can still push out the 5th-oldest hash early — the "last 5" window is best-effort, not transactional across requests.
5. **Rate limiting / lockout not specific to wrong-current-password attempts.** The endpoint sits under the generic `Authentication` rate-limit policy; wrong current-password attempts are not fed into `IAuthAttemptService` (unlike sign-in), so brute-forcing the current password through this endpoint is bounded only by the shared limiter.
6. **`Password:forgot` response is intentionally identical for unknown emails** (`Handlers/PasswordCommandHandlers.cs:87-92`, good), but the reset path never invalidates *unconsumed* reset tokens after a successful change — a second, still-valid reset token issued before the change can be replayed to change the password again (its own history check would pass if the attacker picks a different password).
