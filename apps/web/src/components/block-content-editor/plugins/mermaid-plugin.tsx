"use client"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodeToNearestRoot } from "@lexical/utils"
import { COMMAND_PRIORITY_EDITOR } from "lexical"
import { useEffect } from "react"
import type { JSX } from "react/jsx-runtime"

import { $createMermaidNode, MermaidNode, type MermaidData } from "../nodes/mermaid-node"
import { INSERT_MERMAID_COMMAND } from "./floating-content-insert-plugin"

export function MermaidPlugin(): JSX.Element | null {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([MermaidNode])) {
      throw new Error("MermaidPlugin: MermaidNode not registered on editor")
    }

    return editor.registerCommand<MermaidData>(
      INSERT_MERMAID_COMMAND,
      (payload) => {
        const mermaidNode = $createMermaidNode(payload)
        $insertNodeToNearestRoot(mermaidNode)
        return true
      },
      COMMAND_PRIORITY_EDITOR,
    )
  }, [editor])

  return null
}
