# Quiz Content Package Extraction

Status: implemented.

## Summary

Create a framework-independent package for the complete persisted quiz document:

```text
packages/features/quiz-content
```

The package name will be:

```text
@game-guild/quiz-content
```

The package will own the specialization of `@game-guild/block-list` for quiz,
the versioned quiz content document, runtime parsing, normalization, grading
composition, learner-document projection, and conversion between persisted
blocks and the collection items consumed by `@game-guild/quiz-surface`.

This extraction closes the remaining dependency from the dedicated quiz
content editor to:

```text
apps/web/src/components/block-content-editor
```

The product has not launched. There is no released quiz document format to
migrate. The implementation must establish the intended V1 document directly,
update current fixtures and consumers in the same change, and avoid legacy
parsers, compatibility aliases, dual writes, deprecated exports, or database
migrations.

## Context

The current package split is incomplete:

- `@game-guild/quiz` owns individual question and answer semantics;
- `@game-guild/quiz-surface` owns React authoring and learner UI;
- `@game-guild/block-list` owns generic ordered block primitives;
- `@game-guild/grading` owns grading policy and trusted grading adapters;
- the dedicated web quiz editor still owns the concrete quiz collection and
  persisted document composition.

The problematic imports are currently in:

```text
apps/web/src/components/learning/console/courses/[course]/content/[contentId]/quiz-content-editor.tsx
```

That component imports `BlockArray`, `BlockStorage`, storage conversion, and
`nextBlockId` from `block-content-editor`. It also implements locally:

- the quiz content root type;
- structural detection of persisted storage;
- parsing and fallback behavior;
- block-to-surface-item conversion;
- surface-item-to-block conversion;
- grading synchronization during serialization;
- document emission to the host.

This makes the old editor the effective owner of the quiz document even though
the quiz domain and UI have already moved into packages. It also leaves runtime
validation weak: the current parser only confirms that `order` is an array and
`blocks` is an object.

## Architectural Decision

### A third package with a distinct responsibility

`@game-guild/quiz-content` will own quiz documents and quiz collections. It is
not a second quiz domain package and not a React package.

The boundaries will be:

```text
@game-guild/quiz
  individual questions, answers, learner-safe entries, validation semantics

@game-guild/quiz-content
  ordered quiz collection, persisted document, parsing, grading composition

@game-guild/quiz-surface
  React editors, players, collection interaction, drag-and-drop
```

The new package specializes `block-list`; it does not reimplement the generic
block engine.

### Dependency direction

Target graph:

```text
@game-guild/quiz-content
  -> @game-guild/quiz
  -> @game-guild/block-list
  -> @game-guild/grading

@game-guild/quiz-surface
  -> @game-guild/quiz
  -> @game-guild/quiz-content
  -> @game-guild/assets
  -> @game-guild/lexical-surface
  -> @game-guild/ui

@game-guild/grading
  -> @game-guild/quiz

apps/web
  -> @game-guild/quiz-content
  -> @game-guild/quiz-surface
```

This graph does not create a cycle. `grading` must remain structurally unaware
of `quiz-content`; its existing quiz adapter may continue accepting block-like
input. `quiz-content` may call the public grading helpers when composing the
document.

Not allowed:

```text
@game-guild/quiz-content -> React
@game-guild/quiz-content -> Next.js
@game-guild/quiz-content -> @game-guild/quiz-surface
@game-guild/quiz-content -> apps/web
@game-guild/quiz-content -> block-content-editor paths

@game-guild/quiz -> @game-guild/quiz-content
@game-guild/block-list -> @game-guild/quiz-content
@game-guild/grading -> @game-guild/quiz-content
```

### Why this does not belong in `quiz`

`@game-guild/quiz` describes one question and its answer semantics. Importing
`block-list` and `grading` there would mix the domain of a question with course
content persistence, ordered collections, and scoring policy. It would also
break the existing architecture test that keeps the quiz domain independent
from block and grading layers.

### Why this does not belong in `quiz-surface`

The document format must be usable without React by server-side TypeScript,
tests, import/export tools, background jobs, and future non-React consumers.
Putting parsing or persistence in the surface would make UI code the owner of
the storage contract.

## Goals

- remove every dedicated quiz-content-editor import from
  `block-content-editor`;
- establish one canonical, versioned quiz document contract;
- define quiz-specific block, storage, item, and read-model types;
- validate unknown JSON before it becomes `QuizEntry`;
- normalize malformed order entries, duplicates, missing payloads, and orphans
  predictably;
- centralize quiz document parsing and serialization;
- centralize conversion between persisted blocks and surface items;
- centralize quiz block ID generation;
- centralize grading composition and synchronization for quiz documents;
- centralize learner-safe projection of an entire quiz document;
- preserve the separation between authoring content, learner content, answer
  keys, answers, and grading metadata;
- leave the old block editor only as a temporary generic consumer of the new
  contracts where quiz support is still needed;
- remove obsolete quiz-specific files from `block-content-editor` when they no
  longer have consumers;
- update the serialization map after implementation.

## Non-Goals

- rewriting `@game-guild/block-list`;
- moving React editors or players into `quiz-content`;
- moving question semantics out of `@game-guild/quiz`;
- moving trusted grading policy out of `@game-guild/grading`;
- changing the `program_contents.JsonBody` database column;
- creating a new API table or EF Core migration;
- implementing official Numeric or Formula grading;
- redesigning the assessment lifecycle;
- preserving unreleased document payloads without `schemaVersion`;
- removing all of `block-content-editor` in this change;
- making the generic block editor aware of quiz internals.

## Canonical V1 Document

The package will introduce a root-level schema version. This version belongs
to the quiz content document and is distinct from `grading.schemaVersion`.

```ts
export const QUIZ_CONTENT_SCHEMA_VERSION = 1 as const;
export const QUIZ_BLOCK_TYPE = "quiz" as const;

export interface QuizBlockDataMap {
  quiz: QuizEntry;
}

export type QuizBlock = TypedBlock<QuizBlockDataMap>;
export type QuizBlockList = TypedBlockList<QuizBlockDataMap>;
export type QuizBlockStorage = TypedBlockStorage<QuizBlockDataMap>;
export type QuizBlockOrderEntry = readonly [id: string, type: "quiz"];
export type QuizBlockView = TypedBlockView<QuizBlockDataMap>;

export interface QuizContentItem {
  id: string;
  entry: QuizEntry;
}

export interface QuizContentDocumentV1 {
  schemaVersion: 1;
  order: QuizBlockOrderEntry[];
  blocks: Record<string, QuizEntry>;
  grading?: ContentGradingDefinition;
}

export type QuizContentDocument = QuizContentDocumentV1;
```

The public type must describe the actual object sent as
`ProgramContent.JsonBody`. No application-owned wrapper may sit between this
type and the API payload.

### Document invariants

- `schemaVersion` is exactly `1`;
- each block ID is a non-empty string;
- every `order` entry has exactly two fields;
- every order type is exactly `"quiz"`;
- each ID appears at most once in `order`;
- every ordered ID has one payload in `blocks`;
- orphan payloads are not part of the normalized document;
- every payload passes the complete `QuizEntry` runtime schema;
- disabled grading is omitted rather than persisted;
- enabled grading is normalized and synchronized with the valid block set;
- grading items address blocks by the same stable ID;
- unknown root and question fields do not silently become part of V1.

Numeric strings may continue to be generated for new local block IDs, but the
document contract must accept any non-empty stable string. Persistence must not
make sequential numeric IDs a permanent domain requirement.

## Runtime Quiz Schemas

The current `validateQuizAuthoringEntry()` validates semantic completeness only
after a value has already been typed as `QuizEntry`. A document parser needs a
safe boundary from `unknown`.

Add runtime schemas to `@game-guild/quiz` for:

- the common entry base;
- feedback and settings;
- attachments and `AssetUri`;
- all 14 question variants;
- all four fill-blank input variants;
- nested option, pair, category, item, hotspot, highlight, scale, and formula
  structures;
- rich-text payload as an object-or-null boundary.

Use a maintained structured schema library already accepted in the workspace,
preferably Zod, instead of ad hoc casts and repeated property checks. The
schemas must infer or satisfy the existing TypeScript contracts so runtime and
compile-time definitions cannot drift unnoticed.

Public domain functions:

```ts
quizEntrySchema
safeParseQuizEntry(value: unknown): QuizEntryParseResult
parseQuizEntry(value: unknown): QuizEntry
isQuizEntry(value: unknown): value is QuizEntry
```

Structural parsing and semantic validation remain separate:

```text
unknown
  -> structural schema
    -> QuizEntry
      -> validateQuizAuthoringEntry
        -> complete/incomplete authoring issues
```

A structurally valid draft may still have semantic authoring issues. The
document parser must not confuse an incomplete draft with malformed JSON.

## Target Package Structure

```text
packages/features/quiz-content/
  package.json
  README.md
  tsconfig.json
  vitest.config.ts
  src/
    index.ts
    constants.ts
    types.ts
    ids.ts
    parsing.ts
    storage.ts
    grading.ts
    learner.ts
    validation.ts
    testing/
      fixtures.ts
      index.ts
    architecture.test.ts
    parsing.test.ts
    storage.test.ts
    grading.test.ts
    learner.test.ts
```

Files may be consolidated when small, but the boundaries between parsing,
storage conversion, grading composition, and learner projection must remain
visible.

## Public API

### Constants and types

Export:

- `QUIZ_CONTENT_SCHEMA_VERSION`;
- `QUIZ_BLOCK_TYPE`;
- `QuizBlockDataMap`;
- `QuizBlock`;
- `QuizBlockList`;
- `QuizBlockStorage`;
- `QuizBlockOrderEntry`;
- `QuizBlockView`;
- `QuizContentItem`;
- `QuizContentDocument`;
- `QuizContentDocumentV1`;
- `QuizContentParseIssue`;
- `QuizContentParseResult`;
- `QuizLearnerContentDocument`;
- `QuizRuntimeContentDocument`.

### Construction and IDs

```ts
createEmptyQuizContentDocument(): QuizContentDocument
createQuizContentItem(entry?: QuizEntry, id?: string): QuizContentItem
nextQuizContentItemId(items: readonly QuizContentItem[]): string
```

`nextQuizContentItemId()` may delegate to `@game-guild/block-list.nextBlockId`
after adapting items to the generic `{ id }` shape.

### Item, block, and storage conversion

```ts
quizContentItemsToBlocks(items): QuizBlockList
quizBlocksToContentItems(blocks): QuizContentItem[]
quizBlocksToStorage(blocks): QuizBlockStorage
quizStorageToBlocks(storage): QuizBlockList
quizContentItemsToStorage(items): QuizBlockStorage
quizStorageToContentItems(storage): QuizContentItem[]
quizDocumentToContentItems(document): QuizContentItem[]
quizContentItemsToDocument(items, grading?): QuizContentDocument
```

These helpers must delegate generic ordering and storage work to
`@game-guild/block-list`. The new package adds quiz validation and concrete
typing; it does not duplicate the block-list algorithms.

### Parse and serialize

```ts
parseQuizContentDocument(value: unknown): QuizContentParseResult
serializeQuizContentDocument(input: QuizContentDocumentInput): QuizContentDocument
assertQuizContentDocument(value: unknown): QuizContentDocument
isQuizContentDocument(value: unknown): value is QuizContentDocument
```

Recommended result shape:

```ts
export interface QuizContentParseIssue {
  code:
    | "invalid-root"
    | "unsupported-version"
    | "invalid-order-entry"
    | "duplicate-block-id"
    | "missing-block-payload"
    | "orphan-block-payload"
    | "invalid-quiz-entry"
    | "invalid-grading";
  path: string;
  message: string;
}

export interface QuizContentParseResult {
  document: QuizContentDocument;
  issues: QuizContentParseIssue[];
}
```

Parsing policy:

- invalid root or unsupported version returns an empty V1 document plus an
  issue;
- invalid individual blocks are omitted and reported without discarding other
  valid questions;
- duplicate order IDs keep the first valid occurrence and report the rest;
- missing and orphan payloads are omitted and reported;
- invalid grading is omitted and reported;
- parsing never returns unvalidated payloads typed as `QuizEntry`;
- serialization validates and normalizes before returning the API object;
- no function silently casts `Record<string, unknown>` to a quiz document.

### Grading composition

Move the host-level grading composition into pure helpers:

```ts
readQuizContentGrading(document): ContentGradingDefinition
enableQuizContentGrading(document, options?): QuizContentDocument
disableQuizContentGrading(document): QuizContentDocument
updateQuizContentGrading(document, updater): QuizContentDocument
syncQuizContentGrading(document): QuizContentDocument
```

Rules:

- disabled grading is represented at runtime by
  `createDisabledGradingDefinition()` and omitted from persisted JSON;
- enabling grading delegates to `createQuizGradingDefinition()`;
- changing questions delegates to `syncQuizGradingDefinition()`;
- serialization delegates grading validation to `@game-guild/grading`;
- the package does not introduce `resultUse`, `feedbackOnly`, or `gradebook`;
- assessment groups and weights remain outside the quiz content document.

### Learner document projection

Move whole-document runtime preparation out of
`apps/web/src/lib/courses/server-actions.ts`. Keep the learner-safe and local
practice contracts distinct:

```ts
interface QuizLearnerContentDocument {
  schemaVersion: 1;
  order: QuizBlockOrderEntry[];
  blocks: Record<string, QuizLearnerEntry>;
  grading?: ContentGradingDefinition;
}

type QuizRuntimeContentDocument =
  | { mode: "local-practice"; document: QuizContentDocument }
  | { mode: "server-graded"; document: QuizLearnerContentDocument };

toQuizLearnerContentDocument(
  document: QuizContentDocument,
): QuizLearnerContentDocument;

prepareQuizContentForRuntime(
  document: QuizContentDocument,
  mode: "local-practice" | "server-graded",
): QuizRuntimeContentDocument;
```

Rules:

- `toQuizLearnerContentDocument()` converts every valid authoring entry with
  `toQuizLearnerEntry()`;
- `server-graded` must never expose answer-key or `authorOnly` fields;
- `local-practice` may remain authoring-shaped because local evaluation needs
  the answer key, but the type and function name must make that trust boundary
  explicit;
- order, IDs, schema version, and permitted grading metadata remain stable;
- learner projection must be tested against the answer-key inventory for all
  14 question types.

## Surface Integration

`QuizCollectionItem` is currently declared inside `quiz-surface`. Replace it
with the package-owned `QuizContentItem`. `quiz-surface` should import this type
for its props, while consumers import the type directly from `quiz-content`.
Remove `QuizCollectionItem`; do not retain a deprecated alias or duplicate
interface.

Update `QuizCollectionEditorProps`:

```ts
export interface QuizCollectionEditorProps {
  items: QuizContentItem[];
  onChange: (items: QuizContentItem[]) => void;
  createItemId?: (items: QuizContentItem[]) => string;
  submissionMode?: QuizSubmissionMode;
  readOnly?: boolean;
  onDragStateChange?: (dragging: boolean) => void;
}
```

Prefer making the default ID strategy call `nextQuizContentItemId()` so hosts
do not need to provide the standard implementation. Keep `createItemId` only
as an optional override for consumers with server-assigned IDs.

Move `QuizSubmissionMode` into a public `quiz-surface` shared/player contract.
The old web-only `lib/quiz-submission-mode.ts` must be removed after consumers
are updated.

## Web Integration

Refactor the dedicated quiz editor so it owns only React form state and visual
controls.

Remove local definitions and helpers:

- `ParsedQuizContent`;
- local `isBlockStorage()`;
- local `parseQuizContent()`;
- local `serializeQuizContent()`;
- local `ensureQuizGradingDefinition()` where replaced by package helpers;
- `BlockArray` state;
- manual block/item mapping;
- imports from `block-content-editor/lib/storage/editor/*`.

Target host state:

```ts
const initial = parseQuizContentDocument(initialContent);
const [document, setDocument] = useState(initial.document);
const items = quizDocumentToContentItems(document);
```

The host may keep visual form controls for max score and passing score, but
updates must call `quiz-content` grading helpers and emit a complete
`QuizContentDocument`.

`content-item-editor.tsx` continues passing the final object as `jsonBody`.
No API contract or database mapping change is required.

## Transitional Block Editor Cleanup

The old generic block editor still supports quiz blocks for its own project
mode. That support may remain temporarily, but ownership must be inverted:

```text
allowed: block-content-editor -> @game-guild/quiz-content
forbidden: @game-guild/quiz-content -> block-content-editor
```

Required cleanup:

1. Change the old `BlockDataMap` to derive its quiz member from
   `QuizBlockDataMap["quiz"]`, instead of independently declaring
   `quiz: QuizEntry`.
2. Remove `nodes/quiz-node.tsx`; use `QuizBlockView` or a generic `BlockView`
   typed by `QuizBlockDataMap`.
3. Update `PreviewQuiz` to accept the package-owned view/item contract.
4. Move `QuizSubmissionMode` to `quiz-surface` and remove the web copy.
5. Delete `hooks/editor/use-quiz-feedback.ts` if import search still confirms
   that it is unused.
6. Keep only generic composition registrations in the old block catalog,
   picker, viewer, and modal.
7. Do not move generic project-type or block-catalog behavior into
   `quiz-content`.
8. Verify that no old file remains the source of a quiz persistence or runtime
   contract.

The goal is not to remove generic quiz rendering from the old editor in this
phase. The goal is to ensure it consumes the new package exactly like any
other host, making later deletion of `block-content-editor` independent from
the quiz feature.

## Implementation Phases

### Phase 1: Runtime schemas in `@game-guild/quiz`

1. Add the schema-library dependency.
2. Define strict schemas for common and nested contracts.
3. Define the 14-entry discriminated union.
4. Export parse, safe-parse, and guard helpers.
5. Ensure inferred schemas satisfy the existing TypeScript contracts.
6. Add valid, invalid, and incomplete-draft tests for every question type.
7. Keep `validateQuizAuthoringEntry()` as semantic validation after parsing.

### Phase 2: Create `@game-guild/quiz-content`

1. Scaffold the package using workspace TypeScript and Vitest conventions.
2. Add dependencies on `quiz`, `block-list`, and `grading`.
3. Define constants, V1 types, and empty document.
4. Implement item/block/storage conversions by delegating to `block-list`.
5. Implement ID generation.
6. Implement parse results and issue reporting.
7. Implement document serialization and assertions.
8. Add architecture tests banning UI and application imports.

### Phase 3: Grading and learner projection

1. Move grading composition from the web editor into package helpers.
2. Normalize and synchronize grading only against valid blocks.
3. Move whole-document learner preparation from web server actions.
4. Add all-question answer-key leak tests.
5. Verify disabled grading is omitted from persisted JSON.
6. Verify learner projection does not mutate authoring input.

### Phase 4: Update `quiz-surface`

1. Replace local `QuizCollectionItem` with `QuizContentItem`.
2. Remove the old `QuizCollectionItem` export and update consumers to import
   `QuizContentItem` from `quiz-content`.
3. Use package ID generation as the default.
4. Move/export `QuizSubmissionMode` from the surface.
5. Update collection tests and architecture constraints.
6. Confirm no surface source imports grading directly.

### Phase 5: Update application consumers

1. Refactor `quiz-content-editor.tsx` to use the new document API.
2. Remove every block-content-editor storage import from that file.
3. Replace `prepareQuizContentForLearner()` in server actions with the package
   projection.
4. Update content creation to use `createEmptyQuizContentDocument()`.
5. Update editor, learner-delivery, assessment lifecycle, and server-action
   fixtures with `schemaVersion: 1`.
6. Keep API `jsonBody` transport unchanged.

### Phase 6: Clean the old source

1. Derive any retained generic editor quiz mapping from `quiz-content`.
2. Remove old quiz node and submission-mode contracts.
3. Remove unused quiz feedback code.
4. Update preview adapters to package-owned types.
5. Run import searches for duplicate quiz block/storage contracts.
6. Delete empty directories and stale tests.

### Phase 7: Documentation and verification

1. Update `docs/types/quiz-surface-serialization-map.md` so V1 and its runtime
   parser become the canonical map.
2. Mark gap 3 resolved and revise gaps 1, 2, and 4 according to the result.
3. Update the READMEs of `quiz`, `quiz-content`, and `quiz-surface`.
4. Mark this plan implemented only after all acceptance criteria pass.

## Test Plan

### Quiz domain schemas

- parse one complete fixture for every question type;
- reject unknown question discriminants;
- reject malformed nested arrays and records;
- reject invalid `AssetUri` values;
- distinguish structurally valid drafts from semantic answer-key issues;
- verify schema and TypeScript union exhaustiveness together.

### Quiz content parsing

- parse an empty V1 document;
- round-trip all 14 question types in one document;
- reject unsupported `schemaVersion`;
- report malformed order tuples;
- report and omit duplicate IDs;
- report and omit missing payloads;
- report and omit orphan payloads;
- report and omit structurally invalid questions while preserving valid ones;
- report and omit invalid grading;
- strip unknown root and question fields according to the strict V1 policy;
- never return an unvalidated object as `QuizEntry`.

### Storage and IDs

- item -> block -> storage -> block -> item round-trip;
- order preservation after drag-and-drop;
- stable IDs after edits and reorder;
- new ID generation with numeric and non-numeric existing IDs;
- no mutation of caller-owned arrays or entries.

### Grading

- enable, disable, update, and synchronize grading;
- add and remove grading items as questions change;
- preserve max and passing score rules;
- classify deterministic, manual, and unsupported questions correctly;
- omit disabled grading from JSON;
- reject grading items that reference removed blocks.

### Learner projection

- redact answer keys for every question type in server-graded mode;
- remove `authorOnly` attachments;
- preserve learner-visible assets and order;
- keep local-practice behavior explicit;
- verify the authoring document is not mutated;
- verify answer-key inventory tests stay exhaustive.

### Integration

- load a V1 quiz document in the course content editor;
- edit, reorder, add, and delete questions;
- save and reopen without data loss;
- toggle grading and update score settings;
- render edit and preview modes;
- deliver learner-safe server-graded content;
- deliver local-practice content intentionally;
- reconcile the linked assessment after save;
- create a new questionnaire with a valid empty V1 document.

### Architecture

- `quiz-content` has no React, Next.js, UI, app alias, or block-content-editor
  imports;
- `quiz` remains independent from `quiz-content`, `block-list`, and grading;
- `grading` does not import `quiz-content`;
- dedicated quiz course components have no block-content-editor imports;
- no duplicate `QuizContentDocument`, `QuizBlockDataMap`, or
  `QuizCollectionItem` declaration remains;
- no old compatibility re-export is introduced.

## Verification Commands

Run at minimum:

```bash
pnpm --filter @game-guild/quiz typecheck
pnpm --filter @game-guild/quiz test
pnpm --filter @game-guild/quiz-content typecheck
pnpm --filter @game-guild/quiz-content test
pnpm --filter @game-guild/quiz-surface typecheck
pnpm --filter @game-guild/quiz-surface test
pnpm --filter @game-guild/grading typecheck
pnpm --filter @game-guild/grading test
pnpm --filter @game-guild/web exec tsc --noEmit --pretty false
```

Run focused web tests for:

```text
quiz-content-editor
content-item-editor
assessment lifecycle
course server actions / learner delivery
block storage and preview adapters still retained by block-content-editor
```

Final import searches:

```bash
rg 'block-content-editor/lib/storage/editor' \
  apps/web/src/components/learning/console/courses \
  packages/features/quiz \
  packages/features/quiz-content \
  packages/features/quiz-surface

rg 'interface QuizCollectionItem|interface QuizContentDocument|quiz: QuizEntry' \
  apps/web/src packages/features
```

The first search must return no dedicated quiz course or package dependency.
The second may return only the canonical declarations or intentional generic
composition verified during implementation.

## Acceptance Criteria

- `@game-guild/quiz-content` is the only owner of the persisted quiz document
  and concrete quiz block collection;
- the canonical document includes `schemaVersion: 1`;
- every persisted question passes a runtime `QuizEntry` schema;
- parsing malformed content produces explicit issues and never unsafe casts;
- `quiz-content-editor.tsx` imports no block-content-editor code;
- the surface item type comes from `quiz-content`;
- standard ID generation comes from `quiz-content`;
- grading composition is not reimplemented in the web component;
- whole-document learner redaction is not implemented in web server actions;
- all 14 question types round-trip through the V1 document;
- server-graded learner documents contain no answer keys;
- disabled grading is absent from persisted JSON;
- no database migration or API table change is introduced;
- retained old block editor quiz support consumes package-owned contracts;
- obsolete quiz-specific source files in `block-content-editor` are deleted;
- package architecture tests enforce the dependency direction;
- the quiz serialization type map is updated to match the implemented V1
  contract;
- all package and focused integration tests pass.

## Completion Definition

The extraction is complete only when deleting the quiz storage contracts and
quiz-specific adapters from `block-content-editor` no longer affects the
dedicated quiz editor, learner delivery, grading composition, or quiz package
tests. At that point, the old editor is at most a consumer of
`@game-guild/quiz-content`, never an owner or dependency of the quiz feature.
