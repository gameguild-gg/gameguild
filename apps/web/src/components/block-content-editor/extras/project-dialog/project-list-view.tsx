"use client"

import type React from "react"
import { ProjectCard } from "./project-card"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"

interface ProjectListViewProps {
  projects: ProjectData[]
  columns?: number
  onOpen: (projectId: string, event?: React.MouseEvent) => void
  onView?: (projectId: string, event?: React.MouseEvent) => void
  onDelete?: (projectId: string, projectName: string) => void
  onInfo?: (project: ProjectData) => void
  onDownload?: (project: ProjectData) => void
  showDeleteButton?: boolean
  showStudioViewerButtons?: boolean
  openButtonText?: string
  openButtonIcon?: React.ReactNode
}

export function ProjectListView({
  projects,
  columns = 1,
  onOpen,
  onView,
  onDelete,
  onInfo,
  onDownload,
  showDeleteButton = true,
  showStudioViewerButtons = false,
  openButtonText = "Open",
  openButtonIcon,
}: ProjectListViewProps) {
  return (
    <div className={columns === 2 ? "grid grid-cols-1 lg:grid-cols-2 gap-4" : "space-y-4"}>
      {projects.map((project) => (
        <ProjectCard
          key={project.id}
          project={project}
          viewMode="list"
          onOpen={onOpen}
          onView={onView}
          onDelete={onDelete}
          onInfo={onInfo}
          onDownload={onDownload}
          showDeleteButton={showDeleteButton}
          showStudioViewerButtons={showStudioViewerButtons}
          openButtonText={openButtonText}
          openButtonIcon={openButtonIcon}
        />
      ))}
    </div>
  )
}