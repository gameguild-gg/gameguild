# Learning Grading Part 1: Package and Frontend Contract

## Summary

Part 1 creates the grading contract and integrates it into the frontend authoring flow without committing to the final backend storage model.

After Part 0, `Content` is again the authoring source for lesson and quiz body data, while `Assessments` no longer owns a separate quiz definition. Part 1 should keep that boundary: grading metadata belongs to content-owned authoring, and the `Assessments` page can only show a temporary projection until Part 2 decides the backend source of truth.

## Goals

- Create a UI-free grading package with typed contracts and schema validation.
- Define `validationMode: 'public' | 'protected'`.
- Encode that `public` validation is pedagogical and never official.
- Let quiz content be the first consumer of the grading contract.
- Keep quiz body data in `ProgramContent.Body` or `ProgramContent.JsonBody`, depending on the current content storage path.
- Add frontend serialization tests for grading config.
- Prepare `Assessments` to consume graded content as a view, without restoring the old `quiz-assessment-editor` path.

## Non-Goals

- Do not implement final protected server-side grading in Part 1.
- Do not add migrations for the full grading config in Part 1.
- Do not make `Assessment.DefinitionPayload` the new source of truth.
- Do not make `Assessments` create or own quiz body data.
- Do not enable official grading for `public` validation.
- Do not force lesson-embedded quiz grading until Part 2 resolves backend rules that currently prevent grading `Lesson` content.

## Current Constraints

The current backend already exposes simple grading fields on `ProgramContent`:

- `GradingMethod`
- `MaxPoints`

The current backend also enforces important rules:

- `Lesson` and `Page` normalize grading to none.
- `Survey` normalizes grading to none.
- `Questionnaire`, `Code`, and `Project` are structured-body content types and are better first candidates.

The current web projection maps `gradingMethod` and `maxPoints` only into `ContentItemDetail.settings`, not into the content tree list item. If the `Assessments` page needs a temporary graded-content projection, Part 1 should explicitly type and map these fields instead of hiding them in generic metadata.

## Phase 1A: Grading Package

Create:

```text
packages/features/grading
```

Proposed package name:

```text
@game-guild/grading
```

The package must be framework-independent. It should not import React, Next.js, dashboard code, or API clients.

Initial exports:

```ts
export type GradingValidationMode = 'public' | 'protected';

export interface ContentGradingConfig {
  enabled: boolean;
  schemaVersion: number;
  validationMode: GradingValidationMode;
  gradebook: GradebookConfig;
  policy: GradingPolicy;
  items: Record<string, GradedItemConfig>;
}

export interface GradebookConfig {
  maxScore: number;
  passingScore?: number;
  weight?: number;
  groupId?: string | null;
  required?: boolean;
  official?: boolean;
}

export interface GradingPolicy {
  maxAttempts?: number | null;
  timeLimitMinutes?: number | null;
  availableFrom?: string | null;
  availableUntil?: string | null;
  feedbackMode?: 'immediate' | 'after-submit' | 'after-close' | 'manual';
  presentationMode?: 'continuous' | 'single-step';
}

export interface GradedItemConfig {
  contentBlockId: string;
  points: number;
  gradingKind: 'deterministic' | 'manual' | 'external';
  answerKeyRef?: string;
  rubricRef?: string;
}
```

Package responsibilities:

- validate grading config shape;
- normalize defaults;
- validate score totals;
- validate public/protected policy compatibility;
- expose types for answer keys, structured submissions, and grade results;
- expose deterministic quiz helper contracts, even if protected execution is deferred;
- provide redaction function signatures for Part 2.

Recommended helpers:

```ts
validateGradingConfig(config: unknown): ContentGradingConfig;
normalizeGradingConfig(config: Partial<ContentGradingConfig>): ContentGradingConfig;
isOfficialGrade(config: ContentGradingConfig): boolean;
assertPublicIsNotOfficial(config: ContentGradingConfig): void;
sumGradedItemPoints(config: ContentGradingConfig): number;
```

Protected helpers can exist as contracts/stubs if backend integration is not ready:

```ts
redactLearnerPayload(contentBody: unknown, grading: ContentGradingConfig): unknown;
buildStructuredSubmissionPayload(input: unknown, grading: ContentGradingConfig): StructuredSubmissionPayload;
gradeDeterministicSubmission(args: GradeSubmissionArgs): GradeResult;
```

## Phase 1B: Frontend Authoring Contract

Add frontend support for attaching a grading config to content authoring state.

Initial target:

- quiz content item (`Questionnaire`) only;
- `validationMode: 'public'` fully available;
- `validationMode: 'protected'` represented in the config, but not treated as officially executable until Part 2.

The content editor should be able to:

- leave grading disabled;
- enable grading metadata for quiz content;
- choose `public` or `protected`;
- configure max score and optional passing score;
- assign points to quiz questions or quiz blocks when stable block IDs exist;
- serialize the grading config through a local frontend contract.

Important boundary:

- Quiz questions remain in the existing quiz/block content body.
- Grading metadata references quiz/question/block IDs.
- The grading package does not become a React UI package.

## Phase 1C: Public Validation Runtime

Implement public validation as a pedagogical learner/client path.

Public mode rules:

- client payload may contain answer keys;
- correctness can be calculated in the browser;
- feedback can be immediate;
- any score shown is practice feedback only;
- public validation must not write official gradebook data;
- UI copy should clearly distinguish practice feedback from official grading.

This is the only validation mode that should be considered functional in Part 1.

## Phase 1D: Protected Mode Placeholder

Protected mode should be typed and serializable, but not fully executable in Part 1.

Protected mode rules:

- the frontend may author intent and config;
- learner-safe redaction is not guaranteed until Part 2;
- server-side validation is not implemented in Part 1;
- official grade submission should remain disabled or clearly blocked;
- any protected flow that would require answer-key secrecy must wait for backend storage and attempt snapshot decisions.

This prevents Part 1 from recreating the mistake from Part 0 by inventing a frontend-only authority for official grading.

## Phase 1E: Temporary Assessments Projection

`Assessments` should continue to behave as a view, not an authoring owner.

For Part 1, use a temporary projection only if needed:

- read graded-looking content from `ProgramContent.GradingMethod` and `ProgramContent.MaxPoints`;
- map these fields into typed frontend content models instead of burying them in generic metadata;
- show rows as content-owned activities;
- route edit actions back to the content editor;
- do not add direct assessment creation;
- do not restore assessment-body editing.

This projection is intentionally incomplete. Part 2 decides whether the final source is `ProgramContent`, a related content-grading table, versioned settings, or a bridge to existing assessment/submission infrastructure.

## Implementation Order

1. Add `packages/features/grading` package scaffold.
2. Add grading config types and schema validation.
3. Add unit tests for validation and public/protected invariants.
4. Add quiz grading metadata extraction helpers for current quiz block data.
5. Add content editor state wiring for quiz grading config.
6. Add public-mode client validation using the package.
7. Add a temporary assessments projection only after content-owned grading data is visible in the frontend model.
8. Update focused dashboard tests.

## Validation Plan

Package:

- invalid configs are rejected;
- `public` plus `official: true` is rejected or normalized to non-official;
- item point totals are validated;
- disabled grading keeps config minimal and inert.

Content editor:

- content without grading saves and loads unchanged;
- quiz content can enable grading metadata;
- quiz content can disable grading metadata again;
- quiz public validation works in the client;
- quiz protected mode can be authored but does not claim official execution.

Assessments view:

- direct assessment body editing remains absent;
- assessment rows, if shown, link back to the owning content item;
- group-only management remains allowed;
- no `quiz-assessment-editor` returns.

Regression:

- lesson and quiz content save paths continue to use `ProgramContent`;
- `React.lazy` plus `Suspense` remain in the lesson editor path;
- no `next/dynamic` is introduced for the content editor route.

## Open Decisions Deferred To Part 2

- Where the full grading config is persisted.
- Where protected answer keys are stored.
- How protected learner attempts snapshot content body plus grading config.
- Whether official submissions should use `AssessmentSubmission`, `ContentInteraction` plus `ActivityGrade`, or a bridge.
- How grade groups attach to content-owned grading config.
- How public practice results are stored without affecting official course grades.
- When lesson-embedded quizzes can become gradable, given current backend restrictions on `Lesson` grading.
