# GameGuild API — Deep Module Analysis

> **Generated:** 2026-02-13
> **Updated:** 2026-06-07
> **Architecture:** .NET 10 Modular Monolith · CQRS · EF Core · REST + GraphQL
> **Total Modules:** 45 (across 10 domains)

---

## Table of Contents

1. [Identity Domain](#1-identity-domain)
   - [1.1 Authentication](#11-gameguildidentityauthentication)
   - [1.2 Authorization](#12-gameguildidentityauthorization)
   - [1.3 Users](#13-gameguildidentityusers)
   - [1.4 Tenants](#14-gameguildidentitytenants)
   - [1.5 Context](#15-gameguildidentitycontext)
2. [Commerce Domain](#2-commerce-domain)
   - [2.1 Products](#21-gameguildcommerceproducts)
   - [2.2 Orders](#22-gameguildcommerceorders)
   - [2.3 Payments](#23-gameguildcommercepayments)
   - [2.4 Subscriptions](#24-gameguildcommercesubscriptions)
   - [2.5 Billing](#25-gameguildcommercebilling)
   - [2.6 Commerce (Shared)](#26-gameguildcommerce-shared-kernel)
3. [Learning Domain](#3-learning-domain)
   - [3.1 Courses (Programs)](#31-gameguildlearningcourses)
   - [3.2 Assessments](#32-gameguildlearningassessments)
   - [3.3 Certificates](#33-gameguildlearningcertificates)
   - [3.4 Cohorts](#34-gameguildlearningcohorts)
   - [3.5 Enrollments](#35-gameguildlearningenrollments)
   - [3.6 Learning (Shared)](#36-gameguildlearning-shared)
4. [Learning Experience Domain](#4-learning-experience-domain)
   - [4.1 Discovery](#41-gameguildlearningexperiencediscovery)
   - [4.2 Learning Paths](#42-gameguildlearningexperiencelearningpaths)
   - [4.3 Recommendations](#43-gameguildlearningexperiencerecommendations)
   - [4.4 Social (Learning)](#44-gameguildlearningexperiencesocial)
5. [Social Domain](#5-social-domain)
   - [5.1 Posts](#51-gameguildsocialposts)
   - [5.2 Follows](#52-gameguildsocialfollows)
   - [5.3 Ratings](#53-gameguildsocialratings)
   - [5.4 Blog](#54-gameguildsocialblog)
   - [5.5 Feed](#55-gameguildsocialfeed)
   - [5.6 Profiles](#56-gameguildsocialprofiles)
   - [5.7 Reactions](#57-gameguildsocialreactions)
6. [Content Domain](#6-content-domain)
   - [6.1 Pages](#61-gameguildcontentpages)
   - [6.2 Resources](#62-gameguildresources)
   - [6.3 Resource Contents](#63-gameguildresourcescontents)
   - [6.4 Assets](#64-gameguildassets)
7. [Gamification Domain](#7-gamification-domain)
   - [7.1 Achievements](#71-gameguildgamificationachievements)
8. [Projects & Testing Domain](#8-projects--testing-domain)
   - [8.1 Projects](#81-gameguildprojects)
   - [8.2 Testing Lab](#82-gameguildtestinglab)
   - [8.3 Game Jams](#83-gameguildgamejams)
9. [Platform Domain](#9-platform-domain)
   - [9.1 Feature Flags](#91-gameguildfeatures)
   - [9.2 Notifications](#92-gameguildnotifications)
   - [9.3 Tags](#93-gameguildtags)
   - [9.4 Localization](#94-gameguildlocalization)
10. [Compliance & Monitoring Domain](#10-compliance--monitoring-domain)
    - [10.1 Audit](#101-gameguildcomplianceaudit)
    - [10.2 KYC](#102-gameguildcompliancekyc)
    - [10.3 FERPA](#103-gameguildcomplianceferpa)
    - [10.4 SLA Monitoring](#104-gameguildmonitoringsla)
11. [Cross-Cutting](#11-cross-cutting)
    - [11.1 SharedKernel](#111-gameguildsharedkernel)

---

## 1. Identity Domain

### 1.1 GameGuild.Identity.Authentication

**Maturity:** ★★★★★ Full — Extremely comprehensive authentication system

**Structure:** Commands, Queries, Controllers, Entities, Services, Handlers, Validators, Middleware, DTOs, Events, Repositories, Data, Mappings, Constants, Enums

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Sign Up / Sign In** | Email+password registration, email+password sign-in |
| **OAuth / Social Login** | Google OAuth, GitHub OAuth (authorize + callback flow) |
| **Web3 Authentication** | Wallet challenge generation, cryptographic signature verification |
| **Token Management** | JWT access/refresh tokens, token refresh, token revocation |
| **Email Verification** | Send verification email, verify email token |
| **Password Management** | Password reset request, password reset execution, password change |
| **MFA (Multi-Factor Auth)** | TOTP setup/complete, SMS setup/complete, MFA verification, backup codes, backup code regeneration, list MFA methods, disable MFA |
| **WebAuthn / Passkeys** | Registration begin/complete, authentication begin/complete, credential CRUD (list, get, verify, delete, rename), WebAuthn status |
| **Session Management** | List active sessions, security analysis, revoke session, terminate others, terminate all, refresh session |
| **Trusted Devices** | Device trust management |
| **API Keys** | Create API key, list API keys, revoke API key |
| **Signing Key Rotation** | List signing keys, rotate keys, cleanup expired keys |
| **Roles (RBAC)** | List roles, get role, create role, update role, delete role, get user roles, assign/remove roles |
| **Service Accounts** | CRUD (create, get, update, list, delete), OAuth token endpoint, secret rotation, lock/unlock, audit log, activate/deactivate, scope management |
| **ABAC Policies** | CRUD (create, get, update, delete), list with filtering, evaluate, bulk evaluate, test expressions, activate/deactivate, clone, statistics, usage tracking, audit trail, validate, detect conflicts, templates, instantiate template |
| **Permission Grants** | Tenant-level grants (create, delete, revoke, batch-create, batch-delete), content-type grants, resource-level grants |
| **Permission Evaluation** | Tenant permission check, content-type check, resource check, list user permissions, effective permissions, resolve permission hierarchy |
| **Permission Admin** | Tenant permission analytics, audit trail, cache stats, cache clear, permission templates, apply template |
| **Conditional Access Policies** | CRUD + evaluation |
| **Access Reviews** | Access review campaigns, items, analytics |
| **Middleware** | ABAC policy middleware, access review middleware, permission caching middleware |

---

### 1.2 GameGuild.Identity.Authorization

**Maturity:** ★★★★★ Full — Enterprise-grade authorization engine (DAC model)

**Structure:** Commands, Queries, Controllers, Entities, Services, Handlers, Behaviors, Caching, Middleware, Providers, Requirements, Rules, Repositories, Notifications, Utilities, Identity, Exceptions, Extensions

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Tenant Permissions** | Grant permissions, revoke permissions, list tenant permissions, check permissions, set global defaults, set tenant defaults, deny permissions, remove denials |
| **Resource Permissions** | Share resources, update user permissions on resources |
| **Access Reviews** | Comprehensive access review system |
| **JIT (Just-In-Time) Elevations** | Temporary privilege escalation with time-bound access |
| **Delegated Administration** | Delegated admin capabilities |
| **SoD (Separation of Duties)** | Create/update/delete SoD rules, get rule by ID, list rules, detect violations per user, list user violations, list active violations, resolve violations, create exceptions, scan for violations |
| **Permission Analytics** | Analytics and reporting on permission usage |
| **Permission Delegations** | Delegate permissions between principals |
| **Permission Registry** | Centralized permission definitions |
| **Caching Layer** | In-memory permission caching for performance |
| **Behaviors** | Authorization pipeline behaviors for CQRS |

---

### 1.3 GameGuild.Identity.Users

**Maturity:** ★★★★★ Full — Rich user management with CQRS

**Structure:** Commands, Queries, Controllers, Entities, Repositories, ValueObjects, Events, Models

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Users CRUD** | Create, list, get by ID |
| **User Profiles** | List profiles, get profile, partial update (PATCH), full update (PUT) |
| **User Preferences** | Get/update user preferences |
| **User Metadata** | Get/update user metadata |
| **Bulk Operations** | Bulk create, bulk update, bulk replace, bulk delete, bulk activate, bulk deactivate, bulk suspend |
| **User Notifications** | List notifications (paginated, filterable), batch mark-as-read/unread, batch archive/unarchive, get/read/unread/archive individual notifications |
| **Value Objects** | Email, Phone, and other value objects for domain safety |

---

### 1.4 GameGuild.Identity.Tenants

**Maturity:** ★★★★★ Full — Multi-tenant management

**Structure:** Commands, Queries, Controllers, Entities, Services, Repositories, DTOs, Events, Middleware, Models, Utilities, Extensions

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Tenant CRUD** | Create tenant, list tenants, get tenant by ID |
| **Tenant Lifecycle** | Activate, deactivate, suspend, archive lifecycle management |
| **Tenant Metadata** | Get/patch/put metadata, custom fields management, tag management |
| **Tenant Settings** | Get/patch/put settings, feature flags per tenant, system limits, integration settings |
| **User Memberships** | List user memberships across tenants, count memberships |
| **Bulk Operations** | Bulk tenant operations |
| **Middleware** | Tenant resolution middleware (X-Tenant-Id header) |

---

### 1.5 GameGuild.Identity.Context

**Maturity:** ★★★ Core — Lightweight actor context infrastructure

**Structure:** Actors, Middleware, Module registration

#### Features & Capabilities

- **Actor Context:** Provides `IActorContextAccessor` for identifying the current authenticated user/actor across the system
- **Middleware:** Request pipeline middleware for establishing identity context

---

## 2. Commerce Domain

### 2.1 GameGuild.Commerce.Products

**Maturity:** ★★★★★ Full — Complete product catalog and entitlement system

**Structure:** Commands, Queries, Controllers, Entities, Services, Repositories, Models

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Product CRUD** | Get product, list products, create product, batch create, full update (PUT), partial update (PATCH), delete product |
| **Product Lifecycle** | Activate, deactivate, archive products |
| **Product Pricing** | Get pricing per product |
| **Entitlements** | List entitlements, check entitlement, batch-check entitlements, create entitlement, revoke entitlement |
| **User Entitlements** | User-facing entitlement queries |
| **Promo Codes** | List promo codes, get by ID, get by code, get usage stats, create, update, delete, activate, deactivate, validate, batch validate |

---

### 2.2 GameGuild.Commerce.Orders

**Maturity:** ★★★★☆ Full — Order management with lifecycle

**Structure:** Commands, Queries, Controllers, Entities, Handlers, Repositories, Events, Models

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Order CRUD** | Create order, add items to order, get order by ID, list orders (filterable), update order, delete order |
| **Order Lifecycle** | Complete, cancel, refund |
| **Payment Operations** | Capture payment, hold payment, release hold |
| **Event-Driven** | Order events for cross-module communication |

---

### 2.3 GameGuild.Commerce.Payments

**Maturity:** ★★★★★ Full — Complete payment processing with wallets and tax engine

**Structure:** Commands, Queries, Controllers, Entities, Services, Repositories, Models, Data

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Payments** | List payments (paginated), create payment, get payment by ID, cancel, refund, retry failed payment |
| **Wallets** | Create wallet, get wallet by user, check balance, add funds, deduct funds, transfer between wallets, lock/unlock wallet, list all wallets, get wallet details, update wallet, delete wallet, freeze/unfreeze, audit log |
| **Tax Calculation** | Calculate taxes, validate tax exemptions |
| **Tax Jurisdictions** | CRUD for tax jurisdictions |
| **Tax Rules** | CRUD for tax rules with filtering |

---

### 2.4 GameGuild.Commerce.Subscriptions

**Maturity:** ★★★★★ Full — Enterprise subscription management

**Structure:** Commands, Queries, Controllers, Entities, Handlers, Services, Repositories, DTOs, Events, Data, Extensions, Specifications, Models

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Subscription CRUD** | Create subscription, get subscription, list subscriptions |
| **Subscription Lifecycle** | Activate, start trial, end trial, cancel, suspend, pause, resume, reactivate, upgrade, downgrade, renew, auto-renew, external IDs management |
| **Subscription Plans CRUD** | Create plan, get plan, list plans, update plan, delete plan |
| **Plan Operations** | Get plan usage, suggest upgrades, get pricing, validate limits |
| **Billing** | Get metrics (MRR, churn, growth), list invoices, usage tracking, billing history |

---

### 2.5 GameGuild.Commerce.Billing

**Maturity:** ★★★★☆ Full — Payment provider webhook integration

**Structure:** Commands, Controllers, Entities, Services, Repositories, DTOs, Data, Extensions, Constants

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Webhook Processing** | Google Pay webhooks, Apple Pay webhooks, Stripe webhooks, PayPal webhooks |
| **Webhook Events** | Get webhook event by ID, retry failed webhook events |
| **Event Deduplication** | Idempotent webhook processing |

---

### 2.6 GameGuild.Commerce (Shared Kernel)

**Maturity:** ★★☆ Foundation — Shared entities and abstractions

- Shared commerce entities (base order, payment types, etc.)
- Shared repositories

---

## 3. Learning Domain

### 3.1 GameGuild.Learning.Courses

**Maturity:** ★★★★★ Full — Comprehensive LMS program/course management

**Structure:** Commands, Queries, Controllers, Entities, Handlers, Services, DTOs, Models, Extensions

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Program/Course CRUD** | Create, read, update, delete programs via `ProgramCrudController` |
| **Program Lifecycle** | Publish, unpublish, archive, manage status transitions via `ProgramLifecycleController` |
| **Program Content** | List content, get content by ID, create, update, delete, list children, reorder, move, filter by required/type/visibility, search content via `ProgramContentController` |
| **Content Interaction** | Track learner interactions with content (progress, completions) via `ContentInteractionController` |
| **Prerequisites** | Manage course prerequisites via `PrerequisitesController` |
| **Activity Grading** | Create grades, get by interaction/grader/student/content, update, delete, list pending, statistics via `ActivityGradeController` |

---

### 3.2 GameGuild.Learning.Assessments

**Maturity:** ★★★★☆ Full — Assessment engine with submissions and grading

**Structure:** Controllers, Entities, Services

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Assessment CRUD** | Create assessment, get by ID, list by course, update, delete |
| **Submissions** | Start submission, submit answers, grade submission, get submission, list submissions for assessment |
| **Student View** | My submissions per enrollment, check if can attempt |

---

### 3.3 GameGuild.Learning.Certificates

**Maturity:** ★★★★☆ Full — Credential issuance and verification

**Structure:** Controllers, Entities, Services

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Certificate Management** | List my certificates, get certificate by ID, issue certificate, revoke certificate |
| **Verification** | Verify certificate by certificate number (public) |
| **Queries** | List by course, list expiring certificates |

---

### 3.4 GameGuild.Learning.Cohorts

**Maturity:** ★★★★☆ Full — Cohort-based learning management

**Structure:** Controllers, Entities, Services

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Cohort CRUD** | Create cohort, get by ID, update, delete |
| **Queries** | List by course, active cohorts, enrollable cohorts |
| **Cohort Lifecycle** | Open enrollment, close enrollment, complete cohort, cancel cohort |

---

### 3.5 GameGuild.Learning.Enrollments

**Maturity:** ★★★★☆ Implemented — CQRS/API-backed enrollment lifecycle

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Enrollment Queries** | Get enrollment by ID, list by user, list by course |
| **Enrollment Commands** | Enroll user, update progress, transition status |
| **Modeling** | EF configuration, repository/service layer, generated client module |
| **Coverage** | `GameGuild.Learning.Enrollments.UnitTests` at 100% line / 100% branch / 100% method |

---

### 3.6 GameGuild.Learning (Shared)

**Maturity:** ★★☆ Foundation — Shared learning abstractions

- Learning constants, DTOs, events, filters, attributes
- `LearningServiceCollectionExtensions` for DI registration

---

## 4. Learning Experience Domain

### 4.1 GameGuild.Learning.Experience.Discovery

**Maturity:** ★★★★★ Full — Content discovery and curation engine

**Structure:** Commands, Queries, Controllers, Handlers, Services, DTOs, Entities

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Featured Content** | List featured, filter by type, get by ID, create, update, toggle visibility, delete |
| **Curated Collections** | List collections, featured collections, get by slug/ID/curator, create, update, publish/unpublish, delete |
| **Search Analytics** | Record searches, track clicks, search history per user, popular searches |

---

### 4.2 GameGuild.Learning.Experience.LearningPaths

**Maturity:** ★★★★★ Full — Structured multi-course learning paths

**Structure:** Commands, Queries, Controllers, Handlers, Services, DTOs, Entities

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Learning Path CRUD** | List paths, search, get by slug/ID, create, update, delete |
| **Discovery** | Featured paths, popular paths, paths by creator |
| **Path Lifecycle** | Publish, unpublish |
| **Course Management** | Add course to path, remove course, reorder courses |
| **Enrollment** | Enroll in path, unenroll, check enrollment, get enrollment status |
| **Progress** | Update progress, mark path complete |
| **Leaderboard & Analytics** | Leaderboard, completion statistics |

---

### 4.3 GameGuild.Learning.Experience.Recommendations

**Maturity:** ★★★★☆ Full — AI-driven course recommendation engine

**Structure:** Commands, Queries, Controllers, Handlers, Services, DTOs, Entities, Strategies

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Personalized Recommendations** | Get my recommendations, generate new recommendations, mark viewed, dismiss, refresh |
| **Profile** | Get/update learner profile, add/remove skills |
| **Statistics** | Recommendation engagement statistics |
| **Discovery** | Popular courses, trending courses, similar courses |
| **Strategies** | Pluggable recommendation strategy pattern |

---

### 4.4 GameGuild.Learning.Experience.Social

**Maturity:** ★★★★★ Full — Social learning features

**Structure:** Controllers, Entities, Services, Configuration

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Discussions** | Create discussion, get by ID, list by course, list by content, pin/unpin, resolve, delete |
| **Replies** | Create reply, list replies for discussion, accept answer, upvote, delete |
| **Reviews** | Create review, get by ID, list by course, my reviews |
| **Likes** | Like/unlike content |
| **Wishlists** | Add/remove courses from wishlist |
| **Activity Feed** | Personalized learning feed, generate feed, mark viewed, dismiss |

---

## 5. Social Domain

### 5.1 GameGuild.Social.Posts

**Maturity:** ★★★★★ Full — Social posting platform

**Structure:** Controllers, Entities, Services, Events, Configuration

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Post CRUD** | List posts, get post, create, update, delete, my posts, by author |
| **Feed** | Personalized feed, trending posts |
| **Post Interactions** | Like, pin, share, view tracking, statistics |
| **Post Following** | Follow/unfollow posts, check follow status |
| **Comments** | List comments, create, update, delete comments on posts |
| **Tags** | Popular tags, post tags, tag search |

---

### 5.2 GameGuild.Social.Follows

**Maturity:** ★★★★★ Full — Comprehensive following/social graph system

**Structure:** Controllers, Entities, Services, Events, Configuration

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Follow/Unfollow** | Follow entity, unfollow entity, check if following |
| **Follow Notifications** | Toggle notifications for followed entities |
| **Followers/Following** | List followers, list following, follower/following counts |
| **Social Graph** | Mutual followers |
| **Batch Operations** | Batch follow-status check, batch follower counts |
| **Privacy Settings** | Get/update privacy settings for follow system |
| **Blocking** | Block user, unblock user, check blocked status, list blocked users |
| **Muting** | Mute user, unmute user, check muted status, list muted users |
| **Statistics** | Engagement statistics |

---

### 5.3 GameGuild.Social.Ratings

**Maturity:** ★★★★★ Full — Universal rating/review system

**Structure:** Controllers, Entities, Services, Events, Configuration

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Ratings** | Create rating, get by ID, get my rating, check has-rated, delete rating |
| **Queries** | List by entity, rating summary, batch summaries, batch my-ratings, by user, rating count |
| **Interactions** | Mark rating as helpful, remove helpful mark, report rating |
| **Discovery** | Top rated by entity type, recent reviews |
| **Admin/Moderation** | Moderation queue, approve, reject, admin delete, recalculate summaries |

---

### 5.4 GameGuild.Social.Blog

**Maturity:** ★★★★☆ Implemented — CQRS/API-backed blog publishing module

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Blog CRUD** | List/create/get blog posts |
| **Publishing Workflow** | Publish, unpublish, feature/unfeature |
| **Engagement Counters** | View tracking and read metadata |
| **Coverage** | `GameGuild.Social.Blog.UnitTests` at 100% line / 100% branch / 100% method |

---

### 5.5 GameGuild.Social.Feed

**Maturity:** ★★★★☆ Implemented — CQRS/API-backed feed module

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Feed Items** | Add feed item, list user feed |
| **Feed State** | Mark read, hide item |
| **Modeling** | EF configuration, repository/service layer, generated client module |
| **Coverage** | `GameGuild.Social.Feed.UnitTests` at 100% line / 100% branch / 100% method |

---

### 5.6 GameGuild.Social.Profiles

**Maturity:** ★★★★ Implemented — CQRS/API-backed social profile module

- Adds profile data, privacy controls, skills, portfolio items, search, and activity/stat counters.
- Uses EF model configuration, repositories, services, CQRS handlers, and `SocialProfilesController`.
- Covered by `GameGuild.Social.Profiles.UnitTests` at 100% line / 100% branch / 100% method.

---

### 5.7 GameGuild.Social.Reactions

**Maturity:** ★★★★☆ Implemented — CQRS/API-backed reaction module

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Reaction CRUD** | Upsert/change/remove reaction per user and target |
| **Aggregation** | Target reaction summary and user-target lookup |
| **Modeling** | EF configuration, repository/service layer, generated client module |
| **Coverage** | `GameGuild.Social.Reactions.UnitTests` at 100% line / 100% branch / 100% method |

---

### 5.8 GameGuild.Social.Groups

**Maturity:** ★★★★☆ Implemented — CQRS/API-backed social groups and memberships

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Group Lifecycle** | List/create/get/update groups, activate, archive, suspend |
| **Membership Workflow** | Join public groups, request private/invite-only groups, approve/reject members, leave groups |
| **Roles** | Owner/admin/moderator/member model with protected owner flow |
| **Modeling** | EF configuration for `social_groups` and `social_group_members`, unique slug, unique group/user membership |
| **Client** | Generated `SocialGroupsSocialgroupsModule` from `/api/social/groups` OpenAPI surface |
| **Coverage** | `GameGuild.Social.Groups.UnitTests` at 100% line / 100% branch coverage |

---

## 6. Content Domain

### 6.1 GameGuild.Content.Pages

**Maturity:** ★★★★☆ Full — CMS pages and content resources

**Structure:** Controllers, Entities, Services, DTOs, Configuration

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Page CRUD** | List pages, get by ID, get by slug, create, update, delete |
| **Page Lifecycle** | Publish, unpublish |
| **Page Sections** | List sections, create section, get section, update section, delete section, reorder sections |
| **Content Resources** | List resources, get by ID/slug, create, update, delete, publish |
| **OpenGraph** | Dynamic OpenGraph metadata endpoint for SEO/social sharing |

---

### 6.2 GameGuild.Resources

**Maturity:** ★★★★★ Full — Multi-tenant resource management and quotas

**Structure:** Commands, Queries, Controllers, Entities, Handlers, Services, Repositories, Models, Data, Events, Behaviors, Attributes, Exceptions, Extensions

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Resource Administration** | Usage reports, usage trends, archive resources, cleanup resources |
| **Tenant Quotas** | CRUD for tenant quotas by type, reset quota, toggle quota enforcement, check quota availability |
| **Tenant Resources** | Usage records, usage summary, resource limits, record usage, record with quota check, reset resources |
| **Tenant Resource Metadata** | Get/set/delete metadata key-value pairs |
| **Tenant Resource Settings** | Resource settings per tenant |
| **User Quotas** | User-level quota management |
| **User Resources** | User resource tracking |
| **User Resource Metadata** | User-level metadata |
| **User Resource Settings** | User-level resource settings |

---

### 6.3 GameGuild.Resources.Contents

**Maturity:** ★★★★★ Full — Content versioning and editorial workflow

**Structure:** Controllers, Entities, Services, Configuration

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Drafts** | Create draft, update draft |
| **Versioning** | Get version, version history by entity, current version, specific version number |
| **Editorial Workflow** | Submit for review, list pending reviews, approve, reject |
| **Publishing** | Publish version, schedule publication, cancel scheduled publication |
| **Version Comparison** | Compare two versions |
| **Rollback** | Rollback entity to previous version |
| **Reviews** | Add version reviews |

---

### 6.4 GameGuild.Assets

**Maturity:** ★★★★★ Full — Enterprise asset management with CDN

**Structure:** Commands, Queries, Controllers, Entities, Services, Repositories, Models, Extensions, Configuration, BackgroundServices

**Sub-systems:** Storage, Transformation, VirusScan, Moderation, Deduplication, Security

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Upload** | Single file upload, chunked upload (init, upload parts, complete, abort) |
| **Asset CRUD** | Get asset by ID, generate signed access URL |
| **CDN Delivery** | Serve assets with token-based access, embed-friendly URL, on-the-fly image transformation |
| **Admin** | Moderation queue, asset reports, review reports, force-delete, list all assets, GC candidates, run virus scan, run garbage collection, mark undeletable, unmark undeletable, review moderation |
| **Security** | Token-based access, virus scanning, content moderation |
| **Background Services** | Asynchronous processing for uploads, virus scans, and cleanup |
| **Deduplication** | Content-hash based deduplication |

---

## 7. Gamification Domain

### 7.1 GameGuild.Gamification.Achievements

**Maturity:** ★★★★☆ Full — Achievement/badge system with point tracking

**Structure:** Controllers, Entities, Services, Events, Configuration

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **My Achievements** | List my achievements, my total points, unnotified achievements, mark notified |
| **Discovery** | List eligible achievements, list all achievements, get achievement details |
| **Admin** | Create achievement, update achievement, delete achievement |
| **Awarding** | Award achievement to user |
| **Events** | Achievement-earned events for notifications/gamification hooks |

---

## 8. Projects & Testing Domain

### 8.1 GameGuild.Projects

**Maturity:** ★★★★★ Full — Complete project portfolio with GraphQL

**Structure:** Commands, Queries, Controllers, Entities, Handlers, Services, Validators, GraphQL, Models, Enums

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Project CRUD** | List projects (paginated), get by ID, get by slug, create, update, delete |
| **Project Lifecycle** | Publish, unpublish, archive |
| **Discovery** | Search projects, popular projects, recent projects, featured projects |
| **Analytics** | Project statistics |
| **Collaborator Permissions** | Get my permissions, list collaborators, add collaborator, update collaborator role, remove collaborator |
| **Role Templates** | List permission role templates, share with role |
| **GraphQL** | Full GraphQL types and resolvers for projects |
| **Enums** | DevelopmentStatus, ProgressStatus, ProjectType, FeedbackFormQuestionType |

---

### 8.2 GameGuild.TestingLab

**Maturity:** ★★★★★ Full — Game testing platform with scheduling and feedback

**Structure:** Commands, Queries, Controllers, Entities, Handlers, Services, Repositories, DTOs, Events, Validators, GraphQL

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Testing Requests** | Create, list, manage testing requests |
| **Testing Sessions** | Create/manage testing sessions for games |
| **Testing Feedback** | Submit feedback for requests, list feedback, user-specific feedback, report feedback, quality scoring |
| **Testing Participants** | Manage testers participating in sessions |
| **Testing Locations** | CRUD for testing locations (physical/virtual) |
| **Testing Lab Settings** | Platform-wide test lab configuration |
| **Permissions** | Role templates CRUD, user role assignment/removal, resource-level permissions, permission checking |
| **GraphQL** | GraphQL types and resolvers |

---

### 8.3 GameGuild.GameJams

**Maturity:** ★★★★☆ Implemented — CQRS/API-backed game jam lifecycle

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Jam Lifecycle** | List/create jams, get by ID, update status |
| **Submissions** | List/create submissions per jam |
| **Judging** | Add/list criteria, score submissions |
| **Coverage** | `GameGuild.GameJams.UnitTests` at 100% line / 100% branch / 100% method |

---

## 9. Platform Domain

### 9.1 GameGuild.Features

**Maturity:** ★★★★☆ Full — Feature flag system with capabilities

**Structure:** Commands, Queries, Controllers, Entities, Services, Repositories, Models, DTOs, Data, Events, Provider, Middleware, Specifications, Extensions

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Feature Flag Evaluation** | Evaluate single flag, get flag value, bulk evaluate flags, list enabled flags |
| **Capabilities** | List capabilities, get capability, create capability, delete capability, sync capabilities, audit log |
| **Feature Flag Provider** | Pluggable feature flag evaluation engine |
| **Middleware** | Feature flag middleware for request pipeline |

---

### 9.2 GameGuild.Notifications

**Maturity:** ★★★★☆ Full — Multi-channel notification system

**Structure:** Controllers, Entities, Services, Configuration

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Notification Queries** | List notifications (paginated, filterable), unread count, get by ID |
| **Notification Actions** | Mark as read, read all, mark as unread, delete notification, delete all read |
| **Preferences** | Get notification preferences, update preferences, set quiet hours |

---

### 9.3 GameGuild.Tags

**Maturity:** ★★★★☆ Implemented — CQRS/API-backed taxonomy and proficiency system

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Tags** | List/search/create/update tags |
| **Relationships** | Create/list relationships between tags |
| **Proficiencies** | Create/list user skill proficiencies |
| **Coverage** | `GameGuild.Tags.UnitTests` at 100% line / 100% branch / 100% method |

---

### 9.4 GameGuild.Localization

**Maturity:** ★★★☆ Core — Localization and translation infrastructure

**Structure:** Abstractions, Services, Repositories, Models, Extensions

**Models:** Language, LocalizableEntityBase, LocalizableResource, LocalizationStatus, ResourceLocalization, TranslationWorkflowEntity

#### Features & Capabilities

- **Localizable Entity Pattern** — Base class for entities that support multiple languages
- **Translation Workflow** — Workflow tracking for translation processes
- **Resource Localization** — String resource localization management
- No REST controllers — provides infrastructure consumed by other modules

---

## 10. Compliance & Monitoring Domain

### 10.1 GameGuild.Compliance.Audit

**Maturity:** ★★★★☆ Full — Security and compliance audit logging

**Structure:** Controllers, Entities, Services, DTOs, Enums, Models, Constants, Extensions

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **Audit Logs** | List audit logs (paginated, filterable), audit statistics, export audit data |
| **Security Audit** | Query security events, authentication audit, permission audit, security dashboard, export security audit |
| **Compliance Standards** | SOC2, ISO 27001, GDPR, HIPAA audit support |

---

### 10.2 GameGuild.Compliance.KYC

**Maturity:** ★★★☆ Core — Know Your Customer identity verification

**Structure:** Commands, Handlers, Services, Repositories, Models

#### Features & Capabilities

- **KYC Verification** — Identity verification workflow
- **KYC Provider** — Pluggable KYC provider abstraction
- **Verification Status** — Status tracking for verification processes
- No REST controllers — operates via commands/handlers (CQRS)

---

### 10.3 GameGuild.Compliance.FERPA

**Maturity:** ★★★★ Implemented — CQRS/API-backed FERPA compliance module

- Adds education record classification, directory-information policy, disclosure consent/logging, and inspection request workflows.
- Uses EF model configuration, repositories, services, CQRS handlers, and `FerpaController`.
- Covered by `GameGuild.Compliance.FERPA.UnitTests` at 100% line / 100% branch / 100% method.

---

### 10.4 GameGuild.Monitoring.SLA

**Maturity:** ★★★★☆ Full — SLA/SLO monitoring and error budgets

**Structure:** Commands, Queries, Controllers, Entities, Services, Repositories, Models, Data, Enums, Extensions

#### Features & Capabilities

| Area | Endpoints / Capabilities |
|------|--------------------------|
| **SLO Management** | Create SLO, list SLOs, get SLO by ID, update SLO, delete SLO |
| **SLI Recording** | Record service level indicators |
| **Compliance** | Check SLO compliance status |
| **Error Budget** | Get remaining error budget for SLO |
| **Violations** | List SLA violations, resolve violations |

---

## 11. Cross-Cutting

### 11.1 GameGuild.SharedKernel

**Maturity:** ★★★★★ Full — Foundational shared infrastructure

**Structure:** CQRS, Entities, Repositories, ValueObjects, Modules, Configuration, Controllers, Endpoints, Diagnostics, Exceptions, Filters, Infrastructure, Middlewares, Models, Serialization, Transformers, Enums

#### Features & Capabilities

| Area | Details |
|------|---------|
| **CQRS Framework** | Custom implementation with `IRequest<T>`, `IRequestHandler<TRequest, TResponse>`, pipeline behaviors (validation, logging, performance), publisher abstraction |
| **Entity Base** | `EntityBase` with audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy), soft-delete support, `Version` for optimistic concurrency |
| **Repositories** | Generic repository pattern abstractions |
| **Value Objects** | Email, Phone, Money, and other domain value objects |
| **Module System** | `IModule` interface for modular registration (`ConfigureServices()`, `MapEndpoints()`) |
| **Base Controller** | `BaseApiController` with standardized response formatting |
| **API Versioning** | Multi-version API support |
| **Exception Handling** | Global exception filters → ProblemDetails |
| **Serialization** | Custom JSON serialization configuration |
| **Diagnostics** | Health checks and diagnostic endpoints |
| **Middleware** | Correlation ID, request logging, tenant resolution |

---

## Summary Statistics

| Metric | Count |
|--------|-------|
| **Total Modules** | 45 |
| **Fully Implemented (★★★★+)** | 35 |
| **Core/Foundation (★★-★★★)** | 10 |
| **Skeleton/Placeholder (★)** | 0 |
| **Controllers** | 80+ |
| **REST Endpoints** | 500+ |
| **Domains** | 10 |
| **Modules with GraphQL** | 2 (Projects, TestingLab) |

### Module Maturity Legend

| Rating | Meaning |
|--------|---------|
| ★★★★★ | **Full** — Production-ready with comprehensive endpoints, CQRS, validation, event handling |
| ★★★★☆ | **Full** — Complete functionality, minor areas may lack polish |
| ★★★☆ | **Core** — Working infrastructure/services, may lack full REST surface |
| ★★☆ | **Foundation** — Entities/models defined, limited or no API exposure |
| ★☆ | **Skeleton** — Placeholder module, minimal content |
