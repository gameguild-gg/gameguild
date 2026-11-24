# Post-04 Emception findings register

Updated: 2026-11-24. Phase: planned, not executed.

This is the authoritative mutable register for part 05 dispositions. The
[part 03 ledger](../03-coding-surface/IMPLEMENTATION-STATE.md) retains original
commands/results; the [gap inventory](../03-coding-surface/EMCEPTION-CAPABILITY-GAPS.md)
retains public-API evidence; the [runtime audit](../03-coding-surface/RUNTIME-RISK-VALIDATION-2026-11-22.md)
is historical, not proof that subsequently corrected bugs remain broken.
New findings from parts 03/04 must be reconciled here before their handoff.

## Classification rules

Keep separate: confirmed defect, unverified combination, unavailable extension,
product proposal and scoped correction awaiting regression. Record actual state
(`open`, `partial`, `unverified`, `unavailable`, `proposal`, `review-required`,
`regression-required`, `fixed`)
and owner approval independently. A planned fix or local pass is not full
production/official acceptance. Human review controls canonical capability status.

Every entry needs its existing/stable ID, expected versus observed behavior,
reproducer and evidence location/hash, affected personas/consumers, owner,
repair scope/authorization, subplan, independent oracle and final disposition.
Ignored local reports are not portable artifacts: preserve sanitized source
fixtures and acquisition commands in the existing harness before relying on
them in CI. If an archive is absent, reproduce it; do not fabricate prior proof.

Core grading errors, privacy/access failures, mutable frozen submissions,
incorrect release or overlapping/leaked active execution block the affected
03/04 gate immediately. Registering them here cannot waive that block.

## Confirmed defects and unfinished proof

| ID / state | Owner and observable finding | Required disposition / subplan |
| --- | --- | --- |
| `EMC-GAP-003` / open | Emception SDL artifact owner; three anonymous document mouse listeners survive each stopped graphical session | 5.2: approved disposal correction and unchanged five-cycle listener/pixel/input/worker/RSS proof; graphics cannot be advertised leak-free meanwhile |
| `CS-RUNTIME-UNRESOLVED-IMPORT` / open | Emception toolchain owner with coding integration owner; undefined native import can produce run/exit 0; intended policy needs investigation | 5.1: characterize legitimate versus missing imports, reject false-success execution at its real boundary without silently changing compiler defaults; any false official approval blocks the core now |
| `CS-RUNTIME-GRAPHICS` / partial | coding-surface owner; public SDL canvas renders, but native WASI execution does not execute the required callbacks; local port is not production/official graphics acceptance | 5.2: use only public canvas lifecycle, preserve explicit unavailable official combinations and prove affected host UI before enabling; no invented visual judge |
| `CS-RUNTIME-REPEATED-EXECUTION` / unverified | coding-surface/runtime owners; cold contexts and five offline editor cycles do not prove five repeated native executions in one live context | 5.3: unchanged ownership checks plus warmed memory/listener plateau, repeated run/cancel/context switch/unmount, OS-headroom measurements; classify any actual leak before repair |
| `CS-RUNTIME-CAPABILITY-MATRIX` / unverified | coding-surface owner; the full family/language/environment/output matrix is not certified by the bounded C++/WASI and SDL proofs | 5.0 inventory, then 5.1/5.2/5.3: actual checks for each offered combination; unverified options remain unavailable, not silently converted to WASI |
| `CS-RUNTIME-PROGRAM-START-BUDGET` / partial, native completion resource-blocked | coding budget consumers with Emception SDK owner; the public run-stage signal precedes artifact loading/WASM compilation; local test accounting now uses the authorized precise acknowledgement | CS-ADR-046 passes unit/type/compatibility and real I/O reference controls; finish the unchanged part 03 proof after OS headroom permits; standalone program and official budget consumers retain their part 03/04 timing/parity gates; 5.1 retains regressions afterward, without deferring core obligations |
| `CS-EMCEPTION-STRICT-LINT` / open | Emception SDK owner; seven pre-existing errors plus seven warnings keep strict lint of the now-touched SDK/type files nonzero; exact HEAD comparison proves no introduced finding | 5.0 reconcile against current upstream, then 5.3 owner-approved minimal cleanup and exact strict lint/type/runtime regressions; do not suppress or call historical debt warning-clean |

### Original evidence and constraints

- `EMC-GAP-003`: `runtime/graphics-gn3BMF` records the original 29-listener
  increase per cycle; `runtime/graphics-QKjfCF/report.json` records the remaining
  three after the authorized native cleanup. Counts after GC are
  18/21/24/27/30. Rendering, channels, worker release and RSS pass, but the
  listener oracle still fails with exit 1. Source diagnosis points to pinned
  `sdl3-runtime.mjs` registrations without removal handles. No global
  EventTarget/document monkey patch, unrelated-listener removal or manufactured
  artifact is permitted. Existing CS-ADR-038 does not authorize a recipe change.
- `CS-RUNTIME-UNRESOLVED-IMPORT`: retain the
  [original audit](../03-coding-surface/RUNTIME-RISK-VALIDATION-2026-11-22.md).
  The shared native composer catches an absent target, including expected
  zero, but that does not establish that an undefined import inside a present
  function fails. Test both and preserve legitimate imported functions.
- `CS-RUNTIME-GRAPHICS`: the local SDL port proves pixels, movement/input,
  desktop/mobile framing and bounded stop; its cleanup remains failed above.
  Do not infer support for raylib/Allegro or authoritative graphics grading.
- Repeated execution: `runtime/program-8GqP4D` stopped under resource pressure;
  `runtime/program-ljMk66/report.json` passes six cold controls.
  `runtime/workbench-90ZCGr/report.json` passes eight actual-package controls,
  including one executing-context replacement and five offline editor cycles.
  It explicitly records `compilerMemoryPlateau: false`. These are not five
  same-context compiler executions. Retain all reports and measure afresh.
  `runtime/exercise-check-sQbj1J` also stops under the OS headroom guard during
  I/O incorrect-solution preparation, after the function and I/O reference
  controls pass: 7537 MiB owned peak, 4574 MiB minimum available, 4772 MiB
  reserve, 816 MiB paging, no OOM. This is a resource refusal, not proof of a
  leak or a passing complete proof. More available RAM is requested; do not
  reduce the reserve or infer a warmed compiler plateau from cold contexts.
- Capability matrix: the new program port currently proves one C++/WASI source
  plus headers/assets; the test/official paths have separate scalar C/C++
  multi-source evidence. Do not silently drop other source units. Enumerate
  C interactive input and graphical presets rather than generalizing a pass.
- Program-start budget: `runtime/exercise-check-DA2oLK/report.json` records a
  correct I/O solution's first executed case and its second case timing out
  while the worker is still compiling WASM, before any `main()` entry event.
  The same five-second limit and exact verdicts were retained. The attempt
  reports zero memory PSI/OOM and no resource stop, so this observation is not
  classified as an OS termination or incorrect-answer/scanf failure. The
  owned cleanup-budget correction and public run-phase exit subscription have
  separate independent tests; neither makes the earlier start signal precise.
  The existing stage contract is not itself wrong: the consumer needs a more
  precise start acknowledgement. User explicitly approved CS-ADR-046; its
  optional SDK callback and owned consumer pass the independent contract/Node
  tests, including default behavior and actual constructor ordering. The
  real I/O reference now passes in `runtime/exercise-check-sQbj1J`; full native
  completion is resource-blocked, not accepted. Neither the standalone
  `program-session.ts` nor the official-worker budget consumer was changed.
  Parts 03/04 must independently close those timing/parity gates before claiming
  complete execution/core grading. Preserve that obligation, not just the local
  fix, when reconciling this entry.
  Reproduce with the existing exercise-validation harness, both default
  families for acceptance; an I/O-only diagnostic report cannot close the gate.
- Strict lint: the [phase-extension rationale](../03-coding-surface/EMCEPTION-PHASE-EXTENSION-RATIONALE.md)
  and original ledger record six warnings in `createEmception.ts` and one in
  `core/test-d/index.test-d.ts`, reproduced against both HEAD and changed source.
  This is baseline debt, not a regression introduced by the phase extension.
  It remains a nonpassing strict gate until actually rechecked/corrected; a
  new upstream cleanup requires scope approval, not a retrospective waiver.
  The authorized CS-ADR-046 expansion now also touches `worker-entry.ts` and
  `tool-runner.ts`: exact unsuppressed HEAD/current comparison adds seven
  already-existing any/this-alias errors, while retaining those seven warnings.
  `program-start-sdk-lint-baseline.json` in the same evidence root records
  identical rule/message/severity lists and zero findings in the new tests.
  None is introduced or suppressed by the optional start signal; strict
  whole-file lint still fails and is not promoted by a zero-delta audit.

All `runtime/*` references above are relative to ignored
`apps/web/test-results/coding-surface-refoundation/`. Their limits and hashes
remain in the source ledger; this document does not rerun or promote them.

## Unavailable capabilities and optional proposals

| ID / state | Owner and accepted limitation | Required decision / subplan |
| --- | --- | --- |
| `EMC-GAP-001` / unavailable | coding and Emception owners; arrays/matrices/compound function transport are not supported; scalar-only initial scope was explicitly accepted (CS-ADR-015) | 5.1: implement only after scoped approval of public typed transport and browser/official parity; otherwise keep an explicit owner-reviewed follow-up; no string encoding workaround |
| `EMC-GAP-002` / unavailable | runtime owner; no configurable per-program RAM enforcement; user approved deferral (CS-ADR-029) | 5.1: preserve historical values and rejection of newly configured unsupported limits; any implementation needs actual runtime enforcement, not a Node/build/OS memory cap |
| `CS-INSTRUCTOR-OPTIONAL-CONTROLS` / proposal | coding UX owner; supplied versus required student code, file freedom and assistance controls need product specification | 5.0 decision and linked follow-up: reuse existing file policies; no new promise of snippets/AI/correctors or typed settings without contracts and enforcement |
| `CS-EMCEPTION-DUPLICATION-REVIEW` / review-required | coding and Emception owners; intentional composition copies are recorded in the duplication register | 5.3: review divergence and ownership; preserving duplication is valid; no automatic shared abstraction, source removal or IDE decomposition |

These entries are tracked here, not automatically included as implementation.
An explicit implement/defer/reject decision must name owner, user-visible scope
and acceptance/follow-up. A confirmed safety/correctness defect cannot be moved
into this table merely to release the phase. Additive languages remain part 06.

## Scoped corrections retained as regression obligations

These are not newly open bugs. Their earlier proof is scoped; final host,
official E2E, repeated-execution and human acceptance remain distinct.

| ID / state | Recorded correction | Regression owner / oracle |
| --- | --- | --- |
| `CS-RUNTIME-COMPILE-FAILURE` / regression-required | CS-ADR-034: explicit terminal phase and official missing-phase guard | 5.1; compile/link failures never pass empty-stdout/matching-exit assertions; legitimate empty and explicit nonzero-exit programs stay valid |
| `CS-RUNTIME-NATIVE-ENTRY` / regression-required | CS-ADR-034: initial editor file is not a mandatory location for main | 5.1; main in another allowed source works, duplicate/absent main fails linking, source order and bytes remain intact |
| `CS-RUNTIME-FUNCTION-ABI` / regression-required | CS-ADR-036: shared owned native C/C++ composition; scalar preview/final-worker parity passes | 5.1; ordinary function-only solutions need no transport boilerplate; helpers, four scalar types, wrong/missing/zero controls agree in preview and official execution |
| `CS-RUNTIME-FUNCTION-PREPARATION-BUDGET` / regression-required | CS-ADR-028/037: bounded preparation separate from student-run timeout and output | 5.1; cold valid compile/link, finite/infinite student program, run-start/missing-phase and aggregate byte/channel controls; no blanket timeout increase |
| `CS-RUNTIME-OUTPUT-CHANNELS` / regression-required | CS-ADR-038: canvas fd 1/2 routing and native cleanup of 26 listener registrations per cycle | 5.2; stdout/stderr stay separate, single-sink callers remain compatible, native stop runs once; residual EMC-GAP-003 stays open independently |
| `CS-RUNTIME-WORKBENCH-OWNERSHIP` / regression-required | CS-ADR-040/042: lazy compiler, input/EOF, cancellation acknowledgement, teardown barriers and stale-result invalidation | 5.3; offline Monaco fetches no compiler, one execution owner, no late callbacks/submission capture across scopes, actual cleanup before restart |

## Updating an entry

Before implementation, link the approved spec/ADR, exact file set and RED test.
Afterward record command, cases/counts/exits, commit and source/artifact identity,
original and corrected evidence, affected-consumer regressions and owner/human
review. A new upstream requirement blocks the row until authorized. A lack of
memory, artifact provenance or a required executable test is an open gate.
Only reviewed passing evidence may change `open` to `fixed`; optional deferral
retains `unavailable` and never becomes `pass` by document editing.
