"use client"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodeToNearestRoot } from "@lexical/utils"
import { COMMAND_PRIORITY_EDITOR } from "lexical"
import { useEffect } from "react"
import type { JSX } from "react/jsx-runtime"

import { $createMarkdownNode, MarkdownNode, type MarkdownData } from "../nodes/markdown-node"
import { INSERT_MARKDOWN_COMMAND } from "./floating-content-insert-plugin"

export function MarkdownPlugin(): JSX.Element | null {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([MarkdownNode])) {
      throw new Error("MarkdownPlugin: MarkdownNode not registered on editor")
    }

    return editor.registerCommand<MarkdownData>(
      INSERT_MARKDOWN_COMMAND,
      (payload) => {
        const markdownNode = $createMarkdownNode(payload)
        $insertNodeToNearestRoot(markdownNode)
        return true
      },
      COMMAND_PRIORITY_EDITOR,
    )
  }, [editor])

  return null
}
