# `@game-guild/emception-ui`

GameGuild's coding-assessment adapter for Emception.

It composes the public `@gameguild/emception-ide` controller and extension API.
It does not own a worker, a virtual filesystem bridge, compiler arguments, or a
second IDE implementation.

## Ownership

- `emception` and `@gameguild/emception-browser` own compilation, VFS access,
  and generic test execution.
- `@gameguild/emception-ide` owns the neutral editor, workspace state, and
  extension slots.
- This package owns GameGuild's coding-assignment DTO mapping and the
  learner/author/grader policies.
- Web routes own authorization, persistence, instructor feedback, and grade
  confirmation.

## Use the assessment editor

Use `CodingAssessmentEditor` for every GameGuild coding-assessment screen.
Supply a public definition to learners and an authorized full definition to
authors or graders. The component runs public tests for learners and full tests
for authors and graders through `AssessmentSession`.

Private fixtures and generated functional-test harnesses are mounted only for
the test invocation. They are not visible files, workspace drafts, or student
submission data.

```tsx
<CodingAssessmentEditor
  mode="grader"
  definition={assignment}
  workspaceConfig={workspaceConfig}
  onRunResult={receiveResult}
/>
```

Use its `extensions` prop for host controls such as authoring fields, feedback
UI, or route-specific actions. Add reusable execution and privacy behavior to
`assessment/session.ts`; do not recreate an IDE, worker client, or test runner
inside a route.
