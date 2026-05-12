"use client"

import { useState, useEffect, useRef } from "react"
import { toast } from "sonner"
import { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import { extractEditorStates } from "@/components/block-content-editor/lib/storage/editor/layout-detector"
import { ENGINE_TYPES } from "@/components/block-content-editor/lib/storage/editor/project-types"
import { cellsToLexical } from "@/components/block-content-editor/lib/storage/editor/cell-converters/lexical"
import { storageToBlocks } from "@/components/block-content-editor/lib/storage/editor/cell-converters/blocks"
import { ProjectContentRenderer } from "@/components/block-content-editor/engines/project-content-renderer"
import { Loader2, FileWarning } from "lucide-react"
import type { ProjectData } from "@/components/block-content-editor/extras/preview/preview-load-operations"

// ============================================================================
// StaticViewer — renders a project in a read-only layout
// ============================================================================

interface StaticViewerProps {
  /** The project ID to load and display */
  projectId: string
  /** Optional CSS class for the outer wrapper */
  className?: string
  /** Whether to show the project title as a heading */
  showTitle?: boolean
  /** Whether to show project metadata (tags, date) */
  showMeta?: boolean
  /** Whether to show the documents sidebar (lexical only) */
  showSidebar?: boolean
  /** Whether to show the table of contents (lexical only) */
  showToc?: boolean
}

export function StaticViewer({ projectId, className, showTitle = true, showMeta = true, showSidebar = false, showToc = false }: StaticViewerProps) {
  const [project, setProject] = useState<ProjectData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())
  const [isDbInitialized, setIsDbInitialized] = useState(false)
  const [availableTags, setAvailableTags] = useState<Array<{ name: string; usageCount: number }>>([])

  useEffect(() => {
    let cancelled = false

    const load = async () => {
      setLoading(true)
      setError(null)

      try {
        await dbStorage.current.init()
        if (cancelled) return
        setIsDbInitialized(true)

        const tags = await dbStorage.current.getAllTags().catch(() => [])
        if (cancelled) return
        setAvailableTags(tags)

        const data = await dbStorage.current.load(projectId)
        if (cancelled) return

        if (!data) {
          setError(`Project "${projectId}" not found`)
        } else {
          setProject(data)
        }
      } catch (err) {
        if (cancelled) return
        const msg = err instanceof Error ? err.message : "Unknown error"
        setError(msg)
        toast.error("Failed to load project", { description: msg })
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    load()
    return () => { cancelled = true }
  }, [projectId])

  // ── Loading state ──
  if (loading) {
    return (
      <div className={`flex items-center justify-center py-20 ${className ?? ""}`}>
        <Loader2 className="h-8 w-8 animate-spin text-gray-400 dark:text-gray-500" />
      </div>
    )
  }

  // ── Error state ──
  if (error || !project) {
    return (
      <div className={`flex flex-col items-center justify-center py-20 text-center ${className ?? ""}`}>
        <FileWarning className="h-12 w-12 text-gray-300 dark:text-gray-600 mb-4" />
        <h3 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-1">Project Not Found</h3>
        <p className="text-sm text-gray-500 dark:text-gray-400">{error ?? "The project could not be loaded."}</p>
      </div>
    )
  }

  // ── Compute layout from project ──
  const projectEngine = (project as any).engine
  const isBlocksEngine = projectEngine === ENGINE_TYPES.BLOCKS
  const projectMeta = project as any

  // Prepare data for the content renderer
  let serializedState: any = undefined
  let blocksArray: any[] | undefined = undefined

  if (isBlocksEngine) {
    const cellStates = extractEditorStates(project.data)
    const storageData = cellStates.blocks.b1 || { order: [], blocks: {} }
    const safeData =
      storageData && typeof storageData === "object" && !Array.isArray(storageData)
        ? storageData
        : { order: [], blocks: {} }
    blocksArray = storageToBlocks(safeData)
  } else {
    const cellStates = extractEditorStates(project.data)
    const blocks = Object.entries(cellStates.blocks).reduce(
      (acc, [blockId, cellsData]) => {
        acc[blockId] = cellsToLexical(cellsData)
        return acc
      },
      {} as Record<string, any>,
    )
    serializedState = Object.values(blocks)[0]
  }

  const storageAdapter = {
    load: async (id: string) => dbStorage.current.load(id),
    list: async () => dbStorage.current.list(),
    searchProjects: async (
      searchTerm: string,
      tags: string[],
      filterMode?: "all" | "any",
      storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive",
    ) => dbStorage.current.searchProjects(searchTerm, tags, filterMode || "any", storageTypeFilter),
  }

  const updatedAt = projectMeta.updatedAt ? new Date(projectMeta.updatedAt) : null
  const tags: string[] = projectMeta.tags ?? []

  return (
    <article className={className}>
      {/* Title + metadata */}
      {(showTitle || showMeta) && (
        <header className="mb-6">
          {showTitle && (
            <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50 mb-2">
              {project.name}
            </h1>
          )}
          {showMeta && (
            <div className="flex flex-wrap items-center gap-3 text-sm text-gray-500 dark:text-gray-400">
              {updatedAt && (
                <time dateTime={updatedAt.toISOString()}>
                  {updatedAt.toLocaleDateString(undefined, { year: "numeric", month: "long", day: "numeric" })}
                </time>
              )}
              {tags.length > 0 && (
                <div className="flex flex-wrap gap-1.5">
                  {tags.map((tag) => (
                    <span
                      key={tag}
                      className="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300"
                    >
                      {tag}
                    </span>
                  ))}
                </div>
              )}
            </div>
          )}
        </header>
      )}

      {/* Content */}
      <ProjectContentRenderer
        project={project}
        isBlocksEngine={isBlocksEngine}
        blocksArray={blocksArray}
        serializedState={serializedState}
        storageAdapter={storageAdapter}
        availableTags={availableTags}
        isDbInitialized={isDbInitialized}
        onProjectSelect={() => {}}
        sidebarOpen={false}
        setSidebarOpen={() => {}}
        showSidebar={showSidebar}
        showTableOfContents={showToc}
      />
    </article>
  )
}
