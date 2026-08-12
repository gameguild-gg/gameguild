# Learning Content Grading Architecture Plan

## Summary

`Content` is the source of truth for course structure and learner activities. A course item can be a lesson, quiz, assignment, project, code exercise, discussion, survey, reflection, or another supported activity type. Some of these content items can have grading enabled.

`Assessments` is an operational view over graded `Content`, not a separate domain that owns quiz or assignment definitions.

The grading system lives in its own feature package and provides typed contracts for scores, weights, answer keys, feedback, attempt policy, learner-safe redaction, structured submissions, and server-side validation. `block-content-editor` remains responsible for authoring and rendering content blocks, while the grading package describes how those blocks become graded.

All official evaluation for grading-enabled content is server-side. The client can author grading definitions, collect learner answers, and show local practice feedback when grading is disabled, but it never validates trusted correctness or produces trusted scores.

## Core Vocabulary

- **Content**: any course item in the content tree.
- **Gradable content**: a content item with grading enabled.
- **Ungraded practice**: a content item with grading disabled. It may use client-side correctness for pedagogy, but it does not create trusted submissions, gradebook entries, or official analytics.
- **Assessment view**: dashboard surface that lists, groups, routes, and analyzes gradable content.
- **Grading definition**: versioned contract that describes score policy, attempt policy, feedback policy, graded items, answer-key references, and result usage.
- **Grading adapter**: content-type-specific implementation that knows how to extract graded items, answer keys, redacted learner payloads, and structured answer payloads for a content type.
- **Result use**: the configured purpose for a server-produced grading result: learner feedback only, or a gradebook entry.
- **Trusted grading result**: a score, correctness result, pass/fail result, or feedback result produced by the server for grading-enabled content.
- **Authoring payload**: full instructor-owned content data, including correct answers when the content type needs them.
- **Learner payload**: redacted content data safe to send to students.
- **Structured answer payload**: learner submission payload sent to the server for grading.

## Server-Side Validation

Server-side validation is the only supported authority for grading-enabled or official results.

Rules:

- these rules apply whenever `grading.enabled === true`;
- answer keys must not be required by learner clients;
- learner payloads must be redacted by the backend before delivery;
- submissions contain learner answers only;
- client-provided score, correctness, answer key, or `isCorrect` values are ignored;
- deterministic grading runs on the server;
- manual, external, or asynchronous grading returns pending/manual status until resolved;
- gradebook and pass/fail decisions consume only server-produced grading results.

Grading-enabled instructor preview can display correctness only when the value comes from a server dry-run response. Ungraded practice preview may display local correctness, but that result is pedagogical only and must not be stored or submitted as grading evidence.

## Result Use

A grading result is always server-validated, and a separate policy decides where that result is used.

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

`uses: ['feedback']` still means grading is enabled and server-side. It is not the same as grading disabled. Grading disabled content may show local practice feedback, but it does not create a trusted grading result.

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
- when grading is disabled, quiz blocks may use the full authoring payload for local practice feedback in the client;
- local practice feedback is never stored or submitted as grading evidence;
- when grading is enabled, learner payload is redacted by the quiz adapter;
- grading-enabled learner submissions use structured answer payloads;
- grading-enabled deterministic question types are graded server-side;
- grading-enabled manual or unsupported question types return pending/manual state.

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
3. If no grading is enabled, return normal content payload; quiz blocks may run local-practice feedback in the client.
4. If grading is enabled, return the adapter-redacted learner payload.
5. Include only policy metadata needed by the learner runtime, such as attempt limits, time limit, availability, and presentation mode.
6. Ensure feedback-only and gradebook-bound content both use server-side submission/result handling.

### Grading-Enabled Submission

When a learner submits:

1. Accept only the learner answer payload.
2. Ignore client-provided score, correctness, answer key, and `isCorrect`.
3. Store answers as structured submission data.
4. Grade on the server using server-owned answer keys and the relevant adapter.
5. Publish the result according to `outcome.uses`.
6. Store feedback-only results for learner feedback and operational review without writing gradebook or final-grade aggregates.

Content with grading disabled does not create an official grading submission. Any local correctness shown by the client is transient practice feedback.

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

Part 1 introduced the grading package and quiz authoring integration that remain the baseline for this work.

Keep:

- content-owned grading metadata;
- `@game-guild/grading` as a framework-independent package;
- quiz as the first consumer;
- assessments as a view over graded content;
- focused tests around content save/reload behavior.

Part 2 keeps official grading server-side, preserves local practice only when grading is disabled, and moves quiz-specific behavior behind an adapter.

### Part 2: Contract Adjustment for Server-Only Grading

Part 2 cleans up the package and frontend contract before backend integration.

Main work:

- add `GradingResultUse` and `GradingOutcomePolicy`;
- keep `ContentGradingDefinition` as the content-owned grading contract;
- move quiz-specific logic behind a quiz adapter;
- keep core grading APIs content-type agnostic;
- remove learner-facing client correctness as a grading path for grading-enabled content;
- keep explicitly local practice feedback for content with grading disabled;
- update docs and tests to match server-only behavior;
- prepare backend-facing schemas/test vectors for Part 3.

### Part 2.5: Quiz Adapter Hardening

Part 2.5 closes the safety and precision gaps found after the Part 2 review, before the backend starts trusting the quiz adapter.

Main work:

- make quiz learner redaction type-aware for every authored quiz type;
- remove answer-key leaks from matching, ordering, categorization, dropdown, word bank, numeric, formula, hotspot, and highlight learner payloads;
- align `gradingKind` with the adapter's real deterministic/manual/external/unsupported capabilities;
- normalize structured answer payloads by whitelisting allowed learner answer fields;
- add backend-facing test vectors for deterministic, manual, and unsupported quiz cases;
- keep grading-enabled quiz runtime as answer collection/submission state only, not client correctness;
- keep ungraded quiz runtime explicitly marked as local practice when it shows client-side correctness.

### Part 2.5B: Quiz Source Type Safety

Part 2.5B follows the adapter hardening by improving the quiz contracts at the `block-content-editor` source.

Main work:

- split quiz authoring, learner-safe, practice, and answer/submission payload types;
- make learner-safe renderers valid by type instead of relying on casts or missing answer-key fields;
- keep answer-key fields restricted to authoring and local-practice paths;
- prefer fixing quiz source contracts/invariants before adding more defensive compensation in the grading quiz adapter;
- add source validation so incomplete quiz definitions do not become deterministic grading items;
- align frontend learner-safe conversion with the grading adapter's redaction behavior.

### Part 3: Backend Integration

Part 3 connects server-side grading to the existing Learning backend.

Target direction:

- `ProgramContent` remains the authoring source of truth.
- A content-owned grading definition is persisted with or beside `ProgramContent`.
- `Assessment` remains projection/submission infrastructure, not authored content source.
- Every content item with `grading.enabled === true` gets a server-side submission/result path.
- Content with grading disabled does not create an `Assessment` projection or `AssessmentSubmission` path.
- The first backend implementation should prefer one active `Assessment` projection per gradable content item so feedback-only and gradebook-bound content can both reuse `AssessmentSubmission`.
- `AssessmentSubmission.StructuredAnswerPayload` stores learner answers for server-side grading.
- `AssessmentSubmission.Score`, `Passed`, `GradedAt`, and `Feedback` store trusted grading results for feedback-only and gradebook-bound content.
- `outcome.uses` controls whether those trusted results propagate into gradebook/final-grade flows.
- `outcome.uses: ['feedback']` is still grading enabled and server-side; it only skips gradebook/final-grade propagation.
- `AssessmentGroup` remains the gradebook grouping surface when `outcome.uses` includes `gradebook`.
- Existing learner dashboard/workspace score queries should continue consuming the trusted backend result path.

### Part 4: Authoring Preview and Grading Dry Run

Part 4 adds preview paths for authors to test quiz content. Ungraded preview can use local-practice feedback immediately. Grading-enabled preview uses server-side dry-run without creating an official learner attempt.

Target direction:

- `quiz-content-editor` gets a `Preview` action.
- If grading is disabled, preview may render from the normal authoring payload and show local practice correctness.
- If grading is enabled, preview renders the quiz with the learner-facing payload shape.
- Grading-enabled preview submits answers to a server dry-run endpoint.
- Grading-enabled dry-run uses the same adapter and grading engine as real learner submissions.
- Dry-run does not create an official attempt, gradebook entry, or learner progress event.
- Dry-run ignores client-provided correctness, score, answer key, and `isCorrect`, just like official grading.
- Preview result can show score, item feedback, and redaction/debug warnings to the author.
- Preview must make it easy to verify that learner payloads do not expose answer keys.

## Test Plan

- Content without grading saves and loads unchanged.
- Content without grading can show local quiz practice correctness without appearing in Assessments.
- Content with grading enabled appears in Assessments.
- Content with grading disabled does not appear in Assessments.
- Quiz grading definition saves and reloads through the current grading definition.
- Quiz adapter extracts graded items and answer keys from authoring payloads.
- Learner payload does not expose correct answers.
- Tampered submissions with score, correctness, answer key, or `isCorrect` are ignored.
- Server-side deterministic quiz grading produces trusted results.
- Result use controls whether a result appears only as feedback or also enters the gradebook.
- Feedback-only graded content still uses server-side grading and `AssessmentSubmission`; it only skips gradebook propagation.
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
