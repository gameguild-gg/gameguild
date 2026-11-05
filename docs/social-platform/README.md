# GameGuild Social + Platform — Documentation Package

> Social family (9 sub-modules) + Platform family (13 sub-modules) · Documents 01–05 cut against `develop` commit `43603cb5b`; the security completion (07–10) against `ce31d585` with every reused citation re-verified.
> Scope: **510 endpoints across 45 controllers / module-API files** (Social 127, Platform 383), plus the service-only modules (`Social.Announcements`, `ProjectWork`, `Localization`, `Teams`, `Tags`) that carry no HTTP surface.

## Scope

### Social family (9 sub-modules, 127 endpoints, 17 controller files)

| Module | Controllers / module-API files | Endpoints |
|---|---|---|
| `GameGuild.Social.Posts` | `PostsCrudController`, `PostCommentsController`, `PostInteractionsController` | 26 |
| `GameGuild.Social.Feed` | `SocialFeedController`, `FeedController` (in `FeedModuleApi.cs`), `SavedPostsController`, `StoriesController` | 15 |
| `GameGuild.Social.Profiles` | `SocialProfilesController` | 11 |
| `GameGuild.Social.Groups` | `SocialGroupsController` (in `GroupsModuleApi.cs`) | 13 |
| `GameGuild.Social.Follows` | `FollowersController` | 21 |
| `GameGuild.Social.Reactions` | `ReactionsController` (in `ReactionModuleApi.cs`) | 4 |
| `GameGuild.Social.Blog` | `BlogAuthoringController`, `BlogAdminController`, `BlogAiController`, `BlogCommentsController`, `BlogPublicController` | 35 |
| `GameGuild.Social.Announcements` | none — service-only bridge (`AnnouncementsModule.cs:22-31` registers `PublicationAnnouncerHandler`) | 0 |
| `GameGuild.Social.Assets` | `SocialMediaAssetsController` | 2 |

### Platform family (13 sub-modules, 383 endpoints, 28 controller files)

| Module | Controllers | Endpoints |
|---|---|---|
| `GameGuild.AI` | `AiController` (12, incl. 2 dual-route actions), `AiPromptTemplatesController` (7), `TenantAiController` (3) | 22 |
| `GameGuild.Analytics` | `AnalyticsController` (7), `DashboardsController` (4), `ProductMetricsController` (2) | 13 |
| `GameGuild.Assets` | `AssetsController` (20), `AssetsAdminController` (15), `AssetLibrariesController` (6), `AssetsCdnController` (3), `SecureAssetDeliveryController` (2) | 46 |
| `GameGuild.Features` | `FeaturesController` (9), `FeatureFlagsController` (4 active; 6 CRUD routes commented out), `CapabilitiesController` (6) | 19 |
| `GameGuild.GameJams` | `GameJamsController` (in `GameJamsModuleApi.cs`) | 9 |
| `GameGuild.Projects` | `ProjectsController` (37), `ProjectPermissionController` (7), `ProjectStoreProductsController` (4) | 48 |
| `GameGuild.ProjectWork` | none — entity + service module (`ProjectWorkModule.cs`) | 0 |
| `GameGuild.TestingLab` | 11 controllers (events 44, sessions 19, requests 15, participation 14, participants 12, permissions 11, feedback 7, templates 6, locations 6, settings 5, analytics 2) | 141 |
| `GameGuild.LaunchPad` | `LaunchPadEventsController` (22), `LaunchPadController` (6), `LaunchPadSettingsController` (2) | 30 |
| `GameGuild.Monitoring.SLA` | `SlaMonitoringController` | 10 |
| `GameGuild.Localization` | none — service/repository module (languages, translation workflow, localized errors) | 0 |
| `GameGuild.Teams` | none — entity + authorization-service module (`TeamsModule.cs`) | 0 |
| `GameGuild.Tags` | none — entity module consumed by Profiles/Posts tag surfaces | 0 |

All 22 assemblies are enabled in the host: the Social nine plus `GameJams`, `LaunchPad`, `Projects`, `ProjectWork`, `Teams`, `TestingLab` via `ApiProductComposition.cs:81-109`; `AI`, `Analytics`, `Assets`, `Features`, `Localization`, `Monitoring.SLA`, `Resources`, `Tags` via the common list `ModuleConfiguration.cs:11-40`. The disabled list (`ApiProductComposition.cs:113-115`) contains only `Compliance.FinancialCrime` and `TrustSafety` — the latter is the moderation-truth fact behind gap N1.

## Document inventory

| File | Contents |
|------|----------|
| [`01-user-flows.md`](01-user-flows.md) | Nine Mermaid `flowchart TD` user flows — post lifecycle (incl. the moderation-queue truth), feed assembly, group membership, blog publish, asset upload → moderation (`ModerationClassificationWorker`), game-jam lifecycle, project lifecycle + collaboration, TestingLab session, AI credit consumption — each with an edge-case matrix and `file:line` per node |
| [`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md) | Route → command/query/service → validator → handler → auth → rate limit → unit test → HOLE? for all 45 controller files; cross-references the CI wiring-gap guard instead of re-deriving the handler-less surface |
| [`03-state-machines.md`](03-state-machines.md) | `stateDiagram-v2` for jam, project (+ project version), asset moderation, group membership, TestingLab event/session, blog post; transition-audit tables with defect index |
| [`04-event-inventory.md`](04-event-inventory.md) | Every event/notification/stream with producer/consumer `file:line` — the 12 orphan `Post*` events, the announcement bridge, blog-AI credit ledger, AI quota/billing trail, asset moderation + outbox events; cross-module `sequenceDiagram`s (Social↔Notifications, Platform↔Economy.AiCredits, Assets↔Openinary) |
| [`05-gaps.md`](05-gaps.md) | 5 KNOWN (cross-referenced to the merged packages' issues #1199–#1226) + 14 NEW gaps with `file:line` (2 candidates withdrawn on verification) — filed as issue **#1250** |
| [`07-stride-threat-model.md`](07-stride-threat-model.md) | S/T/R/I/D/E for 12 families — Social C1 post lifecycle/moderation truth, C2 feed assembly, C3 group membership/takeover, C4 blog publish, C5 reactions, C6 profile exposure [PII]; Platform P1 asset upload→moderation (malware), P2 jam scoring integrity, P3 project data exfiltration, P4 TestingLab session, P5 AI credit theft — cross-refs every #1250 finding and adds **SP1–SP5** (profile read-side visibility, scan-disabled clean pass, anonymous share/statistics, repost block evasion, NoOp AI billing recorder) |
| [`08-dfds.md`](08-dfds.md) | L0 context + one L1 per STRIDE family (12); trust boundaries TB1–TB7 explicit (user↔web↔API↔moderation queue↔CDN/assets↔AI provider↔tenant); PII flows marked |
| [`09-security-architecture.md`](09-security-architecture.md) | component diagram; the moderation pipeline (ModerationClassificationWorker + the TrustSafety disabled truth + SP2/N13 fail-open map); the feed ranking trust boundary (there is none — and why that is the control); who-can-moderate permission map; the CDN token boundary; the AI credit wallet boundary; consolidated fail-open/fail-closed table |
| [`10-cross-module-sequences.md`](10-cross-module-sequences.md) | Social↔Notifications (announcements bridge), Platform↔Economy (AI credits), Assets↔Openinary (provider, 120 s + retry 3), plus ClamAV and token-delivery seams — timeout inventory and per-call fail-open/fail-closed verdicts per the auth package conventions |

## Headline findings

1. **Zero dead endpoints in both families** — 0 of 510 (0.0%), versus 31/177 (17.5%) in the authorization family before its fix (#1200, #1203). Every sent command/query type has a handler class, all 22 assemblies are handler-scanned by `AddCqrs` (`SharedKernel:/CQRS/Extensions/ServiceCollectionExtensions.cs:46-72`), and the CI wiring guard enforces the invariant with an empty baseline (`apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt:23-24`). No `UnavailableReason` commands exist in scope (grep across all 22 modules: zero hits), so no 422-planned surface either. The holes in these families are behavioral, not wiring (see `05-gaps.md`).
2. **The moderation truth is asymmetric**: there is no post/comment text moderation anywhere in Social (gap N1), while Platform `GameGuild.Assets` runs a real pipeline — upload → virus scan → `ModerationClassificationWorker` → Openinary classification → threshold policy → human queue. The two Social content gates are the post attached-media safe-to-serve check (`PostsCrudController.cs:141-154`) and the blog asset-manifest gate at publish (`BlogPostService.cs:199-203`).
3. **All 12 `Post*` domain events are orphans** — declared in `Social.Posts/Events/PostEvents.cs`, never constructed, never dispatched, zero consumers; and they escape the CI G4 dead-event guard because they are plain records on `DomainEventBase`, not `IDomainEvent` (gap N2).
4. **The legacy FeedItem inbox has one producer — itself**: `POST /api/social/feed` is the only writer of `FeedItem` (`FeedModuleApi.cs:85`); no module projects posts/blogs/sessions into it, so `GET /api/social/feed/users/{userId}` returns an inbox only its own endpoint family can append to (gap N3).
5. **Game-jam lifecycle has no from-state guard**: `SetStatusAsync` writes any status onto any status (`GameJamsModuleApi.cs:215-227`) and `SubmitAsync` never checks `jam.Status` at all — entries can be submitted to cancelled or completed jams, with a caller-asserted `UserId` from the request body (`GameJamsModuleApi.cs:229-249`, `:381`) (gap N6).
6. **Group membership is tenant-admin-only**: join/leave/approve/reject/role all sit behind `[Authorize(Policy = Policies.TenantAdmin)]` + `EnsureCanManageGroupAsync` — ordinary members cannot join a group through the API (`GroupsModuleApi.cs:751-845`) (gap N4).
7. **Generic AI chat/generation pays no credits**: `/ai/*` endpoints enforce only the Resources count quota (`AiOrchestrator.cs:331-337`); the full credit lifecycle (Quote → Reserve → Settle/Release) runs exclusively in the Social.Blog and Learning.Courses authoring services (`BlogAuthoringAiService.cs:176-221,495-600`) (gap N12).
8. **Zero FluentValidation coverage across all nine Social modules** and most of Platform — validators exist only in `Assets` (12), `Monitoring.SLA` (11), `Resources` (16), `TestingLab` (4) and `Projects` (1) (gap N7).

## Conventions

- Follows the merged package conventions (`docs/learning/*`, `docs/economy/*`, `docs/commerce/billing-orders/*`): `file:line` on every node, `L###` shorthand inside Mermaid labels, edge-case matrices as tables, honest gaps section.
- Mermaid labels contain no parentheses and no colons (repo rendering constraint).
- Citation shorthand: `Posts:`, `Feed:`, `Groups:`, `Blog:`, `Follows:`, `Reactions:`, `Profiles:`, `SocialAssets:` for the Social modules; `AI:`, `Analytics:`, `Assets:`, `Features:`, `Jams:`, `Projects:`, `TestingLab:`, `LaunchPad:`, `SLA:`, `Resources:` for Platform. Paths are relative to `apps/api/Source/Modules/` unless prefixed `Host:` (= `apps/api/Source/GameGuild.API/`), `SharedKernel:` (= `Modules/GameGuild.SharedKernel/`), or `Tests:` (= `apps/api/tests/`).

## Related reading

- `docs/architecture/api-contract-matrix.md` (PR #1213) — the client-caller half of this surface; the 510 endpoints here overlap its ENDPOINT-NO-CALLER analysis.
- `docs/architecture/CQRS_BYPASS_KNOWN_DEBT.md` — the service-injection bypass debt (`FollowersController`, `TestingLabPermissionController`, `AssetsCdnController`, `FeedController` are in-scope entries).
- `docs/architecture/ASSETS_MODULE_ARCHITECTURE.md` — asset storage/delivery architecture this package's asset-moderation flow complements.
- `docs/operations/background-workers.md` (PR #1213) — worker inventory; `ModerationClassificationWorker` and the virus-scan services appear there too.
