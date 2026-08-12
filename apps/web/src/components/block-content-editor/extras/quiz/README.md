# Block Content Quiz

This folder owns the quiz block UI for `block-content-editor`: authoring
contracts, quiz editors, learner renderers, local-practice answer handling, and
frontend learner-safe types.

It does not own official grading. When grading is enabled, quiz correctness and
scores come from the server through the shared grading contract.

## Runtime Paths

There are two quiz runtime paths:

- `local-practice`: used when grading is disabled. The entry is a
  `QuizPracticeEntry`, which currently matches the authoring shape and can
  include answer-key material. The client may show correct/incorrect feedback
  for pedagogy only.
- `server-graded`: used when grading is enabled. The entry is rendered as a
  learner-safe payload and the client collects answers for server submission.
  The client must not compute trusted correctness or score.

`server-graded` renderers show submission state until trusted server feedback is
available. They must not infer trust from JSON shape or field presence.

## Data Contracts

`contracts.ts` separates quiz data by context:

- `QuizAuthoringEntry`: full instructor-owned quiz definition.
- `QuizPracticeEntry`: grading-disabled practice entry with local answer keys.
- `QuizLearnerEntry`: learner-safe quiz entry without answer-key material.
- `QuizRuntimeEntry`: practice or learner-safe runtime entry.
- `QuizSubmissionAnswer`: frontend answer state shape.

`toQuizLearnerEntry` converts an authoring entry into the frontend learner-safe
shape. This is useful for preview and local contract tests. Backend redaction is
still the authority for grading-enabled learner delivery.

`validateQuizAuthoringEntry` checks authoring completeness and answer-key
validity. It is not a runtime trust check.

## Answer-Key Boundary

Answer-key fields belong only in authoring and local-practice contexts.

Do not add helpers that inspect arbitrary runtime JSON to decide whether local
grading is allowed. The caller must choose the runtime path explicitly through
`submissionMode`.

Renderable learner fields can still look answer-like. For example, dropdown
options and word-bank words are required to render learner-safe fill blanks, but
they do not prove the payload can be graded locally.

## Formula Questions

Formula authoring entries keep the hidden `formula`, `toleranceType`, and
`tolerance`.

`FormulaLearnerEntry` removes those fields and may include a server-generated
`prompt`:

```ts
interface FormulaLearnerPrompt {
  variables: Record<string, number>;
  expectedResult: number;
  decimalPlaces?: number;
}
```

The learner can see the generated variables and expected result, then submit a
formula expression. The client does not validate the grading-enabled formula
answer locally.

## Folder Map

- `types.ts`: authoring model and shared quiz UI types.
- `contracts.ts`: authoring, practice, learner-safe, validation, and redaction
  contracts.
- `editors/`: per-question authoring editors.
- `renderers/`: per-question learner/practice renderers.
- `hooks/use-quiz-answers.ts`: answer state and local-practice checking.
- `utils/formula-evaluator.ts`: local formula helpers for practice/authoring UI.
- `quiz-display.tsx`: runtime display wrapper around renderer, submit state, and
  feedback.

## Grading Package Boundary

Quiz official-grading behavior lives in `@game-guild/grading` under
`packages/features/grading/src/adapters/quiz`.

When changing quiz data shapes, update both sides together:

- frontend authoring and learner-safe contracts;
- grading adapter answer-key extraction;
- grading adapter learner redaction;
- structured answer payload normalization;
- deterministic/manual/unsupported classification;
- focused frontend and package tests.

## Content and Assessments Boundary

Quiz content is authored through content-owned editors and `BlockArrayEditor`.
The quiz UI must not import assessment-owned editors or make assessments the
source of quiz definitions.

`Assessments` lists and routes content with grading enabled. Editing remains
content-owned.

## Adding or Changing a Quiz Type

1. Update the authoring type in `types.ts`.
2. Add or adjust the editor in `editors/`.
3. Add or adjust the renderer in `renderers/`.
4. Update `contracts.ts` with learner-safe and practice behavior.
5. Update `use-quiz-answers.ts` only for grading-disabled local practice.
6. Update the grading quiz adapter for answer keys, redaction, structured
   answers, and grading support.
7. Add tests for authoring validation, learner-safe rendering, local practice,
   adapter redaction, payload normalization, and grading classification.

## Validation

Useful focused commands:

```bash
pnpm --filter @game-guild/web exec vitest run src/components/block-content-editor/extras/quiz/contracts.test.ts src/components/block-content-editor/extras/quiz/quiz-display.test.tsx src/components/block-content-editor/extras/quiz/hooks/use-quiz-answers.test.tsx
pnpm --filter @game-guild/grading test
pnpm --filter @game-guild/grading typecheck
```
