# Learning Grading Part 2 and Part 3 Plan

## Summary

Part 2 is a contract adjustment phase. It removes the old `validationMode` split, makes the grading package content-type agnostic, and prepares the frontend/package boundary for server-only grading.

Part 3 connects that cleaned contract to the Learning backend. The backend will own learner-safe redaction, answer-key storage, submission validation, score production, and gradebook propagation.

## Decisions Already Made

- All learner-facing grading validation happens on the server.
- The client can submit learner answers, but cannot produce trusted score or correctness.
- `validationMode` is removed.
- A separate result-use policy decides where a server-produced result is used.
- `Content` remains the authoring source of truth.
- `Assessments` remains an operational view/projection over graded content.
- Quiz is the first adapter, not the core grading model.
- The grading package must stay framework-independent and content-type agnostic.

## Part 2 Goals

- Remove `validationMode` from package types, tests, content editor code, and docs.
- Add result-use typing so an evaluated activity can be feedback-only or gradebook-bound.
- Refactor `@game-guild/grading` into core contracts plus content-type adapters.
- Move quiz-specific extraction/redaction/payload logic into a quiz adapter.
- Remove learner-facing client correctness as a grading runtime.
- Preserve content save/reload behavior.
- Produce contracts and test vectors that backend Part 3 can mirror.

## Part 2 Non-Goals

- Do not add backend persistence yet.
- Do not add migrations yet.
- Do not make `Assessment.DefinitionPayload` the content source of truth.
- Do not implement official grade propagation.
- Do not solve every content type. Quiz is enough as the first adapter.

## Part 2 Contract Target

```ts
export type GradingResultUse =
  | 'feedback'
  | 'gradebook';

export interface ContentGradingDefinition {
  enabled: boolean;
  schemaVersion: number;
  outcome: GradingOutcomePolicy;
  score: ScorePolicy;
  attempts: AttemptPolicy;
  feedback: FeedbackPolicy;
  presentation: PresentationPolicy;
  items: Record<string, GradedItemConfig>;
}

export interface GradingOutcomePolicy {
  uses: GradingResultUse[];
  gradebook?: GradebookPlacement | null;
}

export interface GradebookPlacement {
  groupId?: string | null;
  weight?: number;
  required?: boolean;
  includeInFinalGrade?: boolean;
}
```

Rules:

- `uses: ['feedback']` means evaluated but not global-grade affecting.
- `uses` including `gradebook` means the result can contribute to gradebook/final grade according to `gradebook`.
- Other product concerns such as completion, certificate eligibility, placement, or analytics should consume the server-produced result from their own context instead of expanding this type before the platform has a concrete grading-package rule for them.

## Part 2 Package Refactor

Target folder direction:

```text
packages/features/grading/src/
  types.ts
  config.ts
  content-storage.ts
  adapters/
    types.ts
    registry.ts
    quiz.ts
```

Core responsibilities:

- validate and normalize grading definitions;
- validate score and result-use policies;
- define answer key, structured submission, and grade result types;
- expose adapter interfaces and registry helpers;
- stay framework-independent.

Quiz adapter responsibilities:

- extract `GradedItemConfig` from quiz blocks;
- extract answer keys from authoring payloads;
- redact answer keys from learner payloads;
- build `StructuredAnswerPayload`;
- provide deterministic server-grading test vectors.

## Part 2 Frontend Adjustments

Content editor:

- Replace validation-mode controls with result-use controls.
- Keep grading enable/disable.
- Keep max score, passing score, item points, attempts, feedback, and presentation controls.
- Persist grading definition inside the content-owned structured body until backend storage is finalized.
- Ensure quiz body remains in `ProgramContent.JsonBody`.

Learner/runtime:

- Remove any learner-facing client correctness as grading behavior.
- Keep answer collection/submission payload shaping.
- Do not show trusted correctness without a server result.

Assessments page:

- Continue listing content-owned graded activities.
- Show result use instead of validation mode.
- Link edits back to content.
- Do not restore assessment-owned quiz editing.

## Part 2 Validation

Package tests:

- `validationMode` no longer exists in normalized definitions.
- feedback-only definitions are valid without gradebook placement.
- gradebook-targeting definitions validate gradebook placement.
- invalid result uses are rejected.
- quiz adapter extracts items from quiz blocks.
- quiz adapter redacts answer keys.
- quiz adapter creates structured answer payloads.

Frontend tests:

- quiz grading metadata saves/reloads without `validationMode`.
- result-use controls save expected metadata.
- assessments projection shows result use.
- learner-facing quiz path does not compute trusted correctness in the client.

Regression:

- content without grading saves and loads unchanged.
- lesson and quiz editors remain componentized.
- `React.lazy` plus `Suspense` remain in place.
- no `next/dynamic` is introduced.

## Part 3 Goals

- Persist content-owned grading definitions in the backend.
- Persist answer-key material server-side only.
- Deliver learner-safe redacted payloads.
- Store learner submissions as structured answer payloads.
- Grade deterministic quiz submissions on the server.
- Ignore client-sent correctness, score, answer key, and `isCorrect`.
- Project all gradable content into existing assessment/submission infrastructure.
- Keep `Assessments` as a view/projection over graded content.

## Backend Fit From Audit

Use the existing Learning modules this way:

- `GameGuild.Learning.Courses`: owns `ProgramContent`, body data, content tree, progress, and content-level authoring.
- `GameGuild.Learning.Assessments`: provides `AssessmentGroup`, `AssessmentSubmission`, attempts, `StructuredAnswerPayload`, score, pass/fail, and result storage that can feed gradebook when `outcome.uses` includes `gradebook`.
- `GameGuild.Learning`: remains shared contracts/events/interfaces, not the concrete persistence owner.

Recommended Part 3 direction:

- `ProgramContent` or a related content-grading table stores `ContentGradingDefinition`.
- A server-owned answer-key store links to content grading definition versions.
- Every content item with `grading.enabled === true` gets a server-side submission/result path.
- The first implementation should prefer one active `Assessment` projection per gradable content item, linked by `Assessment.ContentId`, so feedback-only and gradebook-bound content can both reuse `AssessmentSubmission`.
- The `Assessment.ContentId` link must be validated against `ProgramContent.ProgramId == Assessment.CourseId`.
- Add an index or uniqueness rule for active `Assessment.ContentId` projections if the backend keeps one active projection per gradable content item.
- `AssessmentSubmission.StructuredAnswerPayload` stores submitted answers.
- `AssessmentSubmission.Score`, `Passed`, `GradedAt`, and `Feedback` store trusted server results.
- `AssessmentGroup` maps to `outcome.gradebook.groupId`.
- Learner dashboard/workspace continues consuming `AssessmentSubmission.Score` where possible.

## Part 3 Backend Phases

### Phase 3.1: Storage Shape Decision

- Choose between dedicated `ProgramContent` fields, a related content-grading table, or a versioned settings table.
- Choose answer-key storage shape.
- Define definition versioning and attempt snapshot rules.
- Create/update/delete an `Assessment` projection for every content item where grading is enabled.
- Define one-active-projection semantics for `Assessment.ContentId`.

### Phase 3.2: Authoring APIs

- Extend content save/load contracts with grading definition.
- Validate definitions server-side.
- Extract and store answer keys server-side.
- Maintain the assessment projection whenever grading is enabled.
- Validate that projected assessments link only to content in the same course.

### Phase 3.3: Learner-Safe Delivery

- Add backend redaction through the content-type adapter contract.
- Return redacted learner payloads for graded content.
- Never expose answer keys in learner payloads.
- Include only runtime policy metadata needed by the learner.

### Phase 3.4: Submission and Server Grading

- Accept structured answer payloads.
- Ignore client-provided score/correctness fields.
- Snapshot content body, grading definition, and answer-key version for the attempt.
- Grade deterministic quiz submissions server-side.
- Return pending/manual for non-deterministic items.

### Phase 3.5: Result Propagation

- Apply `outcome.uses`.
- Feedback-only results are stored for learner feedback and operational review, but stay out of gradebook/final grade.
- Gradebook results update assessment/submission analytics.
- Final grade propagation waits for a canonical aggregation owner decision if the current backend still has multiple grade-bearing models.

## Part 3 Security Tests

- Learner payload contains no answer keys.
- Submissions with injected score are ignored.
- Submissions with injected `isCorrect` are ignored.
- Submissions with injected answer keys are ignored.
- Server grading uses persisted answer keys, not learner payload data.
- Attempt snapshots remain stable after content edits.

## Part 4 Handoff

After Part 3 has a server grading path, Part 4 adds authoring preview and grading dry-run.

Handoff requirements:

- backend can return learner-safe redacted payloads for authored content;
- backend can grade deterministic quiz submissions without trusting client correctness;
- dry-run can reuse the same adapter and grading engine as official submissions;
- dry-run can avoid official attempt, progress, gradebook, and final-grade writes;
- frontend can render a learner-like quiz preview from the redacted payload.

See `learning-grading-part-4-authoring-preview-and-dry-run.md`.

## Open Backend Decisions

- Exact persistence shape for content-owned grading definitions.
- Exact persistence shape for answer keys and versioned snapshots.
- Exact lifecycle behavior for one active `Assessment` projection per gradable content item.
- Which grade aggregation model becomes canonical for final course grade.
- Whether manual grading enters the first server implementation or follows deterministic quiz grading.
