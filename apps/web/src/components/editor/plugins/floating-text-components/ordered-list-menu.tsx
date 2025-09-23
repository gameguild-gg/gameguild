"use client"

import { useCallback } from "react"
import { $getSelection, $isRangeSelection, $createTextNode } from "lexical"
import { $createListItemNode } from "@lexical/list"
import { $createCustomListNode } from "@/components/editor/nodes/custom-list-node"
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
  { icon: "1.", name: "Numbers (default)", style: "decimal" },
  { icon: "A.", name: "Uppercase Letters", style: "upper-alpha" },
  { icon: "a.", name: "Lowercase Letters", style: "lower-alpha" },
]

export function OrderedListMenu({ editor, currentListType }: OrderedListMenuProps) {
  const handleOrderedListClick = useCallback(
    (listType: string) => {
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          // Criar uma nova lista customizada com o tipo de estilo especificado
          const listNode = $createCustomListNode("number", 1, listType)
          const listItemNode = $createListItemNode()
          
          // Obter o conteúdo selecionado
          const selectedText = selection.getTextContent()
          
          if (selectedText) {
            // Se há texto selecionado, criar item da lista com esse texto
            listItemNode.append($createTextNode(selectedText))
          }
          
          listNode.append(listItemNode)
          
          // Inserir a lista na posição da seleção
          selection.insertNodes([listNode])
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
      <DropdownMenuSubContent className="w-48">
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
