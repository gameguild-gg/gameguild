# CQRS Bypass — Known Architecture Debt

> **Status**: Known debt — tracked for incremental migration  
> **Created**: 2026-02-06  
> **Priority**: Low (P3) — functional, not a correctness issue

## Context

The codebase uses CQRS (Command Query Responsibility Segregation) via `ISender` for dispatching commands and queries. The majority of controllers (92/112) follow this pattern. However, **20 controllers** still inject service interfaces directly, bypassing the CQRS pipeline.

This is **not a bug** — these controllers work correctly. The debt is that they skip the CQRS pipeline behaviors (validation, logging, performance measurement) and cannot benefit from future pipeline additions without code changes.

## Affected Controllers (20)

| # | Controller | Module | Injected Service(s) |
|---|-----------|--------|---------------------|
| 1 | `AssetsCdnController` | Assets | `ISecureAssetDeliveryService` |
| 2 | `CapabilitiesController` | Learning.Capabilities | `ICapabilityService` |
| 3 | `AchievementsController` | Learning.Achievements | `IAchievementService` |
| 4 | `KeyRotationController` | Identity.Authentication | `IKeyRotationService` |
| 5 | `AssessmentsController` | Learning.Assessments | `IAssessmentService` |
| 6 | `CertificatesController` | Learning.Certificates | `ICertificateService` |
| 7 | `CohortsController` | Learning.Cohorts | `ICohortService` |
| 8 | `PrerequisitesController` | Learning.Prerequisites | `IPrerequisiteService` |
| 9 | `DiscussionsController` | Social.Discussions | `IDiscussionService` |
| 10 | `FeedController` | Social.Feed | `IFeedService` |
| 11 | `LikesController` | Social.Likes | `ILikeService` |
| 12 | `RepliesController` | Social.Replies | `IReplyService` |
| 13 | `ReviewsController` | Social.Reviews | `IReviewService` |
| 14 | `WishlistsController` | Commerce.Wishlists | `IWishlistService` |
| 15 | `NotificationsController` | Communication.Notifications | `INotificationService` |
| 16 | `ProjectPermissionController` | Learning.Projects | `IProjectPermissionService` |
| 17 | `VersioningController` | Learning.Versioning | `IVersioningService` |
| 18 | `FollowersController` | Social.Follows | `IFollowService` |
| 19 | `RatingsController` | Social.Ratings | `IRatingService` |
| 20 | `TestingLabPermissionController` | TestingLab | `ITestingLabPermissionService` |

## Migration Strategy

When touching any of these controllers for feature work:

1. Create CQRS command/query records under the module's `Commands/` or `Queries/` folder
2. Create handler classes implementing `ICommandHandler<T>` / `IQueryHandler<T>`
3. Add FluentValidation validators
4. Replace service injection with `ISender` in the controller
5. Service interface can be removed once all consumers migrate

**Do NOT batch-migrate** — migrate opportunistically when a controller needs changes for other reasons.

## Completed Migrations (reference)

- `OrdersController` — migrated from `IOrderService` to CQRS (2026-02-05)
- All other 92 controllers already use `ISender`
