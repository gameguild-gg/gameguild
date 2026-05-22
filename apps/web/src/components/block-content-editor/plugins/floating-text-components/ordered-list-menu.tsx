"use client"

import { useCallback } from "react"
import { $getSelection, $isRangeSelection, $createTextNode } from "lexical"
import { $createListItemNode } from "@lexical/list"
import { $createCustomListNode, $isCustomListNode } from "@/components/block-content-editor/nodes/custom-list-node"
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
  { icon: "I.", name: "Uppercase Roman", style: "upper-roman" },
  { icon: "i.", name: "Lowercase Roman", style: "lower-roman" },
  { icon: "01.", name: "Zero-padded Numbers", style: "decimal-leading-zero" },
]

export function OrderedListMenu({ editor, currentListType }: OrderedListMenuProps) {
  const handleOrderedListClick = useCallback(
    (listType: string) => {
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          // Detectar cor atual se já estamos em uma lista
          let currentColor = "#3b82f6" // cor padrão
          const anchorNode = selection.anchor.getNode()
          let currentNode: any = anchorNode
          
          while (currentNode) {
            if ($isCustomListNode(currentNode)) {
              currentColor = currentNode.getMarkerColor()
              break
            }
            const parent = currentNode.getParent()
            currentNode = parent
          }
          
          // Criar diretamente um CustomListNode
          const customListNode = $createCustomListNode("number", 1, listType, currentColor)
          const listItemNode = $createListItemNode()
          
          // Se há texto selecionado, usar esse texto no item da lista
          const selectedText = selection.getTextContent()
          if (selectedText) {
            // Criar um novo nó de texto com o conteúdo selecionado
            const textNode = $createTextNode(selectedText)
            listItemNode.append(textNode)
            // Remover o texto selecionado
            selection.removeText()
          }
          
          customListNode.append(listItemNode)
          
          // Inserir a lista customizada na posição atual
          selection.insertNodes([customListNode])

          // Focar no item da lista para permitir edição (ainda dentro
          // do editor.update — métodos de nó exigem active editor state).
          listItemNode.selectEnd()
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
