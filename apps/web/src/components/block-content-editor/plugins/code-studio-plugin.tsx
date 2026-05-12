"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes, COMMAND_PRIORITY_EDITOR } from "lexical"
import { mergeRegister } from "@lexical/utils"
import { INSERT_CODE_STUDIO_COMMAND } from "./floating-content-insert-plugin"
import { $createCodeStudioNode, CodeStudioNode } from "@/components/block-content-editor/nodes/code-studio-node"
import type { CodeStudioMode } from "../extras/code-studio/types"

export function CodeStudioPlugin(): null {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([CodeStudioNode])) {
      throw new Error("CodeStudioPlugin: CodeStudioNode not registered on editor")
    }

    return mergeRegister(
      editor.registerCommand(
        INSERT_CODE_STUDIO_COMMAND,
        (mode: CodeStudioMode) => {
          const codeStudioNode = $createCodeStudioNode(mode)
          $insertNodes([codeStudioNode])
          return true
        },
        COMMAND_PRIORITY_EDITOR,
      ),
    )
  }, [editor])

  return null
}
