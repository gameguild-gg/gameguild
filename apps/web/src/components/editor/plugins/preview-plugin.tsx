"use client"

import { useCallback, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Eye } from "lucide-react"
import type { SerializedEditorState } from "lexical"

import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from "@/components/ui/dialog"
import { PreviewRenderer } from "@/components/editor/extras/preview/preview-renderer"

export function PreviewPlugin() {
  const [editor] = useLexicalComposerContext()
  const [isOpen, setIsOpen] = useState(false)
  const [serializedState, setSerializedState] = useState<SerializedEditorState | null>(null)

  const onClick = useCallback(() => {
    const editorState = editor.getEditorState()
    const serialized = editorState.toJSON()
    setSerializedState(serialized)
    setIsOpen(true)
  }, [editor])

  return (
    <>
      <Button variant="outline" size="sm" onClick={onClick} className="flex items-center gap-2 bg-transparent">
        <Eye className="h-4 w-4" />
        Preview
      </Button>

      <Dialog open={isOpen} onOpenChange={setIsOpen}>
        <DialogContent className="max-w-6xl max-h-[95vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Preview</DialogTitle>
            <DialogDescription>Preview of your content as it would appear to readers</DialogDescription>
          </DialogHeader>
          {serializedState && <PreviewRenderer serializedState={serializedState} />}
        </DialogContent>
      </Dialog>
    </>
  )
}
