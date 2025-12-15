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

export function ProjectGridView({
  projects,
  columns = 5,
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
  // Generate grid columns class based on columns prop
  const getGridClass = () => {
    const colMap: Record<number, string> = {
      5: 'grid-cols-1 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5',
      6: 'grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6',
      7: 'grid-cols-2 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-7',
      9: 'grid-cols-3 md:grid-cols-4 lg:grid-cols-6 xl:grid-cols-9',
      12: 'grid-cols-3 md:grid-cols-6 lg:grid-cols-8 xl:grid-cols-12',
    }
    return colMap[columns] || colMap[5]
  }

  return (
    <div className={`grid ${getGridClass()} gap-4`}>
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