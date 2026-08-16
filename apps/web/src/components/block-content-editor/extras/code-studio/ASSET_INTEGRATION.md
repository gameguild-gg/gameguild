# Code Studio asset integration

Code Studio owns file trees, paths, open tabs, working buffers, and
collections. `@game-guild/assets` owns immutable file bodies.

Saved `CodeFile.content` values may contain a complete stable URI such as:

```text
asset://7776453f-1123-4f56-8abc-1234567890ab
```

The `assetId` field currently carries that same complete URI. It is retained as
a Code Studio manifest field, not as an identifier for a separate storage
system. File reads use `AssetRepository.readText`; changed buffers are imported
as new blobs when saved and the manifest is updated to their new URI.

Collections are persisted separately in the Code Studio IndexedDB database.
They reference asset URIs for file bodies and never use `collection://` or
store base64/data URLs. This keeps collection and folder semantics outside the
assets package while allowing both domains to share durable byte storage.

The file explorer opens `AssetPickerDialog` directly for existing and newly
uploaded files. The app does not initialize or read the removed `GGAssetsDB`
prototype.
