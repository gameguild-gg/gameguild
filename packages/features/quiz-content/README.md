# GameGuild Quiz Content

`@game-guild/quiz-content` owns the versioned persisted quiz document. It
specializes `@game-guild/block-list` with `QuizEntry`, validates unknown JSON,
composes grading metadata, and creates learner-safe document projections.

The package is framework-independent. React authoring and player UI remain in
`@game-guild/quiz-surface`; individual question semantics remain in
`@game-guild/quiz`.

The canonical persisted shape is `QuizContentDocumentV1`:

```ts
{
  schemaVersion: 1;
  order: readonly [string, "quiz"][];
  blocks: Record<string, QuizEntry>;
  grading?: ContentGradingDefinitionV2;
}
```

Use `parseQuizContentDocument()` at unknown JSON boundaries and inspect its
issues. Use `quizContentItemsToDocument()` and
`serializeQuizContentDocument()` when authoring. Server-graded delivery must
pass through `toQuizLearnerContentDocument()` or
`prepareQuizContentForRuntime()` so answer keys are not exposed.

This package has no React, Next.js, application, or block-content-editor
dependency.
