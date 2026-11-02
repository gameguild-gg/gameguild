# Learning Event Flow Inventory

> Learning family modules (`GameGuild.Learning.*`) · Documented against `develop` commit `ac959fd8f`.
> Events published and consumed across the family, the durability posture of each channel, and the honest state of the telemetry pipeline.
> Citation shorthand as defined in [`01-user-flows.md`](01-user-flows.md).

The family uses **four distinct event channels**. Only two of them are durable.

| Channel | Durability | Where |
|---------|-----------|-------|
| 1. Academic outbox (grade-result events) | Durable — DB outbox with per-consumer delivery rows, retries, backoff | `Assessments:/Grading/Persistence/AcademicOutbox.cs`, `GradingEntities.cs` |
| 2. Certificate EVM outbox (integration events) | Durable — journal + worker composition in host | `Host:/Core/Integration/CertificateEvmOutboxServiceRegistration.cs:13-36` |
| 3. MediatR domain events (telemetry) | **In-process only — and in fact never published** | `Shared:/Events/LearningEvents.cs` |
| 4. MediatR in-proc notifications (TestingLab bridge) | In-process, at-least-once via receipt table | `TestingLabBridge:/TestingLabLearningEvidenceHandler.cs:11` |

## 1. Academic outbox — the family's real event backbone

Writer: `AcademicOutboxWriter` (`Assessments:/Grading/Persistence/AcademicOutbox.cs:34`) inserts `AcademicOutboxMessage` (`GradingEntities.cs:1599-1667`) — canonical JSON (1 MiB bound), payload hash, schema version — plus one `AcademicOutboxDelivery` row per registered consumer (`GradingEntities.cs:1673-`).

Dispatcher: `AcademicOutboxDispatcher.DispatchBatchAsync` (`AcademicOutbox.cs:88-116`):

- batch 25 (L85), claim timeout 5 minutes for crashed workers (L86, L96-99),
- claim via `DbUpdateConcurrencyException` race protection (L139-148),
- consumer resolution by frozen `ConsumerKey`; an unregistered consumer is recorded as a failure — not dropped (L150-157),
- on success delivery `Confirm` + message `MarkCompleted` only when **all** deliveries confirmed (L159-174),
- on failure exponential backoff `min 300, 1 shl attempt up to 2^8` seconds (L192), message `MarkFailed`.

Worker: `AcademicOutboxBackgroundService` (`AcademicOutbox.cs:209-236`) polls every 2 s when busy, 5 s idle. Registered at `Assessments:/AssessmentsModule.cs:105`.

Retry/DLQ posture: **unbounded retries with capped 300 s backoff — no dead-letter terminal state.** A permanently failing consumer keeps its delivery cycling Failed→retry forever (`AcademicOutboxStatus.Failed` is recoverable, not terminal). Recorded as gap G-2.

### Outbox event types and consumers

All five consumers are registered in `Assessments:/AssessmentsModule.cs:95-99`.

| Event type | Consumers (Key) | Consumer behavior |
|-----------|-----------------|-------------------|
| `grade-result-released` | `grading.grade-result-released.notification.v1` (`GradeReleaseConsumers.cs:13-70`) | resolves recipient enrollments → dedupe check on `Notification.ReferenceEntityId` → `INotificationService.SendAsync` InApp `AssessmentGraded`; **throws on send failure so the outbox retries** |
| `grade-result-released` | `grading.grade-result-released.passback.v1` (`GradeReleaseConsumers.cs:73-`) | only finalized rounds; `ILtiScorePassback` AGS score publish to the mapped line item |
| release scheduling | `ScheduledGradeReleaseRequestConsumer` (`AssessmentsModule.cs:96`) | schedules `GradeResultReleaseSchedules`; executed by `ScheduledGradeReleaseBackgroundService` (L107) |
| AI review | `AIReviewRequestConsumer` (L97) | enqueues AI review stage work |
| immediate release | `ImmediateGradeReleaseConsumer` (L95) | releases results for `ResultReleaseMode.Immediate` |
| grading finalization | `OfficialGradingFinalizationSink` (L88-89) | writes gradebook projections transactionally |

Related hosted services: `ReviewStageDeadlineBackgroundService` (deadline transitions for review stages, `AssessmentsModule.cs:106`), `AssessmentExecutableVersionPreflightHostedService` (L104).

### Grade-release sub-machines persisted alongside

`GradeResultReleases`, `GradeResultReleaseSchedules` (`GradingEntities.cs:69-81` enums `GradeResultReleaseStatus` Released-only; `GradeResultReleaseScheduleStatus` Scheduled/Cancelled/Released), `GradingExecutions` (`PersistedGradingExecutionStatus`), `GradeRounds` (`PersistedGradeRoundStatus`), `ReviewStages` (`PersistedReviewStageStatus`), `PeerReviewClaims` (`PeerReviewClaimStatus` Assigned/Submitted/Expired/Cancelled).

## 2. Certificate EVM outbox — durable blockchain publication

`ICertificateDurableEvidenceProducer` default is none; the host composes `CertificateEvmEvidenceProducer` + `CertificateEvmPublicationConsumer` handling integration events `CertificateEvmAnchorRequestedV1` and `CertificateEvmRevocationRequestedV1` (`Host:/Core/Integration/CertificateEvmOutboxServiceRegistration.cs:22-35`). `CertificateService` stages the event in the **same transaction** as the certificate row (`Certificates:/Services/CertificateService.cs:102`), so issuance cannot commit without its anchor request being durably queued. The synchronous `ICertificateAnchoring` adapter call afterwards is best-effort telemetry (`CertificateService.cs:123, 513-517`) — the durable path is the outbox.

## 3. Domain telemetry events — defined, handled, never published

`Shared:/Events/LearningEvents.cs` defines 16 events (file L8-366): `CourseViewedEvent`, `CourseEnrolledEvent`, `ContentStartedEvent`, `ContentCompletedEvent`, `CourseCompletedEvent`, `CourseDroppedEvent`, `SearchPerformedEvent`, `SearchResultClickedEvent`, `RecommendationViewedEvent`, `RecommendationClickedEvent`, `RecommendationConvertedEvent`, `LearningPathEnrolledEvent`, `LearningPathCompletedEvent`, `LearningProgressUpdatedEvent`, `CourseRatedEvent`, `CourseWishlistedEvent`, `UserSkillUpdatedEvent`.

Consumers exist: `Recommendations:/Handlers/LearningEventHandlers.cs` implements `IDomainEventHandler<...>` for `CourseCompletedEvent` (L15), `CourseViewedEvent` (L49), `RecommendationConvertedEvent` (L72), `SearchPerformedEvent` (L97), `SearchResultClickedEvent` (L123), `LearningProgressUpdatedEvent` (L152), `UserSkillUpdatedEvent` (L182) — profile/stat/skill maintenance.

**No producer exists.** A family-wide search for constructions/`Publish` of these events returns only the handler file; `RecommendationsModule.cs:6-19` registers neither the handlers nor any dispatcher glue. The pipeline is dead code at both ends while `docs/architecture/LXP_CAPABILITY_ANALYSIS_AND_IMPROVEMENT_PLAN.md:281-329` marks all of it "Complete". This is the package's headline gap (G-1).

Functional equivalents that DID ship: search telemetry is captured synchronously by Discovery's `RecordSearchCommand`/`RecordSearchClickCommand` writing `learning_search_history` (`Discovery:/Controllers/DiscoveryController.cs:355-392`) rather than through events; recommendation view tracking is a direct POST (`Recommendations:/Controllers/RecommendationsController.cs:71`).

## 4. Use-case event contracts — declared "no domain event"

Generated contracts (`Architecture/UseCaseEventContracts.g.cs` per module) count: Courses 77, Assessments 46, LearningPaths 13, Discovery 11, Cohorts 10, Recommendations 10, Enrollments 3, Lti 3 (hand-written `LtiUseCaseEventContracts.cs`). **Every single entry carries `NoDomainEventReason`** — i.e. the whole family declares that its use cases emit only the platform's generic durable operation event, never module domain events. Examples:

- `learning.assessments.create` — "The legacy assessment aggregate currently emits the generic durable operation event only." (`Assessments:/Architecture/UseCaseEventContracts.g.cs`)
- `lti.launch` — "observed through the durable generic operation event" (`Lti:/Architecture/LtiUseCaseEventContracts.cs`)

## 5. In-process notifications (MediatR)

| Event | Publisher | Consumer | Durability |
|-------|-----------|----------|-----------|
| `TestingLearningEvidenceCompletedEvent` (`GameGuild.TestingLab/Events/TestingLearningEvidenceCompletedEvent.cs`) | TestingLab module workflow handlers | `TestingLabBridge:/TestingLabLearningEvidenceHandler.cs:11-30` — writes `testing_lab_learning_evidence_receipts` | at-least-once: receipt idempotency (L16-21) + enrollment/cohort/activity validation throwing on mismatch; **no retry after process restart** (in-proc only) — receipts make replays safe but a crash between publish and handle loses the signal |

## 6. Audit events — transactional, not bus-based

`LearningAuditEvent` records via `ILearningAuditEventSink` (`Shared:/Auditing/ILearningAuditEventSink.cs`) implemented by Compliance.Audit's `CentralLearningAuditEventSink` (`Modules/GameGuild.Compliance.Audit/Services/CentralModuleAuditEventSinks.cs:89-92`, registered `Extensions/AuditModule.cs:143`). Enrollments (`Enrollments:/EnrollmentModuleApi.cs:146-150, 227-245`) and Certificates (`Certificates:/Services/CertificateService.cs:104-108`) write audit rows inside the business transaction. Known action types:

- `Learning.Enrollments.UserEnrolled`, `.ProgressUpdated`, `.StatusChanged`, `.EnrollmentCompleted` (EnrollmentModuleApi L147, 173, 208-214, 240)
- `Learning.Certificates.CertificateIssued` (CertificateService L105)

## 7. Notification touchpoints (published side)

| Call site | Type | Channel |
|-----------|------|---------|
| `GradeReleaseNotificationConsumer` (`Assessments:/Grading/Runtime/GradeReleaseConsumers.cs:50-68`) | `AssessmentGraded` | InApp, deduped by outbox-message id |
| `SendCourseStudentMessageCommandHandler` (`Courses:/Commands/SendCourseStudentMessage/SendCourseStudentMessageCommandHandler.cs:38-44`) | `DirectMessage` | InApp |
| `AssessmentService` (`Assessments:/Services/AssessmentService.cs:1120`) | `System` | — |

## 8. Hosted services inventory (event-adjacent)

| Service | Module | Line |
|---------|--------|------|
| `AcademicOutboxBackgroundService` | Assessments | `AssessmentsModule.cs:105` |
| `ReviewStageDeadlineBackgroundService` | Assessments | `AssessmentsModule.cs:106` |
| `ScheduledGradeReleaseBackgroundService` | Assessments | `AssessmentsModule.cs:107` |
| `AssessmentExecutableVersionPreflightHostedService` | Assessments | `AssessmentsModule.cs:104` |
| `AuthoringAiBackgroundService` | Courses | `Courses:/Configuration/CoursesModule.cs:42`, body `Authoring/AuthoringAiService.cs:1035-1076` (recovers Reserved/Running runs at boot L1062-1075) |

Notably absent: no enrollment-expiry worker, no certificate-expiry worker (D3/D5), no scheduled-publish worker (D8), no social-feed sweeper host.
