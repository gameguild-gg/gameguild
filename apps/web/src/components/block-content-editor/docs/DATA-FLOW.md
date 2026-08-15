# GameGuild Block Content Editor — Data Flow

> Technical walkthrough of how data flows through the editor, viewer, and static-viewer surfaces — from the page boundary down to IndexedDB / Git / remote sync, and back up through the preview pipeline.

For a structural overview of the modules, see [ARCHITECTURE.md](ARCHITECTURE.md). This document focuses on **runtime behavior**: which types are exchanged, which functions transform them, and how the layers connect.

---

## Table of Contents

1. [Layered Mental Model](#layered-mental-model)
2. [Type Vocabulary](#type-vocabulary)
3. [Editor Flow — Write Path](#editor-flow--write-path)
4. [Editor Flow — Read Path (Load Project)](#editor-flow--read-path-load-project)
5. [Viewer Flow](#viewer-flow)
6. [Static-Viewer Flow](#static-viewer-flow)
7. [Preview Pipeline](#preview-pipeline)
8. [Rich-Text Block — Nested Pipeline](#rich-text-block--nested-pipeline)
9. [Storage Adapter Internals](#storage-adapter-internals)
10. [Git History & Snapshots](#git-history--snapshots)
11. [Sync Pipeline (Remote)](#sync-pipeline-remote)
12. [Import / Export](#import--export)
13. [Sequence Diagrams](#sequence-diagrams)

---

## Layered Mental Model

```
┌─────────────────────────────────────────────────────────────────┐
│ PAGE LAYER          app/[locale]/(block-content-editor)/…/page  │
│                     EditorProvider / ViewerProvider             │
├─────────────────────────────────────────────────────────────────┤
│ ENGINE LAYER        engines/                                    │
│                     EditorField · BlockArrayEditor              │
│                     ViewerField · BlockArrayViewer              │
│                     StaticViewer · static-viewer-sections.tsx   │
├─────────────────────────────────────────────────────────────────┤
│ BLOCK LAYER         engines/blocks/ · extras/<area>/            │
│                     BLOCK_REGISTRY · per-block editors          │
│                     plugins/preview-components/                 │
├─────────────────────────────────────────────────────────────────┤
│ NODES (TYPE ONLY)   nodes/                                      │
│                     XxxData · SerializedXxxNode                 │
├─────────────────────────────────────────────────────────────────┤
│ STORAGE LAYER       lib/storage/editor/                         │
│                     EnhancedStorageAdapter                      │
│                     block-storage.ts (serialize/deserialize)    │
│                     block-structure.ts (Block, BlockArray, …)   │
├─────────────────────────────────────────────────────────────────┤
│ PERSISTENCE LAYER   IndexedDB ("GGEditorDB" v6)                 │
│                     Git (isomorphic-git + lightning-fs)         │
│                     SyncManager → Google Drive / Cloud API      │
└─────────────────────────────────────────────────────────────────┘
```

Each layer talks only to its neighbors. Hooks (`useProjectStorage`, `useViewerStorage`, `useProjectHistory`, `useProjectPreview`) live between the engine and storage layers and are the only callers of `EnhancedStorageAdapter` in the application code. `useProjectStorage` is itself a thin composer over six focused sub-hooks under [hooks/editor/](../hooks/editor/) (DB init, state, lists, sizes, sync, operations); only the composer owns the cross-cutting concerns — storage adapter wrapper, URL-hash bootstrap, and debounced auto-save.

---

## Type Vocabulary

The canonical type modules live under [lib/storage/editor/](../lib/storage/editor/) and [lib/sync/editor/](../lib/sync/editor/) and are re-exported through the barrel [lib/storage/editor/index.ts](../lib/storage/editor/index.ts). Callers should import from the barrel and never need to know which file owns a given type.

```
Block              { id: string; type: BlockCellType; data: BlockDataMap[type] }
BlockArray         Block[]                                          // runtime list
BlockOrderEntry    readonly [id: string, type: BlockCellType]       // persistence entry
AnyBlockData       BlockDataMap[BlockCellType]                      // union of all payloads
BlockStorage       { order: BlockOrderEntry[]; blocks: Record<id, AnyBlockData> }

ProjectType        "document" | "quiz" | "general"                  // project-types.ts
ProjectPreferences { global, blocks } stored on ProjectData          // project-preferences.ts
StorageType        "local" | "gameguild-cloud" | "google-drive"     // storage-types.ts
SyncStatus         "pending" | "syncing" | "synced" | "error"        // storage-types.ts
SyncStats          { pending, syncing, synced, errors, ... }         // sync-types.ts

ProjectMetadata    { size, hash, createdAt, updatedAt }
ProjectData        { id, name, data: string, tags, metadata,
                     storageType, syncStatus?, isLocallyAvailable?, preferences? }
ProjectMetadataRecord  ProjectData minus the heavy `data` field

ProjectExportInput / ProjectExportMetadata          // interopAdapter/interop-types.ts
```

Conversion functions in [lib/storage/editor/block-storage.ts](../lib/storage/editor/block-storage.ts):

```
serializeProject(BlockArray)           → string                 // JSON of BlockStorage
deserializeProject(string)             → BlockArray
blocksToStorage(BlockArray)            → BlockStorage
storageToBlocks(BlockStorage | null)   → BlockArray
blockToPreviewNode(Block)              → { type, data | entry, version }  // for preview-components
EMPTY_PROJECT_DATA                     → '{"order":[],"blocks":{}}'
```

`BlockCellType` is the union of the 18 string literals enumerated in `BLOCK_CELL_TYPES` (`"quiz" | "code-studio" | "image" | …`). `BlockDataMap[T]` resolves to the data shape for type `T` (e.g. `BlockDataMap["mermaid"] === MermaidData`). `AnyBlockData = BlockDataMap[BlockCellType]` is the union of every possible payload — the value type stored in `BlockStorage.blocks`. Block IDs are sequential numeric strings produced by `nextBlockId(blocks)` (in [block-structure.ts](../lib/storage/editor/block-structure.ts)) and never recycled. Project IDs remain UUIDs (`generateProjectId()` in [project-id.ts](../lib/storage/editor/project-id.ts)).

---

## Editor Flow — Write Path

Path of a single user edit, from the DOM down to IndexedDB:

```
[1] User clicks "+" between blocks (InsertLine)        BlockArrayEditor
        │
        ▼
[2] BlockTypePicker shows allowed types                BlockTypePicker
    (filtered by FieldConfig.allowedBlockTypes)
        │
        ▼
[3] User picks a type → BLOCK_REGISTRY[type].createEmpty(nextBlockId(blocks))
    Returns: { id, type, data: <defaults> }            block-component-registry.ts
        │
        ▼
[4] setBlocks([…before, newBlock, …after])             useProjectStorage.setBlocks
    (Dispatch<SetStateAction<BlockArray>>)             — forwards to useProjectState
        │
        ▼
[5] React commit → effect detects `blocks` changed
        │
        ▼
[6] Debounced auto-save fires (if !readOnlyRef.current &&
    autoSaveEnabled). The composer owns the debounce;     useProjectStorage
    sub-hooks are state/operation-focused.
        │
        ▼
[7] serializeProject(blocks) → string                  block-storage.ts
        │
        ▼
[8] db.save(id, name, dataString, tags,                EnhancedStorageAdapter.save
    storageType, preferences)
        │
        ▼
[9] Adapter computes hash + size                       hash-manager.ts
    Adapter writes to 3 stores in one transaction:
      • projects               (full ProjectData)
      • project_metadata       (ProjectMetadataRecord)
      • tag_data               (tag → projectIds)
        │
        ▼
[10] Git auto-commit                                   git-history-manager.ts
     (isomorphic-git on lightning-fs)
        │
        ▼
[11] SyncManager.enqueue({ id, op:"upsert" })          sync-manager.ts
     Queue stored in IndexedDB (sync-queue.ts)
        │
        ▼
[12] Async drain:
     • storageType === "google-drive" → GoogleDriveSync.upload(project)
     • storageType === "gameguild-cloud" → api-client.put(project)
     Each call compares local hash with remote before uploading.
```

**Key invariants:**

- `useProjectStorage` is the **only** writer to `EnhancedStorageAdapter` from page code (operations module `extras/editor/project-save-operations.ts` is invoked via the hook).
- The Git commit and the sync enqueue happen *after* IndexedDB is persisted. A failure in step 10 or 11 does not roll back the IndexedDB write.
- `readOnlyRef.current === true` (set while viewing a past commit) gates **all** auto-saves.

---

## Editor Flow — Read Path (Load Project)

Loading happens in two scenarios: on mount (URL hash) and through the open-project dialog.

### On mount

```
[1] EditorProvider mounts → useProjectStorage()
        │
        ▼
[2] useEffect inspects window.location.hash
        │
        ▼
[3] If hash present:
        await db.init()
        await db.load(hash)                            EnhancedStorageAdapter
        │
        ▼
[4] db.load() implementation:
      a. IndexedDB get("projects", id) → ProjectData | undefined
      b. If undefined and remote configured → fetch remote → store locally
      c. Return ProjectData | null
        │
        ▼
[5] setProjectId / setProjectName / setStorageType / setTags / setPreferences
    blocks = deserializeProject(project.data)          block-storage.ts
    setBlocks(blocks)
        │
        ▼
[6] BlockArrayEditor receives the new blocks and rerenders.
```

### Via Open Project dialog

```
ProjectPickerShell → project-picker list → onSelect(id)
   → loadProject({ id, name, content: ProjectData.data, … })
   → setBlocks(deserializeProject(content))
```

---

## Viewer Flow

The viewer is structurally identical to the editor but uses `useViewerStorage` and a different renderer.

```
ViewerPage
  └── ViewerProvider              uses useViewerStorage
        └── ViewerField
              └── BlockArrayViewer
                    └── for each Block:
                          node = blockToPreviewNode(block)
                          render <Preview<Xxx> node={node} />
```

`useViewerStorage`:

1. Reads `window.location.hash` for project ID.
2. Calls `db.load(id)`.
3. Stores the resulting `ProjectData` in state.
4. Exposes `blocks = deserializeProject(currentProject.data)`.

There is no write path here.

---

## Static-Viewer Flow

The static viewer exposes three source modes through three hooks defined in [engines/static-viewer-sections.tsx](../engines/static-viewer-sections.tsx):

```
useStaticProject(id)                  → reads IndexedDB via EnhancedStorageAdapter
useStaticProjectFromFolder(folder)    → fetches /api/static-viewer/folder/[folderName]
useStaticBlocksFromFile(filePath)     → fetches /api/static-viewer/file/[...path]
```

Each hook returns `{ loading, error, blocks }` (the folder hook also returns the full `project` for header rendering). All three pipe the data string through `deserializeProject(...)` to produce a `BlockArray` and then render via `<BlockArrayViewer blocks={blocks} />`.

### Folder source

```
DirectFolderSection
  └── useStaticProjectFromFolder("projeto-…")
        └── fetch("/api/static-viewer/folder/projeto-…")
              └── route.ts (server):
                    - validate folderName against /^[A-Za-z0-9._-]+$/
                    - resolve path under src/data/test-blocks/
                    - assert resolved path stays inside the base dir
                    - read index.json + data.block-content-editor
                    - normalize legacy (flat) index → ProjectMetadata
                    - return { project: ProjectData }
```

### File source

```
DirectFileSection
  └── useStaticBlocksFromFile("…/data.block-content-editor")
        └── fetch("/api/static-viewer/file/<encoded segments>")
              └── route.ts (server):
                    - validate each segment, reject ".." or non-matching chars
                    - read file as UTF-8
                    - validate it parses as JSON
                    - return { data: rawString }
        └── deserializeProject(data) → BlockArray
```

The file source skips the `index.json` step entirely — no title, no tags, no `updatedAt`. Only the block content is rendered.

---

## Preview Pipeline

The preview-components live in [plugins/preview-components/](../plugins/preview-components/) and are the **only** code path that renders blocks for read-only consumption (used by `BlockArrayViewer`, the editor preview dialog, and the static viewer).

```
Block          (id, type, data)
   │
   │  blockToPreviewNode(block)
   ▼
PreviewNode    { type, data, version } (or { type, entry, version } for quiz)
   │
   ▼
switch (block.type) in BlockContentRenderer:
   case "quiz":         <PreviewQuiz       node={node} />
   case "image":        <PreviewImage      node={node} />
   case "video":        <PreviewVideo      node={node} />
   …
   case "rich-text":    <PreviewRichText   node={node} />  ← nested pipeline
```

The `version: 1` field is kept on every preview node so future migrations can fork on shape changes without invalidating existing serialized data.

---

## Rich-Text Block — Nested Pipeline

The `rich-text` block embeds a Lexical editor whose state is stored **as a `SerializedEditorState` object inside `RichTextData.content`** (the outer `BlockStorage` JSON contains the Lexical state directly, not a JSON-encoded string).

```
RichTextData.content
   │  (SerializedEditorState object)
   ▼
Lexical SerializedEditorState
   │
   ▼  (in PreviewRichText)
Render Lexical nodes recursively via:
   • preview-paragraph.tsx
   • preview-text.tsx
   • preview-heading.tsx
   • preview-quote.tsx
   • preview-link.tsx
   • preview-list.tsx / preview-list-item.tsx
```

Inside the editor, the floating toolbar ([plugins/floating-text-format-toolbar-plugin.tsx](../plugins/floating-text-format-toolbar-plugin.tsx)) wires Bold / Italic / Link / Color / Lists / Math through Lexical commands and the `custom-list-node.tsx` Lexical node. KaTeX / MathLive render math nodes inline.

---

## Storage Adapter Internals

`EnhancedStorageAdapter` is the single integration point between the editor and persistence. Its constructor instantiates `SyncManager`, `GoogleDriveSync`, and uses `getHistoryManager()` lazily.

### IndexedDB layout

| Store              | Key path | Holds                                                              |
|--------------------|----------|--------------------------------------------------------------------|
| `projects`         | `id`     | Full `ProjectData` (including `data: string` payload)              |
| `project_metadata` | `id`     | `ProjectMetadataRecord` (sync hot path; cheap to list)             |
| `tag_data`         | `name`   | `TagData = { id, name, projectIds: string[] }`                     |
| `tags`             | `name`   | Legacy store; kept only so migration code can read older DBs       |

DB name is `GGEditorDB`. DB version is `6`. When `oldVersion < 6` during upgrade, **every** existing store is dropped — pre-v6 data uses the flat metadata layout (`size`, `hash`, `createdAt`, `updatedAt` directly on the project record) which is incompatible with the current shape.

### Save algorithm (high level)

```
save(id, name, data, tags, storageType, preferences):
    metadata = computeMetadata(data)              // size + sha-1 hash + timestamps
    project  = { id, name, data, tags, metadata, storageType, preferences }
    record   = projectToMetadataRecord(project)

    tx = db.transaction(["projects","project_metadata","tag_data"], "readwrite")
    tx.objectStore("projects").put(project)
    tx.objectStore("project_metadata").put(record)
    upsertTagIndex(tx, id, tags)
    await tx.complete

    await getHistoryManager().commit(id, project)
    syncManager.enqueueUpsert(id, storageType)
```

### Load algorithm

```
load(id):
    local = await idb.get("projects", id)
    if (local) return local
    if (storageType remote configured):
        remote = await syncManager.fetchRemote(id)
        if (remote) {
            await this.save(remote)        // populate cache + queue
            return remote
        }
    return null
```

### List algorithm

```
list():
    locals  = await idb.getAll("project_metadata")
    remotes = await syncManager.listAvailableRemote()  // metadata only
    merged  = mergeByIdPreferringRemoteHashesForSyncStatus(locals, remotes)
    return projectsFromMetadata(merged)
```

---

## Git History & Snapshots

`EnhancedStorageAdapter` delegates Git operations to [`getHistoryManager()`](../lib/storage/git/git-history-manager.ts).

```
On every successful save():
    historyManager.commit(projectId, project)
        │
        ▼
    isomorphic-git writes:
      • blob = serialized project
      • tree references blob
      • commit (parent = HEAD, message = "auto: <updatedAt>")
    Repository lives on lightning-fs (browser-side fs).

Snapshots:
    createSnapshot(projectId, name)
        → tag the current HEAD with `name`
    loadSnapshot(projectId, tag)
        → resolve tag → checkout commit (read-only)
```

Loading a commit returns the historical `ProjectData`, and the consuming hook (`useProjectHistory`) flips `readOnlyRef.current = true` to suppress auto-save while the user explores the past version.

---

## Sync Pipeline (Remote)

```
SyncManager
  ├── init()                  ← restores queue from IndexedDB
  ├── enqueueUpsert(id, type)
  ├── enqueueDelete(id, type)
  └── drain()                 ← processes one entry at a time

Queue entry { id, op, storageType, attempt, lastError }
```

Two remote backends:

### Google Drive (`google-drive-sync.ts`)

```
upload(project):
    fileName = `${id}.bce.json`
    folder   = configured "GameGuild" folder
    if remoteFile and remoteFile.hash === project.metadata.hash: skip
    else upload (multipart) with custom appProperties.hash

download(id):
    locate file by appProperties.id
    fetch content → ProjectData (after hash normalization)
```

Auth is handled by `services/editor/google-drive-service.ts` using OAuth tokens obtained through `next-auth`.

### GameGuild Cloud (`api-client.ts`)

```
put(project)  → POST/PUT /api/projects/:id  (OpenAPI client)
get(id)       → GET  /api/projects/:id
delete(id)    → DELETE /api/projects/:id
```

Both backends are consulted by hash before uploading to minimize bandwidth.

---

## Import / Export

[`lib/interopAdapter/project-exporter.ts`](../lib/interopAdapter/project-exporter.ts) and [`lib/interopAdapter/project-importer.ts`](../lib/interopAdapter/project-importer.ts) provide deterministic local interop.

### Export (folder or ZIP)

```
projeto-<id>/
├── index.json                 ← ProjectMetadata (id, name, tags, metadata, storageType, preferences, exportedAt, …)
├── data.block-content-editor  ← serialized BlockStorage (the same string as ProjectData.data)
├── assets/manifest.json       ← stable URI metadata for bundled assets
└── assets/objects/*           ← raw bundled file bytes
└── assets/
    └── <assetId>.json         ← (optional) per-asset payloads
```

### Import

The `ProjectImporter` accepts:

- **A ZIP** uploaded through the file picker (`importFromZip(file)`).
- **A folder** picked via `<input webkitdirectory>` (`importFromFolder(files: File[])`).
- **Drag-dropped folders** — `webkitGetAsEntry()` + recursive `readEntries()` collect all files including `assets/`, patching each file's `webkitRelativePath` before passing through `importFromFolder`.

Both paths return `ImportedProjectData = { project: ProjectData, assets: AssetRecord[] }`, which the dialog then writes through `EnhancedStorageAdapter.save()` and the asset DB.

---

## Sequence Diagrams

### Save → Sync

```
User      BlockArrayEditor   useProjectStorage   EnhancedStorageAdapter   IndexedDB   Git   SyncManager   Remote
 │              │                  │                     │                  │         │         │           │
 │ insert block │                  │                     │                  │         │         │           │
 ├──────────────▶                  │                     │                  │         │         │           │
 │              │ setBlocks([…])   │                     │                  │         │         │           │
 │              ├──────────────────▶                     │                  │         │         │           │
 │              │                  │ debounce 800 ms     │                  │         │         │           │
 │              │                  ├─ serializeProject() │                  │         │         │           │
 │              │                  ├─ db.save(…)         │                  │         │         │           │
 │              │                  │                     │ put projects     │         │         │           │
 │              │                  │                     ├─────────────────▶│         │         │           │
 │              │                  │                     │ put project_metadata       │         │           │
 │              │                  │                     ├─────────────────▶│         │         │           │
 │              │                  │                     │ commit(id, project)        │         │           │
 │              │                  │                     ├──────────────────────────▶ │         │           │
 │              │                  │                     │ enqueueUpsert(id)          │         │           │
 │              │                  │                     ├──────────────────────────────────────▶│           │
 │              │                  │                     │                  │         │ drain   │ upload    │
 │              │                  │                     │                  │         │         ├──────────▶│
 │              │                  │                     │                  │         │         │ 200 OK    │
 │              │                  │                     │                  │         │         │◀──────────┤
```

### Load by ID

```
Page       useProjectStorage     EnhancedStorageAdapter   IndexedDB    Remote
  │                │                       │                  │           │
  │ mount (#id)    │                       │                  │           │
  ├────────────────▶                       │                  │           │
  │                ├ db.init()             │                  │           │
  │                ├ db.load(id)           │                  │           │
  │                │                       ├ get(projects,id) │           │
  │                │                       ├──────────────────▶           │
  │                │                       │ ProjectData|undef            │
  │                │                       │◀──────────────────           │
  │                │                       │ (fallback: remote)           │
  │                │                       ├─────────────────────────────▶│
  │                │                       │◀─────────────────────────────│
  │                ◀ ProjectData           │                  │           │
  │                ├ deserializeProject()  │                  │           │
  │                ├ setBlocks(…)          │                  │           │
  ▼                ▼                       │                  │           │
 render BlockArrayEditor                   │                  │           │
```

### Static viewer (folder source)

```
Page → DirectFolderSection → useStaticProjectFromFolder("projeto-…")
                            └ fetch /api/static-viewer/folder/projeto-…
                                  └ server reads src/data/test-blocks/projeto-…/
                                    returns { project: ProjectData }
                            ← deserializeProject(project.data)
                            → <BlockArrayViewer blocks={…} />
                                   └ for each block: blockToPreviewNode → <Preview<Type> />
```

---

## Where to look next

- For module-level structure and file map → [ARCHITECTURE.md](ARCHITECTURE.md).
- For the canonical types → [lib/storage/editor/block-structure.ts](../lib/storage/editor/block-structure.ts), [lib/storage/editor/project-data.ts](../lib/storage/editor/project-data.ts).
- For serialization rules → [lib/storage/editor/block-storage.ts](../lib/storage/editor/block-storage.ts).
- For the write/read implementation → [lib/storage/editor/enhanced-storage-adapter.ts](../lib/storage/editor/enhanced-storage-adapter.ts).
- For the editor entry point → [engines/editor-provider.tsx](../engines/editor-provider.tsx) and [hooks/useProjectStorage.ts](../hooks/useProjectStorage.ts).
- For the static viewer → [engines/static-viewer-sections.tsx](../engines/static-viewer-sections.tsx) and the routes in `app/api/static-viewer/`.
