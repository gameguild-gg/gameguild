/**
 * Asset Collection types for organizing related files
 */

export interface CollectionMetadata {
  /** Unique identifier (SHA1 hash of manifest) */
  id: string
  /** Collection name */
  name: string
  /** Description */
  description?: string
  /** Tags for categorization */
  tags?: string[]
  /** When created */
  created: number
  /** Last updated */
  updated: number
  /** Author */
  author?: string
}

export interface CollectionFile {
  /** File name */
  name: string
  /** Path within collection (e.g., "src/index.js") */
  path: string
  /** Reference to the actual asset ID */
  assetId: string
  /** File size in bytes */
  size?: number
  /** MIME type */
  mimeType?: string
  /** File type: 'f' (padrão), 'm' (main), 't' (test) */
  isFile?: 'f' | 'm' | 't'
  /** If true, the file cannot be edited */
  readonly?: boolean
  /** If false, the file is hidden */
  isVisible?: boolean
}

export interface CollectionFolder {
  /** Folder name */
  name: string
  /** Path within collection (e.g., "src/") */
  path: string
  /** Subfolders */
  folders?: CollectionFolder[]
  /** Files in this folder */
  files: CollectionFile[]
  /** If true, all files inside cannot be edited */
  readonly?: boolean
  /** If false, the folder is hidden */
  isVisible?: boolean
}

export interface CollectionStructure {
  /** Root folders */
  folders: CollectionFolder[]
  /** Root files */
  files: CollectionFile[]
}

export interface CollectionManifest {
  /** Type identifier */
  type: 'collection'
  /** Metadata */
  metadata: CollectionMetadata
  /** File structure */
  structure: CollectionStructure
}

export interface CollectionData {
  /** Collection metadata */
  metadata: CollectionMetadata
  /** The manifest as JSON string */
  manifest: string
}

export interface SaveCollectionParams {
  /** Collection name */
  name: string
  /** Description */
  description?: string
  /** Tags */
  tags?: string[]
  /** File structure */
  structure: CollectionStructure
  /** Author */
  author?: string
}

export interface SaveCollectionResult {
  /** Success status */
  success: boolean
  /** Collection ID */
  collectionId?: string
  /** Error message */
  error?: string
  /** Metadata */
  metadata?: CollectionMetadata
}
