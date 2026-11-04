# Production-Readiness Review — 2026-11 (homologation campaign verdict)

> Reviewed against `origin/develop` @ `0c3805f946453c5c348f1103e9637ab3a48fe218` (2026-11-03, merge of PR
> #1247 `docs/sharedkernel-package`; developed one commit past it — #1249 `docs/social-platform-package`,
> docs-only). Open-issue snapshot taken 2026-11-03 via `gh issue list --state open`.
> This is the accountability artifact of the 2026-10/11 homologation campaign. It compiles — it does not
> soften. Every verdict below is backed by a merged package doc, a pipeline guard, or a live issue; where
> something could not be verified it is marked **UNKNOWN**, not guessed.

**Overall verdict: BLOCKED — not production-ready as-is.** Seven of eleven domain families are
READY-WITH-CONDITIONS; four (Authentication, Commerce-Payments, Compliance-Consent/FERPA/KYC, and the
Commerce-Billing money leg until #1229's follow-ups are confirmed) still carry unmerged production
breakers. The distance to READY is concrete and bounded: §5 lists it as an ordered checklist.

## 0. What the campaign changed (read first)

The campaign's operating loop was: document the module family (flows × wiring matrix × state machines ×
event inventory × STRIDE) → file precise issues → fix the worst in PRs → ratchet the rest in CI guards.
That loop is why several headline findings in the merged package docs are **already stale in the good
direction** — closed with verified fixes:

| Was (package doc said) | Now (issue closed with fix on develop) |
|---|---|
| 31 of 177 authorization endpoints dead (16 × 422, 15 × 500) — `docs/authorization/gaps.md` B1–B3 | **#1203 CLOSED** (PR #1203): the 15 handler-less read queries got real handlers with fail-closed tenant guards; the 16 × 422 planned surface stays by design, enforced by `PlannedAuthorizationSurfaceGuardTests` |
| Access-review "revoke" revokes nothing; conditional policies never enforced at resolution — `gaps.md` D1–D4, G2–G3 | **#1204 CLOSED** (PR #1230): revoke performs grant removal + invalidation + audit + webhook; conditional-policy evaluator registered as a resolution extension (fail-closed on error, default on) |
| Billing/orders money cluster (G-4 wrong refund status/no gateway refund, G-13 client-supplied amount, G-8 five 500 endpoints, G-1 stub retry, G-9/G-10/G-11) — `docs/commerce/billing-orders/gaps.md` | **#1205 CLOSED** (PR #1229): 11 of 14 gaps fixed; G-3/G-12/G-14 recorded as product decisions. Spot-verified: `GetSubscriptionUsageHandler.cs` exists on develop |
| 9 auth event handlers subscribed to never-produced events — `docs/auth/event-flows.md` §2.3 | **#1199 CLOSED**: all nine wired through `AuthenticationEventDispatch` |

Residual staleness in the *bad* direction was spot-verified on 2026-11-03 and is still live on develop:
`StripePaymentService.cs:743` still hardcodes `"USD"` on the refund leg (#1202), `AddConsentModule` still
has zero host call sites (#1219), `IKeyRotationService` is still unregistered and
`KeyRotationBackgroundService` still unhosted (#1189), and the access-review read queries
(`GetAccessReviewCampaignQuery`, `GetAccessReviewAnalyticsQuery`, …) still have no handler.

## 1. Verdict table per domain family

| # | Family | Verdict | Top blockers (open issues) | Evidence: package doc + guard |
|---|--------|---------|----------------------------|-------------------------------|
| 1 | **Identity & Authentication** (25 flows, wiring, STRIDE, DFDs) | **BLOCKED** | #1178 (orchestrated auth mints `sid` token with no `UserSession` row — next request rejected), #1197 (sign-up 500 on duplicate email + TOCTOU + resend-cooldown bypass), #1190 (service-account secret path fails **OPEN** on null caller IP), #1189/#1141 (JWT key rotation entirely dead), #1195 (MFA enrollment returns empty backup codes), #1196 (web never calls sign-out endpoint), #1198 (WebAuthn ceremony/enum/last-credential gaps); in-flight fixes unmerged: **PRs #1164, #1170, #1172** (+ #1160, #1231) | `docs/auth/wiring-matrix.md` (13/324 endpoints fully wired at audit base; credential core — sign-up/in, refresh, revoke, MFA completion, recovery — is the wired-and-tested spine), `docs/security/auth-stride-threat-model.md` (37 mitigations cited, ~34 tracked gaps), `docs/security/auth-dfds.md`, `docs/auth/state-machines.md`, `docs/auth/event-flows.md` |
| 2 | **Authorization & Permissions** | **READY-WITH-CONDITIONS** | #1180 (ABAC request-time gate is a header-writing stub), #1191 (step-up has no platform trigger), #1192 (API-key/trusted-device/contact hardening); access-review read endpoints still 500 (residual of the wiring-matrix `H` class, post-#1203) | `docs/authorization/gaps.md` (A1 #1180 carried; B1–B3/C1–C2 fixed in #1203; D/G clusters fixed in #1230), `docs/authorization/cqrs-wiring-matrix.md`; guards: `PlannedAuthorizationSurfaceGuardTests`, `ControllerAuthorizationArchitectureTests` (T1/T2) |
| 3 | **Commerce — Payments** | **BLOCKED** | **#1202** (refund conversion hardcodes USD — 100× over-refund on zero-decimal currencies; charge legs at `StripePaymentService.cs:89,601` do it right, refund leg at `:743` does not), plus unfixed money-integrity siblings from `06-gaps.md`: N2/N3 (refund counter has no concurrency token; webhook completion does not verify refunded amount), N4 (top-up refunds never reverse the Economy mint), N9 (anonymous currency-suggestion 500) | `docs/commerce/payments/06-gaps.md` (8 KNOWN deliberate — incl. fail-open gateway-outcome K5 — vs 15 NEW), `03-state-machines.md`, `05-cross-module-sequences.md` |
| 4 | **Commerce — Billing/Orders/Subscriptions** | **READY-WITH-CONDITIONS** | Residual accepted gaps G-3/G-12/G-14 (unreachable `Expired`/`Disputed` states, dead webhook command) + G-2 (invoice `PastDue`/`Uncollectible` unreachable — no aging worker, `docs/operations/background-workers.md` "Missing-worker note"), G-6 (ownership-check asymmetry on cancel/update/delete/hold/release) | `docs/commerce/billing-orders/gaps.md`; #1205/#1229 closed the money cluster (G-4/G-8/G-13/G-1/G-9/G-10/G-11/G-5/G-7) |
| 5 | **Compliance — Audit / Consent / FERPA / KYC** | **BLOCKED** (Audit tier alone is production-grade) | **#1219** (Consent module dead at runtime — DI never called, every endpoint 500s; re-grant after revoke guaranteed 500), **#1220** (FERPA/KYC authorization holes: processor ids from request body, no tenant checks, caller-asserted guardian, KYC upload discards the stream; any authenticated user reads any student's records) | `docs/compliance/07-gaps.md` (N1–N16), `06-security-analysis.md` §8 (Consent "**not deployable as-is**", FERPA "needs an authorization layer", KYC two-ledger split), `05-retention-and-performance.md` |
| 6 | **Learning** | **READY-WITH-CONDITIONS** | **#1226** (certificate eligibility bypassed — issuance skips `CheckEligibility`; cohort enrollment skips `CanEnroll` + capacity; FERPA unwired for education records), gaps N6/N7/N9/N10 (ownership/capability guards, advisory review workflow, broken scheduled publish) | `docs/learning/06-gaps.md` (4 KNOWN + 21 NEW), `02-cqrs-wiring-matrix.md`, `03-state-machines.md` D1–D8 |
| 7 | **Economy / Finance** | **READY-WITH-CONDITIONS** (dispatch/anchor workers default-OFF — keep them off until conditions met) | `07-gaps.md` N6 (kill switch does not stop in-flight outbox provider sends), N4/N5 (reserved-but-undispatched payouts and treasury runs pin funds with no cancel), N3 (none of five outboxes has attempt cap/DLQ), N1/N2 (core posting + compliance outboxes are write-only spools, unbounded), K2/K3 (#1093 zero FKs, #1094 index gaps) | `docs/economy/07-gaps.md`, `04-event-inventory.md`, `06-consolidation-crossref.md` (safety net for #1092 consolidation); ADR-20260821 economy-production-rollout |
| 8 | **SharedKernel & host foundation** | **READY-WITH-CONDITIONS** | **#1248** (ObservabilityBehavior, IdempotencyMiddleware, MiddlewareOrderValidator complete-but-unwired; CSV locale drift in 4 files), N5 (five hand-rolled outboxes outside the shared DLQ/replay rail), N6 (middleware order enforced only by comments), N7 (31/364 soft-delete columns have query filters) | `docs/sharedkernel/07-gaps.md` (N1–N7), `04-middleware-chain.md` §4; guards: wiring-gap G1–G6 ratchet (§4 below) |
| 9 | **API contract & web client integration** | **READY-WITH-CONDITIONS** | **#1217** (MFA enrollment posts to unserved route → 404; promo dashboard fetches wrong prefix → renders empty), F3/F4 (17+ endpoints shadowed by hand-rolled fetch — including password sign-in itself, so contract drift is invisible to the generated client), 1,669 of 2,115 generated endpoints have no web caller | `docs/architecture/api-contract-matrix.md` (100% client↔server route match; findings F1–F8) |
| 10 | **Operations & background workers** | **READY-WITH-CONDITIONS** | **#1218** (fraud re-score on volatile in-memory channel — lost on restart/drop; SMS/Push/WhatsApp dispatchers poll while disabled; academic outbox has no DLQ; key-rotation worker dead; 4 orphaned cleanup repositories) | `docs/operations/background-workers.md` (47 implementations, 45+1 registered, gaps G1–G7), `docs/operations/devtron-production-release.md` |
| 11 | **Social + Platform** (late package, merged as #1249 — outside this review's commissioned inputs, recorded for completeness) | **READY-WITH-CONDITIONS** | Zero dead endpoints (0/510) and the wiring guard holds with an empty baseline; the holes are behavioral: no text moderation in Social (N1), 12 orphan `Post*` events escaping the G4 guard by record shape (N2), game-jam status/submit without from-state guards + caller-asserted `UserId` (N6), tenant-admin-only group membership (N4), generic AI endpoints pay no credits (N12), near-zero FluentValidation (N7). 12 NEW gaps carry no dedicated issue numbers yet | `docs/social-platform/README.md` (headline findings), `docs/social-platform/05-gaps.md` (14 NEW, 2 withdrawn on verification) |

## 2. Security posture summary

### 2.1 Enforced now (with citations)

**Pipeline guards — permanent, failing CI on regression:**

1. **Wiring-gap guards G1–G6** (`apps/api/tests/GameGuild.API.UnitTests/Architecture/`), a ratchet over
   `wiring-gap-baseline.txt`: G1 command-handler DI registration, G2 entity-config table materialization,
   G3 `DelegatingHandler` lifetime, G4 domain-event/notification producers, G5 options `SectionName`
   binding, G6 no-op stub middleware. Baseline currently carries **50 known gaps, all mapped to #1184**
   (2 × G2 dead entity configs, 26 × G4 dead events, 22 × G5 dead options); G1/G3/G6 sections are empty.
   The ratchet fails on *new* violations and on *stale* entries (fixed gaps must be removed — it only
   tightens).
2. **Controller authorization architecture tests (rules T1/T2)** — every controller endpoint carries an
   explicit authorization decision or a justified `[AllowAnonymous]` row in the host-side
   `AnonymousEndpointRegistry` (`ControllerAuthorizationArchitectureTests.cs`).
3. **Unguarded-route sweep at runtime** (`GameGuild.API.SecurityTests/UnguardedRouteSweepTests.cs`) —
   reads the running host's route table, then an anonymous HTTP walk over the protected GET surface must
   get 401/403 everywhere; anonymous surface must stay reachable.
4. **Adversarial deny-by-default suite** (`docs/security/deny-by-default-adversarial-validation.md`,
   issue #327): seven attack families (privilege escalation, tenant crossover, token forgery/tampering,
   unguarded sweep, TOCTOU races, cache poisoning, elevation/impersonation seams) against the real host
   with the production JWT pipeline. It **found and fixed** one critical privilege escalation (F-2,
   JIT self-approval with spoofed reviewer id) and one audit-integrity defect (F-3, `PermissionAuditLog`
   never persisted) — both remediated in the same change; all families now PASS.
5. **Planned-surface guard** (`PlannedAuthorizationSurfaceGuardTests`) — the deliberately-unavailable
   authorization management commands stay 422-by-contract and hidden from the generated client.
6. **Migration failure guard** (`DatabaseStartupMigrationFailureGuardTests`) and the PR Required Gate
   composition (classify, secret-scan, repository-policy, web, api, testing-lab, openapi, migration,
   economy ×4) enforced by develop branch protection.

**Fail-closed decisions in the foundation** (`docs/sharedkernel/README.md`, `01-cqrs-pipeline.md`):
unknown permissions throw; quota-service failures reject; unavailable commands resolve to explicit stubs;
missing event contracts throw; missing DataProtection env vars kill boot; BCrypt-12 password hashing,
SHA-256 token hashing, `LogRedaction`, certificate-protected DataProtection (env-only, fail-fast).

**Containment coverage** (auth): refresh-token replay → family revocation + token-version increment
(`LocalAuthService.cs:760-776, 934-972`, event `RefreshTokenReplayContainedV1` in-transaction);
suspicious-login risk events → session revocation + whole-token set (`RiskEventSessionRevocationHandler`);
per-request jti revocation + token-version middleware; HS256 pinned, reserved-claim firewall, tenant
re-resolution server-side. Residual: the per-jti deny-list is populated by no auth path (#1179) and key
rotation is dead (#1189) — containment depends on short (60 min) access-token lifetime alone for now.

### 2.2 Open security issues, ranked

| Rank | Issue | Why it ranks here |
|------|-------|-------------------|
| 1 | **#1202** refund USD hardcode (money) | Direct 100× over-refund on zero-decimal currencies (JPY), under-refund on KWD-class; verified live at `StripePaymentService.cs:743` on 2026-11-03. Pairs with top-up mint non-reversal (payments N4) for a combined drain |
| 2 | **#1219 / #1220** compliance cluster | Consent module entirely dead (500s) while advertised in the contract; FERPA/KYC surfaces let any authenticated user act on any student/subject with caller-supplied attribution — the module that exists to prevent exactly this |
| 3 | **Auth in-flight: PRs #1164 / #1170 / #1172 (+ #1160, #1231) unmerged** | They carry the fixes for #1141-class key rotation, service-account PATCH persistence, server-side Turnstile, MFA re-verification, trusted-device cleanup, geo/IP hardening batch. Until merged, #1178/#1189/#1190/#1195/#1197 remain live auth breakers. (#1164's required gate is green with a red *non-required* GitGuardian lane; #1170/#1172 checks stale/running at review time) |
| 4 | **#1180** ABAC gate is a stub + residual access-review 500s | No request-time ABAC gate exists; `Permit` from `:evaluate` can be mistaken for approval. Post-#1203/#1230 the engine's decision doors work, but any tenant relying on ABAC policies is unprotected |
| 5 | **#1191** step-up has no platform trigger; **#1190** fail-open secret path | Economy's step-up executor is strong (Serializable, receipt-bound), but auth-level step-up is unused machinery; service-account secret path fails OPEN on null caller IP |
| 6 | **#1226** learning authorization bypasses | Certificate issuance without eligibility, ungated cohort enrollment, cross-user enrollment reads/writes |
| 7 | **#1218** worker/ops holes | Fraud re-score volatility (tightening passes silently lost), no DLQ on academic outbox, dead key-rotation worker |
| 8 | **#1192 / #1198 / #1181 / #1182 / #1184 / #1248** | Hardening batch: API-key/trusted-device/contact gaps, WebAuthn ceremony, external-login link gaps, notification gaps, dead-wiring ratchet backlog, kernel unwiring |

Pre-campaign open security issues still carried: #1082–#1091 (brute-force/lockout/session hardening
batch — partially in PR #1170), #1115–#1128 (auth edge cases — partially in PR #1160), #1124
(unverified-email account takeover), #1141–#1155 (STRIDE-index gaps — partially in PR #1172).

## 3. Known-issue ledger — every OPEN issue from the campaign (#1178–#1248)

Classification: **PB** = prod-blocker (must fix or de-scope before GA) · **SF** = should-fix (first
30 days) · **AK** = acceptable-known (tracked, does not gate). Closed-in-range for the record: #1199,
#1203, #1204, #1205 (all verified fixed on develop, §0).

| Issue | Title (abbreviated) | Class | Note |
|-------|---------------------|------|------|
| #1178 | Orchestrated auth completion: token with `sid`, no `UserSession` row | **PB** | Breaks the orchestrated flow at first use |
| #1179 | Per-jti deny-list never populated | SF | Containment gap; 60-min TTL mitigates |
| #1180 | ABAC middleware stubs; no request-time gate | **PB** (if ABAC used) / SF (if unused) | Decide: wire or remove surface |
| #1181 | External login link gaps (no GitHub/MS link, Discord state, unlink case) | SF | |
| #1182 | OAuth accounts get no welcome email; password-change notifications missing | SF | Quota/cost entries also skipped |
| #1184 | Additional exists-but-unwired artifacts | SF | Ratcheted in G2/G4/G5 baseline (50 entries) |
| #1189 | JWT key rotation entirely dead (extends #1141) | **PB** | Verified unregistered on develop |
| #1190 | Service-account secret path fails OPEN; bind 500s; no unbind | **PB** | Fail-open on null caller IP |
| #1191 | Step-up: no platform trigger; WebAuthn not challenge-bound | SF | Economy enforces its own step-up |
| #1192 | API-key + trusted-device + trusted-contact hardening | SF | |
| #1195 | MFA enrollment returns empty backup-codes array | **PB** | Users cannot recover; locks MFA users out (see PR #1231) |
| #1196 | Web client never calls sign-out endpoint | SF | Server state survives client logout |
| #1197 | Sign-up 500 on duplicate email; TOCTOU; resend cooldown bypass | **PB** | First-run UX + enumeration |
| #1198 | WebAuthn concurrent-ceremony break, begin enumeration, no last-credential guard | SF | |
| #1202 | Payments refund USD hardcode | **PB** | Money; top-ranked |
| #1206–#1212, #1221–#1225 | SEO / Analytics / Ads epic tree (consent-gated trackers, JSON-LD, OG, Search Console, attribution) | AK | Growth surface, not launch-gating; #1206 consent management should precede any tracker enablement |
| #1217 | Web calls non-existent routes (MFA enrollment 404; promo dashboard empty) | **PB** | User-visible breakage in shipped pages |
| #1218 | Worker holes (fraud channel, dormant polling, academic DLQ, dead rotation) | **PB** | Ops integrity for money-adjacent paths |
| #1219 | Consent module dead at runtime; re-grant 500 | **PB** | Fix or remove the surface |
| #1220 | FERPA/KYC authorization holes | **PB** | Fix or feature-gate before any tenant data |
| #1226 | Learning: eligibility bypass, cohort gates, FERPA unwired | **PB** (certificates/cohorts in scope) / SF (de-scoped) | |
| #1233–#1246 | Web UX batch (14 issues: navigation, spacing, labels, upload errors, Turnstile friction) | SF/AK | Polish; none launch-blocking |
| #1248 | Kernel: ObservabilityBehavior/IdempotencyMiddleware/OrderValidator unwired; CSV locale drift | SF | Decision needed (wire or delete); CSV drift is a silent data-corruption class |
| (unnumbered) | Social+Platform behavioral gaps: no Social text moderation, orphan `Post*` events, game-jam guard-free status writes, tenant-admin-only groups, credit-free generic AI, missing validators | SF | `docs/social-platform/05-gaps.md` N1–N14 (2 withdrawn); file issues during the fix window — moderation (N1) is SF-high if user-generated text ships at GA |

Also open and gating (pre-range, carried): #1092 (economy 108-table consolidation), #1093 (zero DB
FKs — P0 financial), #1094 (index audit), #1082–#1091 + #1115–#1128 + #1141–#1156 (auth hardening
batches), #1134/#1133/#1128 (auth test asks), #1070–#1081 (docs tasks — the campaign itself; the auth/
commerce/compliance/economy/learning/sharedkernel/authorization halves are delivered by this corpus,
remaining families outstanding).

## 4. Pipeline enforcement inventory

**Permanent in CI now:**

- Wiring-gap guards **G1–G6** with the tighten-only ratchet baseline (§2.1). These six are the campaign's
  structural legacy: the four bug classes that inspired them (#1154 dead logout command, #1158 configs
  without tables, #1165 singleton handler, #1180/#1182/#1184 dead wiring) can no longer regress silently.
- Authorization-surface guards: T1/T2 architecture rules + host `AnonymousEndpointRegistry` +
  runtime unguarded-route sweep + `PlannedAuthorizationSurfaceGuardTests`.
- Adversarial security suite (7 families) runs in PR verification for changes under `apps/api/tests`.
- `PR Required Gate` on develop: classify, secret-scan, repository-policy, web, api, testing-lab,
  openapi, migration, economy ×4; branches must be up to date. Non-required lanes: Codacy (always
  ACTION_REQUIRED — noise), GitGuardian, flaky Emception package job (known `coding-cycle-e2e` rubric
  stream timeouts; evidence comment + orchestrator decision per brief).

**Still unguarded — the suggested ratchets (from `docs/sharedkernel/07-gaps.md` §"Suggested ratchets"
and family docs):**

1. **G7 candidate** — registered `IPipelineBehavior<,>` implementations must be referenced by a host
   registration test (would have caught dead `ObservabilityBehavior`, #1248 N1).
2. `Use*`-extension scan — middleware extensions defined but never called by `ConfigurePipeline`
   (N2/N3 class: IdempotencyMiddleware, MiddlewareOrderValidator).
3. **Middleware order ratchet** — textual order assertion for the six security-critical `Use*` calls in
   `ConfigurePipeline` (N6: today a reorder that puts rate limiting before revocation, or tenant before
   auth, passes CI).
4. **CSV culture lint** — `Append(` chains in `text/csv`-returning files must pass an explicit culture
   (N4: silent locale corruption in Assets/AI/Analytics exports).
5. Unthrottled-surface sweep — economy (19 controllers), learning (31), consent/FERPA/KYC, 12 payments
   controllers (payments N10, economy N9, learning N4, compliance N8/N12).
6. Dead-state sweep — declared FSM states with no writer (billing G-2/G-3/G-12 residuals, compliance
   N3/N15, learning N18): no guard enumerates unreachable states.
7. DI-composition test per module — the Consent N1 class (module enabled for handler scan but its
  service layer never registered) is invisible to every current guard.

## 5. Go-to-prod checklist (ordered; scope estimates)

| # | Work item | Issues | Scope | Unblocks |
|---|-----------|--------|-------|----------|
| 1 | Merge the three auth PRs **#1164, #1170, #1172** (rebase on develop, required gates green; #1164's GitGuardian red is a non-required lane — evidence comment per brief), then **#1160** and **#1231** | #1141–#1157, #1115–#1128, #1195 partially | M (review + rebase; code done) | Auth verdict |
| 2 | Fix **#1202**: use `request.Currency` on the refund leg (`StripePaymentService.cs:743`) + non-USD test fixtures; same PR: N3 webhook amount verification + N2 concurrency token on `Payment` | #1202 | S (one-line fix) + M (hardening) | Payments verdict |
| 3 | **Consent**: either call `AddConsentModule` in `ApiProductComposition` + add the re-grant path (upsert on revoked row) or remove the module from the enabled set and the OpenAPI surface | #1219 | S (wire) + S (re-grant) | Compliance verdict |
| 4 | **FERPA/KYC**: derive actors from context, add tenant/ownership scoping, ownership-check the disclosure gate, decide the two-ledger bridge (N7) — or feature-gate both modules off for GA | #1220 | M | Compliance verdict |
| 5 | **Learning gates**: call `CheckEligibilityAsync` at issuance, `Cohort.CanEnroll` + capacity at enrollment, ownership guards on enrollment/certificate reads | #1226 | S–M | Learning conditions |
| 6 | **Workers**: persist the fraud re-score queue (due-sweep row), add `return` to the four dormant dispatcher branches, attempt-cap + dead-letter the academic outbox (copy the Billing pattern), register-or-delete `KeyRotationBackgroundService` | #1218 | M | Ops conditions |
| 7 | **Web routes**: point MFA enrollment at `/v1/auth/mfa/sign-in/complete` (or implement), fix promo-campaigns prefix | #1217 | S | Web conditions |
| 8 | **Kernel decisions**: register or delete `ObservabilityBehavior` / `IdempotencyMiddleware` / `MiddlewareOrderValidator`; fix the 4 CSV locale files | #1248 | S each | SharedKernel conditions |
| 9 | **Economy pre-enable conditions** (before flipping `DispatchWorkersEnabled`): kill-switch re-check in the dispatch processor (N6), cancel paths for reserved payouts/treasury runs (N4/N5), attempt caps on the five outboxes (N3), dispatcher for the core/compliance outboxes or stop writing (N1/N2) | economy 07-gaps N1–N7 | M–L | Payout/treasury go-live |
| 10 | **Dismiss/de-scope decisions** (orchestrator, with evidence comments): ABAC surface (#1180), step-up producers (#1191), access-review read endpoints, billing G-2 aging worker, SEO/Ads tree timing (#1206 consent first), UX batch (#1233–#1246) | ledger AK/SF rows | decision-only | Final READY |

Estimated aggregate to flip the overall verdict: items 1–8 are the launch gate (≈ 2–4 engineer-weeks
including review); item 9 gates only the payout/treasury switch, not the API launch; item 10 is
bookkeeping.

## 6. Operational readiness

**Workers and DLQ posture** (`docs/operations/background-workers.md`, audited 2026-11-02): 47
implementations, 45+1 registered. Reference implementations with full DLQ semantics exist
(`BillingWebhookRetryWorker`, core `OutboxDispatcher`/`InboxStore`). Open gaps (= issue #1218):
fraud re-score on a volatile `DropWrite` channel (lost on restart/full); academic outbox retries forever
with no dead-letter (300 s cap, no ceiling); SMS/Push/WhatsApp dispatchers poll while disabled (missing
`return`); `KeyRotationBackgroundService` implemented but never registered; four orphaned cleanup
repositories (trusted devices, flow state, sessions, expired revocations); Assets scan/moderation
claimless picks (unsafe under scale-out); Economy outboxes without attempt caps (economy N3); invoice
lifecycle with no worker at all (billing G-2).

**Retention**: the audit tier enforces per-tenant retention (default 400 d, batch 5000, legal holds
suspend every deletion, archive tier, execution history) — production-grade. Gaps: `SecurityAlerts`
unbounded; **no Consent/FERPA/KYC table has any retention worker** (deadlines stored, never acted on —
compliance N3); Economy core/compliance outboxes grow unbounded (N1/N2); no partitioning anywhere in the
audit family (hot/archive split + batch cap is the only size lever); webhook raw-payload retention
opt-in/default-off (deliberate, PII-driven).

**Rollback / migration rehearsal**: the Devtron runbook *requires* expand/contract migrations, a
pre-deploy migration job proven in staging (`DEVTRON_API_PREDEPLOY_MIGRATIONS`), and **three consecutive
staging releases including a forced-smoke rollback** before production cutover, plus three healthy
production releases recording rollback results. **Status of those executions: still UNEVIDENCED** — no
execution record (run logs, release-register entries, rollback results) exists in the repository, and
this review did not have access to the deployment environment to verify them. This is a hard
verification item for the release owner; the runbook's own standard says image rollback never reverses
schema, so the staging migration-failure and rollback drills must be evidenced before GA. Database
backup/restore drills: **procedure now defined — execution unevidenced** (`docs/operations/disaster-drill.md`).

**Update 2026-11-04 (ops rehearsal campaign, PR "ops: migration/rollback rehearsal + load smoke +
disaster drills"):** the previously-UNKNOWN *capability* halves are now rehearsed and documented,
resolving the two UNKNOWN rows above into concrete verdicts:

- **Migration/rollback rehearsal: REHEARSED (locally, disposable PG 17, CI-pinned image) — procedure
  + script + findings delivered.** `docs/operations/migration-rehearsal.md` +
  `scripts/devops/migration-rehearsal.sh`. Verified: full-history forward apply is complete
  (226/226 migrations == `__EFMigrationsHistory` rows == assembly chain); the last 3 develop
  migrations revert cleanly one-by-one (`AddEconomyTopUpRefundReversalCapability`,
  `AddSecurityBatch3Enhancements` #1269, `AddLedgersRatingsAndAchievementsTables`) and the
  down-3/up-3 round-trip reproduces a byte-identical schema. Findings ledger (F1–F6): the only
  lossy-but-correct Down is #1269's `Users.PasswordChangedAt` drop (rollback = password-age policy
  reset for every user — sign-off required); PR #1285's
  `AddAcademicOutboxDeadLetter` is **broken twice over** — its check constraints are mutually
  contradictory (status admits `'deadlettered'`, lifecycle tests `'dead_lettered'`, so the
  application's terminal dead-letter write fails at the database — DLQ cannot function as shipped)
  and its Down() **cannot execute once any dead-lettered row exists** (constraint re-add violates,
  live-reproduced 23514). Fix #1285 before merge. The Devtron staging forced-smoke-rollback
  executions remain the open verification item.
- **Load smoke: HARNESS DELIVERED — first green run pending.**
  `apps/api/tests/GameGuild.LoadSmoke/` + `.github/workflows/load-smoke.yml` (session-state
  acceptance pattern): boots the real API host with the real JWT pipeline against migrated
  PostgreSQL and drives the five hottest paths (sign-in, feed page, wallet read, product browse,
  checkout initiation) at N=10 × K=25 with p95 budgets and a zero-5xx assertion. Any blown budget
  is issue material with the emitted profile. Compiles clean; hosted run executes in its own
  workflow lane.
- **Disaster drills (paper): RUNBOOK DELIVERED — drills not yet executed.**
  `docs/operations/disaster-drill.md`: DB restore-point objective (RPO 15 min / RTO 1 h PROPOSED;
  **KNOWN GAP — no production backup topology exists anywhere in-repo**, so the objective is NOT
  MET until defined and drilled), outbox poison-message recovery (the `/v1/admin/events` DLQ +
  replay surface, worker restart order; academic/fraud DLQ blocked on PR #1285's F3/F4 defects),
  key-rotation emergency (**KNOWN GAP — rotation still dead on develop, #1189; the validated fix's
  delivery path is #1164 → #1264 → #1273, merged to `main` f4b1d8e7 on 2026-11-04, pending
  develop sync — verify `IKeyRotationService` DI + hosted worker per-SHA before relying on the
  rotation surface; static-rotation fallback procedure documented**), and webhook signing-secret
  rotation (dual-accept window verified against the code; `POST /api/v1/billing/webhooks/secrets:rotate`).

## 7. Method and citation hygiene

- Corpus: `docs/auth/**` (26 files), `docs/authorization/**` (12), `docs/commerce/payments/**` (7),
  `docs/commerce/billing-orders/**` (8), `docs/compliance/**` (8), `docs/learning/**` (7),
  `docs/economy/**` (8), `docs/operations/background-workers.md`, `docs/architecture/api-contract-matrix.md`,
  `docs/sharedkernel/**` (8), `docs/security/**` (auth STRIDE/DFD/architecture + deny-by-default suite).
  Ops campaign addendum (2026-11-04): `docs/operations/migration-rehearsal.md`,
  `docs/operations/disaster-drill.md`, `scripts/devops/migration-rehearsal.sh`,
  `apps/api/tests/GameGuild.LoadSmoke/`, `.github/workflows/load-smoke.yml`.
  One package landed during review and is included for completeness: `docs/social-platform/**` (PR #1249).
  Older review docs (e.g. `COMPREHENSIVE_SECURITY_ARCHITECTURE_REVIEW.md`, Jan 2026, "production-ready")
  predate the campaign and are **superseded** by this verdict where they conflict.
- Issue states from `gh` on 2026-11-03; fix claims for closed issues cross-checked against develop
  (handler/registration presence verified for #1205's `GetSubscriptionUsageHandler`; #1219/#1189/#1202
  and the access-review reads re-verified as still broken at `0c3805f9`).
- Counts quoted from package docs reflect their audit bases (`74e9bba4c` / `ac959fd8f` /
  `43603cb5b`); §0 lists the known deltas since. Where a doc's number could not be re-derived here, the
  doc is cited as the source of record.
- Conservative bias by design: families carrying any unfixed user-visible 500, money-math, or
  authorization-bypass defect are BLOCKED regardless of how small the fix is.
