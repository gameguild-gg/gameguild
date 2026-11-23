# Coding surface implementation state

Last updated: 2026-11-23.

## Active continuation: production text workbench - 2026-11-23

- Entry: `2bfbdd63a5`, clean tree. The requested documentation commit is done.
  Scoped UX03-B/C owned editor/execution composition is implemented and its
  focused gates pass. This is not UX04 authoring redesign or whole-phase
  acceptance. CS-ADR-042 records ownership and the current consumer boundary.
- Contract first: an editor-only file port is ready without a runtime; each
  local operation captures immutable file/definition bytes. Program and tests
  share one owner, cancellation/drain barrier and stale-result invalidation.
  Submission capture stays offline and has no academic side effects. Only the
  host invokes official commands. Program input and test input stay distinct.
- Allowed paths: owned surface contracts, editor state, workbench/session/UI,
  focused unit/type/browser fixtures and harness, and the four existing web
  compositions if context keys or editor-only callback types require them.
  Update exports/architecture tests and current state/ADR/evidence records.
  No SDK, retained IDE/UI, compiler/preset, API/schema, launcher or dependency
  change. No private/reference material in learner buffers or local storage.
- Terminal gates: strict contracts before behavior; independent behavioral RED;
  G-SURFACE types/tests/strict lint, focused host regressions, real production
  workbench exercise under G-RUNTIME with OS headroom, and G-DOC/diff checks.
  Unsupported operations remain explicitly unavailable. Five-cycle and real
  terminal evidence must be reported separately; no promotion by mocked UI.

- Additional allowed host configuration: exclude generated `apps/web/test-results`
  from the web tsconfig's broad input glob. The full web check reproduced
  unresolved imports in ignored standalone repro entries, not application
  sources. Retain those artifacts/evidence; do not weaken source checking or
  omit the versioned package browser fixtures. Verify the parsed input graph
  and rerun the complete web check after this scope correction.

### Scoped workbench result and evidence

- Implementation commits: `2953dfcef9` (owned execution contracts/coordinator),
  `fcac79341a` (offline editor, terminal/UI/host integration and real proof),
  `ac940065c0` (generated report input scope). The documentation is committed
  separately; no push, PR or release was requested/performed.
- Contract types compiled before behavior. Independent first RED: 12 tests
  failed at the deliberately unimplemented owned coordinator; retained
  `runtime/20261123-workbench-red.json`. Later independent Auditor negatives
  caught early cancellation-state capture, loss of a pending barrier through
  an editor-only intermediate context, late submission capture and missing
  run-start budget acknowledgement. Root causes were corrected in owned code.
- `CodingEditorController` is ready offline, without an SDK `api` property.
  `CodingWorkbenchSession` snapshots exact editor bytes, owns one program/test
  operation, drains actual teardown, propagates cleanup failure, blocks stale
  callbacks and invalidates results after edits. Host submission remains a
  separate official intent. No replacement compiler, judge or grading policy.
- Tests use the existing public engine/native mapper with strict identity,
  phase and output/time checks. Infrastructure exceptions/missing phase cannot
  become numeric wrong-answer estimates. The proven scalar WASI function path
  is wired; the independently preserved graphics/RAM limitations stay visible.
- The actual package UI uses Monaco file tabs and xterm with independent
  stdout/stderr, finite batch input versus interactive input/EOF, separate run
  program/test/stop commands, real cancellation acknowledgement and stale
  result indication. Four host consumers now carry content/attempt/inspection
  identity or preview generation. Learner diagnostic summaries invalidate on
  edits. Private author buffers are not persisted or exposed to other modes;
  scoped local recovery uses strict canonical submission deltas, not a legacy
  decoder. Failed local recovery/save is visibly reported.
- Final G-SURFACE: strict package types, full strict lint and 227 tests, zero
  failures/skips. G-WEB: six explicit suites/133 tests pass (coding activity,
  route, definition editor, grader, bridge and lesson workspace). The complete
  web typecheck passes. No source checking, library rule or test was disabled.
- Web typecheck investigation: the first Node run exhausted its default 4 GiB
  heap; the second completed and exposed ignored standalone reproduction TS
  files incorrectly included by the broad application glob. Excluded only
  generated `test-results`, retaining the evidence and all versioned fixtures.
  Parsed input verification removes exactly six generated repro roots, zero
  application roots, retaining 1495 web inputs and all four host consumers.
  Final full check with a 6 GiB *maximum* Node heap (not reserved RAM), CPUs 0-3
  and OS guard: exit 0, peak owned RSS 6009 MiB, minimum available 4963 MiB
  versus 4772 MiB reserve, zero PSI/swap/OOM, about 66 seconds. Report:
  `runtime/20261123-workbench-web-types-resources-3.json`.
- Lint was run separately with package and existing web configurations; mixing
  their parser projects in one invocation was rejected, not counted as a pass.
  The web audit used an explicit empty bulk-suppression map to expose all
  applicable diagnostics, without adding/changing any repository suppression.
  Final map: `coding-surface-refoundation/empty-eslint-suppressions.json` in
  ignored web test evidence. No unpruned-suppression bypass was used.
  Removed one existing inline mount-effect waiver by using explicit guarded
  seed dependencies. Final host/harness lint is strict and suppression-free.
- Final actual-component proof: `runtime/workbench-90ZCGr/report.json`, eight
  required controls pass: interactive native I/O, stop awaiting input, executing
  context replacement followed by a correct function run, correct/wrong stdio,
  correct/wrong scalar function and compilation failure. Each observes at most
  one compiler worker and zero retained compiler workers. No uncaught page
  exceptions. Tests and program execution remain observably different, with
  no official-grade calls. Exact current owned/composer/SDK/harness/manifest
  hashes were independently verified after the final run.
- Five offline edit/capture/unmount cycles fetch no compiler assets and start
  no compiler workers. After warmup, DOM nodes/listeners remain at 47/182;
  observed heap increase is about 1.5 MiB, within the focused 8 MiB allowance.
  Inspected actual Monaco desktop/mobile and xterm/results captures; no page
  horizontal overflow. This is not a five-execution compiler-memory plateau.
- Runtime resource report `runtime/20261123-workbench-final-resources.json`:
  exit 0, 6860 MiB peak owned RSS, 5018 MiB minimum available versus 4772 MiB
  reserve, zero PSI/OOM and 1 MiB swap, about 132 seconds on CPUs 0-7. Exactly
  one compiler at a time, including the same-context replacement control.
- Preserve nonpassing attempts: `workbench-g1thNN` exposed the CSR fixture's
  missing React NODE_ENV definition; `workbench-E4wzxG` used Playwright's 5 s
  default while native compilation was still active; `workbench-I34EfZ` exposed
  the real cancellation acknowledgement race. `workbench-BtrGmz` then passed
  seven controls; the final run includes adversarial context replacement and
  the final budget/source changes. No failed report was overwritten/relaxed.
- G-REGRESSION: preserved Emception UI 27 tests, CI classifier/required gate 95
  tests and preflight helpers 26 tests pass, no required skips. Initial pnpm/
  subprocess/loopback restrictions were retried with appropriate execution
  access; bootstrap failures are not product RED. No API, platform, schema,
  client generation, SDK/IDE/UI, presets/compiler, launchers or dependencies
  changed in this slice. No full API/optimized-web build or canonical promotion.
- Still open: UX04 teacher task views, the final learner/review refoundation,
  actual Next-host/persona/publication/official-grade/release E2E, the separate
  repeated-execution plateau and full graphics acceptance. The isolated proof
  uses installed local Monaco assets; it does not certify a host's loader or
  private preview/official workflow. Existing post-04/pre-05 findings remain.
- Final G-DOC: 49 documents, 197 local links, seven atomic tables/25 complete
  rows, zero failures; whitespace audit passes. Every task-required command
  session is complete and all owned browser/fixture servers are closed.

Next exact action: UX04-A in `06-refoundation/04-instructor-authoring-and-validation.md`.
Compose the agreed teacher task views over existing draft contracts, keep
editing offline and remove long authoring forms from execution/output regions.
Do not call this completion of part 03/04. Preserve separate runtime stress,
official grading and human acceptance gates while advancing independent rows.

## Current priority and post-plan findings - 2026-11-23

- User requested commits before continuing, and explicitly prioritizes finishing
  parts 03 and 04 over correcting deferrable runtime/enhancement findings.
  CS-ADR-039 records this execution-order decision. The prior pending SDL
  repair/iframe question is superseded: neither approach is being implemented.
- Clarified by the user in CS-ADR-041: by the end of part 04, real simple
  function and standard-I/O exercises must prove the entire official grading
  journey, including wrong-answer/error negatives and released learner results.
  Local ports or the preserved working IDE are not substitutes. Then perform
  owner-reviewed stabilization of recorded defects before entering part 05.
  This does not waive required core/UX, privacy, execution-ownership or
  academic-integrity acceptance.
- Committed the preceding work separately: `2ebdabd6ae` (authorized SDK fixes),
  `9a44da314b` (owned graphical port and proof), `92df896f3a` (evidence/decisions).
  Pre-commit surface regression: 157 tests pass. No push or remote publication.
- Prior row: independent UX03-B text execution/input, scoped gate passed.
  Allowed paths: owned program-session contracts/implementation/tests, package
  exports/architecture, an isolated public-API browser fixture/harness, and
  current plan/decision/state records. Preserve SDK/IDE/UI/compiler/presets.
- Contract: immutable public candidate/files; separate batch and interactive
  input/output; no academic effects; bounded preparation/program/output;
  cancellation and actual disposal before restart. Use public orchestration,
  not copied compiler flags or a replacement executor. Gates: strict types,
  independent behavioral RED, G-SURFACE/lint and focused real-artifact input
  proof under the existing OS-headroom policy. No full stack needed for this row.
- Deferral is not a green test or approval of an unsupported capability. Continue
  independent textual/authoring/learner/E2E work; retain actual failed reports.
  Part 4 must carry these findings in its handoff and exclude no failing core
  workflow silently. Security, private projection, immutable attempts, official
  scores/release, cancellation and worker ownership remain mandatory.

| Finding / owner | Disposition after parts 03 and 04, before part 05 | Reopening oracle / current limitation |
| --- | --- | --- |
| `EMC-GAP-003` / Emception SDL artifact owner | defer artifact disposal fix; no global event interception or iframe redesign now | rerun the unchanged five-cycle graphics proof; document-listener plateau must pass; local graphics is not promoted as leak-free production support |
| `CS-RUNTIME-UNRESOLVED-IMPORT` / Emception toolchain owner | defer general undefined-import policy investigation | reproduce undefined target and require a genuine failure without changing legitimate programs; core grading fixtures retain negative controls; any discovered false academic pass blocks that flow |
| `EMC-GAP-001` / coding + Emception owners | retain accepted simple-type function scope; compound values later | typed arrays/matrices and actual browser/official parity; no string-encoding workaround |
| `EMC-GAP-002` / runtime owner | retain explicitly deferred per-program RAM control | real enforceable runtime memory control; UI cannot publish an unenforced limit |
| instructor preparation/file/assistance proposals / coding owner | optional expansion after core authoring and both plans | specify defaults, contracts and enforcement first; see the existing deferred-controls section, not new assumed product settings |

### UX03-B text execution result

- Implementation commit: `04374fe181`; the following documentation commit
  records this scoped result and the accepted post-plan priority. No push.
- Public contract compiled and three type-contract tests passed before behavior.
  Independent RED: 23 failures among 27 behavioral tests, at the deliberately
  unimplemented owned factory. Four validation tests already failed closed.
  Kept `runtime/20261123-program-red.json`; no infrastructure failure is counted
  as a behavioral oracle. Three later Auditor controls add split UTF-8 channel
  decoding, delayed timer dispatch and pending execution ownership.
- Implemented `createCodingProgramSession()`: immutable strictly parsed public
  delivery/candidate, lazy boot, separate bounded preparation/program timers,
  finite batch bytes/EOF versus interactive stdin, separate stdout/stderr,
  bounded visible output/input and actual disposal acknowledgement. No academic
  command, test verdict, reference solution or score is emitted.
- It adapts process I/O around the existing public browser `compileAndRun`
  helper, without compiler flags or another pipeline. The public browser
  callback's numeric EOF is -1, including asynchronous EOF; the strict type
  check caught the incompatible Promise<number|null> form before release.
  `waitingForInput` describes the SDK's prefetch bridge, not proof that native
  code has blocked in fd_read. No SDK source or retained UI change was needed.
- Bound of this new proof: C++/WASI, one native source plus public headers/assets.
  Unsupported source shapes/environments are rejected before boot, never
  silently truncated. Existing multi-source test/official mappers are unchanged.
  Input is capped at 64 KiB per local run; visible logs at 100000 characters;
  actual output/time follow the published definition. RAM remains unavailable.
- Actual local-asset proof: `runtime/program-ljMk66/report.json` passes six
  independent controls: finite batch, user input after a real prompt, empty
  batch/EOF with exit 3, stop while awaiting input, compiler error and an
  infinite student's-program timeout. All finish with zero owned workers;
  no uncaught page exceptions. Source, SDK-build, harness and manifest hashes
  are recorded. This is port calibration, not the production xterm/editor UI,
  persona grading flow, repeated-mount plateau or whole CODE-UX-03 acceptance.
- Preserve earlier evidence: `program-8lQE6G` failed because its test checked
  output immediately after the SDK prefetched stdin; the native prompt/output
  oracle remains required. `program-8GqP4D` was stopped by the OS guard during
  repeated compiler sessions in one context; it is not a passing runtime run.
  The final command isolates cold controls sequentially and explicitly records
  `repeatedMountPlateau: false`; it does not hide that separate open oracle.
- Final resource report `runtime/20261123-program-cold-resources.json`: exit 0,
  4757 MiB peak owned RSS, 6946 MiB minimum available versus 4772 MiB reserve,
  zero memory PSI/OOM, 1 MiB swap, about 69 seconds, CPUs 0-7. Exactly one
  browser/context/compiler at a time; all owned jobs/servers are closed.
- G-SURFACE: strict types, 191 tests/zero failures/zero skips and full strict
  lint pass; web harness strict lint passes. No API/platform/schema/client,
  dependencies, launchers, compiler/preset/harness, SDK or preserved UI delta
  after the preceding commits. No full API/web build or canonical promotion.
- Auditor verified every recorded owned-source, built SDK, harness and manifest
  hash against the final checkout. G-DOC passes 49 documents, 190 local links,
  seven atomic tables/25 complete rows; git diff --check passes. All command
  sessions needed by this slice are complete; no owned browser/server remains.

The text-port composition is now covered by the current workbench result above.
Continue at its exact UX04-A handoff. Required repeated-execution/graphics and
Next-host/persona evidence stays open. Continue the core user journeys, not
optional SDL artifact repair, new assistance settings or unrelated platform work.
Part 04 must produce its separate official-grading core report before any
claim that basic coding grading works. The post-04 repair checkpoint is then
mandatory before part 05; do not start language expansion to avoid known debt.

The CS-ADR-041 scheduling clarification was committed as `2bfbdd63a5`, after its
documentation-only audit (49 documents, 197 local links, seven atomic tables/
25 rows). The subsequent product/runtime evidence is recorded separately above;
the clarification alone did not close any execution gate. No push was requested.

## Active continuation: UX03-B graphical execution port - 2026-11-23

- Entry: `b07338ce46`, clean worktree; user requests continuation of part 3.
  Stage: runtime audit. Preserve all earlier runtime evidence and decisions.
- Implemented the owned local canvas execution port using the existing lazy
  runtime and public `canvas.build/start/stop` APIs. Bind it to an immutable,
  strictly parsed learner projection and explicit candidate key. No private
  tests/reference solutions, official commands or numeric grading results.
- First supported proof is C++/SDL3 with one native translation unit plus
  headers/assets. Reject unsupported source shapes before boot, rather than
  omitting source files or changing the meaning of the initial editor file.
  This bounded proof does not promote the whole environment/capability matrix.
- Contract first: typed states, separate bounded stdout/stderr, explicit stop,
  timeout/failure and teardown acknowledgement. Extended the owned lazy port
  with observable disposal completion so a late boot cannot overlap a restart.
  Types/contracts passed before behavioral RED/implementation. Real rendering,
  input/resize/cleanup remain separate oracles.
- Allowed paths: `coding-surface/src/graphics-session*`, existing owned
  runtime-session/types/tests, package exports/architecture tests, focused
  public-API browser evidence harness and these plan/state/ADR records.
  CS-ADR-038 additionally records the user's two explicit canvas-wasi/runtime
  forwarding/cleanup authorizations and focused tests. No retained UI,
  IDE/preset/compiler/harness, API/platform, dependencies, launcher or data edit.
- Gates: G-SURFACE; focused G-PREFLIGHT/G-RUNTIME with real published assets,
  one browser/compiler and OS-headroom guard; preserved UI regression and
  G-DOC. No full API/web build for this independent package/runtime slice.
  Production authoring/learner cutover and whole-phase acceptance remain open.
- Contract stage passed strict types and seven selected contract checks before
  behavior. The first RED retained 24 failures among 52 tests; two new fixture
  assumptions were invalid (initial file must remain a native source, and an
  incompatible explicit identity is rejected at parsing). Those are test
  corrections, not product defects. Corrected message checks avoid the existing
  cross-realm Error matcher issue. Three additional independent Auditor RED
  cover factory failure, late teardown failure and delayed timer dispatch.
  All 55 focused lifecycle/graphics tests then passed before real execution.
- Real proof ran through the isolated companion
  `apps/web/scripts/coding-surface-graphics-e2e.mjs`, with its strict typed
  `src/testing/graphics-browser.ts` calibration. It reuses installed public SDK
  exports and verified local 4.5.2 assets; no SDK source alias or toolchain build.
  The initial two blank-target failures are preserved. The SDL artifact requires
  its connected unique `#canvas` selector; the owned port now rejects a missing,
  detached or ambiguous target before boot instead of mutating another target.
- Authorized SDK correction: fd 1/2 use their correct callbacks; the optional
  second sink preserves the existing single-sink call. Invalid descriptors
  fail closed. Stopping the SDL callback lifecycle now calls SDL_AppQuit once;
  pause/owned-URL teardown still run if native/module cleanup throws. Four
  independent RED among 14 selected tests precede the minimal change.
- Current real evidence: ignored runtime/graphics-QKjfCF/report.json, desktop.png
  and mobile.png. Five cycles render the expected pixels, animate, respond to
  keyboard input and resize without overflow. Captures were inspected. Worker
  teardown completes within 301 ms including observation; the RSS plateau and
  stdout oracle pass. This is an isolated graphics port, not the production UI,
  an interactive text terminal, official graphics grading or human acceptance.
- Remaining failure: event listeners after cleanup are 18/21/24/27/30. The
  native cleanup removes 26 of the original 29 additions per cycle, but three
  anonymous document mouse listeners remain in the published SDL glue. No
  public removal hook exists for those anonymous callbacks. G-RUNTIME exits 1
  for eventListenerPlateau; CS-REF-12 and full CODE-UX-03 stay open. A structural
  choice is pending: coordinate an artifact fix with the Emception owner, or
  authorize investigation of a disposable frame. Do not patch global event
  methods, delete unrelated listeners or pretend disposal is complete.
- Original failed observations remain in graphics-yNyt28/iVVFLu (blank target),
  graphics-EZx93H (stdout on stderr), graphics-gn3BMF (original five cycles),
  and sibling 20261123-canvas-*-red.json / graphics-upstream-red.tap. The first
  suite-wide attempt also caught a lexical architecture guard: a local helper
  named release was renamed disposeOwnedRuntime; the guard was not weakened.
- Static/regression gates: surface strict types, 157/157 tests, complete strict
  lint; browser local build/types and 62 tests (one existing opt-in Node smoke
  skip), IDE 26, webcomponent seven, preserved UI 27, preflight/CI 121 and
  artifact-policy 36 tests pass. Total 436 passing non-duplicated tests; real
  graphics lifecycle still fails as above. Root lint prints existing React/
  pages notices; npm prints existing pnpm-environment notices. No suppression.
- Both actual local manifest preflights pass. G-DOC checks 49 documents,
  187 local links, seven atomic tables and 25 complete rows without failures;
  git diff --check passes. Only the two authorized upstream runtime source
  files changed, with their focused tests; protected IDE/UI/compiler/presets,
  API/platform, dependencies and launchers have no diff from the entry commit.
- Resources: one browser/compiler, CPUs 0-7, 4772 MiB available-RAM reserve.
  Corrected five-cycle peak 7376 MiB, minimum available 5546 MiB, zero PSI/OOM
  or resource stops; report 20261123-graphics-fixed-resources.json. No fixed
  RSS/heap ceiling, forced OS/swap allocation or unrelated process kill.
  All owned browser/server/process groups are closed. No full API/web build,
  persona flow, SDK/IDE publication, commit, push or canonical promotion.
- Next: resolve EMC-GAP-003 before declaring cleanup/capability acceptance;
  continue other UX03 obligations only within their independent atomic scope.
  Production cutover remains UX04/05/06 work, not implied by this local port.

## Native function support through owned composition - 2026-11-23

- Entry: `c733a556c3`; preserve all earlier uncommitted UX03-A capability work.
  CS-ADR-036 records the user's explicit choice of owned support/templates,
  superseding the upstream-harness proposal, not upstream preservation.
- Current stage: typed/template and phase-budget integration implemented and
  audited; real scalar/runtime proof passes. Full G-RUNTIME and
  CODE-UX-02/03 remain open for their separate acceptance obligations.
  Do not promote function eligibility, canonical status or human acceptance.
- Ownership: coding owns a pure source composer and C++ public-harness factory
  port; surface owns public/full preview mapping; the product-specific worker
  regenerates official support from the frozen definition. Only that exact
  pure module crosses its import guard, with positive/negative boundary tests.
  The current-checkout worker build remains mandatory. SDK acquisition and
  its classifier/provenance are separate and unchanged.
- Preserved: Emception harness/IDE, retained UI, presets/flags,
  dependencies, API/platform/schema, historical migrations, launchers and
  actual submitted bytes. The only core/browser change is the explicitly
  authorized optional phase-start contract/forwarding (CS-ADR-037). No new
  package or compiler/judge. No account reset,
  unrelated service stop, upstream SDK publication or whole-toolchain build.
- Source contract: ordinary functions need no main/extern-C boilerplate.
  C uses int/double/_Bool/const-char-pointer; C++ retains the public scalar
  harness and ordinary-function ABI bridge. Weak presence checks reject absent
  targets expecting zero; supplied weak startup preserves existing main.
  Temporary support stays outside /home/user and is restored/removed by the
  public overlay. Headers are not separate translation units. Cases, group
  order/weights and receipt shape remain unchanged. Historical definitions
  without explicit execution configuration retain their original mapper.
- Input validation rejects unsafe/nonrepresentable native literals explicitly:
  int32 bounds, finite decimal syntax, embedded NUL/unpaired surrogates and
  C++ strings the existing serializer cannot represent. No silent coercion,
  truncation, invented compound transport or reinterpretation of C as C++.
  Unknown/mismatched language identities and graphics function execution fail
  before composition. Generic unresolved imports inside a student's function
  remain a separate finding; checking target presence does not resolve them.
- Contract-first record: strict composer port before behavior; corrected
  independent RED 33/33; four official integration RED with the historical
  mapper control already passing; two additional identity RED. Surface RED
  includes four intended mapper failures, a corrected invalid fixture and
  JS-realm assertion correction, not six new product defects. Assert original
  error identity and restored byte values; preserve projection/overlay code.
- Feasibility: twelve fixed real-WASM C/C++ controls pass, including correct,
  wrong, compile failure, absent-zero, present-zero and retained-main sources.
  Peak 5579 MiB; minimum available 9064 MiB; no OOM/resource stop.
- Initial production parity: ten public previews and seven scalar official
  controls pass; C++ string preparation fails before comparison. Preserve that
  failed report, its actual bundle hash and the three independent negative/
  historical controls. Checkout hashes are not a producer/build receipt.
- Root cause: independent private-scratch and fixed-output controls both hit
  the unchanged combined 30-second budget. First C++ string group completes in
  about 26 seconds. The second spends about 23 seconds in two successful
  compilation steps, then is cancelled during linking. Output-path reuse is
  not the cause; no wrong-answer comparison has run. This diagnostic's exit 0
  means observations were captured, not that either product control passed.
- Budget rule explicitly approved: the published exercise time limit applies
  only to student execution. Compilation/linking need a separate bounded
  infrastructure budget (CS-ADR-037). Do not merely increase the existing
  combined timeout or remove cancellation. The public low-level helper has
  onPhase, but the native multi-source facade did not expose that contract.
  The user subsequently explicitly authorized the narrow optional facade
  callback. CompileOptions.onPhase and the typed preparation port passed
  contract checks before behavior. The infrastructure policy is 120000 ms
  total preparation; aggregate output and global watchdog stay. The facade
  adds six callback-forwarding lines, never another native pipeline. The
  worker avoids a second student timer around fallback raw compiler calls.
  Independent RED: 28 failures among 110 selected tests (23 lifecycle/budget
  and five worker integration failures); old controls remained green. The
  initial timer-test diagnostic had an asynchronous assertion cleanup issue,
  corrected before that second RED run without changing expected behavior.
- Current new gates: 136 worker contracts and strict bundle build; 273 core,
  57 browser, 26 IDE, seven webcomponent and 27 retained-UI tests pass. The
  browser suite's existing opt-in Node smoke remains skipped; actual
  sandboxed-browser/WASM controls supply the required runtime layer. Feature
  gates rechecked: coding 130, adapter 16, surface 123, all strict types.
  Total: 795 passing unit/contract/regression tests. Unsuppressed strict lint
  passes all owned feature sources, worker/budget, public type and new facade
  tests. Whole-file facade/type-fixture lint has seven pre-existing unused
  declarations, independently reproduced from HEAD via ESLint stdin. Do not
  suppress them or report the whole-file lint as warning-clean. Root ESLint
  also prints its existing React-detection/pages configuration notices.
- Adversarial audit found a cancellation edge: the fallback's compilation
  scope could remain pending after termination, allowing a later raw tool
  to bypass protect. An independent RED records one unwanted raw call;
  the minimal failed-state check now rejects it before the compilation
  bypass. All 87 budget/worker tests pass. The first complete matrix uses
  its fixed pre-audit bundle: ten previews and ten official controls pass.
  The final guarded bundle was rebuilt and all ten official controls were
  repeated successfully. Preserve both producer hashes, never mixed results.
- Final real proof: C/C++ integer, float, boolean and string public previews
  return [true], their deliberately partial implementations return [true,false]
  officially, and absent-zero targets return [false]. All preview source bytes
  remain unchanged and transient files are removed. Final worker SHA-256:
  bbba59817b176d4617ec0a896c1ab302d43d60303c4ad7aa6638a6b317383948.
  Manifest SHA-256 remains f4b9f7944f4def6678cafa7895bb0f766255683e60b0abcd306196a01a581dd8.
  Three further final-bundle controls pass: retained C++ extern-C/main returns
  [true,true]; a normal C program with a frozen 3000-ms limit passes despite
  10775 ms of total preparation/worker startup; the paired infinite program
  stops in 13145 ms total, with exit 1, sanitized execution-failed diagnostic
  and NO grade receipt. The 3/3 gate exits 0 because that failure is expected.
- Scope: scalar composition and separate budget findings have real proof.
  This does not settle generic undefined imports, graphics/interactive work,
  workbench integration, persona flows or human acceptance.
  Capability eligibility/canonical status are not promoted automatically.
- Resources: one compiler/browser at a time; latest proofs use CPUs 0-7,
  leaving four logical CPUs free. Keep the 4772 MiB available-memory reserve,
  PSI/paging/OOM guard, separate 120-second preparation and frozen student
  limits (at most 30 seconds/2 MB). Final official matrix: peak 8848 MiB,
  minimum available 6573 MiB, 21 MiB global swap activity, zero PSI/OOM/stops;
  safety controls: peak 4705 MiB, minimum available 10861 MiB, no OOM/stop. No fixed
  total-RSS cap, forced allocation of host RAM/swap or unrelated process kill.
- Evidence under ignored apps/web/test-results/coding-surface-refoundation/
  runtime/: 20261123-function-composition-presence-events has passing
  feasibility, all original failed exploratory attempts remain in sibling
  probe/instrumented/presence directories; composer-red-types.json,
  surface-typed-red.json, worker-red.tap and identity-red.json preserve RED.
  20261123-function-production-parity preserves incomplete parity and hashes;
  20261123-function-full-string-diagnostic preserves the paired budget failure;
  20261123-function-official-negatives has three fresh verified receipts.
  Current proof: 20261123-phase-budget-full-matrix (ten current-preview and
  ten pre-audit official controls), 20261123-phase-budget-final-official (all
  ten rebuilt final-worker controls), and 20261123-phase-budget-final-safety
  (three final controls). Sibling *-resources.json and static-gates.json retain
  commands, resources, RED counts and baseline lint debt. The earlier focused
  string proof is retained as 20261123-phase-budget-string-control.
  Reproduction archive: 20261123-phase-budget-repro.tgz; SHA-256
  f51c6a66065cc838303fc0cfecc67c036a383026b05fb11aab31c87b303ef53c.
  It contains diagnostic sources/guard, not another human scenario CLI or
  compiler assets. Rebuild the worker from the tested checkout; adapt local
  paths and use fresh evidence directories, never overwrite old receipts.
- Documentation gate: 49 documents, 187 local links, seven atomic tables and
  25 complete rows pass; git diff --check passes. Static gate logs are in the
  phase-budget-static-gates.json. No full API/web build applies to this
  package/worker slice. SDK/component builds are local consumer checks, not
  a standalone IDE/toolchain build or publication. All owned jobs closed.
- Next action: resume UX03 workbench/runtime-capability integration from the
  updated finding matrix. Preserve unverified combinations as unavailable;
  investigate undefined imports and graphics through their declared boundaries.
  Full-phase closure still needs all applicable gates, including the visible
  upstream lint debt, independent UI/runtime and human acceptance.
- Commit handoff: the user requested separate local commits after validation.
  `3d9aec305e` records capability eligibility; `9f43ccc413` records pure native
  composition and preview integration; `86ff793c62` records the authorized
  optional public phase observer; `fa757f0791` records official native grading
  and separate preparation/student budgets. The synchronized plan, rationale
  and evidence ledger are a separate documentation commit. Committing does
  not change the tested source bytes or close the remaining gates. No push
  was requested or performed; generated ignored evidence is not committed.

## UX03-A capability resolution preparation - 2026-11-22

- Entry: `c733a556c3`; preserve the existing uncommitted continuation notes.
  The user requested continuation of part 3.6. The separate harness question
  has not been answered explicitly; do not infer a new protected-source grant.
- Active slice/stage: independent UX03-A capability resolver / audited foundation.
  No production UI cutover or broader CODE-UX-02/03 closure. Supported
  operations and acknowledged gaps must remain distinguishable without
  launching a runtime or imposing academic policy.
- Allowed files: new runtime capability contracts/resolver and focused tests
  under packages/features/coding-surface/src, index/architecture tests and this
  ledger/ADR/UX03 documentation. Closing synchronization includes the coordinator,
  consumer map and existing identity-duplication entry, not a new ownership rule.
  No Emception/retained UI, API/schema, dependency,
  asset, preset/compiler, dev launcher or existing execution-path change.
- Specification: resolve local operation eligibility from explicit family,
  frozen environment/toolchain, authorized surface mode and the existing
  allowLearnerCreatedFiles assignment flag and host readOnly restriction.
  Authoring forms cannot execute;
  learner tests use public scope only; reviewer diagnostics remain local and
  file creation is unavailable in inspection/validation modes. File creation
  during ordinary authoring does not require a bootable runtime.
- Canvas: target requirement derives from runtime, never family or panel
  visibility. Minimizing graphics cannot remove a required target. Program
  execution is not a function-test verdict. Current function-ABI and official
  graphics findings stay explicit; unverified combinations remain unavailable,
  not a successful result or an academic zero.
- Contract gate precedes behavior: strict G-SURFACE typecheck and the focused
  type-contract suite, then independent behavioral RED. Closing gate is full
  G-SURFACE plus G-DOC and an unchanged protected-source diff. This pure
  foundation needs no API/Next/compiler build, service or account; existing
  real runtime observations bound the declared support and are not rerun or
  promoted by resolver unit tests. Use one process group, CPUs 0-3 and the
  OS-headroom/PSI guard.
- Contract stage: strict package typecheck and all three new type-contract
  tests exit 0 before resolver behavior. No bootstrap failure counted as RED.
  Next: independently assert availability, projection scope, retained canvas
  requirements, frozen identity rejection, file policy and zero runtime boot.
- Behavioral RED: all 27 new cases fail against the typed stub for missing
  resolution, after strict typecheck passes. The raw nonzero result is retained
  in ignored runtime/20261122-ux03a-capabilities/red-tests.json below the
  refoundation evidence directory. No import or infrastructure failure counted.
  Builder now implements only the pure resolver; Auditor/closing gates follow.
- First full suite: 112/112 pass, no skips. Auditor returns to contract stage
  for the existing host readOnly restriction: it must override author/learner
  file creation without disabling permitted diagnostic execution. Add that
  explicit input and independent negative cases before its minimal correction.
- Auditor refinement: the new strict contract compiles; all three selected
  readOnly cases then fail for the missing restriction/validation, with 27
  deliberately unselected cases (not gate closure). The minimal override and
  strict boolean check are now implemented; rerun the complete suite with no
  selection or required skips. Preserve readonly-red.json as failed evidence.
- Closing G-SURFACE: strict typecheck, all 115 tests across eight files and
  unsuppressed strict package ESLint exit 0; no required skips. Preserved
  emception-ui regression passes 27/27 in seven suites. This supplies contract
  preparation for CS-REF-10/11/12/21, not their required browser/runtime/human
  acceptance. No full API/web build applies to this package-only pure port.
- Resources: all tasks sequential on CPUs 0-3 under the OS guard; peak observed
  682 MiB and minimum available 15322 MiB, above the 4772 MiB reserve. No new
  OOM, resource stop or surviving owned job. No runtime/browser boot was needed.
- Auditor: same-agent adversarial review, not independent/human acceptance.
  Sources import only owned types/domain constants; no academic commands,
  storage, network, dynamic import or worker. There is no diff from entry in
  Emception/retained UI, API/platform, presets, dependencies or dev launchers.
  Availability is technical eligibility, not artifact readiness or a result;
  the unchanged production workbench is not cut over by this helper.
- Evidence: ignored apps/web/test-results/coding-surface-refoundation/runtime/
  20261122-ux03a-capabilities contains original RED, read-only refinement RED,
  the complete final Vitest report, command resource reports and verification.json
  with the six source/test hashes and protected-path checks. The read-only
  verifier first failed to spawn git inside the sandbox (EPERM); its identical
  unrestricted rerun passes. That environment failure is not a product RED.
  G-DOC checks 49 documents, 187 links, seven atomic tables and 25 complete
  rows with no failures; git diff --check passes. No canonical status promoted.
- Historical next action, superseded by CS-ADR-036: obtain the separate function-harness authorization before
  changing that protected generator or its dependent function execution path.
  No explicit additional harness grant is recorded; this slice remains owned
  coding-surface-only preparation.
  Preserve explicit function/graphics limitations; do not impose extern-C/main
  boilerplate, invent a host harness or substitute a WASI run for canvas.
  Independent work may proceed under its own atomic specification. Whole
  CODE-UX-02/03 and the broader refoundation remain open.

## Commit and continuation checkpoint - 2026-11-22

- User approved committing the validated changes and continuing part 3.6.
  Preserve the runtime findings and upstream ownership boundaries; no push
  or independent IDE refactor was requested.
- Commits: `33b62e2b4f` records the minimum execution-phase correction and its
  rationale; `18103947ff` records the independently tested lazy runtime port.
  `c733a556c3` groups initial-file semantics with the reconciled plan,
  decisions, evidence and handoff. Generated assets/evidence remain ignored.
- Closing documentation audit: 49 parsed documents, 187 local links, seven
  atomic tables and 25 complete rows; no failures. `git diff --check` passes.
  The source/evidence hashes match the audited worktree. Existing upstream
  lint warnings and the paired pre-existing IDE input defect remain visible.
- Next active slice: UX02-C/UX03 runtime-fidelity investigation, specification
  stage. Read only the public functional-test contracts, existing owned
  assessment mapper/worker integration and the recorded synthetic evidence to
  determine whether supported composition can fix function ABI/startup. Then
  distinguish diagnostic canvas support from official graphics execution.
  No broader upstream change, private import, parallel harness, schema change
  or capability promotion is authorized. If composition cannot meet the
  declared contract, record the exact gap and apply EMCEPTION-GATE before
  dependent implementation. CODE-UX-02/03 remain open.
- Investigation result at `c733a556c3`: the public functional generator accepts
  only index/name options, always emits C++ with an extern-C declaration and
  constructor-run assertions, and supplies no main. The selected C preset
  forces every source to C; the C++ preset forces every source to C++.
  The high-level native facade forwards source/output but not native flags
  into preset compilation. Relabeling the generated file or passing a build
  flag cannot repair the existing language/linkage/startup contract. Prior
  real-worker failures in CS-RUNTIME-FUNCTION-ABI remain the execution evidence;
  this continuation adds source-level confirmation, not a new browser pass.
- Structural stop: CS-ADR-034 explicitly excludes the harness. A new user
  question requests a narrowly scoped public C/C++ functional-harness option
  and explicit function-exercise runner, preserving the current default, IDE,
  presets and compiler flags, with compatibility tests. No protected edit or
  substitute host harness was made. Wait for that answer before designing the
  dependent contract; independently supported work remains permissible.
- Post-commit G-SURFACE rerun: strict typecheck, all 80 tests and unsuppressed
  strict ESLint exit 0. One command at a time on CPUs 0-3 under the OS guard;
  peak owned RSS 631 MiB, minimum available RAM 15081 MiB against 4772 MiB
  reserve, no new OOM or resource stop. No API/Next build, developer service,
  account, database, asset or upstream source change during this continuation.
- Next exact action: resolve the pending harness authorization; if accepted,
  specify and compile its language/startup/scalar contracts before writing
  behavioral tests or implementation. If declined, record the unavailable
  function capability rather than imposing boilerplate or a false fallback.

## IDE compatibility and change rationale audit - 2026-11-22

- User request: justify the authorized Emception changes, verify that the
  extension does not regress the IDE, then write the supporting document only
  after the evidence supports that conclusion. Baseline remains 4a212cd7ff;
  preserve all existing uncommitted changes. No commit or push requested.
- Scope: read the exact public types/facade/engine/worker diff and IDE/UI
  consumers; run their existing builds/types/tests. Use temporary diagnostic
  harnesses and ignored synthetic evidence for paired browser observations.
  No protected production source, compiler flags, dependencies, manifests,
  migrations, dev launchers, real accounts or services may change in this audit.
- Contract first: optional phase preserves old ToolResult assignability;
  api.run and CanvasSession/build discriminants are distinct and unchanged.
  The IDE's terminal path uses runTool/api.run, its graphics path uses
  api.canvas, and assessment extensions may consume runTests. Correctly
  rejecting a tagged build failure in runTests is intentional, not an IDE
  execution-policy change. Current-source SDK types must compile before
  interpreting any browser result.
- Oracles: existing SDK/IDE/retained UI tests; inspect unchanged IDE source;
  paired baseline/current SDK with the same actual IDE component, pinned local
  WASM assets and browser. Check real editing/tabs, terminal stdin/output,
  compilation-error recovery, SDL canvas pixels/cleanup and editor focus.
  Report each observation at its actual evidence layer; mock/type/DOM evidence
  cannot stand in for real compilation or pixels.
- Resources: one heavy job/browser at a time, CPUs 0-3 and the established
  OS-headroom/PSI guard, not a forced all-RAM heap or full runtime rebuild.
  Preserve failed probes and classify baseline/harness/product failures.
- Quality gates: relevant official SDK and IDE tests/builds/types, retained
  emception-ui regression, browser controls and source/hash preservation;
  existing strict upstream lint debt remains visible. A supporting document
  may state scoped non-regression only after those observations, not universal
  compatibility or completed CODE-UX-02/03/release/human acceptance.
- Result: all 13 package commands exit 0: core 273, browser 47 with its one
  existing opt-in skip, webcomponent 7, IDE 26, retained UI 27, public tsd and
  browser/xterm/React/webcomponent/IDE types/builds. Current public interface
  signatures and old ToolResult fields are AST-identical; phase alone is added
  optionally. Raw run is unchanged. IDE/UI, compiler/presets, canvas/runtime,
  worker protocol, functional harness, API and dev launchers have no new diff.
- Paired real-IDE result: eight normal controls pass across baseline/current
  (editing/tabs, independent shell, stdin/error recovery and actual SDL3
  pixels/animation/editor focus/Stop). Two public-engine controls reproduce the
  baseline's invalid-source false approval and verify the candidate rejection.
  No accepted control has page/unexpected console errors; all contexts close
  with zero remaining workers. Worker bundles have equal normalized JS AST
  hashes, not equal raw bytes or waived producer provenance.
- Important retained failure: shell input followed by a program in the same
  IDE session feeds the old shell line to fgets. Both baseline and candidate
  fail the same assertion and exit 1. These are two pre-existing product
  failures, not green/skipped acceptance or an authorized unrelated IDE fix.
  Source API/run/terminal/worker preservation and paired reproduction support
  non-regression classification, not a claim that all IDE flows work.
- Resources for all 12 browser observations: peak 5708 MiB, minimum available
  11045 MiB against the 4772 MiB reserve; no new OOM or resource stop. SDK gates
  peak 775 MiB. One heavy task/browser, CPUs 0-3, no full toolchain/Next/API
  build, service/data reset, publication or commit. Both local asset trees'
  29 bundle hashes pass; strict upstream lint still has seven unchanged warnings.
- Evidence: ignored apps/web/test-results/coding-surface-refoundation/ide/
  20261122-phase-compatibility/{report.json,source-audit.json,gates.json}, individual
  reports/logs/screenshots/resources and repro.tgz. Archive SHA-256:
  `339520b52e99b058ed01f2f73f4d08ab352b3dc81db173b0dde85295d71460fb`.
  Preparation failures (CJS/ESM fixture resolution, missing generated snapshot
  module and Monaco autoindentation) remain distinct from product observations;
  no product rule or compiler option was weakened to close the comparisons.
- Supporting explanation written only after compatibility evidence:
  [Emception phase extension rationale](./EMCEPTION-PHASE-EXTENSION-RATIONALE.md).
  It details necessity, alternatives, every production responsibility, IDE
  impact, intentional engine behavior change, the pre-existing input defect,
  compatibility limits and normal release requirements. No canonical status
  or broader phase gate is promoted by this audit.

## Authorized execution phase and initial-file semantics - 2026-11-22

- Entry: `4a212cd7ff`; preserve the uncommitted UX03-A lazy-port/assets/docs
  preparation recorded below. The user explicitly authorized (1) the minimum
  public execution-phase extension and (2) initial-file semantics, allowing
  main in another translation unit. These answers resolve the two pending
  questions; they do not authorize a general Emception refactor.
- Active row/stage: CS-RUNTIME-COMPILE-FAILURE correction and
  CS-RUNTIME-NATIVE-ENTRY disposition / audited implementation. CODE-UX-02/03 remain open
  for function ABI, graphics and the broader acceptance requirements.
- Allowed source: core ToolResult types/runtime validation/type tests; browser
  createEmception facade and focused tests; the existing public test engine's
  consumption of that metadata and its tests; the grading worker's phase guard
  and composition tests; coding entry-path documentation and owned surface
  Initial file labels/tests. Update the shared governance/ADRs/gap/consumer and
  duplication records. No platform/API/schema, IDE/UI, compiler flags, harness,
  dependency/version/lock change, dev launcher or new executor.
- Contract: additive optional phase = compile | link | run. Existing raw-tool
  results without metadata remain assignable; every browser compileAndRun
  outcome supplies its actual terminal phase. Optional omission is not proof
  of execution: official grading rejects missing/unknown phase as unavailable.
  Compile/link failures cannot satisfy stdio/stdio-file/doctest assertions;
  completed runs retain their existing output/exit semantics, including an
  explicitly expected nonzero program exit. No diagnostic-text guessing.
- Initial-file contract: entryPoint.path keeps its serialized name and public
  source-file validation; it selects the first editor file, not main ownership.
  Compile/link all source translation units, preserve valid multi-file work
  and existing source ordering. No mandatory boilerplate, symbol regex check,
  database rewrite or new teacher restriction. Function symbols keep their
  separate signature contract; the function ABI finding is not fixed here.
- Atomic cycle: compile the additive types and legacy assignability first;
  add independent failing phase/comparator/worker tests; implement only this
  behavior; build the current TS SDK and run its complete suites/type tests;
  then exercise the real worker with the verified published 4.5.2 WASM assets.
- Oracles: compile/link failure with empty expected output (even matching exit)
  yields false, valid empty output passes, expected exit 1 remains valid,
  missing phase is infrastructure failure, main in another source passes,
  renamed main/helpers pass, duplicate/no main fails linking. Retain historical
  audit evidence; replace only the previously provisional entry oracle with
  the explicitly approved behavior.
- Gates: core build:ts/test/test:types, browser build/typecheck/test, complete
  worker Node selection plus strict tsc, G-SURFACE and affected host tests/types,
  real-artifact worker controls, G-DOC and unchanged CI artifact-policy tests.
  Exact-source producer requirements remain required; local diagnostics are
  not a release/provenance waiver. One heavy task/browser at a time, CPUs 0-3,
  OS-headroom/PSI guard; no fixed 14 GB ceiling or unrelated process cleanup.
- Contract gate passed before behavioral changes: official core build:ts/tsd
  validates the optional field, old result-shape assignability and invalid
  phase rejection at type level. Behavioral RED selected 27 cases: 20 failures
  (missing facade metadata, false build approval and missing runtime validation)
  and seven existing positive controls. No bootstrap error counted as RED.
  Core implementation now passes its complete 273-case suite; browser SDK
  build and 47 tests pass with its pre-existing opt-in smoke left skipped.
  Actual browser/worker evidence remains required, not inferred from that skip.
- Additional RED: four official-worker unknown/missing-phase cases failed to
  reject before the guard (two valid-stage controls passed). The owned surface
  Initial file case failed on the old Entry point label while its selected
  helper/other-main workspace rendered normally. Minimal guard/label changes
  pass the closing regressions and real-artifact controls below.
- Closing gates: core 273/273 plus build:ts/tsd; browser build/typecheck and
  47 passes, with its one pre-existing opt-in smoke still skipped; worker
  selection 110/110 plus strict tsc; surface 80/80 plus strict types/lint.
  Coding 96/96 and adapter 16/16 plus both strict typechecks; retained
  emception-ui 27/27; affected authoring/learner/review web selection 135/135.
  Full web tsc exits 0 under the OS guard, peak 1874 MiB and minimum available
  14351 MiB; no new OOM, resource stop, Next/API build or full-stack startup.
  Preflight contracts 26/26, both actual manifest checks and unchanged artifact
  acquisition-policy tests 36/36 pass. Expected duplicate-ZIP fixture warnings
  remain visible; no exact-source producer acceptance is inferred.
- Real proof: current-source SDK/worker bundle, published 4.5.2 WASM assets,
  official worker.mjs and sandboxed local Chrome, one request at a time.
  All 12 receipts match their request/manifest hashes and declared booleans:
  valid C/C++ empty output, expected/unexpected nonzero exit, compile/link
  failures with empty stdout or matching exit, main in either translation
  unit, duplicate main and absent main. Peak owned RSS 3698 MiB; minimum
  available 13047 MiB against the 4772 MiB reserve; no new OOM or resource stop.
  Candidate evidence: ignored apps/web/test-results/coding-surface-refoundation/
  runtime/20261122-phase-and-initial-file-audited/{report.json,verification.json}.
  Repro archive SHA-256:
  `24af4437d585b08bb17963b2d3aaecbe40fbe66f79ed6770a2870441a67d7fca`.
  report.json/verification.json record exact changed-source and bundle hashes.
- Preserve the first attempt in sibling 20261122-phase-and-initial-file:
  an undefined-function fixture was incorrectly assumed to fail linking; a
  header/printf multi-file control exceeded the unchanged 30-second budget.
  Neither is counted as a pass. A separate public-API probe proves that this
  preset permits undefined imports and reports run/exit 0 for that fixture,
  while duplicate/absent main reports link/exit 1. Final controls use those
  confirmed linker failures and header-free helpers with explicit exit 7;
  no compiler flag, budget or assertion was weakened. The import behavior
  remains CS-RUNTIME-UNRESOLVED-IMPORT for owner review, not fixed or promoted.
- Auditor: same-agent adversarial review, not independent/human acceptance.
  Strict upstream whole-file ESLint remains NONZERO: six existing unused-variable
  warnings in createEmception.ts and one in core/test-d/index.test-d.ts.
  Running the identical unsuppressed linter on HEAD/current stdin proves the
  same seven diagnostics, zero introduced. Baseline report is preserved beside
  the candidate evidence; no cleanup outside the authorized scope or lint waiver.
  SDK methods/wire names, source ordering, time/output limits, compiler flags,
  harness, IDE/UI, platform/API, migrations, dependencies and dev launchers
  remain unchanged. G-SURFACE lint passes; this is not an all-green upstream gate.
- G-DOC passes: 48 parsed documents, 174 local links, seven atomic tables,
  25 complete rows and no failures; git diff --check passes. The preserved
  upstream lint comparison is a debt classification, not a replacement gate.
- Next: continue the explicit function-ABI/graphics investigation and capability
  disposition before the production workbench cutover. The two user questions
  are answered (CS-ADR-034); do not ask them again or redo OOM diagnosis.
  CODE-UX-02/03, broader runtime/resource/human acceptance and upstream lint
  debt remain open. No canonical capability status, commit or push was changed.

## Refoundation resume: UX03-A preparation - 2026-11-22

- Baseline: `4a212cd7ff`, clean and synchronized feature/coding-grading.
  The user requested continuation of part 3.6 after the develop merge. This
  entry supersedes the historical merge-only task scope below, not its evidence.
- Active row/stage: UX03-A independent preparation / specification. UX02-C's
  official-runtime acceptance remains open. Do not cut over the production UI
  or claim CODE-UX-02/03 closed while the reviewed runtime findings remain.
- Allowed files: new `runtime-types.ts`, `runtime-session.ts` and focused
  contract/behavior tests in `packages/features/coding-surface/src/`, its
  `index.ts`/architecture tests, this ledger and the affected coordinator,
  UX02/UX03 status/ADR entries. Existing generated local release/CDN assets may
  be refreshed only after a verified backup, using the existing sync helper.
  Isolated synthetic acquisition/build/browser probes may live under /tmp;
  sanitized outputs/backups belong in the ignored refoundation test directory.
  Closing documentation also updates CONSUMER-MAP.md, PRODUCT-MODEL.md,
  EMCEPTION-CAPABILITY-GAPS.md and the root DUPLICATION-REGISTER.md to reconcile
  the observed asset/preparation state, not change product requirements.
  No dependency, API, database, dev launcher or protected upstream source edit.
- Specification: creating/inspecting a runtime session loads no browser module,
  fetches no manifest and creates no worker. Only explicit acquire may boot.
  Reject an unsupported/mismatched frozen identity before runtime creation;
  share one pending acquisition; reuse only this session's runtime; dispose
  idempotently, abort metadata loading and dispose a late-created instance.
  Failures are explicit and cannot become ready. This port has no academic
  command, compiler implementation or case/answer payload.
- Contract: typed session state, injected public-module/manifest ports and
  factory, compiled before behavior. Public Emception parser validates manifest
  structure; owned checks compare its release/ABI/lock/receipt to the requested
  coding identity. Metadata compatibility is not binary provenance or a run.
- Oracles: CS-REF-12/21 preparation; lazy/no-worker, identity mismatch, schema-1
  rejection, shared acquisition, explicit retry, boot failure, disposal during
  fetch/boot and no late ready state. Behavioral RED must precede implementation.
- Gates: G-SURFACE typecheck, full tests with one worker and strict ESLint;
  G-PREFLIGHT-UNIT plus both actual local manifest checks and byte hashes after
  backed-up synchronization; G-DOC. No API/Next build or full-stack startup.
  Use the OS-headroom/PSI policy, sequential work and CPUs 0-3, not a fixed
  14 GB ceiling. Existing services/data remain untouched.
- Historical structural questions sent to the user: whether to extend the public result
  minimally with execution phase, and whether native entry means initial editor
  file or mandatory main owner. Dependent runtime fixes remain stopped; do not
  infer approval or replace the public compiler/test engine. Both answers were
  subsequently authorized and implemented under CS-ADR-034 above.
- Contract evidence: coding-surface strict typecheck and the three new port
  contract cases pass. The first pnpm invocation lacked Node on PATH and is
  classified as bootstrap failure; the unchanged command with the installed
  Node 24.16.0/pnpm 10.34.6 passed. Next: behavioral RED, then implementation.
- Behavioral RED: the capture/identity acquisition case failed because the
  typed stub rejected acquisition as unimplemented; no import/bootstrap failure
  was counted as RED. Minimal implementation now passes 22 lifecycle cases.
  The real public parser runs in Node for these non-DOM tests. Vitest's regex
  rejection matcher reported an empty message; exact rejected-object message
  assertions pass and retain the stronger disposed-error oracle. No assertion,
  dependency or production rule was relaxed.
- Asset precondition RED: both actual local manifests fail the existing
  frozen-toolchain check (schema 1). Backup/refresh remains necessary; no file
  has been overwritten by this check.
- Builder result: the public `createCodingRuntimeSession()` port is available
  but deliberately not wired into the old production workbench. It performs a
  public-parser metadata preflight, lazy boot, single pending acquisition,
  explicit retry and cancellation/disposal of only its own runtime. No program
  compiler, test engine, new worker implementation or academic command exists
  in this port. CS-ADR-033 records the boundary and proof limits.
- Asset result: the previously verified published emception@4.5.2 archive was
  rechecked against SHA-512 SRI; all 29 bundle hashes passed before copying.
  The existing sync helper refreshed canonical and served generated assets;
  both current manifests and all copied bundle hashes pass. Old schema-1
  copies remain byte-preserved under ignored
  `apps/web/test-results/coding-surface-refoundation/assets/20261122-4a212cd7ff/`,
  with metadata. No manifest hand-edit, download fallback or source build.
- Real-browser diagnostic: isolated current-source consumer bundle with the
  same public entry aliases as the official build, existing Chrome and exact
  published assets. Zero workers or asset fetches before acquire; concurrent
  acquire returns the same promise/API; one real worker boots; disposal leaves
  zero workers; wrong release is refused without a second worker. No page
  errors, program compilation, case verdict, API/Next/database or persona.
  Final report: ignored
  `coding-surface-refoundation/runtime/20261122-ux03a-audited/report.json`
  below apps/web/test-results. This is boot-only evidence, not G-RUNTIME closure.
  Current source SHA-256 is
  `ae350c7919844b168ad19539dc59d1480f69874e1757924317a162702b52fc01`.
  Its sibling repro.tgz preserves the scratch scripts/OS guard; SHA-256
  `1e782349e99edc96c1bd1735615e4841738dd633cdd85b92cfe3b5ae572f4009`.
  Fresh scratch/output paths are required when replaying; do not overwrite
  previous evidence. No real account, answer or token is included.
- Resources: asset refresh/build/browser peaked at 101/219/1178 MiB owned RSS;
  minimum available RAM 18042/18234/18047 MiB, above the 4772 MiB reserve.
  No new OOM, resource stop or sustained memory pressure. Four CPUs, one task
  at a time, no fixed 14 GB ceiling. Existing swap occupancy was not treated
  as failure. All owned browser/server processes closed.
- Final audited-source rerun: diagnostic build/browser peak 252/1003 MiB,
  minimum available 17587/17507 MiB, no stop/new OOM. Metadata identity,
  lazy/single worker and real disposal controls all pass again. Vite's two
  existing upstream static/dynamic-import warnings remain visible; no
  warning-clean production build is claimed from this diagnostic bundle.
- Auditor (same-agent self-review, not independent/human acceptance): fixed
  loss of the original boot error by preserving Error.cause without exposing
  raw detail in UI state. The independent cause assertion failed before this
  correction; final full suite passes 79/79, zero skips. Public imports, no
  worker at construction, protected paths, late disposal and no academic
  commands were reviewed. No types, assertions or rules were weakened.
- Closing commands: `pnpm --filter @game-guild/coding-surface typecheck`,
  `pnpm --filter @game-guild/coding-surface test --maxWorkers=1` and
  `pnpm exec eslint --no-inline-config --max-warnings=0 packages/features/coding-surface/src`
  all exit 0. The unchanged G-PREFLIGHT-UNIT selection passes 26/26 outside
  sandbox subprocess restrictions; both actual local manifest checks pass.
  `pnpm --filter @game-guild/emception-ui test` passes 27/27 in seven suites.
  G-ARTIFACT-POLICY passes 36/36; its intentional duplicate-ZIP warnings were
  not suppressed. These are tests, not exact-source producer acceptance.
- G-DOC: 48 parsed documents, 173 local links, seven specification tables and
  25 complete atomic rows; no failures. git diff --check passes. Protected
  upstream, UI, platform/API, dev launchers and dependency manifests have no
  task diff. No backend/full-web build is needed for this package-only port.
- Handoff superseded by CS-ADR-034 above: both scope questions are answered;
  only the explicitly approved phase/initial-file correction was implemented.
  CODE-UX-02/03 remain open; no instructor/learner UI cutover or human runtime
  acceptance is claimed by this independent preparation.

## Develop integration - 2026-11-22

- User committed the audit/deferrals as 094764ffb1 and authorized proceeding
  with develop integration. Clean entry, feature/coding-grading; fetched
  origin/develop e0567d4224, 25 local and 197 incoming commits. No push requested.
- Active task: merge only, preserving both branches' functionality and migration
  history. Runtime corrections and instructor controls below remain deferred;
  no phase/capability gate is closed by this merge.
- Manual scope: combine coding and mandatory API-security CI gates/tests;
  remove duplicate dependency keys while preserving develop's locked 3.1.0;
  regenerate client output from current integrated OpenAPI using official tools;
  maintain this ledger and the user-revised resource policy. Accept incoming
  platform/Emception changes without
  opportunistic modifications. No dev launcher, IDE, compiler or data reset.
- Verification: strict YAML/JSON and lockfile consistency, fail-closed required
  gate tests, CI/artifact contracts, coding/adapter/surface and affected host/
  client tests and types. Run the required API security suites and current host
  build under measured limits; classify external/resource failures explicitly.
- Resources (user revision, 2026-11-22): 14 GB is not a fixed ceiling. Use
  sequential heavy checks and CPUs 0-3, preserving at least two CPU cores.
  Preserve OS headroom and stop owned work on sustained memory pressure or
  swap thrashing; existing swap occupancy alone is not pressure. No forced
  heap/RSS ceiling, swap/cache tuning, unrelated process termination or full
  runtime/stack startup. This supersedes earlier local build budgets, not the
  separately deferred exercise-memory feature.
- Status: integration resolved and verified for the authorized merge commit.
  Host/full-solution builds, current-host API selection and final OpenAPI/client
  provenance pass. No unmerged paths. The external Authentication failure
  below remains visible; no claim that every CI suite is green. No push.
- Early checks: frozen install with scripts disabled passed on pnpm 10.34.6
  (1160 MiB peak, 12526 MiB minimum available). Combined required-gate tests
  pass 63/63; nine changed JSON manifests have no duplicate keys, workspace
  overrides match the parsed lockfile, and all 17 workflow jobs have valid
  dependencies. Sandbox subprocess failures were rerun with the same tests
  outside that restriction; they are not product failures or waived tests.
- Preservation checks: incoming Authentication/Authorization/Ledgers, upstream
  core/browser/IDE/UI and the migration tree match develop exactly. Our coding
  packages, owned Courses/CodeAdapter source, grading worker and dev launchers
  match the pre-merge branch. Five incoming migrations plus their designers
  and snapshot are retained; no local database was opened or upgraded.
- First current-host build stopped at its 600-second command budget after
  rebuilding the incoming references. Guard exit 1, sampled peak 3938 MiB and
  minimum available 12353 MiB; no OOM or reported C# error. The child's exit 0
  during termination is not a successful build. This was a historical attempt,
  not the resource profile authorized by the subsequent user revision.
- Warm-reference retry failed with managed OutOfMemoryException in Csc after
  387020 ms, sampled peak 6834 MiB and minimum available 8443 MiB. This invocation
  forced workstation GC and a 6 GiB managed heap: an already documented
  insufficient profile, not a new C# defect or kernel OOM. Repeating that profile
  was an execution mistake. No owned dotnet/csc process remains. Do not repeat
  the old heap-tuning investigation or treat stale host output as current.
- The subsequent user reply replaces the proposed fixed 14 GB retry with an
  OS-friendly policy. The next host build uses SDK-default server GC without
  inherited artificial heap caps, one MSBuild worker and a temporary owned-
  process monitor. Preflight checks and sampled available RAM, memory PSI,
  swap-in/out and OOM counters must remain observable; a resource stop is
  never a successful gate. Validate the monitor's decision rules before use.
- OS-friendly monitor: 8/8 synthetic pressure-policy tests and explicit owned-
  child success/deadline-cleanup controls pass. Documentation/link gate passes.
  The uncapped four-CPU host attempt was correctly stopped for sustained low
  headroom after 261018 ms: 14037 MiB peak owned RSS, 4632 MiB minimum available
  against a 4772 MiB reserve, 547 MiB aggregate paging and no new kernel OOM.
  Child exit 0 during cancellation is not success; no owned compiler survives.
  Report: /tmp/coding-merge-develop-host-20261122-resources.json.
- Next measured profile: two CPUs and process-local GCConserveMemory=9, with
  SDK server GC, no heap/RSS ceiling, the same strict source/analyzer set and
  the same pressure monitor/deadline. This is a deliberate concurrency/GC
  reduction after reviewing the stop, not an automatic identical retry or
  increased memory grant. It changes no repository build/runtime setting.
  The SDK documents the memory/collection-frequency tradeoff in
  [Conserve memory](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector#conserve-memory).
- Current integrated host PASS with that economical profile: strict Release
  build, all project references/analyzers/generators retained, 0 warnings and
  0 errors, 412027 ms. Peak owned RSS 13679 MiB, minimum available 5025 MiB
  against a 4772 MiB reserve, maximum memory PSI 0/0, 9 MiB aggregate paging,
  no resource stop or new OOM. No fixed 14 GB ceiling or heap cap was used.
  Report: /tmp/coding-merge-develop-host-conserve-20261122-resources.json.
  This proves the integrated host build, not the pending API test selection,
  OpenAPI capture, full solution, live startup or runtime acceptance.
- OpenAPI reconciliation PASS: existing exporter built strict Release, 0
  warnings/errors; its host DLL/XML and Courses DLL hashes match the fresh
  binaries. Offline v1 capture contains 1803 paths and 2376 schemas, including
  all four coding execution schemas. All 2181 currently integrated method/path
  operations match exactly; no additions/removals. No application/database or
  hosted-service startup. Artifact: /tmp/coding-merge-develop-20261122-v1.json.
- Official client generation PASS: 38513 ms, 2278 MiB peak, no resource stop.
  SHA-256 comparison of all 233 generated files against the pre-generation
  merged tree proves only .metadata.json changed. All generated TypeScript
  remains byte-identical to the 1794-test/type/build-validated merged source;
  its incoming request-body/MFA and our coding contracts are preserved.
  Metadata hash a7d0cf66b79d997701a6606e52c0a4c4408faf6dd446978d5aeffe0886884a83.
  Metadata was staged; the index has no unmerged paths and passes diff --check.
- Broader solution attempt reached its 1200000-ms command deadline while
  compiling Identity.Provisioning.UnitTests near the solution's tail. The csc
  exit 143 follows our SIGTERM, not a preceding C# diagnostic. Guard exit 1;
  peak 14050 MiB, minimum available 4696 MiB (only transiently below reserve),
  maximum memory PSI some/full 3.21/2.64, 1970 MiB aggregate system paging,
  no new OOM. This attempt is incomplete, not green. The final host and API
  test assembly compiled before the stop and have matching host DLL hashes.
  Resume the exact strict solution incrementally under the same OS safeguards;
  do not exclude projects or inflate a test timeout. No compiler survives.
- Full solution PASS on the explicit incremental continuation: exact strict
  Release solution, 0 warnings/errors, no excluded projects/analyzers, 38513 ms,
  1192 MiB peak, 18812 MiB minimum available, no pressure/stop/OOM. The earlier
  time stop remains recorded, not retroactively green. All current solution
  output is built; API tests use --no-build only after this successful build
  and explicit equality of the host DLL in the test output and final API bin.
- Current-host API architecture/security/OpenAPI/coding-migration selection
  PASS: 526/526, zero skips, 172514 ms, 3946 MiB peak, no stop/OOM. Covers the
  incoming required-body and step-up protocol tests as well as endpoint guards,
  dependency boundaries and historical coding migration assertions. Evidence:
  artifacts/test-results/merge-develop-20261122/api/api-architecture-security-openapi.trx.
- The solution rebuild changed the host binary hash. The first offline
  capture remains a valid earlier build record, but renew exporter/capture
  against the final solution output before closing metadata provenance.
- Final provenance PASS: exporter refreshed against already-built solution
  references, strict warnings, without another host compilation. Its API DLL/
  XML and Courses DLL match the final originals; API test output also matches
  final host b9e235bea1e85e9ec121c646e2f4fe9c79ccbc533c51076dffd6912f61f2a75e.
  /tmp/coding-merge-develop-final-20261122-v1.json passes the same complete
  endpoint/schema preflight and is byte-identical to the official generator's
  earlier input (raw SHA-256 ae450ceda56d37c22e28c0970564f939a547c66aabfd92047696a60350f5cf4c).
  No second generation or manual metadata edit is necessary for identical
  input bytes. Normalized generator hash remains a7d0cf66b79d997701a6606e52c0a4c4408faf6dd446978d5aeffe0886884a83.
- Package regression: coding 96/96, code adapter 16/16 and surface 46/46;
  all three strict typechecks pass. No skips; sampled test peak 768 MiB.
- Environment repair: the first wider Node selection found five browser-hook
  failures because the pinned headless shell was absent, stale unreferenced
  Next 16.4.2 cache content, and a mismatched installed picomatch patch. Frozen
  forced install repaired both patch hashes without changing the lockfile.
  All seven Next consumers resolve 16.4.5, and 16.4.2 is absent from the lock;
  its two orphan directories were archived under /tmp, not source-deleted.
  Installed only the already-declared Playwright headless shell. No assertions,
  security versions or test selection were relaxed. The identical selection
  now passes 284/284, zero skips (557 MiB peak); the count excludes the obsolete
  Next instance after its removal from the derived workspace cache.
- Authentication current-source Release suite: 4752/4753, zero skips; only the
  known GenerateEmailCodeAsync_ThrottlesResendsWithinSixtySeconds failure remains.
  The module is unchanged from develop and is not patched here. Guard exit 1,
  1250 MiB peak. Its default TestResults TRX preserves the full result; a shell
  quoting mistake affected the requested report destination after the test
  finished, not the independently reported failing assertion/count. Later test
  logger arguments are quoted explicitly. No green Authentication claim.
- Authorization current-source Release suite: 2815/2815, zero skips; strict
  build/test exit 0, 867 MiB peak. Evidence is in the ignored
  artifacts/test-results/merge-develop-20261122/authorization/authorization.trx.
- Client: current merged output passes all 1794 tests in 116 files, strict
  source types, full ESM/CJS/DTS build and public API declaration contracts.
  Peaks: 1352/5046/3308/690 MiB respectively. Existing DTS notices about private
  NetworkError/Transport exports remain visible. Final official generation
  changed only metadata, with byte-identical TypeScript and final-capture
  provenance verified above; no gate was waived. Post-reconciliation MFA and
  mandatory-mutation-body HTTP contracts also pass 6/6, zero skips.
- Final solution-output domain regression: Courses 1227/1227, CodeAdapter
  179/179 and Assessments 453/453, zero skips. All three use the freshly built
  Release binaries; peaks 489/430/505 MiB. TRX evidence is in the corresponding
  courses/code-adapter/assessments subdirectories of the ignored merge report.
- Web: all 17 requested filters exist; 325/325 tests in 22 files pass, covering
  coding consumers and incoming sign-in/MFA/session changes. Full configured
  web tsc passes (6249 MiB peak, 9927 MiB minimum available). No web build,
  application startup, real-user login or academic runtime acceptance is claimed.
- Final terminal/self-review checks: strict selected root lint exits 0 with no ESLint
  diagnostics; existing root React-detection/pages-path notices remain visible.
  Worker strict types and 104/104 Node contracts pass, zero skips. Documentation
  audit remains 48 documents, 173 local links and 25 complete atomic rows. No
  CI, type or test rules were weakened; runtime bugs below remain open.
- Handoff: conclude the authorized non-interactive merge commit after the
  final staged diff/link/marker audit. No push is authorized. Keep the external
  Authentication failure separate and preserve its failed evidence; do not
  opportunistically patch the platform. Further work starts from the deferred
  follow-ups below, not a new claim of native execution or academic acceptance.
  Apply PRODUCT-MODEL.md's OS-friendly policy to future checks, not historical
  fixed 10/14 GB ceilings or the failed 6 GiB workstation profile. No runtime
  correction, instructor-control implementation or developer service change
  was part of this merge. No owned compiler/browser is left running.

## Deferred corrections and instructor controls - 2026-11-22

The user originally requested recording these follow-ups for later implementation.
CS-ADR-034 subsequently authorizes the minimum phase extension and initial-file
policy; their implementation/proof is at the top of this ledger. Remaining
items and instructor controls are still deferred, not newly authorized by this
handoff. The existing runtime
[audit](./RUNTIME-RISK-VALIDATION-2026-11-22.md) retains the reproductions and
qualifications. No new capability status or completed gate is inferred.

### Runtime corrections

Resume within [UX03-A/B](./06-refoundation/03-workbench-runtime-and-capabilities.md)
after its prerequisites. The phase correction and initial-file policy now have
scoped real-worker proof. Resolve function ABI and graphics before advertising
their combinations. Independent work may proceed,
but an affected capability cannot be advertised as ready while its oracle fails.

| Finding | Deferred action | Acceptance requirement |
| --- | --- | --- |
| `CS-RUNTIME-COMPILE-FAILURE` | corrected under CS-ADR-034; retain public phase and official missing-phase guard | current-source real-worker controls reject compile/link failures, including matching exit; valid empty/nonzero-exit controls pass; no broader gate promotion |
| `CS-RUNTIME-NATIVE-ENTRY` | initial editor file explicitly approved; no mandatory main location or serialized rename | real-worker controls pass main in either source and reject duplicate/absent main; owned Initial file label and selection test pass |
| `CS-RUNTIME-FUNCTION-ABI` | compose language, harness linkage and startup through supported public APIs | an ordinary C/C++ function-only solution works without incidental main/extern-C boilerplate; helper files work when allowed; intentionally wrong solutions fail |
| `CS-RUNTIME-GRAPHICS` | use the public canvas lifecycle for graphical diagnostics and define official execution support separately | real supported-artifact rendering/input/resize/cleanup; unsupported official execution is unavailable, not a passing WASI fallback, zero grade or invented visual judge |
| `CS-RUNTIME-UNRESOLVED-IMPORT` | investigate the observed undefined-import policy with the Emception owner before changing linking/runtime behavior | an undefined-function fixture returned run/exit 0; phase metadata must not be misrepresented as proof that external symbols are implemented |

These corrections do not authorize upstream edits beyond CS-ADR-028/034.
Preserve the compiler, harness, IDE/UI and remaining core/browser source; use
EMCEPTION-GATE for an additional genuine public gap. Preserve
published attempts/revisions and the distinction between compiler failure,
learner error and unavailable infrastructure.

### Instructor configuration proposals

Revisit [UX04-A](./06-refoundation/04-instructor-authoring-and-validation.md)
with the relevant UX02 contracts before adding controls. These are proposed
enhancements, not confirmed defects or a newly accepted persistence model.

- Preparation: let the instructor choose how much code is supplied versus
  written by the learner. Function implementation, completing starter code and
  writing a whole program are teaching choices; completion need not become a
  third exercise family. Specify who supplies the driver/main, imports and
  helpers instead of allowing a runtime defect to impose those requirements.
- File freedom: reuse mutable/read-only files and allowLearnerCreatedFiles;
  evaluate required files or narrower editable regions only if the teaching
  task needs them. Do not claim those additional restrictions already exist.
- Assistance: consider completion, snippets, formatting, static diagnostics,
  correction suggestions, authored hints and optional AI assistance as separate
  controls. Editor assistance is not AIReview or academic review policy.
  Offer only capabilities proven for the selected language/environment.
- Interaction: use a selector for mutually exclusive preparation choices and
  switches/checkboxes for independent options. Reject contradictory settings;
  do not expose every combination merely because it fits a boolean model.
- Enforcement: future settings must roundtrip through draft/API/delivery and
  remain bound to the published revision. The trusted submission/execution
  boundary validates file rules; hiding or disabling controls is insufficient.
  Disabling assistance must not hide actual execution/compiler errors. Editor
  restrictions do not prevent use of tools outside the application.
- Scope: defaults, precise option granularity and entry semantics remain open
  until specification/ADR review. Reuse declared Monaco/public Emception APIs;
  no library installation, platform change or speculative table/migration is
  implied. A proven storage change still requires SCHEMA-GATE and its migration.

### Resume and verification

Before implementation, select one follow-up and record its allowed paths,
contract, independent failing oracle and exact terminal gate in this ledger.
Follow Specification -> Contract/schema -> Test -> Minimal implementation ->
Quality gate. Apply G-CONTRACT/G-SURFACE and affected G-WEB/G-API for persisted
settings; runtime fixes also need real-artifact G-RUNTIME controls, not mocks
alone. Human UX acceptance and canonical capability review remain separate.

Current edit scope: this ledger and links in the UX03/UX04 plans only. G-DOC
requires parsed local-link/atomic-table checks and git diff --check. No product
test, application build, runtime execution, merge or automatic commit is part
of this documentation task.

G-DOC passed: installed remark/GFM parsed 48 documents and checked 173 local
links, both new heading anchors and all 25 atomic rows across seven specification
tables. git diff --check also exited 0. The audit distinguishes specification
tables from the existing three-column review checklist; no table was weakened.

## Runtime-risk validation entry - 2026-11-22

- User request: independently validate the four CS-RUNTIME findings, not fix
  them or merge develop. Entry HEAD `e3402737304571e3c92e5b9a11bb3f2b26ec471c`,
  clean worktree. No application/API/database startup or real personas needed;
  this is an isolated executor audit, not an end-to-end academic grade/release.
- Invariants: preserve production source, upstream IDE/UI/runtime, existing
  generated assets and frozen identities. Do not replace an unavailable
  compiler with a simulator and claim runtime evidence. Public API mocks can
  establish composition only. No canonical capability status promotion.
- Contract/oracles: CS-RUNTIME-COMPILE-FAILURE compares a valid empty-output
  program with an invalid program under the same empty expectation; failed
  compilation must not pass. CS-RUNTIME-NATIVE-ENTRY tests a renamed valid
  main with helpers, a selected source without main plus another main, and
  duplicate mains; distinguish observed linking from unspecified entry policy.
  CS-RUNTIME-FUNCTION-ABI tests ordinary C/C++ functions with and without a
  startup main and a compatible-linkage positive control. CS-RUNTIME-GRAPHICS
  distinguishes the official WASI path from the public canvas lifecycle; a
  preset label, error or blank canvas alone is not graphical runtime proof.
- Sequence: verify current strict worker contracts; cheapest real-comparator
  reproduction; frozen asset acquisition/preflight; actual current-source
  worker tests with independent controls; adversarial classification/report.
  Preparation/bootstrap failures are blockers, never behavioral red evidence.
- Allowed edits: this ledger and the existing capability-gap record; isolated
  synthetic audit fixtures/build/probes under /tmp. Add a compact report with
  exact commands if findings are reproducible. No product implementation,
  package/lockfile/CI changes, source release dispatch or automatic commit.
- Resources: one browser/compiler at a time, four CPUs (0-3), ordinary <=10 GB
  total owned RSS, >=4 GiB system headroom and bounded command deadlines.
  Use the existing owned-process guard; stop no unrelated development service.
  Published 4.5.2 metadata/manifest matches the frozen identity, but both
  existing local manifests are schema 1. Acquire into scratch only; do not
  overwrite them or infer asset readiness from the registry metadata.
- Completed evidence: [runtime-risk report](./RUNTIME-RISK-VALIDATION-2026-11-22.md).
  Three candidate bugs and one entry-policy decision, not four unquestioned
  product defects. Thirteen real-worker probes plus two public-canvas controls;
  explicit exit-0 C/C++ and compatible-linkage controls pass. Invalid source
  is approved under an empty expectation, ordinary/function-only solutions
  fail, and the entry file does not own main. Public canvas renders exact
  pixels, whereas native compile/link/run exits 0 without running SDL callbacks.
  The generic whole-worker graphics failure does not independently prove its
  cause. The initial mismatched SDL fixture is discarded as harness evidence.
- Publication assets: isolated exact 4.5.2 archive SRI and all 29 bundle hashes
  pass; original local manifests remain untouched. Temporary bundles use
  current public sources without changing them. Source hashes, receipts and
  captures are retained in the ignored archive identified by the report.
- Resource outcome: maximum sampled owned worker/browser RSS 5727 MiB,
  minimum system available 12817 MiB; no OOM or guard stop. Four CPUs, one
  browser/compiler at a time, no API/Next/database boot. Strict current worker
  and fixture tsc pass. Desired-behavior red remains red; no rules were waived.
- Next: review the entry-policy decision and correction scope. No product fix,
  upstream edit, phase advancement, canonical status promotion, merge or
  automatic commit is authorized or implied by validation.
- Closing checks: unchanged worker Node suite 104/104, zero skips; strict
  worker/fixture types and documentation audit pass (48 documents, 168 links,
  seven atomic tables, 25 rows). Both canvas captures were visually inspected.
  The desired native oracles still fail; existing unit green does not waive them.

## User-requested commit checkpoint - 2026-11-22

The user requested separate reviewable commits before evaluating whether to
merge develop. Preserve the completed validation evidence below; committing
partial work does not close WB-B or authorize its four runtime corrections.

| Commit | Responsibility |
| --- | --- |
| `511e10d26c` | coding execution contracts, draft compilation, projections and tests |
| `0cbc7889cc` | owned backend DTOs/validation, response limits, revision and parity tests |
| `0ef558955b` | web wire/publication bridge, tests and officially generated coding client schemas |
| `921e844947` | targeted worker configuration/budget forwarding and import-boundary tests |
| `852ddedd79` | historical toolchain tests bound to their retained locks/manifests |

The sixth commit records this checkpoint, ADRs, duplication/consumer maps,
validation results and the unresolved execution risks. No existing commit is
rewritten, no push or merge is included, and no IDE/public runtime or migration
change is introduced. Compare freshly fetched develop after this checkpoint;
do not infer upstream fixes from branch names or an old tracking reference.

## Execution record: UX02-C / WB-B configuration publication - 2026-11-21

- Entry: `6d90892224`, clean `feature/coding-grading`, one local documentation
  commit ahead of its tracking branch. The strict Release host build passed
  at the preceding source revision with all 36 analyzer/generator inputs;
  architecture/runtime/client gates were not thereby passed.
- Active stage: specification, then contracts/schema, independent red tests,
  minimal implementation and adversarial audit. Acceptance: CS-REF-06/07/09/22
  and WB-B time/output/entry/environment/toolchain fidelity, not UI cutover.
- Contract: optional additive `Execution` in existing coding JSON freezes
  limits, entry point and toolchain; `Environment.Runtime` separates runtime
  from language. Presence is all-or-nothing and validates the exact supported
  environment/tools/identity. Legacy content without these fields retains its
  historical interpretation; no automatic rewrite, version/alias or new table.
  New explicit drafts may not silently fall back to defaults/current identity.
- Memory: export an explicitly unavailable capability. Preserve any historical
  configured canonical value and incomplete draft buffer; reject it on new
  publication/execution rather than silently dropping it. A new draft uses an
  empty RAM buffer and publishes no configured RAM value. Supported positive
  time/output maxima remain 30,000 ms / 2,000,000 bytes; max files remains 100.
- Allowed files: coding contracts/schemas/projection/draft compiler and focused
  tests/fixtures; owned web coding types/bridge and focused tests; Courses
  coding DTO/validator and CodeAdapter projection/response checks and tests;
  the authorized grading worker entry/budget tests/build selection; official
  generated client files only through its generator; these plan/state/ADR files.
  Expand only for a demonstrated dependency. Preserve platform, migrations,
  Emception IDE/UI/public runtime, CI policy, dev scripts and existing data.
- Gates: strict coding/adapter/surface types; contract/publication and wire
  roundtrip tests; Courses/CodeAdapter persistence/revision tests; strict worker
  tsc/lint and bounded Node suites; G-WEB and affected G-API/architecture; official
  current OpenAPI/client generation; documentation/import/privacy audit.
  Existing Authentication failure stays owned externally; reconcile historical
  worker assertions without removing historical fixtures or weakening policy.
- Resources: sequential heavy commands, one test worker, four CPUs, >=4 GiB
  system headroom. Ordinary checks use <=10 GB owned RSS; a required current
  host build uses the demonstrated monitored <=14 GB profile, never an unbounded
  command or IDE build. Record actual outcomes; no full-stack/browser start
  is needed for contract-only evidence.
- Contract gates passed: coding strict types + 13 execution-schema cases;
  actual Courses Release build, warnings as errors, exit 0, no warnings/errors.
  Web strict types passed with an 8 GiB managed Node heap, peak 6600 MiB RSS.
- Behavioral red: domain publication 12 failures; web publication 5 failures;
  backend Courses 97/120 and CodeAdapter 68/86 failures for the intended missing
  validation/file-limit behavior. Worker 14/20 fail for ignored frozen values.
  A first fake-clock harness run was interrupted after its cleanup hung; its
  corrected complete red run has no cancelled/skipped cases and is the evidence.
- In-scope implementation evidence so far: coding 96/96; web coding-assignment
  103/103; worker composition 20/20 and strict tsc; domain/adapter strict lint.
  Extra independent web red cases exposed weight clamping and scalar coercion;
  fixed only for explicit configuration, preserving legacy interpretation.
- Historical worker gate: record CS-ADR-031 before correcting its two stale
  source-text assertions. Its subsequent complete selection passes 78/78.
  Expand allowed files only to that existing test, the consumer/duplication map
  and capability-gap/UX03 entry notes to record the necessary boundary trace.
  No upstream implementation or runtime capability is authorized/inferred.
- Independent read-only Auditor identified real-runtime risks: source ordering
  alone does not prove native entry ownership; the C/C++ function harness has
  language/linkage/startup prerequisites; the native WASI run path is not the
  graphics canvas path; a nonzero compile result can pass an empty-output stdio
  case if no exit expectation exists. Preserve these as blocking UX03 oracles,
  not successful execution evidence or permission to edit upstream code.
- Structural stop: WB-B execution-fidelity acceptance and CODE-UX-02 stay open.
  The user was asked whether to expand only the grading worker's public-API
  composition or defer execution acceptance to UX03. Pending that answer, do
  not implement a parallel harness, modify upstream or silently reduce support.
  Independent contract/build/client checks may continue; no UI cutover occurs.
- Backend green: complete Courses 1145/1145 and CodeAdapter 179/179; actual
  Release strict builds and tests, no skipped cases. Includes four revision
  persistence tests, explicit limits/identity/path/merged-file bounds, scalar
  parity, legacy absence and unknown-field negatives. The additional validation
  red was 101/122 failed before fixing its causes, not a bootstrap failure.
- Full solution attempt: strict `dotnet build apps/api/GameGuild.sln -c Release
  --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
  -p:TreatWarningsAsErrors=true` was stopped at the 600000-ms command budget;
  sampled peak 9694 MiB RSS, minimum available 8076 MiB, no OOM. The child CLI
  returned 0 during termination, but the guard correctly returned 1. This is
  NOT a successful build or permission to use an unfinished host capture.
  No broader gate is waived. Finish the final owned parity tests, then attempt
  the actual current host with warmed references and the same bounded profile.
- Final Auditor parity red: 40/82 cases fail for lossy explicit weights,
  invalid/duplicate/overlong parameter identifiers and null stdout, with
  positive/legacy controls retained. Fix those causes only in new explicit
  configuration, then rerun both complete affected backend suites.
- Final affected backend green: Courses 1227/1227 and CodeAdapter 179/179,
  zero skips, strict Release builds. Parent-owned sequential execution; sampled
  peaks 856/588 MiB, elapsed 54377/16829 ms. This supersedes the earlier counts
  only for the current implementation; no official native runtime was invoked.
- Current host PASS: actual GameGuild.API Release + project references built
  with all analyzers/generators and warnings as errors, zero warnings/errors.
  No stale host or frozen generator substitute: source edits were finished
  before the command. Elapsed 00:09:57.97 / guard 598285 ms; peak 13046 MiB RSS,
  minimum available 5461 MiB, no stop/OOM. Four CPUs and 13.8 GB sampled ceiling.
  This is not a passed full solution, architecture test, startup or native run.
  Use the existing offline metadata exporter and official client generator
  only after current v1 endpoint-coverage/config-schema checks; no worker boot.
- OpenAPI/client source: existing offline exporter built strict Release, exit 0;
  captured 1802 paths / 2371 schemas from the current host metadata. Host DLL,
  Courses DLL and host XML hashes match the freshly built originals exactly.
  The v1 selector excludes unversioned shell minimal endpoints; root is excluded
  from description. All 2180 committed method/path operations match the capture,
  no additions/removals, all four new execution schemas present. This is default
  v1 metadata evidence, not live-host startup or deployment-env override proof.
  Official generator exited 0 (52.8 s, 2186 MiB peak), changing only owned coding
  types/schemas and generator metadata, including previously deferred metadata.
  No manual generated edit, endpoint/module churn, API, database or worker boot.
- API architecture initial invocation stopped at compiler CS0006 for the absent
  Release test-support Finance assembly. No test executed; this is a build
  prerequisite, not behavioral red or an authorization failure. Compile only
  that existing support library, then rerun the unchanged test selection.
- API architecture/security/OpenAPI PASS after the support build: 382/382,
  no skips; actual current Release API/test binaries, strict warnings, 146840 ms,
  peak 3357 MiB, minimum available 16656 MiB. No support/platform code edit.
  This closes the selected architecture prerequisite, not the full solution
  timeout, external Authentication failure or native-execution acceptance.
- Client closing checks pass: official-config tsc, complete ESM/CJS/DTS build,
  public-API type contract and 1772 unit tests (112 files, one worker). Resource
  peaks 4985/3328/735/1261 MiB under the 9.5 GB guard; no Node heap override is
  committed. The existing DTS private-export warnings for NetworkError/Transport
  remain visible; no client transport/API implementation was changed.
- Final boundary audit: the first worker mapping imported coding's runtime
  parser. That would bundle a feature implementation into the upstream artifact
  closure while the unchanged source classifier excludes feature-only changes.
  Corrected this introduced coupling rather than widening CI or upstream scope:
  local typed wire guard + existing worker budget/frozen-binding contracts,
  independently tested for parity. No feature implementation import or new
  dependency is left in the grading artifact.
  The new AST import oracle failed before correction, with 45 positive/negative
  composition cases already green; all 46 now pass without the feature loader.
  Final worker selection: 104/104, zero skips, strict tsc exit 0. No native
  execution was started; the runtime-fidelity stop is unchanged.
- Earlier worker evidence was 87/87. Four unknown-field negatives and five
  case-varied legacy labels were independently red. The latter preserve the
  actual old exact-key lookup -> C++ fallback behavior; new explicit labels
  remain strict and cannot use that fallback. No real runtime pass is claimed.
- Root security: Authorization 2815/2815. Authentication repeats only its known
  `GenerateEmailCodeAsync_ThrottlesResendsWithinSixtySeconds` failure:
  4572/4573 passed. Keep the external failure visible and its module unchanged.
- Final web regression selection: 181/181 across eight files (108 coding wire/
  publication/session tests plus 73 authoring/grader consumer tests), zero skips,
  12.8 s, sampled peak 834 MiB. No application/runtime boot or fixture reset.
- Closing lint found two unsafe spreads after Array.isArray narrowed raw input
  to any[]. Replace only those spreads with direct unknown-value validation of
  each array; no assertion, suppression, type relaxation or behavior change.
  The unchanged type-aware web lint selection then passed, zero warnings.
  Root strict lint passed for coding, adapter, surface and changed worker files.
  Current-source web tsc also passed after client generation (6133 MiB peak).
  After the narrowing correction, the final repeat passes 181/181, zero skips
  (17489 ms, 814 MiB peak), and web tsc again exits 0 (11692 ms, 1679 MiB peak).
- Documentation/import audit: 47 documents, 166 valid local links, seven atomic
  tables and 25 complete atomic rows, exit 0. Protected platform, upstream
  IDE/UI/public runtime, developer launchers, dependency/CI configuration and
  historical migrations remain untouched. All manual edits use owned paths;
  generated types/metadata came only from the official generator.
- Next: retain the completed contract/build/client/lint/type/document evidence
  and obtain the runtime-scope disposition. No
  production save coordinator or user interface changes now. Native execution
  acceptance remains unproven and is not inferred from composition mocks.

## Execution record: user-approved 14 GB full-build measurement - 2026-11-21

- User explicitly approved the proposed single 14 GB measurement. This
  supersedes the pending request below for this measurement only; configurable
  exercise RAM remains deferred. No product/build configuration change.
- Contract: build the actual API host and its project references in Release,
  keeping every source, migration, analyzer and source generator, with warnings
  as errors. No frozen generator output or diagnostic suppression. Use the
  existing restored dependencies, one MSBuild worker, no shared compiler/node
  reuse, four CPUs (affinity 0-3), SDK default server GC, 12 GiB managed heap
  and process-local memory conservation 5. No API/web/database boot.
- Budget: <=14,000,000,000 bytes total owned RSS. The temporary supervisor
  stops at 13,800,000,000 sampled bytes to allow monitoring/termination margin,
  >=4 GiB system available RAM, and a 600-second deadline. No concurrent build;
  stop only the owned process tree. Preflight available RAM: 19,732,804 KiB;
  no .NET/compiler process is active. Reserve eight of twelve logical CPUs.
- Gate: record the real strict build exit, warning/error counts, peak RSS and
  available-memory minimum. Resource stops never count as success even if the
  CLI exits 0 after termination. A Release host success is not a full-solution,
  default Debug, architecture-test, runtime or dev:fast validation.
- Allowed tracked edit: this ledger; preserve the other four plan edits.
  The guard change is temporary under `/tmp`. No migration, source, package,
  SDK, launcher, environment file or analyzer configuration change is allowed
  by this diagnostic slice. No automatic higher-budget retry follows failure.
- PASS: actual Release host/project-reference build exited 0 with 0 warnings
  and 0 errors. CLI elapsed 00:04:46.78; supervisor elapsed 287011 ms, sampled
  peak 12873 MiB (about 13.5 GB), minimum system available 7418 MiB; no guard
  stop, managed OOM or kernel OOM. The same complete build can finish inside
  the approved budget; this is not proof of a universal minimum RAM need.
- Independently inspected the running native Csc response: 36 analyzer inputs,
  GGARCH, Sonar, migration snapshot, optimization and warnings-as-errors are
  present; skip-analyzers is absent. No diagnostic-only response/frozen output
  was used. Native Csc's process name is `csc`, so cleanup checks include it,
  not just `dotnet` and `VBCSCompiler`.
- Fresh Release assembly/PDB inspection: revision
  `ec1ff97b50262e633931511229c574e3e1289536`, compiler/runtime unchanged,
  710 PDB documents, all 706 physically present source hashes match, no
  differing source. Four generator outputs are represented in the PDB but
  normally not persisted on disk. The API executable was not started.
- Disposition: the strict Release host build obstruction is cleared for this
  measured profile without a product-code change. Default Debug, the complete
  solution, API architecture/security tests, runtime startup, dev:fast and
  OpenAPI/client generation were NOT run in this slice. Their remaining gates
  are not silently passed; no coding-surface phase is declared complete.

## Execution record: bounded compiler root-cause investigation - 2026-11-21

- Request: determine how to complete the API build without the repeated OOM,
  comparing the earlier successful build rather than repeating heap tuning.
- Contract-first diagnosis: inspect the retained 2026-11-19 assembly/PDB without
  executing it; compare compiler identity, source checksums and repository
  history. Standard-library-only inspection helpers and compiler transcripts
  stay under `/tmp`; no tool/package installation or source checkout/reset.
- A separate compiler/analyzer isolation experiment may suppress diagnostic
  execution ONLY in temporary non-release outputs to identify its cost. This
  is not a passing product build or permission to disable GGARCH/Sonar rules
  in the project, CI or dev commands. Preserve the complete compiler input
  set, generators and migration history. Restore/run the unmodified strict
  build and applicable architecture tests before claiming a correction.
- Allowed tracked edit now: this ledger. A production change requires an
  identified cause, focused regression tests and an explicit ownership/scope
  decision if it touches protected or another developer's architecture.
  Preserve all five existing plan edits. No database, API boot, frontend,
  Emception runtime/IDE, generated client, dependency or SDK change.
- Budget: sequential owned commands, <=10 GB total monitored RSS, >=4 GiB
  system headroom, reserve at least two CPUs, no inherited GC workaround
  unless an individual experiment declares it. Stop only owned processes.
- Gates: positive diagnostic evidence and measured exit/resource results;
  then full-input, analyzer-enabled warning-clean API build plus applicable
  architecture suites. A temporary isolation pass never closes G-API.
- Retained binary evidence: InformationalVersion is
  `4.4.0+2e35c3fc49e756fbc1b362452e97ba1f55f026a5`; compiler and runtime are the
  exact currently installed 5.9.0-1.26529.119 / 10.0.15 identities. Of 710 PDB
  documents, 705 present source hashes match, one generated AssemblyInfo differs
  (revision), and four generator outputs are not persisted. Source bytes are
  202,196,903. No historical assembly is used as current contract/test evidence.
- Capture the SDK's actual CscCommandLineArgs with SkipCompilerExecution=true
  and BuildProjectReferences=false against already-built references. This may
  update SDK intermediate metadata, but executes no compiler and does not
  replace the retained API binary. Derive isolated response files mechanically
  from those structured arguments; redirect ALL compiler outputs to `/tmp`.
  The first diagnostic uses `/skipanalyzers+`, preserving compiler/source
  generators, source inputs and compiler warnings. It is explicitly not a
  qualifying build. Compare to analyzer-enabled cases before choosing a fix.
  Initial compiler-only budget: four CPUs, default server GC, 6 GiB managed
  heap and memory conservation 5, 9.5 GB external sampled RSS stop, >=4 GiB
  system headroom, 600 seconds. No other build runs concurrently.
- The SDK argument capture first failed inside the sandbox without usable
  diagnostics, then succeeded with process visibility, with no compilation.
  All 702 tracked host sources and four SDK-generated sources are present;
  no additional ignored source or missing tracked source was found.
- Compiler-only result: SIGABRT/exit 1 after 170350 ms, sampled peak 6495 MiB,
  minimum system available 11723 MiB, no external guard stop. The managed OOM
  occurred in MethodCompiler's semantic-model creation callback. Diagnostic
  suppression at this 6 GiB heap does not make compilation fit; it does not
  prove analyzer cost is zero or identify one as the culprit. No product output
  was replaced. One harness-only CS2023 warning came from an SDK `/noconfig`
  argument repeated inside the response; derived future responses remove that
  duplicate and keep the real CLI `/noconfig` outside the response.
- Next discriminating case: full analyzer-enabled Csc invocation using the
  shared compiler (the ordinary launcher default), a unique diagnostic pipe
  and one-second server keepalive. Use four CPUs, default server GC, 8 GiB
  managed heap, conservation 5, <=9.5 GB monitored total RSS, >=4 GiB system
  headroom and 600 seconds. Derived outputs and generated sources stay in
  `/tmp`. Verify the owned server's group/lifecycle and stop only that group
  if necessary; do not shut down any existing compiler/IDE server.
- Shared-mode observation: the unique VBCSCompiler was owned by the monitored
  process group and started the full analyzer-enabled compilation. Its four
  generated files total 153,257 bytes; no large generator-output expansion was
  found. The server then exited without a completed result and Csc fell back
  to another local compile. Stop that owned fallback rather than automatically
  repeating it: SIGTERM at 305576 ms, peak 8616 MiB, system available >=9307
  MiB, supervisor exit 1. The server's exact exit cause was not captured, so do
  not label it a proved analyzer/SDK regression or a successful shared build.
  No .NET/compiler process remained after cleanup.
- CI configuration evidence: `.github/workflows/pr-verify.yml` builds the host
  with `-c Release -p:TreatWarningsAsErrors=true`; previous attempts used Debug.
  Next qualifying comparison uses that complete Release host build, all
  configured analyzers/generators and all sources, no shared compiler, one
  MSBuild worker, four CPUs, server-GC default, 8 GiB managed heap, conservation
  5, 10 GB total monitored RSS, >=4 GiB system headroom and 600 seconds. No API
  boot. Release success would establish the CI build, not silently close a
  distinct Debug/default-solution gate or claim hot-reload behavior.
- The SDK parser independently confirms the diagnostic flags: 706 input
  source files, SkipAnalyzers=true in the compiler-only case; full serial case
  has SkipAnalyzers=false and ConcurrentBuild=false. Both retain nullable
  analysis and the same parser/interceptor features. An offline throwaway
  console confirms `Configuration=Release dotnet run` resolves Release without
  editing a launcher; this is not an API/runtime validation.
- Release result: dependencies compiled, but host compilation again threw
  managed OOM in MemberSemanticModel.NodeMapBuilder / MethodCompiler. The
  600-second monitor subsequently stopped cleanup, supervisor exit 1, peak
  9107 MiB, system available >=9449 MiB. No selected test ran. Release alone
  does not resolve this measured failure; do not recommend it as a proved fix.
- Next isolation changes a pipeline boundary, not the heap: use all 706 Debug
  inputs plus the four generator outputs captured from the same current
  invocation, and remove analyzer/generator assemblies from this TEMPORARY
  compiler process. This is not a product build or gate waiver. Preserve
  all source bodies, migration files, generated implementations, references,
  compiler warnings/nullable checks and language features. Outputs remain
  under `/tmp/frozen-outputs`. Same budget as compiler-only: four CPUs,
  6 GiB managed heap, conservation 5, 9.5 GB RSS ceiling and 600 seconds.
  Its comparison distinguishes plain compilation from the attached generation
  and diagnostic pipeline; a pass does not identify an individual plugin.
- Frozen-output diagnostic result: exit 0, no compiler warning/error, 162467
  ms, peak 6425 MiB, system available >=12695 MiB. This confirms that complete
  source compilation and generated implementations fit the bounded process
  in this diagnostic; it is NOT an analyzer-enabled product build or G-API.
- Next differential diagnostic retains all pure diagnostic assemblies
  (including GGARCH and Sonar) and the same four frozen outputs, removing only
  assemblies containing generators. Inspect plugin metadata without executing
  plugins; mixed generator/analyzer assemblies are excluded together and this
  limitation prevents treating the experiment as a complete gate. Keep four
  CPUs, 8 GiB managed heap, conservation 5, <=9.5 GB total sampled RSS, >=4 GiB
  system headroom, 600 seconds and temporary outputs only. No analyzer is
  disabled in repository files or normal build/dev commands.
- Frozen-output/static-analysis result: SIGABRT/exit 1 after 285438 ms, peak
  8564 MiB, system available >=10269 MiB, no external resource stop. The 8 GiB
  managed heap exhausted in MemberSemanticModel.NodeMapBuilder / MethodCompiler,
  even with generator assemblies absent. Therefore generation alone is not
  sufficient to explain the failure; neither Sonar nor GGARCH is individually
  identified as its cause. The plain-source pass cannot waive these checks.
- Disposition: the compiler's analysis/semantic-model pipeline is a measured
  contributor, not a proved new source/SDK regression. Historical source volume
  explains a large workload, but the retained PDB contains no past peak-memory
  measurement and does not establish that the old build fit today's cap.
  No valid complete host build has yet succeeded within these measured profiles.
  Do not disable checks, delete/exclude migrations, replace current outputs with
  the temporary DLL, or claim dev:fast/architecture-test success from isolation.
- Asked for an optional single <=14 GB monitored full-build measurement, with
  >=4 GiB system headroom and at least two CPUs reserved. The subsequent user
  approval and bounded execution contract are recorded above. A positive full
  build is still required before API architecture tests, current OpenAPI/client
  capture, or closure of G-API.

## Execution record: API build versus dev:fast diagnosis - 2026-11-21

- User request: investigate why the API compilation failed, including whether
  our validation invocation differs from the working `dev:fast` workflow.
- Read-only comparison: `scripts/dev-fast.mjs` launches `dotnet watch run`
  against the same API project, with the root `.env` and polling enabled.
  Turbopack is the web bundler, not the API compiler. No build-time analyzer
  bypass or GC override is present in that launcher or the local `.env`.
  Both invocations resolve the only installed SDK, 10.0.404. The SDK compiler
  runtime configuration enables server GC; our prior commands instead forced
  workstation GC and 2/6/8 GiB managed heaps. Those managed OOMs are not proof
  that the normal development build is broken or needs more than 10 GB.
- Diagnostic contract: one host-only build using the SDK's default GC policy,
  no inherited GC hard-limit overrides or temporary compiler response file.
  Retain all sources/analyzers, warnings as errors, one MSBuild worker, no
  shared compiler/node reuse, CPU affinity 0-9, sampled total owned RSS <=
  10,000,000,000 bytes, >=4 GiB system available RAM and a 600-second deadline.
  Do not launch the application, databases, frontend or full validation stack.
  Record whether this reaches compilation success, a compiler diagnostic or
  an external resource stop before choosing the next action.
- Allowed edits: this execution ledger and temporary monitoring artifacts.
  No source/project/dev-command/dependency/migration change is authorized by
  this diagnostic slice. Existing five plan edits are preserved.
- Quality gate: actual host build exit and resource measurements, followed by
  the API architecture selection only if a current host assembly is produced.
  A resource stop or development-only success cannot waive strict test gates.
- First diagnostic result: the default-GC host build was stopped by the owned
  RSS monitor after 111406 ms, sampled peak 9710 MiB, minimum system available
  9056 MiB. The CLI returned 0 after SIGTERM, but the supervisor correctly
  returned 1; no current host or test success is claimed. No compiler diagnostic
  or kernel OOM occurred. Compiler CPU reached approximately eight cores before
  this stop. All migration sources and 36 analyzer/source-generator inputs
  were present, with `/warnaserror+` and no skip-analyzers switch.
- Historical comparison: API project, migration tree, architecture analyzer,
  Directory.Build.props, SDK selection and dev-fast launcher are unchanged
  between `72d8865ca7` (2026-11-18) and entry HEAD. This does not identify a
  new coding-source compiler regression; changed module dependencies can still
  invalidate an incremental host assembly. The last on-disk host is dated
  2026-11-19 and cannot stand in for the current DTOs.
- One discriminating scheduling check follows: retain default server GC and
  no managed-heap override, but expose two processors and use the installed
  compiler's `-parallel-` response switch. This tests compiler/GC concurrency
  inside the same 10 GB authorization, not another heap increase or analyzer
  bypass. CPU affinity 0-1 leaves ten machine CPUs available. Keep the same
  strict source/analyzer set, total-RSS/system-headroom/deadline guard, and no
  application launch. Record a resource stop as incomplete, never success.
- Scheduling comparison result: supervisor exit 1 at 235285 ms, sampled peak
  9565 MiB, minimum system available 9126 MiB. The external RSS guard stopped
  the owned build; no compiler error or test execution occurred. Concurrency
  reduction alone was insufficient in this measured default-GC invocation.
  No owned compiler remained and no project input was modified.
- Final budget-aware check: preserve the SDK's server GC rather than forcing
  workstation GC, expose two processors, bound the managed heap at 8 GiB and
  set process-local `DOTNET_GCConserveMemory=7`. This is a documented runtime
  memory/throughput tradeoff, including automatic large-object-heap compaction
  when fragmented, not a source/analyzer bypass or higher resource allowance.
  Keep the existing 10,000,000,000-byte total-RSS monitor and all other gates.
  No environment file, launcher or machine-level GC configuration changes.
  Source: https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector#conserve-memory.
  Stop after this check if it cannot finish within the accepted resources;
  further profiling or a project-boundary change needs a separate disposition.
- Final result: actual build exit 1, 0 warnings, 1 `Out of memory` error from
  Roslyn's Csc task for GameGuild.API.csproj; elapsed 387734 ms, sampled combined
  peak 8956 MiB, minimum system available 8952 MiB. The total-RSS monitor did
  not stop this attempt. The compiler reached its configured managed-memory
  limit despite server GC and memory conservation. It subsequently exited
  normally with build failure; a cleanup check found no remaining .NET/compiler
  process. No tests or current OpenAPI capture ran.
- Audit/disposition: all three commands are bounded strict validation builds,
  NOT a bit-for-bit execution of `dev:fast`: warnings as errors, disabled shared
  compilation, worker/CPU restrictions and a total-RSS monitor differ from the
  ordinary watcher. Earlier workstation/heap overrides also differed. The
  ordinary launcher has neither these GC caps nor the monitor's 10 GB ceiling.
  Therefore these results cannot prove a new `dev:fast`/Turbopack regression,
  nor that every possible safe build configuration needs more than 10 GB.
  They establish why these verification attempts failed: managed heap OOM or
  deliberate external resource stops, not a C# source diagnostic or a grading
  test assertion. The historical migration volume is cost evidence, not an
  allocation profile identifying a particular analyzer as the root cause.
- No automatic retry, higher budget, analyzer exclusion, source rewrite or
  migration removal follows. A separate bounded compiler/analyzer profiling
  task or a user-approved resource disposition is needed to finish the common
  host gate. WB-B remains independently implementable; the API test/client
  gates remain open, not passed and not reclassified as a product defect.
- Tracked edits in this diagnosis are plan/state clarification only. Existing
  implementation, `.env`, both dev launchers, project files, SDK selection and
  all historical migrations are unchanged. Documentation audit/whitespace
  checks are repeated after this record; no functional validation is inferred.

## Execution record: user-approved API validation budget - 2026-11-21

- Entry: `ec1ff97b50262e633931511229c574e3e1289536`, tree
  `e9729dbb0ca3882deee3845540b54bacd6168b62`, clean
  `feature/coding-grading`. The metadata and targeted worker changes are now
  committed. No fetch, merge, automatic commit or source rewrite is requested.
- Explicit resource authorization: the user permits up to 10 GB RAM for the
  local builds/tests that were previously stopped. This supersedes their
  former 6 GiB RSS / 2 GiB managed-heap disposition only for this validation
  attempt. It is NOT approval for per-exercise memory settings or upstream
  Emception changes. The user subsequently accepted deferring configurable
  exercise RAM; CS-ADR-029 records that separate decision.
- Entry resources: 18,267 MiB MemAvailable on a 31,806 MiB machine, 12 CPUs.
  Reserve CPUs 10-11, use affinity 0-9, one MSBuild worker, no node reuse/shared
  compiler and sequential commands. The temporary monitor limits the combined
  owned-process sampled RSS to 10,000,000,000 bytes (decimal, not 10 GiB),
  preserves >=4 GiB system MemAvailable and retains the 600-second command
  deadline. A 6 GiB per-process managed heap leaves headroom inside the total
  RSS ceiling. No unrelated process/service may be stopped to make space.
- Scope: verify existing committed implementation, not introduce behavior.
  Allowed tracked edits are this ledger, the ADR log, capability-gap log,
  active 3.6.2 subplan and its 3.6 coordinator, to record the user's scope
  decision. Temporary resource-helper edits stay under `/tmp`.
  No implementation/schema/dependency/generated-client change,
  no new tool, migration, data reset, runtime boot or IDE build/publication.
- Arbiter: build/run API authorization/dependency architecture selection,
  then the complete API unit project if the selection passes; run the full
  solution warning-clean build independently when safely budgeted. Record
  real exit codes/counts and any baseline failures, never a killed CLI's 0.
  Keep existing Authentication and historical-worker failures visible.
- Structural disposition: the user explicitly approved deferring configurable
  exercise RAM and delivering enforceable time/output configuration without
  changing the upstream runtime. No additional user decision is pending for
  that scope. WB-B remains implementation/gate work, not a decision blocker.
- First retry result: exit 1 before any selected test ran, after 505129 ms.
  Roslyn failed in `MemberSemanticModel.NodeMapBuilder` with managed OOM at
  the 6 GiB heap cap. Combined sampled owned RSS peaked at 6855 MiB and system
  available memory stayed >=11204 MiB; the 10 GB RSS guard never tripped.
  Do not describe this as proof that 10 GB is insufficient. No kernel OOM or
  owned compiler survived. The complete API suite/full solution did not run.
- One adjusted retry is still inside the same user authorization: allow an
  8 GiB managed compiler heap while retaining the exact 10,000,000,000-byte
  combined RSS ceiling, system headroom, CPU reservation and 600-second
  command deadline. Disable only compiler parallel scheduling via the SDK's
  documented `-parallel-` option in a temporary response file, supplied through
  its existing CompilerResponseFile property. No analyzer/rule, migration,
  input source or required test is excluded. Installed SDK compiler help and
  Microsoft.CSharp.Core.targets confirmed that option/property pair.
  Stop after a second managed/resource failure rather than repeatedly raising
  heap limits or disguising an incomplete build as test success.
- Adjusted retry result: exit 1 before any selected test ran, after 292693 ms.
  Roslyn again threw managed `OutOfMemoryException`, now while building
  operation/semantic models in its analyzer driver. The temporary `-parallel-`
  response file and 8 GiB managed heap did not produce a current host assembly.
  Sampled combined owned RSS peaked at 8916 MiB (9.349 GB decimal); system
  available memory stayed >=9759 MiB. The 10 GB RSS guard did not trip; neither
  attempt was a kernel OOM or a test assertion failure. No owned compiler or
  testhost remained afterwards. No further memory retry was started.
- Read-only size inventory: the API host has 702 C# source files totaling
  202,185,187 bytes, including 456 migration-related files totaling 200,570,794
  bytes. This is relevant build-cost evidence, not a proved allocation profile
  or permission to remove/rewrite/exclude migrations or turn off analyzers.
  No source, project, dependency or migration file was changed by validation.
- Outcome: API architecture selection, full API unit suite, full solution
  build and official OpenAPI/client generation remain open because the common
  host compilation did not finish. No stale assembly, zero executed cases or
  killed process is reported as a pass. The next technical action is diagnosis
  of this host compilation within the accepted budget, not another automatic
  heap increase. Independent WB-B implementation remains possible under the
  accepted memory-capability deferral; it cannot close the broader API gate.
- Documentation gate: structured Markdown audit passed for 47 documents,
  165 local links and all 25 atomic rows across seven tables; whitespace clean.
  Only the five explicitly allowed plan/state documents changed. Resource
  scheduling helpers/response file are temporary `/tmp` files, not dev-command
  changes. No additional product/scope decision is waiting on the user.

## Execution record: UX02-C publication preparation - 2026-11-21

- Entry: `68a9d5e702950d4b0b77f6705e2d59ca76c1123e`, tree
  `1d0a75bb6f143fb62cc76e65d33441a41c66e194`, clean
  `feature/coding-grading`. Active stage: specification, then contracts/tests.
  The broader Authentication/API resource gate remains open; no waiver.
- Actual local inventory: started only this project's existing PostgreSQL
  volume `game-guild-postgres-data`, used `default_transaction_read_only=on`,
  then stopped the container. Counts: 721 contents, two coding definitions,
  neither with Problem; 712 drafts, none coding; 1353 assessment revisions
  and 555 submissions, none coding. No IDs, user content or credentials were
  collected; no writes, migration, reset or old-volume reinterpretation.
  These local counts do not assert anything about remote databases.
- Preservation decision: additive optional problem/reference and stable-ID
  fields, omitted when absent. Retained definitions/snapshots keep their old
  interpretation and serialization; no automatic backfill or fabricated
  problem/solution. New draft compilation must validate complete values.
  Existing JSON/revision storage fits; no table, migration or V2 adapter.
- Specification: the coding definition owns the problem. Published projections
  freeze it, technical cases and reference solutions; learner projections
  explicitly exclude references/private cases/files. Reference solutions are
  replacements for public editable text files, never compilation starter files.
  Explicit file/test/case IDs survive content edits; historical absent IDs
  retain the existing bridge's deterministic mapping. A later source edit
  cannot change an earlier frozen delivery or submitted-file binding.
- Allowed files: coding contracts/schemas/draft compiler/projections and tests;
  code adapter fixtures/tests; Courses coding JSON DTOs/validator;
  CodeAdapter publication/projection paths and focused integration tests;
  web coding wire types/bridge and tests; generated client only through the
  official OpenAPI generator; shared state/ADRs/consumer map/subplan.
  No active UI writer, platform, generic grading policy, Emception/UI, dev
  command, dependency, release workflow or database schema change.
- Required boundary expansion: Courses `CodingAssignmentContentService`
  has its own enrolled-learner projection in addition to the official adapter.
  Include that method and its existing endpoint/service tests in the allowlist;
  both projections must strip the new protected solutions before DTOs ship.
  Share the adapter's unchanged canonical path algorithm from an owned Courses
  helper so metadata validation can use it without a reverse dependency.
- Acceptance IDs: `UX02-C-PROBLEM`, `UX02-C-PRIVATE`, `UX02-C-REVISION`,
  `UX02-C-STABLE-ID`, `UX02-C-INCOMPLETE`, `UX02-C-HISTORY`.
  Gates: strict contract types/schema fixtures before consumers; behavioral
  red before minimal implementation; G-CONTRACT/G-SURFACE/G-WEB and focused
  affected G-API, official client generation if DTOs change, G-REGRESSION,
  same-agent negative/data/import audit. Runtime/compiler and human acceptance
  remain separate. No repeat of the twice-stopped full API compile without
  a recorded resource disposition.
- Resource entry: MemAvailable 13,604 MiB. Sequential one-worker checks,
  CPU affinity 0-9, 4096 MiB Node heap, <=6144 MiB owned RSS and >=4096 MiB
  system available memory. No compiler/Next/full stack is needed for domain
  preparation. Record actual commands, red evidence and remaining gates below.
- New bounded API resource disposition: after the owned module builds passed
  below 1.1 GiB RSS, allow one API architecture/build attempt needed to capture
  the official changed DTO contract. Use one MSBuild worker, no shared compiler,
  workstation GC and a 2 GiB managed heap hard limit per process; retain the
  unchanged 6 GiB owned-RSS / 4 GiB available / four-minute stop guard. This
  reduces the earlier unbounded compiler allocation, not the quality gate.
  If it fails, record the generator/API gate as open; never hand-edit generated
  output or report an old host assembly as current contract evidence.
- The API attempt stopped at 240 seconds before producing a current host
  assembly: owned RSS peaked at 2682 MiB, available memory stayed >=11187 MiB,
  no owned process survived. The supervisor's exit is 1, not the terminated
  CLI's 0. Old 2026-11-19 assemblies were explicitly rejected as evidence.
  One host-only build may now use 600 seconds with the same 2 GiB managed
  limit, unchanged RSS/available thresholds and CPU reservation. This changes
  measured compilation time only, not heap, assertions or analyzer strictness;
  default lightweight-command budget stays 240 seconds. No memory-pressure
  failure or kernel OOM occurred, and no tool/dependency is installed.
- That host-only build failed after 323265 ms with Roslyn's managed
  `OutOfMemoryException` inside the explicit 2 GiB compiler heap limit.
  Owned RSS peaked at 2683 MiB; system available memory stayed >=10904 MiB.
  No kernel OOM or surviving compiler process. Do not automatically raise the
  heap or retry. Current OpenAPI capture/client generation and API architecture
  evidence remain open; old host binaries are not current evidence.
- Explicit user exception: a targeted grading-worker change is authorized,
  not a change to the IDE, `@game-guild/emception-ui` or the upstream runtime.
  Additional allowed files are `tools/emception/grading/browser.ts`, a typed
  execution-budget helper and its focused Node tests, and the worker build's
  explicit test selection. The existing public `dispose()` can terminate the
  owned worker. Public browser `run()` currently ignores `timeoutMs` and
  exposes no per-program memory limit; never claim these are enforced merely
  because a draft or `TestPlan` contains numbers.
- Worker slice WB-A: validate a positive safe-integer time/output contract;
  meaningful red tests for an unresolved operation, streamed UTF-8 output,
  exact boundary, settled-output fallback, cleanup and sanitized failure;
  implement cancellation only through the public API. Existing 30-second /
  2,000,000-byte defaults remain until WB-B's published configuration mapping.
  Time covers each compile-and-run invocation; output includes its compiler
  and program streams. Exceeding a resource budget is execution unavailable,
  never a fabricated zero grade. Gates: strict worker tsc, strict changed-file
  lint and the worker's bounded Node suites. Real compiler execution is later
  UX03 evidence, not inferred from a fake API.
- WB-B was structurally paused pending the memory-capability decision, now
  resolved by the explicit deferral in CS-ADR-029. Do not erase historical
  fields, silently ignore a configured value, invent a memory
  monitor or modify the protected runtime. Official source-release provenance
  classification must be recorded after a grading-worker source change;
  this exception does not authorize IDE deployment or CI-policy relaxation.
- WB-A contract gate passed before behavior: strict grading tsc and the
  positive safe-integer schema case. Six of nine behavioral cases then failed
  for the intended budget defects; the minimal guard now passes all nine.
  Add `grading/browser-budget.test.mjs` to the allowed tests: use Node's
  module hooks to replace only public collaborators with a fake API, proving
  stream plumbing, rejection instead of a score, disposal and prompt failure
  even when overlay cleanup cannot settle. No test executes compiler code.

### Builder / Auditor results for UX02-C preparation

Same-agent Builder and adversarial Auditor. This is publication metadata and
worker-safety preparation, not completed UX02-C, UI cutover or runtime proof.

- Implemented: optional published problem and protected validation solutions,
  stable file/test/case identities, explicit draft compilation, both learner
  projection boundaries, and exclusion of teacher solutions from official
  student execution. Existing absent fields serialize as absent. No adapter
  version, relational change, migration, automatic conversion or data reset.
- Strict contract fixtures passed before consumer behavior. Initial red
  evidence: seven valid draft compilations; twelve of fifteen backend metadata
  cases; nine of ten web publication cases. Separate audit regressions then
  reproduced mixed-family acceptance, total problem-size bypass without a
  reference, extra private fields and a lost `__proto__` file-map key.
  Fixes retained the independent assertions, strict types and lint rules.
- Two revision tests use the actual AssessmentAuthoringService, EF InMemory
  persistence, immutable published revisions and a persisted submission.
  Publishing a later edited problem/file leaves the first snapshot/hash and
  submission binding unchanged. This is not PostgreSQL HTTP/browser evidence.
- WB-A's nine guard cases and four worker-composition cases pass. The latter
  first reproduced three failures. Deadline/output cancellation disposes only
  the owned public runtime, stops later compiler calls and can reject while
  terminated-VFS cleanup is still pending. Existing fixed 30-second / 2 MB
  defaults remain; author-configured limits have NOT yet been wired/published.

| Command / check | Result | Scope / limitation |
| --- | --- | --- |
| Coding, grading-adapter-code and coding-surface typechecks/tests and strict package lint | exit 0; 78 / 16 / 46 tests | no compiler/runtime boot |
| G-WEB six-file selection: publication, bridge, client, draft persistence, Learning authoring and workspace | exit 0; 141 tests | publication suite adds 14; API collaborators mocked |
| Full web `tsc --noEmit --pretty false`; strict changed-file web lint | exit 0 | two unsafe-array-spread diagnostics fixed without suppressions |
| Courses full test project, warning-clean build | exit 0; 903 tests | new service-level learner redaction case; EF InMemory |
| CodeAdapter full test project, warning-clean build | exit 0; 93 tests | metadata, official execution boundary and two persisted revision cases |
| Assessments full test project, warning-clean build | exit 0; 453 tests | includes neighboring Quiz adapter contracts |
| Authorization full unit suite | exit 0; 2815 tests | current unchanged platform/security source |
| Authentication full unit suite | exit 1; 4572 passed, one failed | existing `GenerateEmailCodeAsync_ThrottlesResendsWithinSixtySeconds`; no source/test change |
| Grading worker strict tsc and explicit-root strict ESLint on its five changed/new files | exit 0 | no ignored files, disabled rules, IDE build or asset publication |
| Worker new focused Node tests | exit 0; 13/13 | fake public collaborators, not real WASM execution |
| Full existing + new worker Node selection | exit 1; 60/62 passed | two pre-existing historical-identity/source-constant assertions remain unresolved |
| Preserved `@game-guild/emception-ui test` | exit 0; 27 tests | package and UI source unchanged |
| Quiz validation neighbors: bounded web validation tests, MFA/support contracts, grading and Quiz adapter tests | exit 0; 16 + 39 + 16 + 12 tests | one-worker equivalents of the root contract command; no browser/canonical-status promotion |
| Release classifier and required-gate Node suites | exit 0; 54 tests | no CI source/config changed |
| G-DOC structured Markdown audit and whitespace check | exit 0; 47 documents, 165 valid local links, 25 atomic rows in seven tables | no missing/empty atomic stage; no broken local link |
| API host build / architecture tests / official OpenAPI-client generation | open | recorded managed compiler OOM / earlier time stop; no stale-host or manual-generation substitute |

Worker failure reproducer: `node --test --test-concurrency=1
tools/emception/grading/toolchain-binding.test.mjs`. Its assertions named
`version 1 matches the server adapter source constants` and `version 2 ...`
expect old 4.4.0 / 4.5.1 identities inside the current C# adapter source. A
read-only comparison of that source at entry HEAD `68a9d5e702` proves those
literals were already absent; current identity 4.5.2 is present. Retained Node
identity validation still passes. Do not remove historical tests/identities or
reintroduce adapter versions to hide the disagreement. Reconcile the historical
contract/disposition before calling the complete worker gate green.

Commands use installed Node/pnpm, CPU affinity 0-9, one Vitest worker and
sequential checks. Backend commands use `--no-restore -m:1 -nr:false
-p:UseSharedCompilation=false -p:TreatWarningsAsErrors=true`, workstation GC
and the recorded 2 GiB per-process managed heap cap. Independent guards remain
6 GiB owned RSS / 4 GiB system available. Sampled final peaks: web lint 1978 MiB,
web tests 803 MiB, CodeAdapter 566 MiB, Assessments 646 MiB, Authorization
771 MiB and Authentication 1071 MiB. No resource stop in these final suites;
the earlier host failure remains separately recorded.

Official `needs_source_release()` now returns true for the five changed/new
`tools/emception/grading/` paths. This is a real required CI-provenance gate,
not permission to dispatch the standalone IDE deployment or fabricate a
manifest. No runtime bundle, compiler, CDN refresh, browser, Next server,
release or remote workflow was started. The only temporary service was the
project PostgreSQL read-only inventory above; it was stopped afterwards.

Final audit preserves platform modules, generic grading policy, IDE/UI/public
runtime, dependency manifests, CI and dev commands. New worker positive timer
cleanup uses Node's clock mock rather than requiring the host to finish within
five wall-clock milliseconds. All 13 focused worker cases still pass; no
required skip or relaxed assertion was introduced. The temporary resource and
Markdown auditors live only under `/tmp`, not in the developer workflow.

## Execution record: UX02-B persistence preparation - 2026-11-21

- Entry: committed UX02-A at `bb9cb883c7db6065a812449d5ed7bfd126245c38`,
  tree `01ab03dd3b781e8774da66a355e4c4ba2c0369df`, clean `feature/coding-grading`;
  user requested continuation. No merge or automatic commit.
- The broader UX02-A G-API failure/resource stop remains open. Governance
  permits unaffected checks within their own boundary; this continuation
  prepares and tests host persistence/recovery without closing UX02-A,
  entering UX02-C or cutting over the production editor.
- Specification: incomplete coding documents use Learning's existing
  revisioned authoring draft. Server Saved requires the exact write ack;
  edits made during that write remain unsaved. Failures/conflicts retain
  buffers and cannot silently overwrite another author. Reload/discard is
  explicit. Recovery checkpoints are cloned, author-only in-memory values,
  scoped to tenant/actor/course/content and base draft/publication revisions.
  No private solutions/cases are written to browser storage. A refresh or
  another device can recover acknowledged server data, not unsynced memory.
- Allowed edits: new persistence/session contracts and focused tests in
  `packages/features/coding-surface/src/`; its exports/architecture checks;
  a host bridge and focused tests in `apps/web/src/lib/coding-assignment/`;
  shared plan/state/ADRs. Existing Learning actions and authorization remain
  the server boundary; no second endpoint/store or active writer is added.
- Implemented: surface persistence/session ports and host bridge through the
  existing authorized Learning actions. A single in-flight save freezes its
  payload; only an exact body/revision acknowledgement advances Saved. A
  conflict stays locked even if reload fails. Scope changes/disposal prevent
  late responses from reviving a previous actor's state. Host metadata is read
  atomically with its revision; reload cannot enable a write using old metadata.
- Contract stage passed before behavior: strict surface types, two initial
  schema/port fixtures and lint; full web TypeScript validated the host port
  and fixtures. Missing public behavior then failed 22 session / 16 host
  oracles before implementation. Callback tests now import their pure module;
  public export/import boundaries have separate architecture/type assertions.
  Initial ES library/readonly fixture errors were corrected in tests without
  weakening compiler settings; they are not behavioral red evidence.
- Same-agent Auditor reproduced and fixed: lost conflict lock after failed
  reload; receiver-dependent recovery callback; changed body at the same
  revision; incomplete scope accepted by the host; cyclic metadata classified
  as a network failure; stale metadata overwrite after a newer coding reload.
  Malformed acknowledgements are invalid data, never Saved. Required oracles
  were retained. Both families use the same JSON fixtures as backend tests.
- Gate: G-CONTRACT/G-SURFACE; focused host bridge and existing Learning
  authoring/workspace tests, strict changed-file lint, G-WEB typecheck when
  safely budgeted. Existing UX02-A EF persistence/rejection evidence remains
  scoped as recorded below; no new backend change is planned. Full G-API and
  production browser/human acceptance remain open, not implicitly waived.
- Resources: entry MemAvailable 13,937 MiB; sequential checks, one JS worker,
  <=4096 MiB JS heap and CPU affinity 0-9 (two of twelve cores reserved).
  Do not repeat the twice-stopped API-wide compilation in this slice.
- No active editor/writer, generated client, API production source, table,
  migration, platform, Emception/UI, dev command or dependency was changed.
  This is tested integration preparation, not production UI cutover or human
  acceptance. The lesson coordinator must own the single save loop and adopt
  the whole acknowledged/reloaded lesson revision before issuing another write.
- Next: preserve the open broader gate; prepare the UX02-C published-data
  inventory and immutable publication/projection contract. Do not claim that
  an authoring draft is already publishable, or enter CODE-UX-03 runtime work.

### Builder / Auditor results for UX02-B preparation

Both roles were performed by the same agent. All selected final cases below
passed without skips; external/open gates were not rerun or relabeled green.

| Command / check | Result | Scope / limitation |
| --- | --- | --- |
| G-CONTRACT, coding and adapter typechecks/tests and strict package lint | exit 0; 49 + 15 tests | canonical executable contracts unchanged |
| G-SURFACE, typecheck, tests and strict lint | exit 0; 46 tests, 32 more than the committed baseline | memory-only lifecycle, ack/conflict/scope protections; no runtime/compiler |
| G-WEB focused selection: new `learning-draft-persistence.test.ts`, existing bridge, authoring actions and authoring workspace | exit 0; 106 tests, including 22 new host cases | real Server Actions with mocked HTTP; no API server or browser claim |
| Full web `tsc --noEmit --pretty false` and strict changed-file ESLint | exit 0 | 4096 MiB heap ceiling; no ignored type/lint diagnostics |
| Courses `--no-build --no-restore --filter FullyQualifiedName~CodingDraft` | exit 0; 4/4 | existing unchanged compiled service tests; EF InMemory roundtrip/conflict, not PostgreSQL HTTP |
| CodeAdapter `--no-build --no-restore --filter FullyQualifiedName~AuthorOnlyDraft` | exit 0; 2/2 | existing unchanged compiled draft-discriminator rejection cases |
| Preserved `@game-guild/emception-ui test`; release-classifier/required-gate Node suites | exit 0; 27 + 54 tests | no upstream/UI/CI source changes or publication |
| G-DOC, task-delta/negative-constraint review and official `needs_source_release()` on all 14 changed/new paths | 47 documents, 165 valid local links, 25 atomic rows; classifier false; whitespace clean | no runtime/IDE producer required; no production/human acceptance inferred |
| Broader G-API / full solution build | still open; not retried | previously recorded Authentication failure and twice-stopped API compilation remain; no new backend source change |

New implementation paths: surface `authoring-persistence.ts`,
`authoring-persistence-types.ts`, their contract/behavior tests and public
exports/architecture tests; host `learning-draft-persistence.ts` and its test.
CS-ADR-026 and the consumer map describe the required coordinator integration.

Commands use installed Node/pnpm, CPU affinity `0-9`, one Vitest worker and
sequential groups. The web lint runs from `apps/web` against exactly its two
new files with `--no-inline-config --max-warnings=0`; package lint covers all
three owned source directories. The G-WEB selection adds the new host test to
the three existing gate files. No dependency/tool was installed. An ephemeral
`/tmp/coding-ux02b-resource-guard.mjs` guards only owned processes, reserving
4 GiB system MemAvailable and stopping above 6 GiB owned RSS; this is not a
committed CLI or developer-command change. No API compile, Next build, runtime,
Docker, migration, data reset or background service was started.

Final resource observations: combined sampled owned-process RSS peaked at
826 MiB for G-WEB tests, 1404 MiB for full web types, 518 MiB for preserved
Emception UI tests and 326/311 MiB for the focused compiled backend cases.
The monitor includes descendants with separate process groups. Earlier
contract-stage full web types sampled 1551 MiB; all monitored commands stayed
well below the existing guard. RSS sums can double-count shared pages; these
are observations, not a physical-memory/runtime performance benchmark.
Two CPU cores remained reserved, and no resource stop was needed in this slice.

Bootstrap corrections are not product failures/passes: Jest rejected the old
G-REGRESSION invocation combining the package's existing `--runInBand` with
`--maxWorkers=1`. The plan command now uses the declared sequential test script;
all 27 cases ran. No protected package/config was edited. The Markdown audit
resolved unified/remark-parse from the declared react-markdown dependency
context after a direct web lookup failed; no library was installed.

## Execution record: UX02-A - 2026-11-21

- Entry: `f5436065e9f67e29967e77bbc22a8e9185b9e6fe`, tree
  `916eced726bb8da768be2229f80308df4ad6ca9f`, clean
  `feature/coding-grading`; no merge in progress.
- `CODE-UX-01` closure audit: strict prototype typecheck/lint passed;
  30 prototype and 26 preflight tests passed without skips; coding 15,
  adapter 15, bridge 5 and acquisition-policy 36 tests passed; package
  typechecks and strict contract lint passed. Documentation audit resolved
  168 local links in 47 files and all 25 atomic rows in seven tables.
  User design/scalar acceptance from 2026-11-20 remains authoritative.
  Published asset refresh and real execution remain a UX03 obligation.
- Active slice: `UX02-A`, implementation and in-scope audit complete;
  formal broader G-API gate remains open.
  Allowed edits: coding draft
  contracts/schemas, pure transformations, synthetic fixtures and tests;
  existing Courses authoring persistence tests and CodeAdapter rejection
  tests; shared plan/state/ADRs and the part 4 dependency-status mirror.
  No production host, canonical definition,
  generated client, table, migration, platform or Emception changes in this slice.
- Contract: a distinct `coding-authoring-draft` JSON document may contain
  incomplete problem, signature, scalar input and expectation fields. It is
  never asserted to be a publishable definition or delivered to a learner.
  One signature belongs to the function draft; stdio expectations distinguish
  absent (`null`) from intentionally empty (`""`). IDs survive edits and
  explicit duplication. Family/signature/environment/toolchain transformations carry an
  impact report; stale confirmation fails and cancellation preserves current data.
- Data preservation: existing canonical/wire interpretation and active writer
  remain unchanged. There is no automatic conversion or migration of persisted
  definitions. Import of an incompatible mixed/signature definition must fail
  explicitly rather than reinterpret it. Real persisted inventory is still
  unknown and remains required before a future breaking interpretation/cutover.
- Gate: strict coding/adapter types, focused draft schema/operation tests,
  G-CONTRACT, existing bridge regression, actual EF-backed shared-draft
  roundtrip/conflict tests and CodeAdapter draft-rejection tests. Any external
  backend failure remains visible; no relaxation of compiler, linter or security.
- Contract stage passed before transformation implementation: strict coding
  typecheck, 17 schema tests and lint. Initial operation-port test was red
  before implementation. Auditor then added two behavioral regressions that
  failed for the intended reasons: duplicate parameter names with different
  types incorrectly retained a value, and object property order produced a
  false change impact. Both causes were corrected without changing the oracles.
- In-scope backend proof: four shared-fixture save/reopen/conflict tests and
  the full Courses suite (902), CodeAdapter (74), Assessments (453) and
  Authorization (2815) suites passed without skips. Authentication reproduced
  its previously recorded email-resend throttle failure (4572/4573 passed).
  No Authentication source or test was modified. API.UnitTests compilation
  was stopped by the resource guard before a test result; the full solution
  build was not started. Details follow; neither is reported as passing.
- Next: resolve the broader gate failures with their owners/resource-budget
  disposition before declaring UX02-A green. UX02-B/C and all production,
  runtime and human acceptance remain open.

### Builder / Auditor results for UX02-A

Both passes were performed sequentially by the same agent, not an independent
reviewer. No human or production acceptance is inferred.

| Stage / command | Observed outcome | Scope / limitation |
| --- | --- | --- |
| Contract before behavior: `pnpm --filter @game-guild/coding typecheck`; focused `authoring-draft-contracts.test.ts`; strict ESLint | exit 0; 17 schema cases | incomplete shape, IDs/budgets, explicit missing/empty values and author-only discriminator |
| Transformation test-first and adversarial audit | absent operations failed first; two added behavioral regressions failed with concrete wrong values before their corrections | no assertion relaxation, suppressions or test skips |
| Final G-CONTRACT: coding/adapter typechecks and tests; strict ESLint for both packages | exits 0; coding 49, adapter 15 tests | 34 new draft contract/transformation cases; canonical definition and adapter behavior unchanged |
| Downstream regression: coding-surface typecheck/tests; web canonical bridge Vitest | exits 0; surface 14, bridge 5 tests | current consumers preserved; no production UI replacement or real runtime |
| Shared fixture through `ProgramContentAuthoringServiceTests.CodingDraft_*` | four focused cases passed, then full Courses 902/902 | actual service/EF InMemory write/read and expected-revision conflict; not PostgreSQL HTTP/browser proof |
| `GameGuild.Learning.Assessments.CodeAdapter.UnitTests` | 74/74 passed | includes both author-only-discriminator rejection cases with an otherwise valid definition |
| `GameGuild.Learning.Assessments.UnitTests` | 453/453 passed | existing academic contracts/regressions |
| `GameGuild.Identity.Authorization.UnitTests` | 2815/2815 passed | no platform changes |
| `GameGuild.Identity.Authentication.UnitTests` | exit 1; 4572 passed, one failed, zero skips | known `EmailCodeServiceTests.GenerateEmailCodeAsync_ThrottlesResendsWithinSixtySeconds`, also recorded before this task |
| `GameGuild.API.UnitTests` compilation, first attempt and reduced-concurrency attempt | both resource-stopped; no test result and no pass | peak sampled owned RSS 6457 / 6230 MiB exceeded the 6144 MiB guard; the CLI exited 0 on SIGTERM but the supervisor reported failure |
| `dotnet build apps/api/GameGuild.sln` | not run | stopped after the API compilation exceeded the same budget twice; no analyzer/typing relaxation or larger heap |
| Documentation/import/path audit | 47 Markdown files, 165 valid local links, 25 atomic rows; whitespace clean; official classifier says `source_release_required: false` for all 17 changed/new paths | no Emception/IDE/CI, dependency manifest, API production source, host writer, database or migration change |

Executed backend commands used `--no-restore -m:1 -nr:false
-p:UseSharedCompilation=false --nologo --verbosity quiet` with explicit project
paths from G-API. The focused persistence run used `--filter
FullyQualifiedName~CodingDraft`; the full suites did not filter/skip the known
Authentication failure. VSTest needed loopback sockets outside the sandbox;
the initial sandbox denial is bootstrap evidence, not a failed product test.
The Node shell used installed Node 24.16.0. pnpm needed access to its existing
cache outside the sandbox; no tool/dependency/version was installed or changed.

Resources: initial MemAvailable 15,209 MiB on 31,806 MiB RAM, with 12 CPUs.
All checks were sequential. JS used one worker and a 4096 MiB heap ceiling.
Backend MSBuild used one worker and at most ten CPUs, reserving two; the
bounded API retry used only two CPUs. The non-API backend suites peaked at
1327 MiB sampled owned RSS. The two API compilation attempts exceeded the
6 GiB owned-process target, so the monitor stopped their process groups,
preserving at least 8274 / 8011 MiB sampled MemAvailable. No kernel OOM or
successful API test run is inferred from those stops. The full build was not
attempted after the second stop. A process check confirmed no dotnet,
VBCSCompiler or testhost was left running by these checks. Existing developer
services, Docker, data and ignored Emception assets were not reset/replaced.

Outstanding ownership: the Authentication maintainer owns the existing resend
test failure. API-wide compilation needs a separately justified resource-budget
disposition or a less memory-intensive build path, not a disguised success.
Neither failure was fixed by changing other developers' source or the gate.

Current status: functional baseline and canonical consolidation 3.5 completed
locally. `CODE-UX-01` closed on 2026-11-21 after its corrected audit.
`CODE-UX-02` remains open for official-runtime fidelity and the external
Authentication failure. Contract/wire/backend/client, the strict full-solution
build and selected API architecture checks pass after the develop merge.
UX03-A independent preparation now supplies a tested lazy runtime port and
verified/backed-up published local assets. Its real-browser boot/disposal
diagnostic passes; case execution, capability resolution and production UI
cutover are not complete. No upstream IDE build/publication is required.

Este e o handoff mutavel da parte 3. Atualize-o com fatos observados. Nao marque
um gate como concluido sem registrar a evidencia executada.

## Baseline de execucao

- Branch: `feature/coding-grading`.
- Commit-base da consolidacao: `810c2a0764`.
- Baseline reavaliada em `43144e23ec`: branch alinhada com
  `origin/feature/coding-grading`, 188 commits atras e 7 a frente de
  `origin/develop` antes da atualizacao documental da parte 4.
- HEAD at refoundation entry: `723eeeb84589cfb23b0def630ee46ead02890acc`.
- Root tree: `c1236b3e35c0da3eed4e79135eb6b7090b91863f`.
- Entry worktree preserved: Learning workspace and content-tree source/tests,
  coding-surface source/tests/package, untracked `coding-workbench.tsx`, and
  `pnpm-lock.yaml`. No pre-existing changes were reverted. No merge/commit,
  fetch or database reset was performed. Historical develop counts above
  are not current remote-state evidence.
- Ultimo merge relevante: nenhum merge em andamento.
- Observacao: a criacao destes planos nao caracteriza a implementacao.

## Pre-requisitos

| Entrega | Estado | Evidencia |
| --- | --- | --- |
| Parte 01, dominio e adapter TypeScript | concluida | plano 01 e suites registradas nele |
| Parte 02, boundary backend de code | concluida | plano 02, build integral e suites registradas nele |
| Parte 3.1 | concluida | buscas de consumidores, contratos e exports publicos registradas em 2026-11-18 |
| Parte 3.2 | concluida | package isolado, 14 testes e typecheck verdes |
| Parte 3.3 | concluida | dois perfis, quatro modos, privacy e contrato canonico cobertos |
| Parte 3.4 | em validacao final | consumidores e CI migrados; runner fixture emits canonical `ExerciseType`; local assets pass preflight; full coding-cycle E2E remains pending |
| Parte 3.5 | concluida localmente | `CANON-01` a `CANON-06` concluidos; validacao local verde, com fixture do runner e browser CI ainda pendentes no fechamento de `CANON-07` |
| Parte 3.6 | in progress | `CODE-UX-01` closed; UX02-C/WB-B contracts, wire, backend and current client checks pass; official execution fidelity and broader G-API disposition remain open |

## Etapa ativa

- Subplan: [3.6.2 domain and drafts](./06-refoundation/02-domain-drafts-and-data-boundaries.md).
- Current gate: `CODE-UX-02` remains open; independent UX03-A preparation is
  implemented and under closing audit.
  Explicit configuration roundtrip/backend validation are tested; official
  execution fidelity and the broader G-API disposition remain open. Current
  host/client and selected architecture evidence are complete and recorded.
  No runtime/production/human acceptance is inferred from this preparation.
- Design and initial scalar-only scope remain approved. ADRs 014-024 retain
  ownership/governance/artifact decisions; ADR025 records the new author-only
  draft; ADR026 records revision-bound host persistence and memory-only recovery;
  ADR027 publication metadata; ADR028 the user-approved grading-worker exception;
  ADR029 the approved exercise-memory deferral and independent build budget;
  ADR030 the frozen execution contract; ADR031 the historical identity oracle;
  ADR032 the worker's deliberately local transport guard/import boundary;
  ADR033 the owned lazy runtime port and boot-only evidence; ADR034 the authorized
  terminal-phase metadata and initial-source semantics. Actual local
  artifact preflight passes; native conformance remains open.
- Responsavel: Codex.

### Current compact checkpoint

- Baseline: `4a212cd7ff`, clean resumed entry after the verified develop merge.
  Allowed files, contract/RED evidence and commands are in the dated resume
  record at the top. No automatic commit or push is authorized.
- Active row/stage: authorized runtime phase/initial-file correction / audit;
  UX02-C/WB-B official execution fidelity remains open. The port is not yet
  connected to the production author/learner workbench or save coordinator.
- Contract: no V2, table, migration or platform edit. CS-ADR-034 permits only
  additive public phase metadata and its existing facade/engine/worker usage.
  Legacy snapshots remain intact; configured exercise RAM remains unavailable.
  Metadata preflight compares the requested frozen identity, not a guessed
  preset. The port contains no compiler, test engine or academic command.
- Terminal: core 273, browser 47 plus one existing opt-in skip, worker 110,
  surface 80, coding 96, adapter 16, retained UI 27 and affected web 135 pass;
  strict SDK/surface/host types pass. Unsuppressed upstream ESLint remains
  nonzero on seven identical HEAD/current warnings; no new diagnostic.
  Asset/preflight/policy checks pass. Prior merge checks include strict complete solution build, API 526,
  Courses 1227, CodeAdapter 179, Assessments 453, Authorization 2815, client
  1794 and web selection 325. Authentication remains 4752/4753, the external
  resend-throttle failure; it is not silently waived or fixed here.
- Resources: OS-friendly headroom/pressure policy supersedes fixed 10/14 GB
  ceilings. Final official-worker controls peak at 3698 MiB, minimum available
  13047 MiB; host tsc peaks at 1874 MiB. No new OOM/resource stop; one heavy
  task/browser, CPUs 0-3, no full stack.
- Runtime/human: both local asset trees pass exact metadata/bundle checks,
  backed up before refresh. The real browser proves lazy acquisition and worker
  disposal; 12 subsequent official-worker controls now prove the scoped
  compile/link correction and approved initial-file behavior, not function ABI,
  graphics, interactive input, five-cycle resources or human UX.
  Exact-source CI requirements remain intact.
- Decisions: both questions explicitly authorized and implemented (CS-ADR-034).
  Remaining function/graphics/import findings do not inherit upstream permission.
- Next exact action: investigate function ABI and graphics through public APIs
  before capability promotion/cutover; apply EMCEPTION-GATE for additional
  protected edits. Preserve the validated lazy port and assets. Do not redo OOM diagnosis, deploy the
  independent IDE, install another compiler/harness or claim a phase gate pass.

| Gate da refundacao | Estado | Evidencia necessaria |
| --- | --- | --- |
| `CODE-UX-01` | complete | corrected contract/document/design/preparation audit passed on 2026-11-21; no runtime/build claim |
| `CODE-UX-02` | in progress / gate open | contract/wire/backend/current-client/selected-architecture and final full-solution checks pass; runtime fidelity and external Authentication failure remain open |
| `CODE-UX-03` | preparation in progress / gate open | local assets, lazy runtime and scoped 12-case stdio/initial-file controls pass; broader resolver, function/graphics/terminal and five-cycle resource acceptance remain open |
| `CODE-UX-04` | nao iniciado | autoria completa dos dois tipos e validacao pelo professor |
| `CODE-UX-05` | nao iniciado | resolucao, envio, recuperacao e revisao por persona |
| `CODE-UX-06` | nao iniciado | integracao, matriz de aceite e revisao humana |

## Fatos confirmados

- `@game-guild/emception-ui` deve permanecer preservado.
- `coding-surface` sera um consumidor paralelo dos exports publicos do
  Emception.
- a primeira composicao visual com o componente monolitico `Ide` foi corrigida:
  o package agora possui o workbench e consome `createEmception()` headless;
- somente fluxos de grading explicitamente selecionados podem migrar.
- `function` e `standard-io` formam o corte inicial de tipos.
- nenhuma mudanca de tabela foi decidida para a 3.6; necessidade demonstrada
  exige `SCHEMA-GATE`, migration e validacao, sem apagar historico.
- `CodingAssignmentContent.ExerciseType` permanece no JSON ja persistido; nao
  cria coluna, tabela ou migration.
- o diff atual de `@game-guild/emception-ui` e `tools/emception` permanece vazio;
- `@game-guild/coding` possui um unico contrato, parser por payload e fixture
  canonica, com `exerciseType` obrigatorio;
- backend e TypeScript registram apenas `code-assessment-type` e
  `code-wasm-review` na versao `1`, com payload schema `code-files`;
- qualquer adapter, handler, payload schema ou toolchain diferente da identidade
  corrente falha fechado;
- nomes `V1` importados de `@game-guild/grading` pertencem ao protocolo
  generico e permanecem fora da consolidacao de code.
- a superficie de autoria precisa ser distinta da superficie de execucao;
- configuracao passa a views por tarefa, com largura integral ou composicao
  lateral quando util; output e console nao recebem formularios;
- o contrato canonico completo nao deve ser usado como estado intermediario de
  formulario; recuperacao de draft e validacao para publicacao sao operacoes
  distintas no modelo alvo;
- canvas e terminal sao capabilities e nao exigem automaticamente outro tipo;
- contratos e componentes nossos podem evoluir para o modelo aceito, em vez de
  preservar a estrutura inicial apenas porque ja foi implementada.

## Decisoes abertas

`CS-ADR-017` adopts the Meta-Harness protocol for all of 3.6; `CS-ADR-018`
records the additional design iteration. The user subsequently explicitly
approved the prototype design and initial scalar-only function scope on
2026-11-20. This does not approve a production cutover or CI deployment.

`CS-ADR-014` records the actual field ownership/persistence trace and the
single Learning draft/publication model. `PRODUCT-MODEL.md` contains the
teacher/learner journeys, component dispositions, runtime matrix and budgets.
Implementation of these domain choices belongs to `CODE-UX-02`; no database
schema or platform module was changed.

`EMC-GAP-001` is confirmed through actual installed public API calls:
scalar harness generation succeeds; Array inputs, returns and nested values
are rejected. The official validator is also scalar-only. `CS-ADR-015`
now records the explicit user acceptance of initial scalar scope with an
upstream follow-up. Compound support remains unimplemented, not implicitly
supported or permanently excluded.
The installed ESM public export was re-probed at HEAD `40a8470b46` with the
same boundary result. CS-ADR-023 retains the discovered manual producer
deployment side effect; CS-ADR-024 withdraws the unnecessary build-only CI
proposal and records the compatible published consumer path.

## Bloqueios

Resolved fixture defect: the actual cycle imports `buildCodingContent()` from
`apps/web/scripts/coding-cycle-fixture.mjs`, which emits `ExerciseType:
"function"`. Its focused canonical bridge test passes, including rejection
of a missing type and private-test redaction. Historical mixed execution
coverage is retained; this does not claim the new family-specific browser
journeys are implemented.

A entrada da parte 4 continua bloqueada por `CODE-UX-01` a `CODE-UX-06`.
O browser E2E deve provar o modelo refundado, e nao congelar como aceita a
composicao antiga. Preflight verde nao substitui aceite de produto ou CI.

The real local preflight still rejects the schema-1 assets. Compatible 4.5.2
manifests are available from published packages; the earlier zero-result
current-HEAD GitHub queries do not establish a consumer artifact shortage.
Actual synchronization/local verification is pending before CODE-UX-03
execution, not a blocker requiring an Emception build before CODE-UX-02.
Preserve compiler identity, applicable CI provenance and old generated assets.
See `CS-ADR-024` and `QUALITY-GATES.md` for the backed-up refresh path.

Resource entry: 31,806 MiB RAM, 6,486 MiB available, 9,391 MiB swap used,
12 logical CPUs. No full stack/build was started. The historical cycle
requests a 12 GiB build heap; it is not suitable for an unmeasured local run
on that baseline. The recorded budget requires adequate memory first.

A suite `GameGuild.Identity.Authentication.UnitTests` possui uma falha externa
reproduzida tambem de forma isolada em
`EmailCodeServiceTests.GenerateEmailCodeAsync_ThrottlesResendsWithinSixtySeconds`
(`4572/4573` testes verdes). Nao ha diff no modulo de Authentication e a falha
nao pertence ao contrato de coding; ela deve ser tratada pelo responsavel desse
modulo sem ampliar o escopo da parte 3.

## Evidencias de validacao da etapa ativa

Historical implementation checks are retained below. Current `CODE-UX-01`
evidence is listed separately; it does not certify runtime or product gates.

| Data | Comando ou inspecao | Resultado | Escopo |
| --- | --- | --- | --- |
| 2026-11-18 | `rg` de imports Emception e leitura dos cinco consumidores | cinco fluxos academicos confirmados; package e aliases preservados | 3.1 |
| 2026-11-18 | leitura dos exports publicos do Emception | IDE, controller, presets, testing e overlay publicos cobrem o corte | 3.1 |
| 2026-11-18 | `git status --short --branch` e `git rev-parse HEAD` | worktree limpo no commit-base registrado | 3.1 |
| 2026-11-18 | testes e typecheck de `coding`, `grading-adapter-code` e `coding-surface` | 15 + 15 + 14 testes; tres typechecks verdes | 3.2/3.3 |
| 2026-11-18 | Vitest direto nos sete consumidores web | 7 arquivos e 77 testes verdes com um worker | 3.4 |
| 2026-11-18 | `@game-guild/emception-ui test` | 7 suites e 27 testes verdes; package preservado | 3.4 |
| 2026-11-18 | TypeScript integral de `apps/web` | verde | 3.4 |
| 2026-11-18 | testes focados de `GameGuild.Learning.Courses` | 898 testes verdes | 3.4 |
| 2026-11-18 | `dotnet build apps/api/GameGuild.sln --no-restore --nologo -m:2` | sucesso, zero warnings e zero erros | 3.4 |
| 2026-11-18 | build de runtime Emception grading | 49 testes e bundle Vite verdes | 3.4 |
| 2026-11-18 | build de producao web | 337 paginas geradas; passou no retry previsto pelo guard de seguranca do Next | 3.4 |
| 2026-11-18 | testes de classificacao, artifacts, required gate e toolchain binding do CI | 96 testes verdes | 3.4 |
| 2026-11-18 | preflight browser coding cycle | 26 testes verdes | 3.4 |
| 2026-11-18 | browser coding cycle integral local | interrompido fail-closed: CDN local schema 1 nao satisfaz manifest V3 schema 2 | 3.4 |
| 2026-11-18 | publish da API com `-m:2`, captura OpenAPI e gerador oficial de `@game-guild/client` | `exerciseType` presente no contrato; diff gerado restrito a tipos/schema e metadata | 3.4 |
| 2026-11-18 | `@game-guild/client typecheck` e `@game-guild/client build` | verdes | 3.4 |
| 2026-11-19 | inventario `rg` de contratos, parsers, fixtures, adapters, toolchains e payload schemas | `CANON-01` concluido; escopo restrito a coding, adapter, surface, backend CodeAdapter, web e testes | 3.5 |
| 2026-11-19 | suites e typechecks de `coding`, `grading-adapter-code`, `coding-surface` e `grading` | 15 + 15 + 14 + 16 testes verdes; quatro typechecks verdes | 3.5 |
| 2026-11-19 | testes focados dos consumidores web | 11 arquivos e 195 testes verdes com um worker | 3.5 |
| 2026-11-19 | TypeScript integral de `apps/web` com heap de 8 GiB | verde apos regeneracao do client | 3.5 |
| 2026-11-19 | publish da API, captura OpenAPI e geracao oficial de `@game-guild/client` | `exerciseType` requerido; client typecheck e build verdes | 3.5 |
| 2026-11-19 | `CodeAdapter.UnitTests`, `Assessments.UnitTests` e `Courses.UnitTests` | 72 + 453 + 898 testes verdes | 3.5 |
| 2026-11-19 | `AssessmentGradingPostgreSqlHttpTests` com certificado local de Data Protection | 14 testes de integracao PostgreSQL verdes | 3.5 |
| 2026-11-19 | `@game-guild/emception-ui test` | 7 suites e 27 testes verdes; package preservado | 3.5 |
| 2026-11-19 | preflight do coding cycle fora do sandbox | 45 testes verdes, incluindo toolchain, browser observer e processos | 3.5 |
| 2026-11-19 | build de producao web | compilacao, TypeScript e 337 paginas verdes com `cpus: 1` | 3.5 |
| 2026-11-19 | `dotnet build apps/api/GameGuild.sln --no-restore --nologo -m:2 -p:TreatWarningsAsErrors=true` | sucesso em 5m40s, zero warnings e zero erros | 3.5 |
| 2026-11-19 | `CodingAssignmentJsonBodyMigrationTests` | 8 testes verdes; snapshot test-only preserva a saida historica sem enfraquecer `ExerciseType` no contrato atual | 3.5 |
| 2026-11-19 | `GameGuild.API.UnitTests` integral | 1882 testes verdes | 3.5 |
| 2026-11-19 | `GameGuild.Identity.Authorization.UnitTests` integral | 2815 testes verdes | 3.5 |
| 2026-11-19 | `GameGuild.Identity.Authentication.UnitTests` integral e caso isolado | 4572/4573 verdes; falha externa de throttle de email reproduzida isoladamente, sem diff no modulo | 3.5 |
| 2026-11-19 | `@game-guild/coding-surface typecheck` e `test` apos substituir o `Ide` monolitico | typecheck verde; 2 arquivos e 14 testes verdes | correcao visual 3.2/3.3 |
| 2026-11-19 | inspecao humana da autoria no Learning em viewport desktop | workbench amplo confirmado, mas configuracao, output e formularios de testes ainda misturados; parte 3.6 requerida | baseline `CODE-UX-01` |
| 2026-11-19 | revisao documental com Node em modo somente leitura e `git diff --check -- docs/plans/coding-assessment-and-code-studio` | 42 documentos e 109 links locais sem destino ausente; seis subplanos, 22 IDs de aceite e dependencias consistentes; sem erros de whitespace | planejamento 3.6, nao execucao dos gates |

The earlier planning rewrite did not run application builds. Its note follows:

A reescrita documental nao executou builds, testes de aplicacao ou novos
benchmarks de runtime. O comando `node` nao estava no PATH desta sessao; a
checagem usou o executavel instalado em `.nvm/versions/node/v24.16.0/bin/node`,
sem iniciar servicos nem alterar os arquivos de aplicacao ja presentes no worktree.

### `CODE-UX-01` execution evidence - 2026-11-19

| Check | Observed result | Limit |
| --- | --- | --- |
| installed public `emception/testing` calls | integer harness generated; arrays/matrices/aggregate returns rejected | feasibility only, no compiler boot |
| package manifests and installed public exports | core/browser/IDE 4.5.2, headless workspace/testing/canvas exports present | a public export does not establish a working family/environment combination |
| real cycle fixture through `coding-surface-bridge.test.ts` | 5 tests passed, including `CS-REF-21` canonical fixture/redaction/missing-type regression | no API/database writes |
| toolchain/editor/web-runtime preflight suites | 26 tests passed, sequentially | subprocess tests use dummy Node executables, not a Next build |
| preflight ordering regression after edit | affected 14-test toolchain suite passed again; invalid artifacts are rejected before grading-runtime build | no full runtime build |
| actual release manifest preflight | rejected: verified artifact matching frozen identity required | gate remains blocked |
| exact-name GitHub Actions artifact query | `total_count: 0` | read-only; no producer launched, no files downloaded |
| prototype Playwright inspection | 69 checks; six captures, three viewports; keyboard/task navigation and no horizontal overflow | layout only, no Emception or academic behavior |
| prototype browser resource observation | 2.27 seconds; maximum sampled known Chromium RSS 376 MiB; browser closed | not a WASM memory/startup benchmark |
| focused lint, syntax and document checks | fixture/bridge ESLint passed; script syntax passed; 122 local links across 43 docs resolve; diff whitespace clean | no application build |

The commands and capture paths are recorded in `PRODUCT-MODEL.md`. Node
24.16.0 was used explicitly because the tool shell lacks Node on PATH.
Sandbox restrictions required rerunning subprocess/Chromium checks outside
the sandbox; those bounded checks passed. No runtime was left running.

Current work files: `coding-cycle-fixture.mjs`, its importing cycle,
`coding-surface-bridge.test.ts`, the layout HTML specimen, `PRODUCT-MODEL.md`
and shared plan/state/decision/consumer/gap/duplication records. The original
application/source/lockfile changes are preserved and are not this execution's
production implementation. No backend, generated client, migration or Emception
source file was edited.

### Critical review and planning follow-up - 2026-11-19

The actual specimen was inspected in Chromium, alongside the first-party
references recorded in `COMPETITIVE-BASELINE.md`. UX-F01..06 identify missing
typed authoring, learner diagnosis, faithful reference/preview workflows,
host-constrained layouts, capability compatibility and draft recovery.
In the simulated 1280x720 host, the central region was 704 px wide and expected
output began at y=796; the 390x844 learner specimen placed result tabs at
y=1096. Changing the problem/starter to a maximum function did not change its
preview. These are prototype findings, not measurements of live Learning.
Review captures: `/tmp/coding-surface-review-20261119/` (ephemeral local evidence).

The follow-up changes documentation only: 01a, governance, terminal gates,
atomic rows in all six subplans, acceptance mapping and shared handoff.
No prototype, application, dependency, runtime, migration or platform code was
changed during this planning task. Prior application diffs remain untouched.
Passing layout checks have not been promoted to UX or runtime acceptance.

| Planning check | Result | Scope / limitation |
| --- | --- | --- |
| Node 24.16.0 read-only inline audit using installed remark parser/GFM | exit 0; 46 documents, 165 local links, 7 atomic subplans/24 rows; six findings mapped and all 22 CS-REF IDs preserved | local targets and required table structure, not runtime or UX acceptance |
| Node 24.16.0 read-only gate-target/manifest audit | exit 0; 14 existing command paths and six package manifests checked | planned prototype files are explicitly future deliverables, not executed tests |
| `git diff --check -- docs/plans/coding-assessment-and-code-studio` | exit 0 | documentation whitespace |
| Auditor self-review against Meta-Harness and entry scope | no new boundary violation identified; tiered context and next action reconciled; upstream/dev scripts have no diff | same-agent review, not independent human design acceptance |

### Authoring terminology correction - 2026-11-19

Baseline: clean worktree at `efcc1c69fe`. User explicitly requested the label
**Validate exercise** to distinguish authoring preparation from learner grading.
Only prototype copy and plan/state/ADR wording changed; internal IDs, behavior,
production code and grading contracts are unchanged.

Playwright/Node inline check: exact tab-name assertion failed before the edit
(exit 1, zero matches), then passed with keyboard navigation, panel/heading,
status and overflow checks at 1280x720 and 390x844 (exit 0). Mobile capture
was visually inspected. Captures: `/tmp/coding-surface-label-20261119/`.
The default Playwright browser was absent and sandbox launch was denied;
these bootstrap failures were not the behavioral red evidence. The checks
used the already-installed Chrome outside the sandbox, one browser, closed
afterward. No application stack, build, installation or real runtime was run.

### 3.6.1a interactive prototype execution - 2026-11-20

Baseline: `8c6c31b459a994e7f7f6fd7d0166d8446632a377`, clean worktree after
the user committed prior naming changes. This execution changed only the HTML,
its new `06-refoundation/prototype/` directory and shared documentation/ADRs.
Production packages, Emception/UI, dependency manifests, auth/platform, API,
migrations and developer commands have no task delta. No consumer cutover.

All rows followed specification/contracts, strict compilation, an independent
behavioral red, minimal implementation, then Builder/Auditor checks:

| Slice | Behavioral red / audited defect | Green design evidence |
| --- | --- | --- |
| UXP-01 | absent title/blank draft controls | shell, keyboard tasks, malformed states and bounded harness |
| UXP-02 | absent signature/family fields; duplicate environment field/panel ID | function and I/O from blank, type-specific values, stable case CRUD, explicit empty output and compatible environments |
| UXP-03 | absent reference/preview controls | correct/wrong records, stale candidate identity, edited/redacted preview, independent files and escaping |
| UXP-04 | absent learner entry | wrong/expected/actual, absent oracle, compile/error/cancel/stale states, frozen acknowledgement and preserved host feedback |
| UXP-05 | absent save/panel controls; clipped long names/lost case scroll/mobile label collision in capture review | retained failures/conflicts, scoped cloned recovery, lossless cancel, panels, focused views and keyboard reachability |
| UXP-06 | incomplete selection accepted; unknown fixture/unsafe IDs; no-op typed commands; unavailable capability; modal return focus; pending signature and scratch changes incorrectly affecting submission | all 13 frozen browser oracles and 11 Node tests pass; cases are required by exact ID, no skip/zero-case success; same-agent adversarial audit completed |

Terminal checks use installed Node 24.16.0, TypeScript 6.0.3, ESLint 10.13.0,
Playwright 1.64.1 and existing Chrome. The shell lacks Node on PATH, so commands
used `/home/miguel/.nvm/versions/node/v24.16.0/bin/node` directly with the root
`node_modules/typescript/bin/tsc` and `node_modules/eslint/bin/eslint.js`.
No dependency was installed. Node subprocess/Chrome checks ran outside the
restricted sandbox; its earlier file-only summary was not accepted as proof.

```bash
node node_modules/typescript/bin/tsc --project docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype/tsconfig.json --noEmit
node node_modules/eslint/bin/eslint.js --no-inline-config --max-warnings=0 docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype
taskset -c 0-9 node --test --test-concurrency=1 docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype/contracts.test.mjs docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype/workflows.test.mjs
```

Results: types/lint exit 0, final cumulative run 24/24 tests, zero skipped,
about 12 seconds including all refreshed captures. Source formatting uses the
installed Prettier. G-DOC exit 0: 47 documents, 168 local links, all 24 atomic
rows populated and all 22 CS-REF IDs preserved; new sources are ASCII and
`git diff --check` is clean. All task paths are confined to this docs subtree.
Final capture audit also reproduced a selected case leaving the visible list
after a mobile/desktop transition. An explicit failing viewport assertion was
added; selection scrolling was fixed and the full 24-test gate passed again.
The exact task is also reviewable through the prototype's frozen checklist.
Application/API/Emception build and security suites are inapplicable to this
documentation-only design fixture; they were not rerun or claimed passed.

Captures are ignored artifacts in
`apps/web/test-results/coding-surface-refoundation/`: six `uxp05-teacher-*` /
`uxp05-learner-*` images at 1600x1000, 1280x720 and 390x844, plus
`uxp06-teacher-io.png` and three `uxp06-canvas-*` images. Teacher/learner and
canvas captures were visually inspected. Canvas pixels, movement, stop/reopen
and distinct echo input are synthetic design checks, not Emception conformance.

Observed resources: one browser/page, seven reported Chromium processes with
summed RSS 858 MiB, Node RSS 132 MiB; the RSS sum double-counts shared memory
and is a single sample, not a peak/physical-memory benchmark. CPU affinity
`0-9` reserves two of 12 cores. Entry MemAvailable 15,718 MiB; no heavy stack
or compiler started. Browser/context disposal passed; no services left by the
prototype. Production runtime budgets remain unproven.

Actual frozen-toolchain preflight was rerun using the command in G-PREFLIGHT:
exit 1, `Code grading requires verified artifacts matching its frozen toolchain.`
No permissive manifest, upstream edit or producer launch was used. The earlier
exact-name GitHub query remains historical evidence, not a new remote query.

Human: pending. UXP-06 automation and same-agent audit do not approve usability.
No implicit acceptance of scalar-only scope or runtime support. Saved/recovery
records are in-memory for the open tab; actual refresh restarts the design
study. Independent runtime and durable persistence remain later requirements.

### UXP-07 editor-fidelity follow-up - 2026-11-20

Specification/contracts preceded behavioral changes. The strict contract
compiled before interaction implementation. An explicit new command-schema
test failed before validation was added; all three new browser oracles failed
on absent source tabs/explorer, not on harness/bootstrap. The earlier dirty
prototype work was preserved. Allowed files stayed within the prototype,
HTML entry and its current plan/model/gates/ADR/ledger.

Builder: a shared editor composition now covers author, learner and redacted
preview. Open tabs, explorer, colorized/numbered native text, per-file caret/
scroll, code modification markers and read-only support represent the intended
Monaco-backed experience without loading Monaco or a runtime. Author creation
and rename reject traversal/path collisions; deletion requires confirmation;
tab closure never deletes buffers. Signature configuration collapses outside
editing; learner study controls collapse on mobile entry to preserve code space.
Reference/incorrect preparation uses the same visual editor, separately from
the supplied learner files. No new learner file-management policy was assumed.

Auditor (same-agent): caught an explorer drawer intercepting tabs at constrained
width, missing keyboard focus after closing the last tab and support-file edits
reusing an old recorded verdict. Those causes were fixed; a Node test reproduced
the simulation defect before the fix. Current file sets reach the simulation
and unrecorded support/source combinations cannot fabricate results. Added
empty-tab, save-marker, read-only, per-file position, Tab/Escape and forced-color
checks. No types, lint rules or required assertions were suppressed.

Terminal: the same G-PROT commands above pass after the final changes, exit 0:
strict TypeScript, ESLint without warnings, and 30/30 tests (14 Node + 16 browser),
zero skips, 14.6 seconds. One Chrome, sequential pages, CPU affinity 0-9 leaves
two cores free. Network/compiler workers remain absent; context/browser cleanup
passes. No stack, dependency installation, heavy build or service was started.

Six additional captures `uxp07-author-editor-*` / `uxp07-learner-editor-*`
are in the same ignored output directory. Inspected author 1600/1280/390 and
learner 1280/390 captures; the browser also asserts mobile code height >=120 px,
command reachability and no page overflow. Native rendering is a design
specimen, not proof of editor language services or public runtime integration.
The earlier measured RSS sample is historical, not a new resource benchmark.

G-DOC passed: 47 Markdown documents, 168 valid relative links, all 25 atomic
rows populated, ASCII prototype sources and clean `git diff --check`. The
read-only AST check identifies specification tables by both Slice and
Specification headers; evidence/checklist tables are not misclassified as
six-column atomic specifications.
Current instructions and CS-ADR-022 record the updated seven-slice selector.
Human design acceptance, compound-value disposition and verified artifact
obligations remain pending; no production gate was entered or waived.

### UX01-C closure audit - 2026-11-20

Historical audit: its mandatory exact-source/build-only CI interpretation and
next-action proposal below were superseded by CS-ADR-024. Test observations
remain evidence of their stated scope, not runtime or production acceptance.

Baseline is the committed prototype `40a8470b46`, with clean entry worktree.
Scope was read-only technical validation plus the current documentation;
the artifact/compound decisions were not bypassed by starting production work.

| Check | Current result | Meaning |
| --- | --- | --- |
| strict prototype typecheck/lint and full sequential design suite | exit 0; 30/30 tests, zero skips, 14.3 seconds | committed design/contract regression only |
| coding and grading-adapter-code typechecks, focused ESLint | exit 0, no warnings | no schema/type/lint weakening |
| coding package suite | 15/15 | current owned contracts |
| grading-adapter-code package suite | 15/15 | pure adapter parity |
| real canonical bridge focused suite | 5/5 | current fixture, required family and private-data redaction |
| preflight/editor/runtime-launch unit suites | 26/26, zero skips | no Next, Docker, API or WASM boot; mocked child processes |
| official acquisition unit suite | 36/36 | mocked GitHub, digests/provenance/archive/producer-step checks, no download; duplicate-ZIP warnings are intentional negative fixtures |
| installed public ESM harness probe, cwd apps/web | 4 scalar harnesses pass; 6 compound probes reject | no compiler boot or private imports; not full execution conformance |
| current exact-source producer/artifact API queries | 0 runs, 0 artifacts | no available producer result for the current committed source |
| actual frozen manifest check | exit 1: verified artifact matching frozen toolchain required | G-PREFLIGHT not passed |

Commands used the installed Node 24.16.0 and package-local Vitest 5.0.5,
TypeScript 6.0.3, ESLint 10.13.0 and Chrome. Package tests ran with
`node node_modules/vitest/vitest.mjs run --maxWorkers=1` from their workspace;
the bridge selected `src/lib/coding-assignment/coding-surface-bridge.test.ts`
from `apps/web`. Package types used root TypeScript with their explicit
`--project .../tsconfig.json --noEmit --pretty false`; no tool was installed.
The public probe used ESM `import ... from 'emception/testing'`, not a CJS
require resolver or an upstream private file. Preflight and prototype commands
are in QUALITY-GATES; acquisition used the existing Python unittest suite.

Resource entry: MemAvailable 14,992 MiB, swap used 8,467 MiB. All checks stayed
lightweight, sequential and CPU-affined to 0-9 (two cores reserved), one browser
with disposed pages/contexts. No full application/API/compiler build, producer,
release write, database, dependency installation or background service.
Python bytecode created by its tests was removed; only docs remain changed.

Read-only cloud review: latest successful workflow run 44523205221 skipped
the producer and uploaded no artifacts; overall green is not release evidence.
Current manual `emception.yml` would also deploy the IDE to GitHub Pages even
with `publish_packages: false`. This contradicts the earlier safe-dispatch
assumption. CS-ADR-023 records that safety finding; its build-only proposal
was subsequently withdrawn by CS-ADR-024. No remote workflow/issue/publication
change was performed.

Human questions presented: accept the design after its walkthrough; approve
temporary scalar-only function scope or require compound support first; hand
build-only CI changes to the Emception owner or authorize that CI-only scope
here. These historical questions are not current instructions: design/scalar
scope was explicitly accepted and the unnecessary CI proposal withdrawn.
No automatic human acceptance or gate completion was inferred.

Final documentation audit: 47 documents, 168 valid local links and 25 complete
atomic rows; `git diff --check` passes. Explicit diff against runtime/IDE, CI,
apps/packages, scripts and root AGENTS is empty. Only the six current planning/
state documents changed; no unrelated source or generated bytecode remains.

## Arquivos alterados pela implementacao

- contrato canonico unico de coding, sem aliases de compatibilidade;
- identidade unica e concordante do adapter backend e TypeScript;
- novo `packages/features/coding-surface`;
- workbench visual proprio em `coding-workbench.tsx`, com runtime headless e
  sem renderizar a IDE generica;
- campo JSON `ExerciseType` e validacao no modelo de Learning Courses;
- contrato OpenAPI e cliente TypeScript gerado para `ExerciseType`;
- bridge e cinco consumidores de grading em `apps/web`;
- lane `coding`, classificacao e required gate no CI;
- testes, fixtures e documentos de checkpoint associados.
- ajuste exclusivamente test-only para ler a saida historica da migration; a
  migration, snapshots de banco e schema permanecem inalterados.

## Proxima acao exata

Continue the runtime disposition in
[`3.6.3`](./06-refoundation/03-workbench-runtime-and-capabilities.md), retaining
the open broader gates in [`3.6.2`](./06-refoundation/02-domain-drafts-and-data-boundaries.md):

1. Retain the explicit user design approval recorded for
   [3.6.1a](./06-refoundation/01a-prototype-workflows-and-critical-review.md).
   Do not repeat the prototype work or mislabel that approval as production
   runtime/usability acceptance. Initial scalar-only scope is also explicitly
   accepted in CS-ADR-015; compound support remains tracked in EMC-GAP-001.
2. Retain the tested UX02-A/B ports and UX02-C/WB-B configuration and immutable
   revision evidence. The local read-only inventory is not a remote-data claim.
   RAM deferral is implemented; the historical worker assertions are reconciled.
   The pre-merge strict host/selected architecture/client evidence is complete;
   current merge verification is tracked at the top of this ledger. The user
   replaced fixed 10/14 GB build ceilings with OS-headroom/pressure monitoring
   on 2026-11-22; apply PRODUCT-MODEL.md rather than old execution budgets.
   Do not repeat obsolete OOM diagnosis, disable
   analyzers or hand-edit a generated client. The final full-solution build now
   passes; retain earlier time stops as history and the external Authentication
   failure as a separate incomplete broader gate.
   CS-ADR-034 resolves both user questions; the phase correction and initial-file
   behavior pass 12 real-worker controls. Function ABI, graphics, permitted
   undefined imports and broader runtime/CI/lint acceptance remain open. Do not turn partial completion into gate
   approval, install a second autosave or deploy the standalone IDE.
3. Retain UX03-A's verified local asset preparation and lazy acquisition port.
   Old canonical/served assets are backed up; both refreshed 4.5.2 manifests
   and all bundle hashes pass. In another environment, repeat the backed-up
   G-PREFLIGHT preparation rather than assuming dev/sync replaced schema 1.
   Boot/disposal evidence alone does not establish program/case conformance;
   the subsequent scoped stdio controls do not prove function/graphics support.
   Inspect actual changed-path classification for conditional CI provenance.
4. Recheck memory against the recorded budget before any full stack run.
   Preserve the existing developer environment; do not reset data or stop
   unrelated processes to make room.
   Use a local toolchain build only if compatible assets cannot be obtained
   or a separately approved runtime change genuinely requires one. No such
   need is established for the already published 4.5.2 assets.

Nao iniciar o programa abrangente `CODE-E2E-00` antes de `CODE-UX-06` nem
declarar qualidade de concorrencia apenas com a reescrita dos documentos.

### Subsequent product response - 2026-11-20

The user explicitly approved (1) the current prototype design and (2) the
initial simple-type function scope. These decisions are now recorded in
CS-ADR-015/022, 01a, the product model and CS-REF-18 disposition. No task-by-task
human test log, production acceptance or compound-runtime support was invented.
For (3), the user requested an explanation of the build; this is not approval
to modify CI, dispatch a workflow, deploy the IDE or publish packages. Only
documentation was updated; no build or new test run was necessary for these
recorded decisions. Its then-current artifact prerequisite was later corrected
by CS-ADR-024; it is not an instruction to wait for a compiler build.

### Consumer artifact planning correction - 2026-11-20

The user explicitly requested correction of the mandatory Emception build /
publication dependency. CS-ADR-024 records the replacement policy. Changes
are confined to the plans and shared handoffs; existing design/scalar-scope
approval, strict runtime checks, historical data and upstream ownership stay.

Read-only evidence gathered immediately before this update:

- npm metadata returned HTTP 200/version 4.5.2 for both public packages.
- Toolchain/Core CDN manifests returned HTTP 200/schema 2/version 4.5.2 and
  passed the current frozen version/ABI/lock/receipt/fingerprint check.
- Actual local canonical/web manifests are schema 1 and fail that check;
  other conventional local staged manifest locations were absent.
- Six classifier probes distinguish feature/docs consumers from runtime and
  lockfile changes; the 36 existing acquisition tests passed. No CI rule was
  changed, and required exact-source provenance is not bypassed.

This is manifest/policy evidence, not a full download, byte-level asset audit,
local refresh, browser execution, CODE-UX-01 closure or production acceptance.
The next executor follows the current action list above, not superseded
build-only instructions in historical records. No runtime/IDE, CI, app source,
database, dependency or developer-service change is part of this update.

Documentation Builder/Auditor pass (same agent, not independent product review):

| Check | Result | Scope |
| --- | --- | --- |
| installed Node read-only link/atomic-table/dependency audit | exit 0; 47 documents, 168 local links, 7 specification tables and 25 complete atomic rows | validates baseline/UX03 ownership and required safeguards, not runtime behavior |
| `python3 -B -m unittest discover -s scripts/ci/tests -p acquire_emception_release_test.py` | exit 0; 36 tests | existing conditional acquisition contract; duplicate-ZIP warnings are intentional negative fixtures |
| six direct `needs_source_release()` probes | exit 0; feature/domain/adapter/docs false, runtime and lockfile true | no classifier edit or new source-build requirement |
| `git diff --check -- docs/plans/coding-assessment-and-code-studio` and changed-path review | exit 0; only planning Markdown changed | preserves the pre-existing dirty plan changes; no app/runtime/IDE/CI edits |

The read-only audit uses filesystem APIs after sandbox child-process denial;
atomic-shape checks target specification tables, not historical result/checklist
tables. No production type/lint rule was relaxed and no unexecuted runtime,
full build or product gate was marked passed. No heavy test was needed for
this documentation-only correction.

## Checklist de checkpoint

Antes de interromper ou trocar de executor:

- [x] baseline de avaliacao registrada;
- [x] etapa e gate refletem o estado real;
- [x] arquivos alterados listados;
- [x] comandos e resultados registrados;
- [x] decisoes transversais registradas;
- [x] consumer map atualizado;
- [x] gaps e duplicacoes atualizados quando aplicavel;
- [x] bloqueios possuem evidencia;
- [x] proxima acao e executavel sem depender da conversa.
