"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_BUTTON_COMMAND } from "./floating-content-insert-plugin"
import { $createButtonNode } from "../nodes/button-node"
import type { ButtonData } from "../nodes/button-node"

export function ButtonPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_BUTTON_COMMAND,
      (payload: Partial<ButtonData> = {}) => {
        editor.update(() => {
          const buttonNode = $createButtonNode(payload)
          $insertNodes([buttonNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
