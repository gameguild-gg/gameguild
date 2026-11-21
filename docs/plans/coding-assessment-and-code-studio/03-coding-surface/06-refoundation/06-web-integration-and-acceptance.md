# 3.6.6. Web integration and acceptance

Status: planned. Gate: `CODE-UX-06`. Depends on `CODE-UX-05`.

## Invariants and boundaries

Apply [GOVERNANCE.md](./GOVERNANCE.md). Only named owned consumers migrate.
Keep upstream/UI, platform purity, official grading and published history
intact. Integration consumes validated contracts; it cannot weaken them to
make a route, build or acceptance report pass.

## Outcome

Accept the refounded surface in actual Learning journeys, with measured UX,
security and resource behavior. Do not substitute package snapshots for product
acceptance or claim that the broader part 4 validation program is complete.

## Atomic sequence and terminal gates

Each row completes Specification -> Contract/schema -> Test -> Minimal
implementation -> Quality gate; see [QUALITY-GATES.md](./QUALITY-GATES.md).

| Slice | Specification | Contract / schema | Test oracle | Minimal implementation | Quality gate |
| --- | --- | --- | --- | --- | --- |
| UX06-A | enumerate navigation/authoring/preview/learner/reviewer consumers | route composition ports, SSR/browser and payload boundaries | reach each through normal navigation; permissions/private projections and generated types agree | migrate named consumers and remove superseded owned wiring | contract compilation + G-CONTRACT/G-SURFACE/G-WEB and affected G-API |
| UX06-B | execute accepted teacher/learner tasks under real host/runtime constraints | frozen CS-REF oracles, layout/focus/resource observations and persona fixtures | complete required matrix; real canvas/input; no leaks, lost edits or misreported results | root-cause fixes within existing contracts; restart contract cycle if a contract must change | G-RUNTIME + G-REGRESSION + inspected stress captures + explicit human acceptance |
| UX06-C | hand off a reviewable, reproducible implementation to part 4 | evidence manifest, resolved/deferred decision ownership and next-action contract | no required skip or unreviewed capability promotion; all references/statuses agree | reconcile docs/consumers and bounded final builds | G-FINAL + G-DOC; Auditor checks negative constraints and independent human status |

## Work

1. Reconcile the consumer map: lesson format selection, lesson authoring,
   assessment definition, author test, learner activity, grader and SpeedGrader.
   Reach each through normal navigation; direct component mounts are insufficient.
2. Keep host code responsible for auth/tenant, routing, persistence, conflicts,
   official commands and status. Remove superseded family-specific forms and
   generic output-slot wiring in our migrated consumers. Do not retain unused
   interfaces just because they were created earlier in this roadmap.
3. Verify imports, SSR/browser boundaries, bundling and aliases. `dev:fast` must
   work with the current bundler; do not switch to a slower workflow as a fix.
   Emception UI, its exports and unrelated consumers remain preserved.
4. Run the acceptance matrix at the correct evidence layers. Add stable
   `CS-REF-*` references to tests or mappings so part 4 can reuse them. Update
   harness selectors to target meaningful accessible behavior, not brittle
   markup from the retired layout. Do not remove a security oracle to get green.
5. Inspect responsive screenshots with Learning sidebars open and closed.
   Cover wide desktop, constrained desktop and mobile/container widths. Check
   focus, labels, contrast, overflow, text fitting and usable panel dimensions.
   Use existing design-system controls and icons; avoid explanatory decoration,
   nested cards or full IDE chrome that obscures the task.
6. Record real resource evidence under the 3.6.1 budget. Confirm cheap form-only
   navigation, bounded execution and cleanup after cancellation and repeated
   activity changes. Full builds and runtime stress tests run sequentially.
7. Conduct separate human instructor and learner walkthroughs from the
   competitive baseline and acceptance matrix. Classify failures as product,
   harness, infrastructure or upstream. A screenshot is not human acceptance.
8. Reconcile all plans, decisions, gaps and duplication entries with what was
   actually delivered. Record accepted limitations explicitly; arrays or missing
   runtime behavior cannot disappear from the report because they are difficult.

## Validation order

Run the cheapest checks first, against the installed toolchain:

```bash
pnpm --filter @game-guild/coding test --maxWorkers=1
pnpm --filter @game-guild/coding typecheck
pnpm --filter @game-guild/grading-adapter-code test --maxWorkers=1
pnpm --filter @game-guild/grading-adapter-code typecheck
pnpm --filter @game-guild/coding-surface test --maxWorkers=1
pnpm --filter @game-guild/coding-surface typecheck
pnpm --filter @game-guild/emception-ui test --maxWorkers=1
```

Then run affected web component tests and integral web typecheck. If the owned
domain/API changed, include CodeAdapter, Courses and Assessments tests, applicable
PostgreSQL integration, generated-client checks and the security/architecture
suites required by `AGENTS.md`. Storage changes additionally need migration
validation. Record exact project/filter commands rather than claiming that a
subset proves a full suite.

Use the canonical fixture specified in `CODE-UX-01` and actual local artifact
preflight completed in `CODE-UX-03`, then focused one-worker Playwright journeys
and existing consumer CI. Run backend/web builds
with bounded concurrency where required by the diff; keep at least two cores
available when hardware permits. Do not run full builds alongside browser
execution. Do not disable typechecks, analyzers or provenance to pass.

No new human CLI is introduced. Existing test commands may orchestrate tests;
part 4 owns the integrated web lab/scenario catalog. Preserve unrelated running
services and clean up only resources created by the validation run.

## Completion gate

- All required `CS-REF-*` cases have the declared evidence. Decision-gated work
  has implementation evidence or explicit product/owner-approved disposition.
- Instructor and learner human acceptance is recorded; pending review keeps
  this gate pending. Generated reports never promote themselves to accepted.
- No private payload leak, destructive reset, unresolved data loss or misleading
  academic state remains in the accepted workflows.
- Both families work through Learning, not just package demos. Reviewer and
  author-preview flows use the correct data scope.
- Graphics, terminal and lifecycle evidence are real, not mocked verdicts.
- Runtime budgets, responsive behavior and accessible controls meet the accepted
  model. Known external failures remain documented, not silently waived.
- `@game-guild/emception-ui` and Emception sources have no refoundation diff;
  public API consumption and unrelated-consumer regressions are verified.
- All changed owned contracts have parity and applicable upgrade coverage.
- The remaining work in part 4 is named explicitly, with an exact next command
  or prerequisite and no dependency back to unfinished product tasks in 3.6.

## Handoff to part 4

Update `../IMPLEMENTATION-STATE.md` with the final commit/evidence and mark
`CODE-UX-06` complete only after its gate. The fixture, compatible pinned assets
and local preflight should already be ready; 4.0 revalidates them on the reconciled branch
and closes `CODE-UI-07` using official lane evidence if still pending.

Part 4 then expands scenario coverage, authorized API oracles, security cases
and the web-only validation lab. It does not repeat the surface migration or
rescue a knowingly incomplete refoundation. The integrated PR for parts 01-04
still follows the roadmap after those gates, not immediately after this subplan.
