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
  GripVertical,
  Lock,
  Unlock,
  Eye,
  EyeOff,
  Package,
  Download,
  Save
} from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { DeleteConfirmDialog } from "../../dialogs/delete-confirm-dialog"
import { DuplicateNameDialog } from "../../dialogs/duplicate-name-dialog"
import { BaseConfirmDialog } from "../../dialogs/base-confirm-dialog"
import { MediaUploadDialog } from "../../media-upload-dialog"
import { FileSourceMenu } from "../file-source-menu"
import { SaveCollectionDialog } from "./save-collection-dialog"
import type { CodeFile, FileTreeFolder, FileTreeItem, FileType } from "../types"
import { cn } from "@/lib/utils"
import { assetManager } from "@/components/block-content-editor/lib/storage/assets/asset-manager"

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
  onReorderFiles?: (newOrder: CodeFile[]) => void
  onAddFileFromAsset?: (path: string, assetId: string, fileName: string, content: string) => void
  onChangeFileType?: (fileId: string, fileType: FileType) => void
  onToggleFileVisibility?: (fileId: string) => void
  onToggleFolderVisibility?: (folderId: string) => void
  onToggleFileReadonly?: (fileId: string) => void
  onToggleFolderReadonly?: (folderId: string) => void
  onToggleFocusFolder?: (folderId: string) => void
  onSetAllReadonly?: (readonly: boolean) => void
  onSetAllHidden?: (hidden: boolean) => void
  onImportCollection?: (path: string, files: Array<{ name: string; path: string; assetId: string; isFile?: 'f' | 'm' | 't'; readonly?: boolean; isVisible?: boolean }>, folderMetadata?: Map<string, { readonly?: boolean; isVisible?: boolean }>) => void
  onSaveAsCollection?: (path: string, folderName?: string) => Promise<{ success: boolean; error?: string }>
  isPreview?: boolean
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
  onReorderFiles,
  onAddFileFromAsset,
  onChangeFileType,
  onToggleFileVisibility,
  onToggleFolderVisibility,
  onToggleFileReadonly,
  onToggleFolderReadonly,
  onToggleFocusFolder,
  onSetAllReadonly,
  onSetAllHidden,
  onImportCollection,
  onSaveAsCollection,
  isPreview = false,
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
  const [showAssetDialog, setShowAssetDialog] = useState(false)
  const [assetDialogPath, setAssetDialogPath] = useState("")
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
  const [bulkActionDialog, setBulkActionDialog] = useState<{
    open: boolean
    type: "readonly" | "hidden"
    action: "set" | "unset"
  } | null>(null)
  const [showCollectionBrowser, setShowCollectionBrowser] = useState(false)
  const [collectionPath, setCollectionPath] = useState("")
  const [showSaveCollectionDialog, setShowSaveCollectionDialog] = useState(false)
  const [saveCollectionPath, setSaveCollectionPath] = useState("")
  const [saveCollectionFolderName, setSaveCollectionFolderName] = useState("")

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

  const handleOpenAssetDialog = (path: string = "") => {
    setAssetDialogPath(path)
    setShowAssetDialog(true)
  }

  const handleAssetSelected = async (results: any) => {
    if (!onAddFileFromAsset) return

    // Processar resultados (pode ser array ou objeto único)
    const assets = Array.isArray(results) ? results : [results]

    for (const asset of assets) {
      if (asset.type === "file" && asset.assetId && asset.name) {
        // Carregar conteúdo do asset
        const { assetManager } = await import("@/components/block-content-editor/lib/storage/assets/asset-manager")
        const assetData = await assetManager.getAsset(asset.assetId)
        
        if (assetData?.data) {
          // Se for um dataURL, converter para texto
          let content = ""
          if (assetData.data.startsWith("data:")) {
            // Extrair base64 e decodificar
            const base64Data = assetData.data.split(",")[1]
            if (base64Data) {
              content = atob(base64Data)
            }
          } else {
            content = assetData.data
          }

          onAddFileFromAsset(assetDialogPath, asset.assetId, asset.name, content)
        }
      }
    }

    setShowAssetDialog(false)
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

  const handleBulkReadonlyClick = () => {
    // Verificar se algum arquivo/pasta já está readonly
    const hasReadonly = files.some(f => f.readonly) || folders.some(f => f.readonly)
    setBulkActionDialog({
      open: true,
      type: "readonly",
      action: hasReadonly ? "unset" : "set"
    })
  }

  const handleBulkHiddenClick = () => {
    // Verificar se algum arquivo/pasta já está hidden
    const hasHidden = files.some(f => !f.isVisible) || folders.some(f => !f.isVisible)
    setBulkActionDialog({
      open: true,
      type: "hidden",
      action: hasHidden ? "unset" : "set"
    })
  }

  const handleConfirmBulkAction = () => {
    if (!bulkActionDialog) return

    if (bulkActionDialog.type === "readonly") {
      onSetAllReadonly?.(bulkActionDialog.action === "set")
    } else if (bulkActionDialog.type === "hidden") {
      onSetAllHidden?.(bulkActionDialog.action === "set")
    }

    setBulkActionDialog(null)
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

  const handleDrop = (e: React.DragEvent, targetId: string, targetType: "folder" | "root" | "file") => {
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

    // Se dropou arquivo sobre arquivo
    if (targetType === "file" && draggedType === "file" && onReorderFiles) {
      const draggedFile = files.find(f => f.id === draggedId)
      const targetFile = files.find(f => f.id === targetId)
      
      if (draggedFile && targetFile) {
        // Extrair o path pai de cada arquivo
        const draggedPath = draggedFile.path.substring(0, draggedFile.path.lastIndexOf('/') + 1)
        const targetPath = targetFile.path.substring(0, targetFile.path.lastIndexOf('/') + 1)
        
        // Se estão no mesmo nível, reordenar
        if (draggedPath === targetPath) {
          // Filtrar arquivos do mesmo nível
          const sameLevelFiles = files.filter(f => {
            const filePath = f.path.substring(0, f.path.lastIndexOf('/') + 1)
            return filePath === draggedPath
          })
          
          // Criar nova ordem
          const newOrder = [...sameLevelFiles]
          const draggedIndex = newOrder.findIndex(f => f.id === draggedId)
          const targetIndex = newOrder.findIndex(f => f.id === targetId)
          
          if (draggedIndex !== -1 && targetIndex !== -1) {
            // Remover da posição atual
            const [movedFile] = newOrder.splice(draggedIndex, 1)

            // Verificar se o arquivo foi encontrado
            if (!movedFile) return

            // Ajustar o índice de destino se estamos movendo para baixo
            // Quando removemos o item antes, os índices mudam
            const adjustedTargetIndex = draggedIndex < targetIndex ? targetIndex - 1 : targetIndex

            // Inserir na nova posição
            newOrder.splice(adjustedTargetIndex, 0, movedFile)
            
            // Combinar com arquivos de outros níveis mantendo ordem
            const otherFiles = files.filter(f => {
              const filePath = f.path.substring(0, f.path.lastIndexOf('/') + 1)
              return filePath !== draggedPath
            })
            
            onReorderFiles([...otherFiles, ...newOrder])
          }
          
          setDraggedItem(null)
          setDropTarget(null)
          return
        } else {
          // Se estão em níveis diferentes, mover e depois reordenar na posição do alvo
          const targetFolderPath = targetPath.slice(0, -1)
          const targetFileId = targetFile.id
          
          // Primeiro move o arquivo
          onMoveFile(draggedId, targetFolderPath)
          
          // Depois reordena para garantir a posição correta
          setTimeout(() => {
            if (!onReorderFiles) return
            
            // Simular o novo path do arquivo movido
            const draggedFileName = draggedFile.path.substring(draggedFile.path.lastIndexOf('/') + 1)
            const newDraggedPath = targetFolderPath ? `${targetFolderPath}/${draggedFileName}` : draggedFileName
            
            // Pegar todos os arquivos do nível de destino (incluindo o que acabou de ser movido)
            const targetLevelFiles = files
              .map(f => {
                // Atualizar o path do arquivo movido
                if (f.id === draggedId) {
                  return { ...f, path: newDraggedPath }
                }
                return f
              })
              .filter(f => {
                const filePath = f.path.substring(0, f.path.lastIndexOf('/') + 1)
                return filePath === targetPath
              })
            
            // Criar nova ordem: remover o movido e inserir antes do alvo
            const newOrder = targetLevelFiles.filter(f => f.id !== draggedId)
            const targetIndex = newOrder.findIndex(f => f.id === targetFileId)
            const movedFile = targetLevelFiles.find(f => f.id === draggedId)
            
            if (targetIndex !== -1 && movedFile) {
              newOrder.splice(targetIndex, 0, movedFile)
              
              // Combinar com arquivos de outros níveis
              const otherFiles = files
                .filter(f => f.id !== draggedId) // Remover o arquivo movido da lista antiga
                .filter(f => {
                  const filePath = f.path.substring(0, f.path.lastIndexOf('/') + 1)
                  return filePath !== targetPath
                })
              
              onReorderFiles([...otherFiles, ...newOrder])
            }
          }, 100)
          
          setDraggedItem(null)
          setDropTarget(null)
          return
        }
      }
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
              <span className="flex-1 truncate flex items-center gap-1" onClick={() => onToggleFolder(folder.id)}>
                {folder.name}
                {folder.isFocusFolder && (
                  <span className="text-[9px] px-1 py-0.5 rounded bg-cyan-100 dark:bg-cyan-900/30 text-cyan-600 dark:text-cyan-400" title="Focus folder for focus-editor">
                    🎯
                  </span>
                )}
                {folder.readonly && (
                  <span className="text-[9px] px-1 py-0.5 rounded bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400" title="Read-only">
                    🔒
                  </span>
                )}
                {!folder.isVisible && !isPreview && (
                  <span className="text-[9px] px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400" title="Hidden in preview">
                    🙈
                  </span>
                )}
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
                    className="absolute right-0 top-6 z-50 w-48 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg py-1"
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
                    {onAddFileFromAsset && (
                      <button
                        className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                        onClick={(e) => {
                          e.stopPropagation()
                          handleOpenAssetDialog(folder.path)
                          setOpenMenuId(null)
                        }}
                      >
                        <FileText className="h-3 w-3" />
                        Add from Assets
                      </button>
                    )}
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
                    {onToggleFolderVisibility && (
                      <button
                        className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                        onClick={(e) => {
                          e.stopPropagation()
                          onToggleFolderVisibility(folder.id)
                          setOpenMenuId(null)
                        }}
                      >
                        {folder.isVisible ? (
                          <>
                            <span className="h-3 w-3 flex items-center justify-center">👁️</span>
                            Hide in Preview
                          </>
                        ) : (
                          <>
                            <span className="h-3 w-3 flex items-center justify-center">🙈</span>
                            Show in Preview
                          </>
                        )}
                      </button>
                    )}
                    {onToggleFolderReadonly && (
                      <button
                        className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                        onClick={(e) => {
                          e.stopPropagation()
                          onToggleFolderReadonly(folder.id)
                          setOpenMenuId(null)
                        }}
                      >
                        {folder.readonly ? (
                          <>
                            <span className="h-3 w-3 flex items-center justify-center">🔓</span>
                            Allow Editing
                          </>
                        ) : (
                          <>
                            <span className="h-3 w-3 flex items-center justify-center">🔒</span>
                            Set Read-Only
                          </>
                        )}
                      </button>
                    )}
                    {onToggleFocusFolder && (
                      <button
                        className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                        onClick={(e) => {
                          e.stopPropagation()
                          onToggleFocusFolder(folder.id)
                          setOpenMenuId(null)
                        }}
                      >
                        {folder.isFocusFolder ? (
                          <>
                            <span className="h-3 w-3 flex items-center justify-center">🎯</span>
                            Unset Focus Folder
                          </>
                        ) : (
                          <>
                            <span className="h-3 w-3 flex items-center justify-center">🎯</span>
                            Set as Focus Folder
                          </>
                        )}
                      </button>
                    )}
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
                    <div className="border-t border-gray-200 dark:border-gray-700 my-1" />
                    {onImportCollection && (
                      <button
                        className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                        onClick={(e) => {
                          e.stopPropagation()
                          setCollectionPath(folder.path)
                          setShowCollectionBrowser(true)
                          setOpenMenuId(null)
                        }}
                      >
                        <Download className="h-3 w-3" />
                        Import Collection
                      </button>
                    )}
                    {onSaveAsCollection && (
                      <button
                        className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                        onClick={(e) => {
                          e.stopPropagation()
                          setSaveCollectionPath(folder.path)
                          setSaveCollectionFolderName(folder.name)
                          setShowSaveCollectionDialog(true)
                          setOpenMenuId(null)
                        }}
                      >
                        <Save className="h-3 w-3" />
                        Save as Collection
                      </button>
                    )}
                  </div>
                )}
              </div>
            </>
          )}
        </div>
        
        {isExpanded && (
          <div>
            {subFolders
              .filter(f => !isPreview || f.isVisible)
              .map(subFolder => renderFolder(subFolder, level + 1))}
            {folderFiles
              .filter(f => !isPreview || f.isVisible)
              .map((file, index) => renderFile(file, level + 1, index === folderFiles.length - 1))}
            
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

  const renderFile = (file: CodeFile, level: number = 0, isLastInLevel: boolean = false) => {
    const isActive = file.id === activeFileId
    const isDragging = draggedItem?.id === file.id
    const isDropTarget = dropTarget === file.id
    const isDropTargetAfter = dropTarget === `${file.id}-after`
    
    return (
      <div key={file.id}>
        <div
          className={cn(
            "flex items-center gap-1 px-2 py-1 cursor-pointer group text-sm select-none",
            isActive 
              ? "bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300" 
              : "hover:bg-gray-100 dark:hover:bg-gray-800",
            isDragging && "opacity-50",
            isDropTarget && "border-t-2 border-blue-500"
          )}
          style={{ paddingLeft: `${level * 12 + 8}px` }}
          onClick={() => onFileSelect(file.id)}
          draggable={dragEnabled && !editingId}
          onDragStart={(e) => handleDragStart(e, file.id, "file")}
          onDragOver={(e) => handleDragOver(e, file.id)}
          onDragLeave={handleDragLeave}
          onDrop={(e) => handleDrop(e, file.id, "file")}
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
            <span className="flex-1 truncate flex items-center gap-1">
              {file.name}
              {file.readonly && (
                <span className="text-[9px] px-1 py-0.5 rounded bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400" title="Read-only">
                  🔒
                </span>
              )}
              {!file.isVisible && !isPreview && (
                <span className="text-[9px] px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400" title="Hidden in preview">
                  🙈
                </span>
              )}
              {file.isFile === 'm' && (
                <span className="text-[9px] px-1 py-0.5 rounded bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400" title="Main file (Project)">
                  M
                </span>
              )}
              {file.isFile === 't' && (
                <span className="text-[9px] px-1 py-0.5 rounded bg-purple-100 dark:bg-purple-900/30 text-purple-600 dark:text-purple-400" title="Test file">
                  T
                </span>
              )}
              {file.assetId && (
                <span className="text-[9px] px-1 py-0.5 rounded bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400" title="From assets">
                  A
                </span>
              )}
              {file.isModified && (
                <span className="text-[9px] px-1 py-0.5 rounded bg-yellow-100 dark:bg-yellow-900/30 text-yellow-600 dark:text-yellow-400" title="Modified">
                  ●
                </span>
              )}
            </span>
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
                  {onChangeFileType && (
                    <>
                      <div className="border-t border-gray-200 dark:border-gray-700 my-1" />
                      <div className="px-3 py-1 text-xs text-gray-500 dark:text-gray-400 font-medium">Mark as:</div>
                      <button
                        className={cn(
                          "w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2",
                          file.isFile === 'f' && "bg-gray-100 dark:bg-gray-700"
                        )}
                        onClick={(e) => {
                          e.stopPropagation()
                          onChangeFileType(file.id, 'f')
                          setOpenMenuId(null)
                        }}
                      >
                        {file.isFile === 'f' && <span className="text-blue-600 dark:text-blue-400">✓</span>}
                        <span className={file.isFile !== 'f' ? 'ml-5' : ''}>Regular File</span>
                      </button>
                      <button
                        className={cn(
                          "w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2",
                          file.isFile === 'm' && "bg-gray-100 dark:bg-gray-700"
                        )}
                        onClick={(e) => {
                          e.stopPropagation()
                          onChangeFileType(file.id, 'm')
                          setOpenMenuId(null)
                        }}
                      >
                        {file.isFile === 'm' && <span className="text-green-600 dark:text-green-400">✓</span>}
                        <span className={file.isFile !== 'm' ? 'ml-5' : ''}>Main File (Project)</span>
                      </button>
                      <button
                        className={cn(
                          "w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2",
                          file.isFile === 't' && "bg-gray-100 dark:bg-gray-700"
                        )}
                        onClick={(e) => {
                          e.stopPropagation()
                          onChangeFileType(file.id, 't')
                          setOpenMenuId(null)
                        }}
                      >
                        {file.isFile === 't' && <span className="text-purple-600 dark:text-purple-400">✓</span>}
                        <span className={file.isFile !== 't' ? 'ml-5' : ''}>Test File</span>
                      </button>
                      <div className="border-t border-gray-200 dark:border-gray-700 my-1" />
                    </>
                  )}
                  {onToggleFileVisibility && (
                    <button
                      className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                      onClick={(e) => {
                        e.stopPropagation()
                        onToggleFileVisibility(file.id)
                        setOpenMenuId(null)
                      }}
                    >
                      {file.isVisible ? (
                        <>
                          <span className="h-3 w-3 flex items-center justify-center">👁️</span>
                          Hide in Preview
                        </>
                      ) : (
                        <>
                          <span className="h-3 w-3 flex items-center justify-center">🙈</span>
                          Show in Preview
                        </>
                      )}
                    </button>
                  )}
                  {onToggleFileReadonly && (
                    <button
                      className="w-full px-3 py-1.5 text-left text-xs hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
                      onClick={(e) => {
                        e.stopPropagation()
                        onToggleFileReadonly(file.id)
                        setOpenMenuId(null)
                      }}
                    >
                      {file.readonly ? (
                        <>
                          <span className="h-3 w-3 flex items-center justify-center">🔓</span>
                          Allow Editing
                        </>
                      ) : (
                        <>
                          <span className="h-3 w-3 flex items-center justify-center">🔒</span>
                          Set Read-Only
                        </>
                      )}
                    </button>
                  )}
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
        
        {/* Drop zone after last file */}
        {isLastInLevel && draggedItem?.type === "file" && (
          <div
            className={cn(
              "h-4 transition-all",
              isDropTargetAfter && "h-8 bg-blue-50 dark:bg-blue-900/20 border-2 border-dashed border-blue-500"
            )}
            style={{ paddingLeft: `${level * 12 + 8}px` }}
            onDragOver={(e) => {
              if (!dragEnabled || !draggedItem) return
              e.preventDefault()
              e.stopPropagation()
              e.dataTransfer.dropEffect = "move"
              setDropTarget(`${file.id}-after`)
            }}
            onDragLeave={() => setDropTarget(null)}
            onDrop={(e) => {
              e.preventDefault()
              e.stopPropagation()
              if (!dragEnabled || !draggedItem || draggedItem.type !== "file") return
              
              // Get the files in the same level as target
              const filePath = file.path.substring(0, file.path.lastIndexOf('/') + 1)
              const sameLevelFiles = files.filter(f => {
                const fPath = f.path.substring(0, f.path.lastIndexOf('/') + 1)
                return fPath === filePath
              })
              
              const draggedFile = files.find(f => f.id === draggedItem.id)
              if (!draggedFile) return
              
              // Check if same level
              const draggedPath = draggedFile.path.substring(0, draggedFile.path.lastIndexOf('/') + 1)
              
              if (draggedPath === filePath) {
                // Same level - reorder
                const newOrder = [...sameLevelFiles]
                const draggedIndex = newOrder.findIndex(f => f.id === draggedItem.id)
                const targetIndex = newOrder.findIndex(f => f.id === file.id)
                
                if (draggedIndex !== -1 && targetIndex !== -1) {
                  const [movedFile] = newOrder.splice(draggedIndex, 1)
                  if (!movedFile) return
                  
                  // Insert after target (no adjustment needed since we're going after)
                  newOrder.splice(targetIndex + (draggedIndex < targetIndex ? 0 : 1), 0, movedFile)
                  
                  const otherFiles = files.filter(f => {
                    const fPath = f.path.substring(0, f.path.lastIndexOf('/') + 1)
                    return fPath !== filePath
                  })
                  
                  if (onReorderFiles) {
                    onReorderFiles([...otherFiles, ...newOrder])
                  }
                }
              } else {
                // Different level - move to target folder and reorder after target file
                const targetFolderPath = filePath.slice(0, -1)
                const targetFileId = file.id
                const draggedFileId = draggedItem.id
                
                // Primeiro move o arquivo
                onMoveFile(draggedFileId, targetFolderPath)
                
                // Depois reordena para garantir a posição após o arquivo alvo
                setTimeout(() => {
                  if (!onReorderFiles) return
                  
                  // Simular o novo path do arquivo movido
                  const draggedFileName = draggedFile.path.substring(draggedFile.path.lastIndexOf('/') + 1)
                  const newDraggedPath = targetFolderPath ? `${targetFolderPath}/${draggedFileName}` : draggedFileName
                  
                  // Pegar todos os arquivos do nível de destino (incluindo o que acabou de ser movido)
                  const targetLevelFiles = files
                    .map(f => {
                      // Atualizar o path do arquivo movido
                      if (f.id === draggedFileId) {
                        return { ...f, path: newDraggedPath }
                      }
                      return f
                    })
                    .filter(f => {
                      const fPath = f.path.substring(0, f.path.lastIndexOf('/') + 1)
                      return fPath === filePath
                    })
                  
                  // Criar nova ordem: remover o movido e inserir após o alvo
                  const newOrder = targetLevelFiles.filter(f => f.id !== draggedFileId)
                  const targetIndex = newOrder.findIndex(f => f.id === targetFileId)
                  const movedFile = targetLevelFiles.find(f => f.id === draggedFileId)
                  
                  if (targetIndex !== -1 && movedFile) {
                    // Inserir após o arquivo alvo
                    newOrder.splice(targetIndex + 1, 0, movedFile)
                    
                    // Combinar com arquivos de outros níveis
                    const otherFiles = files
                      .filter(f => f.id !== draggedFileId) // Remover o arquivo movido da lista antiga
                      .filter(f => {
                        const fPath = f.path.substring(0, f.path.lastIndexOf('/') + 1)
                        return fPath !== filePath
                      })
                    
                    onReorderFiles([...otherFiles, ...newOrder])
                  }
                }, 100)
              }
              
              setDraggedItem(null)
              setDropTarget(null)
            }}
          />
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
          {onSetAllReadonly && (
            <Button
              variant="ghost"
              size="sm"
              className="h-6 w-6 p-0"
              onClick={handleBulkReadonlyClick}
              title="Toggle Read-Only for All"
            >
              {files.some(f => f.readonly) || folders.some(f => f.readonly) ? (
                <Unlock className="h-3 w-3" />
              ) : (
                <Lock className="h-3 w-3" />
              )}
            </Button>
          )}
          {onSetAllHidden && (
            <Button
              variant="ghost"
              size="sm"
              className="h-6 w-6 p-0"
              onClick={handleBulkHiddenClick}
              title="Toggle Visibility for All"
            >
              {files.some(f => !f.isVisible) || folders.some(f => !f.isVisible) ? (
                <Eye className="h-3 w-3" />
              ) : (
                <EyeOff className="h-3 w-3" />
              )}
            </Button>
          )}
          {onAddFileFromAsset ? (
            <FileSourceMenu
              onCreateNew={() => handleStartCreating("file", "")}
              onAddFromAssets={() => handleOpenAssetDialog("")}
              trigger={
                <Button variant="ghost" size="sm" className="h-6 w-6 p-0" title="Add File">
                  <FileText className="h-3 w-3" />
                </Button>
              }
            />
          ) : (
            <Button
              variant="ghost"
              size="sm"
              className="h-6 w-6 p-0"
              onClick={() => handleStartCreating("file", "")}
              title="New File"
            >
              <FileText className="h-3 w-3" />
            </Button>
          )}
          <Button
            variant="ghost"
            size="sm"
            className="h-6 w-6 p-0"
            onClick={() => handleStartCreating("folder", "")}
            title="New Folder"
          >
            <Plus className="h-3 w-3" />
          </Button>
          {onImportCollection && (
            <Button
              variant="ghost"
              size="sm"
              className="h-6 w-6 p-0"
              onClick={() => {
                setCollectionPath("")
                setShowCollectionBrowser(true)
              }}
              title="Import Collection"
            >
              <Download className="h-3 w-3" />
            </Button>
          )}
          {onSaveAsCollection && (
            <Button
              variant="ghost"
              size="sm"
              className="h-6 w-6 p-0"
              onClick={() => {
                setSaveCollectionPath("")
                setSaveCollectionFolderName("")
                setShowSaveCollectionDialog(true)
              }}
              title="Save as Collection"
            >
              <Package className="h-3 w-3" />
            </Button>
          )}
        </div>
      </div>

      {/* File Tree */}
      <div className="flex-1 overflow-auto">
        {folders
          .filter(f => !f.path.includes("/"))
          .filter(f => !isPreview || f.isVisible)
          .map(folder => renderFolder(folder))}
        {rootFiles
          .filter(f => !isPreview || f.isVisible)
          .map((file, index) => renderFile(file, 0, index === rootFiles.length - 1))}
        
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

      {/* Bulk Action Confirmation Dialog */}
      {bulkActionDialog && (
        <BaseConfirmDialog
          open={bulkActionDialog.open}
          onOpenChange={(open) => !open && setBulkActionDialog(null)}
          title={
            bulkActionDialog.type === "readonly"
              ? bulkActionDialog.action === "set"
                ? "Set All Files Read-Only"
                : "Allow Editing for All Files"
              : bulkActionDialog.action === "set"
              ? "Hide All Files in Preview"
              : "Show All Files in Preview"
          }
          description={
            bulkActionDialog.type === "readonly"
              ? bulkActionDialog.action === "set"
                ? "This will make all files and folders read-only. You won't be able to edit them."
                : "This will allow editing for all files and folders."
              : bulkActionDialog.action === "set"
              ? "This will hide all files and folders in preview mode."
              : "This will show all files and folders in preview mode."
          }
          onConfirm={handleConfirmBulkAction}
          confirmText={bulkActionDialog.action === "set" ? "Apply" : "Apply"}
          icon={
            bulkActionDialog.type === "readonly" ? (
              bulkActionDialog.action === "set" ? (
                <Lock className="h-12 w-12 text-orange-500" />
              ) : (
                <Unlock className="h-12 w-12 text-green-500" />
              )
            ) : bulkActionDialog.action === "set" ? (
              <EyeOff className="h-12 w-12 text-gray-500" />
            ) : (
              <Eye className="h-12 w-12 text-blue-500" />
            )
          }
        />
      )}

      {/* Media Upload Dialog for Code Files */}
      {onAddFileFromAsset && (
        <MediaUploadDialog
          open={showAssetDialog}
          onOpenChange={setShowAssetDialog}
          onMediaSelected={handleAssetSelected}
          title="Add Code File from Assets"
          acceptTypes="*/*"
          urlPlaceholder="https://example.com/code-file.js"
          uploadLabel="Select a code file from your device"
          urlLabel="Enter the URL of the code file"
          multiple={true}
          compress={false}
          allowCompressionToggle={false}
          forceTextStorage={true}
        />
      )}

      {/* Collection Browser Dialog */}
      {onImportCollection && showCollectionBrowser && (
        <MediaUploadDialog
          open={showCollectionBrowser}
          onOpenChange={setShowCollectionBrowser}
          title="Import Collection"
          sources={{ collections: true, files: false, url: false }}
          onMediaSelected={async (result) => {
            const results = Array.isArray(result) ? result : [result]
            
            for (const res of results) {
              if (res.data.startsWith('collection://')) {
                const collectionId = res.data.replace('collection://', '')
                
                try {
                  // Load collection manifest
                  const manifest = await assetManager.getCollection(collectionId)
                  
                  if (manifest) {
                    // Extract files from collection structure
                    const files: Array<{ name: string; path: string; assetId: string; isFile?: 'f' | 'm' | 't'; readonly?: boolean; isVisible?: boolean }> = []
                    const folderMetadata = new Map<string, { readonly?: boolean; isVisible?: boolean }>()
                    
                    const collectFiles = (folder: any, basePath = '') => {
                      // Store folder metadata
                      if (folder.path) {
                        folderMetadata.set(folder.path, {
                          readonly: folder.readonly,
                          isVisible: folder.isVisible,
                        })
                      }
                      
                      // Add files from this folder
                      if (folder.files) {
                        folder.files.forEach((file: any) => {
                          files.push({
                            name: file.name,
                            path: file.path,
                            assetId: file.assetId || '',
                            isFile: file.isFile,
                            readonly: file.readonly,
                            isVisible: file.isVisible,
                          })
                        })
                      }
                      
                      // Recursively collect from subfolders
                      if (folder.folders) {
                        folder.folders.forEach((subfolder: any) => {
                          collectFiles(subfolder, folder.path || '')
                        })
                      }
                    }
                    
                    collectFiles(manifest.structure)
                    
                    // Import files
                    onImportCollection(collectionPath, files, folderMetadata)
                  }
                } catch (error) {
                  console.error('Failed to import collection:', error)
                }
              }
            }
            
            setShowCollectionBrowser(false)
          }}
        />
      )}

      {/* Save Collection Dialog */}
      {onSaveAsCollection && (
        <SaveCollectionDialog
          open={showSaveCollectionDialog}
          onOpenChange={setShowSaveCollectionDialog}
          onSave={async (params) => {
            if (onSaveAsCollection) {
              return await onSaveAsCollection(saveCollectionPath, saveCollectionFolderName || params.name)
            }
            return { success: false, error: "Handler not available" }
          }}
          folderName={saveCollectionFolderName}
        />
      )}
    </div>
  )
}
