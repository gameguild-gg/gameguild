"use client"

import { useCallback, useState, useEffect } from "react"
import { $getSelection, $isRangeSelection, FORMAT_TEXT_COMMAND, SELECTION_CHANGE_COMMAND } from "lexical"
import { $isTextNode } from "lexical"
import { $patchStyleText } from "@lexical/selection"
import { Underline, Minus, Strikethrough, Check } from "lucide-react"
import {
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu"

interface FormattingMenuComponentProps {
  editor: any
  isUnderline: boolean
  setIsUnderline: (value: boolean) => void
  isStrikethrough: boolean
  setIsStrikethrough: (value: boolean) => void
}

export function FormattingMenuComponent({ 
  editor, 
  isUnderline, 
  setIsUnderline,
  isStrikethrough,
  setIsStrikethrough
}: FormattingMenuComponentProps) {
  const [isOverline, setIsOverline] = useState(false)

  // Função para injetar CSS customizado para melhor posicionamento das linhas
  const injectDecorationStyles = useCallback(() => {
    const styleId = 'custom-text-decorations'
    if (!document.getElementById(styleId)) {
      const style = document.createElement('style')
      style.id = styleId
      style.textContent = `
        /* Melhor posicionamento para overline */
        .lexical-editor [style*="overline"] {
          position: relative;
        }
        /* Melhor posicionamento para strikethrough */
        .lexical-editor [style*="line-through"] {
          position: relative;
        }
      `
      document.head.appendChild(style)
    }
  }, [])

  // Injetar estilos ao montar o componente
  useEffect(() => {
    injectDecorationStyles()
  }, [])

  // Função para aplicar underline usando CSS inline apenas no texto selecionado
  const handleUnderlineToggle = useCallback(() => {
    editor.update(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        // Verificar se já tem underline na seleção
        const nodes = selection.getNodes()
        const hasUnderlineFormat = selection.hasFormat("underline")
        const hasUnderlineCss = nodes.some(node => {
          if ($isTextNode(node)) {
            const style = node.getStyle()
            return style.includes('border-bottom:') ||
                   style.includes('text-decoration-line: underline') || 
                   style.includes('text-decoration: underline')
          }
          return false
        })
        
        const hasUnderline = hasUnderlineFormat || hasUnderlineCss

        if (hasUnderline) {
          // Remover underline da seleção
          // Primeiro remover formato nativo se existir
          if (hasUnderlineFormat) {
            editor.dispatchCommand(FORMAT_TEXT_COMMAND, "underline")
          }
          
          // Remover estilos CSS de underline apenas da seleção
          $patchStyleText(selection, {
            'border-bottom': null,
            'text-decoration': (currentValue: string | null) => {
              if (!currentValue) return ''
              const newValue = currentValue.replace(/underline/g, '').trim()
              return newValue || ''
            },
            'text-decoration-line': (currentValue: string | null) => {
              if (!currentValue) return ''
              const newValue = currentValue.replace(/underline/g, '').replace(/\s+/g, ' ').trim()
              return newValue || ''
            }
          })
        } else {
          // Adicionar underline apenas à seleção usando border-bottom
          $patchStyleText(selection, {
            'border-bottom': '1px solid currentColor'
          })
        }
        
        setIsUnderline(!hasUnderline)
      }
    })
  }, [editor])

  // Função para aplicar overline usando CSS inline apenas no texto selecionado
  const handleOverlineToggle = useCallback(() => {
    editor.update(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        // Verificar se já tem overline na seleção
        const nodes = selection.getNodes()
        const hasOverline = nodes.some(node => {
          if ($isTextNode(node)) {
            const style = node.getStyle()
            return style.includes('text-decoration-line: overline') || 
                   style.includes('text-decoration: overline') ||
                   style.includes('overline')
          }
          return false
        })

        if (hasOverline) {
          // Remover overline apenas da seleção
          $patchStyleText(selection, {
            'text-decoration': (currentValue: string | null) => {
              if (!currentValue) return ''
              const newValue = currentValue.replace(/overline/g, '').trim()
              return newValue || ''
            },
            'text-decoration-line': (currentValue: string | null) => {
              if (!currentValue) return ''
              const newValue = currentValue.replace(/overline/g, '').replace(/\s+/g, ' ').trim()
              return newValue || ''
            }
          })
        } else {
          // Adicionar overline apenas à seleção
          $patchStyleText(selection, {
            'text-decoration-line': (currentValue: string | null) => {
              if (!currentValue) return 'overline'
              if (currentValue.includes('overline')) return currentValue
              return currentValue + ' overline'
            }
          })
        }
        
        setIsOverline(!hasOverline)
      }
    })
  }, [editor])

  // Função para aplicar strikethrough usando CSS inline apenas no texto selecionado
  const handleStrikethroughToggle = useCallback(() => {
    editor.update(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        // Verificar se já tem strikethrough na seleção
        const nodes = selection.getNodes()
        const hasStrikethroughFormat = selection.hasFormat("strikethrough")
        const hasStrikethroughCss = nodes.some(node => {
          if ($isTextNode(node)) {
            const style = node.getStyle()
            return style.includes('text-decoration-line: line-through') || 
                   style.includes('text-decoration: line-through') ||
                   style.includes('line-through')
          }
          return false
        })
        
        const hasStrikethrough = hasStrikethroughCss || hasStrikethroughFormat

        if (hasStrikethrough) {
          // Remover strikethrough da seleção
          // Primeiro remover formato nativo se existir
          if (hasStrikethroughFormat) {
            editor.dispatchCommand(FORMAT_TEXT_COMMAND, "strikethrough")
          }
          
          // Remover estilos CSS de strikethrough apenas da seleção
          $patchStyleText(selection, {
            'text-decoration': (currentValue: string | null) => {
              if (!currentValue) return ''
              const newValue = currentValue.replace(/line-through/g, '').trim()
              return newValue || ''
            },
            'text-decoration-line': (currentValue: string | null) => {
              if (!currentValue) return ''
              const newValue = currentValue.replace(/line-through/g, '').replace(/\s+/g, ' ').trim()
              return newValue || ''
            }
          })
        } else {
          // Adicionar strikethrough apenas à seleção
          $patchStyleText(selection, {
            'text-decoration-line': (currentValue: string | null) => {
              if (!currentValue) return 'line-through'
              if (currentValue.includes('line-through')) return currentValue
              return currentValue + ' line-through'
            }
          })
        }
        
        setIsStrikethrough(!hasStrikethrough)
      }
    })
  }, [editor])

  // Função para detectar underline, overline e strikethrough na seleção atual
  const detectFormats = useCallback(() => {
    editor.getEditorState().read(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        const nodes = selection.getNodes()
        
        // Detectar underline (CSS, border-bottom + formato nativo)
        const hasUnderlineCss = nodes.some(node => {
          if ($isTextNode(node)) {
            const style = node.getStyle()
            return style.includes('border-bottom:') ||
                   style.includes('text-decoration-line: underline') || 
                   style.includes('text-decoration: underline') ||
                   style.includes('underline')
          }
          return false
        })
        
        const hasUnderlineFormat = selection.hasFormat("underline")
        const hasUnderline = hasUnderlineCss || hasUnderlineFormat
        
        // Detectar overline
        const hasOverline = nodes.some(node => {
          if ($isTextNode(node)) {
            const style = node.getStyle()
            return style.includes('text-decoration-line: overline') || 
                   style.includes('text-decoration: overline') ||
                   style.includes('overline')
          }
          return false
        })
        
        // Detectar strikethrough (CSS + formato nativo)
        const hasStrikethroughCss = nodes.some(node => {
          if ($isTextNode(node)) {
            const style = node.getStyle()
            return style.includes('text-decoration-line: line-through') || 
                   style.includes('text-decoration: line-through') ||
                   style.includes('line-through')
          }
          return false
        })
        
        const hasStrikethroughFormat = selection.hasFormat("strikethrough")
        const hasStrikethrough = hasStrikethroughCss || hasStrikethroughFormat
        
        setIsUnderline(hasUnderline)
        setIsOverline(hasOverline)
        setIsStrikethrough(hasStrikethrough)
      }
    })
  }, [editor])

  // Detectar formatos quando a seleção muda
  useEffect(() => {
    const removeListener = editor.registerCommand(
      SELECTION_CHANGE_COMMAND,
      () => {
        detectFormats()
        return false
      },
      1
    )
    
    detectFormats()
    
    return removeListener
  }, [editor, detectFormats])

  return (
    <DropdownMenuSub>
      <DropdownMenuSubTrigger>
        <Underline className="mr-2 h-5 w-5" />
        <span>Formatting</span>
        {(isUnderline || isOverline || isStrikethrough) && <Check className="ml-auto h-5 w-5" />}
      </DropdownMenuSubTrigger>
      <DropdownMenuSubContent>
        <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Decorações de Texto</div>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onSelect={(e) => e.preventDefault()}
          onClick={handleUnderlineToggle}
        >
          <Underline className="mr-2 h-5 w-5" />
          <span>Underline</span>
          {isUnderline && <Check className="ml-auto h-5 w-5" />}
        </DropdownMenuItem>
        <DropdownMenuItem
          onSelect={(e) => e.preventDefault()}
          onClick={handleStrikethroughToggle}
        >
          <Strikethrough className="mr-2 h-5 w-5" />
          <span>Strikethrough</span>
          {isStrikethrough && <Check className="ml-auto h-5 w-5" />}
        </DropdownMenuItem>
        <DropdownMenuItem
          onSelect={(e) => e.preventDefault()}
          onClick={handleOverlineToggle}
        >
          <Minus className="mr-2 h-5 w-5" />
          <span>Overline</span>
          {isOverline && <Check className="ml-auto h-5 w-5" />}
        </DropdownMenuItem>
      </DropdownMenuSubContent>
    </DropdownMenuSub>
  )
}
