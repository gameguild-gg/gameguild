# GameGuild Block Content Editor — Architecture Reference

> Comprehensive technical reference for the composable Block Content Editor.

---

## Table of Contents

1. [Overview](#overview)
2. [High-Level Architecture](#high-level-architecture)
3. [Page Composition](#page-composition)
4. [Provider Layer](#provider-layer)
5. [Configuration System](#configuration-system)
6. [The Block Engine](#the-block-engine)
7. [Block Types](#block-types)
8. [Content Modes & Block Restrictions](#content-modes--block-restrictions)
9. [Storage Layer](#storage-layer)
10. [Block Storage Format](#block-storage-format)
11. [Nodes (Type-Only Modules)](#nodes-type-only-modules)
12. [Plugins (Rich-Text Scope)](#plugins-rich-text-scope)
13. [Preview Components](#preview-components)
14. [Hooks](#hooks)
15. [UI Components (Extras)](#ui-components-extras)
16. [Pages](#pages)
17. [Static Viewer](#static-viewer)
18. [Technologies & Libraries](#technologies--libraries)
19. [File Map](#file-map)

---

## Overview

The GameGuild Block Content Editor is a composable, single-engine content editor built with Next.js (App Router) and React 19. Content is modeled as an ordered list of standalone **blocks** — there is no global rich-text tree between blocks. Each block is one of 21 typed units (quiz, code studio, image, mermaid, rich-text, …) rendered top-to-bottom.

Pages are assembled by combining a Provider, a Layout, a Toolbar, a Field, and Dialogs, driven by two configuration objects: `FieldConfig` and `ToolbarConfig`. Lexical is used only **inside** two blocks (`rich-text` for inline formatting and `quiz` for essay-question answers); the editor itself has no Lexical engine.

### Design Principles

- **Composability** — Every page is a composition of Provider + Field + Toolbar + Dialogs.
- **Configuration over code** — `FieldConfig` controls *what* is available; `ToolbarConfig` controls *what is visible*.
- **One engine, one schema** — Single Block Array model, single persistence format (`BlockStorage`).
- **Mode-based restrictions** — Block types are allowed/blocked declaratively per `ProjectMode`, enforced at insert-time and in the picker.
- **Storage abstraction** — IndexedDB + Google Drive + GameGuild Cloud behind a single adapter interface (`EnhancedStorageAdapter`).
- **Module boundary** — Editor-only hooks, storage, sync, services, viewers, and utilities live under [components/block-content-editor](../).

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────┐
│                      Page                           │
│  ┌───────────────────────────────────────────────┐  │
│  │           EditorProvider / ViewerProvider     │  │
│  │  ┌─────────────────────────────────────────┐  │  │
│  │  │           Layout (Studio/Viewer)        │  │  │
│  │  │  ┌───────────────────────────────────┐  │  │  │
│  │  │  │         Toolbar                   │  │  │  │
│  │  │  ├───────────────────────────────────┤  │  │  │
│  │  │  │         Field                     │  │  │  │
│  │  │  │  ┌─────────────────────────────┐  │  │  │  │
│  │  │  │  │      Block Array Engine     │  │  │  │  │
│  │  │  │  └─────────────────────────────┘  │  │  │  │
│  │  │  └───────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────┘  │  │
│  │               Dialogs                         │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

**Top-level data flow:**

```
User edit → BlockArrayEditor → setBlocks (in useProjectStorage)
        → debounced auto-save
        → serializeProject(blocks) → EnhancedStorageAdapter.save()
        → IndexedDB (projects + project_metadata + tag_data)
        → Git auto-commit (isomorphic-git on lightning-fs)
        → SyncManager enqueue → (Google Drive | GameGuild Cloud)
```

For a step-by-step walkthrough of every layer, see [DATA-FLOW.md](DATA-FLOW.md).

---

## Page Composition

Every editor page follows the same pattern:

```tsx
// Minimal page — quiz-only editor
const fieldConfig: Partial<FieldConfig> = {
  allowedBlockTypes: [],
  allowedModes: ["quiz-page"],
  defaultMode: "quiz-page",
}

export default function QuizEditorPage() {
  return (
    <EditorProvider fieldConfig={fieldConfig} toolbarConfig={toolbarConfig}>
      <StudioLayout>
        <EditorToolbar />
        <EditorField />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
```

**Component roles:**

| Component         | Role                                                              |
|-------------------|-------------------------------------------------------------------|
| `EditorProvider`  | Wraps hooks (storage, history, preview), manages UI state         |
| `StudioLayout`    | Responsive container, top header, content area                    |
| `EditorToolbar`   | Header + action bar (save, open, create, preview, etc.)           |
| `EditorField`     | Renders `BlockArrayEditor` (the only field implementation)        |
| `EditorDialogs`   | All modal dialogs (create, save-as, history, preview, etc.)       |

For the viewer (read-only):

```tsx
<ViewerProvider>
  <ViewerLayout>
    <ViewerToolbar />
    <ViewerField />
  </ViewerLayout>
  <ViewerDialogs />
</ViewerProvider>
```

---

## Provider Layer

### EditorProvider

**File:** [engines/editor-provider.tsx](../engines/editor-provider.tsx)

Initializes three hooks and merges configs:

```
EditorProvider
  ├── useProjectStorage(initialDefaults)  → project state, CRUD, auto-save
  ├── useProjectHistory(project)          → commit/snapshot navigation
  └── useProjectPreview(project)          → preview dialog state
```

Exposes via React Context (`useEditor()`):

- `project` — all storage state and operations (`UseProjectStorageReturn`)
- `history` — commit navigation, `isViewingHistory`
- `preview` — preview dialog control
- `fieldConfig` — merged `FieldConfig`
- `toolbarConfig` — merged `ToolbarConfig`
- `ui` — all UI state (dialog open/close, save handlers, navigation guards)

Also handles:

- **Ctrl+S** keyboard shortcut → `handleSave()`
- **Navigation guards** — prompts to save before leaving with unsaved changes
- **Exit confirmation** — dialog when navigating away with content

### ViewerProvider

**File:** [engines/viewer-provider.tsx](../engines/viewer-provider.tsx)

Wraps `useViewerStorage()` for read-only content display. Exposes via `useViewer()`.

---

## Configuration System

### `FieldConfig`

Controls **what** the editor supports.

```typescript
interface FieldConfig {
  /** Which block types to show in the picker (undefined = all 21) */
  allowedBlockTypes?: BlockCellType[]
  /** Which content modes are available in create dialog (undefined = all) */
  allowedModes?: ProjectMode[]
  /** Default project mode */
  defaultMode?: ProjectMode
}
```

Behavior:

- `allowedBlockTypes` — filters the Block Types tab in the block picker. Empty array or single entry hides the tab.
- `allowedModes` — filters the Content Mode dropdown in the Create Project dialog. Single entry hides it. First entry becomes the effective initial mode.
- `defaultMode` — UI hint for dialog default; overridden by `allowedModes[0]` when set.

### `ToolbarConfig`

Controls **what is visible** in the toolbar. All booleans default to `true`.

```typescript
interface ToolbarConfig {
  showSave?: boolean
  showSaveAs?: boolean
  showOpen?: boolean
  showCreate?: boolean
  showPreview?: boolean
  showHistory?: boolean
  showAutoSave?: boolean
  showSizeIndicator?: boolean
  showSyncStatus?: boolean
  showProjectTitle?: boolean
  showModeIndicator?: boolean
  showStorageInfo?: boolean
  showNavHome?: boolean
  showNavViewer?: boolean
  showNavStudio?: boolean
}
```

### Merge behavior

Both configs have merge helpers. Partial configs are merged with defaults:

```typescript
const fieldConfig = mergeFieldConfig(fieldPartial)       // fills missing with DEFAULT_FIELD_CONFIG
const toolbarConfig = mergeToolbarConfig(toolbarPartial) // fills missing with DEFAULT_TOOLBAR_CONFIG
```

See [engines/editor-config.ts](../engines/editor-config.ts).

---

## The Block Engine

The editor has a single engine: the **Block Array Engine**. Every project is an ordered list of standalone blocks; there is no inline rich text between blocks.

**Files:** [engines/blocks/](../engines/blocks/)

| File                                                                                       | Role                                                                                                            |
|--------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------|
| [block-array-editor.tsx](../engines/blocks/block-array-editor.tsx)                         | Main editor: ordered list with insert seams between blocks and drag-to-reorder                                  |
| [block-array-viewer.tsx](../engines/blocks/block-array-viewer.tsx)                         | Read-only renderer; maps each `Block` to its preview component                                                  |
| [block-component-registry.ts](../engines/blocks/block-component-registry.ts)               | Registry mapping the 21 block types to icon, label, description, and `createEmpty()`                            |
| [block-type-picker.tsx](../engines/blocks/block-type-picker.tsx)                           | Modal picker with two tabs: Block Types + Templates (quiz presets)                                              |
| [block-editor-modal.tsx](../engines/blocks/block-editor-modal.tsx)                         | Per-block edit dialog dispatching to type-specific editors                                                      |
| [block-drag-drop.tsx](../engines/blocks/block-drag-drop.tsx)                               | Drag-and-drop reordering helpers                                                                                |

### Block picker behavior

The picker has two tabs: **Block Types** and **Templates** (quiz presets).

- When `allowedBlockTypes` is empty (`[]`) or undefined **and** `allowedModes` includes `"quiz-page"` → Block Types tab is **hidden**, opens on Templates.
- When `allowedBlockTypes` has exactly 1 entry → Block Types tab is **hidden** (pointless with a single item).
- When `allowedBlockTypes` has 2+ entries **and** `allowedModes` includes `"quiz-page"` → both tabs visible, defaults to Templates.
- Otherwise → both tabs visible, defaults to Block Types.

---

## Block Types

21 block types are registered in [BLOCK_REGISTRY](../engines/blocks/block-component-registry.ts). The registry keys are the same strings used as `BlockCellType` discriminants.

| Type           | Label        | Notes                                                          |
|----------------|--------------|----------------------------------------------------------------|
| `quiz`         | Quiz         | Interactive question (uses Lexical internally for essays)      |
| `code-studio`  | Code Studio  | Full file-tree code IDE with multi-language runners            |
| `button`       | Button       | Action button (URL, copy, email, download)                     |
| `image`        | Image        | Image media                                                    |
| `video`        | Video        | Video media (direct/YouTube/Vimeo/DailyMotion)                 |
| `audio`        | Audio        | Audio media (direct/YouTube/Spotify/SoundCloud)                |
| `gallery`      | Gallery      | Image gallery (1–4 columns, span layouts)                      |
| `youtube`      | YouTube      | YouTube embed                                                  |
| `spotify`      | Spotify      | Spotify track/album/playlist/artist embed                      |
| `mermaid`      | Mermaid      | Mermaid diagram                                                |
| `vega-lite`    | Vega-Lite    | Vega-Lite visualization                                        |
| `table`        | Table        | Spreadsheet-style table                                        |
| `presentation` | Presentation | Presentation player (ODP/PPTX)                                 |
| `rich-text`    | Rich Text    | Inline rich-text block (uses Lexical internally)               |
| `markdown`     | Markdown     | Markdown content                                               |
| `html`         | HTML         | Raw HTML (sanitized via DOMPurify)                             |
| `header`       | Header       | Heading with styling                                           |
| `divider`      | Divider      | Section divider                                                |
| `admonition`   | Admonition   | Note/warning/tip box                                           |
| `source`       | Source       | Citation/source reference list                                 |
| `project`      | Project      | Embedded sub-project (placeholder; not currently loaded)       |

The `BlockCellType` union and `BlockDataMap` (type-level map from `BlockCellType` to its data shape) are the single source of truth for block typing. See [lib/storage/editor/block-structure.ts](../lib/storage/editor/block-structure.ts).

---

## Content Modes & Block Restrictions

### Modes

Three content modes constrain which block types are allowed:

| Mode         | Description                                       |
|--------------|---------------------------------------------------|
| `free-page`  | No restrictions. All block types allowed.         |
| `code-page`  | Blocks `code-studio` from being inserted.         |
| `quiz-page`  | Blocks `quiz` from being inserted as a free pick. |

Restrictions live in [lib/storage/editor/project-modes.ts](../lib/storage/editor/project-modes.ts) as `NODE_RESTRICTIONS`:

```typescript
{
  "free-page": { blocks: { b1: [null, null] } },
  "code-page": { blocks: { b1: ["code-studio", null] } },
  "quiz-page": { blocks: { b1: ["quiz", null] } },
}
```

Each tuple is `[blocked, allowed]`. `'*'` means all; `null` means none; a string or string array names specific types.

### Enforcement

- **Picker filter** — `BlockTypePicker` honors `allowedBlockTypes` from `FieldConfig`.
- **Mode helper** — `isNodeAllowed()` in `project-modes.ts` is consulted by code paths that need to validate inserts programmatically.

`allowedModes` in `FieldConfig` serves double duty:

1. Restricts the Content Mode dropdown in the Create Project dialog (single entry hides it).
2. Sets the effective initial mode — `allowedModes[0]` overrides `defaultMode`.

---

## Storage Layer

### Three-Tier Persistence

```
┌──────────────────────────┐
│     IndexedDB (local)    │  ← Primary. Always available.
├──────────────────────────┤
│     Google Drive         │  ← Lazy-load. Hash-based sync.
├──────────────────────────┤
│     GameGuild Cloud      │  ← Queue-based sync via SyncManager.
└──────────────────────────┘
```

### `EnhancedStorageAdapter`

**File:** [lib/storage/editor/enhanced-storage-adapter.ts](../lib/storage/editor/enhanced-storage-adapter.ts)

IndexedDB schema (`DB_NAME = "GGEditorDB"`, `DB_VERSION = 6`):

- `projects` — Full `ProjectData` objects (keyPath: `id`)
- `project_metadata` — Sync-optimized metadata with hash index (`ProjectMetadataRecord`)
- `tag_data` — Tag → ProjectIds relationships
- `tags` — Legacy store (kept for backward migration only)

When the DB is opened with an `oldVersion` between 1 and 5, every store is dropped and recreated — pre-v6 records use incompatible shapes (flat metadata fields).

Key operations:

- `save(id, name, data, tags, storageType, preferences)` — Persists, auto-commits, enqueues sync.
- `load(id)` — IndexedDB first; remote fallback.
- `list()` — Merges local + remote metadata.
- `delete(id)` — Removes from all stores.
- `searchProjects(term, tags, filterMode, storageTypeFilter)` — Full-text + tag search.

### `ProjectData`

Defined once in [lib/storage/editor/project-data.ts](../lib/storage/editor/project-data.ts):

```typescript
interface ProjectMetadata {
  size: number
  hash: string
  createdAt: string
  updatedAt: string
}

interface ProjectData {
  id: string
  name: string
  /** Serialized BlockStorage (JSON string). */
  data: string
  tags: string[]
  metadata: ProjectMetadata
  syncStatus?: SyncStatus
  storageType: StorageType // "local" | "gameguild-cloud" | "google-drive"
  isLocallyAvailable?: boolean
  preferences?: ProjectPreferences
}
```

`ProjectMetadataRecord` mirrors `ProjectData` without the heavy `data` payload and lives in the sync-optimization store.

### Git history

`EnhancedStorageAdapter` auto-commits on every save through [`getHistoryManager()`](../lib/storage/git/git-history-manager.ts). Operations exposed:

- `listHistory(projectId)` — Full commit list.
- `loadCommit(projectId, sha)` — View old version (read-only; sets `readOnlyRef`).
- `listSnapshots(projectId)` — Named snapshot list.
- `createSnapshot(projectId, name)` — Named tag.
- `loadSnapshot(projectId, tag)` — Restore tagged version.

Git runs entirely in the browser on top of `isomorphic-git` + `@isomorphic-git/lightning-fs`.

### Sync

`SyncManager` ([lib/sync/editor/sync-manager.ts](../lib/sync/editor/sync-manager.ts)) drives a queue stored in IndexedDB (`sync-queue.ts`). The Google Drive bridge ([google-drive-sync.ts](../lib/sync/editor/google-drive-sync.ts)) compares local and remote `hash`es before uploading. Cloud-side sync uses the GameGuild API client at [lib/api/editor/api-client.ts](../lib/api/editor/api-client.ts).

---

## Block Storage Format

The Block Array is serialized directly to a flat JSON schema. There is no intermediate Cellular format.

**File:** [lib/storage/editor/block-storage.ts](../lib/storage/editor/block-storage.ts)

```typescript
interface BlockStorage {
  order: string[]                                // Block IDs in display order
  blocks: Record<string, BlockStorageEntry>      // Block ID → { type, data }
}

interface BlockStorageEntry<T extends BlockCellType = BlockCellType> {
  type: T
  data: BlockDataMap[T]
}
```

Round-trip helpers:

- `serializeProject(blocks: BlockArray): string` — runtime → JSON string for storage.
- `deserializeProject(data: string): BlockArray` — JSON string → runtime.
- `blocksToStorage` / `storageToBlocks` — lower-level structural conversion.
- `blockToPreviewNode(block)` — wraps a `Block` in the `{ type, data | entry, version }` shape consumed by preview components.
- `EMPTY_PROJECT_DATA` — canonical empty payload (`{"order":[],"blocks":{}}`).

`BlockArray = Block[]`, where `Block<T extends BlockCellType>` is a discriminated union over the 21 `BlockCellType`s and their corresponding data shapes via `BlockDataMap`.

---

## Nodes (Type-Only Modules)

Each per-block file under `nodes/` exposes only:

- `XxxData` — the runtime data shape for the block.
- `SerializedXxxNode` — the storage-side type, defined as `SerializedBlockNode<"type", XxxData>`.

These modules carry **no Lexical imports**. They are consumed by:

- [lib/storage/editor/block-structure.ts](../lib/storage/editor/block-structure.ts) (assembles `BlockDataMap`).
- The preview components in `plugins/preview-components/` (type-only reads).
- Block editor UIs in `extras/*`.

### Exceptions

| File                                                              | Purpose                                                                                       |
|-------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| [nodes/base/serialized-block-node.ts](../nodes/base/serialized-block-node.ts) | `SerializedBlockNode<TType, TData>` base type (no Lexical)                                    |
| [nodes/base/media-node-base.tsx](../nodes/base/media-node-base.tsx)           | `BaseMediaData` shared by `image`, `video`, `audio`, `gallery` (no Lexical)                   |
| [nodes/custom-list-node.tsx](../nodes/custom-list-node.tsx)                   | **Live Lexical node.** Used inside the rich-text block's floating toolbar for ordered/unordered/colored lists |
| [nodes/spotify-node.tsx](../nodes/spotify-node.tsx)                           | Also exports the pure helper `extractSpotifyInfo(url)`                                        |

### Files in `nodes/`

`admonition-node.tsx`, `audio-node.tsx`, `button-node.tsx`, `code-studio-node.tsx`, `custom-list-node.tsx`, `divider-node.tsx`, `gallery-node.tsx`, `header-node.tsx`, `html-node.tsx`, `image-node.tsx`, `markdown-node.tsx`, `mermaid-node.tsx`, `project-node.tsx`, `quiz-node.tsx`, `rich-text-node.tsx`, `source-node.tsx`, `spotify-node.tsx`, `table-node.tsx`, `vega-lite-node.tsx`, `video-node.tsx`, `youtube-node.tsx`, plus `base/`.

> The `presentation` block does not have a dedicated `nodes/` file; its data type lives under [extras/presentation/](../extras/presentation/) and is referenced as `unknown` in `BlockDataMap`.

---

## Plugins (Rich-Text Scope)

The plugins under [plugins/](../plugins/) run **inside the rich-text block**. They are loaded by the rich-text editor in [extras/rich-text/](../extras/rich-text/).

```
plugins/
├── floating-text-format-toolbar-plugin.tsx   # Bold, italic, link, color, lists, …
├── floating-text-components/                 # 10 sub-components used by the toolbar
└── preview-components/                       # Read-only renderers (see below)
```

- The floating text toolbar registers list operations through `custom-list-node.tsx`'s `$createCustomListNode` / `$isCustomListNode`.
- Block insertion never goes through Lexical commands — it goes through the `BlockTypePicker`.

---

## Preview Components

Read-only renderers used by the viewer field, the static viewer, and the editor preview dialog. Each takes a serialized node payload (the `{ type, data | entry, version }` shape produced by `blockToPreviewNode`) and renders DOM.

**Path:** [plugins/preview-components/](../plugins/preview-components/)

```
preview-admonition.tsx   preview-list-item.tsx    preview-source.tsx
preview-audio.tsx        preview-list.tsx         preview-spotify.tsx
preview-button.tsx       preview-markdown.tsx     preview-table.tsx
preview-code-studio.tsx  preview-mermaid.tsx      preview-text.tsx
preview-divider.tsx      preview-paragraph.tsx    preview-vega-lite.tsx
preview-gallery.tsx      preview-project.tsx      preview-video.tsx
preview-header.tsx       preview-quiz.tsx         preview-youtube.tsx
preview-heading.tsx      preview-quote.tsx
preview-html.tsx         preview-rich-text.tsx
preview-image.tsx
preview-link.tsx
```

The `paragraph`, `text`, `heading`, `quote`, `link`, `list`, `list-item` components are used **inside** `preview-rich-text.tsx` to render the Lexical EditorState JSON embedded in a `rich-text` block. They are not standalone block types.

---

## Hooks

### `useProjectStorage`

**File:** [hooks/useProjectStorage.ts](../hooks/useProjectStorage.ts)

The main hook. Manages all project state and persistence.

**Accepts:** `ProjectStorageDefaults?` — optional initial `mode`.

**Returns (`UseProjectStorageReturn`):**

- Project metadata: `projectId`, `projectName`, `projectMode`, `storageType`, `tags`, `preferences`
- Content: `blocks: BlockArray`, `setBlocks`
- Operations: `save()`, `saveAs()`, `loadProject()`, `createProject()`
- Lists: `savedProjects`, `availableTags`, `refreshProjects()`, `refreshTags()`
- Size: `projectSize`, `assetsSize`, `assets`
- Sync: `syncStats`, `autoSaveEnabled`
- Direct access: `db` (`EnhancedStorageAdapter`)

**Auto-save:** Debounced save triggered by state changes. Suppressed when `readOnlyRef.current` is `true` (history viewing).

**URL hash:** On mount, checks `window.location.hash` for project ID and loads it.

### `useProjectHistory`

**File:** [hooks/useProjectHistory.ts](../hooks/useProjectHistory.ts)

Git-based version history (commits, snapshots, return-to-head). Toggles `readOnlyRef` while viewing a past commit.

### `useProjectPreview`

**File:** [hooks/useProjectPreview.ts](../hooks/useProjectPreview.ts)

Preview dialog open/close + serialization of the current `blocks` for read-only render.

### `useViewerStorage`

**File:** [hooks/useViewerStorage.ts](../hooks/useViewerStorage.ts)

Read-only storage for the viewer page — loads from URL hash, exposes `currentProject` and `blocks`.

### Additional hooks

`useHomeStorage`, `useAssetManager`, `useCollectionManager`, `useProjectManager`, `useDarkMode` — used by the home/manager pages and shared UI surfaces.

---

## UI Components (Extras)

Located in [extras/](../extras/). Key areas:

| Directory                | Purpose                                                        |
|--------------------------|----------------------------------------------------------------|
| `editor/`                | Create-project dialog, info dialog, storage-option selector    |
| `dialogs/`               | Confirmation dialogs (delete, refresh)                         |
| `media/`                 | Unified media editor, asset image handling                     |
| `media-upload-dialog/`   | Universal media upload (file + URL + collections)              |
| `manager-page/`          | Project/asset manager UI (grid/list, filters, pagination)      |
| `preview/`               | Preview infrastructure used by the editor preview dialog       |
| `project-dialog/`        | Shared open-project dialog shell (`ProjectPickerShell`)        |
| `quiz/`                  | Quiz display, entry types, settings, examples                  |
| `code-studio/`           | Code editor/IDE with runners (JS, Python, C++, Rust, …)        |
| `presentation/`          | Presentation player, ODP/PPTX parsers                          |
| `mermaid/`               | Mermaid diagram editor                                         |
| `vega-lite/`             | Vega-Lite visualization editor                                 |
| `markdown/`              | Markdown editor and renderer                                   |
| `html/`                  | HTML editor and viewer                                         |
| `rich-text/`             | Embedded Lexical-based rich-text block                         |
| `source-code/`           | Source file viewer for the `source` block                      |
| `content-edit-menu.tsx`  | Edit/delete menu for inline block actions                      |
| `settings-menu/`         | Editor preferences UI                                          |

---

## Pages

All pages live under `app/[locale]/(block-content-editor)/block-content-editor/`.

| Route                                  | Page          | Description                                       |
|----------------------------------------|---------------|---------------------------------------------------|
| `/block-content-editor`                | Home          | Project manager (grid/list), asset manager, collections |
| `/block-content-editor/studio`         | Studio        | Full editor — all block types, all modes          |
| `/block-content-editor/viewer`         | Viewer        | Read-only content viewer                          |
| `/block-content-editor/quiz-editor`    | Quiz Editor   | Quiz-mode only, picker restricted to templates    |
| `/block-content-editor/doc-editor`     | Doc Editor    | Rich-text-only block (`["rich-text"]`), free mode |
| `/block-content-editor/block-editor`   | Block Editor  | Free mode, all block types                        |
| `/block-content-editor/full-editor`    | Full Editor   | All defaults enabled                              |
| `/block-content-editor/static-viewer`  | Static Viewer | Header + content only (no sidebar/TOC)            |
| `/block-content-editor/publish`        | Publish       | Placeholder                                       |

---

## Static Viewer

The static viewer is a composable, read-only surface for rendering one or more projects without exposing the editor chrome.

**Files:**

- [engines/static-viewer.tsx](../engines/static-viewer.tsx) — Single-project entry point keyed by `projectId`.
- [engines/static-viewer-sections.tsx](../engines/static-viewer-sections.tsx) — Section components and the hooks they consume.

It supports three project sources:

| Source                | Hook                              | Section component       | Origin                                                   |
|-----------------------|-----------------------------------|-------------------------|----------------------------------------------------------|
| IndexedDB (by id)     | `useStaticProject(id)`            | `DirectSection`         | `EnhancedStorageAdapter.load(id)`                        |
| Filesystem folder     | `useStaticProjectFromFolder(name)`| `DirectFolderSection`   | `GET /api/static-viewer/folder/[folderName]`             |
| Single content file   | `useStaticBlocksFromFile(path)`   | `DirectFileSection`     | `GET /api/static-viewer/file/[...path]`                  |

The folder and file sources read from `apps/web/src/data/test-blocks/`. Both API routes apply per-segment regex validation and a resolved-path containment check to defend against path traversal.

Other section components in the same file:

- `StaticProjectHeader` / `StaticProjectContent` — composable parts.
- `LinkSection` — buttons linking to other project IDs.
- `FeaturedSection`, `AllProjectsSection`, `ByTagSection` — list surfaces.
- `useProjectList`, `useHashNavigation` — supporting hooks.

---

## Technologies & Libraries

This section enumerates the third-party libraries the editor depends on, both at the framework layer and per block type.

### Framework & cross-cutting

| Concern                  | Libraries                                                                                                          |
|--------------------------|--------------------------------------------------------------------------------------------------------------------|
| Framework                | `next`, `react`, `react-dom`                                                                                       |
| i18n                     | `next-intl`                                                                                                        |
| Styling                  | Tailwind CSS, `tailwind-merge`, `clsx`, `class-variance-authority`, `tailwindcss-animate`                          |
| UI primitives            | Radix UI (`@radix-ui/react-*`), shadcn/ui patterns                                                                 |
| Icons                    | `lucide-react`, `react-icons`                                                                                      |
| State                    | React state, `zustand`, `use-immer` / `immer`                                                                      |
| Forms                    | `react-hook-form`, `@hookform/resolvers`, `zod`                                                                    |
| Drag and drop            | `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities`                                                         |
| Toasts                   | `sonner`                                                                                                           |
| Date utilities           | `date-fns`                                                                                                         |
| Theming                  | `next-themes`                                                                                                      |
| Lazy loading             | `next/dynamic`                                                                                                     |
| Data fetching            | `@tanstack/react-query`, `@apollo/client`                                                                          |
| Local persistence        | IndexedDB (native), `isomorphic-git`, `@isomorphic-git/lightning-fs`                                               |
| Import / export          | `jszip`                                                                                                            |
| Workers                  | `comlink`                                                                                                          |
| Sanitization             | `dompurify`                                                                                                        |
| Math rendering           | `katex`, `mathlive`                                                                                                |

### Per-block libraries

| Type           | Primary runtime libraries                                                                                                                                  |
|----------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `quiz`         | `canvas-confetti`; `lexical`, `@lexical/react`, `@lexical/rich-text` (essay answers)                                                                       |
| `code-studio`  | `monaco-editor`, `@monaco-editor/react`, `shiki`, `@shikijs/monaco`, `@xterm/xterm` (+ addons); language runners: `pyodide`, `quickjs-emscripten`, `esbuild-wasm`, `wasmoon`, `wabt`, `sql.js`, `@runno/*` |
| `button`       | `lucide-react` (icons), Radix UI                                                                                                                           |
| `image`        | Native `<img>`; asset URL resolver (`use-resolved-media`)                                                                                                  |
| `video`        | Native `<iframe>` / HTML5 `<video>`; URL parsers for YouTube/Vimeo/DailyMotion                                                                             |
| `audio`        | HTML5 `<audio>`; URL parsers for YouTube/Spotify/SoundCloud                                                                                                |
| `gallery`      | `embla-carousel-react`, `embla-carousel-autoplay`; CSS Grid layout                                                                                         |
| `youtube`      | Native iframe + URL parsing (`youtube-nocookie.com`)                                                                                                       |
| `spotify`      | Native iframe; `extractSpotifyInfo()` helper                                                                                                               |
| `mermaid`      | `mermaid` (dynamically imported, client-side SVG render)                                                                                                   |
| `vega-lite`    | `vega-lite`, `vega`, `vega-themes`; `d3-dsv` for CSV data loading                                                                                          |
| `table`        | Pure shadcn/Tailwind UI                                                                                                                                    |
| `presentation` | `reveal.js`, `@marp-team/marp-core`, `@xmldom/xmldom`, `fast-xml-parser`                                                                                   |
| `rich-text`    | `lexical`, `@lexical/react`, `@lexical/rich-text`, `@lexical/list`, `@lexical/link`, `@lexical/code`, `@lexical/markdown`, `@lexical/utils`; `katex`, `mathlive`, `rehype-katex` |
| `markdown`     | `react-markdown`, `remark-gfm`, `remark-math`, `rehype-raw`, `rehype-katex`; `reading-time-estimator`                                                       |
| `html`         | `dompurify`; iframe sandbox                                                                                                                                |
| `header`       | `lucide-react` (icons); pure CSS variants                                                                                                                  |
| `divider`      | Pure CSS                                                                                                                                                   |
| `admonition`   | `lucide-react`                                                                                                                                             |
| `source`       | Pure formatter (APA, MLA, Chicago, Harvard, IEEE)                                                                                                          |
| `project`      | `lexical` (referenced project state), `sonner`                                                                                                             |

### Storage and sync stack

| Concern             | Libraries / modules                                                                                |
|---------------------|----------------------------------------------------------------------------------------------------|
| Local storage       | IndexedDB (native), `EnhancedStorageAdapter` (`lib/storage/editor/`)                               |
| Git history         | `isomorphic-git`, `@isomorphic-git/lightning-fs` (`lib/storage/git/`)                              |
| Sync queue          | IndexedDB-backed (`lib/sync/editor/sync-queue.ts`, `sync-manager.ts`)                              |
| Google Drive        | Google Drive REST API via `services/editor/google-drive-service.ts`, bridged in `google-drive-sync.ts` |
| GameGuild Cloud     | OpenAPI-generated client (`@hey-api/openapi-ts`) consumed by `lib/api/editor/api-client.ts`        |
| Auth                | `next-auth`                                                                                        |
| Encryption          | `ethers` (Web3 auth flows, used only in cloud sync paths)                                          |

---

## File Map

### Core (`engines/`)

```
engines/
├── editor-config.ts                # FieldConfig, ToolbarConfig, defaults, merge helpers
├── editor-provider.tsx             # EditorProvider, EditorContextValue, useEditor()
├── viewer-provider.tsx             # ViewerProvider, ViewerContextValue, useViewer()
├── editor-field.tsx                # Renders BlockArrayEditor
├── viewer-field.tsx                # Renders viewer content or empty state
├── editor-toolbar.tsx              # Header + action bar with ToolbarConfig conditionals
├── viewer-toolbar.tsx              # Viewer header + action bar
├── editor-dialogs.tsx              # All editor dialogs (create, save-as, history, …)
├── viewer-dialogs.tsx              # Viewer dialogs (exit confirm)
├── project-content-renderer.tsx    # Shared block-list renderer
├── static-viewer.tsx               # Static (read-only) viewer entry point
├── static-viewer-sections.tsx      # Section components + hooks (id / folder / file sources)
└── blocks/                         # The Block Array engine
    ├── block-array-editor.tsx
    ├── block-array-viewer.tsx
    ├── block-component-registry.ts
    ├── block-drag-drop.tsx
    ├── block-editor-modal.tsx
    └── block-type-picker.tsx
```

### Hooks

```
hooks/
├── editor/                         # Editor-specific sub-hooks
├── useProjectStorage.ts            # Main storage hook (CRUD, auto-save, sync)
├── useProjectHistory.ts            # Git-based version history
├── useProjectPreview.ts            # Preview dialog management
├── useViewerStorage.ts             # Read-only viewer storage
├── useHomeStorage.ts               # Home/manager-page storage
├── useAssetManager.tsx             # Asset DB facade
├── useCollectionManager.tsx        # Collection facade
├── useProjectManager.tsx           # Project manager facade
└── useDarkMode.ts                  # Theme toggle
```

### Storage and persistence (`lib/storage/`)

```
lib/storage/
├── assets/                         # Asset DB, collections, URL resolution
│   ├── asset-manager.ts
│   ├── collection-types.ts
│   ├── index.ts
│   ├── types.ts
│   └── use-resolved-media.ts
├── editor/                         # Project/content persistence
│   ├── project-data.ts             # ProjectData, ProjectMetadata, ProjectMetadataRecord
│   ├── enhanced-storage-adapter.ts # IndexedDB + sync + Drive adapter
│   ├── block-storage.ts            # serialize/deserialize + EMPTY_PROJECT_DATA + blockToPreviewNode
│   ├── block-structure.ts          # Block, BlockArray, BlockStorage, BlockDataMap
│   ├── project-modes.ts            # ProjectMode, NodeRestrictions, isNodeAllowed()
│   ├── project-preferences.ts      # ProjectPreferences
│   ├── storage-types.ts            # StorageType, SyncStatus constants
│   └── editor-preferences.ts       # Editor preferences DB
└── git/                            # Isomorphic-git history/snapshots
    ├── git-fs.ts
    ├── git-history-manager.ts
    └── index.ts
```

### Sync, API, services, utilities

```
lib/api/editor/
└── api-client.ts                   # GameGuild Cloud API client

lib/sync/editor/
├── google-drive-sync.ts            # Google Drive sync bridge
├── hash-manager.ts                 # Project/hash helpers
├── sync-config.ts                  # Sync configuration manager
├── sync-manager.ts                 # Queue orchestration + remote checks
└── sync-queue.ts                   # IndexedDB-backed sync queue

services/editor/
└── google-drive-service.ts         # Google Drive API integration

lib/interopAdapter/
├── project-exporter.ts             # ZIP/folder export with assets
├── project-importer.ts             # ZIP / folder import with assets
└── README.md

lib/editor/
└── webp-converter.ts               # Browser-side image conversion

utils/editor/
├── google-drive-security.ts
└── google-drive-test.ts            # Manual browser-console test script
```

### Nodes (type modules)

```
nodes/
├── base/
│   ├── media-node-base.tsx         # BaseMediaData (type-only)
│   └── serialized-block-node.ts    # SerializedBlockNode<TType, TData>
├── custom-list-node.tsx            # Live Lexical node (rich-text toolbar)
├── admonition-node.tsx
├── audio-node.tsx
├── button-node.tsx
├── code-studio-node.tsx
├── divider-node.tsx
├── gallery-node.tsx
├── header-node.tsx
├── html-node.tsx
├── image-node.tsx
├── markdown-node.tsx
├── mermaid-node.tsx
├── project-node.tsx
├── quiz-node.tsx
├── rich-text-node.tsx
├── source-node.tsx
├── spotify-node.tsx                # also exports extractSpotifyInfo()
├── table-node.tsx
├── vega-lite-node.tsx
├── video-node.tsx
└── youtube-node.tsx
```

### Plugins (rich-text scope)

```
plugins/
├── floating-text-format-toolbar-plugin.tsx
├── floating-text-components/       # Bold, italic, link, color, lists, fonts, …
└── preview-components/             # Read-only renderers (used by viewer + preview)
```

### Static-viewer API routes

```
app/api/static-viewer/
├── folder/[folderName]/route.ts    # Returns ProjectData for a folder under src/data/test-blocks/
└── file/[...path]/route.ts         # Returns raw block-content-editor file contents
```

---

For a complete walkthrough of how a user edit propagates from the page to the database (and how a read trip works in the opposite direction), see [DATA-FLOW.md](DATA-FLOW.md).
