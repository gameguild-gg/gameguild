"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_TABLE_COMMAND } from "./floating-content-insert-plugin"
import { $createTableNode } from "../nodes/table-node"
import type { TableData } from "../nodes/table-node"

export function TablePlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_TABLE_COMMAND,
      (payload: Partial<TableData> = {}) => {
        editor.update(() => {
          const tableNode = $createTableNode(payload)
          $insertNodes([tableNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
