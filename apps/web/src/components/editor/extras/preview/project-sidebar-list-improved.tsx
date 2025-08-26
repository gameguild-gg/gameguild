"use client"

import { useState, useEffect } from "react"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { ScrollArea } from "@/components/ui/scroll-area"
import { FileText, Tag, Calendar, ChevronDown, Search, Filter, Pin, PinOff } from "lucide-react"
import {
  formatFileSize,
  filterProjects,
  filterTagsForDropdown,
  toggleTag,
  clearAllFilters,
  loadAllProjects,
  createSearchFilterState,
  hasActiveFilters,
  getFilterSummary,
  type ProjectData,
  type SearchFilters,
  type StorageAdapter
} from "../search-filter"

interface ProjectSidebarListProps {
  storageAdapter: StorageAdapter
  availableTags: Array<{ name: string; usageCount: number }>
  currentProject: ProjectData | null
  onProjectSelect: (project: ProjectData) => void
  isDbInitialized: boolean
  isSticky?: boolean
}

export function ProjectSidebarList({
  storageAdapter,
  availableTags,
  currentProject,
  onProjectSelect,
  isDbInitialized,
  isSticky = false,
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
  const [isPinned, setIsPinned] = useState(false)

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
      handleLoadProjects()
    }
  }, [isDbInitialized])

  // Filter projects when search term or tags change
  useEffect(() => {
    if (searchTerm || selectedTags.length > 0) {
      handleSearchProjects()
    } else {
      setFilteredProjects(projects)
    }
  }, [searchTerm, selectedTags, tagFilterMode, projects])

  const handleLoadProjects = async () => {
    try {
      setLoading(true)
      const allProjects = await loadAllProjects(storageAdapter)
      setProjects(allProjects)
      setFilteredProjects(allProjects)
    } catch (error) {
      console.error("Failed to load projects:", error)
    } finally {
      setLoading(false)
    }
  }

  const handleSearchProjects = async () => {
    try {
      setLoading(true)
      const filters: SearchFilters = {
        searchTerm,
        selectedTags,
        tagFilterMode
      }
      const results = await filterProjects(storageAdapter, projects, filters)
      setFilteredProjects(results)
    } catch (error) {
      console.error("Failed to search projects:", error)
    } finally {
      setLoading(false)
    }
  }

  const handleTagToggle = (tagName: string) => {
    const newSelectedTags = toggleTag(selectedTags, tagName)
    setSelectedTags(newSelectedTags)
  }

  const handleClearFilters = () => {
    const clearedFilters = clearAllFilters()
    setSearchTerm(clearedFilters.searchTerm)
    setSelectedTags(clearedFilters.selectedTags)
    setTagSearchInput("")
  }

  const filteredTagsForDropdown = filterTagsForDropdown(availableTags, tagSearchInput)

  return (
    <div className={`${isPinned ? 'sticky top-24 max-h-[calc(100vh-7rem)]' : ''}`}>
      <div className={`w-96 bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-800 shadow-sm flex flex-col ${isPinned ? 'max-h-[calc(100vh-7rem)]' : 'h-full'} mr-8`}>
        {/* Header */}
        <div className="p-6 border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Documents</h3>
            <div className="flex items-center gap-1">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setIsPinned(!isPinned)}
                className="h-8 w-8 p-0 hover:bg-gray-100 dark:hover:bg-gray-800"
                title={isPinned ? "Unpin sidebar" : "Pin sidebar"}
              >
                {isPinned ? <PinOff className="h-4 w-4" /> : <Pin className="h-4 w-4" />}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
                className="h-8 w-8 p-0 hover:bg-gray-100 dark:hover:bg-gray-800"
              >
                <Filter className="h-4 w-4" />
              </Button>
            </div>
          </div>

        {/* Search */}
        <div className="relative mb-4">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            placeholder="Search documents..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10 h-9 text-sm bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-700"
          />
        </div>

        {/* Collapsible Filters */}
        {showFilters && (
          <div className="space-y-4 pt-4 border-t border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between">
              <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">Filter Mode:</Label>
              <select
                value={tagFilterMode}
                onChange={(e) => setTagFilterMode(e.target.value as "all" | "any")}
                className="px-3 py-1.5 border rounded-md text-sm bg-background border-gray-200 dark:border-gray-700"
              >
                <option value="any">Any tags</option>
                <option value="all">All tags</option>
              </select>
            </div>

            {/* Tag Filter */}
            <div className="relative tag-dropdown-container">
              <Label className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2 block">Tags:</Label>
              <Input
                placeholder="Search tags..."
                value={tagSearchInput}
                onChange={(e) => {
                  setTagSearchInput(e.target.value)
                  setShowTagDropdown(true)
                }}
                onFocus={() => setShowTagDropdown(true)}
                className="h-9 text-sm pr-10 bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-700"
              />
              <button
                type="button"
                onClick={() => setShowTagDropdown(!showTagDropdown)}
                className="absolute right-3 top-9 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                <ChevronDown className={`w-4 h-4 transition-transform ${showTagDropdown ? "rotate-180" : ""}`} />
              </button>

              {showTagDropdown && (
                <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-lg shadow-lg max-h-40 overflow-y-auto">
                  {filteredTagsForDropdown.length === 0 ? (
                    <div className="px-3 py-2 text-sm text-gray-500">No tags found</div>
                  ) : (
                    filteredTagsForDropdown.map((tag) => (
                      <button
                        key={tag.name}
                        type="button"
                        onClick={() => {
                          handleTagToggle(tag.name)
                          setShowTagDropdown(false)
                        }}
                        className="w-full px-3 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between text-sm transition-colors"
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
                              <svg className="w-2.5 h-2.5 text-white" fill="currentColor" viewBox="0 0 20 20">
                                <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                              </svg>
                            )}
                          </div>
                          <span>{tag.name}</span>
                        </div>
                        <span className="text-gray-400 text-xs">({tag.usageCount})</span>
                      </button>
                    ))
                  )}
                </div>
              )}
            </div>

            {/* Selected Tags */}
            {selectedTags.length > 0 && (
              <div className="space-y-3">
                <div className="flex flex-wrap gap-1.5">
                  {selectedTags.map((tagName) => (
                    <Badge
                      key={tagName}
                      variant="secondary"
                      className="text-xs px-2.5 py-1 cursor-pointer hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
                      onClick={() => handleTagToggle(tagName)}
                    >
                      {tagName} ×
                    </Badge>
                  ))}
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleClearFilters}
                  className="h-7 text-xs text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300 p-0"
                >
                  Clear all filters
                </Button>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Project List */}
      <ScrollArea className="flex-1 px-6 py-4 overflow-auto">
        <div className="space-y-3">
          {loading ? (
            <div className="text-center py-8 text-sm text-gray-500">
              <div className="animate-pulse">Loading documents...</div>
            </div>
          ) : filteredProjects.length === 0 ? (
            <div className="text-center py-12">
              <FileText className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-1">
                {searchTerm || selectedTags.length > 0 ? "No documents found" : "No documents available"}
              </p>
              {searchTerm || selectedTags.length > 0 ? (
                <p className="text-xs text-gray-400 dark:text-gray-500">Try adjusting your search or filters</p>
              ) : null}
            </div>
          ) : (
            filteredProjects.map((project) => (
              <div
                key={project.id}
                onClick={() => onProjectSelect(project)}
                className={`p-4 rounded-lg border cursor-pointer transition-all duration-200 hover:shadow-sm ${
                  currentProject?.id === project.id
                    ? "bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-700/50 shadow-sm"
                    : "bg-gray-50 dark:bg-gray-800/50 border-gray-200 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800"
                }`}
              >
                <div className="flex items-start gap-3">
                  <div className={`p-2 rounded-md ${
                    currentProject?.id === project.id
                      ? "bg-blue-100 dark:bg-blue-900/40"
                      : "bg-gray-100 dark:bg-gray-700"
                  }`}>
                    <FileText className="w-4 h-4 text-gray-600 dark:text-gray-400" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate mb-1">
                      {project.name}
                    </h4>
                    
                    {project.tags && project.tags.length > 0 && (
                      <div className="flex flex-wrap gap-1 mb-3">
                        {project.tags.slice(0, 3).map((tag) => (
                          <Badge key={tag} variant="outline" className="text-xs px-2 py-0.5 h-5">
                            {tag}
                          </Badge>
                        ))}
                        {project.tags.length > 3 && (
                          <Badge variant="outline" className="text-xs px-2 py-0.5 h-5">
                            +{project.tags.length - 3}
                          </Badge>
                        )}
                      </div>
                    )}

                    <div className="flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
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
      <div className="p-4 border-t border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50 rounded-b-xl flex-shrink-0">
        <div className="text-xs text-gray-500 dark:text-gray-400 text-center">
          <div className="font-medium">
            {(() => {
              const summary = getFilterSummary(filteredProjects.length, projects.length, selectedTags.length)
              return summary.main
            })()}
          </div>
          {(() => {
            const summary = getFilterSummary(filteredProjects.length, projects.length, selectedTags.length)
            return summary.subtitle && (
              <div className="mt-1 text-gray-400 dark:text-gray-500">
                {summary.subtitle}
              </div>
            )
          })()}
        </div>
      </div>
    </div>
    </div>
  )
}
