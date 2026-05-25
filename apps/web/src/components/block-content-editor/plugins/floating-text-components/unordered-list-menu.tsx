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
  { icon: "•", name: "Disc (default)", style: "disc" },
  { icon: "○", name: "Circle", style: "circle" },
  { icon: "■", name: "Square", style: "square" },
  { icon: "▶", name: "Arrow", style: "arrow" },
  { icon: "★", name: "Star", style: "star" },
]

export function UnorderedListMenu({ editor, currentListType }: UnorderedListMenuProps) {
  const handleUnorderedListClick = useCallback(
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
          
          // Criar diretamente um CustomListNode para listas não ordenadas
          const customListNode = $createCustomListNode("bullet", 1, listType, currentColor)
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
        <span className="mr-2">•</span>
        <span>Unordered Lists</span>
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent className="w-48">
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
