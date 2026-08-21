# Learning Grading Part 2.5: Quiz Adapter Hardening

## Summary

Part 2.5 is a hardening pass between the completed Part 2 contract cleanup and the Part 3 backend integration.

Part 2 successfully moved grading toward a content-owned, server-only, content-type agnostic contract. The remaining work is to make the first adapter, quiz, safe and precise enough for backend use. The main risk is not the high-level contract anymore; it is the quiz adapter details around learner-safe redaction, answer-key extraction, structured answers, and deterministic grading classification.

## Decisions Preserved

- `Content` remains the authoring source of truth.
- `Assessments` remains an operational projection over graded content.
- All trusted correctness and score calculation happens on the server.
- Quiz blocks with grading disabled may show local-practice correctness in the client.
- Assessment groups and their weights own gradebook placement outside this package.
- Quiz is only the first adapter. The core grading package must stay content-type agnostic.
- The frontend may collect answers, but it cannot produce trusted correctness, trusted score, or official gradebook updates.

## Goals

- Make quiz redaction type-aware and safe for every currently supported quiz question shape.
- Align `gradingKind` with what the server-side adapter can actually grade.
- Produce backend-facing quiz test vectors that Part 3 can mirror in .NET.
- Ensure structured answer payloads are whitelisted and normalized instead of accepting arbitrary client fields.
- Keep content save/reload behavior unchanged.
- Preserve `React.lazy` plus `Suspense`, with no `next/dynamic`.

## Non-Goals

- Do not add backend persistence yet.
- Do not add migrations.
- Do not implement `AssessmentSubmission` wiring.
- Do not restore client-side correctness for grading-enabled learner runtime.
- Do not remove local-practice correctness from quizzes where grading is disabled.
- Do not introduce a separate assessment-owned quiz editor.
- Do not solve every future content type. This pass hardens the quiz adapter as the first adapter.
- Do not implement authoring preview or dry-run UI; that remains Part 4 after backend support exists.

## Workstream 1: Quiz Answer-Key Inventory

Create a documented inventory for every quiz type the editor can author.

For each question type, define:

- authoring payload fields;
- learner-safe payload fields;
- answer-key fields;
- structured answer shape;
- grading support status: `deterministic`, `manual`, `external`, or `unsupported`;
- whether the learner renderer needs extra non-answer metadata to work.

Question types to inventory:

- `SINGLE_CHOICE`;
- `MULTIPLE_CHOICE`;
- `TRUE_FALSE`;
- `FILL_IN_THE_BLANK`;
- `SHORT_ANSWER`;
- `ESSAY`;
- `MATCHING`;
- `ORDERING`;
- `CATEGORIZATION`;
- `RATING`;
- `NUMERIC`;
- `FORMULA`;
- `HOTSPOT`;
- `HIGHLIGHT`.

## Workstream 2: Type-Aware Learner Redaction

Replace mostly field-name-based redaction with type-aware redaction.

Required redaction rules:

- `SINGLE_CHOICE`: remove `correctOptionId`; keep answer options.
- `MULTIPLE_CHOICE`: remove `correctOptionIds`; avoid leaking the number of correct choices through renderer-only fields unless the author explicitly configures a learner-visible selection limit.
- `TRUE_FALSE`: remove `correctAnswer`; keep true/false choices.
- `FILL_IN_THE_BLANK` text and number: remove accepted/correct values and tolerance fields.
- `FILL_IN_THE_BLANK` dropdown and word bank: do not encode correctness as the first option/word in learner payload; use explicit answer key material instead.
- `SHORT_ANSWER`: remove `acceptedAnswers` and `caseSensitive`.
- `ESSAY`: remove model answers from learner payload unless the backend returns them later as allowed feedback.
- `MATCHING`: do not send `pair.right` coupled to each left item as the answer key; learner payload should expose left prompts and right options separately, with answer-key mapping stored server-side.
- `ORDERING`: remove `correctPosition`; learner payload should expose display order without correct-position metadata.
- `CATEGORIZATION`: remove `correctCategoryIds`; keep category labels and item text.
- `RATING`: remove `correctRating` when it is answer-key material.
- `NUMERIC` and `FORMULA`: remove `correctValue`, `formula`, tolerance, and generated-answer data from learner payload unless the field is explicitly safe display metadata.
- `HOTSPOT`: do not expose correct zones in learner payload.
- `HIGHLIGHT`: do not expose correct spans in learner payload.

Implementation direction:

- Keep authoring payloads rich.
- Add explicit learner-safe DTO builders in the quiz adapter.
- Prefer preserving renderer ergonomics with safe metadata over leaking answer-key fields.
- Add regression tests that serialize learner payloads and assert answer-key fields are absent.

## Workstream 3: Grading Capability Alignment

Make `gradingKind` match the adapter's actual grading capability.

Current risk:

- `isDeterministicQuizQuestionType` is too optimistic if it marks a question as deterministic while `gradeQuizAnswer` returns `unsupported`.

Required behavior:

- deterministic only when the server adapter can grade the type reliably;
- manual when the type needs human grading;
- external when a future external grader will own it;
- unsupported when it should not be graded yet.

Initial recommended classification:

- `SINGLE_CHOICE`: deterministic.
- `MULTIPLE_CHOICE`: deterministic.
- `TRUE_FALSE`: deterministic.
- `FILL_IN_THE_BLANK`: deterministic only for supported input variants after redaction/answer-key rules are fixed.
- `SHORT_ANSWER`: deterministic for exact/accepted-answer matching.
- `ESSAY`: manual by default.
- `MATCHING`: deterministic after payload/answer-key shape is made explicit.
- `ORDERING`: deterministic after `correctPosition` is removed from learner payload.
- `CATEGORIZATION`: deterministic after answer-key extraction is explicit.
- `RATING`: deterministic only when a correct rating exists; otherwise treat as feedback/completion or unsupported, not correctness.
- `NUMERIC`: unsupported until numeric grading is implemented server-side.
- `FORMULA`: unsupported or external until formula grading is implemented server-side.
- `HOTSPOT`: deterministic only after safe coordinate/zone answer-key extraction is implemented.
- `HIGHLIGHT`: deterministic only after safe span answer-key extraction is implemented.

## Workstream 4: Structured Answer Payload Hardening

`buildQuizStructuredAnswerPayload` should normalize learner answers per quiz type instead of cloning broad objects.

Required behavior:

- whitelist accepted answer fields;
- drop injected `score`, `isCorrect`, `correctness`, answer key, and feedback fields;
- normalize missing answers to an explicit empty answer shape;
- keep stable content block IDs as keys;
- preserve enough data for server grading and manual review.

Test cases:

- injected score/correctness is ignored;
- injected answer-key material is ignored;
- unknown fields do not reach the normalized payload;
- each quiz type has a representative structured answer fixture.

## Workstream 5: Backend-Facing Test Vectors

Add shared test vectors under the grading package so Part 3 can mirror them in the .NET backend.

Suggested location:

```text
packages/features/grading/src/test-vectors/
  quiz-authoring-payloads.ts
  quiz-learner-payloads.ts
  quiz-answer-keys.ts
  quiz-submissions.ts
```

Each vector should include:

- content type;
- authoring payload;
- grading definition;
- extracted answer key;
- learner-safe payload;
- learner submission;
- expected grade result when deterministic;
- expected unsupported/manual status when not deterministic.

## Workstream 6: Frontend Contract Cleanup

Keep the frontend consistent with server-only grading for grading-enabled content, while preserving local practice feedback for ungraded content.

Required cleanup:

- keep grading-enabled quiz runtime as answer collection and submission state only;
- keep `local-practice` behavior explicit for quiz blocks where grading is disabled;
- keep `showFeedback` separate from trusted server feedback;
- avoid passing `showFeedback=true` into renderers for grading-enabled content unless there is trusted server feedback;
- allow `showFeedback=true` for explicitly local-practice content that cannot create official submissions or gradebook effects;
- review quiz renderer text that implies local correctness is available immediately;
- avoid importing assessment concepts into quiz content editor;
- keep `quiz-content-editor` using `BlockArrayEditor` as the content authoring surface.

## Workstream 7: Adapter Registry Boundary

Make the adapter boundary ready for backend and future content types.

Required decisions:

- decide whether the quiz adapter is registered by default or explicitly by the caller;
- keep registry APIs content-type agnostic;
- avoid importing frontend quiz UI types into the grading package;
- expose stable adapter operations for Part 3:
  - extract items;
  - extract answer key;
  - redact learner payload;
  - build structured answer payload;
  - grade deterministic submissions.

## Validation Checklist

Package tests:

- all quiz types have answer-key inventory coverage;
- all quiz types have learner-redaction coverage;
- deterministic classifications match implemented grading behavior;
- unsupported/manual classifications do not produce trusted scores;
- structured answer payload normalization drops tampered fields;
- deterministic submissions ignore client-sent score and correctness;
- normalized definitions use the current content-owned grading contract.

Frontend tests:

- quiz content still saves and reloads grading definitions;
- assessments projection still lists content-owned graded items;
- grading-enabled learner-facing quiz path does not compute trusted correctness;
- grading-disabled quiz path shows local practice correctness after submit;
- renderer feedback for grading-enabled content only appears when a trusted result exists;
- no `next/dynamic` is introduced.

Manual checks:

- create a quiz content item;
- add representative quiz question types;
- confirm grading-disabled quiz submissions show local practice feedback only;
- enable grading;
- confirm grading submits through the server path instead of local correctness;
- assign the resulting assessment to zero- and positive-weight groups;
- save and reload;
- confirm the stored authoring JSON still contains answer-key material only in authoring storage;
- confirm learner-safe payload tests prove answer-key material is removed.

## Acceptance Criteria

Part 2.5 is complete when:

- the quiz adapter has explicit type-aware redaction for every authored quiz type;
- backend-facing fixtures exist for at least the supported deterministic quiz types and representative unsupported/manual types;
- `gradingKind` never claims deterministic support for a type that returns `unsupported`;
- structured answer payload normalization drops client-tampered score/correctness/answer-key fields;
- package typecheck and package tests pass;
- focused web tests for content and assessments pass;
- the remaining Part 3 backend work can consume the adapter contract without relying on client trust.

## Handoff to Part 3

After Part 2.5, Part 3 should be able to implement backend grading by mirroring the hardened adapter behavior:

- persist content-owned grading definitions;
- persist answer-key material server-side only;
- deliver learner-safe payloads generated by backend redaction;
- accept normalized structured answer payloads;
- grade deterministic submissions server-side;
- store trusted results in `AssessmentSubmission`;
- propagate to gradebook only for assessments assigned to positive-weight groups.
