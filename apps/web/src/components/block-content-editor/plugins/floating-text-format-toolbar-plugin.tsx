"use client"

import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger, DropdownMenuTrigger
} from "@/components/ui/dropdown-menu"
import { $isLinkNode } from "@lexical/link"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $createHeadingNode, $createQuoteNode, $isHeadingNode, type HeadingTagType } from "@lexical/rich-text"
import { $setBlocksType } from "@lexical/selection"
import {
  $createParagraphNode,
  $getRoot,
  $getSelection,
  $isRangeSelection,
  FORMAT_ELEMENT_COMMAND,
  FORMAT_TEXT_COMMAND,
  type LexicalNode,
} from "lexical"
import {
  AlignCenter,
  AlignJustify,
  AlignLeft,
  AlignRight,
  ArrowUp,
  Bold,
  Check,
  Italic,
  Palette,
  Quote,
  Subscript,
  Superscript,
  TextCursorInput,
  Type,
} from "lucide-react"
import { useCallback, useEffect, useRef, useState } from "react"
import { BackgroundColorMenuComponent } from "./floating-text-components/background-color-menu-component"
import { FontFamilyMenuComponent } from "./floating-text-components/font-family-menu-component"
import { FontSizeMenuComponent } from "./floating-text-components/font-size-menu-component"
import { FormattingMenuComponent } from "./floating-text-components/formatting-menu-component"
import { LinkMenuComponent } from "./floating-text-components/link-menu-component"
import { ListColorMenuComponent } from "./floating-text-components/list-color-menu-component"
import { ListMenuComponent } from "./floating-text-components/list-menu-component"
import { TextColorMenuComponent } from "./floating-text-components/text-color-menu-component"

// Type extension for nodes that support style methods (like TextNode)
interface StylableNode extends LexicalNode {
  getStyle?: () => string;
  setStyle?: (style: string) => void;
}

export function FloatingTextFormatToolbarPlugin() {
  const [editor] = useLexicalComposerContext()
  const toolbarRef = useRef<HTMLDivElement>(null)
  const isDropdownOpenRef = useRef(false)
  const [isText, setIsText] = useState(false)
  const [isLink, setIsLink] = useState(false)
  const [isBold, setIsBold] = useState(false)
  const [isItalic, setIsItalic] = useState(false)
  const [isUnderline, setIsUnderline] = useState(false)
  const [isOverline, setIsOverline] = useState(false)
  const [isStrikethrough, setIsStrikethrough] = useState(false)
  const [isSubscript, setIsSubscript] = useState(false)
  const [isSuperscript, setIsSuperscript] = useState(false)
  const [isCode, setIsCode] = useState(false)
  const [currentCaseFormat, setCurrentCaseFormat] = useState<"uppercase" | "lowercase" | "capitalize" | null>(null)
  const [selectedElementKey, setSelectedElementKey] = useState<string | null>(null)
  const [position, setPosition] = useState<{ top: number; left: number } | null>(null)
  const [forceShow, setForceShow] = useState(false)
  const [currentHeadingLevel, setCurrentHeadingLevel] = useState<HeadingTagType | null>(null)
  const [isQuote, setIsQuote] = useState(false)
  const [currentFontFamily, setCurrentFontFamily] = useState<string>("")
  const [currentFontSize, setCurrentFontSize] = useState<string>("")
  const [showFontSizeInput, setShowFontSizeInput] = useState(false)
  const [currentTextColor, setCurrentTextColor] = useState<string>("")
  const [currentBackgroundColor, setCurrentBackgroundColor] = useState<string>("")
  const [currentAlignment, setCurrentAlignment] = useState<string>("")
  const [currentListType, setCurrentListType] = useState<string>("")
  const [openDropdown, setOpenDropdown] = useState<string | null>(null)

  const addNewLineAtTop = useCallback(() => {
    editor.update(() => {
      const root = $getRoot()
      const firstChild = root.getFirstChild()
      const newParagraph = $createParagraphNode()

      if (firstChild) {
        firstChild.insertBefore(newParagraph)
      } else {
        root.append(newParagraph)
      }
    })
  }, [editor])

  const updateToolbar = useCallback(() => {
    const selection = $getSelection()
    if (!$isRangeSelection(selection)) {
      setIsText(false)
      setIsBold(false)
      setIsItalic(false)
      setIsSubscript(false)
      setIsSuperscript(false)
      setIsCode(false)
      setIsUnderline(false)
      setIsOverline(false)
      setIsStrikethrough(false)
      setCurrentCaseFormat(null)
      setPosition(null)
      return
    }

    setIsBold(selection.hasFormat("bold"))
    setIsItalic(selection.hasFormat("italic"))
    setIsUnderline(selection.hasFormat("underline"))
    setIsStrikethrough(selection.hasFormat("strikethrough"))
    setIsSubscript(selection.hasFormat("subscript"))
    setIsSuperscript(selection.hasFormat("superscript"))
    setIsCode(selection.hasFormat("code"))

    // Verificar se a seleção está dentro de um link
    const nodes = selection.getNodes()
    let isInLink = false
    for (const node of nodes) {
      const parent = node.getParent()
      if ($isLinkNode(parent) || $isLinkNode(node)) {
        isInLink = true
        break
      }
    }
    setIsLink(isInLink)

    if (selection.getNodes().length > 0) {
      const firstNode = selection.getNodes()[0] as StylableNode
      const style = firstNode?.getStyle ? String(firstNode.getStyle()) : ""

      if (style.includes("text-transform: uppercase")) {
        setCurrentCaseFormat("uppercase")
      } else if (style.includes("text-transform: lowercase")) {
        setCurrentCaseFormat("lowercase")
      } else if (style.includes("text-transform: capitalize")) {
        setCurrentCaseFormat("capitalize")
      } else {
        setCurrentCaseFormat(null)
      }
    } else {
      setCurrentCaseFormat(null)
    }

    const hasText = selection.getTextContent().length > 0
    setIsText(hasText)

    // Se não há texto e não está forçando mostrar, limpar posição
    if (!hasText && !forceShow) {
      setPosition(null)
      return
    }

    const anchorNode = selection.anchor.getNode()
    const element = anchorNode.getKey() === "root" ? anchorNode : anchorNode.getTopLevelElementOrThrow()
    if ($isHeadingNode(element)) {
      setCurrentHeadingLevel(element.getTag())
    } else {
      setCurrentHeadingLevel(null)
    }

    const parentElement = anchorNode.getParent()
    if (parentElement && parentElement.getType() === "quote") {
      setIsQuote(true)
    } else {
      setIsQuote(false)
    }

    if (selection.getNodes().length > 0) {
      const firstNode = selection.getNodes()[0] as StylableNode
      const style = firstNode?.getStyle ? String(firstNode.getStyle()) : ""
      const fontFamilyMatch = style.match(/font-family:\s*([^;]+)/)
      if (fontFamilyMatch && fontFamilyMatch[1]) {
        setCurrentFontFamily(fontFamilyMatch[1].replace(/['"]/g, ""))
      } else {
        setCurrentFontFamily("")
      }
    } else {
      setCurrentFontFamily("")
    }

    if (selection.getNodes().length > 0) {
      const firstNode = selection.getNodes()[0] as StylableNode
      const style = firstNode?.getStyle ? String(firstNode.getStyle()) : ""
      const fontSizeMatch = style.match(/font-size:\s*([^;]+)/)
      if (fontSizeMatch && fontSizeMatch[1]) {
        setCurrentFontSize(fontSizeMatch[1].replace(/['']/g, ""))
      } else {
        setCurrentFontSize("")
      }
    } else {
      setCurrentFontSize("")
    }

    if (selection.getNodes().length > 0) {
      const firstNode = selection.getNodes()[0] as StylableNode
      const style = firstNode?.getStyle ? String(firstNode.getStyle()) : ""
      const colorMatch = style.match(/(?<!background-)color:\s*([^;]+)/)
      if (colorMatch && colorMatch[1]) {
        setCurrentTextColor(colorMatch[1].replace(/['']/g, "").trim())
      } else {
        setCurrentTextColor("")
      }
    } else {
      setCurrentTextColor("")
    }

    if (selection.getNodes().length > 0) {
      const firstNode = selection.getNodes()[0] as StylableNode
      const style = firstNode?.getStyle ? String(firstNode.getStyle()) : ""
      const backgroundColorMatch = style.match(/background-color:\s*([^;]+)/)
      if (backgroundColorMatch && backgroundColorMatch[1]) {
        setCurrentBackgroundColor(backgroundColorMatch[1].replace(/['']/g, "").trim())
      } else {
        setCurrentBackgroundColor("")
      }
    } else {
      setCurrentBackgroundColor("")
    }

    if (selection) {
      const element = anchorNode.getTopLevelElementOrThrow()
      setCurrentAlignment(String(element.getFormat()))
    } else {
      setCurrentAlignment("")
    }

    const parentElementList = anchorNode.getParent()
    const parentType = parentElementList?.getType()
    if (parentElementList && (parentType === "list" || parentType === "custom-list")) {
      setCurrentListType((parentElementList as unknown as { getListType: () => string }).getListType())
    } else {
      setCurrentListType("")
    }

    // Calcular posição do toolbar - funciona tanto para texto selecionado quanto linha vazia
    const nativeSelection = window.getSelection()
    if (nativeSelection && nativeSelection.rangeCount > 0) {
      const range = nativeSelection.getRangeAt(0)
      const rect = range.getBoundingClientRect()

      if (rect && (rect.width > 0 || rect.height > 0)) {
        const toolbarHeight = 70 // Altura estimada do toolbar
        const toolbarWidth = 240
        const spacing = 8 // Espaço entre toolbar e texto

        setPosition({
          top: rect.top - toolbarHeight - spacing,
          left: Math.max(8, rect.left + (rect.width - toolbarWidth) / 2),
        })
      } else {
        setPosition(null)
      }
    } else {
      setPosition(null)
    }
  }, [])

  const applyCaseFormat = useCallback(
    (caseType: "uppercase" | "lowercase" | "capitalize") => {
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          const firstNode = selection.getNodes()[0] as StylableNode | undefined
          const currentStyle = firstNode?.getStyle?.() || ""
          const cleanStyle = currentStyle
            .replace(/text-transform:\s*[^;]+;?/g, "")
            .replace(/;;/g, ";")
            .replace(/^;|;$/g, "")

          const newStyle = cleanStyle ? `${cleanStyle}; text-transform: ${caseType}` : `text-transform: ${caseType}`

          selection.getNodes().forEach((node) => {
            const stylableNode = node as StylableNode
            if (stylableNode.setStyle) {
              stylableNode.setStyle(newStyle)
            }
          })

          setCurrentCaseFormat(caseType)
        }
      })
    },
    [editor],
  )

  const removeCaseFormat = useCallback(() => {
    editor.update(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        selection.getNodes().forEach((node) => {
          const stylableNode = node as StylableNode
          if (stylableNode.getStyle && stylableNode.setStyle) {
            const currentStyle = stylableNode.getStyle()
            const cleanStyle = currentStyle
              .replace(/text-transform:\s*[^;]+;?/g, "")
              .replace(/;;/g, ";")
              .replace(/^;|;$/g, "")

            stylableNode.setStyle(cleanStyle)
          }
        })
        setCurrentCaseFormat(null)
      }
    })
  }, [editor])

  useEffect(() => {
    // Forçar inicialização do Radix UI para permitir hover imediato
    const timer = setTimeout(() => {
      if (toolbarRef.current) {
        // Simular uma interação mínima para ativar o Radix UI
        const event = new MouseEvent('mouseenter', { bubbles: false });
        toolbarRef.current.dispatchEvent(event);
      }
    }, 100);

    return () => clearTimeout(timer);
  }, []);

  useEffect(() => {
    const handleDoubleClick = () => {
      setForceShow(true)
      // Aguardar um pouco para garantir que a seleção foi atualizada
      setTimeout(() => {
        editor.getEditorState().read(() => {
          updateToolbar()
        })
      }, 50)
    }

    const editorElement = editor.getRootElement()
    if (editorElement) {
      editorElement.addEventListener('dblclick', handleDoubleClick)
      return () => {
        editorElement.removeEventListener('dblclick', handleDoubleClick)
      }
    }
  }, [editor, updateToolbar])

  const forceShowRef = useRef(forceShow)
  const toolbarRefForListener = useRef(toolbarRef)

  // Atualizar refs quando os valores mudarem
  useEffect(() => {
    forceShowRef.current = forceShow
  }, [forceShow])

  // Fechar toolbar ao clicar fora dele (mas não quando apenas o submenu fechar)
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      // Não fazer nada se toolbar não está visível
      if (!forceShowRef.current) return

      const target = event.target as HTMLElement

      // Verificar se o clique foi dentro do toolbar principal
      const isClickInsideToolbar = toolbarRef.current?.contains(target)
      if (isClickInsideToolbar) {
        return
      }

      // Usar setTimeout para permitir que Radix UI processe o evento primeiro
      setTimeout(() => {
        // Se há algum dropdown aberto, não fechar
        const menuContent = document.querySelector('[data-slot="dropdown-menu-content"]')
        if (menuContent !== null) {
          return
        }

        // Clicou fora de tudo - fechar o toolbar apenas se nenhum dropdown está aberto
        if (forceShowRef.current) {
          setForceShow(false)
          setPosition(null)
        }
      }, 0)
    }

    // Usar 'click' em fase de bubbling após todos os eventos serem processados
    document.addEventListener('click', handleClickOutside, false)
    return () => {
      document.removeEventListener('click', handleClickOutside, false)
    }
  }, [])

  return (
    <>
      {position && (
        <div
          ref={toolbarRef}
          className="fixed z-50 flex items-center gap-2 rounded-lg border-2 border-border/20 bg-background/95 backdrop-blur-sm p-2 shadow-lg ring-1 ring-black/5 transition-all duration-200 ease-in-out hover:shadow-xl"
          style={{
            top: `${position.top}px`,
            left: `${position.left}px`,
            minHeight: "60px",
            minWidth: "240px",
          }}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault()
              e.stopPropagation()
            }
          }}
          onMouseLeave={(e) => {
            // Fechar dropdown quando o mouse sai do toolbar
            setOpenDropdown(null)
          }}
        >
          <Button
            variant="ghost"
            size="icon"
            className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
            onClick={addNewLineAtTop}
            title="Add new line at top"
          >
            <ArrowUp className="h-5 w-5" />
          </Button>

          <DropdownMenu
            open={openDropdown === "formatting"}
            onOpenChange={(open) => setOpenDropdown(open ? "formatting" : null)}
          >
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
                onMouseEnter={() => {
                  setOpenDropdown("formatting")
                }}
                onMouseDown={(e) => {
                  e.stopPropagation()
                }}
                onClick={(e) => {
                  e.stopPropagation()
                  // Alternar entre abrir e fechar quando clica
                  setOpenDropdown(prev => prev === "formatting" ? null : "formatting")
                }}
              >
                <Bold className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              side="top"
              align="start"
              className="w-48"
              onMouseDown={(e) => {
                e.stopPropagation()
              }}
              onClick={(e) => {
                e.stopPropagation()
              }}
            >
              <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Formatting</div>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onSelect={(e) => e.preventDefault()}
                onClick={() => {
                  editor.dispatchCommand(FORMAT_TEXT_COMMAND, "bold")
                  setTimeout(() => {
                    editor.getEditorState().read(() => updateToolbar())
                  }, 0)
                }}
              >
                <Bold className="mr-2 h-5 w-5" />
                <span>Bold</span>
                {isBold && <Check className="ml-auto h-5 w-5" />}
              </DropdownMenuItem>
              <DropdownMenuItem
                onSelect={(e) => e.preventDefault()}
                onClick={() => {
                  editor.dispatchCommand(FORMAT_TEXT_COMMAND, "italic")
                  setTimeout(() => {
                    editor.getEditorState().read(() => updateToolbar())
                  }, 0)
                }}
              >
                <Italic className="mr-2 h-5 w-5" />
                <span>Italic</span>
                {isItalic && <Check className="ml-auto h-5 w-5" />}
              </DropdownMenuItem>

              <FormattingMenuComponent
                editor={editor}
                isUnderline={isUnderline}
                setIsUnderline={setIsUnderline}
                isStrikethrough={isStrikethrough}
                setIsStrikethrough={setIsStrikethrough}
              />
              <DropdownMenuSeparator />

              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  <Superscript className="mr-2 h-5 w-5" />
                  <span>Script</span>
                  {(isSubscript || isSuperscript) && <Check className="ml-auto h-5 w-5" />}
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Posição do Texto</div>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.dispatchCommand(FORMAT_TEXT_COMMAND, "superscript")
                      editor.getEditorState().read(() => updateToolbar())
                    }}
                  >
                    <Superscript className="mr-2 h-5 w-5" />
                    <span>Sobrescrito</span>
                    {isSuperscript && <Check className="ml-auto h-5 w-5" />}
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.dispatchCommand(FORMAT_TEXT_COMMAND, "subscript")
                      editor.getEditorState().read(() => updateToolbar())
                    }}
                  >
                    <Subscript className="mr-2 h-5 w-5" />
                    <span>Subscrito</span>
                    {isSubscript && <Check className="ml-auto h-5 w-5" />}
                  </DropdownMenuItem>
                </DropdownMenuSubContent>
              </DropdownMenuSub>

              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  <Type className="mr-2 h-5 w-5" />
                  <span>Caixa</span>
                  {currentCaseFormat && <Check className="ml-auto h-5 w-5" />}
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Transformar Texto</div>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      if (currentCaseFormat === "uppercase") {
                        removeCaseFormat()
                      } else {
                        applyCaseFormat("uppercase")
                      }
                    }}
                  >
                    <span className="mr-2 font-bold">AA</span>
                    <span>MAIÚSCULAS</span>
                    {currentCaseFormat === "uppercase" && <Check className="ml-auto h-5 w-5" />}
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      if (currentCaseFormat === "lowercase") {
                        removeCaseFormat()
                      } else {
                        applyCaseFormat("lowercase")
                      }
                    }}
                  >
                    <span className="mr-2 font-bold">aa</span>
                    <span>minúsculas</span>
                    {currentCaseFormat === "lowercase" && <Check className="ml-auto h-5 w-5" />}
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      if (currentCaseFormat === "capitalize") {
                        removeCaseFormat()
                      } else {
                        applyCaseFormat("capitalize")
                      }
                    }}
                  >
                    <span className="mr-2 font-bold">Aa</span>
                    <span>Primeira Maiúscula</span>
                    {currentCaseFormat === "capitalize" && <Check className="ml-auto h-5 w-5" />}
                  </DropdownMenuItem>
                  {currentCaseFormat && (
                    <>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem onSelect={(e) => e.preventDefault()} onClick={removeCaseFormat}>
                        <span className="mr-2 font-bold">×</span>
                        <span>Remover Formatação</span>
                      </DropdownMenuItem>
                    </>
                  )}
                </DropdownMenuSubContent>
              </DropdownMenuSub>


            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu
            open={openDropdown === "style"}
            onOpenChange={(open) => setOpenDropdown(open ? "style" : null)}
          >
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
                onMouseEnter={() => {
                  setOpenDropdown("style")
                }}
                onMouseDown={(e) => {
                  e.stopPropagation()
                }}
                onClick={(e) => {
                  e.stopPropagation()
                  // Alternar entre abrir e fechar quando clica
                  setOpenDropdown(prev => prev === "style" ? null : "style")
                }}
              >
                <Palette className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              side="top"
              align="start"
              className="w-64"
              onMouseDown={(e) => {
                e.stopPropagation()
              }}
              onClick={(e) => {
                e.stopPropagation()
              }}
            >
              <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Style</div>
              <DropdownMenuSeparator />

              <FontFamilyMenuComponent editor={editor} currentFontFamily={currentFontFamily} />

              <FontSizeMenuComponent
                editor={editor}
                currentFontSize={currentFontSize}
                setCurrentFontSize={setCurrentFontSize}
              />

              <TextColorMenuComponent
                editor={editor}
                currentTextColor={currentTextColor}
                setCurrentTextColor={setCurrentTextColor}
              />

              <BackgroundColorMenuComponent
                editor={editor}
                currentBackgroundColor={currentBackgroundColor}
                setCurrentBackgroundColor={setCurrentBackgroundColor}
              />

              <ListColorMenuComponent editor={editor} />
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu
            open={openDropdown === "structure"}
            onOpenChange={(open) => setOpenDropdown(open ? "structure" : null)}
          >
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
                onMouseEnter={() => {
                  setOpenDropdown("structure")
                }}
                onMouseDown={(e) => {
                  e.stopPropagation()
                }}
                onClick={(e) => {
                  e.stopPropagation()
                  // Alternar entre abrir e fechar quando clica
                  setOpenDropdown(prev => prev === "structure" ? null : "structure")
                }}
              >
                <AlignLeft className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              side="top"
              align="start"
              className="w-auto"
              onMouseDown={(e) => {
                e.stopPropagation()
              }}
              onClick={(e) => {
                e.stopPropagation()
              }}
            >
              <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Structure</div>
              <DropdownMenuSeparator />
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  <AlignLeft className="mr-2 h-5 w-5" />
                  <span>Headings {currentHeadingLevel ? `(${currentHeadingLevel.toUpperCase()})` : "(Paragraph)"}</span>
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Heading Levels</div>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          $setBlocksType(selection, () => $createHeadingNode("h1"))
                        }
                      })
                    }}
                  >
                    <span className="text-2xl font-bold">H1 - Large Heading</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          $setBlocksType(selection, () => $createHeadingNode("h2"))
                        }
                      })
                    }}
                  >
                    <span className="text-xl font-bold">H2 - Medium Heading</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          $setBlocksType(selection, () => $createHeadingNode("h3"))
                        }
                      })
                    }}
                  >
                    <span className="text-lg font-bold">H3 - Small Heading</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          $setBlocksType(selection, () => $createHeadingNode("h4"))
                        }
                      })
                    }}
                  >
                    <span className="text-base font-bold">H4 - Extra Small</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          $setBlocksType(selection, () => $createHeadingNode("h5"))
                        }
                      })
                    }}
                  >
                    <span className="text-sm font-bold">H5 - Tiny</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          $setBlocksType(selection, () => $createHeadingNode("h6"))
                        }
                      })
                    }}
                  >
                    <span className="text-xs font-bold">H6 - Smallest</span>
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          $setBlocksType(selection, () => $createParagraphNode())
                        }
                      })
                    }}
                  >
                    <span className="text-base">Paragraph - Normal Text</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          const selectedText = selection.getTextContent()
                          if (selectedText) {
                            selection.insertText(`"${selectedText}"`)
                          }
                        }
                      })
                    }}
                  >
                    <span className="text-base italic">Short Quote - Inline Citation</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          $setBlocksType(selection, () => $createQuoteNode())
                        }
                      })
                    }}
                  >
                    <span className="text-base italic border-l-2 border-gray-400 pl-2">
                      Long Quote - Block Citation
                    </span>
                  </DropdownMenuItem>
                </DropdownMenuSubContent>
              </DropdownMenuSub>
              <DropdownMenuSeparator />
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  {currentAlignment === "left" && <AlignLeft className="mr-2 h-5 w-5" />}
                  {currentAlignment === "center" && <AlignCenter className="mr-2 h-5 w-5" />}
                  {currentAlignment === "right" && <AlignRight className="mr-2 h-5 w-5" />}
                  {currentAlignment === "justify" && <AlignJustify className="mr-2 h-5 w-5" />}
                  {currentAlignment === "" && <AlignLeft className="mr-2 h-5 w-5" />}
                  <span>
                    Alignment (
                    {currentAlignment === "left"
                      ? "Left"
                      : currentAlignment === "center"
                        ? "Center"
                        : currentAlignment === "right"
                          ? "Right"
                          : currentAlignment === "justify"
                            ? "Justify"
                            : "Left"}
                    )
                  </span>
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Text Alignment</div>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "left")
                      setTimeout(() => {
                        editor.getEditorState().read(() => updateToolbar())
                      }, 0)
                    }}
                  >
                    <AlignLeft className="mr-2 h-5 w-5" />
                    <span>Align Left</span>
                    {currentAlignment === "left" && <Check className="ml-auto h-5 w-5" />}
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "center")
                      setTimeout(() => {
                        editor.getEditorState().read(() => updateToolbar())
                      }, 0)
                    }}
                  >
                    <AlignCenter className="mr-2 h-5 w-5" />
                    <span>Align Center</span>
                    {currentAlignment === "center" && <Check className="ml-auto h-5 w-5" />}
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "right")
                      setTimeout(() => {
                        editor.getEditorState().read(() => updateToolbar())
                      }, 0)
                    }}
                  >
                    <AlignRight className="mr-2 h-5 w-5" />
                    <span>Align Right</span>
                    {currentAlignment === "right" && <Check className="ml-auto h-5 w-5" />}
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()}
                    onClick={() => {
                      editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "justify")
                      setTimeout(() => {
                        editor.getEditorState().read(() => updateToolbar())
                      }, 0)
                    }}
                  >
                    <AlignJustify className="mr-2 h-5 w-5" />
                    <span>Justify</span>
                    {currentAlignment === "justify" && <Check className="ml-auto h-5 w-5" />}
                  </DropdownMenuItem>
                </DropdownMenuSubContent>
              </DropdownMenuSub>
              <DropdownMenuSeparator />
              <ListMenuComponent
                editor={editor}
                currentListType={currentListType}
                updateToolbar={updateToolbar}
              />
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onClick={() => {
                  editor.update(() => {
                    const selection = $getSelection()
                    if ($isRangeSelection(selection)) {
                      const selectedText = selection.getTextContent()
                      if (selectedText) {
                        const quotedText = `"${selectedText}"`
                        selection.insertText(quotedText)
                      }
                    }
                  })
                }}
              >
                <Quote className="mr-2 h-5 w-5" />
                <span>Quote</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu
            open={openDropdown === "insert"}
            onOpenChange={(open) => setOpenDropdown(open ? "insert" : null)}
          >
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
                onMouseEnter={() => {
                  setOpenDropdown("insert")
                }}
                onMouseDown={(e) => {
                  e.stopPropagation()
                }}
                onClick={(e) => {
                  e.stopPropagation()
                  // Alternar entre abrir e fechar quando clica
                  setOpenDropdown(prev => prev === "insert" ? null : "insert")
                }}
              >
                <TextCursorInput className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              side="top"
              align="start"
              className="w-48"
              onMouseDown={(e) => {
                e.stopPropagation()
              }}
              onClick={(e) => {
                e.stopPropagation()
              }}
            >
              <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Insert</div>
              <DropdownMenuSeparator />
              <LinkMenuComponent />
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      )}
    </>
  )
}
