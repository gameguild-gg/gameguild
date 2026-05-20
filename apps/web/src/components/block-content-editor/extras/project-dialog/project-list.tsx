"use client"

import type React from "react"
import { useState } from "react"

import { FolderOpen } from "lucide-react"
import { DownloadConfirmDialog } from "@/components/block-content-editor/extras/dialogs/download-confirm-dialog"
import { ProjectGridView } from "./project-grid-view"
import { ProjectListView } from "./project-list-view"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"

interface ProjectListProps {
  projects: ProjectData[]
  currentPage: number
  itemsPerPage: number
  searchTerm: string
  selectedTags: string[]
  viewMode?: 'grid' | 'list'
  gridColumns?: number
  listColumns?: number
  onOpen: (projectId: string, event?: React.MouseEvent) => void
  onView?: (projectId: string, event?: React.MouseEvent) => void
  onDelete?: (projectId: string, projectName: string) => void
  onInfo?: (project: ProjectData) => void
  onDownload?: (
    projectId: string,
    projectName: string,
    projectData: string,
    projectTags: string[],
    createdAt: string,
    updatedAt: string,
    projectPreferences?: any
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
  gridColumns = 5,
  listColumns = 1,
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

  const handleDownloadConfirm = async () => {
    if (downloadDialog.project) {
      if (onDownload) {
        // Call the provided onDownload function
        onDownload(
          downloadDialog.project.id,
          downloadDialog.project.name,
          downloadDialog.project.data,
          downloadDialog.project.tags,
          downloadDialog.project.metadata.createdAt,
          downloadDialog.project.metadata.updatedAt,
          downloadDialog.project.preferences
        )
      } else {
        // If no onDownload provided, implement download locally
        try {
          // Dynamic imports to avoid issues if these aren't available
          const [{ HashManager }, { ProjectExporter }] = await Promise.all([
            import("@/components/block-content-editor/lib/sync/editor/hash-manager"),
            import("@/components/block-content-editor/lib/interopAdapter/project-exporter")
          ])

          console.log('[Download] Starting download for project:', downloadDialog.project.id)

          // Generate hash for the project
          const hash = await HashManager.generateHash(downloadDialog.project.data)
          console.log('[Download] Generated hash:', hash)

          // Prepare project data for export using ProjectExporter
          const exportProjectData = {
            id: downloadDialog.project.id,
            name: downloadDialog.project.name,
            data: downloadDialog.project.data,
            tags: downloadDialog.project.tags,
            size: new Blob([downloadDialog.project.data]).size,
            createdAt: downloadDialog.project.metadata.createdAt,
            updatedAt: downloadDialog.project.metadata.updatedAt,
            hash: hash,
            storageType: "local" as const,
            preferences: downloadDialog.project.preferences
          }

          console.log('[Download] Export project data prepared:', {
            id: exportProjectData.id,
            name: exportProjectData.name,
            hasPreferences: !!exportProjectData.preferences
          })

          // Use ProjectExporter to create the ZIP file
          const zipBlob = await ProjectExporter.createZipFile(exportProjectData, hash)
          console.log('[Download] ZIP file created, size:', zipBlob.size)

          // Create download link
          const url = URL.createObjectURL(zipBlob)
          const link = document.createElement("a")
          link.href = url
          link.download = ProjectExporter.getDownloadFilename(exportProjectData)
          document.body.appendChild(link)
          link.click()

          console.log('[Download] Download initiated:', link.download)

          // Cleanup
          document.body.removeChild(link)
          URL.revokeObjectURL(url)
        } catch (error) {
          console.error("Download error:", error)
        }
      }
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
    // Always show confirmation dialog first
    handleDownloadClick(project)
  }

  return (
    <>
      <div className="flex-1 min-h-0 overflow-y-auto p-1">
        {viewMode === 'grid' ? (
          <ProjectGridView
            projects={paginatedProjects}
            columns={gridColumns}
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
            columns={listColumns}
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
        fileSize={formatSize(downloadDialog.project?.metadata.size || 0)}
        lastModified={downloadDialog.project ? new Date(downloadDialog.project.metadata.updatedAt).toLocaleDateString() : ""}
        onConfirm={handleDownloadConfirm}
        project={downloadDialog.project}
      />
    </>
  )
}
