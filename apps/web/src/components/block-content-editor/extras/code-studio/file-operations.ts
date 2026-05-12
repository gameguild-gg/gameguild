import type { CodeStudioData, CodeFile, FileTreeFolder } from "./types"
import { LANGUAGE_CONFIGS, getLanguageFromExtension } from "./types"
import { openFile } from "./editor-state-utils"

export function createFile(
  draft: CodeStudioData,
  path: string,
  name: string,
  activeDisplayId: string = 'display-1'
): void {
  const language = getLanguageFromExtension(name)
  const fullPath = path ? `${path}/${name}` : name
  
  // Verificar se arquivo já existe
  const existingFile = draft.files.find(f => f.path === fullPath)
  if (existingFile) {
    // Arquivo já existe, apenas abrir ele usando utilitário centralizado
    openFile(draft, activeDisplayId, existingFile.id)
    return
  }
  
  // Determinar conteúdo inicial baseado no nome do arquivo
  const fileNameWithoutExt = (name.split('.')[0] || '').toLowerCase()
  const isHelloFile = fileNameWithoutExt === 'hello'
  
  const content = isHelloFile 
    ? LANGUAGE_CONFIGS[language].defaultTemplate 
    : '' // Arquivo vazio para outros nomes
  
  // Criar novo arquivo
  const newFileId = Date.now().toString()
  const newFile: CodeFile = {
    id: newFileId,
    name,
    content,
    language,
    isFile: 'f', // Arquivo padrão
    isVisible: true,
    path: fullPath,
  }

  draft.files.push(newFile)
  
  // Abrir o novo arquivo usando utilitário centralizado
  openFile(draft, activeDisplayId, newFileId)
}

export function createFolder(
  draft: CodeStudioData,
  path: string,
  name: string
): void {
  const fullPath = path ? `${path}/${name}` : name
  const newFolder: FileTreeFolder = {
    id: Date.now().toString(),
    name,
    path: fullPath,
    isExpanded: true,
    isVisible: true,
    children: [],
    type: "folder",
  }
  
  if (!draft.folders) {
    draft.folders = []
  }
  draft.folders.push(newFolder)
}

export function deleteFile(
  draft: CodeStudioData,
  fileId: string
): void {
  // Remover arquivo da lista
  draft.files = draft.files.filter(f => f.id !== fileId)
  
  // Remover de todas as abas abertas (global e displays)
  if (draft.openTabs) {
    draft.openTabs = draft.openTabs.filter(id => id !== fileId)
  }
  
  // Remover das abas de cada display
  draft.layout?.displays.forEach(display => {
    if (display.uniqueOpenTabs) {
      display.uniqueOpenTabs = display.uniqueOpenTabs.filter(id => id !== fileId)
      
      // Se era o arquivo ativo, selecionar outro
      if (display.uniqueActiveFileId === fileId) {
        display.uniqueActiveFileId = display.uniqueOpenTabs.length > 0
          ? display.uniqueOpenTabs[display.uniqueOpenTabs.length - 1]
          : undefined
      }
    }
  })
  
  // Atualizar activeFileId global
  if (draft.activeFileId === fileId) {
    if (draft.openTabs && draft.openTabs.length > 0) {
      draft.activeFileId = draft.openTabs[0]
    } else if (draft.files.length > 0) {
      draft.activeFileId = draft.files[0]?.id
    } else {
      draft.activeFileId = undefined
    }
  }
}

export function deleteFolder(
  draft: CodeStudioData,
  folderId: string
): void {
  const folder = draft.folders?.find(f => f.id === folderId)
  if (!folder) return
  
  // Remover pasta e arquivos dentro dela
  draft.files = draft.files.filter(f => !f.path.startsWith(folder.path))
  if (draft.folders) {
    draft.folders = draft.folders.filter(f => f.id !== folderId)
  }
}

export function renameFile(
  draft: CodeStudioData,
  fileId: string,
  newName: string
): void {
  const file = draft.files.find(f => f.id === fileId)
  if (file) {
    const pathParts = file.path.split('/')
    pathParts[pathParts.length - 1] = newName
    file.name = newName
    file.path = pathParts.join('/')
  }
}

export function renameFolder(
  draft: CodeStudioData,
  folderId: string,
  newName: string
): void {
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
}

export function toggleFolder(
  draft: CodeStudioData,
  folderId: string
): void {
  const folder = draft.folders?.find(f => f.id === folderId)
  if (folder) {
    folder.isExpanded = !folder.isExpanded
  }
}

export function moveFile(
  draft: CodeStudioData,
  fileId: string,
  newPath: string
): void {
  const file = draft.files.find(f => f.id === fileId)
  if (!file) return

  const fileName = file.path.split("/").pop() || file.name
  const newFilePath = newPath ? `${newPath}/${fileName}` : fileName

  // Verificar se já existe arquivo com mesmo nome no destino
  const fileExists = draft.files.some(f => f.path === newFilePath && f.id !== fileId)
  if (fileExists) return

  file.path = newFilePath
  file.name = fileName
}

export function moveFolder(
  draft: CodeStudioData,
  folderId: string,
  newPath: string
): void {
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
}

export function reorderFiles(
  draft: CodeStudioData,
  newOrder: CodeFile[]
): void {
  // Substituir a lista de arquivos pela nova ordem
  draft.files = newOrder
}

export function addFileFromAsset(
  draft: CodeStudioData,
  path: string,
  assetId: string,
  fileName: string,
  content: string,
  activeDisplayId: string = 'display-1'
): void {
  const language = getLanguageFromExtension(fileName)
  const fullPath = path ? `${path}/${fileName}` : fileName
  
  // Verificar se arquivo já existe
  const existingFile = draft.files.find(f => f.path === fullPath)
  if (existingFile) {
    // Se arquivo existe, apenas abrir
    openFile(draft, activeDisplayId, existingFile.id)
    return
  }
  
  // Criar novo arquivo referenciando o asset original
  // Em vez de duplicar o conteúdo, armazenar apenas referência
  const newFileId = Date.now().toString()
  const newFile: CodeFile = {
    id: newFileId,
    name: fileName,
    content: `asset://${assetId}`, // Referência ao asset, não o conteúdo completo
    language,
    isFile: 'f', // Arquivo padrão
    isVisible: true,
    path: fullPath,
    assetId, // Guardar referência ao asset original
    isModified: false, // Marcar como não modificado inicialmente
  }

  draft.files.push(newFile)
  
  // Abrir o novo arquivo
  openFile(draft, activeDisplayId, newFileId)
}

export function markFileAsModified(
  draft: CodeStudioData,
  fileId: string
): void {
  const file = draft.files.find(f => f.id === fileId)
  if (file && file.assetId) {
    file.isModified = true
  }
}

export function createCopyOnSave(
  draft: CodeStudioData,
  fileId: string
): string | null {
  const file = draft.files.find(f => f.id === fileId)
  if (!file || !file.assetId || !file.isModified) {
    return null // Não precisa processar
  }

  // Simplesmente remover a referência ao asset, mantendo o mesmo nome
  // O arquivo passa a ser local com o conteúdo já modificado
  file.assetId = undefined
  file.isModified = false
  
  return fileId // Retorna o mesmo ID pois não criamos novo arquivo
}

/**
 * Verifica se o content é uma referência a asset e retorna true
 */
export function isAssetReference(content: string): boolean {
  return content.startsWith('asset://')
}

/**
 * Extrai o assetId de uma referência asset://id
 */
export function extractAssetId(content: string): string | null {
  if (!isAssetReference(content)) return null
  return content.replace('asset://', '')
}

/**
 * Resolve o conteúdo de um arquivo, buscando do asset se necessário
 */
export async function resolveFileContent(file: CodeFile): Promise<string> {
  // Se não é referência, retornar o conteúdo direto
  if (!isAssetReference(file.content)) {
    return file.content
  }
  
  // É referência a asset, buscar o conteúdo
  const assetId = extractAssetId(file.content)
  if (!assetId) return file.content
  
  try {
    const { assetManager } = await import("@/components/block-content-editor/lib/storage/assets/asset-manager")
    const assetData = await assetManager.getAsset(assetId)
    
    if (assetData?.data) {
      // Se for um dataURL, converter para texto
      if (assetData.data.startsWith("data:")) {
        const base64Data = assetData.data.split(",")[1]
        if (base64Data) {
          return atob(base64Data)
        }
      }
      return assetData.data
    }
  } catch (error) {
    console.error('Failed to resolve asset content:', error)
  }
  
  return '' // Fallback para string vazia se falhar
}

export function toggleFileVisibility(
  draft: CodeStudioData,
  fileId: string
): void {
  const file = draft.files.find(f => f.id === fileId)
  if (file) {
    file.isVisible = !file.isVisible
  }
}

export function toggleFolderVisibility(
  draft: CodeStudioData,
  folderId: string
): void {
  const folder = draft.folders?.find(f => f.id === folderId)
  if (folder) {
    folder.isVisible = !folder.isVisible
  }
}

export function toggleFileReadonly(
  draft: CodeStudioData,
  fileId: string
): void {
  const file = draft.files.find(f => f.id === fileId)
  if (file) {
    file.readonly = !file.readonly
  }
}

export function toggleFolderReadonly(
  draft: CodeStudioData,
  folderId: string
): void {
  const folder = draft.folders?.find(f => f.id === folderId)
  if (folder) {
    folder.readonly = !folder.readonly
  }
}

export function toggleFocusFolder(
  draft: CodeStudioData,
  folderId: string
): void {
  const folder = draft.folders?.find(f => f.id === folderId)
  if (folder) {
    // Desmarcar todas as outras pastas como focus
    draft.folders?.forEach(f => {
      if (f.id !== folderId) {
        f.isFocusFolder = false
      }
    })
    // Toggle na pasta selecionada
    folder.isFocusFolder = !folder.isFocusFolder
  }
}

export function setAllFilesReadonly(
  draft: CodeStudioData,
  readonly: boolean
): void {
  // Setar em todos os arquivos
  draft.files.forEach(file => {
    file.readonly = readonly
  })
  
  // Setar em todas as pastas
  if (draft.folders) {
    draft.folders.forEach(folder => {
      folder.readonly = readonly
    })
  }
}

export function setAllFilesHidden(
  draft: CodeStudioData,
  hidden: boolean
): void {
  // Setar em todos os arquivos
  draft.files.forEach(file => {
    file.isVisible = !hidden
  })
  
  // Setar em todas as pastas
  if (draft.folders) {
    draft.folders.forEach(folder => {
      folder.isVisible = !hidden
    })
  }
}
