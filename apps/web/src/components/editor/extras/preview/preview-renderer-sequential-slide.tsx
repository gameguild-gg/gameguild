"use client"

import { useState } from "react"
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from "lucide-react"
import { Button } from "@/components/ui/button"
import { PreviewRenderer } from "./preview-renderer"
import { PreviewRendererType2 } from "./preview-renderer-type2"
import type { SequentialPanelStructure } from "@/lib/storage/editor/panel-structure"

interface PreviewRendererSequentialSlideProps {
  structure: SequentialPanelStructure
  projectId: string
  projectName?: string
}

export function PreviewRendererSequentialSlide({
  structure,
  projectId,
  projectName,
}: PreviewRendererSequentialSlideProps) {
  const sortedPanels = [...structure.panels].sort((a, b) => a.order - b.order)
  const [currentIndex, setCurrentIndex] = useState(0)

  const currentPanel = sortedPanels[currentIndex]
  const totalPanels = sortedPanels.length

  const goToFirst = () => setCurrentIndex(0)
  const goToPrevious = () => setCurrentIndex(Math.max(0, currentIndex - 1))
  const goToNext = () => setCurrentIndex(Math.min(totalPanels - 1, currentIndex + 1))
  const goToLast = () => setCurrentIndex(totalPanels - 1)

  if (totalPanels === 0) {
    return (
      <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
        <div className="p-12 text-center">
          <p className="text-gray-500 dark:text-gray-400">
            No panels in this sequential project
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
            Panel {currentIndex + 1} of {totalPanels}
          </span>
          {currentPanel?.name && (
            <span className="text-sm text-gray-500 dark:text-gray-400">
              • {currentPanel.name}
            </span>
          )}
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={goToNext}
            disabled={currentIndex === totalPanels - 1}
            className="gap-1"
          >
            Next
            <ChevronRight className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={goToLast}
            disabled={currentIndex === totalPanels - 1}
            className="gap-1"
          >
            Last
            <ChevronsRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Current Panel Content */}
      {currentPanel && (
        <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
          {/* Panel Header */}
          {currentPanel.name && (
            <div className="border-b border-gray-200 bg-gray-50 px-6 py-3 dark:border-gray-800 dark:bg-gray-800/50">
              <div className="flex items-center gap-3">
                <span className="flex h-7 w-7 items-center justify-center rounded bg-blue-100 text-sm font-semibold text-blue-700 dark:bg-blue-900/50 dark:text-blue-300">
                  {currentIndex + 1}
                </span>
                <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                  {currentPanel.name}
                </h2>
              </div>
            </div>
          )}

          {/* Panel Content */}
          <div className="p-6">
            {currentPanel.type === "single" && currentPanel.state ? (
              <div className="max-w-4xl mx-auto">
                <div className="sm:p-2 md:p-6">
                  <PreviewRenderer
                    serializedState={
                      typeof currentPanel.state === "string"
                        ? JSON.parse(currentPanel.state)
                        : currentPanel.state
                    }
                    projectId={projectId}
                  />
                </div>
              </div>
            ) : currentPanel.type === "dual" && currentPanel.left && currentPanel.right ? (
              <PreviewRendererType2
                leftState={
                  typeof currentPanel.left === "string"
                    ? JSON.parse(currentPanel.left)
                    : currentPanel.left
                }
                rightState={
                  typeof currentPanel.right === "string"
                    ? JSON.parse(currentPanel.right)
                    : currentPanel.right
                }
                projectId={projectId}
              />
            ) : (
              <div className="py-8 text-center text-gray-500 dark:text-gray-400">
                Empty panel
              </div>
            )}
          </div>
        </div>
      )}

      {/* Quick Navigation Dots */}
      {totalPanels > 1 && (
        <div className="flex items-center justify-center gap-2 py-4">
          {sortedPanels.map((panel, index) => (
            <button
              key={panel.id}
              onClick={() => setCurrentIndex(index)}
              className={`h-2 rounded-full transition-all ${
                index === currentIndex
                  ? "w-8 bg-blue-600 dark:bg-blue-400"
                  : "w-2 bg-gray-300 hover:bg-gray-400 dark:bg-gray-600 dark:hover:bg-gray-500"
              }`}
              title={panel.name || `Panel ${index + 1}`}
            />
          ))}
        </div>
      )}
    </div>
  )
}
