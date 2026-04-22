"use client"

import { useState, useEffect, useRef, useMemo, useCallback, type ReactNode } from "react"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"
import { extractEditorStates } from "@/lib/storage/editor/layout-detector"
import { ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import { cellsToLexical } from "@/lib/storage/editor/cell-converters/lexical"
import { storageToBlocks } from "@/lib/storage/editor/cell-converters/blocks"
import { PreviewRenderer } from "@/components/editor/extras/preview/preview-renderer"
import { PreviewTableOfContents } from "@/components/editor/extras/preview/preview-table-of-contents"
import { ProjectSidebarList } from "@/components/editor/extras/preview/project-sidebar-list-improved"
import { BlockArrayViewer } from "@/components/editor/engines/blocks/block-array-viewer"
import { Button } from "@/components/ui/button"
import { Loader2, FileWarning } from "lucide-react"
import type { ProjectData } from "@/components/editor/extras/preview/preview-load-operations"
import type { SerializedEditorState } from "lexical"
import type { BlockArray } from "@/lib/storage/editor/block-structure"

// ============================================================================
// Types
// ============================================================================

export interface FeaturedProject {
  id: string
  showSidebar?: boolean
  showToc?: boolean
}

export interface LinkProject {
  id: string
  label: string
  showSidebar?: boolean
  showToc?: boolean
}

export type SelectOpts = { showSidebar?: boolean; showToc?: boolean }

export interface ActiveProject {
  id: string
  showSidebar?: boolean
  showToc?: boolean
}

// Re-export for convenience
export type { ProjectData }

/** Data returned by useStaticProject — pass to part components */
export interface StaticProjectData {
  loading: boolean
  error: string | null
  project: ProjectData | null
  isBlocksEngine: boolean
  serializedState?: SerializedEditorState
  blocksArray?: BlockArray
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
}

// ============================================================================
// useProjectList — loads all projects from IndexedDB
// ============================================================================

export function useProjectList() {
  const [projects, setProjects] = useState<ProjectData[]>([])
  const [loading, setLoading] = useState(true)
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())

  useEffect(() => {
    const init = async () => {
      try {
        await dbStorage.current.init()
        const list = await dbStorage.current.list()
        setProjects(list)
      } catch {
        // DB not available
      } finally {
        setLoading(false)
      }
    }
    init()
  }, [])

  return { projects, loading }
}

// ============================================================================
// useHashNavigation — manages active project via URL hash
// ============================================================================

export function useHashNavigation() {
  const [activeProject, setActiveProject] = useState<ActiveProject | null>(null)

  useEffect(() => {
    const hash = window.location.hash.replace("#", "")
    if (hash) setActiveProject({ id: hash })

    const onHashChange = () => {
      const h = window.location.hash.replace("#", "")
      setActiveProject(h ? { id: h } : null)
    }
    window.addEventListener("hashchange", onHashChange)
    return () => window.removeEventListener("hashchange", onHashChange)
  }, [])

  const selectProject = useCallback((id: string, opts?: SelectOpts) => {
    setActiveProject({ id, ...opts })
    window.history.pushState(null, "", `#${id}`)
  }, [])

  const goBack = useCallback(() => {
    setActiveProject(null)
    window.history.pushState(null, "", window.location.pathname)
  }, [])

  return { activeProject, selectProject, goBack }
}

// ============================================================================
// useStaticProject — loads a single project and prepares render data
// ============================================================================

export function useStaticProject(projectId: string | null): StaticProjectData {
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())
  const [isDbInitialized, setIsDbInitialized] = useState(false)
  const [availableTags, setAvailableTags] = useState<Array<{ name: string; usageCount: number }>>([])
  const [project, setProject] = useState<ProjectData | null>(null)
  const [loading, setLoading] = useState(!!projectId)
  const [error, setError] = useState<string | null>(null)

  // Init DB once
  useEffect(() => {
    let cancelled = false
    dbStorage.current
      .init()
      .then(() => {
        if (cancelled) return
        setIsDbInitialized(true)
        dbStorage.current
          .getAllTags()
          .then((tags) => { if (!cancelled) setAvailableTags(tags) })
          .catch(() => {})
      })
      .catch(() => {})
    return () => { cancelled = true }
  }, [])

  // Load project when ID changes
  useEffect(() => {
    if (!projectId) {
      setProject(null)
      setLoading(false)
      setError(null)
      return
    }
    if (!isDbInitialized) {
      setLoading(true)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    dbStorage.current
      .load(projectId)
      .then((data) => {
        if (cancelled) return
        if (!data) setError(`Project "${projectId}" not found`)
        else setProject(data)
      })
      .catch((err) => {
        if (cancelled) return
        setError(err instanceof Error ? err.message : "Unknown error")
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => { cancelled = true }
  }, [projectId, isDbInitialized])

  // Compute derived content data
  const { isBlocksEngine, serializedState, blocksArray } = useMemo(() => {
    if (!project) return { isBlocksEngine: false, serializedState: undefined, blocksArray: undefined }

    const projectEngine = (project as any).engine
    const isBlocks = projectEngine === ENGINE_TYPES.BLOCKS

    if (isBlocks) {
      const cellStates = extractEditorStates(project.data)
      const storageData = cellStates.blocks.b1 || { order: [], blocks: {} }
      const safeData =
        storageData && typeof storageData === "object" && !Array.isArray(storageData)
          ? storageData
          : { order: [], blocks: {} }
      return { isBlocksEngine: true, serializedState: undefined, blocksArray: storageToBlocks(safeData) }
    }

    const cellStates = extractEditorStates(project.data)
    const blocks = Object.entries(cellStates.blocks).reduce(
      (acc, [blockId, cellsData]) => {
        acc[blockId] = cellsToLexical(cellsData)
        return acc
      },
      {} as Record<string, any>,
    )
    const state = Object.values(blocks)[0] as SerializedEditorState | undefined

    return { isBlocksEngine: false, serializedState: state, blocksArray: undefined }
  }, [project])

  const storageAdapter = useMemo(
    () => ({
      load: (id: string) => dbStorage.current.load(id),
      list: () => dbStorage.current.list(),
      searchProjects: (
        searchTerm: string,
        tags: string[],
        filterMode?: "all" | "any",
        storageTypeFilter?: "local" | "gameguild-cloud" | "google-drive",
      ) => dbStorage.current.searchProjects(searchTerm, tags, filterMode || "any", storageTypeFilter),
    }),
    [],
  )

  return { loading, error, project, isBlocksEngine, serializedState, blocksArray, storageAdapter, availableTags, isDbInitialized }
}

// ============================================================================
// Part components — composable building blocks for project rendering
// ============================================================================

/** Renders the project title and metadata (tags, date) */
export function StaticProjectHeader({
  data,
  showTitle = true,
  showMeta = true,
  className,
}: {
  data: StaticProjectData
  showTitle?: boolean
  showMeta?: boolean
  className?: string
}) {
  if (!data.project || (!showTitle && !showMeta)) return null

  const meta = data.project as any
  const updatedAt = meta.updatedAt ? new Date(meta.updatedAt) : null
  const tags: string[] = meta.tags ?? []

  return (
    <header className={className ?? "mb-6"}>
      {showTitle && (
        <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50 mb-2">
          {data.project.name}
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
  )
}

/** Renders the project main content (blocks or lexical) */
export function StaticProjectContent({
  data,
  className,
}: {
  data: StaticProjectData
  className?: string
}) {
  if (data.loading) {
    return (
      <div className={`flex items-center justify-center py-20 ${className ?? ""}`}>
        <Loader2 className="h-8 w-8 animate-spin text-gray-400 dark:text-gray-500" />
      </div>
    )
  }

  if (data.error || !data.project) {
    return (
      <div className={`flex flex-col items-center justify-center py-20 text-center ${className ?? ""}`}>
        <FileWarning className="h-12 w-12 text-gray-300 dark:text-gray-600 mb-4" />
        <h3 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-1">Project Not Found</h3>
        <p className="text-sm text-gray-500 dark:text-gray-400">{data.error ?? "The project could not be loaded."}</p>
      </div>
    )
  }

  if (data.isBlocksEngine) {
    return (
      <div className={`border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 p-6 ${className ?? ""}`}>
        <BlockArrayViewer blocks={data.blocksArray || []} />
      </div>
    )
  }

  if (data.serializedState) {
    return (
      <div className={`border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 ${className ?? ""}`}>
        <div className="p-6 sm:p-8 md:p-12">
          <PreviewRenderer
            serializedState={data.serializedState}
            projectId={data.project.id}
            storageAdapter={data.storageAdapter}
          />
        </div>
      </div>
    )
  }

  return (
    <p className="text-sm text-gray-500 dark:text-gray-400 text-center py-10">
      This project has no content to display.
    </p>
  )
}

/** Renders the project list sidebar (lexical only) */
export function StaticProjectSidebar({
  data,
  onProjectSelect,
  isSticky = true,
  className,
}: {
  data: StaticProjectData
  onProjectSelect: (project: ProjectData) => void
  isSticky?: boolean
  className?: string
}) {
  if (!data.project || data.isBlocksEngine) return null

  return (
    <div className={className}>
      <ProjectSidebarList
        storageAdapter={data.storageAdapter}
        availableTags={data.availableTags}
        currentProject={data.project}
        onProjectSelect={onProjectSelect}
        isDbInitialized={data.isDbInitialized}
        isSticky={isSticky}
      />
    </div>
  )
}

/** Renders the table of contents (lexical only) */
export function StaticProjectToc({
  data,
  className,
}: {
  data: StaticProjectData
  className?: string
}) {
  if (!data.project || data.isBlocksEngine || !data.serializedState) return null

  return (
    <div className={className ?? "sticky top-24"}>
      <PreviewTableOfContents serializedState={data.serializedState} />
    </div>
  )
}

// ============================================================================
// Section components — convenience wrappers for common patterns
// ============================================================================

/** Renders a project's content inline on the page */
export function DirectSection({
  projectId,
  showTitle,
  showMeta,
  children,
  className,
}: {
  projectId: string
  showTitle?: boolean
  showMeta?: boolean
  children?: ReactNode
  className?: string
}) {
  const data = useStaticProject(projectId)

  return (
    <section className={className}>
      {children}
      {(showTitle || showMeta) && <StaticProjectHeader data={data} showTitle={showTitle} showMeta={showMeta} />}
      <StaticProjectContent data={data} />
    </section>
  )
}

/** Renders buttons that navigate to a project view */
export function LinkSection({
  links,
  onSelect,
  children,
  className,
}: {
  links: LinkProject[]
  onSelect: (id: string, opts?: SelectOpts) => void
  children?: ReactNode
  className?: string
}) {
  return (
    <section className={className}>
      {children}
      <div className="flex flex-wrap gap-3">
        {links.map((link) => (
          <Button
            key={link.id + link.label}
            variant="outline"
            onClick={() => onSelect(link.id, { showSidebar: link.showSidebar, showToc: link.showToc })}
            className="text-sm"
          >
            {link.label}
          </Button>
        ))}
      </div>
    </section>
  )
}

/** Renders every project as clickable cards */
export function AllProjectsSection({
  projects,
  onSelect,
  children,
  className,
  grid,
}: {
  projects: ProjectData[]
  onSelect: (id: string, opts?: SelectOpts) => void
  children?: ReactNode
  className?: string
  grid?: boolean
}) {
  if (projects.length === 0) return null

  return (
    <section className={className}>
      {children}
      <div className={grid ? "grid grid-cols-1 sm:grid-cols-2 gap-3" : "space-y-3"}>
        {projects.map((p) => (
          <ProjectCard key={p.id} project={p} onSelect={() => onSelect(p.id)} />
        ))}
      </div>
    </section>
  )
}

/** Renders only projects matching a given tag */
export function ByTagSection({
  projects,
  tag,
  onSelect,
  children,
  className,
  grid,
}: {
  projects: ProjectData[]
  tag: string
  onSelect: (id: string, opts?: SelectOpts) => void
  children?: ReactNode
  className?: string
  grid?: boolean
}) {
  const filtered = useMemo(
    () => projects.filter((p) => ((p as any).tags ?? []).includes(tag)),
    [projects, tag],
  )

  if (filtered.length === 0) return null

  return (
    <section className={className}>
      {children}
      <div className={grid ? "grid grid-cols-1 sm:grid-cols-2 gap-3" : "space-y-3"}>
        {filtered.map((p) => (
          <ProjectCard key={p.id} project={p} onSelect={() => onSelect(p.id)} />
        ))}
      </div>
    </section>
  )
}

/** Renders hand-picked projects as clickable cards */
export function FeaturedSection({
  projects,
  featured,
  onSelect,
  children,
  className,
  grid = true,
}: {
  projects: ProjectData[]
  featured: FeaturedProject[]
  onSelect: (id: string, opts?: SelectOpts) => void
  children?: ReactNode
  className?: string
  grid?: boolean
}) {
  const items = useMemo(
    () =>
      featured
        .map((fp) => {
          const project = projects.find((p) => p.id === fp.id)
          return project ? { project, showSidebar: fp.showSidebar, showToc: fp.showToc } : null
        })
        .filter((x): x is { project: ProjectData; showSidebar: boolean | undefined; showToc: boolean | undefined } => x != null),
    [projects, featured],
  )

  if (items.length === 0) return null

  return (
    <section className={className}>
      {children}
      <div className={grid ? "grid grid-cols-1 sm:grid-cols-2 gap-3" : "space-y-3"}>
        {items.map((item) => (
          <ProjectCard
            key={item.project.id}
            project={item.project}
            onSelect={() => onSelect(item.project.id, { showSidebar: item.showSidebar, showToc: item.showToc })}
          />
        ))}
      </div>
    </section>
  )
}

// ============================================================================
// ProjectCard
// ============================================================================

export function ProjectCard({
  project,
  onSelect,
}: {
  project: ProjectData
  onSelect: () => void
}) {
  const meta = project as any
  const engine: string = meta.engine ?? "lexical"
  const updatedAt = meta.updatedAt ? new Date(meta.updatedAt) : null
  const tags: string[] = meta.tags ?? []

  return (
    <button
      type="button"
      onClick={onSelect}
      className="w-full text-left rounded-lg border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 p-4 hover:border-blue-300 dark:hover:border-blue-700 hover:shadow-md transition-all cursor-pointer"
    >
      <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 truncate">
        {project.name}
      </h3>
      <div className="flex flex-wrap items-center gap-2 mt-1 text-xs text-gray-500 dark:text-gray-400">
        <span className="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-800 font-medium uppercase">
          {engine}
        </span>
        {updatedAt && (
          <time dateTime={updatedAt.toISOString()}>
            {updatedAt.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" })}
          </time>
        )}
        {tags.map((tag) => (
          <span
            key={tag}
            className="px-1.5 py-0.5 rounded-full bg-blue-50 dark:bg-blue-950 text-blue-600 dark:text-blue-400"
          >
            {tag}
          </span>
        ))}
      </div>
    </button>
  )
}
