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
  getSuggestedLayoutForMode
} from "@/lib/storage/editor/project-modes"
import { createProjectData } from "@/lib/storage/editor/layout-detector"
import { type ProjectType, PROJECT_TYPES, getLayoutFromType } from "@/lib/storage/editor/project-types"
import { type EngineType, ENGINE_TYPES } from "@/lib/storage/editor/project-types"

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
  save: (id: string, name: string, data: string, tags: string[], storageType?: "local" | "gameguild-cloud" | "google-drive", preferences?: any, type?: string, deps?: any, engine?: EngineType) => Promise<void>
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
    type: ProjectType // Project type
    mode: ProjectMode
    engine: EngineType
  }) => void
  onProjectsListUpdate: () => void
  onAvailableTagsUpdate: () => void
  generateProjectId: () => string
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
}: CreateProjectDialogProps) {
  const [newCreateProjectName, setNewCreateProjectName] = useState("")
  const [projectTags, setProjectTags] = useState<string[]>([])
  const [tagInput, setTagInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)
  const [storageOption, setStorageOption] = useState<StorageOption>("local")
  const [projectMode, setProjectMode] = useState<ProjectMode>("free-page")
  const [projectType, setProjectType] = useState<ProjectType>(PROJECT_TYPES.TYPE1)
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
      
      // Create data structure based on project type and engine
      let projectData: string
      
      if (engine === ENGINE_TYPES.BLOCKS) {
        // Block Array engine: empty Cell[] array
        projectData = createProjectData(projectType, {
          blocks: { b1: [] },
        })
      } else {
        // Lexical engine: standard layout-based data
        const layoutType = getLayoutFromType(projectType)
        
        if (layoutType === "slideshow") {
          // Temporary placeholder - will be replaced by parent component
          projectData = JSON.stringify({ version: "slideshow-v1", slides: [] })
        } else {
          projectData = createProjectData(projectType, {
            blocks: {
              b1: emptyState,
            },
          })
        }
      }
      
      await storageAdapter.save(
        newProjectId, 
        newCreateProjectName, 
        projectData, 
        projectTags, 
        storageOption, 
        preferences,
        projectType, // Project type
        undefined, // deps
        engine // Engine type
      )

      // Call the callback to update parent state
      onProjectCreate({
        id: newProjectId,
        name: newCreateProjectName,
        tags: projectTags,
        storageType: storageOption,
        type: projectType,
        mode: projectMode,
        engine,
      })

      // Reset form state
      setNewCreateProjectName("")
      setProjectTags([])
      setTagInput("")
      setShowTagDropdown(false)
      setStorageOption("local")
      setProjectMode("free-page")
      setProjectType(PROJECT_TYPES.TYPE1)
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
    setProjectMode("free-page")
    setProjectType("type1")
    setEngine(ENGINE_TYPES.LEXICAL)
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-7xl max-h-[95vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-2xl">Create New Project</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          {/* Project Name */}
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

          {/* Project Type Selection - Visual Cards */}
          <div>
            <Label className="text-sm font-semibold mb-2 block">Project Layout *</Label>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              {/* Type 1 - Simple */}
              <button
                type="button"
                onClick={() => setProjectType(PROJECT_TYPES.TYPE1)}
                className={`relative p-3 rounded-lg border-2 transition-all text-left ${
                  projectType === PROJECT_TYPES.TYPE1
                    ? "border-blue-500 bg-blue-50 dark:bg-blue-950"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                }`}
              >
                {projectType === PROJECT_TYPES.TYPE1 && (
                  <div className="absolute top-2 right-2">
                    <div className="w-5 h-5 bg-blue-500 rounded-full flex items-center justify-center">
                      <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    </div>
                  </div>
                )}
                
                {/* Visual representation */}
                <div className="mb-2 flex justify-center">
                  <div className="w-full h-24 bg-gradient-to-br from-blue-100 to-blue-200 dark:from-blue-900 dark:to-blue-800 rounded border-2 border-blue-300 dark:border-blue-600 flex items-center justify-center">
                    <div className="text-center">
                      <div className="w-12 h-12 mx-auto bg-white dark:bg-gray-800 rounded shadow-sm mb-1.5"></div>
                      <div className="space-y-1">
                        <div className="h-1.5 w-16 bg-white dark:bg-gray-700 rounded mx-auto"></div>
                        <div className="h-1.5 w-12 bg-white dark:bg-gray-700 rounded mx-auto"></div>
                      </div>
                    </div>
                  </div>
                </div>
                
                <h3 className="font-semibold text-base mb-1">Simple</h3>
                <p className="text-xs text-gray-600 dark:text-gray-400 mb-1.5">
                  One vertical editor for simple documents, articles, or notes.
                </p>
                <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-500">
                  <span className="px-2 py-0.5 bg-gray-100 dark:bg-gray-800 rounded text-[10px]">Single Layout</span>
                </div>
              </button>

              {/* Type 2 - Multi-Panel */}
              <button
                type="button"
                onClick={() => setProjectType(PROJECT_TYPES.TYPE2)}
                className={`relative p-3 rounded-lg border-2 transition-all text-left ${
                  projectType === PROJECT_TYPES.TYPE2
                    ? "border-green-500 bg-green-50 dark:bg-green-950"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                }`}
              >
                {projectType === PROJECT_TYPES.TYPE2 && (
                  <div className="absolute top-2 right-2">
                    <div className="w-5 h-5 bg-green-500 rounded-full flex items-center justify-center">
                      <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    </div>
                  </div>
                )}
                
                {/* Visual representation */}
                <div className="mb-2 flex justify-center">
                  <div className="w-full h-24 bg-gradient-to-br from-green-100 to-green-200 dark:from-green-900 dark:to-green-800 rounded border-2 border-green-300 dark:border-green-600 flex items-center justify-center gap-1 p-2">
                    <div className="flex-1 h-full bg-white dark:bg-gray-800 rounded shadow-sm flex items-center justify-center">
                      <div className="space-y-1 w-full px-1.5">
                        <div className="h-1.5 w-3/4 bg-green-200 dark:bg-green-700 rounded mx-auto"></div>
                        <div className="h-1.5 w-2/3 bg-green-200 dark:bg-green-700 rounded mx-auto"></div>
                      </div>
                    </div>
                    <div className="w-1 h-full bg-green-400 dark:bg-green-600 rounded"></div>
                    <div className="flex-1 h-full bg-white dark:bg-gray-800 rounded shadow-sm flex items-center justify-center">
                      <div className="space-y-1 w-full px-1.5">
                        <div className="h-1.5 w-3/4 bg-green-200 dark:bg-green-700 rounded mx-auto"></div>
                        <div className="h-1.5 w-2/3 bg-green-200 dark:bg-green-700 rounded mx-auto"></div>
                      </div>
                    </div>
                  </div>
                </div>
                
                <h3 className="font-semibold text-base mb-1">Multi-Panel</h3>
                <p className="text-xs text-gray-600 dark:text-gray-400 mb-1.5">
                  Multiple panels side-by-side (starts with 1, add more as needed) for comparison or parallel content.
                </p>
                <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-500">
                  <span className="px-2 py-0.5 bg-gray-100 dark:bg-gray-800 rounded text-[10px]">Multi Layout</span>
                </div>
              </button>

              {/* Type 3 - Slideshow */}
              <button
                type="button"
                onClick={() => setProjectType(PROJECT_TYPES.TYPE3)}
                className={`relative p-3 rounded-lg border-2 transition-all text-left ${
                  projectType === PROJECT_TYPES.TYPE3
                    ? "border-purple-500 bg-purple-50 dark:bg-purple-950"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                }`}
              >
                {projectType === PROJECT_TYPES.TYPE3 && (
                  <div className="absolute top-2 right-2">
                    <div className="w-5 h-5 bg-purple-500 rounded-full flex items-center justify-center">
                      <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    </div>
                  </div>
                )}
                
                {/* Visual representation */}
                <div className="mb-2 flex justify-center">
                  <div className="w-full h-24 bg-gradient-to-br from-purple-100 to-purple-200 dark:from-purple-900 dark:to-purple-800 rounded border-2 border-purple-300 dark:border-purple-600 flex flex-col items-center justify-center gap-0.5 p-1.5">
                    <div className="w-full h-6 bg-white dark:bg-gray-800 rounded shadow-sm flex items-center px-1.5">
                      <div className="flex items-center gap-1">
                        <div className="w-3 h-3 bg-purple-300 dark:bg-purple-600 rounded-full flex items-center justify-center text-[7px] font-bold text-purple-700 dark:text-purple-200">1</div>
                        <div className="h-1 w-8 bg-purple-200 dark:bg-purple-700 rounded"></div>
                      </div>
                    </div>
                    <div className="w-full h-6 bg-white dark:bg-gray-800 rounded shadow-sm flex items-center px-1.5">
                      <div className="flex items-center gap-1">
                        <div className="w-3 h-3 bg-purple-300 dark:bg-purple-600 rounded-full flex items-center justify-center text-[7px] font-bold text-purple-700 dark:text-purple-200">2</div>
                        <div className="h-1 w-8 bg-purple-200 dark:bg-purple-700 rounded"></div>
                      </div>
                    </div>
                    <div className="w-full h-6 bg-white dark:bg-gray-800 rounded shadow-sm flex items-center px-1.5">
                      <div className="flex items-center gap-1">
                        <div className="w-3 h-3 bg-purple-300 dark:bg-purple-600 rounded-full flex items-center justify-center text-[7px] font-bold text-purple-700 dark:text-purple-200">3</div>
                        <div className="h-1 w-8 bg-purple-200 dark:bg-purple-700 rounded"></div>
                      </div>
                    </div>
                  </div>
                </div>
                
                <h3 className="font-semibold text-base mb-1">Slideshow</h3>
                <p className="text-xs text-gray-600 dark:text-gray-400 mb-1.5">
                  Multiple slides in sequence for presentations or tutorials.
                </p>
                <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-500">
                  <span className="px-2 py-0.5 bg-gray-100 dark:bg-gray-800 rounded text-[10px]">Slideshow Layout</span>
                </div>
              </button>
            </div>
          </div>

          {/* Engine Selection */}
          <div>
            <Label className="text-sm font-semibold mb-2 block">Editor Engine *</Label>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {/* Lexical Engine */}
              <button
                type="button"
                onClick={() => setEngine(ENGINE_TYPES.LEXICAL)}
                className={`relative p-3 rounded-lg border-2 transition-all text-left ${
                  engine === ENGINE_TYPES.LEXICAL
                    ? "border-indigo-500 bg-indigo-50 dark:bg-indigo-950"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                }`}
              >
                {engine === ENGINE_TYPES.LEXICAL && (
                  <div className="absolute top-2 right-2">
                    <div className="w-5 h-5 bg-indigo-500 rounded-full flex items-center justify-center">
                      <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    </div>
                  </div>
                )}
                <div className="mb-2 flex justify-center">
                  <div className="w-full h-16 bg-linear-to-br from-indigo-100 to-indigo-200 dark:from-indigo-900 dark:to-indigo-800 rounded border border-indigo-300 dark:border-indigo-600 flex items-center justify-center">
                    <div className="space-y-1 w-3/4">
                      <div className="h-1.5 w-full bg-white dark:bg-gray-700 rounded"></div>
                      <div className="h-1.5 w-4/5 bg-white dark:bg-gray-700 rounded"></div>
                      <div className="h-1.5 w-3/5 bg-white dark:bg-gray-700 rounded"></div>
                    </div>
                  </div>
                </div>
                <h3 className="font-semibold text-base mb-1">Rich Document</h3>
                <p className="text-xs text-gray-600 dark:text-gray-400 mb-1.5">
                  Full rich-text editor with paragraphs, headings, lists, and embedded blocks.
                </p>
                <div className="flex items-center gap-2 text-xs text-gray-500">
                  <span className="px-2 py-0.5 bg-gray-100 dark:bg-gray-800 rounded text-[10px]">Lexical</span>
                </div>
              </button>

              {/* Block Array Engine */}
              <button
                type="button"
                onClick={() => setEngine(ENGINE_TYPES.BLOCKS)}
                className={`relative p-3 rounded-lg border-2 transition-all text-left ${
                  engine === ENGINE_TYPES.BLOCKS
                    ? "border-amber-500 bg-amber-50 dark:bg-amber-950"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                }`}
              >
                {engine === ENGINE_TYPES.BLOCKS && (
                  <div className="absolute top-2 right-2">
                    <div className="w-5 h-5 bg-amber-500 rounded-full flex items-center justify-center">
                      <svg className="w-3 h-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    </div>
                  </div>
                )}
                <div className="mb-2 flex justify-center">
                  <div className="w-full h-16 bg-linear-to-br from-amber-100 to-amber-200 dark:from-amber-900 dark:to-amber-800 rounded border border-amber-300 dark:border-amber-600 flex flex-col items-center justify-center gap-1 p-1.5">
                    <div className="w-full h-4 bg-white dark:bg-gray-700 rounded flex items-center px-1.5">
                      <div className="w-2 h-2 bg-amber-400 dark:bg-amber-500 rounded-sm mr-1"></div>
                      <div className="h-1 w-8 bg-amber-200 dark:bg-amber-700 rounded"></div>
                    </div>
                    <div className="w-full h-4 bg-white dark:bg-gray-700 rounded flex items-center px-1.5">
                      <div className="w-2 h-2 bg-amber-400 dark:bg-amber-500 rounded-sm mr-1"></div>
                      <div className="h-1 w-6 bg-amber-200 dark:bg-amber-700 rounded"></div>
                    </div>
                  </div>
                </div>
                <h3 className="font-semibold text-base mb-1">Block Stack</h3>
                <p className="text-xs text-gray-600 dark:text-gray-400 mb-1.5">
                  Stack of decorator blocks (quiz, code, image, etc.) without rich text.
                </p>
                <div className="flex items-center gap-2 text-xs text-gray-500">
                  <span className="px-2 py-0.5 bg-gray-100 dark:bg-gray-800 rounded text-[10px]">Block Array</span>
                </div>
              </button>
            </div>
          </div>

          {/* Project Mode and Storage - Side by side */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {/* Project Mode */}
            <div>
              <Label htmlFor="project-mode" className="text-sm font-semibold">Content Mode *</Label>
              <select
                id="project-mode"
                value={projectMode}
                onChange={(e) => setProjectMode(e.target.value as ProjectMode)}
                className="w-full px-3 py-2 mt-1.5 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:focus:ring-blue-400"
              >
                <option value="free-page">Free Page - No restrictions</option>
                <option value="code-page">Code Page - Code studio focused</option>
                <option value="quiz-page">Quiz Page - Quiz focused</option>
              </select>
            </div>

            {/* Storage Option */}
            <div>
              <Label htmlFor="storage-option" className="text-sm font-semibold">Storage Location *</Label>
              <div className="mt-1.5">
                <StorageOptionSelector 
                  selectedOption={storageOption} 
                  onSelectionChange={setStorageOption} 
                />
              </div>
            </div>
          </div>

          {/* Tags Section */}
          <div className="space-y-2">
            <Label className="text-sm font-semibold">Tags (required) *</Label>

            {/* Tag Input with Dropdown */}
            <div className="relative">
              <div className="flex gap-2">
                <div className="relative flex-1">
                  <Input
                    placeholder="Search existing tags or type to create new..."
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
                <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-md shadow-lg max-h-48 overflow-y-auto">
                  {(() => {
                    const filteredTags = tagInput.trim()
                      ? availableTags.filter(
                          (tag) =>
                            tag.name.toLowerCase().includes(tagInput.toLowerCase()) && !projectTags.includes(tag.name),
                        )
                      : availableTags.filter((tag) => !projectTags.includes(tag.name))

                    return (
                      <>
                        {/* Show filtered existing tags or all if no search */}
                        {filteredTags.length > 0 && (
                          <>
                            {filteredTags.slice(0, 10).map((tag) => (
                              <button
                                key={tag.name}
                                type="button"
                                onClick={() => {
                                  setProjectTags((prev) => [...prev, tag.name])
                                  setTagInput("")
                                  setShowTagDropdown(false)
                                }}
                                className="w-full px-3 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between"
                              >
                                <span className="text-sm">{tag.name}</span>
                              </button>
                            ))}
                            {/* Show separator if there are existing tags and we can create new */}
                            {tagInput.trim() &&
                              !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) &&
                              !projectTags.includes(tagInput.trim()) && (
                                <div className="border-t dark:border-gray-700 my-1"></div>
                              )}
                          </>
                        )}

                        {/* Create new tag option */}
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
                              className="w-full px-3 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-700"
                            >
                              <div className="flex items-center gap-2">
                                <svg
                                  className="w-4 h-4 text-green-600 dark:text-green-400"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                >
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M12 4v16m8-8H4"
                                  />
                                </svg>
                                <span className="text-sm">
                                  Create "<strong>{tagInput.trim()}</strong>"
                                </span>
                              </div>
                            </button>
                          )}

                        {/* No results message */}
                        {filteredTags.length === 0 && !tagInput.trim() && (
                          <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">
                            {availableTags.length === 0
                              ? "No tags available yet. Start typing to create your first tag."
                              : "Start typing to search existing tags or create new ones..."}
                          </div>
                        )}

                        {/* No search results */}
                        {filteredTags.length === 0 &&
                          tagInput.trim() &&
                          !availableTags.some((tag) => tag.name.toLowerCase() === tagInput.toLowerCase()) &&
                          !projectTags.includes(tagInput.trim()) && (
                            <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">
                              No existing tags found. Press Enter or click above to create "{tagInput.trim()}"
                            </div>
                          )}

                        {/* Tag already selected message */}
                        {tagInput.trim() && projectTags.includes(tagInput.trim()) && (
                          <div className="px-3 py-2 text-sm text-amber-600 dark:text-amber-400">
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
              <div className="space-y-2">
                <Label className="text-sm text-gray-600 dark:text-gray-400">Selected tags:</Label>
                <div className="flex flex-wrap gap-2">
                  {projectTags.map((tag, index) => (
                    <span
                      key={index}
                      className="inline-flex items-center gap-1 px-3 py-1 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-sm rounded-full"
                    >
                      {tag}
                      <button
                        type="button"
                        onClick={() => setProjectTags((prev) => prev.filter((_, i) => i !== index))}
                        className="hover:text-blue-600 dark:hover:text-blue-300 ml-1"
                      >
                        <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                      </button>
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Quick add popular tags */}
            {availableTags.length > 0 && projectTags.length === 0 && (
              <div className="space-y-2">
                <Label className="text-sm text-gray-600 dark:text-gray-400">Popular tags:</Label>
                <div className="flex flex-wrap gap-2">
                  {availableTags.slice(0, 6).map((tag) => (
                    <button
                      key={tag.name}
                      type="button"
                      onClick={() => setProjectTags((prev) => [...prev, tag.name])}
                      className="px-2 py-1 text-xs rounded-full border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                    >
                      {tag.name}
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>

          <div className="p-2.5 bg-blue-50 dark:bg-blue-900 rounded-lg">
            <div className="flex items-center gap-2 mb-1">
              <svg
                className="w-4 h-4 text-blue-600 dark:text-blue-400"
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
              <span className="text-xs font-medium text-blue-800 dark:text-blue-200">New Project</span>
            </div>
            <p className="text-xs text-blue-700 dark:text-blue-300">
              This will create a new empty project and clear the current editor content. At least one tag is required.
            </p>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={handleCancel}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={projectTags.length === 0 || !storageOption}>
              Create Project
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
