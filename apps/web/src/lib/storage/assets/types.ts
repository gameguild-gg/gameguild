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
  /** Type of asset: "standard" (standalone) or "bundler" (created as part of a collection) */
  type?: "standard" | "bundler"
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
  /** File name (used to determine MIME type when using dataUrl) */
  fileName?: string
  /** Project/document ID that will use this asset */
  projectId?: string
  /** Node ID that will use this asset */
  nodeId?: string
  /** Author information */
  author?: string
  /** License information */
  license?: string
  /** Type of asset: "standard" (standalone) or "bundler" (created as part of a collection) */
  type?: "standard" | "bundler"
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

/**
 * Mapping of file extensions to MIME types
 */
export const MIME_TYPES: Record<string, string> = {
  // Code files
  'js': 'text/javascript',
  'jsx': 'text/javascript',
  'ts': 'text/typescript',
  'tsx': 'text/typescript',
  'py': 'text/x-python',
  'java': 'text/x-java',
  'c': 'text/x-c',
  'cpp': 'text/x-c++',
  'cs': 'text/x-csharp',
  'php': 'text/x-php',
  'rb': 'text/x-ruby',
  'go': 'text/x-go',
  'rs': 'text/x-rust',
  'swift': 'text/x-swift',
  'kt': 'text/x-kotlin',
  'sql': 'text/x-sql',
  'html': 'text/html',
  'css': 'text/css',
  'scss': 'text/x-scss',
  'sass': 'text/x-sass',
  'less': 'text/x-less',
  'json': 'application/json',
  'xml': 'application/xml',
  'yaml': 'text/yaml',
  'yml': 'text/yaml',
  'md': 'text/markdown',
  'txt': 'text/plain',
  'sh': 'text/x-sh',
  'bash': 'text/x-sh',
  // Images
  'jpg': 'image/jpeg',
  'jpeg': 'image/jpeg',
  'png': 'image/png',
  'gif': 'image/gif',
  'svg': 'image/svg+xml',
  'webp': 'image/webp',
}

/**
 * Get MIME type from file extension
 * @param fileName - The file name or extension
 * @returns MIME type or 'text/plain' if not found
 */
export function getMimeTypeFromFileName(fileName: string): string {
  const ext = fileName.split('.').pop()?.toLowerCase()
  if (!ext) return 'text/plain'
  return MIME_TYPES[ext] || 'text/plain'
}

/**
 * MIME type prefixes that indicate text content
 */
export const TEXT_MIME_TYPES = [
  'text/',
  'application/javascript',
  'application/json',
  'application/xml',
  'application/typescript',
]

/**
 * File extensions that should be treated as text files
 */
export const TEXT_EXTENSIONS = [
  '.txt', '.md', '.js', '.ts', '.jsx', '.tsx',
  '.json', '.xml', '.html', '.css', '.scss', '.sass',
  '.py', '.java', '.c', '.cpp', '.h', '.hpp',
  '.rs', '.go', '.rb', '.php', '.sh', '.bash',
  '.yml', '.yaml', '.toml', '.ini', '.conf',
  '.sql', '.lua', '.r', '.swift', '.kt', '.cs',
  '.vb', '.pl', '.scala', '.dart', '.zig', '.nim',
]
