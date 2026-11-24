# Optional native program-start acknowledgement

Date: 2026-11-23. Decision: [CS-ADR-046](./DECISIONS.md).
Scope: explicitly authorized browser-SDK extension, not IDE refoundation.
Acceptance: consult the current [implementation ledger](./IMPLEMENTATION-STATE.md);
implementation or a unit pass alone does not close the native gate.

## Observed problem

The existing `onPhase('run')` announces the start of the WASI tool invocation.
That invocation still has to fetch its artifact from the VFS, compile the WASM
module and instantiate it before calling constructors and `main()`/`_start()`.
It is not an acknowledgement that the student's program has started.

The failed `runtime/exercise-check-DA2oLK/report.json` preserves a correct
standard-I/O solution. Its first case succeeds after roughly 1.1 seconds of
artifact loading and 656 ms of WASM compilation. Its second case reaches
`Compiling WASM module`, but the five-second student deadline expires before
the program-entry message. The report contains 816 timestamped console entries
with none omitted. Its resource guard records no memory PSI, OOM or forced stop.
This establishes premature budget attribution, not an incorrect expected
answer, broken scanf, or a reason to replace the fixture until it passes.

Ending the clock on the public `exit` event separately fixes charging completed
programs for scratch cleanup. It cannot correct an early start signal.
Neither defect justifies increasing the student's published execution limit.

## Minimal extension

1. Add optional `RunOptions.onProgramStart`, inherited by compile options.
2. Keep the callback on the parent side. Only callers supplying it request
   `wantProgramStart` on the existing run message; no function is serialized.
3. Forward an optional observer through the browser client/worker. The native
   WASI runner emits a correlated notification after module preparation and
   before explicit global constructors or program entry.
4. Route the notification only to that pending run, at most once. Unknown,
   duplicate and completed run IDs cannot start or renew another deadline.
5. Forward the observer for persisted-source builds and the facade's existing
   source-text pipeline without editing compiler presets or copying its build
   logic. Direct `run('wasi-run', ...)` can also opt in.
6. The owned preview keeps its existing bounded preparation budget until this
   acknowledgement, then applies the unchanged student limit. A returned run
   without acknowledgement is an infrastructure error, not a test verdict.

This observer describes the explicit native entry boundary after instantiation;
it does not promise a general CPU meter or delimit an arbitrary WASM start
section executed as part of instantiation. The exercised coding path compiles
C/C++ source with the existing fixed native presets. Other runtimes and
graphics do not gain new supported combinations through this observer.

## Why this preserves the IDE

- The observer is optional. Existing callers do not request or receive an
  additional wire notification and keep the same stage callbacks and results.
- No IDE/UI source, file explorer, Monaco, dock, terminal, canvas, preset,
  compiler flag, binary artifact, default timeout or output policy is changed.
- Shell/stdin routing uses the same run IDs and shared-buffer path. The new
  notification is not a terminal response and cannot resolve a pending run.
- Existing default constructor and entry behavior remains the same. The
  observer is invoked only when supplied; it is not another execution engine.
- No release/publication is needed for this local proof. Build only the local
  core/browser TypeScript outputs consumed by the existing web package. The
  frozen toolchain manifest and binary identities remain unchanged; evidence
  additionally binds the changed SDK source and built-JavaScript hashes.

This local-proof distinction does not waive the repository's conditional CI
exact-source producer policy for changes under `tools/emception/`. Keep that
existing PR/release verification unchanged; it is separate from rebuilding or
publishing a standalone IDE just to execute local package tests.

## Objective compatibility evidence

Contracts and strict type fixtures compiled before behavioral implementation.
The independent Node RED contained five new failures and four passing legacy/
negative controls. It exercises facade forwarding, real WASM instantiation,
constructors and correlated worker routing. No compiler or program is mocked
in the native constructor-order control; Node only loads the actual raw Python
asset as text and observes the real WebAssembly instantiation boundary.

The implemented focused suite passes all nine controls. Complete core SDK
regressions pass 276 tests. Browser SDK regressions pass 68 tests, with only
the pre-existing optional remote smoke disabled; that skip is not native
acceptance. All 27 tests in the unchanged Emception UI pass. The separate
owned preview RED proves the early clock and missing-start failures; all
18 focused runner controls then pass, including active-program cancellation,
output and cleanup budgets. The required six-case real browser proof remains
the closing oracle, with its outcome recorded in the ledger, not inferred here.

Strict SDK lint is not clean: seven errors and seven warnings already exist
in the touched upstream files. An unsuppressed comparison against `HEAD`
produces identical diagnostics and no new findings; new tests are lint-clean.
Do not present this as a passing strict upstream lint gate or repair unrelated
upstream declarations under this authorization. Their disposition remains in
the [part 05 register](../05-emception-stabilization/FINDINGS.md).

## Remaining boundaries

This extension does not itself migrate the Learning host, write official
results or change the standalone program/official grading budget consumers.
Parts 03/04 must prove their current timing/parity separately before claiming
complete execution/core grading. The [program-start finding](../05-emception-stabilization/FINDINGS.md)
retains that obligation; registering it for later regression cannot waive an
affected part 03/04 gate. No broader SDK repair is authorized by this document.
