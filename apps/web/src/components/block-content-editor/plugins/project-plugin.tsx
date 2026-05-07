"use client"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { useEffect } from "react"
import { COMMAND_PRIORITY_EDITOR, $getSelection, $isRangeSelection } from "lexical"
import { $createProjectNode, type ProjectData } from "../nodes/project-node"
import { INSERT_PROJECT_COMMAND } from "./floating-content-insert-plugin"
import { $insertNodeToNearestRoot } from "@lexical/utils"

export function ProjectPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    return editor.registerCommand(
      INSERT_PROJECT_COMMAND,
      (projectData: ProjectData) => {
        editor.update(() => {
          const selection = $getSelection()
          
          if (!$isRangeSelection(selection)) {
            return
          }

          const projectNode = $createProjectNode(projectData)
          $insertNodeToNearestRoot(projectNode)
        })

        return true
      },
      COMMAND_PRIORITY_EDITOR
    )
  }, [editor])

  return null
}
