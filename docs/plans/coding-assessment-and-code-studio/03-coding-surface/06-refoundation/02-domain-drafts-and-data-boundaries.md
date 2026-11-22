# 3.6.2. Domain, drafts and data boundaries

Status: in progress; UX02-A contracts, transformations and backend draft
roundtrip/conflict evidence are implemented. Its broader G-API gate remains
open because of the pre-existing Authentication throttle failure and the
full-solution command's time-budget stop. The current strict Release API host
build passed under the user-approved monitored 14 GB profile, with zero
warnings/errors, without disabling checks or changing dev:fast. The selected
API architecture/security/OpenAPI suite also passed (382 tests). These results
do not turn the incomplete full-solution build into a pass.
UX02-B persistence/session ports and the host bridge are implemented and tested
in scope; unaffected checks proceeded under the governance's external-failure
rule, not a waiver of the broader gate. No production editor/writer cutover.
UX02-C problem/reference/identity metadata, immutable revision tests and the
authorized WB-A worker safety guard are implemented/tested in scope. WB-B adds
explicit published configuration, inverse wire mapping, backend validation and
revision preservation under the accepted RAM deferral. Historical worker
assertions were reconciled without deleting history (CS-ADR-031). WB-B remains
partial: actual official execution fidelity is not established by forwarding
mocks. Four runtime risks, the pending scope decision and the broader G-API
disposition remain recorded in the ledger. Current-source OpenAPI and official
client generation, types, complete build and 1772 client tests passed.
Gate: `CODE-UX-02`. Depends on `CODE-UX-01` (closed on 2026-11-21).

The new author-only `coding-authoring-draft` contract lives in
`packages/features/coding/src/authoring-draft*`. Its shared synthetic fixture
is `packages/features/coding/fixtures/coding-authoring-drafts.json`.
The canonical executable/wire contract and current production editor are
unchanged in UX02-A; this is not a cutover or a second server draft store.
Typed draft changes do not constitute publication validation or runtime support.

UX02-B provides `createCodingDraftSession()` in the surface and
`createLearningCodingDraftPersistence()` in the web host. The latter reuses
Learning's authorized actions, never its direct published-content upsert.
The host supplies one atomic `{ revision, payload }` read so a coding reload
cannot enable a write with stale surrounding lesson metadata. The existing
lesson coordinator remains the sole save/publication owner when these ports
are wired during the later UI integration; no second autosave is installed now.
Only acknowledged server work survives refresh/navigation/another device.
Unsynced checkpoint recovery is explicitly author-only memory, scoped to
tenant/actor/course/content and draft/publication revision, never browser
storage. CS-ADR-026 records the lifecycle and durability guarantees.

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

### Remaining UX02-C boundary work

The shared fixture `packages/features/coding/fixtures/coding-publication-cases.json`
now exercises both families and unchanged historical serialization. It does
not yet prove a complete draft-to-wire publication path. Finish these steps
before closing the atomic row:

1. Apply the accepted `EMC-GAP-002` deferral (`CS-ADR-029`): configurable RAM
   remains explicitly unavailable in this delivery. Preserve historical data;
   do not silently persist/ignore a configured unsupported value. No upstream
   runtime or IDE change is authorized. WB-A retains legacy fixed defaults;
   WB-B maps explicit published time/output with an unavailable RAM capability.
   This is contract/composition evidence, not native execution acceptance.
2. Trace and roundtrip limits, explicit entry point, environment and frozen
   toolchain through draft compilation, wire DTO, validation, revision,
   learner/reviewer projection and official execution. Reject unsupported
   combinations/identities instead of substituting a current/default value.
3. Preserve the completed current-source OpenAPI v1 capture and official client
   generation: all 2180 committed method/path operations match; only owned
   coding schemas/types and generator metadata changed. Client types, full
   ESM/CJS/DTS build, public-API types and 1772 tests pass. Existing DTS warnings
   stay visible. The actual host build and 382 selected architecture tests pass;
   the full-solution time stop remains a separate incomplete gate. Do not exceed
   the budget, omit analyzers or migrations, use stale binaries or hand-edit
   generated output. No dev:fast change or IDE deployment is required.
4. Retain CS-ADR-031's reconciled historical-worker assertions and the external
   Authentication failure with its recorded disposition. Resolve the four
   CS-RUNTIME execution oracles and pending scope answer before claiming WB-B
   official execution fidelity; forwarding mocks cannot close this acceptance.
   Worker edits activate exact-source CI provenance; no gate waiver or IDE
   deployment is implied. Existing metadata/unit passes are not full closure.

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
