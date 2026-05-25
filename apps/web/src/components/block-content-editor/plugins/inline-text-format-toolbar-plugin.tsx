/**
 * Inline (fixed) text-format toolbar for inline Lexical editors.
 *
 * Renders a compact, always-visible button bar above the content
 * editable so users can apply common formatting (headings, bold/italic
 * /underline/strikethrough/code, lists, alignment, link) without having
 * to make a selection first. The pre-existing
 * `FloatingTextFormatToolbarPlugin` remains as the advanced selection
 * toolbar (font family/size, custom colors, list marker colors, case
 * transforms, etc.) — the two are complementary.
 */

"use client"

import { useCallback, useEffect, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import {
  $createHeadingNode,
  $createQuoteNode,
  $isHeadingNode,
  type HeadingTagType,
} from "@lexical/rich-text"
import { $setBlocksType } from "@lexical/selection"
import { $createListItemNode, REMOVE_LIST_COMMAND } from "@lexical/list"
import { TOGGLE_LINK_COMMAND, $isLinkNode } from "@lexical/link"
import {
  $createParagraphNode,
  $createTextNode,
  $getSelection,
  $isRangeSelection,
  FORMAT_ELEMENT_COMMAND,
  FORMAT_TEXT_COMMAND,
  SELECTION_CHANGE_COMMAND,
  COMMAND_PRIORITY_LOW,
  type LexicalNode,
} from "lexical"
import {
  AlignCenter,
  AlignJustify,
  AlignLeft,
  AlignRight,
  Bold,
  ChevronDown,
  Code,
  Italic,
  Link as LinkIcon,
  List,
  ListOrdered,
  Strikethrough,
  Underline,
  X,
} from "lucide-react"
import { mergeRegister } from "@lexical/utils"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { cn } from "@/lib/utils"
import {
  $createCustomListNode,
  $isCustomListNode,
} from "../nodes/custom-list-node"

type BlockType = "paragraph" | "h1" | "h2" | "h3" | "h4" | "h5" | "h6" | "quote"

const BLOCK_OPTIONS: Array<{ value: BlockType; label: string }> = [
  { value: "paragraph", label: "Parágrafo" },
  { value: "h1", label: "Título 1" },
  { value: "h2", label: "Título 2" },
  { value: "h3", label: "Título 3" },
  { value: "h4", label: "Título 4" },
  { value: "h5", label: "Título 5" },
  { value: "h6", label: "Título 6" },
  { value: "quote", label: "Citação" },
]

const UNORDERED_LIST_STYLES: Array<{ icon: string; name: string; style: string }> = [
  { icon: "•", name: "Disco (padrão)", style: "disc" },
  { icon: "○", name: "Círculo", style: "circle" },
  { icon: "■", name: "Quadrado", style: "square" },
  { icon: "▶", name: "Seta", style: "arrow" },
  { icon: "★", name: "Estrela", style: "star" },
]

const ORDERED_LIST_STYLES: Array<{ icon: string; name: string; style: string }> = [
  { icon: "1.", name: "Números (padrão)", style: "decimal" },
  { icon: "A.", name: "Letras maiúsculas", style: "upper-alpha" },
  { icon: "a.", name: "Letras minúsculas", style: "lower-alpha" },
  { icon: "I.", name: "Romanos maiúsculos", style: "upper-roman" },
  { icon: "i.", name: "Romanos minúsculos", style: "lower-roman" },
  { icon: "01.", name: "Números com zero", style: "decimal-leading-zero" },
]

export function InlineTextFormatToolbarPlugin() {
  const [editor] = useLexicalComposerContext()
  const [isBold, setIsBold] = useState(false)
  const [isItalic, setIsItalic] = useState(false)
  const [isUnderline, setIsUnderline] = useState(false)
  const [isStrikethrough, setIsStrikethrough] = useState(false)
  const [isCode, setIsCode] = useState(false)
  const [isLink, setIsLink] = useState(false)
  const [blockType, setBlockType] = useState<BlockType>("paragraph")
  const [listType, setListType] = useState<"" | "bullet" | "number">("")
  const [alignment, setAlignment] = useState<string>("")

  const updateState = useCallback(() => {
    const selection = $getSelection()
    if (!$isRangeSelection(selection)) return

    setIsBold(selection.hasFormat("bold"))
    setIsItalic(selection.hasFormat("italic"))
    setIsUnderline(selection.hasFormat("underline"))
    setIsStrikethrough(selection.hasFormat("strikethrough"))
    setIsCode(selection.hasFormat("code"))

    const anchorNode = selection.anchor.getNode()
    const topLevel =
      anchorNode.getKey() === "root"
        ? anchorNode
        : anchorNode.getTopLevelElementOrThrow()

    if ($isHeadingNode(topLevel)) {
      setBlockType(topLevel.getTag() as BlockType)
    } else if (topLevel.getType() === "quote") {
      setBlockType("quote")
    } else {
      setBlockType("paragraph")
    }

    setAlignment(String(topLevel.getFormat?.() ?? ""))

    const parent = anchorNode.getParent()
    const parentType = parent?.getType()
    if (parent && (parentType === "list" || parentType === "custom-list")) {
      const lt = (parent as unknown as { getListType: () => "bullet" | "number" }).getListType()
      setListType(lt)
    } else {
      setListType("")
    }

    const nodes = selection.getNodes()
    let inLink = false
    for (const node of nodes) {
      if ($isLinkNode(node) || $isLinkNode(node.getParent())) {
        inLink = true
        break
      }
    }
    setIsLink(inLink)
  }, [])

  useEffect(() => {
    return mergeRegister(
      editor.registerUpdateListener(({ editorState }) => {
        editorState.read(() => updateState())
      }),
      editor.registerCommand(
        SELECTION_CHANGE_COMMAND,
        () => {
          updateState()
          return false
        },
        COMMAND_PRIORITY_LOW,
      ),
    )
  }, [editor, updateState])

  const applyBlockType = useCallback(
    (next: BlockType) => {
      editor.update(() => {
        const selection = $getSelection()
        if (!$isRangeSelection(selection)) return
        if (next === "paragraph") {
          $setBlocksType(selection, () => $createParagraphNode())
        } else if (next === "quote") {
          $setBlocksType(selection, () => $createQuoteNode())
        } else {
          $setBlocksType(selection, () => $createHeadingNode(next as HeadingTagType))
        }
      })
    },
    [editor],
  )

  const insertCustomList = useCallback(
    (kind: "bullet" | "number", styleType: string) => {
      editor.update(() => {
        const selection = $getSelection()
        if (!$isRangeSelection(selection)) return

        // Reuse current marker color if cursor is already inside a list.
        let currentColor = "#3b82f6"
        let currentNode: LexicalNode | null = selection.anchor.getNode()
        while (currentNode) {
          if ($isCustomListNode(currentNode)) {
            currentColor = currentNode.getMarkerColor()
            break
          }
          currentNode = currentNode.getParent()
        }

        const customListNode = $createCustomListNode(kind, 1, styleType, currentColor)
        const listItemNode = $createListItemNode()

        const selectedText = selection.getTextContent()
        if (selectedText) {
          listItemNode.append($createTextNode(selectedText))
          selection.removeText()
        }

        customListNode.append(listItemNode)
        selection.insertNodes([customListNode])
        listItemNode.selectEnd()
      })
    },
    [editor],
  )

  const removeList = useCallback(() => {
    editor.dispatchCommand(REMOVE_LIST_COMMAND, undefined)
  }, [editor])

  const insertLink = useCallback(() => {
    if (isLink) {
      editor.dispatchCommand(TOGGLE_LINK_COMMAND, null)
      return
    }
    const url = typeof window !== "undefined" ? window.prompt("URL do link:") : null
    if (url) {
      editor.dispatchCommand(TOGGLE_LINK_COMMAND, url)
    }
  }, [editor, isLink])

  const btn = (active: boolean) =>
    cn(
      "h-8 w-8 p-0",
      active
        ? "bg-accent text-accent-foreground"
        : "hover:bg-accent/60",
    )

  return (
    <div className="flex flex-wrap items-center gap-1 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900/40 px-2 py-1.5">
      <Select value={blockType} onValueChange={(v) => applyBlockType(v as BlockType)}>
        <SelectTrigger className="h-8 w-[140px] text-xs">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {BLOCK_OPTIONS.map((o) => (
            <SelectItem key={o.value} value={o.value} className="text-xs">
              {o.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <span className="mx-1 h-5 w-px bg-gray-300 dark:bg-gray-700" />

      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(isBold)}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "bold")}
        title="Negrito"
        aria-label="Negrito"
      >
        <Bold className="h-4 w-4" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(isItalic)}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "italic")}
        title="Itálico"
        aria-label="Itálico"
      >
        <Italic className="h-4 w-4" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(isUnderline)}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "underline")}
        title="Sublinhado"
        aria-label="Sublinhado"
      >
        <Underline className="h-4 w-4" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(isStrikethrough)}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "strikethrough")}
        title="Tachado"
        aria-label="Tachado"
      >
        <Strikethrough className="h-4 w-4" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(isCode)}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "code")}
        title="Código"
        aria-label="Código"
      >
        <Code className="h-4 w-4" />
      </Button>

      <span className="mx-1 h-5 w-px bg-gray-300 dark:bg-gray-700" />

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className={cn(
              "h-8 px-1.5 gap-0.5",
              listType === "bullet"
                ? "bg-accent text-accent-foreground"
                : "hover:bg-accent/60",
            )}
            title="Lista com marcadores"
            aria-label="Lista com marcadores"
          >
            <List className="h-4 w-4" />
            <ChevronDown className="h-3 w-3 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <div className="px-2 py-1 text-xs font-medium text-muted-foreground">
            Lista com marcadores
          </div>
          <DropdownMenuSeparator />
          {UNORDERED_LIST_STYLES.map((s) => (
            <DropdownMenuItem
              key={s.style}
              onSelect={(e) => e.preventDefault()}
              onClick={() => insertCustomList("bullet", s.style)}
            >
              <span className="mr-2 w-5 text-center">{s.icon}</span>
              <span>{s.name}</span>
            </DropdownMenuItem>
          ))}
          {listType === "bullet" && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem onSelect={(e) => e.preventDefault()} onClick={removeList}>
                <X className="mr-2 h-4 w-4" />
                <span>Remover lista</span>
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className={cn(
              "h-8 px-1.5 gap-0.5",
              listType === "number"
                ? "bg-accent text-accent-foreground"
                : "hover:bg-accent/60",
            )}
            title="Lista numerada"
            aria-label="Lista numerada"
          >
            <ListOrdered className="h-4 w-4" />
            <ChevronDown className="h-3 w-3 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <div className="px-2 py-1 text-xs font-medium text-muted-foreground">
            Lista numerada
          </div>
          <DropdownMenuSeparator />
          {ORDERED_LIST_STYLES.map((s) => (
            <DropdownMenuItem
              key={s.style}
              onSelect={(e) => e.preventDefault()}
              onClick={() => insertCustomList("number", s.style)}
            >
              <span className="mr-2 w-5 text-center">{s.icon}</span>
              <span>{s.name}</span>
            </DropdownMenuItem>
          ))}
          {listType === "number" && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem onSelect={(e) => e.preventDefault()} onClick={removeList}>
                <X className="mr-2 h-4 w-4" />
                <span>Remover lista</span>
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      <span className="mx-1 h-5 w-px bg-gray-300 dark:bg-gray-700" />

      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(alignment === "left" || alignment === "")}
        onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "left")}
        title="Alinhar à esquerda"
        aria-label="Alinhar à esquerda"
      >
        <AlignLeft className="h-4 w-4" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(alignment === "center")}
        onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "center")}
        title="Centralizar"
        aria-label="Centralizar"
      >
        <AlignCenter className="h-4 w-4" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(alignment === "right")}
        onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "right")}
        title="Alinhar à direita"
        aria-label="Alinhar à direita"
      >
        <AlignRight className="h-4 w-4" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(alignment === "justify")}
        onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "justify")}
        title="Justificar"
        aria-label="Justificar"
      >
        <AlignJustify className="h-4 w-4" />
      </Button>

      <span className="mx-1 h-5 w-px bg-gray-300 dark:bg-gray-700" />

      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={btn(isLink)}
        onClick={insertLink}
        title="Inserir/remover link"
        aria-label="Inserir/remover link"
      >
        <LinkIcon className="h-4 w-4" />
      </Button>
    </div>
  )
}
