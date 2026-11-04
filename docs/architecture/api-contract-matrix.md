# API contract matrix — OpenAPI surface × web client usage

Last audited: 2026-11-02 against `develop` (`ac959fd8f`). The endpoint scan ran
against `74e9bba4c` and was re-verified after develop advanced (PRs #1185–#1201):
the only client regeneration in that window (`d3e340865`) added Cloudflare
Turnstile request fields for issue #1082 — `endpoints.gen.ts` is unchanged, so
the counts below are the historical audit snapshot. The F1/F2 correction notes
describe the current implementation, and F5 corrects a controller-count false
positive. The adjacent F3/F4 transport findings remain unresolved; these scoped
corrections are not a newly certified surface audit. F3 records the subsequent
Next registration/token-revocation and extended-auth migrations, and F4 records the password/MFA credential-provider
migration; the other provider transports remain.

Sources of truth:

- **Client surface**: `packages/infrastructure/client/src/generated/endpoints.gen.ts`
  (2,115 endpoint definitions; 224 module classes in `generated/modules/`), generated
  from the API OpenAPI document — `.metadata.json` records
  `apiVersion 4.4.0`, hash `a60945fa…25d158`, generated `2026-11-01T22:48:05Z`
  (commit `d3e340865`).
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

## Companion server-side matrices (do not redo here)

Four module packages merged alongside this document document the **server side**
of the same surface — route → command/query → validator → handler wiring. This
matrix is deliberately the complementary half: it answers "who actually calls
the endpoint from the web client", which the server-side matrices cannot. Read
them together: an endpoint that is dead server-side **and** has no caller here
is the safest deletion candidate of all.

| Package doc | PR | Scope |
| --- | --- | --- |
| `docs/auth/wiring-matrix.md` | #1194 | all 46 auth + authorization controllers, CQRS pipeline cell by cell |
| `docs/authorization/cqrs-wiring-matrix.md` + `docs/authorization/gaps.md` | #1200 | 24 controllers / 177 endpoints; gaps B1–B4: **31 endpoints dead server-side** (16 answer 422 planned, 15 answer 500 with no handler — issue #1203) |
| `docs/commerce/billing-orders/cqrs-wiring-matrix.md` + `docs/commerce/billing-orders/gaps.md` | #1201 | billing, orders, subscriptions handlers and FSM gaps |
| `docs/commerce/payments/02-cqrs-wiring-matrix.md` + `docs/commerce/payments/06-gaps.md` | #1193 | payments command/query wiring and money-critical gaps |

The 31 dead authorization endpoints of issue #1203 live inside the 95
deliberately-hidden `IgnoreApi` policy controllers catalogued in F6 below —
dead on the server (no handler), hidden from the contract (no client method),
and uncalled by the web app: a triple-confirmed removal backlog.

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
| POST | `/v1/auth/password:change` | `postAuthPasswordChange` | `lib/auth/password-change-action.ts`; SDK helper migrated (F3) |
| POST | `/v1/auth/password:reset` | `postAuthPasswordReset` | SDK reset migrated (F3); counts remain historical |
| POST | `/v1/auth/password:reset-request` | `postAuthPasswordResetRequest` | SDK request migrated (F3); counts remain historical |
| POST | `/v1/auth/polymorphic` | `postAuthPolymorphic` | — |
| POST | `/v1/auth/saml/{slug}:assertion-consumer` | `postAuthSamlAssertionConsumer` | `app/api/auth/saml/[slug]/callback/route.ts` |
| GET | `/v1/auth/saml/{slug}:logout-url` | `getAuthSamlLogoutUrl` | — |
| GET | `/v1/auth/saml/{slug}:metadata` | `getAuthSamlMetadata` | — |
| POST | `/v1/auth/saml/{slug}:sign-in-authorize` | `postAuthSamlSignInAuthorize` | `app/api/auth/saml/[slug]/signin/route.ts` |
| GET | `/v1/auth/session-state` | `getAuthSessionState` | `app/[locale]/(dashboards)/workspace/settings/account/page.tsx` |
| POST | `/v1/auth/sign-in` | `postAuthSignIn` | — (raw transport, F3/F4) |
| POST | `/v1/auth/sign-out` | `postAuthSignOut` | — (raw transport, F3/F4) |
| POST | `/v1/auth/sign-up` | `postAuthSignUp` | SDK Next registration migrated (F3); counts remain historical |
| POST | `/v1/auth/tokens:refresh` | `postAuthTokensRefresh` | — (raw transport, F3/F4) |
| POST | `/v1/auth/tokens:revoke` | `postAuthTokensRevoke` | SDK Next logout migrated (F3); counts remain historical |
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

- **A1 — primary credential transport gap (historical audit).**
  At audit time, password sign-in, sign-out, token refresh, sign-up, password reset, email-code,
  magic-link, web3 and MFA-verify all reach the API through hand-rolled `fetch`
  calls in `apps/web/src/lib/auth/*` and
  `packages/infrastructure/client/src/runtime/auth/*` (see F3/F4), so their
  generated methods showed as "never called" while the endpoints were among the
  hottest in production. The F3/F4 correction notes record subsequent generated
  migrations; these historical audit counts have not been recertified.
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

**Correction (issue #1217, F1):** the SDK no longer dispatches that standalone
enrollment action or fetches the unserved API path. Required-MFA sign-in already
returns the sign-in-bound `mfaEnrollment` offer; the Next handler passes that offer
to the UI without issuing a session. Completion continues through the real
`/v1/auth/mfa/sign-in/complete` route with the limited bearer and proof. Tests cover
the offer, local rejection of the obsolete action, CSRF and session isolation.

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

**Correction (issue #1217, F2):** the page and all campaign proxy calls now use
`/v1/promo-campaigns`, including performance and lifecycle routes. A proxy test
derives the expected prefix from `PromoCampaignsController` itself. Initial HTTP,
network and malformed-response failures render a safe server-side alert; only an
actual successful empty collection renders the empty state. A later successful
server render replaces the alert with fresh campaign data. These corrections do
not resolve F3/F4/F5 or turn the no-caller inventory into a deletion list.

### F3 — SDK registration/logout and extended auth migrated; web transports remain

The original audit identified 17+ generated endpoints shadowed by hand-written
transports in the client runtime and web app. The Next route and server-action
registration paths now share `runtime/auth/sign-up.ts`, which calls generated
`AuthModule.postAuthSignUp`. Generated schemas validate both directions before
session finalization. CAPTCHA and guest-continuity fields, legacy profile/claims,
callbacks and redirects are retained; malformed, denied, incomplete and pending
MFA/step-up responses never create an ordinary session. Network/server/response
failures produce a service-unavailable error, distinct from invalid registration.

The two Next logout paths now call generated `AuthModule.postAuthTokensRevoke`
through `runtime/auth/sign-out.ts`. Revocation carries the access bearer from the
decrypted session: the endpoint requires `[Authorize]` and its handler enforces
refresh-token ownership. The old calls supplied no bearer, so could clear local
cookies while leaving remote refresh credentials valid. Missing/malformed tokens
are not sent, and request-body actor/token fields cannot replace the session's
credentials. Logout never refreshes or rotates tokens. Revocation has a five-second
request budget; failure still permits local cookie cleanup. Server authorization
and ownership checks are unchanged. The old enrollment route is covered by F1.

Authenticated `setupTotpMfa` and `getMfaMethods` now use generated
`AuthMultiFactorModule` operations with the current actor's bearer, no automatic
refresh, quiet diagnostics and a five-second request budget. Setup uses the served
`/v1/auth/mfa/totp:setup` action (the old `/totp/setup` route is absent), maps
`secretKey` to the public `secret` field and explicitly returns
`requiresVerification: true`. It validates the provisioning data: the controller
leaves `isSuccess` at its DTO default even on a successful HTTP response. Missing,
invalid or denied provisioning throws `MfaVerificationError`.

Method discovery validates the backend's method-info objects and returns the
existing string names (`totp`, `backup_code`, etc.) for available methods, including
those available for enrollment but not enabled yet. Valid null/empty lists and HTTP
rejections retain the empty-list fallback. Invalid successful responses and network
failures throw a typed error instead of returning unchecked data. These settings
operations do not complete the limited-challenge sign-in flow, which remains
separate.

`verifyMfa` now completes that sign-in flow through generated
`AuthModule.postAuthMfaSignInComplete` at `/v1/auth/mfa/sign-in/complete`, instead
of posting a session ID to the authenticated settings-verification route. It takes
the opaque first-factor `mfaToken`, TOTP or backup-code proof and an optional device
binding. Actor/tenant/IP assertions and the deprecated ordinary-bearer argument
are never forwarded. The deprecated `mfaSessionId` input accepts an opaque challenge
only when no explicit `mfaToken` was supplied; a session GUID cannot authenticate.
Unsupported SMS, invalid challenges and oversized proofs fail before network access.

Generated validation precedes the result mapping. Denied, pending MFA/step-up,
malformed and incomplete replies cannot yield ordinary credentials; completion
requires access/refresh credentials and a validated identity. Legacy profile/claim
extensions remain supported, and one-time enrollment backup codes are returned
separately from tokens. This operation has no automatic refresh or diagnostic
payload logging and has a five-second request budget. Failures preserve the public
`MfaVerificationError` type. The backend completion endpoint returns generic
verification failure; this adapter does not invent an attempts-remaining count.

The SDK reset request, reset confirmation and authenticated password change now
call generated `AuthModule` operations. Both mutations were missing the backend's
mandatory `confirmPassword`; existing callers now send their `newPassword` as the
confirmation, while explicit mismatches fail locally. Generated validators check
the request and reply, and mutation completion requires `success: true`, rather
than treating any HTTP success as proof that the password changed. Errors retain
the public `PasswordResetError` type.

Optional reset tenant context and change-session revocation policy are forwarded
through the supported DTO fields. Omission preserves the backend's revocation
default, and an empty current password still supports its OAuth-only initial-password
flow. Change uses the supplied actor bearer; reset stays anonymous. Caller-provided
actor, IP, tenant or session assertions cannot replace the authenticated change
context. These operations do not rotate credentials, log payloads or wait beyond
the five-second request budget.

Reset-request HTTP outcomes remain indistinguishable to callers: success, rejection,
account flags and malformed remote acknowledgements all resolve without account
information. Local syntax errors and network/deadline failures produce generic
typed errors. This preserves the existing neutral HTTP acknowledgement; it does not
claim that an email was delivered when the server rejected or failed the request.

The three SDK email helpers now use generated operations. Authenticated
`sendVerificationEmail` previously sent no body to an endpoint that requires an
email address. It now reads the server-authenticated actor ID from session state,
uses that ID for the protected own-user read, checks the returned profile ID and
submits that profile's email. It never decodes a JWT to infer identity or accepts a
caller-supplied recipient. Revoked tokens, invalid bound sessions and mismatched
profiles fail before sending; an authenticated stateless principal does not need
a browser session. The user controller retains its self-read policy and actor
guard; policy denials stop the operation without a recipient fallback. All three
requests share one five-second operation budget, including body
consumption, without token refresh or diagnostic payload logging.

Anonymous resend still sends only the caller's email, and token verification may
forward the endpoint's optional tenant context. Generated schemas validate both
directions; verification requires explicit `success: true`. Send/resend return no
server message and make no email-delivery or account-existence claim.
Failures preserve `EmailVerificationError`, and malformed or unsuccessful replies
cannot report email verification as complete.

The four SDK session helpers now use `AuthSessionsModule`. Listing explicitly
validates every row because the generated collection method currently only casts
its result. Native arrays and the existing `{ sessions }` envelope are supported;
the native nullable device/location objects are normalized to optional fields
before schema validation. Required identity, timestamps and current-session fields
must be present. `lastUsedAt` maps to public `lastActiveAt`, and
`deviceInfo.userAgent` maps to `userAgent`; missing details are not fabricated.
Invalid collections fail with `AuthServiceUnavailableError`, rather than appearing
as an empty or partial list. The existing HTTP-rejection empty-list fallback remains.

Single-session termination validates the UUID before calling the generated method,
whose path currently interpolates the ID. Traversal, query and fragment text never
reaches the transport. Mutations use the current actor bearer with no request body,
automatic refresh or payload logging, and a five-second request budget. Generated
acknowledgements are validated; bulk counts must be non-negative integers, including
the valid zero-session case. Errors retain `SessionTerminationError`. Actor, tenant
and current-session decisions remain in the backend, and local cookie cleanup
remains the caller's responsibility. The obsolete raw-fetch helpers were removed:
all thirteen functions in `runtime/auth/extended-operations.ts` now use generated
operations. This is not a claim that other SDK providers or web transports migrated.

Remaining transports from the historical inventory (line numbers below belong to
the original audit, not the current source):

- `apps/web/src/lib/auth/password-actions.ts:29,78`; `apps/web/src/lib/auth/email-code-request.ts:14`;
  `apps/web/src/lib/auth/magic-link-request.ts:16`;
  `apps/web/src/app/api/auth/session-state/route.ts:18`;
  `apps/web/src/lib/workspace-actions.ts:506` (`/v1/assets`);
  `apps/web/src/app/api/assets/[assetId]/route.ts:45`, `apps/web/src/app/api/courses/content-reports/route.ts:105`;
  `apps/web/src/app/api/audit/permission-patterns/route.ts:28` (`/v1/audit-queries/:execute`).

### F4 — password/MFA sign-in migrated; other provider transports remain

The original audit found that
`packages/infrastructure/client/src/runtime/auth/providers/credentials.ts` built
its password and MFA completion requests by hand. The generated `postAuthSignIn`
had no callers, including after the Turnstile field was added for #1082.

The 2026-11-04 correction routes the default credentials provider through
`AuthModule.postAuthSignIn` and `AuthModule.postAuthMfaSignInComplete`, using the
shared client transport and generated request/response validation. CAPTCHA,
tenant and persistence fields now participate in that contract. Malformed
successful responses cannot become sessions or be mistaken for MFA challenges.
The custom sign-in path remains supported through a request interceptor; it uses
the same generated sign-in contract. Older profile extensions and claim arrays
remain compatible without overriding validated contract fields.

Credential diagnostics are explicitly disabled, including console groups. The
shared error adapter accepts legacy string `message` payloads after standard
ProblemDetails fields, preserving account-lock and invalid-credential messages;
service outages and malformed responses remain service failures. MFA challenges
and one-time recovery-code handling retain their existing no-session/no-persistence
boundaries.

Wire-level checks also found two API/schema mismatches that the old raw transport
had hidden. The host now preserves nullable enum references in serialized OpenAPI
without making the shared enum nullable. Pending first factors omit default,
unspecified credential expiry timestamps; completed sign-ins retain their actual
expiries. The client generator accepts RFC3339 date-time offsets, including the
`DateTimeOffset` expiry in the native enrollment offer, while timezone-free
timestamps remain invalid. The client was regenerated from the freshly built
full host's captured OpenAPI document; generated files were not hand-edited.

`tests/auth/providers/credentials-contract.test.ts` exercises invalid requests,
malformed successful responses, the actual fetch boundary and compatibility
controls, including two synthetic fixtures serialized by the native pending-MFA
response constructor. Existing provider, Next handler and enrollment-action tests retain the
MFA and callback behavior checks with JSON responses and valid identifier fixtures.

`apps/web/src/lib/auth/web3-credentials.ts`, `email-code-credentials.ts` and
`magic-link-credentials.ts` still use separate transports for their flows. They
and the other F3 call sites remain unresolved; this correction does not close the
whole #1217 issue or certify the historical endpoint inventory.

### F5 — corrected: Orders exposes seven reviewed operations, all present in the client

The original scan counted 14 compiled `OrdersController` methods as live HTTP
actions. The host unconditionally registers
`MinimumOrderRouteApplicationModelConvention`, which removes every Orders action
without `[MinimumOrderRoute]` from the MVC application model. This affects runtime
routing as well as OpenAPI. Cancel, refund, patch, delete, hold, release and HEAD
are deliberately excluded pending production safety review; their absence from
the generated client is not VERSIONED-DIFF.

The seven reviewed operations are list, get-by-id, create, add-item, capture,
complete and payment-intent. All seven appear in
`generated/modules/commerce-orders.gen.ts`.

Evidence rechecked on 2026-11-04:

- `ModuleOpenApiIntegrationTests.Swagger_ShouldExposeOnlyTheVerifiedOrderSurface`
  asserts the exact seven method/path pairs in the hosted OpenAPI document.
- `Runtime_ShouldRouteOnlyVerifiedOrderOperations` checks the actual HTTP boundary:
  the excluded cancel action returns 404, list remains authenticated, and the
  reviewed get/payment-intent routes are available to routing.
- Both tests passed in [CI run 41495243956](https://github.com/gameguild-gg/gameguild/actions/runs/41495243956)
  at `a54a689798c3c15b823bbdf731884fd62f92ac6b`, retained in the
  `full-application-openapi-a54a689798c3c15b823bbdf731884fd62f92ac6b` TRX artifact.
  The convention, its host registration, marker attribute and these tests are
  unchanged at `c42b1049`. The controller's intervening change adds the API rate
  limit policy; its route and action markers are unchanged. This is scoped routing
  evidence, not a fresh execution of the entire current API.

Do not expose unreviewed actions or hand-edit generated files to satisfy a source
method count. Future reviewed route additions must still use the real OpenAPI
capture and generation pipeline described below.

### F6 — live controller surface outside the versioned contract (171 actions)

- **95 actions hidden on purpose** — `[ApiExplorerSettings(IgnoreApi = true)]` on 12
  legacy auth policy controllers (`AbacPolicyController.cs:19`,
  `PermissionAdminController`, `PermissionEvaluationController`, `PermissionGrantsController`,
  `ConditionalPolicyCrudController`, `ConditionalPolicyEvaluationController`,
  `AccessReviewItemController`, `AccessReviewAnalyticsController`,
  `AccessReviewCampaignController`, `PolicyBundlesController` in
  `Identity.Authorization`). Intentional, but they are live authenticated routes
  with no client and no contract tests — and the authorization package audit
  (PR #1200, issue #1203) proved 31 of them dead server-side (422 planned /
  500 no handler), so this cluster is hidden *and* broken.
- **56 unversioned actions** — controllers with no `[ApiVersion]` are excluded from
  the versioned OpenAPI document and therefore from the client:
  `Social.Ratings/Controllers/RatingsController.cs` (`api/[controller]`, 21 actions),
  `Finance.Ledgers/Controllers/LedgerEntriesController.cs` (13),
  `LedgersController.cs` (11),
  `Gamification.Achievements/Controllers/AchievementsController.cs` (11).
  These are callable HTTP endpoints that no generated client can express.
- **20 historically accounted** by F5 (7 Orders methods excluded from MVC, as
  corrected above), the 12 version-parameterized routes (below) and one catch-all
  (`OpenGraphController` `{*slug}`). The seven excluded methods are not live HTTP
  endpoints; the historical headline count must not be read as a current inventory.

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

- **G1** — A repository-wide controller-to-client parity check would need to use
  the composed MVC surface, including intentional exclusions. F5 is not evidence
  of generator drift: existing OpenAPI and runtime integration tests explicitly
  enforce the seven-operation Orders boundary. The verify-openapi lane validates
  generation against the captured document, not every compiled controller method.
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
