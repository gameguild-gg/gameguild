"use client"

import { useState } from "react"
import { ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import { BlockArrayEditor } from "@/components/editor/engines/blocks/block-array-editor"
import { EditorLayoutType1 } from "@/components/editor/engines/lexical/editor-layout-type1"
import { EngineChooser } from "./engine-chooser"
import { useEditor } from "./editor-provider"

export function EditorField() {
  const { project, history, fieldConfig } = useEditor()
  const [engineChosen, setEngineChosen] = useState(false)

  // Show engine chooser when no project is loaded, user hasn't chosen yet, and multiple engines are available
  const showChooser = !engineChosen && !project.projectId && fieldConfig.engines.length > 1
  if (showChooser) {
    return (
      <EngineChooser
        engines={fieldConfig.engines}
        onChoose={(engine) => {
          project.setEngine(engine)
          setEngineChosen(true)
        }}
      />
    )
  }

  if (project.engine === ENGINE_TYPES.BLOCKS) {
    const noBlockTypes = !fieldConfig.allowedBlockTypes || fieldConfig.allowedBlockTypes.length <= 1
    const isQuizMode = fieldConfig.allowedModes?.includes("quiz-page")
    const hideBlocks = noBlockTypes || (isQuizMode && !fieldConfig.allowedBlockTypes?.length)
    return (
      <div className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 p-4">
        <BlockArrayEditor
          blocks={project.blockArrayBlocks}
          onChange={project.setBlockArrayBlocks}
          readOnly={history.isViewingHistory}
          allowedBlockTypes={fieldConfig.allowedBlockTypes}
          defaultPickerTab={hideBlocks || isQuizMode ? "templates" : "blocks"}
          hideBlockTypesTab={hideBlocks}
        />
      </div>
    )
  }

  return (
    <EditorLayoutType1
      editorRef={project.editorRef}
      editorState={project.editorState}
      onEditorChange={project.setEditorState}
      onLoadingChange={(setLoading) => {
        project.setLoadingRef.current = setLoading
      }}
      projectId={project.projectId}
      mode={project.projectMode}
      storageAdapter={project.storageAdapter}
      readOnly={history.isViewingHistory}
    />
  )
}
