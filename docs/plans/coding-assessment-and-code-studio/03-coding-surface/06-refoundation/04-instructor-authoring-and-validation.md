# 3.6.4. Instructor authoring and exercise validation

Status: independent UX04-A package task views implemented and locally validated
on 2026-11-23 (CS-ADR-044). UX04-B/C and whole `CODE-UX-04` acceptance remain
open. Final phase entry/acceptance still depends on `CODE-UX-03`; advancing this
independent offline row does not waive its runtime or human gates.

## Invariants and boundaries

Apply [GOVERNANCE.md](./GOVERNANCE.md). Implement the accepted 01a interactions
inside owned surface/host boundaries using 3.6.2 contracts. Forms start no
compiler; reference data stays protected; academic settings stay with grading.
Do not copy prototype state into a competing persistence/schema layer.

## Outcome

An instructor creates and verifies a complete coding exercise without needing
JSON, hidden IDE controls or knowledge of the current component implementation.
The starting point is the teaching task, not a preloaded graphics demo.

The [deferred instructor proposals](../IMPLEMENTATION-STATE.md#deferred-corrections-and-instructor-controls---2026-11-22)
cover supplied-versus-learner-written code, file freedom and editor assistance.
Before implementing them in UX04-A, specify compatible choices and complete the
affected UX02 contracts/tests. Recording these proposals does not approve their
defaults, add a new exercise family or move grading policy into this surface.

## Atomic sequence and terminal gates

Each row completes Specification -> Contract/schema -> Test -> Minimal
implementation -> Quality gate; see [QUALITY-GATES.md](./QUALITY-GATES.md).

| Slice | Specification | Contract / schema | Test oracle | Minimal implementation | Quality gate |
| --- | --- | --- | --- | --- | --- |
| UX04-A | blank-to-complete authoring for both families with compatible defaults | task props/events, signature-derived fields and case operations over accepted draft | typed edits/duplicates/deletes, field errors, no lost navigation state and no runtime boot | Problem/Environment/Starter/Tests task views and compact case editor | contract compilation + G-SURFACE + affected G-WEB + G-AUTHORING; inspect adopted UXP-02 behavior |
| UX04-B | prepare and validate correct/wrong solutions; faithful learner preview | candidate-bound validation and redacted preview ports with isolated scratch | reference passes, wrong solution fails, edits stale results; non-default preview reflects draft without private canary | validation preparation/results and shared solving preview | G-SURFACE + G-WEB + G-RUNTIME for CS-REF-05/19 |
| UX04-C | save/reopen, change impact, conflict recovery and constrained layouts | existing persistence/status ports and lossless transformation intents | save failure is not Saved; cancel preserves input; keyboard/focus and task controls survive panel changes | real persistence/recovery integration and adaptive authoring composition | G-SURFACE + G-WEB + focused G-RUNTIME for CS-REF-01/02/08; instructor walkthrough |

## Work

UX04-A exposes `CodingAuthoringSurface` over the existing incomplete draft; its
actual-component proof is not the Learning production cutover. The existing host
forms remain until UX04-B/C supply exercise validation and acknowledged host
persistence. Their removal and final save/reopen/learner-preview walkthrough are
still required, not silently dropped because package tests pass.

1. Implement a dedicated authoring surface with a simple exercise-family
   selector and task views: Problem, Environment, Starter code, Tests, Validate
   exercise. The last task checks exercise preparation, not learner grading;
   keep its explicit name in the tab, heading and localized labels.
   Use the accepted draft and typed family registry. Shared state survives view
   changes; navigation is not restricted to a linear wizard.
2. Provide space according to the task: full-width problem/case editing, a
   selected-case inspector beside code where useful, and a preview-focused view.
   Panels resize/collapse without losing the selection or text. Consume the
   available Learning content area; do not rebuild global navigation or nest
   decorative cards around every work area.
3. Problem editing covers statement, examples, constraints and permitted
   supporting material using the repository's existing content primitives. Show
   the same structured examples and instructions in the learner preview. Do not
   show only an entry-point summary where the problem belongs.
4. Environment editing separates language from runtime/libraries and explains
   unavailable configurations through actionable states. Generate a suitable
   starter for the chosen family/environment when supported. Never replace
   authored files with a preset implicitly or enable an untested combination.
5. Starter-code editing exposes file names, editable/read-only status and allowed
   learner-created files. Keep reference solutions separate, protected and easy
   for the instructor to inspect. Author-test runs a copy; it never replaces the
   learner starter with the correct answer.
6. Implement type-specific configuration and cases as described below. Keep
   technical expected results distinct from grading settings. Existing academic
   controls may be composed from grading through explicit ports; do not create
   a second score/review policy implementation in coding-surface.
7. Validate the current candidate against its reference solution and one or
   more deliberately wrong solutions. Report failures by case, including
   infrastructure failures, and invalidate success after changes. A reference
   solution is a powerful authoring aid, not a hidden requirement for manual
   review activities. Never manufacture expected output when no oracle exists.
8. Provide a separate learner-preview action using the same learner components
   and redacted projection, with independent scratch files. Teacher full-case
   validation is not learner preview and must not share private VFS/storage with
   it. Neither action enrolls, submits, grades or releases anything officially.
9. Integrate host persistence ports, dirty/saving/saved/error/conflict states,
   retry and recovery. Confirm destructive type/environment/signature/reset
   actions with a concrete impact summary; cancellation is lossless. Use field
   errors and a navigable summary without moving the whole workbench vertically.

## Family-specific configuration

| Concern | Function | Standard I/O |
| --- | --- | --- |
| Setup | one signature, named parameters, return type, entry source | entry source and applicable execution settings |
| Starter | function stub matching the signature; permitted surrounding code | suitable program scaffold; not an unrelated canvas demo |
| Cases | typed arguments and expected return aligned to the signature | stdin, explicit expected stdout, optional stderr/exit status |
| Editing | structured inputs; optional raw view only if it roundtrips safely | multiline fields showing whitespace/line boundaries when relevant |
| Validation | signature changes reconcile cases atomically | absent expectation differs from intentional empty output |

Both families need compact case navigation, meaningful names, duplicate/delete,
public/private indication, per-case diagnostics and edge-case coverage. Avoid
exposing irrelevant test kinds. Function console diagnostics may still be useful;
restricting case kinds must not hide all stdout/stderr from a function run.

Do not introduce broad matching policies such as regex/tolerance merely because
a competitor offers them. Existing comparison semantics must be visible and
correct; a new comparator requires domain/runtime/server parity and tests.

## Evidence and gate

- `CS-REF-01`, `02`, `05`, `08` and `19` pass on both fresh and reopened data.
- Unit/component tests cover drafts, field errors, keyboard/focus, case selection,
  duplication and cancelled transformations; no backend/compiler is needed just
  to edit those fields.
- Browser tests create each family through the real UI, save and reopen it,
  validate a correct and an incorrect solution, then open redacted learner preview.
- A small instructor walkthrough verifies task completion without JSON,
  misplaced controls or loss of work. Record pending human feedback honestly.
- The host no longer owns family-specific form components or injects them into
  a generic output/explorer slot. Remove superseded owned code only after its
  consumers have moved; keep unrelated consumers and upstream code intact.

Update the part 3 trackers with actual commands and observations. Next:
[3.6.5](./05-learner-and-review-experiences.md).
