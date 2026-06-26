"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import {
  Code2,
  File as FileIcon,
  Pencil,
  Plus,
  RefreshCw,
  Save,
  ShieldAlert,
  Trash2,
} from "lucide-react"
import { useTheme } from "next-themes"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { cn } from "@/lib/utils"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"

import { BlockEditorShell } from "@/components/block-content-editor/extras/block-editor-shell"
import { useEditorSettings } from "../settings-menu"
import { FileTabs } from "../code-studio/file-tabs"
import { MonacoCodeEditor } from "../code-studio/monaco-code-editor"
import type { CodeFile } from "../code-studio/types"

import type { HTMLData, HTMLFile } from "@/components/block-content-editor/nodes/html-node"
import {
  ALLOWED_HTML_EXTENSIONS,
  buildHTMLPreviewSrcDoc,
  createDefaultHTMLData,
  languageForFile,
  validateHTMLFileName,
} from "./html-utils"
import { DeleteConfirmDialog } from "../dialogs/delete-confirm-dialog"

interface HTMLEditorProps {
  initialData?: HTMLData
  onSave: (data: HTMLData) => void
  onCancel: () => void
}

/**
 * Two-pane custom-node editor:
 *   - Left: file explorer + tab strip + Monaco editor (HTML/CSS/JSON/XML).
 *   - Right: live iframe preview built from `index.html` and sibling files.
 *
 * Scripting is disabled at every layer: the file allowlist rejects
 * JS-flavoured extensions (`html-utils.validateHTMLFileName`), the
 * preview document strips `<script>` tags and inline event handlers, and
 * the iframe runs with an empty `sandbox` attribute (no scripts, no
 * same-origin).
 */
export function HTMLEditor({ initialData, onSave, onCancel }: HTMLEditorProps) {
  const { resolvedTheme } = useTheme()
  const isDarkMode = resolvedTheme === "dark"
  const settings = useEditorSettings("html")

  // ------------------------------------------------------------------
  // Multi-file state — seeded from initialData or a fresh default.
  // ------------------------------------------------------------------
  const [data, setData] = useState<HTMLData>(() => {
    if (initialData && Array.isArray(initialData.files) && initialData.files.length > 0) {
      return {
        files: initialData.files,
        openTabs: initialData.openTabs?.length ? initialData.openTabs : [initialData.files[0]!.id],
        activeFileId: initialData.activeFileId ?? initialData.files[0]!.id,
      }
    }
    return createDefaultHTMLData()
  })

  const activeFile = useMemo(
    () => data.files.find(f => f.id === data.activeFileId) ?? data.files[0],
    [data.files, data.activeFileId],
  )

  // ------------------------------------------------------------------
  // Live iframe preview — rebuilt with a short debounce when files change.
  // ------------------------------------------------------------------
  const [previewDoc, setPreviewDoc] = useState<string>("")
  useEffect(() => {
    const handle = setTimeout(() => setPreviewDoc(buildHTMLPreviewSrcDoc(data.files)), 200)
    return () => clearTimeout(handle)
  }, [data.files])

  // ------------------------------------------------------------------
  // File operations
  // ------------------------------------------------------------------
  const [newFileName, setNewFileName] = useState("")
  const [newFileError, setNewFileError] = useState<string | null>(null)
  const [renamingId, setRenamingId] = useState<string | null>(null)
  const [renameValue, setRenameValue] = useState("")
  const [renameError, setRenameError] = useState<string | null>(null)
  const [pendingDelete, setPendingDelete] = useState<HTMLFile | null>(null)

  const updateActive = useCallback((id: string) => {
    setData(prev => {
      const exists = prev.files.some(f => f.id === id)
      if (!exists) return prev
      const openTabs = prev.openTabs.includes(id) ? prev.openTabs : [...prev.openTabs, id]
      return { ...prev, activeFileId: id, openTabs }
    })
  }, [])

  const updateContent = useCallback((id: string, content: string) => {
    setData(prev => ({
      ...prev,
      files: prev.files.map(f => (f.id === id ? { ...f, content } : f)),
    }))
  }, [])

  const handleCreateFile = useCallback(() => {
    const validation = validateHTMLFileName(newFileName, data.files.map(f => f.name))
    if (!validation.ok) {
      setNewFileError(validation.reason ?? "Invalid file name")
      return
    }
    const id = typeof crypto !== "undefined" ? crypto.randomUUID() : `f-${Date.now()}`
    const file: HTMLFile = { id, name: newFileName.trim(), content: "" }
    setData(prev => ({
      files: [...prev.files, file],
      openTabs: [...prev.openTabs, id],
      activeFileId: id,
    }))
    setNewFileName("")
    setNewFileError(null)
  }, [newFileName, data.files])

  const beginRename = useCallback((file: HTMLFile) => {
    setRenamingId(file.id)
    setRenameValue(file.name)
    setRenameError(null)
  }, [])

  const commitRename = useCallback(() => {
    if (!renamingId) return
    const current = data.files.find(f => f.id === renamingId)
    if (!current) {
      setRenamingId(null)
      return
    }
    if (renameValue.trim() === current.name) {
      setRenamingId(null)
      return
    }
    const others = data.files.filter(f => f.id !== renamingId).map(f => f.name)
    const validation = validateHTMLFileName(renameValue, others)
    if (!validation.ok) {
      setRenameError(validation.reason ?? "Invalid file name")
      return
    }
    setData(prev => ({
      ...prev,
      files: prev.files.map(f => (f.id === renamingId ? { ...f, name: renameValue.trim() } : f)),
    }))
    setRenamingId(null)
    setRenameError(null)
  }, [renamingId, renameValue, data.files])

  const performDelete = useCallback(() => {
    if (!pendingDelete) return
    const id = pendingDelete.id
    setData(prev => {
      const files = prev.files.filter(f => f.id !== id)
      const openTabs = prev.openTabs.filter(t => t !== id)
      const activeFileId =
        prev.activeFileId === id
          ? (openTabs[openTabs.length - 1] ?? files[0]?.id)
          : prev.activeFileId
      return { files, openTabs, activeFileId }
    })
    setPendingDelete(null)
  }, [pendingDelete])

  const handleCloseTab = useCallback((id: string) => {
    setData(prev => {
      const openTabs = prev.openTabs.filter(t => t !== id)
      const activeFileId =
        prev.activeFileId === id
          ? (openTabs[openTabs.length - 1] ?? prev.files[0]?.id)
          : prev.activeFileId
      return { ...prev, openTabs, activeFileId }
    })
  }, [])

  const handleReorderTabs = useCallback((newOrder: string[]) => {
    setData(prev => ({ ...prev, openTabs: newOrder }))
  }, [])

  // ------------------------------------------------------------------
  // Adapt our HTMLFile[] to code-studio's CodeFile[] for FileTabs reuse.
  // ------------------------------------------------------------------
  const tabFiles: CodeFile[] = useMemo(
    () =>
      data.files.map(f => ({
        id: f.id,
        name: f.name,
        content: f.content,
        language: languageForFile(f.name),
        isFile: "f",
        isVisible: true,
        path: f.name,
      })),
    [data.files],
  )

  const handleSave = () => {
    onSave(data)
  }

  return (
    <BlockEditorShell
      settings={settings}
      onClose={onCancel}
      icon={<Code2 className="h-5 w-5 text-orange-600 dark:text-orange-400" />}
      title="HTML Custom Block"
      headerMeta={
        <div className="flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400">
          <ShieldAlert className="h-3.5 w-3.5" />
          <span>Scripts disabled</span>
        </div>
      }
      footer={
        <div className="flex gap-2 justify-end">
          <Button
            variant="outline"
            onClick={onCancel}
            className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
          >
            Cancel
          </Button>
          <Button
            onClick={handleSave}
            className="flex items-center gap-2 bg-orange-600 hover:bg-orange-700 dark:bg-orange-500 dark:hover:bg-orange-600"
          >
            <Save className="h-4 w-4" />
            Save
          </Button>
        </div>
      }
    >
      <div className="flex-1 overflow-hidden flex">
        {/* LEFT — Editor environment */}
        <div className="flex-1 min-w-0 flex border-r border-gray-200 dark:border-gray-800">
          {/* File panel */}
          <aside className="w-56 shrink-0 flex flex-col border-r border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-950">
            <div className="px-3 py-2 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between">
              <h3 className="text-[11px] font-semibold uppercase tracking-wide text-gray-600 dark:text-gray-400">
                Files
              </h3>
              <span className="text-[10px] text-gray-400 dark:text-gray-600">
                {data.files.length}
              </span>
            </div>

            <ul className="flex-1 overflow-y-auto py-1">
              {data.files.map(file => {
                const isActive = file.id === data.activeFileId
                const isRenaming = renamingId === file.id
                return (
                  <li key={file.id}>
                    {isRenaming ? (
                      <div className="px-2 py-1">
                        <Input
                          autoFocus
                          value={renameValue}
                          onChange={e => {
                            setRenameValue(e.target.value)
                            setRenameError(null)
                          }}
                          onBlur={commitRename}
                          onKeyDown={e => {
                            if (e.key === "Enter") commitRename()
                            else if (e.key === "Escape") {
                              setRenamingId(null)
                              setRenameError(null)
                            }
                          }}
                          className="h-7 text-xs"
                        />
                        {renameError && (
                          <p className="mt-1 text-[10px] text-red-600 dark:text-red-400">
                            {renameError}
                          </p>
                        )}
                      </div>
                    ) : (
                      <div
                        className={cn(
                          "group flex items-center gap-1.5 px-3 py-1 cursor-pointer text-xs",
                          isActive
                            ? "bg-white dark:bg-gray-900 text-orange-600 dark:text-orange-400 font-medium"
                            : "text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-900",
                        )}
                        onClick={() => updateActive(file.id)}
                      >
                        <FileIcon className="h-3 w-3 shrink-0" />
                        <span className="truncate flex-1">{file.name}</span>
                        <button
                          type="button"
                          aria-label={`Rename ${file.name}`}
                          className="opacity-0 group-hover:opacity-100 text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
                          onClick={e => {
                            e.stopPropagation()
                            beginRename(file)
                          }}
                        >
                          <Pencil className="h-3 w-3" />
                        </button>
                        <button
                          type="button"
                          aria-label={`Delete ${file.name}`}
                          className="opacity-0 group-hover:opacity-100 text-gray-400 hover:text-red-600 dark:hover:text-red-400"
                          onClick={e => {
                            e.stopPropagation()
                            setPendingDelete(file)
                          }}
                        >
                          <Trash2 className="h-3 w-3" />
                        </button>
                      </div>
                    )}
                  </li>
                )
              })}
            </ul>

            <div className="border-t border-gray-200 dark:border-gray-800 p-2 space-y-1">
              <div className="flex gap-1">
                <Input
                  value={newFileName}
                  onChange={e => {
                    setNewFileName(e.target.value)
                    setNewFileError(null)
                  }}
                  onKeyDown={e => {
                    if (e.key === "Enter") handleCreateFile()
                  }}
                  placeholder="name.html"
                  className="h-7 text-xs"
                />
                <TooltipProvider delayDuration={300}>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        type="button"
                        size="sm"
                        variant="outline"
                        onClick={handleCreateFile}
                        disabled={!newFileName.trim()}
                        className="h-7 px-2"
                      >
                        <Plus className="h-3.5 w-3.5" />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent side="top">Add file</TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              </div>
              {newFileError && (
                <p className="text-[10px] text-red-600 dark:text-red-400">{newFileError}</p>
              )}
              <p className="text-[10px] text-gray-500 dark:text-gray-500">
                Allowed: {ALLOWED_HTML_EXTENSIONS.join(", ")}
              </p>
            </div>
          </aside>

          {/* Tabs + Monaco */}
          <div className="flex-1 min-w-0 flex flex-col">
            <FileTabs
              files={tabFiles}
              openTabs={data.openTabs}
              activeFileId={data.activeFileId}
              onSelectTab={updateActive}
              onCloseTab={handleCloseTab}
              onReorderTabs={handleReorderTabs}
            />
            <div className="flex-1 min-h-0">
              {activeFile ? (
                <MonacoCodeEditor
                  key={activeFile.id}
                  value={activeFile.content}
                  language={languageForFile(activeFile.name)}
                  onChange={(value) => updateContent(activeFile.id, value)}
                  options={settings.editor}
                  fileId={activeFile.id}
                  filePath={activeFile.name}
                  instanceId={`html-block-${activeFile.id}`}
                />
              ) : (
                <div className="h-full flex items-center justify-center text-sm text-gray-500 dark:text-gray-500">
                  No file selected.
                </div>
              )}
            </div>
          </div>
        </div>

        {/* RIGHT — Live preview */}
        <div className="w-2/5 min-w-[320px] flex flex-col bg-white dark:bg-gray-950">
          <div className="px-4 py-2 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 flex items-center justify-between">
            <h3 className="text-[11px] font-semibold uppercase tracking-wide text-gray-600 dark:text-gray-400">
              Live preview · index.html
            </h3>
            <Button
              type="button"
              size="sm"
              variant="ghost"
              className="h-6 px-2 text-[11px]"
              onClick={() => setPreviewDoc(buildHTMLPreviewSrcDoc(data.files))}
              title="Refresh preview"
            >
              <RefreshCw className="h-3 w-3" />
            </Button>
          </div>
          <div className="flex-1 overflow-hidden">
            <iframe
              key={isDarkMode ? "dark" : "light"}
              srcDoc={previewDoc}
              title="HTML preview"
              sandbox=""
              className="w-full h-full border-0 bg-white"
            />
          </div>
        </div>
      </div>

      {pendingDelete && (
        <DeleteConfirmDialog
          open
          onOpenChange={(open) => {
            if (!open) setPendingDelete(null)
          }}
          title={`Delete ${pendingDelete.name}?`}
          itemName={pendingDelete.name}
          itemType="file"
          onConfirm={performDelete}
          confirmText="Delete"
        />
      )}
    </BlockEditorShell>
  )
}
