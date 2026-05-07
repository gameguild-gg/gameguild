"use client"

import { useCallback } from "react"
import { $getSelection, $isRangeSelection } from "lexical"
import { $patchStyleText } from "@lexical/selection"
import { Palette } from "lucide-react"
import {
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import { ColorPalette } from "@/components/block-content-editor/extras/color-palette"

interface TextColorMenuComponentProps {
  editor: any
  currentTextColor: string
  setCurrentTextColor: (color: string) => void
}

export function TextColorMenuComponent({ editor, currentTextColor, setCurrentTextColor }: TextColorMenuComponentProps) {
  const handleTextColorChange = useCallback(
    (color: string) => {
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          // Use $patchStyleText to apply color only to selected text range
          $patchStyleText(selection, {
            color: color && color !== "transparent" && color !== "" ? color : null
          })
        }
      })
      setCurrentTextColor(color)
    },
    [editor, setCurrentTextColor],
  )

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <Palette className="mr-2 h-4 w-4" />
        <span>Text Color</span>
        <div
          className="ml-auto h-4 w-4 rounded-full border"
          style={{ backgroundColor: currentTextColor || "transparent" }}
        />
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent className="w-64">
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Text Color</div>
        <DropdownMenuSeparator />
        <ColorPalette
          selectedColor={currentTextColor}
          onColorChange={handleTextColorChange}
          customInputLabel="Custom:"
        />
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
