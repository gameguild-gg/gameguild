"use client"

import { useCallback, useState, useEffect } from "react"
import { $getSelection, $isRangeSelection, SELECTION_CHANGE_COMMAND } from "lexical"
import { $isListNode } from "@lexical/list"
import { $isCustomListNode } from "@/components/editor/nodes/custom-list-node"
import { Palette } from "lucide-react"
import {
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"
import { ColorPalette } from "@/components/editor/extras/color-palette"

interface ListColorMenuComponentProps {
  editor: any
}

// Cor padrão para listas (azul)
const DEFAULT_LIST_COLOR = "#3b82f6"

export function ListColorMenuComponent({ editor }: ListColorMenuComponentProps) {
  const [currentListColor, setCurrentListColor] = useState<string>("")

  const handleListColorChange = useCallback(
    (color: string) => {
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          // Encontrar o nó de lista pai - buscar em todos os nós selecionados
          const nodes = selection.getNodes()
          let listNode = null
          
          // Tentar encontrar lista a partir de qualquer nó selecionado
          for (const node of nodes) {
            let currentNode: any = node
            while (currentNode) {
              if ($isListNode(currentNode) || $isCustomListNode(currentNode)) {
                listNode = currentNode
                break
              }
              const parent = currentNode.getParent()
              currentNode = parent
            }
            if (listNode) break
          }
          
          // Se não encontrou, tentar a partir do anchor node
          if (!listNode) {
            const anchorNode = selection.anchor.getNode()
            let currentNode: any = anchorNode
            while (currentNode) {
              if ($isListNode(currentNode) || $isCustomListNode(currentNode)) {
                listNode = currentNode
                break
              }
              const parent = currentNode.getParent()
              currentNode = parent
            }
          }
          
          if (listNode && $isCustomListNode(listNode)) {
            // Atualizar a cor do marcador na lista customizada
            listNode.setMarkerColor(color)
            setCurrentListColor(color)
            
            // Forçar re-renderização do DOM
            const writable = listNode.getWritable()
            writable.__markerColor = color
          }
        }
      })
    },
    [editor]
  )

  // Função para detectar a cor atual da lista selecionada
  const detectCurrentListColor = useCallback(() => {
    editor.getEditorState().read(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        const nodes = selection.getNodes()
        let foundColor = ""
        
        // Tentar encontrar lista a partir de qualquer nó selecionado
        for (const node of nodes) {
          let currentNode: any = node
          while (currentNode) {
            if ($isCustomListNode(currentNode)) {
              foundColor = currentNode.getMarkerColor() || DEFAULT_LIST_COLOR
              break
            }
            const parent = currentNode.getParent()
            currentNode = parent
          }
          if (foundColor) break
        }
        
        // Se não encontrou, tentar a partir do anchor node
        if (!foundColor) {
          const anchorNode = selection.anchor.getNode()
          let currentNode: any = anchorNode
          while (currentNode) {
            if ($isCustomListNode(currentNode)) {
              foundColor = currentNode.getMarkerColor() || DEFAULT_LIST_COLOR
              break
            }
            const parent = currentNode.getParent()
            currentNode = parent
          }
        }
        
        // Se ainda não encontrou, usar cor padrão
        if (!foundColor) {
          foundColor = DEFAULT_LIST_COLOR
        }
        
        setCurrentListColor(foundColor)
      }
    })
  }, [editor])

  // Detectar cor atual quando a seleção muda
  useEffect(() => {
    const removeListener = editor.registerCommand(
      SELECTION_CHANGE_COMMAND,
      () => {
        detectCurrentListColor()
        return false
      },
      1
    )
    
    // Detectar cor inicial
    detectCurrentListColor()
    
    return removeListener
  }, [editor, detectCurrentListColor])

  // Remover o useState incorreto e substituir
  // useState(() => {
  //   detectCurrentListColor()
  // })

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <Palette className="mr-2 h-4 w-4" />
        <span>List Color</span>
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent className="w-64">
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">List Color</div>
        <DropdownMenuSeparator />
        <ColorPalette
          selectedColor={currentListColor || DEFAULT_LIST_COLOR}
          onColorChange={handleListColorChange}
          showCustomInput={true}
          customInputLabel="Custom:"
        />
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
