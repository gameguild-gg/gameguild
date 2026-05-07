"use client"

import type { SerializedEditorState } from "lexical"
import { PreviewRendererType1 } from "@/components/block-content-editor/extras/preview/preview-renderer-type1"
import { BlockArrayViewer } from "@/components/block-content-editor/engines/blocks/block-array-viewer"
import type { ProjectData } from "@/components/block-content-editor/extras/preview/preview-load-operations"
import type { BlockArray } from "@/lib/storage/editor/block-structure"

// ============================================================================
// ProjectContentRenderer — shared content renderer for both Viewer and
// StaticViewer pages. Renders blocks or lexical content given prepared data.
// ============================================================================

interface ProjectContentRendererProps {
  project: ProjectData
  isBlocksEngine: boolean
  /** For blocks engine */
  blocksArray?: BlockArray
  /** For lexical engine */
  serializedState?: SerializedEditorState
  storageAdapter: {
    load: (id: string) => Promise<ProjectData | null>
    list: () => Promise<ProjectData[]>
    searchProjects: (
      searchTerm: string,
      tags: string[],
      filterMode?: "all" | "any",
      storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive",
    ) => Promise<ProjectData[]>
  }
  availableTags: Array<{ name: string; usageCount: number }>
  isDbInitialized: boolean
  onProjectSelect: (project: ProjectData) => void
  sidebarOpen: boolean
  setSidebarOpen: (open: boolean) => void
  showSidebar?: boolean
  showTableOfContents?: boolean
}

export function ProjectContentRenderer({
  project,
  isBlocksEngine,
  blocksArray,
  serializedState,
  storageAdapter,
  availableTags,
  isDbInitialized,
  onProjectSelect,
  sidebarOpen,
  setSidebarOpen,
  showSidebar = true,
  showTableOfContents = true,
}: ProjectContentRendererProps) {
  if (isBlocksEngine) {
    return (
      <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 p-6">
        <BlockArrayViewer blocks={blocksArray || []} />
      </div>
    )
  }

  if (serializedState) {
    return (
      <PreviewRendererType1
        serializedState={serializedState}
        currentProject={project}
        storageAdapter={storageAdapter}
        availableTags={availableTags}
        isDbInitialized={isDbInitialized}
        onProjectSelect={onProjectSelect}
        sidebarOpen={sidebarOpen}
        setSidebarOpen={setSidebarOpen}
        showSidebar={showSidebar}
        showTableOfContents={showTableOfContents}
      />
    )
  }

  return (
    <p className="text-sm text-gray-500 dark:text-gray-400 text-center py-10">
      This project has no content to display.
    </p>
  )
}
