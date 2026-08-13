# Lexical Surface Internal Architecture

Status: implemented.

## Summary

`@game-guild/lexical-surface` has already been extracted from the web
application and owns the complete Lexical rich-document experience. The next
step is to reorganize its internals so that features, editor infrastructure,
shared UI, and public contracts have explicit boundaries.

This plan is an internal refactor. It must preserve the existing public API,
serialized Lexical documents, editing behavior, preview behavior, feature flag
semantics, and host adapter contracts.

The package should remain feature-first: a document feature keeps its node,
component, plugin, commands, types, and dialogs together. A generic folder that
collects every plugin separately is not a target because it would split code
that changes together.

## Goals

- make it clear which modules are public and which are package internals;
- keep each document feature self-contained;
- separate the surface composer from plugin composition;
- share formatting controls between the top and floating toolbars;
- define toolbar and slash-menu insertions from one catalog;
- remove internal imports through the package's own public exports;
- move cross-feature UI and Lexical helpers out of feature-specific folders;
- reduce large files into modules with focused responsibilities;
- make adding a document feature predictable and testable;
- remove extraction leftovers, stale exports, and unused dependencies.

## Non-Goals

- changing the serialized shape or type identifier of any Lexical node;
- changing the `LexicalSurface` public component contract;
- changing save, load, preview, or read-only behavior;
- replacing Lexical, the UI package, or the current visual design;
- adding block-array, quiz, grading, code-studio, markdown, or HTML behavior;
- making plugins dynamically installable by consumers;
- moving each document feature into its own package;
- rewriting working feature implementations while moving their files.

## Current State

The current package is already grouped reasonably well by document feature,
but three different categories live at the root of `src`:

- document features such as `media`, `table`, `equation`, and `admonition`;
- editor UI and behavior such as `toolbar`, `floating`, `picker`, and
  `draggable`;
- infrastructure such as `lexical`, `shared`, `lib`, `features.ts`, and
  `adapters.tsx`.

The main structural pressure points are:

1. `lexical-surface.tsx` imports and conditionally mounts every plugin.
2. `toolbar/toolbar-plugin.tsx` owns state synchronization, formatting
   controls, insertion UI, dialogs, and the toolbar layout in one large file.
3. The top Insert menu and slash picker duplicate labels, icons, commands,
   dialogs, feature checks, and ordering.
4. The floating toolbar imports controls from the top toolbar implementation.
5. Shared controls such as `ColorPicker` live under `toolbar` even though
   several document features use them.
6. Internal files import `@game-guild/lexical-surface/*`, crossing the public
   package boundary to access code in the same package.
7. `package.json` exposes broad wildcard subpaths and contains stale entries.
8. The package has little automated coverage relative to its behavioral
   surface.

## Architectural Principles

### Feature-first ownership

A document feature owns all code that is specific to that feature:

```text
features/media/
  index.ts
  media-node.tsx
  media-component.tsx
  media-plugin.tsx
  media-data.ts
  url-detection.ts
```

Code should leave the feature folder only when it has at least two independent
consumers and no feature-specific meaning.

### Public imports outside, relative imports inside

Consumers outside the package use `@game-guild/lexical-surface`. Files inside
the package use relative imports or narrowly defined internal aliases if the
workspace later establishes a standard for them.

Internal code must not import the package through its own public export map.

### Schema support is not feature availability

All supported node classes must remain registered so existing documents can be
deserialized and rendered. Feature flags control editing capabilities and UI,
not whether persisted node types are understood.

### One source of truth per catalog

Node registration, insertion definitions, default feature values, and read-only
feature policy each need one authoritative declaration. Different interfaces
may render the same data differently but must not duplicate it.

### Behavior-preserving migration

Move and extract code before redesigning it. Each phase must typecheck and pass
tests independently. Public exports are changed only after all consumers have
been inventoried.

## Target Structure

```text
packages/features/lexical-surface/
  src/
    index.ts
    surface/
      lexical-surface.tsx
      editor-body.tsx
      editor-plugins.tsx
      surface-config.ts
    schema/
      nodes.ts
      theme.ts
      initial-editor-state.ts
    capabilities/
      feature-flags.ts
      insertion-catalog.tsx
      insertion-types.ts
    editor-ui/
      formatting/
        block-format-control.tsx
        font-family-control.tsx
        font-size-control.tsx
        color-controls.tsx
        alignment-control.tsx
        additional-format-control.tsx
        format-commands.ts
        format-state.ts
        index.ts
      top-toolbar/
        top-toolbar-plugin.tsx
        toolbar-context.tsx
        insert-menu.tsx
        page-settings-menu.tsx
        index.ts
      floating-toolbar/
        text-format-toolbar-plugin.tsx
        link-editor-plugin.tsx
        index.ts
      picker/
      code-actions/
      context-menu/
      draggable/
      emoji/
      shortcuts/
    features/
      admonition/
      button/
      collapsible/
      divider/
      embeds/
      equation/
      excalidraw/
      layout/
      media/
      mermaid/
      page/
      sticky/
      table/
      vega-lite/
    integrations/
      adapters.tsx
    shared/
      lexical/
        get-selected-node.ts
        node-delete-protection.ts
      positioning/
        floating-position.ts
      ui/
        color-picker.tsx
        dropdown.tsx
        dropdown-color-picker.tsx
        dialogs/
      client-only-lazy.tsx
    icons/
      index.ts
```

The final names may be adjusted when a move reveals a better local boundary,
but the ownership categories must remain explicit.

## Phase 1: Stabilize the Boundary

This phase changes imports and package metadata without moving the main feature
folders.

### Tasks

1. Inventory every external import of `@game-guild/lexical-surface` and its
   subpaths.
2. Replace all imports of `@game-guild/lexical-surface/*` inside the package
   with relative internal imports.
3. Classify current exports as:
    - supported root API;
    - intentionally supported subpath API;
    - package-internal implementation.
4. Remove the broad `./*` export after confirming no external consumer needs
   it.
5. Remove stale export entries, including paths that do not exist.
6. Replace `export *` feature barrels with explicit exports where the feature
   is part of a supported subpath API.
7. Update stale comments that still describe the package as part of
   `block-content-editor` or refer to extraction waves.
8. Audit package dependencies against actual source imports.
9. Remove dependencies left behind by host adapters or move test-only
   dependencies to `devDependencies` when appropriate.
10. Document the supported root API and any retained public subpaths in the
    package README.

### Acceptance Criteria

- no source file inside the package imports `@game-guild/lexical-surface/*`;
- no source file imports from `apps/web` or another application directory;
- `package.json` contains no wildcard export exposing arbitrary internals;
- every package export resolves to an existing file;
- existing external consumers compile without changing behavior;
- package typecheck and current tests pass.

## Phase 2: Extract Shared Infrastructure

This phase moves utilities that currently imply ownership by one UI surface but
are used by several features.

### Tasks

1. Move `toolbar/color-picker.tsx` to `shared/ui/color-picker.tsx`.
2. Move generic dropdown primitives to `shared/ui` if they are consumed by
   both top and floating toolbars.
3. Move generic confirmation dialogs to `shared/ui/dialogs`.
4. Move `get-selected-node.ts` and node deletion protection hooks to
   `shared/lexical`.
5. Move floating position helpers to `shared/positioning`.
6. Move `client-only-lazy.tsx` directly under `shared`.
7. Keep feature-specific dialogs inside their owning feature.
8. Add narrow `index.ts` files only where they simplify imports without
   exposing unrelated internals.

### Acceptance Criteria

- no document feature imports a generic control from `toolbar`;
- shared modules contain no imports from a concrete document feature;
- feature-specific UI remains inside its feature folder;
- no new circular dependency is introduced;
- formatting, color, deletion, and floating behavior remain unchanged.

## Phase 3: Separate Formatting From Toolbar Layout

The top and floating toolbars represent different interfaces over the same
formatting operations and state. They should share controls and behavior, not
one toolbar's implementation file.

### Tasks

1. Extract formatting commands from `toolbar-plugin.tsx` into
   `editor-ui/formatting/format-commands.ts`.
2. Extract selection-derived formatting state into
   `editor-ui/formatting/format-state.ts`.
3. Extract these reusable controls:
    - block format;
    - font family;
    - font size;
    - text and background color;
    - alignment and indentation;
    - additional text formats.
4. Keep top-toolbar-specific layout, history controls, page settings, and
   insertion UI under `editor-ui/top-toolbar`.
5. Keep floating positioning and bubble-specific layout under
   `editor-ui/floating-toolbar`.
6. Make both toolbars consume the shared formatting layer.
7. Preserve floating-toolbar selection handling for portal-based popovers.
8. Preserve the active block, font, size, color, alignment, and inline format
   indicators in both toolbars.

### Acceptance Criteria

- the floating toolbar does not import `top-toolbar-plugin.tsx`;
- formatting commands and active-state calculation are not duplicated;
- both toolbars show the same effective formatting for the same selection;
- all dropdowns preserve the editor selection when used from the floating
  toolbar;
- top toolbar behavior and layout remain unchanged;
- focused interaction tests cover the shared formatting state.

## Phase 4: Create a Shared Insertion Catalog

The Insert dropdown and slash picker should render a shared declaration of
available document insertions.

### Catalog Contract

The catalog should carry only insertion-domain information. It must not become
a registry for every plugin behavior.

```ts
type InsertionDefinition = {
    id: string;
    feature: keyof LexicalSurfaceFeatures;
    label: string;
    keywords: readonly string[];
    icon: React.ComponentType<{ className?: string }>;
    execute?: (editor: LexicalEditor) => void;
    dialog?: React.ComponentType<InsertionDialogProps>;
    surfaces: readonly ("toolbar" | "picker")[];
};
```

### Tasks

1. Define the insertion types separately from their React catalog.
2. Move feature-backed insertion labels, icons, keywords, dialogs, command
   dispatch, and ordering into `capabilities/insertion-catalog.tsx`.
3. Keep core text options such as paragraph, heading, quote, and code in a
   dedicated formatting catalog or in the picker if they are picker-only.
4. Make the top Insert menu filter the catalog by `toolbar` support and resolved
   feature flags.
5. Make the slash picker filter the same catalog by `picker` support and
   resolved feature flags.
6. Preserve the rule that disabling only one insertion surface does not disable
   feature plugins needed by the other surface.
7. Preserve the rule that disabling both `insertMenu` and `picker` disables
   insertion-only feature plugins.
8. Add a catalog consistency test ensuring every catalog feature exists in the
   feature contract.

### Acceptance Criteria

- feature-backed insertions are declared once;
- toolbar and slash picker expose the same enabled feature set unless a catalog
  entry explicitly limits its supported surfaces;
- insertion ordering is deterministic;
- dialogs open from both surfaces with the correct active editor;
- feature-flag tests cover toolbar-only, picker-only, both-enabled, and
  both-disabled configurations.

## Phase 5: Separate Surface Composition

This phase reduces `LexicalSurface` to its public responsibilities and moves
plugin mounting into focused internal components.

### Tasks

1. Move `LexicalSurfaceProps` and the public component to
   `surface/lexical-surface.tsx`.
2. Move editable area rendering, page layout wrapper, placeholder, and change
   callbacks to `surface/editor-body.tsx`.
3. Move conditional plugin mounting to `surface/editor-plugins.tsx`.
4. Move composer configuration construction to `surface/surface-config.ts`.
5. Move shared node registration, theme, and initial-state helpers to
   `schema/`.
6. Rename `SHARED_LEXICAL_NODES` to a package-owned name such as
   `LEXICAL_SURFACE_NODES` while preserving any public compatibility alias if
   needed.
7. Make always-mounted core plugins explicit instead of leaving unexplained
   exceptions among feature-gated plugins.
8. Keep node registration independent from editing feature flags.

### Acceptance Criteria

- `LexicalSurface` primarily wires providers and composer configuration;
- `EditorBody` owns the editable layout but not the full plugin catalog;
- `EditorPlugins` owns plugin mounting but not public props or package exports;
- edit, preview, and read-only compositions deserialize the same node schema;
- `onChange`, `onContentChange`, `mountKey`, adapters, toolbar wrapper, page
  settings, and scroll behavior remain compatible.

## Phase 6: Move to the Target Directory Structure

Only after dependency directions are clean should feature folders be moved
under `features/` and editor behavior under `editor-ui/`.

### Tasks

1. Move one feature folder at a time, updating internal relative imports.
2. Preserve public exports through root or compatibility subpath barrels during
   the move.
3. Move editor behavior folders into `editor-ui` after they no longer own
   shared formatting or shared UI.
4. Move adapters to `integrations/adapters.tsx`.
5. Move feature flags and insertion definitions to `capabilities/`.
6. Move node registry, theme, and serialization helpers to `schema/`.
7. Standardize feature file names where this does not rename serialized node
   types or public symbols.
8. Remove obsolete directories only after `rg` confirms no references remain.
9. Update README ownership and contribution guidance.

### Feature Folder Convention

Use these names when applicable:

```text
feature-name/
  index.ts
  feature-name-node.tsx
  feature-name-component.tsx
  feature-name-plugin.tsx
  feature-name-dialog.tsx
  commands.ts
  types.ts
```

Do not create empty convention files. Commands and types stay with the primary
module until they have multiple consumers or materially improve readability.

### Acceptance Criteria

- the root of `src` contains only the package entry point and responsibility
  directories;
- every document feature is under `features/`;
- editor interfaces are under `editor-ui/`;
- schema, capabilities, integrations, and shared infrastructure have no
  ambiguous ownership;
- public consumer imports remain stable;
- no serialized node name or JSON shape changes.

## Testing Strategy

### Existing Gates

Run after every phase:

```bash
pnpm --filter @game-guild/lexical-surface typecheck
pnpm --filter @game-guild/lexical-surface test
```

Also run focused web consumer tests for lesson editing, content item editing,
preview rendering, and essay integration whenever public exports or composition
change.

### New Package Tests

Add focused coverage for:

1. feature resolution in editable and read-only modes;
2. independent `insertMenu` and `picker` behavior;
3. insertion catalog consistency and filtering;
4. shared formatting state for paragraph, headings, lists, fonts, sizes,
   colors, alignment, and inline formats;
5. top and floating toolbar state parity;
6. serialized state round-trip for every custom node;
7. read-only rendering of all supported custom nodes;
8. missing-adapter fallback for host-owned media integration;
9. package boundary rules.

### Architectural Checks

Add an automated check, test, or lint rule that rejects:

- imports from `apps/*` inside the package;
- imports of `@game-guild/lexical-surface/*` from inside its own `src`;
- unapproved public subpaths;
- insertion catalog feature keys absent from `LexicalSurfaceFeatures`.

## Migration Safety Rules

- do not combine file moves with behavior redesign in the same change;
- do not rename node `getType()` values or serialized fields;
- do not unregister nodes based on editing flags;
- do not remove a package export until repository-wide search confirms it is
  unused or a compatibility export is in place;
- do not change toolbar or picker ordering accidentally during catalog
  extraction;
- do not eager-load heavy client-only implementations that are currently lazy;
- preserve portal and selection behavior in floating controls;
- preserve user changes already present in files touched by the migration.

## Recommended Change Sequence

Implement the plan as reviewable changes in this order:

1. internal import cleanup and explicit package exports;
2. stale comments, stale exports, and dependency cleanup;
3. shared UI and Lexical helper extraction;
4. shared formatting layer and toolbar split;
5. shared insertion catalog;
6. surface composition split;
7. schema and capability directory moves;
8. document feature directory moves;
9. editor UI directory moves;
10. documentation and final architecture enforcement.

Each change should leave the package usable. Avoid one commit that moves every
file because it would make behavior regressions and ownership mistakes harder
to review.

## Definition of Done

- package root exports are explicit and documented;
- package internals do not consume their own public subpaths;
- `LexicalSurface` no longer contains the complete plugin-mounting matrix;
- top and floating toolbars share formatting controls and state logic;
- top Insert and slash picker use one insertion catalog;
- shared UI is not owned by a concrete toolbar or document feature;
- all document features follow the feature-first folder convention;
- schema support remains independent from editing capability flags;
- current editor, save/load, preview, and read-only flows remain correct;
- package and focused consumer tests pass;
- architecture checks prevent the extracted boundaries from regressing.

## Post-Implementation Ownership Decision

Mermaid and Vega-Lite are complete document features owned by this package.
Their editors, viewers, templates, validation, and runtime libraries live under
their respective feature folders. The corresponding adapter slots remain only
as optional host overrides; they are not required for either feature to work.

Host-specific media upload and asset URL resolution remain adapters. The
package manifest directly owns the Mermaid, Vega, CSV parsing, and Monaco
dependencies used by its built-in implementations, even when the web
application independently uses the same libraries elsewhere.
