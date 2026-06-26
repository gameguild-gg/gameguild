/**
 * Project Exporter Utility
 * Handles exporting project data to standardized format
 * Works for both Google Drive and local file exports
 */

import JSZip from "jszip"
import { assetManager } from "@/components/block-content-editor/lib/storage/assets/asset-manager"
import type { AssetData, AssetUsage } from "@/components/block-content-editor/lib/storage/assets/types"
import type { ProjectExportInput, ProjectExportMetadata } from "./interop-types"

export type { ProjectExportInput, ProjectExportMetadata } from "./interop-types"
/** @deprecated Use {@link ProjectExportInput} */
export type ProjectData = ProjectExportInput
/** @deprecated Use {@link ProjectExportMetadata} */
export type ProjectMetadata = ProjectExportMetadata

export interface ExportedProjectStructure {
  metadata: ProjectExportMetadata
  data: string
  folderName: string
  assets?: Record<string, AssetData>
  assetIndex?: Record<string, AssetUsage[]>
}

export class ProjectExporter {
  private static readonly EXPORT_VERSION = "2.0" // Updated to include assets
  private static readonly METADATA_FILENAME = "index.json"
  private static readonly DATA_FILENAME = "data.block-content-editor"
  private static readonly ASSETS_FOLDER = "assets"
  private static readonly ASSET_INDEX_FILENAME = "asset_index.json"

  /**
   * Prepare project data for export with standardized structure
   */
  static async prepareForExport(
    projectData: ProjectExportInput,
    hash: string
  ): Promise<ExportedProjectStructure> {
    console.log('[ProjectExporter] Preparing project for export:', projectData.id)
    
    // Export project assets
    const projectAssets = await assetManager.exportProjectAssets(projectData.id)
    console.log('[ProjectExporter] Exported assets:', Object.keys(projectAssets).length)
    
    const assetIndex = await assetManager.exportProjectAssetIndex(projectData.id)
    console.log('[ProjectExporter] Exported asset index entries:', Object.keys(assetIndex).length)

    const metadata: ProjectExportMetadata = {
      id: projectData.id,
      name: projectData.name,
      tags: projectData.tags,
      size: projectData.size,
      hash,
      createdAt: projectData.createdAt,
      updatedAt: projectData.updatedAt,
      storageType: projectData.storageType || "local",
      version: ProjectExporter.EXPORT_VERSION,
      exportedAt: new Date().toISOString(),
      assetsCount: Object.keys(projectAssets).length,
      preferences: projectData.preferences,
    }

    console.log('[ProjectExporter] Metadata prepared:', {
      id: metadata.id,
      name: metadata.name,
      assetsCount: metadata.assetsCount,
      hasPreferences: !!metadata.preferences
    })

    return {
      metadata,
      data: projectData.data,
      folderName: `projeto-${projectData.id}`,
      assets: projectAssets,
      assetIndex,
    }
  }

  /**
   * Create folder structure for Google Drive or other structured storage
   */
  static createFolderStructure(exportedProject: ExportedProjectStructure): {
    indexContent: string
    dataContent: string
    folderName: string
    indexFileName: string
    dataFileName: string
  } {
    return {
      indexContent: JSON.stringify(exportedProject.metadata, null, 2),
      dataContent: exportedProject.data,
      folderName: exportedProject.folderName,
      indexFileName: ProjectExporter.METADATA_FILENAME,
      dataFileName: ProjectExporter.DATA_FILENAME
    }
  }

  /**
   * Create ZIP file for local download
   */
  static async createZipFile(
    projectData: ProjectData,
    hash: string
  ): Promise<Blob> {
    console.log('[ProjectExporter] Creating ZIP file for project:', projectData.id)
    
    const exportedProject = await ProjectExporter.prepareForExport(projectData, hash)
    const folderStructure = ProjectExporter.createFolderStructure(exportedProject)

    const zip = new JSZip()
    
    // Create project folder inside ZIP
    const projectFolder = zip.folder(folderStructure.folderName)
    
    if (!projectFolder) {
      throw new Error("Failed to create project folder in ZIP")
    }

    // Add metadata file
    projectFolder.file(folderStructure.indexFileName, folderStructure.indexContent)
    console.log('[ProjectExporter] Added metadata file')
    
    // Add data file
    projectFolder.file(folderStructure.dataFileName, folderStructure.dataContent)
    console.log('[ProjectExporter] Added data file')

    // Add assets if present
    if (exportedProject.assets && Object.keys(exportedProject.assets).length > 0) {
      console.log('[ProjectExporter] Adding assets folder with', Object.keys(exportedProject.assets).length, 'assets')
      const assetsFolder = projectFolder.folder(ProjectExporter.ASSETS_FOLDER)
      
      if (assetsFolder) {
        // Add each asset file
        for (const assetId in exportedProject.assets) {
          const assetData = exportedProject.assets[assetId]
          if (assetData) {
            // Save asset metadata and data as JSON
            assetsFolder.file(`${assetId}.json`, JSON.stringify(assetData, null, 2))
            console.log('[ProjectExporter] Added asset:', assetId)
          }
        }

        // Add asset index
        if (exportedProject.assetIndex) {
          projectFolder.file(
            ProjectExporter.ASSET_INDEX_FILENAME,
            JSON.stringify(exportedProject.assetIndex, null, 2)
          )
          console.log('[ProjectExporter] Added asset index with', Object.keys(exportedProject.assetIndex).length, 'entries')
        }
      }
    } else {
      console.log('[ProjectExporter] No assets to add')
    }

    // Generate ZIP blob
    console.log('[ProjectExporter] Generating ZIP blob...')
    const blob = await zip.generateAsync({ 
      type: "blob",
      compression: "DEFLATE",
      compressionOptions: {
        level: 6
      }
    })
    
    console.log('[ProjectExporter] ZIP blob generated, size:', blob.size)
    return blob
  }

  /**
   * Create legacy format for backward compatibility
   */
  static async createLegacyZipFile(projectData: ProjectData): Promise<Blob> {
    const zip = new JSZip()

    // Add the lexical file with .block-content-editor extension
    zip.file(`${projectData.name}.block-content-editor`, projectData.data)

    // Create legacy index.json with project metadata
    const legacyMetadata = {
      id: projectData.id,
      name: projectData.name,
      tags: projectData.tags,
      size: projectData.size,
      createdAt: projectData.createdAt,
      updatedAt: projectData.updatedAt,
      version: "1.0",
      type: "gg-lexical-project",
    }

    zip.file("index.json", JSON.stringify(legacyMetadata, null, 2))

    return await zip.generateAsync({ 
      type: "blob",
      compression: "DEFLATE",
      compressionOptions: {
        level: 6
      }
    })
  }

  /**
   * Get appropriate filename for download
   */
  static getDownloadFilename(projectData: ProjectData): string {
    const sanitizedName = projectData.name.replace(/[^a-zA-Z0-9\s\-_]/g, "").trim()
    const timestamp = new Date().toISOString().split('T')[0]
    
    return `${sanitizedName}-${timestamp}.zip`
  }

  /**
   * Get metadata only (for quick sync checks)
   */
  static async getMetadataOnly(
    projectData: ProjectData,
    hash: string
  ): Promise<ProjectMetadata> {
    const exportedProject = await ProjectExporter.prepareForExport(projectData, hash)
    return exportedProject.metadata
  }

  /**
   * Validate export data before processing
   */
  static validateExportData(projectData: ProjectData): boolean {
    return !!(
      projectData.id &&
      projectData.name &&
      projectData.data &&
      Array.isArray(projectData.tags) &&
      projectData.createdAt &&
      projectData.updatedAt
    )
  }
}
