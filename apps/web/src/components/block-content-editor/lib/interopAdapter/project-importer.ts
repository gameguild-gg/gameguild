/**
 * Project Importer Utility
 * Handles importing project data from standardized format
 * Works for both Google Drive and local file imports
 */

import JSZip from "jszip"
import { assetManager } from "@/components/block-content-editor/lib/storage/assets/asset-manager"
import type { AssetData, AssetUsage } from "@/components/block-content-editor/lib/storage/assets/types"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"

export type ProjectStorageType = "local" | "gameguild-cloud" | "google-drive"

export interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  hash?: string
  storageType?: ProjectStorageType
  preferences?: ProjectPreferences
}

export interface ProjectMetadata {
  id: string
  name: string
  tags: string[]
  size: number
  hash: string
  createdAt: string
  updatedAt: string
  storageType: string
  version: string
  exportedAt?: string
  assetsCount?: number
  preferences?: ProjectPreferences
}

export interface ImportedProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  metadata: ProjectMetadata | null
  assets?: Record<string, AssetData>
  assetIndex?: Record<string, AssetUsage[]>
  preferences?: ProjectPreferences
}

export interface FolderStructureData {
  indexContent: string
  dataContent: string
  folderName: string
}

export class ProjectImporter {
  private static readonly SUPPORTED_EXTENSIONS = ['.zip', '.block-content-editor']
  private static readonly METADATA_FILENAME = 'index.json'
  private static readonly DATA_FILENAME = 'data.block-content-editor'
  private static readonly ASSETS_FOLDER = 'assets'
  private static readonly ASSET_INDEX_FILENAME = 'asset_index.json'

  /**
   * Import from file (ZIP or .block-content-editor)
   */
  static async importFromFile(file: File): Promise<ImportedProjectData> {
    const fileName = file.name.toLowerCase()
    const fileExtension = '.' + fileName.split('.').pop()

    if (!ProjectImporter.SUPPORTED_EXTENSIONS.includes(fileExtension)) {
      throw new Error(`Unsupported file format. Supported: ${ProjectImporter.SUPPORTED_EXTENSIONS.join(', ')}`)
    }

    if (fileExtension === '.zip') {
      return await ProjectImporter.importFromZip(file)
    } else if (fileExtension === '.block-content-editor') {
      return await ProjectImporter.importFromBlockContentEditorFile(file)
    }

    throw new Error('Unexpected file type')
  }

  /**
   * Import from ZIP file (projeto-* folder structure)
   */
  private static async importFromZip(file: File): Promise<ImportedProjectData> {
    const zip = new JSZip()
    const zipContent = await zip.loadAsync(file)

    // Find projeto-* folder
    const projectFolders = Object.keys(zipContent.files).filter(path => {
      const parts = path.split('/')
      const file = zipContent.files[path]
      return parts.length >= 2 && parts[0]?.startsWith('projeto-') && file && !file.dir
    })

    if (projectFolders.length === 0) {
      throw new Error('No projeto-* folder found in ZIP file')
    }

    // Get the first project folder
    const firstFolder = projectFolders[0]
    if (!firstFolder) throw new Error('Invalid folder structure')
    
    const projectFolderPath = firstFolder.split('/')[0]
    const indexPath = `${projectFolderPath}/${ProjectImporter.METADATA_FILENAME}`
    const dataPath = `${projectFolderPath}/${ProjectImporter.DATA_FILENAME}`

    const indexFile = zipContent.files[indexPath]
    const dataFile = zipContent.files[dataPath]

    if (!indexFile || !dataFile) {
      throw new Error('Missing index.json or data.block-content-editor file in projeto folder')
    }

    try {
      const indexContent = await indexFile.async('text')
      const dataContent = await dataFile.async('text')
      
      const metadata: ProjectMetadata = JSON.parse(indexContent)
      
      // Validate metadata structure
      if (!ProjectImporter.isValidMetadata(metadata)) {
        throw new Error('Invalid metadata structure')
      }

      // Validate lexical data
      JSON.parse(dataContent)

      // Import assets if present
      const assets: Record<string, AssetData> = {}
      let assetIndex: Record<string, AssetUsage[]> = {}

      // Check for asset_index.json
      const assetIndexPath = `${projectFolderPath}/${ProjectImporter.ASSET_INDEX_FILENAME}`
      const assetIndexFile = zipContent.files[assetIndexPath]
      
      if (assetIndexFile) {
        const assetIndexContent = await assetIndexFile.async('text')
        assetIndex = JSON.parse(assetIndexContent)
      }

      // Check for assets folder
      const assetsPath = `${projectFolderPath}/${ProjectImporter.ASSETS_FOLDER}/`
      const assetFiles = Object.keys(zipContent.files).filter(path => 
        path.startsWith(assetsPath) && path.endsWith('.json')
      )

      for (const assetPath of assetFiles) {
        const assetFile = zipContent.files[assetPath]
        if (assetFile && !assetFile.dir) {
          const assetContent = await assetFile.async('text')
          const assetData: AssetData = JSON.parse(assetContent)
          const assetId = assetPath.split('/').pop()?.replace('.json', '')
          if (assetId) {
            assets[assetId] = assetData
          }
        }
      }

      return {
        id: metadata.id,
        name: metadata.name,
        data: dataContent,
        tags: metadata.tags,
        metadata,
        assets: Object.keys(assets).length > 0 ? assets : undefined,
        assetIndex: Object.keys(assetIndex).length > 0 ? assetIndex : undefined,
        preferences: metadata.preferences,
      }
    } catch (error) {
      throw new Error(`Failed to parse project data: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  /**
   * Import from single .block-content-editor file
   */
  private static async importFromBlockContentEditorFile(file: File): Promise<ImportedProjectData> {
    const content = await file.text()
    const baseName = file.name.replace(/\.block-content-editor$/, '')

    // Validate lexical data
    try {
      JSON.parse(content)
    } catch {
      throw new Error('Invalid Block Content Editor data format')
    }

    return {
      id: '',
      name: baseName || 'Imported Project',
      data: content,
      tags: [],
      metadata: null
    }
  }

  /**
   * Import from a set of files representing an uncompressed projeto-* folder.
   * Accepts files from <input webkitdirectory> or extracted from drag-and-drop entries.
   * Each file should have a relative path (webkitRelativePath) like:
   *   projeto-<id>/index.json
   *   projeto-<id>/data.block-content-editor
   *   projeto-<id>/asset_index.json
   *   projeto-<id>/assets/<assetId>.json
   * Bare files (without a parent folder) are also tolerated.
   */
  static async importFromFolder(files: File[]): Promise<ImportedProjectData> {
    if (!files || files.length === 0) {
      throw new Error('No files provided in folder selection')
    }

    // Build a map keyed by the path *inside* the project folder (or just filename if at root)
    const fileMap = new Map<string, File>()
    for (const file of files) {
      const rel = (file as File & { webkitRelativePath?: string }).webkitRelativePath || file.name
      const parts = rel.split('/').filter(Boolean)
      // Drop the top-level folder name if it exists, so paths are relative to the project root
      const innerPath = parts.length > 1 ? parts.slice(1).join('/') : parts[0]
      if (!innerPath) continue
      fileMap.set(innerPath, file)
    }

    const indexFile = fileMap.get(ProjectImporter.METADATA_FILENAME)
    const dataFile = fileMap.get(ProjectImporter.DATA_FILENAME)

    if (!indexFile || !dataFile) {
      throw new Error(`Selected folder must contain both ${ProjectImporter.METADATA_FILENAME} and ${ProjectImporter.DATA_FILENAME}`)
    }

    try {
      const indexContent = await indexFile.text()
      const dataContent = await dataFile.text()

      const metadata: ProjectMetadata = JSON.parse(indexContent)

      if (!ProjectImporter.isValidMetadata(metadata)) {
        throw new Error('Invalid metadata structure')
      }

      JSON.parse(dataContent)

      // Optional assets
      const assets: Record<string, AssetData> = {}
      let assetIndex: Record<string, AssetUsage[]> = {}

      const assetIndexFile = fileMap.get(ProjectImporter.ASSET_INDEX_FILENAME)
      if (assetIndexFile) {
        assetIndex = JSON.parse(await assetIndexFile.text())
      }

      const assetPrefix = `${ProjectImporter.ASSETS_FOLDER}/`
      for (const [path, file] of fileMap.entries()) {
        if (path.startsWith(assetPrefix) && path.endsWith('.json')) {
          const assetId = path.slice(assetPrefix.length).replace(/\.json$/, '')
          if (assetId) {
            assets[assetId] = JSON.parse(await file.text())
          }
        }
      }

      return {
        id: metadata.id,
        name: metadata.name,
        data: dataContent,
        tags: metadata.tags,
        metadata,
        assets: Object.keys(assets).length > 0 ? assets : undefined,
        assetIndex: Object.keys(assetIndex).length > 0 ? assetIndex : undefined,
        preferences: metadata.preferences,
      }
    } catch (error) {
      throw new Error(`Failed to parse project folder: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  /**
   * Import from folder structure (for Google Drive)
   */
  static async importFromFolderStructure(
    folderData: FolderStructureData
  ): Promise<ImportedProjectData> {
    try {
      const metadata: ProjectMetadata = JSON.parse(folderData.indexContent)
      
      // Validate metadata structure
      if (!ProjectImporter.isValidMetadata(metadata)) {
        throw new Error('Invalid metadata structure')
      }

      // Validate lexical data
      JSON.parse(folderData.dataContent)

      return {
        id: metadata.id,
        name: metadata.name,
        data: folderData.dataContent,
        tags: metadata.tags,
        metadata,
        preferences: metadata.preferences,
      }
    } catch (error) {
      throw new Error(`Failed to import from folder structure: ${error instanceof Error ? error.message : 'Unknown error'}`)
    }
  }

  /**
   * Convert imported data to standard ProjectData format
   */
  static convertToProjectData(
    importedData: ImportedProjectData,
    newId?: string,
    newStorageType?: ProjectStorageType
  ): ProjectData {
    const now = new Date().toISOString()
    
    return {
      id: newId || importedData.id || '',
      name: importedData.name,
      data: importedData.data,
      tags: importedData.tags,
      size: new Blob([importedData.data]).size,
      createdAt: importedData.metadata?.createdAt || now,
      updatedAt: now, // Always update to current time on import
      hash: importedData.metadata?.hash,
      storageType: newStorageType || (importedData.metadata?.storageType as ProjectStorageType) || "local",
      preferences: importedData.preferences || importedData.metadata?.preferences,
    }
  }

  /**
   * Import assets into AssetManager for the target project
   * Returns stats about imported assets
   */
  static async importProjectAssets(
    importedData: ImportedProjectData,
    targetProjectId: string
  ): Promise<{ imported: number; skipped: number; updated: number }> {
    if (!importedData.assets || !importedData.assetIndex) {
      console.log('[ProjectImporter] No assets to import')
      return { imported: 0, skipped: 0, updated: 0 }
    }

    console.log('[ProjectImporter] Importing assets:', {
      assetsCount: Object.keys(importedData.assets).length,
      indexCount: Object.keys(importedData.assetIndex).length,
      targetProjectId
    })

    try {
      const result = await assetManager.importProjectAssets(
        importedData.assets,
        importedData.assetIndex,
        targetProjectId
      )

      console.log('[ProjectImporter] Assets imported successfully:', result)
      return result
    } catch (error) {
      console.error('[ProjectImporter] Failed to import assets:', error)
      throw error
    }
  }

  /**
   * Validate imported project data
   */
  static validateImportedData(importedData: ImportedProjectData): boolean {
    try {
      // Validate basic structure
      if (!importedData.name || !importedData.data) {
        return false
      }

      // Validate lexical data is valid JSON
      JSON.parse(importedData.data)

      return true
    } catch {
      return false
    }
  }

  /**
   * Validate metadata structure
   */
  private static isValidMetadata(metadata: unknown): metadata is ProjectMetadata {
    if (!metadata || typeof metadata !== 'object') return false
    const m = metadata as Record<string, unknown>
    return !!(
      m.id &&
      m.name &&
      Array.isArray(m.tags) &&
      m.createdAt &&
      m.updatedAt &&
      m.storageType &&
      m.version
    )
  }

  /**
   * Get supported file extensions
   */
  static getSupportedExtensions(): string[] {
    return [...ProjectImporter.SUPPORTED_EXTENSIONS]
  }

  /**
   * Check if filename is supported
   */
  static isSupportedFile(filename: string): boolean {
    const extension = '.' + filename.toLowerCase().split('.').pop()
    return ProjectImporter.SUPPORTED_EXTENSIONS.includes(extension)
  }
}
