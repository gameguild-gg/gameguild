# 3.6.1. Product model and evidence baseline

Status: partial execution; prototype automation/remediation implemented, human
design acceptance, exact artifact and compound-value disposition pending.
Gate: `CODE-UX-01`. Updated: 2026-11-20.

Execution evidence: [`PRODUCT-MODEL.md`](./PRODUCT-MODEL.md), the
[layout specimen](./layout-prototype.html), and the shared
[implementation state](../IMPLEMENTATION-STATE.md). Ownership is recorded in
`CS-ADR-014`; scope/provenance decisions in `015` and `016`. No downstream
gate has passed.

## Invariants and boundaries

Apply [GOVERNANCE.md](./GOVERNANCE.md), including topology/import direction,
negative constraints and the mandatory contract-first cycle. This phase owns
design evidence, public feasibility probes and harness preparation. It does
not authorize production refoundation, platform edits or upstream changes.
The shared ledger is the single `state.md` role; use the referenced ADRs.

Read the [refoundation index](../06-authoring-information-architecture-and-teacher-ux.md),
[competitive baseline](./COMPETITIVE-BASELINE.md),
[acceptance matrix](./ACCEPTANCE-MATRIX.md) and part 3 context files first.

## Outcome

Define the experience and contract ownership before deciding what current code
to keep. Produce a concrete, reviewable model for both instructor and learner,
plus the infrastructure needed to validate it without depending on part 4.0.

## Atomic sequence and terminal gates

Each row completes Specification -> Contract/schema -> Test -> Minimal
implementation -> Quality gate. Commands are in
[QUALITY-GATES.md](./QUALITY-GATES.md).

| Slice | Specification | Contract / schema | Test oracle | Minimal implementation | Quality gate |
| --- | --- | --- | --- | --- | --- |
| UX01-A | inventory ownership, capabilities and actual data boundaries | field map, public API signatures and feasibility fixture expectations | supported/rejected function values and canonical wire requirements | bounded public probes, fixture repair and evidence/ADRs | G-DOC + relevant G-CONTRACT/G-WEB + preflight unit command; actual runtime check reported separately |
| UX01-B | resolve the six critical design findings | presentation states/events and complete teacher/learner task contract | UXP-01..06 oracles in 01a | isolated typed prototype iteration | complete 01a: G-PROT-flow + inspected/human design evidence |
| UX01-C | establish runtime test prerequisites and product scope | exact artifact binding, resource budget and explicit compound-value disposition | wrong identity fails; verified release passes; required decision is recorded | supported artifact acquisition/preflight, no manifest fabrication | G-PREFLIGHT + G-DOC; all CODE-UX-01 obligations and decisions recorded |

UX01-B can proceed while UX01-C is externally blocked. Its passing terminal
checks do not approve array scope, runtime support or the overall gate.

## Work

1. Record HEAD, relevant dirty files, actual public API/package versions and
   existing runtime artifacts. Preserve unrelated developer changes. Inventory
   consumers of the surface, its contracts and persisted coding definitions.
2. Map the instructor journey from a blank exercise to a validated candidate,
   and the learner journey from first opening to released feedback. Include
   incomplete work, failed execution, reopening and reviewing a frozen attempt.
3. Define task layouts for Problem, Environment, Starter code, Tests and
   Validate exercise. Prototype the learner problem/editor/results composition too.
   Inspect wide, constrained-by-sidebar and mobile containers. Do not approve a
   fixed inspector width or the current component tree as the product model.
   Complete [3.6.1a](./01a-prototype-workflows-and-critical-review.md): the
   first specimen did not demonstrate complete workflows or faithful preview.
4. Classify existing components as reuse, replace or remove after cutover, with
   reasons. Preserve existing correctness/privacy tests; replace assertions
   that encode a rejected layout. Do not rename the same IDE wrapper and call
   it a refoundation.
5. Record a field ownership table in `../DECISIONS.md`: statement, examples,
   constraints, language/runtime, starter files, reference solution, tests,
   capabilities, local layout preferences, draft, revision and submission.
   Identify existing host/domain owners before adding a second source of truth.
6. Record a capability matrix with family, language, runtime identity, run
   operation, input/output channel, public export, official executor support,
   test and state. Separate observed support from assumptions. Include batch
   stdin, interactive stdin, graphics and function values.
7. Probe compound function values through public APIs. The local
   `tools/emception/packages/core/src/testing/functional/harness.test.ts`
   currently asserts rejection of Array parameters/returns. This is evidence
   of a limitation in that path, not proof that every public path was inspected.
   Record whether a supported path exists; otherwise open an upstream/product
   decision through `EMCEPTION-GATE`. Do not patch upstream or disguise values
   as strings. Track the outcome under `CS-REF-18`.
8. Define a resource budget from the measured development machine: one active
   execution per surface, lazy runtime boot, bounded logs and explicit memory,
   startup and cleanup observations. Record measurable acceptance thresholds
   before large browser runs. Do not arbitrarily increase heaps to hide leaks.
9. Repair the existing coding-cycle canonical fixture, including required
   `ExerciseType`, and its focused tests. Acquire the exact artifact through
   repository-supported producer/consumer mechanisms and run preflight. Reuse
   the current harness; no manual manifest, private import, new coding CLI or
   relaxed provenance check. Record `CS-REF-21` separately from product success.

## Decisions that must be explicit

- What is learner-visible and revision-bound, rather than mutable lesson chrome?
- Where is incomplete work recoverable, and what does Saved actually guarantee?
- How are starter code, a protected reference solution and learner scratch work
  isolated from each other?
- Which environments support Run program, Run tests and interactive output?
- Which graphics combinations can be offered now without pretending they have
  an official automatic visual judge?
- Are compound function values implemented now, blocked upstream, or excluded
  by an explicit product decision with a tracked follow-up?

Technical choices that follow these requirements can be made autonomously.
Scope reductions, upstream changes, publication compatibility decisions and
storage changes needing `SCHEMA-GATE` require the relevant recorded review;
do not seek approval for every local component extraction.

## Evidence and gate

- Instructor and learner task models and annotated layout proposals are
  recorded and reviewed; no data owner or publication boundary is implicit.
- UX-F01..06 are resolved through the typed prototype and critical walkthrough,
  with terminal and human outcomes recorded separately. Previous overflow/tab
  checks are historical layout evidence, not this acceptance.
- Accepted/rejected competitor patterns map to `CS-REF-*` cases.
- Reuse/removal map is grounded in actual consumers, not assumptions about
  legacy code. Emception preservation boundaries are unchanged.
- Capability matrix and compound-value disposition include evidence, owner and
  follow-up; no unsupported option is described as operational.
- Canonical fixture/preflight tests pass and the exact artifact is available
  for required runtime checks. If not, record an infrastructure blocker, not
  a successful gate. Preparation itself does not close `CODE-UI-07`.
- Resource budget and safe test commands are recorded. Screenshots/prototypes
  are design evidence, not evidence that execution already works.

Update `../IMPLEMENTATION-STATE.md`, `../DECISIONS.md`,
`../CONSUMER-MAP.md` and `../EMCEPTION-CAPABILITY-GAPS.md`. Next:
[3.6.2](./02-domain-drafts-and-data-boundaries.md).

## Not this subplan

No wholesale implementation, new languages, database reset, platform auth
changes or upstream IDE edits. A feasibility probe remains small and bounded;
production behavior belongs in its following subplan.
