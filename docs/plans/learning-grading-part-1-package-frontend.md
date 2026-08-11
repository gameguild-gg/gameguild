# Learning Grading Part 1: Completed Package and Frontend Handoff

## Summary

Part 1 has already been executed. This document is now a historical snapshot and handoff, not a new implementation target.

Part 1 created the first grading package and connected quiz authoring to content-owned grading metadata. Some of that first-pass logic is now intentionally superseded by the server-only architecture. Those corrections belong to Part 2 and later.

## Completed Scope

Part 1 delivered:

- the initial `@game-guild/grading` package;
- content-owned quiz grading metadata;
- quiz content save/reload through the existing content path;
- initial grading metadata helpers;
- initial assessments projection over graded content;
- preservation of lesson and quiz editor componentization;
- `React.lazy` plus `Suspense` loading, with no `next/dynamic`.

## Completed Contract Shape

Part 1 started from a first-pass contract similar to:

```ts
export type GradingValidationMode =
  | 'public'
  | 'protected';

export interface ContentGradingConfig {
  enabled: boolean;
  schemaVersion: number;
  validationMode: GradingValidationMode;
  gradebook: GradebookConfig;
  policy: GradingPolicy;
  items: Record<string, GradedItemConfig>;
}
```

This shape is useful only as the current input to Part 2. It should not be treated as the final contract.

## Durable Decisions

Keep these decisions from Part 1:

- `Content` remains the authoring source for lesson, quiz, assignment, project, code, discussion, reflection, survey, and future activity data.
- `Assessments` remains a view/projection over graded content, not a separate quiz or assignment authoring owner.
- `@game-guild/grading` remains a framework-independent package.
- Quiz is the first content type to use the grading contract.
- The content editor saves quiz body data through `ProgramContent.JsonBody`.
- Assessment rows link back to the owning content item.

## Superseded Logic

Part 2 must remove or replace these Part 1 choices:

- `validationMode: 'public' | 'protected'`;
- public/client validation as a learner runtime;
- client-produced correctness as any trusted grading evidence;
- public/client results as any persisted grading source of truth;
- quiz-specific helpers living on the core package surface instead of behind an adapter;
- `ContentGradingConfig` as the long-term contract name/shape.

All learner-facing grading validation now happens on the server.

## Current Backend Constraints

The backend already exposes simple grading fields on `ProgramContent`:

- `GradingMethod`;
- `MaxPoints`.

The backend also currently enforces important rules:

- `Lesson` and `Page` normalize grading to none.
- `Survey` normalizes grading to none.
- `Questionnaire`, `Code`, and `Project` are structured-body content types and are better first candidates under the current backend rules.

These constraints should be reconciled in Part 3 after Part 2 cleans the frontend/package contract.

## Part 2 Handoff

Part 2 starts from the already-executed Part 1 state:

- package and frontend exist with `validationMode`;
- quiz adapter boundaries are not clean yet;
- client-side quiz grading helpers exist and may be used in learner-facing paths;
- the backend audit indicates `AssessmentSubmission` is the better first trusted server-side submission store;
- backend persistence for the full content-owned grading definition is still undecided.

Part 2 owns the contract correction:

- remove `validationMode`;
- add `GradingResultUse` and `GradingOutcomePolicy`;
- rename/reshape `ContentGradingConfig` toward `ContentGradingDefinition`;
- move quiz-specific extraction/redaction/payload logic behind a quiz adapter;
- remove learner-facing client correctness as a grading path;
- keep content save/reload behavior stable while the contract changes.

## Handoff Validation

Part 2 should prove that:

- existing content without grading still saves and loads unchanged;
- quiz content can keep its authored body data through `ProgramContent`;
- quiz grading metadata migrates away from `validationMode`;
- result use replaces public/protected controls;
- direct assessment body editing remains absent;
- assessment rows, if shown, link back to the owning content item;
- no `quiz-assessment-editor` returns;
- `React.lazy` plus `Suspense` remain in place;
- no `next/dynamic` is introduced for the content editor route.
