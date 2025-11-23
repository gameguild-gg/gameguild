"use client"

import type { CodeFile } from "./types"
import { registerFileSystemOverlay, RegisteredFileSystemProvider } from '@codingame/monaco-vscode-files-service-override'
import { URI } from 'vscode-uri'

let fileSystemProvider: RegisteredFileSystemProvider | null = null
let disposable: ReturnType<typeof registerFileSystemOverlay> | null = null
let isInitialized = false

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
  fileSystemProvider = null
  isInitialized = false
  console.log('[Monaco FS] File system disposed')
}
