"use client"
/**
 * Main editor field component, used in both the doc editor and block editor.
 */
import { useCallback, useEffect, useRef, useState } from "react"
import { BlockArrayEditor } from "@/components/block-content-editor/engines/blocks/block-array-editor"
import { BLOCK_REGISTRY } from "@/components/block-content-editor/engines/blocks/block-component-registry"
import { nextBlockId } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { useEditor } from "./editor-provider"
import { LexicalSurface } from "@/components/block-content-editor/lexical-surface"

export function EditorField() {
  const { project, history, effectiveFieldConfig: fieldConfig } = useEditor()
  const [blocksDragging, setBlocksDragging] = useState(false)
  const [scaledHeight, setScaledHeight] = useState<number | null>(null)
  const fieldRef = useRef<HTMLDivElement>(null)
  const wrapperRef = useRef<HTMLDivElement>(null)

  // Mode "single block document": ensures exactly one block of
  // the allowed type (or rich-text by default). Automatically creates if
  // the list is empty, so the user opens directly in the
  // editor without needing to go through the block picker.
  useEffect(() => {
    if (!fieldConfig.singleBlockMode) return
    if (history.isViewingHistory) return
    if (project.blocks.length > 0) return
    const type = fieldConfig.allowedBlockTypes?.[0] ?? "rich-text"
    const config = BLOCK_REGISTRY[type]
    if (!config) return
    project.setBlocks([config.createEmpty(nextBlockId(project.blocks))])
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fieldConfig.singleBlockMode, project.blocks.length, history.isViewingHistory])

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
  const isQuizMode = fieldConfig.projectType === "quiz"
  const hideBlocks = hasRestrictedBlockTypes || (isQuizMode && !fieldConfig.allowedBlockTypes?.length)

  // Document Mode interception: fully unlocked LexicalSurface without block chrome
  const isDocumentMode = fieldConfig.projectType === "document" && project.blocks.length === 1 && project.blocks[0]?.type === "rich-text"

  if (isDocumentMode) {
    const block = project.blocks[0]!
    const data = block.data as any

    // mountKey ensures LexicalComposer re-mounts when a different project
    // is loaded (otherwise the old empty state stays).
    const documentMountKey = `${project.projectId ?? "new"}-${block.id}`

    return (
      <div className="border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-950 shadow-sm rounded-lg flex flex-col min-h-full">
        <LexicalSurface
          namespace="DocumentEditor"
          mountKey={documentMountKey}
          initialState={data?.content ?? null}
          readOnly={history.isViewingHistory}
          onChange={(content) => {
            const next = [...project.blocks]
            next[0] = { ...block, data: { ...data, content } }
            project.setBlocks(next)
          }}
          placeholder="Start writing your document..."
          className="flex-1 flex flex-col"
          contentClassName="min-h-[600px] max-w-none px-8 py-10"
        />
      </div>
    )
  }

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
          singleBlockMode={fieldConfig.singleBlockMode}
          onDragStateChange={handleDragStateChange}
        />
      </div>
    </div>
  )
}
