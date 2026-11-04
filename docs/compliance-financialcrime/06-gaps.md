# FinancialCrime (AML) — gaps

> Module `GameGuild.Compliance.FinancialCrime` · Documented against `develop` commit `60100459e6`.
> KNOWN = already tracked elsewhere (issue/doc referenced) · NEW = found by this audit. Every entry carries file:line at the documented commit.

## NEW (found by this audit)

### FC1 — The AML ingestion side has no caller; the program cannot start. **(headline)**

`IngestScreeningAsync` (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:86-135`), `RecordSignalAsync` (`:137-174`), `ConsumeDecisionAsync` (`:255-284`) and `ReadDueRescreeningsAsync` (`:307-320`) have **zero callers** in product sources (verified by search at `60100459e6` — no controller, no worker, no webhook, no connector). Cases open only inside the first two (`:118-123, :166-168`), so through any wired path the case tables stay empty forever; the risk gates therefore read `financial-crime-evidence-unavailable` for every subject and deny every value-moving operation that is switched on (`Risk:/PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs:82-87`). The exposure is double: (a) *availability* — the compliance program cannot run; (b) *rollout trap* — an operator enabling Economy value movement per `Risk:/EconomyRiskComposition.cs:36-64` gets a universal denial, not a working AML-gated flow, unless they know a screening connector must be built first.

### FC2 — Single-analyst approval: an assignee can decide their own case Approved.

Assignment requires only `OperateCompliance` and any target actor is accepted — the controller passes the caller as both assignee and later decider (`API:/Core/Controllers/EconomyComplianceAdministrationController.cs:81-101`), and `DecideCaseAsync` only checks `caseRow.AssignedTo == decision.DecidedBy` (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:210`). No second-person rule, no segregation-of-duties permission for the releasing decision (compare treasury withdrawals, which demand fresh step-up). Financial-crime case closure should require at least the step-up discipline used for compliance-hold releases in the same handler (`API:/Core/Commands/EconomyComplianceEndpointCommands.cs:22-28`).

### FC3 — `NeedsReview` cases have no closure deadline or sweeper.

A non-Approved decision parks the case in `NeedsReview` (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:232-233`) and nothing ever closes it if nobody re-engages: no SLA timer, no deadline column, no sweep worker (contrast the payments dispute deadline reminder). For AML, an abandoned review means an indefinite hold on the subject with no forced disposition.

### FC4 — No examiner-facing export.

`ReadCaseDetailsAsync` (`:355-396`) is the only aggregate read; there is no bulk export, CSV/JSON/PDF pack, or regulator delivery path (contrast `CompliancePackagingController` in Compliance.Audit). SAR/STR references are stored hash-only (`:298-304`) — correct minimization, but combined with no export the platform cannot currently *produce* evidence for an examiner beyond per-case API responses limited to 500 rows (`:322-353`).

### FC5 — The evidence-ledger schism extends the KYC N7 two-ledger problem to three ledgers.

Kinds `kyc-aml`, `financial-crime`, `trust-safety` (`Risk:/PostgreSqlComplianceEvidenceStore.cs:119-123`) are written by disjoint producers and read by disjoint consumers; the sign-in gate reads the legacy KYC table only (`API:/Core/Compliance/KycAndHoldSignInCompliancePolicy.cs:53`), and the risk gates read evidence kinds only (`Risk:/PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs:182-191`). No bridge, no precedence rule, no cross-ledger consistency check. (Extends compliance `07-gaps.md` N7; the decision of which ledger is authoritative is still open.)

### FC6 — `DisabledModules` listing is misleading and inert.

`"Compliance.FinancialCrime"` sits in `DisabledModules` (`API:/Core/Setup/ApiProductComposition.cs:114-117`) while the composition is explicitly registered via `AddEconomyCapabilityComposition` (`API:/Core/Setup/EconomyCapabilityCompositionExtensions.cs:22`) and the module class itself (`FC:/FinancialCrimeModule.cs:8-14`, `EnabledByDefault => true`) is never instantiated by any host path. The listing only affects assembly scanning for handlers/convention services — of which the module has none — so today it neither disables anything nor documents anything true. Risk: a future contributor adds a handler to the module and it silently never registers; or removes the explicit composition trusting the "disabled" label and kills the money-path AML gate.

### FC7 — The admin surface is unthrottled.

No `EnableRateLimiting` anywhere on `EconomyComplianceAdministrationController` (`:35-44`) — same shape as compliance N8 (`docs/compliance/07-gaps.md` N8). Authenticated principals with `OperateCompliance` can hammer case reads (`take` clamps only at 500, `FC:/PostgreSqlFinancialCrimeControlPlane.cs:338-341`).

### FC8 — Hold-activation uses a synthetic system actor with no traceable link.

`OpenCaseAndHoldAsync` activates holds with the literal actor `Guid.Parse("00000000-000-0000-0000-000000000001")` (`:418-421`); the only linkage back to the triggering screening/signal is the case's `Opened` event evidence hash (`:417`). Hold-store audit rows therefore attribute AML holds to "system" with no correlation id — an examiner following the chain must join through the case table manually.

## KNOWN (tracked elsewhere, re-verified here)

| # | Gap | Tracking | Re-verified |
|---|-----|----------|-------------|
| K1 | The evidence outbox `economy.compliance-evidence.published.v1` has no consumer | economy package N2 (`docs/economy/07-gaps.md`), verified by search there | yes — the only writer reachable today is the case-decision envelope (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:244`), the fan-out leg still dead |
| K2 | KYC two-ledger divergence (extended by FC5) | compliance package N7 (`docs/compliance/07-gaps.md`) | yes — unchanged at `60100459e6` |
| K3 | Compliance surfaces unthrottled (extends to FC7) | compliance package N8 | yes |
| K4 | Economy value movement opt-in / jurisdiction allowlist fail-closed defaults | `Risk:/EconomyRiskComposition.cs:17-27, 36-64` | yes — and with FC1 the allowlist can never produce an Allow verdict on the AML leg |

## Gap → fix sketch (ordering by risk)

1. **FC1** is the gating gap: wire one ingress (provider webhook or scheduled puller calling `IngestScreeningAsync` + `ReadDueRescreeningsAsync`) before any Economy value-moving capability is enabled in a jurisdiction — the rest of the control plane is ready to receive.
2. **FC2/FC3** are regulatory hygiene on the live half: step-up on Approved decisions + a `NeedsReview` deadline sweeper.
3. **FC6** rename/reconcile the DisabledModules entry or instantiate the module class for real (choose one truth).
4. **FC4** examiner export can ride the Compliance.Audit packaging pipeline (it already has CSV/JSON/XML + scheduled exports).
