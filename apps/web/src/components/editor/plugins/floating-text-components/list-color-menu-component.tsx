"use client"

import { useCallback, useState, useEffect } from "react"
import { $getSelection, $isRangeSelection, SELECTION_CHANGE_COMMAND } from "lexical"
import { $isListNode } from "@lexical/list"
import { $isCustomListNode } from "@/components/editor/nodes/custom-list-node"
import { Palette, Check } from "lucide-react"
import {
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"

interface ListColorMenuComponentProps {
  editor: any
}

const LIST_COLORS = [
  { name: "Azul (Padrão)", value: "oklch(0.488 0.243 264.376)", preview: "#3b82f6" },
  { name: "Vermelho", value: "oklch(0.576 0.232 27.33)", preview: "#ef4444" },
  { name: "Verde", value: "oklch(0.518 0.177 142.495)", preview: "#22c55e" },
  { name: "Roxo", value: "oklch(0.569 0.243 305.06)", preview: "#a855f7" },
  { name: "Amarelo", value: "oklch(0.824 0.179 83.87)", preview: "#eab308" },
  { name: "Rosa", value: "oklch(0.656 0.258 355.32)", preview: "#ec4899" },
  { name: "Laranja", value: "oklch(0.672 0.192 60.77)", preview: "#f97316" },
  { name: "Ciano", value: "oklch(0.709 0.191 195.198)", preview: "#06b6d4" },
  { name: "Preto", value: "oklch(0.2 0 0)", preview: "#000000" },
  { name: "Cinza", value: "oklch(0.5 0 0)", preview: "#6b7280" },
]

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
              foundColor = currentNode.getMarkerColor()
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
              foundColor = currentNode.getMarkerColor()
              break
            }
            const parent = currentNode.getParent()
            currentNode = parent
          }
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
      <DropdownMenuSubContent className="w-48">
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Cores dos Marcadores</div>
        <DropdownMenuSeparator />
        {LIST_COLORS.map((color, index) => (
          <DropdownMenuItem
            key={index}
            onClick={() => handleListColorChange(color.value)}
            onSelect={(e) => e.preventDefault()}
          >
            <div 
              className="mr-2 h-4 w-4 rounded border" 
              style={{ backgroundColor: color.preview }}
            />
            <span>{color.name}</span>
            {currentListColor === color.value && <Check className="ml-auto h-4 w-4" />}
          </DropdownMenuItem>
        ))}
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
