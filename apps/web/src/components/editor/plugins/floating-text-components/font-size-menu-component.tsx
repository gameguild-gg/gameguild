"use client"

import {
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { $getSelection, $isRangeSelection, type LexicalNode } from "lexical"
import { useCallback } from "react"

// Type extension for nodes that support style methods (like TextNode)
interface StylableNode extends LexicalNode {
  getStyle?: () => string;
  setStyle?: (style: string) => void;
}

interface FontSizeMenuComponentProps {
  editor: any
  currentFontSize: string
  setCurrentFontSize: (size: string) => void
}

const FONT_SIZES = [8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48, 64, 72, 96]

export function FontSizeMenuComponent({ editor, currentFontSize, setCurrentFontSize }: FontSizeMenuComponentProps) {
  const handleFontSizeChange = useCallback(
    (fontSize: string) => {
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          const nodes = selection.getNodes()
          nodes.forEach((node) => {
            const stylableNode = node as StylableNode
            if (node.getTextContent() && stylableNode.getStyle && stylableNode.setStyle) {
              const currentStyle = stylableNode.getStyle() || ""
              let newStyle = currentStyle.replace(/font-size:\s*[^;]+;?/g, "")
              newStyle += `font-size: ${fontSize};`
              stylableNode.setStyle(newStyle.trim())
            }
          })
        }
      })
    },
    [editor],
  )

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <span className="mr-2 h-4 w-4 text-center">Aa</span>
        <span>Size: {currentFontSize || "Default"}</span>
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent className="w-40">
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">
          Current: {currentFontSize || "Default"}
        </div>
        <DropdownMenuSeparator />
        {FONT_SIZES.map((size) => (
          <DropdownMenuItem
            key={size}
            onSelect={(e) => e.preventDefault()}
            onClick={() => {
              const newSize = `${size}px`
              setCurrentFontSize(newSize)
              handleFontSizeChange(newSize)
            }}
          >
            <span style={{ fontSize: `${Math.min(size, 16)}px` }}>{size}px</span>
          </DropdownMenuItem>
        ))}
        <DropdownMenuSeparator />
        <div className="p-2">
          <Label htmlFor="custom-font-size" className="sr-only">
            Custom Size
          </Label>
          <Input
            id="custom-font-size"
            type="number"
            placeholder="Custom (px)"
            value={Number.parseInt(currentFontSize) || ""}
            onChange={(e) => {
              const value = e.target.value
              if (value) {
                setCurrentFontSize(`${value}px`)
              } else {
                setCurrentFontSize("")
              }
            }}
            onBlur={() => {
              if (currentFontSize) {
                handleFontSizeChange(currentFontSize)
              }
            }}
            onKeyDown={(e) => {
              if (e.key === "Enter" && currentFontSize) {
                handleFontSizeChange(currentFontSize)
                e.preventDefault()
              }
              e.stopPropagation()
            }}
            className="w-full text-xs"
          />
        </div>
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
