"use client"

import type { SerializedEditorState } from "lexical"
import { PreviewRenderer } from "./preview-renderer"
import { PreviewTableOfContents } from "./preview-table-of-contents"
import { ProjectSidebarList } from "./project-sidebar-list-improved"
import type { ProjectData } from "./preview-load-operations"
import type { StorageType } from "@/components/block-content-editor/lib/storage/editor/storage-types"

interface PreviewRendererType1Props {
  serializedState: SerializedEditorState
  currentProject: ProjectData
  storageAdapter: {
    load: (id: string) => Promise<ProjectData | null>
    list: () => Promise<ProjectData[]>
    searchProjects: (
      searchTerm: string,
      tags: string[],
      filterMode?: "all" | "any",
      storageTypeFilter?: StorageType
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

export function PreviewRendererType1({
  serializedState,
  currentProject,
  storageAdapter,
  availableTags,
  isDbInitialized,
  onProjectSelect,
  sidebarOpen,
  setSidebarOpen,
  showSidebar = true,
  showTableOfContents = true,
}: PreviewRendererType1Props) {
  return (
    <div className="flex flex-col lg:flex-row lg:gap-8">
      {/* Desktop Sidebar */}
      {showSidebar && (
        <aside className="hidden lg:block lg:w-1/3 xl:w-1/4">
          <ProjectSidebarList
            storageAdapter={storageAdapter}
            availableTags={availableTags}
            currentProject={currentProject}
            onProjectSelect={onProjectSelect}
            isDbInitialized={isDbInitialized}
            isSticky={true}
          />
        </aside>
      )}

      {/* Mobile Sidebar Overlay */}
      {showSidebar && sidebarOpen && (
        <div className="fixed inset-0 z-50 flex lg:hidden">
          <div className="fixed inset-0 bg-black bg-opacity-50" onClick={() => setSidebarOpen(false)} />
          <div className="relative h-full w-80 bg-white shadow-xl dark:bg-gray-900">
            <div className="flex items-center justify-between border-b border-gray-200 p-4 dark:border-gray-700">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Documents</h3>
              <button
                onClick={() => setSidebarOpen(false)}
                className="h-8 w-8 p-0 rounded hover:bg-gray-100 dark:hover:bg-gray-800"
              >
                ✕
              </button>
            </div>
            <div className="h-full">
              <ProjectSidebarList
                storageAdapter={storageAdapter}
                availableTags={availableTags}
                currentProject={currentProject}
                onProjectSelect={(project) => {
                  onProjectSelect(project)
                  setSidebarOpen(false)
                }}
                isDbInitialized={isDbInitialized}
              />
            </div>
          </div>
        </div>
      )}

      <main className={`flex-1 ${showSidebar ? 'lg:w-3/4 xl:w-3/4' : 'w-full'}`}>
        <div className={`grid grid-cols-1 gap-4 ${showTableOfContents ? 'xl:grid-cols-7' : ''}`}>
          <div className={showTableOfContents ? 'xl:col-span-5' : ''}>
            <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
              <div className="p-6 sm:p-8 md:p-12">
                <PreviewRenderer serializedState={serializedState} projectId={currentProject.id} storageAdapter={storageAdapter} />
              </div>
            </div>
          </div>

          {showTableOfContents && (
            <aside className="xl:col-span-2">
              <div className="sticky top-24">
                <PreviewTableOfContents serializedState={serializedState} />
              </div>
            </aside>
          )}
        </div>
      </main>
    </div>
  )
}
