# Refoundation execution governance

Status: required for planning and execution of part 3.6 and its subplans.
Adopted: 2026-11-19, by explicit product instruction (Meta-Harness).

## Invariants and boundaries

1. Preserve root/app `AGENTS.md`, platform purity, dependency direction,
   authorization, tenant isolation and reviewed anonymous-endpoint rules.
2. Coding owns problems, signatures, files and technical cases. Grading owns
   official attempts, academic scores, review and release. Surface execution
   produces diagnostic evidence; it never becomes the academic authority.
   Name the authoring task **Validate exercise**, not bare Validate/Validation.
   It checks the prepared exercise with reference/incorrect solutions; it is
   not the Assessments/Grading workflow for evaluating learner submissions.
   Localized labels must retain that explicit exercise scope.
3. The host owns authentication, persistence, concurrency, routing and official
   commands. Packages consume typed ports; they do not import app internals.
4. Preserve `tools/emception/**` and `packages/infrastructure/ui-emception/**`.
   Consume supported public exports. An unavailable capability follows
   `EMCEPTION-GATE`; it does not justify a private import or another executor.
5. Preserve published revisions, submitted files and historical migrations.
   Inventory actual stored data before changing interpretation. A necessary
   storage change follows `SCHEMA-GATE`, with a real migration and upgrade proof.
6. Private cases/reference solutions never enter learner payloads, VFS, logs,
   storage or publicly accessible assets. Preview uses redacted data and its
   own scratch state; hidden controls are not a permission boundary.
7. One coherent contract spans TS, API, snapshots and adapters. Incomplete
   drafts have their own model. No speculative V2 aliases or silent fallback.
8. Canvas is an execution/output capability. Family, language, environment,
   capability and academic policy remain distinct axes. Availability requires
   evidence for the combination, not merely a visible checkbox.
9. Form editing starts no compiler. Use one browser worker, bounded logs,
   sequential heavy checks and the resource budget in `PRODUCT-MODEL.md`.
   Keep at least two CPU cores free where hardware permits. Do not change
   `dev:fast`, stop unrelated services or reset data to facilitate a test.

## Directory topology

| Location | Owner / permitted responsibility |
| --- | --- |
| `docs/plans/coding-assessment-and-code-studio/03-coding-surface/` | shared state, ADRs, consumers and upstream gaps |
| `06-refoundation/` below that directory | product evidence, this governance, gates and prototype plans |
| `06-refoundation/prototype/` (planned) | isolated typed design fixture and bounded tests; never imported by production |
| `packages/features/coding/src/` | framework-independent contracts, parsers and transformations |
| `packages/features/grading-adapter-code/src/` | pure translation between coding and generic grading |
| `packages/features/coding-surface/src/` | reusable task views, local state and public runtime integration |
| `apps/web/src/lib/coding-assignment/` | host wire bridges and authorized integration |
| `apps/web/src/components/learning/` | Learning composition and persistence/command ports |
| `apps/web/scripts/coding-cycle-*` | existing test orchestration and artifact preflight |
| `apps/api/Source/Modules/GameGuild.Learning.Courses/` | owned coding content/draft domain paths |
| `apps/api/Source/Modules/GameGuild.Learning.Assessments.CodeAdapter/` | server-side code adapter |
| `apps/api/Source/GameGuild.API/` | host registration and product-specific bridges |
| `apps/api/tests/` | affected contract, domain, integration and architecture tests |
| `tools/emception/`, `packages/infrastructure/ui-emception/` | preserved upstream product and UI; read-only for this workstream |
| platform modules listed in root `AGENTS.md` | no coding-specific edits or dependencies |

## Dependency graph

Arrows mean "may import/depend on". This is the boundary relevant to this
workstream, not an invitation to remodel all existing application dependencies.

```text
apps/web -> coding-surface -> coding -> zod
                         -> grading-adapter-code -> coding
                                                 -> grading
                         -> public Emception APIs
                         -> declared editor/UI libraries
apps/web -> authorized API client and host persistence/command ports

API composition root -> Courses
                     -> Assessments
                     -> Assessments.CodeAdapter -> Assessments abstractions
domain modules -> platform modules

prototype fixture/test harness -> declared TS/Node/Playwright tools
production -X-> prototype fixture
grading -X-> coding, quiz, Emception or UI
coding -X-> grading, React, Next.js, browser storage or app routes
platform -X-> domain modules
coding-surface -X-> apps/web, Next.js, emception-ui or private Emception source
```

Callback ports invert control without reversing these imports. Existing
third-party dependencies are not authorization to install another library.
Check public exports and declared dependencies before planning their use.

## Mandatory atomic cycle

Every row in a subplan's atomic sequence executes the following cycle before
the next row. The work list describes requirements, not permission to skip it.

| Stage | Required evidence before advancing |
| --- | --- |
| Specification | user task, preconditions, expected result, failures, stable acceptance IDs and allowed files are recorded |
| Contract / schema | typed inputs, outputs, states, errors, ownership and invariants are defined; strict type/schema checks pass before behavioral implementation |
| Test | assertions are written against the specification; a missing/wrong behavior is shown to fail for the intended reason; dependencies/bootstrap errors are not useful red evidence |
| Minimal implementation | implement only that contract and the current row's behavior; reuse existing libraries; no opportunistic refactors |
| Quality gate | Auditor examines the diff and runs the exact applicable terminal commands; all selected required cases pass with exit 0; record evidence and remaining human/runtime requirements |

A behavioral test is expected to be red before implementation. Compilation,
schema or infrastructure failures must be classified separately. Tests use
independent expected values, public effects and negative cases, not copies of
the implementation's calculations. Freeze expected behavior before coding;
changing an oracle requires a specification/ADR update, never just a green run.

For design-only work, the contract is the prototype's explicit presentation
state and event model. It is not a second production domain or backend schema.
Simulated outcomes are labeled in the evidence and can never satisfy a
real-runtime or official grading gate.

## Blind terminal arbiter

[QUALITY-GATES.md](./QUALITY-GATES.md) defines commands and evidence layers.
Every atomic row names its gate command set. A future test file is an explicit
planned deliverable; a missing file, empty test selection, required skip,
timeout or nonzero exit leaves the gate open.

Terminal success is necessary. UX acceptance additionally requires inspected
screenshots and explicit human task review. Do not translate zero overflow,
a test count, screenshot existence or a simulated pass into usability approval.
Record terminal, runtime and human status separately in the single ledger.

An external failure is recorded with its reproducer and owner. It is not waived
or relabeled green; unaffected checks may proceed within their own boundary.
Do not broaden this workstream into unrelated authentication/economy fixes.

## Negative constraints

- No new explicit/implicit `any`, unvalidated unsafe casts or double assertions
  used to force a value through a contract. Use `unknown` plus real narrowing
  at untrusted boundaries. Do not weaken strict compiler settings.
- No new lint/TypeScript suppressions, suppression baselines, ignored changed
  files, disabled analyzers, `--noCheck`, ignored build errors or fake exit 0.
  Do not expand existing exceptions. Unrelated baseline debt stays visible.
- No weakening assertions, accepting private leaks, broad timeout increases,
  retries that hide a defect, `passWithNoTests`, or required test skips to pass.
- No package back-imports into the host, new platform/domain coupling, generic
  grading knowledge of coding types, or academic policy implemented in UI.
- No teacher secrets in a learner fixture, browser cache or preview. No real
  course data/credentials in committed screenshots or test fixtures.
- No manufactured artifact/provenance, permissive compiler identity, private
  upstream imports, parallel harness/compiler, or undeclared dependency.
- No hardcoded preview that ignores the edited draft, configuration forms in
  terminal output, or raw string fields substituting for the promised typed
  function interaction. Optional advanced raw input must roundtrip safely.
- No destructive family/environment reset without an impact summary and
  lossless cancellation. No Saved label before the relevant persistence ack.
- No simulated output presented as runtime evidence or local pass presented
  as official/released assessment feedback.

## Turn discipline and structural stop

Read context in tiers:

1. **Always:** applicable `AGENTS.md`, the current ledger checkpoint, this
   invariant/graph section and the active atomic row.
2. **For this row:** referenced ADRs, contract exports, relevant acceptance
   rows, current consumers and focused tests. Reuse already-read context.
3. **Only when needed:** upstream implementation, sibling domains, historical
   evidence and other subplans. Record the reason for extending the boundary.

Before edits, record the allowed files and the gate command. Preserve existing
worktree changes. Expand the file set only for a demonstrated dependency of
the current task; update the ledger before doing so.

When a structural ambiguity, requirement conflict or undeclared library need
is found, stop dependent implementation immediately. Record the conflicting
contracts/evidence and ask a concise question with alternatives. Do not invent
a new ownership rule or silently accept a scope reduction. Existing decisions
and explicit authorization remain valid; ordinary local choices inside those
boundaries do not require repeated approval. Independent read-only diagnosis
may continue while the question is pending.

If a later instruction would violate an invariant, do not generate the
violating change. Cite the rule and propose a compliant alternative. A genuine
change of product/ownership policy must be explicitly resolved and recorded
before dependent work resumes; this document is not a silent override of a
later deliberate user decision.

## Builder and Auditor

Builder implements the current row after its contract and test evidence.
Auditor then challenges ownership, negative constraints, missing states,
privacy, stale results and test-oracle validity, and runs its gate. One agent
may perform both passes sequentially; label this honestly as self-review,
not an independent reviewer or human acceptance. Auditor failures return the
same row to its earliest affected stage. Fix the cause; preserve the gate.

## Memory and handoff

[`../IMPLEMENTATION-STATE.md`](../IMPLEMENTATION-STATE.md) is this workstream's
required `state.md` ledger. Retain that established filename and a single
source of truth. [`../DECISIONS.md`](../DECISIONS.md) holds short numbered ADRs;
do not create another mutable tracker in this directory.

Keep the current checkpoint compact:

```text
Baseline: commit, dirty scope, relevant artifact identity
Active row / stage: specification | contract | test | implementation | audit
Allowed files and contract exports:
Acceptance IDs and gate command:
Contract result / behavioral red evidence:
Builder change / Auditor findings:
Terminal evidence: command, selected cases, exit, artifact path
Runtime evidence: observed | pending | blocked | not applicable
Human evidence: reviewed | pending (never inferred)
Open decision/blocker: evidence, owner, dependent work
Next exact action:
```

After each approved row, update this checkpoint and any affected ADR/consumer/
gap entry. Replace obsolete *current* instructions, link superseded decisions,
and retain compact historical evidence. Never erase an unresolved failure or
an immutable migration to make the context look cleaner.
