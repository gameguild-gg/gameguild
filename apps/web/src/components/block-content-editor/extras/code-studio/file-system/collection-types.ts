import type { AssetUri } from "@game-guild/assets"

export interface CollectionMetadata {
  id: string
  name: string
  description?: string
  tags?: string[]
  created: number
  updated: number
  author?: string
}

export interface CollectionFile {
  name: string
  path: string
  assetUri?: AssetUri
  size?: number
  mimeType?: string
  isFile?: 'f' | 'm' | 't'
  readonly?: boolean
  isVisible?: boolean
}

export interface CollectionFolder {
  name: string
  path: string
  folders?: CollectionFolder[]
  files: CollectionFile[]
  readonly?: boolean
  isVisible?: boolean
}

export interface CollectionStructure {
  folders: CollectionFolder[]
  files: CollectionFile[]
}

export interface CollectionManifest {
  type: 'collection'
  metadata: CollectionMetadata
  structure: CollectionStructure
}

export interface SaveCollectionParams {
  name: string
  description?: string
  tags?: string[]
  structure: CollectionStructure
  author?: string
}
