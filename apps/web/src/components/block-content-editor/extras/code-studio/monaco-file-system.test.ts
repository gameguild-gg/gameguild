// @vitest-environment node

import { afterEach, describe, expect, it, vi } from "vitest"
import type { CodeFile, SupportedLanguage } from "./types"
import type { MonacoRuntime } from "./monaco-runtime"
import {
  createMonacoDirectory,
  deleteMonacoFile,
  disposeMonacoFileSystem,
  getFileSystemProvider,
  initializeMonacoFileSystem,
  registerPathCompletionProvider,
  syncFilesToMonacoFS,
  updateMonacoFile,
} from "./monaco-file-system"

interface MockUri {
  path: string
  toString: () => string
}

interface MockModel {
  uri: MockUri
  getValue: () => string
  setValue: (value: string) => void
  getLanguageId: () => string
  setLanguageId: (language: string) => void
  isDisposed: () => boolean
  dispose: () => void
}

interface CompletionSuggestion {
  label: string
}

interface CapturedCompletionProvider {
  provideCompletionItems: (
    model: { uri: MockUri; getLineContent: (lineNumber: number) => string },
    position: { lineNumber: number; column: number },
  ) => { suggestions: CompletionSuggestion[] }
}

function createUri(path: string): MockUri {
  return {
    path,
    toString: () => `file://${path}`,
  }
}

function createDisposable() {
  return { dispose: vi.fn() }
}

function createMonacoRuntime() {
  const models = new Map<string, MockModel>()
  const providers = new Map<string, unknown>()
  const extraLibraryDisposables: Array<ReturnType<typeof createDisposable>> = []
  const completionDisposables: Array<ReturnType<typeof createDisposable>> = []

  const createModel = (content: string, language: string, uri: MockUri): MockModel => {
    let value = content
    let languageId = language
    let disposed = false
    const model: MockModel = {
      uri,
      getValue: () => value,
      setValue: (nextValue) => {
        value = nextValue
      },
      getLanguageId: () => languageId,
      setLanguageId: (nextLanguage) => {
        languageId = nextLanguage
      },
      isDisposed: () => disposed,
      dispose: () => {
        disposed = true
        models.delete(uri.toString())
      },
    }
    models.set(uri.toString(), model)
    return model
  }

  const addExtraLib = vi.fn(() => {
    const disposable = createDisposable()
    extraLibraryDisposables.push(disposable)
    return disposable
  })

  const runtime = {
    Uri: {
      from: ({ path }: { path: string }) => createUri(path),
      parse: (value: string) => createUri(value.replace(/^file:\/\//, "")),
    },
    Range: class MockRange {
      constructor(
        public startLineNumber: number,
        public startColumn: number,
        public endLineNumber: number,
        public endColumn: number,
      ) {}
    },
    editor: {
      getModel: (uri: MockUri) => models.get(uri.toString()) ?? null,
      createModel,
      setModelLanguage: (model: MockModel, language: string) => model.setLanguageId(language),
    },
    languages: {
      CompletionItemKind: { File: 1, Module: 2 },
      registerCompletionItemProvider: (language: string, provider: unknown) => {
        providers.set(language, provider)
        const disposable = createDisposable()
        completionDisposables.push(disposable)
        return disposable
      },
    },
    typescript: {
      typescriptDefaults: { addExtraLib },
      javascriptDefaults: { addExtraLib },
    },
  } as unknown as MonacoRuntime

  return {
    runtime,
    models,
    providers,
    addExtraLib,
    extraLibraryDisposables,
    completionDisposables,
  }
}

function createFile(path: string, content: string, language: SupportedLanguage = "typescript"): CodeFile {
  return {
    id: path,
    name: path.split("/").at(-1) ?? path,
    content,
    language,
    isFile: "f",
    isVisible: true,
    path,
  }
}

afterEach(() => {
  for (const instanceId of ["alpha", "beta", "default"]) {
    disposeMonacoFileSystem(instanceId)
    disposeMonacoFileSystem(instanceId)
  }
  vi.unstubAllGlobals()
})

describe("monaco-file-system server compatibility", () => {
  it("can be imported and initialized without a browser window", async () => {
    expect(await initializeMonacoFileSystem("alpha")).toBeNull()
    expect(getFileSystemProvider("alpha")).toBeNull()
  })
})

describe("Monaco workspace registry", () => {
  it("isolates models and path completions by Code Studio instance", async () => {
    vi.stubGlobal("window", {})
    const monaco = createMonacoRuntime()

    await initializeMonacoFileSystem("alpha")
    await initializeMonacoFileSystem("beta")
    registerPathCompletionProvider(monaco.runtime)

    await syncFilesToMonacoFS(
      [
        createFile("src/index.ts", "import { helper } from '", "typescript"),
        createFile("src/helper.ts", "export const helper = 1", "typescript"),
      ],
      "alpha",
    )
    await syncFilesToMonacoFS(
      [
        createFile("src/index.ts", "import { secret } from '", "typescript"),
        createFile("src/secret.ts", "export const secret = 2", "typescript"),
      ],
      "beta",
    )

    expect(monaco.models.has("file:///alpha/src/index.ts")).toBe(true)
    expect(monaco.models.has("file:///beta/src/index.ts")).toBe(true)
    expect(getFileSystemProvider("alpha")?.directories).toEqual(["src"])

    const provider = monaco.providers.get("typescript") as CapturedCompletionProvider
    const source = "import { helper } from '"
    const result = provider.provideCompletionItems(
      {
        uri: createUri("/alpha/src/index.ts"),
        getLineContent: () => source,
      },
      { lineNumber: 1, column: source.length + 1 },
    )

    expect(result.suggestions.map((suggestion) => suggestion.label)).toContain("./helper.ts")
    expect(result.suggestions.map((suggestion) => suggestion.label)).not.toContain("./secret.ts")

    disposeMonacoFileSystem("alpha")
    expect(monaco.models.has("file:///alpha/src/index.ts")).toBe(false)
    expect(monaco.models.has("file:///beta/src/index.ts")).toBe(true)
  })

  it("updates models and extra libraries without leaking registrations", async () => {
    vi.stubGlobal("window", {})
    const monaco = createMonacoRuntime()

    await initializeMonacoFileSystem("alpha")
    await initializeMonacoFileSystem("alpha")
    registerPathCompletionProvider(monaco.runtime)
    await syncFilesToMonacoFS(
      [createFile("src/index.ts", "export const value = 1")],
      "alpha",
    )

    expect(monaco.addExtraLib).toHaveBeenCalledTimes(2)
    await updateMonacoFile("src/index.ts", "export const value = 2", "alpha")

    expect(monaco.models.get("file:///alpha/src/index.ts")?.getValue()).toBe("export const value = 2")
    expect(monaco.addExtraLib).toHaveBeenCalledTimes(4)
    expect(monaco.extraLibraryDisposables.slice(0, 2).every((item) => item.dispose.mock.calls.length === 1)).toBe(true)

    disposeMonacoFileSystem("alpha")
    expect(getFileSystemProvider("alpha")?.consumerCount).toBe(1)
    expect(monaco.models.has("file:///alpha/src/index.ts")).toBe(true)

    disposeMonacoFileSystem("alpha")
    expect(getFileSystemProvider("alpha")).toBeNull()
    expect(monaco.models.has("file:///alpha/src/index.ts")).toBe(false)
    expect(monaco.extraLibraryDisposables.every((item) => item.dispose.mock.calls.length === 1)).toBe(true)
    expect(monaco.completionDisposables.every((item) => item.dispose.mock.calls.length === 1)).toBe(true)
  })

  it("removes stale files and explicit deletions from the Monaco registry", async () => {
    vi.stubGlobal("window", {})
    const monaco = createMonacoRuntime()

    await initializeMonacoFileSystem("alpha")
    registerPathCompletionProvider(monaco.runtime)
    createMonacoDirectory("empty", "alpha")
    await syncFilesToMonacoFS(
      [
        createFile("src/index.ts", "export {}"),
        createFile("src/nested/helper.ts", "export const helper = 1"),
      ],
      "alpha",
    )
    expect(getFileSystemProvider("alpha")?.directories).toEqual(["empty", "src", "src/nested"])

    await syncFilesToMonacoFS([createFile("src/index.ts", "export {}")], "alpha")
    expect(monaco.models.has("file:///alpha/src/nested/helper.ts")).toBe(false)
    expect(getFileSystemProvider("alpha")?.directories).toEqual(["empty", "src"])

    await deleteMonacoFile("src/index.ts", "alpha")
    expect(getFileSystemProvider("alpha")?.files).toEqual([])
    expect(monaco.models.has("file:///alpha/src/index.ts")).toBe(false)
  })
})
