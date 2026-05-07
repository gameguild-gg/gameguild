# Asset Storage System

## Overview

The Asset Storage System provides centralized management for media files (images, videos, etc.) in the GameGuild Lexical Editor. Instead of embedding media files directly in documents as data URLs, assets are stored separately in IndexedDB with an index tracking their usage across projects and nodes.

## Architecture

### Components

1. **AssetManager** (`asset-manager.ts`)
   - Core class that handles all asset operations
   - Manages IndexedDB storage for assets
   - Maintains an asset index (similar to `assets.json`)
   - Generates SHA1 hashes for deduplication
   - Tracks asset usage across projects and nodes

2. **Types** (`types.ts`)
   - `AssetMetadata`: Metadata for each asset (name, origin, author, license, hash, usage tracking)
   - `AssetIndex`: Index structure containing all asset metadata
   - `AssetData`: Combined metadata and actual file data
   - `SaveAssetParams`: Parameters for saving assets
   - `SaveAssetResult`: Result of save operations

3. **Utility Functions** (`index.ts`)
   - `resolveAssetUrl()`: Converts asset URLs to data URLs
   - `isAssetUrl()`: Checks if a URL is an asset URL
   - `getAssetIdFromUrl()`: Extracts asset ID from URL

### Storage Structure

Assets are stored in IndexedDB with two object stores:

1. **`assets`** - Stores actual asset data
   ```typescript
   {
     metadata: AssetMetadata,
     data: string // Data URL or blob
   }
   ```

2. **`asset_index`** - Stores the asset index
   ```typescript
   {
     version: "1.0",
     lastUpdated: "2023-...",
     assets: {
       [sha1hash]: AssetMetadata
     }
   }
   ```

## Usage Flow

### 1. Uploading Media

When a user uploads media through `MediaUploadDialog`:

```typescript
// User selects a file
const file = event.target.files[0]

// Optional: Compress the file
const compressed = await WebPConverter.convertToWebP(file, settings)

// Save to asset storage
const result = await assetManager.saveAsset({
  file: file,
  dataUrl: compressed.dataUrl, // If compressed
  projectId: "project-123",
  nodeId: "node-456",
  author: "user@example.com",
  license: "user-uploaded"
})

// Returns: { success: true, assetId: "abc123...", assetUrl: "asset://abc123..." }
```

### 2. Using Assets in Nodes

Nodes receive asset URLs instead of data URLs:

```typescript
interface MediaUploadResult {
  type: "file" | "url"
  data: string // "asset://abc123..." instead of "data:image/png;base64,..."
  assetId?: string
  name?: string
  size?: number
}
```

To display the asset, resolve the URL:

```typescript
import { resolveAssetUrl } from "@/components/block-content-editor/lib/storage/assets"

const dataUrl = await resolveAssetUrl("asset://abc123...")
// Use dataUrl in img src, etc.
```

### 3. Asset Deduplication

The system automatically deduplicates assets using SHA1 hashes:

- Same file uploaded multiple times = stored once
- Different nodes can reference the same asset
- Usage is tracked per project/node

### 4. Usage Tracking

Each asset tracks which projects and nodes use it:

```typescript
{
  id: "abc123...",
  name: "image.png",
  sha1hash: "abc123...",
  usedBy: [
    {
      projectId: "project-123",
      projectName: "My Project",
      nodeIds: ["node-456", "node-789"]
    }
  ]
}
```

### 5. Cleanup

Remove unused assets:

```typescript
// Cleanup assets with no references
const deletedCount = await assetManager.cleanupUnusedAssets()

// Delete specific asset usage
await assetManager.deleteAsset(assetId, projectId, nodeId)
```

## API Reference

### AssetManager

#### `init(): Promise<void>`
Initialize the AssetManager and IndexedDB.

#### `saveAsset(params: SaveAssetParams): Promise<SaveAssetResult>`
Save an asset to storage.

**Parameters:**
```typescript
{
  file?: File,              // File object from input
  dataUrl?: string,         // Data URL (for compressed files)
  urlSource?: string,       // External URL
  projectId?: string,       // Project using this asset
  nodeId?: string,          // Node using this asset
  author?: string,          // Author information
  license?: string          // License information
}
```

**Returns:**
```typescript
{
  success: boolean,
  assetId?: string,         // SHA1 hash of the asset
  assetUrl?: string,        // "asset://hash"
  error?: string,
  metadata?: AssetMetadata
}
```

#### `getAsset(assetId: string): Promise<AssetData | null>`
Get complete asset data including metadata and file data.

#### `getAssetUrl(assetId: string): Promise<string | null>`
Get the data URL for an asset.

#### `deleteAsset(assetId: string, projectId?: string, nodeId?: string): Promise<boolean>`
Delete an asset or remove specific usage.

#### `listAssets(): Promise<AssetMetadata[]>`
List all assets in storage.

#### `getAssetsForProject(projectId: string): Promise<AssetMetadata[]>`
Get all assets used by a specific project.

#### `cleanupUnusedAssets(): Promise<number>`
Remove assets with no usage references. Returns count of deleted assets.

#### `getStats(): Promise<Stats>`
Get storage statistics:
```typescript
{
  totalAssets: number,
  totalSize: number,
  usedAssets: number,
  unusedAssets: number
}
```

#### `exportProjectAssets(projectId: string): Promise<Record<string, AssetData>>`
Export all assets used by a specific project for backup/transfer.

**Returns:** Map of assetId → AssetData

#### `exportProjectAssetIndex(projectId: string): Promise<Record<string, AssetUsage[]>>`
Export usage index for a specific project.

**Returns:** Map of assetId → AssetUsage[] (filtered for project)

#### `importProjectAssets(assets, assetIndex, targetProjectId): Promise<ImportResult>`
Import assets from exported data into the current storage.

**Parameters:**
- `assets`: Record<string, AssetData> - Exported assets
- `assetIndex`: Record<string, AssetUsage[]> - Exported usage index
- `targetProjectId`: string - ID of project importing the assets

**Returns:**
```typescript
{
  imported: number,  // New assets added
  updated: number,   // Existing assets updated
  skipped: number    // Assets that failed to import
}
```

#### `removeProjectFromAssets(projectId: string): Promise<number>`
Remove all usage tracking for a project (when project is deleted).
Returns count of assets affected
}
```

## Integration with MediaUploadDialog

The `MediaUploadDialog` component has been updated to:

1. **Save files to assets** instead of returning data URLs directly
2. **Process files sequentially** for safety
3. **Return asset URLs** (`asset://hash`) to consuming nodes
4. **Support compression** before saving

Example usage:

```typescript
<MediaUploadDialog
  open={isOpen}
  onOpenChange={setIsOpen}
  onMediaSelected={(results) => {
    // results contain asset URLs: "asset://abc123..."
    // Nodes receive these URLs and can resolve them when needed
  }}
  compress={true}
  allowCompressionToggle={true}
/>
```

## Benefits

1. **Deduplication**: Same file stored once, referenced multiple times
2. **Usage Tracking**: Know which projects/nodes use each asset
3. **Centralized Management**: Single source of truth for all assets
4. **Space Efficiency**: Remove unused assets, compress before storage
5. **Performance**: Load assets on-demand instead of embedding in documents
6. **Export/Import Support**: Assets are included when exporting/importing projects
7. **Migration Ready**: Easy to migrate to cloud storage in the future

## Project Export/Import

### Export Structure

When exporting a project, assets are included in the ZIP file:

```
projeto-abc123/
├── index.json           # Project metadata (includes assetsCount)
├── data.block-content-editor       # Lexical editor state
├── asset_index.json     # Asset usage tracking for this project
└── assets/              # Asset files
    ├── hash1.json       # Asset metadata + data
    ├── hash2.json
    └── hash3.json
```

### Export Process

```typescript
// Automatic - handled by ProjectExporter
const zipBlob = await ProjectExporter.createZipFile(projectData, hash)
// Assets are automatically included
```

### Import Process

```typescript
// Automatic - handled by ProjectImporter
const importedData = await ProjectImporter.importFromFile(file)
// importedData.assets and importedData.assetIndex are populated

// Then imported automatically when saving:
await assetManager.importProjectAssets(
  importedData.assets,
  importedData.assetIndex,
  newProjectId
)
```

### Asset Index Structure in Export

The `asset_index.json` contains only usage tracking for the exported project:

```json
{
  "abc123...": [
    {
      "projectId": "project-123",
      "nodeIds": ["node-456", "node-789"]
    }
  ]
}
```

When imported, the projectId is updated to the new project's ID.

## Future Enhancements

- [ ] Cloud storage sync (S3, Google Cloud Storage)
- [ ] Asset versioning
- [ ] Thumbnail generation
- [ ] Asset search and filtering
- [ ] Bulk asset operations
- [ ] Asset analytics (usage statistics)
- [ ] Asset sharing between users
- [ ] Asset CDN integration
- [x] Project export/import with assets

## Migration Notes

For nodes that currently use data URLs:

1. Keep backward compatibility by checking URL format:
   ```typescript
   if (url.startsWith("asset://")) {
     // New asset system
     const dataUrl = await resolveAssetUrl(url)
   } else if (url.startsWith("data:")) {
     // Legacy data URL - migrate to asset
     const result = await assetManager.saveAsset({ dataUrl: url })
   } else {
     // External URL
   }
   ```

2. Run migration script to convert existing data URLs to assets
3. Update nodes to use `resolveAssetUrl()` when displaying media

## Example: Image Node Integration

```typescript
import { resolveAssetUrl, isAssetUrl } from "@/components/block-content-editor/lib/storage/assets"

function ImageNode({ src }: { src: string }) {
  const [dataUrl, setDataUrl] = useState<string>()

  useEffect(() => {
    async function loadImage() {
      if (isAssetUrl(src)) {
        const url = await resolveAssetUrl(src)
        setDataUrl(url || undefined)
      } else {
        setDataUrl(src)
      }
    }
    loadImage()
  }, [src])

  return dataUrl ? <img src={dataUrl} /> : <div>Loading...</div>
}
```
