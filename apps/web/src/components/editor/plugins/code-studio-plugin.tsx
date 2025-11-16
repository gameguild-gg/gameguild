"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes, COMMAND_PRIORITY_EDITOR } from "lexical"
import { mergeRegister } from "@lexical/utils"
import { INSERT_CODE_STUDIO_COMMAND } from "./floating-content-insert-plugin"
import { $createCodeStudioNode, CodeStudioNode } from "@/components/editor/nodes/code-studio-node"

export function CodeStudioPlugin(): null {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([CodeStudioNode])) {
      throw new Error("CodeStudioPlugin: CodeStudioNode not registered on editor")
    }

    return mergeRegister(
      editor.registerCommand(
        INSERT_CODE_STUDIO_COMMAND,
        () => {
          const codeStudioNode = $createCodeStudioNode()
          $insertNodes([codeStudioNode])
          return true
        },
        COMMAND_PRIORITY_EDITOR,
      ),
    )
  }, [editor])

  return null
}
