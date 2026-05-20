"use client"

import type React from "react"
import { ProjectCard } from "./project-card"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"

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
      5: 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5',
      6: 'grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6',
      7: 'grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-7',
      9: 'grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-7 xl:grid-cols-9',
      12: 'grid-cols-3 sm:grid-cols-6 md:grid-cols-8 lg:grid-cols-10 xl:grid-cols-12',
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