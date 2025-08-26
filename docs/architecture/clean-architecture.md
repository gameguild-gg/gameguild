# Clean Architecture & Infrastructure

Consolidated summary (replaces multiple legacy architecture docs).

## Layering

```text
Presentation  (Controllers / Minimal APIs / GraphQL)
Application   (CQRS commands & queries, validators, pipeline behaviors)
Domain        (Entities, Value Objects, Domain Events)
Infrastructure (EF Core, Auth, External Services)
Common        (Abstractions & cross‑cutting concerns)
```

## Key Patterns

- CQRS (MediatR): Commands mutate; Queries read
- Pipeline Behaviors: Logging, Validation, Performance
- Domain Events: Raised inside aggregates, dispatched post-commit
- Result / ProblemDetails for consistent error surface
- Context Middleware: `IUserContext`, `ITenantContext`

## Dependency Injection

Extension methods compose registration: `AddPresentation()`, `AddApplication()`, `AddInfrastructure()`. Shared abstractions live under `Common/` to avoid circular references.

## Validation

Unified behavior aggregates DataAnnotations + FluentValidation errors and short‑circuits prior to handler execution.

## Logging & Performance

Behaviors wrap MediatR pipeline & domain event dispatch measuring elapsed time + memory deltas; only log warnings for slow operations (threshold configurable).

## Domain Event Flow

1. Aggregate method adds event to internal list
2. DbContext collects events while tracking
3. `SaveChangesAsync` succeeds
4. Dispatcher publishes events
5. Handlers perform side-effects (posts, notifications) without altering primary result

## User & Tenant Context

Middleware parses auth headers / JWT → populates scoped context objects used by handlers & validators (no static access; test-friendly).

## Adding a Feature Checklist

1. Define entity & EF configuration
2. Implement command/query + handler
3. Add validator (if needed)
4. Raise domain events inside aggregate methods
5. Implement event handlers (side-effects only)
6. Expose endpoint (controller / minimal API)
7. Add unit + integration tests
