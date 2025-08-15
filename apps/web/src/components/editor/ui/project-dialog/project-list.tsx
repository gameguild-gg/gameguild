"use client"

import type React from "react"
import { useState } from "react"

import { Button } from "@/components/ui/button"
import { FolderOpen, Trash2, Download } from "lucide-react"
import { DownloadConfirmDialog } from "@/components/editor/ui/download-confirm-dialog"

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
      <div className="max-h-[30vh] overflow-y-auto">
        <div className="space-y-2">
          {paginatedProjects.map((project) => (
            <div
              key={project.id}
              className="flex items-center justify-between p-3 border dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
            >
              <div className="flex flex-col flex-1 min-w-0 max-w-[calc(100%-120px)]">
                <div className="flex items-start gap-2 mb-1">
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate max-w-full block">
                    {project.name}
                  </span>
                </div>
                {project.tags && project.tags.length > 0 && (
                  <div className="flex gap-1 mb-1 flex-wrap">
                    {project.tags.slice(0, 2).map((tag) => (
                      <span
                        key={tag}
                        className="inline-flex items-center px-1.5 py-0.5 rounded text-xs bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 truncate max-w-20"
                      >
                        {tag}
                      </span>
                    ))}
                    {project.tags.length > 2 && (
                      <span className="text-xs text-gray-500 dark:text-gray-400 flex-shrink-0">
                        +{project.tags.length - 2}
                      </span>
                    )}
                  </div>
                )}
                <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 flex-wrap">
                  <span className="flex-shrink-0">{formatSize(project.size)}</span>
                  <span className="flex-shrink-0">•</span>
                  <span className="flex-shrink-0">Updated {new Date(project.updatedAt).toLocaleDateString()}</span>
                  <span className="flex-shrink-0">•</span>
                  <span className="text-gray-400 dark:text-gray-500 font-mono text-xs truncate max-w-20">
                    ID: {project.id.slice(0, 8)}...
                  </span>
                </div>
              </div>
              <div className="flex gap-1 ml-3 flex-shrink-0">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onOpen(project.id)}
                  className="text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300 gap-1 px-2"
                >
                  {openButtonIcon}
                  <span className="hidden sm:inline">{openButtonText}</span>
                </Button>
                {onDownload && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleDownloadClick(project)}
                    className="text-green-600 hover:text-green-700 dark:text-green-400 dark:hover:text-green-300 px-2"
                    title="Download project folder"
                  >
                    <Download className="w-4 h-4" />
                  </Button>
                )}
                {showDeleteButton && onDelete && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => onDelete(project.id, project.name)}
                    className="text-red-600 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300 px-2"
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                )}
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
