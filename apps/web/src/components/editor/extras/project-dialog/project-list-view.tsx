"use client"

import type React from "react"
import { ProjectCard } from "./project-card"

interface ProjectData {
  id: string
  name: string
  type: "type1" | "type2" | "type3"
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
  storageType?: "local" | "gameguild-cloud" | "google-drive"
  isLocallyAvailable?: boolean
  preferences?: any
}

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