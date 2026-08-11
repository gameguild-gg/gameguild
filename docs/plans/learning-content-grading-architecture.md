# Learning Content Grading Architecture Plan

## Summary

`Content` is the source of truth for course structure and learner activities. A course item can be a lesson, quiz, assignment, project, code exercise, discussion, survey, reflection, or another supported activity type. Some of these content items can have grading enabled.

`Assessments` is an operational view over graded `Content`, not a separate domain that owns quiz or assignment definitions.

The grading system lives in its own feature package and provides typed contracts for scores, weights, answer keys, feedback, attempt policy, learner-safe redaction, structured submissions, and server-side validation. `block-content-editor` remains responsible for authoring and rendering content blocks, while the grading package describes how those blocks become graded.

All official evaluation is server-side. There is no `validationMode` split. The client can author grading definitions and submit learner answers, but it never validates correctness or produces trusted scores.

## Core Vocabulary

- **Content**: any course item in the content tree.
- **Gradable content**: a content item with grading enabled.
- **Assessment view**: dashboard surface that lists, groups, routes, and analyzes gradable content.
- **Grading definition**: versioned contract that describes score policy, attempt policy, feedback policy, graded items, answer-key references, and result usage.
- **Grading adapter**: content-type-specific implementation that knows how to extract graded items, answer keys, redacted learner payloads, and structured answer payloads for a content type.
- **Result use**: the configured purpose for a server-produced grading result: learner feedback only, or a gradebook entry.
- **Authoring payload**: full instructor-owned content data, including correct answers when the content type needs them.
- **Learner payload**: redacted content data safe to send to students.
- **Structured answer payload**: learner submission payload sent to the server for grading.

## Server-Side Validation

Server-side validation is the only supported grading authority.

Rules:

- answer keys must not be required by learner clients;
- learner payloads must be redacted by the backend before delivery;
- submissions contain learner answers only;
- client-provided score, correctness, answer key, or `isCorrect` values are ignored;
- deterministic grading runs on the server;
- manual, external, or asynchronous grading returns pending/manual status until resolved;
- gradebook and pass/fail decisions consume only server-produced grading results.

Instructor preview can display correctness only when the value comes from a server dry-run response. The client must not compute trusted correctness for learner runtime or author preview.

## Result Use

The old `validationMode: 'public' | 'protected'` split is removed. A grading result is always server-validated, and a separate policy decides where that result is used.

Target type direction:

```ts
export type GradingResultUse =
  | 'feedback'
  | 'gradebook';

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

Examples:

- exercise with evaluation but no global grade: `uses: ['feedback']`;
- quiz that contributes to the course grade: `uses: ['feedback', 'gradebook']`.

`gradebook` details are only meaningful when `uses` includes `gradebook`.

Future workflows such as completion, certificate eligibility, placement, or analytics should consume the server-produced result from their own feature context. They should not become first-class `GradingResultUse` values until the platform has a concrete rule that belongs inside the grading package itself.

## Target Ownership

### Content

`ProgramContent` owns the course item and its authored activity data.

Target shape:

```ts
interface ContentItem {
  id: string;
  type: ContentType;
  title: string;
  body: unknown;
  grading?: ContentGradingDefinition;
}
```

`body` contains content/editor data. `grading` contains the content-owned grading contract.

### Grading Package

Package path:

```text
packages/features/grading
```

Package name:

```text
@game-guild/grading
```

The package owns:

- grading schema types;
- score, weight, and result-use rules;
- attempt and availability policy types;
- feedback policy types;
- answer-key extraction contracts;
- learner-payload redaction contracts;
- structured submission payload contracts;
- grade result contracts;
- adapter registration contracts;
- content-type adapters, starting with quiz;
- validation helpers shared by web and API tests.

The package must remain UI-free and framework-independent. It must not import React, Next.js, dashboard code, or API clients.

### Grading Adapters

The core grading logic is content-type agnostic. Content-type details live in adapters.

Adapter direction:

```ts
export interface GradingAdapter<TAuthoringPayload = unknown> {
  contentType: string;
  extractItems(payload: TAuthoringPayload): Record<string, GradedItemConfig>;
  extractAnswerKey(payload: TAuthoringPayload, grading: ContentGradingDefinition): AnswerKey;
  redactLearnerPayload(payload: TAuthoringPayload, grading: ContentGradingDefinition): unknown;
  buildStructuredAnswerPayload(input: unknown, grading: ContentGradingDefinition): StructuredAnswerPayload;
}
```

Quiz is the first adapter, but the package should be ready for code, project, file submission, reflection, survey-like rubric cases, video checkpoints, and future activity types.

### Block Content Editor

`block-content-editor` owns the editing experience for blocks.

Allowed responsibility:

- author quiz/question blocks;
- expose stable block IDs and metadata;
- expose extension points for grading metadata panels;
- collect learner answer inputs for submission payloads.

Not allowed responsibility:

- trusted scoring;
- gradebook rules;
- answer-key storage for learner runtime;
- server trust decisions;
- assessment analytics.

### Assessments

`Assessments` is a view over graded `Content`.

It should:

- list content items where `grading.enabled === true`;
- group graded content by gradebook group/category when configured;
- show max score, weight, result use, attempts, availability, and grading status;
- route edits back to the owning content item;
- display analytics from submissions/gradebook;
- never become the source of truth for quiz or assignment body data.

## Content Grading Contract

Target contract direction:

```ts
interface ContentGradingDefinition {
  enabled: boolean;
  schemaVersion: number;
  outcome: GradingOutcomePolicy;
  score: ScorePolicy;
  attempts: AttemptPolicy;
  feedback: FeedbackPolicy;
  presentation: PresentationPolicy;
  items: Record<string, GradedItemConfig>;
}

interface ScorePolicy {
  maxScore: number;
  passingScore?: number;
}

interface AttemptPolicy {
  maxAttempts?: number | null;
  timeLimitMinutes?: number | null;
  availableFrom?: string | null;
  availableUntil?: string | null;
  dueAt?: string | null;
  allowLateSubmissions?: boolean;
  lateSubmissionDeadline?: string | null;
}

interface FeedbackPolicy {
  mode?: 'immediate' | 'after-submit' | 'after-close' | 'manual';
}

interface PresentationPolicy {
  mode?: 'continuous' | 'single-step';
}

interface GradedItemConfig {
  contentBlockId: string;
  points: number;
  gradingKind: 'deterministic' | 'manual' | 'external';
  answerKeyRef?: string;
  rubricRef?: string;
}
```

The exact names can change during implementation. The durable boundary is that grading metadata references content blocks instead of becoming embedded as a quiz-only concern.

## Quiz as First Adapter

Quiz should be the first implementation target because it already exists in the editor.

For quiz content:

- quiz block data remains in content body;
- grading config references quiz/question block IDs;
- answer keys are extracted by the quiz adapter for server-side storage/execution;
- learner payload is redacted by the quiz adapter;
- learner submissions use structured answer payloads;
- deterministic question types can be graded server-side;
- manual or unsupported question types return pending/manual state.

This keeps the model ready for assignments, projects, code exercises, file submissions, video checkpoints, and other future content types.

## Backend Flow

### Authoring

When an instructor saves a content item:

1. Save `ProgramContent` body data.
2. Save the content-owned grading definition when grading is enabled.
3. Validate the grading definition against the grading package schema or mirrored .NET schema.
4. Use the content-type adapter to extract answer-key material.
5. Store answer-key material only in server-owned data.
6. Maintain the `Assessment` projection used for server-side submission/result infrastructure when grading is enabled.

### Learner Start

When a learner opens gradable content:

1. Load the content item.
2. Load its grading definition.
3. If no grading is enabled, return normal content payload.
4. If grading is enabled, return the adapter-redacted learner payload.
5. Include only policy metadata needed by the learner runtime, such as attempt limits, time limit, availability, and presentation mode.
6. Ensure feedback-only and gradebook-bound content both use server-side submission/result handling.

### Submission

When a learner submits:

1. Accept only the learner answer payload.
2. Ignore client-provided score, correctness, answer key, and `isCorrect`.
3. Store answers as structured submission data.
4. Grade on the server using server-owned answer keys and the relevant adapter.
5. Publish the result according to `outcome.uses`.
6. Store feedback-only results for learner feedback and operational review without writing gradebook or final-grade aggregates.

## Frontend Flow

### Content Page

The content editor should let the instructor:

- edit the content body;
- enable or disable grading for that content item;
- configure result use;
- configure max score, passing score, weights, attempts, time limits, feedback, and presentation;
- assign points to individual blocks/questions;
- save everything through the content-owned path.

The same content item remains visible in the content tree.

### Assessments Page

The assessments page should:

- query graded content items;
- show them as assessment activities;
- offer filters/grouping by content type, result use, grade group, status, and required/optional;
- link to the same content editor;
- optionally open a focused grading settings panel for the content item.

There should not be a separate `quiz-assessment-editor` that stores a second quiz definition.

## Package API Direction

The grading package should expose framework-independent APIs similar to:

```ts
validateGradingDefinition(config: unknown): ContentGradingDefinition;
normalizeGradingDefinition(config: Partial<ContentGradingDefinition>): ContentGradingDefinition;
sumGradedItemPoints(config: ContentGradingDefinition): number;
getResultUses(config: ContentGradingDefinition): readonly GradingResultUse[];
registerGradingAdapter(adapter: GradingAdapter): void;
getGradingAdapter(contentType: string): GradingAdapter | null;
```

Adapter APIs:

```ts
extractAnswerKey(contentType: string, contentBody: unknown, grading: ContentGradingDefinition): AnswerKey;
redactLearnerPayload(contentType: string, contentBody: unknown, grading: ContentGradingDefinition): unknown;
buildStructuredAnswerPayload(contentType: string, input: unknown, grading: ContentGradingDefinition): StructuredAnswerPayload;
```

Server execution can use generated schemas, mirrored .NET contracts, or shared test vectors from the package.

## Implementation Split

### Part 0: Correct the Accidental Coupling

Completed cleanup target:

- Stop treating `Assessment` as the owner of quiz definitions.
- Remove or quarantine `quiz-assessment-editor` as a separate quiz definition editor.
- Remove frontend-only `SubmissionModality` decisions from content and assessment editors.
- Keep `Assessments` as a list/view concept, even if the current backend still exposes assessment entities.
- Keep current quiz content save behavior intact.
- Preserve all content type options: `Lesson`, `Quiz`, `Assignment`, `Project`, `Discussion`, `Code`, `Reflection`, and `Survey`.

### Part 1: Initial Package and Frontend Contract

Part 1 has already been executed. It introduced the initial grading package and quiz authoring integration. Its `validationMode` split is now superseded by the server-only decision, but that correction belongs to Part 2 and later.

Keep:

- content-owned grading metadata;
- `@game-guild/grading` as a framework-independent package;
- quiz as the first consumer;
- assessments as a view over graded content;
- focused tests around content save/reload behavior.

Part 2 must replace:

- `validationMode`;
- public/client validation as a learner runtime;
- quiz-only package shape at the core level.

### Part 2: Contract Adjustment for Server-Only Grading

Part 2 cleans up the package and frontend contract before backend integration.

Main work:

- remove `validationMode`;
- add `GradingResultUse` and `GradingOutcomePolicy`;
- rename/reshape `ContentGradingConfig` toward `ContentGradingDefinition`;
- move quiz-specific logic behind a quiz adapter;
- keep core grading APIs content-type agnostic;
- remove learner-facing client correctness as a grading path;
- update docs and tests to match server-only behavior;
- prepare backend-facing schemas/test vectors for Part 3.

### Part 3: Backend Integration

Part 3 connects server-side grading to the existing Learning backend.

Target direction:

- `ProgramContent` remains the authoring source of truth.
- A content-owned grading definition is persisted with or beside `ProgramContent`.
- `Assessment` remains projection/submission infrastructure, not authored content source.
- Every content item with `grading.enabled === true` gets a server-side submission/result path.
- The first backend implementation should prefer one active `Assessment` projection per gradable content item so feedback-only and gradebook-bound content can both reuse `AssessmentSubmission`.
- `AssessmentSubmission.StructuredAnswerPayload` stores learner answers for server-side grading.
- `AssessmentSubmission.Score`, `Passed`, `GradedAt`, and `Feedback` store trusted grading results for feedback-only and gradebook-bound content.
- `outcome.uses` controls whether those trusted results propagate into gradebook/final-grade flows.
- `AssessmentGroup` remains the gradebook grouping surface when `outcome.uses` includes `gradebook`.
- Existing learner dashboard/workspace score queries should continue consuming the trusted backend result path.

### Part 4: Authoring Preview and Grading Dry Run

Part 4 adds a preview path for authors to test quiz content and server-side grading without creating an official learner attempt.

Target direction:

- `quiz-content-editor` gets a `Preview` action.
- Preview renders the quiz with the learner-facing payload shape.
- Preview submits answers to a server dry-run endpoint.
- Dry-run uses the same adapter and grading engine as real learner submissions.
- Dry-run does not create an official attempt, gradebook entry, or learner progress event.
- Dry-run ignores client-provided correctness, score, answer key, and `isCorrect`, just like official grading.
- Preview result can show score, item feedback, and redaction/debug warnings to the author.
- Preview must make it easy to verify that learner payloads do not expose answer keys.

## Test Plan

- Content without grading saves and loads unchanged.
- Content with grading enabled appears in Assessments.
- Content with grading disabled does not appear in Assessments.
- Quiz grading definition saves and reloads without `validationMode`.
- Quiz adapter extracts graded items and answer keys from authoring payloads.
- Learner payload does not expose correct answers.
- Tampered submissions with score, correctness, answer key, or `isCorrect` are ignored.
- Server-side deterministic quiz grading produces trusted results.
- Result use controls whether a result appears only as feedback or also enters the gradebook.
- Author preview can test quiz rendering and server-side dry-run grading without writing gradebook data.
- Assessments page lists quiz, assignment, project, code, survey, reflection, and other future graded content types through the same content-grading contract.
- Existing `React.lazy` + `Suspense` loading remains in place; no `next/dynamic` in the editor route.

## Open Decisions

- Whether the grading definition should live as a dedicated `ProgramContent` field, a related table, or a versioned settings object.
- How answer keys should be stored for complex content types such as code projects and file submissions.
- Exact lifecycle rules for the one-active-projection-per-gradable-content model, including create/update/delete behavior.
- Which existing grade aggregation owner becomes canonical for final course grade propagation.
- Whether manual grading should be part of the first server-side implementation or deferred until deterministic quiz grading is stable.
- Whether grading dry-run results should be persisted as ephemeral audit/debug records or remain response-only.
