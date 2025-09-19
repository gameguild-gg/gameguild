# Core and CQRS — Deep Analysis

This document analyzes the Core and CQRS layers in GameGuild.API, grounded by the current codebase, and proposes targeted improvements.

## Implemented

- Entities and domain base
  - `Source/Core/Entities/EntityBase.cs`: Generic `EntityBase<TKey>` and default `EntityBase` with:
    - Auditing: `CreatedAt`, `UpdatedAt`; soft delete via `DeletedAt`; optimistic concurrency via `Version` with `[ConcurrencyCheck]` and DbContext increment logic.
    - ID: generic `TKey` plus `Guid` default with `Create<T>()` factory patterns and partial init via reflection.
    - Tenant support: optional `Tenant` nav prop and `IsGlobal` helper; `IHasDomainEvents` with in-memory `_domainEvents` and helpers.
  - `ApplicationDbContext.cs`:
    - Applies configurations by assembly, tenant FK wiring for `ITenantable`, base entity configuration and soft-delete filters via extension methods, inheritance strategies for content hierarchy.
    - Timestamp and version updates in `SaveChanges()`/`SaveChangesAsync()`.

- CQRS core
  - Abstractions: `IRequest<T>`, `ICommand`, `IQuery`, `INotification`, `IPipelineBehavior<TReq,TRes>`, `RequestHandlerDelegateBase<TRes>`, etc.
  - Mediator: `Mediator.cs` with O(1) handler lookup caches (`_handlerCache`, `_handlerTypeCache`) and compiled delegates; supports `Send<T>`, `Send(object)`, `Publish<T>`, `Publish(object)`, and `CreateStream`.
  - Registration: `CQRS/Configuration/ServiceCollectionExtensions.cs` with scanning of handlers/notifications/pre/post/exception handlers; `AddPipelineBehavior<TBehavior>` helpers; `AddAdvancedPipelineBehaviors()` and `AddCachingBehavior()`; `CqrsConfiguration` for publisher choice.
  - Notifications and domain events:
    - Publishers: `ForeachAwaitPublisher`, `TaskWhenAllPublisher`, `NoWaitPublisher`.
    - Domain events: `DomainEventPublisher`, `DomainEventsDispatcher`, `DomainEventProcessorService` background loop draining pending events from tracked entities implementing `IHasDomainEvents`.

- Pipeline behaviors (Core)
  - `LoggingBehavior`, `PerformanceBehavior` (requires `IDateTimeProvider`), `RequestPreProcessorBehavior`, `RequestPostProcessorBehavior`, `RequestExceptionBehavior` (exception handler/action discovery with caches), `TransactionBehavior` (wraps `ICommand` or `ITransactionalRequest` in EF Core transaction with execution strategy), `CachingBehavior` (for `ICachedRequest`/`ICachedRequest` variants), `ValidationBehavior` (FluentValidation to unified `Result`/`Result<T>` mapping).
  - Caching contracts: `ICacheService` and in-memory `MemoryCacheService` with registration via `CacheServiceExtensions.AddMemoryCacheService()`.
  - Request caching model(s): `Core/Models/ICachedRequest.cs` (absolute + sliding) and `Core/Behaviors/ICacheableRequest.cs` (absolute only).
  - Example usage: `Modules/Users/Queries/GetAllUsersQuery.cs` implements `ICachedRequest` with key and expirations.

## Lacking / Gaps

- Dual caching interfaces
  - Both `Core/Models/ICachedRequest` and `Core/Behaviors/ICacheableRequest` exist; `CachingBehavior` expects `ICachedRequest` with `SlidingExpiration` while `Behaviors/ICacheableRequest.cs` exposes no sliding expiration. This is confusing and error-prone.
- Pipeline wiring clarity
  - No central composition root shown in `Program.cs` here; ensure `AddCQRS(...).AddAdvancedPipelineBehaviors()` plus `AddPipelineBehavior<LoggingBehavior>()`, `PerformanceBehavior`, `TransactionBehavior`, `ValidationBehavior`, `CachingBehavior` are registered in desired order.
- Result-first consistency
  - `ValidationBehavior` assumes `Result`/`Result<T>` responses; ensure all handlers adopt Result pattern to avoid exceptions on validation failure paths.
- Domain events persistence/outbox
  - Domain event processing is in-memory/tracked-entities based. There’s no durable outbox/inbox for cross-process reliability or after-commit dispatch.
- Transaction scoping
  - `TransactionBehavior` applies to `ICommand` or `ITransactionalRequest` and commits unless `IsFailure` in `Result`. For non-`Result` responses, failure won’t roll back early; contract isn’t enforced.
- Concurrency tokens
  - `Version` incremented in `DbContext` but no concurrency exception handling behavior; optimistic concurrency conflicts aren’t translated to domain `Error`s.
- Observability depth
  - `PerformanceBehavior` logs basic metrics; no OpenTelemetry spans/correlation outside logging; `LoggingBehavior` creates a per-request GUID but correlation across layers not guaranteed.

## Improvements

- Unify caching contract
  - Consolidate to one interface, preferably `ICachedRequest` with optional `SlidingExpiration`. Deprecate `Behaviors/ICacheableRequest.cs`, update `CachingBehavior` constraint and usages.
  - Add cache invalidation helpers for commands that mutate cached query data (e.g., `ICacheInvalidator` or publish cache-busting notifications).
- Strong pipeline composition
  - In DI setup, standardize ordering: Validation → Authorization → Logging → Performance → Transaction → Caching → Handler → PostProcessors → Exception mapping.
  - Provide `AddCorePipeline()` extension to register the set in order.
- Result pattern enforcement
  - Add analyzer or runtime guard to ensure handlers return `Result`/`Result<T>`. Extend `RequestExceptionBehavior` to convert common exceptions (e.g., `DbUpdateConcurrencyException`) into `Result.Failure(Error.Concurrency(...))` when response is `Result`.
- Domain events outbox
  - Implement EF Core outbox table and store events on save; background worker reads and publishes with retry/backoff; mark processed. This decouples dispatch from DbContext tracking and supports cross-process resiliency.
- Concurrency and retries
  - Add behavior to catch `DbUpdateConcurrencyException` and return retryable `Error` with conflict info; optionally re-execute handler with limited retries for idempotent commands.
- Observability
  - Wrap mediator `Send/Publish` in OpenTelemetry Activities with trace context; enrich logs with `CorrelationId` from headers; add event tags for request/response sizes.
- Testing hooks
  - Provide in-memory fake `ICacheService` and deterministic `IDateTimeProvider` in tests; add unit tests for each behavior to assert ordering and effects.

## Good Design Choices

- Mediator optimizations
  - Compiled delegates and type caches in `Mediator` reduce reflection overhead and achieve O(1) repeated sends.
- Behavior breadth and separation
  - Clear, single-responsibility pipeline behaviors cover logging, performance, exceptions, transactions, caching, pre/post processors, and validation.
- Entity base pragmatism
  - Soft delete, timestamps, and simple versioning built-in; helpful helpers (`Touch`, `SoftDelete`, `Restore`) and domain events stored on entity.
- Registration ergonomics
  - `ServiceCollectionExtensions` provides simple helpers to register handlers and behaviors and tune scanning.

## Risks / Code Smells

- Reflection-heavy property setters in `EntityBase`
  - `SetProperties` and constructor partial init via reflection can hide bugs and bypass invariants. Prefer explicit factory methods or mappers for write paths.
- Mixed caching abstractions
  - Two interfaces and behavior expecting one increases footguns and subtle runtime mismatches.
- Background event processor
  - Scanning `ChangeTracker` in a hosted service may miss events after context disposal; duplicates possible; no persistence of events.
- Transaction behavior predicate
  - Requests returning non-`Result` and throwing late errors might commit partial changes. Ensure commands conform to `Result` and domain exceptions are mapped.

## Quick References (files)

- Core
  - `Source/Core/Entities/EntityBase.cs`
  - Behaviors: `LoggingBehavior.cs`, `PerformanceBehavior.cs`, `RequestExceptionBehavior.cs`, `RequestPreProcessorBehavior.cs`, `RequestPostProcessorBehavior.cs`, `TransactionBehavior.cs`, `CachingBehavior.cs`, `UnifiedValidationBehavior.cs`
  - Contracts: `Core/Models/ICachedRequest.cs`, `Core/Behaviors/ICacheableRequest.cs`, `Core/Behaviors/ICacheService.cs`
- CQRS
  - `Mediator.cs`, `Abstractions/*`, `Configuration/ServiceCollectionExtensions.cs`, `MemoryCacheService.cs`, `CacheServiceExtensions.cs`
  - Events: `Events/DomainEventPublisher.cs`, `Events/DomainEventsDispatcher.cs`, `Events/DomainEventProcessorService.cs`
- Database
  - `Source/Database/ApplicationDbContext.cs`

## Suggested Next Steps

1) Consolidate caching interfaces and update usages; add cache invalidation pattern for commands.
2) Add `AddCorePipeline()` registration with explicit ordering and include `AuthorizationBehavior` if desired.
3) Introduce concurrency handling behavior mapping EF concurrency exceptions to `Result` errors.
4) Implement an Outbox for domain events with background dispatcher and retries.
5) Add OpenTelemetry spans around mediator operations and enrich logs with correlation.
