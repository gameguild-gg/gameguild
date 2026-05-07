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
import { isNodeAllowed, type ProjectMode, type NodeRestrictions } from "@/components/block-content-editor/lib/storage/editor/project-modes"

export const INSERT_NODE_COMMAND: LexicalCommand<{ nodeType: string }> = createCommand()

interface NodeValidationPluginProps {
  mode: ProjectMode
  blockId?: string  // Block identifier (b1, b2, b3, etc.)
  panelId?: string  // Panel identifier (panel-1, panel-2, etc.)
  customRestrictions?: NodeRestrictions  // Custom project-specific restrictions
}

export function NodeValidationPlugin({ mode, blockId, panelId, customRestrictions }: NodeValidationPluginProps) {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    // If no blockId specified or free-page mode with no custom restrictions, allow everything
    if (!blockId || (mode === "free-page" && !customRestrictions)) {
      return
    }

    // Register command listener for node insertion
    const removeListener = editor.registerCommand(
      INSERT_NODE_COMMAND,
      (payload) => {
        const { nodeType } = payload
        
        // Check if node is allowed (considering panel and custom restrictions)
        if (!isNodeAllowed(nodeType, blockId, mode, customRestrictions)) {
          // Get friendly names for nodes
          const nodeFriendlyNames: Record<string, string> = {
            "code-studio": "Code Studio",
            "quiz": "Quiz"
          }
          
          const friendlyName = nodeFriendlyNames[nodeType] || nodeType
          const blockName = `block ${blockId}`
          
          // Show error toast
          toast.error(`Cannot insert ${friendlyName}`, {
            description: `${friendlyName} nodes are not allowed in ${blockName} for this project mode.`,
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
  }, [editor, mode, blockId, panelId, customRestrictions])

  return null
}
