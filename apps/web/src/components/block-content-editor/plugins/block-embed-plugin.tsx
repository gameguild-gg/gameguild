"use client"

/**
 * Lexical plugin that registers the `INSERT_BLOCK_COMMAND` used by the
 * slash-menu and "+" button to insert a `BlockEmbedNode` into the current
 * editor surface.
 */

import { useEffect } from "react"
import {
  COMMAND_PRIORITY_EDITOR,
  createCommand,
  type LexicalCommand,
} from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodeToNearestRoot } from "@lexical/utils"

import { $createBlockEmbedNode, BlockEmbedNode } from "../nodes/block-embed-node"
import type { EmbeddableBlock } from "../embed/types"

export const INSERT_BLOCK_COMMAND: LexicalCommand<EmbeddableBlock> = createCommand(
  "INSERT_BLOCK_COMMAND",
)

export function BlockEmbedPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([BlockEmbedNode])) {
      console.error(
        "[BlockEmbedPlugin] BlockEmbedNode is not registered on this editor. " +
          "Add it to the `nodes` array of LexicalComposer initialConfig.",
      )
      return
    }

    return editor.registerCommand<EmbeddableBlock>(
      INSERT_BLOCK_COMMAND,
      (block) => {
        const node = $createBlockEmbedNode(block)
        $insertNodeToNearestRoot(node)
        return true
      },
      COMMAND_PRIORITY_EDITOR,
    )
  }, [editor])

  return null
}
