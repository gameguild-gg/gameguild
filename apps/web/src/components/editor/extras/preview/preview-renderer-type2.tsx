"use client"

import type { SerializedEditorState } from "lexical"
import type { ProjectPreferences, PanelData } from "@/lib/storage/editor/project-preferences"
import { AdvancedMultiBlockPreview } from "./multi-block-preview"

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
    <AdvancedMultiBlockPreview
      blockStates={blockStates}
      projectId={projectId}
      storageAdapter={storageAdapter}
      preferences={preferences}
      isEditable={true}
      onLayoutChange={onLayoutChange}
    />
  )
}

