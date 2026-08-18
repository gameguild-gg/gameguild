# Quiz Domain and Surface Packages

Status: implemented.

## Summary

Extract the quiz feature currently located under:

```text
apps/web/src/components/block-content-editor/extras/quiz
```

into two packages with different dependency and trust boundaries:

```text
packages/features/quiz
packages/features/quiz-surface
```

The packages will be named:

```text
@game-guild/quiz
@game-guild/quiz-surface
```

`@game-guild/quiz` owns framework-independent question contracts and behavior.
`@game-guild/quiz-surface` owns the React authoring and learner experiences.

This plan supersedes Part 2 of
`docs/plans/block-list-and-quiz-package-extraction.md`. The `block-list` part of
that plan is already implemented and remains valid.

The product has not launched. There is no legacy quiz format or released
browser data to migrate. The extraction may establish the intended contracts
directly and update all current consumers in the same change. It must not add
compatibility layers, legacy parsers, dual-write behavior, or deprecated
exports that would immediately become cleanup work.

## Context

The following foundations are already packages:

- `@game-guild/block-list` owns generic ordered block structure;
- `@game-guild/assets` owns asset IDs, metadata, Blob persistence, and asset
  resolution;
- `@game-guild/lexical-surface` owns rich-text editing and rendering;
- `@game-guild/grading` owns trusted grading policy, answer-key handling,
  result calculation, and gradebook-facing semantics.

The current quiz folder contains approximately 8,800 lines and combines:

- persisted question contracts;
- authoring-only answer keys;
- learner-safe projections;
- local-practice correctness;
- React answer state;
- learner renderers;
- authoring editors;
- a full-screen editor shell;
- formula parsing and evaluation;
- Lexical essay integration;
- file upload and image resolution;
- block-content-editor adapters.

Moving this folder unchanged into one package would preserve the current
coupling. The extraction must define ownership first and move code only after
the target boundaries exist.

## Architectural Decision

### Two packages, not one mixed package

`@game-guild/quiz` is the canonical domain package. It can be imported by the
web application, grading adapters, tests, and future server-side TypeScript
code without loading React or editor dependencies.

`@game-guild/quiz-surface` is a React package. It consumes the domain package
and provides controlled authoring and learner surfaces. It does not own trusted
grading and does not define a second copy of question contracts.

This dependency direction prevents either of these undesirable cycles:

```text
quiz-surface -> grading -> quiz-surface
grading -> React/editor package -> application UI
```

The target direction is:

```text
apps/web -> @game-guild/block-list
apps/web -> @game-guild/quiz
apps/web -> @game-guild/quiz-surface
apps/web -> @game-guild/grading

@game-guild/quiz-surface -> @game-guild/quiz
@game-guild/quiz-surface -> @game-guild/assets
@game-guild/quiz-surface -> @game-guild/lexical-surface
@game-guild/quiz-surface -> @game-guild/ui

@game-guild/quiz -> @game-guild/assets (domain contracts only)
@game-guild/grading/adapters/quiz -> @game-guild/quiz
```

Not allowed:

```text
@game-guild/quiz -> React
@game-guild/quiz -> @game-guild/quiz-surface
@game-guild/quiz -> @game-guild/grading
@game-guild/quiz -> apps/web
@game-guild/quiz -> @game-guild/block-list

@game-guild/quiz-surface -> @game-guild/grading
@game-guild/quiz-surface -> apps/web
@game-guild/quiz-surface -> block-content-editor paths

@game-guild/block-list -> quiz or quiz-surface
@game-guild/lexical-surface -> quiz or quiz-surface
@game-guild/assets -> quiz or quiz-surface
```

## Goals

- establish one canonical contract for every quiz question type;
- keep domain code independent from React, Next.js, and editor UI;
- separate authoring payloads from learner-safe payloads at the type level;
- remove answer-key handling and local correctness logic from React hooks;
- eliminate correctness-rule duplication between Quiz and Grading;
- replace the generic string-map answer state with typed question answers;
- make server-graded UI unable to compute trusted correctness locally;
- use `@game-guild/assets` for hotspot images and attachments;
- use `@game-guild/lexical-surface` only through a narrow essay adapter;
- provide controlled React surfaces that are independent from modal ownership;
- keep block composition and block IDs in the application and `block-list`;
- make adding or modifying a question type predictable and exhaustively tested;
- remove the original web Quiz implementation after all imports are migrated.

## Non-Goals

- implementing grading HTTP endpoints or backend persistence;
- moving generic block editing UI into quiz packages;
- making `block-list` aware of quiz payloads;
- making quiz question types dynamically installable at runtime;
- creating compatibility exports in the old web quiz directory;
- preserving `data:` URLs or direct media URLs in quiz documents;
- trusting client-side correctness for grading-enabled content;
- implementing official formula grading before a server-safe evaluator is
  selected and supported by `@game-guild/grading`;
- redesigning the whole assessment, course, or submission experience.

## Current Problems to Correct During Extraction

### Domain and UI are mixed

The current `types.ts` contains persisted contracts, factories, parser helpers,
and a Lexical type import. `contracts.ts` contains learner redaction and
authoring validation. `use-quiz-answers.ts` contains React state and most of the
question correctness implementation.

These responsibilities must be split before the corresponding files are moved.

### Quiz and Grading duplicate question semantics

`use-quiz-answers.ts` and `@game-guild/grading/adapters/quiz` independently
implement correctness rules. Their behavior can diverge for tolerances,
partial answers, normalization, matching, ordering, hotspot coordinates, and
highlight overlap.

The quiz domain must own question-level evaluation semantics. Grading may call
that evaluator with a server-owned answer key and translate the result into
scores and trusted grading results.

### The current answer state is structurally weak

`QuizAnswerState` stores unrelated question answers in shared arrays and maps.
Some question types encode structure into strings or JSON:

- matching encodes pair assignments into delimited strings;
- hotspot coordinates are written into text-answer keys;
- formula attempt values are serialized into a text-answer field;
- essay plain text and rich text share magic map keys.

The package must replace this with a discriminated answer union and explicit
adapters for the generic grading transport.

### Asset ownership is bypassed

The current hotspot editor reads files as base64 and persists `imageUrl`.
Attachments duplicate name, MIME type, and size beside an asset URI.

The new contracts must persist only stable asset IDs plus quiz-specific
presentation metadata. Exact URLs, Blob URLs, data URLs, names, MIME types, and
byte sizes remain owned by `@game-guild/assets`.

### React code imports application internals

Quiz editors import primitives through `@/components/ui`, while the main editor
imports `BlockEditorShell`, editor settings, and Math input through
`block-content-editor` paths. A package cannot retain these imports.

Package code must import `@game-guild/ui`, `@game-guild/assets/react`, and
`@game-guild/lexical-surface`, or own quiz-specific controls itself.

## Package 1: `@game-guild/quiz`

### Responsibility

Own the complete framework-independent quiz domain:

- authoring contracts;
- learner-safe contracts;
- typed answer contracts;
- question factories and nested IDs;
- authoring validation;
- learner redaction;
- answer normalization;
- question-level practice evaluation;
- formula and highlight domain helpers;
- quiz asset-reference discovery;
- test vectors shared with grading.

It does not own React state, UI components, block lists, gradebook policy,
attempt persistence, HTTP requests, or course behavior.

Its only feature-package dependency is `@game-guild/assets`, used for the
stable `AssetUri` contract and asset-reference discovery. It does not import
browser repositories, React asset hooks, pickers, providers, or resolution UI.

### Target Structure

```text
packages/features/quiz/
  package.json
  README.md
  tsconfig.json
  vitest.config.ts
  src/
    index.ts
    authoring/
      authoring-entry.ts
      factories.ts
      validation.ts
    learner/
      learner-entry.ts
      redaction.ts
    answers/
      answer-types.ts
      normalization.ts
      grading-transport.ts
    questions/
      question-type.ts
      shared.ts
      single-choice.ts
      multiple-choice.ts
      true-false.ts
      fill-blank.ts
      short-answer.ts
      essay.ts
      matching.ts
      ordering.ts
      categorization.ts
      rating.ts
      numeric.ts
      formula.ts
      hotspot.ts
      highlight.ts
    evaluation/
      evaluation-result.ts
      evaluate-answer.ts
      numeric-comparison.ts
      text-comparison.ts
    formula/
      formula-expression.ts
      formula-prompt.ts
    highlight/
      parse-highlight-source.ts
    assets/
      collect-quiz-asset-uris.ts
    testing/
      fixtures.ts
      test-vectors.ts
    architecture.test.ts
    authoring.test.ts
    learner.test.ts
    answers.test.ts
    evaluation.test.ts
    assets.test.ts
```

Files can be consolidated when they are genuinely small. The responsibility
boundaries are required; creating one file for every heading regardless of
content is not.

### Public Exports

Keep the public surface explicit:

```json
{
  "exports": {
    ".": "./src/index.ts",
    "./testing": "./src/testing/index.ts"
  }
}
```

Consumers must not deep-import individual question files. The package root
exports stable domain contracts and helpers. Test fixtures use the testing
entrypoint.

### Canonical Question Contracts

Use one discriminated union keyed by stable string values:

```ts
export const QuizQuestionType = {
  SingleChoice: "SINGLE_CHOICE",
  MultipleChoice: "MULTIPLE_CHOICE",
  TrueFalse: "TRUE_FALSE",
  FillInTheBlank: "FILL_IN_THE_BLANK",
  ShortAnswer: "SHORT_ANSWER",
  Essay: "ESSAY",
  Matching: "MATCHING",
  Ordering: "ORDERING",
  Categorization: "CATEGORIZATION",
  Rating: "RATING",
  Numeric: "NUMERIC",
  Formula: "FORMULA",
  Hotspot: "HOTSPOT",
  Highlight: "HIGHLIGHT"
} as const;
```

Prefer a const object plus derived union over a TypeScript enum. The persisted
values remain readable strings and do not require enum runtime behavior.

Each authoring entry contains prompt data, settings, feedback policy,
quiz-specific presentation data, and the answer-key fields required by its
type. It does not contain block ID or block ordering; those belong to the
consumer's `Block`.

### Authoring, Practice, and Learner Contracts

These contracts must remain distinct:

```ts
type QuizAuthoringEntry = /* includes answer-key material */;
type QuizPracticeEntry = QuizAuthoringEntry;
type QuizLearnerEntry = /* redacted render data only */;
type QuizRuntimeEntry = QuizPracticeEntry | QuizLearnerEntry;
```

`QuizPracticeEntry` intentionally has answer-key material because local
practice is pedagogical and not trusted grading. `QuizLearnerEntry` must not
contain fields that prove correctness.

Do not use optional answer-key fields to make one permissive universal shape.
Authoring completeness and learner safety must be represented explicitly.

### Typed Answers

Replace the generic `QuizAnswerState` with a discriminated union:

```ts
export type QuizAnswer =
  | { type: "SINGLE_CHOICE"; optionId: string | null }
  | { type: "MULTIPLE_CHOICE"; optionIds: string[] }
  | { type: "TRUE_FALSE"; value: boolean | null }
  | { type: "FILL_IN_THE_BLANK"; values: Record<string, string> }
  | { type: "SHORT_ANSWER"; value: string }
  | { type: "ESSAY"; richText: SerializedRichTextPayload; plainText: string }
  | { type: "MATCHING"; matches: Record<string, string> }
  | { type: "ORDERING"; itemIds: string[] }
  | { type: "CATEGORIZATION"; categoryIdsByItem: Record<string, string[]> }
  | { type: "RATING"; value: number | null }
  | { type: "NUMERIC"; value: string }
  | { type: "FORMULA"; expression: string }
  | { type: "HOTSPOT"; point: { x: number; y: number } | null }
  | { type: "HIGHLIGHT"; spans: Array<{ start: number; end: number }> };
```

The precise names can change during implementation, but structured data must
not be encoded into delimiter strings, arbitrary map keys, or nested JSON
strings.

Provide pure helpers:

- `createEmptyQuizAnswer(type)`;
- `normalizeQuizAnswer(type, unknownValue)`;
- `toStructuredGradingAnswer(answer)`;
- `fromStructuredGradingAnswer(type, answer)` when the reverse operation is
  required.

The generic `StructuredAnswer` remains a grading transport, not the Quiz UI's
runtime state model.

### Evaluation Boundary

Provide a pure question evaluator:

```ts
evaluateQuizAnswer(
  entry: QuizPracticeEntry,
  answer: QuizAnswer,
): QuizEvaluationResult;
```

The result expresses question semantics only:

```ts
type QuizEvaluationResult =
  | { status: "correct" }
  | { status: "incorrect"; reason?: string }
  | { status: "pending" }
  | { status: "unsupported"; reason?: string };
```

It does not calculate gradebook placement, final score policy, attempt limits,
or trusted submission status.

`@game-guild/grading` invokes this evaluator only with server-owned authored
data and translates the result into `GradeItemResult`. This keeps question
semantics single-sourced while preserving the server trust boundary.

The learner surface never calls this evaluator for a server-graded entry.

### Formula Questions

Formula attempt data must be separated from learner answers:

```ts
interface FormulaPrompt {
  promptId?: string;
  variables: Record<string, number>;
  expectedResult?: number;
  decimalPlaces?: number;
}
```

For local practice, the domain may generate a prompt locally. For official
grading, the server provides the prompt or seed and keeps the expected formula
and result authoritative.

The current handwritten evaluator should not be expanded casually. During
extraction:

1. preserve its supported expression subset behind a narrow API;
2. add adversarial tests for malformed numbers, unary operators, functions,
   precedence, invalid identifiers, division by zero, and non-finite results;
3. evaluate a proven expression parser before enabling official formula
   grading;
4. keep parser implementation details out of React components.

### Rich Text

The domain package must not import Lexical. Use a UI-neutral JSON contract:

```ts
export type SerializedRichTextPayload = Record<string, unknown> | null;
```

The Quiz Surface essay adapter converts between this payload and the public
serialized-state contract from `@game-guild/lexical-surface`.

Do not expose Lexical editor instances or commands through Quiz domain APIs.

### Asset Contracts

Attachments keep only quiz-owned data:

```ts
interface QuizAttachment {
  assetUri: AssetUri;
  role: "question" | "answer" | "feedback" | "source";
  label?: string;
  altText?: string;
}
```

Do not persist asset name, MIME type, byte size, exact URL, data URL, Blob URL,
or provider information in a quiz entry. These are resolved by
`@game-guild/assets`.

Hotspot uses:

```ts
interface HotspotAuthoringEntry {
  imageAssetUri: AssetUri | null;
  imageWidth: number;
  imageHeight: number;
  hotspots: HotspotPoint[];
}
```

Provide `collectQuizAssetUris(entry)` so the host can include quiz assets in
document-level usage reconciliation and project portability checks.

### IDs

Use `crypto.randomUUID()` for option, pair, item, category, blank, and hotspot
IDs. Remove `Math.random().toString(36)` IDs.

Nested IDs must be stable across editing. Reordering must not regenerate IDs.

## Package 2: `@game-guild/quiz-surface`

### Responsibility

Own the complete reusable React experience for quiz questions and their
controlled visual collection:

- controlled authoring surface;
- optional dialog wrapper;
- learner player;
- local-practice player;
- answer session hook or reducer wrapper;
- question editors and renderers;
- feedback presentation;
- question-type selector and templates;
- quiz-specific block collection, insertion seams, ordering, editing, and
  deletion UX;
- asset picker and asset resolution integration;
- Lexical essay integration;
- accessible interactions for ordering, matching, hotspot, and highlighting.

It owns the quiz-specific collection UI, but not the host's persisted block
schema or document IDs. It does not own assessment navigation, persistence,
trusted grading, API calls, route-level modal ownership, or course behavior.

### Target Structure

```text
packages/features/quiz-surface/
  package.json
  README.md
  tsconfig.json
  vitest.config.ts
  src/
    index.ts
    editor/
      index.ts
      quiz-collection-editor.tsx
      quiz-editor-surface.tsx
      quiz-editor-dialog.tsx
      question-type-selector.tsx
      question-settings.tsx
      editor-validation.tsx
    player/
      index.ts
      quiz-player.tsx
      quiz-practice-player.tsx
      quiz-feedback.tsx
      quiz-session-reducer.ts
      use-quiz-session.ts
    questions/
      single-choice/
        editor.tsx
        player.tsx
      multiple-choice/
        editor.tsx
        player.tsx
      true-false/
        editor.tsx
        player.tsx
      fill-blank/
        editor.tsx
        player.tsx
      short-answer/
        editor.tsx
        player.tsx
      essay/
        editor.tsx
        player.tsx
        lexical-adapter.tsx
      matching/
        editor.tsx
        player.tsx
      ordering/
        editor.tsx
        player.tsx
      categorization/
        editor.tsx
        player.tsx
      rating/
        editor.tsx
        player.tsx
      numeric/
        editor.tsx
        player.tsx
      formula/
        editor.tsx
        player.tsx
        math-input.tsx
      hotspot/
        editor.tsx
        player.tsx
      highlight/
        editor.tsx
        player.tsx
    registry/
      question-metadata.ts
      editor-registry.tsx
      player-registry.tsx
    shared/
      answer-option.tsx
      question-header.tsx
      surface-layout.tsx
      validation-summary.tsx
    testing/
      fixtures.tsx
      render-helpers.tsx
    architecture.test.ts
```

Question folders are vertical slices. A question's editor and player should
remain together because they evolve around the same domain contract. Shared UI
moves to `shared` only when at least two independent question types use it.

### Public Exports

Use explicit entrypoints to avoid loading authoring dependencies in learner
runtime bundles:

```json
{
  "exports": {
    ".": "./src/index.ts",
    "./player": "./src/player/index.ts",
    "./editor": "./src/editor/index.ts",
    "./testing": "./src/testing/index.ts"
  }
}
```

Recommended usage:

```ts
import { QuizPlayer } from "@game-guild/quiz-surface/player";
import { QuizEditorSurface } from "@game-guild/quiz-surface/editor";
import { QuizCollectionEditor } from "@game-guild/quiz-surface/editor";
```

The root may re-export only small shared contracts and the most common surface.
It must not eagerly export every editor, renderer, and internal registry.

### Controlled Editor API

The core authoring component is controlled and host-independent:

```tsx
<QuizEditorSurface
  value={entry}
  onChange={setEntry}
  onCommit={saveEntry}
  onCancel={closeEditor}
/>
```

The package may provide `QuizEditorDialog` as a convenience, but
`QuizEditorSurface` must not require a dialog, route, or
`block-content-editor` shell.

`QuizCollectionEditor` receives controlled `{ id, entry }` items. The host
maps those items to its storage format and supplies ID creation; the package
owns the complete quiz-block interaction instead of importing the generic
application `BlockArrayEditor`.

Editor preferences are either:

- props passed by the host; or
- quiz-owned settings with a stable package API.

The package must not import `useEditorSettings` from the application.

### Player APIs and Trust Modes

Keep practice and server-graded paths impossible to confuse:

```tsx
<QuizPracticePlayer
  entry={practiceEntry}
  answer={answer}
  onAnswerChange={setAnswer}
/>
```

```tsx
<QuizPlayer
  entry={learnerEntry}
  answer={answer}
  onAnswerChange={setAnswer}
  onSubmit={submitAnswer}
  submissionResult={serverResult}
/>
```

`QuizPracticePlayer` may call `evaluateQuizAnswer`. `QuizPlayer` receives only
a learner-safe entry and displays correctness or score only from an explicit
server result supplied by the host.

Do not use a permissive prop where `submissionMode` is optional and an authored
entry can accidentally reach a server-graded player.

### Session State

Move state transitions into a pure reducer. The React hook wraps the reducer
and lifecycle behavior but does not contain per-question correctness switches.

Expected responsibilities:

- initialize a typed answer for the entry type;
- update or replace an answer;
- mark submission pending;
- accept a server result;
- reset an attempt;
- expose whether editing is disabled;
- preserve controlled and uncontrolled integration where intentionally
  supported.

Random formula variables belong to attempt/prompt state, not to the answer.

### Question Registries

Use exhaustive static registries rather than a dynamic plugin system in the
first implementation:

- domain metadata: type, label, description, factory;
- editor registry: type to editor component;
- player registry: type to player component.

TypeScript should require every known question type to appear in each relevant
registry. Unknown question types should fail closed with a clear unsupported
state.

Heavy authoring components such as Lexical, MathLive, and advanced drag/drop
editors should be loaded lazily from the editor registry. Learner player imports
must not pull the complete authoring surface into their bundle.

### UI Dependencies

The package imports primitives from `@game-guild/ui`, not `@/components/ui`.
It may directly depend on:

- `@game-guild/quiz`;
- `@game-guild/assets` and `@game-guild/assets/react`;
- `@game-guild/lexical-surface`;
- `@game-guild/ui`;
- React and React DOM as peer dependencies;
- `react-hook-form` for authoring forms;
- `lucide-react` for icons;
- MathLive or the selected math input dependency;
- an established accessible drag-and-drop library when required.

Every runtime dependency must be declared by this package. It must not rely on
dependencies being present through `apps/web`.

### Visual and Interaction Rules

- use semantic UI tokens instead of hardcoded blue/gray/green palettes;
- keep editor and player layouts responsive without nested cards;
- provide keyboard interaction for reordering and matching;
- expose visible focus states and appropriate ARIA labels;
- prevent dynamic feedback from shifting surrounding block layout abruptly;
- keep touch targets usable on mobile;
- resolve assets through asset hooks and release object URL leases;
- do not show author-only attachments or feedback in learner mode;
- do not use decorative UI text to explain basic controls.

## Grading Integration

### Ownership

`@game-guild/quiz` owns:

- what an answer means for a question type;
- answer normalization;
- answer-key completeness;
- question-level correctness evaluation;
- learner redaction rules specific to quiz fields.

`@game-guild/grading` owns:

- trusted invocation on the server path;
- answer-key storage and extraction orchestration;
- scoring and maximum points;
- attempts and submission status;
- feedback release policy;
- gradebook semantics;
- result aggregation across content blocks;
- defensive normalization of untrusted submission envelopes.

### Adapter Refactor

Refactor `packages/features/grading/src/adapters/quiz` to import domain helpers
from `@game-guild/quiz`.

Remove duplicated correctness branches from grading after equivalent domain
tests and grading adapter tests pass. Keep grading-specific translation and
defensive checks in the adapter.

The grading core and adapter registry remain content-type agnostic. Only the
quiz adapter depends on `@game-guild/quiz`.

Add `@game-guild/quiz` to `packages/features/grading/package.json`; never add
`@game-guild/quiz-surface` there.

## Block List and Application Integration

The concrete block map remains outside `block-list`:

```ts
interface EditorBlockDataMap {
  quiz: QuizAuthoringEntry;
  // other block payloads
}
```

The web application keeps thin composition adapters:

- quiz serialized block type using `QuizAuthoringEntry`;
- block picker metadata that calls quiz factories;
- block editor modal that mounts `QuizEditorSurface`;
- preview adapter that mounts `QuizPracticePlayer` or `QuizPlayer`;
- submission orchestration that talks to application APIs;
- document-level asset usage reconciliation.

The package must not import `SerializedBlockNode`, `BlockView`, editor routes,
dashboard context, or application storage.

## Migration Plan

### Phase 0: Freeze behavior with test vectors

1. Inventory all current question fields and persisted defaults.
2. Add fixtures for all fifteen question types.
3. Record authoring validation, learner redaction, answer normalization, and
   local-practice evaluation behavior.
4. Cross-check current Quiz behavior against grading adapter behavior and
   resolve disagreements explicitly.
5. Identify unsupported grading cases instead of treating them as correct.

Exit criteria:

- every question type has a complete authored fixture;
- every question type has a learner-safe expected fixture;
- deterministic, manual, pending, and unsupported cases are documented.

### Phase 1: Create `@game-guild/quiz`

1. Scaffold package metadata, TypeScript, Vitest, README, and exports.
2. Move and split contracts from current `types.ts` and `contracts.ts`.
3. Replace enums with stable const-object unions where practical.
4. Replace Lexical types with `SerializedRichTextPayload`.
5. Add typed answer contracts and normalization.
6. Move factories and use UUIDs for nested identities.
7. Move validation, redaction, highlight parsing, and formula helpers.
8. Add `collectQuizAssetUris`.
9. Add an architecture test prohibiting React, web, block-list, grading, and
   Lexical imports.

Exit criteria:

- domain tests pass independently;
- the package has no React or application dependency;
- web code can temporarily import domain contracts from the new package.

### Phase 2: Unify Quiz and Grading semantics

1. Move local-practice correctness from `use-quiz-answers.ts` into the domain
   evaluator.
2. Add evaluator vectors for every supported type and edge case.
3. Update the grading quiz adapter to call domain normalization and evaluation.
4. Keep trusted scoring and answer-key orchestration in grading.
5. Remove duplicated question correctness from grading only after comparison
   tests pass.
6. Remove the `@game-guild/grading` import from Quiz React code.

Exit criteria:

- one implementation owns each deterministic question rule;
- quiz and grading package tests pass;
- server-graded learner payloads cannot be locally evaluated as authored
  entries.

### Phase 3: Create the learner surface

1. Scaffold `@game-guild/quiz-surface` and explicit entrypoints.
2. Implement typed answer session reducer and hook.
3. Move shared feedback and player shell components.
4. Move learner renderers one question type at a time.
5. Split practice and server-graded player APIs.
6. Add interaction and accessibility tests for each renderer.
7. Keep player bundles independent from authoring editors.

Recommended order:

1. single choice, multiple choice, true/false;
2. short answer, fill blank, rating;
3. matching, ordering, categorization;
4. essay and highlight;
5. numeric, formula, and hotspot.

Exit criteria:

- all preview paths use package players;
- server mode shows only host-supplied results;
- old renderer files have no remaining consumers.

### Phase 4: Create the authoring surface

1. Implement controlled `QuizEditorSurface`.
2. Replace the application `BlockEditorShell` dependency with a package-owned
   layout or host wrapper.
3. Move question selector metadata and factories to the proper package layers.
4. Move editors one vertical slice at a time.
5. Replace app UI imports with `@game-guild/ui`.
6. Move or redesign Quiz-owned Math input.
7. Add validation summary and prevent commit of invalid authored entries where
   product behavior requires completeness.
8. Add optional `QuizEditorDialog` only after the unframed surface works.

Exit criteria:

- the web modal is only a composition wrapper;
- quiz-surface contains no application imports;
- all editor dependencies are package-declared.

### Phase 5: Integrate Assets and Lexical Surface

1. Change hotspot `imageUrl` to `imageAssetUri`.
2. Replace `FileReader.readAsDataURL` with `AssetPickerDialog` or repository
   import APIs.
3. Resolve hotspot images with asset URL leases.
4. Normalize attachments to stable asset IDs and quiz-specific labels only.
5. Add asset usage collection tests.
6. Implement the essay Lexical adapter using public `lexical-surface` APIs.
7. Ensure learner redaction removes author-only attachments and model answers.

Exit criteria:

- no quiz payload persists data URLs, Blob URLs, or exact provider URLs;
- all hotspot and attachment rendering resolves through assets;
- only the essay integration imports lexical-surface.

### Phase 6: Migrate consumers and remove the origin

1. Add both package dependencies to `apps/web/package.json`.
2. Add packages to Next transpilation/build configuration when required by the
   current workspace setup.
3. Update quiz node and block-data maps to import `@game-guild/quiz`.
4. Update block picker factories and labels.
5. Update editor modal lazy loading to `@game-guild/quiz-surface/editor`.
6. Update preview and learner rendering to
   `@game-guild/quiz-surface/player`.
7. Update grading package dependencies and imports.
8. Move package-owned tests and retain only application composition tests in
   the web app.
9. Search for imports from the old quiz directory.
10. Delete the old web quiz directory once no consumer remains.
11. Remove dependencies from `apps/web/package.json` that are now owned only by
    quiz packages.

Do not leave compatibility re-export files in the old directory. The project
has not launched, and direct imports should be corrected instead.

## File Ownership Mapping

| Current area | Target owner |
| --- | --- |
| `types.ts` contracts | `@game-guild/quiz` |
| `types.ts` factories | `@game-guild/quiz` authoring factories |
| `contracts.ts` validation | `@game-guild/quiz` authoring validation |
| `contracts.ts` learner redaction | `@game-guild/quiz` learner redaction |
| `use-quiz-answers.ts` correctness | `@game-guild/quiz` evaluation |
| `use-quiz-answers.ts` React state | `@game-guild/quiz-surface` player |
| `utils/formula-evaluator.ts` | `@game-guild/quiz` formula domain |
| `renderers/*` | `@game-guild/quiz-surface` question slices |
| `editors/*` | `@game-guild/quiz-surface` question slices |
| `essay-lexical-editor.tsx` | quiz-surface essay Lexical adapter |
| `quiz-display.tsx` | quiz-surface player |
| `quiz-feedback.tsx` | quiz-surface player/shared UI |
| `quiz-type-selector.tsx` | quiz-surface editor plus quiz factories |
| `quiz-settings-dialog.tsx` | controlled editor plus optional dialog |
| `quiz-wrapper.tsx` | quiz-surface shared layout, if still useful |
| `preview-quiz.tsx` | thin web composition adapter |
| `quiz-node.tsx` | web block integration using quiz domain type |

## Testing Strategy

### Domain tests

For every question type, cover:

- factory output;
- nested ID uniqueness and stability;
- complete and incomplete authoring validation;
- authoring-to-learner redaction;
- absence of answer-key fields in learner payloads;
- empty answer creation;
- unknown answer normalization;
- correct, incorrect, pending, and unsupported evaluation where applicable;
- serialization roundtrip of domain JSON;
- referenced asset discovery.

### Security tests

- unknown question types redact common answer-key field names defensively;
- server-graded players cannot receive authored entries through their props;
- answer payload normalization drops score, correctness, feedback, formula,
  hotspot answer keys, and other claims;
- author-only attachments never enter learner payloads;
- no persisted hotspot or attachment contains `data:`, `blob:`, signed, or exact
  provider URLs;
- malformed formula input cannot invoke `eval`, `Function`, global objects, or
  arbitrary property access;
- provider or asset resolution failures render explicit unavailable states.

### Surface tests

- controlled editor changes and commits the expected domain value;
- changing question type resets only through an explicit action;
- every editor roundtrips its own factory value;
- every player reads and updates its typed answer;
- practice mode shows local feedback;
- server mode never invents correctness;
- reset creates a clean answer and attempt state;
- keyboard navigation works for choice, matching, ordering, categorization,
  hotspot alternatives, and highlighting;
- object URL leases are released on asset changes and unmount;
- heavy editor modules are not imported by the player entrypoint.

### Integration tests

- block-list storage roundtrips a `QuizAuthoringEntry` as opaque block data;
- block preview renders the canonical `BlockView.data` payload;
- project export discovers and bundles local quiz assets;
- learner redaction and grading adapter use the same domain fixtures;
- web lazy editor opens, commits, saves, reloads, and previews each question
  category;
- grading-disabled and grading-enabled paths remain distinct end to end.

### Architecture tests

Add source-boundary tests that fail when:

- quiz domain imports React, Lexical, Next.js, grading, block-list, or web paths;
- quiz-surface imports web paths or grading;
- grading imports quiz-surface;
- package internals import themselves through public package entrypoints;
- player entrypoints import editor modules;
- undeclared app aliases appear under either package.

## Acceptance Criteria

### `@game-guild/quiz`

- is the only source of canonical Quiz question contracts;
- has no React, Next.js, Lexical, grading, block-list, or app imports;
- exposes distinct authoring, practice, learner, and answer types;
- uses typed answers without delimiter or nested JSON encodings;
- owns question-level validation, redaction, and evaluation;
- uses source-neutral `AssetUri` references only;
- provides asset-reference discovery;
- uses stable UUIDs for nested entities;
- has fixtures and tests for all fifteen question types.

### `@game-guild/quiz-surface`

- owns all reusable Quiz React authoring and learner UI;
- imports no `apps/web` or `block-content-editor` modules;
- imports no `@game-guild/grading` modules;
- exposes separate editor and player entrypoints;
- keeps editor code out of player bundles;
- provides controlled surfaces rather than owning persistence;
- integrates hotspot and attachments through `assets`;
- integrates essay rich text through a narrow `lexical-surface` adapter;
- provides accessible keyboard and mobile interactions;
- declares every workspace and npm dependency it uses.

### Grading and application

- grading imports only `@game-guild/quiz`, never quiz-surface;
- question correctness rules are not duplicated in grading and React;
- grading remains authoritative for grading-enabled submissions;
- block-list remains generic and unaware of Quiz;
- the web app contains only Quiz composition adapters;
- no imports remain from the original web Quiz folder;
- the original web Quiz folder is deleted;
- no compatibility or legacy layer remains.

## Verification Commands

Use the workspace package manager and the Node version selected by the
repository environment:

```bash
pnpm --filter @game-guild/quiz test
pnpm --filter @game-guild/quiz typecheck
pnpm --filter @game-guild/quiz-surface test
pnpm --filter @game-guild/quiz-surface typecheck
pnpm --filter @game-guild/grading test
pnpm --filter @game-guild/grading typecheck
pnpm --filter @game-guild/block-list test
pnpm --filter @game-guild/assets test
pnpm --filter @game-guild/lexical-surface test
pnpm --filter @game-guild/web test
pnpm --filter @game-guild/web build
```

If the full web suite has unrelated failures, run and report focused Quiz,
block storage, preview, grading adapter, and asset portability tests separately.
The package typechecks and package-local tests remain mandatory.

## Completion Definition

The work is complete only when the new packages are the sole owners of Quiz
domain and reusable UI, all application and grading consumers use their public
entrypoints, asset and rich-text integrations use the existing feature
packages, and the original Quiz implementation under `block-content-editor`
has been removed.
