import type { LexicalEditor } from "lexical"
import type React from "react"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"
import type { ProjectPreferences } from "@/lib/storage/editor/project-preferences"
import { type ProjectType} from "@/lib/storage/editor/project-types"
import { AdvancedMultiBlockEditor } from "./multi-block-editor"

interface EditorLayoutType2Props {
  blockRefs: React.MutableRefObject<Record<string, LexicalEditor | null>>
  blockStates: Record<string, string>
  onBlockChange: (blockId: string, state: string) => void
  onBlockAdd?: () => void
  onBlockRemove?: (blockId: string) => void
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId: string
  mode?: ProjectMode
  currentProjectType?: ProjectType
  storageAdapter?: any
  preferences?: ProjectPreferences
  onPreferencesChange?: (preferences: ProjectPreferences) => void
  currentProjectId?: string
  readOnly?: boolean
}

/**
 * Editor Layout Type 2: Multiple horizontal panels
 * This layout displays multiple editors side by side (b1, b2, b3...bN)
 */
export function EditorLayoutType2({
  blockRefs,
  blockStates,
  onBlockChange,
  onBlockAdd,
  onBlockRemove,
  onLoadingChange,
  projectId,
  mode = "free-page",
  currentProjectType,
  storageAdapter,
  preferences,
  onPreferencesChange,
  currentProjectId,
  readOnly = false,
}: EditorLayoutType2Props) {
  // Always use AdvancedMultiBlockEditor, even for 1 block
  return (
    <div className="h-[calc(100vh-12rem)] border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-900">
      <AdvancedMultiBlockEditor
        blockRefs={blockRefs}
        blockStates={blockStates}
        onBlockChange={onBlockChange}
        onBlockAdd={readOnly ? undefined : onBlockAdd}
        onBlockRemove={readOnly ? undefined : onBlockRemove}
        onLoadingChange={onLoadingChange}
        projectId={projectId}
        mode={mode}
        currentProjectType={currentProjectType}
        storageAdapter={storageAdapter}
        preferences={preferences}
        onPreferencesChange={readOnly ? undefined : onPreferencesChange}
        currentProjectId={currentProjectId}
        readOnly={readOnly}
      />
    </div>
  )
}