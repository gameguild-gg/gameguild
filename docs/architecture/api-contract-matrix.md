# API contract matrix — OpenAPI surface × web client usage

Last audited: 2026-11-02 against `develop` (`74e9bba4c`).

Sources of truth:

- **Client surface**: `packages/infrastructure/client/src/generated/endpoints.gen.ts`
  (2,115 endpoint definitions; 224 module classes in `generated/modules/`), generated
  from the API OpenAPI document — `.metadata.json` records
  `apiVersion 4.4.0`, hash `4576ad34…c5177c6`, generated `2026-11-01T17:27:30Z`
  (commit `4db4ca6e2`).
- **Server surface**: controller route attributes across `apps/api/Source`
  (267 files, 2,283 route/action pairs incl. dual-route actions).
- **Usage**: full scan of `apps/web/src` (1,992 TS/TSX files) for generated-module
  method calls, plus hand-rolled `fetch` transports in `apps/web/src` and the client
  package's own runtime (`packages/infrastructure/client/src/runtime`,
  `…/integrations/next`).

## Headline numbers

| Metric | Count |
| --- | --- |
| Generated client endpoints | 2,115 |
| Controller route/actions parsed | 2,283 |
| Client endpoints matched to a controller route | 2,100 + 12 version-parameterized + 3 parser artifacts = **2,115 (100%)** |
| Called from apps/web via generated methods | 421 (19.9%) |
| Called only via hand-rolled `fetch` (raw transport) | 17 |
| Referenced only by tests | 8 |
| **Never called by any transport (ENDPOINT-NO-CALLER candidates)** | **1,669** |
| Controller endpoints with no client method (live HTTP, absent from contract) | 171 (95 hidden by design, 56 unversioned, 20 see F5/F6) |
| Raw-fetch URLs with no API route (CLIENT-CALL-NO-ENDPOINT) | 2 (F1, F2) |

### Finding classes

- **ENDPOINT-NO-CALLER** — endpoint exists in the generated client and on the API,
  but apps/web never invokes it by any transport. Candidate for deprecation review;
  not automatically dead (mobile/CLI/third-party consumers may exist outside this
  repo, and admin consoles are an explicitly excluded operational surface per
  ADR-20260821).
- **CLIENT-CALL-NO-ENDPOINT** — code in the web app or client runtime fetches a URL
  that no controller serves. A 404 (or silent fallback) waiting to happen.
- **VERSIONED-DIFF** — the generated client and the live controllers disagree:
  controller actions missing from the client, or endpoints excluded from the
  versioned OpenAPI document.

## Auth module — full matrix (139 endpoints, 34 called)

`apps/web/src` consumes auth through four transports: generated modules
(`lib/auth/*-actions.ts`), Next route handlers (`app/api/auth/**/route.ts`), the
client package's Next integration (server actions for sign-up/token revoke), and the
client runtime's `runtime/auth/extended-operations.ts` hand-fetches. The table below
marks the **first caller found** per endpoint; "raw fetch only" means the endpoint is
exercised, but not through its generated method (finding F3).

### Core credentials and providers (`auth.gen.ts` -> `AuthModule`) — 44 endpoints, 13 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| POST | `/v1/auth/discord:sign-in-authorize` | `postAuthDiscordSignInAuthorize` | — |
| POST | `/v1/auth/discord:sign-in-callback` | `postAuthDiscordSignInCallback` | — |
| POST | `/v1/auth/email-code:consume` | `postAuthEmailCodeConsume` | — |
| POST | `/v1/auth/email-code:request` | `postAuthEmailCodeRequest` | — |
| POST | `/v1/auth/email:send-verification` | `postAuthEmailSendVerification` | — |
| POST | `/v1/auth/email:verify` | `postAuthEmailVerify` | — |
| HEAD | `/v1/auth/external-logins` | `headAuthExternalLogins` | — |
| POST | `/v1/auth/external-logins/discord:link-authorize` | `postAuthExternalLoginsDiscordLinkAuthorize` | `app/api/auth/link/discord/route.ts` |
| POST | `/v1/auth/external-logins/discord:link-callback` | `postAuthExternalLoginsDiscordLinkCallback` | `app/api/auth/link/discord/callback/route.ts` |
| POST | `/v1/auth/external-logins/google` | `postAuthExternalLoginsGoogle` | `lib/auth/external-logins-actions.ts` |
| DELETE | `/v1/auth/external-logins/{provider}` | `deleteAuthExternalLogins` | `lib/auth/external-logins-actions.ts` |
| GET | `/v1/auth/external-logins/{provider}/link-preview` | `getAuthExternalLoginsLinkPreview` | — |
| POST | `/v1/auth/external-logins/{provider}/scopes:revoke` | `postAuthExternalLoginsScopesRevoke` | `lib/auth/external-logins-actions.ts` |
| GET | `/v1/auth/github:authorize` | `getAuthGithubAuthorize` | — |
| POST | `/v1/auth/github:callback` | `postAuthGithubCallback` | — |
| POST | `/v1/auth/google:sign-in` | `postAuthGoogleSignIn` | — |
| POST | `/v1/auth/magic-link:consume` | `postAuthMagicLinkConsume` | — |
| POST | `/v1/auth/magic-link:request` | `postAuthMagicLinkRequest` | — |
| POST | `/v1/auth/mfa/sign-in/complete` | `postAuthMfaSignInComplete` | — |
| GET | `/v1/auth/oidc/{slug}:end-session-url` | `getAuthOidcEndSessionUrl` | — |
| POST | `/v1/auth/oidc/{slug}:sign-in-authorize` | `postAuthOidcSignInAuthorize` | — |
| POST | `/v1/auth/oidc/{slug}:sign-in-callback` | `postAuthOidcSignInCallback` | — |
| GET | `/v1/auth/oidc:discover-provider` | `getAuthOidcDiscoverProvider` | — |
| GET | `/v1/auth/orchestrated/{flowId}` | `getAuthOrchestrated` | — |
| POST | `/v1/auth/orchestrated/{flowId}:abandon` | `postAuthOrchestratedAbandon` | — |
| POST | `/v1/auth/orchestrated/{flowId}:complete` | `postAuthOrchestratedComplete` | — |
| POST | `/v1/auth/orchestrated/{flowId}:process-step` | `postAuthOrchestratedProcessStep` | — |
| POST | `/v1/auth/orchestrated:initiate` | `postAuthOrchestratedInitiate` | — |
| POST | `/v1/auth/password:change` | `postAuthPasswordChange` | `lib/auth/password-change-action.ts` |
| POST | `/v1/auth/password:reset` | `postAuthPasswordReset` | raw fetch only (F3) |
| POST | `/v1/auth/password:reset-request` | `postAuthPasswordResetRequest` | raw fetch only (F3) |
| POST | `/v1/auth/polymorphic` | `postAuthPolymorphic` | — |
| POST | `/v1/auth/saml/{slug}:assertion-consumer` | `postAuthSamlAssertionConsumer` | `app/api/auth/saml/[slug]/callback/route.ts` |
| GET | `/v1/auth/saml/{slug}:logout-url` | `getAuthSamlLogoutUrl` | — |
| GET | `/v1/auth/saml/{slug}:metadata` | `getAuthSamlMetadata` | — |
| POST | `/v1/auth/saml/{slug}:sign-in-authorize` | `postAuthSamlSignInAuthorize` | `app/api/auth/saml/[slug]/signin/route.ts` |
| GET | `/v1/auth/session-state` | `getAuthSessionState` | `app/[locale]/(dashboards)/workspace/settings/account/page.tsx` |
| POST | `/v1/auth/sign-in` | `postAuthSignIn` | — (raw transport, F3/F4) |
| POST | `/v1/auth/sign-out` | `postAuthSignOut` | — (raw transport, F3/F4) |
| POST | `/v1/auth/sign-up` | `postAuthSignUp` | raw fetch only (F3) |
| POST | `/v1/auth/tokens:refresh` | `postAuthTokensRefresh` | — (raw transport, F3/F4) |
| POST | `/v1/auth/tokens:revoke` | `postAuthTokensRevoke` | raw fetch only (F3) |
| POST | `/v1/auth/web3/challenge` | `postAuthWeb3Challenge` | — (raw transport, F3/F4) |
| POST | `/v1/auth/web3:verify` | `postAuthWeb3Verify` | — (raw transport, F3/F4) |

### Account recovery (`auth-account-recovery.gen.ts`) — 6 endpoints, 0 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| POST | `/v1/auth/account-recovery/approve` | `postAuthAccountRecoveryApprove` | — |
| POST | `/v1/auth/account-recovery/approve-passkey:begin` | `postAuthAccountRecoveryApprovePasskeyBegin` | — |
| POST | `/v1/auth/account-recovery/approve-passkey:complete` | `postAuthAccountRecoveryApprovePasskeyComplete` | — |
| POST | `/v1/auth/account-recovery/cancel` | `postAuthAccountRecoveryCancel` | — |
| POST | `/v1/auth/account-recovery/request` | `postAuthAccountRecoveryRequest` | — |
| POST | `/v1/auth/account-recovery/reset` | `postAuthAccountRecoveryReset` | — |

### API keys (`auth-api-keys.gen.ts`) — 4 endpoints, 0 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/auth/api-keys` | `getAuthApiKeys` | — |
| POST | `/v1/auth/api-keys` | `postAuthApiKeys` | — |
| POST | `/v1/auth/api-keys/{keyId}:revoke` | `postAuthApiKeysRevoke` | — |
| POST | `/v1/auth/api-keys/{keyId}:rotate` | `postAuthApiKeysRotate` | — |

### Guest sessions (`auth-guest.gen.ts`) — 2 endpoints, 0 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/auth/guest-session` | `getAuthGuestSession` | — |
| POST | `/v1/auth/guest-session` | `postAuthGuestSession` | — |

### Multi-factor (`auth-multi-factor.gen.ts`) — 21 endpoints, 10 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| POST | `/v1/admin/users/{userId}/mfa:reset` | `postAdminUsersMfaReset` | — |
| GET | `/v1/auth/mfa` | `getAuthMfa` | `lib/auth/mfa-actions.ts`, `lib/user-settings/mfa-actions.ts` |
| GET | `/v1/auth/mfa/backup-codes` | `getAuthMfaBackupCodes` | `lib/auth/mfa-actions.ts`, `lib/user-settings/mfa-actions.ts` |
| POST | `/v1/auth/mfa/backup-codes:regenerate` | `postAuthMfaBackupCodesRegenerate` | `lib/auth/mfa-actions.ts`, `lib/user-settings/mfa-actions.ts` |
| POST | `/v1/auth/mfa/email:challenge` | `postAuthMfaEmailChallenge` | — |
| POST | `/v1/auth/mfa/email:complete` | `postAuthMfaEmailComplete` | — |
| POST | `/v1/auth/mfa/email:setup` | `postAuthMfaEmailSetup` | — |
| GET | `/v1/auth/mfa/methods` | `getAuthMfaMethods` | `lib/auth/mfa-actions.ts` |
| POST | `/v1/auth/mfa/push:challenge` | `postAuthMfaPushChallenge` | — |
| POST | `/v1/auth/mfa/push:register-device` | `postAuthMfaPushRegisterDevice` | — |
| POST | `/v1/auth/mfa/push:respond` | `postAuthMfaPushRespond` | — |
| POST | `/v1/auth/mfa/recovery:confirm` | `postAuthMfaRecoveryConfirm` | — |
| POST | `/v1/auth/mfa/recovery:continue` | `postAuthMfaRecoveryContinue` | — |
| POST | `/v1/auth/mfa/recovery:request` | `postAuthMfaRecoveryRequest` | — |
| POST | `/v1/auth/mfa/sms:challenge` | `postAuthMfaSmsChallenge` | — |
| POST | `/v1/auth/mfa/sms:complete` | `postAuthMfaSmsComplete` | `lib/auth/mfa-actions.ts` |
| POST | `/v1/auth/mfa/sms:setup` | `postAuthMfaSmsSetup` | `lib/auth/mfa-actions.ts` |
| POST | `/v1/auth/mfa/totp:complete` | `postAuthMfaTotpComplete` | `lib/auth/mfa-actions.ts` |
| POST | `/v1/auth/mfa/totp:setup` | `postAuthMfaTotpSetup` | `lib/auth/mfa-actions.ts`, `lib/user-settings/mfa-actions.ts` |
| POST | `/v1/auth/mfa/verify` | `postAuthMfaVerify` | raw fetch only (F3) |
| POST | `/v1/auth/mfa:disable` | `postAuthMfaDisable` | `lib/auth/mfa-actions.ts` |

### Roles (`auth-roles.gen.ts`) — 9 endpoints, 0 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/roles` | `getRolesForGetRoles` | — |
| POST | `/v1/roles` | `postRoles` | — |
| POST | `/v1/roles/:assign` | `postRolesAssign` | — |
| POST | `/v1/roles/:bulk-assign` | `postRolesBulkAssign` | — |
| POST | `/v1/roles/:remove` | `postRolesRemove` | — |
| GET | `/v1/roles/user/{userId}` | `getRolesUser` | — |
| DELETE | `/v1/roles/{roleId}` | `deleteRoles` | — |
| GET | `/v1/roles/{roleId}` | `getRolesForGetRolesByRoleId` | — |
| PUT | `/v1/roles/{roleId}` | `putRoles` | — |

### SCIM provisioning (`auth-scim-provisioning.gen.ts`) — 4 endpoints, 0 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/auth/scim-provisioning-tokens` | `getAuthScimProvisioningTokens` | — |
| POST | `/v1/auth/scim-provisioning-tokens` | `postAuthScimProvisioningTokens` | — |
| POST | `/v1/auth/scim-provisioning-tokens/{tokenId}:revoke` | `postAuthScimProvisioningTokensRevoke` | — |
| POST | `/v1/auth/scim-provisioning-tokens/{tokenId}:rotate` | `postAuthScimProvisioningTokensRotate` | — |

### Service accounts (`auth-service-accounts.gen.ts`, `auth-service-accounts-tokens.gen.ts`) — 15 endpoints, 0 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/auth/service-accounts` | `getAuthServiceAccountsForGetAuthServiceAccounts` | — |
| POST | `/v1/auth/service-accounts` | `postAuthServiceAccounts` | — |
| DELETE | `/v1/auth/service-accounts/{serviceAccountId}` | `deleteAuthServiceAccounts` | — |
| GET | `/v1/auth/service-accounts/{serviceAccountId}` | `getAuthServiceAccountsForGetAuthServiceAccountsByServiceAccountId` | — |
| HEAD | `/v1/auth/service-accounts/{serviceAccountId}` | `headAuthServiceAccounts` | — |
| PATCH | `/v1/auth/service-accounts/{serviceAccountId}` | `patchAuthServiceAccounts` | — |
| GET | `/v1/auth/service-accounts/{serviceAccountId}/audit-log` | `getAuthServiceAccountsAuditLog` | — |
| PATCH | `/v1/auth/service-accounts/{serviceAccountId}/client-certificate` | `patchAuthServiceAccountsClientCertificate` | — |
| PATCH | `/v1/auth/service-accounts/{serviceAccountId}/scopes` | `patchAuthServiceAccountsScopes` | — |
| POST | `/v1/auth/service-accounts/{serviceAccountId}:deactivate` | `postAuthServiceAccountsDeactivate` | — |
| POST | `/v1/auth/service-accounts/{serviceAccountId}:lock` | `postAuthServiceAccountsLock` | — |
| POST | `/v1/auth/service-accounts/{serviceAccountId}:reactivate` | `postAuthServiceAccountsReactivate` | — |
| POST | `/v1/auth/service-accounts/{serviceAccountId}:rotate-secret` | `postAuthServiceAccountsRotateSecret` | — |
| POST | `/v1/auth/service-accounts/{serviceAccountId}:unlock` | `postAuthServiceAccountsUnlock` | — |
| POST | `/v1/oauth/token` | `postOauthToken` | — |

### Sessions (`auth-sessions.gen.ts`) — 7 endpoints, 6 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/auth/sessions` | `getAuthSessions` | `lib/auth/sessions-actions.ts` |
| DELETE | `/v1/auth/sessions/{sessionId}` | `deleteAuthSessions` | `lib/auth/sessions-actions.ts` |
| PATCH | `/v1/auth/sessions/{sessionId}/name` | `patchAuthSessionsName` | `lib/auth/sessions-actions.ts` |
| GET | `/v1/auth/sessions:analyze-security` | `getAuthSessionsAnalyzeSecurity` | `lib/auth/sessions-actions.ts` |
| POST | `/v1/auth/sessions:refresh` | `postAuthSessionsRefresh` | — |
| POST | `/v1/auth/sessions:terminate-all` | `postAuthSessionsTerminateAll` | `lib/auth/sessions-actions.ts` |
| POST | `/v1/auth/sessions:terminate-others` | `postAuthSessionsTerminateOthers` | `lib/auth/sessions-actions.ts` |

### Signing keys (`auth-signing-keys.gen.ts`) — 3 endpoints, 0 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/auth/signing-keys` | `getAuthSigningKeys` | — |
| POST | `/v1/auth/signing-keys:cleanup` | `postAuthSigningKeysCleanup` | — |
| POST | `/v1/auth/signing-keys:rotate` | `postAuthSigningKeysRotate` | — |

### Step-up (`auth-step-up.gen.ts`) — 3 endpoints, 2 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| POST | `/v1/auth/step-up/challenges` | `postAuthStepUpChallenges` | `lib/economy/console-actions.ts` |
| POST | `/v1/auth/step-up/challenges/{challengeId}:verify` | `postAuthStepUpChallengesVerify` | `lib/economy/console-actions.ts` |
| POST | `/v1/auth/step-up/challenges/{challengeId}:webauthn-options` | `postAuthStepUpChallengesWebauthnOptions` | — |

### Trusted contacts (`auth-trusted-contacts.gen.ts`) — 4 endpoints, 0 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/auth/trusted-contacts` | `getAuthTrustedContacts` | — |
| POST | `/v1/auth/trusted-contacts` | `postAuthTrustedContacts` | — |
| POST | `/v1/auth/trusted-contacts/confirm` | `postAuthTrustedContactsConfirm` | — |
| DELETE | `/v1/auth/trusted-contacts/{contactId}` | `deleteAuthTrustedContacts` | — |

### Trusted devices (`auth-trusted-devices.gen.ts`) — 4 endpoints, 3 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/auth/trusted-devices` | `getAuthTrustedDevices` | `lib/auth/sessions-actions.ts` |
| POST | `/v1/auth/trusted-devices` | `postAuthTrustedDevices` | `lib/auth/sessions-actions.ts` |
| DELETE | `/v1/auth/trusted-devices/{deviceId}` | `deleteAuthTrustedDevices` | `lib/auth/sessions-actions.ts` |
| PATCH | `/v1/auth/trusted-devices/{deviceId}/name` | `patchAuthTrustedDevicesName` | — |

### WebAuthn (`auth-webauthn.gen.ts`) — 13 endpoints, 0 called

| Method | Path | operationId | Called from |
| --- | --- | --- | --- |
| GET | `/v1/auth/webauthn` | `getAuthWebauthn` | — |
| POST | `/v1/auth/webauthn/authentication:begin` | `postAuthWebauthnAuthenticationBegin` | — |
| POST | `/v1/auth/webauthn/authentication:complete` | `postAuthWebauthnAuthenticationComplete` | — |
| GET | `/v1/auth/webauthn/credentials` | `getAuthWebauthnCredentialsForGetAuthWebauthnCredentials` | — |
| DELETE | `/v1/auth/webauthn/credentials/{credentialId}` | `deleteAuthWebauthnCredentials` | — |
| GET | `/v1/auth/webauthn/credentials/{credentialId}` | `getAuthWebauthnCredentialsForGetAuthWebauthnCredentialsByCredentialId` | — |
| HEAD | `/v1/auth/webauthn/credentials/{credentialId}` | `headAuthWebauthnCredentials` | — |
| PATCH | `/v1/auth/webauthn/credentials/{credentialId}` | `patchAuthWebauthnCredentials` | — |
| POST | `/v1/auth/webauthn/credentials/{credentialId}:activate` | `postAuthWebauthnCredentialsActivate` | — |
| POST | `/v1/auth/webauthn/credentials/{credentialId}:deactivate` | `postAuthWebauthnCredentialsDeactivate` | — |
| POST | `/v1/auth/webauthn/credentials/{credentialId}:verify` | `postAuthWebauthnCredentialsVerify` | — |
| POST | `/v1/auth/webauthn/registration:begin` | `postAuthWebauthnRegistrationBegin` | — |
| POST | `/v1/auth/webauthn/registration:complete` | `postAuthWebauthnRegistrationComplete` | — |

### Auth module findings

- **A1 — the primary credential flows bypass the generated client entirely.**
  Password sign-in, sign-out, token refresh, sign-up, password reset, email-code,
  magic-link, web3 and MFA-verify all reach the API through hand-rolled `fetch`
  calls in `apps/web/src/lib/auth/*` and
  `packages/infrastructure/client/src/runtime/auth/*` (see F3/F4), so their
  generated methods show as "never called" while the endpoints are among the
  hottest in production. The generated auth surface is exercised mainly for
  session/device/MFA settings management.
- **A2 — complete dead clusters (0 callers, any transport)**: account recovery (6),
  API keys (4), SCIM tokens (4), service accounts (15), roles (9), WebAuthn (13),
  trusted contacts (4), signing keys (3), admin MFA reset, MFA email/push/recovery
  flows, `sessions:refresh`. The signing-keys cluster corroborates the worker-doc
  gap G2: the `JwtSigningKey` rotation subsystem has neither an automatic worker
  nor any UI caller.
- **A3 — HEAD existence probes** (`headAuthExternalLogins`, `headAuthServiceAccounts`,
  `headAuthWebauthnCredentials`) are generated and matched to real `[HttpHead]`
  actions but have no web callers.

## Repo-wide summary by area (2,115 endpoints)

"gen" = called via generated methods, "raw" = raw-fetch only, "test" = tests only,
"dead" = no caller found by any transport.

| Area | Endpoints | gen | raw | test | dead | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| commerce (payments/billing/products/subscriptions) | 345 | 16 | 8 | 0 | 321 | largest dead surface; raw dashboard fetches (F3) |
| compliance-audit | 237 | 0 | 1 | 0 | 236 | field-level, chain-of-custody, retention-simulation, value-snapshots fully uncalled |
| learning (assessments/courses/certificates/cohorts) | 229 | 108 | 0 | 8 | 113 | core teaching surface is the most-used after economy |
| testing-lab | 141 | 112 | 0 | 0 | 29 | heaviest generated-method consumer |
| social (blog/follows/groups/profiles) | 140 | 21 | 1 | 0 | 118 | |
| auth (all `auth-*` + `auth`) | 139 | 29 | 5 | 0 | 105 | see auth matrix |
| economy (wallets/marketplace/payouts/treasury/bounties) | 119 | 94 | 0 | 0 | 25 | `economy.gen.ts` 19/19 called; admin cluster 39/52 |
| access-control | 113 | 0 | 0 | 0 | 113 | zero web callers |
| learning-experience (paths/discovery) | 97 | 17 | 0 | 0 | 80 | |
| users (profile/preferences/notifications settings) | 96 | 7 | 0 | 0 | 89 | |
| tenants | 66 | 0 | 0 | 0 | 66 | |
| projects + api-project | 88 | 4 | 0 | 0 | 84 | |
| assets | 46 | 0 | 2 | 0 | 44 | raw `/v1/assets` fetches (F3) |
| notifications | 32 | 12 | 0 | 0 | 20 | |
| content (pages/versioning/marketing) | 32 | 1 | 0 | 0 | 31 | |
| launch-pad | 30 | 0 | 0 | 0 | 30 | |
| compliance (consent/ferpa) | 22 | 0 | 0 | 0 | 22 | |
| resources | 22 | 0 | 0 | 0 | 22 | |
| ai (credits/prompt templates) | 19 | 0 | 0 | 0 | 19 | |
| features (flags) | 19 | 0 | 0 | 0 | 19 | |
| identity (facade misc) | 17 | 0 | 0 | 0 | 17 | |
| api-teams / api-security / api-admin / api-access | 23 | 0 | 0 | 0 | 23 | |
| analytics | 14 | 0 | 0 | 0 | 14 | |
| health / monitoring | 20 | 0 | 0 | 0 | 20 | served by `Core/Controllers/HealthController.cs` |
| game-jams | 9 | 0 | 0 | 0 | 9 | |
| **Total** | **2,115** | **421** | **17** | **8** | **1,669** | |

## Findings

### F1 — CLIENT-CALL-NO-ENDPOINT: MFA enrollment handler posts to a non-existent route

`packages/infrastructure/client/src/integrations/next/handlers.ts:364` fetches
`${config.apiUrl}/v1/auth/mfa/sign-in/enrollment` for the
`action=mfa&providerId=enrollment` route-case (`handlers.ts:337-338`). That path
matches **no controller** (nothing under
`apps/api/Source/Modules/GameGuild.Identity.Authentication/Controllers/MfaController.cs`
serves `sign-in/enrollment`; the only "MfaSignInEnrollment" hit repo-wide is an
audit-event name in `Compliance.Audit/Services/CentralAuthenticationAuditEventSink.cs:19`)
and **no generated client method** (`endpoints.gen.ts` has only
`/v1/auth/mfa/sign-in/complete` at line 19716). Any consumer hitting the shipped
Next auth handler's MFA-enrollment action gets a 404. Either implement the
endpoint, point the handler at `/v1/auth/mfa/sign-in/complete`, or remove the
action.

### F2 — CLIENT-CALL-NO-ENDPOINT: promo-campaigns dashboard calls wrong path prefix

`apps/web/src/app/api/billing/promo-campaigns/route.ts:50` and
`apps/web/src/app/[locale]/(dashboards)/console/economy/promo-campaigns/page.tsx:14`
fetch `${apiBaseUrl()}/api/v1/promo-campaigns`. `apiBaseUrl()` resolves to the bare
API origin (`process.env.API_URL`, e.g. `http://localhost:8080` —
`apps/web/src/app/api/auth/guest-session/route.ts:8-14`), and the controller route is
`v{version:apiVersion}/promo-campaigns`
(`Commerce.Products/Controllers/PromoCampaignsController.cs:18`) — i.e. the API
serves `/v1/promo-campaigns`, not `/api/v1/promo-campaigns`. The generated client
has the correct path (`/v1/promo-campaigns`, `endpoints.gen.ts:27570`). The page
silently swallows the failure (`if (!response.ok) return [];`), so the dashboard
renders empty instead of erroring.

### F3 — parallel hand-rolled transport shadows 17+ generated endpoints

The client package's own runtime and the web app hand-fetch URLs that also exist as
generated methods, so contract drift here is invisible to TypeScript:

- `packages/infrastructure/client/src/integrations/next/actions.ts:239` (`/v1/auth/sign-up`),
  `:292` (`/v1/auth/tokens:revoke`);
- `…/integrations/next/handlers.ts:533` (`/v1/auth/sign-up`), `:575` (`/v1/auth/tokens:revoke`), `:364` (F1);
- `…/runtime/auth/extended-operations.ts:162` (`/v1/auth/mfa/verify`), `:201` (`/v1/auth/mfa/methods`),
  `:220` (`/v1/auth/password:reset-request`), `:299` (`/v1/auth/sessions`),
  `:314` (`/v1/auth/sessions/{id}`);
- `apps/web/src/lib/auth/password-actions.ts:29,78`; `apps/web/src/lib/auth/email-code-request.ts:14`;
  `apps/web/src/lib/auth/magic-link-request.ts:16`;
  `apps/web/src/app/api/auth/session-state/route.ts:18`;
  `apps/web/src/lib/workspace-actions.ts:506` (`/v1/assets`);
  `apps/web/src/app/api/assets/[assetId]/route.ts:45`, `apps/web/src/app/api/courses/content-reports/route.ts:105`;
  `apps/web/src/app/api/audit/permission-patterns/route.ts:28` (`/v1/audit-queries/:execute`).

### F4 — password sign-in itself is not the generated method

`packages/infrastructure/client/src/runtime/auth/providers/credentials.ts:38-108`
defaults to `fetch`-posting `/v1/auth/sign-in` (falling back to
`/v1/auth/mfa/sign-in/complete`), and `apps/web/src/lib/auth/web3-credentials.ts`,
`email-code-credentials.ts`, `magic-link-credentials.ts` do the same for their
flows. `postAuthSignIn` (`endpoints.gen.ts`, `AuthModule`) has zero callers. This
is F3's highest-impact instance: the single most security-critical endpoint in the
contract is validated by neither the generated types nor `safeParse` schemas.

### F5 — VERSIONED-DIFF: 7 live Orders actions missing from the generated client

`Commerce.Orders/Controllers/OrdersController.cs` serves 14 actions; the client
(`orders.gen.ts`, 7 endpoints) covers only 7. Missing
(`OrdersController.cs:143,159,250,266,345,361` + HEAD):
`POST /v1/orders/{orderId}:cancel`, `POST …:refund`, `PATCH /v1/orders/{orderId}`,
`DELETE /v1/orders/{orderId}`, `POST …:hold`, `POST …:release`, `HEAD /v1/orders/{orderId}`.
The client was regenerated after these actions landed (regen `4db4ca6e2` 2026-11-02;
`:hold`/`:release` added by `2ab11e06a` 2026-08-30, an ancestor), so the OpenAPI
document itself is dropping them — regenerate via the real pipeline
(`scripts/ci/verify-openapi-surface.sh` or the PR `openapi-<sha>` artifact) and
diff, never hand-splice.

### F6 — live controller surface outside the versioned contract (171 actions)

- **95 actions hidden on purpose** — `[ApiExplorerSettings(IgnoreApi = true)]` on 12
  legacy auth policy controllers (`AbacPolicyController.cs:19`,
  `PermissionAdminController`, `PermissionEvaluationController`, `PermissionGrantsController`,
  `ConditionalPolicyCrudController`, `ConditionalPolicyEvaluationController`,
  `AccessReviewItemController`, `AccessReviewAnalyticsController`,
  `AccessReviewCampaignController`, `PolicyBundlesController` in
  `Identity.Authorization`). Intentional, but they are live authenticated routes
  with no client and no contract tests.
- **56 unversioned actions** — controllers with no `[ApiVersion]` are excluded from
  the versioned OpenAPI document and therefore from the client:
  `Social.Ratings/Controllers/RatingsController.cs` (`api/[controller]`, 21 actions),
  `Finance.Ledgers/Controllers/LedgerEntriesController.cs` (13),
  `LedgersController.cs` (11),
  `Gamification.Achievements/Controllers/AchievementsController.cs` (11).
  These are callable HTTP endpoints that no generated client can express.
- **20 accounted** by F5 (7 orders), the 12 version-parameterized routes (below) and
  one catch-all (`OpenGraphController` `{*slug}`).

### F7 — version-as-path-parameter quirk (12 endpoints, not drift)

`commerce-marketplace-cart.gen.ts` (5 methods, **all actively used** by
`lib/marketplace/queries.ts:28` / `actions.ts:26`), `testing-lab-testing-templates`
(6) and `courses/{courseId}/checkout/complete` (1) generate method signatures like
`getVMarketplaceCart(version: string)` with the URL built as
`` `/v${version}/marketplace/cart` `` — the OpenAPI path parameter `{version}`
becomes a required call argument instead of being pinned by the client's default
version negotiation. Every other versioned endpoint is generated with the resolved
`/v1/...` prefix. Works, but callers must remember to pass `'1'`.

### F8 — ENDPOINT-NO-CALLER: 1,669 generated endpoints with no web consumer

Distribution by area is in the summary table. Largest fully-dead clusters (0 of
their endpoints called): `compliance-audit-field-level` (46),
`compliance-audit-chain-of-custody` (36), `api-project-work` (27),
`commerce-subscriptions` (26), `learning-experience-learning-paths` (26),
`compliance-audit-retention-simulation` (24), `compliance-audit-value-snapshots` (24),
`tenants` (24), `users-preferences` (24), access-control (113 across 17 files).
Many of these are deliberately-generated admin/compliance surfaces (ADR-20260821
excludes operational consoles), so "no web caller" ≠ "delete" — but they are the
deprecation-review backlog this matrix exists to track.

## Gaps (process and tooling, with file:line)

- **G1** — No CI check compares controllers against the generated client: F5 (7
  missing Orders actions) passed `4db4ca6e2` regeneration undetected. The
  verify-openapi lane validates the document hash, not controller↔client parity.
- **G2** — The hand-rolled transports (F3/F4) sit in
  `packages/infrastructure/client/src/runtime/auth/` and
  `…/integrations/next/` — inside the same package that ships the generated
  client, so lint rules cannot distinguish "consumer code" from "client code".
  Migrating them to the generated modules would shrink the audit surface to the
  generated files alone.
- **G3** — 95 `IgnoreApi` auth-policy actions (F6) have no replacement surface in
  the client; if they are legacy, deprecate and delete; if operational, they need
  at least contract tests.
- **G4** — The 56 unversioned controller actions (F6) silently fall out of every
  regenerated client; either version them or mark them `IgnoreApi` deliberately.
- **G5** — `.metadata.json` records only the document hash; there is no recorded
  controller-side fingerprint, so "client generated from older API" (the task's
  VERSIONED-DIFF class) can only be detected by re-running the pipeline. Recording
  the contributing controller files' hash in `.metadata.json` would make drift a
  diff instead of an investigation.

## Regeneration reminder

The OpenAPI client must never be hand-spliced: regenerate via
`scripts/ci/verify-openapi-surface.sh` (disposable cert + PG) or the PR
`openapi-<sha>` evidence artifact with
`pnpm --filter @game-guild/client generate -- --openapi <file> --force`.
