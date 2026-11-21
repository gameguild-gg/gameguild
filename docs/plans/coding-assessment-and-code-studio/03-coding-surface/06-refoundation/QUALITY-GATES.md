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
