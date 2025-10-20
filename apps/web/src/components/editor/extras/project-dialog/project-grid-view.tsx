"use client"

import type React from "react"
import { ProjectCard } from "./project-card"

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

interface ProjectGridViewProps {
  projects: ProjectData[]
  onOpen: (projectId: string) => void
  onView?: (projectId: string) => void
  onDelete?: (projectId: string, projectName: string) => void
  onInfo?: (project: ProjectData) => void
  onDownload?: (project: ProjectData) => void
  showDeleteButton?: boolean
  showStudioViewerButtons?: boolean
  openButtonText?: string
  openButtonIcon?: React.ReactNode
}

export function ProjectGridView({
  projects,
  onOpen,
  onView,
  onDelete,
  onInfo,
  onDownload,
  showDeleteButton = true,
  showStudioViewerButtons = false,
  openButtonText = "Open",
  openButtonIcon,
}: ProjectGridViewProps) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
      {projects.map((project) => (
        <ProjectCard
          key={project.id}
          project={project}
          viewMode="grid"
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