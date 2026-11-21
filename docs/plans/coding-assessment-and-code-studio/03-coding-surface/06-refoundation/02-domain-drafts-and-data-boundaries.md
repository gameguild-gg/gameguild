# 3.6.2. Domain, drafts and data boundaries

Status: in progress; UX02-A contracts, transformations and backend draft
roundtrip/conflict evidence are implemented. Its broader G-API gate remains
open because of the pre-existing Authentication throttle failure and the
API-wide compilation resource stop. The full solution build was not run.
UX02-B/C are not started. See the shared ledger for commands and evidence.
Gate: `CODE-UX-02`. Depends on `CODE-UX-01` (closed on 2026-11-21).

The new author-only `coding-authoring-draft` contract lives in
`packages/features/coding/src/authoring-draft*`. Its shared synthetic fixture
is `packages/features/coding/fixtures/coding-authoring-drafts.json`.
The canonical executable/wire contract and current production editor are
unchanged in UX02-A; this is not a cutover or a second server draft store.
Typed draft changes do not constitute publication validation or runtime support.

Entry does not require an Emception build, IDE publication or actual runtime
boot. This phase validates domain/draft/persistence behavior without compiler
execution; compatible local artifacts and G-PREFLIGHT are required before
the real runtime checks in `CODE-UX-03` (`CS-ADR-024`).

## Invariants and boundaries

Apply [GOVERNANCE.md](./GOVERNANCE.md) before specification. Allowed scope is
coding/domain contracts, adapters, affected Learning write/read paths and their
tests. The host owns persistence; grading owns academic state. Preserve
private projections, immutable history and the one-way graph. Schema/upstream
changes require their recorded decisions, not a UI convenience.

## Outcome

Make the accepted exercise model persist, reopen and freeze correctly. Do not
preserve a deficient contract solely to avoid changing our adapter or API.
Conversely, do not invent a new contract for a concern already owned correctly
by Learning or grading. Apply the ownership decisions from 3.6.1.

## Atomic sequence and terminal gates

Each row completes Specification -> Contract/schema -> Test -> Minimal
implementation -> Quality gate; see [QUALITY-GATES.md](./QUALITY-GATES.md).

| Slice | Specification | Contract / schema | Test oracle | Minimal implementation | Quality gate |
| --- | --- | --- | --- | --- | --- |
| UX02-A | trace accepted problem/family/case fields and inventory existing data | incomplete draft vs publishable definition; typed signature/cases, stable IDs and wire parity | blank/invalid/empty-output fixtures, TS/backend acceptance parity, lossless cancelled transformation | owned parsers/mappings and pure transformations | G-CONTRACT + affected G-API contract tests; contract stage passes before host behavior |
| UX02-B | define save/reopen/conflict and protected-data guarantees | host persistence ports, expected revision, recovery scope and reference separation | incomplete save/reopen, failure/conflict, cross-actor recovery denial and protected-file negatives | existing Learning draft integration and authorized projections | G-CONTRACT + G-WEB + G-API, including actual persistence tests |
| UX02-C | bind published problem and submitted files to their revision | immutable snapshot/projection and generated wire contract | later edits cannot rewrite an old attempt; private canaries absent; upgrade tests if storage changes | adapter/domain integration and generated client through official tooling | complete applicable G-CONTRACT/G-WEB/G-API + Auditor data/import review |

## Work

1. Trace each required field through authoring, wire DTO, storage, revision,
   adapter snapshot and learner/reviewer projection. Add or revise owned
   contracts and mappings where the trace is incomplete. If Learning already
   owns the problem text, retain one authoritative source and explicitly bind
   its revision to the delivered exercise; do not duplicate editable copies.
2. Model an incomplete authoring draft separately from a valid publishable
   definition. Share state across task views. Keep stable file/case IDs and
   explicit operations for edits, duplication and type/environment changes.
   The exact reducer/component API is not predetermined by the old surface.
3. Implement a deliberate recovery strategy through host-owned persistence
   ports. Prefer existing draft infrastructure. State exactly what survives
   refresh, navigation and another device. Local recovery must be labeled as
   local, namespaced by actor/tenant/content/revision or attempt, and handle
   quota/serialization failures. Never claim server Saved for unsynced work.
4. Separate saving/recovering an incomplete draft from validating/publishing a
   complete exercise. Never submit a falsely typed canonical definition just
   to make autosave work. If server draft support needs an owned domain change,
   implement it with contract tests rather than hiding the gap in the UI.
5. Distinguish starter files, support assets and protected reference solutions.
   A teacher-only solution must not be copied into learner VFS, delivered
   payloads, asset URLs, logs or learner storage. Hidden controls are not
   authorization. Reuse existing authorized asset handling instead of inventing
   a second asset/collection subsystem.
6. Define family-specific setup and case contracts. A function has one coherent
   signature; I/O has explicit input and expected output semantics. Distinguish
   an absent expectation from an intentionally empty string. Implement typed
   compound values only under the accepted `CS-REF-18` decision, consistently
   across compiler harness, browser and authoritative execution.
7. Keep execution configuration separate from view preferences and academic
   policy. Persist requirements affecting reproducibility; keep panel sizes and
   selected tabs local. Academic weights/scores use grading value contracts;
   technical input values must not be mistaken for academic scores.
8. Define atomic transformations for family, signature and environment changes:
   preserve compatible data, summarize incompatible data and make cancellation
   lossless. Never reinterpret existing tests silently. Invalidate validation
   results after a material change to the candidate.
9. Update TS parsers, backend validation, wire bridges, generated client via its
   official generator, snapshots and adapter fixtures together where affected.
   Do not hand-edit generated artifacts. Unknown identities fail closed.

## Compatibility and persistence guard

Replace development-only interfaces without gratuitous V2/compatibility layers.
Do not erase real published revisions or reinterpret submissions after the
fact. Inventory persisted uses and record a preservation/migration decision
before a breaking change. The absence of a public launch is not permission to
reset a developer's database or discard their exercises.

Prefer existing JSON/draft/revision storage when it fits. If tables, columns or
indexes must change, stop at `SCHEMA-GATE`, justify the need and include a real
migration plus clean-install and upgrade tests. Historical migrations stay.
Platform modules remain domain-free and are not changed to implement this UI.

## Evidence and gate

- `CS-REF-06`, `07`, `09` and `22` have contract/integration evidence. Both
  families roundtrip through the real write/read boundary, not only a React test.
- Incomplete draft recovery is tested separately from publication validation;
  failure/conflict states never silently lose or overwrite work.
- Redaction tests inspect serialized deliveries, nested fields, logs and asset
  access where relevant. A learner cannot submit protected-file replacements.
- Frozen statements, configuration, test identities and submission files remain
  tied to the correct revision. A later edit does not change an old attempt.
- Type changes and malformed inputs are covered by TS/backend parity fixtures.
- Required package tests/typechecks and affected API/security tests pass.
  Follow root/app `AGENTS.md`; update generated contracts only when needed.
- No frontend-only persistence workaround, speculative compatibility layer or
  unauthorized platform/Emception change is left behind.

Record files, decisions, migration disposition and actual commands in the part
3 trackers. Next: [3.6.3](./03-workbench-runtime-and-capabilities.md).
