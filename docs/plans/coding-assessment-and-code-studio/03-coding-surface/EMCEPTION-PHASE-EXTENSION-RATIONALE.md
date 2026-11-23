# Emception execution phase extension and IDE compatibility

Date: 2026-11-22. Audience: Emception maintainers and coding/grading reviewers.
Comparison baseline: `4a212cd7ff761dd0f69ebc5cfcf7f55acf45eb97`, before the
uncommitted extension approved in [CS-ADR-034](./DECISIONS.md).

## Conclusion

The extension fixes a reproduced false approval in Emception's shared test
engine without changing the IDE's terminal or canvas execution paths. No
introduced IDE regression was observed in the audited browser controls.
The change is generic execution metadata, not grading policy added to Emception.

This is a scoped compatibility conclusion, not a claim that the whole IDE is
bug-free. A shell-to-program input handoff defect failed with both the baseline
and modified SDK. Those failures remain recorded below and were not fixed,
suppressed or counted as successful product tests.

## The defect being corrected

`compileAndRun()` can stop during compilation, stop during linking, or reach
program execution. Previously, its public `ToolResult` discarded this distinction.
The result contained an exit code, output and timing, but not which operation
produced them. The lower-level browser pipeline already exposes `finalPhase`;
the higher-level facade lost information it already possessed.

The stdio test engine intentionally treats an omitted `expectedExit` as no
exit-code assertion. This permits valid exercises that compare output without
requiring a particular exit code. However, it also allowed a failed build with
empty stdout to satisfy an empty expected stdout. Requiring an expected exit
does not fully solve this: a linker failure with exit 1 is not a program that
successfully executed and deliberately returned 1.

The paired browser reproduction used the actual IDE's public runtime API:

```js
const path = '/home/user/phase-compatibility.cpp';
await api.workspace.writeFile(
  path,
  'int main(){return INVALID_ENGINE_COMPATIBILITY;}',
);
const report = await api.runTests({
  build: { toolchain: 'cpp', sources: [path] },
  cases: [{ kind: 'stdio', expectedStdout: '' }],
});
```

With the same IDE, browser and compiler assets, the baseline returned
`report.passed === 1`. The modified SDK returned `report.passed === 0` and
`Compilation failed (exit 1)` with the real compiler diagnostic. This was a
real WebAssembly compilation, not a mocked result. The earlier official-worker
audit also verified actual linker failures, empty-output controls and
intentionally expected nonzero program exits in its 12 passing controls.

## The minimum contract change

The public result gains one optional field:

```ts
phase?: 'compile' | 'link' | 'run';
```

It identifies the terminal pipeline stage. It does not replace `exitCode`,
`stdout`, `stderr`, `durationMs`, `timedOut` or `signal`. All existing field
types, optionality and `EmceptionAPI` method signatures remain unchanged.
The contract and old result-shape assignability pass the public `tsd` tests.

Optionality is appropriate because a raw `api.run()` executes an arbitrary
tool, not necessarily a compile/link/run pipeline. Existing providers that
omit the metadata remain valid generic `ToolResult` producers. The browser's
`compileAndRun()` knows its own control flow and now supplies the stage on
every returned outcome. It stops at the same points as before and uses the
same compiler, linker, argv, source order and run command.

`phase: 'run'` means that the pipeline reached the run operation. It does not
prove a correct answer, successful execution, implemented external imports,
or that `main()` actually performed the intended work. Output, exit and timeout
assertions still apply. In particular, an expected nonzero program exit remains
valid; a compilation/linking failure cannot impersonate that program result.

## Changes by responsibility

There are five changed production files under Emception and six test files.
No IDE implementation file changed.

| Production file | Necessary change |
| --- | --- |
| [Core result contract](../../../../tools/emception/packages/core/src/types.ts) | Adds optional, runtime-neutral stage metadata; existing methods and result fields stay intact. |
| [Result validator](../../../../tools/emception/packages/core/src/runtime/tool-result.ts) | Accepts omission and the three declared values; rejects malformed metadata rather than accepting an undocumented stage. |
| [Browser facade](../../../../tools/emception/packages/browser/src/createEmception.ts) | Preserves the known stage for inline and multi-file builds. Selects the actual failed linker result instead of the preceding successful compiler result. Rejects an impossible pipeline outcome without inventing execution. |
| [Existing test engine](../../../../tools/emception/packages/core/src/testing/engine.ts) | Prevents tagged compile/link outcomes from satisfying stdio, file-based stdio or doctest program assertions. Keeps existing timeout precedence and hidden-path redaction. |
| [Official grading boundary](../../../../tools/emception/grading/browser.ts) | Requires valid stage metadata for official compile-and-run execution. Missing/unknown metadata is unavailable infrastructure, not an invented passing receipt or a learner's zero score. |

The core and browser changes contain no course, assessment, rubric, score,
reviewer, tenant or release-policy implementation. They describe execution
facts and make the existing generic test engine respect them. Academic
interpretation remains outside Emception. The official worker's stricter
trust requirement does not become an IDE requirement.

## Why the alternatives are weaker

- **Always require exit zero:** this changes exercise semantics and rejects a
  program deliberately expected to return a nonzero value. It also confuses a
  tool's exit code with the student's program exit code.
- **Infer failure from stderr:** warnings and legitimate program diagnostics
  can use stderr. Diagnostic wording is not a stable machine-readable stage
  contract. No diagnostic-text heuristic was introduced.
- **Infer the stage in each host:** the high-level native facade calls its
  local `run` function; replacing the public `api.run` property is not a reliable
  interception point. Tool-name counting and private worker hooks would couple
  grading to SDK internals and become another fragile execution adapter.
- **Use only the existing low-level convenience pipeline:** its `finalPhase`
  is useful, but that pipeline accepts source text and writes its selected
  source path. It does not replace the higher-level facade's multi-source VFS
  build contract. Routing around the facade would risk compiling the wrong
  file or require copying its compiler orchestration into the host.
- **Create another test engine or a V2 result:** neither is needed to retain
  one fact already known by the SDK. Reusing the existing engine prevents the
  IDE preview and official execution from acquiring different comparators.

Fixing only the worker would leave the shared public engine's false approval
available to other consumers. Adding metadata without consuming it would leave
the defect intact. Requiring metadata without updating its producer would
make official grading unavailable. The producer, generic consumer and official
boundary therefore form one coordinated correction.

## Why normal IDE execution is preserved

The [IDE orchestration](../../../../tools/emception/packages/ide/src/components/Ide.tsx)
uses `runTool()` and `api.run()` for its native terminal path. The raw facade
`run` function is unchanged, confirmed by parsed-source comparison.
The IDE also keeps its own compile/link exit checks, terminal stdin reader,
status transitions and editor focus handling.

Graphics uses the separate public [Canvas API](../../../../tools/emception/packages/browser/src/canvas.ts).
Its build/start/stop implementation and runtime glue are unchanged. Its outer
build-result `phase` (`compile`, `link`, `ready`) belongs to a different result
type from the optional field on `ToolResult`. The IDE receives the same
CanvasSession or build-failure objects as before; no new field is spread into
the session returned by `api.canvas.start()`.

The retained [assessment session](../../../../packages/infrastructure/ui-emception/src/assessment/session.ts)
can call the public test engine. There, rejecting a failed build is an
intentional correctness improvement, including when invoked inside the IDE.
Compatibility does not mean preserving an incorrect test approval.

Source comparison against the baseline confirms no changes to the IDE,
`@game-guild/emception-ui`, presets/linker flags, canvas/runtime implementation,
worker protocol, tool runner or functional harness. The generated browser
workers also have equal normalized JavaScript AST hashes:

```text
47f95fc4d4fbe5783f5d94c3511472bb05599f8ead55a8a0b4a5e80a1745cdb1
```

That comparison removes comments and normalizes formatting. The raw bundle
hashes differ because the baseline was built from a separate directory; AST
equality is supplementary evidence, not a byte-identity or release-provenance
waiver. The IDE/browser execution tests below provide the behavioral proof.

## Compatibility validation

Both variants used the same unchanged public IDE component and demo App,
Chrome `157.0.8103.78`, React `19.2.8`, installed Monaco `0.57.0` and published
4.5.2 WASM assets. The host served Monaco locally through its public loader
configuration; it did not replace the editor with a mock. Network access was
limited to the local test origin. Each scenario used a fresh browser context.

| Browser observation | Baseline SDK | Modified SDK |
| --- | --- | --- |
| Real Monaco editing and switching between main/header tabs | Pass | Pass |
| Interactive shell command in an independent context | Pass | Pass |
| C++ compile/run, typed stdin, visible compiler failure and valid-run recovery | Pass | Pass |
| SDL3 build/start, changing nonblank WebGL pixels, editor input after graphics and Stop | Pass | Pass |
| Public test engine with invalid source and empty expected stdout | Incorrect approval reproduced | Correct rejection verified |
| Shell command followed by a program reading stdin in the same session | Fail: stale shell text reaches program input | Same failure reproduced |

These are 12 observations: eight normal compatibility controls passed, two
engine controls verified the intended correction, and two product failures
remain failures. No unhandled page errors or unexpected console errors occurred
in the accepted compatibility controls. All owned browser contexts closed with
zero remaining workers. Screenshots and canvas pixel observations are retained.

All 13 official SDK/IDE build, type and test commands exited zero. The suites
passed core 273, browser 47, webcomponent 7, IDE 26 and retained emception-ui 27
tests. The browser suite's one pre-existing opt-in smoke remains skipped;
the real-browser observations above are separate evidence, not a claim that
this skipped test ran. Core public type tests and browser/xterm/React/
webcomponent/IDE typechecks passed.

The 12 browser observations peaked at 5708 MiB owned RSS with at least 11045 MiB
available RAM, above the 4772 MiB OS reserve. Four CPUs were used; there were
no new kernel OOM events or resource-guard stops. These measurements are not
a performance benchmark or proof of a five-cycle memory plateau.

## Pre-existing input handoff defect

The failure reproduced in both SDK variants follows this sequence:

1. Type `echo SHELL_COMPATIBILITY` into the IDE shell.
2. Compile a C++ program that prints a prompt and reads one line with `fgets`.
3. Type `compatibility` after the program prompt.

Expected output is `IDE_GOT:compatibility`. Both variants instead produced
`IDE_GOT:echo SHELL_COMPATIBILITY`. The failure is consistent with the IDE's
terminal input buffer retaining bytes while the shell also handles input;
input ownership needs a separate investigation by the Emception maintainer.

It exists before this extension, while `api.run`, IDE terminal code and the
execution worker remain unchanged. The independent shell and stdin scenarios
were tested separately, and the combined failure was still rerun and retained
for both variants. No product fix or passing-test waiver was applied here.

## Compatibility limits and release requirements

The optional property preserves ordinary old result shapes. It is not a
guarantee about every external consumer: code requiring an exact JSON key set,
exhaustive mapped types, or a previously undocumented `phase` value needs
review. The validator now owns that field's three declared values when present.
The actual IDE consumers and package types were checked, rather than assuming
that an optional field can never affect anyone.

Official grading must bundle the updated facade, core engine and worker
together. An old SDK that omits phase is not sufficient for the stricter
official worker; the result must remain unavailable instead of silently passing.
This is not a new course JSON version, database schema or migration. Stored
attempts and receipts are not rewritten.

The `Initial file` decision is separate: coding's `entryPoint.path` selects the
initial editor source and may differ from the file containing `main()`.
Its serialized name and source ordering remain unchanged. The Emception IDE's
own workspace entry selection was not modified to impose that domain decision.

The extension does not fix functional-harness ABI, official graphics grading,
compound values, unresolved imports or per-exercise memory enforcement. For
example, the existing linker can permit an undefined import and return a run
result; phase metadata alone cannot establish that the import is implemented.
See the [runtime findings](./EMCEPTION-CAPABILITY-GAPS.md).

This audit exercised C++ terminal and SDL3 IDE workflows in Chrome, not every
browser, preset, custom extension or deployment. It does not close CODE-UX-02/03
or certify the full platform. Seven strict upstream lint warnings remain
identical to HEAD: six in the facade and one in the core type-test file. No new
diagnostic or suppression was introduced, but whole-file strict lint is not green.

Only JavaScript/TypeScript SDK builds and isolated test hosts were generated.
No compiler/WASM rebuild, IDE deployment, production publication, dev launcher,
platform module, migration or dependency change was made. The existing
exact-source CI/release requirements remain mandatory for these changed sources;
local reuse of verified compiler assets does not waive them.

## Evidence and reproduction

Local ignored evidence:

```text
apps/web/test-results/coding-surface-refoundation/ide/20261122-phase-compatibility/
  report.json                 paired matrix, including both failing input cases
  source-audit.json           public contract, preserved paths and source hashes
  gates.json                  exact package commands and exit codes
  asset-integrity.json        29 verified bundle hashes in each local asset tree
  *-report.json               individual browser observations
  *-console.json              captured synthetic diagnostics
  *-resources.json            OS/resource measurements
  *.png                      actual IDE and canvas screenshots
  repro.tgz                   diagnostic scripts and resource guard
```

The canonical and served manifest SHA-256 is
`f4b9f7944f4def6678cafa7895bb0f766255683e60b0abcd306196a01a581dd8`.
The reproduction archive SHA-256 is
`339520b52e99b058ed01f2f73f4d08ab352b3dc81db173b0dde85295d71460fb`.
These local artifacts are not committed or automatically available in another
checkout; attach them when sharing the full browser evidence. The archive
contains local paths: replay with fresh scratch/evidence directories, retaining
the recorded baseline and adapting those paths without overwriting prior runs.

Repeatable package checks, from the repository root with installed dependencies:

```sh
pnpm --filter emception run test:types
pnpm --filter emception test
pnpm --filter @gameguild/emception-xterm build
pnpm --filter @gameguild/emception-browser test
pnpm --filter @gameguild/emception-browser typecheck
pnpm --filter @gameguild/emception-xterm typecheck
pnpm --filter @gameguild/emception-react build
pnpm --filter @gameguild/emception-react typecheck
pnpm --filter @gameguild/emception-webcomponent test
pnpm --filter @gameguild/emception-webcomponent typecheck
pnpm --filter @gameguild/emception-ide test
pnpm --filter @gameguild/emception-ide typecheck
pnpm --filter @game-guild/emception-ui test
```

The permanent regression assertions are in the changed core result/engine/type
tests, browser facade tests and grading-worker tests. The separate official
worker evidence and remaining plan gates are in the
[implementation ledger](./IMPLEMENTATION-STATE.md).

## Addendum: optional phase-start notifications - 2026-11-23

The user separately authorized `CompileOptions.onPhase` after the two-group
C++ string control reproduced a 30-second timeout during linking, before any
student comparison. Terminal `ToolResult.phase` cannot tell a caller when to
start a timer: it arrives only after the operation finishes.

The existing low-level public helper already has `onPhase`, but it compiles
one source string. Replacing the multi-file facade with that helper would
lose translation-unit semantics or require another compiler orchestrator.
Parsing tool output or counting exit events would not be a stable contract.

The narrow addition is:

```ts
interface CompileOptions extends RunOptions {
  // Existing sources/build options remain unchanged.
  onPhase?: (phase: 'compile' | 'link' | 'run') => void;
}
```

The native facade calls this optional synchronous observer before the first
compilation, linking and program execution. Multiple source files still emit
one compilation start. The source-text path forwards the existing helper's
notifications, excluding its internal file-write stage. A failed stage emits
no later starts; a thrown observer stops before the next tool. The callback
is consumed locally by the facade, not serialized into a worker message.

This does not introduce grading knowledge into Emception. The SDK exposes
only generic lifecycle observation. The product-specific grading wrapper
owns the separate 120000-ms total preparation deadline and the frozen
exercise deadline starting at `run`. Preparation is never reset per source
or link phase. The time measure is execution-phase wall time, including
runtime startup, not a claim of instruction-only CPU accounting. Aggregate
output limits and the five-minute worker watchdog remain unchanged. A missing
or regressing notification is unavailable execution, never an invented pass
or a learner's zero grade. The current worker must bundle the matching facade.

### Why existing IDE consumers are preserved

- The option is absent by default; previous options remain assignable.
- Emitted command arguments, presets, flags, source bytes, terminal result
  metadata, toolchain artifacts and the public testing harness are unchanged.
- Existing callers do not acquire a new timer or grading policy. Only the
  owned grading wrapper chooses to use the lifecycle callback for budgeting.
- Canvas, raw `run`, the IDE component and `@game-guild/emception-ui` are not
  edited. An optional observer does not alter their layout or execution path.
- Local TypeScript/package/worker builds are consumer checks. No standalone
  IDE deployment, SDK publication or compiler rebuild is required here.

### Verification boundary

Strict type tests cover absent/valid callbacks and reject an invalid stage
observer. Ten independent facade tests cover both source-text and multi-file
paths, start-before-tool ordering, exactly-once compilation notification,
terminal failure and callback cancellation. Budget tests cover finite shared
preparation, fresh learner time, hung preparation/programs, phase violations,
output overflow and the fallback path's nested raw tool calls. Existing core,
browser, IDE, web component and retained UI suites are rerun independently.

The actual current-source worker passes the formerly failing C++ string
public/private control, all ten official scalar controls and three real
retained/finite/infinite controls. Full scalar parity, resource measurements,
exact commands and the seven pre-existing upstream lint warnings are recorded in
the implementation ledger. These checks do not close graphics execution,
unresolved imports inside a student's function, workbench integration or
human acceptance; no canonical capability status is promoted automatically.
