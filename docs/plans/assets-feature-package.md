# Assets Feature Package Plan

Status: implemented.

## Summary

Create a reusable asset domain package at:

```text
packages/features/assets
```

with package name `@game-guild/assets`.

The package owns stable asset references, local browser persistence, usage
tracking, remote provider contracts, React integration, and reusable asset
selection UI. It serves Lexical Surface, Vega-Lite, Quiz, Code Studio, and
future consumers without depending on `apps/web` or infrastructure clients.

The product has not launched. There is no legacy asset data or browser schema
to migrate. Schema changes may reset development data instead of adding
compatibility code.

## Decisions

### One local backend

IndexedDB is the only local storage backend.

The same database stores:

- logical asset records;
- deduplicated Blob objects;
- usage references.

Blob objects and metadata participate in the same IndexedDB transactions. The
package does not use OPFS, Cache Storage, a fallback backend, a write journal,
or a remote byte cache.

### Local files are authoring data

Locally imported files use the source-neutral `asset://<uuid>` form and are
never automatically evicted. SHA-256 identifies physical content for
deduplication, while each logical import receives its own URI, name, MIME type,
scope, and tags.

The browser repository has a configurable local size policy. Its default limit
is 64 MiB. Hosts may lower the limit or declare kinds such as video to be
remote-only. An asset outside the policy is rejected with an explicit error.

### Large files remain remote

Provider-owned and external assets use the same `asset://<uuid>` form as local
assets. Documents persist only this stable logical ID, never a provider key,
provider object ID, direct URL, signed URL, data URL, or Blob URL. Asset
metadata owns the current location:

- `local` for browser-owned bytes;
- `provider` with a provider key and optional provider object ID;
- `external` with a provider key and opaque reference, such as a YouTube URL.

Uploading a local asset preserves its logical ID and URI. It promotes the
record's location to `provider`, so documents do not need reference rewrites.

For preview and playback, the provider resolves the stable URI to an ephemeral
URL. Large videos can therefore stream from the provider without being copied
into browser storage.

Byte-oriented methods are explicit:

- `createObjectUrl` resolves an ephemeral provider URL;
- `readBlob`, `readStream`, and `readText` download bytes when requested;
- downloaded remote bytes are not cached by the package.

The object table is separate from metadata and can later retain small remote
objects as a cache without changing the public identity model. Automatic remote
download caching is not part of the current implementation.

### Providers remain host-owned

The package defines `RemoteAssetProvider`, but does not implement dashboard or
infrastructure integration. A host supplies authentication, authorization,
tenant context, upload endpoints, and URL resolution.

Remote deletion must honor the same usage protection as local deletion. The
caller must explicitly pass `force` to delete a referenced asset.

## Goals

- use one source-neutral asset URI format across consuming domains;
- keep file bodies out of editor documents and project manifests;
- persist local Blob data without base64 encoding;
- make logical metadata and physical object updates atomic;
- deduplicate identical bytes without merging logical identities;
- preserve MIME type per logical asset;
- track usages and prevent accidental deletion of referenced assets;
- support local object URLs, text, Blob, and stream reads;
- stream large remote media through ephemeral provider URLs;
- expose independently testable headless and React APIs;
- work without a configured remote provider;
- keep the package independent from applications and infrastructure clients.

## Non-goals

- preserving experimental or development browser data;
- supporting SHA-1 or previous asset reference formats;
- implementing dashboard authentication or backend clients;
- caching remote downloads;
- storing large video locally by default;
- owning Lexical nodes, Quiz entries, Code Studio trees, or project manifests;
- persisting signed URLs, provider tokens, data URLs, or Blob URLs;
- executing uploaded HTML, scripts, SVG, or archives;
- implementing DRM or encrypted local storage.

## Public references

```text
asset://<uuid>
```

UUIDs identify logical records regardless of storage location. Content hashes
remain internal physical object identifiers and must not be used as document
references.

## Package structure

```text
packages/features/assets/
  src/
    core/          asset contracts, URIs, validation, MIME classification
    browser/       IndexedDB schema, hashing, repository, object URL leases
    repository/    repository contract and provider composition
    providers/     remote provider contract and registry
    processing/    optional import processing pipeline
    react/         context, hooks, picker, preview, storage status
    testing/       memory repository, fake provider, contract suites
```

Public entrypoints remain explicit:

```text
@game-guild/assets
@game-guild/assets/browser
@game-guild/assets/providers
@game-guild/assets/processing
@game-guild/assets/react
@game-guild/assets/testing
```

## IndexedDB schema

Database: `game-guild-assets`

### `assets`

Logical records keyed by UUID. Records contain stable URI, location, optional
object hash, name, MIME type, kind, size, scope, source, tags, and timestamps.

### `objects`

Physical objects keyed by SHA-256. Each record contains the Blob, size,
reference count, and creation timestamp.

### `usages`

References keyed by a serialized tuple of scope, consumer, and asset URI.
Scope keys are serialized tuples rather than delimiter-concatenated strings.

## Transaction rules

- Hash computation occurs before opening a write transaction.
- Import inserts the logical record and creates or increments its physical
  object in one `readwrite` transaction.
- Removal deletes the logical record and decrements or deletes its object in
  one transaction.
- Usage reconciliation replaces all usages for a scope in one transaction.
- Mutations use strict durability hints where data loss matters.
- Correctness relies on IndexedDB transaction scheduling, not optional Web
  Locks or cross-tab in-memory mutexes.

## Remote provider contract

A provider supports capabilities for ID lookup, upload, list, download, URL
resolution, and optional deletion. Downloads, URL resolution, and deletion
receive the resolved record, so routing depends on metadata rather than parsing
the public URI. Uploads receive the existing logical ID and must preserve it.

```ts
interface RemoteAssetProvider {
  readonly key: string;
  readonly capabilities: RemoteAssetProviderCapabilities;

  upload(...): Promise<AssetRecord[]>;
  get(...): Promise<AssetRecord | null>;
  list(...): Promise<AssetPage>;
  download(...): Promise<AssetDownload>;
  resolveUrl(...): Promise<ResolvedAssetUrl>;
  delete?(...): Promise<void>;
}
```

Provider results are validated against the requested logical URI, ID, and the
provider key declared by their metadata location.
Explicit downloads validate size and, when supplied, SHA-256 content hash.

## React behavior

`AssetsProvider` exposes a repository and optional import processors. The
package supplies hooks for records, lists, storage status, URL resolution, and
uploads.

`AssetPickerDialog` supports upload, drag and drop, validation, filtering,
selection, and local-storage status. Consumers own accepted MIME types, kinds,
and feature-specific size limits.

`AssetPreview` resolves local object URLs through leases and remote URLs through
the provider. URLs are released when the component unmounts or changes asset.

## Portability

Records with a local location are not portable unless a transport bundles their
bytes. Import restores the bundled bytes under the same logical UUID; no
document rewrite is required. Provider and external records are portable when
their provider is registered and still resolves the referenced asset.

Bundle manifests include the physical SHA-256. Import verifies the bytes before
restoring the UUID and rejects an existing logical ID whose content hash does
not match, preventing silent identity collisions.

Consumers must call `checkPortability` before publishing or using transports
that cannot include local bytes.

## Verification

The package test suite covers:

- URI parsing and rejection of prototype references;
- file validation and structured URI discovery;
- processing pipeline order;
- memory repository contract;
- IndexedDB persistence across repository instances;
- concurrent deduplicated imports and reference counts;
- per-logical-asset MIME preservation;
- collision-free usage scopes;
- remote URL resolution without download or local caching;
- explicit remote byte download;
- remote deletion protection through usages;
- provider upload, list, download, URL resolution, and deletion.

TypeScript typechecking and package tests must pass before integration changes
are accepted.
