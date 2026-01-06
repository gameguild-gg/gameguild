/**
 * Asset storage types for media files
 */

export interface AssetMetadata {
  /** Unique identifier for the asset (SHA1 hash) */
  id: string
  /** Original filename */
  name: string
  /** Origin/source of the asset (e.g., "upload", "url", "import") */
  origin: string
  /** Type of asset: "asset" (standalone) or "collection" (created as part of a collection) */
  type?: "asset" | "collection"
  /** Author/uploader information */
  author?: string
  /** License information */
  license?: string
  /** SHA1 hash of the file content */
  sha1hash: string
  /** File size in bytes */
  size: number
  /** MIME type */
  mimeType: string
  /** Storage type: 'dataurl' (base64) or 'text' (plain text) */
  storageType?: 'dataurl' | 'text'
  /** When the asset was created */
  createdAt: string
  /** When the asset was last updated */
  updatedAt: string
}

export interface AssetUsage {
  /** Project/document ID */
  projectId: string
  /** Project/document name */
  projectName?: string
  /** List of node IDs that use this asset */
  nodeIds: string[]
}

export interface AssetIndex {
  /** Version of the asset index structure */
  version: string
  /** When the index was last updated */
  lastUpdated: string
  /** Map of asset ID (SHA1 hash) to usage tracking only */
  assets: Record<string, AssetUsage[]>
}

export interface AssetData {
  /** Asset metadata */
  metadata: AssetMetadata
  /** The actual file data (data URL or blob) */
  data: string
}

export interface SaveAssetParams {
  /** File to save */
  file?: File
  /** Data URL (for non-file uploads) */
  dataUrl?: string
  /** URL source (for URL-based uploads) */
  urlSource?: string
  /** Project/document ID that will use this asset */
  projectId?: string
  /** Node ID that will use this asset */
  nodeId?: string
  /** Author information */
  author?: string
  /** License information */
  license?: string
  /** Type of asset: "asset" (standalone) or "collection" (created as part of a collection) */
  type?: "asset" | "collection"
  /** Force storage as plain text instead of base64 (for code files) */
  forceTextStorage?: boolean
}

export interface SaveAssetResult {
  /** Whether the save was successful */
  success: boolean
  /** Asset ID (SHA1 hash) */
  assetId?: string
  /** URL to access the asset */
  assetUrl?: string
  /** Error message if save failed */
  error?: string
  /** Asset metadata */
  metadata?: AssetMetadata
}
