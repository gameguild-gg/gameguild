# Skeleton & Dead Modules Inventory

> **Generated:** 2026-02-08 | **Branch:** `feat/modular-backend`
>
> These modules exist in the solution but have **minimal or no implementation**. They are planned for future development and migration. This document serves as a roadmap for what each module should become.

---

## Summary

| # | Module | Status | Files | Lines | Priority |
|---|--------|--------|-------|-------|----------|
| 1 | `GameGuild.Compliance.FERPA` | 🔴 **DEAD** — empty project, zero code | 0 | 0 | 🔴 High |
| 2 | `GameGuild.Social.Profiles` | 🔴 **DEAD** — empty project, zero code | 0 | 0 | 🔴 High |
| 3 | `GameGuild.GameJams` | 🟠 **SKELETON** — entity stubs only | 5 | 79 | 🟡 Medium |
| 4 | `GameGuild.Social.Reactions` | 🟠 **SKELETON** — single entity only | 1 | 53 | 🟡 Medium |
| 5 | `GameGuild.Social.Feed` | 🟠 **SKELETON** — single entity only | 1 | 77 | 🟡 Medium |
| 6 | `GameGuild.Social.Blog` | 🟠 **SKELETON** — single entity only | 1 | 80 | 🟡 Medium |
| 7 | `GameGuild.Learning.Enrollments` | 🟠 **SKELETON** — single entity only | 1 | 82 | 🔴 High |
| 8 | `GameGuild.Tags` | 🟡 **PARTIAL** — entities + EF config, no services | 10 | 238 | 🔴 Critical |
| 9 | `GameGuild.Commerce` (base) | 🟡 **PARTIAL** — shared entities + base repo | 3 | 484 | 🟢 Low |

**Not skeleton (confirmed substantive):** All other modules have working controllers, services, CQRS handlers, and real business logic. Modules like `Learning.Cohorts` (710 lines), `Learning.Experience.Social` (2,771 lines), `Learning.Experience.Discovery` (1,748 lines), and `Learning.Experience.LearningPaths` (2,092 lines) are small but fully functional.

**Does not exist:** `GameGuild.Social.Groups` — referenced in some discussions but never created. Needed for study groups, team collaboration, and cohort-based social learning.

---

## Module Details

### 1. 🔴 `GameGuild.Compliance.FERPA` — DEAD (0 lines)

**Current state:** Empty `.csproj` file with zero code. Not even a placeholder class.

> ⚠️ Note: The `.csproj.DotSettings` file references `GameGuild.Monitoring.FERPA` — a namespace mismatch that should be corrected when implementing.

#### What It Should Have

FERPA (Family Educational Rights and Privacy Act) compliance is **legally required** if GameGuild serves US educational institutions or handles student education records.

| Feature Area | Capabilities |
|-------------|-------------|
| **Education Record Management** | Define what constitutes an "education record" (grades, assessments, enrollments, progress); classify data by FERPA protection level; record access audit trail |
| **Consent Management** | Parental consent workflow for minors (<18); student consent for disclosure to third parties; consent record storage with timestamps; consent revocation handling |
| **Directory Information Controls** | Configurable directory information fields per institution; opt-out mechanism for students; institution-level policy settings |
| **Access Control & Disclosure** | Legitimate educational interest verification; third-party disclosure tracking with purpose logging; annual notification system; FERPA-specific role definitions (school official, authorized representative) |
| **Data Subject Rights** | Student right to inspect records; right to request amendment; formal hearing process for disputed records; complaint tracking and resolution |
| **Reporting & Audit** | FERPA compliance audit reports; disclosure logs with timestamps; access pattern anomaly detection; integration with `Compliance.Audit` module |
| **Data Retention** | FERPA-compliant retention policies; secure data deletion (not just soft-delete); record transfer between institutions |
| **Technical Safeguards** | Encryption at rest/in transit for education records; data masking for non-authorized viewers; de-identification for analytics use |

**Dependencies:** `Compliance.Audit`, `Identity.Authorization`, `Identity.Users`, `Learning.Courses`, `Learning.Assessments`

---

### 2. 🔴 `GameGuild.Social.Profiles` — DEAD (0 lines)

**Current state:** Empty `.csproj` file. No code at all.

#### What It Should Have

User profiles are the **public-facing identity** on the platform. Currently, user data lives in `Identity.Users` but that's for account management, not social presentation.

| Feature Area | Capabilities |
|-------------|-------------|
| **Profile Data** | Display name, bio, avatar, banner image, pronouns, location, timezone; social links (GitHub, LinkedIn, portfolio, itch.io); custom vanity URL |
| **Skill & Portfolio Showcase** | Skills/tags with proficiency levels (integrates with `Tags` module); completed courses, certificates, learning paths; project portfolio with pinned projects; game jam participation history |
| **Activity & Stats** | Public activity feed; learning streak, XP, level (from `Gamification.Achievements`); contribution stats (posts, reviews, projects); GitHub-style contribution heatmap |
| **Privacy Controls** | Granular visibility settings per profile section (public/connections/private); profile visibility by role (student, instructor, employer); block/mute management (integrates with `Social.Follows`) |
| **Professional Features** | Resume/CV generation from profile data; availability status (open to opportunities, mentoring, collaboration); endorsements and recommendations |
| **Profile Management** | Profile completeness score and nudges; profile verification badge; bulk profile import for institutions |

**Dependencies:** `Identity.Users`, `Tags`, `Gamification.Achievements`, `Social.Follows`, `Projects`, `Learning.Certificates`

---

### 3. 🟠 `GameGuild.GameJams` — SKELETON (79 lines)

**Current state:** 5 bare entity/model classes with basic properties:
- `Jam.cs` — Name, Slug, Theme, Description, Rules, StartDate, EndDate, Status, MaxParticipants, MinTeamSize, MaxTeamSize
- `JamSubmission.cs` — JamId, ProjectVersionId, UserId, SubmissionNotes
- `JamJudgingCriteria.cs` — JamId, Name, Description, Weight, MaxScore
- `JamScore.cs` — SubmissionId, CriteriaId, JudgeUserId, Score, Feedback
- `JamStatus.cs` — Enum: Upcoming, Active, Voting, Completed, Cancelled

**What's missing:** No `IModule` registration, no controllers, no services, no CQRS handlers, no EF configuration, no validators, no DTOs. Entities are plain POCOs with DataAnnotation validation only (no factory methods, no domain behavior).

#### What It Should Have

Game jams are the **signature differentiator** for a game development LXP.

| Feature Area | Capabilities |
|-------------|-------------|
| **Jam Lifecycle** | Create/configure jams with rules, themes, dates, judging criteria; state machine: Draft → Announced → Registration → Active → Voting → Completed/Cancelled; auto-state transitions via background jobs (e.g., auto-start at scheduled time) |
| **Registration & Teams** | Individual or team registration; team formation, invitations, role assignment (artist, programmer, designer, audio); capacity management with waitlists; prerequisite course requirements to enter |
| **Submission System** | Link submissions to `Projects` module; file uploads (builds, screenshots, videos, GDD); submission deadline enforcement with grace period config; versioned submissions; platform tags for submitted games |
| **Judging & Scoring** | Multi-criteria weighted scoring; judge assignment with conflict-of-interest checks; peer voting mode vs panel judging vs hybrid; score aggregation and ranking algorithms; community voting with anti-fraud measures (rate limiting, verified-only) |
| **Results & Awards** | Final rankings with category winners (best art, best gameplay, etc.); certificates and badge integration (`Gamification.Achievements`); winner showcase gallery page; downloadable builds from finalists |
| **Communication** | Jam-scoped announcements and updates; participant chat/discussions; mentor assignment during active jams; devlog/progress update system |
| **Analytics** | Participation rates, completion rates, team sizes; submission quality metrics; engagement tracking; historical jam comparisons |
| **Recurring Jams** | Template system for recurring monthly/weekly jams; series tracking (Ludum Dare style); cumulative leaderboards across jam series |

**Dependencies:** `Projects`, `Gamification.Achievements`, `Identity.Users`, `Social.Posts`, `Tags`, `Learning.Certificates`

---

### 4. 🟠 `GameGuild.Social.Reactions` — SKELETON (53 lines)

**Current state:** Single entity `Reaction.cs` with:
- Properties: UserId, TargetId, TargetType, Type
- Factory method `Create()` and `ChangeType()` method
- Two enums: `ReactionType` (Like, Love, Insightful, Celebrate, Support, Curious) and `ReactionTargetType` (Post, Comment, BlogPost, CourseReview, Discussion, Reply)

#### What It Should Have

| Feature Area | Capabilities |
|-------------|-------------|
| **Reaction CRUD** | Add/remove/change reactions on any content type; one reaction per user per target (upsert semantics); polymorphic target resolution |
| **Aggregation** | Real-time reaction counts per target (cached); reaction breakdown by type; "top reactions" display |
| **Notification Integration** | Notify content author when reactions received; batch notification for multiple reactions; configurable notification preferences |
| **Analytics** | Most reacted content per time period; reaction sentiment analysis; engagement scoring based on reactions |
| **Module Registration** | `IModule` implementation; EF configuration; controller with REST endpoints; service layer |

**Dependencies:** `Social.Posts`, `Social.Blog`, `Social.Feed`, `Notifications`

---

### 5. 🟠 `GameGuild.Social.Feed` — SKELETON (77 lines)

**Current state:** Single entity `FeedItem.cs` with:
- Properties: UserId, ContentId, ContentType, AuthorId, RelevanceScore, Reason, IsRead, IsHidden, ContentCreatedAt
- Factory method `Create()`, `MarkRead()`, `Hide()` methods
- Two enums: `FeedContentType` (Post, BlogPost, CourseReview, ProjectUpdate, Achievement, CourseCompletion) and `FeedItemReason` (Following, Trending, Recommended, Mentioned, Replied, Liked, InNetwork)

#### What It Should Have

| Feature Area | Capabilities |
|-------------|-------------|
| **Feed Generation** | Build personalized feed from followed users' activity; include trending content from across platform; mix algorithmic and chronological ordering; configurable feed sources per user |
| **Feed Algorithms** | Relevance scoring based on engagement, recency, social proximity; de-duplication of similar content; diversity injection (avoid content type monotony); "explore" feed for discovery beyond network |
| **Feed Management** | Mark read/unread; hide content; "show less like this" feedback; infinite scroll pagination with cursor-based queries |
| **Real-time Updates** | SignalR/WebSocket push for new feed items; "X new items" notification badge; live-updating reaction counts |
| **Content Aggregation** | Aggregate similar activities ("3 people completed Unity course"); group notifications by source; daily/weekly digest generation |
| **Performance** | Pre-computed feed materialization for active users; cache invalidation strategy; fan-out on write vs fan-out on read hybrid |

**Dependencies:** `Social.Posts`, `Social.Blog`, `Social.Follows`, `Social.Reactions`, `Projects`, `Gamification.Achievements`, `Learning.Courses`

---

### 6. 🟠 `GameGuild.Social.Blog` — SKELETON (80 lines)

**Current state:** Single entity `BlogPost.cs` with:
- Properties: AuthorId, Title, Slug, Excerpt, Content, CoverImageUrl, Status, PublishedAt, IsFeatured, AllowComments, ViewsCount, LikesCount, CommentsCount, ReadTimeMinutes
- Factory method `Create()`, `Publish()`, `Unpublish()`, `Feature()`, `Unfeature()`, counter increment/decrement methods
- Enum: `BlogPostStatus` (Draft, Published, Archived)

#### What It Should Have

| Feature Area | Capabilities |
|-------------|-------------|
| **Blog CRUD** | Rich text editor (markdown + WYSIWYG); draft autosave; revision history; SEO metadata (meta description, canonical URL, Open Graph) |
| **Content Organization** | Categories and tags (integrates with `Tags` module); series/collection grouping; table of contents auto-generation; related posts suggestions |
| **Publishing Workflow** | Draft → Review → Published → Archived; scheduled publishing; co-authoring support; editorial review for institutional blogs |
| **Reader Experience** | Estimated read time; progress indicator; bookmark/save for later; text-to-speech; syntax highlighting for code blocks |
| **Engagement** | Comment threads (integrate with `Social.Posts` comment system or own); reactions (integrate with `Social.Reactions`); sharing tools; report/flag inappropriate content |
| **Analytics** | Views, unique readers, read completion rate; referral sources; engagement metrics per post; author analytics dashboard |
| **RSS & Syndication** | RSS/Atom feed generation; email newsletter integration; cross-posting to external platforms |

**Dependencies:** `Tags`, `Social.Reactions`, `Social.Feed`, `Assets` (for images), `Notifications`

---

### 7. 🟠 `GameGuild.Learning.Enrollments` — SKELETON (82 lines)

**Current state:** Single entity `Enrollment.cs` with:
- Properties: CourseId, UserId, CohortId, Status, EnrolledAt, CompletedAt, DroppedAt, Progress (0-100), LastActivityAt
- Factory method `Create()`, `UpdateProgress()`, `Complete()`, `Drop()`, `Pause()`, `Resume()` methods
- Enum: `EnrollmentStatus` (Active, Paused, Completed, Dropped, Expired)

> ⚠️ Note: `Learning.Courses` module already has enrollment-like functionality. This module should either **replace** the enrollment logic in Courses or be **merged** into it. Clarify the boundary before implementing.

#### What It Should Have

| Feature Area | Capabilities |
|-------------|-------------|
| **Enrollment Management** | Enroll/unenroll students; enforce prerequisites (passed courses, skill tags); capacity limits with waitlists; approval workflows for restricted courses; bulk enrollment for institutions |
| **Progress Tracking** | Lesson-level completion tracking; time spent per module; last activity tracking; resume-where-left-off; progress milestones with notifications |
| **Cohort Integration** | Assign enrollment to cohort for paced learning; cohort-specific deadlines; cohort progress leaderboard; group pacing enforcement |
| **Certification Flow** | Auto-issue certificate on completion (integrate with `Learning.Certificates`); grade calculation from assessments; transcript generation |
| **Engagement & Retention** | Inactive student reminders; at-risk student detection (ML-based); nudge system for stalled progress; re-enrollment after drop |
| **Reporting** | Enrollment funnel analytics (enrolled → active → completed → certified); dropout analysis; cohort comparison reports; institution-level dashboards |
| **Access Control** | Time-limited access (enrollment expiry); paid course access enforcement (integrates with `Commerce`); institutional seat licensing |

**Dependencies:** `Learning.Courses`, `Learning.Assessments`, `Learning.Certificates`, `Learning.Cohorts`, `Commerce.Orders`, `Notifications`

---

### 8. 🟡 `GameGuild.Tags` — PARTIAL SKELETON (238 lines)

**Current state:** Well-designed entity model but zero operational code:
- `Tag.cs` — Name, Description, Type, Color, Icon, IsActive, navigation to relationships (37 lines)
- `TagProficiency.cs` — Skill proficiency levels tied to tags and users (36 lines)
- `TagRelationship.cs` — Source→Target junction with type and weight (31 lines)
- `TagType.cs` — Enum: Skill, Topic, Technology, Difficulty, Category, Industry, Certification (26 lines)
- `TagRelationshipType.cs` — Enum: Related, Parent, Child, Requires, Suggested (20 lines)
- `TagRelationshipConfiguration.cs` — EF config with FK constraints and self-reference check (22 lines)
- `Models/SkillProficiencyLevel.cs` — Enum: Beginner→Master 6 levels (25 lines)
- ⚠️ **Duplicate enums** in `Models/TagType.cs` and `Models/TagRelationshipType.cs` — identical to `Entities/` versions

**What's missing:** No `IModule`, no controllers, no services, no CQRS, no repositories, no DTOs, no validators. No polymorphic tagging mechanism (how do courses/projects/users get tagged?).

#### What It Should Have

Tags are the **semantic backbone** of the entire LXP — they drive search, discovery, recommendations, and skill tracking.

| Feature Area | Capabilities |
|-------------|-------------|
| **Tag CRUD & Taxonomy** | Create/edit/delete/merge tags; hierarchical taxonomy with parent/child; tag aliasing ("C#" = "CSharp" = "C Sharp"); bulk import/export of tag trees; admin moderation queue |
| **Polymorphic Tagging** | Tag any entity type: Courses, Projects, Users, Blog Posts, Discussions, Game Jams, Learning Paths; multi-tag with ordering/weighting; tagged entity count tracking per tag |
| **Skill Graph** | Prerequisite relationships (Tag A requires Tag B); related skill mapping for recommendations; skill progression paths (Beginner → Expert); visual skill tree rendering data |
| **Search & Discovery** | Tag-based filtering across all content types; tag cloud generation; trending tags; tag co-occurrence analysis for "related tags"; autocomplete/typeahead search |
| **User Skill Profiles** | Users claim tags at proficiency levels; skill verification through assessments; peer endorsements; skill gap analysis ("You know X, learn Y next"); employer skill matching |
| **Admin & Analytics** | Tag usage analytics; orphan tag detection; tag merge/split operations; synonym management; popular tag trends over time |
| **API** | Full CRUD endpoints; autocomplete endpoint; batch tagging endpoint; tag statistics; tag hierarchy tree endpoint |

**Dependencies:** Used by almost every module — `Learning.Courses`, `Projects`, `Social.Profiles`, `Recommendations`, `GameJams`, `Social.Blog`, `Social.Posts`

---

### 9. 🟢 `GameGuild.Commerce` (base) — PARTIAL (484 lines)

**Current state:** Shared commerce infrastructure:
- `PricingRule.cs` — Rich entity (234 lines) with volume discounts, time-based pricing, region-based pricing, customer segments, Buy X Get Y promotions. Well-documented with XML docs.
- `PricingRuleTier.cs` — Tier definitions for volume-based pricing
- `CommerceRepositoryBase.cs` — Base repository pattern for commerce modules

**Assessment:** This is **intentionally** a shared library for `Commerce.Orders`, `Commerce.Payments`, `Commerce.Products`, and `Commerce.Subscriptions`. It's not dead — it's a base module. The existing submodules are substantive. **No action needed** beyond keeping it as shared infrastructure.

---

## Missing Module: `GameGuild.Social.Groups`

This module **does not exist** but is referenced in architecture discussions. For a game development LXP, social groups are important for:

| Feature Area | Capabilities |
|-------------|-------------|
| **Group Types** | Study groups, project teams, interest communities, course cohort groups, institution groups |
| **Group Management** | Create/join/leave groups; public/private/invite-only settings; role hierarchy (owner, admin, moderator, member); membership approval workflows |
| **Group Content** | Group-scoped discussions and posts; shared resource library; group announcements; pinned content |
| **Collaboration** | Group project workspaces; shared learning goals and progress tracking; group game jam participation; peer review assignments within groups |
| **Discovery** | Group search and recommendations; "Groups you might like" based on tags/interests; trending groups; group activity feed |

**Priority:** 🟡 Medium — Important for community building and cohort-based learning but not blocking core LXP functionality.

---

## Implementation Priority Order

Based on platform dependencies and LXP criticality:

| Priority | Module | Rationale |
|----------|--------|-----------|
| 1 | **Tags** | Semantic backbone — search, discovery, recommendations, skill profiles all depend on it. Unblocks multiple other modules. |
| 2 | **Learning.Enrollments** | Core learning flow — enrollment is fundamental to an LXP. Must clarify boundary with `Learning.Courses` enrollment logic. |
| 3 | **Social.Profiles** | Public identity layer — needed for social features, marketplace, and professional networking. |
| 4 | **Compliance.FERPA** | Legal requirement for US educational institutions. Must exist before onboarding schools/universities. |
| 5 | **Social.Reactions** | Small scope, high engagement impact. Powers engagement across Posts, Blog, Reviews. |
| 6 | **Social.Feed** | Personalized content delivery — drives daily active usage and retention. |
| 7 | **Social.Blog** | Content marketing + community knowledge sharing. Instructor and student blogs. |
| 8 | **GameJams** | Signature differentiator for game dev education. Major engagement driver but not core learning flow. |
| 9 | **Social.Groups** | Community building. Can be deferred until social features mature. |

---

*This document should be updated as modules are implemented. Check off completed modules and adjust priorities based on product roadmap.*
