"use client"

import { useState } from "react"
import { 
  ChevronRight, 
  ChevronDown, 
  File, 
  Folder, 
  FolderOpen,
  Plus,
  FileText,
  Trash2,
  Edit3
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import type { CodeFile, FileTreeFolder, FileTreeItem } from "./types"
import { cn } from "@/lib/utils"

interface FileExplorerProps {
  files: CodeFile[]
  folders: FileTreeFolder[]
  activeFileId?: string
  onFileSelect: (fileId: string) => void
  onCreateFile: (path: string, name: string) => void
  onCreateFolder: (path: string, name: string) => void
  onDeleteFile: (fileId: string) => void
  onDeleteFolder: (folderId: string) => void
  onRenameFile: (fileId: string, newName: string) => void
  onRenameFolder: (folderId: string, newName: string) => void
  onToggleFolder: (folderId: string) => void
}

export function FileExplorer({
  files,
  folders,
  activeFileId,
  onFileSelect,
  onCreateFile,
  onCreateFolder,
  onDeleteFile,
  onDeleteFolder,
  onRenameFile,
  onRenameFolder,
  onToggleFolder,
}: FileExplorerProps) {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState("")
  const [creatingType, setCreatingType] = useState<"file" | "folder" | null>(null)
  const [creatingPath, setCreatingPath] = useState("")
  const [newItemName, setNewItemName] = useState("")

  // Organizar arquivos em árvore
  const buildTree = (): FileTreeItem[] => {
    const tree: FileTreeItem[] = [...folders]
    
    // Adicionar arquivos raiz (sem pasta)
    const rootFiles = files.filter(f => !f.path.includes("/"))
    tree.push(...rootFiles)
    
    return tree.sort((a, b) => {
      // Pastas primeiro
      if ('type' in a && a.type === 'folder' && !('type' in b)) return -1
      if ('type' in b && b.type === 'folder' && !('type' in a)) return 1
      return a.name.localeCompare(b.name)
    })
  }

  const handleStartCreating = (type: "file" | "folder", path: string = "") => {
    setCreatingType(type)
    setCreatingPath(path)
    setNewItemName("")
  }

  const handleFinishCreating = () => {
    if (!newItemName.trim()) {
      setCreatingType(null)
      return
    }

    if (creatingType === "file") {
      onCreateFile(creatingPath, newItemName)
    } else if (creatingType === "folder") {
      onCreateFolder(creatingPath, newItemName)
    }

    setCreatingType(null)
    setNewItemName("")
    setCreatingPath("")
  }

  const handleStartRename = (id: string, currentName: string) => {
    setEditingId(id)
    setEditingName(currentName)
  }

  const handleFinishRename = (id: string, isFolder: boolean) => {
    if (editingName.trim() && editingName !== (isFolder ? folders : files).find(f => f.id === id)?.name) {
      if (isFolder) {
        onRenameFolder(id, editingName)
      } else {
        onRenameFile(id, editingName)
      }
    }
    setEditingId(null)
    setEditingName("")
  }

  const renderFileIcon = (fileName: string) => {
    const ext = fileName.split('.').pop()?.toLowerCase()
    return <File className="h-4 w-4 text-gray-500 dark:text-gray-400 shrink-0" />
  }

  const renderFolder = (folder: FileTreeFolder, level: number = 0) => {
    const isExpanded = folder.isExpanded
    const folderFiles = files.filter(f => f.path.startsWith(folder.path + "/") && 
                                          f.path.split("/").length === folder.path.split("/").length + 1)
    const subFolders = folders.filter(f => f.path.startsWith(folder.path + "/") &&
                                          f.path.split("/").length === folder.path.split("/").length + 1)
    
    return (
      <div key={folder.id}>
        <div
          className={cn(
            "flex items-center gap-1 px-2 py-1 select-none hover:bg-gray-100 dark:hover:bg-gray-800 cursor-pointer group",
            "text-sm"
          )}
          style={{ paddingLeft: `${level * 12 + 8}px` }}
        >
          <button
            onClick={() => onToggleFolder(folder.id)}
            className="p-0 hover:bg-transparent"
          >
            {isExpanded ? (
              <ChevronDown className="h-4 w-4 text-gray-500" />
            ) : (
              <ChevronRight className="h-4 w-4 text-gray-500" />
            )}
          </button>
          
          {editingId === folder.id ? (
            <Input
              value={editingName}
              onChange={(e) => setEditingName(e.target.value)}
              onBlur={() => handleFinishRename(folder.id, true)}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleFinishRename(folder.id, true)
                if (e.key === "Escape") setEditingId(null)
              }}
              className="h-6 text-xs flex-1"
              autoFocus
            />
          ) : (
            <>
              {isExpanded ? (
                <FolderOpen className="h-4 w-4 text-blue-500 shrink-0" />
              ) : (
                <Folder className="h-4 w-4 text-blue-500 shrink-0" />
              )}
              <span className="flex-1 truncate" onClick={() => onToggleFolder(folder.id)}>
                {folder.name}
              </span>
              <div className="opacity-0 group-hover:opacity-100 flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-5 w-5 p-0"
                  onClick={(e) => {
                    e.stopPropagation()
                    handleStartCreating("file", folder.path)
                  }}
                >
                  <FileText className="h-3 w-3" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-5 w-5 p-0"
                  onClick={(e) => {
                    e.stopPropagation()
                    handleStartCreating("folder", folder.path)
                  }}
                >
                  <Plus className="h-3 w-3" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-5 w-5 p-0"
                  onClick={(e) => {
                    e.stopPropagation()
                    handleStartRename(folder.id, folder.name)
                  }}
                >
                  <Edit3 className="h-3 w-3" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-5 w-5 p-0"
                  onClick={(e) => {
                    e.stopPropagation()
                    onDeleteFolder(folder.id)
                  }}
                >
                  <Trash2 className="h-3 w-3 text-red-500" />
                </Button>
              </div>
            </>
          )}
        </div>
        
        {isExpanded && (
          <div>
            {subFolders.map(subFolder => renderFolder(subFolder, level + 1))}
            {folderFiles.map(file => renderFile(file, level + 1))}
            {creatingType && creatingPath === folder.path && (
              <div 
                className="flex items-center gap-1 px-2 py-1"
                style={{ paddingLeft: `${(level + 1) * 12 + 8}px` }}
              >
                {creatingType === "folder" ? (
                  <Folder className="h-4 w-4 text-blue-500 shrink-0" />
                ) : (
                  <File className="h-4 w-4 text-gray-500 shrink-0" />
                )}
                <Input
                  value={newItemName}
                  onChange={(e) => setNewItemName(e.target.value)}
                  onBlur={handleFinishCreating}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") handleFinishCreating()
                    if (e.key === "Escape") setCreatingType(null)
                  }}
                  placeholder={creatingType === "folder" ? "Folder name" : "File name"}
                  className="h-6 text-xs flex-1"
                  autoFocus
                />
              </div>
            )}
          </div>
        )}
      </div>
    )
  }

  const renderFile = (file: CodeFile, level: number = 0) => {
    const isActive = file.id === activeFileId
    
    return (
      <div
        key={file.id}
        className={cn(
          "flex items-center gap-1 px-2 py-1 cursor-pointer group text-sm select-none",
          isActive 
            ? "bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300" 
            : "hover:bg-gray-100 dark:hover:bg-gray-800"
        )}
        style={{ paddingLeft: `${level * 12 + 24}px` }}
        onClick={() => onFileSelect(file.id)}
      >
        {editingId === file.id ? (
          <Input
            value={editingName}
            onChange={(e) => setEditingName(e.target.value)}
            onBlur={() => handleFinishRename(file.id, false)}
            onKeyDown={(e) => {
              if (e.key === "Enter") handleFinishRename(file.id, false)
              if (e.key === "Escape") setEditingId(null)
            }}
            className="h-6 text-xs flex-1"
            autoFocus
            onClick={(e) => e.stopPropagation()}
          />
        ) : (
          <>
            {renderFileIcon(file.name)}
            <span className="flex-1 truncate">{file.name}</span>
            <div className="opacity-0 group-hover:opacity-100 flex items-center gap-1">
              <Button
                variant="ghost"
                size="sm"
                className="h-5 w-5 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  handleStartRename(file.id, file.name)
                }}
              >
                <Edit3 className="h-3 w-3" />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="h-5 w-5 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  onDeleteFile(file.id)
                }}
              >
                <Trash2 className="h-3 w-3 text-red-500" />
              </Button>
            </div>
          </>
        )}
      </div>
    )
  }

  const tree = buildTree()
  const rootFiles = files.filter(f => !f.path.includes("/"))

  return (
    <div className="h-full flex flex-col bg-gray-50 dark:bg-gray-950 border-r border-gray-200 dark:border-gray-800">
      {/* Header */}
      <div className="flex items-center justify-between p-2 border-b border-gray-200 dark:border-gray-800 bg-gray-100 dark:bg-gray-900">
        <span className="text-xs font-medium text-gray-600 dark:text-gray-400">FILES</span>
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            className="h-6 w-6 p-0"
            onClick={() => handleStartCreating("file", "")}
            title="New File"
          >
            <FileText className="h-3 w-3" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="h-6 w-6 p-0"
            onClick={() => handleStartCreating("folder", "")}
            title="New Folder"
          >
            <Plus className="h-3 w-3" />
          </Button>
        </div>
      </div>

      {/* File Tree */}
      <div className="flex-1 overflow-auto">
        {folders.filter(f => !f.path.includes("/")).map(folder => renderFolder(folder))}
        {rootFiles.map(file => renderFile(file))}
        
        {creatingType && creatingPath === "" && (
          <div className="flex items-center gap-1 px-2 py-1">
            {creatingType === "folder" ? (
              <Folder className="h-4 w-4 text-blue-500 shrink-0" />
            ) : (
              <File className="h-4 w-4 text-gray-500 shrink-0" />
            )}
            <Input
              value={newItemName}
              onChange={(e) => setNewItemName(e.target.value)}
              onBlur={handleFinishCreating}
              onKeyDown={(e) => {
                if (e.key === "Enter") handleFinishCreating()
                if (e.key === "Escape") setCreatingType(null)
              }}
              placeholder={creatingType === "folder" ? "Folder name" : "File name"}
              className="h-6 text-xs flex-1"
              autoFocus
            />
          </div>
        )}
      </div>
    </div>
  )
}
