# GameGuild.Learning — Documentation Package

> Learning family modules (`GameGuild.Learning.*`) · Documented against `develop` commit `ac959fd8f`; the security trio (07-09) against `develop` @ `ea4d1f91ce` (post-#1226 / post-#1275 — where the wiring matrix's pre-fix Auth/HOLE columns disagree with 07 on the N1/N6/N7/N10/N17 surfaces, 07 is authoritative).
> Scope: the full Learning family — Courses (programs, content, interactions, prerequisites, checkout), Enrollments, Assessments (+ QuizAdapter, grading runtime, peer review, gradebook), Certificates (incl. blockchain anchoring), Cohorts (+ scheduling), Workspaces, LTI 1.3, Experience modules (Discovery, LearningPaths, Recommendations, Social), and the TestingLab learning bridge.

## Document inventory

| File | Contents |
|------|----------|
| [`01-user-flows.md`](01-user-flows.md) | Mermaid `flowchart TD` user flows — catalog→enrollment→completion (dual enrollment stores), assessment taking (quiz adapter, server-enforced timers, autosave), certificate issuance incl. EVM anchoring, cohort management, LTI 1.3 launch, recommendations/feed — each with an edge-case matrix and file:line per node |
| [`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md) | Route → command/query → validator → handler → auth → rate limit → unit test → HOLE? for all 31 controllers across the family |
| [`03-state-machines.md`](03-state-machines.md) | `stateDiagram-v2` for enrollment, assessment attempt, certificate, cohort, program publication; transitions audit with defect index D1-D8 |
| [`04-event-flow-inventory.md`](04-event-flow-inventory.md) | Four event channels — academic outbox (durable, 5 consumers, backoff posture), certificate EVM outbox, dead telemetry domain events, in-proc TestingLab bridge; audit-event inventory; hosted-services inventory |
| [`05-cross-module-sequences.md`](05-cross-module-sequences.md) | `sequenceDiagram` — Learning↔Economy.AiCredits, ↔Notifications (whatsAppEnabled surface), ↔Compliance.Audit (wired) vs FERPA (not), ↔TestingLab, ↔Identity.Authentication, ↔Commerce, ↔Assets; "not wired" register |
| [`06-gaps.md`](06-gaps.md) | 4 KNOWN + 21 NEW gaps with file:line (headline: certificate eligibility bypass, FERPA unwired, cohort enrollment ungated — N1/N6/N7/N10/N17 since fixed by PR #1275 closing #1226, N3 by the #1218 G5 dead-letter ceiling) |
| [`07-stride-threat-model.md`](07-stride-threat-model.md) | STRIDE for the 12 sub-modules (SL1-SL12): catalog enumeration, self-enroll escalation, cohort guard, assessment cheating (answer leakage / timing / mid-quiz hijack), gradebook authorization, certificate forgery, LTI launch forgery + grade-change attacks, TestingLab bridge scope, personalization inference, workspaces, authoring AI, event backbone — FERPA cross-ref + residual register R-1..R-4 |
| [`08-dfds.md`](08-dfds.md) | L0 context + L1 per major family (catalog/enrollment, assessment, certification, LTI) — student PII marked on every education-record flow, external LTI platform drawn as trust boundary TB3, EVM node TB6 PII-free by construction |
| [`09-security-architecture.md`](09-security-architecture.md) | Component diagram, the eligibility-gate position (post-#1226 N1), the anchoring pipeline (PII-free digests, fail-closed verify), the cohort guard pattern (#1275 — port, shared-context atomicity, optimistic concurrency, fail-closed default), the who-can-grade permission map, security-relevant configuration |

## Conventions

- Follows the merged package conventions (`docs/auth/flows/*`, `docs/commerce/payments/*`): file:line on every node, `L###` shorthand inside Mermaid labels, edge-case matrices as tables, honest gaps section.
- Mermaid labels contain no parentheses and no colons (repo rendering constraint).
- Citation shorthand (`Courses:`, `Assessments:`, …) defined at the top of [`01-user-flows.md`](01-user-flows.md).

## Module shape at a glance

- **Sub-modules (12)**: `GameGuild.Learning` (shared kernel: base controller, audit sink contract, grading value contracts, telemetry events), `Learning.Courses`, `Learning.Enrollments`, `Learning.Assessments`, `Learning.Assessments.QuizAdapter`, `Learning.Certificates`, `Learning.Cohorts`, `Learning.Workspaces`, `Learning.Lti`, `Learning.Experience.Discovery`, `Learning.Experience.LearningPaths`, `Learning.Experience.Recommendations`, `Learning.Experience.Social`, plus the `Learning.TestingLab` bridge (4-file integration shim into `GameGuild.TestingLab`).
- **Controllers (31)**: ProgramCrud, ProgramLifecycle, ProgramContent, ProgramContentAuthoring, ContentInteraction, ActivityGrade, CourseAccess, CourseCheckout, CourseStudents, CourseSupportTickets, LessonInteractionEvents, Prerequisites (Courses); Enrollments (in `EnrollmentModuleApi.cs`); Assessments, GroupSets, PeerReviews, Rubrics, Tasks (Assessments); Certificates; Cohorts, CohortSchedules; LearnerWorkspace (Workspaces); Lti; Discovery, LearningPath, Recommendations, Discussions, Feed, Likes, Replies, Reviews, Wishlists (Experience).
- **Tables**: ~80 mapped tables across the family (`ToTable` + `[Table]`), from `programs`/`program_users` through the grading runtime's 17 tables (`Assessments`, `AssessmentSubmissions`, `GradingExecutions`, `GradeRounds`, `ReviewStages`, `AcademicOutbox*`, …) to the Experience stores (`learning_paths`, `learning_course_recommendations`, `personalized_feed_items`, …).
- **Hosted services (5)**: academic outbox dispatcher, review-stage deadlines, scheduled grade release, executable-version preflight (`Assessments:/AssessmentsModule.cs:104-107`), AI authoring worker (`Courses:/Configuration/CoursesModule.cs:42`).
- **Host composition**: `Host:/Core/Setup/ApiProductComposition.cs:84-95` wires the modules (line numbers current @ `ea4d1f91ce`; the package's original `142-155`/`210` references predate host-side growth); `:238` swaps the no-op certificate anchoring for the EVM adapter (composition `:170-171`); `Host:/Core/Integration/CertificateEvmOutboxServiceRegistration.cs` composes durable anchor publication.
- **Event backbone**: the academic outbox (`grade-result-released` → notification + LTI AGS passback) is the family's only fully durable event machinery; the 16 telemetry domain events in `Shared:/Events/LearningEvents.cs` are dead code (issue #1184 G4; handlers also unregistered — see gaps N2).

## Related reading

- `docs/architecture/ADR-20260903-academic-values-and-gradebook.md`, `ADR-20260903-assessment-completion-and-release.md`, `ADR-20260903-assessment-revisions-and-executable-snapshots.md`, `ADR-20260903-grading-execution-reliability.md`, `ADR-20260903-grading-retention.md` — grading runtime ADRs.
- `docs/architecture/grading-authorization-matrix.md` — who may grade what.
- `docs/api/blockchain-certificate-anchoring.md`, `docs/api/evm-certificate-anchoring-ceremony.md`, `docs/api/evm-certificate-verification.md` — the EVM side of certificates.
- `docs/architecture/LXP_CAPABILITY_ANALYSIS_AND_IMPROVEMENT_PLAN.md` — capability plan (see gap N15 for its stale telemetry claims).
- `docs/architecture/CQRS_BYPASS_KNOWN_DEBT.md` — the bypass debt this family's matrix documents.
