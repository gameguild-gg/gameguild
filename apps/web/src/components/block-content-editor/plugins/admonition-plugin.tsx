"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_ADMONITION_COMMAND } from "./floating-content-insert-plugin"
import { $createAdmonitionNode } from "../nodes/admonition-node"
import type { AdmonitionData } from "../nodes/admonition-node"

export function AdmonitionPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_ADMONITION_COMMAND,
      (payload: Partial<AdmonitionData> = {}) => {
        editor.update(() => {
          const admonitionNode = $createAdmonitionNode(payload)
          $insertNodes([admonitionNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
