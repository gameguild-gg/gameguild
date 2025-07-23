"use client"

import { Button } from "@/components/editor/ui/button"
import { Input } from "@/components/editor/ui/input"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/editor/ui/dialog"
import { Label } from "@/components/editor/ui/label"
import { useState, useEffect } from "react"
import { toast } from "sonner"

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
  save: (id: string, name: string, data: string, tags: string[]) => Promise<void>
}

interface CreateProjectDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
  availableTags: Array<{ name: string; usageCount: number }>
  onProjectCreate: (projectData: { id: string; name: string; tags: string[] }) => void
  onProjectsListUpdate: () => void
  onAvailableTagsUpdate: () => void
  isStorageAtLimit: () => boolean
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
  isStorageAtLimit,
  generateProjectId,
}: CreateProjectDialogProps) {
  const [newCreateProjectName, setNewCreateProjectName] = useState("")
  const [projectTags, setProjectTags] = useState<string[]>([])
  const [tagInput, setTagInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)

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

    // Check storage limit before creating new project
    if (isStorageAtLimit()) {
      toast.error("Armazenamento lotado", {
        description: "Limite de armazenamento atingido. Exclua projetos para liberar espaço",
        duration: 5000,
        icon: "🚫",
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
    const emptyState =
      '{"root":{"children":[{"children":[],"direction":null,"format":"","indent":0,"type":"paragraph","version":1}],"direction":null,"format":"","indent":0,"type":"root","version":1}}'

    try {
      const newProjectId = generateProjectId()
      await storageAdapter.save(newProjectId, newCreateProjectName, emptyState, projectTags)

      // Call the callback to update parent state
      onProjectCreate({
        id: newProjectId,
        name: newCreateProjectName,
        tags: projectTags,
      })

      // Reset form state
      setNewCreateProjectName("")
      setProjectTags([])
      setTagInput("")
      setShowTagDropdown(false)
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
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Create New Project</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div>
            <Label htmlFor="create-project-name">Project Name</Label>
            <Input
              id="create-project-name"
              value={newCreateProjectName}
              onChange={(e) => setNewCreateProjectName(e.target.value)}
              placeholder="Enter project name..."
              onKeyDown={(e) => e.key === "Enter" && !e.shiftKey && handleCreate()}
              className="mt-1"
            />
          </div>

          {/* Tags Section */}
          <div className="space-y-3">
            <Label>Tags (required) *</Label>

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
                                <span className="text-xs text-gray-500 dark:text-gray-400">
                                  ({tag.usageCount} uses)
                                </span>
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
                      {tag.name} ({tag.usageCount})
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>

          <div className="p-3 bg-blue-50 dark:bg-blue-900 rounded-lg">
            <div className="flex items-center gap-2 mb-2">
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
              <span className="text-sm font-medium text-blue-800 dark:text-blue-200">New Project</span>
            </div>
            <p className="text-xs text-blue-700 dark:text-blue-300">
              This will create a new empty project and clear the current editor content. At least one tag is required.
            </p>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={handleCancel}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={projectTags.length === 0}>
              Create Project
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
