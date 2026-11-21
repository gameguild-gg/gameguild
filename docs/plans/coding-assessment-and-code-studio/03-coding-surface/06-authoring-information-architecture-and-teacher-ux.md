# Part 3.6. Coding surface refoundation

Status: in progress; `CODE-UX-01` closed on 2026-11-21. The 3.6.1a interactive prototype
and its automated audit are implemented. The user approved the prototype
design and initial scalar-only function scope on 2026-11-20. The baseline
documentation/contract audit passed. UX02-A contracts and UX02-B persistence
preparation are implemented/tested in scope; their broader G-API gate remains
open. UX02-C metadata/immutable revision and targeted worker safety preparation
are tested in scope; full configuration/wire parity, memory disposition,
official client generation, the historical-worker gate and production
integration are not complete. The single ledger records current evidence.
Compatible 4.5.2 assets
are published; local synchronization and execution checks belong to `CODE-UX-03`,
not to a mandatory Emception build or IDE publication.
Production refoundation and product acceptance are not complete.

Updated: 2026-11-21.

Gate IDs: `CODE-UX-01` through `CODE-UX-06`.

This file retains its existing path for incoming references. It replaces the
earlier teacher-inspector plan, not the separate roadmap part 06 about Code
Studio. The six previously planned gates are redefined below; none had been
completed when this plan was rewritten. Historical test results do not certify
this new scope. Current evidence is in
[`PRODUCT-MODEL.md`](./06-refoundation/PRODUCT-MODEL.md) and the shared state.

## Governing invariants and execution protocol

The mandatory [Meta-Harness governance](./06-refoundation/GOVERNANCE.md)
applies to this coordinator and every subplan, including the current prototype
remediation. It defines directory ownership, the one-way import graph, negative
constraints, structural Stop & Ask, Builder/Auditor separation and the single
state/ADR handoff. Root/app `AGENTS.md` and existing Emception/grading/data
boundaries remain authoritative.

Every atomic slice follows Specification -> Contract/schema -> Test -> Minimal
implementation -> Quality gate. Contract validation precedes behavioral code.
Use [terminal gate commands](./06-refoundation/QUALITY-GATES.md); missing tests,
required skips and nonzero exits cannot close a gate. Terminal success, real
runtime evidence and explicit human acceptance remain separate requirements.

## Product mandate

Rebuild coding-surface around authoring and solving coding exercises, not
around preserving the current IDE wrapper. The target is the quality of the
selected competitor workflows in
[`COMPETITIVE-BASELINE.md`](./06-refoundation/COMPETITIVE-BASELINE.md), measured
by the local [acceptance matrix](./06-refoundation/ACCEPTANCE-MATRIX.md).
This is not a promise of complete feature parity with every competitor.

The instructor must be able to describe a problem, prepare a working
environment and starter code, define useful cases, and validate the student
experience. The learner must understand the task, experiment, diagnose results,
preserve work and submit a specific solution. A reviewer must inspect that
submitted solution and its authorized evidence without changing the attempt.

The existing UI, component names, props, state shape and test snapshots are
evidence, not product requirements. They may be replaced where they obstruct
these workflows. Reuse code when it fits the target model, not to defend sunk
implementation effort. Characterization tests protect valid behavior and
security boundaries; they must not freeze a known UX defect.

## Scope and boundaries

| Owner | Responsibility | Refoundation rule |
| --- | --- | --- |
| `@game-guild/coding` | exercise meaning, validated definitions, delivery and submission contracts | evolve owned contracts when required by the accepted model |
| `@game-guild/coding-surface` | task-oriented authoring, solving and inspection surfaces; local execution orchestration | replace components, generic slots and state structures when necessary |
| `@game-guild/grading-adapter-code` and backend CodeAdapter | translate coding evidence into generic grading contracts | change only to support the accepted coding model, with parity tests |
| `apps/web` and domain write/read paths | auth, tenant, persistence, revisions, routing and authorized commands | integrate the new surface, including necessary domain changes |
| grading | academic scores, review policy, release and official attempts | preserve ownership; no policy engine in coding-surface |
| Emception public APIs | runtime, VFS, toolchains, execution and graphics | consume, do not fork or remodel the upstream product |
| `@game-guild/emception-ui` | independent existing product and its consumers | leave package, aliases and unrelated consumers unchanged |

This is not a frontend-only workaround mandate. If a required concept cannot
roundtrip through the API, fix the owned contract and domain integration, not
an ephemeral UI field. Do not add speculative V2 contracts, aliases or duplicate
adapters. Inventory real published revisions before changing interpretation;
do not assume all existing data is disposable. Preserve historical migrations.

No new table is presumed necessary. A demonstrated storage change follows the
roadmap `SCHEMA-GATE`, includes the actual migration and upgrade validation, and
does not authorize changes to platform modules. An Emception gap follows
`EMCEPTION-GATE`; rewriting our surface does not authorize editing upstream.

## Product model to establish

Separate the following axes explicitly:

| Axis | Examples | Must not determine |
| --- | --- | --- |
| Exercise family | implement a function; program with standard input/output | review policy or language |
| Pedagogical preparation | write, complete, debug or refactor starter code | a new persisted type merely because the instructions differ |
| Language and environment | C++; compiler/runtime plus SDL or raylib | problem type inferred from file extension or preset |
| Execution capability | function tests, batch stdin, interactive stdin, graphics output | academic approval |
| Presentation | statement, files, diagnostics, results, terminal, canvas | access permission |
| Academic workflow | attempts, scoring, review and release | workbench layout |

Deliver `function` and `standard-io` first because they cover two distinct
problem-solving workflows. Neither is a fallback for an unsupported activity.
Projects and SQL are future families requiring their own contracts and gates.
Do not publish placeholders or force an interactive project into standard I/O
solely to make it selectable. Debugging is normally a starter-code variant;
canvas is normally an output capability, not a third family.

Function signatures, typed cases and future compound values must be modeled
deliberately. The current scalar-only implementation is not the quality target
for all algorithm exercises. Array/matrix support requires a public-runtime and
server feasibility decision in `CODE-UX-01`; a blocker must remain visible with
an owner and explicit product disposition, not silently become a permanent UX
restriction. See `CS-REF-18` in the acceptance matrix.

## Target experiences

### Instructor

Provide task views for **Problem**, **Environment**, **Starter code**, **Tests**
and **Validate exercise**. Localized labels must also explicitly name the
exercise. These are navigable views with a shared draft, not a mandatory
one-way wizard.

- Problem covers statement, examples, constraints and learner-facing resources.
- Environment separates language from runtime and compatible tools.
- Starter code distinguishes editable files, supplied support files and
  teacher-only reference material. Prototype this with the intended explorer,
  file tabs and source editor composition, not generic source form fields;
  closing a tab is independent from deleting a file. Production editor/runtime
  integration is still separate from visual design evidence.
- Tests provides a compact case list and type-specific editor, duplication,
  public/private visibility and useful validation errors.
- Validate exercise checks reference and deliberately incorrect solutions,
  then opens the actual learner presentation with redacted data. This prepares
  the exercise; it does not evaluate learner submissions, create official
  attempts, assign grades or release results. Those remain in Assessments/Grading.

Use the available central workspace. Text authoring and large case tables can
use full width; configuring a selected case can sit beside code. Resizable,
collapsible panels are tools, not a requirement to fit every task in a
360-420 px inspector. Console and test results never contain authoring forms.

### Learner and reviewer

- Problem, examples and constraints remain reachable while coding.
- Code, cases and results have stable, resizable regions or focused tabs.
- Run program, run tests and submit are separate actions where applicable.
- Personal scratch cases do not mutate the teacher's cases or official state.
- Refresh recovers the correct draft; reset and environment changes do not
  silently overwrite work; a submitted snapshot is distinguishable from edits.
- Review uses the frozen submission and authorized evidence, not the latest
  local learner files or current mutable exercise definition.
- Local results never impersonate an official grade or bypass release policy.

### Execution resources

Distinguish output logs, interactive terminal input and graphics rendering.
Reuse public Emception capabilities and existing editor/terminal libraries.
Do not create another compiler, filesystem, shell, graphics runtime or judge.

Runtime support, exercise requirements, assignment configuration and authorized
mode jointly determine available actions. A boolean on an exercise profile is
not proof of capability. A runtime that requires a graphics target still needs
that target when its panel is minimized. Visual output is not automatically
machine-gradable; no pixel judge or instructional algorithm visualizer is part
of this cut.

Do not start a runtime to edit a statement or test form. Execution must support
bounded output, cancellation, stale-result invalidation and cleanup. Measure
memory and startup costs; do not accept an attractive layout that leaks workers
or launches a second runtime for each tab.

## Subplans and order

| Order | Subplan | Gate | Required result |
| --- | --- | --- | --- |
| 3.6.1 | [Product model and evidence baseline](./06-refoundation/01-product-model-and-evidence-baseline.md) | `CODE-UX-01` | accepted workflows, ownership, capability matrix and runtime preparation contract |
| 3.6.1a | [Prototype workflows and critical review](./06-refoundation/01a-prototype-workflows-and-critical-review.md) | required design checkpoint inside `CODE-UX-01` | typed, stateful teacher/learner journeys; six review findings resolved; human design review |
| 3.6.2 | [Domain, drafts and data boundaries](./06-refoundation/02-domain-drafts-and-data-boundaries.md) | `CODE-UX-02` | coherent contracts, recoverable drafts, revisions and private/public projections |
| 3.6.3 | [Workbench, runtime and capabilities](./06-refoundation/03-workbench-runtime-and-capabilities.md) | `CODE-UX-03` | tested execution foundation, graphics/terminal integration and resource lifecycle |
| 3.6.4 | [Instructor authoring and exercise validation](./06-refoundation/04-instructor-authoring-and-validation.md) | `CODE-UX-04` | teacher can create, validate, save and reopen both exercise families |
| 3.6.5 | [Learner and review experiences](./06-refoundation/05-learner-and-review-experiences.md) | `CODE-UX-05` | solve, experiment, recover, submit and inspect the correct snapshot |
| 3.6.6 | [Web integration and acceptance](./06-refoundation/06-web-integration-and-acceptance.md) | `CODE-UX-06` | integrated representative journeys, human acceptance and explicit handoff |

Each production gate requires its predecessor. The 3.6.1a iteration belongs
inside the active first gate. Product/contract work in `CODE-UX-01/02` does not
require a compiler build, a new upstream release or a successful runtime boot.
Real execution remains required in `CODE-UX-03` and subsequent runtime gates;
it is not waived by design approval or contract checks.
Small feasibility probes and test infrastructure preparation belong in 3.6.1;
do not implement later production phases under the label of discovery. Each
subplan's atomic sequence uses the mandatory cycle and named command gates.

## Remove the validation dependency cycle

`CODE-UX-01` records the canonical fixture, frozen runtime binding, lightweight
preflight tests and resource budget. Before real runtime checks in `CODE-UX-03`,
synchronize the compatible published assets and pass actual local preflight.
Do not postpone this preparation until part 4.0 or require it before the pure
domain/draft work in `CODE-UX-02`. This does not close `CODE-UI-07` by itself.

Consume the existing public Emception runtime; do not publish its independent
IDE or modify its CI to develop coding-surface. Version 4.5.2 is already
published and its manifest passed the frozen binding check on 2026-11-20.
The local schema-1 copies still need replacement through a backed-up,
verified synchronization. `CS-ADR-024` defines this distinction and the
conditional exact-source CI path. A local toolchain build is a fallback only
when compatible artifacts cannot be obtained, not a planned prerequisite.

Component-level layout tests can run without a compiler artifact, but they do
not prove execution. An unavailable artifact blocks the affected runtime gate;
it is not replaced by a permissive manifest or fake successful execution.

After `CODE-UX-06`, part 4.0 reconciles/revalidates the current branch and closes
the remaining official lane evidence. It no longer owns prerequisites that
3.6 needs before it can finish. Part 4 retains the broad scenario catalog,
security matrix, web lab and end-to-end grading acceptance. The PR strategy for
parts 01-04 remains unchanged.

## Context and evidence protocol

Use tiered context from the governance: current ledger/invariants/active slice
first, then only its contracts, acceptance rows, ADRs and consumers. Expand to
upstream/history on demonstrated need. Do not reload every plan in every turn
or create competing state trackers.

- Record product and contract decisions in [DECISIONS.md](./DECISIONS.md)
  before coding.
- Keep gate status, commit, dirty worktree, commands, outcomes, unresolved
  decisions and exact next action in
  [IMPLEMENTATION-STATE.md](./IMPLEMENTATION-STATE.md).
- Update [CONSUMER-MAP.md](./CONSUMER-MAP.md),
  [EMCEPTION-CAPABILITY-GAPS.md](./EMCEPTION-CAPABILITY-GAPS.md) and the roadmap
  [duplication register](../DUPLICATION-REGISTER.md) whenever an ownership or
  reuse decision changes.
- Reference stable `CS-REF-*` cases in tests and evidence. The acceptance matrix
  is a plan, not a generated claim that a capability passed.
- Record before/after screenshots and inspect them; sanitize traces and use
  synthetic data. Never commit real reference answers, private cases or secrets.
- Complete the Builder pass, then audit against the negative constraints and
  execute the slice's terminal gate. Update the compact current checkpoint;
  preserve historical evidence while removing obsolete next-action instructions.

The tracker is shared with the rest of part 3 so another executor can resume
without this chat. Subplans in `06-refoundation/` reference these same files,
not their own copies.

## Acceptance and exclusions

Completion requires the matrix's required cases, human teacher and learner
walkthroughs, resource evidence and preserved security/academic boundaries.
A decision-gated capability needs implemented evidence or explicit owner/product
acceptance of a tracked limitation; an agent cannot downgrade it to optional
merely to finish. Do not claim broad competitor parity for a deliberately
restricted exercise set.

The initial layout specimen was replaced by a typed interactive design study.
Its 30 automated checks cover the six findings and editor composition, with
inspected desktop/mobile captures. The user explicitly approved its design
on 2026-11-20; preserve that decision without inventing task-by-task human logs
or repeating prototype work. See
[3.6.1a](./06-refoundation/01a-prototype-workflows-and-critical-review.md).
Simulated behavior does not establish runtime, API persistence or official
grading. Production phases and their human/runtime acceptance remain gated.

Excluded: new languages, a new general IDE, Code Studio/Lexical, a replacement
grading engine, plagiarism/proctoring, AI assistants, multiplayer, private tests
in learner browsers and destructive Emception changes. Protect these boundaries,
not the accidental structure of our current surface.
