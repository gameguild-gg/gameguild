/**
 * ExcalidrawComponent — rendered by `ExcalidrawNode.decorate()`.
 *
 * Click-to-select via Lexical `CLICK_COMMAND`, double-click to re-open
 * the editor modal. Resizer/caption from the playground is intentionally
 * omitted to keep this surface lean.
 */
"use client"

import * as React from "react"
import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { useLexicalEditable } from "@lexical/react/useLexicalEditable"
import { useLexicalNodeSelection } from "@lexical/react/useLexicalNodeSelection"
import { mergeRegister } from "@lexical/utils"
import {
  $getNodeByKey,
  CLICK_COMMAND,
  COMMAND_PRIORITY_LOW,
  isDOMNode,
  type NodeKey,
} from "lexical"
import { cn } from "@/lib/utils"
import type { AppState, BinaryFiles } from "@excalidraw/excalidraw/types"
import ExcalidrawImage from "./excalidraw-image"
import ExcalidrawModal, { type ExcalidrawInitialElements } from "./excalidraw-modal"
import { $isExcalidrawNode } from "./excalidraw-node"

export default function ExcalidrawComponent({
  nodeKey,
  data,
  width,
  height,
}: {
  nodeKey: NodeKey
  data: string
  width: "inherit" | number
  height: "inherit" | number
}): React.JSX.Element {
  const [editor] = useLexicalComposerContext()
  const isEditable = useLexicalEditable()
  const [isModalOpen, setModalOpen] = useState<boolean>(
    data === "[]" && editor.isEditable(),
  )
  const imageContainerRef = useRef<HTMLDivElement | null>(null)
  const buttonRef = useRef<HTMLButtonElement | null>(null)
  const [isSelected, setSelected, clearSelection] = useLexicalNodeSelection(nodeKey)

  useEffect(() => {
    if (!isEditable) {
      if (isSelected) clearSelection()
      return
    }
    return mergeRegister(
      editor.registerCommand(
        CLICK_COMMAND,
        (event: MouseEvent) => {
          const buttonElem = buttonRef.current
          const eventTarget = event.target
          if (
            buttonElem !== null &&
            isDOMNode(eventTarget) &&
            buttonElem.contains(eventTarget)
          ) {
            if (!event.shiftKey) clearSelection()
            setSelected(!isSelected)
            if (event.detail > 1) setModalOpen(true)
            return true
          }
          return false
        },
        COMMAND_PRIORITY_LOW,
      ),
    )
  }, [clearSelection, editor, isSelected, setSelected, isEditable])

  const deleteNode = useCallback(() => {
    setModalOpen(false)
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node) node.remove()
    })
  }, [editor, nodeKey])

  const setData = (
    els: ExcalidrawInitialElements,
    aps: Partial<AppState>,
    fls: BinaryFiles,
  ) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isExcalidrawNode(node)) {
        if ((els && els.length > 0) || Object.keys(fls).length > 0) {
          node.setData(JSON.stringify({ appState: aps, elements: els, files: fls }))
        } else {
          node.remove()
        }
      }
    })
  }

  const { elements = [], files = {}, appState = {} } = useMemo(() => {
    try {
      return JSON.parse(data)
    } catch {
      return { elements: [], files: {}, appState: {} }
    }
  }, [data])

  const closeModal = useCallback(() => {
    setModalOpen(false)
    if (elements.length === 0) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node) node.remove()
      })
    }
  }, [editor, nodeKey, elements.length])

  return (
    <>
      {isEditable && isModalOpen && (
        <ExcalidrawModal
          initialElements={elements}
          initialFiles={files}
          initialAppState={appState as AppState}
          isShown={isModalOpen}
          onDelete={deleteNode}
          onClose={closeModal}
          onSave={(els, aps, fls) => {
            setData(els, aps, fls)
            setModalOpen(false)
          }}
        />
      )}
      {elements.length > 0 && (
        <button
          ref={buttonRef}
          type="button"
          className={cn(
            "inline-block align-baseline border-2 rounded",
            isSelected
              ? "border-blue-500"
              : "border-transparent hover:border-gray-300 dark:hover:border-gray-700",
          )}
        >
          <ExcalidrawImage
            imageContainerRef={imageContainerRef}
            elements={elements}
            files={files}
            appState={appState as AppState}
            width={width}
            height={height}
          />
        </button>
      )}
    </>
  )
}
