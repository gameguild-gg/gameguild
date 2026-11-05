# Learning — STRIDE Threat Model

> Learning family modules (`GameGuild.Learning.*`) · `develop` @ `ea4d1f91ce` (post-#1226 / post-#1275).
> Twelve families cover the twelve sub-modules of the tracker row: catalog+discovery, enrollment, cohorts, assessment taking, grading+gradebook, certificates, LTI, TestingLab bridge, experience personalization, workspaces, authoring AI, shared kernel+event backbone.
> Per family: Spoofing / Tampering / Repudiation / Information disclosure / Denial of service / Elevation of privilege, each with a verdict (MITIGATED · OPEN — see gap · PARTIAL · ACCEPTED by design) and citation. Citation shorthand as defined in [`01-user-flows.md`](01-user-flows.md); gap ids refer to [`06-gaps.md`](06-gaps.md) unless noted.
> **State note**: the fixes for issue **#1226** landed via PR **#1275** (eligibility gate, cohort guard, ownership/capability gates, scheduled-publish worker) plus `7ca692160c` (outbox dead-letter ceiling, #1218 G5). [`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md) was documented against the pre-fix commit `ac959fd8f` — where its Auth/HOLE columns disagree with this file on the N1/N6/N7/N10/N17 surfaces, **this file is authoritative** for the current tree.

## Trust boundaries

- **TB1** — anonymous internet → API. Anonymous-allowlisted surface only: public catalog reads, certificate public verify (+anchor), the three LTI protocol endpoints, anonymous recommendation surfaces. Platform default rate policy; no per-route limits anywhere in the family (gap N4).
- **TB2** — authenticated actor → API. JWT → `ActorContext` (`SubjectIdAsGuid`, `IsSystemAdmin`, `IsTenantAdmin`); every Learning controller carries `[Authorize]`.
- **TB3** — external LTI platform ↔ API (the only outbound/inbound external system). Inbound: OIDC login + signed `id_token` (RS256 over platform JWKS). Outbound: AGS score passback with platform credentials. This boundary is separately drawn in [`08-dfds.md`](08-dfds.md) L1-LTI.
- **TB4** — API → PostgreSQL `public` schema (~80 family tables).
- **TB5** — mutation services → compliance `AuditLog` (same transaction, fail-closed).
- **TB6** — API → EVM blockchain node (RPC + signer). Crosses with **PII-free digests only** (see SL6).

---

## SL1 — Course catalog & discovery (Courses + Experience.Discovery)

Headline threat: **enumeration of unpublished content**.

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | — | n/a | catalog reads carry no identity decisions; actor identity only enters at self-enroll (SL2) |
| T | Draft metadata tampering via public surface | MITIGATED | anonymous surfaces are read-only; writes require `[Authorize]` + course capabilities (SL11, SL2) |
| R | Catalog reads unaudited | ACCEPTED | read-only public surface, no attribution value (`Courses:/Services/ProgramReadService.cs:29-56`) |
| I | **Unpublished / non-public course enumeration** | MITIGATED on anonymous surfaces | `GetPublishedProgramBySlugAsync` forces `Status == Published && Visibility == Public` (`Courses:/Services/ProgramReadService.cs:29-31`); the anonymous `public` listing uses `GetPublicPublishedProgramsAsync` with the same pair (`:52-56`); self-enroll 404s before any write on unpublished/non-public (`Courses:/Controllers/ProgramCrudController.cs:304-308`) — **residual**: authenticated by-id/with-content reads (`ProgramCrudController.cs:175,190`) return any course to any authenticated user without a visibility check (PARTIAL — see 02 matrix, `GET {id}` row) |
| I | Discovery search over other users' history | OPEN — N20 | `Discovery:/Controllers/DiscoveryController.cs:359-363,393` accepts `userId` from query/body — telemetry and history spoofable for other users |
| D | Catalog/listing cost | PARTIAL | pagination `take 50` defaults; but zero per-route rate limits family-wide (N4) and search scans can be unindexed |
| E | — | n/a | no privilege boundary crossed by reads; member → author escalation is SL11 |

## SL2 — Enrollment & monetization gate (Enrollments + Courses checkout)

Headline threat: **self-enroll privilege escalation** — enroll self or others without paying, without ownership, or into closed windows. The #1226 N6 fix is the mitigation.

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Enroll impersonating another user | MITIGATED | self-enroll takes the user id from the auth context only (`ProgramCrudController.cs:298-302` — `GetCurrentUserId`, 401 when absent); `POST api/learning/enrollments` with a body `UserId` is owner-scoped post-#1275 — self or system admin, else 403 (`Enrollments:/EnrollmentModuleApi.cs:445-450`, helper `CanAccessUserData` `:383-386`) |
| T | Free ride on a monetized course | MITIGATED | self-enroll 402 when linked products exist or price > 0 (`ProgramCrudController.cs:323-331`); enrollment only after `CompleteCourseCheckoutCommand`, idempotent with `AlreadyHadAccess` (`Courses:/Controllers/CourseCheckoutController.cs:47-58`) |
| T | Enrollment window / capacity bypass | MITIGATED (window) · OPEN (prereqs) | `IsEnrollmentOpen` gate 409 (`ProgramCrudController.cs:313-320`, entity gate `Courses:/Entities/Program.cs:195-198`); but neither self-enroll nor admin-enroll consults `IPrerequisiteService` — N16 |
| T | Progress/status tampering on another user's enrollment | MITIGATED | progress and status mutations owner-scoped post-#1275 (`EnrollmentModuleApi.cs:474-505`); clamped 0-100 (`Enrollments:/Entities/Enrollment.cs:37`) |
| R | Enrollment/drop denied | MITIGATED | enrollment + `UserEnrolled` audit in one transaction, audit failure rolls back (`EnrollmentModuleApi.cs:163-170`) |
| I | Cross-user enrollment read | MITIGATED | `GET {id}` and `GET users/{userId}` owner-scoped (`EnrollmentModuleApi.cs:410-421`); roster read requires course Edit capability (PR #1275) |
| D | Enrollment write amplification | ACCEPTED | one audited transaction per mutation |
| E | **Arbitrary-status self-completion** (progress → `Completed` without doing the work) | PARTIAL | `POST {id}/status/{status}` is owner-scoped but still lets the owner set `Completed` on their own enrollment without content completion — the certificate path no longer trusts it (SL6 gate), yet the learner UX does; dual-store divergence compounds it (N21: `program_users` vs `learning_enrollments`, unioned only at read — `Enrollments:/CourseEnrollmentAccessReader.cs:14-33`) |

## SL3 — Cohorts & scheduling

Headline threat: capacity/state bypass on cohort-tagged enrollment. The #1226 N17 guard (PR #1275) is the mitigation.

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | — | n/a | actor identity enters via SL2; cohort mutations are capability-gated |
| T | **Enroll into a closed/full cohort** | MITIGATED | `ICohortEnrollmentGuard.ReserveCapacityAsync` checks `Cohort.CanEnroll()` (open + active + spare capacity) and stages the seat increment on the shared `DbContext` so it commits in the same transaction as the enrollment row (`Learning:/Abstractions/ICohortEnrollmentGuard.cs:17-27`, `Cohorts:/Services/CohortEnrollmentGuard.cs:17-55`); optimistic-concurrency `Version` turns two racers for the last seat into a mapped 409 |
| T | Seat leak on drop | MITIGATED | drop/expire releases the seat exactly once, compensating best-effort — a vanished cohort must not block a drop (`EnrollmentModuleApi.cs:249-258`) |
| T | Ungated cohort path when guard unconfigured | MITIGATED (fail-closed) | no guard → `Error.Failure("Cohort", …)` before any write (`EnrollmentModuleApi.cs:149-154`) |
| T | Cohort state machine escape (resurrect Completed/Cancelled) | OPEN — N11 | `Open()` flips any cohort to Active unconditionally (`Cohorts:/Entities/Cohort.cs:51-56`); Complete/Cancel callable from Scheduled (`:77-89`) |
| R | Cohort create/open/close/complete/cancel unaudited | PARTIAL | lifecycle mutations are capability-gated post-#1275 (`Cohorts:/Controllers/CohortsController.cs:43,112,130,148,166,184,202`) but the family's audit events center on enrollments/certificates — cohort transitions carry no dedicated audit event |
| I | Roster/calendar disclosure | PARTIAL | schedule + calendar reads (`CohortsController.cs` GET routes) are `[Authorize]`-only — no membership check; tenant inferred from actor (`:43-47` in the pre-fix doc, unchanged) |
| D | Schedule generation cost | ACCEPTED | preview/generation is staff-triggered, bounded by content count |
| E | Member creates/modifies cohorts | MITIGATED | create `RequireContentTypePermission<Cohort>(Create)` (`CohortsController.cs:43`); update/open/close/complete/cancel `RequireResourcePermission(…Edit)` (`:112,130,148,166,184`); delete `Delete` (`:202`) — post-#1275 N7 fix |

## SL4 — Assessment taking (Assessments + QuizAdapter)

Headline threat: **cheating** — answer leakage, timing attacks, session hijack mid-quiz.

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Taking an exam as someone else (session hijack mid-quiz) | PARTIAL | every route requires `[Authorize]` + `CourseCapability.Learn` on the assessment's course (`Assessments:/Controllers/AssessmentsController.cs:485,524,1307,1576`); the JWT honors `TokenVersion` so a detected hijack is revocable — but there is **no per-attempt re-authentication or attempt-bound token**: a stolen cookie mid-quiz can autosave/submit as the victim until revocation. Recorded as residual R-1 |
| T | **Answer-key leakage in delivery** | MITIGATED | learner delivery is redacted per item type by an explicit field allowlist — `QuizDeliveryGenerator.RedactEntry` copies only `type/stem/points/settings` + per-type safe fields; `RedactBlanks` strips blank answers, MATCHING pairs are redacted (`QuizAdapter:/QuizDeliveryGenerator.cs:11-14,36-60`); answer material only re-enters server-side via `QuizAnswerDecoder` at grading time |
| T | **Timing attack** (submit past the limit, outside window) | MITIGATED | client timer is advisory; the server compares UTC stamps at submit — `TryGetSubmissionTiming` rejects past-limit, pre-open, post-close, and gates late submits on `AllowLateSubmissions`/`LateSubmissionDeadline` (`Assessments:/Grading/Runtime/AssessmentGradingRuntimeService.cs:1643-1670`) |
| T | Edit answers after submit | MITIGATED | `SetPayload` throws unless `InProgress` (`Assessments:/Entities/Assessment.cs:748-751`); duplicate submit rejected (`:792-795`) |
| T | Graded against a tampered/drifting definition | MITIGATED | learner graded against the immutable `AssessmentDefinitionRevision` captured at attempt start (`Assessment.cs:636-640`); revision hash preflight at publish (`Assessments:/AssessmentsModule.cs:107-108`) |
| T | Second attempt after seeing answers | MITIGATED | runtime supports exactly one attempt — `MaxAttempts != 1` rejects (`AssessmentGradingRuntimeService.cs:1635-1638`) |
| R | Draft/submit/grade attribution | MITIGATED | grading commands idempotent with receipts (`AssessmentGradingRuntimeService.cs:1691-1700`); grade release audited through the outbox consumers (SL12) |
| I | Another learner's submission | MITIGATED | submission reads Learn-capability + owner checks (`AssessmentsController.cs:487,526,560,600` Forbid paths); peer reviewers see only assigned submissions (SL5) |
| I | Payload-borne exfiltration | MITIGATED | payload fields bounded 2048 chars, URLs normalized http/https (`Assessment.cs:910-939`) |
| D | Autosave/grading flooding | PARTIAL | no per-route rate limits (N4); grading runtime serialized per execution with optimistic version checks on collective drafts (`Assessment.cs:808-828`) |
| E | Non-learner starts an attempt | MITIGATED | start policy validates window + attempts (`AssessmentGradingRuntimeService.cs:1631-1639`) behind the Learn capability check |

## SL5 — Grading, peer review & gradebook

Headline threat: unauthorized grade changes; retaliatory/fraudulent peer grading. See the permission map in [`09-security-architecture.md`](09-security-architecture.md) §5.

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Reviewer impersonation in peer review | MITIGATED | claim is actor-bound and single-owner — `PeerReviewStageHandler` claim/submit (`Assessments:/Controllers/AssessmentsController.cs` claim L379, submit L428); group members of the target are excluded (grading-authorization-matrix) |
| T | **Grade tampering outside the runtime** | MITIGATED | manual `Grade` cannot touch runtime-graded submissions — regrade only via `ApplyRuntimeGrade(allowRegrade)` (`Assessment.cs:877-897`); instructor override/regrade require `Program.{id}.Review` (grading-authorization-matrix) surfaced in-controller as `CourseCapability.StaffReview` (`AssessmentsController.cs:1699-1700`) |
| T | Released grade altered in transit to LTI | MITIGATED | passback consumer throws on non-finalized rounds and retries with backoff (`Assessments:/Grading/Runtime/GradeReleaseConsumers.cs:97-100`); dead-lettered after 5 attempts (SL12) |
| R | Override/regrade denial | MITIGATED | override records actor, previous + new value, required reason (grading-authorization-matrix invariants; `docs/architecture/grading-authorization-matrix.md`) |
| I | **Gradebook disclosure** (FERPA education record) | PARTIAL | gradebook-by-enrollment read: instructor view via `StaffReview`, else resolves the enrollment to the actor and Forbids foreign reads (`AssessmentsController.cs:915-941`); analytics gated `CanEditOrReview` (`:905-907`) — but no FERPA disclosure gate exists anywhere (see FERPA cross-ref below) |
| D | Gradebook projection cost | ACCEPTED | single canonical projection per enrollment, read-path only |
| E | Learner grades peers/self for advantage | PARTIAL | self/peer review evidence is draft-bound, finalization unique, aggregate projected only after finalize (grading-authorization-matrix "Sujeito coletivo") — review **weights** set at authoring are Edit-gated; a malicious author stacking peer weight is inside their own course |

## SL6 — Certificates & EVM anchoring

Headline threat: **forgery** — a certificate for work never done. The #1226 N1 eligibility gate (PR #1275) + blockchain anchoring are the mitigations.

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Issue a certificate as/for another learner | MITIGATED | the gate inside `IssueCertificateAsync` verifies the enrollment **belongs to the requested recipient** (403) and course (409) — `Certificates:/Services/CertificateService.cs:52-99`; by-id read is recipient-scoped post-#1275 (`Certificates:/Controllers/CertificatesController.cs:73-89` via `CanAccessUserData`) |
| T | **Certificate for an incomplete/phantom enrollment** | MITIGATED | gate verifies enrollment exists (404) and is `Completed`/`CompletedWithCertificate` (409) — `CertificateService.cs:59-99`; baked into issuance so the endpoint command and any completion flow inherit it; duplicate per enrollment+template conflicts (`:101-108`) |
| T | **Number forgery / local DB edit passed off as real** | MITIGATED (anchored certs) | anchor verification recomputes the canonical SHA-256 digest and requires the chain record to corroborate it — mismatch surfaces `Mismatch` with both digests, absence `NotAnchored`, backend errors `Unreachable` (fail-closed, never a fake pass) (`Host:/Core/Integration/BlockchainCertificateAnchoringAdapter.cs:92-144`); public verify endpoints are number-based and anonymous (`CertificatesController.cs:96-98,120-122`) |
| T | Anchor poisoning with PII | MITIGATED | the adapter publishes only the SHA-256 of a canonicalized PII-free payload; recipient name reduced to a digest before leaving the class, revocation reason never published (`BlockchainCertificateAnchoringAdapter.cs:16-18,79-87`) |
| T | Revocation evasion | PARTIAL | revoke is terminal (`Certificates:/Entities/Certificate.cs:120-126`) and revocation is anchored best-effort (`BlockchainCertificateAnchoringAdapter.cs:54-77`); a never-anchored certificate's revocation is skipped with a debug log — local status still authoritative on the verify endpoint |
| R | Issue/revoke/renew denial | MITIGATED | audited transactions — `BeginAuditedMutationTransactionAsync` + `Learning.Certificates.*` audit events, failure rolls back (`CertificateService.cs:143-148` issue, `:344-349` revoke, `:407-411` renew, `:533-536` template paths) |
| I | Public verify as an oracle | MITIGATED | unknown numbers return an invalid verdict **with Revoked status, audited** — no existence leak beyond verdict (`CertificateService.cs:243-` verify path); anchor verify response carries only PII-free evidence (digests, tx/network ids, block, timestamp — `CertificatesController.cs:110-118` remarks) |
| I | Course-wide recipient listing | MITIGATED | staff-gated on course Edit capability post-#1275 (`CertificatesController.cs:199-214`) |
| D | Anchoring backend down | MITIGATED (issuance) / FAIL-CLOSED (verify) | issuance never blocks on the chain — best-effort with durable retry via the EVM outbox (`CertificateService.cs:167`, `AnchorIssuedCertificateSafelyAsync:566`; consumer `Host:/Core/Integration/CertificateEvmPublicationConsumer.cs:16-50`); verification reports `Unreachable` rather than guessing |
| E | Member issues/revokes/renews certificates or templates | MITIGATED | `RequireResourcePermission<PermissionType, Certificate>` Create/Delete/Edit on issue/revoke/renew (`CertificatesController.cs:138,166,183`); template reads/writes `RequireContentTypePermission<CertificateTemplate>` (`:221-223`+); the **interactive** eligibility query remains advisory (`CheckEligibilityAsync` `CertificateService.cs:455-510` — comment names the authoritative gate) |

## SL7 — LTI 1.3 (external tool launch)

GameGuild is the tool; the platform is external and untrusted at TB3. Headline threats: **launch forgery** and **grade change attacks**.

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | **Forged launch** (crafted id_token / unknown platform) | MITIGATED | login requires the `iss`+`client_id`+`deployment_id` triple to match an active deployment (401 before state issuance) (`Lti:/Controllers/LtiController.cs:88-92`); the launch `id_token` is RS256-validated against the platform JWKS (`Lti:/Commands/LtiEndpointCommands.cs:68-71`) |
| Sp | Replay of a captured launch | MITIGATED | `state` + `nonce` are single-use — `stateStore.TryConsume` (`LtiEndpointCommands.cs:74-83`); `sub`+`nonce` claims required (`:74-78`) |
| Sp | Token leakage into logs/URLs | MITIGATED | `id_token` in the query string is rejected outright before parse (`LtiController.cs:137-140`); authorization redirect must be https, absolute, credential-free — else 400 (`:265-282`, `TryBuildAuthorizationRedirect`) |
| Sp | Platform user maps to a local account they don't own | MITIGATED | mapping by `sub` then email resolves an existing account; `UserNotFound` — **no JIT provisioning** (`LtiEndpointCommands.cs:86-101`) |
| T | **Grade change attack via AGS inbound** | MITIGATED (by absence) | inbound AGS/NRPS sync is not implemented (gap N13) — there is no inbound grade-writing surface to attack; grades only flow **out** |
| T | Outbound passback of a non-final grade | MITIGATED | only `grade-result-released` outbox events trigger `ILtiScorePassback`; non-finalized rounds throw and retry (`Assessments:/Grading/Runtime/GradeReleaseConsumers.cs:73-100`); line-item mapping duplication 409s (`LtiController.cs:234-236`) |
| T | Session-cookie theft mid-launch (iframe surface) | ACCEPTED (with compensations) | `gg_session` is `SameSite=None` because the tool runs in the platform iframe (`LtiController.cs:176-184`) — the LTI state/nonce protocol is the anti-CSRF compensation; session JWT honors `TokenVersion` for revocation |
| R | Deployment registration / line-item admin | MITIGATED | system-admin-only inline checks (`LtiController.cs:192-195,215-218`); tool JWKS served for excluded deployments logs errors (`:57-60`) |
| I | Cross-tenant launch data | PARTIAL | deployment triple is tenant-scoped; but a compromised platform can assert any `login_hint` — the local mapping check is the only brake (acceptable under LTI trust model) |
| D | Launch/protocol flooding | PARTIAL | the three protocol endpoints are anonymous (by protocol necessity) with platform-default rate limits only (N4) |
| E | Launch as admin | MITIGATED | launch resolves to a learner session; admin endpoints re-check `IsSystemAdmin` server-side |

## SL8 — TestingLab bridge (Learning.TestingLab)

Headline threat: **session data exfiltration** + evidence forgery from the lab side.

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Forged completion evidence (spooofed TestingLab event) | PARTIAL | the bridge is an in-process `INotificationHandler` — only code inside the API boundary can raise `TestingLearningEvidenceCompletedEvent`; the handler re-verifies course ownership, active `ProgramUser`, and (cohort case) a non-dropped cohort enrollment before writing the completion effect (`TestingLabBridge:/TestingLabLearningEvidenceHandler.cs:16-70`) |
| T | Replay/duplicate evidence inflating completions | MITIGATED | idempotency fence — `testing_lab_learning_evidence_receipts` keyed by `EvidenceId`, duplicate events return without effect (`TestingLabLearningEvidenceHandler.cs:16-21`) |
| T | Evidence completing activity in the wrong course/tenant | MITIGATED | activity must belong to the configured course, be non-deleted, tenant-consistent (`:23-33`); enrollment must be active and tenant-consistent (`:35-48`) |
| R | Completion-effect denial | PARTIAL | receipt write is silent to audit (no dedicated audit event); a lost in-proc event on restart loses the completion entirely — durable channel absent (documented in [`04-event-flow-inventory.md`](04-event-flow-inventory.md) §5) |
| I | **Session data exfiltration into Learning** | MITIGATED (scope) | the event carries identifiers only — `EvidenceId`, `LearningActivityId`, `CourseId`, `UserId`, `CohortId`, `TenantId` (`TestingLabLearningEvidenceHandler.cs:14-70` consumes exactly these); **no TestingLab session content, replays, or artifacts cross into Learning tables** — the session-data exfiltration surface itself lives in Platform.TestingLab (coverage row 38, docs pending); Learning stores only the receipt + interaction effect |
| D | Evidence storm | ACCEPTED | in-proc fan-out, one receipt row per evidence id |
| E | — | n/a | the bridge writes learner-scoped effects only; no admin surface |

## SL9 — Experience personalization (Recommendations + Social + LearningPaths)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Telemetry spoofing for another user (view/dismiss as them) | PARTIAL | `me`-scoped recommendation actions resolve the actor from context (`Recommendations:/Controllers/RecommendationsController.cs:38,56,71,83,95`); but the Enrollment/Discovery history surfaces still take `userId` from query/body — N20 (`Enrollments:/EnrollmentModuleApi.cs:330-336`, `Discovery:/Controllers/DiscoveryController.cs:359-363,393`) |
| T | Feed/recommendation tampering | MITIGATED | writes are self-scoped rows (`personalized_feed_items`, `learning_course_recommendations`); moderation actions are capability-gated post-security-batch (06 N7 rows marked landed) |
| R | Curation/moderation actions | PARTIAL | gated but see 06 N7 for the audit posture of curation writes |
| I | **Inference disclosure from anonymous surfaces** | ACCEPTED (bounded) | `popular`/`trending`/`similar` are anonymous aggregates (`RecommendationsController.cs:192-229`) — no per-learner data; skill-profile self-service is `me`-scoped (`:155-174`) |
| I | Stale feed items retention | OPEN | expired items cleared only when `ClearExpiredFeedItemsAsync` is invoked — no hosted sweeper (`Social:/Services/FeedService.cs:175`) |
| T | Dead telemetry pipeline | KNOWN-DEAD | the 16 `LearningEvents.cs` telemetry events have no producers (K1 / #1184 G4); handlers are centrally registered and pinned by test post-#1275 (N2 resolved-as-pinned) — no spoofing surface while dead |
| D | Strategy flood | MITIGATED | per-strategy candidate cap 20, per-strategy failure swallowed (`Recommendations:/Services/RecommendationEngine.cs:17,66-78`) |
| E | — | n/a | self-scope only |

## SL10 — Workspaces (LearnerWorkspace)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Dashboard as another learner | MITIGATED | `me/dashboard` and `me/search` resolve the actor from `ActorContext` (`Workspaces:/Controllers/LearnerWorkspaceController.cs:17,54` — `SubjectIdAsGuid is not Guid` → 401) |
| I | Course workspace without enrollment | PARTIAL | `courses/{courseId}/workspace` executes with actor + course ids (`:33-53`); the query returns null → 404 when the learner has no workspace — the enrollment binding is query-side, weaker than an explicit capability check (residual R-2) |
| T/R/D/E | — | n/a | read-only module (3 GET routes, `[Authorize]` at `:12`); no mutation surface |

## SL11 — Content authoring & AI (Courses authoring + Economy.AiCredits)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | AI-run attribution | MITIGATED | entitlement/runs resolve the author from context; wallet reserve/settle keyed by run + idempotency key ([`05-cross-module-sequences.md`](05-cross-module-sequences.md) §1) |
| T | **Prompt injection via authored content → poisoned assessments** | PARTIAL | generated content lands in drafts and must pass revision prepare/publish (hash preflight `Assessments:/AssessmentsModule.cs:104-107`); no content-injection scanner exists — reviewer is the control (ACCEPTED at current scale) |
| T | Credit theft (settle without run) | MITIGATED | wallet ports are fail-closed; a wallet outage blocks new runs; settlement crash-safe via `AuthoringAiBackgroundService` boot recovery (05 §1) |
| R | AI spend attribution | MITIGATED | quote/reserve/settle/release each idempotency-keyed (05 §1) |
| I | Draft content leakage | PARTIAL | authoring surface is `[Authorize]`-only — no creator/tenant guard on update observed (02 matrix G-5 rows) |
| D | AI-run flooding (cost amplification) | OPEN — N4 | `ai-runs` (`Courses:/Controllers/ProgramContentAuthoringController.cs:157`) has no rate limit — the wallet balance is the only ceiling |
| E | Member authors content | MITIGATED | publication requires `CourseCapability.Publish` (`Courses:/Controllers/ProgramLifecycleController.cs` capability attributes); scheduled publish now honored via `PublishDueProgramsWorker` sweeping Draft/Review only (N10 fixed — `Courses:/Services/ScheduledPublishWorker.cs`) |

## SL12 — Shared kernel, audit sink & event backbone

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Audit events with forged actor | MITIGATED | actor/tenant backfilled from ambient context, never payload-only (audit sink contract; see `docs/compliance/09-security-architecture.md` sink pattern) |
| T | Audit bypass (mutate without audit) | MITIGATED | `BeginAuditedMutationTransactionAsync` requires the owning transaction and fails closed — audit failure rolls back the business write (`Enrollments:/EnrollmentModuleApi.cs:163-170`; certificates `CertificateService.cs:143-148`) |
| T | Outbox poisoning (infinite retry of a broken consumer) | MITIGATED | dead-letter after 5 attempts with terminal `DeadLettered` status + metrics + replay transition (`Assessments:/Grading/Persistence/AcademicOutbox.cs:89-94,202-213`; `GradingEntities.cs:1667,1790-1801`) — #1218 G5 |
| R | Grade release denial | MITIGATED | `grade-result-released` deliveries are durable, backoff-capped, and now terminally dead-lettered with a replay path |
| I | PII into audit/events | PARTIAL | audit events carry shape-level data (ids, statuses); the EVM evidence path is PII-free by canonicalizer (SL6); telemetry events would carry interaction data but are dead (K1) |
| D | Outbox dispatch cost | ACCEPTED | bounded claim batches; hosted dispatcher |
| E | Helper misuse — `CanAccessUserResource` | PARTIAL | the base helper grants system **and tenant** admins cross-user access (`Shared:/Abstractions/LearningControllerBase.cs:125-147`) while the #1275 enrollment gate grants only system admins (`EnrollmentModuleApi.cs:383-386`) — two admin tiers, deliberately different; documented here as R-3 |

---

## FERPA cross-ref — education records without a FERPA gate

Learning data **is** education-record material under FERPA's regulatory definition: enrollments, assessment submissions, grades/gradebook projections, survey and reflection responses, and completion evidence all directly identify a student. Two findings frame the family's posture:

1. **The FERPA↔Learning handoff does not exist.** Zero references to `IFerpaService` or any FERPA type exist anywhere in `GameGuild.Learning.*`; education records are never registered with the FERPA module and disclosure-sensitive surfaces (gradebook `Assessments:/Controllers/AssessmentsController.cs:915`, survey results, reflection responses) do not consult disclosure consents. This is the **structural finding registered during the compliance audit (#1220's package)** and verbatim documented as "the handoff that does not exist" — `docs/compliance/10-cross-module-sequences.md:85-102` (consequence: "disclosures logged by FERPA cover only records someone manually registered; the platform's actual education data (Learning) is invisible to the FERPA machinery"), summary row `:162`. Learning-side gap id: **N5** (filed in [`06-gaps.md`](06-gaps.md)).
2. **What Learning does on its own**: ownership scoping post-#1275 (SL2/SL6), instructor-vs-own-enrollment gradebook split (SL5), PII-free anchoring (SL6). These are access controls, not FERPA disclosure controls — directory-information policy, guardian assertions, and disclosure logging all live unused on the FERPA side (`Modules/GameGuild.Compliance.FERPA/Services/FerpaService.cs:3-27`), where #1220 additionally found processor ids taken from request bodies and caller-asserted guardians. Until the handoff is built, any FERPA audit of this platform must treat Learning authorization as the only active control plane over education records.

## Residual register (new, from this model)

| # | Residual | Family | Suggested control |
|---|---|---|---|
| R-1 | No per-attempt re-authentication / attempt-bound token for quiz sessions — a stolen mid-quiz session can act until revoked | SL4 | bind autosave/submit to a short-lived attempt token minted at start |
| R-2 | Course-workspace read relies on query-side null rather than an explicit enrollment/capability check | SL10 | explicit `Learn` capability check on the workspace route |
| R-3 | Two divergent admin tiers — base helper allows tenant admins cross-user, #1275 gates allow only system admins | SL12 | pick one tier per surface deliberately and pin with tests |
| R-4 | Authenticated by-id catalog/course reads lack a visibility check (unpublished enumeration by members) | SL1 | published/visibility filter or capability check on by-id reads |

## Prioritized mitigations

1. **N5 / FERPA handoff** (with #1220's FERPA-side holes) — register education records and consult disclosure consents on gradebook/survey/reflection surfaces; until then this model's SL2/SL5/SL6 rows are the compensating controls.
2. **N4** — pin `EnableRateLimiting` on the expensive/anonymous surfaces first (LTI protocol trio, ai-runs, bulk roster ops, verify endpoints).
3. **N11** — from-state guards on cohort `Open`/`Complete`/`Cancel` (state-machine defect D6).
4. **N16** — consult `IPrerequisiteService` in the enroll funnel (needs the `Result<>` contract noted in PR #1275).
5. **R-1/R-2/R-4** — the three cheap residual controls above.
6. **N20/N21** — actor-scoped telemetry/history + reconciliation job between the two enrollment stores.
