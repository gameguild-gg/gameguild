"use client"

import { useState, useEffect } from "react"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { ScrollArea } from "@/components/ui/scroll-area"
import { FileText, Tag, Calendar, ChevronDown, ChevronRight, Search, Filter } from "lucide-react"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
}

interface ProjectSidebarListProps {
  storageAdapter: {
    list: () => Promise<ProjectData[]>
    searchProjects: (searchTerm: string, tags: string[], filterMode: "all" | "any") => Promise<ProjectData[]>
  }
  availableTags: Array<{ name: string; usageCount: number }>
  currentProject: ProjectData | null
  onProjectSelect: (project: ProjectData) => void
  isDbInitialized: boolean
}

export function ProjectSidebarList({
  storageAdapter,
  availableTags,
  currentProject,
  onProjectSelect,
  isDbInitialized,
}: ProjectSidebarListProps) {
  const [projects, setProjects] = useState<ProjectData[]>([])
  const [filteredProjects, setFilteredProjects] = useState<ProjectData[]>([])
  const [searchTerm, setSearchTerm] = useState("")
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [tagFilterMode, setTagFilterMode] = useState<"all" | "any">("any")
  const [showFilters, setShowFilters] = useState(false)
  const [loading, setLoading] = useState(false)
  const [tagSearchInput, setTagSearchInput] = useState("")
  const [showTagDropdown, setShowTagDropdown] = useState(false)

  // Close tag dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (showTagDropdown) {
        const target = event.target as Element
        if (!target.closest(".tag-dropdown-container")) {
          setShowTagDropdown(false)
        }
      }
    }

    document.addEventListener("mousedown", handleClickOutside)
    return () => document.removeEventListener("mousedown", handleClickOutside)
  }, [showTagDropdown])

  // Load projects on initialization
  useEffect(() => {
    if (isDbInitialized) {
      loadProjects()
    }
  }, [isDbInitialized])

  // Filter projects when search term or tags change
  useEffect(() => {
    if (searchTerm || selectedTags.length > 0) {
      searchProjects()
    } else {
      setFilteredProjects(projects)
    }
  }, [searchTerm, selectedTags, tagFilterMode, projects])

  const loadProjects = async () => {
    try {
      setLoading(true)
      const allProjects = await storageAdapter.list()
      setProjects(allProjects)
      setFilteredProjects(allProjects)
    } catch (error) {
      console.error("Failed to load projects:", error)
    } finally {
      setLoading(false)
    }
  }

  const searchProjects = async () => {
    try {
      setLoading(true)
      const results = await storageAdapter.searchProjects(searchTerm, selectedTags, tagFilterMode)
      setFilteredProjects(results)
    } catch (error) {
      console.error("Failed to search projects:", error)
    } finally {
      setLoading(false)
    }
  }

  const handleTagToggle = (tagName: string) => {
    const newSelectedTags = selectedTags.includes(tagName)
      ? selectedTags.filter((t) => t !== tagName)
      : [...selectedTags, tagName]
    setSelectedTags(newSelectedTags)
  }

  const clearFilters = () => {
    setSearchTerm("")
    setSelectedTags([])
    setTagSearchInput("")
  }

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return "0 B"
    const k = 1024
    const sizes = ["B", "KB", "MB", "GB"]
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i]
  }

  const filteredTagsForDropdown = tagSearchInput.trim()
    ? availableTags.filter((tag) => tag.name.toLowerCase().includes(tagSearchInput.toLowerCase()))
    : availableTags

  return (
    <div className="w-80 bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 flex flex-col h-full">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100">Documents</h3>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowFilters(!showFilters)}
            className="h-8 w-8 p-0"
          >
            <Filter className="h-4 w-4" />
          </Button>
        </div>

        {/* Search */}
        <div className="relative mb-3">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            placeholder="Search documents..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10 h-8 text-sm"
          />
        </div>

        {/* Collapsible Filters */}
        {showFilters && (
          <div className="space-y-3 pt-3 border-t border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between">
              <Label className="text-xs font-medium text-gray-700 dark:text-gray-300">Filter Mode:</Label>
              <select
                value={tagFilterMode}
                onChange={(e) => setTagFilterMode(e.target.value as "all" | "any")}
                className="px-2 py-1 border rounded text-xs bg-background"
              >
                <option value="any">Any tags</option>
                <option value="all">All tags</option>
              </select>
            </div>

            {/* Tag Filter */}
            <div className="relative tag-dropdown-container">
              <Input
                placeholder="Search tags..."
                value={tagSearchInput}
                onChange={(e) => {
                  setTagSearchInput(e.target.value)
                  setShowTagDropdown(true)
                }}
                onFocus={() => setShowTagDropdown(true)}
                className="h-8 text-xs pr-8"
              />
              <button
                type="button"
                onClick={() => setShowTagDropdown(!showTagDropdown)}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
              >
                <ChevronDown className={`w-3 h-3 transition-transform ${showTagDropdown ? "rotate-180" : ""}`} />
              </button>

              {showTagDropdown && (
                <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-md shadow-lg max-h-32 overflow-y-auto">
                  {filteredTagsForDropdown.length === 0 ? (
                    <div className="px-3 py-2 text-xs text-gray-500">No tags found</div>
                  ) : (
                    filteredTagsForDropdown.map((tag) => (
                      <button
                        key={tag.name}
                        type="button"
                        onClick={() => {
                          handleTagToggle(tag.name)
                          setShowTagDropdown(false)
                        }}
                        className="w-full px-3 py-1.5 text-left hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between text-xs"
                      >
                        <div className="flex items-center gap-2">
                          <div
                            className={`w-3 h-3 border rounded flex items-center justify-center ${
                              selectedTags.includes(tag.name)
                                ? "bg-blue-500 border-blue-500"
                                : "border-gray-300"
                            }`}
                          >
                            {selectedTags.includes(tag.name) && (
                              <svg className="w-2 h-2 text-white" fill="currentColor" viewBox="0 0 20 20">
                                <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                              </svg>
                            )}
                          </div>
                          <span>{tag.name}</span>
                        </div>
                        <span className="text-gray-400">({tag.usageCount})</span>
                      </button>
                    ))
                  )}
                </div>
              )}
            </div>

            {/* Selected Tags */}
            {selectedTags.length > 0 && (
              <div className="space-y-2">
                <div className="flex flex-wrap gap-1">
                  {selectedTags.map((tagName) => (
                    <Badge
                      key={tagName}
                      variant="secondary"
                      className="text-xs px-2 py-0.5 cursor-pointer"
                      onClick={() => handleTagToggle(tagName)}
                    >
                      {tagName} ×
                    </Badge>
                  ))}
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={clearFilters}
                  className="h-6 text-xs text-blue-600 hover:text-blue-700 p-0"
                >
                  Clear all filters
                </Button>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Project List */}
      <ScrollArea className="flex-1">
        <div className="p-2 space-y-1">
          {loading ? (
            <div className="text-center py-8 text-sm text-gray-500">Loading documents...</div>
          ) : filteredProjects.length === 0 ? (
            <div className="text-center py-8">
              <FileText className="w-8 h-8 text-gray-300 dark:text-gray-600 mx-auto mb-2" />
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {searchTerm || selectedTags.length > 0 ? "No documents found" : "No documents available"}
              </p>
            </div>
          ) : (
            filteredProjects.map((project) => (
              <div
                key={project.id}
                onClick={() => onProjectSelect(project)}
                className={`p-3 rounded-lg border cursor-pointer transition-colors ${
                  currentProject?.id === project.id
                    ? "bg-blue-50 dark:bg-blue-900/30 border-blue-200 dark:border-blue-700"
                    : "bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-700"
                }`}
              >
                <div className="flex items-start gap-2">
                  <FileText className="w-4 h-4 text-gray-400 dark:text-gray-500 mt-0.5 flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                      {project.name}
                    </h4>
                    
                    {project.tags && project.tags.length > 0 && (
                      <div className="flex flex-wrap gap-1 mt-1">
                        {project.tags.slice(0, 2).map((tag) => (
                          <Badge key={tag} variant="outline" className="text-xs px-1 py-0">
                            {tag}
                          </Badge>
                        ))}
                        {project.tags.length > 2 && (
                          <Badge variant="outline" className="text-xs px-1 py-0">
                            +{project.tags.length - 2}
                          </Badge>
                        )}
                      </div>
                    )}

                    <div className="flex items-center gap-2 mt-2 text-xs text-gray-500 dark:text-gray-400">
                      <div className="flex items-center gap-1">
                        <Calendar className="w-3 h-3" />
                        {new Date(project.updatedAt).toLocaleDateString()}
                      </div>
                      <span>•</span>
                      <span>{formatFileSize(project.size)}</span>
                    </div>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </ScrollArea>

      {/* Footer Stats */}
      <div className="p-3 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
        <div className="text-xs text-gray-500 dark:text-gray-400 text-center">
          {filteredProjects.length} of {projects.length} documents
          {selectedTags.length > 0 && (
            <span className="ml-1">
              • Filtered by {selectedTags.length} tag{selectedTags.length > 1 ? 's' : ''}
            </span>
          )}
        </div>
      </div>
    </div>
  )
}
