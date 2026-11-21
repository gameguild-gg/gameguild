# Refoundation acceptance matrix

Status: planned requirements, not executed results. Updated: 2026-11-20.

These IDs are stable requirements for part 3.6, not a second executable
capability catalog. Part 4 maps them to its catalog and reuses evidence rather
than creating conflicting statuses. Record actual results in the part 3
`IMPLEMENTATION-STATE.md`, with commit, persona, test, evidence and reviewer.

`Required` means no completion without evidence. `Decision-gated` means either
implemented evidence or an explicit product/owner-approved limitation with a
tracking reference. The implementing agent cannot unilaterally defer it.

Product disposition recorded on 2026-11-20: the user explicitly approved the
prototype design and initial simple-type function scope. CS-REF-18 uses its
accepted-limitation branch, tracked by CS-ADR-015 and EMC-GAP-001. This does
not mark arrays/matrices as supported or any production/runtime requirement
as passed. CODE-UX-06 human acceptance remains separate.

| ID | Requirement and observable oracle | Primary gate | Evidence | Scope |
| --- | --- | --- | --- | --- |
| `CS-REF-01` | Instructor authors an I/O exercise, edits/duplicates cases, validates, saves and reopens without lost data or console forms | `CODE-UX-04` | component + real browser + human | Required |
| `CS-REF-02` | Instructor defines one function signature; cases align with it; generated starter and reopened definition agree | `CODE-UX-04` | contract + component + real browser + human | Required |
| `CS-REF-03` | Learner reads the I/O problem, runs sample/custom input, diagnoses results and sends a distinct official submission | `CODE-UX-05` | real browser + authorized API | Required |
| `CS-REF-04` | Learner solves a function without writing transport boilerplate; typed cases and the submitted code agree | `CODE-UX-05` | real browser + authorized API | Required |
| `CS-REF-05` | A correct reference passes; a deliberately wrong solution fails relevant cases; validation uses the current candidate and becomes stale after edits | `CODE-UX-04` | real runtime + browser | Required |
| `CS-REF-06` | Statement, examples, constraints and execution configuration roundtrip and stay bound to the revision used by an attempt | `CODE-UX-02` | TS/backend parity + persistence integration | Required |
| `CS-REF-07` | Incomplete instructor work and learner edits recover after refresh within their declared persistence guarantees; actors/tenants/attempts never mix | `CODE-UX-02` | storage/component + API where applicable + browser | Required |
| `CS-REF-08` | Type/environment/signature changes and reset show impact; cancel is lossless; confirm is atomic; no stale successful result survives | `CODE-UX-04` | component + browser | Required |
| `CS-REF-09` | Learner delivery, responses, logs and storage contain no private cases or teacher reference solution; arbitrary uploads cannot replace protected files | `CODE-UX-02` | negative contract/API + browser payload assertions | Required |
| `CS-REF-10` | A supported existing graphical environment visibly renders and responds in our workbench; resize, stop and reopen work without changing exercise family or upstream UI | `CODE-UX-03` | real runtime + inspected desktop/mobile captures + pixel/interaction checks | Required |
| `CS-REF-11` | Interactive input reaches a supported program, while batch stdin remains deterministic; logs alone are never presented as an interactive terminal | `CODE-UX-03` | real runtime + browser | Required |
| `CS-REF-12` | Form editing starts no runtime; only one execution per surface is active; cancellation/unmount/repeated mounting releases workers and graphics sessions | `CODE-UX-03` | lifecycle tests + measured browser/process evidence | Required |
| `CS-REF-13` | Authorized cases show meaningful expected/actual differences, including intentional empty output; compile error, timeout and executor failure are not wrong-answer verdicts | `CODE-UX-03` | contract + runtime + browser | Required |
| `CS-REF-14` | Learner scratch cases never mutate teacher cases; absent expected values produce observed output, not fabricated correctness; no official attempt is consumed | `CODE-UX-05` | component + API/event assertions | Required |
| `CS-REF-15` | Official submit freezes the visible file state, survives safe retry and cannot create duplicate academic work; later draft edits do not rewrite the submission | `CODE-UX-05` | browser + official API | Required |
| `CS-REF-16` | Reviewer inspects the frozen attempt; learner sees only released results after refresh/new session; editor/runtime failures do not hide an already released result | `CODE-UX-05` | isolated persona browsers + authorized API | Required |
| `CS-REF-17` | Author and learner layouts work with surrounding panels open/closed; keyboard navigation, focus, labels, resizing and narrow layouts remain usable | `CODE-UX-06` | component + inspected viewport screenshots + human tasks | Required |
| `CS-REF-18` | Array/matrix function cases have a coherent typed model and proven browser/server execution, or an explicitly accepted upstream limitation and follow-up | `CODE-UX-01` decision; `02`/`03` implementation if feasible | public API feasibility + conformance evidence or approved disposition | Decision-gated |
| `CS-REF-19` | Instructor learner-preview uses the same solving components with redacted data and independent scratch state; private validation remains a separate tool and has no academic effects | `CODE-UX-04` | projection + browser + API/event assertions | Required |
| `CS-REF-20` | Preserved Emception UI and unrelated consumers remain untouched; coding changes trigger consumer validation without forcing a producer release | `CODE-UX-06` | diff/import/CI classification + regressions | Required |
| `CS-REF-21` | Canonical fixture and frozen-binding local artifact preflight are ready before real runtime checks; wrong identity fails closed; no compulsory source build for unchanged-runtime consumers | `CODE-UX-01` preparation contract; `CODE-UX-03` actual assets/preflight | harness/preflight tests + pinned artifact integrity and conditional CI provenance | Required |
| `CS-REF-22` | Owned contract changes agree across UI, API, snapshots and adapters; any actual storage change includes its migration and upgrade coverage | `CODE-UX-02` | parity/integration/architecture + migration tests if applicable | Required |

## Prototype review mapping

The [Meta-Harness](./GOVERNANCE.md) governs all evidence. The current
[01a iteration](./01a-prototype-workflows-and-critical-review.md) resolves
design findings before production implementation. Its UXP IDs are atomic
design slices, not new canonical capability IDs or replacement CS-REF statuses.

| Review finding | Prototype slices | Production acceptance retained |
| --- | --- | --- |
| UX-F01: generic/incomplete family authoring | UXP-01/02 | CS-REF-01/02/06/08 |
| UX-F02: missing learner diagnosis cycle | UXP-04 | CS-REF-03/04/13/14/15/16 |
| UX-F03: incomplete validation and unfaithful preview | UXP-03 | CS-REF-05/09/19 |
| UX-F04: host-constrained space and navigation | UXP-05 | CS-REF-10/11/17 |
| UX-F05: incompatible environment choices | UXP-02/05 | CS-REF-08/10/11/18 |
| UX-F06: absent incomplete/error/recovery states | UXP-01/05 | CS-REF-07/08/12/15/16 |

UXP-06 audits all six findings and the complete human journeys. Automated
prototype results establish only the simulated interaction contract. Required
runtime/API/production browser layers above remain pending until executed.

## Representative fixtures

Use simple, synthetic exercises whose expected behavior is easy to inspect:

1. Function: add two integers, including zero and negative inputs. This tests
   the workflow, not the whole algorithm catalog. Add a separate list/matrix
   example only when `CS-REF-18` is implemented.
2. Standard I/O: read two numbers and print their sum, with public and private
   cases, a deliberately wrong implementation and an intentional empty-output
   case in a suitable separate fixture.
3. Graphics/interactive capability: a small existing-runtime program with
   predictable visible output and an observable input action. Prove each allowed
   family/runtime combination before publishing it. A graphics demo alone does
   not prove official grading support or justify inventing a `visual` type.

Reference and private data in committed test fixtures must be synthetic.
Runtime captures from real courses must be sanitized; do not log complete
teacher payloads to establish redaction assertions.

## Human acceptance

Record separate instructor and learner walkthroughs, not only an admin demo.
Each covers one new exercise and one reopened exercise, a mistake/recovery,
the correct expected outcome and a narrow layout. Note task completion,
confusion, data loss, excessive navigation and actionable failures. A polished
screenshot alone does not pass a workflow.

Automated evidence may establish correctness, but only explicit human review
closes the usability acceptance in `CODE-UX-06`. Pending human review remains
pending, not implicitly approved by the implementing agent.
