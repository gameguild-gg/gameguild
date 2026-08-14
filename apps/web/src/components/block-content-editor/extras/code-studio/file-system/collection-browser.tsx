"use client"

import { useEffect, useState } from "react"
import { Folder, File, ChevronRight, ChevronDown, Package, Trash2, Download, Plus } from "lucide-react"
import type { CollectionMetadata, CollectionManifest, CollectionFolder, CollectionFile } from "./collection-types"
import { collectionRepository } from "./collection-repository"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Badge } from "@/components/ui/badge"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"

interface CollectionBrowserProps {
  onImportFiles: (files: Array<{ name: string; path: string; assetId: string; isFile?: 'f' | 'm' | 't'; readonly?: boolean; isVisible?: boolean }>, folderMetadata?: Map<string, { readonly?: boolean; isVisible?: boolean }>) => void
  onClose: () => void
}

export function CollectionBrowser({ onImportFiles, onClose }: CollectionBrowserProps) {
  const [collections, setCollections] = useState<CollectionMetadata[]>([])
  const [selectedCollection, setSelectedCollection] = useState<CollectionManifest | null>(null)
  const [expandedFolders, setExpandedFolders] = useState<Set<string>>(new Set())
  const [selectedFiles, setSelectedFiles] = useState<Set<string>>(new Set())
  const [searchQuery, setSearchQuery] = useState("")
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    loadCollections()
  }, [])

  const loadCollections = async () => {
    setIsLoading(true)
    try {
      const list = await collectionRepository.list()
      setCollections(list)
    } catch (error) {
      console.error("Failed to load collections:", error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleSelectCollection = async (collectionId: string) => {
    try {
      const manifest = await collectionRepository.get(collectionId)
      setSelectedCollection(manifest)
      setSelectedFiles(new Set())
      setExpandedFolders(new Set())
    } catch (error) {
      console.error("Failed to load collection:", error)
    }
  }

  const handleDeleteCollection = async (collectionId: string) => {
    if (!confirm("Delete this collection? Referenced assets will not be affected.")) return

    try {
      await collectionRepository.remove(collectionId)
      await loadCollections()
      if (selectedCollection?.metadata.id === collectionId) {
        setSelectedCollection(null)
      }
    } catch (error) {
      console.error("Failed to delete collection:", error)
    }
  }

  const toggleFolder = (path: string) => {
    setExpandedFolders((prev) => {
      const next = new Set(prev)
      if (next.has(path)) {
        next.delete(path)
      } else {
        next.add(path)
      }
      return next
    })
  }

  const toggleFileSelection = (file: CollectionFile) => {
    const key = file.path
    setSelectedFiles((prev) => {
      const next = new Set(prev)
      if (next.has(key)) {
        next.delete(key)
      } else {
        next.add(key)
      }
      return next
    })
  }

  const handleImportAll = () => {
    if (!selectedCollection) return

    const allFiles: Array<{ name: string; path: string; assetId: string; isFile?: 'f' | 'm' | 't'; readonly?: boolean; isVisible?: boolean }> = []
    const folderMetadata = new Map<string, { readonly?: boolean; isVisible?: boolean }>()

    const collectFiles = (folder: CollectionFolder) => {
      // Store folder metadata
      console.log('[CollectionBrowser] Storing folder metadata:', folder.path, {
        readonly: folder.readonly,
        isVisible: folder.isVisible,
      })
      folderMetadata.set(folder.path, {
        readonly: folder.readonly,
        isVisible: folder.isVisible,
      })
      
      for (const file of folder.files) {
        allFiles.push({
          name: file.name,
          path: file.path,
          assetId: file.assetUri ?? "",
          isFile: file.isFile,
          readonly: file.readonly,
          isVisible: file.isVisible,
        })
      }
      if (folder.folders) {
        for (const subfolder of folder.folders) {
          collectFiles(subfolder)
        }
      }
    }

    // Collect from root
    for (const file of selectedCollection.structure.files) {
      allFiles.push({
        name: file.name,
        path: file.path,
        assetId: file.assetUri ?? "",
        isFile: file.isFile,
        readonly: file.readonly,
        isVisible: file.isVisible,
      })
    }
    for (const folder of selectedCollection.structure.folders) {
      collectFiles(folder)
    }

    console.log('[CollectionBrowser] Final folderMetadata:', Array.from(folderMetadata.entries()))
    onImportFiles(allFiles, folderMetadata)
    onClose()
  }

  const handleImportSelected = () => {
    if (!selectedCollection || selectedFiles.size === 0) return

    const filesToImport: Array<{ name: string; path: string; assetId: string; isFile?: 'f' | 'm' | 't'; readonly?: boolean; isVisible?: boolean }> = []
    const folderMetadata = new Map<string, { readonly?: boolean; isVisible?: boolean }>()

    const findFiles = (folder: CollectionFolder) => {
      // Store folder metadata
      console.log('[CollectionBrowser] Storing folder metadata (selected):', folder.path, {
        readonly: folder.readonly,
        isVisible: folder.isVisible,
      })
      folderMetadata.set(folder.path, {
        readonly: folder.readonly,
        isVisible: folder.isVisible,
      })
      
      for (const file of folder.files) {
        if (selectedFiles.has(file.path)) {
          filesToImport.push({
            name: file.name,
            path: file.path,
            assetId: file.assetUri ?? "",
            isFile: file.isFile,
            readonly: file.readonly,
            isVisible: file.isVisible,
          })
        }
      }
      if (folder.folders) {
        for (const subfolder of folder.folders) {
          findFiles(subfolder)
        }
      }
    }

    // Find in root
    for (const file of selectedCollection.structure.files) {
      if (selectedFiles.has(file.path)) {
        filesToImport.push({
          name: file.name,
          path: file.path,
          assetId: file.assetUri ?? "",
          isFile: file.isFile,
          readonly: file.readonly,
          isVisible: file.isVisible,
        })
      }
    }
    for (const folder of selectedCollection.structure.folders) {
      findFiles(folder)
    }

    console.log('[CollectionBrowser] Final folderMetadata (selected):', Array.from(folderMetadata.entries()))
    onImportFiles(filesToImport, folderMetadata)
    onClose()
  }

  const renderFolder = (folder: CollectionFolder, level: number = 0) => {
    const isExpanded = expandedFolders.has(folder.path)
    const paddingLeft = level * 16

    return (
      <div key={folder.path} className="select-none">
        <div
          className="flex items-center gap-2 py-1 px-2 hover:bg-accent/50 cursor-pointer rounded"
          style={{ paddingLeft }}
          onClick={() => toggleFolder(folder.path)}
        >
          {isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
          <Folder className="h-4 w-4 text-blue-500" />
          <span className="text-sm">{folder.name}</span>
        </div>

        {isExpanded && (
          <div>
            {folder.files.map((file) => renderFile(file, level + 1))}
            {folder.folders?.map((subfolder) => renderFolder(subfolder, level + 1))}
          </div>
        )}
      </div>
    )
  }

  const renderFile = (file: CollectionFile, level: number = 0) => {
    const isSelected = selectedFiles.has(file.path)
    const paddingLeft = level * 16 + 24 // Extra padding to align with folder content

    return (
      <div
        key={file.path}
        className={`flex items-center gap-2 py-1 px-2 hover:bg-accent/50 cursor-pointer rounded ${
          isSelected ? "bg-accent" : ""
        }`}
        style={{ paddingLeft }}
        onClick={() => toggleFileSelection(file)}
      >
        <File className="h-4 w-4 text-gray-500" />
        <span className="text-sm flex-1">{file.name}</span>
        {file.size && <span className="text-xs text-muted-foreground">{formatBytes(file.size)}</span>}
      </div>
    )
  }

  const formatBytes = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  const filteredCollections = collections.filter(
    (c) =>
      searchQuery === "" ||
      c.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      c.description?.toLowerCase().includes(searchQuery.toLowerCase())
  )

  if (isLoading) {
    return (
      <Dialog open onOpenChange={onClose}>
        <DialogContent className="sm:max-w-[700px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Package className="h-5 w-5" />
              Import Collection
            </DialogTitle>
          </DialogHeader>
          <div className="flex items-center justify-center h-96">
            <div className="text-center">
              <Package className="h-12 w-12 mx-auto mb-2 text-muted-foreground" />
              <p className="text-sm text-muted-foreground">Loading collections...</p>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    )
  }

  if (!selectedCollection) {
    return (
      <Dialog open onOpenChange={onClose}>
        <DialogContent className="sm:max-w-[700px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Package className="h-5 w-5" />
              Import Collection
            </DialogTitle>
          </DialogHeader>
          <div className="flex flex-col h-96">
            <div className="p-4 border-b">
              <Input
                placeholder="Search collections..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="mb-2"
              />
            </div>

            <ScrollArea className="flex-1">
              <div className="p-4 space-y-2">
                {filteredCollections.length === 0 ? (
                  <div className="text-center py-8">
                    <Package className="h-12 w-12 mx-auto mb-2 text-muted-foreground" />
                    <p className="text-sm text-muted-foreground">No collections found</p>
                    <p className="text-xs text-muted-foreground mt-1">
                      Save a Code Studio project as a collection to import it later
                    </p>
                  </div>
                ) : (
                  filteredCollections.map((collection) => (
                    <div
                      key={collection.id}
                      className="border rounded-lg p-3 hover:bg-accent/50 cursor-pointer transition-colors"
                      onClick={() => handleSelectCollection(collection.id)}
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <Package className="h-4 w-4" />
                            <h3 className="font-medium text-sm">{collection.name}</h3>
                          </div>
                          {collection.description && (
                            <p className="text-xs text-muted-foreground mt-1">{collection.description}</p>
                          )}
                          <div className="flex items-center gap-2 mt-2">
                            {collection.tags?.map((tag) => (
                              <Badge key={tag} variant="secondary" className="text-xs">
                                {tag}
                              </Badge>
                            ))}
                          </div>
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={(e) => {
                            e.stopPropagation()
                            handleDeleteCollection(collection.id)
                          }}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </ScrollArea>
          </div>
        </DialogContent>
      </Dialog>
    )
  }

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent className="sm:max-w-[700px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Package className="h-5 w-5" />
            {selectedCollection.metadata.name}
          </DialogTitle>
        </DialogHeader>
        <div className="flex flex-col h-96">
          <div className="p-4 border-b">
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-2">
                <Button variant="ghost" size="sm" onClick={() => setSelectedCollection(null)}>
                  ←
                </Button>
                <h2 className="font-semibold">{selectedCollection.metadata.name}</h2>
              </div>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" onClick={handleImportAll} disabled={selectedFiles.size > 0}>
                  <Download className="h-4 w-4 mr-1" />
                  Import All
                </Button>
                <Button size="sm" onClick={handleImportSelected} disabled={selectedFiles.size === 0}>
                  <Plus className="h-4 w-4 mr-1" />
                  Import Selected ({selectedFiles.size})
                </Button>
              </div>
            </div>
            {selectedCollection.metadata.description && (
              <p className="text-xs text-muted-foreground">{selectedCollection.metadata.description}</p>
            )}
          </div>

          <ScrollArea className="flex-1">
            <div className="p-4">
              {selectedCollection.structure.files.map((file) => renderFile(file))}
              {selectedCollection.structure.folders.map((folder) => renderFolder(folder))}
            </div>
          </ScrollArea>
        </div>
      </DialogContent>
    </Dialog>
  )
}
