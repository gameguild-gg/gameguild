# GameGuild Editor — Architecture Reference

> Comprehensive documentation for the composable content editor.
> Last updated: April 2026 (post-refactor).

---

## Table of Contents

1. [Overview](#overview)
2. [High-Level Architecture](#high-level-architecture)
3. [Page Composition](#page-composition)
4. [Provider Layer](#provider-layer)
5. [Configuration System](#configuration-system)
6. [Engines](#engines)
7. [Layouts](#layouts)
8. [Content Modes & Node Restrictions](#content-modes--node-restrictions)
9. [Storage Layer](#storage-layer)
10. [Data Flow & Conversion Pipeline](#data-flow--conversion-pipeline)
11. [Nodes](#nodes)
12. [Plugins](#plugins)
13. [Hooks](#hooks)
14. [UI Components (Extras)](#ui-components-extras)
15. [Pages](#pages)
16. [File Map](#file-map)

---

## Overview

The GameGuild Editor is a composable, multi-engine content editor built with Next.js and React. It supports two editing engines (Lexical rich-text and Block Array), three layout types, three content modes, and three storage backends. Pages are assembled by combining **Provider → Layout → Toolbar + Field + Dialogs** components, with behavior controlled through two configuration objects: `FieldConfig` and `ToolbarConfig`.

### Design Principles

- **Composability** — Every page is a composition of Provider + Field + Toolbar + Dialogs.
- **Configuration over code** — `FieldConfig` controls *what* is available; `ToolbarConfig` controls *what is visible*.
- **Engine agnostic** — The same Provider/Toolbar/Dialogs work with both Lexical and Blocks engines.
- **Mode-based restrictions** — Node types are allowed/blocked declaratively per mode, enforced at insert-time.
- **Storage abstraction** — IndexedDB + Google Drive + Cloud behind a single adapter interface.

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────┐
│                      Page                           │
│  ┌───────────────────────────────────────────────┐  │
│  │           EditorProvider / ViewerProvider      │  │
│  │  ┌─────────────────────────────────────────┐  │  │
│  │  │           Layout (Studio/Viewer)        │  │  │
│  │  │  ┌───────────────────────────────────┐  │  │  │
│  │  │  │         Toolbar                   │  │  │  │
│  │  │  ├───────────────────────────────────┤  │  │  │
│  │  │  │         Field                     │  │  │  │
│  │  │  │  ┌─────────────────────────────┐  │  │  │  │
│  │  │  │  │   Engine (Lexical / Blocks) │  │  │  │  │
│  │  │  │  └─────────────────────────────┘  │  │  │  │
│  │  │  └───────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────┘  │  │
│  │               Dialogs                         │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

**Data flow (simplified):**

```
User Edit → Engine → Hook setters → Auto-save → EnhancedStorageAdapter → IndexedDB + Sync
```

---

## Page Composition

Every editor page follows the same pattern:

```tsx
// Minimal page — quiz-only editor
const fieldConfig: Partial<FieldConfig> = {
  engines: ["blocks"],
  layouts: ["type1"],
  allowedBlockTypes: [],
  allowedModes: ["quiz-page"],
  defaultEngine: "blocks",
  defaultLayout: "type1",
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

| Component         | Role                                                      |
|-------------------|-----------------------------------------------------------|
| `EditorProvider`  | Wraps hooks (storage, history, preview), manages UI state |
| `StudioLayout`    | Responsive container with max-width based on layout       |
| `EditorToolbar`   | Header + action bar (save, open, create, preview, etc.)   |
| `EditorField`     | Dispatches to correct engine/layout based on project state|
| `EditorDialogs`   | All modal dialogs (create, save-as, history, preview, etc.)|

For the **viewer** (read-only):

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

**File:** `engines/editor-provider.tsx`

Initializes three hooks and merges configs:

```
EditorProvider
  ├── useProjectStorage(initialDefaults)  → project state, CRUD, auto-save
  ├── useProjectHistory(project)          → commit/snapshot navigation
  └── useProjectPreview(project)          → preview dialog state
```

Exposes via React Context (`useEditor()`):
- `project` — all storage state and operations
- `history` — commit navigation, `isViewingHistory`
- `preview` — preview dialog control
- `fieldConfig` — merged FieldConfig
- `toolbarConfig` — merged ToolbarConfig
- `ui` — all UI state (dialog open/close, save handlers, navigation guards)

Also handles:
- **Ctrl+S** keyboard shortcut
- **Navigation guards** — prompts save before leaving with unsaved changes
- **Exit confirmation** — dialog when navigating away with content

### ViewerProvider

**File:** `engines/viewer-provider.tsx`

Wraps `useViewerStorage()` for read-only content display. Exposes via `useViewer()`.

---

## Configuration System

### FieldConfig

Controls **what** the editor supports.

```typescript
interface FieldConfig {
  engines: EngineType[]              // ["lexical", "blocks"]
  layouts: ProjectType[]             // ["type1", "type2", "type3"]
  allowedBlockTypes?: BlockCellType[] // For blocks engine picker
  allowedNodeTypes?: string[]         // For lexical node restrictions
  allowedModes?: ProjectMode[]        // Restricts content mode selector
  defaultEngine?: EngineType
  defaultLayout?: ProjectType
  defaultMode?: ProjectMode
}
```

**Behavior:**
- `engines` / `layouts` — filter options in Create Project dialog.
- `allowedBlockTypes` — filters Block Types tab in block picker. Empty array or single entry hides the tab.
- `allowedModes` — filters Content Mode dropdown in Create Project dialog. Single entry hides it. First entry becomes the effective initial mode.
- `defaultEngine` / `defaultLayout` — initial state when no project is loaded.
- `defaultMode` — UI hint for dialog default; overridden by `allowedModes[0]` when set.

### ToolbarConfig

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
  showPreviewModeSelector?: boolean
  showNavHome?: boolean
  showNavViewer?: boolean
  showNavStudio?: boolean
}
```

### Merge behavior

Both configs have merge helpers. Partial configs are merged with defaults:

```typescript
const fieldConfig = mergeFieldConfig(fieldPartial)    // fills missing with DEFAULT_FIELD_CONFIG
const toolbarConfig = mergeToolbarConfig(toolbarPartial) // fills missing with DEFAULT_TOOLBAR_CONFIG
```

---

## Engines

The editor supports two engines, selected per-project.

### Lexical Engine (`"lexical"`)

A rich-text editor powered by Meta's Lexical framework. Content is a tree of Lexical nodes with 23 custom decorator node types embedded inline.

**File:** `engines/lexical/lexical-editor.tsx`

- Registers ~28 node types (base + custom decorators)
- Loads ~16 plugins (formatting, insertion, validation, etc.)
- Provides context for project ID, storage adapter, and loading state
- Supports three layouts: single, multiple (multi-panel), slideshow

### Blocks Engine (`"blocks"`)

An array-based editor where each block is a standalone content unit (quiz, diagram, markdown, etc.).

**Files:** `engines/blocks/`

- `block-array-editor.tsx` — Main editor: ordered list with insert lines, drag-to-reorder, inline editing
- `block-array-viewer.tsx` — Read-only renderer + `blockToSerializedNode()` for preview
- `block-type-picker.tsx` — Modal picker with two tabs: Block Types (categorized) + Templates (quiz presets)
- `block-editor-modal.tsx` — Edit dialog dispatching to type-specific editors
- `block-component-registry.ts` — Registry mapping 21 block types to icons, labels, `createEmpty()`, editors, renderers

**Block Types (21):**

| Type | Label | Category |
|------|-------|----------|
| `quiz` | Quiz | Interactive |
| `code` | Code Studio | Interactive |
| `btn` | Button | Interactive |
| `img` | Image | Media |
| `vid` | Video | Media |
| `aud` | Audio | Media |
| `gal` | Gallery | Media |
| `yt` | YouTube | Media |
| `spot` | Spotify | Media |
| `mmd` | Mermaid | Data & Diagrams |
| `vega` | Vega-Lite | Data & Diagrams |
| `tbl` | Table | Data & Diagrams |
| `pres` | Presentation | Data & Diagrams |
| `rt` | Rich Text | Content |
| `md` | Markdown | Content |
| `html` | HTML | Content |
| `hdr` | Header | Content |
| `div` | Divider | Structure |
| `adm` | Admonition | Structure |
| `src` | Source | Structure |
| `proj` | Project | Structure |

### Block picker behavior

The picker has two tabs: **Block Types** and **Templates** (quiz presets).

- When `allowedBlockTypes` is empty (`[]`) or undefined AND `allowedModes` includes `"quiz-page"` → Block Types tab is **hidden**, opens on Templates.
- When `allowedBlockTypes` has exactly 1 entry → Block Types tab is **hidden** (pointless with a single item).
- When `allowedBlockTypes` has 2+ entries AND `allowedModes` includes `"quiz-page"` → Both tabs visible, **defaults to Templates**.
- Otherwise → Both tabs visible, **defaults to Block Types**.

---

## Layouts

Layouts determine how the editor area is structured. Only applies to the Lexical engine.

| ProjectType | Internal Layout | Description |
|-------------|-----------------|-------------|
| `type1`     | `single`        | One editor pane. Simple document editing. |
| `type2`     | `multiple`      | Multiple panels with dynamic block management. Side-by-side editing. |
| `type3`     | `slideshow`     | Presentation-style with slides. Each slide is an independent or dependent sub-project. |

**Layout components:**

- `EditorLayoutType1` — Single editor pane
- `EditorLayoutType2` — Multi-panel with block add/remove
- `EditorLayoutSlideshow` — Slide management with import/convert/navigate

The Blocks engine always uses a flat list (no layout variants).

---

## Content Modes & Node Restrictions

### Modes

Three content modes control which node types are allowed:

| Mode | Description | Default Restrictions |
|------|-------------|---------------------|
| `free-page` | No restrictions. All nodes allowed everywhere. | None |
| `code-page` | Code-focused. Block b1 blocks code-studio; b2 allows only code-studio. | `b1: ['code-studio', null], b2: ['*', 'code-studio']` |
| `quiz-page` | Quiz-focused. Block b1 blocks quiz; b2 allows only quiz. | `b1: ['quiz', null], b2: ['*', 'quiz']` |

### Restriction Format

```typescript
interface NodeRestrictions {
  blocks?: Record<string, [blocked, allowed]>
  panels?: Record<string, [blocked, allowed]>
}
// blocked/allowed can be: null | "*" | "nodeType" | string[]
```

**Resolution rules (priority order):**
1. Panel-level restriction (highest priority)
2. Block-specific restriction
3. Fallback to `b2` then `b1` defaults
4. Tuple interpretation:
   - `allowed === "*"` → permit all
   - `allowed` is list → ONLY those types permitted
   - `allowed === null` → check blocked list

### Enforcement

- **Insert-time:** `NodeValidationPlugin` intercepts `INSERT_NODE_COMMAND` and calls `isNodeAllowed()`.
- **Picker-time:** `FloatingContentInsertPlugin` checks `isNodeAllowed()` to show/hide insert options.
- **Block picker:** `allowedBlockTypes` in FieldConfig filters the Block Type Picker directly.

### allowedModes in FieldConfig

`allowedModes` serves double duty:
1. **Restricts the Content Mode dropdown** in the Create Project dialog (single entry hides it entirely).
2. **Sets the effective initial mode** — `allowedModes[0]` is used as the initial project mode, overriding `defaultMode`.

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

### EnhancedStorageAdapter

**File:** `lib/storage/editor/enhanced-storage-adapter.ts`

IndexedDB schema (v3):
- `projects` — Full `ProjectData` objects (keyPath: `id`)
- `project_metadata` — Sync-optimized metadata with hash index
- `tag_data` — Tag→ProjectIds relationships

Key operations:
- `save(id, name, data, tags, storageType, preferences, type, deps, engine)` — Persist + auto-commit + sync queue
- `load(id)` — IndexedDB first, then remote fallback
- `list()` — Merges local + remote metadata
- `delete(id)` — Removes from all stores
- `searchProjects(term, tags, filterMode, storageTypeFilter)` — Full-text + tag search

### ProjectData

```typescript
interface ProjectData {
  id: string
  name: string
  data: string              // Serialized content (Cellular or raw)
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  hash?: string
  storageType?: StorageType // "local" | "gameguild-cloud" | "google-drive"
  type?: ProjectType        // "type1" | "type2" | "type3"
  engine?: EngineType       // "lexical" | "blocks"
  preferences?: ProjectPreferences
  deps?: StorageProjectData[]
  syncStatus?: SyncStatus
}
```

### Git History

Auto-commits on every save. Supports:
- `getCommits(projectId)` — Full history
- `loadCommit(projectId, sha)` — View old version (read-only)
- `createSnapshot(projectId, name)` — Named tag
- `loadSnapshot(projectId, tag)` — Restore tagged version

---

## Data Flow & Conversion Pipeline

### The Cell Layer

All content passes through a unified **Cellular** format for storage, regardless of engine:

```
Lexical EditorState ←→ CellularDocument ←→ Storage (string)
Block Array         ←→ BlockStorage      ←→ Storage (string)
```

### CellularDocument

```typescript
interface CellularDocument {
  v: 0                    // Version
  u: "lexical" | "blocks" // UI origin
  c: Cell[]               // Content cells
}

type Cell = [data: object, metadata: CellMetadata]
// metadata.t = cell type ("p", "h", "q", "l", "image", "quiz", etc.)
```

### Converters

**Lexical ↔ Cells** (`cell-converters/lexical.ts`):
- `lexicalToCells(editorState)` — Walks Lexical tree, produces Cell array
- `cellsToLexical(doc)` — Reconstructs SerializedEditorState from cells

**Blocks ↔ Storage** (`cell-converters/blocks.ts`):
- `blocksToStorage(blocks)` → `{ order: string[], blocks: Record<string, Cell> }`
- `storageToBlocks(storage)` → `Block[]`
- `blockToSerializedNode(block)` → fake Lexical node for preview reuse

### Layout Detection

`layout-detector.ts` analyzes raw project data to determine layout:
- `detectProjectLayout(data)` → `{ layout, blockCount, hasSlides }`
- `extractEditorStates(data, type)` → per-block editor states
- `createProjectData(type, states)` → serialize states for storage

---

## Nodes

23 custom decorator nodes, each with:
- **Node class** (extends `DecoratorNode` or `MediaNodeBase`)
- **Plugin** (registers `INSERT_*_COMMAND`)
- **Preview renderer** (in `plugins/preview-components/`)
- **Editor UI** (in `extras/` or inline)

| Node | Class | Base |
|------|-------|------|
| Admonition | `AdmonitionNode` | DecoratorNode |
| Audio | `AudioNode` | MediaNodeBase |
| Button | `ButtonNode` | DecoratorNode |
| Code Studio | `CodeStudioNode` | DecoratorNode |
| Custom List | `CustomListNode` | ListNode |
| Divider | `DividerNode` | DecoratorNode |
| Gallery | `GalleryNode` | DecoratorNode |
| Header | `HeaderNode` | DecoratorNode |
| HTML | `HTMLNode` | DecoratorNode |
| Image | `ImageNode` | MediaNodeBase |
| Markdown | `MarkdownNode` | DecoratorNode |
| Mermaid | `MermaidNode` | DecoratorNode |
| Presentation | `PresentationNode` | DecoratorNode |
| Project | `ProjectNode` | DecoratorNode |
| Quiz | `QuizNode` | DecoratorNode |
| Rich Text | `RichTextNode` | DecoratorNode |
| Source Code | `SourceCodeNode` | DecoratorNode |
| Source | `SourceNode` | DecoratorNode |
| Spotify | `SpotifyNode` | DecoratorNode |
| Table | `TableNode` | DecoratorNode |
| Vega-Lite | `VegaLiteNode` | DecoratorNode |
| Video | `VideoNode` | MediaNodeBase |
| YouTube | `YouTubeNode` | DecoratorNode |

`MediaNodeBase` (`nodes/base/media-node-base.tsx`) — shared base for Image, Video, Audio nodes with common media handling.

---

## Plugins

### Node Insertion Plugins

Each decorator node has a dedicated plugin that registers an `INSERT_*_COMMAND`:

```
image-plugin.tsx     → INSERT_IMAGE_COMMAND
video-plugin.tsx     → INSERT_VIDEO_COMMAND
audio-plugin.tsx     → INSERT_AUDIO_COMMAND
quiz-plugin.tsx      → INSERT_QUIZ_COMMAND
code-studio-plugin.tsx → INSERT_CODE_STUDIO_COMMAND
... (one per node type)
```

### Core Plugins

| Plugin | Purpose |
|--------|---------|
| `floating-content-insert-plugin.tsx` | Floating `+` toolbar for inserting all node types. Checks `isNodeAllowed()`. |
| `floating-text-format-toolbar-plugin.tsx` | Floating toolbar for text formatting (bold, italic, link, color, etc.) |
| `node-validation-plugin.tsx` | Enforces mode-based node restrictions at insert-time |
| `code-plugin.tsx` | Basic code block handling |
| `preview-plugin.tsx` | Opens preview dialog with serialized state |

### Floating Text Format Components

Located in `plugins/floating-text-components/`:

- Background color picker
- Font family selector
- Font size selector
- Text formatting (bold, italic, underline, strikethrough, subscript, superscript, code)
- Link editor
- List management (ordered, unordered)
- Text/list color pickers

---

## Hooks

### useProjectStorage

**File:** `hooks/useProjectStorage.ts`

The main hook. Manages all project state and persistence.

**Accepts:** `ProjectStorageDefaults?` — optional initial `engine`, `layout`, `mode`.

**Returns:**
- Project metadata: `projectId`, `projectName`, `projectType`, `layout`, `engine`, `projectMode`, `storageType`, `tags`, `preferences`
- Editor states: `editorState`, `blockStates`, `blockArrayBlocks`, `slideshowStructure`, `slideshowDeps`
- Operations: `save()`, `saveAs()`, `loadProject()`, `createProject()`
- Slideshow: `convertToIndependent()`, `convertToDependent()`, `importConfirm()`
- Block ops: `addBlock()`, `removeBlock()`
- Lists: `savedProjects`, `availableTags`, `refreshProjects()`, `refreshTags()`
- Size: `projectSize`, `assetsSize`, `assets`
- Sync: `syncStats`, `autoSaveEnabled`
- Direct access: `db` (EnhancedStorageAdapter), `storageAdapter`

**Auto-save:** Debounced save triggered by state changes. Suppressed when `readOnlyRef.current` is true (history viewing).

**URL hash:** On mount, checks `window.location.hash` for project ID and loads it.

### useProjectHistory

**File:** `hooks/useProjectHistory.ts`

Git-based version history.

- `commits` — list of past commits
- `isViewingHistory` — true when viewing an old version
- `loadCommit(sha)` — switch to read-only old version
- `loadSnapshot(tag)` — switch to named snapshot
- `returnToHead()` — back to latest
- `createSnapshot(name)` — tag current state

### useProjectPreview

**File:** `hooks/useProjectPreview.ts`

Preview dialog management.

- `isPreviewOpen` / `setIsPreviewOpen`
- `openPreview()` — serializes current state and opens dialog

### useViewerStorage

**File:** `hooks/useViewerStorage.ts`

Read-only storage for the viewer page.

- Loads project from URL hash
- Computes layout info (layout type, per-block states, slideshow resolution)
- Returns `layoutInfo` with states, blocksArray, slideshowData, previewMode

---

## UI Components (Extras)

Located in `extras/`. Key areas:

| Directory | Purpose |
|-----------|---------|
| `editor/` | Create project dialog, info dialog, storage option selector |
| `dialogs/` | Confirmation dialogs (delete, refresh) |
| `media/` | Unified media editor, asset image handling |
| `media-upload-dialog.tsx` | Universal media upload (file + URL) |
| `manager-page/` | Project/asset manager UI (grid/list views, filters, pagination) |
| `preview/` | Preview renderers for all node types |
| `quiz/` | Quiz display, entry types, settings, examples |
| `code-studio/` | Code editor/IDE with runners (JS, Python, C++, Rust, etc.) |
| `presentation/` | Presentation player, ODP/PPTX parsers |
| `mermaid/` | Mermaid diagram editor |
| `vega-lite/` | Vega-Lite visualization editor |
| `markdown/` | Markdown editor and component renderer |
| `html/` | HTML editor and viewer |
| `rich-text/` | Rich text embedded editor |
| `content-edit-menu.tsx` | Edit/delete menu for decorator nodes |
| `settings-menu.tsx` | Settings menu for editor preferences |

---

## Pages

All pages live under `app/[locale]/(gglexical)/gglexical/`.

| Route | Page | Description |
|-------|------|-------------|
| `/gglexical` | Home | Project manager with grid/list views, asset manager, collections |
| `/gglexical/studio` | Studio | Full editor — all engines, layouts, modes |
| `/gglexical/viewer` | Viewer | Read-only content viewer |
| `/gglexical/quiz-editor` | Quiz Editor | Blocks-only, quiz mode, restricted picker |
| `/gglexical/doc-editor` | Doc Editor | Lexical-only, type1 layout, free mode |
| `/gglexical/full-editor` | Full Editor | All defaults enabled |
| `/gglexical/publish` | Publish | Placeholder (renders null) |

---

## File Map

### Core (engines/)

```
engines/
├── editor-config.ts          # FieldConfig, ToolbarConfig, defaults, merge helpers
├── editor-provider.tsx        # EditorProvider, EditorContextValue, useEditor()
├── viewer-provider.tsx        # ViewerProvider, ViewerContextValue, useViewer()
├── editor-field.tsx           # Dispatches to Lexical layout or BlockArrayEditor
├── viewer-field.tsx           # Dispatches to viewer content or empty state
├── editor-toolbar.tsx         # Header + action bar with ToolbarConfig conditionals
├── viewer-toolbar.tsx         # Viewer header + action bar
├── editor-dialogs.tsx         # All editor dialogs (create, save-as, history, etc.)
├── viewer-dialogs.tsx         # Viewer dialogs (exit confirm)
├── blocks/                    # Block Array engine
│   ├── block-array-editor.tsx
│   ├── block-array-viewer.tsx
│   ├── block-component-registry.ts
│   ├── block-editor-modal.tsx
│   └── block-type-picker.tsx
└── lexical/                   # Lexical engine
    ├── lexical-editor.tsx
    ├── editor-layout-type1.tsx
    ├── editor-layout-type2.tsx
    └── editor-layout-slideshow.tsx
```

### Hooks

```
hooks/
├── useProjectStorage.ts       # Main storage hook (CRUD, auto-save, sync)
├── useProjectHistory.ts       # Git-based version history
├── useProjectPreview.ts       # Preview dialog management
└── useViewerStorage.ts        # Read-only viewer storage
```

### Storage (lib/storage/editor/)

```
lib/storage/editor/
├── enhanced-storage-adapter.ts  # IndexedDB + sync + Drive adapter
├── project-types.ts             # EngineType, ProjectType, layout helpers
├── project-modes.ts             # ProjectMode, NodeRestrictions, isNodeAllowed()
├── project-preferences.ts       # ProjectPreferences, per-panel config
├── block-structure.ts           # Block, BlockArray, BlockStorage types
├── cell-structure.ts            # Cell, CellularDocument, CellularContent
├── layout-detector.ts           # detectProjectLayout(), extractEditorStates()
├── slideshow-structure.ts       # SlideshowStructure, slide helpers
├── storage-types.ts             # StorageType, SyncStatus constants
├── multi-block-layout.ts        # Multi-panel layout config
├── panel-structure.ts           # Panel layout types
├── project-resolver.ts          # Slide/dependent project resolution
├── editor-preferences.ts        # Editor preferences DB
└── cell-converters/
    ├── index.ts                 # Router for cell conversion
    ├── lexical.ts               # Lexical ↔ Cells conversion
    ├── blocks.ts                # Blocks ↔ BlockStorage conversion
    ├── cell-data.ts             # Cell data type definitions
    └── cell-metadata.ts         # Cell metadata types
```

### Nodes (23 types)

```
nodes/
├── base/media-node-base.tsx     # Shared base for Image, Video, Audio
├── admonition-node.tsx
├── audio-node.tsx
├── button-node.tsx
├── code-studio-node.tsx
├── custom-list-node.tsx
├── divider-node.tsx
├── gallery-node.tsx
├── header-node.tsx
├── html-node.tsx
├── image-node.tsx
├── markdown-node.tsx
├── mermaid-node.tsx
├── presentation-node.tsx
├── project-node.tsx
├── quiz-node.tsx
├── rich-text-node.tsx
├── source-code-node.tsx
├── source-node.tsx
├── spotify-node.tsx
├── table-node.tsx
├── vega-lite-node.tsx
├── video-node.tsx
├── youtube-node.tsx
├── presentation/               # Presentation sub-components
└── quiz/                       # Quiz sub-components
```

### Plugins

```
plugins/
├── floating-content-insert-plugin.tsx   # Main insert toolbar
├── floating-text-format-toolbar-plugin.tsx # Text formatting toolbar
├── node-validation-plugin.tsx           # Mode-based restriction enforcement
├── code-plugin.tsx                      # Code block handling
├── preview-plugin.tsx                   # Preview dialog
├── [node]-plugin.tsx                    # One per node type (22 files)
├── floating-text-components/            # 10 formatting UI components
└── preview-components/                  # 30+ preview renderers
```
