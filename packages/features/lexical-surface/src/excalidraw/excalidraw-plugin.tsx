/**
 * ExcalidrawPlugin — registers `INSERT_EXCALIDRAW_COMMAND`. When invoked,
 * opens a modal; on save inserts a new `ExcalidrawNode` at the selection.
 *
 * Ported from `lexical-playground/src/plugins/ExcalidrawPlugin/index.tsx`.
 */
"use client"

import * as React from "react"
import { useEffect, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $wrapNodeInElement } from "@lexical/utils"
import {
  $createParagraphNode,
  $insertNodes,
  $isRootOrShadowRoot,
  COMMAND_PRIORITY_EDITOR,
  createCommand,
  type LexicalCommand,
} from "lexical"
import type { AppState, BinaryFiles } from "@excalidraw/excalidraw/types"
import { $createExcalidrawNode, ExcalidrawNode } from "./excalidraw-node"
import ExcalidrawModal, { type ExcalidrawInitialElements } from "./excalidraw-modal"

export const INSERT_EXCALIDRAW_COMMAND: LexicalCommand<void> = createCommand(
  "INSERT_EXCALIDRAW_COMMAND",
)

export function ExcalidrawPlugin(): React.JSX.Element | null {
  const [editor] = useLexicalComposerContext()
  const [isModalOpen, setModalOpen] = useState<boolean>(false)

  useEffect(() => {
    if (!editor.hasNodes([ExcalidrawNode])) {
      throw new Error("ExcalidrawPlugin: ExcalidrawNode not registered on editor")
    }
    return editor.registerCommand(
      INSERT_EXCALIDRAW_COMMAND,
      () => {
        setModalOpen(true)
        return true
      },
      COMMAND_PRIORITY_EDITOR,
    )
  }, [editor])

  const onClose = () => setModalOpen(false)
  const onDelete = () => setModalOpen(false)
  const onSave = (
    elements: ExcalidrawInitialElements,
    appState: Partial<AppState>,
    files: BinaryFiles,
  ) => {
    editor.update(() => {
      const node = $createExcalidrawNode()
      node.setData(JSON.stringify({ appState, elements, files }))
      $insertNodes([node])
      if ($isRootOrShadowRoot(node.getParentOrThrow())) {
        $wrapNodeInElement(node, $createParagraphNode).selectEnd()
      }
    })
    setModalOpen(false)
  }

  return isModalOpen ? (
    <ExcalidrawModal
      initialElements={[]}
      initialAppState={{} as AppState}
      initialFiles={{}}
      isShown={isModalOpen}
      onDelete={onDelete}
      onClose={onClose}
      onSave={onSave}
    />
  ) : null
}
