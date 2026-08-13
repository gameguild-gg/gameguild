/**
 * AdmonitionPlugin — registers INSERT_ADMONITION_LEXICAL_COMMAND.
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
import type { AdmonitionType } from "./admonition"
import { $createAdmonitionLexicalNode, AdmonitionLexicalNode } from "./admonition-node"

export type AdmonitionPayload = {
  type?: AdmonitionType
  title?: string
  content?: string
}

export const INSERT_ADMONITION_LEXICAL_COMMAND: LexicalCommand<AdmonitionPayload | undefined> = createCommand(
  "INSERT_ADMONITION_LEXICAL_COMMAND",
)

export function AdmonitionPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([AdmonitionLexicalNode])) {
      throw new Error("AdmonitionPlugin: AdmonitionLexicalNode not registered on editor")
    }
    return editor.registerCommand<AdmonitionPayload | undefined>(
      INSERT_ADMONITION_LEXICAL_COMMAND,
      (payload) => {
        const node = $createAdmonitionLexicalNode(
          payload?.type ?? "note",
          payload?.title ?? "",
          payload?.content ?? "",
        )
        $insertNodes([node])
        if ($isRootOrShadowRoot(node.getParentOrThrow())) {
          $wrapNodeInElement(node, $createParagraphNode).selectEnd()
        }
        return true
      },
      COMMAND_PRIORITY_EDITOR,
    )
  }, [editor])

  return null
}
