"use client"

import { useCallback, useRef, useState } from "react"
import { BlockArrayEditor } from "@/components/block-content-editor/engines/blocks/block-array-editor"
import { useEditor } from "./editor-provider"

export function EditorField() {
  const { project, history, fieldConfig } = useEditor()
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
      requestAnimationFrame(() => {
        wrapperRef.current?.scrollIntoView({ behavior: "auto", block: "nearest" })
      })
    }
  }, [])

  const hasRestrictedBlockTypes = !!fieldConfig.allowedBlockTypes && fieldConfig.allowedBlockTypes.length <= 1
  const isQuizMode = fieldConfig.allowedModes?.includes("quiz-page")
  const hideBlocks = hasRestrictedBlockTypes || (isQuizMode && !fieldConfig.allowedBlockTypes?.length)

  return (
    <div ref={wrapperRef} style={blocksDragging ? { height: scaledHeight ?? undefined } : undefined}>
      <div
        ref={fieldRef}
        className="border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 p-4 transition-transform duration-300 ease-in-out"
        style={blocksDragging ? { transform: "scale(0.5)", transformOrigin: "top center" } : undefined}
      >
        <BlockArrayEditor
          blocks={project.blocks}
          onChange={project.setBlocks}
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
