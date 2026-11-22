# Coding surface implementation state

Last updated: 2026-11-22.

## Deferred corrections and instructor controls - 2026-11-22

The user requested recording these follow-ups for later implementation. This
is a documentation-only handoff, not authorization to implement them now or
acceptance of a particular entry/scaffolding policy. The existing runtime
[audit](./RUNTIME-RISK-VALIDATION-2026-11-22.md) retains the reproductions and
qualifications. No new capability status or completed gate is inferred.

### Runtime corrections

Resume within [UX03-A/B](./06-refoundation/03-workbench-runtime-and-capabilities.md)
after its prerequisites. Address false approval first; resolve the entry policy
before dependent function/scaffolding changes. Independent work may proceed,
but an affected capability cannot be advertised as ready while its oracle fails.

| Finding | Deferred action | Acceptance requirement |
| --- | --- | --- |
| `CS-RUNTIME-COMPILE-FAILURE` | distinguish compilation/linking failure from a completed program before comparing output | invalid source with empty expected stdout cannot pass; valid empty-output programs and intentionally expected nonzero program exits remain valid controls |
| `CS-RUNTIME-NATIVE-ENTRY` | decide whether the selected path means initial editor file or executable root; make learner responsibilities explicit | renamed main plus helpers works; competing/absent/duplicate main cases follow the reviewed contract, without breaking legitimate multi-file programs |
| `CS-RUNTIME-FUNCTION-ABI` | compose language, harness linkage and startup through supported public APIs | an ordinary C/C++ function-only solution works without incidental main/extern-C boilerplate; helper files work when allowed; intentionally wrong solutions fail |
| `CS-RUNTIME-GRAPHICS` | use the public canvas lifecycle for graphical diagnostics and define official execution support separately | real supported-artifact rendering/input/resize/cleanup; unsupported official execution is unavailable, not a passing WASI fallback, zero grade or invented visual judge |

These corrections do not authorize new upstream edits. Preserve Emception
core/browser/IDE/UI and use EMCEPTION-GATE if public composition is genuinely
insufficient; the earlier targeted worker exception remains limited. Preserve
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
`CODE-UX-02` is in progress: UX02-A draft contracts and pure transformations
are implemented, but its broader security gate has an external failure.
UX02-B persistence/session ports and host-action integration are implemented
and tested in scope; formal closure and the production cutover remain open.
UX02-C publication metadata and WB-A safety preparation are implemented/tested
in scope; full wire/configuration parity and broader gates remain open. The
production UI has not been cut over to the refounded model. Published assets
still require local refresh and
real execution checks in CODE-UX-03, not an upstream IDE build/publication.

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
| Parte 3.4 | em validacao final | consumidores e CI migrados; runner fixture now emits canonical `ExerciseType`; local asset refresh and real browser E2E remain pending |
| Parte 3.5 | concluida localmente | `CANON-01` a `CANON-06` concluidos; validacao local verde, com fixture do runner e browser CI ainda pendentes no fechamento de `CANON-07` |
| Parte 3.6 | in progress | `CODE-UX-01` closed; UX02-C/WB-B contracts, wire, backend and current client checks pass; official execution fidelity and broader G-API disposition remain open |

## Etapa ativa

- Subplan: [3.6.2 domain and drafts](./06-refoundation/02-domain-drafts-and-data-boundaries.md).
- Current gate: `CODE-UX-02`, UX02-C/WB-B implementation and audit in progress.
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
  ADR032 the worker's deliberately local transport guard/import boundary.
  Actual local artifact preflight remains a CODE-UX-03 obligation.
- Responsavel: Codex.

### Current compact checkpoint

- Baseline: `6d90892224`, clean implementation entry on feature/coding-grading.
  The user-requested commit sequence is recorded above; no merge or push is
  authorized by that checkpoint. Allowed scope and validation evidence remain
  in the WB-B execution record.
- Active row/stage: UX02-C / WB-B audit and independent closing gates.
  Execution/environment JSON, domain compilation, inverse wire bridge,
  revision freezing, redaction and strict backend validation are implemented.
  Time/output/file-limit forwarding has composition tests, not real native
  conformance. Production UI/save coordinator and preserved IDE are unchanged.
- Contract: legacy absence stays absent; no V2, rewrite, table or migration.
  Unsupported configured RAM remains recoverable but rejects publication.
  New explicit configuration cannot fall back to current/default identity,
  drop malformed tests or coerce mismatched scalar values.
- Terminal: coding/adapter/surface types pass; package tests 96/16/46. Complete
  Courses 1227, CodeAdapter 179 and Assessments 453 pass. Final web selection
  passes 181/181; worker 104/104 plus strict tsc. Authorization 2815 and selected
  API architecture/security/OpenAPI 382 pass. Authentication retains its
  external 4572/4573 result. Official current-source client generation, types,
  complete ESM/CJS/DTS build, public-API types and 1772 tests pass; its two
  existing private-export DTS warnings remain visible. No endpoint churn.
- Resources: the current actual Release host build passed, zero warnings/errors,
  under the approved monitored 14 GB profile; peak 13046 MiB, 598285 ms, minimum
  available 5461 MiB, no stop/OOM or source/analyzer bypass. The full-solution
  Release attempt stopped at its ten-minute budget without OOM and did not pass.
  Ordinary checks remain <=10 GB; no concurrent heavy checks or IDE build.
- Runtime/human: isolated real-WASM worker and public-canvas diagnostics now
  confirm three candidate bugs and one entry-policy decision; see the dated
  report above. This is not full-stack, human UX or academic acceptance.
  Existing schema-1 development assets remain unchanged; scratch schema-2
  assets pass preflight. Normal exact-source CI provenance is still required.
- Open decision: user asked whether to expand only the grading worker's
  public-API composition or defer execution acceptance. No implicit support
  reduction, upstream change or runtime-fidelity pass while waiting.
- Next exact action: review the dated runtime findings, decide entry semantics
  and obtain correction scope before dependent implementation. Prior contract,
  lint/types, host/client and selected API checks remain valid for their stated
  source; no repeated OOM investigation is needed. WB-B remains partial and
  CODE-UX-02 open until fidelity acceptance is satisfied. Keep the sole lesson
  coordinator and existing data.

| Gate da refundacao | Estado | Evidencia necessaria |
| --- | --- | --- |
| `CODE-UX-01` | complete | corrected contract/document/design/preparation audit passed on 2026-11-21; no runtime/build claim |
| `CODE-UX-02` | in progress / gate open | contract/wire/backend/current-client/selected-architecture checks pass; runtime fidelity, the full-solution time stop and external Authentication failure remain open |
| `CODE-UX-03` | nao iniciado | refresh compatible published assets and pass local preflight before real runtime, canvas/terminal, cancellation and resource checks |
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

Continue [`3.6.2`](./06-refoundation/02-domain-drafts-and-data-boundaries.md):

1. Retain the explicit user design approval recorded for
   [3.6.1a](./06-refoundation/01a-prototype-workflows-and-critical-review.md).
   Do not repeat the prototype work or mislabel that approval as production
   runtime/usability acceptance. Initial scalar-only scope is also explicitly
   accepted in CS-ADR-015; compound support remains tracked in EMC-GAP-001.
2. Retain the tested UX02-A/B ports and UX02-C/WB-B configuration and immutable
   revision evidence. The local read-only inventory is not a remote-data claim.
   RAM deferral is implemented; the historical worker assertions are reconciled.
   Current strict host/selected architecture/client evidence is complete. Keep
   the demonstrated monitored 14 GB profile for required host checks only;
   ordinary checks remain <=10 GB. Do not repeat obsolete OOM diagnosis, disable
   analyzers or hand-edit a generated client. Preserve the full-solution time
   stop and external Authentication failure as incomplete broader gates.
   Official execution fidelity is still blocked by the four CS-RUNTIME oracles
   and the pending scope answer. Do not turn partial completion into gate
   approval, install a second autosave or deploy the standalone IDE.
3. Before CODE-UX-03 real execution, preserve old generated CDN/release copies,
   explicitly refresh the compatible pinned published assets and pass both
   local manifest checks in G-PREFLIGHT. The current helper can retain schema 1;
   rerunning sync/dev without a backed-up refresh is not readiness evidence.
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
