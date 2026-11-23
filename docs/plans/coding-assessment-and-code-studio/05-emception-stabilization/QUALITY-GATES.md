# Part 05 terminal quality gates

Status: command contract, not executed product results.
Inherit the [coordinator](../05-emception-stabilization-and-polish.md) and
[existing strict gates](../03-coding-surface/06-refoundation/QUALITY-GATES.md).
Commands run from the repository root. Use installed tools and existing configs;
missing planned cases, required skips, resource stops or nonzero exits keep the
gate open. Record exact selected IDs/counts and paths, not only a test total.

## G-POL-DOC and G-POL-BASELINE

```bash
git diff --check -- docs/plans/coding-assessment-and-code-studio
git rev-parse HEAD
git status --short --branch
node --test --test-concurrency=1 apps/web/scripts/coding-cycle-toolchain-support.test.mjs apps/web/scripts/coding-cycle-editor-support.test.mjs apps/web/scripts/coding-cycle-web-runtime.test.mjs
```

G-POL-DOC additionally parses Markdown links/anchors and atomic-cycle tables
using the already declared remark/GFM tooling. Audit numbering, local targets,
all finding IDs and links back from parts 03/04. Record the exact read-only audit
command/result. Whitespace alone is insufficient. No product build is needed
for a documentation-only update.

Baseline requires accepted part 04 evidence, inventory reconciliation and actual
artifact preflight as defined by existing G-PREFLIGHT. Lightweight helper tests
alone cannot prove runtime readiness. No mandatory new IDE publication/build
is introduced; preserve existing conditional exact-source CI requirements.

## G-POL-CONTRACT and G-POL-CONSUMERS

Use the existing G-CONTRACT/G-SURFACE and affected G-WEB commands. At minimum:

```bash
pnpm --filter @game-guild/coding typecheck
pnpm --filter @game-guild/grading-adapter-code typecheck
pnpm --filter @game-guild/coding-surface typecheck
pnpm --filter @game-guild/coding test --maxWorkers=1
pnpm --filter @game-guild/grading-adapter-code test --maxWorkers=1
pnpm --filter @game-guild/coding-surface test --maxWorkers=1
pnpm exec eslint --no-inline-config --max-warnings=0 packages/features/coding/src packages/features/grading-adapter-code/src packages/features/coding-surface/src
```

Contract-only types/schema fixtures precede behavior. Run the complete affected
groups after the repair; record exact focused host test/lint paths using the
existing web config. No `any`, unsafe cast, lint suppression or weakened rule.
The final applicable web typecheck/build and backend gates cannot be omitted
because a package subset passed.

## G-POL-NATIVE

```bash
pnpm --dir tools/emception exec tsc -p grading/tsconfig.json --pretty false
node --test --test-concurrency=1 tools/emception/grading/execution-budget.test.mjs tools/emception/grading/browser-budget.test.mjs tools/emception/grading/toolchain-binding.test.mjs tools/emception/grading/worker-diagnostics.test.mjs
GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL=chrome node apps/web/scripts/coding-surface-workbench-e2e.mjs
```

These existing commands are necessary, not sufficient for new findings. Before
implementation, add named real-artifact missing-target/undefined-import and
scalar/phase/budget controls to the existing workbench/official-worker harness,
with independently frozen expectations and exact reproducible commands in
state. Require both public preview and actual official worker; simulated
receipts or stale dist cannot pass. Missing new controls keep this gate open.

## G-POL-GRAPHICS and G-POL-STABILITY

```bash
GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL=chrome node apps/web/scripts/coding-surface-graphics-e2e.mjs
GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL=chrome node apps/web/scripts/coding-surface-program-e2e.mjs
GAMEGUILD_CODE_WORKER_BROWSER_CHANNEL=chrome node apps/web/scripts/coding-surface-workbench-e2e.mjs
```

Omit the channel variable if the already installed bundled Chromium is used.
Run sequentially with verified assets and the OS-headroom/CPU policy in 5.3.
Keep all graphics five-cycle pixel/input/channel/listener/worker/RSS assertions.
The current graphics listener test fails; documenting it does not pass it.

Extend the existing workbench harness with the specified five actual native
executions in one live consumer; freeze plateau oracles before repair. Its
current cold controls/offline editor cycles do not satisfy G-POL-STABILITY.
Record exact new case IDs and command selection, source/SDK/manifest hashes,
raw counters, OS pressure and proof that owned processes close.

## G-POL-UPSTREAM

Required for an authorized upstream repair and at final compatibility acceptance:

```bash
pnpm build:emception
pnpm test:emception
pnpm test:emception:types
pnpm test:emception:e2e --workers=1
pnpm --filter @game-guild/emception-ui test
pnpm --dir tools/emception exec eslint --no-inline-config --max-warnings=0 packages/browser/src/createEmception.ts packages/core/test-d/index.test-d.ts
node --test --test-concurrency=1 scripts/ci/tests/classify-release-changes.test.mjs scripts/ci/tests/pr-required-gate.test.mjs
```

Use the upstream package's existing strict type/lint configuration for every
changed upstream source, with no new suppressions. Preserve source-compatibility,
IDE/webcomponent/browser and existing demos. `build:emception` builds packages;
it is not permission to build/publish an unrelated IDE release or alter presets.
An authorized toolchain/glue change additionally needs the actual affected
artifact's normal build, integrity and CI provenance. No hand-edited bundle,
manifest or frozen identity. Record precise source/artifact commands before repair.

## G-POL-API and G-POL-CORE

G-POL-API is existing G-API when backend/wire/official semantics are affected,
including root `AGENTS.md` authorization/authentication/API security suites and
warning-clean solution build. Preserve all migrations and module direction.

Final core acceptance reruns the part 04 commands:

```bash
pnpm coding:validation:contracts
pnpm coding:validation:smoke
pnpm coding:validation:full
```

Those commands/catalog are planned deliverables of part 04, not asserted to
exist or pass at the time this plan is written. Their absence blocks part 05
entry/final acceptance. Require real official function/stdio review/release and
new-session learner results plus wrong/error/access negatives. Follow the
[grading agent protocol](../../../testing/quiz-grading/AGENT_PROTOCOL.md) when
shared grading/review/results are affected. Candidate reports never promote
canonical capability status automatically. Owner and human task acceptance are
additional gates, not inferred from terminal success.
