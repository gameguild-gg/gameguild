import JSZip from "jszip"
import { findAssetUris, parseAssetUri, type AssetRecord, type AssetUri } from "@game-guild/assets"
import { getDefaultBrowserAssetRepository } from "@game-guild/assets/browser"
import type { ProjectExportInput, ProjectExportMetadata } from "./interop-types"

export type { ProjectExportInput, ProjectExportMetadata } from "./interop-types"
export type ProjectData = ProjectExportInput
export type ProjectMetadata = ProjectExportMetadata

interface ExportedAsset {
  uri: AssetUri
  record: AssetRecord
  blob: Blob
  path: string
}

interface AssetBundleManifestEntry {
  uri: AssetUri
  name: string
  mimeType: string
  size: number
  path: string
}

export interface ExportedProjectStructure {
  metadata: ProjectExportMetadata
  data: string
  folderName: string
  assets: ExportedAsset[]
}

const assetRepository = getDefaultBrowserAssetRepository()

export class ProjectExporter {
  private static readonly EXPORT_VERSION = "3.0"
  private static readonly METADATA_FILENAME = "index.json"
  private static readonly DATA_FILENAME = "data.block-content-editor"
  private static readonly ASSETS_FOLDER = "assets"
  private static readonly ASSET_MANIFEST_FILENAME = "manifest.json"

  static async prepareForExport(
    projectData: ProjectExportInput,
    hash: string,
  ): Promise<ExportedProjectStructure> {
    const parsed = JSON.parse(projectData.data) as unknown
    const uris = findAssetUris(parsed).filter(
      (uri) => parseAssetUri(uri)?.source === "local",
    )
    const assets: ExportedAsset[] = []

    for (const [index, uri] of uris.entries()) {
      const record = await assetRepository.get(uri)
      if (!record) throw new Error(`Project asset is unavailable: ${uri}`)
      const blob = await assetRepository.readBlob(uri)
      assets.push({
        uri,
        record,
        blob,
        path: `objects/${String(index).padStart(4, "0")}`,
      })
    }

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
      assetsCount: assets.length,
      preferences: projectData.preferences,
    }

    return {
      metadata,
      data: projectData.data,
      folderName: `projeto-${projectData.id}`,
      assets,
    }
  }

  static createFolderStructure(exportedProject: ExportedProjectStructure) {
    if (exportedProject.assets.length > 0) {
      throw new Error("Folder export cannot omit local asset bytes; use ZIP export")
    }
    return {
      indexContent: JSON.stringify(exportedProject.metadata, null, 2),
      dataContent: exportedProject.data,
      folderName: exportedProject.folderName,
      indexFileName: ProjectExporter.METADATA_FILENAME,
      dataFileName: ProjectExporter.DATA_FILENAME,
    }
  }

  static async createZipFile(projectData: ProjectData, hash: string): Promise<Blob> {
    const exported = await ProjectExporter.prepareForExport(projectData, hash)
    const zip = new JSZip()
    const projectFolder = zip.folder(exported.folderName)
    if (!projectFolder) throw new Error("Failed to create project folder in ZIP")

    projectFolder.file(
      ProjectExporter.METADATA_FILENAME,
      JSON.stringify(exported.metadata, null, 2),
    )
    projectFolder.file(ProjectExporter.DATA_FILENAME, exported.data)

    if (exported.assets.length > 0) {
      const assetsFolder = projectFolder.folder(ProjectExporter.ASSETS_FOLDER)
      if (!assetsFolder) throw new Error("Failed to create assets folder in ZIP")
      const manifest: AssetBundleManifestEntry[] = exported.assets.map(
        ({ uri, record, path }) => ({
          uri,
          name: record.name,
          mimeType: record.mimeType,
          size: record.size,
          path,
        }),
      )
      assetsFolder.file(
        ProjectExporter.ASSET_MANIFEST_FILENAME,
        JSON.stringify(manifest, null, 2),
      )
      for (const asset of exported.assets) assetsFolder.file(asset.path, asset.blob)
    }

    return zip.generateAsync({
      type: "blob",
      compression: "DEFLATE",
      compressionOptions: { level: 6 },
    })
  }

  static getDownloadFilename(projectData: ProjectData): string {
    const sanitizedName = projectData.name.replace(/[^a-zA-Z0-9\s\-_]/g, "").trim()
    const timestamp = new Date().toISOString().split("T")[0]
    return `${sanitizedName}-${timestamp}.zip`
  }

  static async getMetadataOnly(
    projectData: ProjectData,
    hash: string,
  ): Promise<ProjectMetadata> {
    return (await ProjectExporter.prepareForExport(projectData, hash)).metadata
  }

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
