import JSZip, { type JSZipObject } from "jszip"
import {
  computeAssetContentHash,
  findAssetUris,
  isAssetUri,
  parseAssetUri,
  type AssetUri,
} from "@game-guild/assets"
import { getDefaultBrowserAssetRepository } from "@game-guild/assets/browser"
import type { ProjectPreferences } from "@/components/block-content-editor/lib/storage/editor/project-preferences"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"
import type { ProjectExportInput, ProjectExportMetadata } from "./interop-types"

export type { ProjectExportInput, ProjectExportMetadata } from "./interop-types"
export type ProjectStorageType = StorageType
export type ProjectData = ProjectExportInput
export type ProjectMetadata = ProjectExportMetadata

interface AssetBundleManifestEntry {
  uri: AssetUri
  name: string
  mimeType: string
  size: number
  contentHash: string
  path: string
}

export interface ImportedProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  metadata: ProjectExportMetadata | null
  assetsImported: number
  importedAssetUris: AssetUri[]
  preferences?: ProjectPreferences
}

export interface FolderStructureData {
  indexContent: string
  dataContent: string
  folderName: string
}

const assetRepository = getDefaultBrowserAssetRepository()

function rewriteAssetUris(value: unknown, replacements: Map<AssetUri, AssetUri>): unknown {
  if (isAssetUri(value)) return replacements.get(value) ?? value
  if (Array.isArray(value)) return value.map((item) => rewriteAssetUris(item, replacements))
  if (!value || typeof value !== "object") return value
  return Object.fromEntries(
    Object.entries(value as Record<string, unknown>).map(([key, item]) => [
      key,
      rewriteAssetUris(item, replacements),
    ]),
  )
}

export class ProjectImporter {
  private static readonly SUPPORTED_EXTENSIONS = [".zip", ".block-content-editor"]
  private static readonly METADATA_FILENAME = "index.json"
  private static readonly DATA_FILENAME = "data.block-content-editor"
  private static readonly ASSETS_FOLDER = "assets"
  private static readonly ASSET_MANIFEST_FILENAME = "manifest.json"
  private static readonly MAX_ASSET_COUNT = 2000
  private static readonly MAX_ASSET_SIZE = 64 * 1024 * 1024
  private static readonly MAX_BUNDLE_ASSET_BYTES = 1024 * 1024 * 1024

  static async importFromFile(file: File): Promise<ImportedProjectData> {
    const extension = `.${file.name.toLowerCase().split(".").pop()}`
    if (!ProjectImporter.SUPPORTED_EXTENSIONS.includes(extension)) {
      throw new Error(`Unsupported file format. Supported: ${ProjectImporter.SUPPORTED_EXTENSIONS.join(", ")}`)
    }
    return extension === ".zip"
      ? ProjectImporter.importFromZip(file)
      : ProjectImporter.importFromBlockContentEditorFile(file)
  }

  private static async importAssetBundle(
    manifestFile: JSZipObject | File | undefined,
    readObject: (path: string) => Promise<Blob | null>,
    documentUris: readonly AssetUri[],
  ): Promise<Map<AssetUri, AssetUri>> {
    const replacements = new Map<AssetUri, AssetUri>()
    if (!manifestFile) return replacements
    const manifestText =
      "async" in manifestFile ? await manifestFile.async("text") : await manifestFile.text()
    const parsedManifest = JSON.parse(manifestText) as unknown
    if (!Array.isArray(parsedManifest)) throw new Error("Invalid asset bundle manifest")
    const manifest = parsedManifest as AssetBundleManifestEntry[]
    if (manifest.length > ProjectImporter.MAX_ASSET_COUNT) {
      throw new Error("Asset bundle contains too many files")
    }
    const available = new Set<AssetUri>()
    const referenced = new Set(documentUris)
    let totalSize = 0
    for (const entry of manifest) {
      const parsedUri = isAssetUri(entry.uri) ? parseAssetUri(entry.uri) : null
      if (
        !parsedUri ||
        !referenced.has(entry.uri) ||
        !entry.name ||
        !entry.mimeType ||
        !/^sha256:[0-9a-f]{64}$/.test(entry.contentHash) ||
        !Number.isSafeInteger(entry.size) ||
        entry.size <= 0 ||
        entry.size > ProjectImporter.MAX_ASSET_SIZE ||
        !/^objects\/[a-zA-Z0-9._-]+$/.test(entry.path)
      ) {
        throw new Error("Invalid asset bundle manifest")
      }
      if (available.has(entry.uri)) throw new Error("Asset bundle contains duplicate references")
      available.add(entry.uri)
      totalSize += entry.size
      if (totalSize > ProjectImporter.MAX_BUNDLE_ASSET_BYTES) {
        throw new Error("Asset bundle exceeds the import size limit")
      }
    }
    const created: AssetUri[] = []
    try {
      for (const entry of manifest) {
        const blob = await readObject(entry.path)
        if (!blob || blob.size !== entry.size) {
          throw new Error(`Missing or corrupt bundled asset: ${entry.name}`)
        }
        if (await computeAssetContentHash(blob) !== entry.contentHash) {
          throw new Error(`Bundled asset failed its integrity check: ${entry.name}`)
        }
        const existed = await assetRepository.get(entry.uri)
        if (existed) {
          if (existed.contentHash !== entry.contentHash) {
            throw new Error(`Asset identity conflicts with existing content: ${entry.uri}`)
          }
          replacements.set(entry.uri, existed.uri)
          continue
        }
        const imported = await assetRepository.importBlob(blob, {
          id: parseAssetUri(entry.uri)!.id,
          name: entry.name,
          mimeType: entry.mimeType,
          source: { type: "device", value: "project-import" },
        })
        replacements.set(entry.uri, imported.uri)
        created.push(imported.uri)
      }
      return replacements
    } catch (error) {
      await Promise.all(
        created.map((uri) =>
          assetRepository.remove(uri, { force: true }).catch(() => undefined),
        ),
      )
      throw error
    }
  }

  private static async importFromZip(file: File): Promise<ImportedProjectData> {
    const zip = await new JSZip().loadAsync(file)
    const projectPath = Object.keys(zip.files)
      .map((path) => path.split("/")[0])
      .find((path) => path?.startsWith("projeto-"))
    if (!projectPath) throw new Error("No project folder found in ZIP file")
    const indexFile = zip.files[`${projectPath}/${ProjectImporter.METADATA_FILENAME}`]
    const dataFile = zip.files[`${projectPath}/${ProjectImporter.DATA_FILENAME}`]
    if (!indexFile || !dataFile) throw new Error("Project metadata or document is missing")

    const metadata = JSON.parse(await indexFile.async("text")) as ProjectMetadata
    if (!ProjectImporter.isValidMetadata(metadata)) throw new Error("Invalid metadata structure")
    const parsed = JSON.parse(await dataFile.async("text")) as unknown
    const documentUris = findAssetUris(parsed)
    const assetsRoot = `${projectPath}/${ProjectImporter.ASSETS_FOLDER}/`
    const manifestFile = zip.files[`${assetsRoot}${ProjectImporter.ASSET_MANIFEST_FILENAME}`]
    const replacements = await ProjectImporter.importAssetBundle(manifestFile, async (path) => {
      const object = zip.files[`${assetsRoot}${path}`]
      return object ? object.async("blob") : null
    }, documentUris)
    const rewritten = rewriteAssetUris(parsed, replacements)

    return {
      id: metadata.id,
      name: metadata.name,
      data: JSON.stringify(rewritten),
      tags: metadata.tags,
      metadata,
      assetsImported: replacements.size,
      importedAssetUris: Array.from(replacements.values()),
      preferences: metadata.preferences,
    }
  }

  private static async importFromBlockContentEditorFile(file: File): Promise<ImportedProjectData> {
    const content = await file.text()
    const parsed = JSON.parse(content) as unknown
    return {
      id: "",
      name: file.name.replace(/\.block-content-editor$/, "") || "Imported Project",
      data: content,
      tags: [],
      metadata: null,
      assetsImported: 0,
      importedAssetUris: [],
    }
  }

  static async importFromFolder(files: File[]): Promise<ImportedProjectData> {
    if (!files.length) throw new Error("No files provided in folder selection")
    const fileMap = new Map<string, File>()
    for (const file of files) {
      const relative = (file as File & { webkitRelativePath?: string }).webkitRelativePath || file.name
      const parts = relative.split("/").filter(Boolean)
      const innerPath = parts.length > 1 ? parts.slice(1).join("/") : parts[0]
      if (innerPath) fileMap.set(innerPath, file)
    }
    const indexFile = fileMap.get(ProjectImporter.METADATA_FILENAME)
    const dataFile = fileMap.get(ProjectImporter.DATA_FILENAME)
    if (!indexFile || !dataFile) throw new Error("Selected folder is missing project files")
    const metadata = JSON.parse(await indexFile.text()) as ProjectMetadata
    if (!ProjectImporter.isValidMetadata(metadata)) throw new Error("Invalid metadata structure")
    const parsed = JSON.parse(await dataFile.text()) as unknown
    const documentUris = findAssetUris(parsed)
    const root = `${ProjectImporter.ASSETS_FOLDER}/`
    const replacements = await ProjectImporter.importAssetBundle(
      fileMap.get(`${root}${ProjectImporter.ASSET_MANIFEST_FILENAME}`),
      async (path) => fileMap.get(`${root}${path}`) ?? null,
      documentUris,
    )
    const rewritten = rewriteAssetUris(parsed, replacements)
    return {
      id: metadata.id,
      name: metadata.name,
      data: JSON.stringify(rewritten),
      tags: metadata.tags,
      metadata,
      assetsImported: replacements.size,
      importedAssetUris: Array.from(replacements.values()),
      preferences: metadata.preferences,
    }
  }

  static async importFromFolderStructure(
    folderData: FolderStructureData,
  ): Promise<ImportedProjectData> {
    const metadata = JSON.parse(folderData.indexContent) as ProjectMetadata
    if (!ProjectImporter.isValidMetadata(metadata)) throw new Error("Invalid metadata structure")
    JSON.parse(folderData.dataContent)
    return {
      id: metadata.id,
      name: metadata.name,
      data: folderData.dataContent,
      tags: metadata.tags,
      metadata,
      assetsImported: 0,
      importedAssetUris: [],
      preferences: metadata.preferences,
    }
  }

  static convertToProjectData(
    importedData: ImportedProjectData,
    newId?: string,
    newStorageType?: ProjectStorageType,
  ): ProjectData {
    const now = new Date().toISOString()
    return {
      id: newId || importedData.id || "",
      name: importedData.name,
      data: importedData.data,
      tags: importedData.tags,
      size: new Blob([importedData.data]).size,
      createdAt: importedData.metadata?.createdAt || now,
      updatedAt: now,
      hash: importedData.metadata?.hash,
      storageType: newStorageType || (importedData.metadata?.storageType as ProjectStorageType) || "local",
      preferences: importedData.preferences || importedData.metadata?.preferences,
    }
  }

  static validateImportedData(importedData: ImportedProjectData): boolean {
    try {
      return Boolean(importedData.name && importedData.data && JSON.parse(importedData.data))
    } catch {
      return false
    }
  }

  private static isValidMetadata(metadata: unknown): metadata is ProjectMetadata {
    if (!metadata || typeof metadata !== "object") return false
    const value = metadata as Record<string, unknown>
    return Boolean(
      value.id &&
      value.name &&
      Array.isArray(value.tags) &&
      value.createdAt &&
      value.updatedAt &&
      value.storageType &&
      value.version,
    )
  }

  static getSupportedExtensions(): string[] {
    return [...ProjectImporter.SUPPORTED_EXTENSIONS]
  }

  static isSupportedFile(filename: string): boolean {
    return ProjectImporter.SUPPORTED_EXTENSIONS.includes(
      `.${filename.toLowerCase().split(".").pop()}`,
    )
  }
}
