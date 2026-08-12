# Learning Grading Part 1: Package and Frontend Baseline

## Summary

Part 1 defines the current frontend/package baseline for content-owned grading work.

The grading package exists, quiz authoring is connected to content-owned grading metadata, and official grading for grading-enabled content is server-side.

## Current Scope

The current baseline includes:

- the `@game-guild/grading` package;
- content-owned quiz grading metadata;
- quiz content save/reload through the existing content path;
- grading metadata helpers;
- assessments projection over graded content;
- preservation of lesson and quiz editor componentization;
- `React.lazy` plus `Suspense` loading, with no `next/dynamic`.

## Current Contract Direction

The grading contract is content-owned and server-validated whenever grading is enabled:

```ts
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
```

## Current Decisions

Keep these decisions:

- `Content` remains the authoring source for lesson, quiz, assignment, project, code, discussion, reflection, survey, and future activity data.
- `Assessments` remains a view/projection over graded content, not a separate quiz or assignment authoring owner.
- `@game-guild/grading` remains a framework-independent package.
- Quiz is the first content type to use the grading contract.
- The content editor saves quiz body data through `ProgramContent.JsonBody`.
- Assessment rows link back to the owning content item.

## Runtime Boundary

All learner-facing grading validation for grading-enabled content happens on the server.

Client-side quiz correctness is only local-practice feedback for content where grading is disabled. It is not trusted grading evidence and is not persisted as a score.

Quiz-specific extraction, redaction, payload shaping, and deterministic grading behavior live behind the quiz adapter instead of the core package surface.

## Current Backend Constraints

The backend already exposes simple grading fields on `ProgramContent`:

- `GradingMethod`;
- `MaxPoints`.

The backend also currently enforces important rules:

- `Lesson` and `Page` normalize grading to none.
- `Survey` normalizes grading to none.
- `Questionnaire`, `Code`, and `Project` are structured-body content types and are better first candidates under the current backend rules.

These constraints should be reconciled in Part 3 after the frontend/package contract is stable.

## Part 2 Handoff

Part 2 continues from this baseline:

- quiz adapter boundaries are not clean yet;
- client-side quiz grading helpers exist and may be used in learner-facing paths;
- the backend audit indicates `AssessmentSubmission` is the better first trusted server-side submission store;
- backend persistence for the full content-owned grading definition is still undecided.

Part 2 owns the contract tightening:

- add `GradingResultUse` and `GradingOutcomePolicy`;
- use `ContentGradingDefinition` as the content-owned grading contract;
- move quiz-specific extraction/redaction/payload logic behind a quiz adapter;
- remove learner-facing client correctness as a grading path;
- keep content save/reload behavior stable while the contract changes.

## Handoff Validation

Part 2 should prove that:

- existing content without grading still saves and loads unchanged;
- quiz content can keep its authored body data through `ProgramContent`;
- quiz grading metadata saves and reloads through the current grading definition;
- result use controls save expected metadata;
- direct assessment body editing remains absent;
- assessment rows, if shown, link back to the owning content item;
- no `quiz-assessment-editor` returns;
- `React.lazy` plus `Suspense` remain in place;
- no `next/dynamic` is introduced for the content editor route.
