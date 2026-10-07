# Code assessment prerequisite for PR #699

The owner explicitly requires correction of the Code assessment flow before merging
PR #699. The earlier merge exception for PR #697 does not apply. PR #699 remains a
draft until this complete flow is accepted.

## Acceptance sequence

1. Register a versioned Code adapter through the existing assessment execution
   contracts. Validate authoring and response files, materialize an immutable private
   projection, and expose only public material in the learner delivery.
2. Publish Code content through revision preparation and official capability
   validation. Attempts bind to the published revision; later authoring changes must
   not alter an existing attempt.
3. Execute the frozen public and private test plan in a trusted server worker using
   the existing Emception WebAssembly toolchain. Bound input, output, runtime and
   concurrent execution. Infrastructure errors do not award a grade. Learner supplied
   scores or test reports are rejected.
4. Move the learner and instructor consumers to the existing official submission,
   review and release endpoints. Preserve submission restoration and instructor
   feedback. Public test execution in the learner IDE remains a preview.
5. Verify focused contracts, PostgreSQL HTTP ownership/revision/privacy/finalization
   cases and the actual instructor/learner browser cycle. Keep all failed runs and
   their source identities. Merge only after the coherent revision passes required
   checks and primary work preservation is verified.

The generic graded-submission guard, review methods and existing browser assertions
are acceptance requirements and remain enforced.

## Runtime configuration

Code uses the existing official grading transaction and requires a trusted worker.
The API does not accept browser test reports or fall back to client scores when
the worker is unavailable. Prepare/publish can validate the executable contract;
operational acceptance additionally requires a configured worker and the real cycle.

Build its browser runtime from the same checkout with
`node grading/build.mjs` in `tools/emception`. Retain that checkout's grading
scripts and dependencies alongside its generated `artifacts/grading/runtime`.
Install the matching Playwright Chromium and use an unprivileged account capable
of running its sandbox. Supply these API configuration values as absolute paths:

| Configuration | Required value |
| --- | --- |
| `CodeGradingWorker:NodeExecutable` | Installed Node executable |
| `CodeGradingWorker:ScriptPath` | `tools/emception/grading/worker.mjs` |
| `CodeGradingWorker:RuntimeDirectory` | Built `tools/emception/artifacts/grading/runtime` |
| `CodeGradingWorker:CdnDirectory` | Verified Emception release CDN containing `manifest.json` |
| `CodeGradingWorker:BrowserDirectory` | Optional Playwright browser installation directory |
| `CodeGradingWorker:DeadlineSeconds` | 1–300; default 300 |

Version 1 freezes artifact version `4.4.0`, ABI `emception-browser-v1` and
canonical lock hash
`bb4e8ca4a8cc4640ec8f7f1d2f7dc14829992e44ff0309527a44b8a83d0b14ed`.
The worker requires a schema-v2 manifest with matching identity and valid release
receipt/fingerprint metadata. Retain these artifacts for existing attempts. A
compiler update requires another adapter version. Legacy local manifests cannot
be relabeled to satisfy this requirement.

Each API worker admits one active execution, with a five-second slot wait. It
limits request bytes, file/test counts, pipe output and browser console output;
the owned process is terminated on cancellation or deadline. Node and Chromium
JavaScript heaps are bounded, but this is not a guarantee of a 512 MB total
process or WebAssembly memory ceiling. The enclosing deployment must provide its
normal OS/container resource limits. Compilation and student execution stay in
the browser virtual filesystem; the process receives a credential-free environment,
and its browser can reach only the local static runtime/CDN server.

The Code submit transport waits up to 330 seconds for the bounded server result.
Failed execution rolls back the academic response, grade and command receipt.
The native browser driver builds/configures this same worker automatically. This
does not configure an existing production API deployment.

The browser grade check still requires exactly 100 points. It uses the existing
`scoreValueFromPoints` helper to compare the API's hundredths (10,000 units), as
the rubric and the official review endpoints do. Treating 100 wire units as 100
points would accept only one point and contradict the academic value contract.
No acceptance assertion is removed or treated as an expected failure.

## Evidence checkpoints

- Published `11186bd6d3aaf241dbf38276661a11bf49adc1ab` passed PR Verify
  run `37534149942` with 5,959 main API cases and 15 separate OpenAPI cases.
  These are prior-source evidence, not acceptance of this Code increment.
- Native Emception run `37534149877` finished on 2026-10-06: toolchain build,
  receipt checks, packaging and cache save passed; the published Code consumer
  had eight failing assertions in its 24-assertion cycle. This failed run is retained.
- The initial repaired consumer suite passed 47 cases in four files; four
  additional action tests verified the enrollment/idempotency route and Code
  transport deadline. The complete initial assessment suite passed 424 cases.
- Six PostgreSQL HTTP cases passed with private-data redaction, frozen content
  and rubric, authorized/idempotent review, criterion-score restoration, release,
  malicious response rejection and unavailable-worker rollback.
- A local legacy-WASM diagnostic returned the expected `[true,true,false]` for
  two public cases and a deliberately failing private case. It predates the
  verified manifest requirement and is not acceptance of the pinned release.
  Contract/worker failures preceding that diagnostic are retained as well.

Fresh coherent-source tests and the real pinned-release browser cycle remain
required before merge. All local receipts, raw logs, TRX files and native
diagnostics are retained under `artifacts/test-results/issue-263-refresh-lifecycle-20261005`
(the downloaded native report also exists under `artifacts/code11186native`).

The coherent local assessment run passed 426 cases, focused PostgreSQL HTTP 21,
Authentication 2,316 and Authorization 1,667; full Web and generated-client type
checks, generated-client build and all 51 focused consumer/action tests passed.
The full solution built with warnings treated as errors (zero warnings/errors).

The local complete API-unit repetition timed out before its cleanup worker
created the initial-delay timer. Its failed testhost was explicitly identified
and interrupted after 1,006 received results (1,005 passed/one failed), and the
complete run remains unaccepted. All 20 cleanup worker cases passed separately
on the same compiled source. This points to competing test initialization;
the exact cause is not certified by the isolated pass. The class now uses the
existing nonparallel lifecycle infrastructure collection, keeping every deadline
and shutdown assertion unchanged. A fresh complete API-unit run and native CI
are required for acceptance; the failed/partial run is retained.

The corrected source rebuilt with zero warnings/errors. Its 20 cleanup worker
cases passed again. A fresh complete API-unit repetition uses an explicitly owned
loopback PostgreSQL instance and remains pending at publication preparation.
Source receipts confirm that the collection annotation is the only API file
change since the 426/21/2,316/1,667 passing runs; production and those suites'
sources are unchanged. Those results do not replace the complete API-unit run.

The native driver syntax and actual shared score helper were also checked with
Node 22.23.3, matching the CI's Node 22 major. The helper returned 10,000 units
for 100 points. The Emception compiler cache inputs remain unchanged. Publication
preservation verified all 55 primary dirty files, without byte/status changes or
overlap, with four local branches, three worktrees and no stashes. Publication
receipts and the source bridge are dated 2026-10-07 UTC under the same artifact
directory. These preparation checks do not accept the pinned browser cycle.
