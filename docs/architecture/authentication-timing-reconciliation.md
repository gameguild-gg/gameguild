# Authentication timing reconciliation — #288

## Scope and status

Issue [#288](https://github.com/gameguild-gg/gameguild/issues/288) remains **OPEN**.
The current slice repairs total elapsed-work accounting and missing credential
work. It has been applied to the existing feature checkout after the accepted
[PR #699](https://github.com/gameguild-gg/gameguild/pull/699) merge to `develop`
at `d5f417328e9ff9d3fa0517796a623e310aeb1ef0`.

The original title/body and historical discussion remain authoritative. The
2026-10-04 source audit identified a timing origin created after account/password
work. The additional checks below come from the actual API contract and repository
security invariants; they do not manufacture an original acceptance checklist.
This slice preserves the current sign-up response contract. The separate #287
generic admission and sign-up decision remain outstanding.

## Latest verified local status — 2026-10-08

- PR #699 is merged; #263 is accepted and officially closed. This #288 increment
  has passed local validation and is being published from the existing branch.
- The solution builds with zero warnings/errors. Complete API Unit **1,082/1,082**
  and Integration **424/424** passed, raw `Completed`, no failures/skips, verified Unit theory expansion and exact Integration
  discovered-name coverage, unchanged actual source and verified owned cleanup.
- All eleven component/HTTP selections passed and final cleanup was independently
  reconciled. Authentication 2,371, Authorization 1,667, OpenAPI 15 at its unchanged
  4 GiB limit, and all eight ownership regressions passed. Selected counts overlap.
- Actual Kestrel observations cover configured cost 10, controlled low risk,
  uncontrolled ambient load, active gates, and exactly one verification/dummy
  per request. They do not certify all hash profiles or production distributions.
- Matching-head native/security and remaining supported hash/profile timing
  acceptance are pending. **#288 and #287 remain OPEN.** No waiver is requested.
- All 328 scoped IDs and original criteria are preserved. The 55 dirty primary
  files remain byte/status identical. This work created no branch, worktree or stash.

Historical executions and their limits are retained below.

## Implemented behavior

| Boundary | Change | Regression evidence |
|---|---|---|
| Local account lookup | Create a server-owned monotonic origin before lookup; subtract total elapsed work from the existing 400 ms floor. | `TimingOriginBaselineTests`, `MonotonicTimingTests` |
| Polymorphic resolution | Propagate the origin created before candidate lookup in an internal, non-JSON-bindable request property. | Origin baseline, JSON boundary test, actual polymorphic HTTP cases |
| Account/IP and advisory-lock admission | Reuse the server origin through a private HTTP context key. All three pre-provider denials complete actual dummy work and the same generic unauthorized response/floor without bypassing lockouts. | `RequestTimingOriginTests`, real hashing admission tests, passed held-lock/concurrent PostgreSQL/Kestrel cases and independently reconciled complete local sequence |
| Credential verification | Report actual completed BCrypt/PBKDF2 work independently of credential validity or account existence. Preserve the boolean verification API. | `CredentialWorkTests` and actual HTTP credential classes |
| Missing/unusable credentials | Perform real dummy BCrypt for nonexistent/passwordless accounts, malformed records and rejected oversized legacy inputs. | Work baseline and HTTP observations |
| Password policy | Resolve the same current BCrypt policy for writes and dummy work, preserving all three existing configuration paths and precedence. | Precedence/reload tests and completed cost observations |
| Request cancellation | Check cancellation before/after verification, cancel remaining delay, and propagate request cancellation instead of producing a generic credential failure. | Monotonic, lookup, real verification and compensation cancellation tests |
| Failure handling | Preserve attempt/risk/audit handling; an unavailable attempt store after a lookup failure cannot skip timing compensation. Attempt compensation once. | `AuthenticationTimingBoundaryTests` |
| Legacy simulation | Give either account class the same actual dummy work and shared floor; remove existence-dependent random delays. Retain the existing signature. | Two-class baseline, regression and cancellation tests |
| Opaque custom providers | Retain their signatures and never assume they completed costly work. Use conservative compensation. | Legacy provider boundary test |

`PasswordVerificationResult.WorkPerformed` means a derivation/verification actually
completed. Parsing an unusable record, rejecting an empty password or rejecting
an oversized legacy BCrypt input does not set it. A dummy configuration/hash
failure is not replaced with a sleep or represented as completed protection.
Synchronous BCrypt/PBKDF2 already running cannot be interrupted mid-operation.

## Executed evidence

Evidence is retained under
`artifacts/test-results/issue-263-refresh-lifecycle-20261005/`.
Artifacts identify the source bytes and execution revision; the implementation
was still uncommitted during these local checks.

| Execution | Result | Retained receipt |
|---|---|---|
| Prepared exact 14-file bundle plus complete original Authentication suite | 2,349 passed; zero failed/skipped | `issue-288-staged-credential-work-20261007/repository-timestamp-application-bundle.json` |
| Actual legacy simulation baseline | One failure / one control passed | `issue-288-actual-legacy-baseline-01/result.json` |
| Added boundary-test compilation | Failed because the new test file lacked its Xunit import; corrected before execution. Not a product baseline. | `issue-288-actual-authentication-focused-before-boundary-fix/result.json` |
| Actual focused defect baseline | 47 passed / two failed: skipped simulation hashing and dual lookup/attempt-store failure. | `issue-288-actual-authentication-focused-before-boundary-fixed-test-import/result.json` |
| Actual focused regression after correction | 51 passed; zero failed/skipped | `issue-288-actual-authentication-focused-after-boundary-fix/result.json` |
| Actual complete solution build, warnings as errors | Zero warnings/errors | `issue-288-actual-build-01/result.json` |
| Complete actual Authentication suite after application | 2,364 passed; zero failed/skipped | `issue-288-actual-authentication-accepted-source01/result.json` |
| Complete actual Authorization suite after application | 1,667 passed; zero failed/skipped | `issue-288-actual-authorization-accepted-source01/result.json` |
| Actual API Unit first execution | Interrupted after four confirmed Docker/Testcontainers startup/removal failures; 1,017 completed tests passed and four failed. This is not a complete-suite pass. Original log and TRX are preserved. | `issue-288-actual-api-unit-accepted-source01/result.json` |
| API Unit repeat using the repository's isolated PostgreSQL mode | 1,031 completed tests passed, zero completed tests failed; explicitly aborted to repair the separately observed concurrent HTTP admission defect. This is not a complete-suite pass. | `issue-288-actual-api-unit-isolated-gate01/result.json` |
| Applied credential HTTP, admission and actual Kestrel network controls | Prepared; not yet executed. The real-socket harness includes sequential and two-request concurrent observations. | `issue-288-network-controls-20261007/preparation.json` |
| First actual Kestrel harness execution | Two test failures before HTTP observations: the explicit client options retained default port 80 instead of the verified dynamic listener. Corrected client BaseAddress; listener/port checks are retained. | `issue-288-actual-network-controls-ambient-load01/result.json` |
| Second Kestrel collection attempt | PostgreSQL readiness Docker CLI call timed out at the existing 10-second limit. No HTTP test execution occurred. Owned container cleanup was verified; the failed setup receipt is retained. | `issue-288-owned-postgres-network-controls-ambient-load02.json` |
| Third actual Kestrel execution | Sequential case passed; concurrent case failed because lockout admission returned a different credential error before the timing boundary. Actual product defect; both results are retained. | `issue-288-actual-network-controls-ambient-load03/result.json` |
| Admission repair build at 4 GiB managed heap cap | Failed: Roslyn terminated with `OutOfMemoryException` while compiling the API. No source change during execution. Original failed log is retained; analyzers were not disabled. | `issue-288-actual-build-admission-repair01/result.json` |
| Complete solution build after admission repair | Zero warnings/errors; source unchanged. The owned build used an 8 GiB managed heap cap after inspecting available memory; no machine/paging or unrelated-process changes. | `issue-288-actual-build-admission-repair02/result.json` |
| Actual focused authentication regression after admission repair | 56 passed; zero failed/skipped, including all four private HTTP origin tests. | `issue-288-actual-authentication-focused-admission-repair02/result.json` |
| Actual account/IP admission regression | Nine passed; zero failed/skipped. Real dummy work, canonical generic denial, shared floor and request cancellation verified. | `issue-288-actual-api-admission-focused-admission-repair02/result.json` |
| Complete actual Authentication suite after admission repair | 2,368 passed; zero failed/skipped. | `issue-288-actual-authentication-admission-repair02/result.json` |
| Complete actual Authorization suite after admission repair | 1,667 passed; zero failed/skipped. | `issue-288-actual-authorization-admission-repair02/result.json` |
| Actual API Unit architecture/security suites after admission repair | 125 passed; zero failed/skipped. These are the selected architecture/security suites, not the whole API Unit project. | `issue-288-actual-api-architecture-security-admission-repair02/result.json` |
| Actual PostgreSQL focused suite after admission repair | 78 passed / one failed among 79. All three deterministic Kestrel held-lock cases passed. A direct concurrency case measured 399.6326 ms, below the unchanged 400 ms floor. The complete run failed. | `issue-288-actual-api-focused-admission-repair02/result.json` |
| Deterministic precision/cancellation baseline | Three failed / one control passed: fractional timer remainder, early wake-up, and a lookup returning a known account after cancellation. | `issue-288-actual-precision-baseline-before-precision-repair01/result.json` |
| Precision/cancellation regression after source repair | All four passed; zero failed/skipped. Timer waits round upward and recheck the monotonic origin; cancelled lookup cannot start verification. | `issue-288-actual-precision-baseline-after-precision-repair01/result.json` |

The complete build log SHA256 is
`63ad50cd3dbbd2bb8424c216d2d7529f62d14bfb52a91c8a7a2957dbd49fcfdf`.
The passing focused regression log SHA256 is
`6880494ff7f8a30212b46bdc74a8045b8f446ed2c43e54a6b50bcd6a2f0d368f`.
The 47-pass/two-failure baseline log SHA256 is
`13a48c253dd6cf3fb9cc3179885ecc9b1c350e01a2e18ef5bbfd2d3229fffced`.
The warning-clean admission repair build log SHA256 is
`62a172c915ccfad35a3aaf47d5fc475c9eb6460a7456948f37adf5d595802162`.
The failed 4 GiB compiler build log SHA256 remains
`3659c53ce73f884375692274a08d789b2b8e2b7421c83705b38243c98a7e8f50`.
The complete repaired-source Authentication log/TRX SHA256 values are
`6e616262b8cefed5fdd7e41cad395a54039d0dc49122aacabd7cd1629bf39168` /
`7fe811b54ee41e7d2fab6c3d716db86aec733bb2a495b7655e0a7378c0b08841`;
Authorization log/TRX values are
`0cf9dc1b2a236f6544c22b3c8c45f5b3fd31e790b52c0c03a4f9b83313528968` /
`a4d2b185097a44fad6f9466dbe7639188450b0acfeca9db0e365b2226dbac9ce`.
The interrupted API Unit log SHA256 is
`508053046f5d893bede362bdfb18a48e4deac95780281bf80f63176efe20f5cd`;
its TRX SHA256 is
`fdbbaad5e0b52177a2417b6a148650271affb464b2f7fa617d52d06897597ea6`.
The execution was explicitly interrupted only after checking the exact owned
test-host PID, creation time and process ancestry. The cancellation receipt and
pre-cancellation log remain in
`issue-288-api-unit-docker-infrastructure-cancellation-20261007.json`.
No Docker daemon or unrelated process was restarted/stopped. The repeat uses
the existing `ECONOMY_POSTGRES_CONNECTION` contract with an owned disposable
loopback-only PostgreSQL server; it does not change tests or their deadlines.

The isolated repeat TRX explicitly records `TestRunAborted`; its 1,031 passing
results are partial evidence. The owned test process was stopped only after
verifying its PID, creation time and ancestry, to apply the observed admission
repair. See `issue-288-api-unit-cancelled-for-observed-admission-repair-20261007.json`.
Its log SHA256 is `a240ac75700665fc9e315c263b702b794ab7596d638b6b134a2b5c0e0312a9f5`
and TRX SHA256 is `1eb656ed4dfc73fb4c0b8cdc9693edb393dcff9b696af788914396b51de6e74e`.

The admission-focused complete run log/TRX SHA256 values are
`644634b1601476e9f1cc5466e158f1f739bfb18101b0b51fd575e16d997d8afa` /
`c3500847263dc4a5111cdb43e8ce865251f5fa7b00194aaa71295fd89baabb71`.
The owned PostgreSQL cleanup passed and absence was independently verified.
The failed sequence stopped before the large network observations and complete
integration suite; those later checks did not run for that sequence.

The precision baseline log/TRX values are
`966103ed3db87137fab246e954c0552355242b5effe698340c88a987faa36d8b` /
`45a7dea6b84fb6d39ddc1bd9d5af5d1683bd895e333b63d3bbcfd6c9dbc3dc1c`;
the repaired four-case regression values are
`e24392a13ebb687898cd4b772525255138368a98cd4de580e15814d2adf93084` /
`1b991cf9fab0b09aa1ef81134215ee84ece38cbc437efd8958d413454958a30a`.
Assertions remain at 400 ms; no tolerance, target, deadline or test retry was
increased. A separate complete validation sequence is required for the precision
repair source; the earlier component passes are not certification of that source.

The earlier prepared full-suite 2,348-pass/one-failure result is also retained.
That repository timestamp test compared persisted `SystemClock` time with a later
system UTC value. The existing frozen clock seam reproduces the clock mismatch;
the correction asserts the exact persisted timestamp and resets AsyncLocal state
in `finally`. No timing tolerance was enlarged.

## HTTP and acceptance boundaries

The prepared implementation produced generic 401 responses for 60 interleaved
denials plus nine warmups; a separate instrumented repetition verified no extra
dummy hash after actual wrong-password verification and exactly one completed
cost-10 dummy hash for missing/passwordless accounts. A separate actual admission
control returned 401/401/429. These earlier runs used an isolated prepared DLL,
real migrated PostgreSQL and a controlled low-risk classifier. Their logs, TRX,
collection failures and cleanup repair remain intact.

The applied-source tests cover current, cost-4 legacy and cost-13 slow BCrypt,
full-length PBKDF2, malformed records, oversized legacy input, missing/passwordless
accounts and actual e-mail/username/phone resolution. They assert generic denial,
the total floor, token absence and completed dummy work. The separate applied
HTTP controls verify the loaded DLL SHA against the actual module build; no
prepared DLL is overlaid.

The 400 ms floor is a minimum, **not a universal constant-time guarantee**. Slow
supported hashes, database/network latency and host load can exceed it. Earlier
observed medians/p95 differ between account classes. Unit passes and TestServer
observations do not prove that those distributions are non-exploitable.
Network/load timing acceptance and matching-head native/security gates remain
pending; #288 cannot be closed from this local slice alone.

The Kestrel harness verifies the actual server type, loopback listener and client
port, the loaded actual Authentication DLL digest, active rate-limit policy and
completed dummy work correlated per request. The first run passed the server/DLL
checks but failed the client-port assertion; its two failures are test-setup
evidence, not a product timing baseline. Its log SHA256 is
`6c1c5261ca990a502ac0264d67fe28cd4146c28e435fd52981df85df984ee56e`
and TRX SHA256 is
`b6538d6845f34f51bd28505fd0eede70860a1ca74fe3e22b8cf8d31d44238fc8`.
Ambient-load observations run while the separate API Unit suite and other host
applications are active; this is explicitly recorded rather than represented as
a controlled performance benchmark. No deadline, assertion or test retry policy
was expanded after either failed execution/setup attempt.

The third execution verified actual Kestrel, migrated PostgreSQL and the actual
module digest before issuing requests. Its sequential case completed 60
observations and nine warmups. All returned generic 401, no tokens and at least
400 ms, with actual completed credential work. The complete execution still
failed its concurrent case. The log SHA256 is
`3aff0a698966849b49663393dee656bd9a4228605aa3dd19fb84c8ff10a23ef3`
and TRX SHA256 is
`b1c0a0d7333528fb83dbafcb9c4199a10c8d92aae8c6dec80cc5c63f9ae266d4`.
The partial extraction in `issue-288-network-actual-partial-observations-20261007.json`
is expressly not acceptance of the complete execution or issue.

| Sequential group under ambient load | Median ms | P95 ms |
|---|---:|---:|
| Wrong password | 2295.2079 | 6268.3339 |
| Passwordless | 2454.1956 | 5024.9467 |
| Nonexistent | 2934.6370 | 5797.6364 |

These are uncontrolled local observations, not a performance certificate. The
admission repair addresses the observed fast/different denial path; it does not
establish indistinguishability for all supported hash costs and production load.
The network regression will require exactly one completed actual verification
or dummy operation for each request. A gate-denied existing account has not
verified its password and therefore must complete dummy work. Missing/passwordless
accounts must complete one dummy operation and zero real verifications.
Each network case also requires an actual admitted wrong-password verification
among its measured samples. Only that case's own synthetic attempt rows are
removed between experiments in the disposable fixture database, so a previous
experiment cannot consume the next case's shared IP budget. Account/IP thresholds
and gates remain active; no production history is removed. The native integration
suite additionally includes three deterministic Kestrel cases with a PostgreSQL
advisory lock held by another session, plus the existing two concurrency cases
now using the real timing protection service.

No new worktree, branch or stash was created. The original 55 dirty primary files
are checked byte-for-byte by the execution sequence. The frozen 328-issue inventory
and original requirements remain preserved.

## Actual precision repair validation and remaining factory lifecycle failure

The `precision-repair03` sequence tested the actual applied source, preserving
the earlier failures and the 400 ms assertions. Its solution build completed
with zero warnings and errors. These checks completed successfully:

| Check | Passed |
|---|---:|
| Authentication timing, credential work, repository and request-origin regressions | 59 |
| API admission filter regressions | 9 |
| Complete Authentication unit project | 2371 |
| Complete Authorization unit project | 1667 |
| API architecture and security selection | 125 |
| Native PostgreSQL authentication integration selection | 79 |
| Actual Kestrel sequential and concurrent observations | 2 |
| Actual source HTTP timing control | 1 |
| Actual source active rate-limit control | 1 |

The complete API integration project then executed **216 tests: 207 passed and
9 failed**. The retained receipt is
`issue-288-actual-api-integration-precision-repair03/result.json`, with log SHA256
`90464590091aa02717bdc06fefc224f892a4ef935183b9ac1a0f5bc15b4ba8b3` and TRX SHA256
`0a684fc1dbd2b498abb48df874a3a7163dbdb02c9ae6d60a9f4d61bf5f1e23d0`.
Five failures report out-of-memory exceptions, two report the unchanged five-minute
host initialization timeout and two expected successful sign-in but received HTTP
500. The two HTTP failures are not independently attributed to memory exhaustion.
The complete validation sequence failed; component passes do not erase that result.
Its disposable PostgreSQL server was removed and its absence verified.

The earlier sequence scripts guessed an API integration minimum of 416 before
the complete project execution. That guess was incorrect: the actual unfiltered
project discovered and executed 216 tests. Neither failed sequence reached its
minimum-count acceptance assertion, because the actual test command had failed.
Future validation must use actual complete-project discovery and account explicitly
for newly added tests. The previous receipts and scripts remain preserved.

The actual .NET MVC Testing dependency is version 10.0.9. Its
`WebApplicationFactory.WithWebHostBuilder` stores derived factories in the shared
parent's `Factories` collection; disposing a child does not remove this reference.
The PostgreSQL collection fixture currently derives a new configured factory for
many test cases and lives for the complete collection. This is a concrete retention
path and a candidate explanation for the complete-run resource failure. A
deterministic ownership regression reproduced the retained reference after child
disposal (expected parent count 0, actual 1). Its retained baseline log/TRX SHA256
values are `3e93309d36a578b2112843fcd8e171a317bcf7f451a1917f5c8fdec869fe5954` /
`14076b6daf342d600d8d484e5836a3f3e354c12347171413db243515c216c9b6`.
An independent configurable factory now preserves the existing database, test
authentication and configuration callbacks. The same ownership is forwarded by
the provider-session and Web3 wrapper fixtures. Twenty-eight factory construction
calls were updated, with their assertions and callbacks preserved. The first
post-change check failed compilation because those two wrapper methods had not
yet been added; no test executed in that check. Its log SHA256 remains
`da7e740be9e10a4348945676b928c747df9fcdd22e955912914f2ebd0a795d0b`.
Both corrected ownership regressions passed: the shared parent's reference count
stays unchanged, and an initialized independent host uses the same PostgreSQL
connection, receives the callback's configuration and rejects new scopes after
disposal. The passing log/TRX SHA256 values are
`6813e83d7e0a9d89b54792959c52fa2ef4d4fc1eba193877a2a3ed103f6b237c` /
`5b58f897cab89d3baf067a22037ce6f3c4e3cc22f5cb850fc70cd5d47fe5756b`.
The full corrected solution build also completed with zero warnings and errors;
its log SHA256 is `40f9dfc7e696832cf794198a951920909238df444747a5bfed1825c835cf8ca8`.
The complete sequence is now running against that exact build's source map.
Full-suite success is required before considering that diagnosis
resolved; the managed test heap remains 4 GiB and deadlines/assertions stay intact.
Source reference:
[ASP.NET Core 10.0.9 WebApplicationFactory](https://raw.githubusercontent.com/dotnet/aspnetcore/v10.0.9/src/Mvc/Mvc.Testing/src/WebApplicationFactory.cs).

The successful `precision-repair03` Kestrel observations are separately preserved
in `issue-288-completed-network-observations-precision-repair03-20261007.json`.
There are 120 measured requests and 18 warmups. Every request completed exactly
one actual credential verification or cost-10 dummy operation, returned the
canonical token-free 401 and met the 400 ms floor. Account/IP gates and rate limits
remained enabled. Each case included an admitted wrong-password verification.

| Concurrency | Group | Median ms | P95 ms |
|---:|---|---:|---:|
| 1 | Wrong password | 459.0101 | 538.0562 |
| 1 | Passwordless | 459.9090 | 576.4000 |
| 1 | Nonexistent | 465.4159 | 647.1093 |
| 2 | Wrong password | 466.1387 | 521.8751 |
| 2 | Passwordless | 470.3415 | 524.5233 |
| 2 | Nonexistent | 469.9502 | 585.2653 |

These measurements retain their exact source/DLL hashes and uncontrolled ambient
host-load qualification. They cover configured BCrypt cost 10 and do not certify
all supported hash profiles or production timing indistinguishability. #288
remains open; matching-head native/security validation and the remaining timing
acceptance are still required.

The independent-factory source also passed the complete Authentication project
(2,371), complete Authorization project (1,667), API architecture/security selection
(125), admission selection (9), timing/credential selection (59), ownership selection
(2) and PostgreSQL authentication selection (79). The new Kestrel repetition passed
both cases, retaining another 120 measurements and 18 warmups with the same active
gates, actual loaded module digest and per-request completed-work assertions.
See `issue-288-completed-network-observations-factory-repair01-20261007.json`;
its log/TRX SHA256 values are
`7704a1f17fcfae244ed7757c86c438ddcf9f7f6dd6cef507f2d0fbfffd0ed2b3` /
`8984e700ee91532169c3b0357e9188138c69c32554ec03851ac299ce3ba0f0d1`.

| Independent factory repetition concurrency | Group | Median ms | P95 ms |
|---:|---|---:|---:|
| 1 | Wrong password | 843.8931 | 2165.8758 |
| 1 | Passwordless | 937.2971 | 1828.6252 |
| 1 | Nonexistent | 752.6416 | 2325.5314 |
| 2 | Wrong password | 623.4826 | 1121.4207 |
| 2 | Passwordless | 564.0599 | 1059.2242 |
| 2 | Nonexistent | 557.9355 | 708.7331 |

Host load remains uncontrolled. These changed observed distributions are preserved
as measured, rather than selecting only the earlier, lower-latency run. The generic
response/work/floor assertions passed; distribution indistinguishability and the
complete API integration rerun have not yet been accepted.

### Complete-suite discovery and remaining host ownership, 2026-10-07

The `factory-repair01` sequence failed. The raw TRX reports `Failed`, with 227
distinct reported cases: 207 passed and 20 failed. Its execution log and TRX
SHA256 values are `33cec205f689729c5d35e60d6428cdad9a0d029be1ea9de944400f4b54843e14`
and `e8821a1ce2db0e5db42d85612e016a1435948220f2e55b69005d1ee7654a539b`.
The already failing owned testhost was stopped after diagnostics, using its exact
process identity and creation time. The saved TRX outcome remains authoritative;
the controlled stop does not make it an `Aborted` TRX. The owned PostgreSQL
container was removed and its absence verified. Sources stayed unchanged during
the execution. No complete-suite success is claimed.

An independent `dotnet test --list-tests --no-build --no-restore` discovery against
the same compiled API integration assembly found **418 distinct cases**. Its log
SHA256 is `aaf8969a58e7291b5f9e8b21c00c29546682224ece9cb24e9940857e00891b35`.
The earlier 216 and 227 counts describe cases that reached the reports, not the
complete discovered suite. The inference of 218 from the earlier report plus two
new facts was incorrect. The previous 416 minimum had not been validated by
discovery at the time. Keep both failed sequence receipts; future runs must use
actual discovery and prove every discovered case was reported without skips.
The names, missing cases and duplicate checks are retained in
`issue-288-integration-discovery-before-startup-disposal-20261007/result.json`.

Three remaining startup/OpenAPI test classes derive a factory per test instance
from a shared class fixture without implementing per-instance disposal. Their
configuration callbacks and assertions will be preserved while correcting that
ownership. A local diagnostic collection observed 15 delegated factories and 12
copies of several background services. This supports investigating retained
hosts, but is not a complete root analysis or proof that every failure has the
same cause. The GC dump warned about lossy collection, reported exactly ten
million objects and induced a full GC. Its statistics are partial observations,
not a complete managed-heap inventory or a performance acceptance run. The
separate process dump collector failed; its partial file is not accepted as a
usable dump. These local ignored diagnostics contain no published object values.

The 55 dirty primary-checkout files remain byte/status identical to the original
guard. #288 stays open pending the full suite, supported hash-profile timing
acceptance and matching-head native/security gates.

The three disposal-contract regressions failed before the startup/OpenAPI repair
(0 passed / 3 failed). Their retained log/TRX SHA256 values are
`6675e150a8197db4f8b9a791de65a13e1b757afef80777a926b9e6a2c718bd8a` /
`400e11a9978450b63875846796215f08219ec74e0480c0e7aa7f7040937d5e5e`.
The corrected classes implement per-test disposal and construct independent
configured factories. The development-only Swagger host and commerce/startup
hosts now also have explicit ownership. Configuration callbacks, database
provider selection, assertions, timeouts and retry counts were preserved.
All eight ownership regressions passed, including actual initialization of each
of the three in-memory hosts followed by rejected scope creation after disposal.
The passing log/TRX SHA256 values are
`cfcc29b2a66fb28e7a5104d31e46b2bc01377c63b214874683f6d925b9e2b600` /
`0f72a877c5aea203e5f959f52c461f31ff9662d8973b1c1880d89f10d3d637f6`.
The owned PostgreSQL container was removed and verified absent.

The complete solution rebuild finished with zero warnings/errors. Its execution
log SHA256 is `5e2d47ec75bbaa4c7a0962a08d14ebdf0b48c441f2942b75e9964456b9978c6f`.
Independent discovery now identifies **424 distinct integration cases**: all 418
previous cases plus exactly six disposal regressions. Discovery log SHA256 is
`12e536b3afa25bc226f2a6b0ce7e2d660afe9d4fb8b0f5a02114ee8d8b5f1dbe`.
`startup-disposal02` is repeating the full suite first, followed by the other
component and HTTP gates. Acceptance requires the final reported names to match
the actual discovered set exactly, with all cases passed and none skipped.
This run is pending; the previous partial failures remain recorded.

`startup-disposal02` subsequently failed under the local 4 GiB full-project cap:
216 cases were reported, 211 passed and 5 failed. The raw outcome is `Failed`;
208 discovered cases never reached that report. The failing owned testhost was
stopped by exact identity without inducing diagnostic GC, and its owned
PostgreSQL container was removed and verified absent. Source bytes stayed
unchanged. The log/TRX SHA256 values are
`0754499883af7b66d4b1b98731a2b5bd83f5ca7161fb76fff03f891f3f45885f` /
`9242df9580742e819bf45396c258d97fd5a39bf7e86500c81108964e4e3a6d70`.
The passing ownership tests establish disposal behavior; they do not establish
that the complete project's memory consumption is resolved.

Inspection of `.github/workflows/pr-verify.yml` identified a local collector
resource-scope mismatch. Native affected-project tests run without a 4 GiB heap
cap; the separate full-application `FullyQualifiedName~OpenApi` step has that cap.
The local collector had applied 4 GiB to every test task, including all 424 API
integration cases. `ci-resource-scope01` now preserves and independently discovers
the exact native OpenAPI selection with its original 4 GiB cap, then repeats all
424 cases with an 8 GiB local full-project safety limit. The machine had 21,622 MiB
free physical memory while the failed owned process was still alive. Other
component, HTTP and network test tasks retain their 4 GiB limits. All source,
assertion, timeout, retry, no-skip, discovery-name, primary preservation and
container-cleanup checks remain in place. Native CI has not been edited or waived.
The earlier 4 GiB full-project failures are retained as resource observations.
The new sequence is pending and does not yet certify full-suite or issue acceptance.

The exact native OpenAPI selection has now passed: **15 executed / 15 passed /
0 failed / 0 skipped**, with raw TRX outcome `Completed`, source bytes unchanged
and the original 4 GiB limit. Reported names matched the independent filtered
discovery exactly. Its log/TRX SHA256 values are
`aa82f6c53bea585a3daa45f1126def6d0fcac873646b2759be859b2c8f03a102` /
`9e40984c8fad45581ef1242de7bbe1b31e356ff17792f4ae7c2cf9aa75324ecd`.
The owned PostgreSQL container was removed and verified absent. The separate
424-case full-project execution is now running; no final result is inferred from
the successful OpenAPI selection.

### Unreported complete-suite termination and independent restart, 2026-10-07

The `ci-resource-scope01` full-project execution ended without `results.trx`
or a collector `result.json`. Its execution-session handle and all matching
owned Python/.NET/testhost processes were independently absent; the cause
is unknown. The last verified live testhost observation was 22:12:21 UTC,
with approximately 7,341 MiB private memory. Neither a test exit code nor
pass/fail counters can be inferred from this attempt. Its log SHA256 is
`90bd48e61fdcebf331e8718635fb71f95de013bb87f9e209840519cba8e56e2a`.
The actual source map still matched the warning-clean build. The exact
owned PostgreSQL container was verified by ID, name and task label, removed,
and independently confirmed absent at 22:24:54 UTC. The 15 passing OpenAPI
results above remain separate, conclusive results.

`ci-resource-scope02` began at 22:26:05 UTC as a hidden independent Python
process, with unique output files and a saved PID/creation-time/helper-hash
receipt. Survival after the launching shell exited was verified. It repeats
the same source, primary-checkout, discovery, no-skip, raw outcome and cleanup
checks, preserving the exact 4 GiB OpenAPI limit and bounded 8 GiB full-project
limit. No source, assertion, timeout or retry was relaxed. This attempt is
pending; #288 is still OPEN, and complete-suite success is not claimed.

### Complete API integration pass, 2026-10-07 22:53 UTC

The independent `ci-resource-scope02` full-project execution completed from
22:29:50 to 22:53:49 UTC, with **424 executed / 424 passed / 0 failed / 0 skipped**.
The raw TRX outcome is `Completed`, and its 424 unique reported names exactly
match the compiled discovery, including all eight ownership regressions. Actual
source bytes stayed identical to the warning-clean build. The complete-project
managed heap remained bounded at 8 GiB; the separate native OpenAPI selection
retained its original 4 GiB limit. Native workflow/assertions/deadlines/retries
were unchanged. The exact owned PostgreSQL container was removed and verified
absent; cleanup and functional check both returned zero.

Execution log SHA256:
`3f7ba7da6c340ff50c3d066ebc025edf5d46036822102af3e64fbbe1d12a25f3`.
TRX SHA256:
`55b143c04d7c05fb39f3b1b459d5639083ade2faf7f478550e5cdaaaab29b72a`.
The subsequent focused Authentication and API admission checks passed before the
complete component projects started. The whole local sequence and timing-profile
acceptance remain pending. All earlier failed and unreported attempts remain
preserved; this passing execution establishes its own complete-project result.

### Repeated component and actual-network results, 2026-10-07 23:32 UTC

The same actual source subsequently passed complete Authentication (2,371) and
Authorization (1,667), focused timing/credential cases (59), admission cases (9),
API architecture/security (125), and PostgreSQL authentication (79). Selections
overlap their complete projects; these counts are not summed as unique coverage.

The actual Kestrel repetition passed both sequential/concurrent cases. All 120
measured requests and 18 warmups returned the same canonical token-free 401,
met the 400 ms minimum, and completed exactly one verification/dummy operation.
Each wrong-password group includes an admitted verification; subsequent account
or IP admission denials may use dummy work. Gates remain active. Configured cost
is 10, the low-risk classifier is controlled, and ambient host load is uncontrolled.
The actual loaded Authentication module SHA256 is `cbb002853fb20326ad6f622fdaca7f242900af87c9ea26dbe1014d57921ec640`.
Log/TRX SHA256: `20f9cd2abe978a8ae90ef8e5509b74d48a2fa02fd961d32b58c433a57472c2f4` / `a8725f7d85baa5b2303d3014b4c225cb7f12b62e7dc3ce6aaddf3f56e22ec1af`.

| Concurrency | Group | Median ms | P95 ms |
|---:|---|---:|---:|
| 1 | wrong-password | 1105.2937 | 2272.3222 |
| 1 | passwordless | 1085.1852 | 3288.7787 |
| 1 | nonexistent | 976.9663 | 3180.8305 |
| 2 | wrong-password | 1102.0370 | 1917.2714 |
| 2 | passwordless | 1372.6284 | 1942.3945 |
| 2 | nonexistent | 1370.8254 | 1928.2995 |

These observed distributions are retained alongside both previous repetitions.
They do not certify all supported hash profiles or production indistinguishability.
The final HTTP controls are still running. Independently compiled API Unit
selection now discovers 1,078 distinct cases; a full-project supplement is prepared
after the current sequence finishes, with exact discovery/report matching required.
No additional unit case has been executed by that preparation. #288 remains open.

### Final HTTP controls and independent cleanup reconciliation, 2026-10-07

All eleven actual-source functional selections completed successfully, including
canonical real HTTP credential denials and active 401/401/429 rate limiting. Every
reported case passed, no case was skipped, and every raw TRX outcome is `Completed`.
The API integration selection exactly covered all 424 discovered cases and all
eight ownership regressions. The current path set and bytes of all 6,456 actual
source/build-definition files matched the warning-clean build; the 55 unrelated
primary-checkout files remained byte/status-identical to the preservation guard.

The final rate-limit wrapper timed out after 30 seconds while collecting removal
of its exclusively owned PostgreSQL container. Its functional test had already
passed. The original wrapper and sequence remain failed receipts; neither was
rewritten as passing. Independent exact-ID inspections subsequently confirmed
that container `04cb8c214cca7336aef5c79a044d6d620fd055f11b9430571282728f38b239b4`
was absent, most recently at 23:54:20 UTC. No functional selection was rerun merely
to repair cleanup evidence, and no global Docker operation was performed.

The new, separate reconciliation accepts the completed functional selections and
verified cleanup. Its first attempt retained an additional diagnostic failure:
two distinct Authentication additional-claim theory cases have the same shortened
VSTest display name. All 2,371 cases have distinct test IDs and execution IDs and
passed. The successful reconciliation verifies those identities, while retaining
exact unique-name/discovery matching for API integration and OpenAPI. It does not
waive any test assertion, timeout, retry, native/security check or issue criterion.

Original sequence SHA256:
`16e56de41827de1b5ba44c693fad50b9a72ebb4ba7fd126b47cacc078a1a5724`.
Original cleanup SHA256:
`131fd6b079d2a358f6cdd213cfa745b487ed3c59113225141e83c6af8cbd1d23`.
HTTP timing-control log/TRX SHA256:
`576ee4d128ec03ca720e84cbabd0130037e08a8d2c5677921564fcb61e948ecd` /
`5e68012c0ff152fd46d96006d9ce2cc3bab1bc5226f7645877defa13037df25e`.
HTTP rate-limit log/TRX SHA256:
`61622d1140577cfef816bf9c4c4f332f20e9ef0a95f74d8f34246eb6724b8aa3` /
`16162dc0d4d401823f018b3fffe2f9db32a0bb46c9836bd9d73dd6205485212f`.

The full API Unit supplement began at 23:55:18 UTC as an independently identified
hidden process. It must report all 1,078 independently discovered cases with exact
discovery coverage, no skips, raw `Completed`, unchanged actual source and primary
checkout, and verified exclusive PostgreSQL cleanup. Its preparation/start is not
acceptance evidence. #288 and #287 remain open; matching-head native/security and
remaining supported-hash/profile timing acceptance are still required.

### Complete actual API Unit supplement, 2026-10-08

All **1,082 actual runtime cases** were reported and passed, raw
`Completed`, with verified expanded-theory coverage and zero failures/skips. Actual
source remained unchanged; exclusive PostgreSQL cleanup and all 55 primary files
were independently verified. This replaces the prior partial/aborted Unit result
as current evidence without deleting that history. Discovery lists 1,078 entries,
including one deferred Exception-data theory. Its five source-defined rows have
distinct runtime names/execution IDs and one shared discovery test ID. All other
1,077 names match exactly; 1,078 - 1 + 5 = 1,082, with no missing case. The prior
strict 1,078-counter receipt remains unaccepted and unchanged. Log/TRX SHA256:
`368d37b3eea48442da5915b5bbe5e00b0035e0aae026c55773d1be379bf85b75` /
`0fd4a2545181e870588f214f86c4d0fdb748356d825a123fc8a90aedae890be7`.

The separate eleven-selection/cleanup reconciliation SHA256 is
`411c6e341c79fe9d42e59065a6f480dd1f29244e295b45d7844abf4c5ca34a6b`. Its original failed sequence and cleanup receipts
remain unchanged. Complete local validation permits review of this increment;
matching-head native/security and remaining issue acceptance are still pending.

### PR #704 follow-up: shared admission and dependency gates, 2026-10-08

PR #704 is open at published head
`aa4ccb64da1ad29ae1a9166e215ac6b597263070`. Its API verify, OpenAPI consistency
and four CodeQL analyses passed. Repository policy failed on six newly reported
Next advisories, Codacy requested changes, and Copilot identified a real bypass:
polymorphic password sign-in did not enter the account/IP admission gate.
These results belong to that published head. The preceding broad local evidence
does not certify the subsequently modified source.

The follow-up in the same owned checkout introduces a required Authentication
module admission port implemented by the API. Local MVC and polymorphic
email/username/phone entry points share the original thresholds, canonical email,
source-IP normalization and PostgreSQL advisory-lock key scopes. The caller holds
the lease through credential verification and durable attempt/session recording.
Admission denial uses the same server-owned timing origin and generic token-free
error. New unit cases cover canonical identifiers, denial before credential work,
and lease disposal; twelve new HTTP cases cover account/IP thresholds and held
account/IP locks for all three polymorphic credential kinds.

Actual-source focused Authentication passed **96/96**, API admission/registration
passed **23/23**, and complete Assets passed **1,164/1,164**, with zero failures or
skips. The API and Assets source maps match. The earlier Authentication focused
map differs only in the two subsequently added Assets image tests; Authentication
production and test bytes still match. Assets includes actual benign PNG decoding,
repeatable perceptual hashes, distinct mirrored patterns and stream reset checks.
These passing selections do not establish the new PostgreSQL HTTP cases.

New execution artifacts are under
`D:/Codex/work-artifacts/release-2026-11-25/gameguild-issues-01a0d900/`:

| Selection | Log SHA256 | TRX SHA256 |
|---|---|---|
| Authentication focused | `1d9bcc5c6601e0e87ce250236b89405404b1344bfe3436c890b17f2525d3e0ee` | `8e551a49dc0085d2efbf23013a8e173ac365e972fafd8468ba5e94bd81e6f3a5` |
| API admission/registration | `ca1e9b0fbae64e40b110a1741c4e2794f05b1b5d609dd84819a8d6de21873c62` | `dbc47b168ebdb106fd072f80d38e70e8b5cda1438913dca927185355bd0790ef` |
| Complete Assets | `389f8801e1124802c6ab62b60eddfef5912a822eac7b698bcc4a72182462f622` | `aa44cfff04250ab10f3c523cb9644b4a2be37922e1b864985345a8d988ad269c` |

Three owned PostgreSQL preparation attempts executed **zero functional cases**.
The first two failed readiness/exec collection; the third timed out collecting
`docker run` although its labelled container had started. Its retained server log
shows cluster initialization, but does not establish readiness or the root cause
of the slow Docker operations. Independent exact-ID/owner inspections confirmed
all three containers absent after cleanup. The original failed receipts remain
unchanged. No global Docker restart, prune, timeout extension or test retry change
was made. The twelve new HTTP cases remain unverified.

Next consumers, eslint configuration and the root override were normally upgraded
from 16.3.6 to 16.3.8; the frozen install and strict pnpm audit passed. The remaining
two known advisories still require and passed their existing seventeen patch
regressions. Security-validator regressions passed 9/9 and dependency-policy cases
passed 7/7. No audit/security policy was relaxed. The full Web regression is still
running and has reported two Markdown/VegaLite failures; no current Web acceptance
or attribution to a particular cause is claimed.

ImageSharp 4.1.2 replaces vulnerable 3.1.11, but requires a configured Six Labors
license. Debug compilation emitted its two license warnings. The actual Release
Assets build failed on license validation with unchanged inputs, exit code 1;
log SHA256 is
`621d72e2442ac0177b26775a9f851f8b0cf14e9cd4432584c10a57334329500d`.
The owner must choose a valid configured license or a compatible replacement.
No purchase/application, warning suppression or license-validation bypass was
performed. Passing Debug tests do not prove Release readiness.

At 02:37:58 UTC the primary develop checkout still had the same 55 unrelated files,
with original bytes and Git status, at `d5f417328e9ff9d3fa0517796a623e310aeb1ef0`.
Only row #288 changed in the 328-ID matrix; all other rows and their original
requirements were preserved. #288 and #287 remain open. The follow-up is not yet
committed or pushed; supported hash/profile timing, real admission HTTP tests,
Release licensing, Web failures and matching-head native/security/review remain
required before merge or closure.

The subsequently completed full Authentication project passed **2,378/2,378**,
zero failures/skips, raw `Completed`, exit code 0 and unchanged actual source.
This includes the seven added shared-admission cases and retains every existing
case. Its complete source map matches the API/Assets follow-up maps. Log/TRX
SHA256: `fe67315f36e53d20988ebf8fc897e96ea7e9948d5e6541601fdd66b24a841b4a`
/ `6638d4a78630359090fe563404a2b81012686e6b80337d461ddba1347dda30b2`.
This result does not resolve the pending PostgreSQL, Release or Web gates.

The new API Integration test project also compiled from actual unchanged source
in Debug (exit code 0; retained license warnings), with no functional cases run.
Compile log SHA256:
`7536132810de1e7e49d871d5000129fa5f685dc318f696c6cf8e3c3bfe236f63`.
Short CPU profiles from the exclusively owned Vitest process showed waiting/module
loading; its active test thread changed from 287 to 329. This establishes continued
execution despite silent reporting, not the cause of the two failures. The
temporary loopback inspector and profiling sessions were closed. Full Web results
remain pending.

The fresh fixed-ID GitHub snapshot at 02:48:47 UTC reports **75 closed / 253 open**:
28 `COMPLETED`, 46 `DUPLICATE`, one `NOT_PLANNED`. #323 and #328 were closed by a
separate audit as already implemented on develop, with criterion/source maps and
reported test evidence in their respective closure comments. This front inspected
those comments without independently rerunning their closure-specific integration
or performance cases. Their live state and evidence links were reconciled in the
matrix without changing their original requirements. No issue was newly closed
by this continuation, and #288 remains open.

The first full Web run then ended with exit code 1 and no complete suite counters.
The diagnostic teardown attempted a dynamic import through `Runtime.evaluate`;
that VM context lacked its import callback, causing
`ERR_VM_DYNAMIC_IMPORT_CALLBACK_MISSING` and interrupting Vitest. This is a
collector-induced failure, not an accepted complete result or a product defect.
The earlier two Markdown failures precede the diagnostic and remain recorded.
The original log and failed execution are preserved. A clean focused Markdown
repetition was started without an inspector, with unchanged source/assertions and
the existing timeout; full Web acceptance remains pending.

That clean focused repetition passed all **8/8** Markdown cases, including the two
previous failures, with the existing timeout and no inspector. Inputs were
unchanged; log SHA256:
`5cf143b1cb72ddb5166a0e75da9b7668ccdc97f2fce226ae3e6eea3609392406`.
This narrower repetition does not replace complete Web coverage or establish the
cause of the earlier failures. A fresh full Web run with default concurrency and
timeouts, byte guards and a JSON reporter was started separately; its completion
and final counters remain pending.

A fourth PostgreSQL execution prepared a new, exclusively labelled loopback-only
container with isolated 512 MiB tmpfs storage. PostgreSQL settings, test assertions,
collector deadlines and test retries were unchanged. Docker had responded to a
read-only availability probe in 5.68 seconds; the host sample showed 100% CPU and
about 19.5 GiB free memory. These observations do not establish the cause of earlier
startup failures. The new server became ready and the actual-source polymorphic,
lockout-concurrency and timing HTTP selection started at 03:04:25 UTC. Container
`281eeaa8cae73362fad68b73d173322099454043ab2fb1b89c57ed6dcad8e4c4`
belongs exclusively to this execution and is scheduled for exact-ID verified
removal by its wrapper. Runtime results and cleanup remain pending. This execution
does not test persistence across server crashes or production timing distributions.

The fourth selection completed with **78/78 passed**, zero failures/skips, raw
`Completed` and unchanged actual source. It includes all twelve new account/IP and
held-account/held-IP HTTP cases for email, username and phone. Runtime execution
IDs are unique and the source map matches complete Authentication, API admission
and complete Assets. Exclusive container removal returned zero and exact-ID
inspection confirmed absence. Log/TRX SHA256:
`5002cac56fb36f1eb764fde39a61c219205ecbbf434a04431cba8e24632356c8`
/ `6bddf185a0395e2486bc7add3936cee9bdcb182260ad7b606d3d2b936ca997c0`.
This three-class selection excludes the separate password-hash HTTP class and is
not a complete Integration project result. Release licensing, clean complete Web,
matching-head native/security/review and remaining timing-profile acceptance are
still pending. The earlier three preparation failures remain unchanged.

## Date-clock and native-publication checkpoint — 2026-10-08

The clean complete Web02 run finished with **2,996 passed / 2 failed / 2,998
total**, zero skipped and unchanged source. Both failures were date-selector
queries for 8 October: the real calendar prefixes the accessible name with
`Today, ` when the host date reaches that fixture date. Log SHA256:
`8771ecef23a9e769b6d7193092b16a5e7e02c0c9364bf9dd82e031613ef7dd94`.
The earlier interrupted execution and narrower Markdown repetition remain
separate historical results.

The date test now fixes only `Date`; event, delay and timeout timers remain real.
All ten original tests, assertions and deadlines are retained. An eleventh case
explicitly verifies the today-prefixed button and the complete submitted interval.
Focused execution passed **11/11**, with unchanged test bytes. Log SHA256:
`a621fb704858a9b06c576afeb9e7cc9199bb75383c9f9dc986eaf32cb515fa2c`.
The separate complete Web03 run started at 03:53:16 UTC. Its sole guarded source
delta against Web02 is that test file; default concurrency, isolation and deadlines
are unchanged and no inspector is attached. An observed Markdown/Mermaid failure
is retained; final counters are pending. No cause or complete Web acceptance is
claimed from the partial report.

Current #145 MFA diagnostic failures and its complete original scope are recorded
separately in `authentication-mfa-policy-reconciliation.md`. Their failed policy
preconditions do not prove a runtime login bypass. No MFA product code was changed
in this publication increment.

At 04:11:41 UTC the fixed 328-ID scope still had **75 closed / 253 open**: 28
completed, 46 duplicates, one owner-cancelled. Only #145/#288 changed in this
matrix update; the other 326 rows and every original acceptance field were
preserved. The primary develop checkout's 55 unrelated files and Git status
were again byte-verified unchanged at `d5f417328e9ff9d3fa0517796a623e310aeb1ef0`.
No new issue was closed by this front.

The local increment is being published in the same existing PR #704 branch so
native CI/security/review can examine the actual fixes. Publication is a review
candidate and does not establish merge readiness. The unresolved Release license,
complete Web/native/security/review results and remaining timing-profile acceptance
remain required. No merge waiver, audit relaxation, license suppression, new
worktree, branch or stash is introduced.
