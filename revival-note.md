CI revival update:

- Merged current `develop` (branch now contains develop, satisfying the up-to-date requirement) and retargeted the PR base `main` -> `develop`, matching where all current integration PRs land.
- Previous stall: `Classify release surfaces` and `Repository policy` died as 24h runner-starvation timeouts with `PR Required Gate` left queued; the new head triggers a fresh full run.
- Semantic reconciliation with develop's #1361 step-up trigger work:
  - `TrustedDevicesController` now follows develop's structured 423 `IStepUpRequiredResponder` trigger verbatim (this branch's older 403 pre-created-challenge variant for trust-device was superseded by #1361 and is retired here, along with `TrustedDeviceStepUpPostgreSqlHttpTests` — develop's `TrustedDeviceSessionPostgreSqlHttpTests` covers the new flow).
  - The branch's novel scope — receipt enforcement for API-key creation and service-account secret rotation (operation-bound receipts, fail-closed consumption) — is preserved, keyed to `IStepUpReceiptService` + `StepUpBinding`, alongside develop's creator-cascade hardening in the same handler.
  - The branch's challenge hardening (WebAuthn ceremony binding, 5-slot verification budget, bounded expired-challenge cleanup) is retained; it is ahead of develop's step-up infra and complementary to the responder.
  - Docs (`step-up-authentication-flow.md` hardening section) now describe both trigger layers accurately.
- Also dropped main-lineage MFA surfaces that develop replaced (password-bearing SMS enrollment, cancel-recovery command, duplicate SMS challenge handler, strict-verify variants), aligning those files with develop.
- Generated trust-device SDK module reverted to develop's copy (only a doc-comment differed; signatures identical), so OpenAPI client consistency is preserved.
- Codacy/GitGuardian/pr_agent noise is out of scope per standing directive.

Solution builds clean locally (0 warnings, 0 errors) via the build gate.
