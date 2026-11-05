# SharedKernel & Host Core — cross-cutting foundation audit

Audit package over `GameGuild.SharedKernel` (`apps/api/Source/Modules/GameGuild.SharedKernel/`)
and the API host core (`apps/api/Source/GameGuild.API/Core/`). Base: `origin/develop` @
`43603cb5b` (2026-11-03); docs 08–09 were cut at `ce31d5850c` (2026-11-05) with all reused
cites re-verified. Companion packages: `docs/auth/` (event flows, state machines),
`docs/authorization/`, `docs/economy/`, `docs/compliance/`.

| Doc | Contents |
|---|---|
| [01-cqrs-pipeline.md](01-cqrs-pipeline.md) | The home-grown mediator, the four-behavior chain (Authorization → Validation → Quota → Operation/transaction) with file:line, failure semantics (all fail-closed), and the full Controller→response flowchart incl. exception paths |
| [02-entity-softdelete.md](02-entity-softdelete.md) | `EntityBase`/`StatefulEntity` hierarchy, `DeletedAt` semantics (364 columns / 550 tables), query-filter conventions, the `*_live` partial-index strategy (17 indexes post-#1177/#1170), and the CSV/export invariant-culture hazard |
| [03-outbox-core.md](03-outbox-core.md) | Shared outbox primitives — 2 s poll, batch 50, 2-min claim lease, `FOR UPDATE SKIP LOCKED`, per-aggregate FIFO, 5 attempts, `2^n` s backoff capped 900 s, DLQ, replay command, cleanup/tombstones — plus the reuse census (Economy's five hand-rolled queues and one more) |
| [04-middleware-chain.md](04-middleware-chain.md) | The complete `ConfigurePipeline` order with registration lines, per-adjacency hazards (exception-first, auth spine, rate-limit-after-auth trade-off, response-cache variance), and the G6 stub-guard / orphan history |
| [05-security-primitives.md](05-security-primitives.md) | BCrypt work factor (default 12, bounds 10–16), SHA-256 token hashing, `LogRedaction`, certificate-protected DataProtection (env-only, fail-fast), env-only secret conventions, `AnonymousEndpointRegistry` contract |
| [06-event-bridge-registry.md](06-event-bridge-registry.md) | Host security bridge registrations, notification `EmailRendererRegistry`, and the `UseCaseOperationOccurredV1` generic contract — 1,161 platform contracts (258 hand-written + 903 generated), 164 of them auth |
| [07-gaps.md](07-gaps.md) | Seven NEW gaps (dead `ObservabilityBehavior`, unwired `IdempotencyMiddleware`/order validator, CSV locale drift, hand-rolled outboxes, unarmed order ratchet, soft-delete filter inconsistency) vs the known ratcheted set |
| [08-stride-threat-model.md](08-stride-threat-model.md) | The meta-threat-model — STRIDE over the six foundation surfaces (CQRS pipeline position privilege, outbox poison-blast-radius/DLQ escape, middleware order tripwire, soft-delete resurrection, DataProtection key blast radius, LogRedaction escapes) with NEW findings S2-a/S2-b/S4-a/S4-b |
| [09-dfds.md](09-dfds.md) | L0 (kernel as the substrate every module rides) + three L1s: one request's journey through all 26 middleware registrations and the behavior chain, the outbox write→lease→dispatch→DLQ→replay flow, the DataProtection key derivation chain |

Cross-cutting summary: the foundation is deliberately **fail-closed everywhere** — unknown
permissions throw, quota-service failures reject, unavailable commands resolve to explicit
stubs, missing event contracts throw, missing DataProtection env vars kill boot. The residual
risk concentrates in **wiring drift**: complete primitives that were never connected
(`07-gaps.md` N1–N3) and module-local reimplementations of platform machinery (N5).
