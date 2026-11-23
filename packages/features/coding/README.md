# `@game-guild/coding`

Pure contracts and transformations for coding assessments. The package owns authoring,
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

`composeCodingNativeFunctionSupport` is a pure source-composition port for
WASI C/C++ function exercises. Consumers provide authorized cases and, for
C++, the existing public harness factory. The returned file is temporary and
outside the submitted workspace; apply it through a restoring workspace
overlay, never persist it as learner source. The grading worker regenerates
its own support from the frozen definition.

Native scalar ABI: `int`, `double`, `_Bool`, `const char*` in C; `int`,
`double`, `bool`, `std::string` in C++. Integers outside signed 32-bit range,
non-finite/unsafe decimals, embedded NUL/unpaired surrogates, and C++ strings
the unchanged public serializer cannot represent are rejected explicitly.
Array/object transport is not implemented. This helper is not a compiler,
authorization decision, academic evaluator or general unresolved-import guard.
