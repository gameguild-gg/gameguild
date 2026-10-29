import type { ComponentProps } from "react"
import { fireEvent, render, screen, waitFor } from "@testing-library/react"
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"

import type { ImportedProjectData } from "../../lib/interopAdapter/project-importer"
import { ImportProjectDialog } from "./import-project-dialog"

const boundary = vi.hoisted(() => ({
  importFromFile: vi.fn<(file: File) => Promise<ImportedProjectData>>(),
  importFromFolder: vi.fn<(files: File[]) => Promise<ImportedProjectData>>(),
  validateImportedData: vi.fn<(data: unknown) => boolean>(),
  reconcileUsage: vi.fn<() => Promise<void>>(),
  removeAsset: vi.fn<() => Promise<void>>(),
  error: vi.fn(),
  success: vi.fn(),
  info: vi.fn(),
}))

vi.mock("@/components/block-content-editor/lib/interopAdapter/project-importer", () => ({
  ProjectImporter: {
    importFromFile: boundary.importFromFile,
    importFromFolder: boundary.importFromFolder,
    validateImportedData: boundary.validateImportedData,
    isSupportedFile: () => true,
    getSupportedExtensions: () => [".zip", ".block-content-editor"],
  },
}))

vi.mock("@game-guild/assets", () => ({ findAssetUris: () => [] }))
vi.mock("@game-guild/assets/browser", () => ({
  getDefaultBrowserAssetRepository: () => ({
    reconcileUsage: boundary.reconcileUsage,
    remove: boundary.removeAsset,
  }),
}))
vi.mock("sonner", () => ({
  toast: { error: boundary.error, success: boundary.success, info: boundary.info },
}))

const importedProject: ImportedProjectData = {
  id: "source-project",
  name: "Imported notes",
  data: '{"order":[],"blocks":{}}',
  tags: ["notes"],
  metadata: null,
  assetsImported: 0,
  importedAssetUris: [],
}

type DialogProps = ComponentProps<typeof ImportProjectDialog>

function renderDialog() {
  const props = {
    open: true,
    onOpenChange: vi.fn<DialogProps["onOpenChange"]>(),
    isDbInitialized: true,
    storageAdapter: {
      list: vi.fn<DialogProps["storageAdapter"]["list"]>().mockResolvedValue([]),
      save: vi.fn<DialogProps["storageAdapter"]["save"]>().mockResolvedValue(undefined),
    },
    availableTags: [],
    onProjectCreate: vi.fn<DialogProps["onProjectCreate"]>(),
    onProjectsListUpdate: vi.fn<DialogProps["onProjectsListUpdate"]>(),
    onAvailableTagsUpdate: vi.fn<DialogProps["onAvailableTagsUpdate"]>(),
    generateProjectId: () => "created-project",
  }
  render(<ImportProjectDialog {...props} />)
  return props
}

function selectProjectFile() {
  const input = document.querySelector<HTMLInputElement>('input[type="file"]:not([multiple])')
  if (!input) throw new Error("The project file picker was not rendered")
  const file = new File(["project bundle"], "notes.zip", { type: "application/zip" })
  fireEvent.change(input, { target: { files: [file] } })
  return file
}

function dropDirectory(entry: FileSystemDirectoryEntry) {
  const dropZone = screen.getByText(/Drag and drop your project file or folder here/).parentElement
  if (!dropZone) throw new Error("The project drop zone was not rendered")
  fireEvent.drop(dropZone, {
    dataTransfer: { items: [{ webkitGetAsEntry: () => entry }], files: [] },
  })
}

function directoryEntry(name: string, reader: FileSystemDirectoryReader): FileSystemDirectoryEntry {
  const entry: FileSystemDirectoryEntry = {
    name,
    fullPath: `/${name}`,
    isDirectory: true,
    isFile: false,
    filesystem: { name: "import-test", get root() { return entry } },
    createReader: () => reader,
    getDirectory: vi.fn<FileSystemDirectoryEntry["getDirectory"]>(),
    getFile: vi.fn<FileSystemDirectoryEntry["getFile"]>(),
    getParent: vi.fn<FileSystemDirectoryEntry["getParent"]>(),
  }
  return entry
}

function fileEntry(file: File, parent: FileSystemDirectoryEntry): FileSystemFileEntry {
  return {
    name: file.name,
    fullPath: `${parent.fullPath}/${file.name}`,
    isFile: true,
    isDirectory: false,
    filesystem: parent.filesystem,
    file: (success) => success(file),
    getParent: vi.fn<FileSystemFileEntry["getParent"]>(),
  }
}

describe("ImportProjectDialog browser and storage boundaries", () => {
  beforeEach(() => {
    vi.resetAllMocks()
    boundary.importFromFile.mockResolvedValue(importedProject)
    boundary.importFromFolder.mockResolvedValue(importedProject)
    boundary.validateImportedData.mockReturnValue(true)
    boundary.reconcileUsage.mockResolvedValue(undefined)
    boundary.removeAsset.mockResolvedValue(undefined)
    vi.spyOn(console, "error").mockImplementation(() => undefined)
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it("keeps the imported name and tags when saving a valid project", async () => {
    const props = renderDialog()
    const file = selectProjectFile()
    await screen.findByDisplayValue(importedProject.name)

    fireEvent.click(screen.getByRole("button", { name: "Save and Exit" }))

    await waitFor(() => expect(props.onProjectCreate).toHaveBeenCalledWith({
      id: "created-project", name: importedProject.name, tags: importedProject.tags,
    }))
    expect(boundary.importFromFile).toHaveBeenCalledWith(file)
    expect(props.storageAdapter.save).toHaveBeenCalledWith(
      "created-project", importedProject.name, importedProject.data, importedProject.tags,
      undefined, undefined,
    )
  })

  it.each([null, undefined, "unstructured failure", { message: 42 }, { message: "" }])(
    "reports a fallback when an importer rejects with %j",
    async (failure) => {
      boundary.importFromFile.mockRejectedValueOnce(failure)
      const props = renderDialog()
      selectProjectFile()

      await waitFor(() => expect(boundary.error).toHaveBeenCalledWith(
        "Import failed", expect.objectContaining({ description: "Failed to import project file" }),
      ))
      expect(props.onProjectCreate).not.toHaveBeenCalled()
    },
  )

  it.each([new Error("Invalid project manifest"), { message: "Invalid project manifest" }])(
    "preserves a valid importer error message: %j",
    async (failure) => {
      boundary.importFromFile.mockRejectedValueOnce(failure)
      renderDialog()
      selectProjectFile()

      await waitFor(() => expect(boundary.error).toHaveBeenCalledWith(
        "Import failed", expect.objectContaining({ description: "Invalid project manifest" }),
      ))
    },
  )

  it("reports a directory reader failure without starting an import", async () => {
    const readEntries = vi.fn<FileSystemDirectoryReader["readEntries"]>()
      .mockImplementation((_success, onError) => onError?.(new DOMException("Directory access denied")))
    renderDialog()
    dropDirectory(directoryEntry("bundle", { readEntries }))

    await waitFor(() => expect(boundary.error).toHaveBeenCalledWith(
      "Import failed", expect.objectContaining({ description: "Directory access denied" }),
    ))
    expect(boundary.importFromFolder).not.toHaveBeenCalled()
    expect(boundary.importFromFile).not.toHaveBeenCalled()
  })

  it("reads every directory batch and preserves nested file paths", async () => {
    const rootReader = { readEntries: vi.fn<FileSystemDirectoryReader["readEntries"]>() }
    const nestedReader = { readEntries: vi.fn<FileSystemDirectoryReader["readEntries"]>() }
    const root = directoryEntry("bundle", rootReader)
    const nested = directoryEntry("nested", nestedReader)
    const index = new File(["{}"], "index.json")
    const content = new File(["{}"], "data.block-content-editor")
    rootReader.readEntries
      .mockImplementationOnce((success) => success([fileEntry(index, root)]))
      .mockImplementationOnce((success) => success([nested]))
      .mockImplementationOnce((success) => success([]))
    nestedReader.readEntries
      .mockImplementationOnce((success) => success([fileEntry(content, nested)]))
      .mockImplementationOnce((success) => success([]))
    renderDialog()
    dropDirectory(root)

    await waitFor(() => expect(boundary.importFromFolder).toHaveBeenCalledWith([index, content]))
    expect(index.webkitRelativePath).toBe("bundle/index.json")
    expect(content.webkitRelativePath).toBe("bundle/nested/data.block-content-editor")
    expect(rootReader.readEntries).toHaveBeenCalledTimes(3)
    expect(nestedReader.readEntries).toHaveBeenCalledTimes(2)
  })

  it("keeps the imported project available when listing stored projects fails", async () => {
    const props = renderDialog()
    props.storageAdapter.list.mockRejectedValueOnce(new Error("Storage is unavailable"))
    selectProjectFile()
    await screen.findByDisplayValue(importedProject.name)

    fireEvent.click(screen.getByRole("button", { name: "Save and Exit" }))

    await waitFor(() => expect(boundary.error).toHaveBeenCalledWith(
      "Failed to save project", expect.objectContaining({
        description: "Could not save the imported project. Please try again.",
      }),
    ))
    expect(props.storageAdapter.save).not.toHaveBeenCalled()
    expect(props.onProjectCreate).not.toHaveBeenCalled()
    expect(props.onOpenChange).not.toHaveBeenCalledWith(false)
    expect(screen.getByDisplayValue(importedProject.name)).toBeInTheDocument()
  })
})
