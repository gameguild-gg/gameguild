/**
 * StickyPlugin — registers INSERT_STICKY_COMMAND.
 */
"use client"

import { useEffect } from "react"
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
import { $createStickyNode, StickyNode, StickyStyle } from "./sticky-node"

export type StickyPayload = {
  text?: string
  color?: string
}

export const INSERT_STICKY_COMMAND: LexicalCommand<StickyPayload | undefined> = createCommand(
  "INSERT_STICKY_COMMAND"
)

export function StickyPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([StickyNode])) {
      throw new Error("StickyPlugin: StickyNode not registered on editor")
    }
    return editor.registerCommand<StickyPayload | undefined>(
      INSERT_STICKY_COMMAND,
      (payload) => {
        const stickyNode = $createStickyNode(payload?.text ?? "", payload?.color ?? "yellow")
        $insertNodes([stickyNode])
        if ($isRootOrShadowRoot(stickyNode.getParentOrThrow())) {
          $wrapNodeInElement(stickyNode, $createParagraphNode).selectEnd()
        }
        return true
      },
      COMMAND_PRIORITY_EDITOR
    )
  }, [editor])

  return null
}
