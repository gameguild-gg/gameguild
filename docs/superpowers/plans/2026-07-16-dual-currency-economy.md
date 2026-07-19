# Dual-Currency Economy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILLS: use `superpowers:test-driven-development`, `superpowers:subagent-driven-development`, and `superpowers:verification-before-completion`. Every implementation task runs in a dedicated worktree from the latest verified `develop`.

**Status:** In progress. Architecture and threat model are approved; Phase 1 security prerequisites are being delivered in dependency order.

**Goal:** Implement GameGuild's internal hard-coin and soft-coin economy with immutable accounting, provenance-safe settlement, estimated and reconciled ad rewards, fraud controls, marketplace integration, payouts, and custody reporting.

**Architecture:** `docs/architecture/DUAL_CURRENCY_ECONOMY.md`

**Threat model:** `docs/security/DUAL_CURRENCY_ECONOMY_THREAT_MODEL.md`

**Source whitepaper:** `docs/papers/dual-currency-economy-whitepaper.md`

**Tech stack:** .NET 10, ASP.NET Core, PostgreSQL, Entity Framework Core, custom GameGuild CQRS, FluentValidation, xUnit, FluentAssertions, Stripe/Stripe Connect, Next.js, Playwright.

## Global Delivery Rules

- `develop` is the only integration branch.
- Never modify or merge `main` unless the user explicitly requests it.
- Every task uses a small domain-named branch and isolated worktree.
- A branch starts from the latest clean, synchronized `develop` unless the dependency graph explicitly names a prerequisite branch.
- Workers are not alone in the codebase. They must not revert unrelated changes and must stay inside assigned ownership.
- Write failing tests before production code.
- Controllers dispatch custom CQRS commands and queries. They do not access repositories directly.
- Persisted enum values are explicit and stable.
- Coin amounts use checked integers with fixed parity: `100 HC/USD`, `100,000 SC/USD`, and principal `1 HC = 1,000 SC`.
- Coins are fungible for value and eligible use, while every root mint, split, merge, transfer, conversion, fee, escrow position, and burn preserves exact fragment lineage.
- A user-facing total is a rebuildable projection only, never monetary state. An observed external deposit is a nonmonetary pending claim; authoritative confirmation atomically creates the mint posting and root lot.
- Spending and withdrawal allocate only eligible confirmed fragments in global FIFO order by `confirmed_at` then journal sequence. Payout burns the oldest at-least-120-day earned-hard fragments only after provider success.
- Soft remains noncashable and has no reverse SC-to-HC conversion despite its fixed internal parity.
- Every protected financial operation requires transaction-bound user authorization, a fresh `RiskDecisionId`, current policy, aggregate limits, sufficient reserve/margin, required Compliance/TrustSafety status, and immutable audit evidence.
- `GameGuild.Economy.Risk` owns decisions, entity graph, velocity/exposure limits, cooldowns, review queues, and risk holds. `GameGuild.Economy` remains the only monetary authority and rejects mismatched or stale decisions.
- Payout, ownership, email, MFA, identity, bank, and payout-destination changes require step-up reauthentication plus cooldown/risk review before protected value movement.
- The general runtime role cannot write journal tables. Only the constrained `GameGuild.Economy` security-definer posting interface writes them through registered template versions.
- Only the schema-rollup task edits centralized API migrations and the EF model snapshot during parallel feature rounds.
- Features remain disabled until their schema, security, reconciliation, and operational gates pass.
- Every candidate branch updates from current `develop` and passes the full integrated gate before it is merged; passing only a focused project is insufficient.
- Every new Economy assembly enforces 100% line, branch, and method coverage in CI. PostgreSQL concurrency, provider contract, and browser journeys remain additional gates and cannot be replaced by unit coverage.
- After a verified merge into `develop`, remove the worktree and delete the local and remote task branch immediately.
- Keep `develop` deployable at every merge boundary.

## Worktree Layout

Use ignored paths under `.tmp/`:

```text
.tmp/economy-wallet-authorization
.tmp/economy-provider-schema-expand
.tmp/economy-stripe-webhooks
.tmp/economy-authoritative-pricing
.tmp/economy-production-guards
.tmp/economy-ci-gates
.tmp/economy-security-schema
.tmp/economy-domain-contracts
.tmp/economy-ledger-kernel
.tmp/economy-ledger-schema
.tmp/economy-foundation-schema-rollup
.tmp/economy-balance-projections
.tmp/economy-monetary-policy
.tmp/economy-risk-engine
.tmp/economy-core-reserve-authority
.tmp/economy-financial-crime
.tmp/economy-trust-safety
.tmp/economy-hard-funding
.tmp/economy-ad-rewards
.tmp/economy-bounties
.tmp/economy-marketplace
.tmp/economy-ai-costs
.tmp/economy-capability-bootstrap
.tmp/economy-capabilities-schema
.tmp/economy-disputes-debt
.tmp/economy-connect-payouts
.tmp/economy-treasury
.tmp/economy-admin-withdrawal
.tmp/economy-treasury-schema
.tmp/economy-api-activation
.tmp/economy-user-experience
.tmp/economy-ad-reward-experience
.tmp/economy-operations-console
.tmp/economy-operations-runbook
.tmp/economy-release-verification
.tmp/economy-shadow-rollout
.tmp/economy-activation
```

Do not install dependencies independently in every worktree unless required. Reuse package stores and NuGet caches. Remove completed worktrees promptly to protect disk capacity.

## Integrated Verification Commands

Task 0.2 wires these commands into CI for both `develop` and pull requests. The gate discovers Economy projects that exist in the candidate commit and compares them to a checked-in required-project manifest; the commit that introduces an assembly must introduce its test project and manifest entry in the same change. The existing whole-solution suite always runs.

```bash
bash scripts/ci/verify-economy.sh
```

Task 0.2 creates that fail-fast Bash script with strict error handling and a `run` wrapper that propagates every nonzero native exit code. It restores/builds/tests the whole solution, discovers required Economy unit/integration projects, writes every TRX/Cobertura/Vitest/Playwright result under `artifacts/test-results`, publishes the API, starts the published API against disposable PostgreSQL on an ephemeral port, waits for readiness, captures a deterministic OpenAPI artifact, regenerates the client from that artifact, diff-checks generated output, builds/tests the client and web, and finally parses all evidence for skips/zero-test suites.

Expected result at a merge gate: every step exits `0`; the required-project manifest has no missing or zero-test project; TRX/Vitest/Playwright parsing reports no skipped, pending, todo, or undiscovered required suite; generated-client output is clean; each new Economy coverage report states 100% line/branch/method; PostgreSQL role/concurrency tests use a fresh database; and browser journeys produce no failed request or console error attributable to the economy flow. The script always stops the ephemeral API/database in `finally`.

## Parallelization Model

### Round 1: Existing Security Preconditions

Task 1.0 first merges the additive provider-mapping/inbox schema required by later consumers. Four agents may then work in parallel because their primary write scopes are separate:

| Lane | Branch                                    | Exclusive ownership                                                                                                                   |
| ---- | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| A    | `fix/economy-wallet-authorization`        | Payments wallet controller, wallet authorization policies, focused tests                                                              |
| B    | `fix/economy-stripe-webhook-verification` | Billing Stripe webhook ingress/service, signature tests                                                                               |
| C    | `fix/economy-authoritative-pricing`       | Payment command pricing validation, Orders/Products module logic, focused tests; no shared API composition files                      |
| D    | `fix/economy-production-guardrails`       | Shared API composition/startup, Orders route activation after Lane C merges, deployment configuration, simulation guards, smoke tests |

Merge one at a time after review. Lane C merges before Lane D's final API-composition update. Before each merge, update the candidate from current `develop`, rerun the strict API build plus combined Commerce security test set, merge, and rerun the same gate on the resulting `develop` SHA. A centralized constraint/backfill rollup follows the four code branches.

### Round 2: Economy Foundation

Foundation tasks are mostly sequential because they establish contracts and database invariants:

```text
domain contracts
    -> ledger kernel
        -> persistence model and writer contract
            -> balance/lot projections
                -> monetary policy and holds
                    -> risk engine and protected-operation contract
                        -> foundation schema/role rollup
                            -> core reserve authority
```

Independent review agents may run in parallel with implementation, but only one worker edits the core module at a time.

### Round 3: Product Capabilities

After the core posting API, policy contracts, and reserve authority are merged, first merge one bootstrap branch that creates and registers every disabled capability/test project in API composition. Task 3.1 then implements and merges the sole hard-funding/provider-source path with exclusive Billing/Payments/funding ownership and no shared Core or catalog edits. After Task 3.1 is deleted, Tasks 3.2-3.5 can run in parallel without journal or shared-composition ownership:

| Lane | Branch                                | Exclusive ownership                                          |
| ---- | ------------------------------------- | ------------------------------------------------------------ |
| A    | `feat/economy-ad-rewards`             | `GameGuild.Economy.AdRewards` and its tests                  |
| B    | `feat/economy-bounties`               | `GameGuild.Economy.Bounties` and its tests                   |
| C    | `feat/economy-marketplace-settlement` | Products/Orders currency policy and settlement orchestration |
| D    | `feat/economy-ai-cost-accounting`     | AI usage-cost facts and economy charge integration           |

These branches must not edit centralized migrations, the API project-reference list, default module configuration, or shared infrastructure composition. A schema-rollup branch generates and tests the integrated migration after the model branches merge.

### Round 4: Money Movement And Treasury

Payout and treasury work is intentionally less parallel because both consume holds, maturity, provider reconciliation, and reserves:

```text
disputes and debt
    -> Connect/KYC payout lifecycle
        -> treasury, reserves, and custody
            -> admin withdrawal
```

Task 4.3 must merge and its worktree/branch must be deleted before Task 4.4 is created. Task 4.3 owns Treasury reserve/custody implementation; Task 4.4 owns only admin-withdrawal workflow files and consumes the merged public Treasury contracts.

### Round 5: Activation And UX

API activation begins only after integrated schema and security gates pass. Task 5.1 owns generated client contracts plus shared API/web composition and navigation, and must merge first. Tasks 5.2-5.4 then use separate worktrees from that resulting `develop` SHA and own disjoint feature route/component/test directories; they do not edit shared package metadata, generated client output, navigation, or common browser fixtures. Shared E2E-fixture integration is serialized in Task 6.1. Browser feature work can run alongside operational runbook work.

---

## Phase 0: Canonical Documentation

### Task 0.1: Whitepaper And Architecture

**Branch:** `docs/dual-currency-economy`

**Produces:** Repository copy of the source whitepaper, architecture, threat model, documentation index, and this plan.

- [x] Preserve the source whitepaper byte-for-byte.
- [x] Document `100 HC/USD`, `100,000 SC/USD`, exact `1 HC = 1,000 SC` principal conversion, and noncashable soft status.
- [x] Document package ownership and dependency direction.
- [x] Document confirmed-mint semantics, fungible fragment lineage, global FIFO allocation, root reversal recovery, reserve-unit correction, and custody separation.
- [x] Document fraud controls and production gates.
- [x] Run independent architecture, security, and plan reviews.
- [x] Commit, merge into `develop`, push, remove worktree, and delete branch.
- [x] Obtain stakeholder approval before Task 1.1.

### Task 0.2: CI And Runtime Baseline

**Branch:** `build/economy-ci-gates`

**Produces:** A .NET 10, warning-clean, PostgreSQL-backed, coverage-enforced CI baseline capable of proving every later economy gate.

- [x] Update CI setup to the repository's pinned .NET 10 SDK and fail on compiler/analyzer warnings for Economy and touched Commerce assemblies.
- [x] Run CI for `develop` and pull requests, align pnpm with the repository-declared `pnpm@10.0.0`, and remove the workflow's current .NET 9/pnpm 9 drift.
- [x] Enable deterministic coverage collection and fail any new Economy assembly below 100% line, branch, or method coverage.
- [x] Add a checked-in required-project manifest, dynamic discovery for Economy projects present in each commit, and fail on missing, zero-test, skipped, pending, or todo suites.
- [x] Preserve the existing whole-solution API test run in addition to focused Economy coverage gates.
- [x] Add disposable PostgreSQL service execution for migration, role, trigger, and concurrency tests.
- [x] Add one strict fail-fast verification script that checks every native exit code and writes all test/coverage/browser evidence under one asserted artifact tree.
- [x] Extend the client generator to accept a captured OpenAPI artifact; start the published API on an ephemeral port with readiness handling, capture that artifact, regenerate, and diff-check deterministically.
- [x] Add exact API publish, provider-contract, frontend build, and Playwright smoke commands.
- [x] Prove the gate fails with one intentional warning, one lowered coverage result, and one PostgreSQL test failure before restoring green state.
- [x] Merge and delete this branch before any Phase 1 branch is created.

## Phase 1: Existing Security Preconditions

### Task 1.0: Provider Security Expand Migration

**Branch:** `fix/economy-provider-schema-expand`

**Produces:** Additive nullable inbox/provider mapping fields, scoped indexes, and compatibility reads required before webhook and pricing consumers deploy.

- [x] Add the centralized expand migration before Tasks 1.1-1.4 branches are created.
- [x] Preserve compatibility with current code and data; add no destructive or prematurely non-null constraint.
- [x] Test migration up/down/current paths on real PostgreSQL.
- [x] Merge, verify, and delete the branch before parallel Phase 1 work begins.

### Task 1.1: Wallet Object-Level Authorization

**Branch:** `fix/economy-wallet-authorization`

**Produces:** No authenticated user can read or mutate another wallet; generic value-minting endpoints are not public.

- [x] Write authorization matrix tests for self, other user, tenant admin, platform admin, and missing tenant/context.
- [x] Derive self-service wallet identity from actor context.
- [x] Require explicit platform permission for list/freeze/unfreeze/admin operations.
- [x] Remove or disable public arbitrary credit/debit operations.
- [x] Add audit events for privileged wallet actions.
- [x] Run Payments unit/integration tests and strict API build.

### Task 1.2: Stripe Webhook Authenticity And Inbox

**Branch:** `fix/economy-stripe-webhook-verification`

**Produces:** Valid signatures are durably accepted exactly once and bound to the correct local financial object; invalid or unpersisted events fail correctly.

- [x] Write forged, stale, wrong-secret, malformed, duplicate, and database-failure tests.
- [x] Verify Stripe signature and timestamp before processing.
- [x] Persist a valid event to the durable inbox before returning 2xx.
- [x] Scope idempotency by provider account, environment, endpoint, and event ID.
- [x] Bind events to immutable provider-object mappings and validate livemode/environment, Connect account, tenant, amount, currency, cumulative refund/dispute totals, and supported schema versions.
- [x] Enforce application-level provider/environment/account/object/monetary-leg uniqueness and cumulative confirmed/refunded/disputed amount bounds without trusting internal IDs alone; Task 1.5 owns the post-backfill atomic database constraints.
- [x] Preserve retryable status for processing failures.
- [x] Minimize and classify retained payload data.
- [x] Run Billing and Payments tests plus provider contract tests.

**Verification:** Billing unit `259/259`, Billing endpoint integration `7/7`, Payments unit `829/829`, Payments integration `40/40`, and strict API build `0` warnings/errors.

### Task 1.3: Authoritative Payment Pricing

**Branch:** `fix/economy-authoritative-pricing`

**Produces:** Client requests cannot control charged amount, currency, tenant, product, order, or subscription state; the minimum secure Orders path is active before order charging is allowed.

- [x] Write underpayment, currency substitution, tenant mismatch, stale-price, and invalid-order tests.
- [x] Implement and integration-test the minimum Orders payment path inside Orders/Products ownership; keep unrelated Orders capabilities disabled and leave shared API composition to Task 1.4.
- [x] Resolve order totals from immutable server-side price snapshots and reject any order charge without an authoritative payable order.
- [x] Resolve subscription charges from immutable server-side invoice/subscription snapshots without requiring an Order ID.
- [x] Validate subscription payment amount and billing period.
- [x] Ensure refund/cancel/retry operations enforce ownership and state.
- [x] Run Orders, Products, Payments, Billing, and Subscriptions tests.

**Verification:** Orders unit `189/189`, Orders integration `8/8`, Products unit `554/554`, Products integration `10/10`, Payments unit `872/872`, Payments integration `41/41`, Billing unit `259/259`, Billing integration `7/7`, Subscriptions unit `739/739`, Subscriptions integration `62/62`, and strict API build `0` warnings/errors. Total focused tests: `2,741/2,741`.

### Task 1.4: Production Fail-Closed Configuration

**Branch:** `fix/economy-production-guardrails`

**Produces:** Staging/production cannot start value-moving capabilities with simulation or missing secrets, and shared API composition activates only the minimum verified Orders path.

- [x] Write startup validation tests.
- [x] Fail startup when payment simulation is enabled outside Development/Test.
- [x] Fail startup when required webhook/provider secrets are absent.
- [x] Separate migration-role and runtime-role configuration.
- [x] After Task 1.3 merges, activate its minimum Orders path in shared API composition and prove unrelated Orders routes remain disabled.
- [x] Add health/readiness details for provider and inbox state without exposing secrets.
- [x] Update deployment smoke documentation and tests.

Verification: targeted API/Commerce suites, strict touched-project builds, API publish, client tests/build, compose validation, and the integrated Economy gate passed with 262 evidence files on 2026-07-18.

### Task 1.5: Security Schema Rollup

**Branch:** `fix/economy-security-schema`

**Produces:** Backfill, uniqueness/non-null enforcement, and safe contract cleanup after the Task 1.0 expansion and verified application cutover.

- [x] Generate the centralized constraint/backfill migration only after Tasks 1.1-1.4 are merged.
- [x] Verify Task 1.0 columns/indexes are populated before enforcing non-null or uniqueness constraints.
- [x] Test migration up/down/current paths and duplicate provider events against real PostgreSQL.
- [x] Run strict API publish and the full Commerce security suite on the exact post-migration `develop` SHA.
- [x] Keep destructive contract steps in a later release after production compatibility is proven.

Verification: the additive `20260718171325_RollupProviderSecurityConstraints` migration passed up/down/current, duplicate-event, partial-mapping, and concurrent-claim tests against PostgreSQL. Commerce and API regression suites passed `2,917/2,917`, deployment smoke passed `3/3`, the shell evidence gate passed `16/16`, and strict API build/publish completed with zero warnings and zero errors on integrated `develop` SHA `cc623d358`.

### Phase 1 Gate

- [x] Tasks 1.0-1.5 are merged in dependency order, verified, and deleted.
- [x] Full Commerce security regression passes.
- [x] API strict build has zero warnings/errors.
- [x] No public endpoint can directly mint or arbitrarily debit wallet value.

Phase 1 endpoint audit: wallet controllers expose self-service creation/read/lock operations and permission-gated administration of settings, closure, freeze, and audit reads. Legacy add, deduct, transfer, and ledger commands are internal-only and have no public controller or minimal-API route.

## Phase 2: Economy Foundation

### Task 2.1: Domain Contracts And Posting Matrix

**Branch:** `feat/economy-domain-contracts`

**Produces:** `GameGuild.Economy` project skeleton, fixed-parity integer amount types, explicit enums, fungible-fragment contracts, typed posting requests/results, and exhaustive posting-matrix tests.

- [x] Add checked `HardCoinAmount`, `SoftCoinAmount`, `UsdNanoAmount`, fixed parity constants, currency/provenance/account-code types, and overflow/property tests.
- [x] Define wallet, root mint, posting, fragment allocation/lineage, hold, reserve, policy, and idempotency contracts.
- [x] Expose no authoritative mutable balance setter; displayed quantities are rebuildable projections over source-stamped lots, fragment lineage, and exact partial allocations.
- [x] Encode exact principal `1 HC = 1,000 SC` hard-to-soft conversion only; fees are separate and no reverse contract exists.
- [x] Add immutable type/version posting/transition templates for confirmed top-up mint, full/partial provider reversal, spend, conversion, system-backed grant, burn, escrow, reclaim, refund, payout reservation/success/failure, and admin-withdrawal reservation/success/failure. Observation/failure before confirmation has source evidence but no monetary posting.
- [x] Test zero-sum behavior, exact account shape/sign/provenance/authority/caps, stable serialization/hash input, and rejection of balanced but invalid groups.
- [x] Add module project references without enabling routes or persistence.

Verification: `GameGuild.Economy` is a domain-only assembly with no EF Core reference, controller, endpoint, mutable balance setter, or reverse soft-to-hard contract. Its `60/60` tests pass at `100%` line, branch, and method coverage; strict API build and Release publish complete with zero warnings and zero errors on integrated `develop` SHA `70299dfd1`.

### Task 2.2: Immutable Ledger Kernel

**Branch:** `feat/economy-ledger-kernel`

**Produces:** Posting aggregate, immutable source stamps and credit lots, journal entries, exact fragment allocations/lineage, chain head, idempotency, outbox, and transactional posting service.

- [x] Write posting, idempotency, partial source-allocation, deterministic root-range split/merge lineage, mixed-root reversal, exact restoration/retirement, and concurrency unit tests first.
- [x] Implement append-only entities without `EntityBase`; observed funding creates source evidence only, while every confirmed mint/derived credit requires a hashed source stamp and stores authoritative `confirmed_at` plus original `matures_at`.
- [x] Implement global confirmed-fragment FIFO by `confirmed_at` then stable journal sequence; provenance never reorders eligible spending.
- [x] Map every output lot to exact parent fragments and prove per-currency lineage conservation across recipient, fee, escrow, conversion, and retirement outputs.
- [x] Carry immutable ordered root trace-unit intervals; preserve unrelated ranges in mixed-root lots; select partial cumulative reversals as deterministic nonoverlapping intervals.
- [x] Add a per-root reversal epoch/fence checked by every allocator so descendant discovery cannot race spend, transfer, conversion, escrow, or payout.
- [x] Implement global chain-head lock and deterministic entry hashing.
- [x] Commit posting, allocation, lineage, projection update, idempotency result, and outbox atomically.
- [x] Expose typed core commands only; no generic public posting endpoint.
- [x] Sign periodic chain heads with independent credentials and persist anchors through an immutable-storage outbox contract; support on-demand anchors that also bind a canonical dispatch-snapshot hash.

Verification: the immutable ledger kernel and its atomic in-memory transaction boundary are integrated on `develop` SHA `4e6cae8f3`. `GameGuild.Economy.UnitTests` passes `124/124` tests with zero skips and `100%` line, branch, and method coverage. Strict API build and Release publish complete with zero warnings and zero errors. Persistence mappings and the constrained database writer remain explicitly scoped to Task 2.3.

### Task 2.3: Persistence Model And Constrained Writer Contract

**Branch:** `feat/economy-ledger-schema`

**Produces:** Complete EF mappings and a versioned security-definer posting-interface contract without editing centralized migrations.

- [x] Map all foundation models and indexes, including root ranges/reversal state and dispatch snapshot/external anchors, without generating a migration in this branch.
- [x] Add indexes and unique constraints for idempotency, sequence, internal source leg, provider monetary leg, reference, allocation, lineage/root ranges, reversal epochs, and root confirmed lot.
- [x] Define the registered posting-template catalog and procedure input/output contract.
- [x] Define separate migration, general runtime, and economy-writer role privileges.
- [ ] Add SQL contract tests proving balanced but unauthorized shapes, absent/reused/mutated source stamps, unconfirmed external mint, cumulative provider over-credit, forged confirmation time, early maturity, over-allocation, overlapping root ranges, stale reversal epoch, and lineage nonconservation are rejected by the writer interface.
- [x] Leave all centralized migration and model-snapshot edits to Task 2.7.

Verification: persistence mappings and constrained-writer contracts are integrated on `develop` SHA `29033bbbf`. `GameGuild.Economy.UnitTests` passes `149/149` tests with zero skips and `100%` line, branch, and method coverage. The strict Release API build and publish complete with zero warnings and zero errors. Executable PostgreSQL rejection tests remain open as the final Task 2.3 item; no migration or centralized model snapshot was changed.

### Task 2.4: Balance And Lot Projections

**Branch:** `feat/economy-balance-projections`

**Produces:** Fast pending-claim/confirmed-fragment reads, confirmed-fragment FIFO availability, full recomputation, mismatch containment, and wallet query contracts.

- [x] Test total, pending-confirmation, confirmed, immature-earned, held, spendable, and withdrawable calculations.
- [x] Prove pending funding is visible as a nonmonetary claim, creates no journal credit/lot, and contributes zero to spend, transfer, conversion, escrow, and payout authorization.
- [ ] Prove FIFO uses `confirmed_at` then journal sequence, preserves residual-fragment rank, skips ineligible fragments, and serializes concurrent spend/payout allocators; pending claims never enter selection.
- [x] Implement synchronous projection updates inside posting transactions.
- [x] Implement full journal recomputation and comparison.
- [x] Rebuild aggregate views exclusively from source evidence, lots, lineage, allocations, holds, and retirements; expose no scalar monetary setter.
- [x] Enforce lower available/withdrawable value on disagreement.
- [x] Add wallet review state and reconciliation alerts.
- [ ] Add projection corruption and recovery PostgreSQL tests.

Verification: immutable-fact wallet rebuilding, journal recomputation, lower-value containment, review alerts, atomic projection updates, and concurrent source-fragment transfer allocation are integrated on `develop` SHA `dadf79e28`. `GameGuild.Economy.UnitTests` passes `170/170` tests with zero skips and `100%` line, branch, and method coverage; strict Release Economy/API builds and API publish complete with zero warnings and zero errors. The FIFO requirement remains open only for the future payout allocator, and PostgreSQL corruption/recovery remains open until Task 2.7 installs the integrated schema and writer procedure.

### Task 2.5: Monetary Policy, Holds, And Maturity

**Branch:** `feat/economy-monetary-policy`

**Produces:** Immutable effective-dated fee/rate policy, fixed 120-day earned-hard maturity, hold state machine, and account/debt restrictions.

- [x] Write effective-date, source-stamp, confirmation, exact-120-day-boundary, new-lot-clock, rounding, hold-race, and maturity tests.
- [x] Implement versioned conversion fees, ad-reward inputs, service prices/margins, limits, and maturity settings; fixed parity is a non-configurable domain invariant.
- [x] Set every new earned-hard lot to `matures_at = confirmed_at + 120 days`; purchased hard and all soft remain permanently noncashable.
- [x] Provide no accelerated-release command or permission. Correct bad stamps/times only through reversal and a new correctly stamped lot.
- [x] Implement partial and full holds with append-only hold events.
- [x] Prove an active hold remains effective and payout-blocking before, at, and after the 120-day boundary; maturity never releases or shortens a hold.
- [ ] Serialize hold, spend, refund, and payout operations on lot projections.
- [x] Add explicit account freeze and debt restrictions.

Verification: immutable effective-dated policy, fixed `100 HC/USD` and `100,000 SC/USD` parity, estimated ad-reward safety reserves, stressed service-margin validation, exact source-confirmed 120-day earned-hard maturity, append-only partial/full holds, account/debt restrictions, and shared hold/transfer serialization are integrated on `develop` SHA `f12665372`. `GameGuild.Economy.UnitTests` passes `190/190` tests with zero skips and `100%` line, branch, and method coverage; strict Release Economy/API builds and API publish complete with zero warnings and zero errors. The shared serialization item remains open only for refund and payout operations, which do not exist until their later scheduled tasks.

### Task 2.6: Risk Engine And Protected-Operation Contract

**Branch:** `feat/economy-risk-engine`

**Produces:** `GameGuild.Economy.Risk`, transaction risk decisions, entity graph contracts, aggregate limits, protected-change cooldowns, Trust/Safety and FinancialCrime input contracts, review queue, and Core decision-validation hooks.

- [x] Write tests for missing, expired, reused, wrong-outcome, actor-mismatched, destination-mismatched, source-root-mismatched, amount-mismatched, stale-policy, stale-reserve, stale-kill-switch, stale-counter, and stale-graph decisions.
- [x] Add explicit `Allow`, `Challenge`, `Hold`, `Review`, and `Deny` outcomes; prove only `Allow` can authorize value movement and `Hold` can only create or preserve nonspendable holds.
- [x] Define risk-decision snapshots that bind actor, operation template, amount, currency legs, source roots, destination, provider reference, policy version, reserve version, feature flag, kill-switch epoch, entity-cluster evidence, and reason codes.
- [x] Implement privacy-preserving entity graph contracts for account, tenant, KYC identity, payment instrument, bank account, payout destination, device-risk token, IP/prefix, ASN, referral, project, product, marketplace counterparty, and provider object using opaque or KMS-HMAC references.
- [x] Implement aggregate exposure and velocity limit contracts across wallet, identity cluster, source root, destination, counterparty pair, product, tenant, provider account, device/IP/ASN cluster, and global loss budget.
- [x] Implement protected-change cooldown policies for password reset, MFA reset, email change, ownership transfer, identity update, bank/payout-destination change, new-device login, and high-risk session elevation.
- [x] Add transaction-bound reauthentication evidence requirements for payout, destination change, ownership transfer, hold release, high-risk settlement, and administrative adjustment.
- [x] Add review-case, appeal, manual decision, dual-approval, and immutable audit contracts without exposing risk bypass details in user-facing responses.
- [x] Add `Compliance.FinancialCrime` and `TrustSafety` input contracts so Core can fail closed when required status is blocked, stale, unknown, or unauditable.
- [x] Wire Core protected posting commands to require and validate `RiskDecisionId`; keep all value-moving capabilities disabled until schema rollup verifies persistence and counters.

Verification: the protected-operation risk engine is integrated on `develop` SHA `09e87379a`. It includes immutable context-bound decisions, one-time exact replay, opaque/HMAC entity graph contracts, serialized multi-dimensional exposure counters, cooldown and reauthentication evidence, FinancialCrime and TrustSafety fail-closed inputs, independent review/appeal/dual-approval workflows, redacted audit output, and the Core posting gate. The gate remains intentionally closed until Task 2.7 verifies both schema persistence and counter constraints. `GameGuild.Economy.UnitTests` passes `223/223` tests with zero skips and `100%` line, branch, and method coverage; strict Release Economy/API builds and API publish complete with zero warnings and zero errors.

### Task 2.7: Foundation Schema, Roles, And Immutability Rollup

**Branch:** `feat/economy-foundation-schema-rollup`

**Produces:** One additive foundation migration, database roles, constrained writer/transition procedures, mutation-denial triggers, immutable-anchor persistence, risk-decision persistence/counter constraints, and real PostgreSQL verification after Tasks 2.1-2.6 are merged.

- [ ] Generate the centralized migration and model snapshot from the current integrated `develop`.
- [ ] Install grants so the general runtime role cannot directly mutate any immutable or integrity-bearing mutable economy table; the writer role can only execute registered procedures.
- [ ] Harden security-definer ownership, explicit execute ACLs, pinned trusted `search_path`, schema-qualified references, caller capability validation, and absence of caller-selected SQL.
- [x] Add update/delete denial triggers plus source/provider uniqueness, cumulative provider amount, one-root-lot, maturity, allocation, lineage/root-range conservation, reversal-epoch, template-shape, limit, and reserve constraints.
- [x] Add risk-decision uniqueness, single-use consumption, decision-operation binding, aggregate-counter, cooldown, hold, review-case, and audit-evidence constraints.
- [x] Run migration up/down/current tests on disposable PostgreSQL.
- [x] Verify concurrent posting, rollback, role denial, procedure enforcement, chain anchoring, projection rebuild, stale-decision rejection, and aggregate-counter oversubscription behavior.

### Task 2.8: Core Reserve Authority

**Branch:** `feat/economy-core-reserve-authority`

**Produces:** Core-owned reserve head, exclusive external-asset allocation lock, version/freshness contract, authorization epoch, and conservative formula contracts required before any capability can request value issuance.

- [x] Write reserve-version race, stale/unknown input, duplicate asset allocation, formula-boundary, and authorization-epoch tests first.
- [x] Implement guarded reserve proposal validation and atomic activation in Core; Treasury may propose observations/calculations but cannot mutate active state.
- [x] Define deterministic hard face-value and soft face-value/stressed-portfolio formula contracts, including open authorizations, worst-case unreserved service mix, checked ceiling arithmetic, and `0 <= margin_ppm < 1,000,000`.
- [x] Require every issuance, conversion, settlement, reversal, payout, and withdrawal template to name and lock an active reserve version and matching risk decision even while production capabilities remain disabled.
- [x] Provide test fixtures only, not a production reserve override. Real external observations and reconciliation arrive in Task 4.3.
- [x] Merge and delete this branch before Task 3.0.

Verification: Core now owns an unforgeable reserve authorization lock, deterministic conservative formulas, compare-and-swap activation, monotonic reserve and authorization epochs, exclusive external-asset allocation, and fail-closed protected-posting bindings. PostgreSQL persists one active reserve head through a security-definer procedure with pinned `search_path`, restricted table ACLs, advisory transaction serialization, and migration up/down coverage. `GameGuild.Economy.UnitTests` passes `244/244` tests with zero skips and `100%` line, branch, and method coverage; `GameGuild.API.UnitTests` passes `187/187`, the focused PostgreSQL migration suite passes `2/2`, the EF model reports no pending changes, and strict Release API build completes with zero warnings and zero errors.

### Phase 2 Gate

- [ ] Journal operations are balanced, idempotent, immutable, and chain-verified.
- [ ] Database role and trigger mutation tests pass.
- [ ] Balanced but unauthorized issuance and conversion shapes fail at the database writer boundary.
- [ ] Projection recompute and lower-value containment pass.
- [ ] Core reserve authority rejects stale/invalid proposals and serializes every value capability against one active version.
- [ ] Core rejects protected operations without a fresh matching risk decision, current aggregate counters, valid cooldown state, and current Compliance/TrustSafety inputs.
- [ ] No provider or leaf module can write journal tables directly.
- [ ] Core module coverage meets the repository's enforced threshold with all financial branches exercised.

## Phase 3: Funding And Product Capabilities

### Task 3.0: Capability Project Bootstrap

**Branch:** `feat/economy-capability-bootstrap`

**Produces:** Disabled AdRewards, Bounties, Payouts, Treasury, FinancialCrime, and TrustSafety projects plus test projects, API references, module registry entries, and shared composition hooks required by later parallel branches.

- [x] Create projects and references without enabling any value-moving route.
- [x] Register `GameGuild.Economy.Risk`, `GameGuild.Compliance.FinancialCrime`, and `GameGuild.TrustSafety` contracts in composition with value-moving decisions disabled by default.
- [x] Add disabled module entries and empty composition hooks following current modular-monolith conventions.
- [x] Add smoke tests proving the API composes with every capability disabled.
- [x] Merge and delete this branch before Task 3.1 is created; Tasks 3.2-3.5 wait for Task 3.1 to merge.

**Verification:** `GameGuild.Economy.UnitTests` passes 247/247 with 100% line, branch, and method coverage. Each of the six new capability test projects passes with 100% line, branch, and method coverage. `GameGuild.API.UnitTests` passes 189/189 with zero skips. The API Release build succeeds with warnings treated as errors and reports 0 warnings and 0 errors.

### Task 3.1: Hard-Coin Funding And Conversion

**Branch:** `feat/economy-hardcoin-funding`

**Dependency:** Run serially after Task 3.0 and merge/delete it before creating Tasks 3.2-3.5.

**Produces:** Stripe top-up to purchased hard, provider saga, and hard-to-soft conversion.

- [x] Test observed-visible-pending, pre-confirmation authorization denial, provider success, duplicate webhook, timeout recovery, failed/expired capture, refund, and concurrent confirmation-versus-failure/expiry/recovery.
- [x] Map authoritative USD minor units one-to-one to hard-coin units.
- [x] Record an observed deposit as an immutable source stamp/evidence plus visible nonmonetary pending claim; prove no mint posting or credit lot exists yet.
- [x] On authoritative provider confirmation, atomically close pending, post the mint, and create exactly one root purchased-hard lot; on failure/expiry, append terminal evidence without monetary posting.
- [x] Prove every terminal race has exactly one source terminal state and at most one root mint under real PostgreSQL concurrency.
- [x] Create and verify append-only source-stamp evidence for observed, confirmed, failed, expired, disputed, and reversed states.
- [x] Enforce globally unique provider monetary-leg binding and cumulative `minted <= confirmed`, `refunded/disputed <= provider totals` invariants.
- [x] Implement full and partial top-up refund/chargeback templates that traverse hard and converted-soft descendants, balance each currency leg, never remint a retired root, and exactly partition root-equivalent recovery, responsible debt/receivable, and policy-versioned loss.
- [x] Implement exact principal conversion `1 HC = 1,000 SC`; retire the hard liability, reclassify backing to soft reserve, and post any configured hard fee separately.
- [x] Implement system-backed grants only from an approved platform hard debit in exact `1 HC = 1,000 SC` blocks.
- [x] Serialize issuance against fresh fixed-parity reserve headroom, matching risk decision, aggregate limits, source-root exposure, and protected-change cooldown state.
- [x] Prove there is no soft-to-hard route or command.

**Verification:** `GameGuild.Economy.UnitTests` passes 320/320 with zero skips and 100% line, branch, and method coverage, including real PostgreSQL terminal-race and provider-constraint tests. `GameGuild.Commerce.Payments.UnitTests` passes 906/906 with zero skips; the new Stripe funding adapter has 100% line and branch coverage. The API Release build succeeds with warnings treated as errors and reports 0 warnings and 0 errors. The integrated schema migration remains intentionally deferred to Task 3.6.

### Task 3.2: Ad Rewards And Reconciliation

**Branch:** `feat/economy-ad-rewards`

**Produces:** `GameGuild.Economy.AdRewards`, verified sessions, estimated reward quotes, fraud decisions, provider batches, and forward-only reconciliation.

- [x] Write token, provider-proof, timing, visibility, replay, velocity, quota, loss-budget, rounding, split-versus-batch, duplicate, and reconciliation tests.
- [x] Implement network policy and cold-start state.
- [x] Implement signed short-lived single-use sessions.
- [x] Convert conservative USD nanos at exactly `100,000 SC/USD` through one rational numerator/final division, retaining the canonical-denominator remainder in an idempotent wallet-level accumulator that survives network/policy retirement.
- [x] Atomically consume completion token, user/device/network/global counters, fraud-loss budget, and reserve headroom before posting reward.
- [x] Require a matching `RiskDecisionId` and entity-graph exposure check before reward issuance; related-account/device/IP/ASN/referral clusters consume aggregate limits together.
- [x] Require independent provider-side completion proof for immediate mint; disable unsupported networks or defer minting until an independently verified report. Fail closed when proof, fraud service, counters, reports, loss budget, or reserve snapshot are unavailable/stale.
- [x] Implement report import and unique reconciliation versioning.
- [x] Update future eCPM/buffer/ranking without changing prior rewards.
- [x] Add network and global kill switches.

**Verification:** `GameGuild.Economy.AdRewards.UnitTests` passes 57/57 with zero skips and 100% line, branch, and method coverage. `GameGuild.Economy.UnitTests` passes 323/323 with zero skips and 100% line, branch, and method coverage. The API Release build succeeds with warnings treated as errors and reports 0 warnings and 0 errors. Integrated persistence and public route composition remain intentionally deferred to Task 3.6.

### Task 3.3: Bounty Escrow

**Branch:** `feat/economy-bounties`

**Produces:** Bounty posting, eligibility-at-claim, escrow, claim, expiry, reclaim, fee, and provenance restoration.

- [ ] Write claim-versus-reclaim race tests.
- [ ] Test repeated bounty credits with independent authoritative `confirmed_at`, exact `matures_at = confirmed_at + 120 days`, and no poster/payer maturity inheritance.
- [ ] Preserve deposited lots and original provenance.
- [ ] Validate claimant eligibility inside the locked terminal transaction.
- [ ] Require risk approval against related-account, referral, device, payment, payout-destination, and counterparty-pair exposure before claim settlement.
- [ ] Create earned proceeds on successful claim.
- [ ] Create a new source stamp and independent 120-day earned-hard lot on a hard bounty claim.
- [ ] Restore original provenance minus fee on reclaim.

### Task 3.4: Marketplace Currency Policy And Settlement

**Branch:** `feat/economy-marketplace-settlement`

**Produces:** Product accepted-currency policy, fixed-mix snapshots, Orders reactivation, atomic settlement, fees, refund holds, and entitlement coordination.

- [ ] Write hard-only, soft-only, either, fixed-mix, insufficient-leg, replay, and refund tests.
- [ ] Test repeated seller/platform-fee credits with independent authoritative `confirmed_at`, exact 120-day clocks where cash-out eligible, and no buyer-lot maturity inheritance.
- [ ] Add versioned Product currency policy and prices.
- [ ] Reactivate Orders only after its integration/security tests pass.
- [ ] Snapshot all currency legs and fees on the order.
- [ ] Require risk approval for settlement using product, seller, buyer, related-account, source-root, refund-pattern, and counterparty-pair limits.
- [ ] Atomically settle all legs or none.
- [ ] Preserve exact parent-fragment lineage across buyer debits, seller credits, platform fees, escrow, and entitlement settlement.
- [ ] Create a source-stamped new 120-day earned-hard lot for each hard-paid seller proceeds credit; do not inherit buyer-lot maturity.
- [ ] Grant entitlements only after confirmed settlement.
- [ ] Restore buyer provenance on refund.

### Task 3.5: AI Cost Accounting

**Branch:** `feat/economy-ai-cost-accounting`

**Produces:** Exact metered provider cost facts, safety-priced soft-coin authorization/charge, commercial-margin enforcement, and treasury inputs.

- [ ] Write provider-cost, duplicate-usage, failed-inference, charge-compensation, exact-margin-equality, one-nano/one-SC-below, overflow, stale/unknown-cost, reserved-service-mix, and worst-case-unreserved-soft tests.
- [ ] Persist exact token/model/provider cost for completed calls.
- [ ] Compute and snapshot SC price from stressed provider cost, fixed `100,000 SC/USD` parity, and configured minimum gross margin.
- [ ] Authorize and reserve charge before billable execution.
- [ ] Require risk approval and aggregate-limit capacity before every billable service authorization.
- [ ] Reject new authorization when the snapshotted price cannot meet the margin floor or the relevant cost feed is stale.
- [ ] Finalize or release based on provider outcome.
- [ ] Publish rate-card and trailing-cost facts to Treasury.

### Task 3.6: Integrated Schema Rollup

**Branch:** `feat/economy-capabilities-schema`

**Produces:** One conflict-free migration for integrated capability models and migration verification.

- [ ] Generate migration only after Tasks 3.1-3.5 are merged.
- [ ] Review migration for unrelated model churn.
- [ ] Run up/down/current migration tests on PostgreSQL.
- [ ] Keep capability routes disabled until migration is deployed.

### Phase 3 Gate

- [ ] Hard top-up and hard-to-soft conversion pass provider and ledger E2E.
- [ ] Full/partial provider reversal after mixed-root merge, split, partial consumption, transfer, fee, escrow, and conversion accounts for every targeted root range as recovery, debt/receivable, or loss; unrelated roots remain untouched and replay cannot over-recover.
- [ ] Ad reward replay produces exactly one credit.
- [ ] Reconciliation changes future policy only.
- [ ] Bounty races have exactly one terminal winner.
- [ ] Fixed-mix orders and refunds are atomic and provenance-safe.
- [ ] Fixed soft parity remains unchanged while AI/service pricing covers stressed provider cost and minimum margin.
- [ ] Every capability branch started after Task 3.0 and avoided shared API composition and centralized migrations.

## Phase 4: Disputes, Payouts, And Treasury

### Task 4.1: Disputes, Reversals, And Debt

**Branch:** `feat/economy-disputes-and-debt`

**Produces:** Provider dispute normalization, descendant-fragment freezes, root-linked reversal groups, negative debt/receivable/loss allocation, and account restrictions.

- [ ] Test dispute before maturity, after maturity, after payout, win, loss, duplicate, out-of-order, partial cumulative interval, mixed-root isolation, and concurrent descendant movement.
- [ ] Traverse root-mint lineage and freeze every reachable current fragment from normalized provider events.
- [ ] Release or consume holds through explicit state transitions.
- [ ] Post offsetting entries rather than editing history.
- [ ] Prove recovered fragments plus responsible debt/receivable plus policy-versioned loss equals the provider-reversed root amount.
- [ ] Record and enforce debt after consumed, externally redeemed, or otherwise irrecoverable fragments.

### Task 4.2: Stripe Connect And KYC Payout Lifecycle

**Branch:** `feat/economy-connect-payouts`

**Produces:** Connected-account onboarding/status, KYC eligibility, earned-lot reservation, payout saga, and provider reconciliation.

- [ ] Write eligibility, maturity, hold, debt, refund/dispute precedence, fencing, stale-command, replay, timeout/ambiguous outcome, provider-binding, dispatch-snapshot tamper, failure, and reconciliation tests.
- [ ] Integrate `Compliance.KYC` and Connect provider contracts.
- [ ] Integrate `Compliance.FinancialCrime`, `TrustSafety`, protected-change cooldown, transaction-bound reauthentication, related-account graph, and dynamic rolling reserve policy inputs.
- [ ] Select only source-stamped, authoritatively confirmed, at-least-120-day-old, unheld, unreserved earned-hard lots.
- [ ] Allocate eligible fragments oldest-first by `confirmed_at` then journal sequence; explicitly reject purchased hard and every soft source even when old enough.
- [ ] Require a fresh payout risk decision binding actor, payee, provider mapping, payout destination, exact fragments/root ranges, KYC/compliance status, Trust/Safety status, cooldown state, debt, reserve, and policy version.
- [ ] Reserve lots before provider execution and assign an operation version, fencing token, and kill-switch epoch.
- [ ] Under the same fragment locks used by refund/dispute, atomically CAS `reserved -> dispatching`, claim the outbox command, and record chain/reserve/command/kill-switch versions as the dispatch linearization point.
- [ ] Canonically hash payee/provider mapping, amount, exact fragment/root ranges, provenance/lineage, holds, KYC, debt, reserve, chain, command, fencing, and kill-switch state; require a verified WORM/KMS anchor over both chain head and snapshot hash.
- [ ] Cancel a refund/dispute that linearizes before dispatch; route one committed after dispatch into the defined compensation/debt workflow.
- [ ] On provider success, burn the exact reserved fragments; on provider failure, release those same reservations without creating a credit, replacement lot, or new maturity clock.
- [ ] Keep fragments reserved through every ambiguous provider outcome; release only before dispatch linearization or after authoritative terminal failure/reconciliation.
- [ ] Require a verified independent anchor covering the exact eligibility chain sequence, creating and verifying an on-demand anchor when necessary.
- [ ] Complete, fail, or recover payout only through bound signed provider events and authoritative reconciliation.
- [ ] Keep high-risk or newly changed payout destinations in review/hold until cooldown and manual review gates pass; maturity alone cannot release the hold.
- [ ] Keep payout execution feature-flagged off pending external approval.

### Task 4.3: Treasury, Reserves, And Custody

**Branch:** `feat/economy-treasury`

**Produces:** External asset observations, fixed-parity hard/soft reserve proposals, stressed service-cost and safety-margin calculations, custody reconciliation, Core activation integration, shortfall controls, and reporting.

- [ ] Write parity, stressed-cost, margin-floor, buffer, haircut/finality, duplicate-asset, concurrent-reserve-lock, variance, stale-input, and shortfall tests.
- [ ] Implement external Stripe/ad receivable snapshots.
- [ ] Implement hard face-value reserve plus chargeback/refund, payout-settlement, and operating-liquidity buffers.
- [ ] Feed Core's reserve authority with external asset observations and a soft calculation of `max(face value at 100,000 SC/USD, stressed expected redemption cost)` plus ad-variance, fraud-loss, provider/FX, and operating-liquidity buffers.
- [ ] Include every confirmed unconsumed user/escrow soft fragment in face-value liability, including held, frozen, reserved, disputed, and service-authorized quantities until authoritative burn/consumption.
- [ ] Compute stressed redemption deterministically from selected-service open authorizations, highest enabled stressed cost-per-SC for unreserved soft, and irreversible in-flight provider costs without double counting.
- [ ] Prevent one external asset allocation from backing both hard and soft liabilities; apply explicit settlement-finality rules and receivable haircuts.
- [ ] Propose signed/versioned calculations to Core and prove Treasury cannot directly mutate the active reserve head; serialize issuance, payout dispatch, refunds, and admin withdrawal against it.
- [ ] Block risky operations on unexplained custody variance or shortfall.
- [ ] Export auditable reconciliation evidence.

### Task 4.4: Monthly Platform Withdrawal

**Branch:** `feat/economy-admin-withdrawal`

**Dependency:** Create this branch only after Task 4.3 is merged, verified, and deleted.

**Produces:** Mature fee-lot selection, dual approval, monthly run, provider transfer, reserve-after-withdrawal check, and reconciliation.

- [ ] Write overlapping-run, immature-fee, active-hold, insufficient-reserve, provider-timeout, and dual-control tests.
- [ ] Reserve exact fee lots for one run.
- [ ] Select the oldest eligible mature fee fragments first.
- [ ] Require independent approval.
- [ ] Recheck custody and reserve immediately before execution.
- [ ] On success, burn the exact reserved fee fragments; on failure, release the same reservations without creating replacement lots or maturity clocks.
- [ ] Complete through fenced provider reconciliation and immutable audit evidence.

### Task 4.5: Money-Movement Schema Rollup

**Branch:** `feat/economy-treasury-schema`

**Produces:** One integrated migration for disputes, debt, payout fencing/provider mappings, reserves, treasury, and admin-withdrawal persistence after Tasks 4.1-4.4 merge.

- [ ] Generate migration and model snapshot only from the integrated `develop` models.
- [ ] Review for unrelated model churn and use expand-first ordering for active Commerce tables.
- [ ] Run up/down/current migration, role, concurrency, and stale-worker tests on PostgreSQL.
- [ ] Publish the API and run payout/refund/treasury E2E with value-moving feature flags still off.

### Phase 4 Gate

- [ ] Dispute, refund, payout, and debt E2E tests pass.
- [ ] KYC and Connect sandbox validation passes.
- [ ] Reserve and custody equations reconcile to zero unexplained variance.
- [ ] Face-value coverage, stressed-cost coverage, commercial margin, and every safety buffer meet policy.
- [ ] Task 4.5 migration and stale-command fencing tests pass.
- [ ] Payout and admin withdrawal remain disabled until legal and operational approval.

## Phase 5: API, Frontend, And Operations

### Task 5.1: Economy API Activation

**Branch:** `feat/economy-api-activation`

**Produces:** CQRS-only user/admin controllers, authorization policies, OpenAPI contracts, health gates, and feature flags.

- [ ] Add self-service wallet, history, conversion, ad, bounty, and payout query/command routes.
- [ ] Add permission-scoped admin review, hold, reconciliation, policy, and audit routes.
- [ ] Add shared economy route/navigation composition once so later frontend branches remain disjoint.
- [ ] Prove controllers contain no repository or DbContext access.
- [ ] Regenerate and test the API client, then fail if a second generation produces a diff.
- [ ] Run API contract and authorization tests.
- [ ] Merge, verify, and delete this branch before creating Tasks 5.2-5.4 branches.

### Task 5.2: User Wallet And Marketplace UX

**Branch:** `feat/economy-user-experience`

**Produces:** User balances, maturity/hold explanations, transactions, conversion, checkout selection, and payout status.

- [ ] Show purchased hard, earned hard, withdrawable hard, pending/held hard, and soft separately.
- [ ] Explain that the displayed balance is a projection composed from traceable fragments; pending funding is visible but not yet minted or usable.
- [ ] Show fixed reference values (`100 HC/USD`, `100,000 SC/USD`) while making soft noncashable status explicit.
- [ ] Explain why value is pending or held without exposing risk internals.
- [ ] Add hard/soft/either/fixed-mix checkout behavior.
- [ ] Add accessible confirmation and recoverable error states.
- [ ] Add responsive and keyboard-complete tests.

### Task 5.3: Ad Reward UX

**Branch:** `feat/economy-ad-reward-experience`

**Produces:** Verified playback, reward receipt, unavailable/rate-limited states, privacy disclosure, and abuse-safe client behavior.

- [ ] Implement controlled video UI and ordered event reporting.
- [ ] Pause verification on hidden/ineligible state.
- [ ] Show reward quote as fixed-value internal platform credit that cannot be withdrawn or converted back to hard coin.
- [ ] Handle risk rejection and rate limits without revealing bypass details.
- [ ] Add Playwright replay, background-tab, interruption, and success journeys.

### Task 5.4: Operations Console

**Branch:** `feat/economy-operations-console`

**Produces:** Reconciliation, reserve, holds, debt, webhook, provider, fraud, policy-version, risk-review, financial-crime, Trust/Safety, and adjustment workflows.

- [ ] Require platform permissions and dual-control where applicable.
- [ ] Surface kill switches and their impact.
- [ ] Provide immutable audit timelines and export.
- [ ] Add risk decision search, review queue, reason-code timeline, entity-cluster exposure, velocity-limit counters, protected-change cooldowns, and manual `Challenge`/`Hold`/`Review`/`Deny` outcomes.
- [ ] Add financial-crime status, compliance holds, sanctions/KYC evidence references, monitoring cases, jurisdiction blocks, and audited protected-data reads.
- [ ] Add Trust/Safety case inputs for prohibited products, project abuse, marketplace integrity, creator enforcement, and release/appeal workflows.
- [ ] Durably audit every privileged KYC/risk read/export with actor, tenant, purpose, scope, and outcome before releasing protected data; require independent approval for bulk export and fail closed when audit persistence/verification is unhealthy.
- [ ] Add incident-focused empty/error/stale states.
- [ ] Add authenticated admin E2E coverage.

## Phase 6: Consolidated Verification And Rollout

### Task 6.1: Financial Integrity Test Campaign

**Branch:** `test/economy-financial-integrity`

- [ ] Run all Economy and Commerce unit/integration tests with zero skips.
- [ ] Run PostgreSQL concurrency and migration suites repeatedly.
- [ ] Run provider replay/order/failure suites.
- [ ] Run complete top-up, conversion, ad, checkout, refund, bounty, dispute, payout, and reserve E2E journeys.
- [ ] Verify 100% line, branch, and method coverage for every new Economy module and exercise all security-critical PostgreSQL/provider/browser branches.
- [ ] Merge the verification code/config, rerun this campaign on the exact resulting `develop` SHA, and record that SHA in the evidence bundle.

### Task 6.2: Shadow Mode And Reconciliation

**Branch:** `feat/economy-shadow-rollout`

- [ ] Merge rollout code/config first, then deploy the exact verified `develop` SHA with value movement disabled.
- [ ] Run chain, projection, provider, reserve, and custody jobs in shadow mode.
- [ ] Exercise kill switches and incident runbook.
- [ ] Confirm no unexplained variance for the agreed observation period.
- [ ] Enable soft issuance for a bounded internal cohort.

### Task 6.3: Controlled Activation

**Branch:** `release/economy-activation`

- [ ] Define immutable configuration versions, loss budgets, and automatic rollback thresholds in this branch before activation.
- [ ] Merge activation configuration into `develop`, rerun the complete Task 6.1 campaign on the resulting SHA, and record that exact SHA plus stakeholder, security, finance, legal, and operations approvals.
- [ ] Deploy and activate only that verified `develop` SHA/configuration version, starting with reads and bounded soft rewards.
- [ ] Continuously evaluate positive readiness predicates; false, unknown, or stale state disables only the affected value-moving capability.
- [ ] Monitor every production gate, margin floor, reserve buffer, and loss budget and reconcile daily during launch.
- [ ] Keep payout and admin withdrawal disabled until separately approved.

## Agent Review Protocol

Every implementation branch receives two reviews before merge:

1. Spec-compliance review against the architecture, threat model, and task checklist.
2. Code-quality/security review focused on ownership, concurrency, authorization, idempotency, and tests.

For high-risk branches (`ledger-kernel`, `ledger-schema`, `stripe-webhooks`, `disputes-and-debt`, `connect-payouts`, `treasury`, `admin-withdrawal`), add a third PostgreSQL/provider failure-mode reviewer.

The implementing agent does not approve its own branch.

## Completion Definition

The dual-currency economy is complete only when:

- every hard invariant is enforced by code and, where possible, the database
- no public path can mint, convert, transfer, refund, or withdraw unauthorized value
- every protected financial operation is bound to transaction authorization, a valid risk decision, aggregate limits, current Compliance/TrustSafety status, reserve/margin headroom, and durable audit evidence
- account-takeover protections, protected-change cooldowns, entity-graph exposure, marketplace abuse detection, dynamic rolling reserves, case review, and appeal workflows are tested and operable
- all value movements are balanced, append-only, idempotent, and provenance-preserving through exact fungible-fragment lineage
- no scalar balance is authoritative; full/partial root reversals account for every descendant fragment as recovery, debt/receivable, or loss
- ad rewards are verified, bounded, reconciled, and fail closed when provider evidence is stale
- reserve and custody reports reconcile without unexplained variance
- provider and browser E2E journeys pass in a production-like environment
- feature flags and kill switches are exercised
- legal/provider-dependent functionality remains disabled until approved
- all delivery branches/worktrees are merged, removed, and deleted according to repository rules
