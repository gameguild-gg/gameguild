# 3.6.5. Learner and review experiences

Status: in progress; UX05-A1 offline case preparation is implemented, with
closing evidence in the shared ledger. Shared solving/execution (A2), actual
runtime/persona acceptance (A3), B and C remain open. Gate: `CODE-UX-05`.
Depends on `CODE-UX-04`, with the named entry exception below.

Specific entry exception: CS-ADR-050 allows independent core preparation while
the global preference offline finding awaits part 05 investigation. Carry the
exception explicitly; it neither closes C3 nor waives human acceptance, other
entry requirements or any learner/grading/security gate in this subplan.

## Invariants and boundaries

Apply [GOVERNANCE.md](./GOVERNANCE.md). Surface owns solving/inspection views
and local diagnostics; host/grading own submission, authorization and release.
Consume frozen/redacted data and existing recovery contracts. Reviewer views
cannot restore mutable learner drafts or imply release through local execution.

## Outcome

Deliver a problem-solving experience, not a read-only teacher editor or a full
generic IDE. The instructor's learner preview and the actual learner must share
presentation logic while retaining different authorized effects and scratch state.

## Atomic sequence and terminal gates

Each row completes Specification -> Contract/schema -> Test -> Minimal
implementation -> Quality gate; see [QUALITY-GATES.md](./QUALITY-GATES.md).

| Slice | Specification | Contract / schema | Test oracle | Minimal implementation | Quality gate |
| --- | --- | --- | --- | --- | --- |
| UX05-A | solve both families using samples/custom input and diagnose errors | shared learner/preview props, immutable samples, scratch and authorized result states | expected/actual display, no-oracle output, stale/cancel/error states; scratch never changes teacher/academic data | focused problem/editor/cases/results views with usable narrow navigation | contract compilation + G-SURFACE/G-WEB + G-RUNTIME for CS-REF-03/04/14 |
| UX05-B | recover edits and submit the exact visible snapshot once | scoped recovery and official-submit intent/ack/retry contract | refresh/actor switch, duplicate click, retry, later edits and no silent reset | host recovery/submission integration and distinct submitted state | G-SURFACE/G-WEB + G-RUNTIME for CS-REF-07/15; affected API cases |
| UX05-C | inspect frozen attempts and show only authorized released feedback | reviewer read-only props and host pending/withheld/released read models | later teacher edits cannot change reviewed work; unreleased data withheld; feedback survives runtime failure | reviewer and learner feedback composition with adaptive regions | G-WEB + G-RUNTIME for CS-REF-16 + applicable quiz-grading protocol; persona walkthroughs |

### UX05-A execution checkpoints

Boundary decision: [CS-ADR-051](../DECISIONS.md#cs-adr-051-prepare-learner-cases-independently-from-execution-and-grades).

1. **A1, offline case preparation:** define/compile public-only controlled
   practice state and component ports, then prove immutable public cases,
   personal copy/edit/duplicate/confirmed-delete, incomplete scalar buffers,
   missing versus empty expectations and context separation. Implement only
   the pure model and offline panel. Gate: strict package types, contract and
   component/model tests, architecture negatives, unsuppressed lint and affected
   existing consumers. This does not enable a run action or close runtime/API
   requirements; the current solver is not replaced by an unfinished panel.
2. **A2, solving composition and execution:** install the panel in the shared
   learner/preview workspace with focused problem/editor/case/result views.
   Reuse one existing execution owner. Specify and test known-expectation
   comparison versus no-oracle observation, including typed function input,
   cancellation, errors and stale results. No fabricated expected value,
   private reference solution, alternative executor or academic command.
3. **A3, acceptance:** prove actual UI/runtime behavior for both families and
   current-source host/persona integration using the existing bounded harnesses.
   Keep CS-REF-03/04/14, human approval and official grading distinct. An A1
   component/type pass cannot promote these later gates.

## Work

1. Compose the problem, examples and constraints alongside a focused editor.
   Use adjustable regions and tabs for files, cases, results and available
   outputs. Preserve task context when changing tabs; avoid forcing simultaneous
   statement, file tree, canvas, terminal and results into narrow columns.
2. For functions, expose the signature/starter and typed sample cases without
   requiring transport boilerplate. For I/O, make batch input and expected
   output explicit. Render only environment actions proven by 3.6.3.
3. Add private-to-the-learner scratch cases. These are experimentation data, not
   official teacher cases or new attempts. Without a permitted expected value,
   show observed output rather than a correctness verdict. Never evaluate
   scratch inputs by sending a hidden reference solution to the browser.
4. Keep Run program, Run tests and Submit separate and clearly scoped. Display
   diagnostics and authorized case differences; distinguish pending, running,
   cancelled, error, pass and fail. Show when results describe an older file
   state. No local result changes the official grading workflow.
5. Integrate draft recovery keyed by tenant, learner, exercise revision and
   attempt. Explain available recovery through status, not misleading server
   Saved labels. Reset requires confirmation; server conflict/revision change
   cannot silently overwrite work. Cancel outstanding runs before context changes.
6. Make the submit boundary deterministic: capture the current editable file
   state once, route through the official host command and preserve idempotent
   retry behavior. Prevent double submission and accidental edits during capture.
   Store/display the acknowledgement and distinguish submitted code from later
   local changes. Do not run a second local grading pass merely to submit.
7. Integrate pending review, withheld result and released feedback through
   existing grading read models. Refresh/new session uses server truth. Already
   released feedback remains readable even if a compiler artifact cannot load.
8. Build reviewer inspection around the frozen revision, submitted files and
   authorized evidence. Do not load mutable learner drafts as the submission.
   Review commands remain host/grading actions, and local inspection runs do
   not regrade implicitly. Keep the existing reviewer permission checks.
9. Ensure narrow layouts, keyboard navigation and focus restoration work in
   solving and inspection, not only in teacher authoring. A visible graphics
   panel must not make the problem or execution errors inaccessible.

## Evidence and gate

- `CS-REF-03`, `04`, `14`, `15` and `16` pass using independent instructor,
  learner and reviewer sessions. Do not substitute SystemAdmin for a learner.
- Both families cover an incorrect attempt, correction, local tests, official
  submission and refresh. User-editable code agrees with the captured payload.
- Scratch activity and author-test produce no official submission/review effects.
- Cross-actor/revision recovery, protected files and private-case redaction
  remain covered by negative tests from `CODE-UX-02`.
- Official pending/released states come from the API. Duplicate-click and
  retry cases do not create duplicate academic work.
- Review inspects the original revision after a teacher changes the current
  exercise. A failed execution is never converted into a zero-grade fallback.
- Runtime unmount/disposal and layout behavior remain verified when switching
  real learner activities, not only in an isolated component fixture.

Use focused Vitest/component checks first, then one-worker real-browser flows.
Follow the quiz-grading agent protocol for shared review/release/result paths;
do not alter authentication/MFA or unrelated suites to facilitate a coding test.

Update consumers, decisions and actual evidence in the part 3 trackers. Next:
[3.6.6](./06-web-integration-and-acceptance.md).
