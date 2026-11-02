# Learning CQRS Wiring Matrix

> Learning family modules (`GameGuild.Learning.*`) · Documented against `develop` commit `ac959fd8f`.
> Columns: route → command/query → validator → handler → auth → rate limit → unit test → HOLE?
> Citation shorthand as defined in [`01-user-flows.md`](01-user-flows.md); test projects under `apps/api/tests/GameGuild.Learning.*/`.

## Auth model (applies to the whole matrix)

1. **Controller surface**: `[Authorize]` on every Learning controller; `[AllowAnonymous]` overrides for catalog reads (`ProgramContentController.cs:32,56`), public program listings (`ProgramCrudController.cs:128,263`), certificate public verify (`CertificatesController.cs:74,98`), social public reads (`Social` controllers), anonymous recommendation surfaces (`RecommendationsController.cs:193,211,229`), and the three LTI protocol endpoints (`LtiController.cs:31,73,134`).
2. **Resource-level capability**: `RequireCourseCapability(CourseCapability.Publish)` on program lifecycle mutations (`ProgramLifecycleController.cs:21,34,48,62,80,94,108,122,136`) and `CourseAccessController` capabilities; `RequireResourcePermission<PermissionType, Certificate>` on certificate mutations (`CertificatesController.cs:115,143,161`); LTI admin checks `actorContextAccessor.ActorContext.IsSystemAdmin` inline (`LtiController.cs:192,215`).
3. **Base pipeline**: controllers derive from `BaseApiController` (Result mapping) or `Shared:/Abstractions/LearningControllerBase.cs:18` (actor context, `GetRequiredUserId` L39-48, `CanAccessUserResource` L125-147).

No Learning controller carries `EnableRateLimiting` — the whole family runs on the platform default policy only (contrast with Payments where mutations pin `ExpensiveOperations`). Recorded as gap G-3.

Validation: most Learning requests are positional `record` DTOs without FluentValidation validators; guards live in entities/services (`Assessment` payload normalizers, `Cohort.Create` argument checks) — "—" means no validator class and the guard is in the handler/entity. Notable exception: `Courses:/Configuration/CoursesModule.cs:65` registers `IValidator<CodingAssignmentContent>`.

## Shared kernel — `LearningControllerBase` (`Shared:/Abstractions/LearningControllerBase.cs`)

Base class only (0 routes).

## EnrollmentsController — `api/learning/enrollments` (`Enrollments:/EnrollmentModuleApi.cs:318-357`, `[Authorize]` L320)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| GET `{id}` (L323) | `GetEnrollmentQuery` (L35) | — | `GetEnrollmentQueryHandler` (L300) | `[Authorize]` only — no ownership check | `Enrollments.UnitTests` | **any authenticated user reads any enrollment** (G-6) |
| GET `users/{userId}` (L330) | `GetUserEnrollmentsQuery` (L37) | — | handler L306 | `[Authorize]` only | `Enrollments.UnitTests` | **cross-user read, no guard** (G-6) |
| GET `courses/{courseId}` (L334) | `GetCourseEnrollmentsQuery` (L39) | — | handler L312 | `[Authorize]` only | covered | roster read — no teacher check (G-6) |
| POST `` (L338) | `EnrollUserCommand` (L29) | — | handler L282 → `EnrollmentService.EnrollAsync` L130 | `[Authorize]` only | covered | **enrolls arbitrary UserId from body — no capability check** (G-6) |
| PATCH `{id}/progress` (L342) | `UpdateEnrollmentProgressCommand` (L31) | — | handler L288 → `UpdateProgressAsync` L164 (clamp L37) | `[Authorize]` only | covered | **progress writable by anyone authenticated** (G-6) |
| POST `{id}/status/{status}` (L350) | `SetEnrollmentStatusCommand` (L33) | — | handler L294 → `SetStatusAsync` L178 | `[Authorize]` only | covered | arbitrary status transitions incl. Completed (G-6, SM audit) |

## ProgramCrudController — `v{version}/courses` (`Courses:/Controllers/ProgramCrudController.cs`, `[Authorize]` L18)

| Route | Command / Query | Validator | Handler | Auth | Unit test | HOLE? |
|-------|-----------------|-----------|---------|------|-----------|-------|
| GET `` (L29) | `GetProgramsQuery` | — | `ProgramReadService` | `[Authorize]` | `Courses.UnitTests` | covered |
| GET `public` (L127) | public listing query | — | same | `[AllowAnonymous]` L128 | covered | covered |
| GET `me` (L138) | creator listing | — | same | `[Authorize]` | covered | covered |
| POST `` (L151) | create program command | — | `ProgramCrudService` → `CreateDraftAsync` (`ProgramLifecycleService.cs:13-21`) | `[Authorize]` | covered | covered |
| GET `{id}` (L175) | by-id query | — | same | `[Authorize]` | covered | covered |
| GET `{id}/with-content` (L190) | with-content query | — | same | `[Authorize]` | covered | covered |
| PUT `{id}` (L205) | update command | — | `ProgramWriteService` | `[Authorize]` | covered | **no creator/tenant guard observed on update** (G-5) |
| DELETE `{id}` (L225) | soft delete | — | same | `[Authorize]` | covered | same class of guard question (G-5) |
| POST `{id}:clone` (L242) | clone command | — | same | `[Authorize]` | covered | covered |
| GET `slug/{slug}` (L262) | slug query | — | same | `[AllowAnonymous]` L263 | covered | covered |
| POST `{id}:self-enroll` (L297) | `AddUserToProgramEndpointCommand` (L333) | — | `ProgramEnrollmentService` | `[Authorize]` + Published/Public L305 + enrollment window L314 + monetized 402 L319-331 | covered | covered (prereqs unchecked — G-15) |
| POST `{id}/users/{userId}` (L346) | add-user command | — | same | `[Authorize]` | covered | **no capability check on adding others** (G-5) |
| POST `{id}/users:enroll` (L365) | enroll-by-reference | — | same | `[Authorize]` | covered | same (G-5) |
| DELETE `{id}/users/{userId}` (L420) | remove-user | — | same | `[Authorize]` | covered | same (G-5) |
| GET `{id}/users` (L435) | roster query | — | same | `[Authorize]` | covered | covered |
| GET `{id}/users/{userId}/progress` (L445) | progress query | — | same | `[Authorize]` | covered | cross-user read (G-6 family) |
| GET `{id}/me/progress` (L460) | own progress | — | same | `[Authorize]` | covered | covered |
| PUT `{id}/users/{userId}/progress` (L480) | update progress | — | `ContentProgressService` | `[Authorize]` | covered | **any user can set any user's progress** (G-6) |
| PUT `{id}/me/progress` (L500) | own progress | — | same | `[Authorize]` | covered | covered |
| POST `{id}/users/{userId}/content/{contentId}:complete` (L525) | complete-content | — | `ContentInteractionService` | `[Authorize]` | covered | same (G-6) |
| POST `{id}/me/content/{contentId}:complete` (L540) | own complete | — | same | `[Authorize]` | covered | covered |
| POST `{id}/users/{userId}:reset` (L560) | reset progress | — | same | `[Authorize]` | covered | destructive, guard unclear (G-5) |
| POST `{id}:monetize` (L586) | monetization command | — | `ProgramPricingMetadata` path | `[Authorize]` | covered | covered |

## ProgramLifecycleController — `v{version}/courses` (`Courses:/Controllers/ProgramLifecycleController.cs`, `[Authorize]` L15)

All nine actions carry `RequireCourseCapability(CourseCapability.Publish)`.

| Route | Command | Handler | HOLE? |
|-------|---------|---------|-------|
| POST `{id}:submit` (L19) | `SubmitProgramLifecycleCommand` | `ProgramLifecycleService.SubmitProgramAsync` L134-147 | covered |
| POST `{id}:approve` (L33) | approve command | `ApproveProgramAsync` L149-163 | **no from-state guard — archived approvable** (G-9) |
| POST `{id}:reject` (L47) | reject command | `RejectProgramAsync` L165-179 | same (G-9) |
| POST `{id}:withdraw` (L66) | withdraw command | `WithdrawProgramAsync` L181-195 | same (G-9) |
| POST `{id}:archive` (L80) | archive command | `ArchiveProgramAsync` L197-211 | covered |
| POST `{id}:restore` (L94) | restore command | `RestoreProgramAsync` L213-226 | covered |
| POST `{id}:publish` (L108) | publish command | `PublishProgramAsync` L100-109 | **no review-gate — draft publishable directly** (G-9) |
| POST `{id}:unpublish` (L122) | unpublish command | `UnpublishProgramAsync` L111-120 | covered |
| POST `{id}:schedule` (L136) | schedule command | `ScheduleProgramAsync` L228-242 | **publishAt ignored — publishes immediately** (G-10) |

## ProgramContentController — `v{version}/courses/{programId}/content` (`Courses:/Controllers/ProgramContentController.cs`, `[Authorize]` L15)

| Route | Command / Query | Handler | Auth | HOLE? |
|-------|-----------------|---------|------|-------|
| GET `` (L31) | content list | `ProgramContentService` | `[AllowAnonymous]` L32 | covered |
| GET `{id}` (L55) | by-id | same | `[AllowAnonymous]` L56 | covered |
| POST `{id}/submit` (L87) | submit user content | `ContentInteractionService` | `[Authorize]` | covered |
| POST `` (L136) | create content | `ProgramContentService` | `[Authorize]` | authoring guard via `IProgramContentAcademicMutationGuard` |
| PUT `{id}` (L161) | update content | same | `[Authorize]` | same guard family |
| DELETE `{id}` (L196) | delete content | delete participants (`QuizAdapter`/`Assessments`) | `[Authorize]` | covered |
| GET `{parentId}/children` (L218) | children query | same | `[Authorize]` | covered |
| POST `reorder` (L247) | reorder command | same | `[Authorize]` | covered |
| POST `{id}/move` (L264) | move command | same | `[Authorize]` | covered |
| GET `required` (L291) | required query | same | `[Authorize]` | covered |
| GET `by-type/{type}` (L307) | typed query | same | `[Authorize]` | covered |
| GET `by-visibility/{visibility}` (L323) | visibility query | same | `[Authorize]` | covered |
| POST `search` (L345) | search query | same | `[Authorize]` | covered |
| GET `stats` (L367) | stats query | same | `[Authorize]` | covered |
| GET `{id}/coding-assignment` (L385) | public coding payload | `CodingAssignmentContentService` | `[Authorize]` L386 | covered |
| GET `{id}/coding-assignment/full` (L410) | full payload | same | `[Authorize]` L411 | covered |
| PUT `{id}/coding-assignment` (L435) | upsert payload | same + `IValidator<CodingAssignmentContent>` | `[Authorize]` L436 | covered — the module's only FluentValidation validator |

## ProgramContentAuthoringController — `.../content/{contentId}/authoring` (`Courses:/Controllers/ProgramContentAuthoringController.cs`, `[Authorize]` L16)

| Route | Command / Query | Handler | HOLE? |
|-------|-----------------|---------|-------|
| GET `` (L22) | draft query | `ProgramContentAuthoringService` | covered |
| PUT `` (L43) | save draft | same | covered |
| POST `publish` (L82) | publish draft | publication participants chain | covered |
| GET `ai/entitlement` (L124) | balance query | `AuthoringAiService.GetEntitlement` L61-65 → Economy wallet | covered |
| GET `ai/conversations` (L144) | conversation list | same | covered |
| POST `ai/runs` (L157) | create AI run | quote → reserve → enqueue (`AuthoringAiService` L52+) | covered |
| GET `ai/runs/{runId}` (L218) | run status | same | covered |
| POST `ai/runs/{runId}/cancel` (L239) | cancel run | release reservation | covered |
| GET `ai/runs/{runId}/stream` (L264) | SSE stream | `ai_authoring_stream_events` | covered |
| POST `ai/proposals/{proposalId}/apply` (L295) | apply proposal | draft mutation | covered |
| DELETE `ai/proposals/{proposalId}` (L336) | discard proposal | same | covered |

## ContentInteractionController — `v{version}/course-interactions` (`Courses:/Controllers/ContentInteractionController.cs`, `[Authorize]` L15)

| Route | Command / Query | Handler | HOLE? |
|-------|-----------------|---------|-------|
| POST `` (L21) | start interaction | `ContentInteractionService` + `ContentInteractionAttemptSelection` | covered |
| PUT `{interactionId}/progress` (L43) | update progress | same | covered |
| POST `{interactionId}/submit` (L66) | submit content | same | covered |
| POST `{interactionId}/complete` (L89) | complete content | same | covered |
| GET `user/{programUserId}/content/{contentId}` (L112) | interaction query | same | cross-user read (G-6 family) |
| GET `user/{programUserId}` (L137) | user interactions | same | same |
| GET `content/{contentId}/survey-results` (L149) + `/visible` (L166) | survey results | survey visibility rules | teacher surface — guard lives in `SurveyResultsVisibility` |
| GET `content/{contentId}/reflection-responses` (L177) + `/visible` (L184) | reflection results | same | same |
| PUT `{interactionId}/time-spent` (L195) | time-on-task update | same | covered |

## Remaining Courses controllers

| Controller (route) | Actions | Wiring | HOLE? |
|--------------------|---------|--------|-------|
| `ActivityGradeController` (`.../activity-grades`, `[Authorize]` L12) | POST L15, GET interaction L42, GET grader L59, GET student L71, PUT L83, DELETE L110, GET pending L127, GET statistics L136, GET content L145 | `ActivityGradeService` + `GradeStatistics` | **no grader-capability attribute observed on grade writes** (G-5) |
| `CourseAccessController` (`.../access`, `[Authorize]` L10) | GET capabilities L13-16 | `CourseAccessEvaluator` (`CoursesModule.cs:26`) | covered |
| `CourseCheckoutController` (`v{version}/courses`, `[Authorize]` L13) | POST `{courseId}/checkout/complete` L17-41 | `CompleteCourseCheckoutCommand` → entitlement + enrollments | covered |
| `CourseStudentsController` (`.../students`, `[Authorize]` L13) | POST message L16-18 | `SendCourseStudentMessageCommandHandler` → `INotificationService` | covered |
| `CourseSupportTicketsController` (`.../support/tickets`, `[Authorize]` L17) | POST L23, GET L60, GET `{ticketId}` L74, POST messages L85, POST `{ticketId}:resolve` L117 | ticket commands | covered |
| `LessonInteractionEventsController` (`.../interactions/{interactionId}/events`, `[Authorize]` L12) | POST L15, GET L38 | `ContentInteractionEvent` append | covered |
| `PrerequisitesController` (`api/prerequisites`, `[Authorize]` L14) | POST L37, GET `{id}` L68, GET course L84, GET dependents L96, GET chain L108, GET check L120, GET check-user L151, PUT L173, DELETE L200, POST reorder L218, GET would-create-cycle L234 | `PrerequisiteService` | check endpoints read any user's readiness (G-6 family) |

## AssessmentsController — `v{version}/assessments` (`Assessments:/Controllers/AssessmentsController.cs`, `[Authorize]` L26)

| Route | Command / Query | Handler | HOLE? |
|-------|-----------------|---------|-------|
| PUT `course/{courseId}/content/{contentId}/draft` (L76) | authoring draft upsert | `AssessmentAuthoringService` | covered |
| GET `{id}/authoring-state` (L103) | authoring state query | same | covered |
| POST `{id}/revisions/prepare` (L124) | prepare revision | same → immutable revision | covered |
| POST `{id}/revisions/publish` (L154) | publish revision | publication participant chain | covered |
| POST `{id}/revisions/unpublish` (L184) | unpublish revision | same | covered |
| POST `{id}/test-runs` (L215) | start test run | test-run service | covered |
| GET `test-runs/{testRunId}` (L251) | test-run query | same | covered |
| POST `test-runs/{testRunId}/submit` (L270) | submit test run | same | covered |
| POST `test-runs/{testRunId}/instructor-review` (L298) | instructor review | review stage handlers | covered |
| POST `test-runs/{testRunId}/self-review` (L325) | self review | same | covered |
| POST `test-runs/{testRunId}/restart` (L353) | restart test run | same | covered |
| POST `test-runs/{testRunId}/peer-reviews/claim` (L379) | claim peer review | `PeerReviewAssignmentService` | covered |
| GET `test-runs/{testRunId}/peer-reviews/{reviewId}` (L404) | review query | same | covered |
| POST `test-runs/{testRunId}/peer-reviews/{reviewId}/submit` (L428) | submit review | same | covered |
| POST `{id}/runtime-submissions/individual` (L461) | start individual | `AssessmentGradingRuntimeService` (policy check L1631-1639) | covered |
| POST `content/{contentId}/runtime-submissions/individual` (L504) | start by content | same | covered |
| POST `{id}/runtime-submissions/collective` (L540) | start group submission | same (collective draft versions L808-828) | covered |
| GET `runtime-submissions/{submissionId}` (L572) | submission query | same | covered |
| PUT `runtime-submissions/{submissionId}/draft` (L611) | autosave draft | same (payload guards L746-778) | covered |
| POST `runtime-submissions/{submissionId}/submit` (L640) | submit | same (timing gate L1643-1670) | covered |
| PUT `.../self-review/draft` (L669) | self-review draft | review stages | covered |
| POST `.../self-review` (L690) | submit self review | same | covered |
| POST `.../instructor-review` (L711) | instructor grade | same | covered |
| POST `.../regrade` (L733) | regrade | `ApplyRuntimeGrade` path | covered |
| POST `.../release` (L751) | release results | `GradeReleaseService` → outbox | covered |
| POST `.../release-schedule` (L769) | schedule release | `GradeResultReleaseScheduleService` | covered |
| POST `.../release-schedule/cancel` (L787) | cancel schedule | same | covered |
| POST `` (L808) | create assessment | `AssessmentService` | covered |
| GET `{id}` (L834) | assessment query | same | covered |
| GET `course/{courseId}` (L854) | course assessments | same | covered |
| GET `course/{courseId}/groups` (L875) | groups query | `GroupSetService` | covered |
| GET `course/{courseId}/analytics` (L896) | analytics query | projections | covered |
| GET `course/{courseId}/gradebook/{enrollmentId}` (L915) | gradebook query | `AssessmentGradebookProjectionService` | covered |
| POST `groups` (L954) | create group | `GroupSetService` | covered |
| PUT `groups/{id}` (L983) | update group | same | covered |
| DELETE `groups/{id}` (L1011) | delete group | same | covered |
| PUT `{id}` (L1039) | update assessment | `AssessmentService` | covered |
| PUT `{id}/group` (L1070) | assign to group | same | covered |

## Other Assessments controllers

| Controller (route) | Actions | Wiring | HOLE? |
|--------------------|---------|--------|-------|
| `GroupSetsController` (`v{version}/courses/{courseId}/group-sets`, `[Authorize]` L17) | GET L46, POST L61, POST groups L92, GET groups L127, join L149, DELETE membership L177, POST members L207, DELETE members L229 | `GroupSetService` | covered |
| `PeerReviewsController` (`v{version}/assessments`, `[Authorize]` L25) | claim L57, GET review L102, submit L166, extend stage L258, reassign L287, resolve-insufficient L326, received L356, submission reviews L397 | `PeerReviewAssignmentService` + stage handlers | covered |
| `RubricsController` (`v{version}/assessments/{assessmentId}/rubric`, `[Authorize]` L17) | PUT L44, GET L71, DELETE L97 | `RubricService` | covered |
| `TasksController` (`v{version}/me/tasks`, `[Authorize]` L15) | GET L21 | `TasksService` | covered |

## CertificatesController — `api/certificates` (`Certificates:/Controllers/CertificatesController.cs`, `[Authorize]` L14)

| Route | Command / Query | Handler | Auth | HOLE? |
|-------|-----------------|---------|------|-------|
| GET `my` (L40) | user certificates | `CertificateService.GetUserCertificatesAsync` L183-189 | `[Authorize]` | covered |
| GET `{id}` (L58) | by-id query | `GetCertificateByIdAsync` L171-175 | `[Authorize]` | **no ownership check on read** (G-6) |
| GET `verify/{certificateNumber}` (L73) | public verify | `VerifyCertificateAsync` L199-218 | `[AllowAnonymous]` L74 | covered (audited) |
| GET `verify/{certificateNumber}/anchor` (L97) | anchor verify | `VerifyCertificateAnchoringAsync` L247-276 | `[AllowAnonymous]` L98 | covered |
| POST `issue` (L114) | `IssueCertificateEndpointCommand` | `IssueCertificateAsync` L44-132 | `RequireResourcePermission Create` L115 | covered — no completion gate on enrollment (G-8) |
| POST `{id}/revoke` (L142) | revoke command | `Revoke` L120-126 + revocation anchoring | `RequireResourcePermission Delete` L143 | covered |
| POST `{id}/renew` (L159) | renew command | `Renew` L132-147 | `RequireResourcePermission Edit` L161 | covered |
| GET `course/{courseId}` (L176) | course certificates | `GetCourseCertificatesAsync` L191-197 | `[Authorize]` | covered |
| GET `templates/course/{courseId}` (L190) | template list | `CertificateTemplateService` | `[Authorize]` | covered |
| GET `templates/{templateId}` (L201) | template by-id | same | `[Authorize]` | covered |
| POST `templates` (L217) | create template | same | `[Authorize]` | **no admin-capability attribute on template authoring** (G-5) |
| PUT `templates/{templateId}` (L242) | update template | same | `[Authorize]` | same |
| DELETE `templates/{templateId}` (L268) | delete template | same | `[Authorize]` | same |
| GET `expiring` (L284) | expiring query | `CertificateService` | `[Authorize]` | covered |

## CohortsController — `api/cohorts` (`Cohorts:/Controllers/CohortsController.cs`, `[Authorize]` L15)

| Route | Command / Query | Handler | HOLE? |
|-------|-----------------|---------|-------|
| POST `` (L41) | `CreateCohortCommand` | `CohortService` → `Cohort.Create` L24-47 | **no instructor capability check** (G-5) |
| GET `{id}` (L61) | by-id query | same | covered |
| GET `course/{courseId}` (L76) | course cohorts | same | covered |
| GET `course/{courseId}/active` (L87) | active filter | same | covered |
| GET `course/{courseId}/enrollable` (L98) | enrollable filter (`CanEnroll` L49) | same | covered |
| PUT `{id}` (L109) | update command | `Cohort.Update` L103-120 | covered |
| POST `{id}/open` (L126) | open command | `Open` L51-56 | covered (state-guard gap G-11) |
| POST `{id}/close` (L143) | close command | `Close` L58-62 | covered |
| POST `{id}/complete` (L160) | complete command | `Complete` L77-82 | covered |
| POST `{id}/cancel` (L177) | cancel command | `Cancel` L84-89 | covered |
| DELETE `{id}` (L194) | delete | `CohortService` | covered |

## CohortSchedulesController — `.../cohorts/{cohortId}/schedule` (`Cohorts:/Controllers/CohortSchedulesController.cs`, `[Authorize]` L13)

| Route | Command / Query | Handler |
|-------|-----------------|---------|
| GET `available-content` (L16) | available content query | schedule generator |
| GET `` (L26) | schedule query | same |
| POST `preview` (L41) | preview command (`CohortScheduleGenerator`) | same |
| PUT `` (L57) | persist schedule | same |
| PATCH `items/{itemId}` (L85) | item update | same |
| POST `items/{itemId}/shift` (L114) | shift command (scopes `SchedulingEnums.cs:43-47`) | `ScheduleConflictDetector` |
| GET `~/courses/{courseId}/cohorts/calendar` (L144) | calendar query | same |

## LtiController (attribute-routed, `Lti:/Controllers/LtiController.cs`, `[Authorize]` L20)

| Route | Command | Handler | Auth | HOLE? |
|-------|---------|---------|------|-------|
| GET `.well-known/jwks.json` (L31) | — (direct context query) | inline | `[AllowAnonymous]` L30 | covered |
| POST `lti/login` (L73) | — (state issue + redirect) | inline + `LtiLaunchStateStore` | `[AllowAnonymous]` L72 | covered |
| POST `lti/launch` (L134) | `LaunchLtiCommand` L156 | `LtiEndpointCommandHandler` L50-107 | `[AllowAnonymous]` L133 | covered |
| POST `v1/lti/deployments` (L189) | `CreateLtiDeploymentCommand` | same handler | inline `IsSystemAdmin` L192 | covered |
| POST `v1/lti/deployments/{id}/line-items` (L212) | `CreateLtiLineItemCommand` | same | inline `IsSystemAdmin` L215 | covered |

## Experience controllers

### DiscoveryController — `v{version}/discovery` (`Discovery:/Controllers/DiscoveryController.cs`, `[Authorize]` L27)

| Route | Command / Query | Handler | HOLE? |
|-------|-----------------|---------|-------|
| GET `featured` (L35) / `featured/type/{type}` (L48) / `featured/{id}` (L62) | featured queries | featured content service | covered |
| POST `featured` (L79) | create featured | same | **curation capability not enforced by attribute** (G-5) |
| PUT `featured/{id}` (L109) / PATCH toggle (L142) / DELETE (L160) | featured mutations | same | same |
| GET `collections` (L178) / `collections/featured` (L192) / `collections/slug/{slug}` (L204) / `collections/{id}` (L221) / `collections/curator/{curatorId}` (L238) | collection queries | collection service | covered |
| POST `collections` (L252) / PUT (L277) / publish L305 / unpublish L321 / DELETE L337 | collection mutations | same | same (G-5) |
| POST `search/record` (L355) | `RecordSearchCommand` | search history service | **userId from query string — history spoofable** (G-6) |
| POST `search/{searchId}/click` (L376) | `RecordSearchClickCommand` | same | covered |
| GET `search/history/{userId}` (L393) | history query | same | cross-user read (G-6) |
| GET `search/popular` (L405) | popular query | same | covered |

### LearningPathController — `v{version}/learning-paths` (`LearningPaths:/Controllers/LearningPathController.cs`, `[Authorize]` L29)

23 GET/POST/PUT/DELETE actions at L37-493 (list, search, featured, popular, slug, by-id, by-creator, create, update, delete, publish L216, unpublish L239, add-course L257, remove-course L287, reorder L303, enroll L328, unenroll L347, enrollment check L364/L379, progress L389, complete L412, abandon L429, user enrollments L448, completed L462, statistics L477, enrollments L493). Wiring: path service + `LearningPathEnrollmentStatus` machine. Publish/unpublish/enroll/complete/abandon carry path-state guards in the service layer; no per-action capability attributes (G-5).

### RecommendationsController — `v{version}/recommendations` (`Recommendations:/Controllers/RecommendationsController.cs`)

| Route | Command / Query | Handler | Auth |
|-------|-----------------|---------|------|
| GET `me` (L38) | list query | `RecommendationService` | `[Authorize]` L39 |
| POST `me/generate` (L56) | generate command | `RecommendationEngine` L10-21 | `[Authorize]` L57 |
| POST `{id}/viewed` (L71) / `{id}/dismiss` (L83) | tracking commands | same | `[Authorize]` |
| POST `me/refresh` (L95) | refresh command | same | `[Authorize]` |
| GET `me/statistics` (L108) / `me/profile` (L122) | profile queries | same | `[Authorize]` |
| PUT `me/profile` (L134) / POST `me/profile/skills` (L155) / DELETE skills (L173) | profile mutations | same | `[Authorize]` |
| GET `popular` (L192) / `trending` (L210) / `courses/{courseId}/similar` (L228) | anonymous queries | strategy layer | `[AllowAnonymous]` |

### Social controllers — `api/social` (Discussions L18, Feed L17, Likes L17, Replies L17, Reviews L18, Wishlists L17; all `[Authorize]`)

| Controller | Actions | Public reads |
|-----------|---------|--------------|
| `DiscussionsController` | create L39, by-id L66, course L88, content L112, pin L136, unpin L155, resolve L174, delete L192 | L67, L89, L113 `[AllowAnonymous]` |
| `FeedController` | me L38, generate L67, viewed L85, dismiss L104 | — |
| `LikesController` | like L38, unlike L57, check L76, count L94, me L112 | L95 `[AllowAnonymous]` |
| `RepliesController` | create L38, list L64, accept L87, upvote L106, delete L124 | L65 `[AllowAnonymous]` |
| `ReviewsController` | create L39, by-id L67, course L86, me L110, helpful L132, delete L150, stats L169, approve L187, feature L206, moderation L225 | L68, L87, L170 `[AllowAnonymous]` |
| `WishlistsController` | add L38, remove L66, me L85, check L107, preferences L125 | — |

Moderation actions (`approve` L187, `feature` L206, `PATCH moderation` L225) have no moderator capability attribute (G-5).

## WorkspacesController — `LearnerWorkspaceController` (`Workspaces:/Controllers/LearnerWorkspaceController.cs`, route `v{version}/learning` L11, `[Authorize]` L12)

| Route | Query | Handler |
|-------|-------|---------|
| GET `me/dashboard` (L17) | `GetLearnerDashboardQuery` | workspace query handlers (`Workspaces:/Queries/`) |
| GET `courses/{courseId}/workspace` (L33) | `GetLearnerCourseWorkspaceQuery` | same |
| GET `me/search` (L54) | `SearchLearnerWorkspaceQuery` | same |

All three pin the actor's own `SubjectIdAsGuid` (L22-25, L37-41, L57-61) — the strictest ownership pattern in the family.

## Test wiring summary

Test projects: `apps/api/tests/GameGuild.Learning.{Courses,Enrollments,Assessments,Certificates,Cohorts,Experience.*}.UnitTests/`. Integration coverage for the grading runtime and gradebook lives in the Assessments unit suite plus `GameGuild.Api` integration tests; the Learning family has **no dedicated integration-test project** (contrast: Payments/Billing have one) — recorded in the gaps file as G-7.
