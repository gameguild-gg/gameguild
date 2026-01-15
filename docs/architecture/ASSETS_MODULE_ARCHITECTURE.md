# GameGuild Assets Module Architecture & Module Analysis Report

**Date:** January 15, 2026  
**Scope:** GameGuild.Resources, GameGuild.Features, GameGuild.Localization (Analysis) + GameGuild.Assets (Design)  
**Review Type:** Deep Module Analysis + New Module Architecture Specification  
**Status:** ✅ COMPLETE

---

## Executive Summary

This report provides:
1. **Deep analysis** of three existing modules: Resources, Features, and Localization
2. **Complete architecture specification** for a new `GameGuild.Assets` module
3. **Security threat model** and mitigations
4. **Integration patterns** with existing modules
5. **Implementation roadmap**

### System Foundation

> **Core Concept:** In GameGuild, **every tenant-scoped entity is a Resource**. All entities inherit from `EntityBase<TKey>` which implements `ITenantScoped`. The `GameGuild.Resources` module provides horizontal quota management infrastructure that applies to ALL entity types (Users, Projects, Posts, Courses, Products, Assets, etc.). The `Identity.Authorization` module defines `ResourceTypes` — a strongly-typed registry of all resource categories for permission evaluation.

### Key Findings

| Module | Status | Critical Issues | Recommended Priority |
|--------|--------|-----------------|---------------------|
| **Resources** | ✅ SOUND | Minor: No `Assets` resource type yet | P2 - Add resource type |
| **Features** | ✅ SOUND | Minor: No asset transformation feature flags | P2 - Add feature keys |
| **Localization** | ⚠️ NEEDS WORK | Incomplete: No error message localization service | P1 - Add error localization |
| **Assets** | 🆕 NEW | N/A - New module design | P0 - Implement |

### Key Risks for Assets Module

| Risk | Severity | Status |
|------|----------|--------|
| Hotlinking/bandwidth abuse | HIGH | Mitigated by access counter + token rotation |
| Token replay attacks | HIGH | Mitigated by time-window rotation |
| Malware upload | CRITICAL | Mitigated by virus scanning pipeline |
| Cross-tenant asset leakage | CRITICAL | Mitigated by fail-closed tenant validation |
| CDN cache poisoning | MEDIUM | Mitigated by canonical URL signing |

---

## Foundational Architecture: Entity = Resource

Understanding the GameGuild entity hierarchy is critical before analyzing any module:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ENTITY/RESOURCE HIERARCHY                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ITenantScoped (interface)                                              │
│       └── Guid? TenantId                                                │
│                                                                         │
│  IEntity<TKey> (interface)                                              │
│       └── TKey Id, IAuditable, IConcurrencyControlled                   │
│                                                                         │
│  EntityBase<TKey> : IEntity<TKey>, ITenantScoped                        │
│       │   - TenantId (nullable for global entities)                     │
│       │   - Version (optimistic concurrency)                            │
│       │   - CreatedAt, UpdatedAt, DeletedAt (soft delete)               │
│       │   - DomainEvents collection                                     │
│       │                                                                 │
│       ├── Tenant          (TenantId = null, IsGlobal = true)            │
│       ├── User            (scoped to tenant)                            │
│       ├── Project         (scoped to tenant)                            │
│       ├── Post            (scoped to tenant)                            │
│       ├── Course          (scoped to tenant)                            │
│       ├── Product         (scoped to tenant)                            │
│       ├── ResourceQuota   (scoped to tenant)                            │
│       ├── FeatureFlag     (scoped to tenant)                            │
│       ├── Language        (scoped to tenant)                            │
│       └── AssetContent    (NEW - scoped to tenant)                      │
│                                                                         │
│  ResourceTypes (Identity.Authorization)                                 │
│       - Strongly-typed identifiers for DAC/ABAC/ACL                     │
│       - User, Tenant, Project, Content, Post, Course, Product...        │
│       - Used in permission grants and policy evaluation                 │
│                                                                         │
│  ResourceUsageType (enum in Resources module)                           │
│       - Registry of quotable resource categories                        │
│       - Users, Projects, Storage, ApiCalls, Assets...                   │
│       - Used by ResourceQuotaBehavior for enforcement                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**Key Insight:** There are THREE "resource" concepts in GameGuild:
1. **EntityBase** — The base class; all entities ARE resources in the tenant-scoped sense
2. **ResourceTypes** — Authorization identifiers for what TYPE of resource an entity is
3. **ResourceUsageType** — Quota tracking categories (may not map 1:1 to ResourceTypes)

---

## PART A — CURRENT MODULE DEEP ANALYSIS

---

## A.1 GameGuild.Resources Module Analysis

### A.1.1 Purpose & Responsibilities

> **CRITICAL ARCHITECTURAL NOTE:** In GameGuild, "Resource" is a **foundational concept** — ALL tenant-scoped entities (Tenants, Users, Projects, Posts, Courses, Products, etc.) are considered **Resources**. They all inherit from `EntityBase<TKey>` which implements `ITenantScoped`, making every entity a trackable, quota-enforceable, permission-controllable resource. The `GameGuild.Resources` module provides the **horizontal infrastructure** for quota management and usage tracking that applies across all entity types in the system.

**What Resources Module OWNS:**
- Resource quota definitions per tenant (soft/hard limits) for ANY entity type
- Usage tracking and recording across ALL resource types
- Quota enforcement via `ResourceQuotaBehavior` pipeline behavior
- `ResourceUsageType` enum — the registry of all trackable resource categories
- Usage trend analysis and reporting
- Cost allocation and SLA impact analysis
- Throttling policies

**Relationship to Other Modules:**
- **All entity modules** (Users, Projects, Posts, Commerce, etc.) → Resources tracks their quotas
- **Commerce.Subscriptions** → Triggers quota updates via `SubscriptionActivatedEvent`
- **Commerce.Products** → Entitlements may include quota allocations
- **Features** → Feature flags may modify quota enforcement behavior
- **Identity.Authorization** → `ResourceTypes` defines the strongly-typed resource identifiers used in DAC/ABAC

### A.1.2 Current Architecture Map

```
GameGuild.Resources/
├── Entities/
│   ├── ResourceQuota.cs          # Core quota entity with soft/hard limits
│   ├── ResourceMetadata.cs       # Key-value metadata storage
│   ├── UsageRecord.cs            # Time-series usage tracking
│   ├── ResourceSettings.cs       # Tenant-specific settings
│   ├── ResourceThrottlingPolicy.cs
│   ├── ResourceUsageTrend.cs
│   ├── UsageRetentionPolicy.cs
│   ├── CostAllocationReport.cs
│   └── SlaImpactAnalysis.cs
├── Abstractions/
│   ├── IResourceQuotaService.cs  # Core quota operations
│   ├── IResourceQuotaRepository.cs
│   ├── IUsageService.cs
│   └── ... (20+ interfaces)
├── Services/
│   ├── ResourceQuotaService.cs   # Main service implementation
│   ├── CachedResourceQuotaService.cs  # Caching decorator
│   ├── UsageService.cs
│   └── ... (trend analysis, SLA, cost allocation)
├── Behaviors/
│   └── ResourceQuotaBehavior.cs  # CQRS pipeline behavior for quota enforcement
├── Attributes/
│   └── RequiresQuotaAttribute.cs # Declarative quota requirement
├── Events/
│   ├── QuotaChangedEvent.cs
│   └── QuotaExceededEvent.cs
└── Models/
    └── ResourceUsageType.cs      # Enum of trackable resources
```

**Key Classes:**
- `ResourceQuota` - Entity with `TenantId`, `Type`, `SoftLimit`, `HardLimit`, `CurrentUsage`, `RowVersion`
- `ResourceQuotaBehavior<TRequest, TResponse>` - Pipeline behavior that enforces `[RequiresQuota]`
- `IResourceQuotaService` - Core interface with `TryAtomicConsumeAsync()` for thread-safe enforcement
- `ResourceUsageType` - Enum with 23 resource types (Users, Projects, Storage, ApiCalls, etc.)

### A.1.3 Data Flow & Request Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     QUOTA ENFORCEMENT FLOW                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. Command with [RequiresQuota(ResourceUsageType.X)] received          │
│                         │                                               │
│                         ▼                                               │
│  2. ResourceQuotaBehavior intercepts in CQRS pipeline                   │
│     ├─ Validates TenantId present (FAIL-CLOSED if missing)              │
│     └─ Calls TryAtomicConsumeAsync()                                    │
│                         │                                               │
│                         ▼                                               │
│  3. TryAtomicConsumeAsync (optimistic concurrency)                      │
│     ├─ Load quota with RowVersion                                       │
│     ├─ Check if CurrentUsage + Amount <= HardLimit                      │
│     ├─ Increment CurrentUsage                                           │
│     ├─ Save with concurrency check                                      │
│     └─ Retry on DbUpdateConcurrencyException (up to 3 times)            │
│                         │                                               │
│              ┌─────────┴─────────┐                                      │
│              ▼                   ▼                                      │
│         SUCCESS              FAILURE                                    │
│     (proceed to handler)  (throw QuotaExceededException)                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### A.1.4 Tenant Scoping Correctness

| Location | TenantId Enforcement | Status |
|----------|---------------------|--------|
| `ResourceQuota` entity | Inherited from `EntityBase` | ✅ CORRECT |
| `ResourceQuotaBehavior` | Fail-closed check on `Actor.TenantId` | ✅ CORRECT |
| `ResourceQuotaService.SetQuotaAsync` | Explicit tenantId parameter | ✅ CORRECT |
| `TryAtomicConsumeAsync` | Uses repository with tenant filter | ✅ CORRECT |
| `UsageRecord` entity | Inherited from `EntityBase` | ✅ CORRECT |

**Verdict:** ✅ Tenant isolation is correctly enforced throughout.

### A.1.5 Code Quality Review (KISS/DRY/SOLID)

| Principle | Assessment | Notes |
|-----------|------------|-------|
| **KISS** | ✅ GOOD | Simple attribute-based quota enforcement |
| **DRY** | ✅ GOOD | Single `TryAtomicConsumeAsync` for all quota checks |
| **SRP** | ✅ GOOD | Clear separation: Quota vs Usage vs Throttling |
| **OCP** | ✅ GOOD | New resource types added via enum extension |
| **LSP** | ✅ GOOD | `CachedResourceQuotaService` properly decorates |
| **ISP** | ✅ FIXED | Segregated into 5 focused interfaces (see below) |
| **DIP** | ✅ GOOD | All dependencies via interfaces |

**ISP Fix Applied:** The original `IResourceQuotaService` (15+ methods) has been split into:

| Interface | Methods | Purpose |
|-----------|---------|---------|
| `IResourceQuotaReader` | 4 | Read-only quota/usage queries |
| `IResourceQuotaWriter` | 2 | Admin quota configuration (set, delete) |
| `IResourceQuotaEnforcer` | 5 | Consumption and limit enforcement |
| `IResourceQuotaAnalytics` | 2 | Reporting and analytics |
| `IResourceQuotaMaintenance` | 3 | Background maintenance tasks |

The unified `IResourceQuotaService` now inherits from all five interfaces for backward compatibility.
DI registration updated to resolve each segregated interface independently.

**Remaining Code Smells:**
1. ~~`IResourceQuotaService` is a "fat interface" with 15+ methods~~ ✅ FIXED
2. `ResourceUsageType` enum may grow unbounded - consider registration pattern

### A.1.6 Security & Risk Review

| Risk | Severity | Mitigation Status |
|------|----------|-------------------|
| Quota bypass via missing TenantId | HIGH | ✅ Fail-closed in `ResourceQuotaBehavior` |
| Race condition on concurrent consume | HIGH | ✅ `TryAtomicConsumeAsync` with RowVersion |
| Negative usage after decrement | LOW | ✅ `DecrementUsageAsync` clamps to 0 |
| Quota manipulation by tenant | LOW | ✅ Quotas set by subscription events, not user API |

### A.1.7 Performance Review

| Concern | Status | Notes |
|---------|--------|-------|
| N+1 queries | ✅ OK | Single query per quota check |
| Caching | ✅ OK | `CachedResourceQuotaService` decorator |
| Hot path overhead | ⚠️ WATCH | Every `[RequiresQuota]` command hits DB |
| Concurrency retry storms | ✅ OK | Max 3 retries with backoff |

**Recommendation:** Consider read-through cache for `GetQuotaAsync` to reduce DB load.

### A.1.8 Recommended Minimal Refactors

| # | Change | Effort | Priority | Status |
|---|--------|--------|----------|--------|
| 1 | Add `ResourceUsageType.Assets` enum value | 5 min | P0 | ⏳ TODO |
| 2 | Add `ResourceUsageType.AssetStorage` enum value | 5 min | P0 | ⏳ TODO |
| 3 | Add `ResourceUsageType.AssetDownloads` enum value | 5 min | P1 | ⏳ TODO |
| 4 | Split `IResourceQuotaService` into query/command interfaces | 2 hrs | P3 | ✅ DONE |

### A.1.9 Required Tests

| Test Name | Purpose |
|-----------|---------|
| `ResourceQuotaBehavior_RejectsMissingTenantId` | Fail-closed on missing context |
| `TryAtomicConsumeAsync_HandlesConc currentRace` | Concurrent consumption safety |
| `TryAtomicConsumeAsync_RejectsOverLimit` | Hard limit enforcement |
| `DecrementUsageAsync_NeverGoesNegative` | Usage floor at 0 |
| `CachedQuotaService_InvalidatesOnChange` | Cache correctness |
| `QuotaChangedEvent_PublishedOnModification` | Audit trail |

---

## A.2 GameGuild.Features Module Analysis

### A.2.1 Purpose & Responsibilities

**What Features SHOULD Own:**
- Feature flag definitions and lifecycle
- Targeting rules (tenant, user, plan, country, custom)
- Rollout percentages and A/B testing
- Feature evaluation with context
- Kill switches for emergency shutoff
- Feature flag analytics and usage tracking

**What Features MUST NOT Do:**
- Enforce quotas (Resources owns this)
- Grant product entitlements (Commerce.Products owns this)
- Manage subscriptions (Commerce.Subscriptions owns this)

### A.2.2 Current Architecture Map

```
GameGuild.Features/
├── Entities/
│   ├── FeatureFlag.cs            # Core feature flag entity
│   ├── FeatureFlagTarget.cs      # Targeting rules
│   ├── FeatureFlagType.cs        # Toggle, Percentage, Experiment
│   └── FeatureFlagUsage.cs       # Usage analytics
├── Abstractions/
│   ├── IFeatureFlagEvaluationEngine.cs
│   ├── IFeatureFlagManagementService.cs
│   ├── IFeatureFlagConfigurationService.cs
│   ├── IFeatureEvaluationStrategy.cs  # Strategy pattern
│   ├── ITargetingRuleHandler.cs       # Chain of responsibility
│   └── ... (15+ interfaces)
├── Services/
│   ├── FeatureFlagEvaluationService.cs
│   ├── FeatureFlagManagementService.cs
│   ├── Decorators/
│   │   ├── CachedFeatureFlagService.cs
│   │   ├── AnalyticsFeatureFlagService.cs
│   │   └── LoggingFeatureFlagService.cs
│   ├── Strategies/
│   │   ├── SimpleToggleStrategy.cs
│   │   ├── PercentageRolloutStrategy.cs
│   │   └── TargetedEvaluationStrategy.cs
│   └── Handlers/
│       ├── TenantTargetingHandler.cs
│       ├── UserTargetingHandler.cs
│       ├── PlanTargetingHandler.cs
│       └── ... (5 handlers)
├── Models/
│   └── FeatureContext.cs         # Evaluation context
└── Provider/
    └── DatabaseFeatureFlagProvider.cs  # OpenFeature integration
```

**Key Classes:**
- `FeatureFlag` - Entity with `Key`, `IsEnabled`, `Type`, `RolloutPercentage`, `Environment`
- `FeatureContext` - Context with `TenantId`, `UserId`, `SubscriptionPlanId`, `Permissions`
- `IFeatureFlagEvaluationService` - Main evaluation interface
- Decorator chain: Logging → Analytics → Caching → Base evaluation

### A.2.3 Data Flow & Request Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   FEATURE FLAG EVALUATION FLOW                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. EvaluateAsync("feature-key", context) called                        │
│                         │                                               │
│                         ▼                                               │
│  2. Decorator Chain (outside-in):                                       │
│     LoggingFeatureFlagService                                           │
│         └─► AnalyticsFeatureFlagService                                 │
│                 └─► CachedFeatureFlagService                            │
│                         └─► FeatureFlagEvaluationService (base)         │
│                         │                                               │
│                         ▼                                               │
│  3. Base Service:                                                       │
│     ├─ Load FeatureFlag from repository                                 │
│     ├─ Validate environment match                                       │
│     ├─ Select strategy by FeatureFlagType                               │
│     └─ Execute strategy.EvaluateAsync()                                 │
│                         │                                               │
│                         ▼                                               │
│  4. Strategy (e.g., TargetedEvaluationStrategy):                        │
│     ├─ Load targeting rules                                             │
│     ├─ Chain of responsibility: Tenant → User → Plan → Country → Custom │
│     └─ Return FeatureEvaluationResult                                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### A.2.4 Tenant Scoping Correctness

| Location | TenantId Enforcement | Status |
|----------|---------------------|--------|
| `FeatureFlag.TenantId` | Inherited from `EntityBase` | ✅ CORRECT |
| `FeatureContext.TenantId` | Optional with warning log | ⚠️ DEFENSIVE |
| `FeatureFlagEvaluationService` | Logs warning if TenantId missing | ⚠️ DEFENSIVE |
| Targeting rules | Tenant targeting handler checks | ✅ CORRECT |

**Issue Found:** `FeatureContext.TenantId` is nullable. If null, targeting rules that depend on tenant may silently fail-open.

**Verdict:** ⚠️ Defensive but not fail-closed. Evaluation proceeds without tenant context.

### A.2.5 Code Quality Review (KISS/DRY/SOLID)

| Principle | Assessment | Notes |
|-----------|------------|-------|
| **KISS** | ✅ GOOD | Clean decorator + strategy pattern |
| **DRY** | ✅ GOOD | Shared evaluation logic in strategies |
| **SRP** | ✅ EXCELLENT | Each strategy/handler has single purpose |
| **OCP** | ✅ EXCELLENT | New strategies/handlers without modification |
| **LSP** | ✅ GOOD | All decorators properly implement interface |
| **ISP** | ✅ GOOD | Segregated into Evaluation/Management/Configuration |
| **DIP** | ✅ GOOD | All via interfaces |

**Code Quality:** Excellent use of design patterns.

### A.2.6 Security & Risk Review

| Risk | Severity | Mitigation Status |
|------|----------|-------------------|
| Feature leak to wrong tenant | MEDIUM | ⚠️ Warning logged, not blocked |
| Feature bypass via missing context | LOW | ⚠️ Falls back to default value |
| Kill switch circumvention | HIGH | ✅ `IsKillSwitch` property checked first |
| Stale cache serving wrong state | MEDIUM | ✅ Distributed cache with TTL |

### A.2.7 Performance Review

| Concern | Status | Notes |
|---------|--------|-------|
| N+1 on targeting rules | ⚠️ WATCH | Targets loaded per evaluation |
| Caching | ✅ OK | Distributed cache decorator |
| Strategy selection | ✅ OK | O(n) where n = strategy count (small) |
| Hot path (high QPS) | ✅ OK | Cache-first architecture |

### A.2.8 Recommended Minimal Refactors

| # | Change | Effort | Priority |
|---|--------|--------|----------|
| 1 | Add asset-related feature flag keys | 30 min | P1 |
| 2 | Consider fail-closed for missing TenantId in production | 1 hr | P2 |
| 3 | Eager-load targeting rules with feature flag | 30 min | P3 |

### A.2.9 Required Tests

| Test Name | Purpose |
|-----------|---------|
| `EvaluateAsync_LogsWarningWithoutTenantId` | Defensive logging |
| `KillSwitch_OverridesAllTargeting` | Emergency shutoff works |
| `PercentageRollout_DeterministicForSameUser` | Consistent bucketing |
| `CacheDecorator_InvalidatesOnFlagUpdate` | Cache correctness |
| `TargetingChain_PriorityOrderRespected` | Correct handler order |
| `ExpiredFlag_ReturnsDefaultValue` | Expiration handling |

---

## A.3 GameGuild.Localization Module Analysis

### A.3.1 Purpose & Responsibilities

**What Localization SHOULD Own:**
- Language definitions and active languages
- Resource localization (field-level translations)
- Translation workflow management
- Localization context (culture, timezone)
- Localized error messages and UI text
- Culture fallback chains

**What Localization MUST NOT Do:**
- Store business data (other modules own entities)
- Enforce access control (Authorization owns this)
- Define feature availability (Features owns this)

### A.3.2 Current Architecture Map

```
GameGuild.Localization/
├── Models/
│   ├── Language.cs               # Language entity
│   ├── ILocalizable.cs           # Interface for localizable entities
│   ├── LocalizableResource.cs    # Base class for localized resources
│   ├── ResourceLocalization.cs   # Localized field storage
│   └── LocalizationStatus.cs     # Draft, Published, MachineTranslated
├── Abstractions/
│   ├── ILocalizationContext.cs   # Culture/timezone context
│   └── ILanguageRepository.cs    # Language CRUD
├── Services/
│   └── LocalizationContext.cs    # Simple context implementation
├── Repositories/
│   └── LanguageRepository.cs     # Language persistence
├── Translation/
│   ├── TranslationWorkflowService.cs  # Translation lifecycle
│   └── TranslationMemoryService.cs    # Translation memory
└── Extensions/
    └── LocalizationModuleExtensions.cs
```

**Key Classes:**
- `Language` - Entity with `Code` (e.g., "en-US"), `Name`, `IsDefault`, `IsActive`
- `ResourceLocalization` - Stores `ResourceId`, `ResourceType`, `FieldName`, `Content`, `LanguageId`
- `ILocalizable` - Interface for entities that can have localized fields
- `LocalizableResource` - Base class inheriting `EntityBase` + localization support
- `LocalizationContext` - Provides `CurrentCulture`, `CurrentUiCulture`, `CurrentTimeZone`

### A.3.3 Data Flow & Request Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   LOCALIZATION RETRIEVAL FLOW                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. Entity implementing ILocalizable loaded                             │
│                         │                                               │
│                         ▼                                               │
│  2. Access entity.Localizations collection                              │
│     (lazy-loaded via EF Core navigation)                                │
│                         │                                               │
│                         ▼                                               │
│  3. Filter by LanguageId matching user's preferred language             │
│     └─ Fallback: Default language if preferred not found                │
│                         │                                               │
│                         ▼                                               │
│  4. Return localized content for requested field                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### A.3.4 Tenant Scoping Correctness

| Location | TenantId Enforcement | Status |
|----------|---------------------|--------|
| `Language` entity | Inherited from `EntityBase` | ✅ CORRECT |
| `ResourceLocalization` entity | Inherited from `EntityBase` | ✅ CORRECT |
| `LocalizationContext` | No tenant awareness | ⚠️ MISSING |
| Translation workflow | Uses in-memory storage (temp) | ⚠️ NOT PERSISTED |

**Issue Found:** `LocalizationContext` is a simple singleton returning hardcoded defaults. It doesn't consider per-tenant language preferences.

**Verdict:** ⚠️ Partially correct. Entities are tenant-scoped but context is not tenant-aware.

### A.3.5 Code Quality Review (KISS/DRY/SOLID)

| Principle | Assessment | Notes |
|-----------|------------|-------|
| **KISS** | ✅ GOOD | Simple model for field-level localization |
| **DRY** | ⚠️ FAIR | No shared localization resolution helper |
| **SRP** | ✅ GOOD | Clear separation of concerns |
| **OCP** | ⚠️ FAIR | Adding new localizable entity requires code changes |
| **LSP** | ✅ GOOD | N/A (no inheritance hierarchy used) |
| **ISP** | ⚠️ FAIR | Missing interface for error message localization |
| **DIP** | ✅ GOOD | Via interfaces |

**Code Smells Identified:**
1. `LocalizationContext` returns hardcoded "en-US" - should read from request/user preferences
2. `TranslationWorkflowService` uses in-memory dictionaries - not production-ready
3. No service for localizing error messages / system strings

### A.3.6 Security & Risk Review

| Risk | Severity | Mitigation Status |
|------|----------|-------------------|
| Cross-tenant translation leakage | LOW | ✅ Entities inherit TenantId |
| XSS via translated content | MEDIUM | ❌ No sanitization visible |
| Language injection | LOW | ✅ Language codes validated by MaxLength |

### A.3.7 Performance Review

| Concern | Status | Notes |
|---------|--------|-------|
| N+1 on localizations | ⚠️ WATCH | Localizations loaded per entity access |
| No caching | ⚠️ MISSING | Localizations fetched from DB each time |
| Translation memory | ✅ OK | In-memory (but not persistent) |

### A.3.8 Recommended Minimal Refactors

| # | Change | Effort | Priority |
|---|--------|--------|----------|
| 1 | Add `ILocalizedErrorService` for error message localization | 2 hrs | P1 |
| 2 | Make `LocalizationContext` read from request headers | 1 hr | P1 |
| 3 | Add caching for frequently-accessed localizations | 2 hrs | P2 |
| 4 | Add content sanitization in `ResourceLocalization` | 1 hr | P2 |
| 5 | Persist `TranslationWorkflowService` to database | 4 hrs | P3 |

### A.3.9 Required Tests

| Test Name | Purpose |
|-----------|---------|
| `ResourceLocalization_EnforcesTenantIsolation` | No cross-tenant leakage |
| `LocalizableResource_FallsBackToDefaultLanguage` | Fallback behavior |
| `TranslationWorkflow_ProgressesThroughStates` | Workflow correctness |
| `LocalizationContext_ReadsFromRequestHeaders` | Dynamic culture (after fix) |
| `LocalizedContent_SanitizesXSS` | Security (after fix) |

---

## PART B — NEW MODULE DESIGN: GameGuild.Assets

---

## B.1 Module Responsibilities

### What Assets OWNS

| Responsibility | Description |
|----------------|-------------|
| **Asset storage** | S3-compatible blob storage for immutable content |
| **Asset metadata** | Content hash, MIME type, size, dimensions, duration |
| **Asset references** | Many-to-one mapping from references to content |
| **Access URL generation** | Time-windowed, signed path-segment tokens |
| **Transformation pipeline** | On-the-fly image/video transformations |
| **Content moderation** | Virus scanning, auto-moderation, user reports |
| **Deduplication** | Content-hash and perceptual-hash based |
| **Garbage collection** | Reference counting, grace period deletion |
| **Access counting** | Anti-hotlinking rate limiting |

### What Assets MUST NOT Do

| Anti-Pattern | Correct Owner |
|--------------|---------------|
| Grant product entitlements | Commerce.Products |
| Define storage quotas | Resources (via `ResourceUsageType.AssetStorage`) |
| Define feature availability | Features (via feature flags) |
| Authenticate users | Identity.Authentication |
| Authorize access directly | Identity.Authorization (Assets calls into it) |
| Handle payments for downloads | Commerce.Orders |
| Localize error messages | Localization (Assets uses its services) |

---

## B.2 Domain Model

### B.2.1 Core Entities

```csharp
/// <summary>
/// Represents the immutable binary content stored in S3.
/// Multiple AssetReferences can point to the same AssetContent.
/// </summary>
[Table("asset_contents")]
public class AssetContent : EntityBase
{
    /// <summary>SHA-256 hash of the content (primary deduplication key)</summary>
    [Required, MaxLength(64)]
    public string ContentHash { get; init; } = string.Empty;
    
    /// <summary>Perceptual hash for image/video similarity detection</summary>
    [MaxLength(64)]
    public string? PerceptualHash { get; set; }
    
    /// <summary>S3 bucket name</summary>
    [Required, MaxLength(100)]
    public string BucketName { get; init; } = string.Empty;
    
    /// <summary>S3 object key (path within bucket)</summary>
    [Required, MaxLength(500)]
    public string ObjectKey { get; init; } = string.Empty;
    
    /// <summary>MIME type of the content</summary>
    [Required, MaxLength(100)]
    public string MimeType { get; init; } = string.Empty;
    
    /// <summary>Size in bytes</summary>
    public long SizeBytes { get; init; }
    
    /// <summary>Image/video width in pixels (null for documents)</summary>
    public int? Width { get; init; }
    
    /// <summary>Image/video height in pixels (null for documents)</summary>
    public int? Height { get; init; }
    
    /// <summary>Video/audio duration in seconds (null for images/documents)</summary>
    public double? DurationSeconds { get; init; }
    
    /// <summary>Content kind classification</summary>
    public AssetKind Kind { get; init; }
    
    /// <summary>Virus scan status</summary>
    public VirusScanStatus VirusScanStatus { get; set; } = VirusScanStatus.Pending;
    
    /// <summary>Virus scan completed at</summary>
    public DateTime? VirusScanCompletedAt { get; set; }
    
    /// <summary>Moderation status</summary>
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
    
    /// <summary>Moderation completed at</summary>
    public DateTime? ModerationCompletedAt { get; set; }
    
    /// <summary>Auto-moderation labels detected (JSON array)</summary>
    [MaxLength(2000)]
    public string? ModerationLabels { get; set; }
    
    /// <summary>Whether this content can ever be deleted (false for legally-protected)</summary>
    public bool IsDeletable { get; set; } = true;
    
    /// <summary>Reference count for GC eligibility</summary>
    public int ReferenceCount { get; set; } = 0;
    
    /// <summary>When this became eligible for GC (null if still referenced)</summary>
    public DateTime? MarkedForDeletionAt { get; set; }
    
    /// <summary>Row version for concurrency</summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
    
    // Navigation
    public virtual ICollection<AssetReference> References { get; init; } = [];
    public virtual ICollection<TransformedAsset> TransformedVersions { get; init; } = [];
}

/// <summary>
/// Represents a logical reference to asset content.
/// This is what users interact with - the same content can have multiple references.
/// </summary>
[Table("asset_references")]
public class AssetReference : EntityBase
{
    /// <summary>Foreign key to the actual content</summary>
    public Guid AssetContentId { get; set; }
    
    /// <summary>User who created this reference</summary>
    public Guid CreatedByUserId { get; set; }
    
    /// <summary>Human-readable name/title</summary>
    [MaxLength(255)]
    public string? DisplayName { get; set; }
    
    /// <summary>Original filename from upload</summary>
    [MaxLength(255)]
    public string? OriginalFilename { get; set; }
    
    /// <summary>Description (localizable)</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>Alt text for accessibility (localizable)</summary>
    [MaxLength(500)]
    public string? AltText { get; set; }
    
    /// <summary>Access policy for this reference</summary>
    public AssetAccessPolicy AccessPolicy { get; set; } = AssetAccessPolicy.Private;
    
    /// <summary>Parent resource type this asset is attached to</summary>
    [MaxLength(100)]
    public string? ParentResourceType { get; set; }
    
    /// <summary>Parent resource ID</summary>
    public Guid? ParentResourceId { get; set; }
    
    /// <summary>Tags for categorization (JSON array)</summary>
    [MaxLength(500)]
    public string? Tags { get; set; }
    
    /// <summary>Access counter for rate limiting</summary>
    public long AccessCount { get; set; } = 0;
    
    /// <summary>Last access time</summary>
    public DateTime? LastAccessedAt { get; set; }
    
    /// <summary>Download window expiry for paid content</summary>
    public DateTime? DownloadWindowExpiresAt { get; set; }
    
    /// <summary>Order ID that granted download access (for paid content)</summary>
    public Guid? GrantedByOrderId { get; set; }
    
    // Navigation
    public virtual AssetContent Content { get; set; } = null!;
    public virtual ICollection<AssetReport> Reports { get; init; } = [];
}

/// <summary>
/// Cached transformed version of an asset.
/// </summary>
[Table("transformed_assets")]
public class TransformedAsset : EntityBase
{
    /// <summary>Source content ID</summary>
    public Guid SourceContentId { get; set; }
    
    /// <summary>Canonical transformation spec (normalized, sorted params)</summary>
    [Required, MaxLength(500)]
    public string TransformationSpec { get; init; } = string.Empty;
    
    /// <summary>S3 object key for transformed content</summary>
    [Required, MaxLength(500)]
    public string ObjectKey { get; init; } = string.Empty;
    
    /// <summary>Transformed size in bytes</summary>
    public long SizeBytes { get; init; }
    
    /// <summary>Transformed width</summary>
    public int? Width { get; init; }
    
    /// <summary>Transformed height</summary>
    public int? Height { get; init; }
    
    /// <summary>Last accessed (for cache eviction)</summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual AssetContent SourceContent { get; set; } = null!;
}

/// <summary>
/// User report of inappropriate content.
/// </summary>
[Table("asset_reports")]
public class AssetReport : EntityBase
{
    /// <summary>Reported asset reference</summary>
    public Guid AssetReferenceId { get; set; }
    
    /// <summary>User who submitted the report</summary>
    public Guid ReportedByUserId { get; set; }
    
    /// <summary>Report reason category</summary>
    public ReportReason Reason { get; set; }
    
    /// <summary>Additional details</summary>
    [MaxLength(2000)]
    public string? Details { get; set; }
    
    /// <summary>Review status</summary>
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    
    /// <summary>Moderator who reviewed</summary>
    public Guid? ReviewedByUserId { get; set; }
    
    /// <summary>Review decision</summary>
    public ReviewDecision? Decision { get; set; }
    
    /// <summary>Review notes</summary>
    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }
    
    /// <summary>When reviewed</summary>
    public DateTime? ReviewedAt { get; set; }
    
    // Navigation
    public virtual AssetReference Reference { get; set; } = null!;
}
```

### B.2.2 Value Objects & Enums

```csharp
/// <summary>
/// Asset content classification.
/// </summary>
public enum AssetKind
{
    Image = 1,
    Video = 2,
    Audio = 3,
    Document = 4,
    Archive = 5,
    Other = 99
}

/// <summary>
/// Access policy for asset references.
/// </summary>
public enum AssetAccessPolicy
{
    /// <summary>Only owner and admins can access</summary>
    Private = 0,
    
    /// <summary>Accessible via short-lived signed URLs (default for ephemeral)</summary>
    SignedUrl = 1,
    
    /// <summary>Accessible to all authenticated users in tenant</summary>
    TenantPublic = 2,
    
    /// <summary>Publicly accessible (use with caution)</summary>
    Public = 3,
    
    /// <summary>Requires purchase/entitlement to access</summary>
    PaidContent = 4
}

/// <summary>
/// Virus scan status.
/// </summary>
public enum VirusScanStatus
{
    Pending = 0,
    Scanning = 1,
    Clean = 2,
    Infected = 3,
    ScanFailed = 4
}

/// <summary>
/// Content moderation status.
/// </summary>
public enum ModerationStatus
{
    Pending = 0,
    Processing = 1,
    Approved = 2,
    Rejected = 3,
    NeedsReview = 4,
    ApprovedWithWarning = 5
}

/// <summary>
/// Report reason categories.
/// </summary>
public enum ReportReason
{
    Inappropriate = 1,
    Copyright = 2,
    Spam = 3,
    Violence = 4,
    Harassment = 5,
    Misinformation = 6,
    Other = 99
}

/// <summary>
/// Report review status.
/// </summary>
public enum ReportStatus
{
    Pending = 0,
    UnderReview = 1,
    Resolved = 2,
    Dismissed = 3
}

/// <summary>
/// Moderator review decision.
/// </summary>
public enum ReviewDecision
{
    NoAction = 0,
    ContentRemoved = 1,
    ContentHidden = 2,
    UserWarned = 3,
    UserSuspended = 4
}

/// <summary>
/// Strongly-typed transformation specification.
/// </summary>
public sealed record TransformationSpec
{
    public int? Width { get; init; }
    public int? Height { get; init; }
    public ImageFit? Fit { get; init; }
    public int? Quality { get; init; }
    public ImageFormat? Format { get; init; }
    public bool? Blur { get; init; }
    public int? BlurRadius { get; init; }
    public bool? Grayscale { get; init; }
    
    /// <summary>
    /// Returns a canonical, sorted string representation for cache keying.
    /// </summary>
    public string ToCanonicalString()
    {
        var parts = new List<string>();
        if (Width.HasValue) parts.Add($"w={Width}");
        if (Height.HasValue) parts.Add($"h={Height}");
        if (Fit.HasValue) parts.Add($"fit={Fit.ToString()!.ToLowerInvariant()}");
        if (Quality.HasValue) parts.Add($"q={Quality}");
        if (Format.HasValue) parts.Add($"f={Format.ToString()!.ToLowerInvariant()}");
        if (Blur == true) parts.Add($"blur={BlurRadius ?? 10}");
        if (Grayscale == true) parts.Add("gray=1");
        parts.Sort(StringComparer.Ordinal);
        return string.Join(",", parts);
    }
    
    public static TransformationSpec Parse(string spec)
    {
        // Parse "w=100,h=200,fit=cover,q=80" format
        var result = new TransformationSpec();
        // ... parsing logic
        return result;
    }
}

public enum ImageFit { Contain, Cover, Fill, Inside, Outside }
public enum ImageFormat { Jpeg, Png, Webp, Avif, Gif }
```

---

## B.3 API Design

### B.3.1 Public Endpoints

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        ASSET PUBLIC API                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  UPLOAD                                                                 │
│  ──────                                                                 │
│  POST   /api/assets/upload                                              │
│         → Multipart upload, returns AssetReferenceId                    │
│         → Triggers: hash → virus scan → moderation → storage            │
│                                                                         │
│  POST   /api/assets/upload/chunked/init                                 │
│         → Initialize chunked upload for large files                     │
│                                                                         │
│  POST   /api/assets/upload/chunked/{uploadId}/part                      │
│         → Upload chunk                                                  │
│                                                                         │
│  POST   /api/assets/upload/chunked/{uploadId}/complete                  │
│         → Complete chunked upload                                       │
│                                                                         │
│  ACCESS URL GENERATION                                                  │
│  ─────────────────────                                                  │
│  POST   /api/assets/{referenceId}/access-url                            │
│         → Generate signed access URL with optional transformation       │
│         → Body: { transformation?: TransformationSpec }                 │
│         → Returns: { url, expiresAt }                                   │
│                                                                         │
│  METADATA                                                               │
│  ────────                                                               │
│  GET    /api/assets/{referenceId}                                       │
│         → Get asset reference metadata (no content)                     │
│                                                                         │
│  PATCH  /api/assets/{referenceId}                                       │
│         → Update display name, description, alt text, tags              │
│                                                                         │
│  DELETE /api/assets/{referenceId}                                       │
│         → Delete reference (decrements content ref count)               │
│                                                                         │
│  REPORTING                                                              │
│  ─────────                                                              │
│  POST   /api/assets/{referenceId}/report                                │
│         → Submit content report                                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.3.2 Admin Endpoints

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        ASSET ADMIN API                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  MODERATION                                                             │
│  ──────────                                                             │
│  GET    /api/admin/assets/moderation/queue                              │
│         → List assets pending moderation review                         │
│                                                                         │
│  POST   /api/admin/assets/{contentId}/moderation/review                 │
│         → Submit moderation decision                                    │
│                                                                         │
│  GET    /api/admin/assets/reports                                       │
│         → List user reports                                             │
│                                                                         │
│  POST   /api/admin/assets/reports/{reportId}/review                     │
│         → Review and resolve report                                     │
│                                                                         │
│  MAINTENANCE                                                            │
│  ───────────                                                            │
│  POST   /api/admin/assets/gc/run                                        │
│         → Trigger garbage collection (normally scheduled)               │
│                                                                         │
│  GET    /api/admin/assets/gc/candidates                                 │
│         → List assets eligible for GC                                   │
│                                                                         │
│  POST   /api/admin/assets/{contentId}/undeletable                       │
│         → Mark asset as non-deletable                                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.3.3 Asset Serving Endpoints (CDN-facing)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     ASSET SERVING ROUTES                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ROUTE TEMPLATES                                                        │
│  ───────────────                                                        │
│                                                                         │
│  1. Direct asset access (with signed token):                            │
│     /assets/{referenceId}/{token}                                       │
│     Example: /assets/550e8400-e29b-41d4-a716-446655440000/a1b2c3d4e5f6  │
│                                                                         │
│  2. Ephemeral access (short token, high rotation):                      │
│     /e/{token}                                                          │
│     Example: /e/xY7kL9mN2pQ4rS6t                                        │
│     → Token encodes: referenceId + window + expiry                      │
│                                                                         │
│  3. Transformation access:                                              │
│     /t/{transformation}/{referenceId}/{token}                           │
│     Example: /t/w=200,h=200,fit=cover/550e.../a1b2c3d4e5f6              │
│     → Transformation in path, not query params                          │
│                                                                         │
│  FLOW: CDN → Access Control → Transformation → Fetch → Storage          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## B.4 URL Design & Token Computation

### B.4.1 Token Structure

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        TOKEN COMPUTATION                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  INPUT COMPONENTS:                                                      │
│  ─────────────────                                                      │
│  • assetReferenceId: Guid                                               │
│  • timeWindow: int (window index based on rotation)                     │
│  • expiryTimestamp: long (Unix seconds)                                 │
│  • accessPolicy: enum (serialized as int)                               │
│  • transformationSpec: string (canonical form, empty if none)           │
│  • tenantId: Guid                                                       │
│  • secretKey: byte[] (per-tenant signing key, rotated monthly)          │
│                                                                         │
│  COMPUTATION:                                                           │
│  ────────────                                                           │
│  payload = $"{assetReferenceId}|{timeWindow}|{expiryTimestamp}|" +      │
│            $"{(int)accessPolicy}|{transformationSpec}|{tenantId}"       │
│                                                                         │
│  signature = HMAC-SHA256(secretKey, payload)                            │
│  token = Base64UrlEncode(signature[0..16])  // First 16 bytes           │
│                                                                         │
│  RESULT: 22-character URL-safe token                                    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.4.2 Movable Time Window (Anti-Stampede)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   MOVABLE TIME WINDOW ROTATION                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  PROBLEM:                                                               │
│  If all tokens expire at midnight, CDN cache invalidates simultaneously │
│  causing a "thundering herd" / stampede on origin.                      │
│                                                                         │
│  SOLUTION: Overlapping 24-hour windows that rotate every 8 hours        │
│                                                                         │
│  TIMELINE:                                                              │
│  ───────────────────────────────────────────────────────────────────    │
│  Hour:  0    8    16   24   32   40   48                                │
│         │    │    │    │    │    │    │                                 │
│  W1:    ├────────────────────┤                                          │
│  W2:         ├────────────────────┤                                     │
│  W3:              ├────────────────────┤                                │
│  W4:                   ├────────────────────┤                           │
│         │    │    │    │    │    │    │                                 │
│                                                                         │
│  At any time, tokens from TWO windows are valid:                        │
│  - Current window (e.g., W2)                                            │
│  - Previous window (e.g., W1)                                           │
│                                                                         │
│  TOKEN GENERATION:                                                      │
│  timeWindow = floor(currentHour / 8)                                    │
│  expiryTimestamp = windowStart + 24 hours                               │
│                                                                         │
│  TOKEN VALIDATION:                                                      │
│  Accept if token.timeWindow == currentWindow                            │
│          OR token.timeWindow == currentWindow - 1                       │
│  AND token.expiryTimestamp > now                                        │
│                                                                         │
│  CDN CACHE:                                                             │
│  Cache-Control: max-age=28800 (8 hours)                                 │
│  Vary: (none - token in path, not header)                               │
│                                                                         │
│  BENEFITS:                                                              │
│  ✓ 8-hour CDN cache effectiveness                                       │
│  ✓ Graceful rotation without stampede                                   │
│  ✓ No query parameters (CDN-friendly)                                   │
│  ✓ Tokens remain valid during transition                                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## B.5 Enforcement Model

### B.5.1 Authorization Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│              AUTHORIZATION AT URL GENERATION (NOT CDN)                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  CLIENT                                                                 │
│     │                                                                   │
│     ▼                                                                   │
│  POST /api/assets/{referenceId}/access-url                              │
│     │                                                                   │
│     ▼                                                                   │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                    AssetAccessService                            │    │
│  │                                                                  │    │
│  │  1. FAIL-CLOSED: Validate TenantId present                       │    │
│  │     └─ if (!Actor.TenantId) throw UnauthorizedException          │    │
│  │                                                                  │    │
│  │  2. Load AssetReference (with tenant filter)                     │    │
│  │     └─ if (ref.TenantId != Actor.TenantId) throw NotFound        │    │
│  │                                                                  │    │
│  │  3. Check access policy:                                         │    │
│  │     ├─ Private: IsOwner OR HasPermission("assets:admin")         │    │
│  │     ├─ SignedUrl: IsOwner OR TenantMember                        │    │
│  │     ├─ TenantPublic: TenantMember                                │    │
│  │     ├─ PaidContent: HasEntitlement(parentResourceId)             │    │
│  │     │              OR DownloadWindowValid(orderId)               │    │
│  │     └─ Public: Always allowed                                    │    │
│  │                                                                  │    │
│  │  4. Check feature entitlement (Features module):                 │    │
│  │     ├─ EvaluateAsync("asset:transformations:enabled")            │    │
│  │     ├─ EvaluateAsync("asset:download:window:hours")              │    │
│  │     └─ EvaluateAsync("asset:hotlink:limit:per:hour")             │    │
│  │                                                                  │    │
│  │  5. Generate signed token                                        │    │
│  │                                                                  │    │
│  │  6. Return URL with embedded token                               │    │
│  │                                                                  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.5.2 Access Control at CDN Origin

```
┌─────────────────────────────────────────────────────────────────────────┐
│              ACCESS CONTROL SERVICE (CDN ORIGIN)                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  REQUEST: GET /e/{token} or /assets/{refId}/{token}                     │
│     │                                                                   │
│     ▼                                                                   │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                  AssetServeMiddleware                            │    │
│  │                                                                  │    │
│  │  1. Parse token from path                                        │    │
│  │     └─ if (!token) return 401 Unauthorized                       │    │
│  │                                                                  │    │
│  │  2. Decode token → extract referenceId, timeWindow, expiry       │    │
│  │     └─ if (decode fails) return 403 Forbidden                    │    │
│  │                                                                  │    │
│  │  3. Validate time window:                                        │    │
│  │     ├─ currentWindow = floor(nowHour / 8)                        │    │
│  │     ├─ if (token.window != current && token.window != current-1) │    │
│  │     │     return 403 Token Expired                               │    │
│  │     └─ if (token.expiry < now) return 403 Token Expired          │    │
│  │                                                                  │    │
│  │  4. Recompute signature with same inputs + tenant secret         │    │
│  │     └─ if (recomputed != token.signature) return 403 Invalid     │    │
│  │                                                                  │    │
│  │  5. Check access counter (anti-hotlinking):                      │    │
│  │     ├─ Load AssetReference                                       │    │
│  │     ├─ if (ref.AccessCount > hourlyLimit) return 429 Rate Limit  │    │
│  │     └─ Increment AccessCount (async, non-blocking)               │    │
│  │                                                                  │    │
│  │  6. Check content moderation status:                             │    │
│  │     └─ if (content.ModerationStatus == Rejected) return 451      │    │
│  │                                                                  │    │
│  │  7. Proceed to serve or transform                                │    │
│  │                                                                  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.5.3 Fail-Closed Rules

| Condition | Behavior |
|-----------|----------|
| Missing TenantId in API request | 401 Unauthorized |
| Asset not found for tenant | 404 Not Found |
| Token missing in serve path | 401 Unauthorized |
| Token decoding fails | 403 Forbidden |
| Token signature mismatch | 403 Forbidden |
| Token time window invalid | 403 Forbidden (Expired) |
| Access counter exceeded | 429 Too Many Requests |
| Content failed moderation | 451 Unavailable For Legal Reasons |
| Content virus detected | 451 Unavailable For Legal Reasons |
| Feature flag disabled | 403 Feature Not Available |

---

## B.6 Integration Points

### B.6.1 Authorization Module Integration

```csharp
// Assets uses DAC for resource-level permissions
public class AssetAuthorizationHandler : IAssetAuthorizationHandler
{
    private readonly IAuthorizationService _authorizationService;
    
    public async Task<bool> CanAccessAsync(
        Guid userId, 
        AssetReference reference,
        CancellationToken ct)
    {
        // Check resource-level permission via DAC
        var result = await _authorizationService.AuthorizeAsync(
            userId,
            reference,
            new AssetAccessRequirement(reference.AccessPolicy));
            
        return result.Succeeded;
    }
}

// New permission keys for Assets
public static class AssetsPermission
{
    public static class Keys
    {
        public const string Read = "assets:read";
        public const string Create = "assets:create";
        public const string Update = "assets:update";
        public const string Delete = "assets:delete";
        public const string Admin = "assets:admin";
        public const string Moderate = "assets:moderate";
        public const string ViewReports = "assets:reports:view";
        public const string ReviewReports = "assets:reports:review";
    }
}

// REQUIRED: Add to ResourceTypes.cs in Identity.Authorization
// This registers Asset as a first-class resource type for DAC/ABAC
public static class ResourceTypes
{
    // ... existing types ...
    
    /// <summary>Asset resource type (binary content and references)</summary>
    public static readonly ConcreteResourceType Asset = new("Asset", "Binary assets and media files");
    
    /// <summary>Asset report resource type (user content reports)</summary>
    public static readonly ConcreteResourceType AssetReport = new("AssetReport", "Content moderation reports");
    
    // Update the All array:
    public static readonly IReadOnlyList<ResourceType> All = new ResourceType[]
    {
        // ... existing types ...,
        Asset, AssetReport  // Add these
    };
}
```

### B.6.2 Resources Module Integration

> **Note:** Since ALL entities are Resources (inherit `EntityBase` → `ITenantScoped`), the Assets module's entities (`AssetContent`, `AssetReference`, etc.) automatically participate in the resource ecosystem. This section covers the **quota tracking** aspects specifically.

```csharp
// Add to ResourceUsageType enum
public enum ResourceUsageType
{
    // ... existing types ...
    
    /// <summary>Number of asset references per tenant</summary>
    Assets = 24,
    
    /// <summary>Total asset storage in bytes per tenant</summary>
    AssetStorage = 25,
    
    /// <summary>Asset downloads per period</summary>
    AssetDownloads = 26,
    
    /// <summary>Asset transformations per period</summary>
    AssetTransformations = 27
}

// Usage in upload command
[RequiresQuota(ResourceUsageType.Assets, 1)]
[RequiresQuota(ResourceUsageType.AssetStorage, /* calculated from file size */)]
public record UploadAssetCommand(...) : ICommand<Guid>;
```

### B.6.3 Features Module Integration

```csharp
// Feature flags for asset capabilities
public static class AssetFeatureFlags
{
    /// <summary>Enable/disable asset transformations for tenant</summary>
    public const string TransformationsEnabled = "asset:transformations:enabled";
    
    /// <summary>Allowed transformation operations (JSON array)</summary>
    public const string AllowedTransformations = "asset:transformations:allowed";
    
    /// <summary>Max transformation dimensions</summary>
    public const string MaxTransformDimension = "asset:transform:max:dimension";
    
    /// <summary>Download window hours for paid content</summary>
    public const string DownloadWindowHours = "asset:download:window:hours";
    
    /// <summary>Hourly access limit per asset (anti-hotlink)</summary>
    public const string HotlinkLimitPerHour = "asset:hotlink:limit:per:hour";
    
    /// <summary>Enable perceptual hash deduplication</summary>
    public const string PerceptualDedupEnabled = "asset:dedup:perceptual:enabled";
    
    /// <summary>Quality threshold for "replace with better" feature</summary>
    public const string QualityUpgradeThreshold = "asset:quality:upgrade:threshold";
}

// Usage in service
public class AssetTransformationService
{
    private readonly IFeatureFlagEvaluationService _features;
    
    public async Task<bool> CanTransformAsync(FeatureContext ctx, TransformationSpec spec)
    {
        var enabled = await _features.EvaluateAsync(
            AssetFeatureFlags.TransformationsEnabled, ctx);
            
        if (!enabled.IsEnabled) return false;
        
        var maxDim = await _features.EvaluateAsync(
            AssetFeatureFlags.MaxTransformDimension, ctx);
            
        var maxAllowed = int.Parse(maxDim.Value ?? "2000");
        return (spec.Width ?? 0) <= maxAllowed && (spec.Height ?? 0) <= maxAllowed;
    }
}
```

### B.6.4 Localization Module Integration

```csharp
// Localized error messages for asset operations
public interface IAssetLocalizationService
{
    string GetModerationRejectionReason(string[] labels, string languageCode);
    string GetAccessDeniedMessage(AssetAccessPolicy policy, string languageCode);
    string GetQuotaExceededMessage(ResourceUsageType type, string languageCode);
    string GetReportReasonLabel(ReportReason reason, string languageCode);
    string GetContentWarningMessage(string[] moderationLabels, string languageCode);
}

// Asset metadata localization via ILocalizable
public class AssetReference : EntityBase, ILocalizable
{
    // ... existing properties ...
    
    public ICollection<ResourceLocalization> Localizations { get; set; } = [];
    
    public ResourceLocalization AddLocalization(
        string fieldName, 
        string content, 
        Language language,
        LocalizationStatus status = LocalizationStatus.Draft)
    {
        var localization = new ResourceLocalization
        {
            ResourceId = Id,
            ResourceType = "AssetReference",
            FieldName = fieldName,
            Content = content,
            LanguageId = language.Id,
            Status = status
        };
        Localizations.Add(localization);
        return localization;
    }
}
```

### B.6.5 Commerce Module Integration

```csharp
// Paid content download window via Orders
public class AssetDownloadWindowService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IFeatureFlagEvaluationService _features;
    
    public async Task<(bool IsValid, DateTime? ExpiresAt)> ValidateDownloadWindowAsync(
        Guid userId,
        AssetReference asset,
        FeatureContext ctx,
        CancellationToken ct)
    {
        if (asset.AccessPolicy != AssetAccessPolicy.PaidContent)
            return (true, null);
            
        if (!asset.GrantedByOrderId.HasValue)
            return (false, null);
            
        // Check if order is fulfilled
        var order = await _orderRepository.GetByIdAsync(asset.GrantedByOrderId.Value, ct);
        if (order?.Status != OrderStatus.Fulfilled)
            return (false, null);
            
        // Get download window hours from feature flag
        var windowResult = await _features.EvaluateAsync(
            AssetFeatureFlags.DownloadWindowHours, ctx);
        var windowHours = int.Parse(windowResult.Value ?? "72"); // Default 72 hours
        
        var windowStart = order.FulfilledAt ?? order.PaidAt ?? order.CreatedAt;
        var expiresAt = windowStart.AddHours(windowHours);
        
        return (DateTime.UtcNow < expiresAt, expiresAt);
    }
}
```

---

## B.7 Storage & Caching Architecture

### B.7.1 S3-Compatible Storage Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     S3 BUCKET STRUCTURE                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  BUCKET: gameguild-assets-{environment}                                 │
│  ├── originals/                                                         │
│  │   ├── {tenant-id}/                                                   │
│  │   │   ├── {year}/{month}/                                            │
│  │   │   │   └── {content-hash}.{ext}                                   │
│  │   │   │       Example: abc123def456.jpg                              │
│  │   │   │                                                              │
│  │   └── global/  (for platform-wide assets)                            │
│  │                                                                      │
│  ├── transformed/                                                       │
│  │   ├── {tenant-id}/                                                   │
│  │   │   └── {content-hash}/{transformation-hash}.{ext}                 │
│  │   │       Example: abc123def456/w200h200cover.webp                   │
│  │   │                                                                  │
│  ├── quarantine/  (virus-detected, pending review)                      │
│  │   └── {tenant-id}/{content-hash}.{ext}                               │
│  │                                                                      │
│  └── pending/  (upload in progress, not yet scanned)                    │
│      └── {upload-id}/{chunk-index}                                      │
│                                                                         │
│  LIFECYCLE RULES:                                                       │
│  • pending/* → Delete after 24 hours                                    │
│  • quarantine/* → Delete after 30 days                                  │
│  • transformed/* → Move to Glacier after 90 days of no access           │
│  • originals/* → Never auto-delete (GC handles)                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.7.2 CDN Caching Rules

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       CDN CACHING STRATEGY                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  PATH PATTERN                  CACHE-CONTROL              TTL           │
│  ──────────────────────────────────────────────────────────────────     │
│  /assets/{id}/{token}          public, max-age=28800      8 hours       │
│  /e/{token}                    public, max-age=28800      8 hours       │
│  /t/{spec}/{id}/{token}        public, max-age=28800      8 hours       │
│  /api/*                        no-store                   0             │
│                                                                         │
│  VARY HEADERS:                                                          │
│  • None for asset paths (token embeds all variant info)                 │
│  • Accept-Encoding (automatic by CDN)                                   │
│                                                                         │
│  CACHE KEY:                                                             │
│  • Full path including token                                            │
│  • No query parameters (by design)                                      │
│                                                                         │
│  CACHE INVALIDATION:                                                    │
│  • By content hash (when content deleted)                               │
│  • By tenant prefix (when tenant deleted)                               │
│  • Manual purge API for emergency                                       │
│                                                                         │
│  CDN PROVIDER CONFIGURATION:                                            │
│  • Origin: assets.gameguild.com (internal)                              │
│  • Origin Shield: Enabled (reduce origin load)                          │
│  • Compression: Enabled for text-based assets                           │
│  • HTTP/2 + HTTP/3: Enabled                                             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.7.3 Transformed Asset Caching

```csharp
public class TransformationCacheService
{
    private readonly IS3Client _s3;
    private readonly ITransformedAssetRepository _repo;
    
    public async Task<TransformedAsset?> GetOrCreateAsync(
        Guid contentId,
        TransformationSpec spec,
        CancellationToken ct)
    {
        var canonicalSpec = spec.ToCanonicalString();
        
        // Check DB cache
        var cached = await _repo.GetByContentAndSpecAsync(contentId, canonicalSpec, ct);
        if (cached != null)
        {
            // Update last accessed (async, non-blocking)
            _ = _repo.TouchLastAccessedAsync(cached.Id, ct);
            return cached;
        }
        
        // Transform on-the-fly
        var content = await _repo.GetContentByIdAsync(contentId, ct);
        var transformed = await TransformAsync(content, spec, ct);
        
        // Store in S3
        var objectKey = $"transformed/{content.TenantId}/{content.ContentHash}/{spec.ToHash()}.{spec.Format}";
        await _s3.PutObjectAsync(objectKey, transformed.Stream, ct);
        
        // Store metadata in DB
        var entity = new TransformedAsset
        {
            SourceContentId = contentId,
            TransformationSpec = canonicalSpec,
            ObjectKey = objectKey,
            SizeBytes = transformed.SizeBytes,
            Width = transformed.Width,
            Height = transformed.Height
        };
        
        return await _repo.CreateAsync(entity, ct);
    }
}
```

---

## B.8 Upload Pipeline

### B.8.1 Complete Upload Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       ASSET UPLOAD PIPELINE                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  CLIENT                                                                 │
│     │                                                                   │
│     ▼                                                                   │
│  1. POST /api/assets/upload (multipart)                                 │
│     │                                                                   │
│     ▼                                                                   │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  2. UPLOAD HANDLER                                              │    │
│  │     ├─ Validate file size against quota                          │    │
│  │     ├─ Validate MIME type against allowed list                   │    │
│  │     ├─ Stream to temp S3 location (pending/)                     │    │
│  │     └─ Return upload receipt (not yet usable)                    │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                         │                                               │
│                         ▼                                               │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  3. HASH COMPUTATION (async worker)                             │    │
│  │     ├─ Compute SHA-256 content hash                              │    │
│  │     ├─ Compute perceptual hash (images/videos)                   │    │
│  │     └─ Check for exact duplicate by content hash                 │    │
│  │                                                                  │    │
│  │     IF DUPLICATE:                                                │    │
│  │     ├─ Reuse existing AssetContent                               │    │
│  │     ├─ Create new AssetReference pointing to it                  │    │
│  │     ├─ Increment ReferenceCount                                  │    │
│  │     ├─ Delete pending upload                                     │    │
│  │     └─ DONE (skip virus/moderation - already passed)             │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                         │ (if not duplicate)                            │
│                         ▼                                               │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  4. VIRUS SCAN                                                  │    │
│  │     ├─ Send to virus scanning service (ClamAV / commercial)     │    │
│  │     │                                                           │    │
│  │     ├─ IF INFECTED:                                             │    │
│  │     │   ├─ Move to quarantine/                                  │    │
│  │     │   ├─ Set VirusScanStatus = Infected                       │    │
│  │     │   ├─ Notify admin                                         │    │
│  │     │   └─ REJECT upload                                        │    │
│  │     │                                                           │    │
│  │     └─ IF CLEAN: Set VirusScanStatus = Clean, continue          │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                         │                                               │
│                         ▼                                               │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  5. AUTO MODERATION                                             │    │
│  │     ├─ Send to moderation service (AWS Rekognition / custom)    │    │
│  │     │                                                           │    │
│  │     ├─ IF REJECTED (high confidence NSFW, violence, etc.):      │    │
│  │     │   ├─ Set ModerationStatus = Rejected                      │    │
│  │     │   ├─ Store ModerationLabels                               │    │
│  │     │   └─ REJECT upload                                        │    │
│  │     │                                                           │    │
│  │     ├─ IF NEEDS_REVIEW (low confidence):                        │    │
│  │     │   ├─ Set ModerationStatus = NeedsReview                   │    │
│  │     │   ├─ Add to moderation queue                              │    │
│  │     │   └─ Asset accessible but flagged                         │    │
│  │     │                                                           │    │
│  │     └─ IF APPROVED: Set ModerationStatus = Approved             │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                         │                                               │
│                         ▼                                               │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │  6. FINALIZATION                                                │    │
│  │     ├─ Move from pending/ to originals/                         │    │
│  │     ├─ Create AssetContent entity                               │    │
│  │     ├─ Create AssetReference entity                             │    │
│  │     ├─ Increment ReferenceCount = 1                             │    │
│  │     ├─ Record quota usage (Resources module)                    │    │
│  │     └─ Emit AssetUploadedEvent                                  │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                         │                                               │
│                         ▼                                               │
│  DONE - Asset ready for access                                          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### B.8.2 Idempotency Strategy

```csharp
public class AssetUploadService
{
    /// <summary>
    /// Idempotency is achieved via content hash deduplication.
    /// Same file uploaded twice = same AssetContent, different AssetReferences.
    /// </summary>
    public async Task<UploadResult> UploadAsync(
        UploadRequest request,
        Stream content,
        CancellationToken ct)
    {
        // 1. Compute content hash while streaming to temp storage
        var (tempKey, contentHash, perceptualHash, metadata) = 
            await StreamToTempWithHashAsync(content, ct);
        
        // 2. Check for exact duplicate
        var existing = await _contentRepo.GetByHashAsync(
            request.TenantId, contentHash, ct);
            
        if (existing != null)
        {
            // 3. Reuse existing content, create new reference
            await DeleteTempAsync(tempKey, ct);
            
            var reference = await CreateReferenceAsync(
                existing.Id, request, ct);
                
            await _contentRepo.IncrementReferenceCountAsync(
                existing.Id, ct);
                
            return UploadResult.DuplicateReused(reference.Id, existing.Id);
        }
        
        // 4. Check for perceptual duplicate (if enabled)
        if (perceptualHash != null)
        {
            var similar = await _contentRepo.FindByPerceptualHashAsync(
                request.TenantId, perceptualHash, threshold: 0.95, ct);
                
            if (similar != null && ShouldReplaceWithBetter(similar, metadata))
            {
                // Replace lower quality with higher quality
                await ReplaceContentAsync(similar, tempKey, metadata, ct);
                var reference = await CreateReferenceAsync(similar.Id, request, ct);
                return UploadResult.QualityUpgraded(reference.Id, similar.Id);
            }
        }
        
        // 5. New content - proceed with full pipeline
        return await ProcessNewContentAsync(tempKey, request, metadata, ct);
    }
    
    private bool ShouldReplaceWithBetter(AssetContent existing, AssetMetadata newMeta)
    {
        // Only replace if new is significantly better
        // e.g., higher resolution, better bitrate
        if (existing.Width.HasValue && newMeta.Width.HasValue)
        {
            var existingPixels = existing.Width.Value * (existing.Height ?? 1);
            var newPixels = newMeta.Width.Value * (newMeta.Height ?? 1);
            
            // New must be at least 50% more pixels
            return newPixels > existingPixels * 1.5;
        }
        
        return false;
    }
}
```

---

## B.9 Deletion & Garbage Collection Model

### B.9.1 Reference Counting

```csharp
public class AssetReferenceService
{
    /// <summary>
    /// Delete an asset reference. If this was the last reference,
    /// mark content for garbage collection.
    /// </summary>
    public async Task DeleteReferenceAsync(Guid referenceId, CancellationToken ct)
    {
        var reference = await _repo.GetByIdAsync(referenceId, ct);
        if (reference == null) return;
        
        // Decrement reference count atomically
        var (newCount, contentId) = await _contentRepo.DecrementReferenceCountAsync(
            reference.AssetContentId, ct);
        
        // Delete the reference
        await _repo.DeleteAsync(referenceId, ct);
        
        // Decrement storage quota
        var content = await _contentRepo.GetByIdAsync(contentId, ct);
        await _quotaService.DecrementUsageAsync(
            reference.TenantId!.Value,
            ResourceUsageType.AssetStorage,
            content?.SizeBytes ?? 0,
            ct);
        
        // If no more references, mark for GC
        if (newCount == 0)
        {
            await _contentRepo.MarkForDeletionAsync(contentId, DateTime.UtcNow, ct);
            _logger.LogInformation(
                "Asset content {ContentId} marked for GC (zero references)",
                contentId);
        }
    }
}
```

### B.9.2 Garbage Collector Worker

```csharp
public class AssetGarbageCollectorWorker : BackgroundService
{
    private readonly TimeSpan _gracePeriod = TimeSpan.FromDays(30);
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(6);
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunGarbageCollectionAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Asset GC failed");
            }
            
            await Task.Delay(_runInterval, ct);
        }
    }
    
    private async Task RunGarbageCollectionAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - _gracePeriod;
        
        // Find content marked for deletion past grace period
        var candidates = await _contentRepo.GetMarkedForDeletionBeforeAsync(
            cutoff, limit: 100, ct);
        
        foreach (var content in candidates)
        {
            // Double-check: still zero references and deletable?
            if (content.ReferenceCount > 0)
            {
                // Someone created a new reference during grace period
                await _contentRepo.ClearDeletionMarkAsync(content.Id, ct);
                continue;
            }
            
            if (!content.IsDeletable)
            {
                _logger.LogWarning(
                    "Skipping non-deletable content {ContentId}", content.Id);
                continue;
            }
            
            // Delete from S3
            await _s3.DeleteObjectAsync(content.BucketName, content.ObjectKey, ct);
            
            // Delete transformed versions
            foreach (var transformed in content.TransformedVersions)
            {
                await _s3.DeleteObjectAsync(content.BucketName, transformed.ObjectKey, ct);
            }
            
            // Delete from DB
            await _contentRepo.PermanentDeleteAsync(content.Id, ct);
            
            _logger.LogInformation(
                "GC deleted asset content {ContentId} ({SizeBytes} bytes)",
                content.Id, content.SizeBytes);
        }
    }
}
```

---

## B.10 Threat Model & Mitigations

| # | Threat | Severity | Attack Vector | Mitigation | Status |
|---|--------|----------|---------------|------------|--------|
| 1 | **Hotlinking / Bandwidth Abuse** | HIGH | Embed asset URL in high-traffic external site | Access counter with hourly rate limit per asset; Feature flag `asset:hotlink:limit:per:hour` | ✅ DESIGNED |
| 2 | **Token Replay Attack** | HIGH | Capture token, use after intended session | Time-window rotation (8hr windows, 24hr validity); Token tied to tenant secret | ✅ DESIGNED |
| 3 | **Path Token Brute Force** | MEDIUM | Guess tokens to access assets | HMAC-SHA256 with 128-bit truncation = 2^128 attempts; Rate limiting on 403s | ✅ DESIGNED |
| 4 | **CDN Cache Poisoning** | MEDIUM | Trick CDN into caching malicious response | Canonical URL signing; No query params; Signature validation before cache | ✅ DESIGNED |
| 5 | **Transformation Downgrade** | LOW | Request tiny/low-quality to waste resources | Whitelist allowed transformations per asset kind; Max dimension limits | ✅ DESIGNED |
| 6 | **Tenant Confusion** | CRITICAL | Access asset from different tenant | Fail-closed TenantId validation; Token includes TenantId in signature | ✅ DESIGNED |
| 7 | **Malware Upload** | CRITICAL | Upload infected file to distribute malware | ClamAV/commercial virus scan before storage; Quarantine infected files | ✅ DESIGNED |
| 8 | **Moderation Bypass** | HIGH | Upload NSFW, quickly share before moderation | Sync scan for high-risk MIME types; Async for low-risk; NeedsReview blocks serving | ⚠️ PARTIAL |
| 9 | **Storage Quota Exhaustion** | MEDIUM | Upload many large files to exhaust quota | `[RequiresQuota]` on upload command; Pre-check before streaming | ✅ DESIGNED |
| 10 | **Reference Count Race** | LOW | Concurrent delete to corrupt ref count | Atomic decrement with RowVersion; Recount if inconsistent | ✅ DESIGNED |
| 11 | **GC Deletes Active Asset** | MEDIUM | Race between new reference and GC | 30-day grace period; Double-check ref count before delete | ✅ DESIGNED |
| 12 | **Download Window Bypass** | MEDIUM | Manipulate order to extend window | Order status checked server-side; FulfilledAt immutable | ✅ DESIGNED |

---

## B.11 Module Code Organization

```
GameGuild.Assets/
├── GameGuild.Assets.csproj
│
├── Abstractions/
│   ├── IAssetContentRepository.cs
│   ├── IAssetReferenceRepository.cs
│   ├── IAssetUploadService.cs
│   ├── IAssetAccessService.cs
│   ├── IAssetTransformationService.cs
│   ├── IAssetModerationService.cs
│   ├── IAssetGarbageCollector.cs
│   ├── IVirusScanService.cs
│   ├── IS3StorageService.cs
│   └── ITokenService.cs
│
├── Entities/
│   ├── AssetContent.cs
│   ├── AssetContentConfiguration.cs
│   ├── AssetReference.cs
│   ├── AssetReferenceConfiguration.cs
│   ├── TransformedAsset.cs
│   ├── TransformedAssetConfiguration.cs
│   ├── AssetReport.cs
│   └── AssetReportConfiguration.cs
│
├── Models/
│   ├── AssetKind.cs
│   ├── AssetAccessPolicy.cs
│   ├── VirusScanStatus.cs
│   ├── ModerationStatus.cs
│   ├── TransformationSpec.cs
│   ├── UploadRequest.cs
│   ├── UploadResult.cs
│   ├── AccessUrlRequest.cs
│   └── AccessUrlResult.cs
│
├── Commands/
│   ├── UploadAsset/
│   │   ├── UploadAssetCommand.cs
│   │   ├── UploadAssetCommandHandler.cs
│   │   └── UploadAssetCommandValidator.cs
│   ├── DeleteAssetReference/
│   ├── UpdateAssetMetadata/
│   ├── ReportAsset/
│   └── ReviewModerationQueue/
│
├── Queries/
│   ├── GetAssetReference/
│   ├── GetAssetAccessUrl/
│   ├── GetModerationQueue/
│   └── GetAssetReports/
│
├── Services/
│   ├── AssetUploadService.cs
│   ├── AssetAccessService.cs
│   ├── AssetTransformationService.cs
│   ├── TokenService.cs
│   ├── Moderation/
│   │   ├── AutoModerationService.cs
│   │   └── ModerationQueueService.cs
│   ├── VirusScan/
│   │   ├── ClamAvVirusScanService.cs
│   │   └── MockVirusScanService.cs
│   └── Deduplication/
│       ├── ContentHashService.cs
│       └── PerceptualHashService.cs
│
├── Workers/
│   ├── AssetGarbageCollectorWorker.cs
│   ├── AssetProcessingWorker.cs
│   └── TransformCacheCleanupWorker.cs
│
├── Middleware/
│   └── AssetServeMiddleware.cs
│
├── Controllers/
│   ├── AssetsController.cs
│   └── AssetAdminController.cs
│
├── Events/
│   ├── AssetUploadedEvent.cs
│   ├── AssetDeletedEvent.cs
│   ├── AssetModerationCompletedEvent.cs
│   └── AssetReportedEvent.cs
│
├── Configuration/
│   ├── AssetsOptions.cs
│   └── S3StorageOptions.cs
│
├── Extensions/
│   └── AssetsModuleExtensions.cs
│
└── Data/
    └── Migrations/
```

---

## B.12 Test Plan

### B.12.1 Unit Tests

| Test Class | Test Cases |
|------------|------------|
| `TokenServiceTests` | `GenerateToken_IncludesAllComponents`, `ValidateToken_RejectsExpiredWindow`, `ValidateToken_RejectsWrongSignature`, `ValidateToken_AcceptsPreviousWindow` |
| `TransformationSpecTests` | `ToCanonicalString_SortsParameters`, `Parse_HandlesAllParameters`, `Parse_RejectsInvalidFormat` |
| `AssetContentTests` | `MarkForDeletion_SetsTimestamp`, `IncrementRefCount_UpdatesCount`, `DecrementRefCount_NeverGoesNegative` |
| `AssetAccessPolicyTests` | `Private_RequiresOwnerOrAdmin`, `PaidContent_RequiresEntitlement`, `TenantPublic_RequiresTenantMembership` |

### B.12.2 Integration Tests

| Test Class | Test Cases |
|------------|------------|
| `AssetUploadIntegrationTests` | `Upload_CreatesContentAndReference`, `Upload_Duplicate_ReusesContent`, `Upload_IncreasesQuotaUsage`, `Upload_InfectedFile_Rejected` |
| `AssetAccessIntegrationTests` | `GenerateUrl_RequiresTenantContext`, `GenerateUrl_ChecksAccessPolicy`, `ServeAsset_ValidatesToken`, `ServeAsset_RejectsExpiredToken` |
| `AssetModerationIntegrationTests` | `Upload_NsfwContent_MarkedNeedsReview`, `ReviewDecision_UpdatesStatus`, `RejectedContent_ReturnsHttp451` |
| `AssetGcIntegrationTests` | `DeleteReference_DecrementsRefCount`, `ZeroRefs_MarkedForDeletion`, `GcWorker_DeletesAfterGracePeriod`, `NewReference_ClearsDeletionMark` |

### B.12.3 Security Tests

| Test Class | Test Cases |
|------------|------------|
| `TokenSecurityTests` | `BruteForce_RateLimited`, `ReplayAttack_RejectedAfterRotation`, `CrossTenant_TokenRejected` |
| `TenantIsolationTests` | `CannotAccessOtherTenantAsset`, `CannotModifyOtherTenantAsset`, `TokenFromOtherTenant_Rejected` |
| `RateLimitTests` | `Hotlink_BlockedAfterLimit`, `Counter_ResetsAfterWindow` |

### B.12.4 Performance Tests

| Test Class | Test Cases |
|------------|------------|
| `TransformationCacheTests` | `CachedTransform_ReturnedFromDb`, `Uncached_TransformedAndStored` |
| `ConcurrentUploadTests` | `ParallelUploads_AllSucceed`, `DuplicateRace_OnlyOneContentCreated` |

---

## PART C — SUPPORTING MODULES (Recommended)

Based on the complexity and separation of concerns, I recommend keeping everything in a single `GameGuild.Assets` module with clear internal namespaces rather than splitting into multiple modules. The reasons:

1. **Assets is already a bounded context** - All components are tightly coupled around asset lifecycle
2. **Avoid distributed transaction complexity** - Upload pipeline needs atomicity
3. **Simpler deployment** - Single module = single deployment unit
4. **Internal cohesion** - Virus scan, moderation, dedup are implementation details

### C.1 Internal Namespace Organization (Preferred)

```
GameGuild.Assets                        # Main module
GameGuild.Assets.Moderation            # Auto-moderation + queue
GameGuild.Assets.VirusScan             # Virus scanning abstraction
GameGuild.Assets.Deduplication         # Hash-based deduplication
GameGuild.Assets.Transformation        # Image/video transformation
GameGuild.Assets.Storage               # S3 abstraction
```

### C.2 If Separate Modules Required (Not Recommended)

Only create separate modules if:
- Virus scanning is reused by other modules (e.g., document uploads elsewhere)
- Moderation is reused for non-asset content (e.g., comments, posts)

In that case:

| Module | Justification |
|--------|---------------|
| `GameGuild.Moderation` | Reusable for comments, posts, profiles |
| `GameGuild.VirusScan` | Reusable for document uploads in Learning module |

---

## PART D — FINAL REPORT

---

## D.1 Executive Summary

### Key Risks & Priorities

| Priority | Area | Risk | Recommended Action |
|----------|------|------|-------------------|
| **P0** | Assets | Module doesn't exist | Implement as specified |
| **P1** | Localization | No error message localization | Add `ILocalizedErrorService` |
| **P1** | Resources | Missing asset resource types | Add to `ResourceUsageType` enum |
| **P1** | Features | Missing asset feature flags | Add feature flag definitions |
| **P2** | Localization | Hardcoded culture context | Read from request headers |
| **P2** | Features | Tenant context is warn-only | Consider fail-closed in production |
| **P3** | Resources | Fat interface `IResourceQuotaService` | Split into query/command |

### Module Health Summary

| Module | Overall Status | Security | Performance | Maintainability |
|--------|---------------|----------|-------------|-----------------|
| Resources | ✅ SOUND | ✅ | ✅ | ⚠️ |
| Features | ✅ SOUND | ⚠️ | ✅ | ✅ |
| Localization | ⚠️ INCOMPLETE | ✅ | ⚠️ | ⚠️ |
| Assets | 🆕 NEW | Designed ✅ | Designed ✅ | Designed ✅ |

---

## D.2 Resources Module — Findings & Fixes

### Findings

1. ✅ **STRENGTH**: Atomic quota consumption with optimistic concurrency
2. ✅ **STRENGTH**: Fail-closed on missing tenant context
3. ⚠️ **GAP**: No `Assets`, `AssetStorage`, `AssetDownloads` resource types
4. ⚠️ **SMELL**: `IResourceQuotaService` has 15+ methods (fat interface)

### Required Fixes

```csharp
// Add to ResourceUsageType.cs
public enum ResourceUsageType
{
    // ... existing ...
    
    Assets = 24,
    AssetStorage = 25,
    AssetDownloads = 26,
    AssetTransformations = 27
}
```

---

## D.3 Features Module — Findings & Fixes

### Findings

1. ✅ **STRENGTH**: Excellent use of Strategy + Decorator + Chain of Responsibility patterns
2. ✅ **STRENGTH**: Comprehensive targeting (tenant, user, plan, country, custom)
3. ⚠️ **GAP**: Missing TenantId only logs warning, doesn't fail-closed
4. ⚠️ **GAP**: No asset-related feature flag definitions

### Required Fixes

```csharp
// Add to feature flag seed data or configuration
public static class AssetFeatureFlags
{
    public const string TransformationsEnabled = "asset:transformations:enabled";
    public const string AllowedTransformations = "asset:transformations:allowed";
    public const string MaxTransformDimension = "asset:transform:max:dimension";
    public const string DownloadWindowHours = "asset:download:window:hours";
    public const string HotlinkLimitPerHour = "asset:hotlink:limit:per:hour";
    public const string PerceptualDedupEnabled = "asset:dedup:perceptual:enabled";
    public const string QualityUpgradeThreshold = "asset:quality:upgrade:threshold";
}
```

---

## D.4 Localization Module — Findings & Fixes

### Findings

1. ✅ **STRENGTH**: Clean entity model for field-level localization
2. ✅ **STRENGTH**: Translation workflow for human translation management
3. ⚠️ **GAP**: `LocalizationContext` returns hardcoded "en-US"
4. ⚠️ **GAP**: No service for localizing error messages / system strings
5. ⚠️ **GAP**: `TranslationWorkflowService` uses in-memory storage

### Required Fixes

```csharp
// 1. Add ILocalizedErrorService interface
public interface ILocalizedErrorService
{
    string GetLocalizedError(string errorCode, string languageCode, params object[] args);
    string GetLocalizedValidationMessage(string propertyName, string validationType, string languageCode);
}

// 2. Update LocalizationContext to read from request
public class RequestAwareLocalizationContext : ILocalizationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CultureInfo CurrentCulture
    {
        get
        {
            var acceptLanguage = _httpContextAccessor.HttpContext?
                .Request.Headers["Accept-Language"].FirstOrDefault();
            return ParseCulture(acceptLanguage) ?? DefaultCulture;
        }
    }
}
```

---

## D.5 Assets Module — Architecture Specification

See **PART B** for complete specification including:

- B.1: Module responsibilities
- B.2: Domain model (entities, value objects, enums)
- B.3: API design (public, admin, serving endpoints)
- B.4: URL design & token computation
- B.5: Enforcement model (authorization, access control)
- B.6: Integration points (Auth, Resources, Features, Localization, Commerce)
- B.7: Storage & caching architecture
- B.8: Upload pipeline
- B.9: Deletion & garbage collection
- B.10: Threat model & mitigations
- B.11: Code organization
- B.12: Test plan

---

## D.6 Security & Risk Register

| ID | Risk | Module | Severity | Likelihood | Impact | Mitigation | Owner |
|----|------|--------|----------|------------|--------|------------|-------|
| R1 | Tenant data leakage via asset | Assets | CRITICAL | LOW | HIGH | Fail-closed tenant validation; Token includes TenantId | Assets Team |
| R2 | Malware distribution | Assets | CRITICAL | MEDIUM | HIGH | ClamAV scan before storage; Quarantine infected | Assets Team |
| R3 | NSFW content exposure | Assets | HIGH | MEDIUM | MEDIUM | Auto-moderation; Human review queue | Moderation Team |
| R4 | Bandwidth theft (hotlinking) | Assets | HIGH | HIGH | MEDIUM | Access counter; Rate limiting; Token rotation | Assets Team |
| R5 | Token replay attack | Assets | HIGH | LOW | MEDIUM | 8-hour window rotation; 24-hour validity | Assets Team |
| R6 | Storage quota exhaustion | Assets | MEDIUM | MEDIUM | LOW | Pre-upload quota check; `[RequiresQuota]` | Resources Team |
| R7 | Feature flag tenant leakage | Features | MEDIUM | LOW | MEDIUM | Add fail-closed option for production | Features Team |
| R8 | Missing error localization | Localization | LOW | HIGH | LOW | Implement `ILocalizedErrorService` | Localization Team |

---

## D.7 Implementation Roadmap

### Phase 1: Foundation (Week 1-2)

| Task | Effort | Dependencies |
|------|--------|--------------|
| Add `ResourceUsageType` values for assets | 1 hr | None |
| Add asset feature flag definitions | 2 hr | None |
| Create `GameGuild.Assets` project structure | 4 hr | None |
| Implement `AssetContent` and `AssetReference` entities | 8 hr | Project structure |
| Implement `ITokenService` with time-window rotation | 8 hr | None |
| Implement `IS3StorageService` abstraction | 8 hr | None |
| Create EF Core migrations | 4 hr | Entities |

### Phase 2: Upload Pipeline (Week 3-4)

| Task | Effort | Dependencies |
|------|--------|--------------|
| Implement basic upload endpoint | 8 hr | Storage service |
| Implement content hash deduplication | 8 hr | Upload endpoint |
| Integrate `IVirusScanService` (ClamAV) | 8 hr | Upload endpoint |
| Implement auto-moderation integration | 16 hr | Upload endpoint |
| Add `[RequiresQuota]` for upload | 4 hr | Resources module |
| Implement chunked upload | 16 hr | Basic upload |

### Phase 3: Access Control (Week 5-6)

| Task | Effort | Dependencies |
|------|--------|--------------|
| Implement `AssetAccessService` | 16 hr | Token service |
| Implement `AssetServeMiddleware` | 16 hr | Access service |
| Implement access counter rate limiting | 8 hr | Serve middleware |
| Integrate with Features module for limits | 8 hr | Features module |
| Integrate with Commerce for paid content | 8 hr | Commerce module |

### Phase 4: Transformations (Week 7)

| Task | Effort | Dependencies |
|------|--------|--------------|
| Implement `TransformationSpec` parsing | 8 hr | None |
| Implement image transformation (ImageSharp) | 16 hr | Upload working |
| Implement transformation caching | 8 hr | Transformation |
| Add transformation feature flag checks | 4 hr | Features module |

### Phase 5: Moderation & GC (Week 8)

| Task | Effort | Dependencies |
|------|--------|--------------|
| Implement moderation queue endpoints | 8 hr | Auto-moderation |
| Implement user report flow | 8 hr | Entities |
| Implement garbage collector worker | 8 hr | Reference counting |
| Implement transform cache cleanup worker | 4 hr | Transform caching |

### Phase 6: Testing & Hardening (Week 9-10)

| Task | Effort | Dependencies |
|------|--------|--------------|
| Unit tests (token, spec, entities) | 16 hr | All code |
| Integration tests (upload, access, GC) | 24 hr | All code |
| Security tests (tenant isolation, rate limit) | 16 hr | Access control |
| Performance tests (concurrent uploads) | 8 hr | Upload pipeline |
| Documentation | 8 hr | All code |

---

## D.8 Test Strategy

### Priority 1: Security Tests (First)

1. Tenant isolation — Cannot access other tenant's assets
2. Token validation — Expired/invalid tokens rejected
3. Virus scanning — Infected files rejected
4. Rate limiting — Hotlinking blocked

### Priority 2: Core Functionality (Second)

1. Upload flow — File stored correctly
2. Deduplication — Same file reuses content
3. Access URL generation — Valid signed URLs
4. Reference counting — Correct increment/decrement

### Priority 3: Edge Cases (Third)

1. GC race conditions
2. Concurrent uploads of same file
3. Transformation limits
4. Quota exhaustion mid-upload

---

## D.9 Open Questions & Assumptions

### Open Questions

| # | Question | Impact | Recommendation |
|---|----------|--------|----------------|
| 1 | Which virus scanning service to use? | Pipeline integration | Start with ClamAV (open source), migrate to commercial if needed |
| 2 | Which auto-moderation service? | Pipeline integration | AWS Rekognition or Azure Content Moderator based on cloud provider |
| 3 | CDN provider? | Caching configuration | CloudFront (AWS), Fastly, or Cloudflare based on existing infra |
| 4 | Max file size per upload? | Quota design | Recommend 100MB default, configurable per plan |
| 5 | How long to keep quarantined files? | Storage cost | Recommend 30 days for investigation, then auto-delete |

### Assumptions

1. **S3-compatible storage available** — MinIO for dev, AWS S3/R2 for prod
2. **CDN in front of asset serving** — Required for performance and cache headers
3. **Background workers supported** — For GC, processing, cleanup
4. **Existing auth system works** — JWT tokens provide TenantId and UserId
5. **Feature flags can be evaluated sync** — For transformation limit checks

---

**Document Version:** 1.0  
**Author:** Platform Architecture Analysis  
**Review Required By:** Security Team, Platform Team, Commerce Team

