"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { useState, useEffect } from "react"
import { toast } from "sonner"
import { StorageOptionSelector, type StorageOption } from "./storage-option-selector"
import { 
  type ProjectMode, 
  PROJECT_MODES, 
  NODE_RESTRICTIONS,
} from "@/components/block-content-editor/lib/storage/editor/project-modes"
import { createProjectData } from "@/components/block-content-editor/lib/storage/editor/layout-detector"
import { type EngineType, ENGINE_TYPES } from "@/components/block-content-editor/lib/storage/editor/project-types"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
}

interface StorageAdapter {
  list: () => Promise<ProjectData[]>
  save: (id: string, name: string, data: string, tags: string[], storageType?: "local" | "gameguild-cloud" | "google-drive", preferences?: any, engine?: EngineType) => Promise<void>
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
    engine: EngineType
  }) => void
  onProjectsListUpdate: () => void
  onAvailableTagsUpdate: () => void
  generateProjectId: () => string
  allowedEngines?: EngineType[]
  allowedModes?: ProjectMode[]
  defaultMode?: ProjectMode
}

export function CreateProjectDialog({
  open,
  onOpenChange,
  isDbInitialized,
  storageAdapter,
  availableTags,
  onProjectCreate,
  onProjectsListUpdate,
  onAvailableTagsUpdate,
  generateProjectId,
  allowedEngines,
  allowedModes,
  defaultMode,
}: CreateProjectDialogProps) {
  const [newCreateProjectName, setNewCreateProjectName] = useState("")
  const [projectTags, setProjectTags] = useState<string[]>([])
  const [tagInput, setTagInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)
  const [storageOption, setStorageOption] = useState<StorageOption>("local")
  const [projectMode, setProjectMode] = useState<ProjectMode>(defaultMode || "free-page")
  const [engine, setEngine] = useState<EngineType>(ENGINE_TYPES.LEXICAL)

  // Close tag dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element
        if (!target.closest(".relative")) {
          setShowTagDropdown(false)
        }
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



    // Check if project with same name already exists
    const existingProjects = await storageAdapter.list()
    if (existingProjects.some((p) => p.name === newCreateProjectName.trim())) {
      // Generate suggested name with version number
      let suggestedName = `${newCreateProjectName.trim()}-v2`
      let counter = 2

      // Keep incrementing until we find an available name
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

    // Create empty project
    const emptyState = {
      root: {
        children: [{
          children: [],
          direction: null,
          format: "",
          indent: 0,
          type: "paragraph",
          version: 1
        }],
        direction: null,
        format: "",
        indent: 0,
        type: "root",
        version: 1
      }
    }

    try {
      const newProjectId = generateProjectId()
      
      // Get restrictions for the mode
      const restrictions = NODE_RESTRICTIONS[projectMode]
      
      // Create preferences with mode and restrictions
      const preferences = {
        global: {
          mode: projectMode,
          restrictions: restrictions
        },
        nodes: {}
      }
      
      // Create data structure based on engine
      let projectData: string
      
      if (engine === ENGINE_TYPES.BLOCKS) {
        // Block Array engine: empty Cell[] array
        projectData = createProjectData({
          blocks: { b1: [] },
        })
      } else {
        // Lexical engine: single editor
        projectData = createProjectData({
          blocks: {
            b1: emptyState,
          },
        })
      }
      
      await storageAdapter.save(
        newProjectId, 
        newCreateProjectName, 
        projectData, 
        projectTags, 
        storageOption, 
        preferences,
        engine // Engine type
      )

      // Call the callback to update parent state
      onProjectCreate({
        id: newProjectId,
        name: newCreateProjectName,
        tags: projectTags,
        storageType: storageOption,
        mode: projectMode,
        engine,
      })

      // Reset form state
      setNewCreateProjectName("")
      setProjectTags([])
      setTagInput("")
      setShowTagDropdown(false)
      setStorageOption("local")
      setProjectMode(defaultMode || "free-page")
      setEngine(ENGINE_TYPES.LEXICAL)
      onOpenChange(false)

      // Update lists
      await onProjectsListUpdate()
      await onAvailableTagsUpdate()

      toast.success("Novo projeto criado", {
        description: `"${newCreateProjectName}" foi criado com sucesso`,
        duration: 3000,
        icon: "🎉",
      })
    } catch (error: any) {
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
    setEngine(ENGINE_TYPES.LEXICAL)
    onOpenChange(false)
  }

  const allModes: { value: ProjectMode; label: string }[] = [
    { value: "free-page", label: "Free Page - No restrictions" },
    { value: "code-page", label: "Code Page - Code studio focused" },
    { value: "quiz-page", label: "Quiz Page - Quiz focused" },
  ]
  const visibleModes = allowedModes ? allModes.filter(m => allowedModes.includes(m.value)) : allModes

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-7xl sm:max-w-7xl">
        <DialogHeader>
          <DialogTitle className="text-2xl">Create New Project</DialogTitle>
        </DialogHeader>

        {/* Project Name - full width */}
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

        {/* Two-column layout: Structure | Metadata */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">

          {/* ─── Left Column: Structure ─── */}
          <div className="space-y-3">

            {/* Engine Selection */}
            <div>
              <Label className="text-sm font-semibold mb-1.5 block">Editor Engine *</Label>
              <div className="grid grid-cols-2 gap-2">
                {/* Lexical Engine */}
                {(!allowedEngines || allowedEngines.includes(ENGINE_TYPES.LEXICAL)) && <button
                  type="button"
                  onClick={() => setEngine(ENGINE_TYPES.LEXICAL)}
                  className={`relative p-2 rounded-lg border-2 transition-all text-left ${
                    engine === ENGINE_TYPES.LEXICAL
                      ? "border-indigo-500 bg-indigo-50 dark:bg-indigo-950"
                      : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                  }`}
                >
                  {engine === ENGINE_TYPES.LEXICAL && (
                    <div className="absolute top-1.5 right-1.5">
                      <div className="w-4 h-4 bg-indigo-500 rounded-full flex items-center justify-center">
                        <svg className="w-2.5 h-2.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                      </div>
                    </div>
                  )}
                  <div className="mb-1.5 flex justify-center">
                    <div className="w-full h-12 bg-linear-to-br from-indigo-100 to-indigo-200 dark:from-indigo-900 dark:to-indigo-800 rounded border border-indigo-300 dark:border-indigo-600 flex items-center justify-center">
                      <div className="space-y-0.5 w-3/4">
                        <div className="h-1 w-full bg-white dark:bg-gray-700 rounded"></div>
                        <div className="h-1 w-4/5 bg-white dark:bg-gray-700 rounded"></div>
                        <div className="h-1 w-3/5 bg-white dark:bg-gray-700 rounded"></div>
                      </div>
                    </div>
                  </div>
                  <h3 className="font-semibold text-sm">Rich Document</h3>
                  <p className="text-[10px] text-gray-600 dark:text-gray-400 leading-tight">
                    Rich-text with headings, lists, embeds
                  </p>
                </button>}

                {/* Block Array Engine */}
                {(!allowedEngines || allowedEngines.includes(ENGINE_TYPES.BLOCKS)) && <button
                  type="button"
                  onClick={() => setEngine(ENGINE_TYPES.BLOCKS)}
                  className={`relative p-2 rounded-lg border-2 transition-all text-left ${
                    engine === ENGINE_TYPES.BLOCKS
                      ? "border-amber-500 bg-amber-50 dark:bg-amber-950"
                      : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                  }`}
                >
                  {engine === ENGINE_TYPES.BLOCKS && (
                    <div className="absolute top-1.5 right-1.5">
                      <div className="w-4 h-4 bg-amber-500 rounded-full flex items-center justify-center">
                        <svg className="w-2.5 h-2.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                      </div>
                    </div>
                  )}
                  <div className="mb-1.5 flex justify-center">
                    <div className="w-full h-12 bg-linear-to-br from-amber-100 to-amber-200 dark:from-amber-900 dark:to-amber-800 rounded border border-amber-300 dark:border-amber-600 flex flex-col items-center justify-center gap-0.5 p-1">
                      <div className="w-full h-3 bg-white dark:bg-gray-700 rounded flex items-center px-1">
                        <div className="w-1.5 h-1.5 bg-amber-400 dark:bg-amber-500 rounded-sm mr-0.5"></div>
                        <div className="h-0.5 w-6 bg-amber-200 dark:bg-amber-700 rounded"></div>
                      </div>
                      <div className="w-full h-3 bg-white dark:bg-gray-700 rounded flex items-center px-1">
                        <div className="w-1.5 h-1.5 bg-amber-400 dark:bg-amber-500 rounded-sm mr-0.5"></div>
                        <div className="h-0.5 w-4 bg-amber-200 dark:bg-amber-700 rounded"></div>
                      </div>
                    </div>
                  </div>
                  <h3 className="font-semibold text-sm">Block Stack</h3>
                  <p className="text-[10px] text-gray-600 dark:text-gray-400 leading-tight">
                    Stack of decorator blocks (quiz, code, etc.)
                  </p>
                </button>}
              </div>
            </div>

            {/* Content Mode */}
            {visibleModes.length > 1 ? (
            <div>
              <Label htmlFor="project-mode" className="text-sm font-semibold">Content Mode *</Label>
              <select
                id="project-mode"
                value={projectMode}
                onChange={(e) => setProjectMode(e.target.value as ProjectMode)}
                className="w-full px-3 py-2 mt-1.5 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400"
              >
                {visibleModes.map(m => (
                  <option key={m.value} value={m.value}>{m.label}</option>
                ))}
              </select>
            </div>
            ) : null}

            {/* Tags Section */}
            <div className="space-y-1.5">
              <Label className="text-sm font-semibold">Tags (required) *</Label>

              {/* Tag Input with Dropdown */}
              <div className="relative">
                <div className="flex gap-2">
                  <div className="relative flex-1">
                    <Input
                      placeholder="Search or create tags..."
                      value={tagInput}
                      onChange={(e) => {
                        setTagInput(e.target.value)
                        setShowTagDropdown(true)
                      }}
                      onKeyDown={(e) => {
                        if (e.key === "Enter" && tagInput.trim()) {
                          e.preventDefault()
                          const newTag = tagInput.trim()
                          if (!projectTags.includes(newTag)) {
                            setProjectTags((prev) => [...prev, newTag])
                          }
                          setTagInput("")
                          setShowTagDropdown(false)
                        }
                        if (e.key === "Escape") {
                          setShowTagDropdown(false)
                        }
                      }}
                      onFocus={() => setShowTagDropdown(true)}
                      className="pr-10"
                    />
                    <button
                      type="button"
                      onClick={() => setShowTagDropdown(!showTagDropdown)}
                      className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                    >
                      <svg
                        className={`w-4 h-4 transition-transform ${showTagDropdown ? "rotate-180" : ""}`}
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                      </svg>
                    </button>
                  </div>
                </div>

                {/* Dropdown with existing tags */}
                {showTagDropdown && (
                  <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-md shadow-lg max-h-36 overflow-y-auto">
                    {(() => {
                      const filteredTags = tagInput.trim()
                        ? availableTags.filter(
                            (tag) =>
                              tag.name.toLowerCase().includes(tagInput.toLowerCase()) && !projectTags.includes(tag.name),
                          )
                        : availableTags.filter((tag) => !projectTags.includes(tag.name))

                      return (
                        <>
                          {filteredTags.length > 0 && (
                            <>
                              {filteredTags.slice(0, 8).map((tag) => (
                                <button
                                  key={tag.name}
                                  type="button"
                                  onClick={() => {
                                    setProjectTags((prev) => [...prev, tag.name])
                                    setTagInput("")
                                    setShowTagDropdown(false)
                                  }}
                                  className="w-full px-3 py-1.5 text-left hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between"
                                >
                                  <span className="text-sm">{tag.name}</span>
                                </button>
                              ))}
                              {tagInput.trim() &&
                                !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) &&
                                !projectTags.includes(tagInput.trim()) && (
                                  <div className="border-t dark:border-gray-700 my-0.5"></div>
                                )}
                            </>
                          )}

                          {tagInput.trim() &&
                            !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) &&
                            !projectTags.includes(tagInput.trim()) && (
                              <button
                                type="button"
                                onClick={() => {
                                  const newTag = tagInput.trim()
                                  setProjectTags((prev) => [...prev, newTag])
                                  setTagInput("")
                                  setShowTagDropdown(false)
                                }}
                                className="w-full px-3 py-1.5 text-left hover:bg-gray-100 dark:hover:bg-gray-700"
                              >
                                <div className="flex items-center gap-2">
                                  <svg className="w-3.5 h-3.5 text-green-600 dark:text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                                  </svg>
                                  <span className="text-sm">
                                    Create "<strong>{tagInput.trim()}</strong>"
                                  </span>
                                </div>
                              </button>
                            )}

                          {filteredTags.length === 0 && !tagInput.trim() && (
                            <div className="px-3 py-1.5 text-sm text-gray-500 dark:text-gray-400">
                              {availableTags.length === 0
                                ? "No tags yet. Type to create one."
                                : "Type to search or create tags..."}
                            </div>
                          )}

                          {filteredTags.length === 0 &&
                            tagInput.trim() &&
                            !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) &&
                            !projectTags.includes(tagInput.trim()) && (
                              <div className="px-3 py-1.5 text-sm text-gray-500 dark:text-gray-400">
                                No existing tags found. Press Enter to create "{tagInput.trim()}"
                              </div>
                            )}

                          {tagInput.trim() && projectTags.includes(tagInput.trim()) && (
                            <div className="px-3 py-1.5 text-sm text-amber-600 dark:text-amber-400">
                              Tag "{tagInput.trim()}" is already selected
                            </div>
                          )}
                        </>
                      )
                    })()}
                  </div>
                )}
              </div>

              {/* Selected Tags */}
              {projectTags.length > 0 && (
                <div className="flex flex-wrap gap-1.5">
                  {projectTags.map((tag, index) => (
                    <span
                      key={index}
                      className="inline-flex items-center gap-1 px-2.5 py-0.5 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs rounded-full"
                    >
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

              {/* Quick add popular tags */}
              {availableTags.length > 0 && projectTags.length === 0 && (
                <div className="space-y-1">
                  <Label className="text-xs text-gray-500 dark:text-gray-400">Popular:</Label>
                  <div className="flex flex-wrap gap-1.5">
                    {availableTags.slice(0, 6).map((tag) => (
                      <button
                        key={tag.name}
                        type="button"
                        onClick={() => setProjectTags((prev) => [...prev, tag.name])}
                        className="px-2 py-0.5 text-xs rounded-full border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                      >
                        {tag.name}
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* ─── Right Column: Metadata ─── */}
          <div className="space-y-3">

            

            {/* Storage Option */}
            <div>
              <Label className="text-sm font-semibold">Storage Location *</Label>
              <div className="mt-1.5">
                <StorageOptionSelector 
                  selectedOption={storageOption} 
                  onSelectionChange={setStorageOption} 
                />
              </div>
            </div>

            {/* Info box */}
            <div className="p-2 bg-blue-50 dark:bg-blue-900/50 rounded-lg">
              <div className="flex items-start gap-2">
                <svg
                  className="w-3.5 h-3.5 text-blue-600 dark:text-blue-400 mt-0.5 shrink-0"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                <p className="text-xs text-blue-700 dark:text-blue-300">
                  Creates a new empty project. At least one tag is required.
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Action buttons */}
        <div className="flex justify-end gap-2 pt-1">
          <Button variant="outline" onClick={handleCancel}>
            Cancel
          </Button>
          <Button onClick={handleCreate} disabled={projectTags.length === 0 || !storageOption}>
            Create Project
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
