# Social + Platform — STRIDE Threat Model

> Social family (9 sub-modules) + Platform family (13 sub-modules) · Documented against `develop` commit `ce31d585` (the package's flows/matrix/states/events were cut against `43603cb5b`; every citation below was re-verified at `ce31d585`).
> Scope: **S/T/R/I/D/E for the nine flow families of [`01-user-flows.md`](01-user-flows.md)** — post lifecycle (with the moderation-queue truth), feed assembly, group membership, blog publish, asset upload → moderation, game-jam lifecycle, project lifecycle, TestingLab session, AI credit consumption — **plus two families the flows package does not cover**: reactions/engagement and profile exposure (PII).
> Cross-references: the filed findings of issue **#1250** (gap register [`05-gaps.md`](05-gaps.md), IDs N1–N16) are reused verbatim below; **new findings first recorded by this model carry the SP prefix** (SP1–SP5) and extend #1250. Architecture: [`09-security-architecture.md`](09-security-architecture.md); DFDs: [`08-dfds.md`](08-dfds.md); sequences with timeout/fail-open verdicts: [`10-cross-module-sequences.md`](10-cross-module-sequences.md).

Citation shorthand as in [`01-user-flows.md`](01-user-flows.md) (`Posts:`, `Feed:`, `Groups:`, `Blog:`, `Follows:`, `Reactions:`, `Profiles:`, `SocialAssets:`, `Announcements:`, `AI:`, `AiCredits:`, `Assets:`, `Jams:`, `Projects:`, `TestingLab:`, `LaunchPad:`, `Resources:`, `Host:`, `SharedKernel:`, `Tests:` — paths relative to `apps/api/Source/Modules/` unless prefixed `Host:` = `apps/api/Source/GameGuild.API/`). `Web:` = `apps/web/src/`.

## 1. Methodology and trust boundaries

STRIDE per family, weighting **T** (tampering — content integrity, scoring integrity) and **I** (information disclosure — PII, cross-tenant) alongside **E** (elevation of privilege). Trust boundaries crossed by the families:

- **TB1** user/browser → web client (Next.js; all rendering escapes by default — the stored-XSS question is *where* raw text can bypass that)
- **TB2** web/client → API host (the auth middleware chain; `[Authorize]` vs `[AllowAnonymous]` per controller)
- **TB3** API → persistence (PostgreSQL — the tamper surface for content, scores, credits)
- **TB4** API → moderation lane (Assets virus scan + `ModerationClassificationWorker` + Openinary vLLM classification — machine-trust boundary)
- **TB5** API → CDN/asset delivery (HMAC-signed path tokens, `[AllowAnonymous]` token-gated routes)
- **TB6** API → AI providers (OpenAI/Anthropic/Google adapters — outbound HTTP with DI-default timeouts)
- **TB7** tenant A → tenant B (tenant isolation inside one database — the N10 class)

## 2. C1 — Post lifecycle and the moderation-queue truth (S/T/R/I/D/E)

**Assets**: `Post` (`Posts:/Entities/Post.cs` — content, visibility, counters; **no moderation status field**), `PostComment`, the attach gate `PostsCrudController.cs:141-154`.

| Threat | Vector | Current control (mitigation) | Verdict |
|---|---|---|---|
| **T — content injection in posts (stored XSS)** | raw text goes `CreatePostRequest` → `CreatePostEndpointCommand` → persistence with **no server-side sanitization or canonicalization** (`PostsCrudController.cs:155-161`) | first-party render is escaped: React text nodes escape; the only `dangerouslySetInnerHTML` in the web app is JSON-LD built from typed fields with `<` → `\u003c` escaping (`Web:lib/seo/jsonld.tsx:235-242`); blog RSS XML-escapes user fields (`Web:lib/blogs/rss.ts:35,50`) | **covered for the first-party web client; open as a contract** — the API stores and returns raw text with no content-type marking (plaintext vs markup), so any downstream consumer (mobile client, email digest, third-party API user via the 510-endpoint surface) that trusts post text as HTML inherits stored XSS. Compounded by N1 (no text moderation to flag payloads) |
| **T — content injection in comments** | same path for `AddPostCommentEndpointCommand` (`Posts:/Controllers/PostCommentsController.cs:44`) | same render-layer escaping; author checks in `Posts:/Services/PostCommentService.cs` | same verdict as above |
| **R — spoofed attribution** | create a post that renders as another user | `CreatePost` takes `userId` from `GetCurrentUserId()` (`PostsCrudController.cs:133-137`) — never from the body | covered |
| **I — visibility bypass** | read a `Private`/`Followers`/`Unlisted` post | service-side visibility check on direct GET (own post or Public; Followers requires following) — `Feed:/Queries/SocialFeedQueryService.cs:332-335` and the F1 truth table | covered |
| **D — moderation bypass (the headline)** | publish text that would be policy-violating | **no text moderation exists anywhere in Social** — no flag/report endpoint for posts or comments across all 26 Posts endpoints, and the `TrustSafety` module is **disabled** in the host (`Host:/Core/Setup/ApiProductComposition.cs:113-115` — disabled list verified at HEAD: `Compliance.FinancialCrime`, `TrustSafety`) | **open — gap N1 / #1250 item 4**. The only gates are the attached-media safe-to-serve check (fails closed, `PostsCrudController.cs:141-154`) and nothing for text |
| **D — republish after soft-delete** | author soft-deletes, recreates identical content | no dedup, no cooldown (`Post.Create` has none — F1 edge 7) | accepted (documented), elevated impact only because of N1 |
| **E — attach another user's asset** | `AssetReferenceId` of a victim's pending/ready asset | `ResolveReadyOwnedInTenantAsync` requires **owned + ready + same tenant** (`PostsCrudController.cs:141-154` → `SocialAssets:`) | covered — fails closed with 400 "Media is unavailable" |

## 3. C2 — Feed assembly (S/T/R/I/D)

**Assets**: `SocialFeedQueryService`, `FeedCursor`, the legacy `FeedItem` inbox (`Feed:/FeedModuleApi.cs`).

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **T — feed manipulation via ranking abuse** | inflate signals that drive ordering | **the feed has no ranking inputs**: ForYou is reverse-chronological visibility-filtered posts merged with TestingSessions (`SocialFeedQueryService.cs:87-113,332-335`); `PostStatistics.TrendingScore` is computed in Posts and read by nothing in Feed (gap N11) | **structural mitigation (nothing to manipulate yet)** — but see N9 below: the counters that *would* feed ranking are already pollutable, so any future ranking adoption inherits poisoned signals |
| **T — anonymous counter inflation** | `POST {id}/view` and `POST {id}/share` are `[AllowAnonymous]`, no dedup (`Posts:/Controllers/PostInteractionsController.cs:83-84,96-97`) | none — counters increment unauthenticated | **open — gap N9, extended here as SP3** (share is anonymous too, not just view; `GET {id}/statistics` is the public read-back `:119-120`) |
| **T — cursor tampering** | forged/opaque cursor to skip or replay pages | keyset cursor with `FeedCursor.TryDecode` throwing `InvalidFeedCursorException` (`Feed:/Queries/FeedCursor.cs`); `take` clamped 1–30 (`SocialFeedQueryService.cs:58`) | covered |
| **T — mute/block evasion via reposts** | blocked author's content re-enters through a third user's repost | the exclusion set keys on **post author ids** (`GetBlockedAuthorIdsAsync`, `SocialFeedQueryService.cs:414-427` applied to feed rows); a repost is a new row authored by the reposter that links the source via `RepostOfPostId` (`Posts:/Entities/Post.cs:50-58`) — the repost row itself is **not** excluded | **partial / SP4 (new)** — block evasion by repost laundering: the exclusion predicate has no transitive term over `RepostOfPostId`; severity depends on how the projection renders the linked source (residual risk recorded; projection check recommended before fix) |
| **T — inbox stuffing** | write arbitrary `FeedItem` rows | the legacy inbox's only writer is the user's own `POST /api/social/feed` (`Feed:/FeedModuleApi.cs:85`) | accepted (self-harm surface only) — the real problem is the inverse, gap N3 (write-starved inbox) |
| **R — session injection mislabelled** | TestingSessions presented inside "ForYou" posts feed | sessions injected only while `Scheduled|Active` and manager not muted (`SocialFeedQueryService.cs:359-387`) | accepted (presentation truth, not spoofing) |
| **D — tag-filter silent empty** | `scope=community&tag=x` returns empty page | documented behavior (F2 edge 3) | accepted |

## 4. C3 — Group membership and takeover (S/T/R/I/E)

**Assets**: `SocialGroup`/`SocialGroupMember` (`Groups:/Entities/SocialGroup.cs:261-267`), the membership endpoints (`GroupsModuleApi.cs:751-845`).

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **E — group takeover by role escalation** | promote self/ally to `Owner` in a group | `PUT members/{userId}/role` requires `Policies.TenantAdmin` **plus** `EnsureCanManageGroupAsync` (`GroupsModuleApi.cs:822-833`); membership machine itself is Pending-gated (`03-state-machines.md` SM4) | **covered against ordinary members; open against tenant admins** — any tenant admin can operate membership on any group in the tenant (grant Owner, remove the real owner). This is by design of the policy, but the group status setter is not state-guarded (below) |
| **E — membership operations on behalf of another user** | join takes the target `UserId` from the request body (`JoinSocialGroupRequest`) | gated by TenantAdmin + manage-group (`GroupsModuleApi.cs:771-784`) | **open as a product-shape gap — N4 / #1250 item 5**: there is *no* member self-service path at all; the flip side is that every membership mutation is admin-asserted, so attribution of "who joined" is the admin, not the member |
| **T — group resurrection/status tampering** | re-activate an `Archived`/`Suspended` group, or suspend a live one to strand members | `SetStatusAsync` is a blind setter — activate/archive/suspend accept any from-state (`GroupsModuleApi.cs:735-748`; defect D8) | **open — N4/D8** |
| **I — member list disclosure** | enumerate a Private group's members | `GET members` is TenantAdmin + manage-group (`GroupsModuleApi.cs:753-769`) | covered (admin-only read) |
| **S — join/leave spam against a group** | membership flooding | all membership writes are admin-gated (N4's flip side) | covered de facto |

## 5. C4 — Blog publish (S/T/R/I/D/E)

**Assets**: `BlogPostService` publish gate (`Blog:/Services/BlogPostService.cs:190-254`), `BlogAssetManifestService`, admin takedown (`BlogAdminController`).

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **T — content injection in blog bodies** | blog body carries markup (block content incl. an HTML block type) | **two-layer control**: (1) the publish gate requires every body `asset-uri` to reference an **Approved + clean** asset (`BlogPostService.cs:199-203`, fails closed naming the asset id); (2) the web render path is structured (Lexical state) and the HTML block preview strips `<script>`, passes DOMPurify `FORBIB_TAGS: ["script"]`, and runs in an **empty-sandbox iframe** (`Web:components/block-content-editor/extras/html/html-utils.ts:194-203` — `FORBID_TAGS: ["script"]` + DOMParser `<script>` removal — and `html-editor.tsx:50-56`) | **covered for media and for the first-party viewer**; residual: raw body blocks leave the API unsanitized (same downstream-consumer contract risk as C1) and text is unmoderated (N1) |
| **D — publish-gate bypass via legacy slug/redirect** | reach content through the legacy slug resolver | `ResolveLegacyBlogSlugQuery` only redirects (`BlogPublicController.cs:66`); comments require `Published` (`BlogPostService.cs` AddCommentAsync) | covered |
| **E — non-primary coauthor publishes** | coauthor forces publication | "Only the primary author can publish" (`BlogPostService.cs:196`); primary transfer is its own guarded endpoint (`BlogAuthoringController.cs:200`) | covered |
| **D — admin takedown evasion** | republish after `AdminUnpublish`/`AdminDelete` | admin unpublish/delete exist (`BlogAdminController.cs:47,68`, `Policies.SystemAdmin`); re-publish from Draft is possible for the author | **partial** — takedown is not sticky (no tombstone); with N1 there is also no text-side reason for the takedown to be enforceable beyond the asset gate |
| **I — draft leakage** | read another author's Draft | authoring endpoints enforce primary/coauthor identity; public surface only serves `Published` | covered |
| **S — announcement spam at publish** | repeated publish → repeated follower fan-out | publish is idempotent per post (`BlogPostService.cs:198-201`); fan-out capped at 50 with over-cap skip (`Announcements:/Services/PublicationAnnouncerHandler.cs:17,126-129`) | covered (cap is also the honest limitation — N-class accepted) |

## 6. C5 — Reactions and engagement (S/T/R)

**Assets**: `ReactionsController` (`Reactions:/ReactionModuleApi.cs:208-264`), `PostEngagementService`, `PostStatistics`.

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **S — reaction flooding** | scripted like/dislike brigading | class `[Authorize]` on reactions (`ReactionModuleApi.cs:209`); no per-target rate limit beyond platform default (matrix rate-limit summary: only Resources/CDN pin limits) | **partial** — authenticated but unthrottled; rides the platform default policy |
| **T — reaction identity spoofing** | react as another user | `SetReactionCommand` takes the actor from `ActorContext.SubjectIdAsGuid`, not the body (`ReactionModuleApi.cs` — controller resolves actor before Send) | covered |
| **R — engagement-count truth** | counters diverge from real reactions | counters updated transactionally by `PostEngagementService`; but see C2/SP3 — `view`/`share` counters are anonymous and dedup-free | **partial** — like/repost counters trustworthy, view/share counters pollutable (N9/SP3) |
| **I — reaction surveillance** | enumerate who reacted to infer reading behavior | reaction summaries are aggregate (`GetTargetReactionsQuery` → summary dto); `me/target` returns only the caller's own reaction | covered |

## 7. C6 — Profile exposure and PII (S/T/R/I/E)

**Assets**: `SocialProfile` (`Profiles:/Entities/ProfileEntities.cs:35-108` — self-disclosed PII: `Bio`, `Location`, `TimeZone`, `WebsiteUrl`, `SocialLinksJson`, plus visibility flags `ShowActivity/ShowPortfolio/ShowSkills` and `ProfileVisibility Private|Connections|Public`).

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **I — profile PII leakage to anonymous readers** | `GET /api/social/profiles/@{handle}` and `GET /search` are `[AllowAnonymous]` (`Profiles:/Controllers/SocialProfilesController.cs:34-42`) | search filters `Visibility == Public` (`Profiles:/Repositories/SocialProfileRepositories.cs:60-63`) — **but `GetByHandleAsync` and `GetByUserAsync` apply NO visibility filter** (`SocialProfileRepositories.cs:47-58`: predicates are handle/userId + not-deleted only), and `ToDto()` returns every field including `Location`, `WebsiteUrl`, `SocialLinksJson` (`ProfileEntities.cs:180-205`) | **open — SP1 (new, extends #1250)**: a `Private`/`Connections` profile is fully readable by anyone who knows or guesses the handle (handles are normalized lowercase — `NormalizeHandle`, `ProfileEntities.cs:207` — so enumeration is cheap). The privacy switch only removes you from search, it does not hide the profile |
| **T — PII injection (stored XSS via profile fields)** | `Bio`/`Headline`/`WebsiteUrl` carry markup | no server-side sanitization on `UpdateSocialProfileCommand` (same class as C1); web renders as React text | covered first-party; same downstream-consumer contract risk as C1 |
| **E — profile takeover** | edit another user's profile/skills/portfolio | every write enforces `IsActor(userId)` or `OwnsProfileAsync(profileId)` (`SocialProfilesController.cs:47,59,73,87,105,122,139` — the N8 candidate, withdrawn because these guards are real) | covered |
| **R — handle squatting of another identity** | register a handle resembling a brand/person | `GetOrCreate` gated to actor/system-admin (`SocialProfilesController.cs:26-29`); no reserved-handle list or verification tier in the write path (`VerifiedAt` exists but has no writer in this module) | accepted (documented) — impersonation is a product-moderation problem, and text moderation is N1-open |
| **I — or-create as another user** | `GET users/{userId}/or-create` mints a profile for a victim | actor-or-system-admin gate (`SocialProfilesController.cs:26-29`) | covered |

## 8. P1 — Asset upload → moderation (Platform; asset malware) (S/T/R/I/D/E)

**Assets**: `AssetsController.Upload` (`Assets:/Controllers/AssetsController.cs:29-66`, 100 MB `RequestSizeLimit`), `VirusScanService`, `VirusScanBackgroundService`, `ModerationClassificationWorker`/`Processor`, `UnresolvedModerationClassifier`, `ModerationDecisionPolicy`.

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **S/T — asset malware upload** | serve a weaponized file to other users | three-stage pipeline: local policy (blocked extensions `.exe/.dll/.msi/.bat/.cmd/.ps1/.sh/…`, `VirusScan/VirusScanService.cs:102-104`, enforced `:409`), optional ClamAV daemon stream-scan (`UseClamAvDaemon`, `:90`), then image classification with fail-closed routing (`Moderation/ModerationDecisionPolicy.cs` — `Failed`/unknown-label/no-label/human-review-mime → `NeedsReview`, never auto-approve); rejected binaries purged | **covered when enabled and ClamAV on**; two open config-truths below |
| **D — malware gate disabled at the options level** | `VirusScanOptions.Enabled=false` returns `IsClean=true, "Scanning disabled"` (`VirusScanService.cs:206-210`) | none — this is a **silent fail-open** distinct from the scanner-unavailable switch | **open — SP2 (new)**: the adjacent switch `FailClosedWhenScannerUnavailable` defaults **true** (`VirusScanService.cs:95` — ClamAV outage rejects the file), so the pipeline is fail-closed per-scanner but fail-open per-feature |
| **D — moderation-off freeze** | `AutoModerationOptions.Enabled=false` stops the classification worker (`BackgroundServices/ModerationClassificationWorker.cs:17-21`); uploads stay `ModerationStatus.Pending` forever | the Social consumer gates fail closed (post attach + blog manifest refuse non-Approved media) — but the queue never drains and nothing alerts | **open — N13** (availability-side of the same coin as SP2) |
| **T — classifier substitution with an over-permissive one** | host registers an `IAssetModerationClassifier` that always returns `safe` | default is fail-closed `UnresolvedModerationClassifier` (`Moderation/UnresolvedModerationClassifier.cs`); any registered classifier bypass is a composition-root decision — `ModerationDecisionPolicy.KnownLabels` still forces unknown labels to review | partial (policy hardening documented; no CI guard on classifier registrations) |
| **E — moderation-queue access** | non-admin reviews/approves own upload | queue + review are `RequireAdminRole` (`AssetsAdminController.cs:20,50,313`) | covered |
| **I — cross-tenant asset read** | fetch another tenant's binaries | tenant validation on secure delivery (`Security/TenantAssetValidationService.cs:60,145`; `SecureAssetDeliveryController.cs:123-131`); CDN routes are token-gated `[AllowAnonymous]` with HMAC-SHA256 + time-window tokens (`Services/AssetTokenService.cs:33-46`) | covered (see 09 §5 for the token boundary) |
| **T — double-processing race** | two workers classify the same content | CAS claim `TryBeginModerationAsync` (`ModerationClassificationProcessor.cs:47-50`); crashed `Processing` rows re-picked indefinitely | partial — K1/#1218 G4 (re-entrancy without lease) |

## 9. P2 — Game-jam lifecycle and scoring integrity (S/T/R/E)

**Assets**: `GameJamsModuleApi` service block (`Jams:/GameJamsModuleApi.cs:215-287`) and routes (`:364-393`).

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **T — jam scoring manipulation via spoofed judge** | score a submission as another user | `ScoreJamSubmissionCommand` takes `request.JudgeUserId` **from the body** (`:391`); score value is clamped to criteria `MaxScore` (`:281`) but the judge identity is caller-asserted | **open — N5 / #1250 item 2** |
| **E — submission as another user** | enter a jam under a victim's identity | `SubmitJamEntryCommand` takes `request.UserId` from the body (`:381`) | **open — N5** |
| **T — resurrect a cancelled jam / rewrite history** | `Completed → Active`, `Cancelled → Upcoming` | `SetStatusAsync` writes any status onto any status, `[Authorize]` only (`:215-227`) | **open — N6/D1**; also no worker flips `Upcoming→Active` at start time (`:197`) |
| **T — entries on cancelled/completed jams** | submit after deadline | `SubmitAsync` never reads `jam.Status` (`:229-249`) | **open — N6/D2** |
| **D — results repudiation** | no audit trail of who scored | scores persist `JudgeUserId` — but it is attacker-controlled (N5), so the trail is repudiable | open (consequence of N5) |
| **S — participant-cap exhaustion** | fill `MaxParticipants` with spoofed entries | cap checked (`:233-236`) but spoofable identities make the cap trivially exhaustible | open (consequence of N5) |

## 10. P3 — Project lifecycle and data exfiltration (S/T/R/I/E)

**Assets**: `ProjectsController` (37 endpoints), `ProjectCommandHandlers`, invitations (`ProjectsController.cs:652-756`), collaborator permissions, LaunchPad participant.

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **I — project data exfiltration by id** | read another tenant's project/version content | publish-failure is NotFound-shaped (`ProjectCommandHandlers.cs:322-324`); collaborator/permission resolvers gate content ops — **but the general read surface relies on per-endpoint permission attributes**; the demonstrated cross-tenant-by-id class in this family is Analytics (N10) | covered on the audited paths; **N10 remains the live instance of the class** (`Analytics:/Repositories/AnalyticsRepositories.cs:103-105` — id + not-deleted only, `[Authorize]` class-level `DashboardsController.cs:8`) — treat every id-addressed Platform read as suspect until scoped (see 09 §6) |
| **T — invitation tampering** | accept/forge another user's invitation | token-validated accept with pending+expiry+email match (`ProjectsController.cs:705-716`, `CanRespondToInvitation`); my-invitations scopes by `InvitedUserId` **or normalized email equality** (`:668-677`) | covered; email-match path means invitation discovery is bounded to the mailbox owner |
| **E — grant permissions you lack** | collaborator escalates a peer | `CanGrantPermissionsAsync` (cannot grant what you lack) — F7 truth | covered |
| **T — publish from any state / re-announce** | re-publish an Archived project; spam announcements | `PublishProjectCommand` has no from-state guard and re-fires `AnnouncePublicationCommand` (`ProjectCommandHandlers.cs:311-341`) | **open — N14/D4**; `ContentStatus.Review` unreachable (N14/D5) |
| **R — attribution** | version release by non-permitted collaborator | `MarkReadyForTesting` = `PermissionType.Edit`, `Release` = `Publish` (`ProjectsController.cs:297-306`) | covered |
| **S — launch-plan strand** | project close with active plans | `LaunchPadProjectLifecycleParticipant.CloseAsync` soft-deletes active plans (`LaunchPad:/LaunchPadProjectLifecycleParticipant.cs:12-29`) | covered (documented behavior) |

## 11. P4 — TestingLab session (S/T/R/I/D)

**Assets**: `TestingSessionsController`, `TestingParticipantsController`, `RequireTestingLabPermission` (`TestingLab:/Authorization/RequireTestingLabPermissionAttribute.cs`).

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **R — registration identity spoofing** | register/waitlist as another user | `RegisterForSession` takes the user from `ActorContext.SubjectIdAsGuid`, 401 otherwise (`TestingParticipantsController.cs:85-96`) — the *correct* pattern the jam module lacks | covered |
| **E — session creation without permission** | create sessions/events as a plain participant | `[RequireTestingLabPermission(Create, Session)]` (`TestingSessionsController.cs:61`); committee voting verifies membership/state/duplicates from actor context (D9 withdrawn — controls are real, `TestingLab:/EventsWorkflow/TestingApplicationHandlers.cs:559-600`) | covered |
| **I — attendance/PII of participants** | enumerate who attended | registrations list is permission-gated (`TestingParticipantsController.cs:110-115`) | covered |
| **T — attendance falsification** | manager marks arbitrary attendance | attendance marks are manager-permissioned (`TestingSessionsController.cs:212`) — trust is the manager role itself | accepted (role trust) |
| **D — analytics export abuse** | export session analytics repeatedly | analytics read + export are plain controller reads (`TestingAnalyticsController.cs:14,25`); no rate pin (matrix rate-limit summary) | accepted (documented) |
| **S — feed-session slot abuse** | inject sessions into every ForYou feed | sessions surface only while `Scheduled|Active` and manager-muted filter applies (`Feed:/Queries/SocialFeedQueryService.cs:359-387`) | covered |

## 12. P5 — AI credit consumption and theft (S/T/R/I/D/E)

**Assets**: `IAiCreditWalletService` (`AiCredits:/Integrations/AiCreditWallet.cs:220-226`), `BlogAuthoringAiService` lifecycle (`Blog:/Authoring/BlogAuthoringAiService.cs:176-221,495-600`), `AiOrchestrator` (`AI:/Services/AiOrchestrator.cs:315-445`).

| Threat | Vector | Current control | Verdict |
|---|---|---|---|
| **T — AI credit theft (consume another wallet)** | quote/reserve against a victim's wallet | `ValidateActor(tenantId, actorId)` in the wallet (`AiCreditWallet.cs:314`); blog runs resolve the actor from context, not body; reservation joins the ambient transaction with `SELECT … FOR UPDATE` row lock (`AiCreditWallet.cs:323-345`) — concurrent reservations on one wallet serialize | covered |
| **T — double-settle / settle-after-release** | replay settle to over-charge or pocket a release | `SettleAsync`/`ReleaseAsync` are idempotency-keyed per run (`AiCreditWallet.cs:396,405`; blog passes deterministic keys `release:{runId}` — `BlogAuthoringAiService.cs:577,600`) | covered |
| **E — free AI usage (credit-theft inverse)** | run generic chat/generation with zero credit accounting | `/ai/*` enforces only the Resources count quota (`AiOrchestrator.cs:331-337`); the wallet lifecycle runs exclusively in blog/learning authoring — chat traffic pays nothing | **open — N12 / #1250 item 3** |
| **D — reservation leak** | provider failure leaves credits reserved forever | release on cancel (`BlogAuthoringAiService.cs:577`) and on failure with code (`:600`); wallet throws on invalid state transitions | covered (leak-free on the audited paths) |
| **R — billing truth** | quota refunds denied | quota decremented back on provider-not-registered and provider-failure (`AiOrchestrator.cs:345,362`); billing trail `NoOp` by default (`AI:/ServiceCollectionExtensions.cs:30` — `TryAddScoped<IAiExecutionBillingRecorder, NoOpAiExecutionBillingRecorder>`) | **partial — SP5 (new)**: with the default NoOp recorder the *billing* side of generic AI is not just credit-free but also unrecorded — the trail the F9 matrix describes exists only when a recorder is registered |
| **I — prompt/response disclosure** | read another tenant's AI history | history reads keyed by tenant/actor (`AI:/Services/AiOrchestrator.cs` history calls; conversations repository scoped) | covered on audited paths |
| **S — token-quota exhaustion** | burn tenant `AiTokens` quota | token quota consumed post-execution (`AiOrchestrator.cs:375`) with refund paths on failure | covered |

## 13. Cross-cutting

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| **E/T — cross-tenant by id (the N10 class)** | any id-addressed Platform read/write lacking a tenant predicate | demonstrated live in Analytics dashboards (`N10`); the class audit is 09 §6 | **open until N10 fixed + class sweep done** |
| **D — no input validation as a threat amplifier** | malformed bodies surface as 500s / silently clamped values | FluentValidation only in Assets/SLA/Resources/TestingLab/Projects — 344 of 510 endpoints unvalidated (gap N7) | open (N7) — amplifies every T vector above |
| **S — anonymous engagement surface** | share/view/statistics endpoints (`PostInteractionsController.cs:83-84,96-97,119-120`) | none | open (N9/SP3) |
| **D — dead events hide engagement truth** | 12 orphan `Post*` events (N2) — no notification/audit trail exists to detect abuse | guard G4 extension filed with #1250 | open (N2) |
| **S — upload pressure** | 100 MB uploads × unthrottled asset POST | `RequestSizeLimit(100MB)` only; no per-user rate pin on upload (matrix rate-limit summary) | accepted (documented) |

**Scope note (which modules ride which family).** The critical families map: Posts→C1(+C5 counters), Feed→C2, Groups→C3, Blog→C4(+P5), Reactions→C5, Profiles→C6, Social.Assets→C1/P1 gates, Announcements→10 §1/C4 fan-out; Platform: Assets→P1, GameJams→P2, Projects(+ProjectWork)→P3, TestingLab→P4, AI→P5, Analytics→P3 §9.8 (N10). The non-critical modules — `Features`, `LaunchPad`, `Monitoring.SLA`, `Localization`, `Teams`, `Tags`, `Resources` — carry no dedicated flow family: their exposure is the wiring surface of [`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md) plus the cross-cutting rows above and the architecture boundaries of [`09-security-architecture.md`](09-security-architecture.md) §1/§4 (platform switches — Features; lifecycle participant — LaunchPad; quota enforcer + the family's only rate-limited controllers — Resources; entity/service-only — Teams/Tags/Localization/ProjectWork).

## 14. Threat-to-issue index

| Threat family | Where tracked | State |
|---|---|---|
| Zero text moderation / TrustSafety disabled (C1, C4, C6-R) | **#1250** (N1) | open |
| Jam caller-asserted ids + no from-state guard (P2) | **#1250** (N5/N6) | open |
| Generic AI credit bypass (P5) | **#1250** (N12) | open |
| Group membership admin-only + status blind setter (C3) | **#1250** (N4) | open |
| Analytics dashboards cross-tenant (P3, class anchor) | **#1250** (N10) | open |
| Anonymous view counter (C2) | **#1250** (N9) — extended by SP3 (share/statistics also anonymous) | open |
| Orphan Post events escape G4 (C1 audit trail) | **#1250** (N2) | open |
| Validation desert (cross-cutting) | **#1250** (N7) | open |
| Moderation-off freeze (P1 availability) | **#1250** (N13) | open |
| Project publish from-state + dead Review state (P3) | **#1250** (N14) | open |
| **SP1** — profile by-handle/by-user reads ignore `ProfileVisibility`; anonymous full-DTO read of Private profiles (C6) | new — this model, extends #1250 | **open** |
| **SP2** — `VirusScanOptions.Enabled=false` is a silent clean-pass fail-open (P1) | new — this model | **open** |
| **SP3** — anonymous share + public statistics extend the N9 counter-pollution surface (C2/C5) | new — this model | open |
| **SP4** — block evasion via repost laundering (exclusion set has no `RepostOfPostId` term) (C2) | new — this model | open (residual-risk framing) |
| **SP5** — default NoOp AI billing recorder makes generic-AI usage unrecorded, not just credit-free (P5) | new — this model | open |

**Positive findings pinned by this model** (so they are not re-litigated): post attach gate fails closed on non-ready media (C1-E); blog publish asset-manifest gate fails closed (C4); TestingLab registration resolves identity from the actor context — the pattern the jam module should copy (P4-R); the AI wallet is actor-validated, transaction-joined and idempotency-keyed (P5); moderation decision policy is fail-closed on every uncertain branch (P1).
