/**
 * Project Importer Utility
 * Handles importing project data from standardized format
 * Works for both Google Drive and local file imports
 */

import JSZip from "jszip"
import { assetManager } from "@/lib/storage/assets/asset-manager"
import type { AssetData, AssetUsage } from "@/lib/storage/assets/types"

export interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  hash?: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  preferences?: any
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
  preferences?: any
}

export interface ImportedProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  metadata: ProjectMetadata | null
  assets?: Record<string, AssetData>
  assetIndex?: Record<string, AssetUsage[]>
  preferences?: any
}

export interface FolderStructureData {
  indexContent: string
  dataContent: string
  folderName: string
}

export class ProjectImporter {
  private static readonly SUPPORTED_EXTENSIONS = ['.zip', '.gglexical']
  private static readonly METADATA_FILENAME = 'index.json'
  private static readonly DATA_FILENAME = 'data.gglexical'
  private static readonly ASSETS_FOLDER = 'assets'
  private static readonly ASSET_INDEX_FILENAME = 'asset_index.json'

  /**
   * Import from file (ZIP or .gglexical)
   */
  static async importFromFile(file: File): Promise<ImportedProjectData> {
    const fileName = file.name.toLowerCase()
    const fileExtension = '.' + fileName.split('.').pop()

    if (!ProjectImporter.SUPPORTED_EXTENSIONS.includes(fileExtension)) {
      throw new Error(`Unsupported file format. Supported: ${ProjectImporter.SUPPORTED_EXTENSIONS.join(', ')}`)
    }

    if (fileExtension === '.zip') {
      return await ProjectImporter.importFromZip(file)
    } else if (fileExtension === '.gglexical') {
      return await ProjectImporter.importFromGGLexicalFile(file)
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
      throw new Error('Missing index.json or data.gglexical file in projeto folder')
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
   * Import from single .gglexical file
   */
  private static async importFromGGLexicalFile(file: File): Promise<ImportedProjectData> {
    const content = await file.text()
    const baseName = file.name.replace(/\.gglexical$/, '')

    // Validate lexical data
    try {
      JSON.parse(content)
    } catch {
      throw new Error('Invalid GGLexical data format')
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
    newStorageType?: "local" | "gameguild-cloud" | "google-drive"
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
      storageType: newStorageType || (importedData.metadata?.storageType as "local" | "gameguild-cloud" | "google-drive") || "local",
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
  private static isValidMetadata(metadata: any): metadata is ProjectMetadata {
    return !!(
      metadata &&
      typeof metadata === 'object' &&
      metadata.id &&
      metadata.name &&
      Array.isArray(metadata.tags) &&
      metadata.createdAt &&
      metadata.updatedAt &&
      metadata.storageType &&
      metadata.version
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
