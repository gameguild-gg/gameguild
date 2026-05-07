"use client"

import { useState, useCallback, useRef } from "react"
import { ENGINE_TYPES } from "@/lib/storage/editor/project-types"
import { BlockArrayEditor } from "@/components/block-content-editor/engines/blocks/block-array-editor"
import { EditorLayoutType1 } from "@/components/block-content-editor/engines/lexical/editor-layout-type1"
import { EngineChooser } from "./engine-chooser"
import { useEditor } from "./editor-provider"

export function EditorField() {
  const { project, history, fieldConfig } = useEditor()
  const [engineChosen, setEngineChosen] = useState(false)
  const [blocksDragging, setBlocksDragging] = useState(false)
  const [scaledHeight, setScaledHeight] = useState<number | null>(null)
  const fieldRef = useRef<HTMLDivElement>(null)
  const wrapperRef = useRef<HTMLDivElement>(null)
  const handleDragStateChange = useCallback((dragging: boolean) => {
    if (dragging && fieldRef.current) {
      setScaledHeight(fieldRef.current.offsetHeight * 0.5)
    }
    setBlocksDragging(dragging)
    if (!dragging) {
      setScaledHeight(null)
    } else {
      // After scale, scroll the field into view so user doesn't see blank space
      requestAnimationFrame(() => {
        wrapperRef.current?.scrollIntoView({ behavior: "auto", block: "nearest" })
      })
    }
  }, [])

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
    const hasRestrictedBlockTypes = fieldConfig.allowedBlockTypes && fieldConfig.allowedBlockTypes.length <= 1
    const isQuizMode = fieldConfig.allowedModes?.includes("quiz-page")
    const hideBlocks = hasRestrictedBlockTypes || (isQuizMode && !fieldConfig.allowedBlockTypes?.length)
    return (
      <div ref={wrapperRef} style={blocksDragging ? { height: scaledHeight ?? undefined } : undefined}>
        <div
          ref={fieldRef}
          className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 p-4 transition-transform duration-300 ease-in-out"
          style={blocksDragging ? { transform: 'scale(0.5)', transformOrigin: 'top center' } : undefined}
        >
          <BlockArrayEditor
            blocks={project.blockArrayBlocks}
            onChange={project.setBlockArrayBlocks}
            readOnly={history.isViewingHistory}
            allowedBlockTypes={fieldConfig.allowedBlockTypes}
            defaultPickerTab={hideBlocks || isQuizMode ? "templates" : "blocks"}
            hideBlockTypesTab={hideBlocks}
            onDragStateChange={handleDragStateChange}
          />
        </div>
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
