"use client"

import { useCallback, useState, useEffect } from "react"
import { $getSelection, $isRangeSelection, FORMAT_TEXT_COMMAND, SELECTION_CHANGE_COMMAND } from "lexical"
import { $isTextNode } from "lexical"
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

  // Função para aplicar underline usando CSS inline (para compatibilidade com overline)
  const handleUnderlineToggle = useCallback(() => {
    editor.update(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        const nodes = selection.getNodes()
        
        // Verificar se já tem underline via CSS (usando border-bottom)
        const hasUnderlineCss = nodes.some(node => {
          if ($isTextNode(node)) {
            const style = node.getStyle()
            return style.includes('border-bottom:') ||
                   style.includes('text-decoration-line: underline') || 
                   style.includes('text-decoration: underline')
          }
          return false
        })
        
        // Verificar se já tem underline via formato nativo
        const hasUnderlineFormat = selection.hasFormat("underline")
        
        const hasUnderline = hasUnderlineCss || hasUnderlineFormat

        // Aplicar ou remover underline
        nodes.forEach(node => {
          if ($isTextNode(node)) {
            const currentStyle = node.getStyle()
            let newStyle = currentStyle
            
            if (hasUnderline) {
              // Remover underline do CSS mas preservar outras decorações
              const hasOverline = currentStyle.includes('overline')
              const hasStrikethrough = currentStyle.includes('line-through')
              
              if (hasOverline && hasStrikethrough) {
                // Manter overline e strikethrough
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*[^;]*underline[^;]*(;|$)/g, 'text-decoration-line: overline line-through;')
                  .replace(/text-decoration:\s*underline[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              } else if (hasOverline) {
                // Manter apenas overline
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*[^;]*underline[^;]*(;|$)/g, 'text-decoration-line: overline;')
                  .replace(/text-decoration:\s*underline[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              } else if (hasStrikethrough) {
                // Manter apenas strikethrough
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*[^;]*underline[^;]*(;|$)/g, 'text-decoration-line: line-through;')
                  .replace(/text-decoration:\s*underline[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              } else {
                // Remover apenas underline (incluindo border-bottom)
                newStyle = currentStyle
                  .replace(/border-bottom:\s*[^;]*(;|$)/g, '')
                  .replace(/text-decoration-line:\s*underline[^;]*(;|$)/g, '')
                  .replace(/text-decoration:\s*underline[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              }
                
              // Também remover formato nativo se existir
              if (hasUnderlineFormat) {
                editor.dispatchCommand(FORMAT_TEXT_COMMAND, "underline")
              }
            } else {
              // Adicionar underline via CSS para compatibilidade com outras decorações
              const hasOverline = currentStyle.includes('overline')
              const hasStrikethrough = currentStyle.includes('line-through')
              
              if (hasOverline && hasStrikethrough) {
                // Combinar com overline e strikethrough existentes
                newStyle = currentStyle.replace(
                  /text-decoration-line:\s*[^;]*(?:overline|line-through)[^;]*(;|$)/g, 
                  'text-decoration-line: underline overline line-through$1'
                )
              } else if (hasOverline) {
                // Combinar com overline existente
                newStyle = currentStyle.replace(
                  /text-decoration-line:\s*overline([^;]*)(;|$)/g, 
                  'text-decoration-line: underline overline$1$2'
                )
              } else if (hasStrikethrough) {
                // Combinar com strikethrough existente
                newStyle = currentStyle.replace(
                  /text-decoration-line:\s*line-through([^;]*)(;|$)/g, 
                  'text-decoration-line: underline line-through$1$2'
                )
              } else {
                // Adicionar underline usando border-bottom para linha contínua
                const borderStyle = 'border-bottom: 1px solid currentColor'
                
                if (currentStyle.trim()) {
                  newStyle = currentStyle + '; ' + borderStyle
                } else {
                  newStyle = borderStyle
                }
              }
            }
            
            node.setStyle(newStyle)
          }
        })
        
        setIsUnderline(!hasUnderline)
      }
    })
  }, [editor])

  // Função para aplicar overline usando CSS inline
  const handleOverlineToggle = useCallback(() => {
    editor.update(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        const nodes = selection.getNodes()
        
        // Verificar se já tem overline
        const hasOverline = nodes.some(node => {
          if ($isTextNode(node)) {
            const style = node.getStyle()
            return style.includes('text-decoration-line: overline') || 
                   style.includes('text-decoration: overline') ||
                   style.includes('overline')
          }
          return false
        })

        // Aplicar ou remover overline
        nodes.forEach(node => {
          if ($isTextNode(node)) {
            const currentStyle = node.getStyle()
            let newStyle = currentStyle
            
            if (hasOverline) {
              // Remover overline mas preservar outras decorações
              const hasUnderline = currentStyle.includes('underline')
              const hasStrikethrough = currentStyle.includes('line-through')
              
              if (hasUnderline && hasStrikethrough) {
                // Manter underline e strikethrough
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*[^;]*overline[^;]*(;|$)/g, 'text-decoration-line: underline line-through;')
                  .replace(/text-decoration:\s*overline[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              } else if (hasUnderline) {
                // Manter apenas underline
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*[^;]*overline[^;]*(;|$)/g, 'text-decoration-line: underline;')
                  .replace(/text-decoration:\s*overline[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              } else if (hasStrikethrough) {
                // Manter apenas strikethrough
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*[^;]*overline[^;]*(;|$)/g, 'text-decoration-line: line-through;')
                  .replace(/text-decoration:\s*overline[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              } else {
                // Remover apenas overline
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*overline[^;]*(;|$)/g, '')
                  .replace(/text-decoration:\s*overline[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              }
            } else {
              // Adicionar overline
              const hasUnderline = currentStyle.includes('underline')
              const hasStrikethrough = currentStyle.includes('line-through')
              
              if (hasUnderline && hasStrikethrough) {
                // Combinar com underline e strikethrough existentes
                newStyle = currentStyle.replace(
                  /text-decoration-line:\s*[^;]*(?:underline|line-through)[^;]*(;|$)/g, 
                  'text-decoration-line: underline overline line-through$1'
                )
              } else if (hasUnderline) {
                // Combinar com underline existente
                newStyle = currentStyle.replace(
                  /text-decoration-line:\s*underline([^;]*)(;|$)/g, 
                  'text-decoration-line: underline overline$1$2'
                )
              } else if (hasStrikethrough) {
                // Combinar com strikethrough existente
                newStyle = currentStyle.replace(
                  /text-decoration-line:\s*line-through([^;]*)(;|$)/g, 
                  'text-decoration-line: overline line-through$1$2'
                )
              } else {
                // Adicionar apenas overline
                if (currentStyle.trim()) {
                  newStyle = currentStyle + '; text-decoration-line: overline'
                } else {
                  newStyle = 'text-decoration-line: overline'
                }
              }
            }
            
            node.setStyle(newStyle)
          }
        })
        
        setIsOverline(!hasOverline)
      }
    })
  }, [editor])

  // Função para aplicar strikethrough usando CSS inline
  const handleStrikethroughToggle = useCallback(() => {
    editor.update(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        const nodes = selection.getNodes()
        
        // Verificar se já tem strikethrough via CSS
        const hasStrikethroughCss = nodes.some(node => {
          if ($isTextNode(node)) {
            const style = node.getStyle()
            return style.includes('text-decoration-line: line-through') || 
                   style.includes('text-decoration: line-through') ||
                   style.includes('line-through')
          }
          return false
        })
        
        // Verificar se já tem strikethrough via formato nativo
        const hasStrikethroughFormat = selection.hasFormat("strikethrough")
        
        const hasStrikethrough = hasStrikethroughCss || hasStrikethroughFormat

        // Aplicar ou remover strikethrough
        nodes.forEach(node => {
          if ($isTextNode(node)) {
            const currentStyle = node.getStyle()
            let newStyle = currentStyle
            
            if (hasStrikethrough) {
              // Remover strikethrough do CSS mas preservar outras decorações
              const hasUnderline = currentStyle.includes('underline')
              const hasOverline = currentStyle.includes('overline')
              
              if (hasUnderline && hasOverline) {
                // Manter underline e overline
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*[^;]*line-through[^;]*(;|$)/g, 'text-decoration-line: underline overline;')
                  .replace(/text-decoration:\s*line-through[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              } else if (hasUnderline) {
                // Manter apenas underline
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*[^;]*line-through[^;]*(;|$)/g, 'text-decoration-line: underline;')
                  .replace(/text-decoration:\s*line-through[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              } else if (hasOverline) {
                // Manter apenas overline
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*[^;]*line-through[^;]*(;|$)/g, 'text-decoration-line: overline;')
                  .replace(/text-decoration:\s*line-through[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              } else {
                // Remover apenas strikethrough
                newStyle = currentStyle
                  .replace(/text-decoration-line:\s*line-through[^;]*(;|$)/g, '')
                  .replace(/text-decoration:\s*line-through[^;]*(;|$)/g, '')
                  .replace(/;;/g, ';')
                  .replace(/^;|;$/g, '')
              }
                
              // Também remover formato nativo se existir
              if (hasStrikethroughFormat) {
                editor.dispatchCommand(FORMAT_TEXT_COMMAND, "strikethrough")
              }
            } else {
              // Adicionar strikethrough via CSS para compatibilidade com outras decorações
              const hasUnderline = currentStyle.includes('underline')
              const hasOverline = currentStyle.includes('overline')
              
              if (hasUnderline && hasOverline) {
                // Combinar com underline e overline existentes
                newStyle = currentStyle.replace(
                  /text-decoration-line:\s*(underline\s+overline|overline\s+underline)([^;]*)(;|$)/g, 
                  'text-decoration-line: underline overline line-through$2$3'
                )
              } else if (hasUnderline) {
                // Combinar com underline existente
                newStyle = currentStyle.replace(
                  /text-decoration-line:\s*underline([^;]*)(;|$)/g, 
                  'text-decoration-line: underline line-through$1$2'
                )
              } else if (hasOverline) {
                // Combinar com overline existente
                newStyle = currentStyle.replace(
                  /text-decoration-line:\s*overline([^;]*)(;|$)/g, 
                  'text-decoration-line: overline line-through$1$2'
                )
              } else {
                // Adicionar apenas strikethrough
                if (currentStyle.trim()) {
                  newStyle = currentStyle + '; text-decoration-line: line-through'
                } else {
                  newStyle = 'text-decoration-line: line-through'
                }
              }
            }
            
            node.setStyle(newStyle)
          }
        })
        
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
