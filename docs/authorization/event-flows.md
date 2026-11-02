# Authorization Event Flows — Inventory, Durability, Cross-Module Edges, Gaps

> Module `GameGuild.Identity.Authorization` (+ host-side consumers in `GameGuild.API/Core/Security` and the Compliance.Audit module) · Documented against `develop` commit `74e9bba4c`.
> Scope: every event the authorization module publishes or consumes — durable outbox events, in-process notifications, webhook fan-out, audit-event sink streams, cache-invalidation signals and audit-table writes — with producer/consumer `file:line` and the durability boundary of each. Companion to `docs/auth/event-flows.md` (authentication module).

Paths are relative to `apps/api/Source`. Line numbers from commit `74e9bba4c`.

## 1. How authorization events move — five transports

| Transport | Write path | Durability boundary | Delivery / consumer | Retry / DLQ |
|---|---|---|---|---|
| **Durable outbox, generic operation event** | `UseCaseOperationBehavior<TRequest,TResponse>` stores a `UseCaseOperationOccurredV1` (`platform.use-case-operation.occurred.v1`) row for **every authorization command** that mutates (`GameGuild.API/Core/Eventing/UseCaseOperationBehavior.cs:117-154`); operation codes declared per command in `Modules/GameGuild.Identity.Authorization/Architecture/UseCaseEventContracts.g.cs` (64 contracts, all `NoDomainEventReason` — the operation event IS the contract; e.g. `identity.authorization.grant-tenant-permission`) | same EF transaction as the command (`ApplicationDbContext.cs:246-287`) | `OutboxBackgroundService` → `OutboxDispatcher` → registered `IIntegrationEventHandler<T>` (Resources cost-accounting consumes platform operation events) | inbox 5 attempts, exp backoff ≤ 900 s, `DeadLetteredAtUtc` + `EventReplayService` replay |
| **In-process notification** (`GameGuild.CQRS.INotification`) | `IPublisher.Publish` inside services/handlers | **none at transport** — handler runs inline in the same scope; durability = whatever the handler persists | synchronous `INotificationHandler<T>` resolution | none at dispatch |
| **Webhook fan-out** (`IPermissionChangeNotifier`) | `NotifyChangeAsync` after a mutation commits (`Modules/GameGuild.Identity.Authorization/Services/FocusedPermissionServices.cs:479-508`; sync `Services/PermissionSyncService.cs:640`; restore `Services/PermissionRestorationService.cs:341`; content-type `Services/ContentTypePermissionGrantService.cs:415`) | **none** — outbound HTTP with HMAC signature; delivery failure never rolls back the permission change (`Services/WebhookPermissionChangeNotifier.cs:243`) | external systems only (no in-repo consumer) | internal retries with backoff, then logged give-up (`:205-243`) |
| **Audit-event sink stream** | host implementations of module sink interfaces push `CreateAuditLogRequest` to Compliance.Audit | durable audit row via `IAuditService.TryLogAsync` (its own spool/persistence) | `GameGuild.Compliance.Audit/Services/AuditService.cs` | sink returns bool; decision unaffected |
| **Cache-invalidation signal** | `ITenantSecurityVersionStore.IncrementVersionAsync` (DB row) + Redis pub/sub | version row durable; Redis message best-effort | `Caching/RedisPermissionCacheInvalidationSubscriber.cs` → local cache eviction | re-poll of version row self-heals |

## 2. Event inventory

### 2.1 In-process notifications

| Type | Producer(s) file:line | Outbox | Consumer(s) file:line | Side effect | HOLE |
|---|---|---|---|---|---|
| `PermissionExpirationNotification` (`Notifications/PermissionExpirationNotification.cs:22`; kinds Expired / Upcoming) | `Services/PermissionExpirationService.cs:83,224-232` (`IPublisher.Publish`) | no — in-process | host `GameGuild.API/Core/Security/PermissionExpirationAlertHandler.cs:45` (`INotificationHandler`) | owner security alert + audit row (issue #331 boundary) | no — consumer exists; but publish is best-effort after commit (`:83-89` logs and continues), so an alert can be lost on queue failure with no retry |
| `AccessReviewReminderNotification` (`Notifications/AccessReviewReminderNotification.cs:10`) | `Services/AccessReviewAnalyticsServices.cs:294-297` | no | **none — zero `INotificationHandler` registrations in `apps/api/Source`** | intended reviewer reminder | **YES — orphan producer**: reviewers are never notified; the reminder counter (`ReminderCount`) increments anyway |

### 2.2 Audit events (sink stream — includes the `PermissionGranted`/`PermissionDenied` observed in logs)

| Audit action type | Producer (host sink) file:line | Module interface it implements | Consumer | Notes | HOLE |
|---|---|---|---|---|---|
| `PermissionGranted` / `PermissionDenied` (`Modules/GameGuild.Compliance.Audit/Constants/AuditActionTypes.cs:30,32`) | `GameGuild.API/Core/Security/AuditPermissionEvaluationLogSink.cs:31` (evaluation-log records; risk Medium allow / High deny, `:31-46`) | `IPermissionEvaluationLogSink` | `IAuditService.TryLogAsync` → durable audit rows | every durable evaluation-log write becomes an audit event | no |
| `PermissionGranted` / `PermissionDenied` | `GameGuild.API/Core/Security/AuditingAuthorizationMiddlewareResultHandler.cs:90` | ASP.NET authorization-middleware result handler | same | request-gate outcomes | no |
| `PermissionGranted` / `PermissionDenied` | `GameGuild.API/Core/Security/AuditingAuthorizationPermissionService.cs:167` and deny paths `:233,246` | decorator over `IAuthorizationPermissionService` | same | every `HasPermissionAsync` outcome audited | no |
| `PermissionGranted` (seed/sync path) | `Modules/GameGuild.Compliance.Audit/Services/AuditService.cs:120,139` | direct | audit rows | taxonomy: Authorization kind, Medium/High risk (`Constants/SecurityEventTaxonomy.cs:65-66`) | no |
| `RoleAssigned` / `RoleRevoked` / `PermissionRevoked` / `PermissionChanged` | **no producer in the roles or grant path writes these action types** | — | referenced by `ComprehensiveEventCoverageService.cs:89,104` coverage checks | coverage service *expects* them | **YES — orphan consumers**: roles lifecycle writes no audit events at all (see roles flow §8) |

### 2.3 Webhook fan-out events (`IPermissionChangeNotifier`, `Services/WebhookPermissionChangeNotifier.cs:74`)

| EventTypeName | Trigger `PermissionChangeEventType` | Producer call sites file:line | Delivery | HOLE |
|---|---|---|---|---|
| `permission.granted` | `Granted` (`:13-19`) | `Services/FocusedPermissionServices.cs:164-170` (grant); also sync-import and restore paths | HMAC-signed HTTP POST, retries then give-up (`:168-243`) | delivery failures only logged — external systems can permanently miss a revocation |
| `permission.revoked` | `Revoked` | `Services/FocusedPermissionServices.cs` (revoke), `Services/PermissionRestorationService.cs:341` | same | as above |
| `permission.denied` | `Denied` | deny path fan-out | same | — |
| `permission.updated` | `Updated` | expiration set/extend (`PermissionExpirationService` mutations), content-type grants (`Services/ContentTypePermissionGrantService.cs:415`) | same | — |
| `permission.restored` | `Restored` | restoration service | same | — |
| `permission.synced` | `Synced` | `Services/PermissionSyncService.cs:640` (import) | same | — |

### 2.4 Durable table "events" (not transported, but the durable record)

| Table | Writer file:line | Reader | Notes |
|---|---|---|---|
| `PermissionAuditLog` | `Services/PermissionAuditService.cs:20-66` — every grant/revoke/deny/expire/elevation mutation | analytics (`Services/AccessReviewAnalyticsServices.cs:401` PermissionAnalyticsService), history reconstruction (`Services/PermissionHistoryReconstructionService.cs`), compliance report | **roles lifecycle and access-review decisions write nothing here** |
| `PolicyRegistryAuditLog` | `Commands/PolicyBundleCommands.cs:91-100,180-189,253-262,362-371,460-469` (bundle lifecycle) | bundle surfaces | in the same unit of work as the bundle mutation |
| `PermissionEvaluationLogEntry` | `Services/PermissionEvaluationLogService.cs` | admin reporting (issues #358/#359) | per-decision durable log |
| `PolicyVersionHistory` | `Repositories/AdvancedPolicyRepositories.cs:96,115,127`; `Repositories/PolicyDefinitionRepository.cs:91,101,111` | `Controllers/PermissionHistoryController.cs:106` | append-only snapshots |

### 2.5 Cache-invalidation signals (internal "events")

| Signal | Producer file:line | Consumer file:line | Durability | HOLE |
|---|---|---|---|---|
| Tenant security version bump | `Services/FocusedPermissionServices.cs:149-150`; `Services/PermissionExpirationService.cs:416-433`; `Commands/PolicyBundleCommands.cs:349-360,449-458`; `Services/JitElevationService.cs:103` | `Caching/` invalidation chain, version re-checked on cache read | durable version row | user-scoped versions (`IUserSecurityVersionStore`) not bumped by single role assign/remove or JIT approval |
| Redis invalidation message | `Caching/RedisPermissionCacheInvalidationPublisher.cs` | `Caching/RedisPermissionCacheInvalidationSubscriber.cs` → local eviction | best-effort pub/sub; self-healing via version re-check | — |

### 2.6 Consumed integration events

The Authorization module registers **no `IIntegrationEventHandler<T>`** (verified: zero matches in the module). Its only inbound cross-module inputs are host-wired interfaces (`IPermissionEvaluationLogSink`, audit decorators) and the request pipeline. There are no orphan *consumers* in this module — the orphan risk is inverted here: producers without consumers (§2.1 `AccessReviewReminderNotification`, §2.2 role audit types) and consumers without producers elsewhere expecting authorization events.

## 3. Outbox durability summary

- Only the **generic operation event** (`platform.use-case-operation.occurred.v1`) is outbox-durable for authorization commands: written in the command transaction (`GameGuild.API/Core/Eventing/UseCaseOperationBehavior.cs:117-154`), delivered by `OutboxBackgroundService` with per-consumer inbox retry/DLQ. All 64 Authorization-module commands declare `NoDomainEventReason` — "the durable generic operation event remains the observable contract" (`Architecture/UseCaseEventContracts.g.cs`, 63× that string + 1 export-specific).
- No *named* authorization domain event exists (no `PermissionGrantedV1` integration event). Observers must rely on: the operation event, the audit tables, and the webhook fan-out (non-durable).
- `PermissionExpirationNotification` alerting depends on an in-process publish that is **after-commit and best-effort** (`Services/PermissionExpirationService.cs:83-89`) — a crash or handler failure between commit and publish loses the alert permanently (the expiration itself is durable; the alert is not).

## 4. Gaps

1. **NEW — orphan producer `AccessReviewReminderNotification`** (`Services/AccessReviewAnalyticsServices.cs:294-297`; no handler anywhere): reminders increment counters and publish into the void.
2. **NEW — expiration alerts are not durable**: best-effort post-commit publish (`Services/PermissionExpirationService.cs:83-89`); no outbox row, no retry, DLQ impossible. Contrast the replayable outbox path the rest of the platform uses.
3. **NEW — webhook revocation delivery is fire-and-forget** (`Services/WebhookPermissionChangeNotifier.cs:243` — "the permission change itself is unaffected"): an external system that misses `permission.revoked` keeps stale allows with no reconciliation signal.
4. **NEW — expected audit coverage without producers**: `ComprehensiveEventCoverageService` lists `RoleAssigned`/`RoleRevoked`/`PermissionRevoked`/`PermissionChanged` (`Modules/GameGuild.Compliance.Audit/Services/ComprehensiveEventCoverageService.cs:89,104`) but the roles path emits none of them.
5. **NEW — user-security-version gaps** on single role assign/remove and JIT approval (see state-machines E3; roles flow §8.1).
