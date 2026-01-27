"use client"

import { PreviewRenderer } from "./preview-renderer"
import { PreviewRendererType2 } from "./preview-renderer-type2"
import type { SlideshowStructure } from "@/lib/storage/editor/slideshow-structure"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"

interface PreviewRendererSlideshowContinuousProps {
  structure: SlideshowStructure
  projectId: string
  projectName?: string
  storageAdapter?: {
    load: (id: string) => Promise<any>
  }
  preferences?: ProjectPreferences
}

export function PreviewRendererSlideshowContinuous({
  structure,
  projectId,
  projectName,
  storageAdapter,
  preferences,
}: PreviewRendererSlideshowContinuousProps) {
  const sortedSlides = [...structure.slides].sort((a, b) => a.order - b.order)

  return (
    <div className="space-y-6">
      {sortedSlides.map((slide, index) => (
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
            {slide.type === "single" && slide.blocks && Object.keys(slide.blocks).length > 0 ? (
              <div className="max-w-4xl mx-auto">
                <div className="sm:p-2 md:p-6">
                  <PreviewRenderer
                    serializedState={
                      typeof Object.values(slide.blocks)[0] === "string"
                        ? JSON.parse(Object.values(slide.blocks)[0] as string)
                        : Object.values(slide.blocks)[0]
                    }
                    projectId={projectId}
                    storageAdapter={storageAdapter}
                  />
                </div>
              </div>
            ) : slide.type === "multiple" && slide.blocks && Object.keys(slide.blocks).length >= 1 ? (
              <PreviewRendererType2
                blockStates={(() => {
                  const { cellsToLexical } = require("@/lib/storage/editor/cell-structure")
                  return Object.entries(slide.blocks).reduce((acc, [blockId, blockState]) => {
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
      ))}

      {sortedSlides.length === 0 && (
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
