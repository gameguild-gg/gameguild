"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { useEffect, useState } from "react"
import { toast } from "sonner"
import { StorageOptionSelector, type StorageOption } from "./storage-option-selector"
import {
  NODE_RESTRICTIONS,
  type ProjectMode,
} from "@/components/block-content-editor/lib/storage/editor/project-modes"
import { EMPTY_PROJECT_DATA } from "@/components/block-content-editor/lib/storage/editor/block-storage"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"

interface StorageAdapter {
  list: () => Promise<ProjectData[]>
  save: (id: string, name: string, data: string, tags: string[], storageType?: "local" | "gameguild-cloud" | "google-drive", preferences?: any) => Promise<void>
}

interface CreateProjectDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
  availableTags: Array<{ name: string }>
  onProjectCreate: (projectData: {
    id: string
    name: string
    tags: string[]
    storageType: "local" | "gameguild-cloud" | "google-drive"
    mode: ProjectMode
  }) => void
  onProjectsListUpdate: () => void
  onAvailableTagsUpdate: () => void
  generateProjectId: () => string
  allowedModes?: ProjectMode[]
  defaultMode?: ProjectMode
}

export function CreateProjectDialog({
  open,
  onOpenChange,
  isDbInitialized: _isDbInitialized,
  storageAdapter,
  availableTags,
  onProjectCreate,
  onProjectsListUpdate,
  onAvailableTagsUpdate,
  generateProjectId,
  allowedModes,
  defaultMode,
}: CreateProjectDialogProps) {
  const [newCreateProjectName, setNewCreateProjectName] = useState("")
  const [projectTags, setProjectTags] = useState<string[]>([])
  const [tagInput, setTagInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)
  const [storageOption, setStorageOption] = useState<StorageOption>("local")
  const [projectMode, setProjectMode] = useState<ProjectMode>(defaultMode || "free-page")

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element
        if (!target.closest(".relative")) setShowTagDropdown(false)
      }
    }
    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [showTagDropdown])

  const handleCreate = async () => {
    if (!newCreateProjectName.trim()) {
      toast.error("Nome obrigatório", {
        description: "Por favor, digite um nome para o projeto",
        duration: 3000,
        icon: "✏️",
      })
      return
    }
    if (projectTags.length === 0) {
      toast.error("Tags obrigatórias", {
        description: "Por favor, adicione pelo menos uma tag ao projeto",
        duration: 3000,
        icon: "🏷️",
      })
      return
    }
    if (!storageOption) {
      toast.error("Opção de armazenamento obrigatória", {
        description: "Por favor, selecione uma opção de armazenamento",
        duration: 3000,
        icon: "💾",
      })
      return
    }

    const existingProjects = await storageAdapter.list()
    if (existingProjects.some((p) => p.name === newCreateProjectName.trim())) {
      let suggestedName = `${newCreateProjectName.trim()}-v2`
      let counter = 2
      while (existingProjects.some((p) => p.name === suggestedName)) {
        counter++
        suggestedName = `${newCreateProjectName.trim()}-v${counter}`
      }
      toast.error("Nome já existe", {
        description: `Já há projeto com o nome "${newCreateProjectName.trim()}". Sugestão: ${suggestedName}`,
        duration: 5000,
        icon: "🚫",
      })
      return
    }

    try {
      const newProjectId = generateProjectId()
      const restrictions = NODE_RESTRICTIONS[projectMode]
      const preferences = {
        global: { mode: projectMode, restrictions },
        nodes: {},
      }

      await storageAdapter.save(
        newProjectId,
        newCreateProjectName,
        EMPTY_PROJECT_DATA,
        projectTags,
        storageOption,
        preferences,
      )

      onProjectCreate({
        id: newProjectId,
        name: newCreateProjectName,
        tags: projectTags,
        storageType: storageOption,
        mode: projectMode,
      })

      setNewCreateProjectName("")
      setProjectTags([])
      setTagInput("")
      setShowTagDropdown(false)
      setStorageOption("local")
      setProjectMode(defaultMode || "free-page")
      onOpenChange(false)

      await onProjectsListUpdate()
      await onAvailableTagsUpdate()

      toast.success("Novo projeto criado", {
        description: `"${newCreateProjectName}" foi criado com sucesso`,
        duration: 3000,
        icon: "🎉",
      })
    } catch (error) {
      console.error("Create error:", error)
      toast.error("Erro ao criar projeto", {
        description: "Não foi possível criar o projeto. Tente novamente.",
        duration: 4000,
        icon: "❌",
      })
    }
  }

  const handleCancel = () => {
    setNewCreateProjectName("")
    setProjectTags([])
    setTagInput("")
    setShowTagDropdown(false)
    setStorageOption("local")
    setProjectMode(defaultMode || "free-page")
    onOpenChange(false)
  }

  const allModes: { value: ProjectMode; label: string }[] = [
    { value: "free-page", label: "Free Page - No restrictions" },
    { value: "code-page", label: "Code Page - Code studio focused" },
    { value: "quiz-page", label: "Quiz Page - Quiz focused" },
  ]
  const visibleModes = allowedModes ? allModes.filter((m) => allowedModes.includes(m.value)) : allModes

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-5xl">
        <DialogHeader>
          <DialogTitle className="text-2xl">Create New Project</DialogTitle>
        </DialogHeader>

        <div>
          <Label htmlFor="create-project-name" className="text-sm font-semibold">Project Name *</Label>
          <Input
            id="create-project-name"
            value={newCreateProjectName}
            onChange={(e) => setNewCreateProjectName(e.target.value)}
            placeholder="Enter project name..."
            onKeyDown={(e) => e.key === "Enter" && !e.shiftKey && handleCreate()}
            className="mt-1.5 h-10"
          />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="space-y-3">
            {visibleModes.length > 1 ? (
              <div>
                <Label htmlFor="project-mode" className="text-sm font-semibold">Content Mode *</Label>
                <select
                  id="project-mode"
                  value={projectMode}
                  onChange={(e) => setProjectMode(e.target.value as ProjectMode)}
                  className="w-full px-3 py-2 mt-1.5 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400"
                >
                  {visibleModes.map((m) => (
                    <option key={m.value} value={m.value}>{m.label}</option>
                  ))}
                </select>
              </div>
            ) : null}

            <div className="space-y-1.5">
              <Label className="text-sm font-semibold">Tags (required) *</Label>
              <div className="relative">
                <div className="flex gap-2">
                  <div className="relative flex-1">
                    <Input
                      placeholder="Search or create tags..."
                      value={tagInput}
                      onChange={(e) => { setTagInput(e.target.value); setShowTagDropdown(true) }}
                      onKeyDown={(e) => {
                        if (e.key === "Enter" && tagInput.trim()) {
                          e.preventDefault()
                          const newTag = tagInput.trim()
                          if (!projectTags.includes(newTag)) setProjectTags((prev) => [...prev, newTag])
                          setTagInput("")
                          setShowTagDropdown(false)
                        }
                        if (e.key === "Escape") setShowTagDropdown(false)
                      }}
                      onFocus={() => setShowTagDropdown(true)}
                      className="pr-10"
                    />
                    <button
                      type="button"
                      onClick={() => setShowTagDropdown(!showTagDropdown)}
                      className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                    >
                      <svg className={`w-4 h-4 transition-transform ${showTagDropdown ? "rotate-180" : ""}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                      </svg>
                    </button>
                  </div>
                </div>

                {showTagDropdown && (
                  <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-md shadow-lg max-h-36 overflow-y-auto">
                    {(() => {
                      const filteredTags = tagInput.trim()
                        ? availableTags.filter((tag) => tag.name.toLowerCase().includes(tagInput.toLowerCase()) && !projectTags.includes(tag.name))
                        : availableTags.filter((tag) => !projectTags.includes(tag.name))
                      return (
                        <>
                          {filteredTags.length > 0 && (
                            <>
                              {filteredTags.slice(0, 8).map((tag) => (
                                <button
                                  key={tag.name}
                                  type="button"
                                  onClick={() => { setProjectTags((prev) => [...prev, tag.name]); setTagInput(""); setShowTagDropdown(false) }}
                                  className="w-full px-3 py-1.5 text-left hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between"
                                >
                                  <span className="text-sm">{tag.name}</span>
                                </button>
                              ))}
                              {tagInput.trim() && !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) && !projectTags.includes(tagInput.trim()) && (
                                <div className="border-t dark:border-gray-700 my-0.5" />
                              )}
                            </>
                          )}
                          {tagInput.trim() && !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) && !projectTags.includes(tagInput.trim()) && (
                            <button
                              type="button"
                              onClick={() => { const newTag = tagInput.trim(); setProjectTags((prev) => [...prev, newTag]); setTagInput(""); setShowTagDropdown(false) }}
                              className="w-full px-3 py-1.5 text-left hover:bg-gray-100 dark:hover:bg-gray-700"
                            >
                              <div className="flex items-center gap-2">
                                <svg className="w-3.5 h-3.5 text-green-600 dark:text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                                </svg>
                                <span className="text-sm">Create "<strong>{tagInput.trim()}</strong>"</span>
                              </div>
                            </button>
                          )}
                          {filteredTags.length === 0 && !tagInput.trim() && (
                            <div className="px-3 py-1.5 text-sm text-gray-500 dark:text-gray-400">
                              {availableTags.length === 0 ? "No tags yet. Type to create one." : "Type to search or create tags..."}
                            </div>
                          )}
                        </>
                      )
                    })()}
                  </div>
                )}
              </div>

              {projectTags.length > 0 && (
                <div className="flex flex-wrap gap-1.5">
                  {projectTags.map((tag, index) => (
                    <span key={index} className="inline-flex items-center gap-1 px-2.5 py-0.5 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs rounded-full">
                      {tag}
                      <button
                        type="button"
                        onClick={() => setProjectTags((prev) => prev.filter((_, i) => i !== index))}
                        className="hover:text-blue-600 dark:hover:text-blue-300 ml-0.5"
                      >
                        <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="space-y-3">
            <div>
              <Label className="text-sm font-semibold">Storage Location *</Label>
              <div className="mt-1.5">
                <StorageOptionSelector selectedOption={storageOption} onSelectionChange={setStorageOption} />
              </div>
            </div>

            <div className="p-2 bg-blue-50 dark:bg-blue-900/50 rounded-lg">
              <p className="text-xs text-blue-700 dark:text-blue-300">
                Creates a new empty project. At least one tag is required.
              </p>
            </div>
          </div>
        </div>

        <div className="flex justify-end gap-2 pt-1">
          <Button variant="outline" onClick={handleCancel}>Cancel</Button>
          <Button onClick={handleCreate} disabled={projectTags.length === 0 || !storageOption}>
            Create Project
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
