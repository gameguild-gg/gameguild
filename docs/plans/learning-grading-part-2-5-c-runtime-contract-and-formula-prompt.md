# Learning Grading Part 2.5C: Runtime Contract Cleanup and Formula Prompt

## Summary

Part 2.5B achieved the main goal of separating quiz authoring payloads from learner-safe payloads. Part 2.5C is a small cleanup pass before the backend-facing Part 3.

This phase addresses two issues found during review:

- structural answer-key detection is not reliable for every quiz type;
- grading-enabled `FORMULA` questions need an explicit learner prompt contract because the learner cannot receive the hidden formula.

The goal is to remove ambiguous runtime inference before it becomes part of the server contract.

## Decisions Preserved

- `Content` remains the authoring source of truth.
- `Assessments` remains a projection over content with grading enabled.
- Official grading remains server-side.
- Ungraded/local-practice quizzes may still validate in the client.
- Grading-enabled learner payloads must not include answer-key material.
- `@game-guild/grading` remains framework-independent and UI-agnostic.
- Quiz remains the first adapter, not the only grading content type.

## Problem 1: Structural Answer-Key Detection

Part 2.5B introduced `hasQuizAnswerKey(entry: QuizRuntimeEntry)`. Its current shape is risky because it tries to infer authoring-vs-learner status from field presence.

That inference cannot be fully correct for all quiz types. For example:

- learner-safe `FILL_IN_THE_BLANK` dropdowns still include `options`;
- learner-safe word-bank blanks still include `words`;
- those fields are needed for rendering, but they do not prove that the payload is authoring-safe or locally gradable.

So a structural helper can accidentally become a security-looking guard while still returning true for redacted payloads.

### Direction

Do not infer trust level from quiz shape.

Instead, trust level must come from the calling context:

- authoring editor has `QuizAuthoringEntry`;
- local-practice runtime has `QuizPracticeEntry`;
- grading-enabled runtime has `QuizLearnerEntry`;
- unknown transport payloads must be parsed into one of those contracts by endpoint/context, not by answer-key heuristics.

### Work

- Remove the exported `hasQuizAnswerKey` helper, or replace it with a non-security helper that only accepts `QuizAuthoringEntry`.
- Ensure no production code uses answer-key detection to decide whether local grading may run.
- Keep `QuizDisplayProps` and `UseQuizAnswersProps` discriminated by `submissionMode`.
- Keep local correctness reachable only through `submissionMode !== "server-graded"` plus a `QuizPracticeEntry` type.
- Add tests proving learner-safe dropdown and word-bank fill blanks are not treated as locally gradable by any runtime helper.
- Use `validateQuizAuthoringEntry` for authoring completeness, not for runtime trust inference.

### Acceptance Criteria

- There is no exported helper that claims to prove answer-key ownership from a `QuizRuntimeEntry` shape.
- Server-graded paths never call local correctness even if a learner-safe payload contains renderable options.
- Local-practice behavior remains unchanged for authoring/practice entries.
- Tests cover fill-blank dropdown and word-bank learner-safe payloads.

## Problem 2: Grading-Enabled Formula Learner Prompt

`FORMULA` authoring entries contain the hidden formula. Part 2.5B correctly removes that formula from learner-safe payloads.

However, the current formula renderer still has a practice-oriented flow: generate variables, compute expected result from `entry.formula`, and test locally. That is valid only when grading is disabled. It cannot work for grading-enabled/server-graded formula questions because the hidden formula cannot be sent to the client.

### Direction

Add an explicit grading-enabled learner prompt shape for formula questions.

For grading-enabled formula questions, the server should provide the learner with generated prompt data, not the answer formula:

```ts
type FormulaLearnerPrompt = {
  variables: Record<string, number>;
  expectedResult: number;
  decimalPlaces?: number;
};
```

The learner submits a formula expression. The server validates it against the hidden formula and any server-owned test cases. The client may display the given variable values and expected result, but it must not compute correctness.

### Work

- Extend `FormulaLearnerEntry` with an optional `prompt` or `prompts` field for grading-enabled runtime.
- Keep `formula`, `tolerance`, and `toleranceType` out of `FormulaLearnerEntry`.
- Update `toQuizLearnerEntry` so local frontend redaction can preserve the field shape, but does not invent server-generated prompts.
- Update `FormulaRenderer`:
  - local-practice authoring entry: keeps current local test behavior;
  - grading-enabled learner entry with prompt: displays server-provided variables and expected result;
  - grading-enabled learner entry without prompt: shows a neutral unavailable state for testing, while still allowing answer capture if appropriate.
- Add tests for grading-enabled formula payload rendering without formula.
- Keep `FORMULA` grading as `unsupported` in `@game-guild/grading` until Part 3 explicitly implements server-side formula evaluation.

### Acceptance Criteria

- Grading-enabled formula learner payloads can render without `formula`.
- Client never derives grading-enabled formula correctness locally.
- The UI has a clear runtime contract for formula prompts that Part 3 can populate from the backend.
- Existing local-practice formula behavior remains intact.
- `@game-guild/grading` still refuses official deterministic formula grading until the server evaluator exists.

## Implementation Order

1. Remove or narrow `hasQuizAnswerKey`.
2. Add tests for learner-safe fill-blank dropdown/word-bank payloads.
3. Add `FormulaLearnerPrompt` to quiz contracts.
4. Update `FormulaRenderer` to branch by explicit authoring-vs-learner prompt availability.
5. Add grading-enabled formula renderer tests.
6. Run focused quiz web tests, focused quiz lint, grading package tests, and grading package typecheck.

## Non-Goals

- Do not implement backend formula evaluation.
- Do not mark `FORMULA` as deterministic in the grading package.
- Do not add migrations.
- Do not change content ownership.
- Do not move grading ownership into assessments.
- Do not add Part 4 preview UI beyond the minimal renderer behavior needed for the formula prompt contract.

## Handoff to Part 3

After Part 2.5C, Part 3 can assume:

- grading runtime is explicit, not inferred from JSON shape;
- learner-safe formula payloads have a place for server-generated prompt data;
- server-side grading can add formula evaluation later without exposing the hidden formula;
- quiz source contracts are less likely to become accidental security boundaries.
