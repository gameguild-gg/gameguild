# `@game-guild/assets`

Browser-first asset persistence and reusable React asset selection for
GameGuild.

## Storage model

The package uses IndexedDB as its only local storage backend. Logical asset
metadata, deduplicated Blob objects, and usages live in one database and can
participate in the same transactions.

Every document reference uses the source-neutral form `asset://<uuid>`. The
record's `location` says whether the asset is browser-local, provider-owned, or
an external reference. Moving an asset to a provider never changes its URI or
requires rewriting a document.

The UUID identifies a logical asset. SHA-256 identifies its physical bytes for
deduplication and must not be persisted in documents as the asset identity.

Browser-local assets are durable authoring data, not cache entries. The package
does not use OPFS, Cache Storage, data URLs, persisted Blob URLs, signed URLs,
or a remote download cache.

The browser repository accepts local assets up to 64 MiB by default. Hosts can
set a lower `maxLocalBytes` or mark kinds as `remoteOnlyKinds`. Files rejected
by that policy must be uploaded through a remote provider instead of being
silently retained in another browser backend.

## Remote assets

Remote providers look up stable logical IDs and return records whose provider
location contains only provider identifiers. Preview and playback call
`resolveUrl` with that metadata and receive an ephemeral URL that is never
written to a document or IndexedDB. This allows large video and media files to
stream without downloading them into browser storage.

External references such as a YouTube URL are also imported as logical assets.
Their exact reference is stored in asset metadata, while the consuming document
keeps only `asset://<uuid>`. A provider for the corresponding `providerKey`
interprets and resolves that opaque reference.

`readBlob`, `readStream`, and `readText` perform an explicit provider download.
They do not cache the response. This supports consumers such as Vega-Lite that
need the actual bytes while keeping ordinary media preview lightweight.

## Entrypoints

- `@game-guild/assets`: contracts and repository types
- `@game-guild/assets/browser`: IndexedDB browser repository
- `@game-guild/assets/providers`: remote provider contracts and composition
- `@game-guild/assets/processing`: optional pre-persistence processing
- `@game-guild/assets/react`: provider, hooks, picker, and upload UI
- `@game-guild/assets/testing`: in-memory repository and fake provider

Use `checkPortability` before transports that cannot bundle local bytes, and
call `reconcileUsage` whenever an owning document or manifest is saved. Provider
integrations are supplied by the host; this package does not import dashboard,
authentication, infrastructure, or provider-specific clients.
