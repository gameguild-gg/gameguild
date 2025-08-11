"use client"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { FolderOpen, Eye } from "lucide-react"
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
  load: (id: string) => Promise<ProjectData | null>
  searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any") => Promise<ProjectData[]>
}

interface OpenProjectDialogPreviewProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  isDbInitialized: boolean
  storageAdapter: StorageAdapter
  availableTags: Array<{ name: string; usageCount: number }>
  onProjectLoad: (projectData: ProjectData) => void
  formatStorageSize: (sizeInKB: number) => string
}

export function OpenProjectDialogPreview({
  open,
  onOpenChange,
  isDbInitialized,
  storageAdapter,
  availableTags,
  onProjectLoad,
  formatStorageSize,
}: OpenProjectDialogPreviewProps) {
  const [searchTerm, setSearchTerm] = useState("")
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [currentPage, setCurrentPage] = useState(1)
  const [itemsPerPage, setItemsPerPage] = useState(10)
  const [filteredProjects, setFilteredProjects] = useState<ProjectData[]>([])
  const [totalProjects, setTotalProjects] = useState(0)
  const [tagSearchInput, setTagSearchInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)
  const [tagFilterMode, setTagFilterMode] = useState<"all" | "any">("any")

  // Format file size
  const formatSize = (sizeInKB: number): string => {
    if (sizeInKB < 1024) {
      return `${sizeInKB.toFixed(1)}KB`
    } else {
      return `${(sizeInKB / 1024).toFixed(1)}MB`
    }
  }

  // Filter projects based on search and tags
  useEffect(() => {
    const filterProjects = async () => {
      if (!isDbInitialized) return

      try {
        let projects: ProjectData[]

        if (searchTerm || selectedTags.length > 0) {
          projects = await storageAdapter.searchProjects(searchTerm, selectedTags, tagFilterMode)
        } else {
          projects = await storageAdapter.list()
        }

        setTotalProjects(projects.length)
        setFilteredProjects(projects)
        setCurrentPage(1) // Reset to first page when filtering
      } catch (error) {
        console.error("Failed to filter projects:", error)
      }
    }

    filterProjects()
  }, [searchTerm, selectedTags, isDbInitialized, tagFilterMode, storageAdapter])

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

  const handleOpen = async (projectId: string) => {
    try {
      const projectData = await storageAdapter.load(projectId)
      if (projectData) {
        onProjectLoad(projectData)
        onOpenChange(false)
        toast.success("Project loaded for preview", {
          description: `"${projectData.name}" is now being previewed`,
          duration: 2500,
          icon: "👁️",
        })
      } else {
        toast.error("Project not found", {
          description: "Could not locate the project file",
          duration: 3000,
          icon: "🔍",
        })
      }
    } catch (error) {
      console.error("Open error:", error)
      toast.error("Error opening project", {
        description: "Could not open the project. Please try again.",
        duration: 4000,
        icon: "❌",
      })
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(open) => {
        onOpenChange(open)
      }}
    >
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" className="gap-2 bg-transparent" disabled={!isDbInitialized}>
          <FolderOpen className="w-4 h-4" />
          Open Project
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-2xl min-w-xl max-h-[80vh]" onInteractOutside={(e) => e.preventDefault()}>
        <DialogHeader>
          <DialogTitle>Open Project for Preview</DialogTitle>
          <p className="text-sm text-muted-foreground">Select a project to preview its content</p>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-3 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
            {/* Search and Filter Section */}
            <div className="flex items-center justify-between">
              <Label className="text-sm font-medium">Filter by Projects:</Label>
              <div className="flex items-center gap-2">
                <Label className="text-xs text-gray-700 dark:text-gray-400">Items per page:</Label>
                <select
                  value={itemsPerPage}
                  onChange={(e) => setItemsPerPage(Number(e.target.value))}
                  className="px-2 py-1 border rounded text-sm bg-background"
                >
                  <option value={3}>3</option>
                  <option value={6}>6</option>
                  <option value={12}>12</option>
                  <option value={24}>24</option>
                </select>
              </div>
            </div>
            <div className="flex-1">
              <Input
                placeholder="Search projects by name..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full"
              />
            </div>

            {/* Tags Filter */}
            <div className="space-y-2 min-h-[15vh] border-b">
              <div className="flex items-center justify-between">
                <Label className="text-sm font-medium">Filter by tags:</Label>
                <div className="flex items-center gap-2">
                  <Label className="text-xs text-gray-700 dark:text-gray-400">Match tags:</Label>
                  <select
                    value={tagFilterMode}
                    onChange={(e) => setTagFilterMode(e.target.value as "all" | "any")}
                    className="px-2 py-1 border rounded text-xs bg-background"
                  >
                    <option value="any">Any tags</option>
                    <option value="all">All tags</option>
                  </select>
                </div>
              </div>

              <div className="relative">
                <div className="flex gap-2">
                  <div className="relative flex-1">
                    <Input
                      placeholder="Search tags or leave empty to see all..."
                      value={tagSearchInput}
                      onChange={(e) => {
                        setTagSearchInput(e.target.value)
                        setShowTagDropdown(true)
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

                {showTagDropdown && (
                  <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-md shadow-lg max-h-48 overflow-y-auto">
                    {availableTags.length === 0 ? (
                      <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">No tags available</div>
                    ) : (
                      (() => {
                        const filteredTags = tagSearchInput.trim()
                          ? availableTags.filter((tag) => tag.name.toLowerCase().includes(tagSearchInput.toLowerCase()))
                          : availableTags

                        return filteredTags.length === 0 ? (
                          <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">
                            No tags found matching "{tagSearchInput}"
                          </div>
                        ) : (
                          filteredTags.map((tag) => (
                            <button
                              key={tag.name}
                              type="button"
                              onClick={() => {
                                setSelectedTags((prev) =>
                                  prev.includes(tag.name) ? prev.filter((t) => t !== tag.name) : [...prev, tag.name],
                                )
                              }}
                              className="w-full px-3 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between transition-colors"
                            >
                              <div className="flex items-center gap-2">
                                <div
                                  className={`w-4 h-4 border rounded flex items-center justify-center ${
                                    selectedTags.includes(tag.name)
                                      ? "bg-blue-500 border-blue-500"
                                      : "border-gray-300 dark:border-gray-600"
                                  }`}
                                >
                                  {selectedTags.includes(tag.name) && (
                                    <svg
                                      className="w-3 h-3 text-white"
                                      fill="none"
                                      stroke="currentColor"
                                      viewBox="0 0 24 24"
                                    >
                                      <path
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                        strokeWidth={2}
                                        d="M5 13l4 4L19 7"
                                      />
                                    </svg>
                                  )}
                                </div>
                                <span className="text-sm">{tag.name}</span>
                              </div>
                              <span className="text-xs text-gray-500 dark:text-gray-400">({tag.usageCount})</span>
                            </button>
                          ))
                        )
                      })()
                    )}
                  </div>
                )}
              </div>

              {/* Selected tags display */}
              {selectedTags.length > 0 && (
                <div className="space-y-2">
                  <div className="flex flex-wrap gap-2">
                    {selectedTags.map((tagName) => (
                      <span
                        key={tagName}
                        className="inline-flex items-center gap-1 px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 text-xs rounded-full"
                      >
                        {tagName}
                        <button
                          type="button"
                          onClick={() => setSelectedTags((prev) => prev.filter((t) => t !== tagName))}
                          className="hover:text-blue-600 dark:hover:text-blue-300"
                        >
                          <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M6 18L18 6M6 6l12 12"
                            />
                          </svg>
                        </button>
                      </span>
                    ))}
                  </div>
                  <div className="flex items-center justify-between">
                    <button
                      type="button"
                      onClick={() => {
                        setSelectedTags([])
                        setTagSearchInput("")
                      }}
                      className="text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
                    >
                      Clear all filters
                    </button>
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                      Showing projects with {tagFilterMode === "all" ? "all" : "any"} of these tags
                    </span>
                  </div>
                </div>
              )}
            </div>

            {/* Projects List */}
            <div className="max-h-[30vh] overflow-y-auto">
              {filteredProjects.length === 0 ? (
                <div className="text-center py-12">
                  <FolderOpen className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
                  <p className="text-sm text-gray-500 dark:text-gray-400">
                    {searchTerm || selectedTags.length > 0
                      ? "No projects found matching your criteria"
                      : "No saved projects found"}
                  </p>
                  <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
                    {searchTerm || selectedTags.length > 0
                      ? "Try adjusting your search or filters"
                      : "Create projects in the editor first"}
                  </p>
                </div>
              ) : (
                <>
                  <div className="space-y-2">
                    {filteredProjects
                      .slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage)
                      .map((project) => {
                        return (
                          <div
                            key={project.id}
                            className="flex items-center justify-between p-3 border dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
                          >
                            <div className="flex flex-col flex-1 min-w-0">
                              <div className="flex items-center gap-2 mb-1">
                                <span className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                                  {project.name}
                                </span>
                                {project.tags && project.tags.length > 0 && (
                                  <div className="flex gap-1">
                                    {project.tags.slice(0, 3).map((tag) => (
                                      <span
                                        key={tag}
                                        className="inline-flex items-center px-1.5 py-0.5 rounded text-xs bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200"
                                      >
                                        {tag}
                                      </span>
                                    ))}
                                    {project.tags.length > 3 && (
                                      <span className="text-xs text-gray-500 dark:text-gray-400">
                                        +{project.tags.length - 3}
                                      </span>
                                    )}
                                  </div>
                                )}
                              </div>
                              <div className="flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
                                <span>{formatSize(project.size)}</span>
                                <span>•</span>
                                <span>Updated {new Date(project.updatedAt).toLocaleDateString()}</span>
                                <span>•</span>
                                <span className="text-gray-400 dark:text-gray-500 font-mono text-xs">
                                  ID: {project.id.slice(0, 8)}...
                                </span>
                              </div>
                            </div>
                            <div className="flex gap-1 ml-3">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleOpen(project.id)}
                                className="text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300 gap-2"
                              >
                                <Eye className="w-4 h-4" />
                                Preview
                              </Button>
                            </div>
                          </div>
                        )
                      })}
                  </div>
                </>
              )}
            </div>

            {/* Pagination */}
            {Math.ceil(totalProjects / itemsPerPage) > 1 && (
              <div className="flex items-center justify-between mt-4 pt-4 border-t dark:border-gray-700">
                <div className="text-sm text-gray-500 dark:text-gray-400">
                  Showing {(currentPage - 1) * itemsPerPage + 1} to{" "}
                  {Math.min(currentPage * itemsPerPage, totalProjects)} of {totalProjects} projects
                </div>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setCurrentPage((prev) => Math.max(1, prev - 1))}
                    disabled={currentPage === 1}
                    className="bg-transparent"
                  >
                    Previous
                  </Button>
                  <div className="flex items-center gap-1">
                    {Array.from({ length: Math.ceil(totalProjects / itemsPerPage) }, (_, i) => i + 1)
                      .filter((page) => {
                        const totalPages = Math.ceil(totalProjects / itemsPerPage)
                        if (totalPages <= 7) return true
                        if (page === 1 || page === totalPages) return true
                        if (page >= currentPage - 1 && page <= currentPage + 1) return true
                        return false
                      })
                      .map((page, index, array) => (
                        <div key={page} className="flex items-center">
                          {index > 0 && array[index - 1] !== page - 1 && (
                            <span className="px-2 text-gray-400">...</span>
                          )}
                          <Button
                            variant={currentPage === page ? "default" : "ghost"}
                            size="sm"
                            onClick={() => setCurrentPage(page)}
                            className={currentPage === page ? "" : "bg-transparent"}
                          >
                            {page}
                          </Button>
                        </div>
                      ))}
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() =>
                      setCurrentPage((prev) => Math.min(Math.ceil(totalProjects / itemsPerPage), prev + 1))
                    }
                    disabled={currentPage === Math.ceil(totalProjects / itemsPerPage)}
                    className="bg-transparent"
                  >
                    Next
                  </Button>
                </div>
              </div>
            )}

            <div className="flex justify-end items-center pt-4 border-t dark:border-gray-700">
              <Button variant="outline" onClick={() => onOpenChange(false)} className="bg-transparent">
                Close
              </Button>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
