# Coding exercise product model

Recorded: 2026-11-19. Execution baseline: `723eeeb84589cfb23b0def630ee46ead02890acc`.
Gate: `CODE-UX-01`. This is design/feasibility evidence, not a passed runtime
gate or a second status tracker. Status and next action belong in
[IMPLEMENTATION-STATE.md](../IMPLEMENTATION-STATE.md).

## Critical review disposition

The 2026-11-19 critical review did not approve the initial specimen as a
complete implementation blueprint. Typed family authoring, diagnosis,
reference preparation/results, preview fidelity, realistic host constraints,
capability combinations and recovery states still need design evidence.
The required [01a iteration](./01a-prototype-workflows-and-critical-review.md)
records UX-F01..06, measured observations and their objective/human gates.
Apply [GOVERNANCE.md](./GOVERNANCE.md) to the next execution. The historical
layout observations below remain valid only within their stated scope.

## Reviewable layout

Open [the layout prototype](./layout-prototype.html) directly in a browser.
It is a standalone design specimen with synthetic data. No authentication,
runtime, server save, grading or submission is connected. The current specimen
keeps execution and persistence disabled. In 01a, explicit synthetic state
transitions may replace those disabled controls so complete interactions can
be reviewed; they must never be reported as real execution/persistence. Its simple
textareas are layout placeholders, not a proposed replacement for Monaco or
the existing content editors. Changing the family loads another specimen;
production transformations require the lossless confirmation in `CS-REF-08`.

The Instructor/Learner controls and Curriculum toggle allow inspection in
different contexts. The prototype supports keyboard tab navigation and
switching exercise families. Graphics demonstrates panel placement only;
the painted rectangle is not Emception execution evidence and does not pass
`CS-REF-10`.

| Task | Proposed composition | Why / acceptance |
| --- | --- | --- |
| Problem | full-width statement, examples and constraints; compact family selector | long text has space; `CS-REF-01`, `02`, `06` |
| Environment | language separate from environment; only proven capabilities selectable | no preset-as-exercise-family; `CS-REF-10`, `11`, `18` |
| Starter code | file list beside a focused editor; a single function signature where applicable | no runtime or terminal needed to prepare source; `CS-REF-02`, `12` |
| Tests | selectable case list beside a spacious, family-specific case editor; stack when narrow | configuration stays out of the console; `CS-REF-01`, `02`, `13`, `17` |
| Validate exercise | protected reference/wrong-solution validation and a separate learner preview | exercise preparation, not learner grading; `CS-REF-05`, `19` |
| Solve | problem alongside editor when space allows; cases/output/optional canvas in focused views | experiment separately from submit; `CS-REF-03`, `04`, `14` |
| Review | the solving regions receive frozen submitted files and authorized evidence | never restore the learner's current draft into a review; `CS-REF-15`, `16` |

**Validate exercise** is the explicit authoring task name, including its tab
and heading; do not abbreviate it to Validate or Validation. It checks the
prepared exercise, not student work. Official learner evaluation, scores,
review and release remain in Assessments/Grading. See `CS-ADR-019`.

### Layout observations

The automated layout inspection used one Chromium instance and sequential
contexts at 1600x1000, 1120x900 and 390x844. There were 69 checks across task
navigation, private-case selection, family-specific configuration, preview,
keyboard navigation and overflow. All passed. Six captures were generated in
`apps/web/test-results/coding-surface-refoundation/`; the authoring wide/mobile
and learner constrained captures were visually inspected. These observations
verify this specimen, not the real course application or human usability.

The later review confirmed that editing statement/starter does not update the
preview. It also measured a 704 px central area with both simulated Learning
panels at 1280x720: expected-output input began below the viewport at y=796.
Learner result tabs began at y=1096 in the short 390x844 specimen. No data loss
or execution claim in production follows from these design-only observations.
They require UXP workflow/layout coverage; the earlier 69 checks do not close it.

On desktop the selected case receives the remaining workspace width. At a
constrained central container, fields stack before they become narrow. On
mobile, statement and workspace become sequential regions; implementation
must add convenient focused navigation/resizing rather than shrinking text.
The surrounding Learning title/settings chrome is host-owned. Its current
wide coding container remains the integration point; the package must respond
to container width, not assume full viewport width.

## Instructor journey

1. Create the existing Code content item through Learning. The host creates or
   opens the shared content draft, with actor/tenant context and its revision.
2. Choose function or standard I/O. The task views share one incomplete draft;
   choosing another task never starts a compiler or attempts publication.
3. Write the problem, examples and constraints. Configure a supported language
   and environment independently from the family. An unavailable capability
   remains unavailable rather than selecting an unrelated environment.
4. Prepare starter and support files. Define one function signature for
   functional cases. Private references are separate validation inputs, never
   files that accidentally compile with the learner solution.
5. Add, edit and duplicate cases. Function values are typed. I/O expectations
   distinguish an absent value from intentional empty output. Configure
   visibility explicitly; preview never receives private cases.
6. Validate the current candidate with a correct reference and an incorrect
   solution. Changing relevant data invalidates results. Compilation errors,
   infrastructure failures and wrong answers remain distinguishable.
7. Preview the same learner components with a redacted delivery and separate
   scratch workspace. This operation creates no academic attempt.
8. Save incomplete work through the existing draft API. Only acknowledged
   server persistence can display Saved. A local recovery copy is labeled
   separately. Conflict recovery never silently overwrites another author.
9. Publish only a valid candidate. It freezes problem, execution data and
   private assessment data together. Grading owns the subsequent official
   assessment revision and review policy.

Failures to cover: empty signature while editing, invalid case input, deleted
entry file, unavailable artifact, compilation failure, cancellation, lost
network, revision conflict and reopening a draft on the same/another device.

## Learner and reviewer journeys

1. Open an authorized enrolled activity. The host obtains the official frozen
   delivery and identifiers; the surface does not choose a mutable lesson as
   its execution source. Opening local experiments must not send a submission.
2. Read the problem/examples and edit allowed starter files. Recover a local
   draft only for the same tenant, actor, revision and attempt. Immutable files
   and submitted snapshots cannot be replaced by local storage.
3. Run public cases or personal scratch input explicitly. Boot one runtime
   lazily. A custom case without an expected value reports observed output,
   not a fabricated pass. Program logs, terminal input and graphics have
   separate, accurate affordances.
4. Diagnose results, edit and rerun. Results are bound to the executed candidate
   and become stale after edits. Stop cancels work and repeated mounting does
   not accumulate workers or graphics sessions.
5. Submit the visible file snapshot through a host intent. Freeze that payload
   and its idempotency key for retry; later edits are a different local draft.
6. Reopen to see official pending/released state from the API. A failed editor
   or unavailable compiler must not hide feedback already released by grading.
7. The authorized reviewer loads the frozen revision and submitted files in
   read-only inspection. An explicit rerun produces diagnostic evidence, not a
   silent score/release mutation. Review/release remain host/grading commands.

Failures to cover: compilation error, timeout, missing graphics/input support,
scratch data without an oracle, refresh, storage quota, stale local recovery,
double-click/retry, actor switch, a later definition edit and unreleased feedback.

## Current implementation dispositions

| Existing area | Disposition | Evidence / consequence |
| --- | --- | --- |
| `coding-surface.tsx` author/learner/grader wrapper | replace composition after consumer cutover | author mode currently shares runtime session with solving; authoring forms must not boot it |
| `coding-workbench.tsx` | replace lifecycle/layout, reuse useful editor/file operations | runtime currently boots on mount; console is plain captured text; canvas is absent; source is an existing dirty file, not disposable work |
| `profile-registry.ts`, `profile-renderers.tsx` | evolve into family-specific form/solve registrations | current profile context is too small to own authoring; no new family for canvas |
| `execution-plan.ts`, `session.ts` | reuse public overlay/harness boundaries; revise run ownership/cancellation/result binding | they call public `emception/testing`; current results are local diagnostics |
| `storage.ts` and workspace scopes | reuse isolation rule; evolve recovery envelope and immutable-file filtering | actor/tenant/revision/attempt are already explicit |
| host definition editor and test editors | replace task composition; reuse valid field validation and visibility controls | 30-second autosave currently skips invalid drafts; tests are inserted through `bottomPanel` |
| host wire bridge | evolve in lockstep with TS/backend snapshots | draft function currently asserts a canonical type before it is valid |
| Learning authoring service | reuse domain persistence/concurrency | existing `program_content_drafts` stores JSON and revision/ETag; no new table is presumed |
| Monaco, xterm, resizable panels | reuse installed libraries when needed | avoid another editor, shell, terminal emulator or docking framework |
| Emception APIs/UI and unrelated consumers | preserve | consume public build/workspace/testing/canvas APIs; no upstream refactor or private import |

The authoritative field map and persistence decisions are in
[DECISIONS.md](../DECISIONS.md), `CS-ADR-014`. Existing tests remain useful for
privacy, path normalization and academic boundaries. Assertions requiring no
canvas or specific slot markup must be replaced when the new behavior lands.
Replacing layout tests does not authorize deleting privacy coverage.

## Runtime feasibility matrix

Installed manifests identify `emception`, `@gameguild/emception-browser` and
`@gameguild/emception-ide` as 4.5.2. Runtime binding is currently artifact 4.5.2,
ABI `emception-browser-v1`, lock hash
`405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37`.
Public package exports were inspected, including their installed `dist` files.
No production support claim follows from an export alone.

| Family / environment | Operation and channel | Public entry point | Official path evidence | Current observation / next proof |
| --- | --- | --- | --- | --- |
| standard I/O / C++ or C, clang/WASI | compile/run with batch stdin and stdout/stderr | `createEmception`, `workspace.setBuild`, `runTests`, `compileAndRun` | Code grading worker builds native sources and runs the full plan | source/export available; real browser/server proof blocked by artifact |
| function / C++, scalar values | generate typed C++ harness, run doctest report | `emception/testing.generateDoctestHarness`, overlay, `runTests` | trusted worker uses the retained assessment plan builder with the same public harness | actual public import generated integer harness; no compiler execution in this gate |
| function / C | signature/harness linkage | same public harness with C source toolchain | native presets exist; generated harness uses C++ linkage/types | do not advertise every scalar/language combination before conformance, especially string parameters |
| function / arrays or matrices | typed compound input/return | `mapCppType`, `generateDoctestHarness` | Courses validator and canonical coding schemas currently allow four scalar kinds only | public import rejects Array parameters, array returns and nested array values; `EMC-GAP-001`, product disposition pending |
| supported native program / interactive stdin | terminal bytes or stream, stdout/stderr | `createEmception` with xterm, `BrowserStdin` callback, `run` | official cases use fixed stdin; interactivity is local experimentation | requires cross-origin-isolation/stream/cancellation proof; log text is insufficient |
| native graphics / SDL3, raylib or Allegro | canvas build/start/input/stop | `api.canvas.build`, `start`, `buildAndStart`, `stop`, `CanvasSession` | current worker is a finite test runner, not a visual judge | exports and runtime profiles exist; family/environment compatibility and actual artifact execution remain unproven |
| project / SQL / new languages | future contracts/runtimes | no claim in this cut | not offered by this plan | out of scope; do not expose inert options |

The generic public `custom`/`doctest` cases accept code supplied by a host; they
do not provide a typed array transport/signature contract for existing coding
definitions. Writing our own parallel harness to exploit that escape hatch
would bypass the agreed Emception boundary. No array-as-string fallback was
introduced. See `CS-ADR-015` and `EMC-GAP-001`.

## Resource budget and validation operations

At entry, the measured host had 31,806 MiB RAM, 6,486 MiB available, 9,391 MiB
swap already used and 12 logical CPUs. Other applications were left alone.
The layout-only browser inspection lasted 2.27 seconds and observed at most
376 MiB of known Chromium process RSS; that is not a WASM/runtime measurement.

Use the following initial validation limits, then measure them with real
artifacts before accepting `CS-REF-12`. They are thresholds, not claimed
results:

| Measurement | Initial limit and response |
| --- | --- |
| heavy stack entry | at least 10 GiB MemAvailable before starting an additional isolated API/web/database/runtime stack; otherwise reuse a healthy authorized stack or defer the heavy check |
| heavy stack total | target at most 6 GiB combined owned process RSS; preserve at least 2 GiB system MemAvailable; stop owned work if this floor is crossed |
| concurrency | one Playwright worker, one runtime/execution per surface, sequential builds; use at most 10 of the 12 logical CPUs, and one MSBuild worker initially |
| output | retain at most 100,000 characters of visible logs; enforce definition/server execution budgets independently |
| lazy boot | statement/test form navigation creates zero workers and fetches no compiler bundles; one explicit execution may create the owned runtime |
| startup | observe cold and cached starts separately; record failure after 60 seconds rather than hanging; no claim that current artifacts meet this threshold |
| cleanup | after stop/unmount, zero owned active sessions/workers within 5 seconds; after five identical open/run/close cycles, post-cleanup RSS growth at most 128 MiB |
| memory pressure | stop the owned run on sustained swap growth above 256 MiB over one minute or after a kernel OOM; no automatic heap increase or rerun |

The historical isolated coding-cycle build currently requests a 12 GiB Node
heap. That command was not run on the pressured host. Integrating a bounded
real stack must happen before using that cycle as this refoundation's local
runtime evidence; a small preflight test spawning dummy Node children is not
such a build. Do not change `dev:fast` or replace its bundler to pass a test.

Executed lightweight checks:

```bash
pnpm --filter @game-guild/web exec vitest run \
  src/lib/coding-assignment/coding-surface-bridge.test.ts --maxWorkers=1
node --test --test-concurrency=1 \
  apps/web/scripts/coding-cycle-toolchain-support.test.mjs \
  apps/web/scripts/coding-cycle-editor-support.test.mjs \
  apps/web/scripts/coding-cycle-web-runtime.test.mjs
```

The shell required the installed Node 24.16.0 executable on PATH. Tests that
spawn Node/Chromium needed execution outside the filesystem sandbox; they did
not start Next.js, PostgreSQL or the WASM compiler. The bridge suite passed
5 tests and the preflight suite passed 26 tests. After moving manifest
verification ahead of the grading-runtime build, the affected 14-test
toolchain suite passed again. Focused ESLint for the fixture and bridge test
passed. All 122 local links across 43 roadmap documents resolve, and
`git diff --check` is clean.

## Artifact and data inventory

Local `apps/web/public/emception/manifest.json` and
`tools/emception/artifacts/toolchain/release/cdn/manifest.json` contain
`version: 1`. The release directory has no `provenance.json`. Running the real
`verifyCodingCycleToolchain()` against that release fails with:

```text
Code grading requires verified artifacts matching its frozen toolchain.
```

A read-only GitHub Actions query for
`emception-web-723eeeb84589cfb23b0def630ee46ead02890acc-c1236b3e35c0da3eed4e79135eb6b7090b91863f`
returned `total_count: 0`. No exact-source artifact was available to acquire.
The existing acquisition script remains authoritative: do not overwrite the
old local release, manufacture provenance or bind grading to an unrelated
compiler. Acquisition needs a verified producer result for the source identity
that will be tested, followed by its preflight.

The real browser-cycle fixture now emits `ExerciseType: "function"` and is
tested through the actual canonical wire bridge. It deliberately retains the
historical mixed stdio/function diagnostic coverage; this is not the new
family-specific product journey. Those representative workflows belong in
the refoundation/part 4 browser cases. Existing pure-family package fixtures
are still available in `@game-guild/coding/testing`.

The runner now verifies synced artifacts before compiling the grading runtime
or creating PostgreSQL. A regression in the existing preflight suite protects
that ordering. This reduces wasted setup for invalid artifacts; it does not
relax identity verification or alter the developer server.

No local course/database was inspected or reset. Existing write paths,
fixtures, snapshots and the retained toolchain histories were inspected in
source. Counts and shapes of real persisted published exercises remain
unknown. Before any breaking interpretation in 3.6.2, inventory them through
authorized domain reads and record preservation/upgrade handling. Unknown is
not evidence that all current exercises are disposable.

## Entry result

The model, ownership decisions, prototype, resource budget and fixture repair
are reviewable. `CODE-UX-01` remains partial: exact artifact/preflight success
is blocked externally, and compound-value scope needs a product disposition.
No downstream gate is marked complete. Human usability acceptance remains in
`CODE-UX-06`, distinct from these automated layout checks.
