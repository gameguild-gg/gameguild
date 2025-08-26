"use client"

import type React from "react"
import { useState } from "react"

import { Button } from "@/components/ui/button"
import { FolderOpen, Trash2, Download } from "lucide-react"
import { DownloadConfirmDialog } from "@/components/editor/extras/dialogs/download-confirm-dialog"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
}

interface ProjectListProps {
  projects: ProjectData[]
  currentPage: number
  itemsPerPage: number
  searchTerm: string
  selectedTags: string[]
  onOpen: (projectId: string) => void
  onDelete?: (projectId: string, projectName: string) => void
  onDownload?: (
    projectId: string,
    projectName: string,
    projectData: string,
    projectTags: string[],
    createdAt: string,
    updatedAt: string,
  ) => void
  showDeleteButton?: boolean
  openButtonText?: string
  openButtonIcon?: React.ReactNode
}

export function ProjectList({
  projects,
  currentPage,
  itemsPerPage,
  searchTerm,
  selectedTags,
  onOpen,
  onDelete,
  onDownload,
  showDeleteButton = true,
  openButtonText = "Open",
  openButtonIcon,
}: ProjectListProps) {
  const [downloadDialog, setDownloadDialog] = useState<{
    open: boolean
    project: ProjectData | null
  }>({ open: false, project: null })

  // Format file size
  const formatSize = (sizeInKB: number): string => {
    if (sizeInKB < 1024) {
      return `${sizeInKB.toFixed(1)}KB`
    } else {
      return `${(sizeInKB / 1024).toFixed(1)}MB`
    }
  }

  const handleDownloadClick = (project: ProjectData) => {
    setDownloadDialog({ open: true, project })
  }

  const handleDownloadConfirm = () => {
    if (downloadDialog.project && onDownload) {
      onDownload(
        downloadDialog.project.id,
        downloadDialog.project.name,
        downloadDialog.project.data,
        downloadDialog.project.tags,
        downloadDialog.project.createdAt,
        downloadDialog.project.updatedAt,
      )
    }
    setDownloadDialog({ open: false, project: null })
  }

  const paginatedProjects = projects.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage)

  if (projects.length === 0) {
    return (
      <div className="max-h-[30vh] overflow-y-auto">
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
              : showDeleteButton
                ? "Create your first project to get started"
                : "Create projects in the editor first"}
          </p>
        </div>
      </div>
    )
  }

  return (
    <>
      <div className="flex-1 min-h-0 overflow-y-auto p-1">
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
          {paginatedProjects.map((project) => (
            <div
              key={project.id}
              className="group relative flex h-40 cursor-pointer flex-col justify-between overflow-hidden rounded-lg border bg-card text-card-foreground shadow-sm transition-all duration-200 ease-in-out hover:shadow-md dark:border-gray-800 dark:hover:border-gray-700"
              onClick={() => onOpen(project.id)}
            >
              <div className="flex flex-col p-4">
                <div className="mb-2">
                  <span
                    className="block truncate font-semibold text-gray-900 dark:text-gray-100"
                    title={project.name}
                  >
                    {project.name}
                  </span>
                </div>
                {project.tags && project.tags.length > 0 && (
                  <div className="mb-3 flex flex-wrap gap-1" title={project.tags.join(", ")}>
                    {project.tags.slice(0, 3).map((tag) => (
                      <span
                        key={tag}
                        className="inline-flex items-center rounded-md bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700 ring-1 ring-inset ring-blue-700/10 dark:bg-blue-900/50 dark:text-blue-300 dark:ring-blue-700/30"
                      >
                        {tag}
                      </span>
                    ))}
                    {project.tags.length > 3 && (
                      <span className="text-xs text-gray-500 dark:text-gray-400">+{project.tags.length - 3}</span>
                    )}
                  </div>
                )}
                <div className="mt-auto text-xs text-gray-500 dark:text-gray-400">
                  <span>{formatSize(project.size)}</span>
                  <span className="mx-1.5">•</span>
                  <span>Updated {new Date(project.updatedAt).toLocaleDateString()}</span>
                </div>
              </div>
              <div className="absolute bottom-2 right-2 flex items-center gap-1 opacity-0 transition-opacity duration-200 group-hover:opacity-100">
                {onDownload && (
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={(e) => {
                      e.stopPropagation()
                      handleDownloadClick(project)
                    }}
                    className="h-7 w-7 text-gray-500 hover:bg-gray-100 hover:text-green-600 dark:hover:bg-gray-800"
                    title="Download project"
                  >
                    <Download className="h-4 w-4" />
                  </Button>
                )}
                {showDeleteButton && onDelete && (
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={(e) => {
                      e.stopPropagation()
                      onDelete(project.id, project.name)
                    }}
                    className="h-7 w-7 text-gray-500 hover:bg-gray-100 hover:text-red-600 dark:hover:bg-gray-800"
                    title="Delete project"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                )}
              </div>
              <div className="absolute top-2 right-2 text-xs font-mono text-gray-400/50 dark:text-gray-500/50">
                {project.id.slice(0, 8)}
              </div>
            </div>
          ))}
        </div>
      </div>

      <DownloadConfirmDialog
        open={downloadDialog.open}
        onOpenChange={(open) => setDownloadDialog({ open, project: null })}
        fileName={`gg-lexical-editor-${downloadDialog.project?.name || "project"}.zip`}
        fileSize={formatSize(downloadDialog.project?.size || 0)}
        lastModified={downloadDialog.project ? new Date(downloadDialog.project.updatedAt).toLocaleDateString() : ""}
        onConfirm={handleDownloadConfirm}
        project={downloadDialog.project}
      />
    </>
  )
}
