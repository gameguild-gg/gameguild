"use client"

import type { CodeFile } from "./types"
import { getTypeScriptSupport, type MonacoRuntime as Monaco } from './monaco-runtime'
import type { editor, IDisposable, languages, Position, Uri } from "monaco-editor"

const DEFAULT_INSTANCE_ID = "default"

interface ExtraLibraryRegistration {
  content: string
  disposables: IDisposable[]
}

interface MonacoWorkspaceState {
  instanceId: string
  consumerCount: number
  files: Map<string, CodeFile>
  directories: Set<string>
  explicitDirectories: Set<string>
  modelUris: Set<string>
  extraLibraries: Map<string, ExtraLibraryRegistration>
}

export interface MonacoWorkspaceSnapshot {
  instanceId: string
  consumerCount: number
  files: readonly CodeFile[]
  directories: readonly string[]
}

const workspaces = new Map<string, MonacoWorkspaceState>()
let completionDisposables: IDisposable[] = []
let completionMonaco: Monaco | null = null
let monacoInstance: Monaco | null = null

function normalizeRelativePath(path: string) {
  const segments: string[] = []

  for (const segment of path.replaceAll("\\", "/").split("/")) {
    if (!segment || segment === ".") continue
    if (segment === "..") {
      segments.pop()
      continue
    }
    segments.push(segment)
  }

  return segments.join("/")
}

function normalizeInstanceId(instanceId?: string) {
  return normalizeRelativePath(instanceId ?? DEFAULT_INSTANCE_ID) || DEFAULT_INSTANCE_ID
}

function getWorkspace(instanceId?: string, create = false) {
  const normalizedInstanceId = normalizeInstanceId(instanceId)
  let workspace = workspaces.get(normalizedInstanceId)

  if (!workspace && create) {
    workspace = {
      instanceId: normalizedInstanceId,
      consumerCount: 0,
      files: new Map(),
      directories: new Set(),
      explicitDirectories: new Set(),
      modelUris: new Set(),
      extraLibraries: new Map(),
    }
    workspaces.set(normalizedInstanceId, workspace)
  }

  return workspace
}

function toWorkspaceUri(monaco: Monaco, workspace: MonacoWorkspaceState, filePath: string): Uri {
  const workspaceRoot = workspace.instanceId === DEFAULT_INSTANCE_ID ? "" : `/${workspace.instanceId}`
  return monaco.Uri.from({
    scheme: "file",
    path: `${workspaceRoot}/${normalizeRelativePath(filePath)}`,
  })
}

function collectDirectories(files: Iterable<CodeFile>) {
  const directories = new Set<string>()

  for (const file of files) {
    const parts = normalizeRelativePath(file.path).split("/")
    for (let index = 1; index < parts.length; index++) {
      directories.add(parts.slice(0, index).join("/"))
    }
  }

  return directories
}

function refreshDirectories(workspace: MonacoWorkspaceState) {
  workspace.directories = new Set([
    ...workspace.explicitDirectories,
    ...collectDirectories(workspace.files.values()),
  ])
}

function disposeExtraLibrary(workspace: MonacoWorkspaceState, filePath: string) {
  const registration = workspace.extraLibraries.get(filePath)
  if (!registration) return

  registration.disposables.forEach((disposable) => disposable.dispose())
  workspace.extraLibraries.delete(filePath)
}

function syncExtraLibrary(workspace: MonacoWorkspaceState, file: CodeFile) {
  const monaco = monacoInstance
  const filePath = normalizeRelativePath(file.path)
  const existing = workspace.extraLibraries.get(filePath)
  const isJavaScriptFamily = file.language === "typescript" || file.language === "javascript"

  if (!monaco || !isJavaScriptFamily) {
    disposeExtraLibrary(workspace, filePath)
    return
  }

  if (existing?.content === file.content) return
  disposeExtraLibrary(workspace, filePath)

  const typescript = getTypeScriptSupport(monaco)
  if (!typescript) return

  const uri = toWorkspaceUri(monaco, workspace, filePath).toString()
  workspace.extraLibraries.set(filePath, {
    content: file.content,
    disposables: [
      typescript.typescriptDefaults.addExtraLib(file.content, uri),
      typescript.javascriptDefaults.addExtraLib(file.content, uri),
    ],
  })
}

function syncModel(
  workspace: MonacoWorkspaceState,
  file: CodeFile,
  previousContent?: string,
  forceUpdate = false,
) {
  const monaco = monacoInstance
  if (!monaco) return

  const uri = toWorkspaceUri(monaco, workspace, file.path)
  const uriKey = uri.toString()
  let model = monaco.editor.getModel(uri)

  if (!model) {
    model = monaco.editor.createModel(file.content, file.language, uri)
  } else {
    const canReplace = forceUpdate || (previousContent !== undefined && model.getValue() === previousContent)
    if (canReplace && model.getValue() !== file.content) {
      model.setValue(file.content)
    }
    if (model.getLanguageId() !== file.language) {
      monaco.editor.setModelLanguage(model, file.language)
    }
  }

  workspace.modelUris.add(uriKey)
  syncExtraLibrary(workspace, file)
}

function disposeWorkspaceFile(workspace: MonacoWorkspaceState, filePath: string) {
  const normalizedPath = normalizeRelativePath(filePath)
  disposeExtraLibrary(workspace, normalizedPath)

  const monaco = monacoInstance
  if (monaco) {
    const uri = toWorkspaceUri(monaco, workspace, normalizedPath)
    const model = monaco.editor.getModel(uri)
    if (model && !model.isDisposed()) model.dispose()
    workspace.modelUris.delete(uri.toString())
  }

  workspace.files.delete(normalizedPath)
}

function disposeWorkspace(workspace: MonacoWorkspaceState) {
  Array.from(workspace.extraLibraries.keys()).forEach((filePath) => disposeExtraLibrary(workspace, filePath))

  const monaco = monacoInstance
  if (monaco) {
    workspace.modelUris.forEach((uriValue) => {
      const model = monaco.editor.getModel(monaco.Uri.parse(uriValue))
      if (model && !model.isDisposed()) model.dispose()
    })
  }

  workspace.modelUris.clear()
  workspace.files.clear()
  workspace.directories.clear()
  workspace.explicitDirectories.clear()
}

function hydrateWorkspace(workspace: MonacoWorkspaceState) {
  workspace.files.forEach((file) => syncModel(workspace, file))
}

function disposeCompletionProviders() {
  completionDisposables.forEach((disposable) => disposable.dispose())
  completionDisposables = []
  completionMonaco = null
}

export function setMonacoInstance(monaco: Monaco) {
  if (monacoInstance !== monaco) {
    disposeCompletionProviders()
    workspaces.forEach((workspace) => {
      Array.from(workspace.extraLibraries.keys()).forEach((filePath) => disposeExtraLibrary(workspace, filePath))
      workspace.modelUris.forEach((uriValue) => {
        const model = monacoInstance?.editor.getModel(monacoInstance.Uri.parse(uriValue))
        if (model && !model.isDisposed()) model.dispose()
      })
      workspace.modelUris.clear()
    })
  }

  monacoInstance = monaco
  workspaces.forEach(hydrateWorkspace)
}

export function initializeMonacoFileSystem(instanceId?: string) {
  if (typeof window === "undefined") return Promise.resolve(null)

  const workspace = getWorkspace(instanceId, true)!
  workspace.consumerCount++
  return Promise.resolve(getFileSystemProvider(instanceId))
}

export function syncFilesToMonacoFS(files: CodeFile[], instanceId?: string) {
  if (typeof window === "undefined") return Promise.resolve()

  const workspace = getWorkspace(instanceId, true)!
  const nextPaths = new Set<string>()

  for (const originalFile of files) {
    const filePath = normalizeRelativePath(originalFile.path)
    if (!filePath) continue

    const file = { ...originalFile, path: filePath }
    const previous = workspace.files.get(filePath)
    workspace.files.set(filePath, file)
    nextPaths.add(filePath)
    syncModel(workspace, file, previous?.content)
  }

  Array.from(workspace.files.keys())
    .filter((filePath) => !nextPaths.has(filePath))
    .forEach((filePath) => disposeWorkspaceFile(workspace, filePath))

  refreshDirectories(workspace)
  return Promise.resolve()
}

export function updateMonacoFile(filePath: string, content: string, instanceId?: string) {
  if (typeof window === "undefined") return Promise.resolve()

  const workspace = getWorkspace(instanceId)
  const normalizedPath = normalizeRelativePath(filePath)
  const current = workspace?.files.get(normalizedPath)
  if (!workspace || !current) return Promise.resolve()

  const updated = { ...current, content }
  workspace.files.set(normalizedPath, updated)
  syncModel(workspace, updated, current.content, true)
  return Promise.resolve()
}

export function deleteMonacoFile(filePath: string, instanceId?: string) {
  const workspace = getWorkspace(instanceId)
  if (!workspace) return Promise.resolve()

  disposeWorkspaceFile(workspace, filePath)
  refreshDirectories(workspace)
  return Promise.resolve()
}

export function createMonacoDirectory(dirPath: string, instanceId?: string) {
  const workspace = getWorkspace(instanceId, true)!
  const normalizedPath = normalizeRelativePath(dirPath)
  if (normalizedPath) {
    workspace.explicitDirectories.add(normalizedPath)
    refreshDirectories(workspace)
  }
}

export function getFileSystemProvider(instanceId?: string): MonacoWorkspaceSnapshot | null {
  const workspace = getWorkspace(instanceId)
  if (!workspace) return null

  return {
    instanceId: workspace.instanceId,
    consumerCount: workspace.consumerCount,
    files: Array.from(workspace.files.values(), (file) => ({ ...file })),
    directories: Array.from(workspace.directories).sort(),
  }
}

export function disposeMonacoFileSystem(instanceId?: string) {
  const workspace = getWorkspace(instanceId)
  if (workspace) {
    workspace.consumerCount = Math.max(0, workspace.consumerCount - 1)
    if (workspace.consumerCount > 0) return

    disposeWorkspace(workspace)
    workspaces.delete(workspace.instanceId)
  }

  if (Array.from(workspaces.values()).every((candidate) => candidate.consumerCount === 0)) {
    workspaces.forEach((candidate) => disposeWorkspace(candidate))
    workspaces.clear()
    disposeCompletionProviders()
    monacoInstance = null
  }
}

function getCompletionContext(modelPath: string) {
  const normalizedModelPath = `/${normalizeRelativePath(modelPath)}`

  for (const workspace of workspaces.values()) {
    if (workspace.instanceId === DEFAULT_INSTANCE_ID) continue
    const prefix = `/${workspace.instanceId}/`
    if (normalizedModelPath.startsWith(prefix)) {
      return {
        workspace,
        relativeModelPath: normalizedModelPath.slice(prefix.length - 1),
      }
    }
  }

  const defaultWorkspace = workspaces.get(DEFAULT_INSTANCE_ID)
  return defaultWorkspace
    ? { workspace: defaultWorkspace, relativeModelPath: normalizedModelPath }
    : null
}

export function registerPathCompletionProvider(monaco: Monaco) {
  setMonacoInstance(monaco)
  if (completionMonaco === monaco && completionDisposables.length > 0) return

  disposeCompletionProviders()
  completionMonaco = monaco

  // Provider combinado para TypeScript e JavaScript
  const createProvider = () => ({
    triggerCharacters: ['"', "'", '/', '.'],
    provideCompletionItems: (model: editor.ITextModel, position: Position) => {
      const lineContent = model.getLineContent(position.lineNumber)
      const textBeforeCursor = lineContent.substring(0, position.column - 1)
      
      const importMatch = textBeforeCursor.match(/(?:import.*?from\s*|require\s*\()\s*['"]([^'"]*?)$/)
      if (!importMatch) return { suggestions: [] }
      
      const currentPath = importMatch[1] || ''
      const context = getCompletionContext(model.uri.path || '')
      if (!context) return { suggestions: [] }
      const modelPath = context.relativeModelPath
      const currentDir = modelPath.split('/').slice(0, -1).join('/')
      
      const suggestions: languages.CompletionItem[] = []

      context.workspace.files.forEach(file => {
        const filePath = `/${file.path}`
        const fileName = file.path.split('/').pop() || ''

        // Não sugerir o próprio arquivo
        if (filePath === modelPath) return

        // Calcular caminho relativo
        let relativePath = ''

        if (currentPath.startsWith('./') || currentPath.startsWith('../') || currentPath.length === 0) {
          // Path relativo ou vazio - sugerir arquivos do mesmo diretário
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
            range: new monaco.Range(
              position.lineNumber,
              position.column - currentPath.length,
              position.lineNumber,
              position.column,
            ),
            detail: `${file.language} file`,
            documentation: `Import from ${file.path}`,
            sortText: `0_${relativePath}`, // Priorizar na lista
          })
        }
      })

      return { suggestions }
    },
  })

  // Provider para Python
  const createPythonProvider = () => ({
    triggerCharacters: ['"', "'", '/', '.'],
    provideCompletionItems: (model: editor.ITextModel, position: Position) => {
      const lineContent = model.getLineContent(position.lineNumber)
      const textBeforeCursor = lineContent.substring(0, position.column - 1)
      
      const importMatch = textBeforeCursor.match(/(?:from\s+|import\s+)([^\s'"]*?)$/)
      if (!importMatch) return { suggestions: [] }
      
      const currentPath = importMatch[1] || ''
      const context = getCompletionContext(model.uri.path || '')
      if (!context) return { suggestions: [] }
      const modelPath = context.relativeModelPath
      const currentDir = modelPath.split('/').slice(0, -1).join('/')
      
      const suggestions: languages.CompletionItem[] = []
      
      context.workspace.files.forEach(file => {
        if (file.language !== 'python') return
        
        const filePath = `/${file.path}`
        const fileName = file.path.split('/').pop() || ''
        const moduleName = fileName.replace(/\.py$/, '')
        
        // Não sugerir o próprio arquivo
        if (filePath === modelPath) return
        
        const fileDir = filePath.split('/').slice(0, -1).join('/')
        
        // Se está no mesmo diretório, sugerir como módulo direto
        if (fileDir === currentDir) {
          suggestions.push({
            label: moduleName,
            kind: monaco.languages.CompletionItemKind.Module,
            insertText: moduleName,
            range: {
              startLineNumber: position.lineNumber,
              startColumn: position.column - currentPath.length,
              endLineNumber: position.lineNumber,
              endColumn: position.column,
            },
            detail: 'Python module',
            documentation: `Import from ${file.path}`,
            sortText: `0_${moduleName}`,
          })
        }
      })
      
      return { suggestions }
    },
  })

  // Provider para Lua
  const createLuaProvider = () => ({
    triggerCharacters: ['"', "'", '/', '.', '('],
    provideCompletionItems: (model: editor.ITextModel, position: Position) => {
      const lineContent = model.getLineContent(position.lineNumber)
      const textBeforeCursor = lineContent.substring(0, position.column - 1)
      
      const requireMatch = textBeforeCursor.match(/require\s*\(?['"]([^'"]*?)$/)
      if (!requireMatch) return { suggestions: [] }
      
      const currentPath = requireMatch[1] || ''
      const context = getCompletionContext(model.uri.path || '')
      if (!context) return { suggestions: [] }
      const modelPath = context.relativeModelPath
      const currentDir = modelPath.split('/').slice(0, -1).join('/')
      
      const suggestions: languages.CompletionItem[] = []
      
      context.workspace.files.forEach(file => {
        if (file.language !== 'lua') return
        
        const filePath = `/${file.path}`
        const fileName = file.path.split('/').pop() || ''
        const moduleName = fileName.replace(/\.lua$/, '')
        
        // Não sugerir o próprio arquivo
        if (filePath === modelPath) return
        
        const fileDir = filePath.split('/').slice(0, -1).join('/')
        
        // Se está no mesmo diretório, sugerir como módulo direto
        if (fileDir === currentDir) {
          suggestions.push({
            label: moduleName,
            kind: monaco.languages.CompletionItemKind.Module,
            insertText: moduleName,
            range: {
              startLineNumber: position.lineNumber,
              startColumn: position.column - currentPath.length,
              endLineNumber: position.lineNumber,
              endColumn: position.column,
            },
            detail: 'Lua module',
            documentation: `Require from ${file.path}`,
            sortText: `0_${moduleName}`,
          })
        }
      })
      
      return { suggestions }
    },
  })

  // Registrar para TypeScript, JavaScript, Python e Lua
  const tsProvider = monaco.languages.registerCompletionItemProvider('typescript', createProvider())
  const jsProvider = monaco.languages.registerCompletionItemProvider('javascript', createProvider())
  const pyProvider = monaco.languages.registerCompletionItemProvider('python', createPythonProvider())
  const luaProvider = monaco.languages.registerCompletionItemProvider('lua', createLuaProvider())

  completionDisposables.push(tsProvider, jsProvider, pyProvider, luaProvider)
}
