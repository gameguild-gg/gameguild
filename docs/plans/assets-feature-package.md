# Assets Feature Package Plan

Status: implemented.

## Summary

Create a reusable browser-first assets package at:

```text
packages/features/assets
```

with the package name:

```text
@game-guild/assets
```

The package will own stable asset contracts, modern browser persistence,
download caching, asset selection and upload UI, local URL resolution, and the
extension points required by a future remote provider.

The first implementation must work without the dashboard or the infrastructure
assets provider. The package will use durable browser storage and remain ready
to synchronize with a remote provider once the internal assets infrastructure
is stable.

This is not an extraction of the current `block-content-editor` asset manager.
That implementation stores large strings and data URLs in a hand-written
IndexedDB database, mixes persistence with project usage, media compression,
Code Studio collections, and dialogs, and uses SHA-1 identifiers as asset URLs.
It must be replaced rather than imported into the new package.

The product has not launched and there is no user data to preserve. The
cutover will therefore be clean: development assets, old browser databases,
and experimental serialized formats are disposable. Fixtures and development
documents must be updated to the new contracts instead of adding compatibility
code to the package.

The package is intended to serve:

- `@game-guild/lexical-surface`, especially media and Vega-Lite;
- the future Quiz package for question and answer attachments;
- the future Code Studio package for persisted file contents and binary files;
- other editors, previews, documents, and application surfaces that need asset
  upload, local persistence, caching, or resolution.

## Decision

Build one asset domain package rather than separate media, document, dataset,
and code-file storage systems.

The package will have two storage tiers:

1. a browser repository available now;
2. an optional remote provider contract, with no dashboard implementation in
   this phase.

The browser repository is a real implementation, not a mock provider. It owns
locally created assets and can cache assets downloaded from a future remote
provider. Locally authored data and evictable remote cache entries must be
distinguished so that cache cleanup can never delete the only copy of a user's
asset.

## Goals

- provide one stable asset reference format across Lexical, Vega-Lite, Quiz,
  Code Studio, and other consumers;
- keep binary and text file bodies out of serialized editor documents;
- persist browser-owned assets across reloads without base64 encoding;
- use modern browser storage with explicit quota, recovery, and concurrency
  behavior;
- resolve local references to object URLs, text, blobs, or streams;
- support images, video, audio, documents, datasets, archives, code files, and
  unknown binary files;
- provide headless APIs and reusable React UI;
- work offline when all referenced assets are present locally;
- expose a remote provider contract without depending on the unfinished
  dashboard or infrastructure implementation;
- support future remote download caching and upload synchronization;
- remove all final consumers of the current `block-content-editor` asset
  prototype;
- make storage behavior independently testable through contract suites.

## Non-Goals

- implementing or connecting the dashboard assets provider now;
- importing `apps/web` authentication, server actions, API routes, or workspace
  components into the package;
- replacing the backend assets domain or defining its final API contract;
- moving the existing `AssetManager` or `MediaUploadDialog` unchanged;
- storing Lexical documents, quiz definitions, Code Studio trees, or project
  manifests inside the asset repository;
- making assets globally or cross-device available without a remote provider;
- silently publishing browser-local assets;
- implementing Digital Rights Management or promising encrypted local storage;
- owning Code Studio folder and collection semantics;
- owning quiz question or answer semantics;
- using signed or expiring URLs as persisted identifiers.
- reading, converting, or preserving development data from `GGAssetsDB`;
- supporting SHA-1 asset references or experimental serialized asset formats.

## Current State

### Current browser storage prototype

The current implementation under:

```text
apps/web/src/components/block-content-editor/lib/storage/assets
```

uses a hand-written IndexedDB database named `GGAssetsDB`. It stores asset data
as strings, commonly data URLs, generates SHA-1 identifiers, and combines:

- byte storage;
- metadata;
- usage tracking by project and temporary node ID;
- Code Studio collection storage;
- import/export behavior;
- cleanup;
- URL resolution.

The current media upload dialog also owns compression, local library browsing,
URL ingestion, collection selection, IndexedDB writes, and review UI in one
component. This code is a behavior reference only and must not be imported by
the new package.

### Lexical integration

`@game-guild/lexical-surface` currently receives a host-specific media dialog
and URL resolver through `LexicalSurfaceProps.adapters`. The web adapter imports
the current `block-content-editor` dialog and IndexedDB manager.

The new package should eventually replace that arrangement with a shared asset
context and package-owned asset UI. The adapter may remain only until its
consumer is switched in the same implementation sequence; it is not a
compatibility contract.

### Vega-Lite data

Vega-Lite currently stores uploaded CSV and JSON contents in:

```ts
Record<string, string>
```

inside the serialized Lexical node. The chart specification uses virtual
`data:filename.csv` URLs, and rendering replaces those references with inline
values.

The virtual filename behavior can remain, but new file uploads must store an
asset reference instead of embedding the full file content in the Lexical JSON.

### Infrastructure assets

The backend and generated infrastructure client are still in development. The
package must not depend on their current behavior or generated upload methods.
It should define a stable provider-facing boundary that can later be implemented
by a dashboard integration package or application composition layer.

## Architectural Principles

### Bytes are not editor state

Editor documents persist asset references and presentation metadata. They do
not persist file bytes, data URLs, signed URLs, Blob URLs, or provider tokens.

### Stable references, replaceable storage

Consumers must not care whether an asset is in OPFS, an IndexedDB fallback, a
remote provider, or a local download cache. They operate on stable asset URIs
and request a suitable resolved representation.

### Browser-owned data is not cache

An asset imported from the user's device and not uploaded remotely is the only
copy managed by the application. It is durable browser-owned data and must not
be evicted automatically.

A downloaded remote object is a cache entry and may be evicted using an LRU
policy because it can be fetched again.

### Domain packages own their manifests

Assets owns file bodies and generic metadata. Lexical owns document nodes,
Vega-Lite owns chart attachment mappings, Quiz owns question attachments, and
Code Studio owns folders, open tabs, file names, and project manifests.

### Provider absence is explicit

The package must operate locally without a remote provider. UI and APIs must
expose whether an asset is local-only, cached remote, synchronized, pending
upload, unavailable, or failed. They must not imply that local-only data is
portable or published.

### Writes and schema upgrades are recoverable

Browser storage operations can be interrupted by tab closure, quota failure,
or process termination. New-package schema upgrades and object writes must be
idempotent, journaled, and recoverable on the next startup.

## Package Boundaries

### `@game-guild/assets` owns

- asset identifiers, URIs, metadata, and typed errors;
- browser repository orchestration;
- OPFS object storage;
- IndexedDB metadata and fallback object storage;
- local download cache and eviction policy;
- object URL lifecycle management;
- quota and persistence status;
- local import, read, list, search, rename, and delete operations;
- remote provider contracts and capability detection;
- React context, hooks, picker, upload, library, and preview components;
- generic file validation and optional processing pipelines;
- contract tests for browser repositories and future providers.

### `@game-guild/assets` does not own

- application authentication;
- dashboard authorization or server actions;
- backend API clients;
- course, project, team, or tenant access policy;
- Lexical nodes and plugins;
- Vega-Lite parsing and rendering;
- quiz entries and grading;
- Code Studio file trees, tabs, execution, or collections;
- project save formats;
- automatic execution of uploaded HTML, SVG, scripts, or archives.

### Consumers own

- deciding which asset kinds and MIME types are accepted;
- deciding whether local-only references are allowed in a publish operation;
- storing asset URIs in their domain models;
- extracting used URIs and reconciling usage when documents are saved;
- rendering domain-specific error and portability policy where needed;
- mapping a future remote provider's permissions to application resources.

## Target Package Structure

```text
packages/features/assets/
  package.json
  README.md
  tsconfig.json
  vitest.config.ts
  src/
    index.ts
    core/
      asset-contracts.ts
      asset-errors.ts
      asset-kinds.ts
      asset-uri.ts
      file-validation.ts
      mime.ts
      search.ts
    repository/
      asset-repository.ts
      asset-service.ts
      repository-contract-suite.ts
    browser/
      browser-asset-repository.ts
      browser-storage-capabilities.ts
      browser-storage-schema.ts
      metadata-database.ts
      opfs-object-store.ts
      indexeddb-object-store.ts
      storage-journal.ts
      quota-manager.ts
      cache-policy.ts
      object-url-registry.ts
      cross-tab-coordinator.ts
      content-hashing.ts
      schema-upgrades.ts
    providers/
      remote-asset-provider.ts
      provider-capabilities.ts
      provider-contract-suite.ts
      provider-registry.ts
    processing/
      asset-processor.ts
      processing-pipeline.ts
      image-metadata.ts
    react/
      assets-provider.tsx
      use-assets.ts
      use-asset.ts
      use-resolved-asset.ts
      use-storage-status.ts
      upload/
        asset-upload-dialog.tsx
        asset-dropzone.tsx
        upload-review.tsx
      picker/
        asset-picker-dialog.tsx
        asset-library.tsx
        asset-filters.tsx
      preview/
        asset-preview.tsx
        asset-thumbnail.tsx
      status/
        asset-status.tsx
        storage-status.tsx
    testing/
      memory-object-store.ts
      memory-asset-repository.ts
      fake-remote-provider.ts
```

The root export should expose stable contracts and primary React APIs. Browser,
provider, and testing implementations should use explicit subpath exports:

```text
@game-guild/assets
@game-guild/assets/browser
@game-guild/assets/providers
@game-guild/assets/react
@game-guild/assets/testing
```

No wildcard package export should expose arbitrary internals.

## Dependency Rules

The package may depend on:

- React and React DOM for the React entrypoint;
- `@game-guild/ui` for shared UI primitives;
- Lucide for icons;
- `idb` as a small, typed IndexedDB wrapper;
- browser standard APIs such as OPFS, Web Crypto, Web Locks, BroadcastChannel,
  Blob, streams, and object URLs.

The package must not import:

- Next.js;
- `apps/web` or another application;
- `@game-guild/client` until a remote integration package is deliberately
  implemented;
- `@game-guild/lexical-surface`;
- quiz, grading, block-list, or Code Studio code;
- current `block-content-editor` storage or UI.

Consumers depend on assets. Assets does not depend on its consumers.

## Public Contracts

### Asset URI

Use versionable, source-explicit URIs:

```text
asset://local/<uuid>
asset://remote/<provider-key>/<opaque-id>
```

Rules:

- local IDs are UUIDs and never content hashes;
- remote IDs are opaque to consumers;
- URI parsing and construction live in `asset-uri.ts`;
- raw strings must be validated before repository operations;
- signed URLs and Blob URLs are resolved values, never asset URIs;
- the package may later add URI versions without changing domain payloads that
  use the exported `AssetUri` type and parser.

### Asset record

The initial public shape should be equivalent to:

```ts
declare const assetUriBrand: unique symbol;

export type AssetUri = string & { readonly [assetUriBrand]: true };

export type AssetKind =
  | "image"
  | "video"
  | "audio"
  | "document"
  | "dataset"
  | "archive"
  | "code"
  | "other";

export type AssetAvailability =
  | "local-only"
  | "cached-remote"
  | "remote"
  | "pending-upload"
  | "unavailable"
  | "failed";

export interface AssetRecord {
  id: string;
  uri: AssetUri;
  name: string;
  kind: AssetKind;
  mimeType: string;
  size: number;
  contentHash?: string;
  availability: AssetAvailability;
  createdAt: string;
  updatedAt: string;
  lastAccessedAt?: string;
  source?: AssetSource;
  scope?: AssetScope;
  tags?: string[];
}
```

The final implementation should use nominal or opaque construction helpers so
invalid strings cannot be treated as trusted asset URIs accidentally.

### Asset scope

Scopes support local organization and future provider mapping without teaching
the package about courses or projects:

```ts
export interface AssetScope {
  type: string;
  id: string;
}
```

Scope is metadata, not an authorization decision. The future remote provider
owns authorization semantics.

### Repository

The browser implementation and future composed implementation should satisfy a
stable repository contract resembling:

```ts
export interface AssetRepository {
  importFiles(files: readonly File[], options?: AssetImportOptions): Promise<AssetRecord[]>;
  importBlob(blob: Blob, options: AssetImportBlobOptions): Promise<AssetRecord>;
  get(uri: AssetUri): Promise<AssetRecord | null>;
  list(query?: AssetQuery): Promise<AssetPage>;
  readBlob(uri: AssetUri, options?: AssetReadOptions): Promise<Blob>;
  readText(uri: AssetUri, options?: AssetReadTextOptions): Promise<string>;
  createObjectUrl(uri: AssetUri): Promise<ResolvedAssetUrl>;
  rename(uri: AssetUri, name: string): Promise<AssetRecord>;
  remove(uri: AssetUri, options?: AssetRemoveOptions): Promise<void>;
  reconcileUsage(scope: AssetScope, usages: readonly AssetUsageInput[]): Promise<void>;
  getStorageStatus(): Promise<AssetStorageStatus>;
  requestPersistentStorage(): Promise<AssetPersistenceResult>;
}
```

Operations must accept `AbortSignal` where I/O can be long-running.

### Resolved values

Resolution is purpose-specific:

- `readBlob` for binary consumers;
- `readText` for Vega-Lite datasets and Code Studio text files;
- `createObjectUrl` for media elements and downloads;
- stream access for large files when supported;
- metadata-only access for library UI.

Do not reduce every operation to `resolveAssetUrl()`. That contract forces text
consumers to fetch object URLs and makes lifecycle, errors, and caching opaque.

### Remote provider

Define but do not implement a contract similar to:

```ts
export interface RemoteAssetProvider {
  readonly key: string;
  readonly capabilities: RemoteAssetProviderCapabilities;
  upload(files: readonly AssetUploadInput[], context: AssetProviderContext): Promise<RemoteAssetRecord[]>;
  get(uri: AssetUri, context: AssetProviderContext): Promise<RemoteAssetRecord | null>;
  list(query: AssetQuery, context: AssetProviderContext): Promise<AssetPage>;
  download(uri: AssetUri, context: AssetProviderContext): Promise<AssetDownload>;
  delete?(uri: AssetUri, context: AssetProviderContext): Promise<void>;
}
```

Provider context is supplied by the host and may contain scope and opaque
application context. It must not require Next.js, dashboard auth types, or a
specific API client.

Provider capabilities declare optional behavior such as upload, search, delete,
bulk operations, revisions, transforms, and direct download. Consumers must not
assume every provider implements every operation.

## Browser Storage Architecture

### Object bytes

Use Origin Private File System through `navigator.storage.getDirectory()` as
the preferred object store.

Object paths should be content-addressed using SHA-256, for example:

```text
objects/ab/abcdef.../<object-id>
```

The exact physical path is private implementation detail. File names supplied
by users must never become OPFS paths.

Use `Blob`, `ArrayBuffer`, and streams. Do not convert binary files to base64 or
data URLs for persistence.

### Metadata

Use IndexedDB through `idb` for transactional metadata and indexes. The initial
schema should contain stores equivalent to:

- `assets`: logical asset records keyed by local asset ID;
- `objects`: physical object metadata keyed by content ID or hash;
- `usages`: scope and consumer references;
- `remoteCache`: remote URI to local content mapping and LRU metadata;
- `journal`: interrupted operation recovery;
- `settings`: schema version, quota, and persistence state.

Do not store large binary bodies in the metadata database when OPFS is
available.

### Fallback object store

When OPFS is unavailable, use a separate IndexedDB object store containing
`Blob` values. The fallback must implement the same internal object-store
contract and pass the same tests.

Fallback selection must be capability-based and observable through storage
status. It must not silently fall back to data URLs or localStorage.

### Identity and deduplication

- logical assets use random UUIDs;
- content uses SHA-256 for integrity and deduplication;
- multiple logical records may point to one stored object;
- imported names, scopes, and metadata remain independent of deduplicated
  content.

Hashing large files should run outside React rendering and should not block the
main thread. The implementation should use a worker-capable hashing strategy
and avoid loading unnecessarily large files into multiple simultaneous buffers.

### Consistency protocol

OPFS and IndexedDB cannot participate in one transaction. Writes therefore use
a small journal:

1. create a pending operation record;
2. write the object bytes;
3. verify size and content hash;
4. commit metadata and usage records in one IndexedDB transaction;
5. mark the journal entry complete;
6. remove unreferenced temporary or orphan objects during recovery.

Startup recovery must be idempotent. It must never expose a metadata record as
ready before its object is readable.

### Concurrency

- coordinate mutations with Web Locks when available;
- rely on short IndexedDB transactions for metadata consistency;
- use BroadcastChannel to invalidate library and object URL caches across tabs;
- provide a safe fallback when Web Locks or BroadcastChannel is unavailable;
- never keep long asynchronous file reads inside an IndexedDB transaction.

### Object URL lifecycle

Blob URLs are process-local resolved values. An object URL registry should:

- cache URLs per asset/content while actively used;
- reference-count or lease them through React hooks;
- revoke URLs after the final consumer releases them;
- invalidate URLs when content changes or is removed;
- never persist a Blob URL in metadata or editor state.

### Quota and persistence

Use `navigator.storage.estimate()` and `navigator.storage.persist()` when
available.

The package should expose:

- usage and quota estimates;
- whether persistent storage was granted;
- whether the active object store is OPFS or IndexedDB fallback;
- local-only bytes versus evictable cache bytes;
- typed quota errors;
- configurable warning thresholds.

Requesting persistent storage should be tied to a clear user action. The
package must not claim guaranteed durability when the browser has not granted
persistent storage.

### Cache policy

Future downloaded remote assets may use an LRU cache with configurable byte and
entry limits.

Rules:

- browser-owned `local-only` assets are never automatically evicted;
- pending uploads are never automatically evicted;
- only reproducible `cached-remote` objects are eviction candidates;
- active leases and explicitly pinned entries are protected;
- eviction removes cached bytes, not the stable remote asset record;
- access tokens and signed URLs are never persisted in the cache metadata.

## React API and UI

### Provider composition

The package should expose an `AssetsProvider` for repository access. The first
application integration supplies the browser repository only:

```tsx
<AssetsProvider repository={browserAssetRepository}>
  {children}
</AssetsProvider>
```

A future composition can add a remote provider without changing consumers:

```tsx
<AssetsProvider
  repository={composedAssetRepository}
  remoteProvider={dashboardAssetProvider}
>
  {children}
</AssetsProvider>
```

No dashboard provider is implemented in this plan.

### Hooks

Initial hooks should include:

- `useAssets()` for repository commands;
- `useAsset(uri)` for metadata and availability;
- `useResolvedAsset(uri, purpose)` for leased object URLs, text, or blobs;
- `useAssetLibrary(query)` for paginated local browsing;
- `useAssetUpload()` for import state and cancellation;
- `useAssetStorageStatus()` for capability, quota, persistence, and recovery
  state.

Hooks must expose loading and typed error states. They must not use alerts or
console logging as their error contract.

### Picker and upload UI

Build a reusable asset picker rather than moving the existing media dialog.

The picker should support:

- local library browsing;
- search, kind, MIME, scope, and recency filters;
- single or multiple selection;
- file upload and drag/drop;
- accepted type and size constraints supplied by the consumer;
- progress, cancellation, validation failures, and quota failures;
- preview appropriate to asset kind;
- explicit local-only status;
- optional external URL entry controlled by the consumer;
- future remote library results through provider capabilities.

The UI should use package-neutral terms such as asset, file, and library. A
consumer may supply labels appropriate to media, datasets, attachments, or
project files.

### Processing pipeline

Asset processing is an extension pipeline before persistence or remote upload:

```ts
export interface AssetProcessor {
  supports(input: AssetProcessingInput): boolean;
  process(input: AssetProcessingInput, context: AssetProcessingContext): Promise<AssetProcessingResult>;
}
```

The storage core does not assume compression. Image metadata extraction and
optional image optimization may be package-provided processors. Code Studio,
Vega-Lite, or application-specific processors remain consumer supplied.

Do not copy the current compression dialogs and WebP converter directly. Their
behavior should be reviewed and rebuilt against the processing contract.

## Consumer Integration

### Lexical Surface

Target behavior:

- `@game-guild/lexical-surface` depends on the public assets React API;
- media insertion opens the package asset picker;
- media nodes store stable asset URIs plus presentation metadata;
- read-only rendering resolves through `useResolvedAsset`;
- external URLs can remain direct URLs when the user explicitly chooses not to
  import them;
- the current media dialog and asset resolver adapters are removed when the
  package integration lands;
- development fixtures and saved examples are rewritten to the new node shape.

The asset package must not depend on Lexical.

### Vega-Lite

Keep virtual filenames in the chart specification for authoring ergonomics:

```json
{
  "data": { "url": "data:sales.csv" }
}
```

Replace embedded file bodies with attachment records:

```ts
export interface VegaDataAttachment {
  name: string;
  assetUri: AssetUri;
  mimeType: "text/csv" | "application/json";
  size: number;
}

export interface VegaLiteData {
  // existing chart properties
  attachments?: Record<string, VegaDataAttachment>;
}
```

Rendering becomes asynchronous before the existing CSV/JSON inline transform:

1. find referenced virtual filenames;
2. resolve attachment URIs through `readText`;
3. validate JSON or parse CSV;
4. produce a temporary inline Vega-Lite specification;
5. keep asset bodies out of the serialized Lexical node.

The current embedded `data` map is removed. Development fixtures and examples
must be recreated with `attachments`; the package does not carry a dual-format
reader.

The Vega data dialog must first receive the same editor-layer portal treatment
as the Mermaid/Vega select menus. This is a UI layering fix independent of the
asset package.

### Quiz

Quiz remains the owner of question, answer, grading, and learner-safe payloads.
It may use asset URIs for:

- question media;
- downloadable source documents;
- answer attachments;
- feedback media.

Author-only assets and learner-visible assets must remain distinguishable in
quiz contracts so learner-safe redaction does not expose private attachments.
The assets package does not decide that policy.

### Code Studio

Code Studio remains the owner of:

- file and folder trees;
- paths and names;
- open tabs and editor state;
- project manifests;
- execution environments;
- save checkpoints and collections.

Assets may store immutable file bodies. Code Studio manifests reference asset
URIs. Unsaved text remains in editor memory; saving a changed file writes a new
asset object or logical revision and updates the manifest.

Do not move the current `collection://` system into assets. A future Code Studio
package should replace collection and tree semantics while using assets only
for file bodies.

Binary and large files should always use asset references. A later Code Studio
decision may keep small text inline in its own manifest, but that policy does
not belong in the assets package.

### Other consumers

Consumers such as assignments, support attachments, portfolio projects, and
document editors should use the same repository and picker contracts instead
of creating new file inputs tied to separate persistence implementations.

## Clean Cutover

No data conversion layer will be built.

- `@game-guild/assets` uses a new database name and storage namespace;
- it never opens or reads `GGAssetsDB`;
- current development assets may be cleared manually;
- test fixtures, seeds, and example documents are updated to use the new URI
  and attachment contracts;
- each consumer removes its current asset writes as the new integration lands;
- after the final consumer switches, the old manager, dialogs, hooks, and
  IndexedDB initialization are deleted outright;
- temporary dual writes and dual readers are prohibited.

This keeps unreleased prototype decisions out of the public package and makes
the new contracts the only supported starting point.

## Portability and Synchronization

Until a remote provider is connected, `asset://local/...` references work only
in the same browser origin and profile.

The package must make this limitation observable:

- local-only badges in library and picker UI;
- an API to list local-only assets used by a scope;
- `checkPortability(uris)` or equivalent before publish/export;
- typed unresolved errors in read-only rendering;
- export/import support for local asset bundles when needed;
- no silent conversion of a local URI into a provider URI.

When a future remote provider uploads a local asset:

1. upload bytes and metadata;
2. receive a remote asset URI;
3. record local-to-remote mapping;
4. preserve local bytes as a cache according to policy;
5. let the owning consumer rewrite its persisted document or manifest;
6. do not mutate serialized documents invisibly from the asset repository.

Synchronization conflict behavior belongs to the remote integration phase and
must be capability-driven.

## Security and Safety

- normalize display names but never use them as storage paths;
- reject empty files and enforce consumer-configured size limits;
- validate declared MIME type and retain detected-type warnings;
- treat SVG, HTML, scripts, archives, and unknown binaries as untrusted;
- do not execute or inject uploaded text as markup;
- render SVG through safe image paths unless a consumer explicitly sanitizes
  inline SVG;
- avoid storing credentials, auth headers, provider tokens, signed URLs, or
  cookies in IndexedDB or OPFS metadata;
- revoke object URLs;
- use typed errors for corrupt, missing, blocked, quota, aborted, and unsupported
  operations;
- prevent path traversal in exported bundles and Code Studio integrations;
- require explicit user intent before importing an external URL into local
  storage;
- cap concurrent hashing, decoding, thumbnail, and upload work;
- avoid reading large files into duplicate buffers;
- preserve browser isolation assumptions without claiming encryption at rest.

## Observability

Expose optional event callbacks or an event subscriber for:

- import started/completed/failed;
- object read failures;
- quota pressure;
- persistence grant result;
- cache eviction;
- recovery and schema upgrade failures;
- missing or unresolved references;
- future provider upload/download failures.

The package should not depend on a concrete analytics service. Hosts may bridge
events to their own telemetry.

## Implementation Phases

### Phase 0: Fix the Current Vega Data Dialog Layer

This is independent of package extraction and should land first.

Tasks:

1. Add a shared feature-editor dialog wrapper with overlay and content above the
   `FeatureEditorShell` layer.
2. Use it in the Vega-Lite data manager and any nested feature-editor dialogs
   with the same problem.
3. Add a regression test for opening and interacting with the dialog.

Acceptance criteria:

- the Data dialog opens above the Vega editor;
- file input, buttons, focus, escape, and outside-click behavior work;
- no global UI dialog z-index is changed for unrelated application dialogs.

### Phase 1: Scaffold and Stabilize Core Contracts

Tasks:

1. Create package metadata, TypeScript config, Vitest config, README, and
   explicit exports.
2. Implement local and remote asset URI parsing and construction.
3. Define asset kinds, records, scopes, queries, storage status, resolved
   values, and typed errors.
4. Define repository and remote provider contracts.
5. Add a memory repository and exported repository/provider contract suites.
6. Document local-only portability behavior.

Acceptance criteria:

- core contracts contain no React, Next.js, Lexical, Quiz, or Code Studio
  imports;
- invalid URIs are rejected deterministically;
- local and remote URI forms are distinguishable;
- memory repository passes the repository contract suite;
- package typecheck and tests pass.

### Phase 2: Implement the Browser Repository

Tasks:

1. Add typed IndexedDB metadata schema using `idb`.
2. Implement OPFS object storage.
3. Implement Blob-in-IndexedDB fallback storage.
4. Implement SHA-256 content identity and logical UUID asset identity.
5. Add staged write journal and startup recovery.
6. Add import, metadata, list, search, rename, read, and remove operations.
7. Add usage reconciliation.
8. Add object URL registry.
9. Add cross-tab invalidation and mutation coordination.
10. Add quota, persistence, and storage capability reporting.
11. Add remote-cache metadata and LRU policy without a remote provider.

Acceptance criteria:

- imported assets survive reload;
- persisted binary files are not base64 strings;
- duplicate bytes share physical content while preserving logical records;
- interrupted writes recover without exposing broken records;
- local-only assets are never cache-evicted;
- OPFS and fallback implementations pass the same contract tests;
- object URLs are revoked after release;
- quota errors do not corrupt metadata.

### Phase 3: Build React Integration and UI

Tasks:

1. Add `AssetsProvider` and repository hooks.
2. Add storage status and local-only status UI.
3. Build the asset library with search and filters.
4. Build upload/dropzone UI with progress, cancellation, validation, and review.
5. Build single and multiple asset picker modes.
6. Build image, audio, video, text/document, and generic file previews.
7. Add configurable acceptance rules and labels.
8. Add optional processor pipeline support.
9. Ensure all dialogs work inside existing editor shells and ordinary pages.

Acceptance criteria:

- UI works with the browser repository only;
- no dashboard or infrastructure imports exist;
- keyboard, focus, and screen-reader behavior are covered;
- long names, large libraries, empty states, errors, and quota states are usable;
- consumers can use headless APIs without importing React UI.

### Phase 4: Integrate Lexical Media

Tasks:

1. Add `@game-guild/assets` as a Lexical Surface dependency.
2. Replace the package's host media dialog with the shared asset picker.
3. Resolve media through leased object URLs.
4. Preserve direct external URLs where supported.
5. Add local-only and unresolved rendering states.
6. Update fixtures and development documents to the new node shape.
7. Remove the media upload and resolver fields from
   `LexicalSurfaceProps.adapters`.
8. Update editor and read-only consumers to mount `AssetsProvider`.

Acceptance criteria:

- new media insertion does not import `block-content-editor`;
- media persists across reload in the same browser;
- media nodes store stable URIs, not data URLs or Blob URLs;
- preview and read-only rendering use the same resolution path;
- no alternate media persistence path remains in Lexical Surface.

### Phase 5: Integrate Vega-Lite Attachments

Tasks:

1. Replace the embedded `data` model with `attachments`.
2. Use the asset picker constrained to CSV and JSON.
3. Store attachment metadata and stable asset URIs.
4. Resolve attachment text asynchronously before chart compilation.
5. Preserve `data:filename` authoring references.
6. Add missing, invalid, too-large, and unavailable attachment states.
7. Update Vega fixtures, templates, and development documents.
8. Cover editor, saved node, preview, and read-only rendering.

Acceptance criteria:

- new CSV/JSON uploads are not embedded in Lexical JSON;
- charts render after reload with browser-local attachments;
- missing attachments produce actionable errors;
- no embedded dataset write or dual-format reader remains;
- selecting and deleting attachments updates usage safely.

### Phase 6: Prepare Quiz and Code Studio Consumers

This phase can proceed as those features become packages.

Tasks:

1. Define quiz attachment fields using `AssetUri` without importing storage
   implementation details.
2. Keep learner-visible and author-only attachment metadata separate.
3. Define Code Studio manifest references to asset bodies.
4. Keep Code Studio paths, folders, collections, and working buffers outside
   assets.
5. Replace current Code Studio member asset and collection persistence.
6. Replace direct `assetManager` use in file explorer and save operations.
7. Replace remaining media/upload consumers in `block-content-editor`.

Acceptance criteria:

- Quiz and Code Studio do not import the current asset manager;
- asset bodies are available through the shared repository;
- domain manifests remain owned by Quiz and Code Studio;
- learner redaction cannot leak author-only attachment references;
- Code Studio does not use `collection://` as an asset storage mechanism.

### Phase 7: Remote Provider Readiness

Do not implement the dashboard provider in this plan. Prepare and verify the
boundary only.

Tasks:

1. Run provider contract tests against the fake provider.
2. Verify upload, download, list, cache, abort, retry, and mapping flows.
3. Verify that expiring URLs are never persisted.
4. Add local-to-remote mapping without automatic document mutation.
5. Document the host integration responsibilities.
6. Define the later infrastructure connection checklist.

Acceptance criteria:

- a provider can be added without changing consumer domain models;
- provider absence remains fully supported;
- downloaded remote assets can use the browser cache;
- local-only assets remain distinguishable after provider composition;
- the package has no dependency on unfinished dashboard code.

### Phase 8: Remove the Current Prototype

Tasks:

1. Confirm all consumers import `@game-guild/assets`.
2. Remove the current media upload and local asset UI.
3. Remove the hand-written `AssetManager` and current resolution hooks.
4. Remove prototype write paths from project import/export and manager pages.
5. Remove `GGAssetsDB` initialization and all SHA-1 asset URI generation.
6. Update or delete stale tests, fixtures, and development seed data.

Acceptance criteria:

- runtime search finds no imports of the current asset manager;
- `block-content-editor` no longer owns media or asset persistence;
- runtime code does not open `GGAssetsDB` or construct SHA-1 asset URIs;
- no compatibility reader, dual write, or conversion layer remains;
- package and consumer tests pass.

## Testing Strategy

### Pure unit tests

- URI parsing and construction;
- kind and MIME classification;
- validation and typed errors;
- query and filter behavior;
- cache selection and eviction;
- metadata schema upgrade behavior;
- portability checks.

### Repository contract tests

Every repository implementation must pass the same suite covering:

- import and read roundtrip;
- text and binary content;
- duplicate object deduplication;
- independent logical metadata;
- rename and delete;
- usage reconciliation;
- abort behavior;
- missing and corrupt data;
- local-only eviction protection;
- object URL lifecycle.

### Browser integration tests

Use real browser tests for behavior not represented faithfully by JSDOM:

- OPFS writes and reads;
- reload persistence;
- IndexedDB fallback;
- multi-tab invalidation;
- object URL rendering;
- drag/drop and file inputs;
- storage persistence request;
- interrupted write recovery;
- schema upgrade from a seeded earlier package schema.

The package unit suite may use `fake-indexeddb`, but real browser coverage is
required before browser persistence is considered complete.

### Consumer tests

- Lexical media insertion, serialization, reload, and read-only rendering;
- Vega CSV and JSON attachment insertion and chart rendering;
- unresolved and local-only states;
- quiz learner-safe attachment projection;
- Code Studio file manifest and binary content resolution;
- fixtures and development documents using only the new asset contracts.

## Package-Level Acceptance Criteria

The package is ready for initial use when:

- it has no application, Next.js, dashboard, Lexical, Quiz, or Code Studio
  imports;
- browser-only usage works without a remote provider;
- OPFS is preferred and Blob-in-IndexedDB fallback works;
- no persisted object uses base64 or data URLs;
- local assets survive reload and resolve offline;
- local-only assets are never automatically evicted;
- downloaded remote cache entries are structurally supported but provider-free;
- quota, persistence, recovery, and unsupported-browser states are explicit;
- object URLs have controlled lifecycles;
- stable asset URIs are the only file references stored by new consumers;
- repository and real browser integration tests pass;
- remote provider contracts are covered by an exported contract suite;
- the package contains no reader or converter for the current prototype data.

## Final Completion Criteria

The broader cutover is complete when:

- Lexical media and Vega-Lite use `@game-guild/assets`;
- Quiz and Code Studio use the same package after their extraction;
- the web app no longer imports the current media dialog or asset manager;
- new editor documents contain no uploaded file bodies;
- SHA-1 asset references and `GGAssetsDB` are absent from runtime code;
- local-only portability is enforced before workflows that require shared or
  published assets;
- the future dashboard provider can be connected through the provider contract
  without redesigning consumer data models;
- `block-content-editor` no longer owns an asset persistence system.

## Deferred Remote Integration Checklist

When the dashboard and infrastructure assets implementation is ready, create a
separate integration plan that covers:

- provider package or app composition ownership;
- authenticated upload transport;
- parent resource scopes and authorization;
- remote metadata mapping;
- stable remote asset URI format;
- upload retry and resumability;
- local-to-remote promotion;
- signed URL renewal without persistence;
- remote revision behavior;
- deletion and retention policy;
- learner and public rendering access;
- synchronization of browser-local assets already referenced by saved
  documents.

That work must implement the contracts defined here rather than adding
dashboard-specific branches to Lexical, Vega-Lite, Quiz, or Code Studio.
