/**
 * Node Validation Plugin
 * 
 * Validates and restricts node insertion based on project mode and panel
 * Prevents users from inserting restricted nodes in code-page and quiz-page modes
 */

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { useEffect } from "react"
import { 
  $getSelection, 
  $isRangeSelection,
  COMMAND_PRIORITY_HIGH,
  INSERT_PARAGRAPH_COMMAND,
  type LexicalCommand,
  createCommand
} from "lexical"
import { toast } from "sonner"
import { isNodeAllowed, type ProjectMode } from "@/lib/storage/editor/project-modes"

export const INSERT_NODE_COMMAND: LexicalCommand<{ nodeType: string }> = createCommand()

interface NodeValidationPluginProps {
  mode: ProjectMode
  panel?: "left" | "right"  // Only for type2 layouts
}

export function NodeValidationPlugin({ mode, panel }: NodeValidationPluginProps) {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    // If no panel specified or free-page mode, allow everything
    if (!panel || mode === "free-page") {
      return
    }

    // Register command listener for node insertion
    const removeListener = editor.registerCommand(
      INSERT_NODE_COMMAND,
      (payload) => {
        const { nodeType } = payload
        
        // Check if node is allowed
        if (!isNodeAllowed(nodeType, panel, mode)) {
          // Get friendly names for nodes
          const nodeFriendlyNames: Record<string, string> = {
            "code-studio": "Code Studio",
            "quiz": "Quiz"
          }
          
          const friendlyName = nodeFriendlyNames[nodeType] || nodeType
          const panelName = panel === "left" ? "left panel" : "right panel"
          
          // Show error toast
          toast.error(`Cannot insert ${friendlyName}`, {
            description: `${friendlyName} nodes are not allowed in the ${panelName} for this project mode.`,
            duration: 3000,
            icon: "🚫"
          })
          
          return true // Command handled, prevent default behavior
        }
        
        return false // Allow insertion
      },
      COMMAND_PRIORITY_HIGH
    )

    return () => {
      removeListener()
    }
  }, [editor, mode, panel])

  return null
}
