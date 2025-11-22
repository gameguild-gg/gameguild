import { produce } from "immer"
import type { CodeStudioData, CodeFile, FileTreeFolder } from "./types"
import { LANGUAGE_CONFIGS, getLanguageFromExtension } from "./types"

export function createFile(
  data: CodeStudioData,
  path: string,
  name: string,
  activeDisplayId?: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    const language = getLanguageFromExtension(name)
    const fullPath = path ? `${path}/${name}` : name
    
    const newFileId = Date.now().toString()
    const newFile: CodeFile = {
      id: newFileId,
      name,
      content: LANGUAGE_CONFIGS[language].defaultTemplate,
      language,
      isMain: draft.files.length === 0,
      isVisible: true,
      path: fullPath,
    }

    draft.files.push(newFile)

    // Se tiver layout e activeDisplayId, verificar editor único
    if (draft.layout && activeDisplayId) {
      const activeDisplay = draft.layout.displays.find(d => d.id === activeDisplayId)
      if (activeDisplay) {
        const uniqueEditorInDisplay = activeDisplay.panels.find(
          p => p.type === "editor" && p.editorInstance === "unique"
        )

        if (uniqueEditorInDisplay) {
          // Editor único: adicionar às abas específicas do display
          if (!activeDisplay.uniqueOpenTabs) {
            activeDisplay.uniqueOpenTabs = []
          }
          activeDisplay.uniqueOpenTabs.push(newFileId)
          activeDisplay.uniqueActiveFileId = newFileId
          return
        }
      }
    }

    // Editor múltiplo ou sem layout: abas globais
    if (!draft.openTabs) {
      draft.openTabs = []
    }
    draft.openTabs.push(newFileId)
    draft.activeFileId = newFileId
  })
}

export function createFolder(
  data: CodeStudioData,
  path: string,
  name: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    const fullPath = path ? `${path}/${name}` : name
    const newFolder: FileTreeFolder = {
      id: Date.now().toString(),
      name,
      path: fullPath,
      isExpanded: true,
      children: [],
      type: "folder",
    }
    
    if (!draft.folders) {
      draft.folders = []
    }
    draft.folders.push(newFolder)
  })
}

export function deleteFile(
  data: CodeStudioData,
  fileId: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    draft.files = draft.files.filter(f => f.id !== fileId)
    
    if (draft.openTabs) {
      draft.openTabs = draft.openTabs.filter(id => id !== fileId)
      
      if (draft.openTabs.length > 0) {
        draft.activeFileId = draft.openTabs[0]
      } else if (draft.files.length > 0) {
        draft.activeFileId = draft.files[0]?.id
      } else {
        draft.activeFileId = undefined
      }
    }
  })
}

export function deleteFolder(
  data: CodeStudioData,
  folderId: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    const folder = draft.folders?.find(f => f.id === folderId)
    if (!folder) return
    
    // Remover pasta e arquivos dentro dela
    draft.files = draft.files.filter(f => !f.path.startsWith(folder.path))
    if (draft.folders) {
      draft.folders = draft.folders.filter(f => f.id !== folderId)
    }
  })
}

export function renameFile(
  data: CodeStudioData,
  fileId: string,
  newName: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    const file = draft.files.find(f => f.id === fileId)
    if (file) {
      const pathParts = file.path.split('/')
      pathParts[pathParts.length - 1] = newName
      file.name = newName
      file.path = pathParts.join('/')
    }
  })
}

export function renameFolder(
  data: CodeStudioData,
  folderId: string,
  newName: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    const folder = draft.folders?.find(f => f.id === folderId)
    if (!folder) return
    
    const oldPath = folder.path
    const pathParts = oldPath.split('/')
    pathParts[pathParts.length - 1] = newName
    const newPath = pathParts.join('/')
    
    // Atualizar pasta
    folder.name = newName
    folder.path = newPath
    
    // Atualizar subpastas
    if (draft.folders) {
      draft.folders.forEach(f => {
        if (f.id !== folderId && f.path.startsWith(oldPath + '/')) {
          const relativePath = f.path.substring(oldPath.length + 1)
          f.path = `${newPath}/${relativePath}`
        }
      })
    }
    
    // Atualizar caminhos dos arquivos dentro da pasta
    draft.files.forEach(f => {
      if (f.path.startsWith(oldPath)) {
        f.path = f.path.replace(oldPath, newPath)
      }
    })
  })
}

export function toggleFolder(
  data: CodeStudioData,
  folderId: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    const folder = draft.folders?.find(f => f.id === folderId)
    if (folder) {
      folder.isExpanded = !folder.isExpanded
    }
  })
}

export function moveFile(
  data: CodeStudioData,
  fileId: string,
  newPath: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    const file = draft.files.find(f => f.id === fileId)
    if (!file) return

    const fileName = file.path.split("/").pop() || file.name
    const newFilePath = newPath ? `${newPath}/${fileName}` : fileName

    // Verificar se já existe arquivo com mesmo nome no destino
    const fileExists = draft.files.some(f => f.path === newFilePath && f.id !== fileId)
    if (fileExists) return

    file.path = newFilePath
    file.name = fileName
  })
}

export function moveFolder(
  data: CodeStudioData,
  folderId: string,
  newPath: string
): Partial<CodeStudioData> {
  return produce(data, draft => {
    const folder = draft.folders?.find(f => f.id === folderId)
    if (!folder) return

    const folderName = folder.path.split("/").pop() || folder.name
    const newFolderPath = newPath ? `${newPath}/${folderName}` : folderName

    // Verificar se já existe pasta com mesmo nome no destino
    const folderExists = draft.folders?.some(f => f.path === newFolderPath && f.id !== folderId)
    if (folderExists) return

    const oldPath = folder.path
    
    // Atualizar pasta principal
    folder.path = newFolderPath
    folder.name = folderName
    
    // Atualizar subpastas
    if (draft.folders) {
      draft.folders.forEach(f => {
        if (f.id !== folderId && f.path.startsWith(oldPath + "/")) {
          const relativePath = f.path.substring(oldPath.length + 1)
          f.path = `${newFolderPath}/${relativePath}`
        }
      })
    }

    // Atualizar arquivos dentro da pasta
    draft.files.forEach(f => {
      if (f.path.startsWith(oldPath + "/")) {
        const relativePath = f.path.substring(oldPath.length + 1)
        f.path = `${newFolderPath}/${relativePath}`
      }
    })
  })
}
