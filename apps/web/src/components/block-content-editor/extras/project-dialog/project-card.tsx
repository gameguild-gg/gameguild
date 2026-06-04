"use client"

import type React from "react"
import { Button } from "@/components/ui/button"
import { FolderOpen, Trash2, Download, Info, HardDrive, Cloud, Database, Wifi, WifiOff, Eye, Blocks } from "lucide-react"
import { useGoogleDriveAuth } from "@/components/block-content-editor/hooks/editor/use-google-drive-auth"
import type { ProjectData } from "@/components/block-content-editor/lib/storage/editor/project-data"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"
import { getProjectTypeLabel } from "@/components/block-content-editor/lib/storage/editor/project-types"

interface ProjectCardProps {
  project: ProjectData
  viewMode: 'grid' | 'list'
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

export function ProjectCard({
  project,
  viewMode,
  onOpen,
  onView,
  onDelete,
  onInfo,
  onDownload,
  showDeleteButton = true,
  showStudioViewerButtons = false,
  openButtonText = "Open",
  openButtonIcon,
}: ProjectCardProps) {
  const { isAuthenticated: isGoogleDriveAuthenticated } = useGoogleDriveAuth()

  // Get project type label from preferences (document/quiz/general).
  const getProjectTypeLabelLocal = (): string => {
    return getProjectTypeLabel(project.preferences?.global?.projectType)
  }

  // Format file size
  const formatSize = (sizeInKB: number): string => {
    if (sizeInKB < 1024) {
      return `${sizeInKB.toFixed(1)}KB`
    } else {
      return `${(sizeInKB / 1024).toFixed(1)}MB`
    }
  }

  // Render storage type indicator
  const renderStorageIndicator = (
    storageType: StorageType | undefined,
    isLocallyAvailable?: boolean
  ) => {
    if (!storageType || storageType === "local") {
      return (
        <div className="flex items-center gap-1 text-xs text-gray-600 dark:text-gray-400" title="Stored locally on this device">
          <HardDrive className="h-3 w-3" />
          <span>Local</span>
        </div>
      )
    }

    if (storageType === "gameguild-cloud") {
      return (
        <div className="flex items-center gap-1 text-xs text-blue-600 dark:text-blue-400" title="Stored on GameGuild Cloud - Always accessible">
          <Database className="h-3 w-3" />
          <span>GameGuild</span>
          <Wifi className="h-2 w-2" />
        </div>
      )
    }

    if (storageType === "google-drive") {
      const isConnected = isGoogleDriveAuthenticated
      const isAvailableLocally = isLocallyAvailable === true
      
      return (
        <div className={`flex items-center gap-1 text-xs ${
          isConnected 
            ? (isAvailableLocally ? "text-green-600 dark:text-green-400" : "text-blue-600 dark:text-blue-400")
            : "text-orange-600 dark:text-orange-400"
        }`} title={
          !isConnected 
            ? "Stored on Google Drive - Connect to access"
            : isAvailableLocally
              ? "Stored on Google Drive - Downloaded and ready"
              : "Stored on Google Drive - Click to download"
        }>
          <Cloud className="h-3 w-3" />
          <span>Google Drive</span>
          {!isConnected ? (
            <WifiOff className="h-2 w-2" />
          ) : isAvailableLocally ? (
            <Wifi className="h-2 w-2" />
          ) : (
            <Download className="h-2 w-2" />
          )}
        </div>
      )
    }

    return null
  }

  if (viewMode === 'grid') {
    return (
      <div
        className="group relative flex min-h-[200px] cursor-pointer flex-col justify-between overflow-hidden rounded-xl border border-border/50 bg-card text-card-foreground shadow-sm transition-all duration-200 ease-out hover:-translate-y-0.5 hover:border-border hover:shadow-md"
        onClick={(e) => !showStudioViewerButtons && onOpen(project.id, e)}
      >
        <div className="flex flex-col p-4 sm:p-5">
          <div className="mb-2 flex items-start justify-between gap-2">
            <div className="flex-1 min-w-0">
              <span
                className="block truncate text-sm sm:text-base font-semibold text-foreground leading-tight"
                title={project.name}
              >
                {project.name}
              </span>
              <span className="text-[11px] text-muted-foreground font-medium">
                {getProjectTypeLabelLocal()}
              </span>
            </div>
            {renderStorageIndicator(project.storageType, project.isLocallyAvailable)}
          </div>
          
          {project.tags && project.tags.length > 0 && (
            <div className="mb-3 flex flex-wrap gap-1.5" title={project.tags.join(", ")}>
              {project.tags.slice(0, 2).map((tag) => (
                <span
                  key={tag}
                  className="inline-flex items-center rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-medium text-blue-700 ring-1 ring-inset ring-blue-700/15 dark:bg-blue-900/40 dark:text-blue-300 dark:ring-blue-700/30 truncate max-w-[120px]"
                >
                  {tag}
                </span>
              ))}
              {project.tags.length > 2 && (
                <span className="text-[11px] text-muted-foreground">+{project.tags.length - 2}</span>
              )}
            </div>
          )}
          
          <div className="mt-auto text-[11px] text-muted-foreground truncate">
            <span>{formatSize(project.metadata.size)}</span>
            <span className="mx-1.5 text-border">•</span>
            <span className="hidden sm:inline">{new Date(project.metadata.updatedAt).toLocaleDateString()}</span>
            <span className="sm:hidden">{new Date(project.metadata.updatedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}</span>
          </div>
        </div>

        {/* Studio/Viewer buttons for grid view */}
        {showStudioViewerButtons && (
          <div className="absolute inset-x-2 bottom-2 space-y-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
            {/* Primary action buttons (Studio/Viewer) */}
            <div className="flex gap-2">
              <Button
                onClick={(e) => {
                  e.stopPropagation()
                  onOpen(project.id, e)
                }}
                onMouseDown={(e) => {
                  if (e.button === 1) {
                    e.preventDefault()
                    e.stopPropagation()
                    onOpen(project.id, { ctrlKey: true } as React.MouseEvent)
                  }
                }}
                size="sm"
                className="flex-1 h-8 bg-blue-600 hover:bg-blue-700 text-white"
              >
                <Blocks className="w-3 h-3 mr-1" />
                Studio
              </Button>
              <Button
                onClick={(e) => {
                  e.stopPropagation()
                  onView?.(project.id, e)
                }}
                onMouseDown={(e) => {
                  if (e.button === 1) {
                    e.preventDefault()
                    e.stopPropagation()
                    onView?.(project.id, { ctrlKey: true } as React.MouseEvent)
                  }
                }}
                size="sm"
                variant="outline"
                className="flex-1 h-8"
              >
                <Eye className="w-3 h-3 mr-1" />
                Viewer
              </Button>
            </div>
            
            {/* Secondary action buttons (Info/Download/Delete) */}
            <div className="flex justify-center gap-1">
              {onInfo && (
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={(e) => {
                    e.stopPropagation()
                    onInfo(project)
                  }}
                  className="h-7 w-7 text-gray-500 hover:bg-gray-100 hover:text-blue-600 dark:hover:bg-gray-800"
                  title="Edit project info"
                >
                  <Info className="h-3 w-3" />
                </Button>
              )}
              {onDownload && (
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={(e) => {
                    e.stopPropagation()
                    onDownload(project)
                  }}
                  className="h-7 w-7 text-gray-500 hover:bg-gray-100 hover:text-green-600 dark:hover:bg-gray-800"
                  title="Download project"
                >
                  <Download className="h-3 w-3" />
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
                  <Trash2 className="h-3 w-3" />
                </Button>
              )}
            </div>
          </div>
        )}

        {/* Regular action buttons for grid view */}
        {!showStudioViewerButtons && (
          <div className="absolute bottom-2 right-2 flex items-center gap-1 opacity-0 transition-opacity duration-200 group-hover:opacity-100">
            {onInfo && (
              <Button
                variant="ghost"
                size="icon"
                onClick={(e) => {
                  e.stopPropagation()
                  onInfo(project)
                }}
                className="h-7 w-7 text-gray-500 hover:bg-gray-100 hover:text-blue-600 dark:hover:bg-gray-800"
                title="Edit project info"
              >
                <Info className="h-4 w-4" />
              </Button>
            )}
            {onDownload && (
              <Button
                variant="ghost"
                size="icon"
                onClick={(e) => {
                  e.stopPropagation()
                  onDownload(project)
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
        )}

        <div className="absolute top-2 right-2 text-[10px] font-mono text-muted-foreground/40">
          {project.id.slice(0, 8)}
        </div>
      </div>
    )
  }

  // List view
  return (
    <div className="group rounded-xl border border-border/50 bg-card p-4 shadow-sm transition-all duration-200 hover:border-border hover:shadow-md">
      <div className="flex items-center justify-between">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-3 mb-2">
            <div className="flex flex-col gap-0.5 min-w-0">
              <span className="font-semibold text-gray-900 dark:text-gray-100 truncate" title={project.name}>
                {project.name}
              </span>
              <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">
                {getProjectTypeLabelLocal()}
              </span>
            </div>
            {renderStorageIndicator(project.storageType, project.isLocallyAvailable)}
          </div>
          
          {project.tags && project.tags.length > 0 && (
            <div className="flex flex-wrap gap-1 mb-2">
              {project.tags.slice(0, 5).map((tag) => (
                <span
                  key={tag}
                  className="inline-flex items-center rounded-md bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700 ring-1 ring-inset ring-blue-700/10 dark:bg-blue-900/50 dark:text-blue-300 dark:ring-blue-700/30"
                >
                  {tag}
                </span>
              ))}
              {project.tags.length > 5 && (
                <span className="text-xs text-gray-500 dark:text-gray-400">+{project.tags.length - 5}</span>
              )}
            </div>
          )}
          
          <div className="text-xs text-gray-500 dark:text-gray-400">
            <span>{formatSize(project.metadata.size)}</span>
            <span className="mx-1.5">•</span>
            <span>Updated {new Date(project.metadata.updatedAt).toLocaleDateString()}</span>
            <span className="mx-1.5">•</span>
            <span className="font-mono">#{project.id.slice(0, 8)}</span>
          </div>
        </div>

        <div className="flex items-center gap-2 ml-4">
          {/* Studio/Viewer buttons for list view */}
          {showStudioViewerButtons ? (
            <>
              {/* Primary actions */}
              <Button
                onClick={(e) => onOpen(project.id, e)}
                onMouseDown={(e) => {
                  if (e.button === 1) {
                    e.preventDefault()
                    onOpen(project.id, { ctrlKey: true } as React.MouseEvent)
                  }
                }}
                size="sm"
                className="bg-blue-600 hover:bg-blue-700 text-white"
              >
                <Blocks className="w-4 h-4 mr-1" />
                Studio
              </Button>
              <Button
                onClick={(e) => onView?.(project.id, e)}
                onMouseDown={(e) => {
                  if (e.button === 1) {
                    e.preventDefault()
                    onView?.(project.id, { ctrlKey: true } as React.MouseEvent)
                  }
                }}
                size="sm"
                variant="outline"
              >
                <Eye className="w-4 h-4 mr-1" />
                Viewer
              </Button>
              
              {/* Secondary actions */}
              {onInfo && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onInfo(project)}
                  title="Edit project info"
                  className="opacity-60 hover:opacity-100"
                >
                  <Info className="w-4 h-4" />
                </Button>
              )}
              
              {onDownload && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onDownload(project)}
                  title="Download project"
                  className="opacity-60 hover:opacity-100"
                >
                  <Download className="w-4 h-4" />
                </Button>
              )}
              
              {showDeleteButton && onDelete && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onDelete(project.id, project.name)}
                  className="opacity-60 hover:opacity-100 text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950"
                  title="Delete project"
                >
                  <Trash2 className="w-4 h-4" />
                </Button>
              )}
            </>
          ) : (
            /* Regular action buttons for list view */
            <>
              <Button
                onClick={(e) => onOpen(project.id, e)}
                size="sm"
                className="gap-1"
              >
                {openButtonIcon || <FolderOpen className="w-4 h-4" />}
                {openButtonText}
              </Button>
              
              {onInfo && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onInfo(project)}
                  title="Edit project info"
                >
                  <Info className="w-4 h-4" />
                </Button>
              )}
              
              {onDownload && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onDownload(project)}
                  title="Download project"
                >
                  <Download className="w-4 h-4" />
                </Button>
              )}
              
              {showDeleteButton && onDelete && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onDelete(project.id, project.name)}
                  className="text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950"
                  title="Delete project"
                >
                  <Trash2 className="w-4 h-4" />
                </Button>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  )
}