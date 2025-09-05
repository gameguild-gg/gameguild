"use client"

import { DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import {
  Bold,
  Italic,
  Quote,
  LinkIcon,
  Subscript,
  Superscript,
  Code,
  Palette,
  AlignLeft,
  AlignCenter,
  AlignRight,
  AlignJustify,
  TextCursorInput,
  Check,
  Underline,
  Strikethrough,
  Type,
} from "lucide-react"
import { useCallback, useEffect, useRef, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import {
  $getSelection,
  $isRangeSelection,
  FORMAT_TEXT_COMMAND,
  SELECTION_CHANGE_COMMAND,
  FORMAT_ELEMENT_COMMAND,
} from "lexical"
import { $createHeadingNode, $isHeadingNode, type HeadingTagType, $createQuoteNode } from "@lexical/rich-text"
import { $setBlocksType } from "@lexical/selection"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
} from "@/components/ui/dropdown-menu"
import { TOGGLE_LINK_COMMAND } from "@lexical/link"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { ListMenuComponent } from "./floating-text-components/list-menu-component"
import { FontFamilyMenuComponent } from "./floating-text-components/font-family-menu-component"
import { FontSizeMenuComponent } from "./floating-text-components/font-size-menu-component"
import { TextColorMenuComponent } from "./floating-text-components/text-color-menu-component"
import { BackgroundColorMenuComponent } from "./floating-text-components/background-color-menu-component"
import { $createParagraphNode } from "lexical"

export function FloatingTextFormatToolbarPlugin() {
  const [editor] = useLexicalComposerContext()
  const toolbarRef = useRef<HTMLDivElement>(null)
  const [isText, setIsText] = useState(false)
  const [isLink, setIsLink] = useState(false)
  const [isBold, setIsBold] = useState(false)
  const [isItalic, setIsItalic] = useState(false)
  const [isUnderline, setIsUnderline] = useState(false)
  const [isStrikethrough, setIsStrikethrough] = useState(false)
  const [isSubscript, setIsSubscript] = useState(false)
  const [isSuperscript, setIsSuperscript] = useState(false)
  const [isCode, setIsCode] = useState(false)
  const [currentCaseFormat, setCurrentCaseFormat] = useState<"uppercase" | "lowercase" | "capitalize" | null>(null)
  const [selectedElementKey, setSelectedElementKey] = useState<string | null>(null)
  const [position, setPosition] = useState<{ top: number; left: number } | null>(null)
  const [currentHeadingLevel, setCurrentHeadingLevel] = useState<HeadingTagType | null>(null)
  const [isQuote, setIsQuote] = useState(false)
  const [currentFontFamily, setCurrentFontFamily] = useState<string>("")
  const [currentFontSize, setCurrentFontSize] = useState<string>("")
  const [showFontSizeInput, setShowFontSizeInput] = useState(false)
  const [currentTextColor, setCurrentTextColor] = useState<string>("")
  const [currentBackgroundColor, setCurrentBackgroundColor] = useState<string>("")
  const [currentAlignment, setCurrentAlignment] = useState<string>("")
  const [currentListType, setCurrentListType] = useState<string>("")

  const [showLinkDialog, setShowLinkDialog] = useState(false)
  const [linkUrl, setLinkUrl] = useState("")
  const [selectedText, setSelectedText] = useState("")

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
      setIsStrikethrough(false)
      setCurrentCaseFormat(null)
      return
    }

    setIsBold(selection.hasFormat("bold"))
    setIsItalic(selection.hasFormat("italic"))
    setIsUnderline(selection.hasFormat("underline"))
    setIsStrikethrough(selection.hasFormat("strikethrough"))
    setIsSubscript(selection.hasFormat("subscript"))
    setIsSuperscript(selection.hasFormat("superscript"))
    setIsCode(selection.hasFormat("code"))

    if (selection.getNodes().length > 0) {
      const firstNode = selection.getNodes()[0]
      const style = firstNode.getStyle ? String(firstNode.getStyle()) : ""

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

    setIsText(selection.getTextContent().length > 0)

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
      const firstNode = selection.getNodes()[0]
      const style = firstNode.getStyle ? String(firstNode.getStyle()) : ""
      const fontFamilyMatch = style.match(/font-family:\s*([^;]+)/)
      if (fontFamilyMatch) {
        setCurrentFontFamily(fontFamilyMatch[1].replace(/['"]/g, ""))
      } else {
        setCurrentFontFamily("")
      }
    } else {
      setCurrentFontFamily("")
    }

    if (selection.getNodes().length > 0) {
      const firstNode = selection.getNodes()[0]
      const style = firstNode.getStyle ? String(firstNode.getStyle()) : ""
      const fontSizeMatch = style.match(/font-size:\s*([^;]+)/)
      if (fontSizeMatch) {
        setCurrentFontSize(fontSizeMatch[1].replace(/['']/g, ""))
      } else {
        setCurrentFontSize("")
      }
    } else {
      setCurrentFontSize("")
    }

    if (selection.getNodes().length > 0) {
      const firstNode = selection.getNodes()[0]
      const style = firstNode.getStyle ? String(firstNode.getStyle()) : ""
      const colorMatch = style.match(/(?<!background-)color:\s*([^;]+)/)
      if (colorMatch) {
        setCurrentTextColor(colorMatch[1].replace(/['']/g, "").trim())
      } else {
        setCurrentTextColor("")
      }
    } else {
      setCurrentTextColor("")
    }

    if (selection.getNodes().length > 0) {
      const firstNode = selection.getNodes()[0]
      const style = firstNode.getStyle ? String(firstNode.getStyle()) : ""
      const backgroundColorMatch = style.match(/background-color:\s*([^;]+)/)
      if (backgroundColorMatch) {
        setCurrentBackgroundColor(backgroundColorMatch[1].replace(/['']/g, "").trim())
      } else {
        setCurrentBackgroundColor("")
      }
    } else {
      setCurrentBackgroundColor("")
    }

    if (selection) {
      const element = anchorNode.getTopLevelElementOrThrow()
      setCurrentAlignment(element.getFormat())
    } else {
      setCurrentAlignment("")
    }

    const parentElementList = anchorNode.getParent()
    if (parentElementList && parentElementList.getType() === "list") {
      setCurrentListType(parentElementList.getListType())
    } else {
      setCurrentListType("")
    }

    const nativeSelection = window.getSelection()
    const range = nativeSelection?.getRangeAt(0)
    const rect = range?.getBoundingClientRect()

    if (rect) {
      const toolbarHeight = 60
      const toolbarWidth = 240

      setPosition({
        top: rect.top - toolbarHeight - 12,
        left: Math.max(8, rect.left + (rect.width - toolbarWidth) / 2),
      })
    } else {
      setPosition(null)
    }
  }, [])

  const handleInsertLink = useCallback(() => {
    if (linkUrl.trim()) {
      editor.dispatchCommand(TOGGLE_LINK_COMMAND, linkUrl.trim())
      setShowLinkDialog(false)
      setLinkUrl("")
      setSelectedText("")
    }
  }, [editor, linkUrl])

  const handleLinkButtonClick = useCallback(() => {
    editor.getEditorState().read(() => {
      const selection = $getSelection()
      if ($isRangeSelection(selection)) {
        const text = selection.getTextContent()
        setSelectedText(text)
        setShowLinkDialog(true)
      }
    })
  }, [editor])

    const applyCaseFormat = useCallback(
    (caseType: "uppercase" | "lowercase" | "capitalize") => {
      editor.update(() => {
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          const currentStyle = selection.getNodes()[0]?.getStyle?.() || ""
          const cleanStyle = currentStyle
            .replace(/text-transform:\s*[^;]+;?/g, "")
            .replace(/;;/g, ";")
            .replace(/^;|;$/g, "")

          const newStyle = cleanStyle ? `${cleanStyle}; text-transform: ${caseType}` : `text-transform: ${caseType}`

          selection.getNodes().forEach((node) => {
            if (node.setStyle) {
              node.setStyle(newStyle)
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
          if (node.getStyle && node.setStyle) {
            const currentStyle = node.getStyle()
            const cleanStyle = currentStyle
              .replace(/text-transform:\s*[^;]+;?/g, "")
              .replace(/;;/g, ";")
              .replace(/^;|;$/g, "")

            node.setStyle(cleanStyle)
          }
        })
        setCurrentCaseFormat(null)
      }
    })
  }, [editor])

  useEffect(() => {
    return editor.registerCommand(
      SELECTION_CHANGE_COMMAND,
      () => {
        updateToolbar()
        return false
      },
      1,
    )
  }, [editor, updateToolbar])

  if (!isText) {
    return null
  }

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
        >
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
              >
                <Bold className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent side="top" align="start" className="w-48">
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
              <DropdownMenuItem
                onSelect={(e) => e.preventDefault()}
                onClick={() => {
                  editor.dispatchCommand(FORMAT_TEXT_COMMAND, "underline")
                  setTimeout(() => {
                    editor.getEditorState().read(() => updateToolbar())
                  }, 0)
                }}
              >
                <Underline className="mr-2 h-5 w-5" />
                <span>Underline</span>
                {isUnderline && <Check className="ml-auto h-5 w-5" />}
              </DropdownMenuItem>
              <DropdownMenuItem
                onSelect={(e) => e.preventDefault()}
                onClick={() => {
                  editor.dispatchCommand(FORMAT_TEXT_COMMAND, "strikethrough")
                  setTimeout(() => {
                    editor.getEditorState().read(() => updateToolbar())
                  }, 0)
                }}
              >
                <Strikethrough className="mr-2 h-5 w-5" />
                <span>Strikethrough</span>
                {isStrikethrough && <Check className="ml-auto h-5 w-5" />}
              </DropdownMenuItem>
              <DropdownMenuItem
                onSelect={(e) => e.preventDefault()}
                onClick={() => {
                  editor.dispatchCommand(FORMAT_TEXT_COMMAND, "subscript")
                  editor.getEditorState().read(() => updateToolbar())
                }}
              >
                <Subscript className="mr-2 h-5 w-5" />
                <span>Subscript</span>
                {isSubscript && <Check className="ml-auto h-5 w-5" />}
              </DropdownMenuItem>
              <DropdownMenuSeparator />

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
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onSelect={(e) => e.preventDefault()}
                onClick={() => {
                  editor.dispatchCommand(FORMAT_TEXT_COMMAND, "superscript")
                  editor.getEditorState().read(() => updateToolbar())
                }}
              >
                <Superscript className="mr-2 h-5 w-5" />
                <span>Superscript</span>
                {isSuperscript && <Check className="ml-auto h-5 w-5" />}
              </DropdownMenuItem>
              <DropdownMenuItem
                onSelect={(e) => e.preventDefault()}
                onClick={() => {
                  editor.dispatchCommand(FORMAT_TEXT_COMMAND, "code")
                  editor.getEditorState().read(() => updateToolbar())
                }}
              >
                <Code className="mr-2 h-5 w-5" />
                <span>Code</span>
                {isCode && <Check className="ml-auto h-5 w-5" />}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
              >
                <Palette className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent side="top" align="start" className="w-64">
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
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
              >
                <AlignLeft className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent side="top" align="start" className="w-auto">
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
                showCurrentType={true}
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

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
              >
                <TextCursorInput className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent side="top" align="start" className="w-48">
              <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Insert</div>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={handleLinkButtonClick}>
                <LinkIcon className="mr-2 h-5 w-5" />
                <span>Link</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      )}
      <Dialog open={showLinkDialog} onOpenChange={setShowLinkDialog}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Add Link</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="selected-text">Selected Text</Label>
              <Input id="selected-text" value={selectedText} readOnly className="bg-muted" />
            </div>
            <div>
              <Label htmlFor="link-url">URL</Label>
              <Input
                id="link-url"
                type="url"
                placeholder="https://example.com"
                value={linkUrl}
                onChange={(e) => setLinkUrl(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    handleInsertLink()
                  }
                }}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowLinkDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleInsertLink} disabled={!linkUrl.trim()}>
              Add Link
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
