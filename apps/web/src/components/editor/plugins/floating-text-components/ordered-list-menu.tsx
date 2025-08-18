"use client"

import { useCallback } from "react"
import { $getSelection, $isRangeSelection } from "lexical"
import { INSERT_ORDERED_LIST_COMMAND } from "@lexical/list"
import { Check } from "lucide-react"
import {
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu"

interface OrderedListMenuProps {
  editor: any
  currentListType: string
}

interface OrderedListStyle {
  icon: string
  name: string
  style: string
}

const ORDERED_LIST_STYLES: OrderedListStyle[] = [
  { icon: "1.", name: "Numbers (default)", style: "list-style-type: decimal;" },
]

export function OrderedListMenu({ editor, currentListType }: OrderedListMenuProps) {
  const handleOrderedListClick = useCallback(
    (style: string) => {
      editor.dispatchCommand(INSERT_ORDERED_LIST_COMMAND, undefined)
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
        <span className="mr-2">1.</span>
        <span>Ordered Lists</span>
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent side="right" align="start" className="w-48">
        {ORDERED_LIST_STYLES.map((listStyle, index) => (
          <DropdownMenuItem
            key={index}
            onClick={() => handleOrderedListClick(listStyle.style)}
            onSelect={(e) => e.preventDefault()}
          >
            <span className="mr-2">{listStyle.icon}</span>
            <span>{listStyle.name}</span>
            {currentListType === "number" && index === 0 && <Check className="ml-auto h-4 w-4" />}
          </DropdownMenuItem>
        ))}
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
