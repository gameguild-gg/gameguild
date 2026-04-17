"use client"

import type { SerializedEditorState } from "lexical"
import type { ProjectPreferences, PanelData } from "@/lib/storage/editor/project-preferences"
import { AdvancedMultiBlockPreview } from "@/components/editor/engines/lexical/multi-block-preview"

interface PreviewRendererType2Props {
  blockStates: Record<string, SerializedEditorState>
  projectId?: string
  storageAdapter?: {
    load: (id: string) => Promise<any>
  }
  preferences?: ProjectPreferences
  onLayoutChange?: (panels: PanelData[], direction: "horizontal" | "vertical") => void
}

export function PreviewRendererType2({ blockStates, projectId, storageAdapter, preferences, onLayoutChange }: PreviewRendererType2Props) {
  // Always use AdvancedMultiBlockPreview, even for 1 block
  return (
    <div className="h-[calc(100vh-12rem)] border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-900">
      <AdvancedMultiBlockPreview
        blockStates={blockStates}
        projectId={projectId}
        storageAdapter={storageAdapter}
        preferences={preferences}
        isEditable={true}
        onLayoutChange={onLayoutChange}
      />
    </div>
  )
}

