import type { CodeFile, FileTreeFolder } from "../types"
import type { CollectionStructure, CollectionFolder, CollectionFile, SaveCollectionParams } from "@/components/block-content-editor/lib/storage/assets/collection-types"
import { assetManager } from "@/components/block-content-editor/lib/storage/assets/asset-manager"

/**
 * Extract asset ID from asset:// URL
 */
function extractAssetId(url: string): string | null {
  if (!url.startsWith("asset://")) return null
  return url.replace("asset://", "")
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
      assetId: '', // Empty string for empty files
      size: 0,
      mimeType: file.language || "text/plain",
      isFile: file.isFile,
      readonly: file.readonly,
      isVisible: file.isVisible,
    }
  }
  
  // Only include files with asset references
  const assetId = extractAssetId(file.content)
  if (!assetId) return null

  return {
    name: file.name,
    path,
    assetId,
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
  console.log('[folderToCollectionFolder] Processing:', folder.name, 'children:', folder.children.length)

  const files: CollectionFile[] = []
  const subfolders: CollectionFolder[] = []

  for (const child of folder.children) {
    if (isFolder(child)) {
      console.log('[folderToCollectionFolder] Child is folder:', child.name)
      const collectionFolder = folderToCollectionFolder(child, folderPath)
      // Only include folders that have files or subfolders
      if (collectionFolder.files.length > 0 || (collectionFolder.folders && collectionFolder.folders.length > 0)) {
        subfolders.push(collectionFolder)
        console.log('[folderToCollectionFolder] Added subfolder:', child.name)
      } else {
        console.log('[folderToCollectionFolder] Skipped empty subfolder:', child.name)
      }
    } else {
      console.log('[folderToCollectionFolder] Child is file:', child.name, 'content:', child.content.substring(0, 50))
      const collectionFile = codeFileToCollectionFile(child, folderPath)
      if (collectionFile) {
        files.push(collectionFile)
        console.log('[folderToCollectionFolder] Added file:', child.name)
      } else {
        console.log('[folderToCollectionFolder] Skipped file (no asset):', child.name)
      }
    }
  }

  console.log('[folderToCollectionFolder] Result for', folder.name, ':', {
    files: files.length,
    subfolders: subfolders.length
  })

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
  console.log('[buildCollectionStructure] Starting with:', { 
    folderCount: folders.length, 
    fileCount: files.length,
    folders: folders.map(f => ({ name: f.name, childCount: f.children.length })),
    files: files.map(f => ({ name: f.name, hasAsset: f.content.startsWith('asset://') }))
  })
  
  const collectionFolders: CollectionFolder[] = []
  const collectionFiles: CollectionFile[] = []

  // Convert root files
  for (const file of files) {
    const collectionFile = codeFileToCollectionFile(file)
    if (collectionFile) {
      collectionFiles.push(collectionFile)
      console.log('[buildCollectionStructure] Added root file:', file.name)
    } else {
      console.log('[buildCollectionStructure] Skipped root file (no asset):', file.name, file.content.substring(0, 50))
    }
  }

  // Convert folders
  for (const folder of folders) {
    console.log('[buildCollectionStructure] Processing folder:', folder.name, 'children:', folder.children.length)
    const collectionFolder = folderToCollectionFolder(folder)
    console.log('[buildCollectionStructure] Converted folder:', {
      name: collectionFolder.name,
      fileCount: collectionFolder.files.length,
      subfolderCount: collectionFolder.folders?.length || 0
    })
    // Only include folders that have content
    if (collectionFolder.files.length > 0 || (collectionFolder.folders && collectionFolder.folders.length > 0)) {
      collectionFolders.push(collectionFolder)
      console.log('[buildCollectionStructure] Added folder:', folder.name)
    } else {
      console.log('[buildCollectionStructure] Skipped empty folder:', folder.name)
    }
  }

  console.log('[buildCollectionStructure] Result:', {
    totalFolders: collectionFolders.length,
    totalFiles: collectionFiles.length
  })

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

    const result = await assetManager.saveCollection(saveParams)

    if (result.success && result.collectionId) {
      return {
        success: true,
        collectionId: result.collectionId,
      }
    } else {
      return {
        success: false,
        error: result.error || "Failed to save collection",
      }
    }
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
    if (extractAssetId(file.content)) {
      count++
    }
  }

  const countInFolder = (folder: FileTreeFolder): number => {
    let folderCount = 0
    for (const child of folder.children) {
      if (isFolder(child)) {
        folderCount += countInFolder(child)
      } else {
        if (extractAssetId(child.content)) {
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
