import type { CodeStudioData, CodeFile, FileTreeFolder } from "./types"
import { LANGUAGE_CONFIGS, getLanguageFromExtension } from "./types"

export function createFile(
  data: CodeStudioData,
  path: string,
  name: string,
  activeDisplayId?: string
): Partial<CodeStudioData> {
  const language = getLanguageFromExtension(name)
  const fullPath = path ? `${path}/${name}` : name
  
  const newFileId = Date.now().toString()
  const newFile: CodeFile = {
    id: newFileId,
    name,
    content: LANGUAGE_CONFIGS[language].defaultTemplate,
    language,
    isMain: data.files.length === 0,
    isVisible: true,
    path: fullPath,
  }

  // Se tiver layout e activeDisplayId, verificar editor único
  if (data.layout && activeDisplayId) {
    const activeDisplay = data.layout.displays.find(d => d.id === activeDisplayId)
    if (activeDisplay) {
      const uniqueEditorInDisplay = activeDisplay.panels.find(
        p => p.type === "editor" && p.editorInstance === "unique"
      )

      if (uniqueEditorInDisplay) {
        // Editor único: adicionar às abas específicas do display
        const currentTabs = activeDisplay.uniqueOpenTabs || []
        const updatedTabs = [...currentTabs, newFileId]
        
        const updatedDisplays = data.layout.displays.map(d => 
          d.id === activeDisplay.id 
            ? { ...d, uniqueOpenTabs: updatedTabs, uniqueActiveFileId: newFileId }
            : d
        )

        return {
          files: [...data.files, newFile],
          layout: {
            ...data.layout,
            displays: updatedDisplays,
          },
        }
      }
    }
  }

  // Editor múltiplo ou sem layout: abas globais
  return { 
    files: [...data.files, newFile],
    openTabs: [...(data.openTabs || []), newFileId],
    activeFileId: newFileId,
  }
}

export function createFolder(
  data: CodeStudioData,
  path: string,
  name: string
): Partial<CodeStudioData> {
  const fullPath = path ? `${path}/${name}` : name
  const newFolder: FileTreeFolder = {
    id: Date.now().toString(),
    name,
    path: fullPath,
    isExpanded: true,
    children: [],
    type: "folder",
  }
  
  return { folders: [...(data.folders || []), newFolder] }
}

export function deleteFile(
  data: CodeStudioData,
  fileId: string
): Partial<CodeStudioData> {
  const newFiles = data.files.filter(f => f.id !== fileId)
  const newOpenTabs = (data.openTabs || []).filter(id => id !== fileId)
  
  return { 
    files: newFiles,
    openTabs: newOpenTabs,
    activeFileId: newOpenTabs.length > 0 ? newOpenTabs[0] : newFiles[0]?.id,
  }
}

export function deleteFolder(
  data: CodeStudioData,
  folderId: string
): Partial<CodeStudioData> {
  const folder = data.folders?.find(f => f.id === folderId)
  if (!folder) return {}
  
  // Remover pasta e arquivos dentro dela
  const newFiles = data.files.filter(f => !f.path.startsWith(folder.path))
  const newFolders = (data.folders || []).filter(f => f.id !== folderId)
  
  return { files: newFiles, folders: newFolders }
}

export function renameFile(
  data: CodeStudioData,
  fileId: string,
  newName: string
): Partial<CodeStudioData> {
  const updatedFiles = data.files.map(f => {
    if (f.id === fileId) {
      const pathParts = f.path.split('/')
      pathParts[pathParts.length - 1] = newName
      return { ...f, name: newName, path: pathParts.join('/') }
    }
    return f
  })
  
  return { files: updatedFiles }
}

export function renameFolder(
  data: CodeStudioData,
  folderId: string,
  newName: string
): Partial<CodeStudioData> {
  const folder = data.folders?.find(f => f.id === folderId)
  if (!folder) return {}
  
  const oldPath = folder.path
  const pathParts = oldPath.split('/')
  pathParts[pathParts.length - 1] = newName
  const newPath = pathParts.join('/')
  
  // Atualizar pasta
  const updatedFolders = (data.folders || []).map(f => {
    if (f.id === folderId) {
      return { ...f, name: newName, path: newPath }
    }
    return f
  })
  
  // Atualizar caminhos dos arquivos dentro da pasta
  const updatedFiles = data.files.map(f => {
    if (f.path.startsWith(oldPath)) {
      return { ...f, path: f.path.replace(oldPath, newPath) }
    }
    return f
  })
  
  return { folders: updatedFolders, files: updatedFiles }
}

export function toggleFolder(
  data: CodeStudioData,
  folderId: string
): Partial<CodeStudioData> {
  const updatedFolders = (data.folders || []).map(f =>
    f.id === folderId ? { ...f, isExpanded: !f.isExpanded } : f
  )
  
  return { folders: updatedFolders }
}

export function moveFile(
  data: CodeStudioData,
  fileId: string,
  newPath: string
): Partial<CodeStudioData> {
  const file = data.files.find(f => f.id === fileId)
  if (!file) return {}

  const fileName = file.path.split("/").pop() || file.name
  const newFilePath = newPath ? `${newPath}/${fileName}` : fileName

  // Verificar se já existe arquivo com mesmo nome no destino
  const fileExists = data.files.some(f => f.path === newFilePath && f.id !== fileId)
  if (fileExists) return {}

  const updatedFiles = data.files.map(f =>
    f.id === fileId ? { ...f, path: newFilePath, name: fileName } : f
  )
  
  return { files: updatedFiles }
}

export function moveFolder(
  data: CodeStudioData,
  folderId: string,
  newPath: string
): Partial<CodeStudioData> {
  const folder = (data.folders || []).find(f => f.id === folderId)
  if (!folder) return {}

  const folderName = folder.path.split("/").pop() || folder.name
  const newFolderPath = newPath ? `${newPath}/${folderName}` : folderName

  // Verificar se já existe pasta com mesmo nome no destino
  const folderExists = (data.folders || []).some(f => f.path === newFolderPath && f.id !== folderId)
  if (folderExists) return {}

  const oldPath = folder.path
  const updatedFolders = (data.folders || []).map(f => {
    if (f.id === folderId) {
      return { ...f, path: newFolderPath, name: folderName }
    }
    // Atualizar subpastas
    if (f.path.startsWith(oldPath + "/")) {
      const relativePath = f.path.substring(oldPath.length + 1)
      return { ...f, path: `${newFolderPath}/${relativePath}` }
    }
    return f
  })

  // Atualizar arquivos dentro da pasta
  const updatedFiles = data.files.map(f => {
    if (f.path.startsWith(oldPath + "/")) {
      const relativePath = f.path.substring(oldPath.length + 1)
      return { ...f, path: `${newFolderPath}/${relativePath}` }
    }
    return f
  })

  return { folders: updatedFolders, files: updatedFiles }
}
