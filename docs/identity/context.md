# Identity.Context — the actor abstractions inventory

> Module `GameGuild.Identity.Context` · `develop` 7ae5a28a65 · Row-6 scope: one short document — the abstractions this module owns and **what implements them** (the module is intentionally abstraction-only: 2,463 lines, no controllers, no commands, no stores of its own).

## 1. What the module is

The platform's actor-language module: the immutable per-request actor model every other module reads. Registered by the host at infrastructure step 03x (`GameGuild.API/Core/Setup/InfrastructureLayerExtensions.cs:155` — `AddIdentityContextModule`). Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Context/` unless stated.

## 2. Abstractions inventory × implementations

| Abstraction | Declared at | Implementation(s) | Registered where |
|---|---|---|---|
| `IActor` | `Actors/IActor.cs:11` | `UserActor` (`Actors/UserActor.cs:9`), `SystemActor` (`Actors/SystemActor.cs:8`), `ServiceActor` (`Actors/ServiceActor.cs:6`) — all sealed records | value types, no DI |
| `ActorKind` enum (6 kinds) | `Actors/ActorKind.cs:46` — `Anonymous=0, User, Service, System, Webhook, External` | resolved by `ActorKindResolver.Resolve(grantType, actorTypeClaim, subjectId)` (`:144`) with the `ActorKindIdentifierAttribute` registry (`:8`) | static resolver |
| `ActorContext` | `Actors/ActorContext.cs:19` — immutable record: `ActorKind`, `SubjectId`, `TenantId`, `Roles`, `Anonymous` singleton (`:26`) | built per request by `ActorContextMiddleware` (**lives in `GameGuild.Identity.Authorization/Middleware/ActorContextMiddleware.cs:28`**, registered by `Authorization`'s `ActorContextExtensions.UseActorContext`, invoked by the host at `GameGuild.API/Core/Setup/PipelineExtensions.cs:114`) | middleware pipeline |
| `ActorContextBuilder` | `Actors/ActorContextBuilder.cs:18` (`WithTenantId :127`, `WithRoles :146`, `Build :234`) | used by the middleware above | — |
| `IActorContextAccessor` / `ActorContextAccessor` | `Actors/IActorContextAccessor.cs`, `Actors/ActorContextAccessor.cs` | `AsyncLocal`-based singleton flow | `IdentityContextModule.AddIdentityContextModule` (`IdentityContextModule.cs:27`); host call site `InfrastructureLayerExtensions.cs:155` |
| `ISecurityAuditLogger` / `SecurityAuditLogger` | `Actors/ISecurityAuditLogger.cs`, `Actors/SecurityAuditLogger.cs:37` — `LogAsync`, `LogUnauthorizedAccessAsync :72`, `LogSensitiveAccessAsync :93`, `LogPrivilegeEscalationAsync :113`, `LogCrossTenantAccessAsync :142` | `SecurityAuditLogger` (singleton) over `SecurityAuditEvent` (`Actors/SecurityAuditEvent.cs:103` + `SecurityEventType` enum `:15`) | same registration (`IdentityContextModule.cs:31`) |
| `ActorAttributes` (499 lines) | `Actors/ActorAttributes.cs` | attribute surfaces other modules decorate with | consumed via reflection |
| `TenantValidationExtensions` | `Actors/TenantValidationExtensions.cs` | guard helpers used by consumers (e.g. `GameGuild.Compliance.Audit/Services/CentralRoleAssignmentAuditEventSink.cs:5` imports the actors namespace) | static |
| `MiddlewareOrderValidator` | `Middleware/MiddlewareOrderValidator.cs:33` — validates Authentication → Tenant → ActorContext → Authorization (`:56-91` throw messages) | invoked by composition roots that want order enforcement | extension method (not called by the current host pipeline — the host's order at `PipelineExtensions.cs:100-119` matches the validated order; the validator is available insurance) |
| `UseIdentityContextModule` | `IdentityContextModule.cs:59` | **no-op placeholder** (returns `app`; comment says ActorContext middleware is added by the Authorization module) | — |

## 3. Consumers (the reason this module exists)

`IActorContextAccessor` / `ISecurityAuditLogger` / `Actors` are consumed by **356 files** across the platform (grep across `apps/api/Source`, excluding the module itself) — every module that must know *who* is acting without re-deriving it from `HttpContext`: audit sinks (Compliance.Audit), billing command handlers, asset security handlers, AI controllers, the Authorization module's own mutation executor, etc. This is why the module has no dependencies outward — everything depends on it (dependency-direction guard: `apps/api/tests/GameGuild.API.UnitTests/Architecture/ModuleDependencyDirectionTests.cs`).

## 4. Notes for the tracker

- Score rationale (row 6): flows/endpoints/events/states/sequences/STRIDE/DFDs are **n/a by nature** (no endpoints, no aggregates, no events); the architecture column is satisfied by this document plus the middleware-order story → 1.0/8 with 7×n/a.
- Gaps worth an issue: `UseIdentityContextModule` is a no-op that duplicates registration advice (its `Add…` twin IS live at `InfrastructureLayerExtensions.cs:155`); `MiddlewareOrderValidator` is registered nowhere — either wire it into a health check or document it as opt-in.
