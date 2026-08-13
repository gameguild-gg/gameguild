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
- the shared Lexical surface package, only inside React/editor integration files;
- npm libraries required by quiz UI or quiz helpers.

The Lexical dependency is intentional. Lexical is being extracted as the shared
package `@game-guild/lexical-surface`, so quiz React code should import its
public surface directly from that package once available. It must not import
Lexical components through the web block-content-editor folder.

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
      rich-text.ts
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
      lexical/
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
  exported UI, icon packages, the Lexical surface package used by essay React
  integration, and npm libraries required by quiz components (including
  `mathlive` if the package keeps the current math input).
- `devDependencies`: test tooling, TypeScript config, React test utilities, and
  package-local test dependencies.

Do not rely on dependencies leaking from `apps/web`.

The package needs two separate dependency surfaces:

- `@game-guild/quiz/domain` has no runtime UI dependency and must remain usable
  from grading/server code.
- `@game-guild/quiz/react` may depend on React, `@game-guild/ui`, the Lexical
  surface package, icons, and authoring/rendering libraries. It must declare
  all of them in `@game-guild/quiz`, rather than relying on web dependencies.

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

`SerializedRichTextPayload` is the persisted quiz contract for essay model
answers. The React Lexical adapter converts between this payload and the
Lexical surface package's editor state at the UI boundary; neither the domain
nor grading depends on Lexical types.

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

The evaluator is the single source of truth for local-practice correctness.
It must cover the deterministic rules currently split between
`use-quiz-answers.ts` and the grading quiz adapter: choice, fill blank, short
answer, essay policy, matching, ordering, categorization, rating, numeric,
formula, hotspot, and highlight. It should return a quiz-owned result such as
`correct`, `incorrect`, `pending`, or `unsupported`, plus any quiz-specific
diagnostics required by the UI. It must not fabricate trusted feedback when
given a learner-safe/server-graded entry with no answer key.

`@game-guild/grading/adapters/quiz` calls this evaluator for authored,
server-owned answer keys and translates its result into `GradeItemResult` and
`GradeResult`. Grading continues to own scoring, gradebook semantics, and trust
boundaries; quiz continues to own question semantics. No second implementation
of quiz correctness remains in grading or in a React hook.

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
- may import the Lexical surface package for essay authoring and rendering in
  `react/lexical/` or another React-only integration file;
- does not import from the web block-content-editor folder.
- owns any quiz-specific support component that is not already a shared package.
- uses props or small local adapters when it needs host/editor integration.

The current web-only `LexicalSurface` use moves behind a quiz-owned React
adapter that imports the Lexical surface package. The adapter accepts and emits
`SerializedRichTextPayload`; `EssayEditor` and `EssayRenderer` must not import
the web lexical-surface path themselves.

The adapter should use the Lexical surface package's public serialized-state and
plain-text callback. It must not import `LexicalEditor`, `$getRoot`, or other
Lexical internals merely to calculate the essay plain-text answer.

The current web-only math input, editor shell, and editor settings integrations
need an explicit owner before moving React files:

- move the math input into quiz React (or a separately extracted shared math
  package) because numeric/formula quiz UI owns its workflow;
- replace `BlockEditorShell` and `useEditorSettings` imports with quiz-owned UI
  or explicit host props/adapters;
- import common primitives from `@game-guild/ui`, never `@/components/ui`.

### Migration Steps

1. Implement and validate `@game-guild/lexical-surface` first. It must export a
  public React/editor entrypoint with serialized-state props and no generic
  block insert/embed capability. Add it as a quiz React dependency before
  moving essay UI.
2. Create `@game-guild/quiz` with isolated `domain` and `react` entrypoints.
  Add an import-boundary test or static check proving the domain cannot import
  React, Lexical, web paths, block-list, or grading.
3. Move the authoring types from the current `types.ts` into `domain/types.ts`.
  Replace `SerializedEditorState` with `SerializedRichTextPayload` without
  changing persisted quiz JSON.
4. Split the current `contracts.ts` by responsibility: learner redaction into
  `domain/learner.ts`, authoring completeness into `domain/validation.ts`, and
  runtime/answer contracts into `domain/runtime.ts` and `domain/answers.ts`.
5. Move factories from the current `types.ts`, formula helpers, and highlight
  parsing into `domain/factories.ts`, `domain/formula.ts`, and
  `domain/highlight.ts`. Add test vectors for every question type.
6. Extract the full local-practice evaluator from `use-quiz-answers.ts` into
  `domain/evaluation.ts`. The hook becomes React state and submission UI only;
  it imports no `@game-guild/grading` symbol.
7. Update the grading quiz adapter to import `@game-guild/quiz/domain` and call
  the evaluator for deterministic question semantics. Keep unknown JSON
  normalization, gradebook scoring, and server trust logic in grading.
8. Add `@game-guild/quiz` to `packages/features/grading/package.json`, then run
  grading tests before moving React files.
9. Move React components, hooks, editors, renderers, templates, and tests into
  `src/react`. Replace app-alias imports with package modules,
  `@game-guild/ui`, the Lexical surface package, or explicit host props.
10. Create a quiz-owned Lexical adapter for essay UI. It is the only quiz file
   that imports the Lexical surface package and it adapts to/from
   `SerializedRichTextPayload`.
11. Move quiz-owned support UI into the package:
  - math input used by numeric/formula quiz UI;
  - quiz editor shell/settings behavior or explicit host adapters;
  - any quiz-specific labels, templates, and type-selector support.
12. Declare all package dependencies in `packages/features/quiz/package.json`.
13. Add `@game-guild/quiz` to `apps/web/package.json`.
14. Add `@game-guild/quiz` to `apps/web/next.config.ts` `transpilePackages`.
15. Move or duplicate tests into the package for:
   - learner redaction;
   - authoring validation;
   - factories;
   - quiz answer evaluation;
   - formula helpers;
   - highlight parsing;
   - runtime display;
   - local-practice answer feedback;
   - server-graded answer collection without local correctness.
16. Replace the web quiz folder with compatibility exports or direct imports from
   `@game-guild/quiz/react`.
17. Update block picker/template imports so quiz templates come from
   `@game-guild/quiz/react`.
18. Keep grading core content-type agnostic. Only the quiz adapter may depend on
  `@game-guild/quiz/domain`.
19. Search for remaining web quiz folder imports and app-alias imports from the
  new package.
20. Remove old web quiz files only when every remaining import is intentionally
   routed through `@game-guild/quiz`.
21. Run focused package, web, grading, and standalone/Docker build checks.

### Acceptance Criteria

- `@game-guild/quiz` contains the complete quiz feature.
- `@game-guild/quiz/domain` has no UI, block-list, grading, React, Next.js, or
  Lexical imports.
- Essay domain contracts use `SerializedRichTextPayload`, not Lexical types.
- Quiz React imports the Lexical surface package only through its quiz-owned
  adapter and never through the web block-content-editor folder.
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
- `useQuizAnswers` does not import `@game-guild/grading` and does not duplicate
  evaluator logic.
- The grading quiz adapter does not duplicate question correctness rules already
  owned by `@game-guild/quiz/domain`.
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
- Do not implement the Lexical surface extraction in this work; consume its
  public package entrypoint once that extraction is available.

## Dependency Direction

Allowed:

```text
apps/web block-content-editor UI -> @game-guild/block-list
apps/web block-content-editor UI -> @game-guild/quiz
apps/web block-content-editor UI -> @game-guild/quiz/react
apps/web block-content-editor UI -> @game-guild/grading

@game-guild/quiz/react -> @game-guild/quiz/domain
@game-guild/quiz/react -> @game-guild/ui
@game-guild/quiz/react -> @game-guild/lexical-surface
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
@game-guild/quiz/domain -> @game-guild/lexical-surface
@game-guild/quiz/react -> @game-guild/grading
@game-guild/quiz/react -> apps/web block-content-editor paths
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
