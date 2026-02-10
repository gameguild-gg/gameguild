"use client"

import { PreviewRendererType2 } from "./preview-renderer-type2"
import type { SlideshowStructure } from "@/lib/storage/editor/slideshow-structure"
import { getDependentProject } from "@/lib/storage/editor/slideshow-structure"
import type { ProjectData } from "@/lib/storage/editor/enhanced-storage-adapter"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"

interface PreviewRendererSlideshowContinuousProps {
  structure: SlideshowStructure
  projectId: string
  projectName?: string
  deps: ProjectData[]
  resolvedProjects?: Map<string, ProjectData | null>
  storageAdapter?: {
    load: (id: string) => Promise<any>
  }
  preferences?: ProjectPreferences
}

export function PreviewRendererSlideshowContinuous({
  structure,
  projectId,
  projectName,
  deps,
  resolvedProjects,
  storageAdapter,
  preferences,
}: PreviewRendererSlideshowContinuousProps) {
  const slides = structure.slides

  const getSlideBlockStates = (slide: (typeof slides)[0]): Record<string, string> => {
    let project: ProjectData | null = null
    if (slide.projectRef.isDependent) {
      project = getDependentProject(deps, slide.projectRef.projectId) || null
    } else {
      project = resolvedProjects?.get(slide.id) || null
    }
    if (!project?.data) return {}
    try {
      const parsed = JSON.parse(project.data)
      const result: Record<string, string> = {}
      for (const [key, value] of Object.entries(parsed)) {
        result[key] = typeof value === 'string' ? value : JSON.stringify(value)
      }
      return result
    } catch {
      return {}
    }
  }

  return (
    <div className="space-y-6">
      {slides.map((slide, index) => {
        const blockStates = getSlideBlockStates(slide)
        return (
          <div
            key={slide.id}
            className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900"
          >
            {/* Slide Header */}
            {slide.name && (
              <div className="border-b border-gray-200 bg-gray-50 px-6 py-3 dark:border-gray-800 dark:bg-gray-800/50">
                <div className="flex items-center gap-3">
                  <span className="flex h-7 w-7 items-center justify-center rounded bg-blue-100 text-sm font-semibold text-blue-700 dark:bg-blue-900/50 dark:text-blue-300">
                    {index + 1}
                  </span>
                  <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                    {slide.name}
                  </h2>
                </div>
              </div>
            )}

            {/* Slide Content */}
            <div className="p-6">
              {Object.keys(blockStates).length > 0 ? (
                <PreviewRendererType2
                  blockStates={(() => {
                    const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
                    return Object.entries(blockStates).reduce((acc, [blockId, blockState]) => {
                      const cellsData = typeof blockState === "string"
                        ? JSON.parse(blockState)
                        : blockState;
                      acc[blockId] = cellsToLexical(cellsData);
                      return acc;
                    }, {} as Record<string, any>)
                  })()}
                  projectId={projectId}
                  storageAdapter={storageAdapter}
                  preferences={preferences}
                />
              ) : (
                <div className="py-8 text-center text-gray-500 dark:text-gray-400">
                  Empty slide
                </div>
              )}
            </div>
          </div>
        )
      })}

      {slides.length === 0 && (
        <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div className="p-12 text-center">
            <p className="text-gray-500 dark:text-gray-400">
              No slides in this slideshow
            </p>
          </div>
        </div>
      )}
    </div>
  )
}
