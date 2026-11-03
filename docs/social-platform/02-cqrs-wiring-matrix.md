# Social + Platform CQRS Wiring Matrix

> 510 endpoints across the Social (9 modules, 127 endpoints) and Platform (13 modules, 383 endpoints) families · Documented against `develop` commit `43603cb5b`.
> Columns per controller: route prefix → dispatch target (command/query via `ISender`, or injected service) → validator → auth → rate limit → unit tests → HOLE?

## Wiring guarantees (read first — do not re-derive)

1. **Handler registration is scanned, not manual-only.** `Host:/Core/Setup/ApplicationLayerExtensions.cs:139-146` collects every enabled module assembly and calls `AddCqrs`, which registers every `IRequestHandler`/`ICommandHandler`/`IQueryHandler` implementation and every FluentValidation validator found in those assemblies (`SharedKernel:/CQRS/Extensions/ServiceCollectionExtensions.cs:46-72`). Per-module `AddScoped` blocks (e.g. `Jams:/GameJamsModuleApi.cs:461-529`) are a legacy double-registration, not the primary wiring.
2. **The CI wiring guard pins this.** Guard G1 (`Tests:/GameGuild.API.UnitTests/Architecture/WiringCommandRegistrationGuardTests.cs:25-76`) fails when any concrete `ICommand`/`ICommand<T>` lacks a closed handler service in the real host collection; its baseline section is **empty** (`Architecture/wiring-gap-baseline.txt:23-24`) — there are no known-unwired commands on develop. The same infrastructure registers `UnavailableCommandHandler` fallbacks for commands whose use-case contract declares `UnavailableReason` (`ServiceCollectionExtensions.cs:74-103`); **zero such declarations exist in the 22 in-scope modules** (grep for `UnavailableReason`: no hits) — so there is no 422-planned surface either.
3. **Verification performed for this matrix.** Every `sender.Send` target type on all 510 endpoints was extracted mechanically and checked for a handler class: **510/510 dispatch to a live handler or an injected service**; the 42 expression-bodied / helper-delegating actions were resolved manually (they Send from one-line expression bodies, e.g. `Jams:/GameJamsModuleApi.cs:353-392`).
4. **Dead-endpoint verdict: 0 of 510 (0.0%).** Contrast: authorization family pre-fix 31/177 = 17.5% (#1200, issue #1203; fixed by `28a865953`). The families' holes are behavioral (gaps N1–N16 in `05-gaps.md`), not wiring.

## Validation coverage summary

FluentValidation validators exist only in: `Assets` (12 command validators), `Monitoring.SLA` (11, every route), `Resources` (16), `TestingLab` (4), `Projects` (`ProjectValidators.cs`). **Zero validators across all nine Social modules, and none in AI / Analytics / Features / GameJams / LaunchPad.** "—" in the per-controller notes means request-shape validation lives in the handler/service (or nowhere — gap N7).

## Rate limiting summary

Only `Resources` controllers pin `EnableRateLimiting` (`Internal` for SystemAdmin ops, `PerTenant`, `PerUser`). CDN asset delivery enforces its own IP/token rate service (`AssetsCdnController.cs` via `IRateLimitService`). Everything else rides the platform default policy.

## Test-project summary

Per-module unit-test projects exist for every HTTP-bearing module (`Tests:/GameGuild.Social.*.UnitTests`, `AI`, `Analytics`, `Assets`(+Integration), `Features`, `GameJams`, `LaunchPad`, `Monitoring.SLA`, `Projects`, `Resources`(+Integration), `TestingLab`). The service-only modules `ProjectWork` and `Teams` have **no test project** (gap N16). Integration coverage beyond Assets/Resources runs through `GameGuild.API.IntegrationTests`.

---

## Social family (127 endpoints)

### `GameGuild.Social.Posts/Controllers/PostCommentsController.cs` (7 endpoints)

`api/v1/posts` · class `[Authorize]` (L16); `[AllowAnonymous]` on the three tag/comment GETs L34,103,114,125 · 0 validators · tests `Social.Posts.UnitTests`. **HOLE?** none — comment add/update/delete are command-dispatched with author checks in `Posts:/Services/PostCommentService.cs`.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `{postId:guid}/comments` | `GetComments` | svc `postService.GetPostCommentsAsync` | 33 |
| POST | `{postId:guid}/comments` | `AddComment` | `AddPostCommentEndpointCommand` | 44 |
| PUT | `{postId:guid}/comments/{commentId:guid}` | `UpdateComment` | `UpdatePostCommentEndpointCommand` | 62 |
| DELETE | `{postId:guid}/comments/{commentId:guid}` | `DeleteComment` | `DeletePostCommentEndpointCommand` | 80 |
| GET | `tags/popular` | `GetPopularTags` | svc `postService.GetPopularTagsAsync` | 102 |
| GET | `{postId:guid}/tags` | `GetPostTags` | svc `postService.GetPostTagsAsync` | 113 |
| GET | `tags/search` | `SearchByTags` | svc `postService.GetPostsByTagsAsync` | 124 |

### `GameGuild.Social.Posts/Controllers/PostInteractionsController.cs` (9 endpoints)

`api/v1/posts` · class `[Authorize]` (L15); `[AllowAnonymous]` on share/view/statistics L84,97,119 · 0 validators · tests `Social.Posts.UnitTests`. **HOLE?** none wiring-wise; behavioral: `POST {id}/view` is anonymous and increments view counters (metrics pollution vector, gap N9).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `{postId:guid}/like` | `ToggleLike` | `TogglePostLikeEndpointCommand` | 31 |
| POST | `{postId:guid}/pin` | `TogglePin` | `TogglePostPinEndpointCommand` | 47 |
| POST | `{postId:guid}/reposts` | `CreateRepost` | `CreateRepostEndpointCommand` | 63 |
| POST | `{postId:guid}/share` | `Share` | `SharePostEndpointCommand` | 83 |
| POST | `{postId:guid}/view` | `RecordView` | `RecordPostViewEndpointCommand` | 96 |
| GET | `{postId:guid}/statistics` | `GetStatistics` | svc `postService.GetPostStatisticsAsync` | 119 |
| POST | `{postId:guid}/follow` | `FollowPost` | `FollowPostEndpointCommand` | 134 |
| DELETE | `{postId:guid}/follow` | `UnfollowPost` | `UnfollowPostEndpointCommand` | 155 |
| GET | `{postId:guid}/follow` | `IsFollowing` | svc `postService.IsFollowingPostAsync` | 171 |

### `GameGuild.Social.Posts/Controllers/PostsCrudController.cs` (10 endpoints)

`api/v1/posts` · class `[Authorize]` (L17); `[AllowAnonymous]` on GET reads L35,46,73,84,111 (public listing, by-id, trending, author, search) · no rate limit · 0 validators · tests `Tests:/GameGuild.Social.Posts.UnitTests`. **HOLE?** `POST /` create resolves attached media via `SocialAssets:` safe-to-serve (`ResolveReadyOwnedInTenantAsync` L142) — text content itself is unmoderated (gap N1). `PUT`/`DELETE` ownership enforced in service.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetPosts` | svc `postService.GetPublicPostsAsync` | 34 |
| GET | `{postId:guid}` | `GetPost` | svc `postService.GetPostByIdAsync` | 45 |
| GET | `feed` | `GetFeed` | svc `postService.GetFeedPostsAsync` | 56 |
| GET | `trending` | `GetTrending` | svc `postService.GetTrendingPostsAsync` | 72 |
| GET | `author/{authorId:guid}` | `GetByAuthor` | svc `postService.GetPostsByAuthorAsync` | 83 |
| GET | `my` | `GetMyPosts` | svc `postService.GetPostsByAuthorAsync` | 94 |
| GET | `search` | `Search` | svc `postService.SearchPostsAsync` | 110 |
| POST | `*(root)*` | `CreatePost` | `CreatePostEndpointCommand` | 130 |
| PUT | `{postId:guid}` | `UpdatePost` | `UpdatePostEndpointCommand` | 186 |
| DELETE | `{postId:guid}` | `DeletePost` | `DeletePostEndpointCommand` | 204 |

### `GameGuild.Social.Feed/Controllers/SocialFeedController.cs` (4 endpoints)

`api/social/feed` · class `[Authorize]` (L9) · 0 validators · tests `Social.Feed.UnitTests`. Assembled-feed reads (profiles/posts/scope page). **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `profiles/users/{userId:guid}` | `GetProfileByUser` | svc `feed.GetProfileByUserAsync` | 15 |
| GET | `profiles/{handle}` | `GetProfileByHandle` | svc `feed.GetProfileByHandleAsync` | 30 |
| GET | `posts/{postId:guid}` | `GetPost` | svc `feed.GetPostAsync` | 45 |
| GET | `*(root)*` | `Get` | svc `feed.GetAsync` | 60 |

### `GameGuild.Social.Feed/FeedModuleApi.cs` (4 endpoints)

Legacy FeedItem inbox: `FeedController` at `api/social/feed` · class `[Authorize]` (L197) · 0 validators · tests `Social.Feed.UnitTests`. **HOLE?** the inbox is write-starved — `AddFeedItemCommand` is the ONLY producer of `FeedItem` rows (`FeedModuleApi.cs:85`); no projection from Posts/Blog/TestingLab exists (gap N3).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `users/{userId:guid}` | `GetUserFeed` | `GetUserFeedQuery` | 200 |
| POST | `*(root)*` | `Add` | `AddFeedItemCommand` | 225 |
| POST | `{id:guid}/read` | `MarkRead` | `MarkFeedItemReadCommand` | 246 |
| POST | `{id:guid}/hide` | `Hide` | `HideFeedItemCommand` | 260 |

### `GameGuild.Social.Feed/SavedPosts/SavedPostsController.cs` (3 endpoints)

`api/social/saved-posts` · class `[Authorize]` (L10) · 0 validators · tests `Social.Feed.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `{postId:guid}` | `Get` | `GetSavedPostStateQuery` | 16 |
| PUT | `{postId:guid}` | `Save` | `SavePostCommand` | 27 |
| DELETE | `{postId:guid}` | `Unsave` | `UnsavePostCommand` | 45 |

### `GameGuild.Social.Feed/Stories/StoriesController.cs` (4 endpoints)

`api/social/stories` · class `[Authorize]` (L10) · 0 validators · tests `Social.Feed.UnitTests`. **HOLE?** none; stories expire by `ExpiresAt` in `Stories:/StoryService.cs`.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetActive` | `GetActiveStoriesQuery` | 14 |
| POST | `*(root)*` | `Create` | `CreateStoryCommand` | 25 |
| POST | `{storyId:guid}/views` | `MarkViewed` | `MarkStoryViewedCommand` | 45 |
| DELETE | `{storyId:guid}` | `Delete` | `DeleteStoryCommand` | 58 |

### `GameGuild.Social.Profiles/Controllers/SocialProfilesController.cs` (11 endpoints)

`api/social/profiles` · class `[Authorize]` (L11); `[AllowAnonymous]` on by-handle and search L34,39 · 0 validators · tests `Social.Profiles.UnitTests`. **HOLE?** none — every mutation enforces actor equality via `IsActor(userId)` / `OwnsProfileAsync` (`SocialProfilesController.cs:47,59,73,87`).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `users/{userId:guid}` | `GetByUser` | `GetSocialProfileByUserQuery` | 19 |
| GET | `users/{userId:guid}/or-create` | `GetOrCreate` | `GetOrCreateSocialProfileQuery` | 23 |
| GET | `@{handle}` | `GetByHandle` | `GetSocialProfileByHandleQuery` | 35 |
| GET | `search` | `Search` | `SearchSocialProfilesQuery` | 40 |
| PUT | `users/{userId:guid}` | `Upsert` | `body.ToCommand` | 44 |
| PUT | `users/{userId:guid}/privacy` | `UpdatePrivacy` | `UpdateProfilePrivacyCommand` | 55 |
| POST | `{profileId:guid}/skills` | `AddSkill` | `AddProfileSkillCommand` | 68 |
| DELETE | `skills/{skillId:guid}` | `RemoveSkill` | `RemoveProfileSkillCommand` | 85 |
| POST | `{profileId:guid}/portfolio` | `AddPortfolioItem` | `AddProfilePortfolioItemCommand` | 102 |
| PUT | `portfolio/{itemId:guid}` | `UpdatePortfolioItem` | `UpdateProfilePortfolioItemCommand` | 113 |
| DELETE | `portfolio/{itemId:guid}` | `RemovePortfolioItem` | `RemoveProfilePortfolioItemCommand` | 130 |

### `GameGuild.Social.Groups/GroupsModuleApi.cs` (13 endpoints)

`api/social/groups` · class `[Authorize]` (L641) · **every membership operation additionally carries `[Authorize(Policy = Policies.TenantAdmin)]` + `EnsureCanManageGroupAsync`** (L751-845) · group create/update/status L693-746 also TenantAdmin · 0 validators · tests `Social.Groups.UnitTests`. **HOLE?** members cannot self-join — the only join path is tenant-admin `POST {id}/members` with a body `UserId` (gap N4); public groups auto-approve at the service layer (`GroupsModuleApi.cs:412-415`).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `List` | see notes | 646 |
| GET | `{id:guid}` | `Get` | `GetSocialGroupQuery` | 678 |
| POST | `*(root)*` | `Create` | `CreateSocialGroupCommand` | 692 |
| PUT | `{id:guid}` | `Update` | `UpdateSocialGroupCommand` | 719 |
| POST | `{id:guid}/activate` | `Activate` | see notes | 735 |
| POST | `{id:guid}/archive` | `Archive` | see notes | 740 |
| POST | `{id:guid}/suspend` | `Suspend` | see notes | 745 |
| GET | `{id:guid}/members` | `ListMembers` | `ListSocialGroupMembersQuery` | 750 |
| POST | `{id:guid}/members` | `Join` | `JoinSocialGroupCommand` | 771 |
| POST | `{id:guid}/members/{userId:guid}/approve` | `Approve` | `ApproveSocialGroupMemberCommand` | 786 |
| POST | `{id:guid}/members/{userId:guid}/reject` | `Reject` | `RejectSocialGroupMemberCommand` | 807 |
| PUT | `{id:guid}/members/{userId:guid}/role` | `ChangeRole` | `ChangeSocialGroupMemberRoleCommand` | 822 |
| DELETE | `{id:guid}/members/{userId:guid}` | `Leave` | `LeaveSocialGroupCommand` | 837 |

### `GameGuild.Social.Follows/Controllers/FollowersController.cs` (21 endpoints)

`api/[controller]` (= `/api/Followers`) · class `[Authorize]` (L12); `[AllowAnonymous]` on counts/batches L106,133,170 · service-injected `IFollowerService` (CQRS-bypass entry in `CQRS_BYPASS_KNOWN_DEBT.md`) · 0 validators · tests `Social.Follows.UnitTests`. **HOLE?** none; block/mute write through the same service and feed exclusion consumes them.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `follow` | `Follow` | `FollowEntityEndpointCommand` | 37 |
| DELETE | `unfollow` | `Unfollow` | `UnfollowEntityEndpointCommand` | 56 |
| GET | `is-following` | `IsFollowing` | svc `_followerService.IsFollowingAsync` | 73 |
| PUT | `notifications` | `UpdateNotifications` | `UpdateFollowNotificationsEndpointCommand` | 82 |
| GET | `followers/{entityId}` | `GetFollowers` | svc `_followerService.GetFollowersAsync` | 105 |
| GET | `following` | `GetFollowing` | svc `_followerService.GetFollowingAsync` | 119 |
| GET | `count/followers/{entityId}` | `GetFollowerCount` | svc `_followerService.GetFollowerCountAsync` | 132 |
| GET | `count/following` | `GetFollowingCount` | svc `_followerService.GetFollowingCountAsync` | 141 |
| GET | `mutual` | `AreMutualFollowers` | svc `_followerService.AreMutualFollowersAsync` | 150 |
| POST | `batch/status` | `GetFollowStatusBatch` | svc `_followerService.GetFollowStatusBatchAsync` | 158 |
| POST | `batch/counts` | `GetFollowerCountsBatch` | svc `_followerService.GetFollowerCountsBatchAsync` | 168 |
| GET | `privacy-settings` | `GetPrivacySettings` | svc `_followerService.GetPrivacySettingsAsync` | 182 |
| PUT | `privacy-settings` | `UpdatePrivacySettings` | `UpdateFollowPrivacyEndpointCommand` | 191 |
| POST | `block` | `BlockUser` | `BlockUserEndpointCommand` | 212 |
| DELETE | `unblock/{blockedUserId}` | `UnblockUser` | `UnblockUserEndpointCommand` | 229 |
| GET | `is-blocked/{blockedUserId}` | `IsUserBlocked` | svc `_followerService.IsUserBlockedAsync` | 246 |
| GET | `blocked-users` | `GetBlockedUsers` | svc `_followerService.GetBlockedUsersAsync` | 255 |
| POST | `mute` | `MuteUser` | `MuteUserEndpointCommand` | 271 |
| DELETE | `unmute/{mutedUserId}` | `UnmuteUser` | `UnmuteUserEndpointCommand` | 290 |
| GET | `is-muted/{mutedUserId}` | `IsUserMuted` | svc `_followerService.IsUserMutedAsync` | 307 |
| GET | `muted-users` | `GetMutedUsers` | svc `_followerService.GetMutedUsersAsync` | 316 |

### `GameGuild.Social.Reactions/ReactionModuleApi.cs` (4 endpoints)

`api/social/reactions` · class `[Authorize]` (L209) · 0 validators · tests `Social.Reactions.UnitTests`. **HOLE?** none. NOTE: Posts' `POST {postId}/like` does NOT go through this module — it dispatches `TogglePostLikeEndpointCommand` in Posts.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `target/{targetType}/{targetId:guid}` | `GetTargetSummary` | see notes | 212 |
| GET | `me/target/{targetType}/{targetId:guid}` | `GetUserReaction` | `GetUserReactionQuery` | 219 |
| PUT | `*(root)*` | `Set` | `SetReactionCommand` | 234 |
| DELETE | `*(root)*` | `Remove` | `RemoveReactionCommand` | 248 |

### `GameGuild.Social.Blog/Controllers/BlogAdminController.cs` (7 endpoints)

`api/social/blog/admin` · class `[Authorize(Policy = Policies.SystemAdmin)]` (L19) · 0 validators · tests `Social.Blog.UnitTests`. **HOLE?** none; `POST import/ghost` is a SystemAdmin bulk import.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `posts` | `Posts` | `ListBlogAdminPostsQuery` | 24 |
| GET | `comments` | `Comments` | `ListBlogAdminCommentsQuery` | 33 |
| GET | `stats` | `Stats` | `GetBlogAdminStatsQuery` | 41 |
| POST | `posts/{id:guid}/unpublish` | `Unpublish` | `AdminUnpublishBlogPostCommand` | 47 |
| DELETE | `posts/{id:guid}` | `DeletePost` | `AdminDeleteBlogPostCommand` | 68 |
| DELETE | `comments/{commentId:guid}` | `DeleteComment` | `AdminDeleteBlogCommentCommand` | 90 |
| POST | `import/ghost` | `ImportGhost` | `ImportGhostContentCommand` | 116 |

### `GameGuild.Social.Blog/Controllers/BlogAiController.cs` (8 endpoints)

`api/social/blog/posts/{postId}/ai` · class `[Authorize]` (L15) · 0 validators · tests `Social.Blog.UnitTests`. Entitlement check → run → SSE stream → proposal apply/discard. **HOLE?** credit reservation is released on cancel/failure (`BlogAuthoringAiService.cs:577-606`) — wiring sound.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `entitlement` | `GetEntitlement` | see notes | 23 |
| GET | `conversations` | `GetConversations` | see notes | 41 |
| POST | `runs` | `CreateRun` | `CreateBlogAiRunCommand` | 52 |
| GET | `runs/{runId:guid}` | `GetRun` | see notes | 109 |
| POST | `runs/{runId:guid}/cancel` | `CancelRun` | `CancelBlogAiRunCommand` | 129 |
| GET | `runs/{runId:guid}/stream` | `StreamRun` | svc `Response.WriteAsync` | 149 |
| POST | `proposals/{proposalId:guid}/apply` | `ApplyProposal` | `ApplyBlogAiProposalCommand` | 177 |
| DELETE | `proposals/{proposalId:guid}` | `DiscardProposal` | `DiscardBlogAiProposalCommand` | 210 |

### `GameGuild.Social.Blog/Controllers/BlogAuthoringController.cs` (11 endpoints)

`api/social/blog/posts` · class `[Authorize]` (L17) · 0 validators · tests `Social.Blog.UnitTests`. **HOLE?** none; publish path enforces primary-author + the asset-manifest safe-to-serve gate (`Blog:/Services/BlogPostService.cs:193-228`).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `*(root)*` | `Create` | `CreateBlogPostCommand` | 22 |
| GET | `mine` | `Mine` | `ListMyBlogPostsQuery` | 43 |
| GET | `{id:guid}` | `GetById` | `GetBlogPostForAuthorQuery` | 51 |
| PUT | `{id:guid}` | `Update` | `UpdateBlogPostDraftCommand` | 71 |
| POST | `{id:guid}/slug` | `ChangeSlug` | `ChangeBlogPostSlugCommand` | 117 |
| POST | `{id:guid}/coauthors` | `AddCoauthor` | `AddBlogPostCoauthorCommand` | 148 |
| DELETE | `{id:guid}/coauthors/{userId:guid}` | `RemoveCoauthor` | `RemoveBlogPostCoauthorCommand` | 178 |
| POST | `{id:guid}/transfer-primary` | `TransferPrimary` | `TransferBlogPrimaryCommand` | 200 |
| POST | `{id:guid}/publish` | `Publish` | `PublishBlogPostCommand` | 231 |
| POST | `{id:guid}/unpublish` | `Unpublish` | `UnpublishBlogPostCommand` | 252 |
| DELETE | `{id:guid}` | `Delete` | `DeleteBlogPostCommand` | 273 |

### `GameGuild.Social.Blog/Controllers/BlogCommentsController.cs` (2 endpoints)

`api/social/blog` · class `[Authorize]` (L15) · 0 validators · tests `Social.Blog.UnitTests`. **HOLE?** none; comments only allowed on Published posts (`BlogPostService.cs` AddCommentAsync guard).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `posts/{id:guid}/comments` | `Add` | `AddBlogCommentCommand` | 23 |
| DELETE | `comments/{commentId:guid}` | `Delete` | `DeleteBlogCommentCommand` | 63 |

### `GameGuild.Social.Blog/Controllers/BlogPublicController.cs` (7 endpoints)

`api/social/blog/public` · class `[Authorize]` (L20) with `[AllowAnonymous]` on all seven reads (L29,42,53,67,78) · 0 validators · tests `Social.Blog.UnitTests`. **HOLE?** none; public index only lists Published+not-deleted.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `authors/{handle}` | `GetAuthorPosts` | `ListAuthorBlogSummariesQuery` | 28 |
| GET | `authors/{handle}/{slug}` | `GetPostDetail` | `GetPublicBlogPostDetailQuery` | 41 |
| GET | `resolve/{handle}/{slug}` | `ResolveRoute` | `ResolveBlogRouteForRedirectQuery` | 52 |
| GET | `resolve-legacy/{slug}` | `ResolveLegacySlug` | `ResolveLegacyBlogSlugQuery` | 66 |
| GET | `posts` | `GetIndex` | `ListPublicBlogSummariesQuery` | 77 |
| POST | `posts/{id:guid}/views` | `RecordView` | svc `viewCounter.IncrementAsync` | 89 |
| GET | `posts/{id:guid}/comments` | `GetComments` | `IsBlogPostPublishedQuery` | 101 |

### `GameGuild.Social.Assets/Controllers/SocialMediaAssetsController.cs` (2 endpoints)

`v{version}/assets/social-media` · class `[Authorize]` (L15) · 0 validators · tests `Social.Assets.UnitTests`. **HOLE?** none — this is the safe-to-serve resolver used by Posts create and Blog publish.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `{assetReferenceId:guid}` | `GetStatus` | svc `assets.InspectOwnedAsync` | 22 |
| POST | `*(root)*` | `Upload` | `UploadAssetCommand` | 42 |


## Platform family (383 endpoints)

### `GameGuild.AI/Controllers/AiController.cs` (12 endpoints)

`v{version}/ai` · class `[Authorize]` (L20) · 0 validators · tests `AI.UnitTests`. Two dual-route actions (`generate-content/email`+`email`, `generate-content/report`+`report`). **HOLE?** no — but quota-only billing: `ChatAiCommand`/`GenerateAiCommand` enforce the Resources `AiRequests` count quota (`AI:/Services/AiOrchestrator.cs:331-337`) and never touch the credit wallet (gap N12). `GET status` is config-derived, no dispatch. History reads go straight to the repository (bypass).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `status` | `Status` | see notes | 30 |
| POST | `chat` | `Chat` | `ChatAiCommand` | 57 |
| POST | `generate-content` | `GenerateContent` | see notes | 67 |
| POST | `generate-content/email` | `GenerateEmail` | see notes | 83 |
| POST | `email` | `GenerateEmail` | see notes | 84 |
| POST | `generate-content/report` | `GenerateReport` | see notes | 100 |
| POST | `report` | `GenerateReport` | see notes | 101 |
| POST | `generate-content/listing-description` | `GenerateListingDescription` | `GenerateAiCommand` | 117 |
| POST | `generate` | `Generate` | `GenerateAiCommand` | 169 |
| GET | `history` | `History` | see notes | 182 |
| GET | `history/export` | `ExportHistory` | see notes | 201 |
| GET | `quotas` | `Quotas` | svc `quotaReader.GetQuotaAsync` | 228 |

### `GameGuild.AI/Controllers/AiPromptTemplatesController.cs` (7 endpoints)

`v{version}/ai/prompt-templates` · class `[Authorize]` (L12); per-action tenant check `requestContextAccessor.CurrentTenantId` · 0 validators · tests `AI.UnitTests`. **HOLE?** none; CRUD is tenant-scoped in `AiPromptTemplateService`.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `List` | see notes | 18 |
| GET | `{id:guid}` | `Get` | see notes | 36 |
| POST | `*(root)*` | `Create` | `CreateAiPromptTemplateCommand` | 52 |
| PUT | `{id:guid}` | `Update` | `UpdateAiPromptTemplateCommand` | 73 |
| DELETE | `{id:guid}` | `Delete` | `DeleteAiPromptTemplateCommand` | 97 |
| POST | `{id:guid}/render` | `Render` | `RenderAiPromptTemplateCommand` | 117 |
| POST | `{id:guid}/generate` | `Generate` | `GenerateFromAiPromptTemplateCommand` | 140 |

### `GameGuild.AI/Controllers/TenantAiController.cs` (3 endpoints)

versioned tenant routes · class `[Authorize]` (L19); tenant match enforced inline · 0 validators · tests `AI.UnitTests`. **HOLE?** none; export clamps take 1-500.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/ai/history` | `GetHistory` | svc `historyReader.GetRecentAsync` | 53 |
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/ai/history/export` | `ExportHistory` | see notes | 74 |
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/ai/quotas` | `GetQuotas` | svc `quotaReader.GetQuotaAsync` | 102 |

### `GameGuild.Analytics/Controllers/AnalyticsController.cs` (7 endpoints)

`api/analytics` · class `[Authorize]` (L8) · 0 validators · tests `Analytics.UnitTests`. **HOLE?** none; `POST events` ingests raw analytics events, `POST warehouse/run` materializes facts.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `events` | `TrackEvent` | `command` | 16 |
| GET | `timeseries` | `GetTimeSeries` | `query` | 23 |
| GET | `kpi/{kpiName}` | `CalculateKpi` | `query` | 37 |
| POST | `funnel` | `AnalyzeFunnel` | `query` | 50 |
| POST | `warehouse/run` | `RunWarehouse` | `RunAnalyticsWarehouseCommand` | 58 |
| GET | `warehouse/facts` | `GetWarehouseFacts` | svc `warehouseService.GetFactsAsync` | 66 |
| GET | `warehouse/export` | `ExportWarehouseFacts` | svc `warehouseService.GetFactsAsync` | 82 |

### `GameGuild.Analytics/Controllers/DashboardsController.cs` (4 endpoints)

`api/analytics/dashboards` · class `[Authorize]` (L8) · 0 validators · tests `Analytics.UnitTests`. **HOLE?** none; no per-user ownership check on GET/PUT by id — any authenticated user reads/mutates any dashboard id (gap N10).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `List` | `GetDashboardsQuery` | 14 |
| GET | `{id:guid}", Name = "GetAnalyticsDashboardById` | `Get` | `GetDashboardByIdQuery` | 21 |
| POST | `*(root)*` | `Create` | `CreateDashboardCommand` | 30 |
| PUT | `{id:guid}` | `Update` | `UpdateDashboardCommand` | 38 |

### `GameGuild.Analytics/Controllers/ProductMetricsController.cs` (2 endpoints)

`api/metrics/product` · class `[Authorize]` (L9) · 0 validators · tests `Analytics.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `Get` | `GetProductMetricsQuery` | 19 |
| GET | `export` | `Export` | `ExportProductMetricsQuery` | 35 |

### `GameGuild.Assets/Controllers/AssetLibrariesController.cs` (6 endpoints)

`v{version}/asset-libraries` · class `[Authorize]` (L14) · 0 validators · tests `Assets.UnitTests`. Folders, restrictions, revisions, copy/restore. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `{resourceType}/{resourceId:guid}` | `Get` | svc `libraryService.GetAsync` | 22 |
| POST | `{resourceType}/{resourceId:guid}/folders` | `CreateFolder` | `CreateAssetFolderCommand` | 29 |
| PUT | `folders/{folderId:guid}/restriction` | `RestrictFolder` | `RestrictAssetFolderCommand` | 41 |
| POST | `assets/{referenceId:guid}/copy` | `Copy` | `CopyAssetReferenceCommand` | 52 |
| GET | `assets/{referenceId:guid}/revisions` | `Revisions` | svc `libraryService.GetRevisionsAsync` | 60 |
| POST | `assets/{referenceId:guid}/revisions/{revisionId:guid}/restore` | `Restore` | `RestoreAssetRevisionCommand` | 67 |

### `GameGuild.Assets/Controllers/AssetsAdminController.cs` (15 endpoints)

`v{version}/admin/assets` · class `[Authorize(Policy = "RequireAdminRole")]` (L16) · tests `Assets.UnitTests`. Moderation queue read L50, report review L78, force-delete L108, GC L247-248, retention L226, undeletable marks L270-288, moderation review L313. **HOLE?** none; the `:review-moderation` action is the human leg of the asset-moderation state machine.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `statistics` | `GetStatistics` | `GetAssetStatisticsQuery` | 26 |
| GET | `statistics:export` | `ExportStatistics` | `ExportAssetStatisticsQuery` | 37 |
| GET | `moderation-queue` | `GetModerationQueue` | `query` | 50 |
| GET | `{id:guid}/reports` | `GetAssetReports` | `query` | 64 |
| POST | `reports/{reportId:guid}:review` | `ReviewReport` | `command` | 78 |
| POST | `{id:guid}:force-delete` | `ForceDeleteAsset` | `command` | 108 |
| GET | `*(root)*` | `ListAdminAssets` | svc `contentRepository.GetPendingVirusScanAsync` | 134 |
| GET | `gc-candidates` | `GetGarbageCollectionCandidates` | svc `contentRepository.GetGarbageCollectionCandidatesAsync` | 179 |
| POST | `{contentId:guid}:run-virus-scan` | `UpdateVirusScanStatus` | `UpdateAssetVirusScanStatusCommand` | 205 |
| GET | `retention` | `GetRetentionReport` | `GetAssetRetentionReportQuery` | 226 |
| POST | `:run-gc` | `TriggerGarbageCollection` | see notes | 247 |
| POST | `retention:run` | `TriggerGarbageCollection` | `RunAssetRetentionCommand` | 248 |
| POST | `{contentId:guid}:mark-undeletable` | `MarkAsNonDeletable` | `SetAssetLegalHoldCommand` | 270 |
| POST | `{contentId:guid}:unmark-undeletable` | `RemoveNonDeletable` | `SetAssetLegalHoldCommand` | 288 |
| POST | `{contentId:guid}:review-moderation` | `ReviewContentModeration` | `ReviewAssetContentModerationCommand` | 313 |

### `GameGuild.Assets/Controllers/AssetsCdnController.cs` (3 endpoints)

`assets` (root-less CDN paths `/assets/{referenceId}/{token}`, `/e/{token}`, `/t/{transformation}/...`) · class `[AllowAnonymous]` (L21) — token-based authorization, not session · service-injected `ISecureAssetDeliveryService` (CQRS-bypass entry) · rate check via `IRateLimitService.CheckAssetAccessRateAsync`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `{referenceId:guid}/{token}` | `GetAssetWithPathToken` | svc `_accessService.ValidateAccessTokenAsync` | 57 |
| GET | `/e/{token}` | `GetEphemeralAsset` | svc `_accessService.ValidateEphemeralTokenAsync` | 111 |
| GET | `/t/{transformation}/{referenceId:guid}/{token}` | `GetTransformedAsset` | svc `_accessService.ValidateAccessTokenAsync` | 172 |

### `GameGuild.Assets/Controllers/AssetsController.cs` (20 endpoints)

`v{version}/assets` · class `[Authorize]` (L16); `[AllowAnonymous]` on the four signed-delivery reads L320,429,500,559 · upload `POST /` 100 MB `RequestSizeLimit` · 12 validators on the command records (`Assets:/Commands/*`) · tests `Assets.UnitTests` + `Assets.IntegrationTests`. **HOLE?** none; upload → `UploadAssetCommand` → virus-scan Pending + Moderation Pending (see flow F5).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `*(root)*` | `Upload` | `command` | 29 |
| POST | `bulk-upload` | `BulkUpload` | `BulkUploadAssetsCommand` | 87 |
| POST | `chunked-uploads` | `InitiateChunkedUpload` | `InitiateChunkedAssetUploadCommand` | 158 |
| POST | `chunked-uploads/{uploadId}/parts` | `UploadChunk` | `UploadAssetChunkCommand` | 196 |
| POST | `chunked-uploads/{uploadId}:complete` | `CompleteChunkedUpload` | `CompleteChunkedAssetUploadCommand` | 241 |
| DELETE | `chunked-uploads/{uploadId}` | `AbortChunkedUpload` | `AbortChunkedAssetUploadCommand` | 298 |
| GET | `{id:guid}` | `GetAsset` | `query` | 319 |
| GET | `{id:guid}/preview` | `GetPreview` | `GetAssetPreviewQuery` | 345 |
| GET | `{id:guid}/extracted-text", Name = "GetAssetExtractedText` | `GetExtractedText` | `GetAssetQuery` | 371 |
| POST | `{id:guid}:generate-access-url` | `GenerateAccessUrl` | `command` | 428 |
| POST | `bulk-download` | `BulkDownload` | `BulkGenerateAssetAccessUrlsQuery` | 473 |
| GET | `{id:guid}/content` | `GetContent` | svc `referenceRepository.GetByIdWithContentAsync` | 499 |
| GET | `{id:guid}:extracted-text", Name = "GetSignedAssetExtractedText` | `GetExtractedText` | svc `referenceRepository.GetByIdWithContentAsync` | 558 |
| PATCH | `{id:guid}` | `UpdateAsset` | `command` | 600 |
| POST | `{id:guid}:rebind-parent` | `RebindParent` | `command` | 630 |
| DELETE | `{id:guid}` | `DeleteAsset` | `command` | 661 |
| POST | `bulk-delete` | `BulkDelete` | `BulkDeleteAssetsCommand` | 686 |
| POST | `{id:guid}:report` | `ReportAsset` | `command` | 714 |
| GET | `*(root)*` | `ListAssets` | `myAssetsQuery` | 746 |
| GET | `search` | `Search` | `SearchAssetsQuery` | 805 |

### `GameGuild.Assets/Security/SecureAssetDeliveryController.cs` (2 endpoints)

`api/assets` · `[AllowAnonymous]` on both actions L69,277 — ephemeral-token + access-url validation is the auth. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `{assetId:guid}/content` | `GetContent` | svc `_rateLimitService.CheckAssetAccessRateAsync` | 68 |
| POST | `{assetId:guid}/access-url` | `GetAccessUrl` | svc `_rateLimitService.CheckAssetAccessRateAsync` | 276 |

### `GameGuild.Features/Controllers/CapabilitiesController.cs` (6 endpoints)

`v{version}/tenants/{tenantId}/capabilities` · class `[Authorize]` (L16) · 0 validators · tests `Features.UnitTests`. **HOLE?** tenant-admin checks live in `CapabilityService`; `POST sync` mirrors plan→capabilities.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetCapabilities` | svc `_capabilityService.GetTenantCapabilitiesAsync` | 54 |
| GET | `{capability}` | `CheckCapability` | svc `_capabilityService.IsCapabilityEnabledAsync` | 75 |
| POST | `*(root)*` | `SetCapabilityOverride` | `SetCapabilityOverrideCommand` | 96 |
| DELETE | `{capability}` | `RemoveCapabilityOverride` | `RemoveCapabilityOverrideCommand` | 132 |
| POST | `sync` | `SyncFromPlan` | `SyncCapabilitiesFromPlanCommand` | 160 |
| GET | `audit-log` | `GetAuditLog` | svc `_capabilityService.GetAuditLogAsync` | 182 |

### `GameGuild.Features/Controllers/FeatureFlagsController.cs` (4 endpoints)

`v{version}/features` · class `[Authorize]` (L16) · 0 validators · tests `Features.UnitTests`. Evaluate + evaluate-bulk + key value + enabled list. **HOLE?** none; bulk evaluate throttles in service.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `:evaluate` | `EvaluateFeature` | `EvaluateFeatureOperationCommand` | 26 |
| GET | `{key}/value` | `GetFeatureValue` | svc `evaluationService.GetValueAsync` | 47 |
| POST | `:evaluate-bulk` | `BulkEvaluateFeatures` | `BulkEvaluateFeatureOperationCommand` | 73 |
| GET | `enabled` | `GetEnabled` | svc `evaluationService.GetEnabledFeaturesAsync` | 94 |

### `GameGuild.Features/Controllers/FeaturesController.cs` (9 endpoints)

`v{version}/features` · class `[Authorize]` (L27); reads need `Policies.FeaturesRead`, mutations `Policies.FeaturesManage` (L32-66+) · 0 validators · tests `Features.UnitTests`. **HOLE?** none. The six commented-out CRUD routes at L148-286 are dead code, not dead endpoints.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetAll` | `query` | 31 |
| GET | `{key}", Name = "GetFeatureByKey` | `GetByKey` | `GetFeatureFlagByKeyQuery` | 43 |
| GET | `{key}/exists` | `CheckExists` | `FeatureFlagExistsQuery` | 54 |
| POST | `*(root)*` | `Create` | `CreateFeatureFlagCommand` | 65 |
| PUT | `{key}` | `Update` | `GetFeatureFlagByKeyQuery` | 78 |
| DELETE | `{key}` | `Delete` | `GetFeatureFlagByKeyQuery` | 94 |
| POST | `{id:guid}:enable` | `Enable` | `EnableFeatureFlagCommand` | 110 |
| POST | `{id:guid}:disable` | `Disable` | `DisableFeatureFlagCommand` | 121 |
| POST | `{id:guid}:toggle` | `Toggle` | `ToggleFeatureFlagCommand` | 132 |

### `GameGuild.GameJams/GameJamsModuleApi.cs` (9 endpoints)

`api/game-jams` · class `[Authorize]` (L350) · 0 validators · tests `GameJams.UnitTests`. Handler registrations are manual AND redundantly double-covered by host scanning (`GameJamsModuleApi.cs:461-529`). **HOLE?** wiring none; behavioral — `SetJamStatusCommand` accepts any target status with no from-state guard (`:215-227`) and `SubmitJamEntryCommand` takes a body `UserId` with no jam-status check (`:381`, `:229-249`) (gaps N6/N5).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `List` | see notes | 353 |
| GET | `{id:guid}` | `Get` | `GetJamQuery` | 357 |
| POST | `*(root)*` | `Create` | see notes | 364 |
| POST | `{id:guid}/status/{status}` | `SetStatus` | `SetJamStatusCommand` | 368 |
| GET | `{id:guid}/submissions` | `GetSubmissions` | svc ` service.GetSubmissionsAsync` | 375 |
| POST | `{id:guid}/submissions` | `Submit` | see notes | 379 |
| GET | `{id:guid}/criteria` | `GetCriteria` | svc ` service.GetCriteriaAsync` | 383 |
| POST | `{id:guid}/criteria` | `AddCriteria` | see notes | 387 |
| POST | `submissions/{submissionId:guid}/scores` | `Score` | see notes | 391 |

### `GameGuild.Projects/Controllers/ProjectPermissionController.cs` (7 endpoints)

`v{version}/projects/{projectId}/permissions` · class `[Authorize]` (L14) · service-injected (CQRS-bypass entry `IProjectPermissionService`) · tests `Projects.UnitTests`. Collaborator CRUD + role templates (static list, L142-160) + `:share-with-role`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `my-permissions` | `GetMyProjectPermissions` | svc `_permissionResolver.GetEffectivePermissionsAsync` | 34 |
| GET | `collaborators` | `GetProjectCollaborators` | svc `_resourcePermissionService.GetResourceUsersAsync` | 46 |
| POST | `collaborators` | `AddCollaborator` | `AddProjectPermissionCollaboratorCommand` | 71 |
| PUT | `collaborators/{collaboratorUserId}` | `UpdateCollaboratorPermissions` | `UpdateProjectPermissionCollaboratorCommand` | 97 |
| DELETE | `collaborators/{collaboratorUserId}` | `RemoveCollaborator` | `RemoveProjectPermissionCollaboratorCommand` | 126 |
| GET | `role-templates` | `GetProjectRoleTemplates` | see notes | 142 |
| POST | `:share-with-role` | `ShareProjectWithRole` | `ShareProjectWithRoleCommand` | 181 |

### `GameGuild.Projects/Controllers/ProjectStoreProductsController.cs` (4 endpoints)

`v1/projects/{projectId}/store-products` + cross-listing `~/v1/store/products/{productId}/projects` · class `[Authorize]` (L9), `[AllowAnonymous]` on the store cross-listing L40 · tests `Projects.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `List` | `GetProjectStoreProductsQuery` | 13 |
| POST | `*(root)*` | `Link` | `LinkProjectStoreProductCommand` | 22 |
| DELETE | `{productId:guid}` | `Unlink` | `UnlinkProjectStoreProductCommand` | 33 |
| GET | `~/v1/store/products/{productId:guid}/projects` | `ListPublicProductProjects` | `GetPublicStoreProductProjectsQuery` | 41 |

### `GameGuild.Projects/Controllers/ProjectsController.cs` (37 endpoints)

`v{version}/projects` · class `[Authorize]` (L20); `[AllowAnonymous]` on public reads L50,172,339,539 · `Projects:/Validators/ProjectValidators.cs` for create/update · tests `Projects.UnitTests`. Many reads query EF inline (`_context.Set<Project>()` L104+ — CQRS bypass). Version transitions `:ready/:release/:archive` delegate to `TransitionProjectVersion` with `PermissionType.Edit/Publish` + entity methods `MarkReadyForTesting()/Release()` (L297-306). **HOLE?** none wiring; collaboration gaps N14 (see flow F7).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetProjects` | `query` | 49 |
| GET | `mine` | `GetMyProjects` | see notes | 99 |
| GET | `accessible-versions` | `GetAccessibleProjectVersions` | see notes | 133 |
| GET | `{id:guid}` | `GetProject` | `query` | 171 |
| GET | `{id:guid}/versions` | `GetProjectVersions` | svc `_authorizationService.HasPermissionAsync` | 188 |
| POST | `{id:guid}/versions` | `CreateProjectVersion` | `CreateProjectVersionEndpointCommand` | 204 |
| PUT | `{id:guid}/versions/{versionId:guid}` | `UpdateProjectVersion` | `UpdateProjectVersionEndpointCommand` | 254 |
| POST | `{id:guid}/versions/{versionId:guid}:ready` | `MarkProjectVersionReady` | see notes | 297 |
| POST | `{id:guid}/versions/{versionId:guid}:release` | `ReleaseProjectVersion` | see notes | 301 |
| POST | `{id:guid}/versions/{versionId:guid}:archive` | `ArchiveProjectVersion` | `TransitionProjectVersionEndpointCommand` | 305 |
| GET | `slug/{slug}` | `GetProjectBySlug` | `query` | 338 |
| POST | `*(root)*` | `CreateProject` | `command` | 356 |
| POST | `jam/{jamId:guid}` | `CreateJamProject` | `CreateJamProjectCommand` | 392 |
| PUT | `{id:guid}` | `UpdateProject` | `command` | 404 |
| DELETE | `{id:guid}` | `DeleteProject` | `command` | 431 |
| POST | `{id:guid}:publish` | `PublishProject` | `command` | 461 |
| POST | `{id:guid}:unpublish` | `UnpublishProject` | `command` | 473 |
| POST | `{id:guid}:archive` | `ArchiveProject` | `command` | 485 |
| POST | `{id:guid}:restore` | `RestoreProject` | `RestoreProjectEndpointCommand` | 497 |
| GET | `search` | `SearchProjects` | `query` | 538 |
| GET | `popular` | `GetPopularProjects` | `query` | 561 |
| GET | `recent` | `GetRecentProjects` | `query` | 574 |
| GET | `featured` | `GetFeaturedProjects` | `query` | 587 |
| GET | `{id:guid}/statistics` | `GetProjectStatistics` | `query` | 600 |
| GET | `category/{categoryId:guid}` | `GetProjectsByCategory` | `query` | 611 |
| GET | `creator/{creatorId:guid}` | `GetProjectsByCreator` | `query` | 624 |
| GET | `role-templates` | `GetProjectRoleTemplates` | see notes | 637 |
| GET | `my-invitations` | `GetMyProjectInvitations` | see notes | 653 |
| GET | `roles/{roleName}/permissions` | `GetRolePermissions` | see notes | 685 |
| POST | `invitations/{invitationToken}:accept` | `AcceptProjectInvitation` | `AcceptProjectInvitationEndpointCommand` | 705 |
| POST | `invitations/{invitationToken}:decline` | `DeclineProjectInvitation` | `DeclineProjectInvitationEndpointCommand` | 761 |
| GET | `{id:guid}/collaborators` | `GetProjectCollaborators` | svc `_authorizationService.HasPermissionAsync` | 788 |
| POST | `{id:guid}/collaborators` | `AddProjectCollaborator` | `AddProjectCollaboratorEndpointCommand` | 814 |
| PUT | `{id:guid}/collaborators/{collaboratorId:guid}` | `UpdateProjectCollaborator` | `UpdateProjectCollaboratorEndpointCommand` | 876 |
| DELETE | `{id:guid}/collaborators/{collaboratorId:guid}` | `RemoveProjectCollaborator` | `RemoveProjectCollaboratorEndpointCommand` | 921 |
| POST | `{id:guid}:share` | `ShareProject` | `ShareProjectEndpointCommand` | 948 |
| POST | `{id:guid}/invitations` | `InviteProjectCollaborator` | `InviteProjectCollaboratorEndpointCommand` | 1004 |

### `GameGuild.TestingLab/Controllers/TestingAnalyticsController.cs` (2 endpoints)

`v{version}/testing/analytics` · class `[Authorize]` · tests `TestingLab.UnitTests`. Report + export. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetReport` | `GetTestingLabAnalyticsReportQuery` | 14 |
| GET | `export` | `Export` | `ExportTestingLabAnalyticsReportQuery` | 25 |

### `GameGuild.TestingLab/Controllers/TestingEventParticipationController.cs` (14 endpoints)

`v{version}/testing/participation` · class `[Authorize]` · tests `TestingLab.UnitTests`. Registrations, check-in/out, no-show, tested-projects, feedback obligations. **HOLE?** none wiring; check-in has no location/window guard at this level (service-enforced).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `slots/{slotId:guid}/registrations` | `Register` | `RegisterTestingEventSlotCommand` | 13 |
| DELETE | `registrations/{registrationId:guid}` | `Cancel` | `CancelTestingEventSlotRegistrationCommand` | 26 |
| GET | `registrations/me` | `GetMyRegistrations` | `GetMyTestingSlotRegistrationsQuery` | 34 |
| GET | `slots/{slotId:guid}/registrations` | `GetRegistrations` | `GetTestingEventSlotRegistrationsQuery` | 41 |
| GET | `participants` | `GetParticipantDirectory` | `GetTestingParticipantDirectoryQuery` | 50 |
| POST | `registrations/{registrationId:guid}:check-in` | `CheckIn` | `CheckInTestingEventRegistrationCommand` | 60 |
| POST | `registrations/{registrationId:guid}:check-out` | `CheckOut` | `CheckOutTestingEventRegistrationCommand` | 68 |
| POST | `registrations/{registrationId:guid}:no-show` | `MarkNoShow` | `MarkTestingEventNoShowCommand` | 76 |
| POST | `registrations/{registrationId:guid}/tested-projects` | `AssignTestedProject` | `AssignTestingProjectToTesterCommand` | 84 |
| POST | `registrations/{registrationId:guid}:complete` | `Complete` | `CompleteTestingEventParticipationCommand` | 93 |
| GET | `feedback-obligations/me` | `GetMyFeedbackObligations` | `GetMyTestingFeedbackObligationsQuery` | 101 |
| GET | `{eventId:guid}/feedback` | `GetEventFeedback` | `GetTestingEventFeedbackQuery` | 109 |
| POST | `feedback-obligations/{obligationId:guid}/feedback` | `SubmitFeedback` | `SubmitTestingEventFeedbackCommand` | 117 |
| GET | `feedback/me` | `GetMyFeedback` | `GetMyTestingEventFeedbackQuery` | 133 |

### `GameGuild.TestingLab/Controllers/TestingEventTemplatesController.cs` (6 endpoints)

`v{version}/testing/event-templates` · class `[Authorize]` · tests `TestingLab.UnitTests`. Template CRUD + revisions + archive/restore. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetTemplates` | see notes | 18 |
| GET | `{templateId:guid}/revisions/{revisionId:guid}` | `GetRevision` | see notes | 42 |
| POST | `*(root)*` | `CreateTemplate` | `CreateTestingEventTemplateEndpointCommand` | 63 |
| PUT | `{templateId:guid}` | `CreateRevision` | `CreateTestingEventTemplateRevisionEndpointCommand` | 91 |
| POST | `{templateId:guid}:archive` | `ArchiveTemplate` | see notes | 128 |
| POST | `{templateId:guid}:restore` | `RestoreTemplate` | `SetTestingEventTemplateArchivedEndpointCommand` | 134 |

### `GameGuild.TestingLab/Controllers/TestingEventsController.cs` (44 endpoints)

`v{version}/testing/events` · class `[Authorize]` (L10); `[AllowAnonymous]` public reads L31,41 · 4 validators (`Validators/*` — create event/session/feedback, submit request) · tests `TestingLab.UnitTests`. The 44 endpoints cover the full event workflow incl. applications lifecycle, committee, slots, votes. **HOLE?** none — committee voting verifies active committee membership from the actor context, UnderReview state and duplicate votes (`EventsWorkflow/TestingApplicationHandlers.cs:559-600`).

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetEvents` | `GetTestingEventsQuery` | 13 |
| GET | `archived` | `GetArchivedEvents` | `GetArchivedTestingEventsQuery` | 22 |
| GET | `public` | `GetPublicEvents` | `GetPublicTestingEventsQuery` | 30 |
| GET | `public/{eventId:guid}` | `GetPublicEvent` | `GetPublicTestingEventQuery` | 40 |
| GET | `{eventId:guid}` | `GetEvent` | `GetTestingEventQuery` | 49 |
| POST | `*(root)*` | `CreateEvent` | `CreateTestingEventCommand` | 53 |
| PUT | `{eventId:guid}/configuration` | `ConfigureEvent` | `ConfigureTestingEventCommand` | 87 |
| PUT | `{eventId:guid}` | `UpdateEvent` | `UpdateTestingEventCommand` | 101 |
| DELETE | `{eventId:guid}` | `DeleteEvent` | `DeleteTestingEventCommand` | 120 |
| POST | `{eventId:guid}:archive` | `ArchiveEvent` | `ArchiveTestingEventCommand` | 128 |
| POST | `{eventId:guid}:restore` | `RestoreEvent` | `RestoreTestingEventCommand` | 136 |
| POST | `{eventId:guid}:open-applications` | `OpenApplications` | `OpenTestingEventApplicationsCommand` | 144 |
| POST | `{eventId:guid}:close-applications` | `CloseApplications` | `CloseTestingEventApplicationsCommand` | 149 |
| POST | `{eventId:guid}:schedule` | `Schedule` | `ScheduleTestingEventCommand` | 154 |
| POST | `{eventId:guid}:activate` | `Activate` | `ActivateTestingEventCommand` | 159 |
| POST | `{eventId:guid}:complete` | `Complete` | `CompleteTestingEventCommand` | 164 |
| POST | `{eventId:guid}:cancel` | `Cancel` | `CancelTestingEventCommand` | 169 |
| PUT | `{eventId:guid}/learning` | `ConfigureLearning` | `ConfigureTestingEventLearningCommand` | 179 |
| GET | `{eventId:guid}/slots` | `GetSlots` | `GetTestingEventSlotsQuery` | 193 |
| POST | `{eventId:guid}/slots` | `CreateSlot` | `CreateTestingEventSlotCommand` | 200 |
| POST | `{eventId:guid}/slots/batch` | `CreateSlots` | `CreateTestingEventSlotsCommand` | 224 |
| PUT | `{eventId:guid}/slots/{slotId:guid}` | `UpdateSlot` | `UpdateTestingEventSlotCommand` | 248 |
| DELETE | `{eventId:guid}/slots/{slotId:guid}` | `DeleteSlot` | `DeleteTestingEventSlotCommand` | 268 |
| GET | `{eventId:guid}/committee` | `GetCommittee` | `GetTestingEventCommitteeQuery` | 276 |
| POST | `{eventId:guid}/committee` | `AddCommitteeMember` | `AddTestingEventCommitteeMemberCommand` | 285 |
| DELETE | `{eventId:guid}/committee/{userId:guid}` | `RemoveCommitteeMember` | `RemoveTestingEventCommitteeMemberCommand` | 295 |
| POST | `{eventId:guid}/applications` | `SubmitApplication` | `SubmitTestingProjectApplicationCommand` | 308 |
| POST | `{eventId:guid}/applications/drafts` | `CreateApplicationDraft` | `CreateTestingProjectApplicationDraftCommand` | 329 |
| PUT | `applications/{applicationId:guid}/draft` | `SaveApplicationDraft` | `SaveTestingProjectApplicationDraftCommand` | 343 |
| POST | `applications/{applicationId:guid}:submit` | `SubmitApplicationDraft` | `SubmitTestingProjectApplicationDraftCommand` | 358 |
| GET | `{eventId:guid}/applications` | `GetApplications` | `GetTestingEventApplicationsQuery` | 366 |
| GET | `{eventId:guid}/applications/access` | `GetApplicationAccess` | `GetTestingEventApplicationAccessQuery` | 375 |
| GET | `{eventId:guid}/applications/tester-eligibility` | `GetTesterEligibility` | `GetTestingApplicationTesterEligibilityQuery` | 383 |
| GET | `applications/me` | `GetMyApplications` | `GetMyTestingProjectApplicationsQuery` | 393 |
| GET | `applications/{applicationId:guid}` | `GetApplication` | `GetTestingProjectApplicationQuery` | 400 |
| PUT | `applications/{applicationId:guid}` | `UpdateApplication` | `UpdateTestingProjectApplicationCommand` | 406 |
| GET | `applications/{applicationId:guid}/review-package` | `GetApplicationReviewPackage` | `GetTestingApplicationReviewPackageQuery` | 419 |
| POST | `applications/{applicationId:guid}:withdraw` | `WithdrawApplication` | `WithdrawTestingProjectApplicationCommand` | 427 |
| POST | `applications/{applicationId:guid}:review` | `BeginReview` | `BeginReviewTestingProjectApplicationCommand` | 433 |
| POST | `applications/{applicationId:guid}/votes` | `CastVote` | `CastTestingApplicationVoteCommand` | 439 |
| POST | `applications/{applicationId:guid}:approve` | `ApproveApplication` | `ApproveTestingProjectApplicationCommand` | 449 |
| POST | `applications/{applicationId:guid}:reject` | `RejectApplication` | `RejectTestingProjectApplicationCommand` | 466 |
| POST | `applications/{applicationId:guid}:waitlist` | `WaitlistApplication` | `WaitlistTestingProjectApplicationCommand` | 475 |
| PUT | `applications/{applicationId:guid}/slot` | `AssignSlot` | `AssignTestingProjectApplicationSlotCommand` | 484 |

### `GameGuild.TestingLab/Controllers/TestingFeedbackController.cs` (7 endpoints)

`v{version}/testing` · class `[Authorize]` · `SubmitFeedbackCommandValidator` · tests `TestingLab.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `feedback` | `GetFeedbackDirectory` | see notes | 21 |
| POST | `requests/{requestId}/feedback` | `AddFeedback` | `AddTestingFeedbackEndpointCommand` | 34 |
| GET | `requests/{requestId}/feedback` | `GetTestingRequestFeedback` | svc `feedbackService.GetTestingRequestFeedbackAsync` | 55 |
| GET | `feedback/by-user/{userId}` | `GetFeedbackByUser` | svc `feedbackService.GetFeedbackByUserAsync` | 64 |
| POST | `feedback` | `SubmitFeedback` | `SubmitTestingFeedbackEndpointCommand` | 73 |
| POST | `feedback/{feedbackId}/report` | `ReportFeedback` | `ReportTestingFeedbackEndpointCommand` | 93 |
| POST | `feedback/{feedbackId}/quality` | `RateFeedbackQuality` | `RateTestingFeedbackQualityEndpointCommand` | 108 |

### `GameGuild.TestingLab/Controllers/TestingLabPermissionController.cs` (11 endpoints)

`v{version}/testing/permissions` · class `[Authorize]` · service-injected `ITestingLabPermissionService` (CQRS-bypass entry) · tests `TestingLab.UnitTests`. Role templates, user roles, resource grants. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `role-templates` | `GetRoleTemplates` | svc `_permissionService.GetRoleTemplatesAsync` | 60 |
| POST | `role-templates` | `CreateTestingLabRoleTemplate` | `CreateTestingLabRoleTemplateEndpointCommand` | 68 |
| PUT | `role-templates/{idOrName}` | `UpdateTestingLabRoleTemplate` | `UpdateTestingLabRoleTemplateEndpointCommand` | 88 |
| DELETE | `role-templates/{idOrName}` | `DeleteTestingLabRoleTemplate` | `DeleteTestingLabRoleTemplateEndpointCommand` | 109 |
| DELETE | `role-templates/by-name/{name}` | `DeleteTestingLabRoleTemplateByName` | `DeleteTestingLabRoleTemplateEndpointCommand` | 129 |
| GET | `users/{userId}` | `GetUserTestingLabPermissions` | svc `_permissionService.GetUserRolesAsync` | 151 |
| POST | `users/{userId}/roles` | `AssignTestingLabRole` | `AssignTestingLabRoleEndpointCommand` | 212 |
| DELETE | `users/{userId}/roles/{roleName}` | `RevokeTestingLabRole` | `RevokeTestingLabRoleEndpointCommand` | 234 |
| POST | `users/{userId}/resources/{resourceType}/{resourceId}` | `GrantResourcePermission` | `GrantTestingLabResourcePermissionEndpointCommand` | 251 |
| DELETE | `users/{userId}/resources/{resourceType}/{resourceId}` | `RevokeResourcePermission` | `RevokeTestingLabResourcePermissionEndpointCommand` | 268 |
| GET | `users/{userId}/check/{resourceType}` | `CheckTestingLabPermission` | svc `_permissionService.HasPermissionAsync` | 287 |

### `GameGuild.TestingLab/Controllers/TestingLabSettingsController.cs` (5 endpoints)

`v{version}/testing/settings` · class `[Authorize]` · tests `TestingLab.UnitTests`. GET/PUT/PATCH/reset/exists on the settings document. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetSettings` | svc `settingsService.GetTestingLabSettingsDtoAsync` | 21 |
| PUT | `*(root)*` | `CreateOrUpdateSettings` | `CreateOrUpdateTestingLabSettingsEndpointCommand` | 36 |
| PATCH | `*(root)*` | `UpdateSettings` | `UpdateTestingLabSettingsEndpointCommand` | 54 |
| POST | `reset` | `ResetSettings` | `ResetTestingLabSettingsEndpointCommand` | 71 |
| GET | `exists` | `SettingsExist` | svc `settingsService.TestingLabSettingsExistAsync` | 88 |

### `GameGuild.TestingLab/Controllers/TestingLocationsController.cs` (6 endpoints)

`v{version}/testing` · class `[Authorize]` · tests `TestingLab.UnitTests`. Location CRUD + restore. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `locations` | `GetTestingLocations` | svc `locationService.GetTestingLocationsAsync` | 20 |
| GET | `locations/{id}` | `GetTestingLocation` | svc `locationService.GetTestingLocationByIdAsync` | 29 |
| POST | `locations` | `CreateTestingLocation` | `CreateTestingLocationEndpointCommand` | 43 |
| PUT | `locations/{id}` | `UpdateTestingLocation` | `UpdateTestingLocationEndpointCommand` | 58 |
| DELETE | `locations/{id}` | `DeleteTestingLocation` | `DeleteTestingLocationEndpointCommand` | 72 |
| POST | `locations/{id}/restore` | `RestoreTestingLocation` | `RestoreTestingLocationEndpointCommand` | 93 |

### `GameGuild.TestingLab/Controllers/TestingParticipantsController.cs` (12 endpoints)

`v{version}/testing` · class `[Authorize]` · tests `TestingLab.UnitTests`. Request participants + session register/waitlist. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `requests/{requestId}/participants/{userId}` | `AddParticipant` | `AddTestingParticipantEndpointCommand` | 32 |
| DELETE | `requests/{requestId}/participants/{userId}` | `RemoveParticipant` | `RemoveTestingParticipantEndpointCommand` | 49 |
| GET | `requests/{requestId}/participants` | `GetTestingRequestParticipants` | svc `participantService.GetTestingRequestParticipantsAsync` | 63 |
| GET | `requests/{requestId}/participants/{userId}/check` | `CheckUserParticipation` | svc `participantService.IsUserParticipantAsync` | 72 |
| POST | `sessions/{sessionId}/register` | `RegisterForSession` | see notes | 85 |
| DELETE | `sessions/{sessionId}/register` | `UnregisterFromSession` | `UnregisterTestingSessionEndpointCommand` | 99 |
| GET | `sessions/{sessionId}/registrations` | `GetSessionRegistrations` | svc `participantService.GetSessionRegistrationsAsync` | 117 |
| POST | `sessions/{sessionId}/waitlist` | `AddToWaitlist` | see notes | 130 |
| DELETE | `sessions/{sessionId}/waitlist` | `RemoveFromWaitlist` | `RemoveTestingSessionWaitlistEndpointCommand` | 144 |
| GET | `sessions/{sessionId}/waitlist` | `GetSessionWaitlist` | svc `participantService.GetSessionWaitlistAsync` | 162 |
| GET | `users/{userId}/activity` | `GetUserTestingActivity` | svc `participantService.GetUserTestingActivityAsync` | 175 |
| GET | `attendance/students` | `GetStudentAttendanceReport` | svc `participantService.GetStudentAttendanceReportAsync` | 184 |

### `GameGuild.TestingLab/Controllers/TestingRequestsController.cs` (15 endpoints)

`v{version}/testing` · class `[Authorize]` (L16) · `CreateTestingRequestCommandValidator` · tests `TestingLab.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `requests` | `GetTestingRequests` | svc `requestService.GetTestingRequestsAsync` | 24 |
| GET | `requests/{id}` | `GetTestingRequest` | `GetTestingRequestDetailQuery` | 36 |
| GET | `requests/{id}/details` | `GetTestingRequestWithDetails` | svc `requestService.GetTestingRequestByIdWithDetailsAsync` | 44 |
| POST | `requests` | `CreateTestingRequest` | `CreateTestingRequestEndpointCommand` | 58 |
| PUT | `requests/{id}` | `UpdateTestingRequest` | `UpdateTestingRequestEndpointCommand` | 97 |
| DELETE | `requests/{id}` | `DeleteTestingRequest` | `DeleteTestingRequestEndpointCommand` | 131 |
| POST | `requests/{id}:restore` | `RestoreTestingRequest` | `RestoreTestingRequestEndpointCommand` | 145 |
| GET | `requests/by-project-version/{projectVersionId}` | `GetTestingRequestsByProjectVersion` | svc `requestService.GetTestingRequestsByProjectVersionAsync` | 172 |
| GET | `requests/by-creator/{creatorId}` | `GetTestingRequestsByCreator` | svc `requestService.GetTestingRequestsByCreatorAsync` | 181 |
| GET | `requests/by-status/{status}` | `GetTestingRequestsByStatus` | svc `requestService.GetTestingRequestsByStatusAsync` | 190 |
| GET | `requests/search` | `SearchTestingRequests` | svc `requestService.SearchTestingRequestsAsync` | 199 |
| POST | `submit-simple` | `SubmitSimpleTestingRequest` | `CreateSimpleTestingRequestEndpointCommand` | 213 |
| GET | `my-requests` | `GetMyTestingRequests` | svc `requestService.GetTestingRequestsByCreatorAsync` | 262 |
| GET | `available-for-testing` | `GetAvailableTestingRequests` | svc `requestService.GetActiveTestingRequestsAsync` | 277 |
| GET | `requests/{requestId}/statistics` | `GetTestingRequestStatistics` | svc `feedbackService.GetTestingRequestStatisticsAsync` | 286 |

### `GameGuild.TestingLab/Controllers/TestingSessionsController.cs` (19 endpoints)

`v{version}/testing` · class `[Authorize]` (L16); `[AllowAnonymous]` on public sessions L136 · `CreateTestingSessionCommandValidator` · tests `TestingLab.UnitTests`. Session CRUD + attendance + project links. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `sessions` | `GetTestingSessions` | svc `sessionService.GetTestingSessionsAsync` | 24 |
| GET | `sessions/{id}` | `GetTestingSession` | svc `sessionService.GetTestingSessionByIdAsync` | 33 |
| GET | `sessions/{id}/details` | `GetTestingSessionWithDetails` | svc `sessionService.GetTestingSessionByIdWithDetailsAsync` | 47 |
| POST | `sessions` | `CreateTestingSession` | `CreateTestingSessionEndpointCommand` | 61 |
| PUT | `sessions/{id}` | `UpdateTestingSession` | `UpdateTestingSessionEndpointCommand` | 82 |
| DELETE | `sessions/{id}` | `DeleteTestingSession` | `DeleteTestingSessionEndpointCommand` | 104 |
| POST | `sessions/{id}:restore` | `RestoreTestingSession` | `RestoreTestingSessionEndpointCommand` | 118 |
| GET | `public/sessions` | `GetPublicTestingSessions` | svc `sessionService.GetPublicTestingSessionsAsync` | 135 |
| GET | `sessions/by-request/{testingRequestId}` | `GetTestingSessionsByRequest` | svc `sessionService.GetTestingSessionsByRequestAsync` | 144 |
| GET | `sessions/by-location/{locationId}` | `GetTestingSessionsByLocation` | svc `sessionService.GetTestingSessionsByLocationAsync` | 153 |
| GET | `sessions/by-status/{status}` | `GetTestingSessionsByStatus` | svc `sessionService.GetTestingSessionsByStatusAsync` | 162 |
| GET | `sessions/by-manager/{managerId}` | `GetTestingSessionsByManager` | svc `sessionService.GetTestingSessionsByManagerAsync` | 171 |
| GET | `sessions/search` | `SearchTestingSessions` | svc `sessionService.SearchTestingSessionsAsync` | 180 |
| GET | `sessions/{sessionId}/statistics` | `GetTestingSessionStatistics` | svc `sessionService.GetTestingSessionStatisticsAsync` | 194 |
| GET | `attendance/sessions` | `GetSessionAttendanceReport` | svc `sessionService.GetSessionAttendanceReportAsync` | 203 |
| POST | `sessions/{sessionId}/attendance` | `UpdateAttendance` | `UpdateTestingSessionAttendanceEndpointCommand` | 212 |
| GET | `sessions/{sessionId:guid}/projects` | `GetSessionProjects` | `GetSessionProjectLinksQuery` | 231 |
| POST | `sessions/{sessionId:guid}/projects` | `LinkSessionProject` | `LinkSessionProjectCommand` | 241 |
| DELETE | `sessions/{sessionId:guid}/projects/{projectId:guid}` | `UnlinkSessionProject` | `UnlinkSessionProjectCommand` | 258 |

### `GameGuild.LaunchPad/LaunchPadController.cs` (6 endpoints)

`v1/launch-pad` · class `[Authorize]` (L9) · 0 validators · tests `LaunchPad.UnitTests`. Launch plan CRUD + checklist + publish. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetDashboard` | `GetLaunchPadDashboardQuery` | 13 |
| GET | `{id:guid}` | `GetLaunchPlan` | `GetLaunchPlanQuery` | 22 |
| GET | `projects/{projectId:guid}` | `GetProjectLaunchPlan` | `GetLaunchPlanByProjectQuery` | 34 |
| POST | `*(root)*` | `CreateLaunchPlan` | `RejectLegacyDirectPlanCreationCommand` | 46 |
| POST | `{id:guid}/checklist/{itemId:guid}:complete` | `CompleteChecklistItem` | `CompleteLaunchChecklistItemCommand` | 62 |
| POST | `{id:guid}:publish` | `PublishLaunch` | `PublishLaunchCommand` | 77 |

### `GameGuild.LaunchPad/LaunchPadEventsController.cs` (22 endpoints)

`v1/launch-pad/events` · class `[Authorize]` (L12); `[AllowAnonymous]` public reads L21,44 · 0 validators · tests `LaunchPad.UnitTests`. 22 endpoints: events, slots, applications (submit/review/withdraw), registrations (cancel/transition), analytics. **HOLE?** none wiring; application review trusts `CanReviewApplicationsAsync` service checks.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `public` | `GetPublicEvents` | see notes | 22 |
| GET | `public/{id:guid}` | `GetPublicEvent` | see notes | 45 |
| GET | `management` | `GetManagedEvents` | svc `authorization.CanManageEventsAsync` | 66 |
| GET | `{id:guid}/management` | `GetManagedEvent` | svc `authorization.CanManageEventsAsync` | 88 |
| POST | `*(root)*` | `CreateEvent` | `CreateLaunchPadEventEndpointCommand` | 109 |
| POST | `{id:guid}:transition` | `TransitionEvent` | `TransitionLaunchPadEventEndpointCommand` | 141 |
| PUT | `{id:guid}` | `UpdateEvent` | `UpdateLaunchPadEventEndpointCommand` | 177 |
| POST | `{eventId:guid}/slots` | `CreateSlot` | `CreateLaunchPadSlotEndpointCommand` | 210 |
| PUT | `slots/{slotId:guid}` | `UpdateSlot` | `UpdateLaunchPadSlotEndpointCommand` | 244 |
| DELETE | `slots/{slotId:guid}` | `DeleteSlot` | `DeleteLaunchPadSlotEndpointCommand` | 285 |
| POST | `{eventId:guid}/applications` | `SubmitApplication` | `SubmitLaunchPadApplicationEndpointCommand` | 310 |
| GET | `applications/me` | `GetMyApplications` | svc `authorization.CanSubmitProjectAsync` | 377 |
| PUT | `applications/{applicationId:guid}` | `UpdateApplication` | `UpdateLaunchPadApplicationEndpointCommand` | 406 |
| POST | `applications/{applicationId:guid}:withdraw` | `WithdrawApplication` | `WithdrawLaunchPadApplicationEndpointCommand` | 473 |
| GET | `{eventId:guid}/applications/management` | `GetEventApplications` | svc `authorization.CanReviewApplicationsAsync` | 497 |
| POST | `applications/{applicationId:guid}:review` | `ReviewApplication` | `ReviewLaunchPadApplicationEndpointCommand` | 519 |
| POST | `slots/{slotId:guid}/registrations` | `Register` | `RegisterLaunchPadParticipantEndpointCommand` | 562 |
| GET | `registrations/me` | `GetMyRegistrations` | see notes | 593 |
| POST | `registrations/{registrationId:guid}:cancel` | `CancelRegistration` | `CancelLaunchPadRegistrationEndpointCommand` | 611 |
| POST | `registrations/{registrationId:guid}:transition` | `TransitionRegistration` | `TransitionLaunchPadRegistrationEndpointCommand` | 638 |
| GET | `{eventId:guid}/registrations/management` | `GetEventRegistrations` | svc `authorization.CanManageParticipantsAsync` | 676 |
| GET | `analytics` | `GetAnalytics` | svc `authorization.CanViewAnalyticsAsync` | 701 |

### `GameGuild.LaunchPad/LaunchPadSettingsController.cs` (2 endpoints)

`v1/launch-pad/settings` · class `[Authorize]` (L10) · 0 validators · tests `LaunchPad.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `*(root)*` | `GetSettings` | `CreateDefaultLaunchPadSettingsCommand` | 18 |
| PUT | `*(root)*` | `UpdateSettings` | `UpdateLaunchPadSettingsEndpointCommand` | 40 |

### `GameGuild.Monitoring.SLA/Controllers/SlaMonitoringController.cs` (10 endpoints)

`api/v{version}/sla` · class `[Authorize]` (L17) · 11 validators on every command/query (`Commands/*`, `Queries/*`) — the only fully-validated controller set in scope · tests `Monitoring.SLA.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| POST | `slos` | `CreateSlo` | `enrichedCommand` | 39 |
| GET | `slos` | `GetSlos` | `query` | 62 |
| GET | `slos/{id:guid}` | `GetSlo` | `query` | 86 |
| PUT | `slos/{id:guid}` | `UpdateSlo` | `enrichedCommand` | 107 |
| DELETE | `slos/{id:guid}` | `DeleteSlo` | `command` | 129 |
| POST | `slis` | `RecordSliMetric` | `enrichedCommand` | 147 |
| GET | `slos/{id:guid}/compliance` | `GetCompliance` | `query` | 168 |
| GET | `slos/{id:guid}/error-budget` | `GetErrorBudget` | `query` | 186 |
| GET | `violations` | `GetViolations` | `query` | 210 |
| POST | `violations/{id:guid}/resolve` | `ResolveViolation` | `enrichedCommand` | 238 |

### `GameGuild.Resources/Controllers/ResourcesController.cs` (4 endpoints)

`resources/usage*` + archive/cleanup · class `[Authorize(Policy = AuthorizationPolicies.SystemAdmin)]` (L20) + `[EnableRateLimiting(RateLimitPolicies.Internal)]` (L21) — the only SystemAdmin+rate-limited controller in scope · 16 validators across `Commands/*`,`Queries/*` · tests `Resources.UnitTests` + `Resources.IntegrationTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/resources/usage` | `GetUsage` | `GetResourceUsageByTypeQuery` | 34 |
| GET | `v{version:apiVersion}/resources/usage-trends` | `GetUsageTrends` | `GetResourceUsageTrendsQuery` | 53 |
| POST | `v{version:apiVersion}/resources:archive` | `Archive` | `ArchiveResourceUsageRecordsCommand` | 78 |
| POST | `v{version:apiVersion}/resources:cleanup` | `Cleanup` | `CleanupOrphanedResourcesCommand` | 98 |

### `GameGuild.Resources/Controllers/TenantQuotasController.cs` (7 endpoints)

`tenants/{tenantId}/quotas*` · class `[Authorize]` (L22) + `[EnableRateLimiting(PerTenant)]` (L23) · validators on mutations · tests `Resources.UnitTests`. **HOLE?** none; tenant match enforced inline.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/quotas` | `GetTenantQuotas` | `query` | 79 |
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/quotas/{type}` | `GetQuota` | `query` | 105 |
| PUT | `v{version:apiVersion}/tenants/{tenantId:guid}/quotas/{type}` | `SetQuota` | `command` | 141 |
| DELETE | `v{version:apiVersion}/tenants/{tenantId:guid}/quotas/{type}` | `DeleteQuota` | `command` | 169 |
| POST | `v{version:apiVersion}/tenants/{tenantId:guid}/quotas/{type}:reset` | `ResetQuota` | `command` | 199 |
| POST | `v{version:apiVersion}/tenants/{tenantId:guid}/quotas/{type}:toggle` | `ToggleQuota` | `command` | 226 |
| POST | `v{version:apiVersion}/tenants/{tenantId:guid}/quotas/{type}:check` | `CheckQuota` | `query` | 256 |

### `GameGuild.Resources/Controllers/TenantResourceMetadataController.cs` (4 endpoints)

`tenants/{tenantId}/resources/metadata*` · `[Authorize]` + PerTenant rate limit · tests `Resources.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/metadata` | `GetTenantMetadata` | svc `metadataRepository.GetByCategoryAsync` | 69 |
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/metadata/{key}` | `GetTenantMetadataByKey` | svc `metadataRepository.GetByKeyAsync` | 93 |
| PUT | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/metadata/{key}` | `SetTenantMetadata` | `SetTenantResourceMetadataCommand` | 124 |
| DELETE | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/metadata/{key}` | `DeleteTenantMetadata` | `DeleteTenantResourceMetadataCommand` | 151 |

### `GameGuild.Resources/Controllers/TenantResourceSettingsController.cs` (5 endpoints)

`tenants/{tenantId}/resources/settings*` · `[Authorize]` + PerTenant rate limit · tests `Resources.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings` | `GetTenantSettings` | svc `settingsRepository.GetByCategoryAsync` | 69 |
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings/{key}` | `GetTenantSettingByKey` | svc `settingsRepository.GetByKeyAsync` | 93 |
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings/{key}/effective` | `GetEffectiveValue` | svc `settingsRepository.GetEffectiveValueAsync` | 124 |
| PUT | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings/{key}` | `SetTenantSetting` | `SetTenantResourceSettingCommand` | 155 |
| DELETE | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/settings/{key}` | `DeleteTenantSetting` | `DeleteTenantResourceSettingCommand` | 182 |

### `GameGuild.Resources/Controllers/TenantResourcesController.cs` (6 endpoints)

`tenants/{tenantId}/resources*` · `[Authorize]` + PerTenant rate limit · validators on record/reset · tests `Resources.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/usage-records` | `GetUsageRecords` | `GetResourceUsageRecordsQuery` | 74 |
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/usage-summary` | `GetCurrentUsageSummary` | `GetCurrentResourceUsageSummaryQuery` | 103 |
| GET | `v{version:apiVersion}/tenants/{tenantId:guid}/resources/limits` | `CheckLimits` | `CheckResourceUsageLimitsQuery` | 126 |
| POST | `v{version:apiVersion}/tenants/{tenantId:guid}/resources:record` | `Record` | `RecordResourceUsageCommand` | 153 |
| POST | `v{version:apiVersion}/tenants/{tenantId:guid}/resources:record-with-quota-check` | `RecordWithQuotaCheck` | `RecordResourceUsageCommand` | 181 |
| POST | `v{version:apiVersion}/tenants/{tenantId:guid}/resources:reset` | `Reset` | `ResetResourceUsageCommand` | 245 |

### `GameGuild.Resources/Controllers/UserQuotasController.cs` (7 endpoints)

`users/{userId}/quotas*` · class `[Authorize]` (L20) + `[EnableRateLimiting(PerUser)]` (L21) · validators on mutations · tests `Resources.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/users/{userId:guid}/quotas` | `GetUserQuotas` | `query` | 57 |
| GET | `v{version:apiVersion}/users/{userId:guid}/quotas/{type}` | `GetQuota` | `query` | 83 |
| PUT | `v{version:apiVersion}/users/{userId:guid}/quotas/{type}` | `SetQuota` | `command` | 119 |
| DELETE | `v{version:apiVersion}/users/{userId:guid}/quotas/{type}` | `DeleteQuota` | `command` | 147 |
| POST | `v{version:apiVersion}/users/{userId:guid}/quotas/{type}:reset` | `ResetQuota` | `command` | 177 |
| POST | `v{version:apiVersion}/users/{userId:guid}/quotas/{type}:toggle` | `ToggleQuota` | `command` | 204 |
| POST | `v{version:apiVersion}/users/{userId:guid}/quotas/{type}:check` | `CheckQuota` | `query` | 234 |

### `GameGuild.Resources/Controllers/UserResourceMetadataController.cs` (3 endpoints)

`users/{userId}/resources/metadata*` · `[Authorize]` + PerUser rate limit · tests `Resources.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/users/{userId:guid}/resources/metadata` | `GetUserMetadata` | svc `metadataRepository.GetByUserAsync` | 57 |
| GET | `v{version:apiVersion}/users/{userId:guid}/resources/metadata/{key}` | `GetUserMetadataByKey` | svc `metadataRepository.GetByUserKeyAsync` | 79 |
| PUT | `v{version:apiVersion}/users/{userId:guid}/resources/metadata/{key}` | `SetUserMetadata` | `SetUserResourceMetadataCommand` | 110 |

### `GameGuild.Resources/Controllers/UserResourceSettingsController.cs` (3 endpoints)

`users/{userId}/resources/settings*` · `[Authorize]` + PerUser rate limit · tests `Resources.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/users/{userId:guid}/resources/settings` | `GetUserSettings` | svc `settingsRepository.GetByUserAsync` | 57 |
| GET | `v{version:apiVersion}/users/{userId:guid}/resources/settings/{key}` | `GetUserSettingByKey` | svc `settingsRepository.GetByUserKeyAsync` | 79 |
| PUT | `v{version:apiVersion}/users/{userId:guid}/resources/settings/{key}` | `SetUserSetting` | `SetUserResourceSettingCommand` | 110 |

### `GameGuild.Resources/Controllers/UserResourcesController.cs` (6 endpoints)

`users/{userId}/resources*` · `[Authorize]` + PerUser rate limit · validators on record/reset · tests `Resources.UnitTests`. **HOLE?** none.

| Verb | Route | Action | Dispatch | L |
|---|---|---|---|---|
| GET | `v{version:apiVersion}/users/{userId:guid}/resources/usage-records` | `GetUsageRecords` | `GetUserResourceUsageRecordsQuery` | 61 |
| GET | `v{version:apiVersion}/users/{userId:guid}/resources/usage-summary` | `GetCurrentUsageSummary` | `GetCurrentUserResourceUsageSummaryQuery` | 83 |
| GET | `v{version:apiVersion}/users/{userId:guid}/resources/limits` | `CheckLimits` | `CheckUserResourceUsageLimitsQuery` | 106 |
| POST | `v{version:apiVersion}/users/{userId:guid}/resources:record` | `Record` | `RecordUserResourceUsageCommand` | 133 |
| POST | `v{version:apiVersion}/users/{userId:guid}/resources:record-with-quota-check` | `RecordWithQuotaCheck` | `CheckUserResourceQuotaQuery` | 161 |
| POST | `v{version:apiVersion}/users/{userId:guid}/resources:reset` | `Reset` | `ResetUserResourceUsageCommand` | 196 |


## Service-only modules (no endpoints)

| Module | Role | Evidence |
|---|---|---|
| `Social.Announcements` | handles `AnnouncePublicationCommand` — creates the community post + notifies followers (fan-out cap 50) | `Announcements:/Services/PublicationAnnouncerHandler.cs:16-68`, registered `AnnouncementsModule.cs:27-29` |
| `ProjectWork` | Kanban board/task entities + `ProjectWorkService` consumed by Projects surfaces | `ProjectWork:/ProjectWorkModule.cs` |
| `Localization` | language repo, translation workflow, localized errors, content sanitizer | `Localization:/Extensions/LocalizationModuleExtensions.cs` |
| `Teams` | `Team`/`TeamMember`/`TeamInvitation` entities + `TeamAuthorizationService` | `Teams:/TeamsModule.cs` |
| `Tags` | `Tag`/`TagProficiency`/`TagRelationship` entities consumed by Posts/Profiles tag surfaces | `Tags:/Configuration/TagsModelConfiguration.cs` |
