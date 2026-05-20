"use client"

import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { FileText, Search, X, HardDrive, Cloud, Database } from "lucide-react"
import { ScrollArea } from "@/components/ui/scroll-area"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"

interface SelectProjectDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onProjectSelect: (project: ProjectData) => void
  currentProjectId?: string
  storageAdapter: {
    list: () => Promise<ProjectData[]>
    searchProjects: (
      searchTerm: string,
      tags: string[],
      filterMode: "all" | "any",
      storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive"
    ) => Promise<ProjectData[]>
  }
}

export function SelectProjectDialog({
  open,
  onOpenChange,
  onProjectSelect,
  currentProjectId,
  storageAdapter,
}: SelectProjectDialogProps) {
  const [projects, setProjects] = useState<ProjectData[]>([])
  const [searchTerm, setSearchTerm] = useState("")
  const [loading, setLoading] = useState(false)
  const [selectedTags, setSelectedTags] = useState<string[]>([])

  const loadProjects = async () => {
    setLoading(true)
    try {
      let allProjects = await storageAdapter.list()
      
      // Exclude current project
      allProjects = allProjects.filter(
        (p) => p.id !== currentProjectId
      )

      // Apply search filter
      if (searchTerm) {
        allProjects = allProjects.filter((p) =>
          p.name.toLowerCase().includes(searchTerm.toLowerCase())
        )
      }

      // Sort by updated date (newest first)
      allProjects.sort((a, b) => new Date(b.metadata.updatedAt).getTime() - new Date(a.metadata.updatedAt).getTime())

      setProjects(allProjects)
    } catch (error) {
      console.error("Failed to load projects:", error)
    } finally {
      setLoading(false)
    }
  }

  // Load projects when dialog opens
  useEffect(() => {
    if (open) {
      loadProjects()
    }
  }, [open])

  // Format size
  const formatSize = (sizeInKB: number): string => {
    if (sizeInKB < 1024) {
      return `${sizeInKB.toFixed(1)}KB`
    } else {
      return `${(sizeInKB / 1024).toFixed(1)}MB`
    }
  }

  // Get storage icon
  const getStorageIcon = (storageType?: "local" | "gameguild-cloud" | "google-drive") => {
    switch (storageType) {
      case "gameguild-cloud":
        return <Database className="h-3 w-3" />
      case "google-drive":
        return <Cloud className="h-3 w-3" />
      default:
        return <HardDrive className="h-3 w-3" />
    }
  }

  // Get project type label
  const getTypeLabel = (): string => {
    return "Single Project"
  }

  const handleSelect = (project: ProjectData) => {
    onProjectSelect(project)
    onOpenChange(false)
  }

  const handleSearch = (value: string) => {
    setSearchTerm(value)
    // Debounce search
    setTimeout(() => {
      loadProjects()
    }, 300)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl max-h-[80vh]">
        <DialogHeader>
          <DialogTitle>Import Project</DialogTitle>
          <DialogDescription>
            Select a project to import. You can edit imported projects, which will create a local copy.
          </DialogDescription>
        </DialogHeader>

        {/* Search bar */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            placeholder="Search projects by name..."
            value={searchTerm}
            onChange={(e) => handleSearch(e.target.value)}
            className="pl-10 pr-10"
          />
          {searchTerm && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setSearchTerm("")
                loadProjects()
              }}
              className="absolute right-1 top-1/2 -translate-y-1/2 h-8 w-8 p-0"
            >
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>

        {/* Filter info */}
        <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
          <span>Showing: Single Projects</span>
        </div>

        {/* Projects list */}
        <ScrollArea className="h-[400px] pr-4">
          {loading ? (
            <div className="flex items-center justify-center h-32">
              <div className="text-sm text-gray-500">Loading projects...</div>
            </div>
          ) : projects.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-32 text-center">
              <FileText className="h-12 w-12 text-gray-400 mb-2" />
              <div className="text-sm text-gray-500">No projects found</div>
              <div className="text-xs text-gray-400 mt-1">
                {searchTerm
                  ? "Try adjusting your search"
                  : "Create a project to import"}
              </div>
            </div>
          ) : (
            <div className="space-y-2">
              {projects.map((project) => (
                <div
                  key={project.id}
                  onClick={() => handleSelect(project)}
                  className="p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:border-blue-500 dark:hover:border-blue-500 hover:bg-blue-50/50 dark:hover:bg-blue-950/20 cursor-pointer transition-all group"
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <FileText className="h-4 w-4 text-blue-600 dark:text-blue-400 shrink-0" />
                        <span className="font-semibold text-gray-900 dark:text-gray-100 truncate">
                          {project.name}
                        </span>
                      </div>

                      <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 flex-wrap">
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-800">
                          {getTypeLabel()}
                        </span>
                        <span className="inline-flex items-center gap-1">
                          {getStorageIcon(project.storageType)}
                          {project.storageType || "local"}
                        </span>
                        <span>•</span>
                        <span>{formatSize(project.metadata.size)}</span>
                        <span>•</span>
                        <span>{new Date(project.metadata.updatedAt).toLocaleDateString()}</span>
                      </div>

                      {project.tags && project.tags.length > 0 && (
                        <div className="flex flex-wrap gap-1 mt-2">
                          {project.tags.slice(0, 3).map((tag) => (
                            <span
                              key={tag}
                              className="inline-flex items-center px-2 py-0.5 text-xs rounded bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300"
                            >
                              {tag}
                            </span>
                          ))}
                          {project.tags.length > 3 && (
                            <span className="text-xs text-gray-500">+{project.tags.length - 3}</span>
                          )}
                        </div>
                      )}
                    </div>

                    <Button
                      variant="ghost"
                      size="sm"
                      className="opacity-0 group-hover:opacity-100 transition-opacity"
                    >
                      Import
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </ScrollArea>

        {/* Footer */}
        <div className="flex justify-between items-center pt-4 border-t">
          <div className="text-xs text-gray-500">
            {projects.length} project{projects.length !== 1 ? "s" : ""} available
          </div>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
