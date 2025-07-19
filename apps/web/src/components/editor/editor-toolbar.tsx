"use client"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $getRoot, $createParagraphNode } from "lexical"
import { ArrowUp } from "lucide-react"
import { PreviewPlugin } from "./plugins/preview-plugin"
import { Button } from "@/components/editor/ui/button"

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
    <div className="flex items-center gap-1 border-b p-1">
      <Button variant="ghost" size="icon" className="h-8 w-8" onClick={addNewLineAtTop} title="Add new line at top">
        <ArrowUp className="h-4 w-4" />
      </Button>
      <PreviewPlugin />
    </div>
  )
}
