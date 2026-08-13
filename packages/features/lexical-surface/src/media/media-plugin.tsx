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
import { $createMediaLexicalNode, MediaLexicalNode } from "./media-node"
import type { MediaType } from "@game-guild/lexical-surface/nodes/base/media-node-base"

export const INSERT_MEDIA_LEXICAL_COMMAND: LexicalCommand<{
  mediaType: MediaType
  src?: string
}> = createCommand("INSERT_MEDIA_LEXICAL_COMMAND")

export function MediaPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([MediaLexicalNode])) {
      throw new Error("MediaPlugin: MediaLexicalNode not registered on editor")
    }

    return editor.registerCommand<{ mediaType: MediaType; src?: string }>(
      INSERT_MEDIA_LEXICAL_COMMAND,
      (payload) => {
        editor.update(() => {
          const node = $createMediaLexicalNode(payload.mediaType, payload.src || "")
          $insertNodes([node])
          if ($isRootOrShadowRoot(node.getParentOrThrow())) {
            $wrapNodeInElement(node, $createParagraphNode).selectEnd()
          }
        })
        return true
      },
      COMMAND_PRIORITY_EDITOR,
    )
  }, [editor])

  return null
}
