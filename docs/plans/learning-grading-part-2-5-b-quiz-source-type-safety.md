# Learning Grading Part 2.5B: Quiz Source Type Safety

## Summary

Part 2.5B is a follow-up to the quiz adapter hardening pass. Part 2.5 made the `@game-guild/grading` quiz adapter safer for backend use, but the `block-content-editor` quiz source still treats authoring payloads, learner-safe payloads, and learner answers too similarly.

This phase improves safety at the source by introducing explicit quiz payload contracts in the quiz editor/rendering layer. The goal is to make accidental answer-key exposure harder in TypeScript, not only at runtime.

When a problem can be solved cleanly in the quiz source, prefer fixing the quiz source instead of adding more defensive compensation in `@game-guild/grading`. The grading package should remain defensive at trust boundaries, but the preferred long-term shape is a strict quiz contract that makes most malformed or unsafe states impossible to produce from the editor.

## Decisions Preserved

- `Content` remains the authoring source of truth.
- `Assessments` remains an operational projection over graded content.
- Official correctness and score calculation remain server-side.
- Quiz blocks with grading disabled may still show local-practice correctness in the client.
- Quiz blocks with grading enabled must render from learner-safe payloads and collect answers only.
- `@game-guild/grading` stays framework-independent and content-type agnostic.
- The quiz adapter remains defensive at server/package trust boundaries, but it should not carry extra complexity that only compensates for avoidable loose quiz source contracts.

## Goals

- Split quiz data contracts into authoring, learner-safe, and answer/submission shapes.
- Remove casts needed to render learner-safe quiz payloads in tests and runtime.
- Make quiz renderers consume learner-safe fields unless they are explicitly in local-practice or authoring mode.
- Keep answer-key fields available only to authoring/local-practice code paths.
- Add source-level validation for incomplete authoring definitions before they become deterministic grading items.
- Align frontend quiz DTOs with the grading adapter's learner redaction and structured answer payload.
- Reduce unnecessary defensive branches in the grading quiz adapter by preventing invalid quiz states at the editor/type layer when feasible.

## Non-Goals

- Do not add backend persistence.
- Do not add migrations.
- Do not change `ProgramContent` ownership.
- Do not create an assessments-owned quiz editor.
- Do not implement the Part 4 dry-run UI.
- Do not remove local-practice correctness from ungraded quizzes.
- Do not trust frontend validation as security authority. Server/package validation remains required.

## Workstream 1: Define Quiz Payload Families

Create explicit quiz type families in `block-content-editor`:

- `QuizAuthoringEntry`: full instructor-owned data, including answer keys.
- `QuizLearnerEntry`: learner-safe rendering data, with answer keys removed.
- `QuizPracticeEntry`: local-practice data, currently equivalent to authoring-safe-with-answer-key for ungraded previews/runtime.
- `QuizSubmissionAnswer` or equivalent: learner answer state sent toward `StructuredAnswerPayload`.

Recommended direction:

- Keep the existing editor-facing types as authoring types where possible.
- Add separate learner-safe types instead of making answer-key fields broadly optional on authoring types.
- Model per-question learner-safe differences:
  - multiple choice uses optional author-visible `selectionLimit`, never `correctOptionIds`;
  - matching uses `pairs: { id; left }[]` plus `rightOptions`;
  - ordering uses `items: { id; text }[]`;
  - categorization uses categories and item text, not `correctCategoryIds`;
  - hotspot includes image metadata but not `hotspots`;
  - highlight includes `plainText` but not `sourceText` or `highlights`.

Implementation preference:

- Put invariants in the quiz source types when the quiz editor can guarantee them.
- Avoid making authoring answer-key fields broadly optional just so learner-safe objects can reuse authoring types.
- Use distinct learner-safe types instead of adding defensive null checks throughout renderers when a better source contract removes the invalid state.

## Workstream 2: Renderer Contract Cleanup

Refactor quiz display/renderers so they do not require answer-key fields for learner-safe rendering.

Required behavior:

- `QuizDisplay` should accept a discriminated runtime contract instead of assuming one authoring `QuizEntry`.
- `submissionMode="server-graded"` should only accept or internally convert to learner-safe entries.
- `submissionMode="local-practice"` may use practice entries with answer-key material.
- Renderers should display and collect answers from learner-safe fields.
- Local correctness hooks should be separate from server-graded answer collection, or guarded by a type that proves answer keys exist.

Expected result:

- No test fixtures need `as MultipleChoiceEntry` or similar casts to render redacted payloads.
- A learner-safe payload missing answer-key fields is valid by type, not merely tolerated at runtime.
- A server-graded renderer cannot accidentally read `correctOptionId`, `correctOptionIds`, `correctAnswer`, `hotspots`, or `highlights`.

## Workstream 3: Conversion and Type Guards

Add explicit conversion helpers at the quiz source boundary.

Suggested helpers:

```ts
toQuizLearnerEntry(entry: QuizAuthoringEntry): QuizLearnerEntry;
toQuizLearnerEntries(entries: readonly QuizAuthoringEntry[]): QuizLearnerEntry[];
isQuizAuthoringEntry(value: unknown): value is QuizAuthoringEntry;
isQuizLearnerEntry(value: unknown): value is QuizLearnerEntry;
hasQuizAnswerKey(entry: QuizAuthoringEntry): boolean;
```

Rules:

- Conversion helpers should mirror the `@game-guild/grading` quiz adapter redaction behavior.
- The grading package remains the backend-facing implementation source for official redaction.
- The frontend helper exists to keep authoring previews and local UI contracts honest before Part 3 endpoints exist.
- Any duplicated redaction rules must be tested against adapter output to avoid drift.

## Workstream 4: Source Validation Before Grading Classification

Improve completeness checks before quiz questions are classified as deterministic.

Prefer source validation in the quiz editor/model first. The grading adapter should still reject or downgrade malformed inputs, because backend inputs are untrusted, but most author-created invalid states should be prevented before save or before grading metadata is generated.

Required validation examples:

- `SINGLE_CHOICE`: has a non-empty `correctOptionId` that exists in `options`.
- `MULTIPLE_CHOICE`: has at least one `correctOptionId`, and every ID exists in `options`.
- `TRUE_FALSE`: has a boolean `correctAnswer`.
- `FILL_IN_THE_BLANK`: every blank has a supported input and required answer-key fields.
- `SHORT_ANSWER`: has at least one non-empty accepted answer.
- `MATCHING`: has at least one pair and every pair has `id`, `left`, and `right`.
- `ORDERING`: every item has a stable ID and numeric `correctPosition`, with no duplicate positions.
- `CATEGORIZATION`: every item has at least one valid category ID.
- `RATING`: deterministic only if `correctRating` is within scale.
- `HOTSPOT`: has image dimensions and at least one hotspot with at least one valid zone.
- `HIGHLIGHT`: has valid non-empty highlight spans within `plainText`.
- `NUMERIC` and `FORMULA`: remain `unsupported` until server-side evaluators are implemented.
- `ESSAY`: remains `manual` unless a later manual grading contract changes it.

Expected result:

- Incomplete deterministic questions become `unsupported` or produce authoring validation errors.
- The server/package does not silently score malformed deterministic items as simply incorrect.

## Workstream 5: Align Grading Package and Quiz UI

Keep the package and frontend contracts aligned without coupling the package to React.

Required cleanup:

- Export or document quiz learner-safe shapes from a framework-independent location when useful.
- Keep `packages/features/grading` free of UI imports.
- Keep `block-content-editor` free of assessment-specific imports.
- Ensure `BlockArrayEditor` remains the source authoring surface for quiz content.
- Add tests that compare frontend learner-safe conversion with adapter redaction for representative quiz types.
- After source contracts are strict, revisit defensive code added in the quiz adapter and simplify any branches that are no longer necessary outside package/server trust-boundary validation.

## Workstream 6: Tests

Package tests:

- deterministic classification rejects incomplete answer-key definitions;
- answer-key extraction skips or marks incomplete deterministic items safely;
- redaction still removes every answer-key field;
- structured answer payload still drops score/correctness/tampering.

Frontend tests:

- authoring editor accepts and saves complete authoring entries;
- learner-safe entries render without casts;
- server-graded quiz display never computes local correctness;
- local-practice quiz display still shows correct/incorrect feedback;
- matching, hotspot, highlight, ordering, categorization, and multiple-choice learner-safe payloads render without answer keys;
- conversion helper output matches grading adapter redaction for representative fixtures.

Manual checks:

- create/edit each quiz type in content;
- save/reload authoring payload and confirm answer keys remain in authoring storage only;
- render learner-safe preview and confirm answer keys are absent;
- submit local-practice ungraded quiz and confirm client feedback still works;
- enable grading and confirm submit only collects answers until server grading exists.

## Acceptance Criteria

Part 2.5B is complete when:

- quiz authoring, learner-safe, and answer/submission payloads have distinct TypeScript contracts;
- learner-safe quiz rendering does not need type casts;
- answer-key fields are not part of learner-safe renderer contracts;
- grading-enabled quiz runtime cannot call local correctness logic by type or explicit runtime guard;
- incomplete deterministic quiz definitions do not become trusted deterministic grading items;
- quiz-source validation prevents invalid authoring states before the grading package has to compensate for them;
- the grading adapter's remaining defensive logic is limited to external/untrusted input boundaries and backend parity;
- focused package and web tests pass;
- Part 3 can consume a clearer frontend/package boundary for learner-safe delivery.

## Handoff to Part 3

After Part 2.5B, Part 3 should be able to rely on:

- authoring payloads that are intentionally distinct from learner payloads;
- explicit conversion/redaction boundaries;
- stronger validation before server-side answer-key persistence;
- learner submission payloads that are normalized and do not include trusted score/correctness fields;
- frontend renderers that already match the learner-safe payload shape the backend will return.
