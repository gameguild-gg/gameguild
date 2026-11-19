# `@game-guild/grading-adapter-code`

Pure translation between `@game-guild/coding` and the generic
`@game-guild/grading` contracts.

The adapter projects one academic code item, allocates its integer score among
frozen tests using Hamilton allocation, emits learner-safe delivery, validates
source-only responses, and maps a trusted server execution report to
`GradeResultV1` plus versioned evidence.

It does not run code. The browser is never an academic authority. Infrastructure
failure, timeout, cancellation, and incomplete execution remain pending and are
not converted to score zero. A failed test is different: it is a valid final
technical outcome and may contribute zero for that test.

The code adapter has one canonical pre-release identity: adapter and handler
version `"1"`, with submission payload schema `code-files`. Unknown identities
fail closed. Versioned names imported from `@game-guild/grading`, such as
`GradeResultV1`, belong to the generic grading protocol and are intentionally
unchanged.
