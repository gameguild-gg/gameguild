# Block List and Quiz Package Extraction Plan

## Summary

This plan extracts two independent packages from the current block content editor
area:

- Part 1: `@game-guild/block-list`
- Part 2: `@game-guild/quiz`

Each package has its own responsibility and dependency boundary. `block-list`
owns only ordered block structure. `quiz` owns the complete quiz feature:
domain contracts, pure helpers, React editors, React renderers, runtime hooks,
settings UI, and templates. The web block content editor composes both packages.

## Package Boundaries

### `@game-guild/block-list`

Purpose: typed block-list structure, persistence conversion, ordered-list
operations, and generic read-model projection.

Must not import:

- React;
- Next.js;
- Lexical;
- quiz;
- grading;
- block-content-editor UI;
- block node components;
- icon libraries.

### `@game-guild/quiz`

Purpose: the full quiz feature package.

It owns:

- quiz question contracts;
- quiz runtime contracts;
- validation;
- learner redaction helpers;
- factories;
- formula and highlight utilities;
- answer-state helpers;
- React editors;
- React renderers;
- quiz display/wrapper/feedback UI;
- quiz settings UI;
- quiz templates/type selector.

Must not import:

- Next.js;
- block-list;
- grading;
- block-content-editor UI;

May import:

- React;
- UI primitives;
- icon libraries;
- Lexical only inside React/editor integration files.
- npm libraries required by quiz UI or quiz helpers.

The quiz package should depend on other workspace packages when they are real
shared packages, such as `@game-guild/ui`. When current quiz code imports local
modules from the web block-content-editor folder, move the quiz-owned pieces
into `@game-guild/quiz` first. Remove the original web copy only after import
search confirms it is used only by quiz.

The package must expose a pure domain entrypoint that has no React, Lexical,
Next.js, block-list, or grading imports.

Quiz-specific answer evaluation belongs to the quiz domain. The grading adapter
may call that evaluator and translate the result into grading package results.
Quiz React code must not import `@game-guild/grading` for local-practice checks.

### Composition Layer

The web block content editor remains the composition layer.

It may import:

- `@game-guild/block-list` for `Block`, `BlockList`, storage, and list
  operations;
- `@game-guild/quiz` for quiz components, templates, runtime hooks, data
  contracts, and factories through explicit entrypoints;
- `@game-guild/grading` for grading definitions and quiz adapter behavior;
- React UI, icons, editors, renderers, and picker components for non-quiz block
  types.

## Part 1: `@game-guild/block-list`

### Goal

Create a pure package for block-list structure so the editor no longer owns the
fundamental block model. The package provides the generic type machinery needed
for consumers to define safe concrete block unions without teaching the package
about those concrete block types.

### Target Package

```text
packages/features/block-list/
  package.json
  README.md
  tsconfig.json
  vitest.config.ts
  src/
    index.ts
    types.ts
    ids.ts
    list.ts
    storage.ts
    view.ts
    validation.ts
    storage.test.ts
    list.test.ts
    view.test.ts
```

### Target Contracts

```ts
export type BlockTypeId = string;

export interface Block<TType extends BlockTypeId = BlockTypeId, TData = unknown> {
  id: string;
  type: TType;
  data: TData;
}

export type BlockList<TBlock extends Block = Block> = TBlock[];

export type BlockOrderEntry<TType extends BlockTypeId = BlockTypeId> =
  readonly [id: string, type: TType];

export interface BlockStorage<
  TType extends BlockTypeId = BlockTypeId,
  TData = unknown,
> {
  order: BlockOrderEntry<TType>[];
  blocks: Record<string, TData>;
}

export type BlockDataByType = Record<string, unknown>;

export type TypedBlock<TMap extends BlockDataByType> = {
  [TType in keyof TMap & string]: Block<TType, TMap[TType]>;
}[keyof TMap & string];

export type TypedBlockList<TMap extends BlockDataByType> =
  TypedBlock<TMap>[];

export interface BlockView<
  TType extends BlockTypeId = BlockTypeId,
  TData = unknown,
> {
  id: string;
  type: TType;
  data: TData;
  version: number;
}

export type TypedBlockView<TMap extends BlockDataByType> = {
  [TType in keyof TMap & string]: BlockView<TType, TMap[TType]>;
}[keyof TMap & string];
```

Concrete consumers define their map outside the package:

```ts
interface EditorBlockDataMap {
  quiz: QuizEntry;
  markdown: MarkdownData;
  video: VideoData;
}

type EditorBlock = TypedBlock<EditorBlockDataMap>;
type EditorBlockList = TypedBlockList<EditorBlockDataMap>;
```

This keeps `block-list` independent while still making the concrete editor block
union type-safe.

### Target Helpers

`ids.ts`:

- `nextBlockId(blocks)`
- `isNumericBlockId(value)`

`list.ts`:

- `insertBlock(blocks, index, block)`
- `updateBlock(blocks, id, dataOrUpdater)`
- `removeBlock(blocks, id)`
- `moveBlock(blocks, fromIndex, toIndex)`
- `findBlock(blocks, id)`
- `hasBlockType(blocks, type)`

`storage.ts`:

- `blocksToStorage(blocks)`
- `storageToBlocks(storage)`
- `serializeBlockList(blocks)`
- `deserializeBlockList(data)`
- `EMPTY_BLOCK_STORAGE`

`view.ts`:

- `blockToView(block, options?)`
- `blocksToViews(blocks, options?)`

The view/read model is generic and always uses `{ id, type, data, version }`.
It must not special-case any block type or rename payload fields per type.

`validation.ts`:

- `isBlock(value)`
- `isBlockStorage(value)`
- `normalizeBlockStorage(value)`

### Migration Steps

1. Create the package with pure types and helpers.
2. Add unit tests for id generation, insert/update/remove/move, storage
   roundtrip, invalid storage normalization, and view conversion.
3. Update the web editor's `block-structure.ts` to re-export or wrap the package
   types while preserving existing import paths.
4. Update the web editor's `block-storage.ts` to call package helpers while
   preserving existing function names where useful.
5. Define the concrete editor block map in the web editor and derive the editor
   block union from `TypedBlock<EditorBlockDataMap>`.
6. Refactor preview components and `BlockArrayViewer` to consume the canonical
   `BlockView` shape from `@game-guild/block-list`.
7. Use `data` for every block view payload, including quiz. Do not use a
   separate `entry` payload field for quiz preview.
8. Keep the current block catalog in the web editor:
   - known block type names;
   - icons;
   - labels;
   - UI categories;
   - `createEmpty` factories.
9. Run focused package tests and web quiz/block tests.

### Acceptance Criteria

- `@game-guild/block-list` has no UI, quiz, grading, node, or Lexical imports.
- The package can serialize and deserialize arbitrary block types.
- The package does not know any concrete block type such as `quiz`,
  `rich-text`, `video`, or `markdown`.
- The package provides generic type-map helpers so consumers can build safe
  concrete block unions without adding concrete block knowledge to `block-list`.
- The package provides a generic `BlockView` read model with `{ id, type, data,
  version }`.
- There are no type-specific branches in block-list view conversion.
- Web preview components consume the canonical `BlockView` shape.
- Quiz preview uses the same `data` field as every other block type.
- Existing web editor block save/load behavior remains unchanged.
- Existing `BlockArrayEditor` and `BlockArrayViewer` keep working through the
  current compatibility exports.
- Existing course content save/reload paths are unaffected.

### Non-Goals

- Do not move block UI.
- Do not move block picker UI.
- Do not move block-specific data types from nodes.
- Do not move quiz types in Part 1.
- Do not touch Lexical surface extraction.
- Do not change persisted JSON shape.
- Do not add type-specific preview adapters to `block-list`.

## Part 2: `@game-guild/quiz`

### Goal

Create a complete quiz feature package so quiz types, helpers, hooks, renderers,
editors, settings, and templates no longer live inside the web block content
editor folder.

### Target Package

```text
packages/features/quiz/
  package.json
  README.md
  tsconfig.json
  vitest.config.ts
  src/
    index.ts
    domain/
      index.ts
      types.ts
      runtime.ts
      learner.ts
      validation.ts
      factories.ts
      answers.ts
      evaluation.ts
      formula.ts
      highlight.ts
      test-vectors.ts
      validation.test.ts
      learner.test.ts
      evaluation.test.ts
      factories.test.ts
    react/
      index.ts
      components/
      quiz-display.tsx
      quiz-feedback.tsx
      quiz-question.tsx
      quiz-settings-dialog.tsx
      quiz-type-selector.tsx
      quiz-wrapper.tsx
      editors/
      hooks/
      renderers/
      question-types/
      react.test.tsx
```

### Package Exports

```json
{
  "exports": {
    ".": "./src/domain/index.ts",
    "./domain": "./src/domain/index.ts",
    "./react": "./src/react/index.ts"
  }
}
```

- `@game-guild/quiz/domain`: pure contracts and helpers. This is the entrypoint
  for `@game-guild/grading` quiz adapter and backend-facing code.
- `@game-guild/quiz/react`: quiz React UI.
- `@game-guild/quiz`: pure domain default export. App UI code should import
  React components from `@game-guild/quiz/react`.

### Package Dependencies

`@game-guild/quiz` should declare every dependency it needs in its own
`package.json`.

Expected dependency direction:

- `dependencies`: `@game-guild/ui`, React peer/runtime dependencies used by
  exported UI, icon packages, Lexical packages used by quiz React integration,
  and npm libraries required by quiz components.
- `devDependencies`: test tooling, TypeScript config, React test utilities, and
  package-local test dependencies.

Do not rely on dependencies leaking from `apps/web`.

After the package is created:

- add `@game-guild/quiz` to `apps/web/package.json`;
- add `@game-guild/block-list` to `apps/web/package.json` after Part 1;
- add `@game-guild/quiz` to `packages/features/grading/package.json` only when
  the quiz adapter imports `@game-guild/quiz/domain`;
- add both new workspace packages to `apps/web/next.config.ts`
  `transpilePackages` so Docker and standalone Next builds compile them.

### Target Domain Contracts

Move quiz contracts into `src/domain`:

- `QuizEntryType`
- `FillBlankInputType`
- `QuizSettings`
- `QuizFeedback`
- all authoring entry types;
- `QuizEntry`;
- `QuizAuthoringEntry`;
- `QuizPracticeEntry`;
- `QuizLearnerEntry`;
- `QuizRuntimeEntry`;
- `QuizSubmissionAnswer`;
- `QuizAnswerState`;
- `FormulaLearnerPrompt`;
- validation issue types.

The domain layer should define a UI-neutral rich-text payload for essay fields:

```ts
export type SerializedRichTextPayload = Record<string, unknown> | null;
```

The web Lexical UI can adapt Lexical serialized state to this payload, but the
domain layer does not import `lexical`.

### Target Domain Helpers

`domain/learner.ts`:

- `toQuizLearnerEntry(entry)`
- `toQuizLearnerEntries(entries)`

`domain/validation.ts`:

- `validateQuizAuthoringEntry(entry)`
- `isCompleteQuizAuthoringEntry(entry)`

`domain/factories.ts`:

- `createDefaultSettings()`
- `createSingleChoiceEntry()`
- `createMultipleChoiceEntry()`
- `createTrueFalseEntry()`
- `createFillInTheBlankEntry()`
- `createShortAnswerEntry()`
- `createEssayEntry()`
- `createMatchingEntry()`
- `createOrderingEntry()`
- `createCategorizationEntry()`
- `createRatingEntry()`
- `createNumericEntry()`
- `createFormulaEntry()`
- `createHotspotEntry()`
- `createHighlightEntry()`

`domain/answers.ts`:

- `createEmptyAnswerState()`
- answer-state normalizers that are independent from React.

`domain/evaluation.ts`:

- `evaluateQuizAnswer(entry, answer)`
- `evaluateQuizSubmission(entries, answers)`

These helpers return quiz-specific correctness/status data. They do not return
gradebook results and do not import `@game-guild/grading`.

`domain/formula.ts`:

- pure formula parsing/evaluation helpers used by authoring and local-practice
  UI.

`domain/highlight.ts`:

- `parseHighlightSource(source)`

### Target React Layer

Move the current quiz UI into `src/react`:

- editors;
- renderers;
- hooks;
- question-type components;
- `QuizDisplay`;
- `QuizFeedback`;
- `QuizQuestion`;
- `QuizSettingsDialog`;
- `QuizTypeSelector`;
- `QuizWrapper`;
- quiz templates.

React-layer rules:

- imports quiz contracts and helpers from `../domain`;
- keeps local-practice checking inside quiz domain evaluation, not grading;
- treats `submissionMode="server-graded"` as answer collection/submission state;
- does not compute trusted correctness or score for grading-enabled runtime;
- may import UI primitives and icons;
- may isolate Lexical essay integration in React files only.
- does not import from the web block-content-editor folder.
- owns any quiz-specific support component that is not already a shared package.
- uses props or small local adapters when it needs host/editor integration.

### Migration Steps

1. Create the package with `domain` and `react` entrypoints.
2. Move pure quiz types and helpers into `src/domain`.
3. Remove the direct `lexical` type dependency from quiz domain contracts.
4. Copy current quiz React components, hooks, editors, renderers, templates, and
   tests into `src/react`.
5. Replace app-alias imports in quiz UI with package-owned modules,
   `@game-guild/ui`, or explicit props.
6. Move quiz-owned support UI into the package:
   - math input used by numeric/formula quiz UI;
   - quiz editor shell, if needed;
   - quiz settings helpers, if needed.
7. Declare all package dependencies in `packages/features/quiz/package.json`.
8. Add `@game-guild/quiz` to `apps/web/package.json`.
9. Add `@game-guild/quiz` to `apps/web/next.config.ts` `transpilePackages`.
10. Move or duplicate tests into the package for:
   - learner redaction;
   - authoring validation;
   - factories;
   - quiz answer evaluation;
   - formula helpers;
   - highlight parsing;
   - runtime display;
   - local-practice answer feedback;
   - server-graded answer collection without local correctness.
11. Replace the web quiz folder with compatibility exports or direct imports from
   `@game-guild/quiz/react`.
12. Update block picker/template imports so quiz templates come from
   `@game-guild/quiz/react`.
13. Update `@game-guild/grading` quiz adapter to consume
   `@game-guild/quiz/domain` contracts and quiz evaluation where useful.
14. Add `@game-guild/quiz` to `packages/features/grading/package.json`.
15. Keep grading core content-type agnostic. Only the quiz adapter may depend on
   `@game-guild/quiz/domain`.
16. Search for remaining web quiz folder imports.
17. Remove old web quiz files only when every remaining import is intentionally
   routed through `@game-guild/quiz`.
18. Run focused package, web, and grading tests.

### Acceptance Criteria

- `@game-guild/quiz` contains the complete quiz feature.
- `@game-guild/quiz/domain` has no UI, block-list, grading, React, Next.js, or
  Lexical imports.
- Web quiz UI is moved into `@game-guild/quiz/react` or re-exported from there.
- Quiz React code has no imports from the web block-content-editor folder.
- Quiz React code does not import `@game-guild/grading`.
- `@game-guild/quiz` declares its own workspace and npm dependencies.
- `apps/web` depends on `@game-guild/quiz`.
- `apps/web/next.config.ts` transpiles `@game-guild/quiz`.
- Docker/standalone Next build does not rely on undeclared transitive
  dependencies.
- `@game-guild/grading` core does not import quiz.
- `@game-guild/grading` quiz adapter may import `@game-guild/quiz/domain`.
- Quiz-specific answer evaluation lives in `@game-guild/quiz/domain`; grading
  maps that result into trusted grading results on the server path.
- Learner-safe quiz contracts remain separate from authoring/practice contracts.
- Grading-disabled quiz practice still shows local correct/incorrect feedback.
- Grading-enabled quiz runtime still collects answers without computing trusted
  correctness in the client.
- Persisted quiz JSON shape remains unchanged.

### Non-Goals

- Do not make quiz depend on block-list.
- Do not make block-list depend on quiz.
- Do not make quiz depend on grading.
- Do not implement backend grading endpoints.
- Do not change course content persistence.
- Do not extract Lexical surface.

## Dependency Direction

Allowed:

```text
apps/web block-content-editor UI -> @game-guild/block-list
apps/web block-content-editor UI -> @game-guild/quiz
apps/web block-content-editor UI -> @game-guild/quiz/react
apps/web block-content-editor UI -> @game-guild/grading

@game-guild/quiz/react -> @game-guild/quiz/domain
@game-guild/quiz/react -> @game-guild/ui
@game-guild/grading/adapters/quiz -> @game-guild/quiz/domain
```

Not allowed:

```text
@game-guild/block-list -> @game-guild/quiz
@game-guild/block-list -> @game-guild/grading
@game-guild/block-list -> apps/web
@game-guild/quiz -> @game-guild/block-list
@game-guild/quiz -> @game-guild/grading
@game-guild/quiz -> apps/web
@game-guild/quiz/domain -> @game-guild/quiz/react
@game-guild/quiz/react -> @game-guild/grading
@game-guild/grading core -> @game-guild/quiz
```

## Build Wiring Checklist

Part 1:

- add `@game-guild/block-list` to `apps/web/package.json`;
- add `@game-guild/block-list` to `apps/web/next.config.ts`
  `transpilePackages`;
- run package tests and web focused tests.

Part 2:

- add `@game-guild/quiz` to `apps/web/package.json`;
- add `@game-guild/quiz` to `apps/web/next.config.ts` `transpilePackages`;
- add `@game-guild/quiz` to `packages/features/grading/package.json` when the
  quiz adapter imports its domain entrypoint;
- ensure `@game-guild/quiz/package.json` declares every React, UI, icon,
  Lexical, and npm dependency it imports;
- run package, web, grading, and Docker-relevant build checks.

## Validation Commands

Use Node 22 when running package commands in this workspace.

```bash
pnpm --filter @game-guild/block-list test
pnpm --filter @game-guild/block-list typecheck
pnpm --filter @game-guild/quiz test
pnpm --filter @game-guild/quiz typecheck
pnpm --filter @game-guild/grading test
pnpm --filter @game-guild/grading typecheck
pnpm --filter @game-guild/web exec vitest run src/components/block-content-editor/extras/quiz/contracts.test.ts src/components/block-content-editor/extras/quiz/quiz-display.test.tsx src/components/block-content-editor/extras/quiz/hooks/use-quiz-answers.test.tsx
```

## Execution Order

1. Implement Part 1 and stabilize web imports through compatibility wrappers.
2. Verify Part 1 independently.
3. Implement Part 2 using the stable block-list extraction as context, but not
   as a package dependency.
4. Verify Part 2 independently.
5. Update local READMEs in both new packages after implementation.
