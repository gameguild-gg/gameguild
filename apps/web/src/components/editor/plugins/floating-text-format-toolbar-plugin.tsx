"use client"

import { DropdownMenuTrigger } from "@/components/editor/ui/dropdown-menu"

import { useCallback, useEffect, useRef, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import {
  $getSelection,
  $isRangeSelection,
  FORMAT_TEXT_COMMAND,
  SELECTION_CHANGE_COMMAND,
  FORMAT_ELEMENT_COMMAND,
} from "lexical"
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
} from "lucide-react"
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
} from "@/components/editor/ui/dropdown-menu"
import { TOGGLE_LINK_COMMAND } from "@lexical/link"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/editor/ui/dialog"
import { Input } from "@/components/editor/ui/input"
import { Label } from "@/components/editor/ui/label"
import { Button } from "@/components/editor/ui/button"
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
  const [isSubscript, setIsSubscript] = useState(false)
  const [isSuperscript, setIsSuperscript] = useState(false)
  const [isCode, setIsCode] = useState(false)
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
      setIsSubscript(false)
      setIsSuperscript(false)
      setIsCode(false)
      setIsUnderline(false)
      return
    }

    setIsBold(selection.hasFormat("bold"))
    setIsItalic(selection.hasFormat("italic"))
    setIsUnderline(selection.hasFormat("underline"))
    setIsSubscript(selection.hasFormat("subscript"))
    setIsSuperscript(selection.hasFormat("superscript"))
    setIsCode(selection.hasFormat("code"))

    setIsText(selection.getTextContent().length > 0)

    // Check current heading level
    const anchorNode = selection.anchor.getNode()
    const element = anchorNode.getKey() === "root" ? anchorNode : anchorNode.getTopLevelElementOrThrow()
    if ($isHeadingNode(element)) {
      setCurrentHeadingLevel(element.getTag())
    } else {
      setCurrentHeadingLevel(null)
    }

    // Check if current selection is a quote
    const parentElement = anchorNode.getParent()
    if (parentElement && parentElement.getType() === "quote") {
      setIsQuote(true)
    } else {
      setIsQuote(false)
    }

    // Get current font family
    const nodes = selection.getNodes()
    if (nodes.length > 0) {
      const firstNode = nodes[0]
      // Ensure the node has getStyle and explicitly convert its result to string
      const style = firstNode.getStyle ? String(firstNode.getStyle()) : ""
      const fontFamilyMatch = style.match(/font-family:\s*([^;]+)/)
      if (fontFamilyMatch) {
        setCurrentFontFamily(fontFamilyMatch[1].replace(/['"]/g, ""))
      } else {
        setCurrentFontFamily("")
      }
    } else {
      setCurrentFontFamily("") // Reset if no nodes are selected
    }

    // Get current font size
    if (nodes.length > 0) {
      const firstNode = nodes[0]
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

    // Get current text color
    if (nodes.length > 0) {
      const firstNode = nodes[0]
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

    // Get current background color
    if (nodes.length > 0) {
      const firstNode = nodes[0]
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

    // Get current alignment using getFormat()
    if (selection) {
      const element = anchorNode.getTopLevelElementOrThrow()
      setCurrentAlignment(element.getFormat())
    } else {
      setCurrentAlignment("")
    }

    // Get current list type
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
      // Calculate position immediately, even without toolbarRef
      const toolbarHeight = 60 // Updated toolbar height
      const toolbarWidth = 240 // Updated toolbar width

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
          {/* Dropdown: Formatação (Text Formatting) */}
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
                  editor.getEditorState().read(() => updateToolbar())
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
                  // Force immediate update
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
                  // Force immediate update
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
                  editor.dispatchCommand(FORMAT_TEXT_COMMAND, "subscript")
                  editor.getEditorState().read(() => updateToolbar())
                }}
              >
                <Subscript className="mr-2 h-5 w-5" />
                <span>Subscript</span>
                {isSubscript && <Check className="ml-auto h-5 w-5" />}
              </DropdownMenuItem>
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

          {/* Dropdown: Estilo (Style) - Font Family, Font Size, Text Color */}
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

          {/* Dropdown: Estrutura (Structure) */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
              >
                <AlignLeft className="h-5 w-5" /> {/* Using AlignLeft as a generic icon for structure */}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent side="top" align="start" className="w-auto">
              <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Structure</div>
              <DropdownMenuSeparator />
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  <AlignLeft className="mr-2 h-5 w-5" /> {/* Usando AlignLeft como ícone para Títulos */}
                  <span>Headings {currentHeadingLevel ? `(${currentHeadingLevel.toUpperCase()})` : "(Paragraph)"}</span>
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent side="right" align="start">
                  <div className="px-2 py-1 text-xs font-medium text-muted-foreground">Heading Levels</div>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()} // Prevent closing
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
                    onSelect={(e) => e.preventDefault()} // Prevent closing
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
                    onSelect={(e) => e.preventDefault()} // Prevent closing
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
                    onSelect={(e) => e.preventDefault()} // Prevent closing
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
                    onSelect={(e) => e.preventDefault()} // Prevent closing
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
                    onSelect={(e) => e.preventDefault()} // Prevent closing
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
                    onSelect={(e) => e.preventDefault()} // Prevent closing
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          // Convert to normal paragraph
                          $setBlocksType(selection, () => $createParagraphNode())
                        }
                      })
                    }}
                  >
                    <span className="text-base">Paragraph - Normal Text</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()} // Prevent closing
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          const selectedText = selection.getTextContent()
                          if (selectedText) {
                            // Wrap selected text in <q> tags for short quote
                            const quotedText = `"${selectedText}"`
                            selection.insertText(quotedText)
                          }
                        }
                      })
                    }}
                  >
                    <span className="text-base italic">Short Quote - Inline Citation</span>
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={(e) => e.preventDefault()} // Prevent closing
                    onClick={() => {
                      editor.update(() => {
                        const selection = $getSelection()
                        if ($isRangeSelection(selection)) {
                          // Convert to blockquote
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
                <DropdownMenuSubContent side="right" align="start">
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
              <DropdownMenuSeparator /> {/* Mantenha este separador se houver outras opções após o alinhamento */}
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

          {/* Dropdown: Inserir (Insert) */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-12 w-12 hover:bg-accent/80 transition-colors duration-150"
              >
                <TextCursorInput className="h-5 w-5" /> {/* Using TextCursorInput as a generic icon for insert */}
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
