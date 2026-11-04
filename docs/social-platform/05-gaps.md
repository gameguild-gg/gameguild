# Social + Platform — Gap Register

> Documented against `develop` commit `43603cb5b`. KNOWN = already tracked by the merged audit packages (PRs #1193–#1216) and their filed issues (#1199–#1226). NEW = first recorded here. Every claim carries `file:line`. Two candidate gaps (N8, N15) were **withdrawn during verification** — recorded at the bottom so the cross-references in the other documents stay honest.

## KNOWN — cross-referenced from the merged packages

| ID | Finding | Tracking | Where it touches this package |
|---|---|---|---|
| K1 | Assets virus-scan/moderation picks re-enter `Processing` rows with no lease — competing consumers can double-process | issue #1218 G4; `docs/operations/background-workers.md:55,58` | flow F5 — the initial claim is CAS-protected (`ModerationClassificationProcessor.cs:47-50`) but crashed `Processing` rows are re-picked indefinitely |
| K2 | `OpenFeatureHostedInitializer` registered but a no-op (Features) | issue #1218 G1 | Features module's only hosted service |
| K3 | "For you" feed label implies ranking the implementation does not provide | issue #1234 | flow F2 / gap N10 — the web issue covers the label; N10 records the backend half (TrendingScore never consulted) |
| K4 | CQRS bypass debt — controllers injecting services instead of `ISender` | `docs/architecture/CQRS_BYPASS_KNOWN_DEBT.md` (20 controllers) | in-scope entries: `FollowersController`, `FeedController`, `AssetsCdnController`, `TestingLabPermissionController`, `ProjectPermissionController` (the doc's Learning-module attributions for the last two are stale — both live in `GameGuild.Projects`/`GameGuild.TestingLab` today) |
| K5 | Dead-endpoint class (handler-less 500s / 422-planned) was an authorization-family phenomenon, since fixed | issues #1203, #1204 (closed); PR `28a865953` | context for this package's headline: the same audit across Social+Platform finds **zero** dead endpoints (matrix §Wiring guarantees) |

## NEW — first recorded here

### N1 — No text moderation anywhere in Social; TrustSafety disabled — severity high
`Post` has no moderation status or report endpoint (`Posts:/Controllers/*.cs` — none of the 26 endpoints flag or review); post text reaches persistence unfiltered (`PostsCrudController.cs:155-161`). The `TrustSafety` module that would host enforcement is disabled (`Host:/Core/Setup/ApiProductComposition.cs:113-115`; entire module = 3 files). Only media is gated (attach-safe-to-serve `PostsCrudController.cs:141-154`; blog manifest `Blog:/Services/BlogPostService.cs:199-203`). Same for post comments and blog comments — no queue, no report route.

### N2 — All twelve `Post*` domain events are orphans that escape the CI dead-event guard — severity medium
Declared `Posts:/Events/PostEvents.cs:6-128`; zero construction sites and zero consumers across `Modules/` and `GameGuild.API/` (grep `PostCreatedEvent(` etc. — only the declarations). Notifications has no subscriber. The G4 guard baseline (`Architecture/wiring-gap-baseline.txt:31-53`) lists dead events for five other modules but not these, because they derive the local `DomainEventBase` (`PostEvents.cs:140`) rather than the durable-event interface the guard scans. Consequence: like/comment/share produce no notifications at all (E1).

### N3 — The FeedItem inbox has exactly one producer — itself — severity medium
`FeedItem` rows are written only by `AddFeedItemCommand` behind `POST /api/social/feed` (`Feed:/FeedModuleApi.cs:85`); no module projects posts, blog publications, or sessions into it. `GET users/{userId}` therefore surfaces only what a user manually appended via the sibling endpoint. Either project real content into the inbox or retire the surface.

### N4 — Group membership is tenant-admin-only; group status transitions unguarded — severity high
All membership operations (join/approve/reject/role/leave) require `Policies.TenantAdmin` + `EnsureCanManageGroupAsync` (`Groups:/GroupsModuleApi.cs:751-845`) — members cannot join or leave through the API, and join takes the target `UserId` from the body. Group status activate/archive/suspend is a blind setter (`:735-748`).

### N5 — Game-jam submitter and judge identities are caller-asserted — severity high
`SubmitJamEntryCommand` takes `request.UserId` (`Jams:/GameJamsModuleApi.cs:381`) and `ScoreJamSubmissionCommand` takes `request.JudgeUserId` (`:391`) straight from request bodies — any authenticated caller can submit or score as another user.

### N6 — Game-jam lifecycle has no from-state guard; submissions ignore jam status — severity high
`SetStatusAsync` assigns any status onto any status (`Jams:/GameJamsModuleApi.cs:215-227`); `SubmitAsync` never reads `jam.Status` — entries land on cancelled/completed jams (`:229-249`). No worker flips Upcoming→Active at start time (status inferred only at creation, `:197`).

### N7 — Zero FluentValidation coverage across all nine Social modules and most of Platform — severity medium
Validators exist only in `Assets` (12), `Monitoring.SLA` (11), `Resources` (16), `TestingLab` (4), `Projects` (1) (`grep AbstractValidator`). 344 of 510 endpoints take request bodies or query-shape inputs with no `AbstractValidator<T>`; malformed inputs surface as 500s or silently clamped values.

### N8 — WITHDRAWN ON VERIFICATION (no gap)
Profiles mutations were suspected of missing actor-equality guards; the controller enforces `IsActor(userId)`/`OwnsProfileAsync` on every write (`Profiles:/Controllers/SocialProfilesController.cs:47,59,73,87`). Number reserved to keep cross-references stable.

### N9 — Anonymous view-counter increments — severity low
`POST /api/v1/posts/{postId}/view` is `[AllowAnonymous]` (`Posts:/Controllers/PostInteractionsController.cs:96-97`); view counters that feed `PostStatistics` (and a would-be trending score) are incrementable unauthenticated and without dedup.

### N10 — Analytics dashboards have no ownership scoping — severity high
`GetDashboardByIdQuery` returns any live dashboard (`Analytics:/Repositories/AnalyticsRepositories.cs:103-105` — filter is id + not-deleted only) and `UpdateDashboardCommandHandler` mutates it (`Commands/DashboardCommands.cs:66-84`) — any authenticated user reads/edits any tenant's dashboard by id. Class `[Authorize]` only (`DashboardsController.cs:8`).

### N11 — "for-you" is not personalized and TrendingScore is dead weight — severity low/medium
ForYou is reverse-chronological merge of visibility-filtered posts + TestingSessions (`Feed:/Queries/SocialFeedQueryService.cs:87-113,332-335`); `PostStatistics.TrendingScore` is computed in Posts and read by nothing in Feed (no reference). Related: issue #1234 (label wording).

### N12 — Generic AI endpoints bypass the credit economy — severity medium
`/ai/chat`, `/ai/generate`, `/ai/generate-content/*` enforce only the Resources `AiRequests` count quota and write a billing trail (`AI:/Services/AiOrchestrator.cs:326-365`, NoOp recorder default `L18-20`); the `IAiCreditWalletService` lifecycle runs exclusively in `Blog:/Authoring/BlogAuthoringAiService.cs:176-221,495-600` and Learning.Courses authoring. Chat traffic is free of credit accounting while authoring traffic pays.

### N13 — Moderation-off is a silent freeze — severity medium
`ModerationClassificationWorker` exits when `AutoModerationOptions.Enabled=false` (`Assets:/BackgroundServices/ModerationClassificationWorker.cs:17-21`); uploads keep arriving with `ModerationStatus.Pending`, the queue never drains, and nothing alerts. (Inverse of #1218 G6, where dispatchers poll while disabled.)

### N14 — Project publish has no from-state guard; `ContentStatus.Review` is unreachable — severity medium
`PublishProjectCommand` publishes from any status and re-fires the announcement each call (`Projects:/Handlers/ProjectCommandHandlers.cs:311-341`); no endpoint ever sets `ContentStatus.Review` (`SharedKernel:/Enums/ContentStatus.cs:12` — grep finds no writer in Projects).

### N15 — WITHDRAWN ON VERIFICATION (no gap)
TestingLab committee voting was suspected of trusting caller identity; the handler verifies active committee membership from the actor context, `UnderReview` state and duplicate votes (`TestingLab:/EventsWorkflow/TestingApplicationHandlers.cs:559-600`). Number reserved to keep cross-references stable.

### N16 — `ProjectWork` and `Teams` have no test projects — severity low
`apps/api/tests/` contains no `GameGuild.ProjectWork.*` or `GameGuild.Teams.*` project; both modules ship entities + services (`ProjectWork:/ProjectWorkModule.cs`, `Teams:/TeamsModule.cs`) with zero test coverage.

## Priority shortlist

1. **N5 + N6** (jam identity + lifecycle) — integrity of competition results.
2. **N10** (dashboards cross-tenant) — one-line fix in repository predicate + policy.
3. **N1** (no text moderation) — product decision needed on TrustSafety enablement.
4. **N4** (group self-service) — product decision on member-facing join.
5. **N12** (AI credit parity) — align with the economy package's billing model.
