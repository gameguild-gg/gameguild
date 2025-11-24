"use client"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $getRoot, $createParagraphNode } from "lexical"
import { ArrowUp } from "lucide-react"
import { PreviewPlugin } from "./plugins/preview-plugin"
import { Button } from "@/components/ui/button"

export function EditorToolbar() {
  const [editor] = useLexicalComposerContext()

  const addNewLineAtTop = () => {
    editor.update(() => {
      const root = $getRoot()
      const firstChild = root.getFirstChild()
      const newParagraph = $createParagraphNode()

      if (firstChild) {
        firstChild.insertBefore(newParagraph)
      } else {
        root.append(newParagraph)
      }
    })
  }

  return (
    <div className="rounded-t-lg flex items-center justify-center gap-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 p-2">
      <Button 
        variant="ghost" 
        size="sm" 
        className="h-8 gap-2 hover:bg-gray-100 dark:hover:bg-gray-800" 
        onClick={addNewLineAtTop} 
        title="Add new line at top"
      >
        <ArrowUp className="h-4 w-4" />
      </Button>
      <PreviewPlugin />
    </div>
  )
}
