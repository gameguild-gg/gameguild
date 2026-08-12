# GameGuild Grading

`@game-guild/grading` is the framework-independent grading contract package for
GameGuild learning content. It defines how content-owned activities describe
scores, attempts, feedback, gradebook use, answer keys, learner-safe payloads,
structured answers, and deterministic grading results.

This package does not own React UI, database persistence, HTTP routes, or
course navigation. It provides contracts and pure helpers that the web app and
the Learning backend can share.

## Ownership Model

- `Content` owns authored activity data and grading metadata.
- `ProgramContent` is the current backend anchor for authored content data.
- `Assessments` is an operational view over content with grading enabled.
- `AssessmentSubmission` is the trusted attempt/result path for learner answers,
  structured answer payloads, score, pass/fail, grading time, and feedback.
- This package owns portable grading contracts and adapter behavior.

## Runtime Model

There are two runtime paths:

- Grading disabled: the learner can receive the full practice payload when the
  content type supports local pedagogy. Any client-side correctness is transient
  practice feedback and is not a trusted score.
- Grading enabled: the learner receives a learner-safe payload, submits answers,
  and the server produces trusted correctness, score, feedback, pass/fail, and
  gradebook effects.

Client-provided `score`, `grade`, `correctness`, `isCorrect`, answer-key fields,
or feedback are never trusted as grading evidence.

## Core Contract

The top-level contract is `ContentGradingDefinition`:

```ts
interface ContentGradingDefinition {
  enabled: boolean;
  schemaVersion: number;
  outcome: GradingOutcomePolicy;
  score: ScorePolicy;
  attempts: AttemptPolicy;
  feedback: FeedbackPolicy;
  presentation: PresentationPolicy;
  items: Record<string, GradedItemConfig>;
}
```

`outcome.uses` controls how a trusted server result is used:

- `feedback`: server-validated feedback/result exists, but it does not affect
  the global grade.
- `gradebook`: the result can contribute to gradebook/final-grade flows through
  the configured `gradebook` placement.

`uses: ['feedback']` still means grading is enabled and server-side. It is not
the same as disabling grading.

## Adapter Contract

Each content type integrates through a `GradingAdapter`:

```ts
interface GradingAdapter<TAuthoringPayload = unknown> {
  contentType: string;
  extractItems(payload: TAuthoringPayload): Record<string, GradedItemConfig>;
  extractAnswerKey(payload: TAuthoringPayload, grading: ContentGradingDefinition): AnswerKey;
  redactLearnerPayload(payload: TAuthoringPayload, grading: ContentGradingDefinition): unknown;
  buildStructuredAnswerPayload(input: unknown, grading: ContentGradingDefinition): StructuredAnswerPayload;
}
```

Adapter responsibilities:

- inspect authored content and produce `GradedItemConfig` entries;
- extract server-owned answer-key material from authored payloads;
- produce learner-safe payloads without answer-key material;
- normalize learner submissions into `StructuredAnswerPayload`;
- support server-side grading helpers when the content type is deterministic.

The adapter registry in `src/adapters/registry.ts` is content-type agnostic.
Register adapters by content type and keep UI-specific imports out of this
package.

## Quiz Adapter

The first adapter is `quiz`, implemented under `src/adapters/quiz`.

It supports:

- item extraction from quiz blocks;
- answer-key extraction from authored quiz data;
- learner-safe redaction;
- structured answer payload whitelisting;
- deterministic grading for supported quiz question types;
- backend-facing test vectors.

Deterministic quiz grading is available when the question has a complete answer
key for the relevant type. Essay is manual. Numeric and formula questions are
unsupported for official deterministic grading until server evaluators exist for
those domains. Any incomplete answer-key shape is classified as unsupported.

## Answer Safety

Authoring payloads may contain answer keys. Learner payloads must not rely on
answer keys or answer-key-shaped fields.

Structured answer payloads are whitelisted to learner answer fields only:

- `selectedOptionIds`
- `textAnswers`
- `categorizations`
- `ordering`
- `rating`

Unsafe nested fields such as answer keys, correctness, scores, grades, formulas,
hotspots, highlights, and feedback are dropped during normalization.

## Storage Helpers

`src/content-storage.ts` provides temporary content-body helpers:

- `readContentGradingDefinition`
- `writeContentGradingDefinition`
- `parseContentBodyObject`

They keep grading metadata inside the content-owned structured body until the
backend stores the full content-owned grading definition directly.

## Development Rules

- Keep this package framework-independent.
- Keep content-type behavior behind adapters.
- Keep official grading server-side for grading-enabled content.
- Keep client-side correctness limited to grading-disabled practice flows.
- Keep `feedback` and `gradebook` as result-use semantics, not runtime trust
  modes.
- Add test vectors when adapter behavior is meant to be mirrored by the backend.

## Validation

Useful package commands:

```bash
pnpm --filter @game-guild/grading test
pnpm --filter @game-guild/grading typecheck
```
