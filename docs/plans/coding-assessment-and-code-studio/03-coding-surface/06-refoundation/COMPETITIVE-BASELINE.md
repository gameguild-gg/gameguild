# Competitive baseline for coding-surface

Research date: 2026-11-19. Evidence: public first-party documentation, including
documented examples. No authenticated instructor usability benchmark was run.
Recheck sources when execution starts if their behavior affects a decision.

## Observations and local decisions

| Reference | Documented behavior | Decision for our product | Acceptance cases |
| --- | --- | --- | --- |
| [LeetCode cases](https://support.leetcode.com/hc/en-us/articles/32442719377939-How-to-create-test-cases-on-LeetCode) | prefilled examples, structured or raw custom inputs, Run distinct from Submit, compound input formats | keep experimentation separate from official submission; offer usable typed cases and investigate arrays/matrices | `CS-REF-03`, `04`, `14`, `18` |
| [HackerRank authoring](https://support.hackerrank.com/articles/2474669643-coding-question) | separate problem, languages, code stubs and cases; reference solution upload and candidate preview | use task-oriented authoring, a single function signature, protected reference material and a real learner preview | `CS-REF-01`, `02`, `05`, `19` |
| [Codio layout](https://docs.codio.com/instructors/authoring/guides/page_editing.html) | author chooses panels, visible files, terminal and preview tabs alongside learning content | compose tools according to the task; do not squeeze every activity into one fixed inspector or generic IDE | `CS-REF-10`, `11`, `17` |
| [Codio code tests](https://docs.codio.com/instructors/authoring/assessments/standard-code-test.html) | input/output authoring, checking a case against running code and reporting differences | support an understandable case editor and reference validation with expected/actual diagnostics | `CS-REF-01`, `05`, `13` |
| [CodeGrade assignment lifecycle](https://help.codegrade.com/automatic-grading-guides/python/create-your-first-python-assignment) | starter files are distinct from a test solution; instructors test an AutoTest snapshot before publishing it | separate starter/reference solutions and make validation of a specific candidate visible before publication | `CS-REF-05`, `06`, `15` |
| [CodinGame teaching example](https://files.codingame.com/codingame/learning/CodinGame_for_Teachers_Onboarding_Lesson.pdf) | a graphical activity driven by a program reading input and producing textual commands | distinguish graphics produced by the learner program from a host visualization of its behavior | `CS-REF-10`; custom instructional visualizers are future work |

These are product observations, not instructions to copy competitors' code,
branding, scoring policies, deployment model or every supported feature.
LeetCode is primarily a solving reference here; HackerRank's documented
author is a question creator, not evidence of a complete teaching LMS.

## Quality bar, not superficial imitation

- An instructor can author a complete problem without editing JSON or scrolling
  past a terminal to find test configuration.
- A learner can understand and solve it without navigating a full generic IDE.
- Both can tell what is saved, which code was executed and what a result means.
- A correct reference passes suitable cases; an intentionally defective
  solution is detected. Validate test usefulness, not only successful compilation.
- Input, expected output and actual output are inspectable for authorized cases.
- Long text, cases, files and graphics receive appropriate space; keyboard use
  and reduced container widths remain supported.
- Failed execution, unavailable infrastructure and an incorrect answer remain
  different states. Passing local tests is not an official grade.
- Resource use is measured. A runtime is not booted merely to edit form fields.

## Deliberate scope decisions

The first delivered families are function implementation and standard I/O
programs. Writing, completing, debugging and refactoring can be preparation
variants of those families. Projects and SQL require later model decisions;
they are not hidden behind misleading function or I/O labels.

Compound function values are a material benchmark gap, not cosmetic polish.
The current local harness tests explicitly reject array arguments and returns.
Determine whether a supported public path and official executor can provide
them before exposing UI controls. If not, record an upstream request and obtain
explicit acceptance of the limited algorithm scope; do not claim full LeetCode
parity. Do not edit Emception or encode arrays as unexplained strings to mask it.

Canvas integration uses existing graphics APIs, but a visualization does not
establish correctness. No custom game referee, algorithm animation engine or
pixel-based assessment is promised here. Academic assessment decisions remain
in grading even where a competitor puts them in its exercise editor.

## Traceability rule

For each adopted behavior, retain the source above, the relevant `CS-REF-*`
case, the design decision and the observed local evidence. Rejected ideas need
a short reason only when they materially affect scope. Do not accumulate
competitor screenshots or features without a corresponding user task.
