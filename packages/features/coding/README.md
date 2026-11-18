# `@game-guild/coding`

Pure, versioned contracts for coding assessments. The package owns authoring,
learner delivery, learner response, execution report, path normalization, and
privacy projection. It has no React, Next.js, grading, storage, or Emception
dependency.

The authoring parser is fail-closed. Learner delivery is a distinct contract
that omits private files, private tests, and visibility metadata. Submissions
contain source files only; scores and execution claims are rejected as unknown
fields. Execution reports are technical evidence and never declare an official
academic grade.

The canonical fixture under `fixtures/` is consumed through the `./testing`
export and is the cross-runtime baseline for the backend adapter work.
