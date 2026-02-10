"use client"

import { useState } from "react"
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from "lucide-react"
import { Button } from "@/components/ui/button"
import { PreviewRendererType2 } from "./preview-renderer-type2"
import type { SlideshowStructure } from "@/lib/storage/editor/slideshow-structure"
import { getDependentProject } from "@/lib/storage/editor/slideshow-structure"
import type { ProjectData } from "@/lib/storage/editor/enhanced-storage-adapter"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"

interface PreviewRendererSlideshowSlideProps {
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

export function PreviewRendererSlideshowSlide({
  structure,
  projectId,
  projectName,
  deps,
  resolvedProjects,
  storageAdapter,
  preferences,
}: PreviewRendererSlideshowSlideProps) {
  const slides = structure.slides
  const [currentIndex, setCurrentIndex] = useState(0)

  const currentSlide = slides[currentIndex]
  const totalSlides = slides.length

  const goToFirst = () => setCurrentIndex(0)
  const goToPrevious = () => setCurrentIndex(Math.max(0, currentIndex - 1))
  const goToNext = () => setCurrentIndex(Math.min(totalSlides - 1, currentIndex + 1))
  const goToLast = () => setCurrentIndex(totalSlides - 1)

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

  if (totalSlides === 0) {
    return (
      <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
        <div className="p-12 text-center">
          <p className="text-gray-500 dark:text-gray-400">
            No slides in this slideshow
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Navigation Controls */}
      <div className="flex items-center justify-between border border-gray-200 bg-white px-4 py-3 dark:border-gray-800 dark:bg-gray-900">
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={goToFirst}
            disabled={currentIndex === 0}
            className="gap-1"
          >
            <ChevronsLeft className="h-4 w-4" />
            First
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={goToPrevious}
            disabled={currentIndex === 0}
            className="gap-1"
          >
            <ChevronLeft className="h-4 w-4" />
            Previous
          </Button>
        </div>

        <div className="flex items-center gap-3">
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Slide {currentIndex + 1} of {totalSlides}
          </span>
          {currentSlide?.name && (
            <span className="text-sm text-gray-500 dark:text-gray-400">
              • {currentSlide.name}
            </span>
          )}
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={goToNext}
            disabled={currentIndex === totalSlides - 1}
            className="gap-1"
          >
            Next
            <ChevronRight className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={goToLast}
            disabled={currentIndex === totalSlides - 1}
            className="gap-1"
          >
            Last
            <ChevronsRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Current Slide Content */}
      {currentSlide && (() => {
        const blockStates = getSlideBlockStates(currentSlide)
        return (
        <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
          {/* Slide Header */}
          {currentSlide.name && (
            <div className="border-b border-gray-200 bg-gray-50 px-6 py-3 dark:border-gray-800 dark:bg-gray-800/50">
              <div className="flex items-center gap-3">
                <span className="flex h-7 w-7 items-center justify-center rounded bg-blue-100 text-sm font-semibold text-blue-700 dark:bg-blue-900/50 dark:text-blue-300">
                  {currentIndex + 1}
                </span>
                <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                  {currentSlide.name}
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
      })()}

      {/* Quick Navigation Dots */}
      {totalSlides > 1 && (
        <div className="flex items-center justify-center gap-2 py-4">
          {slides.map((slide, index) => (
            <button
              key={slide.id}
              onClick={() => setCurrentIndex(index)}
              className={`h-2 rounded-full transition-all ${
                index === currentIndex
                  ? "w-8 bg-blue-600 dark:bg-blue-400"
                  : "w-2 bg-gray-300 hover:bg-gray-400 dark:bg-gray-600 dark:hover:bg-gray-500"
              }`}
              title={slide.name || `Slide ${index + 1}`}
            />
          ))}
        </div>
      )}
    </div>
  )
}
