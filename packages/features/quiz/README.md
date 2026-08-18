# GameGuild Quiz

`@game-guild/quiz` owns framework-independent Quiz contracts and question
semantics. It has no React, Next.js, block-list, Lexical, or grading dependency.

Authoring entries contain answer keys. Learner entries are explicitly redacted.
Typed answers are converted to the generic grading transport only at integration
boundaries.

Unknown persisted values must enter through `quizEntrySchema`,
`safeParseQuizEntry()`, or `parseQuizEntry()`. Structural parsing remains
separate from `validateQuizAuthoringEntry()`, which reports semantic authoring
issues for structurally valid drafts.

The package does not own ordered quiz documents. That contract belongs to
`@game-guild/quiz-content`.
