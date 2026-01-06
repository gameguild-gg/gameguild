"use client"

import { useEffect, useRef } from "react"
import { useImmer } from "use-immer"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { X, Save, Code2, Menu, Lock, Layout } from "lucide-react"
import type { CodeStudioData, CodeFile, FileTreeFolder, PanelConfig, DisplayConfig, PanelType, AspectRatio } from "./types"
import { MonacoCodeEditor } from "./monaco-code-editor"
import { ResultPanel } from "./result-panel"
import type { XTermTerminalHandle } from "./xterm-terminal"
import { MODE_CONFIGS, LANGUAGE_CONFIGS, getLanguageFromExtension, hasValidExtension } from "./types"
import { useTheme } from "next-themes"
import { FileExplorer } from "./file-system/file-explorer"
import { FileTabs } from "./file-tabs"
import { SettingsMenu } from "./settings-menu"
import { ResizablePanel } from "./resizable-panel"
import { GridDropZone } from "./grid-drop-zone"
import { DisplayManager } from "./display-manager"
import { EditorInstanceSwitch } from "./editor-instance-switch"
import { EmptyEditorState } from "./empty-editor-state"
import { cn } from "@/lib/utils"
import { createDefaultLayout } from "./default-layouts"
import * as FileOps from "./file-operations"
import * as LayoutOps from "./layout-operations"
import * as TabOps from "./tab-operations"
import * as PanelOps from "./panel-operations"
import { getGridDimensions, getContainerDimensions } from "./grid-utils"
import { UnifiedCodeRunner, setDownloadNotificationCallback } from "./runners"
import { initializeMonacoFileSystem, syncFilesToMonacoFS, updateMonacoFile, disposeMonacoFileSystem } from "./monaco-file-system"
import { saveProjectAsCollection, countAssetReferences } from "./file-system/collection-utils"
import { assetManager } from "@/lib/storage/assets/asset-manager"

interface CodeStudioEditorProps {
  data: CodeStudioData
  isPreview?: boolean
  onUpdate?: (data: Partial<CodeStudioData>) => void
  onSave?: (data: CodeStudioData) => void
  onCancel?: () => void
  onEdit?: () => void
}

export function CodeStudioEditor({ 
  data, 
  isPreview = false, 
  onUpdate, 
  onSave, 
  onCancel,
  onEdit,
}: CodeStudioEditorProps) {
  const { resolvedTheme } = useTheme()
  const isDarkMode = resolvedTheme === "dark"
  
  const [localData, setLocalData] = useImmer<CodeStudioData>(() => {
    // Criar layout padrão se não existir
    if (!data.layout) {
      return {
        ...data,
        mode: data.mode || "execution",
        layout: createDefaultLayout(data.mode || "execution"),
      }
    }
    return data
  })
  const [isExecuting, setIsExecuting] = useImmer(false)
  const [output, setOutput] = useImmer<string>("")
  const [showSettingsMenu, setShowSettingsMenu] = useImmer(false)
  const [resolvedContents, setResolvedContents] = useImmer<Record<string, string>>({})
  const gridContainerRef = useRef<HTMLDivElement | null>(null)
  const codeRunnerRef = useRef<UnifiedCodeRunner | null>(null)
  const terminalRef = useRef<XTermTerminalHandle | null>(null)
  const initializedRef = useRef(false)

  // Initialize runner and Monaco file system
  useEffect(() => {
    // Setup download notification callback
    setDownloadNotificationCallback((message: string, isDownloading: boolean) => {
      // Mostrar no terminal
      if (terminalRef.current) {
        if (isDownloading) {
          terminalRef.current.write(`\r\n\x1b[33m📥 ${message}\x1b[0m\r\n`)
        } else {
          terminalRef.current.write(`\x1b[32m✓ ${message}\x1b[0m\r\n\r\n`)
          // Limpar o terminal após 2 segundos
          setTimeout(() => {
            if (terminalRef.current) {
              terminalRef.current.write('\x1b[2J\x1b[H') // Clear screen
            }
          }, 2000)
        }
      }
    })

    codeRunnerRef.current = new UnifiedCodeRunner({ 
      timeout: 30000,
      onRequestInput: async (prompt?: string, currentOutput?: string) => {
        if (terminalRef.current) {
          // Write current output to terminal before requesting input
          if (currentOutput) {
            terminalRef.current.write(currentOutput.replace(/\n/g, '\r\n') + '\r\n')
          }
          // Write prompt if provided
          if (prompt) {
            terminalRef.current.write(prompt)
          }
          return await terminalRef.current.requestInput()
        }
        return ""
      },
      onProgress: (message: string) => {
        // Atualizar terminal com mensagens de progresso
        console.log('[Progress]', message, 'terminal:', !!terminalRef.current)
        if (terminalRef.current) {
          try {
            terminalRef.current.write(`\x1b[36m${message}\x1b[0m\r\n`)
          } catch (e) {
            console.error('[Progress] Failed to write:', e)
          }
        }
      }
    })
    
    // Inicializar sistema de arquivos virtual do Monaco
    initializeMonacoFileSystem().then(() => {
      // Sincronizar arquivos iniciais com instanceId único
      if (localData.files.length > 0) {
        syncFilesToMonacoFS(localData.files, localData.id)
      }
    })
    
    return () => {
      codeRunnerRef.current?.dispose()
      codeRunnerRef.current = null
      disposeMonacoFileSystem()
    }
  }, [])
  
  // Sincronizar arquivos quando mudarem ou quando conteúdos forem resolvidos
  useEffect(() => {
    if (localData.files.length > 0 && Object.keys(resolvedContents).length > 0) {
      // Criar versão dos arquivos com conteúdos resolvidos
      const filesWithResolvedContent = localData.files.map(file => ({
        ...file,
        content: resolvedContents[file.id] || file.content
      }))
      syncFilesToMonacoFS(filesWithResolvedContent, localData.id)
    }
  }, [localData.files, localData.id, resolvedContents])

  // Sincronizar com mudanças externas apenas na primeira montagem
  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true
      return // Skip na primeira vez, pois useState já inicializou
    }
    
    // Só sincronizar quando data mudar externamente (edição fora deste componente)
    setLocalData(draft => {
      if (!data.layout) {
        Object.assign(draft, data)
        draft.mode = data.mode || "execution"
        draft.layout = createDefaultLayout(data.mode || "execution")
      } else {
        // Usar dados do JSON exatamente como salvos
        Object.assign(draft, data)
      }
    })
  }, [data, setLocalData])

  // Fechar menu de settings quando clicar fora
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as HTMLElement
      if (showSettingsMenu && !target.closest('.settings-menu-container')) {
        setShowSettingsMenu(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [showSettingsMenu])

  // Resolver conteúdos de assets quando necessário
  useEffect(() => {
    const resolveContents = async () => {
      const newResolvedContents: Record<string, string> = {}
      
      for (const file of localData.files) {
        // Se já foi resolvido e não é referência, manter
        if (resolvedContents[file.id] && !FileOps.isAssetReference(file.content)) {
          newResolvedContents[file.id] = resolvedContents[file.id]!
          continue
        }
        
        // Se é referência a asset, resolver
        if (FileOps.isAssetReference(file.content)) {
          const resolved = await FileOps.resolveFileContent(file)
          newResolvedContents[file.id] = resolved
        } else {
          // Conteúdo direto
          newResolvedContents[file.id] = file.content
        }
      }
      
      setResolvedContents(newResolvedContents)
    }
    
    resolveContents()
  }, [localData.files, setResolvedContents])

  // Se não há modo definido, não renderizar nada
  if (!localData.mode) {
    return null
  }

  const currentMode = MODE_CONFIGS[localData.mode]
  const activeFile = localData.files.find(f => f.id === localData.activeFileId)

  const handleDataChange = (newData: Partial<CodeStudioData>) => {
    setLocalData(draft => {
      Object.assign(draft, newData)
    })
    
    // Propagar mudanças apenas se for preview
    // No modo editor (não preview), só salva quando clicar em Save
    
  }

  const handleCodeChange = async (content: string, fileId: string) => {
    setLocalData(draft => {
      const file = draft.files.find(f => f.id === fileId)
      if (file) {
        // Se o arquivo veio de assets e ainda não foi modificado
        if (file.assetId && !file.isModified) {
          // Marcar como modificado (isso fará com que seja convertido para local ao salvar)
          FileOps.markFileAsModified(draft, fileId)
        }
        
        // Armazenar o novo conteúdo (já não é mais referência)
        file.content = content
        
        // Atualizar também no sistema de arquivos virtual do Monaco com instanceId
        updateMonacoFile(file.path, content, draft.id)
      }
    })
  }

  // File Management
  const handleFileSelect = (fileId: string, panelId?: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    setLocalData(draft => {
      TabOps.selectFile(draft, fileId, panelId, activeDisplay)
    })
  }

  const handleCloseTab = (fileId: string, panelId?: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    setLocalData(draft => {
      TabOps.closeTab(draft, fileId, panelId, activeDisplay)
    })
  }

  const handleReorderTabs = (newOrder: string[], panelId?: string) => {
    const activeDisplay = getActiveDisplay()
    setLocalData(draft => {
      TabOps.reorderTabs(draft, newOrder, panelId, activeDisplay)
    })
  }

  const handleCreateFile = (path: string, name: string) => {
    if (!localData.layout) return
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    setLocalData(draft => {
      FileOps.createFile(draft, path, name, activeDisplay.id)
    })
  }

  const handleAddFileFromAsset = (path: string, assetId: string, fileName: string, content: string) => {
    if (!localData.layout) return
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    setLocalData(draft => {
      FileOps.addFileFromAsset(draft, path, assetId, fileName, content, activeDisplay.id)
    })
  }

  const handleCreateFolder = (path: string, name: string) => {
    setLocalData(draft => {
      FileOps.createFolder(draft, path, name)
    })
  }

  const handleDeleteFile = (fileId: string) => {
    setLocalData(draft => {
      FileOps.deleteFile(draft, fileId)
    })
  }

  const handleDeleteFolder = (folderId: string) => {
    setLocalData(draft => {
      FileOps.deleteFolder(draft, folderId)
    })
  }

  const handleRenameFile = (fileId: string, newName: string) => {
    setLocalData(draft => {
      FileOps.renameFile(draft, fileId, newName)
    })
  }

  const handleRenameFolder = (folderId: string, newName: string) => {
    setLocalData(draft => {
      FileOps.renameFolder(draft, folderId, newName)
    })
  }

  const handleToggleFolder = (folderId: string) => {
    setLocalData(draft => {
      FileOps.toggleFolder(draft, folderId)
    })
  }

  const handleMoveFile = (fileId: string, newPath: string) => {
    setLocalData(draft => {
      FileOps.moveFile(draft, fileId, newPath)
    })
  }

  const handleMoveFolder = (folderId: string, newPath: string) => {
    setLocalData(draft => {
      FileOps.moveFolder(draft, folderId, newPath)
    })
  }

  const handleReorderFiles = (newOrder: CodeFile[]) => {
    setLocalData(draft => {
      FileOps.reorderFiles(draft, newOrder)
    })
  }

  const handleChangeFileType = (fileId: string, fileType: 'f' | 'm' | 't') => {
    setLocalData(draft => {
      const file = draft.files.find(f => f.id === fileId)
      if (file) {
        // Se está marcando como 'm' ou 't', remover a marca de outros arquivos
        if (fileType === 'm') {
          draft.files.forEach(f => {
            if (f.id !== fileId && f.isFile === 'm') {
              f.isFile = 'f'
            }
          })
        } else if (fileType === 't') {
          draft.files.forEach(f => {
            if (f.id !== fileId && f.isFile === 't') {
              f.isFile = 'f'
            }
          })
        }
        file.isFile = fileType
      }
    })
  }

  const handleToggleFileVisibility = (fileId: string) => {
    setLocalData(draft => {
      FileOps.toggleFileVisibility(draft, fileId)
    })
  }

  const handleToggleFolderVisibility = (folderId: string) => {
    setLocalData(draft => {
      FileOps.toggleFolderVisibility(draft, folderId)
    })
  }

  const handleToggleFileReadonly = (fileId: string) => {
    setLocalData(draft => {
      FileOps.toggleFileReadonly(draft, fileId)
    })
  }

  const handleToggleFolderReadonly = (folderId: string) => {
    setLocalData(draft => {
      FileOps.toggleFolderReadonly(draft, folderId)
    })
  }

  const handleSetAllReadonly = (readonly: boolean) => {
    setLocalData(draft => {
      FileOps.setAllFilesReadonly(draft, readonly)
    })
  }

  const handleSetAllHidden = (hidden: boolean) => {
    setLocalData(draft => {
      FileOps.setAllFilesHidden(draft, hidden)
    })
  }

  // Collection handlers
  const handleImportCollection = async (
    path: string, 
    files: Array<{ name: string; path: string; assetId: string; isFile?: 'f' | 'm' | 't'; readonly?: boolean; isVisible?: boolean }>,
    folderMetadata?: Map<string, { readonly?: boolean; isVisible?: boolean }>
  ) => {
    console.log('[handleImportCollection] Importing to path:', path, 'files:', files)
    
    // Group files by their directory paths to create folders first
    const foldersNeeded = new Set<string>()
    
    for (const file of files) {
      // Extract all folder paths needed for this file
      const pathParts = file.path.split('/')
      pathParts.pop() // Remove filename
      
      // Build cumulative paths (e.g., "src", "src/components", "src/components/ui")
      let currentPath = ''
      for (const part of pathParts) {
        currentPath = currentPath ? `${currentPath}/${part}` : part
        const fullFolderPath = path ? `${path}/${currentPath}` : currentPath
        foldersNeeded.add(fullFolderPath)
      }
    }

    // Sort folders by depth (create parent folders first)
    const sortedFolders = Array.from(foldersNeeded).sort((a, b) => {
      const depthA = (a.match(/\//g) || []).length
      const depthB = (b.match(/\//g) || []).length
      return depthA - depthB
    })

    console.log('[handleImportCollection] Creating folders:', sortedFolders)

    // Create folders
    setLocalData(draft => {
      for (const folderPath of sortedFolders) {
        // Check if folder already exists
        const exists = draft.folders?.some(f => f.path === folderPath)
        if (exists) {
          console.log('[handleImportCollection] Folder already exists:', folderPath)
          continue
        }

        // Extract parent path and folder name
        const lastSlash = folderPath.lastIndexOf('/')
        const parentPath = lastSlash >= 0 ? folderPath.substring(0, lastSlash) : ''
        const folderName = lastSlash >= 0 ? folderPath.substring(lastSlash + 1) : folderPath

        console.log('[handleImportCollection] Creating folder:', { folderPath, parentPath, folderName })

        // Get metadata for this folder from collection (if available)
        // Need to remove destination path prefix to get original collection path
        const relativeFolderPath = path && folderPath.startsWith(`${path}/`) 
          ? folderPath.substring(path.length + 1) 
          : folderPath
        const metadata = folderMetadata?.get(relativeFolderPath)
        
        console.log('[handleImportCollection] Folder metadata lookup:', { 
          folderPath, 
          relativeFolderPath, 
          metadata,
          hasMetadata: folderMetadata ? `${folderMetadata.size} entries` : 'no metadata'
        })

        const newFolder: FileTreeFolder = {
          id: `${Date.now()}-${Math.random()}`,
          name: folderName,
          path: folderPath,
          isExpanded: true,
          isVisible: metadata?.isVisible ?? true,
          children: [],
          type: "folder",
          readonly: metadata?.readonly,
        }

        if (!draft.folders) {
          draft.folders = []
        }
        draft.folders.push(newFolder)
      }
    })

    // Import files
    for (const file of files) {
      try {
        console.log('[handleImportCollection] Importing file:', file)

        // Use the asset:// URL or empty content for empty files
        const content = file.assetId ? `asset://${file.assetId}` : ''
        
        // Extract folder path and filename from file.path
        const pathParts = file.path.split('/')
        const fileName = pathParts.pop() || file.name
        const relativeFolderPath = pathParts.join('/')
        const fullPath = path 
          ? (relativeFolderPath ? `${path}/${relativeFolderPath}/${fileName}` : `${path}/${fileName}`)
          : (relativeFolderPath ? `${relativeFolderPath}/${fileName}` : fileName)

        console.log('[handleImportCollection] File paths:', { fileName, relativeFolderPath, fullPath })

        // Add file directly to draft
        setLocalData(draft => {
          // Check if file already exists
          const existingFile = draft.files.find(f => f.path === fullPath)
          if (existingFile) {
            console.log('[handleImportCollection] File already exists, updating content:', fullPath)
            existingFile.content = content
            existingFile.assetId = file.assetId
            existingFile.isModified = false
            return
          }

          const language = getLanguageFromExtension(fileName)
          const newFile: CodeFile = {
            id: `${Date.now()}-${Math.random()}`,
            name: fileName,
            content,
            language,
            isFile: file.isFile || 'f',
            isVisible: file.isVisible ?? true,
            readonly: file.readonly,
            path: fullPath,
            assetId: file.assetId || undefined,
            isModified: false,
          }

          console.log('[handleImportCollection] Created file:', newFile)
          draft.files.push(newFile)
        })
      } catch (error) {
        console.error(`Failed to import file ${file.name}:`, error)
      }
    }

    console.log('[handleImportCollection] Import completed')
  }

  const handleSaveAsCollection = async (path: string, folderName?: string): Promise<{ success: boolean; error?: string }> => {
    console.log('[handleSaveAsCollection] Starting with path:', path, 'folderName:', folderName)
    try {
      // Get files and folders for the specified path
      let targetFiles: CodeFile[] = []
      let targetFolders: FileTreeFolder[] = []

      if (path === "") {
        // Root: get files and folders that are at root level
        // Root files: path doesn't contain '/' (path = filename)
        // Root folders: path doesn't contain '/' (path = foldername)
        console.log('[handleSaveAsCollection] All items:', {
          files: localData.files.map(f => ({ name: f.name, path: f.path })),
          folders: localData.folders.map(f => ({ name: f.name, path: f.path }))
        })
        
        targetFiles = localData.files.filter(f => !f.path.includes('/'))
        targetFolders = localData.folders.filter(f => !f.path.includes('/'))
        
        console.log('[handleSaveAsCollection] Root level:', {
          fileCount: targetFiles.length,
          folderCount: targetFolders.length,
          allFiles: localData.files.length,
          allFolders: localData.folders.length,
          files: targetFiles.map(f => ({ name: f.name, path: f.path })),
          folders: targetFolders.map(f => ({ name: f.name, path: f.path }))
        })
      } else {
        // Specific folder: files and subfolders inside this path
        // Files inside "new": path starts with "new/" (e.g., "new/README.md")
        // Subfolders inside "new": path starts with "new/" (e.g., "new/subfolder")
        const pathPrefix = `${path}/`
        targetFiles = localData.files.filter(f => f.path.startsWith(pathPrefix))
        targetFolders = localData.folders.filter(f => f.path.startsWith(pathPrefix))
        
        console.log('[handleSaveAsCollection] Folder contents:', path, {
          fileCount: targetFiles.length,
          folderCount: targetFolders.length,
          files: targetFiles.map(f => ({ name: f.name, path: f.path })),
          folders: targetFolders.map(f => ({ name: f.name, path: f.path }))
        })
      }

      console.log('[handleSaveAsCollection] Converting files to assets...')
      // Convert local files to assets before saving
      const convertedFiles: CodeFile[] = []
      for (const file of targetFiles) {
        // Check if file is empty
        const isEmpty = !file.content || file.content.trim() === ''
        
        if (isEmpty) {
          // Empty file - keep without creating asset
          convertedFiles.push({
            ...file,
            content: '',
          })
          console.log('[handleSaveAsCollection] File is empty, keeping without asset:', file.name)
          continue
        }
        
        // Check if file needs to be converted to a new asset
        const needsNewAsset = !file.content.startsWith("asset://") || file.isModified
        
        if (file.content.startsWith("asset://") && !file.isModified) {
          // Asset that hasn't been modified - keep reference
          convertedFiles.push(file)
          console.log('[handleSaveAsCollection] File already asset (unmodified):', file.name)
        } else {
          // File needs new asset (either local content or modified asset)
          const reason = !file.content.startsWith("asset://") ? "local content" : "modified asset"
          console.log(`[handleSaveAsCollection] Converting file to asset (${reason}):`, file.name)
          
          // Get actual content
          let contentToSave = file.content
          if (file.content.startsWith("asset://") && file.isModified) {
            // Modified asset - need to get resolved content from resolvedContents
            const resolvedContent = resolvedContents[file.id]
            if (resolvedContent) {
              contentToSave = resolvedContent
              console.log('[handleSaveAsCollection] Using resolved content for modified asset:', file.name)
            } else {
              console.warn('[handleSaveAsCollection] No resolved content found for modified asset:', file.name)
            }
          }
          
          // Create asset from content
          const result = await assetManager.saveAsset({
            dataUrl: contentToSave,
            fileName: file.name,
            author: "Code Studio Collection Export",
            type: "collection",
          })

          if (result.success && result.assetId) {
            // Create new file with asset reference
            convertedFiles.push({
              ...file,
              content: `asset://${result.assetId}`,
            })
          } else {
            console.error(`Failed to create asset for ${file.name}:`, result.error)
            convertedFiles.push(file) // Keep original
          }
        }
      }

      // Recursively build folder structure with files and subfolders
      const buildFolderStructure = async (
        folderPath: string, 
        folderName: string,
        originalFolder: FileTreeFolder
      ): Promise<FileTreeFolder> => {
        // folderPath is the full path of this folder (e.g., "new" or "new/subfolder")
        // Get all files that are directly inside this folder
        // Files in "new": path = "new/filename.ext" (starts with folderPath/ and no additional slashes)
        const pathPrefix = `${folderPath}/`
        const folderFiles = localData.files.filter(f => {
          if (!f.path.startsWith(pathPrefix)) return false
          // Check if file is directly in this folder (no sub-paths)
          const relativePath = f.path.substring(pathPrefix.length)
          return !relativePath.includes('/')
        })
        
        // Get all subfolders that are directly inside this folder
        // Subfolders in "new": path = "new/subfoldername" (starts with folderPath/ and no additional slashes)
        const subfolders = localData.folders.filter(f => {
          if (!f.path.startsWith(pathPrefix)) return false
          const relativePath = f.path.substring(pathPrefix.length)
          return !relativePath.includes('/')
        })
        
        console.log('[buildFolderStructure]', folderName, 'at path:', folderPath, {
          fileCount: folderFiles.length,
          subfolderCount: subfolders.length,
          files: folderFiles.map(f => ({ name: f.name, path: f.path })),
          subfolders: subfolders.map(f => ({ name: f.name, path: f.path })),
          readonly: originalFolder.readonly,
          isVisible: originalFolder.isVisible,
        })

        const children: (CodeFile | FileTreeFolder)[] = []

        // Convert and add files
        for (const file of folderFiles) {
          // Check if file is empty
          const isEmpty = !file.content || file.content.trim() === ''
          
          if (isEmpty) {
            // Empty file - keep without creating asset
            children.push({
              ...file,
              content: '',
            })
            continue
          }
          
          if (file.content.startsWith("asset://") && !file.isModified) {
            // Asset that hasn't been modified - keep reference
            children.push(file)
          } else {
            // File needs new asset (either local content or modified asset)
            let contentToSave = file.content
            if (file.content.startsWith("asset://") && file.isModified) {
              // Modified asset - need to get resolved content
              const resolvedContent = resolvedContents[file.id]
              if (resolvedContent) {
                contentToSave = resolvedContent
              }
            }
            
            const result = await assetManager.saveAsset({
              dataUrl: contentToSave,
              fileName: file.name,
              author: "Code Studio Collection Export",
              type: "collection",
            })

            if (result.success && result.assetId) {
              children.push({
                ...file,
                content: `asset://${result.assetId}`,
              })
            } else {
              children.push(file)
            }
          }
        }

        // Recursively process subfolders
        for (const subfolder of subfolders) {
          const builtSubfolder = await buildFolderStructure(subfolder.path, subfolder.name, subfolder)
          children.push(builtSubfolder)
        }

        return {
          id: `folder-${folderPath}-${folderName}`,
          name: folderName,
          path: folderPath,
          isExpanded: false,
          isVisible: originalFolder.isVisible,
          readonly: originalFolder.readonly,
          children,
          type: "folder"
        }
      }

      // Build converted folders with proper hierarchy
      const convertedFolders: FileTreeFolder[] = []
      for (const folder of targetFolders) {
        console.log('[handleSaveAsCollection] Building folder structure for:', folder.name, 'path:', folder.path)
        const builtFolder = await buildFolderStructure(folder.path, folder.name, folder)
        convertedFolders.push(builtFolder)
      }

      console.log('[handleSaveAsCollection] Calling saveProjectAsCollection with:', {
        name: folderName || "Untitled Collection",
        folderCount: convertedFolders.length,
        fileCount: convertedFiles.length,
        folders: convertedFolders.map(f => ({ 
          name: f.name, 
          childCount: f.children.length,
          children: f.children.map(c => 'children' in c ? `folder:${c.name}` : `file:${c.name}`)
        }))
      })

      // Now save as collection using converted files
      const result = await saveProjectAsCollection({
        name: folderName || "Untitled Collection",
        folders: convertedFolders,
        files: convertedFiles,
      })

      return result
    } catch (error) {
      console.error('Failed to save collection:', error)
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
      }
    }
  }

  // Layout handlers
  const getActiveDisplay = (): DisplayConfig | undefined => {
    if (!localData.layout) return undefined
    return localData.layout.displays.find(d => d.id === localData.layout!.activeDisplayId)
  }

  const handleToggleLayoutEdit = () => {
    setLocalData(draft => {
      LayoutOps.toggleLayoutEdit(draft)
    })
  }

  const handleSelectDisplay = (displayId: string) => {
    setLocalData(draft => {
      LayoutOps.selectDisplay(draft, displayId)
      
      // Ao trocar de display, atualizar activeFileId para refletir o estado do novo display
      const newDisplay = draft.layout?.displays.find(d => d.id === displayId)
      if (newDisplay) {
        const editorPanel = newDisplay.panels.find(p => p.type === 'editor')
        if (editorPanel?.editorInstance === 'unique') {
          // Display com editor único: usar uniqueActiveFileId
          draft.activeFileId = newDisplay.uniqueActiveFileId
        } else {
          // Display com editor múltiplo: manter activeFileId global se estiver nas tabs abertas
          if (draft.openTabs && draft.openTabs.length > 0) {
            if (!draft.activeFileId || !draft.openTabs.includes(draft.activeFileId)) {
              draft.activeFileId = draft.openTabs[draft.openTabs.length - 1]
            }
          } else {
            draft.activeFileId = undefined
          }
        }
      }
    })
  }

  const handleCreateDisplay = (name: string, aspectRatio: AspectRatio) => {
    setLocalData(draft => {
      LayoutOps.createDisplay(draft, name, aspectRatio)
    })
  }

  const handleDeleteDisplay = (displayId: string) => {
    setLocalData(draft => {
      LayoutOps.deleteDisplay(draft, displayId)
    })
  }

  const handleRenameDisplay = (displayId: string, newName: string) => {
    setLocalData(draft => {
      LayoutOps.renameDisplay(draft, displayId, newName)
    })
  }

  const handleChangeAspectRatio = (displayId: string, newAspectRatio: AspectRatio) => {
    setLocalData(draft => {
      LayoutOps.changeAspectRatio(draft, displayId, newAspectRatio)
    })
  }

  const handleUpdateCurrentDisplay = (updatedDisplay: DisplayConfig) => {
    setLocalData(draft => {
      LayoutOps.updateCurrentDisplay(draft, updatedDisplay)
    })
  }

  const handleAddPanel = (type: PanelType, row?: number, col?: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    setLocalData(draft => {
      PanelOps.addPanel(draft, activeDisplay, type, row, col)
    })
  }

  const handleGridDrop = (row: number, col: number, type: PanelType) => {
    handleAddPanel(type, row, col)
  }

  const handlePanelResize = (panelId: string, row: number, col: number, rowSpan: number, colSpan: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    setLocalData(draft => {
      PanelOps.resizePanel(draft, activeDisplay, panelId, row, col, rowSpan, colSpan)
    })
  }

  const handlePanelMove = (panelId: string, row: number, col: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    setLocalData(draft => {
      PanelOps.movePanel(draft, activeDisplay, panelId, row, col)
    })
  }

  const handleRemovePanel = (panelId: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    setLocalData(draft => {
      PanelOps.removePanel(draft, activeDisplay, panelId)
    })
  }

  const handleToggleEditorInstance = (panelId: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    setLocalData(draft => {
      PanelOps.toggleEditorInstance(draft, activeDisplay, panelId)
    })
  }

  const handlePanelDragStart = (panelId: string) => {
    PanelOps.onPanelDragStart(panelId)
  }

  const handlePanelDragEnd = () => {
    PanelOps.onPanelDragEnd()
  }

  // Renderizar conteúdo de cada painel
  const renderPanelContent = (panel: PanelConfig, displayConfig?: DisplayConfig) => {
    switch (panel.type) {
      case "explorer":
        return (
          <FileExplorer
            files={localData.files}
            folders={localData.folders || []}
            activeFileId={localData.activeFileId}
            onFileSelect={handleFileSelect}
            onCreateFile={handleCreateFile}
            onCreateFolder={handleCreateFolder}
            onDeleteFile={handleDeleteFile}
            onDeleteFolder={handleDeleteFolder}
            onRenameFile={handleRenameFile}
            onRenameFolder={handleRenameFolder}
            onToggleFolder={handleToggleFolder}
            onMoveFile={handleMoveFile}
            onMoveFolder={handleMoveFolder}
            onReorderFiles={handleReorderFiles}
            onAddFileFromAsset={handleAddFileFromAsset}
            onChangeFileType={handleChangeFileType}
            onToggleFileVisibility={handleToggleFileVisibility}
            onToggleFolderVisibility={handleToggleFolderVisibility}
            onToggleFileReadonly={handleToggleFileReadonly}
            onToggleFolderReadonly={handleToggleFolderReadonly}
            onSetAllReadonly={handleSetAllReadonly}
            onSetAllHidden={handleSetAllHidden}
            onImportCollection={handleImportCollection}
            onSaveAsCollection={handleSaveAsCollection}
            isPreview={isPreview}
          />
        )
      
      case "editor":
        // No preview, usar displayConfig passado; no editor, usar activeDisplay
        const displayToUse = isPreview && displayConfig ? displayConfig : getActiveDisplay()
        const isUniqueInstance = panel.editorInstance === "unique"
        const currentOpenTabs = isUniqueInstance 
          ? (displayToUse?.uniqueOpenTabs || [])
          : (localData.openTabs || [])
        const currentActiveFileId = isUniqueInstance
          ? displayToUse?.uniqueActiveFileId
          : localData.activeFileId
        
        // No preview, verificar se há explorer no Display Base para permitir fechar tabs
        const hasExplorer = displayConfig ? displayConfig.panels.some(p => p.type === 'explorer') : true
        const canCloseTabs = isPreview ? hasExplorer : true
        
        return (
          <div className="flex flex-col h-full relative">
            {/* Editor Instance Switch */}
            {panel.editorInstance && localData.layout?.editMode && (
              <EditorInstanceSwitch
                editorInstance={panel.editorInstance}
                onToggle={() => handleToggleEditorInstance(panel.id)}
              />
            )}
            
            <FileTabs
              files={localData.files}
              openTabs={currentOpenTabs}
              activeFileId={currentActiveFileId}
              editorInstance={panel.editorInstance}
              panelId={panel.id}
              onSelectTab={(fileId) => handleFileSelect(fileId, panel.id)}
              onCloseTab={canCloseTabs ? (fileId) => handleCloseTab(fileId, panel.id) : undefined}
              onReorderTabs={handleReorderTabs}
            />
            <div className="flex-1 min-h-0 relative">
              {currentOpenTabs.length === 0 ? (
                <EmptyEditorState />
              ) : (
                <>
                  {currentOpenTabs.map((fileId, index) => {
                    const file = localData.files.find(f => f.id === fileId)
                    if (!file) return null
                    const isActive = fileId === currentActiveFileId
                    
                    // Verificar se arquivo ou pasta pai está readonly
                    let isFileReadonly = file.readonly || false
                    if (!isFileReadonly && localData.folders) {
                      // Verificar se está dentro de uma pasta readonly
                      const fileFolder = localData.folders.find(folder => 
                        file.path.startsWith(folder.path + "/")
                      )
                      if (fileFolder?.readonly) {
                        isFileReadonly = true
                      }
                    }
                    
                    return (
                      <div
                        key={`${file.id}-${index}`}
                        className="absolute inset-0"
                        style={{ 
                          visibility: isActive ? 'visible' : 'hidden',
                          pointerEvents: isActive ? 'auto' : 'none',
                          zIndex: isActive ? 1 : 0
                        }}
                      >
                        <MonacoCodeEditor
                          fileId={file.id}
                          filePath={file.path}
                          instanceId={localData.id} // ID único da instância para isolamento completo
                          value={resolvedContents[file.id] || file.content}
                          onChange={(content) => handleCodeChange(content, file.id)}
                          language={file.language}
                          readonly={localData.readonly || isFileReadonly}
                          showLineNumbers={localData.showLineNumbers}
                          fontSize={localData.fontSize}
                          shikiTheme={localData.shikiTheme}
                        />
                      </div>
                    )
                  })}
                </>
              )}
            </div>
          </div>
        )
      
      case "output":
        const mainFile = localData.files.find(f => f.isFile === 'm')
        const testFile = localData.files.find(f => f.isFile === 't')
        
        return (
          <ResultPanel
            ref={terminalRef}
            output={output}
            isExecuting={isExecuting}
            mode={localData.mode!}
            onExecuteFile={handleExecuteFile}
            onExecuteProject={handleExecuteProject}
            onExecuteTest={handleExecuteTest}
            onStop={handleStop}
            testCases={localData.testCases?.[localData.activeFileId || ""] || []}
            activeFile={activeFile}
            hasMainFile={!!mainFile}
            hasTestFile={!!testFile}
          />
        )
    }
  }

  // Executa o arquivo atualmente selecionado
  const handleExecuteFile = async () => {
    if (!codeRunnerRef.current || !terminalRef.current) return
    
    // Buscar arquivo ativo: primeiro tentar painéis únicos, depois global
    const activeDisplay = getActiveDisplay()
    let fileToExecute = activeFile // Padrão: arquivo global ativo
    
    // Se houver painéis com instância única, usar o arquivo ativo deles
    if (activeDisplay) {
      const uniqueEditorPanel = activeDisplay.panels.find(
        p => p.type === 'editor' && p.editorInstance === 'unique'
      )
      if (uniqueEditorPanel && activeDisplay.uniqueActiveFileId) {
        fileToExecute = localData.files.find(f => f.id === activeDisplay.uniqueActiveFileId)
      }
    }
    
    if (!fileToExecute) return
    
    await executeFile(fileToExecute)
  }

  // Executa o arquivo marcado como 'm' (main)
  const handleExecuteProject = async () => {
    if (!codeRunnerRef.current || !terminalRef.current) return
    
    const mainFile = localData.files.find(f => f.isFile === 'm')
    if (!mainFile) return
    
    await executeFile(mainFile)
  }

  // Executa o arquivo marcado como 't' (test)
  const handleExecuteTest = async () => {
    if (!codeRunnerRef.current || !terminalRef.current) return
    
    const testFile = localData.files.find(f => f.isFile === 't')
    if (!testFile) return
    
    await executeFile(testFile)
  }

  // Função auxiliar para executar um arquivo
  const executeFile = async (fileToExecute: CodeFile) => {
    if (!codeRunnerRef.current || !terminalRef.current) return
    
    setIsExecuting(true)
    setOutput('') // Limpar output anterior - isso vai limpar o terminal via useEffect

    terminalRef.current.write('\x1b[33m⟳ Starting execution...\x1b[0m\r\n')

    try {
      // Criar mapa de arquivos para suportar imports - apenas arquivos da mesma linguagem
      // Resolver conteúdos de assets antes de executar
      const filesMap: Record<string, string> = {}
      for (const file of localData.files) {
        if (file.language === fileToExecute.language && 
            hasValidExtension(file.path, fileToExecute.language)) {
          // Resolver conteúdo se for referência a asset
          const content = resolvedContents[file.id] || file.content
          filesMap[`/${file.path}`] = content
        }
      }

      // Usar runWithFiles para suportar imports entre arquivos
      const result = await codeRunnerRef.current.runWithFiles(
        fileToExecute.language,
        `/${fileToExecute.path}`,
        filesMap
      )

      // Escrever resultado no terminal (já foi limpo automaticamente quando isExecuting=true)
      if (result.stdout) {
        terminalRef.current.write(result.stdout.replace(/\n/g, '\r\n'))
      }
      if (result.stderr) {
        if (result.stdout) terminalRef.current.write('\r\n')
        terminalRef.current.write('\x1b[31m' + result.stderr.replace(/\n/g, '\r\n') + '\x1b[0m')
      }
      if (result.exitCode !== 0) {
        terminalRef.current.write(`\r\n\x1b[33m[Process exited with code ${result.exitCode}]\x1b[0m`)
      }
      terminalRef.current.write(`\r\n\x1b[90m[Execution time: ${result.executionTime.toFixed(2)}ms]\x1b[0m\r\n`)
    } catch (error) {
      terminalRef.current.write(`\r\n\x1b[31mExecution error: ${error instanceof Error ? error.message : String(error)}\x1b[0m\r\n`)
    } finally {
      setIsExecuting(false)
    }
  }

  const handleExecute = handleExecuteFile // Manter retrocompatibilidade

  const handleStop = async () => {
    if (codeRunnerRef.current) {
      await codeRunnerRef.current.interrupt()
      setIsExecuting(false)
      setOutput(prev => prev + '\n\x1b[33m[Execution interrupted]\x1b[0m')
    }
  }

  const handleSaveClick = () => {
    // Processar arquivos modificados de assets antes de salvar
    // Agora apenas remove a referência ao asset, mantendo o mesmo nome
    setLocalData(draft => {
      // Encontrar todos os arquivos modificados que vieram de assets
      const modifiedAssetFiles = draft.files.filter(f => f.assetId && f.isModified)
      
      // Para cada um, remover referência ao asset (converte para local)
      for (const file of modifiedAssetFiles) {
        FileOps.createCopyOnSave(draft, file.id)
      }
    })
    
    // Aguardar o próximo tick para garantir que o estado foi atualizado
    setTimeout(() => {
      // Garantir que activeDisplayId seja sempre display-1 ao salvar
      const display1 = localData.layout?.displays.find(d => d.id === 'display-1')
      
      // Determinar o tipo de editor no display-1
      const display1Editor = display1?.panels.find(p => p.type === 'editor')
      const isDisplay1Unique = display1Editor?.editorInstance === 'unique'
      
      // Sincronizar activeFileId com a aba ativa apropriada
      let syncedActiveFileId = localData.activeFileId
      
      if (isDisplay1Unique) {
        // Editor único: usar uniqueActiveFileId do display-1
        if (display1?.uniqueOpenTabs && display1.uniqueOpenTabs.length > 0) {
          syncedActiveFileId = display1.uniqueActiveFileId
        } else {
          syncedActiveFileId = undefined
        }
      } else {
        // Editor múltiplo: usar openTabs global
        if (localData.openTabs && localData.openTabs.length > 0) {
          // Se já tem activeFileId e está nas tabs abertas, manter
          // Senão, usar a última tab aberta
          if (localData.activeFileId && localData.openTabs.includes(localData.activeFileId)) {
            syncedActiveFileId = localData.activeFileId
          } else {
            syncedActiveFileId = localData.openTabs[localData.openTabs.length - 1]
          }
        } else {
          syncedActiveFileId = undefined
        }
      }
      
      const dataToSave = {
        ...localData,
        layout: localData.layout ? {
          ...localData.layout,
          activeDisplayId: 'display-1'
        } : undefined,
        activeFileId: syncedActiveFileId
      }
      onSave?.(dataToSave)
    }, 0)
  }

  const handleCancelClick = () => {
    onCancel?.()
  }

  // Se for preview (renderizado no documento), não mostra o modal
  if (isPreview) {
    // Usar Display Base (display-1) como espelho do preview
    const baseDisplay = localData.layout?.displays.find(d => d.id === 'display-1')
    if (!baseDisplay) return null

    // Verificar se há painel explorer no Display Base
    const hasExplorer = baseDisplay.panels.some(p => p.type === 'explorer')

    return (
      <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-900">
        {/* Header compacto */}
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <Code2 className="h-4 w-4 text-blue-600 dark:text-blue-400" />
            <span className="font-medium text-sm">{localData.title || "Code Studio"}</span>
            {localData.readonly && (
              <span className="text-xs px-2 py-0.5 bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300 rounded-full flex items-center gap-1">
                <Lock className="h-3 w-3" />
                Read Only
              </span>
            )}
          </div>
        </div>

        {/* Layout renderizado com base no Display Base */}
        <div 
          className="grid gap-3 p-3"
          style={{
            gridTemplateColumns: `repeat(${baseDisplay.aspectRatio === '2:1' ? 24 : 12}, 1fr)`,
            gridTemplateRows: `repeat(${baseDisplay.aspectRatio === '1:2' ? 24 : 12}, 1fr)`,
            height: baseDisplay.aspectRatio === '2:1' ? '600px' : baseDisplay.aspectRatio === '1:2' ? '1200px' : '600px',
          }}
        >
          {baseDisplay.panels.map(panel => (
            <div
              key={panel.id}
              style={{
                gridColumn: `${panel.col + 1} / span ${panel.colSpan}`,
                gridRow: `${panel.row + 1} / span ${panel.rowSpan}`,
              }}
              className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-800"
            >
              {renderPanelContent(panel, baseDisplay)}
            </div>
          ))}
        </div>

        {localData.caption && (
          <div className="p-2 text-xs text-gray-600 dark:text-gray-400 border-t border-gray-200 dark:border-gray-800">
            {localData.caption}
          </div>
        )}
      </div>
    )
  }

  // Modal de edição (fullscreen)
  return (
    <div 
      className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
      onClick={handleCancelClick}
    >
      <div 
        className="bg-white dark:bg-gray-900 border dark:border-gray-700 shadow-2xl w-full max-w-7xl h-[90vh] flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <Code2 className="h-5 w-5 text-blue-600 dark:text-blue-400" />
              <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Code Studio</h2>
            </div>
          </div>
          
          <Button variant="ghost" size="sm" onClick={handleCancelClick}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Settings Bar */}
        <div className="flex items-center gap-4 p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          {/* Settings Menu Button */}
          <div className="relative settings-menu-container">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowSettingsMenu(!showSettingsMenu)}
              className="h-8 w-8 p-0"
              title="Settings"
            >
              <Menu className="h-4 w-4" />
            </Button>
            
            {/* Settings Dropdown Menu */}
            {showSettingsMenu && (
              <SettingsMenu
                data={localData}
                onDataChange={handleDataChange}
                onClose={() => setShowSettingsMenu(false)}
              />
            )}
          </div>
          
          <div className="flex items-center gap-2">
            <Label htmlFor="title" className="text-sm font-medium">
              Title:
            </Label>
            <Input
              id="title"
              value={localData.title || ""}
              onChange={(e) => handleDataChange({ title: e.target.value })}
              placeholder="Optional title"
              className="w-48"
            />

            {/* Display Selector - Only when NOT editing layout */}
            {!localData.layout?.editMode && localData.layout && (
              <div className="flex items-center gap-1 ml-4 px-2 py-1 bg-gray-100 dark:bg-gray-800 rounded-md border border-gray-200 dark:border-gray-700">
                {localData.layout.displays.map((display) => (
                  <button
                    key={display.id}
                    onClick={() => handleSelectDisplay(display.id)}
                    className={cn(
                      "px-2.5 py-1 rounded text-xs font-medium transition-all",
                      localData.layout?.activeDisplayId === display.id
                        ? "bg-blue-600 text-white shadow-sm"
                        : "bg-transparent text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700"
                    )}
                    title={display.name}
                  >
                    {display.name}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Layout Edit Button */}
          <div className="ml-auto flex items-center gap-2">
            <Button
              variant={localData.layout?.editMode ? "default" : "outline"}
              size="sm"
              onClick={handleToggleLayoutEdit}
              className="h-8"
              title={localData.layout?.editMode ? "Exit Layout Edit" : "Edit Layout"}
            >
              <Layout className="h-4 w-4 mr-2" />
              {localData.layout?.editMode ? "Done" : "Layout"}
            </Button>
          </div>
        </div>

        {/* Main Content - Grid Layout Customizável */}
        <div className="flex-1 min-h-0 p-3 bg-gray-100 dark:bg-gray-950 overflow-hidden flex flex-col">
          {/* Layout Edit Tools */}
          {localData.layout?.editMode && (
            <div className="mb-3 p-2 bg-white dark:bg-gray-900 border border-blue-500/30 rounded-lg shrink-0">
              <DisplayManager
                displays={localData.layout.displays}
                activeDisplayId={localData.layout.activeDisplayId}
                onSelectDisplay={handleSelectDisplay}
                onCreateDisplay={handleCreateDisplay}
                onDeleteDisplay={handleDeleteDisplay}
                onRenameDisplay={handleRenameDisplay}
                onChangeAspectRatio={handleChangeAspectRatio}
                onAddPanel={handleAddPanel}
              />
            </div>
          )}

          {/* Grid Container */}
          <div className="flex-1 min-h-0 overflow-hidden flex items-center justify-center p-4">
            {(() => {
              const activeDisplay = getActiveDisplay()
              if (!activeDisplay) return null

              const { cols, rows } = getGridDimensions(activeDisplay.aspectRatio)
              const { maxWidth, maxHeight } = getContainerDimensions(activeDisplay.aspectRatio)

              return (
                <div
                  className="w-full h-full"
                  style={{
                    maxWidth,
                    maxHeight,
                  }}
                >
                  <GridDropZone
                    isActive={localData.layout?.editMode || false}
                    onDrop={handleGridDrop}
                    gridCols={cols}
                    gridRows={rows}
                  >
                    <div
                      ref={gridContainerRef}
                      className="h-full w-full grid gap-3"
                      style={{
                        gridTemplateColumns: `repeat(${cols}, 1fr)`,
                        gridTemplateRows: `repeat(${rows}, 1fr)`,
                      }}
                    >
                    {activeDisplay.panels.map(panel => (
                      <ResizablePanel
                        key={panel.id}
                        panel={panel}
                        allPanels={activeDisplay.panels}
                        isEditMode={localData.layout?.editMode || false}
                        gridContainerRef={gridContainerRef}
                        gridCols={cols}
                        gridRows={rows}
                        onResize={handlePanelResize}
                        onMove={handlePanelMove}
                        onRemove={handleRemovePanel}
                        onDragStart={handlePanelDragStart}
                        onDragEnd={handlePanelDragEnd}
                      >
                        {renderPanelContent(panel)}
                      </ResizablePanel>
                    ))}
                  </div>
                </GridDropZone>
                </div>
              )
            })()}
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-4">
            <div className="flex-1">
              <Label htmlFor="caption" className="text-sm font-medium">
                Caption:
              </Label>
              <Input
                id="caption"
                value={localData.caption || ""}
                onChange={(e) => handleDataChange({ caption: e.target.value })}
                placeholder="Optional caption"
                className="mt-1"
              />
            </div>

            <div className="flex gap-2">
              <Button 
                variant="outline" 
                onClick={handleCancelClick}
                disabled={localData.layout?.editMode}
              >
                Cancel
              </Button>
              <Button 
                onClick={handleSaveClick} 
                className="flex items-center gap-2"
                disabled={localData.layout?.editMode}
              >
                <Save className="h-4 w-4" />
                Save
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
