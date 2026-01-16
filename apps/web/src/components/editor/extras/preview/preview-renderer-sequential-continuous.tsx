"use client"

import { PreviewRenderer } from "./preview-renderer"
import { PreviewRendererType2 } from "./preview-renderer-type2"
import type { SequentialPanelStructure } from "@/lib/storage/editor/panel-structure"

interface PreviewRendererSequentialContinuousProps {
  structure: SequentialPanelStructure
  projectId: string
  projectName?: string
  storageAdapter?: {
    load: (id: string) => Promise<any>
  }
}

export function PreviewRendererSequentialContinuous({
  structure,
  projectId,
  projectName,
  storageAdapter,
}: PreviewRendererSequentialContinuousProps) {
  const sortedPanels = [...structure.panels].sort((a, b) => a.order - b.order)

  return (
    <div className="space-y-6">
      {sortedPanels.map((panel, index) => (
        <div
          key={panel.id}
          className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900"
        >
          {/* Panel Header */}
          {panel.name && (
            <div className="border-b border-gray-200 bg-gray-50 px-6 py-3 dark:border-gray-800 dark:bg-gray-800/50">
              <div className="flex items-center gap-3">
                <span className="flex h-7 w-7 items-center justify-center rounded bg-blue-100 text-sm font-semibold text-blue-700 dark:bg-blue-900/50 dark:text-blue-300">
                  {index + 1}
                </span>
                <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                  {panel.name}
                </h2>
              </div>
            </div>
          )}

          {/* Panel Content */}
          <div className="p-6">
            {panel.type === "single" && panel.blocks && Object.keys(panel.blocks).length > 0 ? (
              <div className="max-w-4xl mx-auto">
                <div className="sm:p-2 md:p-6">
                  <PreviewRenderer
                    serializedState={
                      typeof Object.values(panel.blocks)[0] === "string"
                        ? JSON.parse(Object.values(panel.blocks)[0] as string)
                        : Object.values(panel.blocks)[0]
                    }
                    projectId={projectId}
                    storageAdapter={storageAdapter}
                  />
                </div>
              </div>
            ) : panel.type === "multiple" && panel.blocks && Object.keys(panel.blocks).length >= 1 ? (
              <PreviewRendererType2
                blockStates={Object.entries(panel.blocks).reduce((acc, [blockId, blockState]) => {
                  acc[blockId] = typeof blockState === "string"
                    ? JSON.parse(blockState)
                    : blockState;
                  return acc;
                }, {} as Record<string, any>)}
                projectId={projectId}
                storageAdapter={storageAdapter}
              />
            ) : (
              <div className="py-8 text-center text-gray-500 dark:text-gray-400">
                Empty panel
              </div>
            )}
          </div>
        </div>
      ))}

      {sortedPanels.length === 0 && (
        <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
          <div className="p-12 text-center">
            <p className="text-gray-500 dark:text-gray-400">
              No panels in this sequential project
            </p>
          </div>
        </div>
      )}
    </div>
  )
}
