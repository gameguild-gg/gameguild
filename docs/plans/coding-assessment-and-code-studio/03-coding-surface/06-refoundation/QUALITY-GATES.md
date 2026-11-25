# Refoundation terminal quality gates

Status: command contract for part 3.6; not executed results.
All gates inherit [GOVERNANCE.md](./GOVERNANCE.md). Commands run from the
repository root unless a `--dir`/`--filter` supplies the working directory.
Use the installed Node/pnpm versions; do not replace tools or install a library
to bypass a failure. Run heavy groups sequentially under the recorded budget.

## Gate rules

- A group passes only when every applicable command returns 0 and required
  cases actually ran. Record case IDs/counts, skips, exit codes and log paths.
- A selected gate cannot pass with a missing planned test, empty selection,
  swallowed assertion, disabled compiler/linter rule or required skipped case.
- A contract gate must pass before implementing its behavioral consumers.
  Behavioral red evidence precedes the minimal implementation.
- The prototype, runtime and human layers have distinct outcomes. A passing
  prototype gate never closes a production `CS-REF-*` requirement.
- Capture the entry diff and compare the task delta. Existing dirty changes
  are not silently attributed to this task or reverted for a clean report.

## G-DOC: plan integrity

```bash
git diff --check -- docs/plans/coding-assessment-and-code-studio
```

Additionally inspect local Markdown links and require every atomic row to
resolve to a specification, contract, test oracle, implementation boundary and
command set below. Record the executed link/structure check in the ledger;
whitespace alone does not establish a coherent plan. No application build is
needed for a documentation-only change.

## G-PROT: typed prototype and behavioral evidence

The following files are implemented by
[01a](./01a-prototype-workflows-and-critical-review.md). Recorded results belong
in the shared ledger; file existence alone never passes a gate. Use declared
TypeScript, Node's test runner and Playwright; do not add a second app,
dependency or human scenario CLI.
The prototype tsconfig must enable strict checking and include every typed
presentation contract and fixture. Do not omit an inconvenient source file or
weaken the repository's applicable rules to make this isolated check pass.

```bash
pnpm exec tsc --project docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype/tsconfig.json --noEmit
pnpm exec eslint --no-inline-config --max-warnings=0 docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype
node --test --test-concurrency=1 docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype/contracts.test.mjs
node --test --test-concurrency=1 docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype/workflows.test.mjs
```

`G-PROT-contract` is the first three commands, covering types and contract
fixtures before behavior. `G-PROT-flow` is all four. The bounded harness owns
one browser and any loopback-only ephemeral fixture server, compiles typed
fixture code using declared TypeScript when needed, and always disposes them.
Keep the existing HTML entry usable; record its exact preview artifact/URL.

Tests select completed atomic slices explicitly; each selection must report
the required IDs and reject zero tests or missing cases. Intermediate slices
run their contract plus existing regression cases; final prototype approval
requires all `UXP-01` through `UXP-07` oracles with no required skips.
Outputs go to ignored `apps/web/test-results/coding-surface-refoundation/`.
The harness is design validation infrastructure, not an execution engine.

The implemented fixture uses classic JavaScript scripts with strict JSDoc
checking and TypeScript contracts, so the HTML also opens directly without a
server/build. All presentation/fixture sources are included in the strict
tsconfig. The harness uses the installed Playwright Chromium, or the existing
Chrome channel when that bundled executable is unavailable; it installs nothing.
Sandbox bootstrap/subprocess failures are not behavioral red or passing evidence.

Default selection runs all seven slices: 16 required browser oracles, plus the
separate Node contract suite. For an intermediate slice use
`CODING_PROTOTYPE_SLICES=UXP-01,UXP-02` on the workflow command. The selector
enforces every required ID in [the checklist](./prototype/REVIEW-CHECKLIST.md),
not merely one surviving case per slice. Never use intermediate selection to
claim the final gate passed. The bounded local final run uses CPU affinity
`0-9` on the observed 12-core machine, reserving two cores.
UXP-07 covers author file CRUD, tab closure/reopening, independent learner
buffers, read-only support, caret/scroll, keyboard escape, contrast and
desktop/mobile editor captures. The editor is a presentation specimen, not
evidence of Monaco or Emception integration.

## G-CONTRACT: coding and adapter contracts

```bash
pnpm --filter @game-guild/coding typecheck
pnpm --filter @game-guild/grading-adapter-code typecheck
pnpm --filter @game-guild/coding test --maxWorkers=1
pnpm --filter @game-guild/grading-adapter-code test --maxWorkers=1
pnpm exec eslint --no-inline-config --max-warnings=0 packages/features/coding/src packages/features/grading-adapter-code/src
```

Before implementing a contract consumer, run its applicable type/schema
subset and fixtures. The complete group plus affected backend parity is the
closing gate. New recovery, revision and private-projection cases cannot be
replaced by historical test counts.

## G-SURFACE: UI, architecture and local lifecycle

```bash
pnpm --filter @game-guild/coding-surface typecheck
pnpm --filter @game-guild/coding-surface test --maxWorkers=1
pnpm exec eslint --no-inline-config --max-warnings=0 packages/features/coding-surface/src
```

Extend existing `architecture.test.ts` to enforce the graph through production
imports/re-exports and relevant package declarations. Tests must exercise the
active row's state transitions, failures and privacy effects. Frozen markup or
generic snapshots alone are insufficient. During the contract stage, compile
the ports and fixtures before adding their behavior.

## G-WEB: host contracts and composition

```bash
pnpm --filter @game-guild/web exec vitest run src/lib/coding-assignment/coding-surface-bridge.test.ts --maxWorkers=1
pnpm --filter @game-guild/web exec vitest run src/components/learning/authoring/lesson-authoring-workspace.test.tsx --maxWorkers=1
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
```

Add every touched host test file to the recorded command selection. Run the
web workspace's ESLint against its exact changed source/test files with
`--no-inline-config --max-warnings=0`; use its existing configuration, not a
less strict substitute. Record explicit paths, not an unexpanded placeholder.
If the generated client changes, use its official generator, then run its
existing typecheck/build. Do not hand-edit it or suppress integration errors.

UX04-C/C2 additionally requires the current controlled host/document and both
authorized definition-route contracts. The retired console form tests are not
part of this selection; their 35 historical cases and replacement mapping are
explicit in [the consumer map](../CONSUMER-MAP.md#ux04-c-consumer-cutover-and-test-retirement---2026-11-24).
Run this focused closing group with one worker:

```bash
pnpm --filter @game-guild/web exec vitest run \
  src/components/learning/authoring/lesson-authoring-workspace.test.tsx \
  src/components/learning/authoring/lesson-authoring-workspace-picker.test.tsx \
  src/components/learning/authoring/coding-authoring-route-contracts.test.ts \
  src/components/learning/authoring/coding-authoring-route.test.ts \
  src/lib/coding-assignment/learning-authoring-document-contracts.test.ts \
  src/lib/coding-assignment/learning-authoring-document.test.ts \
  src/lib/coding-assignment/coding-publication.test.ts \
  src/lib/coding-assignment/learning-draft-persistence.test.ts \
  src/lib/coding-assignment/coding-surface-bridge.test.ts \
  src/components/learning/coding-activity-client.test.tsx \
  'src/app/[locale]/learn/courses/[slug]/activities/[activityId]/coding-activity.test.tsx' \
  src/components/learning/assessment-grading/assessment-grader.test.tsx \
  --maxWorkers=1
```

This collaborator-level gate cannot satisfy C3 actual-host persistence or human
acceptance. C3 replaces the historical script flow with the typed current-model
driver. Its isolated config retains strict checking and checks its Node tests;
it does not change or weaken the web configuration.

```bash
pnpm --filter @game-guild/web exec tsc --project scripts/coding-authoring-proof.tsconfig.json --noEmit --pretty false
node --test --test-concurrency=1 apps/web/scripts/coding-authoring-browser-contracts.test.mjs
E2E_RUN=1 node apps/web/scripts/coding-definition-authoring-browser-e2e.mjs
```

The last command requires a healthy local dev-fast stack. The existing
`scripts/e2e-stack.mjs` supervisor may start it for the proof; preserve existing
services and stop only test-owned processes afterward. Run heavy groups
sequentially, with the existing OS reserve and at least two CPUs free. The
driver checks every required case, real API roundtrips, no compiler/academic
requests, error observations and owned cleanup before writing a passing report.
Inspect its actual-host captures separately; no automated result is human
approval or official grading acceptance.

UX05-A3 reuses the existing activity E2E entry with a typed real-host driver.
The old optional clicks and runtime mocks no longer certify the refounded
solver. Its verifier requires all eight named cases, independent enrolled
Members, zero practice mutations/private leaks/surviving workers, no browser
errors and successful owned cleanup. CS-ADR-053 separates this proof from
UX05-B/C and interactive login/human approval.

```bash
pnpm --filter @game-guild/web exec tsc --project scripts/coding-learner-proof.tsconfig.json --noEmit --pretty false
node --test --test-concurrency=1 apps/web/scripts/coding-authoring-browser-contracts.test.mjs apps/web/scripts/coding-learner-browser-contracts.test.mjs
pnpm --filter @game-guild/web exec eslint --no-inline-config --max-warnings=0 --suppressions-location test-results/coding-surface-refoundation/empty-eslint-suppressions.json scripts/coding-host-environment.mjs scripts/coding-authoring-browser-contracts.mts scripts/coding-learner-browser-contracts.mts scripts/coding-learner-browser-contracts.test.mjs scripts/coding-learner-browser-proof.mts scripts/coding-activity-browser-e2e.mjs
E2E_RUN=1 node apps/web/scripts/coding-activity-browser-e2e.mjs
```

The lint suppression file must contain an empty object, not a copy of the
application's historical waivers. Full web strict types remain mandatory; an
isolated proof config cannot override their failure. The browser entry checks
the current frozen manifest before provisioning and accepts loopback origins
only. Test accounts are not deleted; owned content/course cleanup errors and
their IDs remain in the report. Never claim a clean reset after interruption.
Use one compiler/browser, supported dev-fast no-build only with verified API
provenance, and the same resource reserve. Stop test-owned processes afterward.

If the supervisor's API rebuild hits the OS reserve, preserve the interrupted
build as failed evidence. Its supported dev-fast no-build path may reuse a
successful local API binary only after verifying its informational commit and
an empty managed-source diff against the current checkout. A backend change
invalidates that proof and requires rebuilding the affected API before the
real-host test. Do not hide the build result or bypass a changed backend.

If the workspace's bulk-suppression ledger obstructs a scoped audit with stale
entries, do not add waivers or ignore unpruned status. Use the same existing
configuration with `--suppressions-location` pointing to an explicitly empty
JSON map in ignored evidence, and record that path. This exposes all active
rule diagnostics rather than applying historical per-finding exemptions.
Keep generated report/reproduction directories out of the application input
glob, but never exclude versioned source or the package's strict fixtures.

## G-API: owned backend and architecture

Apply when API/domain/contracts change; select focused contract tests first,
then run the affected projects and root `AGENTS.md` requirements to close.

```bash
dotnet test apps/api/tests/GameGuild.Learning.Assessments.CodeAdapter.UnitTests/GameGuild.Learning.Assessments.CodeAdapter.UnitTests.csproj -m:1
dotnet test apps/api/tests/GameGuild.Learning.Courses.UnitTests/GameGuild.Learning.Courses.UnitTests.csproj -m:1
dotnet test apps/api/tests/GameGuild.Learning.Assessments.UnitTests/GameGuild.Learning.Assessments.UnitTests.csproj -m:1
dotnet test apps/api/tests/GameGuild.Identity.Authorization.UnitTests/GameGuild.Identity.Authorization.UnitTests.csproj -m:1
dotnet test apps/api/tests/GameGuild.Identity.Authentication.UnitTests/GameGuild.Identity.Authentication.UnitTests.csproj -m:1
dotnet test apps/api/tests/GameGuild.API.UnitTests/GameGuild.API.UnitTests.csproj -m:1
dotnet build apps/api/GameGuild.sln --nologo -m:1 -p:TreatWarningsAsErrors=true
```

Real persistence/projection tests and any required migration upgrade tests are
additional to unit tests. Choose the affected existing integration project and
record exact fixtures/filters before implementation. An external failing suite
remains blocked with evidence; do not fix another owner's module opportunistically.

## G-PREFLIGHT and G-RUNTIME: verified execution

`G-PREFLIGHT-UNIT` is the lightweight command below. It belongs to the baseline
and does not compile a toolchain or prove local artifact/runtime readiness.

```bash
node --test --test-concurrency=1 apps/web/scripts/coding-cycle-toolchain-support.test.mjs apps/web/scripts/coding-cycle-editor-support.test.mjs apps/web/scripts/coding-cycle-web-runtime.test.mjs
```

`G-ARTIFACT-POLICY` preserves the existing conditional CI producer contract:

```bash
python3 -B -m unittest discover -s scripts/ci/tests -p acquire_emception_release_test.py
```

The classifier does not require a new source artifact merely for changes to
coding-surface, coding, grading-adapter-code or these plans. Its actual source
scope also includes dependency manifests/lockfiles and selected host/CI files;
inspect the complete changed-path set, not only the feature package. When the
classifier requires a producer, retain exact-source provenance and existing CI
verification. No CI rule is changed by this plan (`CS-ADR-024`).

### Asset preparation before UX03-A real execution

1. Record the current frozen binding and actual artifact directories. Compatible
   4.5.2 assets already exist in the published packages. On 2026-11-20 their
   schema-2 manifests passed `requireFrozenCodeToolchain()`; that observation
   is not a complete download, file-integrity check or successful execution.
2. Preserve existing generated release/CDN copies in an owned ignored backup
   before replacing anything. Do not overwrite an unowned release or delete
   historical binaries needed by existing snapshots. This preparation changes
   generated assets only, not tracked Emception/runtime/IDE sources.
3. Reuse the existing `sync:emception` published-package path. Its legacy
   compatibility branch may retain a schema-1 source, and `dev:fast` skips
   synchronization when the web manifest exists. A refresh must therefore
   explicitly preserve/move aside the obsolete development copy first; merely
   rerunning dev or sync is not evidence that it refreshed. Record the source
   package/version, registry archive integrity and source/served asset locations.

```bash
pnpm --filter @game-guild/web run sync:emception
```

4. Reject a fallback to 4.4.0 for the current 4.5.2 grading binding. Run the
   actual checks below against the synchronized files, then the bounded runtime
   tests. Never repair the manifest by hand or change the frozen binding to
   make obsolete assets pass.

`G-PREFLIGHT` requires G-PREFLIGHT-UNIT plus this actual local check in
`CODE-UX-03`, before execution. Neither a public manifest check nor a successful
sync message substitutes for these local checks or the later browser gate.

```bash
node --input-type=module -e 'import { verifyCodingCycleToolchain } from "./apps/web/scripts/coding-cycle-toolchain-support.mjs"; for (const path of ["./tools/emception/artifacts/toolchain/release/cdn/manifest.json", "./apps/web/public/emception/manifest.json"]) await verifyCodingCycleToolchain(path);'
```

A local toolchain build is only a fallback if compatible artifacts cannot be
obtained or an explicitly required runtime change needs source validation.
Use the existing documented build/verification path and measured resource
budget; do not add it to every dev startup. No local build is currently required
to obtain 4.5.2. Do not dispatch `emception.yml` or publish the independent IDE
to prepare our dev environment; its manual entry has unrelated deployment
effects. The former build-only CI proposal is withdrawn by `CS-ADR-024`.

`G-RUNTIME` adds the existing browser cycle, only after resource/preflight
prerequisites pass:

```bash
pnpm --filter @game-guild/web test:browser:coding-cycle
```

Extend that harness with the active `CS-REF-*` browser oracles, using one worker
and existing public APIs. Its old tests do not automatically cover the new
terminal, graphics, recovery, reference-validation or submission behaviors.
Before each slice, record the exact selected cases/command and required new
assertions. Do not run its historical 12 GiB build on a pressured host; use the
existing supported stack path under the recorded budget or keep this gate
blocked. Never weaken compilation, provenance or isolation to force a run.

### Focused public graphics-port proof

UX03-B also has an isolated companion using the installed public SDK exports,
the strictly typed surface calibration fixture and the actual local assets:

```bash
GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL=chrome node apps/web/scripts/coding-surface-graphics-e2e.mjs
```

Omit the channel variable when using the already installed bundled Chromium.
Run sequentially with the recorded OS-headroom guard and CPU affinity, never
alongside a full application build. It builds only a small browser consumer,
owns a loopback-only ephemeral server and one sandboxed browser, and writes
fresh ignored evidence under `coding-surface-refoundation/runtime/graphics-*`.
There are no accounts, course writes, fixture preparation CLI or Next startup.

This proves the local graphics port, not production authoring, persona flows,
the interactive text terminal or official graphics grading. It keeps independent
pixel/animation/input/resize, output-channel, worker-disposal, listener and
post-cleanup browser-RSS oracles. RSS samples explicitly use browser GC and
retain the original measurements; missing metrics cannot count as zero usage.
The current five-cycle report fails listener cleanup (EMC-GAP-003); preserve
that nonzero result until the root cause is resolved. No required skip, relaxed
128 MiB plateau, hidden orphan listener or canonical promotion is allowed.

### Focused public text-port proof

```bash
GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL=chrome node apps/web/scripts/coding-surface-program-e2e.mjs
```

Apply the same OS-headroom/CPU policy and verified assets as the graphical
proof, without concurrent heavy builds. The command writes fresh `program-*`
evidence and exits nonzero on a missing input/EOF/channel/compiler/timeout or
worker-disposal oracle. One browser is used with one cold context/compiler at
a time; each control fully closes before the next begins. Explicit collection
avoids carrying collectible prior compiler buffers into the next cold control.

The six required controls are finite batch, actual user input after a native
prompt, empty batch/EOF, cancellation while awaiting input, compile failure and
student-program timeout. The report binds owned source, built public SDK,
harness and manifest hashes. A bridge's prefetch request alone is not a prompt
or proof that the program received input. This is a local public-port proof,
not a shell, production xterm/Monaco UI, academic verdict, persona E2E or the
separate five-cycle same-context plateau. Keep those acceptance layers open.

### Focused production workbench proof

```bash
GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL=chrome node apps/web/scripts/coding-surface-workbench-e2e.mjs
```

Apply the same verified-artifact, OS-headroom and reserved-CPU policy. The
fixture mounts the actual owned `CodingSurface`, installed Monaco and xterm,
with a local Monaco loader. It does not mock a compiler or create an API stack,
accounts or courses. Expectation timing admits the executor's bounded cold
preparation; the actual student-program timer is not extended.

Require five offline edit/capture/unmount cycles without compiler assets or
workers, desktop/mobile framing, native interactive I/O and separate channels,
stop acknowledgement, replacement of an executing context, correct/wrong
standard-I/O and native function tests, visible compilation diagnostics,
stale-result marking and zero retained compiler workers. Keep exact current
source/composer/SDK/manifest/harness hashes and fresh `workbench-*` evidence.
For the five offline editor cycles, collect GC/DOM metrics and bound warmed
heap growth to 8 MiB and DOM growth to 100 nodes. Preserve the raw listener
counts too; these presentation bounds do not replace native execution stress.

This is package UI evidence, not Next-host/publication/persona/release E2E.
The offline editor's measured plateau is not a five-run compiler-memory
plateau. Graphics, all supported runtime combinations, official grading and
human acceptance keep their independent required oracles; no canonical status
is automatically promoted by this focused report.

### Focused exercise preparation and redacted preview proof

The scoped CS-ADR-046 optional native start extension additionally requires:

```bash
pnpm --filter emception test:types
pnpm --filter @gameguild/emception-browser build
node --test --test-concurrency=1 tools/emception/packages/core/tests/program-start.test.mjs tools/emception/packages/browser/tests/program-start.test.mjs
node --test --test-concurrency=1 tools/emception/packages/core/tests/*.test.mjs
node --test --test-concurrency=1 tools/emception/packages/browser/tests/*.test.mjs
pnpm --filter @game-guild/emception-ui test
```

These are local SDK TypeScript builds, not compiler/artifact or IDE builds.
Prove opt-in/once-only run correlation, unchanged default messages/results,
actual WASM constructor/start ordering, failed-preparation rejection and owned
preparation/program/cleanup budget separation. Missing program-start evidence
must fail closed. Record pre-existing optional remote-smoke skips explicitly;
they do not replace the real browser proof below. Unsuppressed upstream lint
debt stays nonpassing, even when an exact HEAD comparison proves no regression.

```bash
GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL=chrome node apps/web/scripts/coding-surface-exercise-validation-e2e.mjs
```

Apply the existing verified-local-artifact, OS-headroom and reserved-CPU policy;
run no other heavy build/compiler alongside it. The command uses actual owned
teacher/learner components, installed local Monaco and the public runtime.
It creates no accounts, API stack or official attempts. It writes fresh
`runtime/exercise-check-*` reports with owned/SDK/composer/manifest/harness
hashes. No Emception source build or independent IDE publication is required for these
unchanged compatible local assets.
Use one browser at a time, with a cold browser per exercise family and streamed
assets, to avoid carrying prior compiler/bundle buffers into the next family.
This deliberately does not establish a same-context compiler-memory plateau.
The owned fixture builder runs in a separate child and exits before acquiring
the browser; its Vite graph must not stay resident in the proof process.
The four focused builder controls must pass:

```bash
node --test --test-concurrency=1 apps/web/scripts/coding-surface-fixture-build.test.mjs
```

Require all six named controls: function, standard I/O, redacted preview,
compilation failure, cancel/drain and layout. In each family the reference
passes both public/private cases and the deliberately wrong solution fails
them; neither modifies starter/source metadata. Draft edits stale the result.
Preview shows the non-default current structured problem, runs only the public
case through the same solver, contains no private canary in DOM/editor models
and resets scratch on reopening with zero browser storage or academic calls.
Compilation failure cannot become `incorrect-detected`. Opening preview while
checking waits for actual worker closure. Observe at most one compiler and
zero retained workers, with desktop/mobile screenshots and inspected framing.

For diagnosis only, `CODING_EXERCISE_FAMILY=function` or `standard-io` selects
one family and records `partialSelection: true`. Unknown selectors fail.
Passing a partial report never closes the required default six-case gate.
Retain bounded timestamped browser-console evidence, its omitted-entry count,
resource report and failed screenshot for investigation. Console text is an
observation, not a production program-start signal or a substitute oracle.

Keep failure reports and the exact focus oracle. React StrictMode and real
Monaco readiness need their own controls; mocks cannot certify those behaviors.
This is local package evidence for `CS-REF-05/19`, not host save/reopen,
authorized persona/publication/release E2E, broader runtime stress or human
acceptance. Those gates remain independently required.

## G-AUTHORING: actual offline instructor task views

```bash
GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL=chrome node apps/web/scripts/coding-surface-authoring-e2e.mjs
```

Use the existing OS-headroom guard and reserved CPU policy. This fixture mounts
the real `CodingAuthoringSurface` and installed local Monaco without an API,
learner account, compiler asset or SDK boot. Require the exact function,
standard-I/O, keyboard, layout and `UX04-B-preview-ui` case IDs, preserved draft values across tasks,
typed/exact-output edits, duplicate/delete cancellation, focus restoration and
desktop/wide/mobile captures. Editing must create no compiler workers/requests
or author browser-storage entries. Missing cases and page exceptions fail.
The added preview UI control opens/edits/closes scratch with real Monaco in
both families, checking return focus without executing any code. It cannot
replace the separate runtime proof above.

The harness waits for the exact returned authoring context, not a tab shared
with the previous mount. Keep source/harness hashes and earlier failed reports.
This is package task-view evidence, not reference/wrong execution, host save/
reopen, SSR, official grading or human acceptance. Those stay with UX04-B/C and
the later complete phase gates. No manifest/toolchain build is needed merely
to edit fields; conditional source-producer CI requirements remain unchanged.

## G-REGRESSION and G-FINAL

```bash
pnpm --filter @game-guild/emception-ui test
node --test --test-concurrency=1 scripts/ci/tests/classify-release-changes.test.mjs scripts/ci/tests/pr-required-gate.test.mjs
git diff --check
```

The preserved Emception UI package already runs `jest --runInBand`. Do not
append `--maxWorkers=1`: Jest rejects combining those two scheduling options.
Its declared script is sequential and runs the same complete required suite.

`G-REGRESSION` also requires an Auditor check of the entry/task delta for
preserved upstream/platform paths and forbidden imports or suppressions.
An empty current diff alone does not prove that an earlier commit was safe.

`G-FINAL` is the applicable union of G-CONTRACT, G-SURFACE, G-WEB, G-API,
G-PREFLIGHT, G-RUNTIME and G-REGRESSION, plus the repository web build under
bounded concurrency:

```bash
pnpm run build:web
```

Reserve two CPU cores using the existing supported build controls; record the
actual command/environment and measurements before this expensive check.
Do not run a heavy build for every small prototype/contract edit. If a command
is inapplicable, record the ownership-based reason before running the slice;
inapplicable is not passed. Required missing evidence remains open.

## Human quality gate

Run the scripted instructor and learner tasks in 01a, then again in the real
application at `CODE-UX-06`. Record task success, wrong turns, recovery,
confusion and blocked actions, with inspected captures and reviewer identity.
Every high-priority design finding needs resolution or explicit product
disposition; no automatic approval from duration, test count or agent opinion.

Terminal checks enforce reproducible behavior. Human acceptance judges whether
the experience is understandable and practical. Both are required for the
final UX gate; the ledger must never merge these into one unexplained green.
