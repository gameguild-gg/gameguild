# 3.6.1a. Prototype workflows and critical review

Status: interactive prototype and automated audit implemented; prototype design
explicitly approved by the user on 2026-11-20 inside `CODE-UX-01`.
Updated: 2026-11-20. This is not production cutover or runtime acceptance.

## Invariants, topology and dependencies

Apply [GOVERNANCE.md](./GOVERNANCE.md) before any work. The relevant graph is
prototype presentation -> explicit fixture contracts; test harness -> declared
TypeScript/Node/Playwright. Production never imports the prototype. The allowed
scope is this directory's HTML/evidence and isolated `prototype/` sources/tests,
plus shared state/ADRs. Runtime, API, database, auth and upstream source are
outside this design iteration.

The existing [product model](./PRODUCT-MODEL.md) and accepted `CS-ADR-014`
remain the ownership baseline. Product contracts are implemented in 3.6.2;
prototype contracts describe presentation states/events and synthetic examples.
No new library, general IDE, compiler, grading policy or scenario CLI.

The initial scalar-only function scope is explicitly accepted in CS-ADR-015;
arrays/matrices remain a tracked future capability, not a permanent exclusion
or an implemented feature. Design/contract work does not depend on an Emception
build or publication. Actual local artifact preflight belongs to `CODE-UX-03`
under `CS-ADR-024`; closing 01a does not establish runtime readiness.

## Findings to resolve

The 2026-11-19 review inspected the actual HTML in Chromium and rechecked the
primary sources in [COMPETITIVE-BASELINE.md](./COMPETITIVE-BASELINE.md).

| Finding | Observed problem | Required design outcome |
| --- | --- | --- |
| UX-F01, high | fixed read-only signature; generic text for function arguments | editable signature and corresponding typed cases; distinct I/O configuration |
| UX-F02, high | learner Cases contains only scratch input; no failure/result journey | sample cases, expected/actual diagnostics, execution states and distinct submission feedback |
| UX-F03, high | reference validation has no preparation/results; preview ignores edited text/source | correct/wrong solution authoring, candidate-bound validation and faithful redacted preview |
| UX-F04, medium | fixed columns/heights; actual Learning settings panel absent | adjustable/focused views under host constraints, including narrow layouts |
| UX-F05, medium | language/environment/graphics independently selectable without compatibility | supported combinations, good defaults, actionable unavailable states and separate output capabilities |
| UX-F06, medium | only a prefilled happy path; no draft/change/conflict recovery | blank-to-complete authoring plus error, persistence and destructive-change states |

Measured baseline: in a 1280x720 simulation with 256 px curriculum and 320 px
settings panels, central width was 704 px and expected-output input began at
y=796. At 390x844, learner result tabs began at y=1096 for the short specimen.
These are layout observations, not live Learning integration measurements.
The preview probe changed the problem to a maximum function but still showed
the original addition problem/starter. The earlier 69 checks did not validate
these workflows. Intentional disabled execution is not itself a defect; absent
interaction design and misleading preview are the issues to fix.

## Required presentation contracts

Specify and compile these before implementing interactions:

- A single incomplete author draft with stable file/case IDs, family-specific
  setup, statement, examples, constraints, starter/support files and protected
  reference/wrong-solution data. No canonical-definition assertion for blanks.
- Function signature: name, ordered named parameters and types, return type
  and source entry. Typed case editors derive from this signature.
- I/O cases: batch input, an explicit expected value or no oracle, intentional
  empty output and existing comparison semantics. Do not invent new comparators.
- Candidate-bound validation and run records, selected cases, observed outputs,
  expected values when authorized, and stale/idle/running/cancelled/error states.
- Separate preview scratch, learner scratch and frozen simulated submission.
  Preview must reflect the current draft while excluding protected material.
- Persistence states: unsaved, saving, acknowledged saved, failed/conflicted,
  and local recovery. Their simulation cannot imply an actual server write.
- Capability availability by evidenced family/language/environment/mode.
  Unavailable and optional-hidden are different states.
- Layout preferences: task, case/file selection, regions and focus, independent
  of execution/academic configuration; changes never discard authored content.

Keep these typed states small and disposable. Reuse existing domain vocabulary
where applicable, but do not preempt 3.6.2 by creating a competing production
schema. Deterministic simulated outcomes come from declared fixtures, not a
second implementation of a compiler or judge.

## Atomic sequence

Every row uses Specification -> Contract/schema -> Test -> Minimal
implementation -> Quality gate. Contracts must compile before behavior; write
the independent failing oracle before implementing its interaction. Gates
reference [QUALITY-GATES.md](./QUALITY-GATES.md).

| Slice | Specification | Contract / schema | Test oracle before behavior | Minimal implementation | Quality gate |
| --- | --- | --- | --- | --- | --- |
| `UXP-01` | declare teacher/learner tasks, field ownership and empty/error fixtures | typed presentation states/events, fixture identity, accepted case selection and harness lifecycle | malformed states are rejected; selected suite cannot pass with zero cases; prototype cannot import app/upstream internals | isolated fixture/test scaffold and accessible entry shell, no production cutover | G-PROT-contract; first harness lifecycle test; Auditor checks dependency graph |
| `UXP-02` | create function and I/O problems from blank; add, duplicate and delete cases | one signature, typed values, stable IDs, distinct I/O expectations, compatible environment choices | signature/starter/cases agree; duplicates get new IDs; empty differs from absent; unsupported combinations unavailable; edits survive task changes | teacher Problem/Environment/Starter/Tests interactions and compact case navigation | G-PROT-flow for UXP-01/02; inspect both families; resolves UX-F01/F05 |
| `UXP-03` | prepare protected reference/wrong solutions, validate and inspect learner view | isolated validation inputs, redacted preview projection, candidate identity and result invalidation | preview shows a newly authored max(a,b) problem/starter; private canary is absent from learner projection/storage; reference and wrong results differ; edits stale the result | preparation controls, per-case validation results, faithful preview and return-to-edit | G-PROT-flow for UXP-01..03; resolves UX-F03 |
| `UXP-04` | learner runs samples/scratch, diagnoses a fault, corrects, then submits | immutable samples, private scratch, typed result states, frozen acknowledgement and later-edit state | wrong answer shows expected/actual; compile error is distinct; custom no-oracle gives only output; cancel/stale work cannot overwrite current state; submit captures visible files once | learner cases/results and explicit run/cancel/submit/status transitions, all simulated | G-PROT-flow for UXP-01..04; resolves UX-F02 |
| `UXP-05` | preserve work through navigation, draft failures and layout changes | change impact/cancel/confirm; persistence/recovery transitions; selected task/focus/panel state | cancelled changes preserve exact values; failed save is not Saved; reopen restores simulated scoped draft; active controls reachable with both host panels; keyboard/focus survives resizing | recovery/error/conflict states, adaptive teacher layouts, focused learner navigation and optional graphics/terminal placement | G-PROT-flow for UXP-01..05; inspected stress captures; resolves UX-F04/F06 |
| `UXP-06` | challenge both complete journeys using different data and errors | frozen review checklist with acceptance IDs and unresolved-findings record | all required workflow cases run; no silent skips; review exposes fixture-vs-runtime boundary; check findings against evidence | fix root causes within the current design and update model/ADRs | full G-PROT-flow + G-DOC; adversarial Auditor pass; explicit human design review |
| `UXP-07` | represent the intended editor/file workflow before human acceptance | shared visual workbench, stable file IDs, open tabs, per-file caret/scroll and protected support | author creates/renames/removes files without losing edits; closing tabs never deletes files; learner buffers and read-only support survive navigation; inspect desktop/mobile | numbered/colorized editor specimen, explorer, file tabs and author file commands; no Monaco/runtime boot or new library | strict G-PROT-contract, red-before-green UXP-07 browser journeys, full regression gate and captures; human acceptance remains separate |

## Interaction and layout requirements

The source regions must resemble the planned Monaco-backed workbench, not
generic form textareas. Use the same explorer/tab/editor arrangement for
author, learner and preview; preserve buffers, selection and scroll per file.
Show read-only support and local modifications explicitly. Tab closure is
navigation, never file deletion. Author file creation/rename validates paths
and duplicates; deletion requires confirmation. Learner file creation/rename
is not inferred from editor access: this iteration retains supplied-file
permissions, rather than inventing a new product policy. Native editing plus
syntactic coloring/line numbers may simulate the editor; no claim of Monaco,
language-service or public runtime integration follows from visual fidelity.

Keep the five task views navigable in any order, with one shared draft. Show
incomplete/error state where it matters and a navigable readiness summary.
Do not turn the whole experience into a mandatory sequential wizard. Provide
defaults for routine setup, exposing advanced controls only when relevant.

Tests use a compact list with selection and an appropriately sized detail area;
support at least 20 synthetic cases and long names in the design stress fixture.
Small scalar values use suitable controls instead of unnecessarily tall text
areas. I/O/code/long statements retain the space they need. Public examples
and executable sample cases must have an explicit relationship; the author
must not unknowingly maintain contradictory copies.

**Validate exercise** shows where to edit the reference/wrong solutions and
which candidate and cases were tested. Its tab and heading must use that
explicit name (or a localized equivalent), never bare Validate/Validation.
Include an accessible-name assertion in UXP-03. This is authoring preparation,
not the Assessments/Grading evaluation of learner submissions.
Distinguish mismatch from compile/infrastructure error.
Preview reflects current edits and has a clear return path. Neither path
creates an official attempt or grants access to private material.

Learner views provide supplied samples and separate custom cases, with results
that explain input, expected/actual where authorized, and diagnostics. Academic
pending/released states are supplied by simulated host data; the surface does
not calculate policy. Distinguish the submitted snapshot from later local edits.

Graphics remains a capability, not a family. Prototype the supported-placement
and unavailable states, focus/resize/stop affordances and access to errors while
graphics is visible. A painted specimen is never real runtime/visual grading
evidence. Keep interactive terminal distinct from batch input and captured logs.

Inspect 1600x1000, 1280x720 and 390x844, with realistic host panel widths, each
panel open/closed, a long problem, multiline I/O and 20 cases. Reuse host sizing
contracts rather than hardcode the simulated widths into product components.
Assertions must cover reachability and focus, not only horizontal overflow:

- focused problem/code/results modes are available in constrained containers;
- the learner can switch code/results without scrolling the entire problem;
- executing/status controls remain reachable in the active work region;
- test selection and field edits survive panel/task changes;
- keyboard users can enter/leave regions and resize or use an equivalent
  focused layout; visible focus and labels remain intact;
- canvas, diagnostics and terminal never make each other inaccessible.

## Human walkthrough and acceptance

Recorded product response, 2026-11-20: the user explicitly approved the current
prototype design. This is design acceptance, not a claim that the user supplied
a task-by-task walkthrough log or that production acceptance passed. The
following journeys remain the review script, including the real application
review required later in CODE-UX-06.

Instructor: start blank, create a function with a different name/data from the
default, add public/private cases, introduce and repair an invalid value, prepare
reference/wrong solutions, validate, change the draft, inspect faithful preview,
simulate a save failure, recover and reopen. Repeat the distinct I/O portions.

Learner: read the actual preview, run a sample, inspect a wrong result, try a
custom input with no expected value, correct code, observe stale/cancel/error
states, submit, edit again and identify the submitted version and feedback.
Repeat key actions with constrained panels and keyboard navigation.

Record completion, confusion, unnecessary navigation and recovery. Human review
may reject a terminal-green design. Resolution of UX-F01..06 is required before
design approval; any scope change must be explicit, not an agent's waiver.
Do not label this as authenticated competitor benchmarking or live E2E.

## Reviewable implementation

Open [layout-prototype.html](./layout-prototype.html) directly. The Study controls
select blank/function/I/O/20-case examples and simulated failure responses.
They collapse on narrow screens. The five author tasks and the independent
learner/preview views use current draft data. See the
[frozen oracle checklist](./prototype/REVIEW-CHECKLIST.md).

UXP-01..06 retain their earlier terminal evidence. The subsequent UXP-07
editor-fidelity iteration adds a shared visual workbench for starter code,
reference/incorrect solutions and learner/preview files. It includes colored
source, line numbers, cursor status, real editable buffers, file tabs, an
explorer, and author create/rename/confirmed-delete commands. Closing a tab
does not delete its file. Per-file caret/scroll and code changes are retained;
supplied read-only support remains protected in learner/preview contexts.

The current gate selects 14 Node contract tests and 16 sequential browser
journeys, with no required skips. Same-agent Auditor and capture inspection
are recorded in the shared ledger, separately from human acceptance. These
editor interactions are a visual/native-text specimen, not loaded Monaco,
language services, a VFS or a real execution engine. No dependency was added.

This specimen uses strict JSDoc-checked scripts plus TypeScript presentation
contracts. It consumes no production module, server or Emception runtime.
Only declared synthetic source/case combinations have recorded outcomes.
Unknown combinations report an unrecorded response. Sample execution, canvas
pixels, echo input and submission acknowledgements are design evidence only.
Unrecorded file sets/support contents also cannot reuse an example verdict.

Saved/recovery data lives in scoped, cloned in-memory stores for the open tab.
**Simulate reload** and **Reopen saved draft** model recovery; an actual browser
refresh starts the study again. Nothing private is written to browser storage.
Durable authorized recovery and API concurrency belong to 3.6.2 onward.

The explicit design approval is recorded above. No additional human task
observations were supplied. Baseline documentation/contract closure is separate
from actual asset preflight in `CODE-UX-03`; production human/runtime review
remains required.

## Handoff

Update `PRODUCT-MODEL.md` with the accepted interaction model and inspected
captures; update the shared ledger and ADRs instead of another state file.
Map UXP evidence to `CS-REF-*` as design evidence only. Prototype work does not
promote runtime capabilities or replace production acceptance.

Return to [3.6.1](./01-product-model-and-evidence-baseline.md) for its baseline
closure audit and runtime-preparation handoff. The initial compound-value
disposition is recorded in CS-ADR-015. Then proceed through 3.6.2..3.6.6,
preserving the accepted behaviors while replacing simulated ports with real
host/runtime contracts. Reuse justified test scenarios, not the prototype's
throwaway implementation as a new architecture constraint.
