# Learning Grading Part 4: Authoring Preview and Dry Run

> Este plano descreve o dry-run determinístico original. O fluxo canônico foi
> ampliado para um test run completo de assessment em
> [`quiz-grading-end-to-end/03-author-assessment-test-runs.md`](./quiz-grading-end-to-end/03-author-assessment-test-runs.md).

## Summary

Part 4 adds author-facing preview flows for quiz content.

The goal is to let course authors test both ungraded practice behavior and grading-enabled server behavior before publishing or relying on the item. Ungraded preview can use local-practice feedback. Grading-enabled dry-run must use the same learner-safe redaction and server grading path as real submissions, but it must not create official attempts, learner progress, gradebook entries, or final-grade effects.

## Dependencies

Part 4 depends on the Part 3 backend path:

- content-owned grading definitions are persisted;
- answer-key material is stored server-side only;
- learner payloads are redacted by the backend;
- structured answer submissions are accepted by the backend;
- deterministic quiz grading can run server-side;
- client-sent score, correctness, answer key, and `isCorrect` are ignored.

The frontend can add local-practice preview for grading-disabled content before Part 3 is complete. The grading-enabled dry-run should wait for the server endpoint.

## Goals

- Add a `Preview` action to `quiz-content-editor`.
- Render grading-disabled quizzes in local-practice mode.
- Render grading-enabled quizzes in a learner-like mode using a learner-safe payload.
- Let the author answer the quiz as a learner would.
- For grading-disabled preview, show local correct/incorrect feedback only.
- For grading-enabled preview, submit answers to a server dry-run endpoint.
- For grading-enabled dry-run, return score, pass/fail status, item feedback, and grading diagnostics to the author.
- For grading-enabled dry-run, use the same grading adapter and deterministic grading engine as real learner submissions.
- Verify that grading-enabled learner preview payloads do not expose answer keys.
- Keep preview and dry-run isolated from official gradebook and learner progress writes.

## Non-Goals

- Do not recreate client-side grading for grading-enabled preview or dry-run.
- Do not make local-practice preview produce trusted scores or official results.
- Do not persist preview results as learner attempts.
- Do not update `AssessmentSubmission.Score` for dry-run requests.
- Do not update gradebook/final-grade aggregates.
- Do not solve previews for every content type in the first pass.
- Do not make `Assessments` the authoring owner for quiz previews.

## Frontend Flow

Authoring surface:

- Show `Preview` in the quiz content editor header or primary actions area.
- Keep the existing save flow separate from preview.
- If grading is disabled, preview may run against the current local authoring state.
- If grading is enabled and there are unsaved changes, require save before server dry-run or send an explicit draft preview payload only if the backend supports it.
- Open preview in a modal, side panel, or dedicated preview route.
- For grading-disabled preview, render from the normal content payload and use `local-practice`.
- For grading-enabled preview, render the quiz from the learner payload, not from the full authoring payload.

Preview states:

- loading learner-safe payload;
- local-practice ready;
- ready to answer;
- submitting dry-run;
- graded result;
- failed redaction or grading error.

Result display:

- local-practice correct/incorrect feedback for grading-disabled quizzes;
- total score;
- max score;
- pass/fail when a passing score exists;
- per-item status when feedback policy allows it;
- general feedback;
- redaction diagnostics for authors, such as missing grading config or missing answer key.

## Backend Flow

Suggested endpoints or service operations:

- `GET /content/{contentId}/grading/preview`
  - returns the learner-safe preview payload for the current author;
  - redacts answer keys;
  - includes runtime policy needed by the preview UI.

- `POST /content/{contentId}/grading/dry-run`
  - accepts structured answers;
  - ignores score/correctness fields if present;
  - grades with server-owned answer keys;
  - returns a dry-run grading result;
  - does not create official attempts or gradebook writes.

The exact route names can follow existing API conventions once Part 3 chooses the final content-grading service boundary.

No backend endpoint is required for grading-disabled local-practice preview unless the product later wants server-rendered draft preview for consistency.

## Security Rules

- The dry-run endpoint must require author/editor permission for the course content.
- Learner-safe preview payload must never contain answer keys.
- Dry-run must use server-owned answer keys, not data supplied by the client.
- Dry-run must ignore client-supplied score, correctness, answer key, and `isCorrect`.
- Dry-run responses can include author-facing diagnostics, but only for authenticated authors/editors.
- The learner runtime must not call author dry-run endpoints.
- Local-practice preview must be disabled automatically when grading is enabled.

## Package Contract

Part 4 should reuse the package contracts from Part 2 and the backend implementation from Part 3.

The grading package may expose shared dry-run result types, but it should not become a UI package.

Useful shared types:

```ts
export interface GradingDryRunRequest {
  contentId: string;
  structuredAnswerPayload: StructuredAnswerPayload;
}

export interface GradingDryRunResult {
  score: number;
  maxScore: number;
  passed?: boolean;
  feedback?: GradingFeedback;
  itemResults: Record<string, GradedItemResult>;
  diagnostics?: GradingDiagnostic[];
}
```

The names can change during implementation, but the boundary should stay the same: the client sends answers, the server returns the trusted dry-run result.

## Tests

Frontend:

- quiz editor shows `Preview`;
- grading-disabled preview renders in local-practice mode and shows correct/incorrect feedback;
- grading-enabled preview renders from learner-safe payload;
- grading-enabled preview submit sends structured answers;
- grading-enabled preview result shows score and feedback from the server;
- unsaved quiz changes are handled explicitly;
- `React.lazy` plus `Suspense` remain in place, with no `next/dynamic`.

Backend:

- preview payload excludes answer keys;
- dry-run correct submission returns expected score;
- dry-run incorrect submission returns expected score;
- dry-run does not create an official attempt;
- dry-run does not update gradebook/final grade;
- dry-run ignores injected score/correctness/answer-key fields.

Regression:

- existing content save/reload remains unchanged;
- official learner submissions still use the Part 3 server grading path;
- grading-disabled preview does not create official submissions or trusted results;
- official feedback-only submissions still store server-produced feedback/results through the submission/result path;
- feedback-only content remains outside gradebook;
- gradebook content only updates gradebook through official submission flow, not dry-run.

## Open Decisions

- Whether preview requires saving first or supports server-side draft payloads.
- Whether preview opens in a modal, drawer, or dedicated route.
- Whether dry-run results are response-only or stored as ephemeral author audit records.
- Which diagnostics are safe and useful to expose to authors.
- Whether future content-type adapters need custom preview renderers or can share a common shell.
