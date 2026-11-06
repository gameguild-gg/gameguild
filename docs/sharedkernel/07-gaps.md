# Cross-Cutting Foundation Gaps — known vs NEW

Audit base: `origin/develop` @ `43603cb5b` (2026-11-03). "NEW" = first recorded by this
sharedkernel audit; "KNOWN" = already ratcheted or tracked elsewhere (cited).

## NEW gaps (this audit)

### N1 — `ObservabilityBehavior` is dead code (never registered)

- `Modules/GameGuild.SharedKernel/CQRS/Behaviors/ObservabilityBehavior.cs:26` — full
  implementation (entry/exit/timing logs, slow-request warning at
  `ObservabilityOptions.WarningThresholdMs` default 3000 ms, exception reporting with
  `Activity.Current` correlation).
- **Zero registration sites** in product sources — only the SharedKernel unit tests reference
  it. The CQRS pipeline therefore has **no behavior-level logging or timing**; the class doc
  even says it "replaces the separate LoggingBehavior and PerformanceBehavior" — those
  predecessors were removed and the replacement was never wired.
- Consequence: per-use-case slow-command warnings and handler-level failure logs do not exist;
  only HTTP-level `UseSerilogRequestLogging` (`Core/Setup/PipelineExtensions.cs:63`) and cost
  telemetry (`CostTelemetryMiddleware`, `:116`) observe requests. `ObservabilityOptions` is a
  dead options class (would also trip a G5-shaped review if it had a `SectionName`).
- Fix shape: one `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ObservabilityBehavior<,>))`
  in `Core/Setup/InfrastructureLayerExtensions.cs` (decide innermost-vs-outermost deliberately;
  see `01-cqrs-pipeline.md` §2).

### N2 — `IdempotencyMiddleware` complete but unwired (single-instance memory store)

- `Modules/GameGuild.SharedKernel/Middlewares/IdempotencyMiddleware.cs:31` + extension
  `UseIdempotency` at `:220`: POST/PUT/PATCH caching keyed on `Idempotency-Key` (24 h default
  cache, 5-min in-flight marker, 409 on concurrent same-key).
- No host call site (`ConfigurePipeline` never invokes it); only unit tests exercise the
  extension (`apps/api/tests/GameGuild.SharedKernel.UnitTests/SharedKernelCoverageCompletionTests.cs:1215-1216`).
- Its own doc comment (`:18-29`) declares the **single-instance limitation** (IMemoryCache) and
  the Redis migration path — i.e. wiring it as-is behind a load balancer would give
  per-instance idempotency, which is worse than none because it is uneven.
- Actual idempotency today is Economy's command-level `IdempotencyKey` value object
  (`GameGuild.API/Core/Controllers/EconomyBountiesController.cs:61` etc.) plus outbox
  `EventId` dedup — the generic HTTP surface simply does not offer idempotency.

### N3 — `MiddlewareOrderValidator` written, documented, never called

- `Modules/GameGuild.Identity.Context/Middleware/MiddlewareOrderValidator.cs:33-106` throws
  with precise fix messages for every security-order violation (Auth → Tenant → Actor →
  Authorization).
- Referenced only in XML remarks (`Modules/GameGuild.Identity.Context/IdentityContextModule.cs:44-55`).
  `ConfigurePipeline` never invokes `ValidateSecurityMiddlewareOrder`, so the guard cannot
  fire. The order is currently correct (see `04-middleware-chain.md` §1) — this is an
  un-armed tripwire, not an active bug. (Reflection-based `_components` inspection at
  `:108-133` may also be brittle across ASP.NET Core versions.)

### N4 — CSV/export invariant-culture discipline is mixed

- Reference-correct: `Modules/GameGuild.Compliance.Audit/Services/AuditCsvExporter.cs:11-27`,
  `Analytics/Queries/ProductMetricsQueries.cs:376`, `AuditController.cs:380,500-603`.
- Gaps that rely on default formatting under a localized request culture:
  `Modules/GameGuild.Assets/Queries/ExportAssetStatisticsQuery.cs:74` (bare `Append(value)`
  — safe only while every metric stays `long`),
  `Modules/GameGuild.AI/Controllers/AiController.cs:353` and `TenantAiController.cs:162`
  (CSV interpolation), `ProductMetricsQueries.cs:334` (filename stamp without culture).
- Tests pin invariant expectations
  (`apps/api/tests/GameGuild.API.IntegrationTests/AuditExportPostgreSqlHttpTests.cs:400`), so a
  decimal-valued metric or a culture-sensitive format added to the gap files will corrupt
  exports only under non-CI locales — silent until a customer opens it in another locale.
  Full list in `02-entity-softdelete.md` §4.

### N5 — Economy hand-rolls five outboxes (plus a sixth in Learning.Assessments)

- `economy_marketplace_outbox`, `economy_payout_dispatch_outbox`,
  `economy_admin_withdrawal_dispatch_outbox`, `economy_ad_reward_pending_claims`,
  `economy_bounties` work-queue, and `Learning.Assessments` academic outbox — cites in
  `03-outbox-core.md` §2.
- Each reimplements claim/lease mechanics but **none** inherits the shared 5-attempt/≤900 s
  backoff, dead-letter bookkeeping, delivery tombstones, or the `ReplayOutboxEventCommand`
  replay surface. Queue depth is invisible to the `gameguild.integration` status/replay views
  (`EventReplayService.GetStatusAsync`). Known design tension (module isolation vs platform
  primitives), first consolidated here.

### N6 — Security middleware spine lacks a test-level order ratchet

- The wiring-gap guards cover middleware *shape* (G6 stubs) but not middleware *order*. The
  order invariant documented in `04-middleware-chain.md` §3.5 is enforced only by comments;
  the purpose-built validator exists but is un-armed (N3). A reorder that puts rate limiting
  before token revocation, or tenant before authentication, would pass CI today.

### N7 — Soft-delete predicate and filter coverage is inconsistent

- 364 `DeletedAt` columns vs **31** soft-delete `HasQueryFilter` registrations, in **two
  predicate styles** (`e.DeletedAt == null` and `!e.IsDeleted` — even mixed within one file,
  `Modules/GameGuild.Resources.Contents/Configuration/ContentVersionEntityConfiguration.cs:44`
  vs `:76`). Tables without a filter return tombstoned rows unless each query filters by hand.
  Whether each unfiltered table *needs* one is module-specific, but there is no inventory of
  which soft-deletable reads are protected by query filters vs by-call-site discipline.

## KNOWN gaps (tracked elsewhere — not re-opened here)

| Gap | Tracking | Cite |
|---|---|---|
| Dead entity configurations without tables (G2) | #1184 | `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt` (2 entries: Commerce.Payments financial-ledger, Compliance.KYC verification) |
| Dead domain events — declared, never produced (G4) | #1184 | same baseline (26 entries across Subscriptions, Features, Tenants, Learning) |
| Dead options SectionNames (G5) | #1184 | same baseline (22 entries) |
| ABAC header-stub copies semantically empty but forwarding | #1180 | baseline header comment; out of G6 scope by design |
| `UserCreatedEvent` never emitted by OAuth provisioning | #1182 | `docs/auth/event-flows.md` §2 |
| `SuspiciousLoginDetectedV1` produced only by the local sign-in path | auth event-flows NEW→tracked | `docs/auth/event-flows.md` §2 |

## Suggested ratchets (if the orchestrator wants them)

1. G7 candidate: "registered `IPipelineBehavior<,>` implementations must be referenced by a
   host registration test" — would have caught N1.
2. Extend `WiringCommandRegistrationGuardTests`-style scanning to `Use*` extension methods
   defined in Sources but never called by `ConfigurePipeline` — would catch N2/N3-class
   orphans generically.
3. An architecture test asserting `ConfigurePipeline` source order for the six security-critical
   `Use*` calls (textual order check is enough) — N6.
4. A Roslyn/regex lint on `Append(` chains in files returning `text/csv` requiring an explicit
   culture argument — N4.
