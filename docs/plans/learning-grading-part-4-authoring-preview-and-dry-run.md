# Learning Grading Part 4: Authoring Preview and Dry Run

## Summary

Part 4 adds an author-facing preview flow for graded content, starting with quiz content.

The goal is to let course authors test the learner experience and server-side grading behavior before publishing or relying on the item. This preview must use the same learner-safe redaction and server grading path as real submissions, but it must not create official attempts, learner progress, gradebook entries, or final-grade effects.

## Dependencies

Part 4 depends on the Part 3 backend path:

- content-owned grading definitions are persisted;
- answer-key material is stored server-side only;
- learner payloads are redacted by the backend;
- structured answer submissions are accepted by the backend;
- deterministic quiz grading can run server-side;
- client-sent score, correctness, answer key, and `isCorrect` are ignored.

The frontend can add a disabled or local shell for the `Preview` button before Part 3 is complete, but the real grading dry-run should wait for the server endpoint.

## Goals

- Add a `Preview` action to `quiz-content-editor`.
- Render the quiz in a learner-like mode using a learner-safe payload.
- Let the author answer the quiz as a learner would.
- Submit answers to a server dry-run endpoint.
- Return score, pass/fail status, item feedback, and grading diagnostics to the author.
- Use the same grading adapter and deterministic grading engine as real learner submissions.
- Verify that learner preview payloads do not expose answer keys.
- Keep preview/dry-run isolated from official gradebook and learner progress writes.

## Non-Goals

- Do not recreate client-side grading.
- Do not persist preview results as learner attempts.
- Do not update `AssessmentSubmission.Score` for dry-run requests.
- Do not update gradebook/final-grade aggregates.
- Do not solve previews for every content type in the first pass.
- Do not make `Assessments` the authoring owner for quiz previews.

## Frontend Flow

Authoring surface:

- Show `Preview` in the quiz content editor header or primary actions area.
- Keep the existing save flow separate from preview.
- If there are unsaved changes, require save before server dry-run or send an explicit draft preview payload only if the backend supports it.
- Open preview in a modal, side panel, or dedicated preview route.
- Render the quiz from the learner payload, not from the full authoring payload.

Preview states:

- loading learner-safe payload;
- ready to answer;
- submitting dry-run;
- graded result;
- failed redaction or grading error.

Result display:

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

## Security Rules

- The dry-run endpoint must require author/editor permission for the course content.
- Learner-safe preview payload must never contain answer keys.
- Dry-run must use server-owned answer keys, not data supplied by the client.
- Dry-run must ignore client-supplied score, correctness, answer key, and `isCorrect`.
- Dry-run responses can include author-facing diagnostics, but only for authenticated authors/editors.
- The learner runtime must not call author dry-run endpoints.

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
- preview renders from learner-safe payload;
- preview submit sends structured answers;
- preview result shows score and feedback;
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
- official feedback-only submissions still store server-produced feedback/results through the submission/result path;
- feedback-only content remains outside gradebook;
- gradebook content only updates gradebook through official submission flow, not dry-run.

## Open Decisions

- Whether preview requires saving first or supports server-side draft payloads.
- Whether preview opens in a modal, drawer, or dedicated route.
- Whether dry-run results are response-only or stored as ephemeral author audit records.
- Which diagnostics are safe and useful to expose to authors.
- Whether future content-type adapters need custom preview renderers or can share a common shell.
