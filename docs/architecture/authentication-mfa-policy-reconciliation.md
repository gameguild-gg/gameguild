# Authentication configuration and MFA reconciliation — #145

## Web MFA consumer checkpoint — 2026-10-08

The existing #704 branch incorporates develop merge #705 at
`7e91dcaa0486789cff02081e56a49e752c3ab405`. The conflict retains both the
uniform email-verification message and the monotonic credential timing floor.
This checkpoint connects the browser credentials consumer to the native limited
MFA enrollment/completion endpoints; #145 and #288 remain open.

- Credentials preserve the opaque expiring first-factor bearer and available
  methods. Completion sends only bearer, method and code: it does not resend a
  password, supplied actor or supplied tenant. An incomplete HTTP success cannot
  become an authenticated provider result.
- Next enrollment and completion require the existing CSRF protection. Enrollment
  exposes only provisioning fields and never sets an ordinary session cookie.
  Error logging records the error type, without serializing the challenge bearer.
- The existing sign-in form supports TOTP, recovery codes and manual authenticator
  setup. Challenge/provisioning information stays in mounted component/hook memory.
  Recovery codes are shown once after confirmation, before client redirect, and
  are excluded from JWT/session storage. Ordinary sign-in retains its layout.
- The Code runner continues the administrator's required MFA before creating a
  course and through browser CSRF/cookie handling before entering SpeedGrader.
  Its independent RFC 6238 helper waits for a new counter rather than disabling
  native replay protection. Original Code functional checks remain verbatim.

Current combined sources passed Authentication 2,535, Authorization 1,667,
required audit 6, host architecture/security 137 and PostgreSQL HTTP/migration 34:
4,379 tests, with no failures/skips. Client build/typecheck and all 1,155 client
tests passed. All 21 form tests and six TOTP support tests passed.
Native22's cleanup collection timeout remains in its original receipt; separate
exact ID/name checks confirmed the owned container absent and all 55 primary
checkout files preserved.

The actual local Code cycle has not passed. Attempts 1 and 2 rebuilt the trusted
runtime and passed its 28 frozen contracts, but stopped before API/browser tests
because the disposable PostgreSQL did not become ready within 60 seconds.
The previous report is inherited historical evidence and must not be treated as
this checkpoint's functional result. The current Debug API build was reused with
recorded hash/source provenance; full CI validation is still required. No timeout,
MFA policy, grading assertion, browser sandbox or scanner was relaxed.

Evidence under `D:/Codex/work-artifacts/release-2026-11-25/gameguild-issues-01a0d900/`:

- `native22-post-develop-merge-cleanup-reconciliation-20261008.json`
- `issue-145-web-mfa-client-validation-20261008/result.json`
- `web-mfa-credentials-baseline-20261008.json` and `web-mfa-handlers-baseline-20261008.json`
- `web-mfa-form-baseline-20261008.json` and `web-mfa-form-final-source-20261008.json`
- `issue145-web-mfa-real-code-cycle-20261008/result.json`
- `issue145-web-mfa-real-code-cycle-attempt02-20261008/result.json`

The published native checkpoint `2868db9576563253e8405e0ec43cf5c0f1a92a16`
passed CodeQL. GitGuardian still fails and Codacy requires action. Release/API/
OpenAPI licensing, fresh combined CI/Code acceptance and the remaining original
#145 requirements remain merge/closure gates. This checkpoint closes zero issues.

## Native limited enrollment checkpoint — 2026-10-08

This follow-up to published PR #704 head `a99605af74bd2a46aa2887e21bd0e0915e6f4394`
adds a locally validated limited enrollment flow. #145 remains open: its twelve
original acceptance criteria, technical requirements and fifteen definition-of-done
items below remain authoritative. Publication/CI and the complete issue are separate gates.

`POST /v1/auth/mfa/sign-in/enrollment` selects identity, tenant, account version and
policy only through the server's expiring first-factor challenge. It returns TOTP
provisioning data with an expiry bounded by that five-minute challenge, and creates
no ordinary token, authenticated session or recovery code. The database records
configuration identity, a canonical-secret fingerprint and initialization time,
never the plaintext challenge bearer or provisioning data in the challenge.
The same live challenge resumes the exact pending configuration. Another challenge
cannot overwrite a live setup. Subject transaction locks and existing configuration
row locks remain held through proof verification and credential commit.

Enrollment completion accepts TOTP only, rechecks exact setup/account/tenant/version/
policy, consumes the challenge once, and persists the existing MFA session proof.
Recovery codes are generated and returned once, only after verified TOTP. Challenge,
configuration, replay watermark, session, refresh token, recovery set and required
MFA audit writes share the owning transaction. Required MFA audit failures now
propagate through the central audit bridge. Ordinary/signup responses omit the new
nullable `mfaEnrollmentBackupCodes`. A separate signup-success contract decision
remains unresolved in this chat; this checkpoint does not change signup behavior.

Native EF Core 10.0.9 generated `20261008125046_BindLimitedMfaEnrollment` and its
snapshot. It adds challenge binding columns/checks and a unique user MFA index.
An explicit preflight aborts on duplicate historical configurations while preserving
every factor row for reconciliation. No factor is automatically chosen or deleted.
The historical challenge-table emptiness test now projects its existing Id column
when testing the older migration, preserving both original empty-table assertions.

| Verified selection | Passed | Scope |
|---|---:|---|
| Authentication UnitTests | 2,528 | Entire native project, including enrollment proof and trusted remote peer tests. |
| Authorization UnitTests | 1,667 | Entire native project. |
| Required MFA audit | 6 | Audit sink success and required-failure propagation. |
| API architecture/security | 137 | Focused native host selection. |
| Configured OpenAPI documentation | 8 | Native documentation selection. |
| MFA PostgreSQL HTTP/migrations | 34 | Original password/completion/replay/refresh cases plus limited enrollment, exact resume, concurrent confirmation, expired/stale/revoked bindings, failed TOTP accounting, backup rejection, audit rollback and migration upgrade/down/duplicate preservation. |
| Native OpenAPI capture | 1 | Actual host export; completion/enrollment request/response schema independently checked. |
| Generated TypeScript client | 1,140 | Native-document generation, build, typecheck and complete client tests. |

The module generator now honors the same strict boolean AllowAnonymous metadata as the endpoint generator. Its original regression had one anonymous-marker failure and eight passing protected/empty-security controls; the assertions are retained. Regeneration changes only authentication decisions in affected modules, and the full client build/typecheck/test selection includes those regressions.

These bounded selections passed with zero failures/skips. Unit17's four completed
groups bind byte-identical production/unit sources; its final Integration compilation
failure remains recorded. The two subsequently repaired integration test files
passed in native20. Native20's and capture21's cleanup collection timeouts also remain in its original
receipt: separate exact-ID/name inspection confirmed absence. No failed receipt is
rewritten as a successful full run. Baseline10's original required-but-unenrolled
regression reproduced HTTP 404 and remains byte-for-byte unchanged; it now passes.
Capture19 failed before tests during Docker startup; capture21 uses the exact owned container after verified recovery. Earlier compiler/restore/precondition failures are retained separately.

Receipts under `D:/Codex/work-artifacts/release-2026-11-25/gameguild-issues-01a0d900/`:

- `native-enrollment-validation20-and-unchanged-unit17-reconciliation-20261008.json`
- `issue-145-native-repository-enrollment-openapi-capture21-execution01/result.json`
- `native-openapi-capture21-cleanup-reconciliation-20261008.json`
- `issue-145-limited-enrollment-anonymous-client-generation-20261008/result.json`
- `issue-145-limited-enrollment-anonymous-client-validation-20261008/result.json`

The 55 unrelated primary files remain preserved; owned test PostgreSQL containers
are verified absent. The Debug native host builds; Release/API/OpenAPI CI still
requires the ImageSharp license decision. GitGuardian's exact a996 occurrence is a
synthetic per-test generated password, but scanner disposition is still pending.
Chrome login reached personal workspace 934842; direct navigation to the check's
incident in workspace 934528 redirects back to the personal workspace without
integrated sources. No integration/access expansion or scanner waiver was performed.
The published Code pipeline also needs the administrative credentials consumer to
complete required MFA instead of attempting course creation with an empty token.

The live fixed-scope refresh records #287 independently closed by merged PR #705
(`ae4301fa045170e51340f93aa076a93b14963ceb`, 2026-10-08 13:22:44 UTC), for uniform
email-verification messages and delivery-failure responses. Its timing child #288
remains open. That develop update has not yet been incorporated into this locally
validated source; its reported 2,323 unit tests are separate evidence and are not
attributed to this checkpoint. Every scoped issue ID remains in the matrix.

Remaining #145 work includes complete MFA integration with the web sign-in flow,
other sign-in schemes, high-risk whole-flow acceptance, ordinary-issuance role/tenant
races, external-provider sandboxes, legacy/recovery requirements and the remaining
original criteria. This checkpoint performs no merge or issue closure.

## Scope and provenance

The original #145 request has twelve acceptance criteria, technical requirements,
and a fifteen-item definition of done. The original #273 description is retained
in canonical #145. Configuration switches and an endpoint's existence alone do
not establish enforcement or successful completion of an authentication flow.

This checkpoint uses the owned checkout based on accepted develop
`d5f417328e9ff9d3fa0517796a623e310aeb1ef0`, published PR #704 head
`b4cf8156baf4ea74a5735146cf70af328564f022`, and its separately guarded local
follow-up. #145 remains open. The separate ASP.NET Core Identity persistence
requirement is recorded as historically superseded in
`docs/api/authentication-options-configuration.md`; the current custom identity
stack does not literally implement that original framework requirement.

## Original acceptance criteria

| Original requirement | Existing integration | Remaining evidence or implementation |
|---|---|---|
| JWT Bearer token authentication with custom claims | `SecurityServiceCollectionExtensions.SetupAuthentication`, `JwtTokenService` | Run current issuance/validation and invalid issuer, audience, lifetime, signature, subject and token-version cases; reconcile custom claims. |
| OAuth2 with Google, Microsoft and GitHub | `OAuthService`, `OAuthAuthService`, typed provider options | Verify state/PKCE, callback exchange, provider identity and tenant binding; run configured provider sandboxes. |
| API keys from header, query and custom sources | `ApiKeyAuthenticationHandler`, configured resolver registration | Run real authentication boundaries for all sources, conflicting sources, HTTPS, expiry, revocation and default scheme preservation. |
| Cookies with secure defaults | Opt-in cookie scheme in `SetupAuthentication` | Verify issuance, deletion, expiration, sliding behavior, secure/HttpOnly/SameSite and CSRF boundaries. |
| Basic authentication for legacy systems | Opt-in `BasicAuthenticationHandler` | Verify HTTPS and account state; enrolled MFA is rejected, but global/subject policy also needs reconciliation. |
| Configurable token expiration and refresh policies | Typed JWT options; accepted PR #699/#263 lifecycle and containment | Preserve accepted rotation, family/account containment, durable notification and session deadlines; validate configuration precedence and public-boundary expiration. |
| MFA integration points | TOTP, backup codes, policy reader, MFA endpoints and authenticated step-up receipts | Reproduce mandatory-policy behavior before claiming a login bypass; enforce a complete first-factor/second-factor flow before ordinary token/session issuance. |
| Custom authentication scheme registration | Additional scheme callback in `SetupAuthentication` | Verify coexistence, named selection and JWT defaults at real middleware boundaries. |
| HSTS, CSP and X-Frame-Options configuration | `SecurityHeadersMiddleware`, pipeline HSTS configuration | Verify actual responses and supported environments, including authenticated and failure responses. |
| Account lockout and rate limiting policies | Shared password admission follow-up and rate-limiting middleware | Current shared-admission HTTP selection passed 78/78, including all 12 added polymorphic account/IP/held-lock cases. This is not every authentication scheme or the complete API Integration project. |
| Password complexity configuration | Typed options and `PasswordHasher` | Verify enforcement at registration, reset and change, policy precedence and long-input handling without weakening existing rules. |
| External identity provider integration | Typed provider configuration and OAuth dispatch | Verify callbacks, external identity binding and configured provider sandboxes; configuration alone is insufficient. |

Technical requirements for concurrent schemes, thread safety, performance,
security audit, authorization integration, migration guidance and comprehensive
tests remain part of the issue. This table does not reduce them to MFA alone.

## Actual-source MFA observations — 2026-10-08

Artifacts are under
`D:/Codex/work-artifacts/release-2026-11-25/gameguild-issues-01a0d900/`.
The diagnostic calls anonymous HTTP against actual API/authentication assemblies,
real password hashing, JWT/session services and migrated PostgreSQL. Only the
risk classifier is controlled to low risk; policy results are not mocked.

1. Baseline03 failed package restore before executing either case. Its original
   failed receipt is retained. Exact container removal was independently verified
   absent at 03:52:59 UTC after an earlier removal collection timeout.
2. Baseline04 executed two cases: one passed, one failed before the true-policy
   sign-in request because the real policy reader returned false despite the
   effective `IOptions<MfaOptions>` value being true. The original assertions,
   log and failed TRX are retained. Its owned container was verified absent.
3. Baseline05's Docker creation collection timed out before any tests. The exact
   owned container subsequently started; its identity, label and loopback binding
   were checked before a separate recovery execution.
4. Baseline05 recovery executed two cases: one passed, one again failed the
   policy precondition before sign-in. Source, primary files, loaded assemblies
   and harness guards stayed unchanged. Cleanup returned zero and exact-ID
   inspection confirmed absence. Log/TRX SHA256:
   `a63aaa6c8a04f3d59ca035e3c67a9f1882f35cdee63f096b0eb8ae8a75ddf2bf`
   / `3849643685673772a67cbfcacdf41bcf094aa3e1d19aea502944712a6b62ba48`.
5. Baseline06 keeps every original assertion and additionally requires actual
   `MfaService`/`MfaAttemptTrackingService` implementations and their effective
   captured options. The real policy reader is constructed with the explicit
   effective options to qualify the factory configuration seam. Results are
   pending. No runtime bypass is established by the preceding failed preconditions.

Static inspection still identifies two concrete concerns: low-risk local password
sign-in does not call the MFA policy reader, and that reader uses the current
HTTP principal's roles instead of resolving the requested subject. Anonymous
sign-in cannot obtain subject-role policy from an authenticated caller principal.
The high-risk response currently returns an unpersisted GUID as step-up evidence.
These source findings require implementation and functional acceptance.

## Required complete flow

- Resolve the authenticated first-factor subject and active tenant membership on
  the server. Evaluate enrolled MFA and required policy for that subject before
  issuing ordinary access/refresh tokens or persisting an authenticated session.
- Persist a short-lived, cryptographically random pre-login challenge. Store a
  hash of its opaque bearer value; bind subject, tenant, token version, validated
  first factor and expiry. Do not accept a client-supplied subject as authority.
- Complete verification through supported MFA providers and existing backup-code
  protections. Expired, replayed, mismatched, revoked or unavailable challenges
  must fail closed. Concurrent consumption must allow at most one issuance.
- Revalidate account, token version and tenant access at completion and preserve
  accepted session/refresh lifecycle invariants. Audit requirement, failure and
  successful completion without recording credentials, codes or bearer values.
- Required but unenrolled accounts need a limited enrollment/recovery flow; they
  must not obtain a normal access token to satisfy enrollment.
- Reconcile Basic, cookies, OAuth and other sign-in methods against the same
  requirement. High-risk login needs usable, persisted verification evidence.
- Preserve existing request/response contracts and regenerate API clients when
  new completion contracts are accepted.

Existing `StepUpReceiptService` requires an authenticated actor, tenant and
session. It cannot directly represent an anonymous pre-login challenge. Existing
`POST /v1/auth/mfa/verify` verifies a supplied user's code and returns a boolean;
it must not become an ordinary token issuer without first-factor and challenge
binding.

Acceptance must include mandatory/enrolled/admin MFA, no ordinary token before
verification, successful TOTP and backup-code completion, failed/expired/replayed
evidence, concurrent consumption, tenant/account/token-version changes, lockout,
enrollment/recovery and every supported sign-in method. Native/security checks
and external-provider sandbox evidence remain required before #145 closes.

## Qualified mandatory-policy defect

Baseline06 again failed before sign-in, this time at the effective options
precondition. That failed result and its original assertions are retained;
no login-enforcement conclusion is drawn from it.

Baseline07 uses the normal test-host `PostConfigure<MfaOptions>` seam to qualify
enforcement independently of configuration-provider ordering. The real
`MfaAttemptTrackingService` and `MfaService` remain in use; no policy result is
mocked. Effective, directly resolved and captured MFA options all match the
requested setting, and the real policy precondition passes.

The execution completed at **04:24:08 UTC**: two cases executed, one control
passed and one mandatory-policy case failed, zero skipped. For the mandatory
case, actual anonymous low-risk password HTTP returned ordinary access and
refresh tokens, with one persisted refresh token and one session, despite the
real policy requiring MFA. The original no-ordinary-token assertion then failed.
This reproduces a local sign-in enforcement defect under the qualified test-host
options. It does not certify production configuration binding, every sign-in
scheme or complete #145 acceptance.

Source, primary files, actual loaded assemblies and harness stayed byte-guarded
unchanged; exact owned-container cleanup and absence were verified. Artifacts:
`issue-145-mfa-policy-http-current-execution07/result.json` and
`issue-145-mfa-required-policy-actual-http-defect-20261008.json`.
Log/TRX SHA256:
`6dcafeeafb367bb7cd8b938963cf0070ac3b008507b8659c452efa8c01d56786`
/ `8c9d0f8238aa2f4d191cfa33ee6ed10aa2e914759f534b7803bfa434c8b40712`.
This is retained defect evidence, not a passing acceptance result. The complete
flow above still needs implementation and verification. Its completion should
reuse `IAuthenticatedSessionIssuer` and the existing MFA providers while
preserving accepted session/root-refresh binding and command transactions.

## Native password preparation and persistence checkpoint — 2026-10-08

The validated challenge, subject-policy, TOTP replay-store and internal response
mapping sources are now applied to the existing authentication worktree.
Password sign-in captures the account token version before password verification,
checks the current account against that evidence, and calls the mandatory MFA
preparation service before its existing credential and session issuance path.
Required, enrolled and high-risk cases produce a persisted limited challenge.
The earlier unpersisted high-risk GUID has been removed from this path. A pending
outcome is retained through the mapper and command transaction; it contains no
ordinary access/refresh credentials or authenticated session.

Ordinary preparation carries its verified subject and policy decision so the
coordinator does not repeat the same reads. The original ordering assertion was
retained: the first native run exposed the extra reads, and the second run passed
all 2,488 authentication unit cases after correcting the implementation. The local
handler records a successful sign-in only when the response is successful.

The native migration `20261008091741_AddSignInMfaChallengesAndTotpReplayState`
adds only `sign_in_mfa_challenges`, `totp_replay_state` and the challenge indexes.
It stores challenge hashes and per-enrollment/key TOTP time-step watermarks;
challenge bearers, TOTP secrets and supplied codes are not added to these tables.
The native migration test checks upgrade, rollback, retention of an existing MFA
enrollment, and `HasPendingModelChanges() == false` without adding a warning
suppression to its database context.

The retained external regression execution ran the original two baseline07 HTTP
cases byte-for-byte plus the native migration case: **3/3 passed**, zero skipped.
Mandatory low-risk sign-in emitted no ordinary credentials, refresh row or session;
the optional policy control retained its normal token/session result. This fixes
the previously qualified password-sign-in defect under the same options seam.

The subsequent execution used the actual repository test projects:

| Native selection | Executed / passed | Scope |
|---|---:|---|
| Authentication UnitTests | 2,488 / 2,488 | Entire authentication unit project. |
| Authorization UnitTests | 1,667 / 1,667 | Entire authorization unit project. |
| API UnitTests architecture/security | 137 / 137 | Focused host architecture, authentication, security and migration-deployment selection. |
| API IntegrationTests MFA policy/migration | 3 / 3 | Native mandatory/optional HTTP cases and migration upgrade/rollback/model consistency. |

All **4,295** selected cases passed with zero skipped. The native HTTP coverage
also requires the real local-authentication implementation and checks the pending
response, persisted bearer hash, subject version, tenant, first factor, enrollment
purpose and five-minute expiry. This selection does not represent the entire API
UnitTests or IntegrationTests projects.

Receipts under the artifact root above:

- `issue-145-native-password-preparation-unit01-execution01/result.json` retains
  the original failed ordering assertion; unit02 retains its passing correction.
- `issue-145-native-migration-scaffold01-execution01/result.json` records the
  offline EF Core 10.0.9 scaffold and its exact generated files.
- `issue-145-mfa-policy-http-native-execution08/result.json` records the unchanged
  original HTTP requirements and the additional migration case.
- `issue-145-native-repository-validation03-execution01/result.json` records the
  four native selections, commands, source binding and log/TRX hashes.

Every execution preserved all 55 unrelated primary-checkout changes. Both owned
PostgreSQL containers were removed and their exact identities verified absent.
The full Debug API build had no errors and retained the existing ImageSharp
license warning; Release/CI license acceptance is unresolved.

**This is an implementation checkpoint, not complete #145 acceptance.** The
public challenge-completion and limited enrollment/recovery flows, MFA evidence
in issued credentials and sessions, completion/risk HTTP acceptance, concurrent
whole-command failure handling, and enforcement across other sign-in schemes
still require work. Account, tenant and role changes across preparation and
ordinary credential issuance also need explicit acceptance. Signup contract
changes remain dependent on the pending #287 product decision. All original
#145 acceptance criteria, technical requirements and definition-of-done items
remain authoritative; the issue is open.

## Native public completion and session proof checkpoint — 2026-10-08

`POST /v1/auth/mfa/sign-in/complete` now completes a persisted first-factor
challenge using TOTP or a backup code. Its request contains only the opaque
challenge bearer, code, method and optional device fingerprint. Account, tenant,
token version and policy fingerprint are read from the server's challenge and
revalidated before and after provider verification. A client cannot select these
bindings. The command uses the existing transaction and durable operation-event
pipeline; its host anonymous entry is explicitly registered and rate limited.

After atomic challenge consumption, the coordinator creates immutable
`SignInMfaProof` and calls the MFA-aware authenticated session issuer. The issuer
persists `SessionMfaEvidence` before signing the access token. The new native
migration `20261008102604_AddSessionMfaEvidence` adds only
`gameguild.authentication.session_mfa_evidence`, its session foreign key,
unique challenge index and integrity constraints. This table contains proof
metadata, not challenge bearers, MFA secrets or supplied codes.

The token carries `amr=mfa`, password/OTP method entries where applicable,
`mfa_verified=true`, the original first-factor `auth_time` and original
`mfa_time`. These AMR values follow [RFC 8176](https://www.rfc-editor.org/rfc/rfc8176.html).
Ordinary issuance still carries no MFA evidence; reserved custom claims cannot
create this evidence. Refresh restores the session proof and checks the current
account, enrollment, tenant, token version and policy fingerprint. It preserves
both original times. A required/enrolled session without proof must authenticate
again; an optional, unenrolled ordinary session retains its ordinary refresh.

The original public-completion regression failed with **404** before the route
was implemented. Its test source is byte-for-byte unchanged, including
completion, replay, preserved refresh timestamps and persisted row assertions.
It now passes against the actual native API and PostgreSQL.

| Native selection | Passed | Scope |
|---|---:|---|
| Authentication UnitTests | 2,518 | Entire project, including typed proof, MFA issuer and refresh-binding cases. |
| Authorization UnitTests | 1,667 | Entire project. |
| API UnitTests architecture/security | 137 | Focused host architecture, authentication, security and migration-deployment selection. |
| API IntegrationTests MFA policy/completion/migration | 19 | TOTP and legacy/new backup codes; replay across challenges; refresh; client binding forgery; expiry; changed account/version/policy/membership; concurrent HTTP issuance; rollback after proof persistence; migration upgrade/rollback/model consistency. |
| API UnitTests configured OpenAPI documentation | 8 | Configured documents, examples and operation documentation. |
| Native OpenAPI capture | 1 | Existing export contract test produces the actual host document; the completion request schema and response contract are independently checked. |

All listed selections passed with zero failed or skipped cases. The four native
groups in validation05 executed **4,339** passing cases; validation07 retains
those production sources and extends the integration selection from 17 to 19.
These are bounded selections, not the complete API UnitTests/IntegrationTests
projects or acceptance of all #145 requirements.

Receipts under the same artifact root:

- `issue-145-native-repository-completion-baseline04-execution01/result.json`
  retains the failing public-route reproduction.
- `issue-145-public-completion-original-regression-preservation-20261008.json`
  records the unchanged original regression SHA256.
- `issue-145-native-repository-completion-validation05-execution01/result.json`
  records the full authentication/authorization and focused host/HTTP selections.
- `issue-145-native-repository-completion-totp-openapi07-execution01/result.json`
  records the 19 native HTTP/migration cases and eight documentation cases.
- `issue-145-native-repository-completion-openapi06-execution01/result.json`
  and `openapi.json` record the actual native document and public contract.
- `issue-145-public-completion-client-generation-20261008/result.json`
  binds the regenerated TypeScript client to that document.
- `issue-145-public-completion-client-validation-20261008/result.json`
  records successful build and typecheck plus **1,135/1,135** client tests,
  zero failures or skipped tests, with generated sources and primary files preserved.

All executions preserved the 55 primary-checkout changes. The exact owned
PostgreSQL containers were removed and independently verified absent.

**#145 remains open.** Limited enrollment/recovery for required but unenrolled
accounts, enforcement and complete acceptance across other sign-in schemes,
high-risk whole-flow HTTP acceptance, role/tenant changes during ordinary
issuance, external-provider sandbox evidence and the remaining original criteria
still require work. The pending #287 signup decision and #704 Release/CI license
and historical-secret scanner gates remain separate. No issue was closed by this
checkpoint.
