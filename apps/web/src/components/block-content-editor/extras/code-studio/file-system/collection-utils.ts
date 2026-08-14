import type { CodeFile, FileTreeFolder } from "../types"
import { isAssetUri, toAssetUri } from "@game-guild/assets"
import type { CollectionStructure, CollectionFolder, CollectionFile, SaveCollectionParams } from "./collection-types"
import { collectionRepository } from "./collection-repository"

/**
 * Extract asset ID from asset:// URL
 */
function extractAssetUri(url: string) {
  return isAssetUri(url) ? toAssetUri(url) : null
}

/**
 * Convert Code Studio file to Collection file
 */
function codeFileToCollectionFile(file: CodeFile, basePath: string = ""): CollectionFile | null {
  const path = basePath ? `${basePath}/${file.name}` : file.name
  
  // Check if file is empty
  const isEmpty = !file.content || file.content.trim() === ''
  
  if (isEmpty) {
    // Empty file - include without asset reference
    return {
      name: file.name,
      path,
      size: 0,
      mimeType: file.language || "text/plain",
      isFile: file.isFile,
      readonly: file.readonly,
      isVisible: file.isVisible,
    }
  }
  
  // Only include files with asset references
  const assetUri = extractAssetUri(file.content)
  if (!assetUri) return null

  return {
    name: file.name,
    path,
    assetUri,
    size: 0, // Size is stored in the asset metadata
    mimeType: file.language || "text/plain",
    isFile: file.isFile,
    readonly: file.readonly,
    isVisible: file.isVisible,
  }
}

/**
 * Type guard to check if item is a folder
 */
function isFolder(item: CodeFile | FileTreeFolder): item is FileTreeFolder {
  return "children" in item
}

/**
 * Convert Code Studio folder to Collection folder
 */
function folderToCollectionFolder(folder: FileTreeFolder, basePath: string = ""): CollectionFolder {
  const folderPath = basePath ? `${basePath}/${folder.name}` : folder.name

  const files: CollectionFile[] = []
  const subfolders: CollectionFolder[] = []

  for (const child of folder.children) {
    if (isFolder(child)) {
      const collectionFolder = folderToCollectionFolder(child, folderPath)
      // Only include folders that have files or subfolders
      if (collectionFolder.files.length > 0 || (collectionFolder.folders && collectionFolder.folders.length > 0)) {
        subfolders.push(collectionFolder)
      }
    } else {
      const collectionFile = codeFileToCollectionFile(child, folderPath)
      if (collectionFile) {
        files.push(collectionFile)
      }
    }
  }

  return {
    name: folder.name,
    path: folderPath,
    files,
    folders: subfolders.length > 0 ? subfolders : undefined,
    readonly: folder.readonly,
    isVisible: folder.isVisible,
  }
}

/**
 * Build Collection structure from Code Studio file tree
 */
export function buildCollectionStructure(folders: FileTreeFolder[], files: CodeFile[]): CollectionStructure {
  const collectionFolders: CollectionFolder[] = []
  const collectionFiles: CollectionFile[] = []

  // Convert root files
  for (const file of files) {
    const collectionFile = codeFileToCollectionFile(file)
    if (collectionFile) {
      collectionFiles.push(collectionFile)
    }
  }

  // Convert folders
  for (const folder of folders) {
    const collectionFolder = folderToCollectionFolder(folder)
    // Only include folders that have content
    if (collectionFolder.files.length > 0 || (collectionFolder.folders && collectionFolder.folders.length > 0)) {
      collectionFolders.push(collectionFolder)
    }
  }

  return {
    folders: collectionFolders,
    files: collectionFiles,
  }
}

/**
 * Save Code Studio project as Collection
 */
export async function saveProjectAsCollection(params: {
  name: string
  description?: string
  tags?: string[]
  folders: FileTreeFolder[]
  files: CodeFile[]
  author?: string
}): Promise<{ success: boolean; collectionId?: string; error?: string }> {
  try {
    const structure = buildCollectionStructure(params.folders, params.files)

    const saveParams: SaveCollectionParams = {
      name: params.name,
      description: params.description,
      tags: params.tags,
      structure,
      author: params.author,
    }

    const result = await collectionRepository.save(saveParams)
    return { success: true, collectionId: result.metadata.id }
  } catch (error) {
    console.error("Failed to save project as collection:", error)
    return {
      success: false,
      error: error instanceof Error ? error.message : "Unknown error",
    }
  }
}

/**
 * Count total files with asset references in a project
 */
export function countAssetReferences(folders: FileTreeFolder[], files: CodeFile[]): number {
  let count = 0

  for (const file of files) {
    if (extractAssetUri(file.content)) {
      count++
    }
  }

  const countInFolder = (folder: FileTreeFolder): number => {
    let folderCount = 0
    for (const child of folder.children) {
      if (isFolder(child)) {
        folderCount += countInFolder(child)
      } else {
        if (extractAssetUri(child.content)) {
          folderCount++
        }
      }
    }
    return folderCount
  }

  for (const folder of folders) {
    count += countInFolder(folder)
  }

  return count
}
