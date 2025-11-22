"use client"

import { useState, useEffect } from "react"
import { 
  ChevronRight, 
  ChevronDown, 
  File, 
  Folder, 
  FolderOpen,
  Plus,
  FileText,
  Trash2,
  Edit3,
  MoreVertical,
  GripVertical
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { DeleteConfirmDialog } from "../../dialogs/delete-confirm-dialog"
import { DuplicateNameDialog } from "../../dialogs/duplicate-name-dialog"
import type { CodeFile, FileTreeFolder, FileTreeItem } from "../types"
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
  onMoveFile: (fileId: string, newPath: string) => void
  onMoveFolder: (folderId: string, newPath: string) => void
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
  onMoveFile,
  onMoveFolder,
}: FileExplorerProps) {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState("")
  const [creatingType, setCreatingType] = useState<"file" | "folder" | null>(null)
  const [creatingPath, setCreatingPath] = useState("")
  const [newItemName, setNewItemName] = useState("")
  const [openMenuId, setOpenMenuId] = useState<string | null>(null)
  const [dragEnabled, setDragEnabled] = useState(false)
  const [draggedItem, setDraggedItem] = useState<{ id: string; type: "file" | "folder" } | null>(null)
  const [dropTarget, setDropTarget] = useState<string | null>(null)
  const [deleteDialog, setDeleteDialog] = useState<{
    open: boolean
    type: "file" | "folder"
    id: string
    name: string
  } | null>(null)
  const [duplicateDialog, setDuplicateDialog] = useState<{
    open: boolean
    type: "file" | "folder"
    originalName: string
    path: string
    mode: "create" | "rename"
    itemId?: string
  } | null>(null)

  // Fechar menu ao clicar fora
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as HTMLElement
      if (openMenuId && !target.closest('.context-menu-container')) {
        setOpenMenuId(null)
      }
    }
    
    if (openMenuId) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [openMenuId])

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
      // Verificar se já existe arquivo com mesmo nome no mesmo caminho
      const fullPath = creatingPath ? `${creatingPath}/${newItemName}` : newItemName
      const fileExists = files.some(f => f.path === fullPath)
      
      if (fileExists) {
        setDuplicateDialog({
          open: true,
          type: "file",
          originalName: newItemName,
          path: creatingPath,
          mode: "create",
        })
        return
      }
      
      onCreateFile(creatingPath, newItemName)
    } else if (creatingType === "folder") {
      // Verificar se já existe pasta com mesmo nome no mesmo caminho
      const fullPath = creatingPath ? `${creatingPath}/${newItemName}` : newItemName
      const folderExists = folders.some(f => f.path === fullPath)
      
      if (folderExists) {
        setDuplicateDialog({
          open: true,
          type: "folder",
          originalName: newItemName,
          path: creatingPath,
          mode: "create",
        })
        return
      }
      
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
        const folder = folders.find(f => f.id === id)
        if (!folder) return
        
        const pathParts = folder.path.split('/')
        pathParts[pathParts.length - 1] = editingName
        const newPath = pathParts.join('/')
        
        // Verificar se já existe pasta com mesmo nome no mesmo nível
        const folderExists = folders.some(f => f.path === newPath && f.id !== id)
        if (folderExists) {
          setDuplicateDialog({
            open: true,
            type: "folder",
            originalName: editingName,
            path: pathParts.slice(0, -1).join('/'),
            mode: "rename",
            itemId: id,
          })
          setEditingId(null)
          setEditingName("")
          return
        }
        
        onRenameFolder(id, editingName)
      } else {
        const file = files.find(f => f.id === id)
        if (!file) return
        
        const pathParts = file.path.split('/')
        pathParts[pathParts.length - 1] = editingName
        const newPath = pathParts.join('/')
        
        // Verificar se já existe arquivo com mesmo nome no mesmo nível
        const fileExists = files.some(f => f.path === newPath && f.id !== id)
        if (fileExists) {
          setDuplicateDialog({
            open: true,
            type: "file",
            originalName: editingName,
            path: pathParts.slice(0, -1).join('/'),
            mode: "rename",
            itemId: id,
          })
          setEditingId(null)
          setEditingName("")
          return
        }
        
        onRenameFile(id, editingName)
      }
    }
    setEditingId(null)
    setEditingName("")
  }

  const handleDuplicateConfirm = (newName: string) => {
    if (!duplicateDialog) return

    const { type, path, mode, itemId } = duplicateDialog

    if (mode === "create") {
      // Criar com o novo nome
      if (type === "file") {
        onCreateFile(path, newName)
      } else {
        onCreateFolder(path, newName)
      }
      setCreatingType(null)
      setNewItemName("")
      setCreatingPath("")
    } else if (mode === "rename" && itemId) {
      // Renomear com o novo nome
      if (type === "file") {
        onRenameFile(itemId, newName)
      } else {
        onRenameFolder(itemId, newName)
      }
    }

    setDuplicateDialog(null)
  }

  const handleDuplicateCancel = () => {
    setDuplicateDialog(null)
    // Se estava criando, não limpar os campos para permitir correção
    // Se estava renomeando, já foi limpo antes de abrir o dialog
  }

  // Drag and Drop handlers
  const handleDragStart = (e: React.DragEvent, id: string, type: "file" | "folder") => {
    if (!dragEnabled) return
    setDraggedItem({ id, type })
    e.dataTransfer.effectAllowed = "move"
  }

  const handleDragOver = (e: React.DragEvent, targetId: string) => {
    if (!dragEnabled || !draggedItem) return
    e.preventDefault()
    e.dataTransfer.dropEffect = "move"
    setDropTarget(targetId)
  }

  const handleDragLeave = () => {
    setDropTarget(null)
  }

  const handleDrop = (e: React.DragEvent, targetId: string, targetType: "folder" | "root") => {
    e.preventDefault()
    e.stopPropagation()
    if (!dragEnabled || !draggedItem) return

    const { id: draggedId, type: draggedType } = draggedItem

    // Não permitir dropar em si mesmo
    if (draggedId === targetId) {
      setDraggedItem(null)
      setDropTarget(null)
      return
    }

    // Determinar o novo path
    let newPath = ""
    if (targetType === "folder") {
      const targetFolder = folders.find(f => f.id === targetId)
      if (targetFolder) {
        newPath = targetFolder.path
        
        // Verificar se está tentando mover uma pasta para dentro dela mesma ou de seus descendentes
        if (draggedType === "folder") {
          const draggedFolder = folders.find(f => f.id === draggedId)
          if (draggedFolder) {
            // Não permitir mover para si mesma ou para suas subpastas
            if (newPath === draggedFolder.path || newPath.startsWith(draggedFolder.path + "/")) {
              setDraggedItem(null)
              setDropTarget(null)
              return
            }
          }
        }
      }
    }

    // Executar a movimentação
    if (draggedType === "file") {
      onMoveFile(draggedId, newPath)
    } else {
      onMoveFolder(draggedId, newPath)
    }

    setDraggedItem(null)
    setDropTarget(null)
  }

  const handleDragEnd = () => {
    setDraggedItem(null)
    setDropTarget(null)
  }

  const handleDeleteClick = (id: string, name: string, type: "file" | "folder") => {
    setDeleteDialog({
      open: true,
      type,
      id,
      name,
    })
    setOpenMenuId(null)
  }

  const handleConfirmDelete = () => {
    if (!deleteDialog) return

    if (deleteDialog.type === "folder") {
      onDeleteFolder(deleteDialog.id)
    } else {
      onDeleteFile(deleteDialog.id)
    }

    setDeleteDialog(null)
  }

  const countFolderContents = (folderId: string): { files: number; folders: number } => {
    const folder = folders.find(f => f.id === folderId)
    if (!folder) return { files: 0, folders: 0 }

    const filesInFolder = files.filter(f => f.path.startsWith(folder.path + "/"))
    const foldersInFolder = folders.filter(f => f.path.startsWith(folder.path + "/") && f.id !== folderId)

    return {
      files: filesInFolder.length,
      folders: foldersInFolder.length,
    }
  }

  const getDeleteDescription = (): string => {
    if (!deleteDialog) return ""

    if (deleteDialog.type === "folder") {
      const contents = countFolderContents(deleteDialog.id)
      const totalItems = contents.files + contents.folders

      if (totalItems === 0) {
        return `Are you sure you want to delete "${deleteDialog.name}"? This folder will be permanently removed and cannot be recovered.`
      }

      const itemsText = []
      if (contents.files > 0) {
        itemsText.push(`${contents.files} file${contents.files > 1 ? "s" : ""}`)
      }
      if (contents.folders > 0) {
        itemsText.push(`${contents.folders} subfolder${contents.folders > 1 ? "s" : ""}`)
      }

      return `Are you sure you want to delete "${deleteDialog.name}"? This folder contains ${itemsText.join(" and ")} that will also be permanently removed and cannot be recovered.`
    }

    return `Are you sure you want to delete "${deleteDialog.name}"? This file will be permanently removed and cannot be recovered.`
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
    
    const isDragging = draggedItem?.id === folder.id
    const isDropTarget = dropTarget === folder.id
    
    return (
      <div key={folder.id}>
        <div
          className={cn(
            "flex items-center gap-1 px-2 py-1 select-none hover:bg-gray-100 dark:hover:bg-gray-800 cursor-pointer group",
            "text-sm",
            isDragging && "opacity-50",
            isDropTarget && "bg-blue-50 dark:bg-blue-900/20 border-l-2 border-blue-500"
          )}
          style={{ paddingLeft: `${level * 12 + 8}px` }}
          draggable={dragEnabled && !editingId}
          onDragStart={(e) => handleDragStart(e, folder.id, "folder")}
          onDragOver={(e) => handleDragOver(e, folder.id)}
          onDragLeave={handleDragLeave}
          onDrop={(e) => handleDrop(e, folder.id, "folder")}
          onDragEnd={handleDragEnd}
        >
          {dragEnabled && !editingId && (
            <GripVertical className="h-3 w-3 text-gray-400 shrink-0 cursor-grab active:cursor-grabbing" />
          )}
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
              <div className="opacity-0 group-hover:opacity-100 flex items-center gap-1 relative context-menu-container">
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-5 w-5 p-0"
                  onClick={(e) => {
                    e.stopPropagation()
                    setOpenMenuId(openMenuId === folder.id ? null : folder.id)
                  }}
                >
                  <MoreVertical className="h-3 w-3" />
                </Button>
                
                {openMenuId === folder.id && (
                  <div 
                    className="absolute right-0 top-6 z-50 w-40 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg py-1"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <button
                      className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                      onClick={(e) => {
                        e.stopPropagation()
                        handleStartCreating("file", folder.path)
                        setOpenMenuId(null)
                      }}
                    >
                      <FileText className="h-3 w-3" />
                      New File
                    </button>
                    <button
                      className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                      onClick={(e) => {
                        e.stopPropagation()
                        handleStartCreating("folder", folder.path)
                        setOpenMenuId(null)
                      }}
                    >
                      <Plus className="h-3 w-3" />
                      New Folder
                    </button>
                    <button
                      className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                      onClick={(e) => {
                        e.stopPropagation()
                        handleStartRename(folder.id, folder.name)
                        setOpenMenuId(null)
                      }}
                    >
                      <Edit3 className="h-3 w-3" />
                      Rename
                    </button>
                    <button
                      className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2 text-red-600 dark:text-red-400"
                      onClick={(e) => {
                        e.stopPropagation()
                        handleDeleteClick(folder.id, folder.name, "folder")
                      }}
                    >
                      <Trash2 className="h-3 w-3" />
                      Delete
                    </button>
                  </div>
                )}
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
    const isDragging = draggedItem?.id === file.id
    
    return (
      <div
        key={file.id}
        className={cn(
          "flex items-center gap-1 px-2 py-1 cursor-pointer group text-sm select-none",
          isActive 
            ? "bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300" 
            : "hover:bg-gray-100 dark:hover:bg-gray-800",
          isDragging && "opacity-50"
        )}
        style={{ paddingLeft: `${level * 12 + 24}px` }}
        onClick={() => onFileSelect(file.id)}
        draggable={dragEnabled && !editingId}
        onDragStart={(e) => handleDragStart(e, file.id, "file")}
        onDragEnd={handleDragEnd}
      >
        {dragEnabled && !editingId && (
          <GripVertical className="h-3 w-3 text-gray-400 shrink-0 cursor-grab active:cursor-grabbing" />
        )}
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
            <div className="opacity-0 group-hover:opacity-100 flex items-center gap-1 relative context-menu-container">
              <Button
                variant="ghost"
                size="sm"
                className="h-5 w-5 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  setOpenMenuId(openMenuId === file.id ? null : file.id)
                }}
              >
                <MoreVertical className="h-3 w-3" />
              </Button>
              
              {openMenuId === file.id && (
                <div 
                  className="absolute right-0 top-6 z-50 w-40 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg py-1"
                  onClick={(e) => e.stopPropagation()}
                >
                  <button
                    className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                    onClick={(e) => {
                      e.stopPropagation()
                      handleStartRename(file.id, file.name)
                      setOpenMenuId(null)
                    }}
                  >
                    <Edit3 className="h-3 w-3" />
                    Rename
                  </button>
                  <button
                    className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2 text-red-600 dark:text-red-400"
                    onClick={(e) => {
                      e.stopPropagation()
                      handleDeleteClick(file.id, file.name, "file")
                    }}
                  >
                    <Trash2 className="h-3 w-3" />
                    Delete
                  </button>
                </div>
              )}
            </div>
          </>
        )}
      </div>
    )
  }

  const tree = buildTree()
  const rootFiles = files.filter(f => !f.path.includes("/"))

  return (
    <div 
      className="h-full flex flex-col bg-gray-50 dark:bg-gray-950 border-r border-gray-200 dark:border-gray-800"
      onDragOver={(e) => {
        if (dragEnabled && draggedItem) {
          e.preventDefault()
        }
      }}
      onDrop={(e) => handleDrop(e, "", "root")}
    >
      {/* Header */}
      <div className="flex items-center justify-between p-2 border-b border-gray-200 dark:border-gray-800 bg-gray-100 dark:bg-gray-900">
        <div className="flex items-center gap-2">
          <span className="text-xs font-medium text-gray-600 dark:text-gray-400">FILES</span>
          <div className="flex items-center gap-1.5">
            <Switch
              checked={dragEnabled}
              onCheckedChange={setDragEnabled}
              className="scale-75"
            />
            <span className="text-[10px] text-gray-500 dark:text-gray-500">
              {dragEnabled ? "Drag enabled" : "Drag disabled"}
            </span>
          </div>
        </div>
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

      {/* Delete Confirmation Dialog */}
      {deleteDialog && (
        <DeleteConfirmDialog
          open={deleteDialog.open}
          onOpenChange={(open) => !open && setDeleteDialog(null)}
          title={deleteDialog.type === "folder" ? "Delete Folder" : "Delete File"}
          itemName={deleteDialog.name}
          itemType={deleteDialog.type}
          onConfirm={handleConfirmDelete}
          description={getDeleteDescription()}
        />
      )}

      {/* Duplicate Name Dialog */}
      {duplicateDialog && (
        <DuplicateNameDialog
          open={duplicateDialog.open}
          onOpenChange={(open) => !open && setDuplicateDialog(null)}
          itemType={duplicateDialog.type}
          originalName={duplicateDialog.originalName}
          onConfirm={handleDuplicateConfirm}
          onCancel={handleDuplicateCancel}
        />
      )}
    </div>
  )
}
