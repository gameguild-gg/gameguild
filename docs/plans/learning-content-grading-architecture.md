# Learning Content Grading Architecture Plan

## Summary

`Content` is the source of truth for course structure and learner activities. A course item can be a lesson, quiz, assignment, project, code exercise, discussion, survey, reflection, or another supported activity type. Some of these content items may have grading enabled.

`Assessments` should be treated as an operational view over graded `Content`, not as a separate domain that owns quiz or assignment definitions.

The grading system should live in its own feature package and provide typed contracts for scores, weights, answer keys, feedback, attempt policy, learner-safe redaction, and server-side validation. `block-content-editor` remains responsible for authoring/rendering content blocks, while the grading package describes how those blocks become graded.

## Core Vocabulary

- **Content**: any course item in the content tree.
- **Gradable content**: a content item with grading enabled.
- **Assessment view**: dashboard surface that lists, groups, edits, and analyzes gradable content.
- **Validation mode**: the trust boundary used to validate learner answers.
- **Public validation**: validation runs in the client and may expose the answer key to the learner payload.
- **Protected validation**: validation runs on the server and never trusts client-reported correctness or score.
- **Official grade**: grade that can affect gradebook, completion, certificate, or pass/fail decisions.

## Validation Modes

Use:

```ts
type GradingValidationMode = 'public' | 'protected';
```

### Public

`public` mode is for learner-visible validation:

- answer keys may be present in the client payload;
- correctness can be computed in the browser;
- feedback can be immediate and rich;
- useful for practice, self-check, formative learning, and transparent exercises;
- stored results must be marked as public/client-validated.

Public validation can produce a pedagogical score, but it must never affect official gradebook, completion gates, certificates, or pass/fail decisions.

### Protected

`protected` mode is for authoritative validation:

- learner payload is redacted;
- answer keys remain server-side;
- submissions use structured payloads;
- client-provided score, correctness, answer key, or `isCorrect` values are ignored;
- grading result is produced by the server;
- required for official gradebook, certification, pass/fail gates, and high-stakes assessment.

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
  grading?: ContentGradingConfig;
}
```

`body` contains the content/editor data. `grading` contains the grading contract for that content item.

### Grading Package

Create a package at:

```text
packages/features/grading
```

Proposed package name:

```text
@game-guild/grading
```

The package owns:

- grading schema types;
- score and weight rules;
- item-level grading rules;
- attempt and availability policy types;
- feedback policy types;
- answer key extraction contracts;
- learner-payload redaction contracts;
- structured submission payload contracts;
- deterministic grading helpers;
- validation helpers shared by web and API tests.

It should not own React UI. UI components can consume the package, but grading rules must remain framework-independent.

### Block Content Editor

`block-content-editor` owns the editing experience for blocks.

It may expose stable block IDs and metadata that grading can reference, but it should not own course grade rules.

Allowed responsibility:

- author quiz/question blocks;
- expose block structure;
- expose extension points for grading metadata panels;
- render public practice validation when the content says `validationMode: 'public'`.

Not allowed responsibility:

- official scoring policy;
- gradebook rules;
- protected answer-key storage;
- server trust decisions;
- assessment analytics.

### Assessments

`Assessments` is a view over graded `Content`.

It should:

- list content items where `grading.enabled === true`;
- group graded content by grading group/category;
- show max score, weight, validation mode, attempts, availability, and grading status;
- route edits back to the owning content item;
- display analytics from submissions/gradebook;
- never become the source of truth for quiz or assignment body data.

## Content Grading Contract

Initial target contract:

```ts
interface ContentGradingConfig {
  enabled: boolean;
  schemaVersion: number;
  validationMode: GradingValidationMode;
  gradebook: GradebookConfig;
  policy: GradingPolicy;
  items: Record<string, GradedItemConfig>;
}

interface GradebookConfig {
  maxScore: number;
  passingScore?: number;
  weight?: number;
  groupId?: string | null;
  required?: boolean;
  /** Official grades require protected validation. Public validation is pedagogical only. */
  official?: boolean;
}

interface GradingPolicy {
  maxAttempts?: number | null;
  timeLimitMinutes?: number | null;
  availableFrom?: string | null;
  availableUntil?: string | null;
  feedbackMode?: 'immediate' | 'after-submit' | 'after-close' | 'manual';
  presentationMode?: 'continuous' | 'single-step';
}

interface GradedItemConfig {
  contentBlockId: string;
  points: number;
  gradingKind: 'deterministic' | 'manual' | 'external';
  answerKeyRef?: string;
  rubricRef?: string;
}
```

The exact names can change during implementation, but the important boundary is that grading metadata references content blocks instead of being embedded as a quiz-only concern.

## Quiz as First Consumer

Quiz should be the first implementation target because it already exists in the editor.

For quiz content:

- quiz block data remains in content body;
- grading config references quiz/question block IDs;
- public mode can validate using data already available to the client;
- protected mode redacts answer keys and validates through the server;
- deterministic question types can be graded immediately server-side;
- manual or coding-style question types can return a pending/manual state.

This keeps the model ready for assignments, projects, code exercises, file submissions, and other future content types.

## Backend Flow

### Authoring

When an instructor saves a content item:

1. Save `ProgramContent.Body`.
2. Save `ProgramContent.GradingConfig` when grading is enabled.
3. Validate the grading config against the grading package schema.
4. For protected mode, store answer-key material only in server-owned data.

### Learner Start

When a learner opens content:

1. Load the content item.
2. Load its grading config.
3. If no grading is enabled, return normal content payload.
4. If `validationMode` is `public`, return the normal learner payload with public validation data.
5. If `validationMode` is `protected`, return a redacted payload without answer keys.

### Submission

When a learner submits:

1. Accept only the learner answer payload.
2. Ignore client-provided score, correctness, answer key, and `isCorrect`.
3. Store answers as structured submission data.
4. If `validationMode` is `public`, store the public/client-validated result as non-protected evidence.
5. If `validationMode` is `protected`, grade on the server from server-owned answer keys.
6. Publish the result to gradebook/analytics according to grading policy.

## Frontend Flow

### Content Page

The content editor should let the instructor:

- edit the content body;
- enable or disable grading for that content item;
- choose `validationMode: 'public' | 'protected'`;
- configure max score, passing score, weights, attempts, time limits, feedback, and presentation;
- assign points to individual blocks/questions.

The same content item remains visible in the content tree.

### Assessments Page

The assessments page should:

- query graded content items;
- show them as assessment activities;
- offer filters/grouping by type, validation mode, grade group, status, and required/optional;
- link to the same content editor;
- optionally open a focused grading settings panel for the content item.

There should not be a separate `quiz-assessment-editor` that stores a second quiz definition.

## Package API Direction

The grading package should expose framework-independent APIs similar to:

```ts
validateGradingConfig(config: unknown): ContentGradingConfig;
redactLearnerPayload(contentBody: unknown, grading: ContentGradingConfig): unknown;
extractAnswerKey(contentBody: unknown, grading: ContentGradingConfig): AnswerKey;
buildStructuredSubmissionPayload(input: unknown, grading: ContentGradingConfig): StructuredSubmissionPayload;
gradeDeterministicSubmission(args: GradeSubmissionArgs): GradeResult;
```

The backend can use the same contracts through generated schemas or mirrored .NET contracts.

## Implementation Split

### Part 0: Correct the Accidental Coupling

Before building the new grading model, clean up the work that pushed quiz grading into `Assessment` as a second authoring domain.

Git history points to these relevant commits:

- `1670552bc4 feat: integration block-content-editor in dashboard`: keep the content editor integration pattern, but verify it only persists content body data through the content save path.
- `898dabd9ef refactor: lesson and quiz content item editor`: keep the lesson/quiz component boundary if it still matches the route.
- `7b238a9742 fix(learning): quiz-content-editor save`: preserve the fix that makes quiz content save into `ProgramContent`.
- `c234e7afb1 feat(dashboard): content and assessment`: unwind the separate dashboard quiz-assessment authoring surface and any content-tree UI that treats assessments as independent quiz definitions attached to content.
- `e021fe5c52 feat(api): Assessment graded system`: stop extending `Assessment.DefinitionPayload` as the future source of truth for authored quiz/grading definitions; re-evaluate these backend pieces in Part 2.

Part 0 goals:

- Stop treating `Assessment` as the owner of quiz definitions.
- Remove or quarantine `quiz-assessment-editor` as a separate quiz definition editor.
- Remove frontend-only `SubmissionModality` decisions from content and assessment editors.
- Remove the content-tree workflow that attaches an independent assessment to content as the primary grading path.
- Keep `Assessments` as a list/view concept, even if the current backend still exposes assessment entities.
- Keep current quiz content save behavior intact.
- Keep lesson and quiz content editors componentized where that componentization only affects `ProgramContent` editing.
- Preserve all content type options: `Lesson`, `Quiz`, `Assignment`, `Project`, `Discussion`, `Code`, `Reflection`, and `Survey`.
- Avoid backend schema removal in this step unless the accidental schema is known to be unreleased and safe to drop.

Part 0 validation:

- Content lesson saves and reloads.
- Content quiz saves and reloads.
- Content type creation keeps all supported future content types visible.
- Assessments no longer stores a second quiz body through dashboard UI.
- No `next/dynamic` is introduced in the editor route; keep `React.lazy` + `Suspense`.

### Part 1: Package and Frontend Contract

Build the package and the frontend-facing authoring model, where the product decisions are clearer and safer to make without committing to a backend storage design.

Package work:

- Add `packages/features/grading`.
- Define `ContentGradingConfig`, `GradingValidationMode`, gradebook config, policy config, item config, answer key, structured submission, and grade result types.
- Encode that `validationMode: 'public'` is pedagogical only.
- Add schema validation tests.
- Keep the package UI-free.

Frontend work:

- Add grading controls to the content editor flow.
- Let quiz be the first content type that can populate `GradedItemConfig`.
- Let future content types opt into the same grading contract.
- Update the assessments dashboard to read from the content-grading contract or a temporary projection of it.
- Link assessment rows back to the owning content item instead of editing a separate quiz body.

Part 1 validation:

- Content without grading saves and loads unchanged.
- Content with grading enabled is discoverable by the assessments surface.
- Quiz in public mode validates in the client and is clearly marked as pedagogical.
- Frontend tests cover contract serialization without relying on backend-only `Assessment.DefinitionPayload`.

### Part 2: Backend Audit and Integration

Analyze the existing backend before choosing a final persistence shape. The current code already has useful mechanisms in both `Courses` and `Assessments`, so this part starts with an audit rather than a migration.

Backend mechanisms to evaluate:

- `ProgramContent.GradingMethod` and `ProgramContent.MaxPoints`.
- `ContentInteraction`, `SubmissionData`, attempts, progress, completion, and time tracking.
- `ActivityGrade`, grade type, grader, points, max points, feedback, and finalization.
- `Assessment`, `AssessmentGroup`, `AssessmentSubmission`, `StructuredAnswerPayload`, attempts, score analytics, and review permissions.
- Current `Assessment.ContentId` linkage and whether it should remain as a projection/compatibility bridge.
- Existing learner workspace grade summaries and `ProgramUser.FinalGrade`.

Part 2 decisions:

- Whether grading config lives directly on `ProgramContent`, in a related content-grading table, or in a versioned settings object.
- Whether official submissions should use `AssessmentSubmission`, `ContentInteraction` + `ActivityGrade`, or a bridge between them.
- How to create immutable snapshots of content body plus grading config per protected attempt.
- Where protected answer keys live and how learner payload redaction is enforced.
- How assessment groups attach to content-owned grading config.
- How public results are stored without entering official grade calculations.
- How protected results flow into gradebook, final grade, analytics, certificates, and pass/fail gates.

Part 2 implementation phases:

#### Phase 2.1: Backend Source-of-Truth Decision

- Document the chosen storage model.
- Add only the minimum schema needed for content-owned grading.
- Preserve compatibility with existing assessment/submission data where required.

#### Phase 2.2: Attach Grading to Content

- Add grading configuration to content save/load contracts.
- Let quiz blocks become the first source of `GradedItemConfig`.
- Ensure non-quiz content types can opt into grading later.

#### Phase 2.3: Public Validation Runtime

- Support public/client validation for quiz content.
- Persist public results with explicit validation provenance.
- Make the UI distinguish public validation from protected validation.
- Exclude public results from official grades.

#### Phase 2.4: Protected Validation Runtime

- Add learner-safe redaction for protected content.
- Add server-side grading for deterministic quiz blocks.
- Store learner submissions as structured answer payloads.
- Ignore client-provided score/correctness.
- Add security tests for tampered payloads.
- Snapshot content body and grading config per attempt.

#### Phase 2.5: Rebuild Assessments as a View

- Query content items with grading enabled.
- Show grading groups, weights, average score, pass rate, attempts, and validation mode.
- Route editing back to content.
- Connect analytics and gradebook to graded content submissions.

## Test Plan

- Content without grading saves and loads unchanged.
- Content with grading enabled appears in Assessments.
- Content with grading disabled does not appear in Assessments.
- Quiz in public mode saves, reloads, validates in client, and marks result provenance as public.
- Quiz in public mode never contributes to official course grade, completion gates, certificates, or pass/fail decisions.
- Quiz in protected mode saves, reloads, redacts answer keys, and grades on server.
- Tampered protected submissions with score, correctness, answer key, or `isCorrect` are ignored.
- Assessments page lists quiz, assignment, project, code, survey, reflection, and other future graded content types through the same content-grading contract.
- Existing `React.lazy` + `Suspense` loading remains in place; no `next/dynamic` in the editor route.

## Open Decisions

- Whether grading config should live as a dedicated `ProgramContent` field, a related table, or a versioned content settings object.
- How grade groups should be modeled when they are displayed in Assessments but attached to Content.
- How protected answer keys should be stored for complex content types such as code projects and file submissions.
- Whether manual grading should be part of the first protected implementation or deferred until deterministic quiz grading is stable.
