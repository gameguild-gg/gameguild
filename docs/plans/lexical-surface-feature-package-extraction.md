# Lexical Surface Feature Package Extraction

## Summary

`lexical-surface` should become its own feature package instead of living inside
`apps/web/src/components/block-content-editor`.

The target package is:

```text
packages/features/lexical-surface
```

Package name:

```text
@game-guild/lexical-surface
```

The package owns the Lexical editing surface, Lexical nodes, Lexical plugins,
toolbar, slash picker, embeds, page layout, and the Lexical-specific helpers
needed to render and edit rich document content.

`block-content-editor` remains responsible for block-array authoring, block
storage, block registry, content item orchestration, and non-Lexical block
editors. It should consume `@game-guild/lexical-surface` instead of importing or
owning Lexical implementation details.

Before extraction, remove generic block insertion and block embed integration
from `LexicalSurface`. The Lexical editor must not expose a `+Block` control,
block-insert menu, or generic block embed plugin. Code-studio, markdown, HTML,
quiz, and every other block-array experience remain owned by
`block-content-editor` or their future feature packages.

## Motivation

The current layout allows `lexical-surface` to import block-content internals
directly. That creates coupling between:

- Lexical document editing;
- block-array content editing;
- Lexical insert features such as media, mermaid, vega, button, divider, and
  dialogs;
- larger block-editor experiences such as quiz and code-studio, which should
  stay outside the Lexical package.

This makes it too easy for future quiz/grading work or code-studio work to
become entangled with Lexical-specific rendering. The package extraction gives
Lexical a clear boundary before preview, grading, and block-content flows become
more complex.

## Current Dependency Shape

Current `lexical-surface` imports from these block-content areas:

- `../lib/lexical/shared-lexical-config`
- `../lib/lexical/initial-editor-state`
- `../lib/client-only-lazy`
- `../../extras/dialogs/*`
- `../../extras/admonition`
- `../../extras/button/button-styles`
- `../../extras/divider/divider-styles`
- `../../extras/math/math-input`
- `../../extras/mermaid/*`
- `../../extras/vega-lite/*`
- `../../extras/media/*`
- `../../extras/media-upload-dialog`
- `../../lib/storage/assets`
- `../../nodes/*`

The block insert/embed imports must be removed, not copied. The remaining
imports should become package-local modules or explicit package dependencies.

The integrated Lexical document features are in scope for the first package:

- admonition;
- button;
- divider;
- equation;
- excalidraw;
- media;
- mermaid;
- vega-lite;
- table;
- layout;
- sticky;
- collapsible;
- normal URL auto-embeds;
- emoji;
- page controls;
- floating toolbars;
- slash picker;
- shortcuts.

Block-array features are not in scope for this package:

- quiz;
- code-studio;
- markdown blocks;
- HTML blocks;
- project/content activity editors;
- block-array storage and registry logic.

Those features remain with `block-content-editor` for now and can be extracted
into their own feature packages later. `LexicalSurface` must not offer a UI path
to insert them.

## Target Ownership

### `@game-guild/lexical-surface`

Owns:

- `LexicalSurface`
- `LexicalSurfaceFeatures`
- `LexicalSurfaceProps`
- Lexical theme
- shared Lexical node list
- initial editor-state helpers
- Lexical nodes and plugins currently under `lexical-surface`
- Lexical-specific embedded components: admonition, button, divider, equation,
  excalidraw, media, mermaid, vega-lite, sticky, layout, collapsible, page,
  table, emoji, floating toolbar, slash picker, and shortcuts
- small internal UI helpers needed only by Lexical nodes/plugins

Does not own:

- `BlockArrayEditor`
- block-array storage
- content grading
- quiz authoring/grading
- code-studio authoring/runtime
- markdown or HTML block authoring/runtime
- course content save/load
- assessment projection
- project/content registry orchestration

### `@game-guild/block-content-editor`

Owns:

- block-array editor and viewer
- block component registry
- block type picker
- block storage and project/content persistence helpers
- content editor shell and orchestration
- integration points that mount `LexicalSurface`

Does not own:

- Lexical nodes/plugins/theme/toolbar internals
- Lexical rich document feature implementation

## Extraction Strategy

Use copy-first extraction.

The first implementation copies `apps/web/src/components/block-content-editor/lexical-surface`
and required dependencies into the new package. Consumers are moved to the
package export only after the package typechecks. The original app folder is
removed after all imports use `@game-guild/lexical-surface`.

This avoids editing consumers while the package is half-built.

## Package Shape

```text
packages/features/lexical-surface/
  package.json
  tsconfig.json
  README.md
  src/
    index.ts
    lexical-surface.tsx
    theme.ts
    lexical/
      initial-editor-state.ts
      shared-lexical-config.ts
    lib/
      client-only-lazy.tsx
      cn.ts
    components/
      dialogs/
      media/
      mermaid/
      vega-lite/
      math/
    nodes/
      custom-list-node.tsx
      media-node-base.tsx
      mermaid-data.ts
      vega-lite-data.ts
    admonition/
    button/
    code-action/
    collapsible/
    context-menu/
    divider/
    draggable/
    embeds/
    emoji/
    equation/
    excalidraw/
    floating/
    icons/
    layout/
    media/
    mermaid/
    page/
    picker/
    shared/
    shortcuts/
    sticky/
    table/
    toolbar/
    vega-lite/
```

The exact subfolder names can stay close to the current `lexical-surface`
structure. Copied dependencies should be grouped by purpose instead of keeping
path names that imply ownership by `block-content-editor`.

## Dependency Rules

Allowed package dependencies:

- React and React DOM
- Lexical packages
- Lucide icons
- KaTeX
- Excalidraw dependencies already used by the current surface
- Mermaid and Vega-Lite dependencies already used by the current surface
- shared UI primitives from workspace packages, preferably `@game-guild/ui`

Avoid:

- imports from `apps/web/src/components/block-content-editor/*`
- imports from `@/components/block-content-editor/*`
- imports from quiz/grading packages
- imports from code-studio packages or app folders
- imports from course/dashboard routes
- imports from app-only aliases when a workspace package should own the
  dependency
- generic block embed, block insert, block registry, or block picker concepts

The package can import truly shared primitives such as `@game-guild/ui`, but
Lexical-specific components copied for this package should stay package-local.

## Phase 1: Inventory and Boundaries

1. Generate a dependency inventory for every import inside
   `apps/web/src/components/block-content-editor/lexical-surface`.
2. Classify each dependency as:
   - package-local Lexical implementation;
   - integrated Lexical insert feature;
   - shared UI dependency;
   - block-content integration dependency;
   - candidate for copy into the package;
   - candidate for explicit adapter prop.
3. Remove `BlockEmbedPlugin`, `BlockInsertMenuPlugin`, any block-insert button,
  their feature flags, toolbar entries, and their callers. Confirm that
  `LexicalSurface` has no `+Block` or block insertion UI afterwards.
4. Confirm no quiz/grading/code-studio/markdown/HTML block dependency is pulled
  into the package.
5. Define package exports before copying code.

Acceptance criteria:

- every dependency leaving `lexical-surface` has a target owner;
- no `block-content-editor` import is left unexplained;
- no quiz/grading/code-studio/markdown/HTML block module is part of the package
  boundary;
- no `BlockEmbedPlugin`, `BlockInsertMenuPlugin`, block-insert button, or
  `+Block` UI remains in the Lexical surface.

## Phase 2: Package Skeleton

1. Create `packages/features/lexical-surface/package.json`.
2. Create `packages/features/lexical-surface/tsconfig.json`.
3. Create `packages/features/lexical-surface/src/index.ts`.
4. Add package dependencies matching the copied implementation.
5. Export:
   - `LexicalSurface`
   - `LexicalSurfaceProps`
   - `LexicalSurfaceFeatures`
   - `LEXICAL_SURFACE_THEME`
   - Lexical node/plugin exports that callers need.

Define a public change callback that supplies serialized editor state and plain
text without requiring feature packages to import `LexicalEditor`, `$getRoot`,
or other Lexical internals just to read text. Low-level editor access, if it is
still needed by a generic document consumer, must be a separate explicit API.

Do not export a block embed or block insertion API. The public surface is for
rich-document editing only, so future feature packages can compose it without
depending on block-array concepts.

Acceptance criteria:

- the package is recognized by `pnpm-workspace.yaml`;
- `pnpm --filter @game-guild/lexical-surface typecheck` can run;
- no app consumer is changed yet.

## Phase 3: Copy Lexical Surface

1. Copy the current `lexical-surface` folder into
   `packages/features/lexical-surface/src`.
2. Copy `lib/lexical/initial-editor-state.ts` into package-local
   `src/lexical/initial-editor-state.ts`.
3. Copy and adapt `lib/lexical/shared-lexical-config.ts` into package-local
   `src/lexical/shared-lexical-config.ts`.
4. Copy `lib/client-only-lazy.tsx` into package-local `src/lib`.
5. Update internal imports from relative app paths to package-local paths.

Acceptance criteria:

- copied Lexical modules no longer import from app
  `block-content-editor/lexical-surface`;
- shared Lexical config references package-local nodes;
- package entry exports the copied surface.

## Phase 4: Internalize Document-Feature Dependencies

Copy or localize the dependencies that are currently owned by
`block-content-editor` but used directly by integrated Lexical features:

- delete/base confirm dialogs used by Lexical nodes;
- admonition component/types;
- button styles;
- divider styles;
- math input;
- mermaid editor/viewer and related data type;
- vega-lite editor/viewer/theme helper and related data type;
- media asset image, upload dialog, URL detection, asset URL resolver;
- `BaseMediaData` and `MediaType`;
- custom list node.

Where a dependency is broader than Lexical, expose it as an adapter prop instead
of copying it blindly. The first candidates for adapter props are asset
resolution and media upload behavior.

Concrete block experiences such as quiz, code-studio, markdown blocks, and HTML
blocks must not be copied into this package. Do not add an adapter for inserting
them: block insertion belongs to `BlockArrayEditor`, not `LexicalSurface`.

Acceptance criteria:

- `packages/features/lexical-surface/src` has no import matching:
  - `@/components/block-content-editor`
  - `apps/web/src/components/block-content-editor`
  - `../extras`
  - `../nodes`
  - `../plugins`
  - `../lib`
- package source has no `BlockEmbedPlugin`, `BlockInsertMenuPlugin`, block
  registry, block picker, or `+Block` UI;
- package-local components preserve the current UI behavior.

## Phase 5: Replace App Imports

1. Add `@game-guild/lexical-surface` as a dependency where the web app or
   `@game-guild/block-content-editor` consumes Lexical.
2. Replace imports from
   `@/components/block-content-editor/lexical-surface` and relative
   `lexical-surface` paths with `@game-guild/lexical-surface`.
3. Keep `BlockArrayEditor`, quiz, code-studio, markdown-block, and HTML-block
  imports unchanged. They remain outside the Lexical package.
4. Remove callers that pass `blockEmbed` or `blockInsertMenu` feature flags;
  those flags no longer exist in the surface API.
5. Run focused typecheck/lint for web and the new package.

Acceptance criteria:

- app consumers render the same Lexical editor using the package export;
- no consumer imports from the app `lexical-surface` folder;
- no quiz/grading/code-studio/markdown/HTML block code changes are required;
- the editor has no `+Block` or other generic block insertion control.

## Phase 6: Remove App-Owned Lexical Surface

1. Delete `apps/web/src/components/block-content-editor/lexical-surface`.
2. Delete app `lib/lexical` files once no consumer imports them.
3. Delete copied app-only nodes/plugins only when no non-Lexical consumer needs
   them.
4. Update docs and READMEs:
   - `packages/features/lexical-surface/README.md`;
   - `packages/features/block-content-editor` docs if needed;
   - any app imports examples.

Acceptance criteria:

- `rg "block-content-editor/lexical-surface"` returns no app imports;
- `rg "../lib/lexical|../../lib/lexical"` returns no active consumer imports;
- deleted app files are not needed by block-array content editing.
- quiz and code-studio remain outside the Lexical package.
- markdown and HTML blocks remain outside the Lexical package.

## Phase 7: Stabilize Public API

1. Keep exported API small:
   - `LexicalSurface`;
   - props/types;
   - theme;
   - explicit node/plugin exports needed by consumers.
2. Keep the essay-compatible state boundary public: serialized editor state in,
  serialized state plus plain text out. This lets `@game-guild/quiz/react`
  implement essay editing through the package without importing Lexical
  internals.
3. Add a README explaining ownership, allowed imports, and integration points.
4. Add package-level tests or type fixtures for:
   - importing `LexicalSurface`;
   - initial editor state helpers;
   - shared node registry export;
   - read-only feature disabling.
  - serialized-state and plain-text change callback behavior.
5. Add comments to internal adapter boundaries where useful.

Acceptance criteria:

- new code can understand where Lexical belongs without reading
  `block-content-editor` internals;
- `@game-guild/lexical-surface` can typecheck independently;
- the package API does not expose block-array editor concepts.
- the package API has no generic block insertion or embed capability.

## Validation Commands

Run these during the extraction:

```bash
pnpm --filter @game-guild/lexical-surface typecheck
pnpm --filter @game-guild/block-content-editor typecheck
pnpm --filter @game-guild/web exec eslint src/components/block-content-editor
pnpm --filter @game-guild/web tsc --noEmit --pretty false
```

If the full web typecheck still fails on unrelated existing issues, capture the
focused package checks and the first unrelated web errors.

## Risks

- UI primitives currently imported from app aliases may not exist in a package
  friendly form.
- Media upload and asset resolution may need adapter props to avoid pulling
  project storage into Lexical.
- Removing generic block embed/insertion may reveal callers that used the
  Lexical toolbar as a second block picker. Those callers must move to
  `BlockArrayEditor`, not to a new Lexical adapter.
- Mermaid, Vega-Lite, Excalidraw, KaTeX, and asset flows may add package
  dependencies that need workspace manifest updates.
- Moving shared node config too early can break every Lexical consumer at once;
  keep the copy-first approach until the new package typechecks.

## Future Package Candidates

These features remain outside the Lexical extraction and can be evaluated as
separate packages later:

- `quiz`: owns quiz authoring, learner-safe rendering, local-practice runtime,
  and grading adapter integration.
- `code-studio`: owns code editing, files, runners, terminals, layouts, and
  code-execution UX.
- project/content activity editors: own activity-specific authoring and runtime
  behavior.

Future packages should integrate with `@game-guild/lexical-surface` through its
public document editor props and serialized-state contracts. They should not
import Lexical internals directly, and `@game-guild/lexical-surface` should not
import their implementation or offer generic block embeds.

## Non-Goals

- Do not change quiz/grading behavior.
- Do not move quiz into `@game-guild/lexical-surface`.
- Do not move code-studio into `@game-guild/lexical-surface`.
- Do not move `BlockArrayEditor`.
- Do not copy or replace block insertion, block embeds, code-studio, markdown
  blocks, or HTML blocks.
- Do not change course content persistence.
- Do not introduce assessment concepts into Lexical.
- Do not redesign the editor UI during extraction.
- Do not replace `React.lazy` with `next/dynamic`.

## Final State

- `@game-guild/lexical-surface` owns Lexical.
- `@game-guild/block-content-editor` owns block content orchestration.
- `BlockArrayEditor` is the only owner of generic block insertion; Lexical has
  no `+Block` UI and does not render generic block-array embeds.
- Web app pages import editor surfaces through packages instead of app-local
  implementation folders.
- Quiz's React package may consume the public lexical-surface editor for essay
  authoring and rendering, while quiz domain and grading remain Lexical-free.
