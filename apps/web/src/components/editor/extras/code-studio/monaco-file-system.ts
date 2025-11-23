"use client"

import type { CodeFile } from "./types"
import { registerFileSystemOverlay, RegisteredFileSystemProvider } from '@codingame/monaco-vscode-files-service-override'
import { URI } from 'vscode-uri'
import type { Monaco } from "@monaco-editor/react"
import type { languages } from "monaco-editor"

let fileSystemProvider: RegisteredFileSystemProvider | null = null
let disposable: ReturnType<typeof registerFileSystemOverlay> | null = null
let isInitialized = false
let currentFiles: CodeFile[] = []
let completionDisposables: Array<{ dispose: () => void }> = []
let monacoInstance: Monaco | null = null

export function setMonacoInstance(monaco: Monaco) {
  monacoInstance = monaco
}

export async function initializeMonacoFileSystem() {
  if (isInitialized) return fileSystemProvider

  try {
    // Criar o provider em memória
    fileSystemProvider = new RegisteredFileSystemProvider(false) // false = read-write
    
    // Registrar o overlay (retorna IDisposable para cleanup)
    disposable = registerFileSystemOverlay(1, fileSystemProvider)
    
    isInitialized = true
    console.log('[Monaco FS] File system initialized')
    return fileSystemProvider
  } catch (error) {
    console.error('[Monaco FS] Failed to initialize:', error)
    return null
  }
}

export async function syncFilesToMonacoFS(files: CodeFile[]) {
  if (!fileSystemProvider) {
    await initializeMonacoFileSystem()
  }

  if (!fileSystemProvider) {
    console.error('[Monaco FS] File system not available')
    return
  }

  // Guardar referência dos arquivos para o completion provider
  currentFiles = files

  try {
    // Criar/atualizar cada arquivo no sistema virtual
    for (const file of files) {
      const uri = URI.file(`/${file.path}`)
      
      // Criar ou atualizar arquivo com opções completas
      await fileSystemProvider.writeFile(
        uri,
        new TextEncoder().encode(file.content),
        { create: true, overwrite: true, unlock: false, atomic: false }
      )
    }
    
    // Se Monaco está disponível, adicionar arquivos como extra libs para TypeScript
    if (monacoInstance) {
      files.forEach(file => {
        if (file.language === 'typescript' || file.language === 'javascript') {
          const filePath = `file:///${file.path}`
          
          // Adicionar como lib extra para o TypeScript worker reconhecer
          monacoInstance!.languages.typescript.typescriptDefaults.addExtraLib(
            file.content,
            filePath
          )
          monacoInstance!.languages.typescript.javascriptDefaults.addExtraLib(
            file.content,
            filePath
          )
        }
      })
    }
    
    console.log(`[Monaco FS] Synced ${files.length} files`)
  } catch (error) {
    console.error('[Monaco FS] Failed to sync files:', error)
  }
}

export async function updateMonacoFile(filePath: string, content: string) {
  if (!fileSystemProvider) {
    console.warn('[Monaco FS] File system not initialized')
    return
  }

  try {
    const uri = URI.file(`/${filePath}`)
    await fileSystemProvider.writeFile(
      uri,
      new TextEncoder().encode(content),
      { create: true, overwrite: true, unlock: false, atomic: false }
    )
  } catch (error) {
    console.error(`[Monaco FS] Failed to update file ${filePath}:`, error)
  }
}

export async function deleteMonacoFile(filePath: string) {
  if (!fileSystemProvider) return

  try {
    const uri = URI.file(`/${filePath}`)
    await fileSystemProvider.delete(uri)
  } catch (error) {
    console.error(`[Monaco FS] Failed to delete file ${filePath}:`, error)
  }
}

export async function createMonacoDirectory(dirPath: string) {
  if (!fileSystemProvider) return

  try {
    const uri = URI.file(`/${dirPath}`)
    // RegisteredFileSystemProvider usa mkdirSync ao invés de mkdir com parâmetro
    fileSystemProvider.mkdirSync(uri)
  } catch (error) {
    console.error(`[Monaco FS] Failed to create directory ${dirPath}:`, error)
  }
}

export function getFileSystemProvider() {
  return fileSystemProvider
}

export function disposeMonacoFileSystem() {
  if (disposable) {
    disposable.dispose()
    disposable = null
  }
  
  // Dispose completion providers
  completionDisposables.forEach(d => d.dispose())
  completionDisposables = []
  
  fileSystemProvider = null
  isInitialized = false
  currentFiles = []
  console.log('[Monaco FS] File system disposed')
}

export function registerPathCompletionProvider(monaco: Monaco) {
  // Salvar instância do Monaco
  monacoInstance = monaco
  
  // Dispose previous providers
  completionDisposables.forEach(d => d.dispose())
  completionDisposables = []

  // Provider combinado para TypeScript e JavaScript
  const createProvider = () => ({
    triggerCharacters: ['"', "'", '/', '.'],
    provideCompletionItems: (model: any, position: any) => {
      const lineContent = model.getLineContent(position.lineNumber)
      const textBeforeCursor = lineContent.substring(0, position.column - 1)
      
      // Detectar se estamos dentro de um import/require/from
      const importMatch = textBeforeCursor.match(/(?:import.*?from\s*|require\s*\()\s*['"]([^'"]*?)$/)
      if (!importMatch || !importMatch[1]) return { suggestions: [] }
      
      const currentPath = importMatch[1] || ''
      const modelPath = model.uri.path || ''
      const currentDir = modelPath.split('/').slice(0, -1).join('/')
      
      const suggestions: any[] = []
      
      // Sugerir arquivos disponíveis
      currentFiles.forEach(file => {
        const filePath = `/${file.path}`
        const fileName = file.path.split('/').pop() || ''
        
        // Não sugerir o próprio arquivo
        if (filePath === modelPath) return
        
        // Calcular caminho relativo
        let relativePath = ''
        
        if (currentPath.startsWith('./') || currentPath.startsWith('../') || currentPath.length === 0) {
          // Path relativo ou vazio - sugerir arquivos do mesmo diretório
          const fileDir = filePath.split('/').slice(0, -1).join('/')
          
          if (fileDir === currentDir) {
            relativePath = './' + fileName
          } else {
            // Calcular caminho relativo entre diretórios
            const currentParts = currentDir.split('/').filter(Boolean)
            const fileParts = fileDir.split('/').filter(Boolean)
            
            let commonLength = 0
            while (commonLength < currentParts.length && 
                   commonLength < fileParts.length && 
                   currentParts[commonLength] === fileParts[commonLength]) {
              commonLength++
            }
            
            const upCount = currentParts.length - commonLength
            const downPath = fileParts.slice(commonLength)
            
            if (upCount === 0) {
              relativePath = './' + downPath.concat([fileName]).join('/')
            } else {
              const upPart = Array(upCount).fill('..').join('/')
              relativePath = upPart + (downPath.length > 0 ? '/' + downPath.join('/') : '') + '/' + fileName
            }
          }
        }
        
        if (relativePath) {
          suggestions.push({
            label: relativePath,
            kind: monaco.languages.CompletionItemKind.File,
            insertText: relativePath,
            range: {
              startLineNumber: position.lineNumber,
              startColumn: position.column - currentPath.length,
              endLineNumber: position.lineNumber,
              endColumn: position.column,
            },
            detail: `${file.language} file`,
            documentation: `Import from ${file.path}`,
            sortText: `0_${relativePath}`, // Priorizar na lista
          })
        }
      })
      
      return { suggestions }
    },
  })

  // Registrar para TypeScript e JavaScript
  const tsProvider = monaco.languages.registerCompletionItemProvider('typescript', createProvider())
  const jsProvider = monaco.languages.registerCompletionItemProvider('javascript', createProvider())

  completionDisposables.push(tsProvider, jsProvider)
  console.log('[Monaco FS] Path completion providers registered')
}
