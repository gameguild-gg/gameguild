"use client"

import type { SerializedEditorState } from "lexical"
import { PreviewRenderer } from "./preview-renderer"

interface PreviewRendererType2Props {
  leftState: SerializedEditorState
  rightState: SerializedEditorState
}

export function PreviewRendererType2({ leftState, rightState }: PreviewRendererType2Props) {
  return (
    <div className="flex flex-col lg:flex-row lg:gap-0 w-full">
      {/* Left Panel */}
      <div className="w-full lg:w-1/2 border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
        <div className="p-6 sm:p-8 md:p-12">
          <PreviewRenderer serializedState={leftState} />
        </div>
      </div>

      {/* Vertical Divider */}
      <div className="hidden lg:block w-px bg-gray-300 dark:bg-gray-700" />

      {/* Right Panel */}
      <div className="w-full lg:w-1/2 border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 lg:border-l-0">
        <div className="p-6 sm:p-8 md:p-12">
          <PreviewRenderer serializedState={rightState} />
        </div>
      </div>
    </div>
  )
}
