# Authentication configuration and MFA reconciliation — #145

## Scope and provenance

The original #145 request has twelve acceptance criteria, technical requirements,
and a fifteen-item definition of done. The original #273 description is retained
in canonical #145. Configuration switches and an endpoint's existence alone do
not establish enforcement or successful completion of an authentication flow.

This checkpoint uses the owned checkout based on accepted develop
`d5f417328e9ff9d3fa0517796a623e310aeb1ef0`, published PR #704 head
`aa4ccb64da1ad29ae1a9166e215ac6b597263070`, and its separately guarded local
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
