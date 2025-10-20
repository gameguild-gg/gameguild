"use client"

import type React from "react"
import { useState } from "react"

import { FolderOpen } from "lucide-react"
import { DownloadConfirmDialog } from "@/components/editor/extras/dialogs/download-confirm-dialog"
import { ProjectGridView } from "./project-grid-view"
import { ProjectListView } from "./project-list-view"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  isLocallyAvailable?: boolean
}

interface ProjectListProps {
  projects: ProjectData[]
  currentPage: number
  itemsPerPage: number
  searchTerm: string
  selectedTags: string[]
  viewMode?: 'grid' | 'list'
  onOpen: (projectId: string) => void
  onView?: (projectId: string) => void
  onDelete?: (projectId: string, projectName: string) => void
  onInfo?: (project: ProjectData) => void
  onDownload?: (
    projectId: string,
    projectName: string,
    projectData: string,
    projectTags: string[],
    createdAt: string,
    updatedAt: string,
  ) => void
  showDeleteButton?: boolean
  showStudioViewerButtons?: boolean
  openButtonText?: string
  openButtonIcon?: React.ReactNode
}

export function ProjectList({
  projects,
  currentPage,
  itemsPerPage,
  searchTerm,
  selectedTags,
  viewMode = 'grid',
  onOpen,
  onView,
  onDelete,
  onInfo,
  onDownload,
  showDeleteButton = true,
  showStudioViewerButtons = false,
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

  const handleProjectDownload = (project: ProjectData) => {
    handleDownloadClick(project)
  }

  return (
    <>
      <div className="flex-1 min-h-0 overflow-y-auto p-1">
        {viewMode === 'grid' ? (
          <ProjectGridView
            projects={paginatedProjects}
            onOpen={onOpen}
            onView={onView}
            onDelete={onDelete}
            onInfo={onInfo}
            onDownload={handleProjectDownload}
            showDeleteButton={showDeleteButton}
            showStudioViewerButtons={showStudioViewerButtons}
            openButtonText={openButtonText}
            openButtonIcon={openButtonIcon}
          />
        ) : (
          <ProjectListView
            projects={paginatedProjects}
            onOpen={onOpen}
            onView={onView}
            onDelete={onDelete}
            onInfo={onInfo}
            onDownload={handleProjectDownload}
            showDeleteButton={showDeleteButton}
            showStudioViewerButtons={showStudioViewerButtons}
            openButtonText={openButtonText}
            openButtonIcon={openButtonIcon}
          />
        )}
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
