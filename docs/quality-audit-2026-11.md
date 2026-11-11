# Documentation Quality Audit — 2026-11 (full-package audit)

> **Auditor**: docs/quality-audit fleet run, 2026-11-05.
> **Audited tree**: `origin/develop` @ `09e889b0b1` (merge of #1321 `docs/tracker-reconcile`), fresh worktree — the exact tree carrying the "50/50 modules at 8/8" claim.
> **Scope**: all 22 documentation package directories (`docs/auth`, `authorization`, `commerce`, `compliance`, `compliance-financialcrime`, `finance-ledgers`, `identity`, `learning`, `economy`, `social-platform`, `gamification`, `notifications`, `trustsafety`, `social/{mentorship,ratings}`, `content`, `resources-contents`, `sharedkernel`, `security`, `operations`, `architecture`, `release`, `ui`) — **345 markdown files**, plus the coverage tracker `docs/documentation-coverage.md`.
> **Method**: every `file:line` citation in scope was extracted and machine-verified (existence, EOF bounds, blank-line signal, doc-local context re-resolution); every Mermaid block (547) was parsed with the real Mermaid 11 parser in headless Chromium; claims were re-verified against current code for every PR that landed after each package was written (#1226→#1322); the tracker was checked row-by-row against the tree. Spot-checks sampled ≥3 citations across different files per package (6 per package, 132 total, all shown load-bearing content).

---

## 1. Executive summary

| Dimension | Result |
|---|---|
| Citations checked / verified true | **7,116 checked — 6,939 (97.5%) resolve to a real, non-blank line**; 8 intentional historical references (events removed in #1199) and 21 ellipsis-shorthand paths (`…Handler.cs`) excluded as by-design; **148 citations (2.1%) are genuinely wrong** (beyond EOF, blank, or non-existent file) |
| Mermaid blocks parsed | **547 — 531 parse, 16 FAIL** (11 distinct files; see §4) |
| Claims-now-false | **12 register entries across 7 packages** (see §5) — the STRIDE-residual-was-fixed class predicted by the brief is real, concentrated in commerce/payments |
| Tracker accuracy | **Structurally stale**: 25 duplicate pre-reconciliation rows contradicting the 8/8 rows; consolidated totals say 20×8/8 while the matrix says 50 rows at 8/8; one ✅ credits a file that does not exist (`docs/notifications/04-event-inventory.md`) |
| Overall verdict | **PASS-WITH-NOTES for the package corpus; the "100% / 50-of-50 at 8/8" headline is NOT yet earned as stated** — it is honest at 49-of-50 with one missing artifact, 16 broken diagrams, and a stale reconciliation layer (see §8) |

The documentation is **substantively real and unusually verifiable** — this is not decorative docs. Counts that could be independently recomputed (ledger route counts 11+13=24, seeder contents, hosted-service registrations, middleware order) matched the code exactly wherever checked. The failure modes are (a) the +N drift class already known from the auth spot-audit, now measured repo-wide; (b) post-authoring fixes (#1268, #1275, #1285, #1290/#1295, #1294) that landed *after* the corresponding gap registers were written, leaving "OPEN" residuals that are now fixed; and (c) a tracker whose final reconciliation commit deleted nothing — it appended.

---

## 2. Citation drift statistics

Extraction: `path/file.ext:NNN(-MMM)` and `path/file.ext#LN` forms. Resolution: exact → path-fragment subsequence → doc-local context (a bare `actions.ts:56` resolves against the fully-qualified `apps/web/src/lib/marketplace/actions.ts` named earlier in the same doc).

**Totals**: 7,116 citations · 6,939 OK (97.5%) · 148 genuinely wrong (2.1%) · 29 excluded by design (8 historical `Events/*.cs` rows in `docs/auth/event-flows.md:85-87` documenting events **removed in #1199** — the docs say so; 21 ellipsis-shorthand paths).

**Wrong-citation classes** (overlap): 85 beyond-EOF · 40 blank-at-line (content sits 1–2 lines away — small additive drift) · 23 file-missing (files deleted or never existed at the cited path).

**Per-package** (checked → genuinely wrong → rate):

| Package | Checked | Wrong | Rate | Worst single offender inside the package |
|---|---|---|---|---|
| auth | 1,972 | 30 | 1.5% | `event-flows.md` `:501` cluster + `NewLogin/SuspiciousLoginAlertPublisher` ranges (+2 over EOF) |
| authorization | 474 | 1 | 0.2% | — |
| commerce | 956 | 14 | 1.5% | `payments/{04,06,07}` + `commerce/09` cite the deleted `FraudBackgroundScoringQueue.cs` (see §5) |
| compliance | 351 | 0 | 0.0% | — |
| compliance-financialcrime | 121 | 1 | 0.8% | — |
| finance-ledgers | 113 | 7 | 6.2%* | *adjudicated down: 5 of 7 are tool mis-resolutions of module-relative `/Architecture/UseCaseEventContracts.g.cs:2-8` (the Finance.Ledgers file is exactly 8 lines); real: `ApplicationDbContext.cs:196` blank |
| identity | 264 | 8 | 3.0% | `tenants/README.md:27` cites `UpdateTenantCommandHandler.cs:16` as proof of *no ownership checks* — the line now **is** the #1287 T1 guard |
| learning | 307 | 9 | 2.9% | `06-gaps.md` `CourseEnrollmentAccessReader.cs:14-33` (file now 28 lines) |
| economy | 303 | 2 | 0.7% | — |
| social-platform | 351 | 9 | 2.6% | `02` `GameJamsModuleApi.cs:461-529` (file now 488) |
| gamification | 103 | 4 | 3.9% | — |
| notifications | 112 | 4 | 3.6% | — |
| trustsafety | 74 | 0 | 0.0% | — |
| social (mentorship+ratings) | 133 | 12 | 9.0%* | *adjudicated down to ~6: ratings `RatingEndpointCommands.cs:23-87` (85 lines), `RatingUseCaseEventContracts.cs:5-15` (14), mentorship controller ranges +1..+4 over EOF |
| content | 258 | 1 | 0.4% | — |
| resources-contents | 184 | 4 | 2.2% | — |
| sharedkernel | 218 | 4 | 1.8% | — |
| security | 367 | 27 | 7.4% | **legacy** deep-review docs (`GAMEGUILD_FEATURES_LEARNING_DEEP_REVIEW.md` cites the deleted `ProgramController.cs`; `FeatureFlagsController.cs:180-425` vs 388 now) — these predate the fleet packages and are not part of any 8/8 row |
| operations | 124 | 0 | 0.0% | — |
| architecture | 29 | 1 | 3.4% | — |
| release | 5 | 0 | 0.0% | — |
| ui | 297 | 10 | 3.4% | `journeys/01` cites `turnstile-widget.tsx:157-206` in a 161-line file — behaviors exist but at other lines (anchors wrong **when written**, not drifted: the file last changed 2026-11-01, the doc 2026-11-04) |

**The +N drift class is repo-wide but shallow.** The auth spot-audit's finding (8/29 drifted, all additive) reproduces at repo scale: 2.1% of citations, overwhelmingly ≤5 lines of movement, concentrated in a handful of hot files (`LocalAuthService.cs`, `GameJamsModuleApi.cs`, `CourseEnrollmentAccessReader.cs`, ratings command files). Two files dominate by volume: legacy `docs/security/*_DEEP_REVIEW.md` (code deleted underneath) and `docs/ui/journeys` (wrong-at-authoring anchors). **The auth spot-audit's hygiene recommendation (re-anchor the 8 drifted citations) was never applied** — `docs/auth/cross-module-sequences.md:67` still cites `LocalAuthService.cs:501` (content now at `:724`).

Positive control (verified exactly): `docs/commerce/products/README.md:40` "AddProductsModule had no host caller (ProductsModule.cs:16)" — historical framing with the fix annotated; `docs/ui/journeys/03:42` `addMarketplaceCartItemAction` at `apps/web/src/lib/marketplace/actions.ts:41` — verbatim; ledger route counts 11 + 13 — exact.

---

## 3. Per-package verdicts

Legend: **PASS** = no material defects found · **PASS-WITH-NOTES** = real but bounded defects · **FAIL** = a claim central to the package is now false, an artifact is missing, or multiple diagrams are broken.

| Package | Verdict | Key evidence |
|---|---|---|
| **auth** (+docs/security/auth-*) | PASS-WITH-NOTES | 1 Mermaid fail (`flows/service-account-flow.md:12`, diamond label w/ apostrophe); `event-flows.md:30` producer-coverage claim now false (§5-A); spot-audit drift never re-anchored; headline #1141 "key rotation unwired" re-verified **still true** (`InfrastructureLayerExtensions.cs:247/:408` host only quota+refresh-token workers; no `IKeyRotationService` registration anywhere) |
| **authorization** | **PASS** | 31 diagrams clean; 1 wrong citation of 474; post-#1230 STRIDE verified current (`flows/permission-grant-lifecycle-flow.md` → `JitElevationRequest.cs:113` self-approval throw — verbatim) |
| **commerce** | **FAIL** | 6 Mermaid failures (4× `billing-orders/08-dfds.md` cylinder `[(…])`, `payments/01-user-flows.md:10` diamond, `products/09:8` cylinder-as-subgraph); N14/R-P5 residual register describes code replaced by #1285 (§5-B); `products/09-security-architecture.md:14` diagram node "AddProductsModule — NEVER CALLED P1" contradicts the same package's README/02 ("live since #1290") — internal inconsistency |
| **compliance** | PASS-WITH-NOTES | 0 wrong citations of 351; diagrams clean; N1 "Consent dead — #1268 OPEN" was honestly dated ("re-verified 2026-11-04… do not mark fixed on the PR's existence") but #1268 **merged 2026-11-05** — now stale (§5-C) |
| **compliance-financialcrime** | **PASS** | Diagrams clean; `AddFinancialCrimeComposition` wiring citation verified (`EconomyCapabilityCompositionExtensions.cs:22`) |
| **finance-ledgers** | PASS-WITH-NOTES | 3 Mermaid failures (`05-cross-module-sequences.md` ×2 — participant id `Actor` collides with the mermaid keyword; `09:8` cylinder-as-subgraph); route counts and the L3 "no consumer" residual verified exact |
| **identity** | PASS-WITH-NOTES | 1 Mermaid fail (`tenants/08-dfds.md:71` `({"…"})` diamond-in-parens); T1 and U5 gap registers now fixed by #1294 (§5-D) with no state note (unlike learning/07) |
| **learning** | PASS-WITH-NOTES | `07-stride` carries an exemplary post-#1275 state note ("this file is authoritative"), but `03-state-machines.md:171` still asserts "no scheduler worker" (D8) — `ScheduledPublishBackgroundService` registered `CoursesModule.cs:66` since 2026-11-04 (§5-E) |
| **economy** | **PASS** | 31 diagrams clean; K4-fixed-post-#1266 statement verified consistent |
| **social-platform** | PASS-WITH-NOTES | 34 diagrams clean; honest pinned-commit scope ("critical flows only", 510-endpoint census); 2 citations drift in `GameJamsModuleApi.cs` (post-authoring shrink) |
| **gamification** | PASS-WITH-NOTES | 2 Mermaid failures (`08-dfds.md:55,69` — hexagon labels containing `[ ]`); "achievements:* never seeded" re-verified true (no seeder entries) |
| **notifications** | **FAIL** | `04-event-inventory.md` **does not exist** — README:13 links to it, `02:98` cites "(04 §3)", tracker row 48 credits "✅ (04 §registry)"; the package commit `dc2acbb471` ("8 artifacts + README") landed 8 files but the numbering skips 04, so one artifact type (event inventory) is absent. Renderer-census content partially lives in 06/09, but the named artifact is a ghost |
| **trustsafety** | **PASS** | 0 wrong citations; T1 "both write surfaces caller-less" re-verified by grep — true (`IngestAsync`/`SubmitAppealAsync` have no non-test callers) |
| **social/mentorship+ratings** | PASS-WITH-NOTES | 1 Mermaid fail (`ratings/08-dfds.md:47` `[(…]]` bracket mismatch); small citation overshoots (≤4 lines) in ratings/mentorship matrices |
| **content** | PASS-WITH-NOTES | 2 Mermaid failures (`03-state-machines.md:46` unquoted state-transition labels with spaces + `<br/>`; `05:9` unterminated `alt`); citations otherwise clean (1/258) |
| **resources-contents** | **PASS** | Diagrams clean; E9/#1278 runtime-500 reconciliation consistent with tracker row 46 |
| **sharedkernel** | **PASS** | Thematic artifact layout actually matches the tracker's section-level mapping (verified `05 §6` AnonymousEndpointRegistry exists at the claimed section) |
| **security** (dir) | PASS-WITH-NOTES | Fleet docs (`auth-*.md`) clean; the 7.4% drift lives entirely in **legacy** deep-reviews predating the packages (deleted `ProgramController.cs` etc.) — recommend archiving or stamping them |
| **operations** | PASS-WITH-NOTES | `background-workers.md` claims 47 hosted implementations; code registers **48** distinct types — `PendingPaymentAgingBackgroundService` (`PaymentsModule.cs:176`) and `ScheduledPublishBackgroundService` (`CoursesModule.cs:66`) are absent from the inventory; `disaster-drill.md:86-152` still calls #1285 "open PR"/"pending merge" (it merged 2026-11-05) |
| **architecture** | PASS-WITH-NOTES | Only 1 Mermaid block in scope (below the 5-per-package bar — nothing to validate at scale); api-contract-matrix totals (2,115 / 19.9%) internally consistent |
| **release** | **PASS** | 0 diagrams, 5/5 citations true; verdict document explicitly dated and pinned to a commit |
| **ui** | PASS-WITH-NOTES | 2 diagrams; ~10 wrong anchors (turnstile cluster wrong-at-authoring, §2); the marketplace-cart citations verified verbatim |

---

## 4. Mermaid failures (all 16, machine-parsed with Mermaid 11)

| # | File:line | Root cause class |
|---|---|---|
| 1 | `docs/auth/flows/service-account-flow.md:12` | Apostrophe inside a quoted diamond label (`{"…provider's…"}`) → parser stops at SQE |
| 2-5 | `docs/commerce/billing-orders/08-dfds.md:102,148,321,358` | Cylinder shape closed with `])` instead of `)]` (`D4[("outbox receipts TB5"])`) — the diamond/cylinder bracket-mismatch class from the earlier batches, still present ×4 in one file |
| 6 | `docs/commerce/payments/01-user-flows.md:10` | Apostrophe in diamond label (`{"cross-tenant guard"}` context) |
| 7 | `docs/commerce/products/09-security-architecture.md:8` | `subgraph STORES[("catalog stores")]` — cylinder shape used as a subgraph title (invalid) |
| 8 | `docs/content/03-state-machines.md:46` | `stateDiagram-v2` transition labels unquoted with spaces, plus `<br/>` in a transition description |
| 9 | `docs/content/05-cross-module-sequences.md:9` | `alt` block never closed with `end` |
| 10-11 | `docs/finance-ledgers/05-cross-module-sequences.md:8,34` | Participant id `Actor` collides with the mermaid `actor` keyword → INVALID token |
| 12 | `docs/finance-ledgers/09-security-architecture.md:8` | cylinder-as-subgraph (`subgraph STORES[("finance schema")]`) |
| 13-14 | `docs/gamification/08-dfds.md:55,69` | Hexagon labels containing unescaped `[Authorize]` brackets (`TB1{{TB1 [Authorize]}}`) |
| 15 | `docs/identity/tenants/08-dfds.md:71` | `R6({"3.6 Resolution arbiter"})` — quoted string inside a `({` diamond opener |
| 16 | `docs/social/ratings/08-dfds.md:47` | `T2[(summary recompute]]` — mismatched closing brackets |

Note: `docs/social-platform/01-user-flows.md:3` explicitly declares the house style ("no parentheses and no colons in labels") — that package's 34 diagrams all parse. The failures above are all in packages authored before that convention hardened.

---

## 5. Claims-now-false register (things that changed but docs say otherwise)

| # | Package / doc | Claim (doc) | Reality on develop @09e889b0b1 | Changed by |
|---|---|---|---|---|
| A | `docs/auth/event-flows.md:30` | "producer coverage gap — orchestrated, polymorphic, federated and MFA sign-in paths never call `ISuspiciousLoginAlertPublisher` (sole caller `LocalAuthService.cs:501`)" | `AuthenticationOrchestrationService.cs:371` calls `suspiciousLoginAlerts.RecordAsync(...)`; sole-caller is false; `:501` → `:724` | `41b7252b1f` (2026-11-02, "extend suspicious-login coverage") |
| B | `docs/commerce/payments/06-gaps.md:69`, `07-stride:87,149,169`, `04:80`, `docs/commerce/09:165,187` | N14 / R-P5: "fraud re-score queue is an in-process bounded Channel — volatile, jobs dropped on restart — OPEN" | `FraudBackgroundScoringQueue.cs` **deleted**; durable `FraudBackgroundScoringJob` table + `FraudBackgroundScoringJobStore` (353 lines) + `FraudBackgroundScoringHostedService` registered (`PaymentsModule.cs:147-149`) | #1285 (`b16822e6c3`, 2026-11-04, "durable fraud queue… #1218 G3") |
| C | `docs/compliance/07-gaps.md:26` + STRIDE/DFD/sequences/state-machines + tracker row 13 | "N1 Consent module dead at runtime — #1268 OPEN and unmerged" | `AddConsentModule()` called at `ApiProductComposition.cs:126` | #1268 merged 2026-11-05T05:53Z (after the doc's re-verification date — the doc was honest when written) |
| D | `docs/identity/tenants/gaps.md:9` + `tenants/README.md:27`; `docs/identity/users/gaps.md:23` (U5) | T1 "any authenticated principal can delete/purge arbitrary tenants — no ownership checks in handlers (`UpdateTenantCommandHandler.cs:16`)"; U5 "self permissions never granted — ordinary users 403 on own profile" | Line 16 now reads "Issue #1287 T1: tenant updates require an active Owner/Admin membership"; `ITenantAdministrationGuard` applied across all 17 tenant command handlers; `SelfOrPermissionRuleEvaluator` v6 makes self-actions claim-based, seeder v6 drops the grant requirement (comments cite "#1287 U5") | #1294 (`123b9445ed`, 2026-11-05) |
| E | `docs/learning/03-state-machines.md:171,151` (D8) | "Scheduled publish… no worker executes scheduledPublishAt" | `ScheduledPublishWorker.cs` + registration `CoursesModule.cs:66` | #1275 / `5b09fa5aed` (2026-11-04) — and the package's own `07-stride` state note says so; `03` was not updated |
| F | `docs/commerce/products/09-security-architecture.md:14,74` | Diagram node: "AddProductsModule — NEVER CALLED P1" + follow-up advice premised on it never being wired | Host composes it (`InfrastructureLayerExtensions.cs` block 10c); package README/02 correctly say "live since #1290" — the 09 diagram contradicts its own package | #1290/#1295 |
| G | `docs/operations/disaster-drill.md:86-87,137-138,152` | "Academic outbox has no DLQ on develop — retries forever; fix on open PR #1285"; "durable FraudBackgroundScoringJob… pending merge" | #1285 merged; DLQ + durable queue are on develop (learning/07 documents the ceiling) | #1285 (2026-11-05) |
| H | `docs/operations/background-workers.md:11,24` | "47 hosted-service implementations" | 48 distinct hosted types registered in Source; `PendingPaymentAgingBackgroundService` and `ScheduledPublishBackgroundService` missing from the inventory | #1266-era + #1275 |
| I | `docs/documentation-coverage.md:117` | "P1 AddProductsModule nunca invocado" (wave-5 note) | Wired since #1290/#1295 | #1295 |
| J | `docs/documentation-coverage.md:114` | "Achados críticos abertos: takeover de tenants (T1), permissão self nunca semeada (U5)" | Both fixed by #1294 | #1294 |
| K | `docs/documentation-coverage.md:23` (row 13) | "F5 + #1268 aberto… N1/N2 residuais — #1268 pendente" | #1268 merged | #1268 |
| L | `docs/auth/spot-audit-2026-11.md` recommendations | "the eight drifted citations should be re-anchored when the auth package is next touched" | Never applied; `cross-module-sequences.md:67` still cites `:501` | — |

Counter-examples (currency done right): `docs/learning/07-stride-threat-model.md:6` state note; `docs/commerce/products/README.md:4` post-#1290 banner; `docs/auth/flows/polymorphic-sign-in-flow.md:10` carries the #1309 admission-lock design; `docs/operations/background-workers.md:70,237-242` documents the durable fraud queue with strikethrough history. So the fleet demonstrably updates docs when it revisits them — the misses are all "fix landed after the package froze".

---

## 6. Internal-consistency findings (wiring matrix ↔ flows ↔ state machines ↔ events ↔ STRIDE ↔ gaps)

1. **finance-ledgers**: wiring matrix "24 rotas" = 11 (LedgersController) + 13 (LedgerEntriesController) — recomputed from source, exact. State machines ↔ event inventory ↔ 09 architecture agree on the "1 durable + dead types / no consumer L3" story. Consistent.
2. **commerce/products**: README/02 (updated) vs 09-security-architecture (stale diagram) — **inconsistent within the package** on the single most load-bearing fact (P1). The tracker row 11 note still says "P1-P9 residuais", compounding it.
3. **learning**: 02/03 (pre-fix, `ac959fd8f`) vs 07 (post-fix, authoritative-by-declaration). The declaration mitigates, but a reader of 03 alone gets the wrong state machine story for scheduled publish.
4. **auth**: event-flows ↔ cross-module-sequences agree with each other (both stale) — consistent-but-wrong, the drift class the brief warned about.
5. **notifications**: README (links 04) ↔ 02 ("04 §3") ↔ tracker ("04 §registry") are mutually consistent about an artifact that does not exist.
6. **State machine ↔ events spot-checks elsewhere** (authorization AccessReviewStatus enum, gamification achievement lifecycle, content ContentVersion 7-state vs resources-contents 03) all agree.

## 7. Coverage-honesty findings

- The Social+Platform package is **genuinely honest**: pinned commits per doc, "critical flows only" declared in the tracker rows and the README's endpoint census; no endpoint claimed as a flow that isn't documented as critical-only. Consistent with the brief's observation.
- **Notifications is the one dishonest cell**: the tracker marks the Eventos column ✅ for a file that was never committed. Everything else in the row is real.
- The **release** verdict doc explicitly separates "compiles" from "ready" and marks UNKNOWNs — good honesty practice.
- **docs/security** contains pre-fleet legacy reports with heavy drift; they are not referenced by any 8/8 row, but they sit in an audited directory and would mislead a reader; recommend stamping them `[historical: reviewed against <commit>]`.

## 8. Tracker accuracy (docs/documentation-coverage.md)

| Defect | Evidence |
|---|---|
| 25 stale duplicate rows | Lines 55-79 repeat rows 20-44 at their **pre-reconciliation** values (4/8, and Mentorship/Ratings 0/8) directly beneath the new 8/8 rows for the same modules — the "final reconciliation" commit appended the new rows without deleting the old block |
| Consolidated totals contradict the matrix | "Números consolidados" (lines 101-103) says **20** modules at 8/8, 29 at ≥4/8, 7 at 0/8 — none of which matches the 50-row matrix above it (49×8/8 + Identity.Context 1/8) |
| Ghost artifact credit | Row 48 "Eventos ✅ … (04 §registry)" — `docs/notifications/04-event-inventory.md` does not exist |
| Stale open-item notes | Row 13 "#1268 pendente"; line 114 "T1/U5 open"; line 117 "P1 nunca invocado" — all fixed (§5) |
| "50/50 at 8/8" arithmetic | 49 rows carry 8/8; row 6 (Identity.Context) is 1/8 with 7×n/a (legitimately n/a, per its own cell). The honest statement is "49/50 + 1 contract-inventory row + 2 contracts-only footnotes" |
| What IS accurate | The per-row artifact pointers (dirs/files) resolve for 47 of 48 checked rows; the endpoint/flow/state-machine counts sampled were accurate wherever recomputable; authz/users/tenants/provisioning/economy/ledgers/content/resources/gamification/trustsafety/social-platform all check out |

## 9. Overall honesty verdict

**Does the documentation deserve the 50/50 claim? Almost — but not as stated.**

What's earned: 345 files of dense, citation-backed documentation; 97.5% of 7,116 citations machine-verified true; 531/547 diagrams parse; gap registers that name real, reproducible defects (trustsafety's caller-less write surfaces, gamification's unseeded permissions, ledgers' no-consumer outbox — all re-verified true here); pinned-commit discipline in the younger packages.

What's not: one 8/8 row rests on a missing artifact (notifications events); 16 diagrams are unparseable; 12 register entries claim open residuals that merges #1268/#1285/#1290/#1294/#1295 already fixed (the exact "STRIDE cites a fixed residual" class the brief predicted); and the tracker — the artifact whose whole job is reconciliation — still contains both the pre- and post-reconciliation truth, plus totals from an older epoch.

**Recommended path to a defensible 50/50 (ordered, all small):**
1. Delete the 25 duplicate rows (55-79) and regenerate "Números consolidados" from the matrix.
2. Author `docs/notifications/04-event-inventory.md` (the census content already exists in 06/09) or re-point row 48 + README + 02 to where the census lives.
3. Fix the 16 Mermaid blocks (§4) — all mechanical.
4. Add post-merge state notes (the learning/07 pattern) to: compliance 07-gaps (N1), identity tenants/users gaps (T1/U5), commerce payments 06/07 (N14/R-P5), commerce/products 09, learning 03 (D8), operations disaster-drill + background-workers.
5. Re-anchor the known +N drift citations (auth spot-audit list + §2 offenders).
6. Stamp the legacy `docs/security/*_DEEP_REVIEW*` reports as historical.

— End of audit. Read-only with respect to all audited packages; this report is the only file added.
