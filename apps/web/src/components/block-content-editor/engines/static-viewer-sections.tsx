"use client"

/**
 * Static-viewer sections
 *
 * Composable read-only surfaces for rendering one or more projects without
 * the editor chrome. Provides three project-source modes through three
 * hooks, plus the corresponding section components:
 *
 *   useStaticProject(id)               → DirectSection
 *     ↳ Reads IndexedDB via `EnhancedStorageAdapter.load(id)`.
 *
 *   useStaticProjectFromFolder(folder) → DirectFolderSection
 *     ↳ Fetches `GET /api/static-viewer/folder/[folderName]`, which reads
 *       `src/data/test-blocks/<folder>/{index.json,data.block-content-editor}`.
 *
 *   useStaticBlocksFromFile(filePath)  → DirectFileSection
 *     ↳ Fetches `GET /api/static-viewer/file/[...path]` — single file, no
 *       index.json, no metadata.
 *
 * All three pipe the serialized payload through `deserializeProject(...)`
 * to produce a `BlockArray` and render it with `<BlockArrayViewer>`.
 *
 * See `docs/DATA-FLOW.md` ("Static-Viewer Flow").
 */

import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from "react"
import { FileWarning, Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"
import { deserializeProject } from "@/components/block-content-editor/lib/storage/editor/block-storage"
import { BlockArrayViewer } from "@/components/block-content-editor/engines/blocks/block-array-viewer"
import type { ProjectData } from "@/components/block-content-editor/extras/preview/preview-load-operations"
import type { BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"

// ============================================================================
// Types
// ============================================================================

export interface FeaturedProject { id: string }
export interface LinkProject { id: string; label: string }
export type SelectOpts = Record<string, never>
export interface ActiveProject { id: string }

export type { ProjectData }

export interface StaticProjectData {
  loading: boolean
  error: string | null
  project: ProjectData | null
  blocks: BlockArray
}

export interface StaticBlocksData {
  loading: boolean
  error: string | null
  blocks: BlockArray
}

// ============================================================================
// Hooks
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
      } catch { /* ignore */ } finally { setLoading(false) }
    }
    init()
  }, [])

  return { projects, loading }
}

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

  const selectProject = useCallback((id: string) => {
    setActiveProject({ id })
    window.history.pushState(null, "", `#${id}`)
  }, [])

  const goBack = useCallback(() => {
    setActiveProject(null)
    window.history.pushState(null, "", window.location.pathname)
  }, [])

  return { activeProject, selectProject, goBack }
}

export function useStaticProject(projectId: string | null): StaticProjectData {
  const dbStorage = useRef<EnhancedStorageAdapter>(new EnhancedStorageAdapter())
  const [isDbInitialized, setIsDbInitialized] = useState(false)
  const [project, setProject] = useState<ProjectData | null>(null)
  const [loading, setLoading] = useState(!!projectId)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    dbStorage.current.init()
      .then(() => { if (!cancelled) setIsDbInitialized(true) })
      .catch(() => {})
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    if (!projectId) { setProject(null); setLoading(false); setError(null); return }
    if (!isDbInitialized) { setLoading(true); return }

    let cancelled = false
    setLoading(true); setError(null)
    dbStorage.current.load(projectId)
      .then((data) => {
        if (cancelled) return
        if (!data) setError(`Project "${projectId}" not found`)
        else setProject(data)
      })
      .catch((err) => {
        if (cancelled) return
        setError(err instanceof Error ? err.message : "Unknown error")
      })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [projectId, isDbInitialized])

  const blocks = useMemo<BlockArray>(() => (project ? deserializeProject(project.data) : []), [project])

  return { loading, error, project, blocks }
}

/**
 * Load a static project from a filesystem folder on the server (e.g. `src/data/test-blocks/<folderName>/`).
 * The folder must contain `index.json` and `data.block-content-editor`.
 * Fetches via the `/api/static-viewer/folder/[folderName]` route.
 */
export function useStaticProjectFromFolder(folderName: string | null): StaticProjectData {
  const [project, setProject] = useState<ProjectData | null>(null)
  const [loading, setLoading] = useState(!!folderName)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!folderName) {
      setProject(null)
      setLoading(false)
      setError(null)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    fetch(`/api/static-viewer/folder/${encodeURIComponent(folderName)}`)
      .then(async (res) => {
        const body = await res.json().catch(() => ({}))
        if (!res.ok) throw new Error(body?.error ?? `Failed to load folder (${res.status})`)
        return body as { project: ProjectData }
      })
      .then(({ project: p }) => {
        if (!cancelled) setProject(p)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Unknown error")
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [folderName])

  const blocks = useMemo<BlockArray>(() => (project ? deserializeProject(project.data) : []), [project])

  return { loading, error, project, blocks }
}

/**
 * Load only a `data.block-content-editor` file (no index.json / no project metadata).
 * `filePath` is relative to `src/data/test-blocks/` (forward-slash separated, e.g.
 * `"projeto-17792247804366bs8q7l9t/data.block-content-editor"`).
 */
export function useStaticBlocksFromFile(filePath: string | null): StaticBlocksData {
  const [raw, setRaw] = useState<string | null>(null)
  const [loading, setLoading] = useState(!!filePath)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!filePath) {
      setRaw(null)
      setLoading(false)
      setError(null)
      return
    }

    let cancelled = false
    setLoading(true)
    setError(null)

    const encoded = filePath
      .split("/")
      .filter(Boolean)
      .map(encodeURIComponent)
      .join("/")

    fetch(`/api/static-viewer/file/${encoded}`)
      .then(async (res) => {
        const body = await res.json().catch(() => ({}))
        if (!res.ok) throw new Error(body?.error ?? `Failed to load file (${res.status})`)
        return body as { data: string }
      })
      .then(({ data }) => {
        if (!cancelled) setRaw(data)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Unknown error")
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [filePath])

  const blocks = useMemo<BlockArray>(() => (raw ? deserializeProject(raw) : []), [raw])

  return { loading, error, blocks }
}

// ============================================================================
// Part components
// ============================================================================

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
  const updatedAt = meta.metadata?.updatedAt ? new Date(meta.metadata.updatedAt) : null
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

  return (
    <div className={`border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 p-6 ${className ?? ""}`}>
      <BlockArrayViewer blocks={data.blocks} />
    </div>
  )
}

// ============================================================================
// Section components
// ============================================================================

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

/**
 * Same shape as DirectSection, but loads the project from a server filesystem folder
 * (under `src/data/test-blocks/<folderName>/`) instead of the IndexedDB store.
 */
export function DirectFolderSection({
  folderName,
  showTitle,
  showMeta,
  children,
  className,
}: {
  folderName: string
  showTitle?: boolean
  showMeta?: boolean
  children?: ReactNode
  className?: string
}) {
  const data = useStaticProjectFromFolder(folderName)
  return (
    <section className={className}>
      {children}
      {(showTitle || showMeta) && <StaticProjectHeader data={data} showTitle={showTitle} showMeta={showMeta} />}
      <StaticProjectContent data={data} />
    </section>
  )
}

/**
 * Render blocks directly from a single `data.block-content-editor` file (no index.json).
 * Since there's no project metadata, no title/meta/tags are shown — just the blocks.
 */
export function DirectFileSection({
  filePath,
  children,
  className,
}: {
  filePath: string
  children?: ReactNode
  className?: string
}) {
  const data = useStaticBlocksFromFile(filePath)

  return (
    <section className={className}>
      {children}
      {data.loading ? (
        <div className="flex items-center justify-center py-20">
          <Loader2 className="h-8 w-8 animate-spin text-gray-400 dark:text-gray-500" />
        </div>
      ) : data.error ? (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <FileWarning className="h-12 w-12 text-gray-300 dark:text-gray-600 mb-4" />
          <h3 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-1">File Not Loaded</h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">{data.error}</p>
        </div>
      ) : (
        <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 p-6">
          <BlockArrayViewer blocks={data.blocks} />
        </div>
      )}
    </section>
  )
}

export function LinkSection({
  links,
  onSelect,
  children,
  className,
}: {
  links: LinkProject[]
  onSelect: (id: string) => void
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
            onClick={() => onSelect(link.id)}
            className="text-sm"
          >
            {link.label}
          </Button>
        ))}
      </div>
    </section>
  )
}

export function AllProjectsSection({
  projects,
  onSelect,
  children,
  className,
  grid,
}: {
  projects: ProjectData[]
  onSelect: (id: string) => void
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
  onSelect: (id: string) => void
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
  onSelect: (id: string) => void
  children?: ReactNode
  className?: string
  grid?: boolean
}) {
  const items = useMemo(
    () =>
      featured
        .map((fp) => projects.find((p) => p.id === fp.id))
        .filter((p): p is ProjectData => p != null),
    [projects, featured],
  )
  if (items.length === 0) return null
  return (
    <section className={className}>
      {children}
      <div className={grid ? "grid grid-cols-1 sm:grid-cols-2 gap-3" : "space-y-3"}>
        {items.map((project) => (
          <ProjectCard key={project.id} project={project} onSelect={() => onSelect(project.id)} />
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
  const updatedAt = meta.metadata?.updatedAt ? new Date(meta.metadata.updatedAt) : null
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
