"use client"

import { useCallback } from "react"
import { $getSelection, $isRangeSelection } from "lexical"
import { $patchStyleText } from "@lexical/selection"
import { Paintbrush } from "lucide-react"
import {
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import { ColorPalette } from "@/components/editor/extras/color-palette"

interface BackgroundColorMenuComponentProps {
  editor: any
  currentBackgroundColor: string
  setCurrentBackgroundColor: (color: string) => void
}

export function BackgroundColorMenuComponent({
  editor,
  currentBackgroundColor,
  setCurrentBackgroundColor,
}: BackgroundColorMenuComponentProps) {
  const handleBackgroundColorChange = useCallback(
    (color: string) => {
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          // Use $patchStyleText to apply background-color only to selected text range
          $patchStyleText(selection, {
            'background-color': color && color !== "transparent" && color !== "" ? color : null
          })
        }
      })
      setCurrentBackgroundColor(color)
    },
    [editor, setCurrentBackgroundColor],
  )

  const handleRemoveBackground = useCallback(() => {
    editor.update(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        // Use $patchStyleText to remove background-color only from selected text range
        $patchStyleText(selection, {
          'background-color': null
        })
      }
    })
    setCurrentBackgroundColor("")
  }, [editor, setCurrentBackgroundColor])

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <Paintbrush className="mr-2 h-4 w-4" />
        <span>Background Color</span>
        <div
          className="ml-auto h-4 w-4 rounded-full border"
          style={{
            backgroundColor: currentBackgroundColor || "transparent",
            backgroundImage: currentBackgroundColor
              ? "none"
              : "linear-gradient(45deg, #ccc 25%, transparent 25%), linear-gradient(-45deg, #ccc 25%, transparent 25%), linear-gradient(45deg, transparent 75%, #ccc 75%), linear-gradient(-45deg, transparent 75%, #ccc 75%)",
            backgroundSize: currentBackgroundColor ? "auto" : "4px 4px",
            backgroundPosition: currentBackgroundColor ? "auto" : "0 0, 0 2px, 2px -2px, -2px 0px",
          }}
        />
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent className="w-64">
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Background Color</div>
        <DropdownMenuSeparator />
        <div className="p-2">
          <button
            onClick={handleRemoveBackground}
            className="w-full mb-2 px-3 py-2 text-sm border rounded hover:bg-accent transition-colors"
          >
            Background Remove
          </button>
        </div>
        <ColorPalette
          selectedColor={currentBackgroundColor}
          onColorChange={handleBackgroundColorChange}
          customInputLabel="Custom:"
        />
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
