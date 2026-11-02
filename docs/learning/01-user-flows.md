# Learning User Flows

> Learning family modules (`GameGuild.Learning.*`) · Documented against `develop` commit `ac959fd8f`.
> Mermaid `flowchart TD` per flow, file:line on every node, `L###` shorthand inside labels, edge-case matrix after each flow.
> Mermaid labels contain no parentheses and no colons (repo rendering constraint).

Citation shorthand used throughout this package:

- `Shared:` = `apps/api/Source/Modules/GameGuild.Learning/`
- `Courses:` = `apps/api/Source/Modules/GameGuild.Learning.Courses/`
- `Enrollments:` = `apps/api/Source/Modules/GameGuild.Learning.Enrollments/`
- `Assessments:` = `apps/api/Source/Modules/GameGuild.Learning.Assessments/`
- `QuizAdapter:` = `apps/api/Source/Modules/GameGuild.Learning.Assessments.QuizAdapter/`
- `Certificates:` = `apps/api/Source/Modules/GameGuild.Learning.Certificates/`
- `Cohorts:` = `apps/api/Source/Modules/GameGuild.Learning.Cohorts/`
- `Lti:` = `apps/api/Source/Modules/GameGuild.Learning.Lti/`
- `Discovery:` = `apps/api/Source/Modules/GameGuild.Learning.Experience.Discovery/`
- `LearningPaths:` = `apps/api/Source/Modules/GameGuild.Learning.Experience.LearningPaths/`
- `Recommendations:` = `apps/api/Source/Modules/GameGuild.Learning.Experience.Recommendations/`
- `Social:` = `apps/api/Source/Modules/GameGuild.Learning.Experience.Social/`
- `Workspaces:` = `apps/api/Source/Modules/GameGuild.Learning.Workspaces/`
- `TestingLabBridge:` = `apps/api/Source/Modules/GameGuild.Learning.TestingLab/`
- `Host:` = `apps/api/Source/GameGuild.API/`

---

## 1. Course catalog to enrollment to completion

The platform runs **two parallel enrollment stores**: `Courses` owns `program_users` (legacy, powers the learner UX and progress) and `Enrollments` owns `learning_enrollments` (stateful lifecycle, powers cohorts and certificates). Both are consulted by `CourseEnrollmentAccessReader` (`Enrollments:/CourseEnrollmentAccessReader.cs:14-33`) — "enrolled" means a row in either.

```mermaid
flowchart TD
    A["Learner browses catalog<br>GET v1-courses-public L127-136<br>Courses-Controllers-ProgramCrudController"] --> B{"Course published and public?<br>Status equals Published and<br>Visibility equals Public L305"}
    B -- no --> B1["404 NotFound L306-307"]
    B -- yes --> C["Self-enroll<br>POST id-self-enroll L297-298"]
    C --> D{"Enrollment window open?<br>Program.IsEnrollmentOpen L314<br>Courses-Entities-Program L195-198"}
    D -- closed or full --> D1["409 Enrollment closed L315-320"]
    D -- open --> E{"Course monetized?<br>linked products or price above 0<br>L319-331"}
    E -- yes --> F["402 Payment required L322-327"]
    F --> G["Commerce checkout then<br>POST courseId-checkout-complete L17-18<br>Courses-Controllers-CourseCheckoutController"]
    G --> H["CompleteCourseCheckoutCommand<br>returns EntitlementId plus EnrollmentIds"]
    E -- no --> I["AddUserToProgramEndpointCommand L333<br>ProgramUser row in program_users"]
    H --> I
    I --> J["Interaction create<br>POST v1-course-interactions L21-23<br>Courses-Controllers-ContentInteractionController"]
    J --> K["Progress update autosave<br>PUT interactionId-progress L43-45"]
    K --> L["Content submit<br>POST interactionId-submit L66-68"]
    L --> M["Content complete<br>POST interactionId-complete L89-91"]
    M --> N{"All required content complete?"}
    N -- no --> K
    N -- yes --> O["Course progress reaches 100<br>UpdateProgress clamps and completes<br>Enrollments-Entities-Enrollment L35-45"]
    O --> P["Enrollment Completed plus<br>dedicated EnrollmentCompleted audit event<br>Enrollments-EnrollmentModuleApi L207-214"]
    P --> Q["Certificate eligible<br>HasCertificateAsync false<br>Certificates-Services-CertificateService L164-169"]
```

Note: the Mermaid edge labels avoid parentheses/colons. The monetized gate is `GetLinkedProductsAsync` + `GetProgramPricingAsync` at `Courses:/Controllers/ProgramCrudController.cs:319-331`.

### Edge-case matrix — catalog, enrollment, completion

| # | Case | Behavior | Where |
|---|------|----------|-------|
| 1 | Unpublished or non-public course self-enroll | 404 before any write | `Courses:/Controllers/ProgramCrudController.cs:305-307` |
| 2 | Enrollment deadline passed / capacity full / status Closed | 409 Conflict with ProblemDetails | `Courses:/Controllers/ProgramCrudController.cs:314-320`, gate `Courses:/Entities/Program.cs:195-198` |
| 3 | Monetized course self-enroll attempt | 402 Payment Required — no free ride | `Courses:/Controllers/ProgramCrudController.cs:319-327` |
| 4 | Checkout completed twice / replay | `AlreadyHadAccess` flag in response, idempotent | `Courses:/Controllers/CourseCheckoutController.cs:47-58` |
| 5 | Duplicate enrollment via `Enrollments` module | `GetActiveAsync` returns existing row, no second insert | `Enrollments:/EnrollmentModuleApi.cs:132-137` |
| 6 | Progress above 100 or below 0 | `Math.Clamp` to 0-100 | `Enrollments:/Entities/Enrollment.cs:37` |
| 7 | Progress hits 100 while Paused | No auto-complete — auto-complete only from Active | `Enrollments:/Entities/Enrollment.cs:41-44` |
| 8 | Enrollment + its audit event | Same DB transaction; audit failure rolls back enrollment | `Enrollments:/EnrollmentModuleApi.cs:141-159` |
| 9 | Cohort capacity exceeded | `CanEnroll` false — but enroll path does not call it (gap G-16) | `Cohorts:/Entities/Cohort.cs:49` |
| 10 | Testing Lab evidence completes an activity | Idempotent receipt + active-enrollment guard | `TestingLabBridge:/TestingLabLearningEvidenceHandler.cs:16-30` |
| 11 | Prerequisite not satisfied | `CheckPrerequisites` reports unmet chain — self-enroll does not consult it (gap G-15) | `Courses:/Controllers/PrerequisitesController.cs:120-151` |

---

## 2. Assessment taking — quiz adapter, timers, autosave

Assessments run on the executable-definition runtime: authoring produces an immutable `AssessmentDefinitionRevision`, the learner interacts with a runtime submission, and grading executes review stages. The quiz flavor is contributed by the `QuizAdapter` module.

```mermaid
flowchart TD
    A["Author drafts assessment<br>PUT course-contentId-draft L76<br>Assessments-Controllers-AssessmentsController"] --> B["Prepare revision<br>POST id-revisions-prepare L124<br>immutable AssessmentDefinitionRevision"]
    B --> C["Publish revision<br>POST id-revisions-publish L154<br>sets PublishedDefinitionRevisionId"]
    C --> D["Learner starts attempt<br>POST id-runtime-submissions-individual L461<br>or contentId variant L504"]
    D --> D1{"ValidateStartPolicy L1631-1639<br>Grading-Runtime-AssessmentGradingRuntimeService"}
    D1 -- "window closed or attempts not 1" --> D2["reject — runtime supports exactly one attempt"]
    D1 -- ok --> E["Submission InProgress<br>AssessmentSubmission.StartIndividual L666-698"]
    E --> F["Autosave draft<br>PUT runtime-submissions-submissionId-draft L611<br>payload guarded by Status equals InProgress L748-751"]
    F --> G["Server timer enforcement at submit<br>TryGetSubmissionTiming L1643-1670<br>submittedAt past startedAt plus TimeLimitMinutes"]
    G --> H["Submit<br>POST runtime-submissions-submissionId-submit L640<br>Status to Submitted or Late L790-806"]
    H --> I["Grading execution orchestrator<br>GradingExecutions row plus ReviewStages"]
    I --> J{"ReviewMethods bitmask"}
    J -- quiz automated --> K["QuizAutomatedReviewStageHandler<br>QuizAdapter L registered Assessments-QuizAdapter-QuizAdapterModule"]
    K --> K1["QuizDeterministicReviewAlgorithm<br>decodes answers via QuizAnswerDecoder"]
    J -- instructor --> L["InstructorReviewStageHandler"]
    J -- self --> M["SelfReviewStageHandler<br>POST self-review L690"]
    J -- peer --> N["PeerReviewStageHandler<br>claim L379 submit L428"]
    J -- AI --> O["AIReviewStageHandler"]
    I --> P["Grade round finalized<br>ApplyRuntimeGrade L845-858"]
    P --> Q{"ResultReleaseMode"}
    Q -- Manual --> R["Instructor releases<br>POST runtime-submissions-submissionId-release L751"]
    Q -- Scheduled --> S["Schedule release<br>POST release-schedule L769<br>cancellable L787"]
    Q -- Immediate --> T["Released on finalization"]
    R --> U["grade-result-released outbox event"]
    S --> U
    T --> U
    U --> V["Notification AssessmentGraded in-app<br>Grading-Runtime-GradeReleaseConsumers L13-70"]
    U --> W["LTI AGS score passback<br>GradeReleasePassbackConsumer L73-"]
```

### Edge-case matrix — assessment taking

| # | Case | Behavior | Where |
|---|------|----------|-------|
| 1 | Submit after time limit | Rejected — `TryGetSubmissionTiming` compares UTC stamps | `Assessments:/Grading/Runtime/AssessmentGradingRuntimeService.cs:1651-1655` |
| 2 | Submit before AvailableFrom / after AvailableUntil | Rejected, not marked late | `Assessments:/Grading/Runtime/AssessmentGradingRuntimeService.cs:1657-1662` |
| 3 | Submit after DueAt | `isLate` true; allowed only when `AllowLateSubmissions` and before `LateSubmissionDeadline` | `Assessments:/Grading/Runtime/AssessmentGradingRuntimeService.cs:1664-1670` |
| 4 | Second attempt | Runtime rejects — `MaxAttempts != 1` throws | `Assessments:/Grading/Runtime/AssessmentGradingRuntimeService.cs:1635-1638` |
| 5 | Payload edit after submit | `SetPayload` throws unless InProgress | `Assessments:/Entities/Assessment.cs:748-751` |
| 6 | Draft edit race on collective submissions | `AdvanceCollectiveDraft` optimistic version check | `Assessments:/Entities/Assessment.cs:808-828` |
| 7 | Modality not accepted by assessment | `SetPayload` throws on disallowed modality | `Assessments:/Entities/Assessment.cs:765-768` |
| 8 | Oversized file/project payload | 2048-char bound per payload field | `Assessments:/Entities/Assessment.cs:926-939` |
| 9 | Non-http URL payload | Rejected by `NormalizeUrlPayload` | `Assessments:/Entities/Assessment.cs:910-924` |
| 10 | Duplicate submit | `Submit` throws unless InProgress | `Assessments:/Entities/Assessment.cs:792-795` |
| 11 | Regrade of graded submission | Only via runtime `ApplyRuntimeGrade` (`allowRegrade`), not manual `Grade` | `Assessments:/Entities/Assessment.cs:877-897` |
| 12 | Grading command replay | `GradingCommandReceipts` idempotency-key lookup | `Assessments:/Grading/Runtime/AssessmentGradingRuntimeService.cs:1691-1700` |
| 13 | Quiz definition drift after publish | Learner graded against the immutable revision captured at start | `Assessments:/Entities/Assessment.cs:636-640` |
| 14 | Client-side timer vs server | Client timer is advisory only; server stamp comparison is the enforcement | flow nodes G and table row 1 |

### QuizAdapter notes

- The quiz type is not special-cased in the runtime: `QuizAssessmentTypeAdapter` is an `IAssessmentTypeAdapter` registration (`QuizAdapter:/QuizAdapterModule.cs:18-20`), and `QuizAutomatedReviewStageHandler` an `IReviewStageHandler` (`QuizAdapter:/QuizAdapterModule.cs:22-24`).
- Quiz content is mirrored into the course content tree on publication via `QuizProgramContentPublicationParticipant` (`QuizAdapter:/QuizProgramContentPublicationParticipant.cs:121-122` carries `timeLimitMinutes` into the content projection), and mirrored out of it on delete (`QuizProgramContentDeleteParticipant`).
- `QuizItemProjector`, `QuizDeliveryGenerator`, `QuizAnswerDecoder`, `QuizDeterministicReviewAlgorithm` are the delivery/grading quartet (`QuizAdapter:/QuizAdapterModule.cs:12-17`).

---

## 3. Certificate issuance incl. blockchain anchoring

Two trust layers: the local certificate row (source of truth) and an auxiliary anchor (config-gated EVM publication through the host composition root).

```mermaid
flowchart TD
    A["Enrollment Completed<br>Enrollments-EnrollmentModuleApi L194-214"] --> B{"Template for course?<br>tenant-specific then default<br>Certificates-Services-CertificateService L140-148"}
    B -- none --> B1["fail — no active template L150-153"]
    B -- found --> C["IssueCertificateAsync L44<br>duplicate check per enrollment L54-61"]
    C --> D["Certificate.Issue Active<br>Certificates-Entities-Certificate L88-106<br>number CERT-date-random L108-111"]
    D --> E["Audited transaction<br>BeginAuditedMutationTransactionAsync L99<br>audit Learning.Certificates.CertificateIssued L104-108"]
    E --> F["Stage EVM evidence outbox<br>evidenceProducer.StageIssued L102"]
    F --> G["Anchor best-effort<br>AnchorIssuedCertificateSafely L513-517<br>failures logged never break issuance"]
    G --> H{"BlockchainCertificates Provider set?"}
    H -- "none" --> H1["NoOpCertificateAnchoring<br>CertificatesModule L25"]
    H -- configured --> H2["BlockchainCertificateAnchoringAdapter<br>Host-Core-Integration L20-23<br>registered Host-Core-Setup-ApiProductComposition L210"]
    H2 --> H3["PII-free canonical payload —<br>recipient name reduced to SHA-256 digest<br>Host adapter L79-87"]
    H3 --> H4["AnchorCertificateAsync on chain<br>adapter L35-37"]
    F --> I["CertificateEvmPublicationConsumer<br>durable journal events<br>CertificateEvmAnchorRequestedV1<br>Host-Core-Integration L31-34"]
    G --> J["Learner notified via my-certificates list<br>GET api-certificates-my L40"]
    K["Verify public endpoint<br>GET verify-certificateNumber L73-74 AllowAnonymous"] --> L["Local validity — Status Active and not expired<br>IsValid L113-118"]
    K --> M["Anchor verification<br>GET verify-certificateNumber-anchor L97-98<br>VerifyAnchoringAsync adapter L90-204"]
    N["Revoke POST id-revoke L142<br>reason required"] --> O["Status Revoked terminal<br>Revoke L120-126"]
    O --> P["Revocation anchored<br>RecordRevocationAsync adapter L54-77"]
    Q["Renew POST id-renew L159"] --> R{"Status Revoked?"}
    R -- yes --> R1["reject — revocation terminal L134-137"]
    R -- no --> S["ExpiresAt extended Status Active L139-147"]
```

### Edge-case matrix — certificates

| # | Case | Behavior | Where |
|---|------|----------|-------|
| 1 | Issue twice for same enrollment + template | Conflict — duplicate check | `Certificates:/Services/CertificateService.cs:54-61` |
| 2 | Inactive or missing template | NotFound failure | `Certificates:/Services/CertificateService.cs:67-70` |
| 3 | Audit sink down at issue | Whole issuance rolls back (fail closed) | `Certificates:/Services/CertificateService.cs:97-117` |
| 4 | Blockchain backend down at issue | Non-fatal — logged, certificate still issued | `Certificates:/Services/CertificateService.cs:510-517`, adapter `Host:/Core/Integration/BlockchainCertificateAnchoringAdapter.cs:46-50` |
| 5 | Anchor verification backend unreachable | Fail-closed "unavailable" verdict, not a fake pass | `Host:/Core/Integration/BlockchainCertificateAnchoringAdapter.cs:104-115` |
| 6 | Anchor digest mismatch after local edit | `Mismatch` verdict with anchored vs recomputed digest | `Host:/Core/Integration/BlockchainCertificateAnchoringAdapter.cs:121-142` |
| 7 | Revoke a never-anchored certificate | Revocation anchoring skipped with debug log | `Host:/Core/Integration/BlockchainCertificateAnchoringAdapter.cs:70-76` |
| 8 | Renew a revoked certificate | Rejected — terminal state | `Certificates:/Entities/Certificate.cs:134-137` |
| 9 | Renew with past expiry date | Rejected — must be future | `Certificates:/Entities/Certificate.cs:139-142` |
| 10 | Unknown number on public verify | Returns invalid verdict with Revoked status, audited | `Certificates:/Services/CertificateService.cs:203-218` |
| 11 | Issued-but-unanchored certificate at verify | `NotAnchored` verdict distinguishes absence from mismatch | `Host:/Core/Integration/BlockchainCertificateAnchoringAdapter.cs:144` |
| 12 | Auto-issue on completion | Not wired — issuance is an explicit API call (gap G-8) | `Certificates:/Controllers/CertificatesController.cs:114` |

---

## 4. Cohort management

```mermaid
flowchart TD
    A["Create cohort<br>POST api-cohorts L41<br>Cohorts-Controllers-CohortsController"] --> B["Cohort.Create Scheduled IsOpen false<br>Cohorts-Entities-Cohort L24-47"]
    B --> C["Open<br>POST id-open L126<br>Open sets IsOpen true and Status Active L51-56"]
    C --> D{"Learner enrolls with cohort<br>EnrollUserCommand CohortId<br>Enrollments-EnrollmentModuleApi L29"}
    D --> E["CanEnroll gate<br>IsOpen and capacity and Status Active<br>Cohorts-Entities-Cohort L49"]
    D2["Scheduled content<br>GET cohorts-cohortId-schedule L26<br>Cohorts-Controllers-CohortSchedulesController"] --> F["Schedule preview<br>POST schedule-preview L41<br>CohortScheduleGenerator"]
    F --> G["Conflicts detected<br>ScheduleConflictDetector severity<br>Cohorts-Entities-SchedulingEnums L49-55"]
    G --> H["Persist schedule<br>PUT schedule L57"]
    H --> I["Shift item<br>POST items-itemId-shift L114<br>scope from ScheduleShiftScope"]
    C --> J["Close enrollment window<br>POST id-close L143<br>IsOpen false Status stays Active L58-62"]
    J --> K["Complete<br>POST id-complete L160<br>Status Completed L77-82"]
    C --> L["Cancel<br>POST id-cancel L177<br>Status Cancelled L84-89"]
    M["Calendar export<br>GET cohorts-calendar L144"]
```

### Edge-case matrix — cohorts

| # | Case | Behavior | Where |
|---|------|----------|-------|
| 1 | Enroll into closed cohort | `CanEnroll` false | `Cohorts:/Entities/Cohort.cs:49` |
| 2 | Capacity exceeded | `CurrentEnrollmentCount < MaxCapacity` gate | `Cohorts:/Entities/Cohort.cs:49` |
| 3 | Counter underflow on drop | Guarded decrement | `Cohorts:/Entities/Cohort.cs:70-75` |
| 4 | Open a Completed or Cancelled cohort | `Open()` overwrites Status to Active unconditionally (gap G-11) | `Cohorts:/Entities/Cohort.cs:51-56` |
| 5 | Schedule item outside cohort window | Shift scopes + conflict severities surfaced by detector | `Cohorts:/Entities/SchedulingEnums.cs:43-55` |
| 6 | Content scheduled before cohort start | `ProgramContentScheduleGuard` participates in publication | `Cohorts:/CohortsModule.cs` guard registration |
| 7 | Cohort tenant vs actor tenant | Effective tenant from actor when request omits it | `Cohorts:/Controllers/CohortsController.cs:43-47` |

---

## 5. LTI 1.3 tool launch

GameGuild is the **tool**; external platforms (Moodle/Canvas-class) are the **platform**. Three unauthenticated protocol endpoints plus two system-admin management endpoints. Deep Linking is not implemented (gap G-13); grades flow out through AGS passback on grade release.

```mermaid
flowchart TD
    A["Platform initiates OIDC login<br>POST lti-login L73 AllowAnonymous<br>Lti-Controllers-LtiController"] --> B{"Form POST?"}
    B -- no --> B1["400 L77-80"]
    B -- yes --> C{"iss plus client_id plus deployment_id<br>match an active deployment? L88-92"}
    C -- no --> C1["401 Unknown platform L89-91"]
    C -- yes --> D["login_hint required L94-98"]
    D --> E["Issue state plus nonce<br>LtiLaunchStateStore.Issue L100"]
    E --> F{"AuthorizationUrl https absolute<br>no embedded credentials? L120-123<br>TryBuildAuthorizationRedirect L265-282"}
    F -- no --> F1["400 misconfigured L121-123"]
    F -- yes --> G["302 redirect to platform auth"]
    G --> H["Platform POSTs signed id_token<br>POST lti-launch L134 AllowAnonymous"]
    H --> I{"id_token in query string?<br>rejected outright L137-140"}
    I -- yes --> I1["400 — token must be in body"]
    I -- no --> J["LaunchLtiCommand L156"]
    J --> K["Parse unvalidated JWT for iss and aud<br>LtiEndpointCommands L51-59"]
    K --> L{"Deployment known? L61-66"}
    L -- no --> L1["UnknownPlatform"]
    L -- yes --> M["RS256 validation against platform JWKS<br>LtiPlatformJwksService.ValidateIdTokenAsync"]
    M --> N{"sub plus nonce claims present? L74-78"}
    N -- no --> N1["MissingClaims"]
    N -- yes --> O["Single-use state and nonce consumption<br>stateStore.TryConsume L81-83"]
    O --> P{"Valid state? "}
    P -- no --> P1["InvalidState"]
    P -- yes --> Q{"LtiUserMapping resolves account?<br>by sub then email L86-101"}
    Q -- no --> Q1["UserNotFound — no JIT provisioning"]
    Q -- yes --> R["Issue GameGuild session JWT<br>GenerateAccessTokenAsync L104-107"]
    R --> S["gg_session cookie SameSite None iframe-safe<br>L177-183"]
    S --> T["302 to dashboard-tasks L184"]
    U["Score out — grade released"] --> V["AGS line-item score passback<br>ILtiScorePassback via outbox consumer<br>Assessments-Grading-Runtime-GradeReleaseConsumers L73-"]
    W["Admin registers deployment<br>POST v1-lti-deployments L189<br>SystemAdmin only L192-195"] --> X["Tool JWKS served<br>GET .well-known-jwks.json L31-64"]
```

### Edge-case matrix — LTI

| # | Case | Behavior | Where |
|---|------|----------|-------|
| 1 | Unknown platform triple at login | 401 before any state issued | `Lti:/Controllers/LtiController.cs:88-92` |
| 2 | Missing login_hint | 400 | `Lti:/Controllers/LtiController.cs:94-98` |
| 3 | Authorization URL http / userinfo-bearing / relative | Fails closed — 400 | `Lti:/Controllers/LtiController.cs:265-282` |
| 4 | id_token delivered in query | Rejected before parse (log-leak hygiene) | `Lti:/Controllers/LtiController.cs:137-140` |
| 5 | Replayed launch (state reuse) | `TryConsume` single-use — InvalidState | `Lti:/Commands/LtiEndpointCommands.cs:81-83` |
| 6 | JWT signature invalid / aud mismatch | InvalidToken from JWKS validation | `Lti:/Commands/LtiEndpointCommands.cs:68-71` |
| 7 | Platform user with no local account | UserNotFound — no automatic account creation | `Lti:/Commands/LtiEndpointCommands.cs:86-101` |
| 8 | Unreadable tool private key at JWKS | Deployment excluded from JWKS, error logged | `Lti:/Controllers/LtiController.cs:57-60` |
| 9 | Non-system-admin hits deployment/line-item admin | Forbid | `Lti:/Controllers/LtiController.cs:192-195, 215-218` |
| 10 | Duplicate assessment-to-line-item mapping | 409 Conflict | `Lti:/Controllers/LtiController.cs:234-236` |
| 11 | Deep-linking message type | Not implemented — only resource-link launches (gap G-13) | `Lti:/Controllers/LtiController.cs` (absent) |
| 12 | Score passback for non-finalized round | Consumer throws — delivery retried with backoff | `Assessments:/Grading/Runtime/GradeReleaseConsumers.cs:97-100` |

---

## 6. Recommendations and personalized feed

```mermaid
flowchart TD
    A["Learner opens recommendations<br>GET v1-recommendations-me L38<br>Recommendations-Controllers-RecommendationsController"] --> B["Stored recommendations rendered"]
    C["Generate on demand<br>POST me-generate L56"] --> D["RecommendationEngine<br>Recommendations-Services L10-21"]
    D --> E1["NextInPathStrategy"]
    D --> E2["SimilarToCompletedStrategy"]
    D --> E3["PopularInCategoryStrategy"]
    D --> E4["TrendingNowStrategy"]
    E1 --> F{"Strategy failed?"}
    E2 --> F
    E3 --> F
    E4 --> F
    F -- "one fails" --> G["logged warning engine continues L78<br>MaxCandidatesPerStrategy 20 L17"]
    F -- ok --> H["Merged ranked list persisted<br>learning_course_recommendations"]
    H --> I["Track view<br>POST id-viewed L71"]
    I --> J["Track dismiss<br>POST id-dismiss L83"]
    K["Refresh stale set<br>POST me-refresh L95"]
    L["Anonymous surfaces<br>popular L192 trending L210 similar L228<br>AllowAnonymous"]
    M["Social personalized feed<br>POST api-social-feed-me-generate L67<br>Social-Controllers-FeedController"] --> N["FeedService.GenerateFeedItemsAsync<br>Social-Services-FeedService L48"]
    N --> O["personalized_feed_items rows<br>expiry sweep ClearExpiredFeedItemsAsync L175"]
    O --> P["Viewed POST feed-id-viewed L85"]
    O --> Q["Dismiss POST feed-id-dismiss L104"]
```

### Edge-case matrix — recommendations and feed

| # | Case | Behavior | Where |
|---|------|----------|-------|
| 1 | One strategy throws | Swallowed per-strategy, remainder still ranked | `Recommendations:/Services/RecommendationEngine.cs:66-78` |
| 2 | Cold-start profile | Popular/Trending strategies still produce candidates | `Recommendations:/RecommendationsModule.cs:12-17` |
| 3 | Strategy candidate flood | Hard cap 20 per strategy | `Recommendations:/Services/RecommendationEngine.cs:17` |
| 4 | Telemetry events feeding personalization | Defined and handled but never published — dead pipeline (gap G-1) | `Shared:/Events/LearningEvents.cs`, `Recommendations:/Handlers/LearningEventHandlers.cs:15-184` |
| 5 | Expired feed items | Cleared only when `ClearExpiredFeedItemsAsync` invoked — no hosted sweeper registered | `Social:/Services/FeedService.cs:175`, `Social:/SocialModule.cs` |
| 6 | Skill profile self-service | add via `me/profile/skills` L155, remove L173 | `Recommendations:/Controllers/RecommendationsController.cs:155-174` |
| 7 | Anonymous similar-courses | `courses/{courseId}/similar` AllowAnonymous | `Recommendations:/Controllers/RecommendationsController.cs:228-229` |
