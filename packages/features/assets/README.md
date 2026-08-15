# `@game-guild/assets`

Browser-first asset storage and reusable React asset selection for GameGuild.

The package stores file bytes in OPFS when available, uses IndexedDB for typed
metadata, and falls back to Blob values in IndexedDB. It works without a remote
provider and never persists data URLs, Blob URLs, signed URLs, or credentials.

## Entrypoints

- `@game-guild/assets`: contracts and repository types
- `@game-guild/assets/browser`: browser repository
- `@game-guild/assets/providers`: future remote provider contracts
- `@game-guild/assets/processing`: optional pre-persistence processing pipeline
- `@game-guild/assets/react`: provider, hooks, picker, and upload UI
- `@game-guild/assets/testing`: in-memory test implementation

Local assets use `asset://local/<uuid>`. Remote providers will use
`asset://remote/<provider>/<opaque-id>`.

Browser-local assets are durable authoring data, not an evictable cache. Use
`checkPortability` before transports that cannot bundle local bytes, and
`reconcileUsage` whenever an owning document or manifest is saved. ZIP-like
transports should store raw files plus a manifest and rewrite stable local URIs
when imported.

Remote integrations register against `RemoteAssetProvider`; no dashboard,
authentication, or infrastructure client is imported by this package. The
provider must return stable remote asset URIs and must never persist signed or
expiring download URLs in consumer documents.

`ComposedAssetRepository` can combine the browser repository with a provider
registry. Remote downloads enter a bounded, evictable LRU cache; browser-owned
local assets and active object URL leases are never cache-eviction candidates.
Hosts can subscribe to repository events without coupling the package to an
analytics implementation.
