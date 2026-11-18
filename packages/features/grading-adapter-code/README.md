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

Backend adapter versions 1, 2, and 3 remain frozen. This package introduces
version 4 for the new contracts; migration of the backend implementation is a
separate phase.
