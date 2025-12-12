/**
 * Project Exporter Utility
 * Handles exporting project data to standardized format
 * Works for both Google Drive and local file exports
 */

import JSZip from "jszip"

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
  exportedAt: string
}

export interface ExportedProjectStructure {
  metadata: ProjectMetadata
  data: string
  folderName: string
}

export class ProjectExporter {
  private static readonly EXPORT_VERSION = "1.0"
  private static readonly METADATA_FILENAME = "index.json"
  private static readonly DATA_FILENAME = "data.gglexical"

  /**
   * Prepare project data for export with standardized structure
   */
  static prepareForExport(
    projectData: ProjectData,
    hash: string
  ): ExportedProjectStructure {
    const metadata: ProjectMetadata = {
      id: projectData.id,
      name: projectData.name,
      tags: projectData.tags,
      size: projectData.size,
      hash,
      createdAt: projectData.createdAt,
      updatedAt: projectData.updatedAt,
      storageType: projectData.storageType || "local",
      version: ProjectExporter.EXPORT_VERSION,
      exportedAt: new Date().toISOString()
    }

    return {
      metadata,
      data: projectData.data,
      folderName: `projeto-${projectData.id}`
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
    const exportedProject = ProjectExporter.prepareForExport(projectData, hash)
    const folderStructure = ProjectExporter.createFolderStructure(exportedProject)

    const zip = new JSZip()
    
    // Create project folder inside ZIP
    const projectFolder = zip.folder(folderStructure.folderName)
    
    if (!projectFolder) {
      throw new Error("Failed to create project folder in ZIP")
    }

    // Add metadata file
    projectFolder.file(folderStructure.indexFileName, folderStructure.indexContent)
    
    // Add data file
    projectFolder.file(folderStructure.dataFileName, folderStructure.dataContent)

    // Generate ZIP blob
    return await zip.generateAsync({ 
      type: "blob",
      compression: "DEFLATE",
      compressionOptions: {
        level: 6
      }
    })
  }

  /**
   * Create legacy format for backward compatibility
   */
  static async createLegacyZipFile(projectData: ProjectData): Promise<Blob> {
    const zip = new JSZip()

    // Add the lexical file with .gglexical extension
    zip.file(`${projectData.name}.gglexical`, projectData.data)

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
  static getMetadataOnly(
    projectData: ProjectData,
    hash: string
  ): ProjectMetadata {
    return ProjectExporter.prepareForExport(projectData, hash).metadata
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
