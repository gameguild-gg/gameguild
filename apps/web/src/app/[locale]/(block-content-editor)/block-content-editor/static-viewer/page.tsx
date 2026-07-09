"use client"

import { Button } from "@/components/ui/button"
import { Eye, Home, Pencil } from "lucide-react"
import Link from "next/link"
import {
  useProjectList,
  useHashNavigation,
  useStaticProject,
  StaticProjectHeader,
  StaticProjectContent,
  DirectSection,
  DirectFolderSection,
  DirectFileSection,
  LinkSection,
  FeaturedSection,
  ByTagSection,
  AllProjectsSection,
} from "@/components/block-content-editor/engines/static-viewer-sections"

// ============================================================================
// Page component
// ============================================================================

const PAGE_TITLE = "Static Viewer"

export default function StaticViewerPage() {
  const { projects, loading } = useProjectList()
  const { activeProject, selectProject, goBack } = useHashNavigation()
  const projectData = useStaticProject(activeProject?.id ?? null)

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
      {/* Nav bar */}
      <nav className="border-b border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900">
        <div className="mx-auto max-w-4xl flex items-center justify-between px-4 py-3">
          <div className="flex items-center gap-2">
            <Eye className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            <span className="font-semibold text-gray-900 dark:text-gray-100">{PAGE_TITLE}</span>
          </div>
          <div className="flex items-center gap-2">
            {activeProject && (
              <Button variant="ghost" size="sm" onClick={goBack}>
                Back
              </Button>
            )}
            <Link href="/block-content-editor/studio">
              <Button variant="ghost" size="sm">
                <Pencil className="h-4 w-4 mr-1" /> Studio
              </Button>
            </Link>
            <Link href="/block-content-editor">
              <Button variant="ghost" size="sm">
                <Home className="h-4 w-4 mr-1" /> Home
              </Button>
            </Link>
          </div>
        </div>
      </nav>

      <main className={`mx-auto px-4 sm:px-6 lg:px-8 py-8 ${activeProject ? 'max-w-full' : 'max-w-4xl'}`}>
        {/* ── Active project view ── */}
        {activeProject && (
          <div className="max-w-4xl mx-auto">
            <StaticProjectHeader data={projectData} />
            <StaticProjectContent data={projectData} />
          </div>
        )}

        {/* ── Sections list ── */}
        {!activeProject && (
          <>
            <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-50 mb-8">Projects</h1>

            {loading && (
              <p className="text-sm text-gray-500 dark:text-gray-400">Loading projects...</p>
            )}

            {!loading && projects.length === 0 && (
              <div className="text-center py-20">
                <Eye className="mx-auto h-12 w-12 text-gray-300 dark:text-gray-600 mb-4" />
                <p className="text-gray-500 dark:text-gray-400 mb-4">No projects yet</p>
                <Link href="/block-content-editor/studio">
                  <Button>Create your first project</Button>
                </Link>
              </div>
            )}

            {!loading && projects.length > 0 && (
              <div className="space-y-10">
                {/* Link buttons */}
                <LinkSection
                  links={[
                    { id: "405d66a5-45d7-472e-81d5-876ec4e3f682", label: "Open Divider Lex" },
                  ]}
                  onSelect={selectProject}
                >
                  <h2 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-3">Quick Access</h2>
                </LinkSection>

                {/* Direct inline render (loaded from IndexedDB by id) */}
                <DirectSection
                  projectId="405d66a5-45d7-472e-81d5-876ec4e3f682"
                  showTitle
                  showMeta
                >
                  <h2 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-3">Direct View (by id)</h2>
                </DirectSection>

                {/* Direct inline render (loaded from a filesystem folder under src/data/test-blocks/) */}
                <DirectFolderSection
                  folderName="projeto-17792247804366bs8q7l9t"
                  showTitle
                  showMeta
                >
                  <h2 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-3">Direct View (from folder)</h2>
                </DirectFolderSection>

                {/* Direct inline render (loaded from a single block-content-editor file, no index.json) */}
                <DirectFileSection
                  filePath="test-blocks/projeto-17792247804366bs8q7l9t/data.block-content-editor"
                >
                  <h2 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-3">Direct View (from file)</h2>
                </DirectFileSection>

                {/* Featured cards */}
                <FeaturedSection
                  projects={projects}
                  featured={[
                    { id: "4317f83d-8fd4-4832-901d-a2c3f0d11fa2" },
                    { id: "4317f83d-8fd4-4832-901d-a2c3f0d11fa2" },
                  ]}
                  onSelect={selectProject}
                >
                  <h2 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-3">Featured</h2>
                </FeaturedSection>

                {/* By tag */}
                <ByTagSection
                  projects={projects}
                  tag="blocks"
                  onSelect={selectProject}
                >
                  <h2 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-3">Blocks Projects</h2>
                </ByTagSection>

                {/* All projects */}
                <AllProjectsSection
                  projects={projects}
                  onSelect={selectProject}
                >
                  <h2 className="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-3">All Projects</h2>
                </AllProjectsSection>
              </div>
            )}
          </>
        )}
      </main>
    </div>
  )
}
