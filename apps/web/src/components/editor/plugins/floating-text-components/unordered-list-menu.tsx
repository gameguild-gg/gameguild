"use client"

import { useCallback } from "react"
import { $getSelection, $isRangeSelection } from "lexical"
import { INSERT_UNORDERED_LIST_COMMAND } from "@lexical/list"
import { Check } from "lucide-react"
import {
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuItem,
} from "@/components/editor/ui/dropdown-menu"

interface UnorderedListMenuProps {
  editor: any
  currentListType: string
}

interface UnorderedListStyle {
  icon: string
  name: string
  style: string
}

const UNORDERED_LIST_STYLES: UnorderedListStyle[] = [
  { icon: "•", name: "Disc (default)", style: "list-style-type: disc;" },
]

export function UnorderedListMenu({ editor, currentListType }: UnorderedListMenuProps) {
  const handleUnorderedListClick = useCallback(
    (style: string) => {
      editor.dispatchCommand(INSERT_UNORDERED_LIST_COMMAND, undefined)
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          const nodes = selection.getNodes()
          nodes.forEach((node) => {
            const listNode = node.getTopLevelElementOrThrow()
            if (listNode.getType() === "list") {
              listNode.setStyle(style)
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
        <span className="mr-2">•</span>
        <span>Unordered Lists</span>
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent side="right" align="start" className="w-48">
        {UNORDERED_LIST_STYLES.map((listStyle, index) => (
          <DropdownMenuItem
            key={index}
            onClick={() => handleUnorderedListClick(listStyle.style)}
            onSelect={(e) => e.preventDefault()}
          >
            <span className="mr-2">{listStyle.icon}</span>
            <span>{listStyle.name}</span>
            {currentListType === "bullet" && index === 0 && <Check className="ml-auto h-4 w-4" />}
          </DropdownMenuItem>
        ))}
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
