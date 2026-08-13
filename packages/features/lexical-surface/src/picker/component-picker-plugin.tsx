/**
 * ComponentPickerPlugin — slash (`/`) typeahead menu adapted from
 * facebook/lexical playground `ComponentPickerPlugin/index.tsx`.
 *
 * Core text options are always available. Feature-backed options are
 * filtered using the same flags that mount their command plugins.
 */
"use client"

import * as React from "react"
import { useCallback, useMemo, useState } from "react"
import { createPortal } from "react-dom"
import { $createCodeNode } from "@lexical/code"
import {
  INSERT_CHECK_LIST_COMMAND,
  INSERT_ORDERED_LIST_COMMAND,
  INSERT_UNORDERED_LIST_COMMAND,
} from "@lexical/list"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { INSERT_DIVIDER_LEXICAL_COMMAND } from "../divider"
import {
  LexicalTypeaheadMenuPlugin,
  MenuOption,
  useBasicTypeaheadTriggerMatch,
} from "@lexical/react/LexicalTypeaheadMenuPlugin"
import { $createHeadingNode, $createQuoteNode } from "@lexical/rich-text"
import { $setBlocksType } from "@lexical/selection"
import {
  $createParagraphNode,
  $getSelection,
  $isRangeSelection,
  LexicalEditor,
  TextNode,
} from "lexical"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog"
import {
  BulletedListIcon,
  CheckListIcon,
  CodeBlockIcon,
  Heading1Icon,
  Heading2Icon,
  Heading3Icon,
  Heading4Icon,
  Heading5Icon,
  Heading6Icon,
  HorizontalRuleIcon,
  NumberedListIcon,
  ParagraphIcon,
  QuoteIcon,
  StickyIcon,
} from "../icons"
import { Sigma as EquationIcon } from "lucide-react"
import { Pencil as ExcalidrawIcon } from "lucide-react"
import { Table as TableIcon } from "lucide-react"
import { InsertEquationDialog } from "../equation"
import { INSERT_EXCALIDRAW_COMMAND } from "../excalidraw"
import { InsertTableDialog } from "../table"
import { InsertLayoutDialog } from "../layout"
import { INSERT_COLLAPSIBLE_COMMAND } from "../collapsible"
import { INSERT_STICKY_COMMAND } from "../sticky"
import { INSERT_ADMONITION_LEXICAL_COMMAND } from "../admonition"
import { INSERT_BUTTON_LEXICAL_COMMAND } from "../button"
import { INSERT_MERMAID_LEXICAL_COMMAND } from "../mermaid"
import { INSERT_VEGA_LITE_LEXICAL_COMMAND } from "../vega-lite"
import { INSERT_MEDIA_LEXICAL_COMMAND } from "../media"
import { AlertCircle as AdmonitionIcon, MousePointerClick as ButtonIcon, GitBranch as MermaidIcon, BarChart3 as VegaIcon, Film as MediaIcon, Columns as ColumnsIcon, PanelTopOpen as CollapsibleIcon } from "lucide-react"
import type { LexicalSurfaceFeatures } from "../features"

type DialogRender = (opts: { activeEditor: LexicalEditor; onClose: () => void }) => React.ReactNode

type IconCmp = React.ComponentType<{
  className?: string
  style?: React.CSSProperties
}>

const menuStyle: React.CSSProperties = {
  backgroundColor: "var(--popover, #ffffff)",
  border: "1px solid var(--border, #d1d5db)",
  borderRadius: "0.375rem",
  boxShadow: "0 20px 25px -5px rgb(0 0 0 / 0.2), 0 8px 10px -6px rgb(0 0 0 / 0.2)",
  boxSizing: "border-box",
  color: "var(--popover-foreground, #111827)",
  maxHeight: "min(22.5rem, calc(100vh - 1rem))",
  minWidth: "15rem",
  overflowY: "auto",
  padding: "0.25rem",
  position: "relative",
  zIndex: 1000,
}

const optionStyle: React.CSSProperties = {
  alignItems: "center",
  background: "transparent",
  border: 0,
  borderRadius: "0.25rem",
  color: "inherit",
  cursor: "pointer",
  display: "flex",
  fontSize: "0.875rem",
  gap: "0.5rem",
  lineHeight: "1.25rem",
  minWidth: 0,
  padding: "0.375rem 0.5rem",
  textAlign: "left",
  width: "100%",
}

const selectedOptionStyle: React.CSSProperties = {
  ...optionStyle,
  backgroundColor: "var(--primary, #2563eb)",
  color: "var(--primary-foreground, #ffffff)",
}

class ComponentPickerOption extends MenuOption {
  readonly title: string
  readonly Icon: IconCmp
  readonly keywords: string[]
  readonly onSelect: () => void
  readonly dialog?: { title: string; render: DialogRender }
  readonly enabled: boolean

  constructor(
    title: string,
    options: {
      Icon: IconCmp
      keywords?: string[]
      onSelect?: () => void
      dialog?: { title: string; render: DialogRender }
      enabled?: boolean
    },
  ) {
    super(title)
    this.title = title
    this.Icon = options.Icon
    this.keywords = options.keywords ?? []
    this.onSelect = (options.onSelect ?? (() => {})).bind(this)
    this.dialog = options.dialog
    this.enabled = options.enabled ?? true
  }
}

function getBaseOptions(
  editor: LexicalEditor,
  features: Required<LexicalSurfaceFeatures>,
): ComponentPickerOption[] {
  return [
    new ComponentPickerOption("Paragraph", {
      Icon: ParagraphIcon,
      keywords: ["normal", "paragraph", "p", "text"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createParagraphNode())
          }
        }),
    }),
    new ComponentPickerOption("Heading 1", {
      Icon: Heading1Icon,
      keywords: ["heading", "header", "h1"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h1"))
          }
        }),
    }),
    new ComponentPickerOption("Heading 2", {
      Icon: Heading2Icon,
      keywords: ["heading", "header", "h2"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h2"))
          }
        }),
    }),
    new ComponentPickerOption("Heading 3", {
      Icon: Heading3Icon,
      keywords: ["heading", "header", "h3"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h3"))
          }
        }),
    }),
    new ComponentPickerOption("Heading 4", {
      Icon: Heading4Icon,
      keywords: ["heading", "header", "h4"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h4"))
          }
        }),
    }),
    new ComponentPickerOption("Heading 5", {
      Icon: Heading5Icon,
      keywords: ["heading", "header", "h5"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h5"))
          }
        }),
    }),
    new ComponentPickerOption("Heading 6", {
      Icon: Heading6Icon,
      keywords: ["heading", "header", "h6"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h6"))
          }
        }),
    }),
    new ComponentPickerOption("Numbered List", {
      Icon: NumberedListIcon,
      keywords: ["numbered list", "ordered list", "ol"],
      onSelect: () => editor.dispatchCommand(INSERT_ORDERED_LIST_COMMAND, undefined),
      enabled: features.list,
    }),
    new ComponentPickerOption("Bulleted List", {
      Icon: BulletedListIcon,
      keywords: ["bulleted list", "unordered list", "ul"],
      onSelect: () => editor.dispatchCommand(INSERT_UNORDERED_LIST_COMMAND, undefined),
      enabled: features.list,
    }),
    new ComponentPickerOption("Check List", {
      Icon: CheckListIcon,
      keywords: ["check list", "todo list"],
      onSelect: () => editor.dispatchCommand(INSERT_CHECK_LIST_COMMAND, undefined),
      enabled: features.list && features.checkList,
    }),
    new ComponentPickerOption("Quote", {
      Icon: QuoteIcon,
      keywords: ["block quote"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createQuoteNode())
          }
        }),
    }),
    new ComponentPickerOption("Code", {
      Icon: CodeBlockIcon,
      keywords: ["javascript", "python", "js", "codeblock"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            if (selection.isCollapsed()) {
              $setBlocksType(selection, () => $createCodeNode())
            } else {
              const textContent = selection.getTextContent()
              const codeNode = $createCodeNode()
              selection.insertNodes([codeNode])
              selection.insertRawText(textContent)
            }
          }
        }),
    }),
    new ComponentPickerOption("Divider", {
      Icon: HorizontalRuleIcon,
      keywords: ["horizontal rule", "divider", "hr"],
      onSelect: () => editor.dispatchCommand(INSERT_DIVIDER_LEXICAL_COMMAND, undefined),
      enabled: features.divider,
    }),
    new ComponentPickerOption("Equation", {
      Icon: EquationIcon,
      keywords: ["equation", "katex", "latex", "math"],
      dialog: {
        title: "Insert Equation",
        render: ({ activeEditor, onClose }) => (
          <InsertEquationDialog activeEditor={activeEditor} onClose={onClose} />
        ),
      },
      enabled: features.equation,
    }),
    new ComponentPickerOption("Excalidraw", {
      Icon: ExcalidrawIcon,
      keywords: ["excalidraw", "diagram", "drawing", "sketch"],
      onSelect: () => editor.dispatchCommand(INSERT_EXCALIDRAW_COMMAND, undefined),
      enabled: features.excalidraw,
    }),
    new ComponentPickerOption("Table", {
      Icon: TableIcon,
      keywords: ["table", "grid", "rows", "columns"],
      dialog: {
        title: "Insert Table",
        render: ({ activeEditor, onClose }) => (
          <InsertTableDialog activeEditor={activeEditor} onClose={onClose} />
        ),
      },
      enabled: features.table,
    }),
    new ComponentPickerOption("Columns Layout", {
      Icon: ColumnsIcon,
      keywords: ["columns", "layout", "grid"],
      dialog: {
        title: "Insert Columns Layout",
        render: ({ activeEditor, onClose }) => (
          <InsertLayoutDialog activeEditor={activeEditor} onClose={onClose} />
        ),
      },
      enabled: features.layout,
    }),
    new ComponentPickerOption("Collapsible container", {
      Icon: CollapsibleIcon,
      keywords: ["collapsible", "accordion", "details", "toggle"],
      onSelect: () => editor.dispatchCommand(INSERT_COLLAPSIBLE_COMMAND, undefined),
      enabled: features.collapsible,
    }),
    new ComponentPickerOption("Sticky Note", {
      Icon: StickyIcon,
      keywords: ["sticky", "note", "postit", "memo"],
      onSelect: () => editor.dispatchCommand(INSERT_STICKY_COMMAND, undefined),
      enabled: features.sticky,
    }),
    new ComponentPickerOption("Admonition", {
      Icon: AdmonitionIcon,
      keywords: ["admonition", "callout", "note", "warning", "info", "tip", "alert"],
      onSelect: () => editor.dispatchCommand(INSERT_ADMONITION_LEXICAL_COMMAND, undefined),
      enabled: features.admonition,
    }),
    new ComponentPickerOption("Button", {
      Icon: ButtonIcon,
      keywords: ["button", "link", "action", "cta", "download"],
      onSelect: () => editor.dispatchCommand(INSERT_BUTTON_LEXICAL_COMMAND, undefined),
      enabled: features.button,
    }),
    new ComponentPickerOption("Mermaid Diagram", {
      Icon: MermaidIcon,
      keywords: ["mermaid", "diagram", "flowchart", "chart", "graph", "sequence", "gantt", "class"],
      onSelect: () => editor.dispatchCommand(INSERT_MERMAID_LEXICAL_COMMAND, undefined),
      enabled: features.mermaid,
    }),
    new ComponentPickerOption("Vega-Lite Chart", {
      Icon: VegaIcon,
      keywords: ["vega", "vega-lite", "chart", "graph", "plot", "visualization", "bar", "line", "scatter"],
      onSelect: () => editor.dispatchCommand(INSERT_VEGA_LITE_LEXICAL_COMMAND, undefined),
      enabled: features.vegaLite,
    }),
    new ComponentPickerOption("Media Block", {
      Icon: MediaIcon,
      keywords: ["media", "image", "video", "audio", "gallery", "photo", "music", "mp4", "mp3"],
      onSelect: () => editor.dispatchCommand(INSERT_MEDIA_LEXICAL_COMMAND, { mediaType: "image" }),
      enabled: features.media,
    }),
  ].filter((option) => option.enabled)
}

export default function ComponentPickerPlugin({
  features,
}: {
  features: Required<LexicalSurfaceFeatures>
}) {
  const [editor] = useLexicalComposerContext()
  const [queryString, setQueryString] = useState<string | null>(null)
  const [pendingDialog, setPendingDialog] = useState<
    | { title: string; render: DialogRender }
    | null
  >(null)

  const checkForTriggerMatch = useBasicTypeaheadTriggerMatch("/", {
    allowWhitespace: true,
    minLength: 0,
  })

  const options = useMemo(() => {
    const baseOptions = getBaseOptions(editor, features)
    if (!queryString) {
      return baseOptions
    }
    const regex = new RegExp(queryString, "i")
    return baseOptions.filter(
      (option) =>
        regex.test(option.title) || option.keywords.some((keyword) => regex.test(keyword)),
    )
  }, [editor, features, queryString])

  const onSelectOption = useCallback(
    (
      selectedOption: ComponentPickerOption,
      nodeToRemove: TextNode | null,
      closeMenu: () => void,
    ) => {
      editor.update(() => {
        nodeToRemove?.remove()
        if (!selectedOption.dialog) {
          selectedOption.onSelect()
        }
        closeMenu()
      })
      if (selectedOption.dialog) {
        setPendingDialog(selectedOption.dialog)
      }
    },
    [editor],
  )

  return (
    <>
      <LexicalTypeaheadMenuPlugin<ComponentPickerOption>
      onQueryChange={setQueryString}
      onSelectOption={onSelectOption}
      triggerFn={checkForTriggerMatch}
      options={options}
      menuRenderFn={(anchorElementRef, { selectedIndex, selectOptionAndCleanUp, setHighlightedIndex }) => {
        if (!anchorElementRef.current || options.length === 0) {
          return null
        }

        anchorElementRef.current.style.zIndex = "1000"

        return createPortal(
          <div
            role="listbox"
            style={menuStyle}
          >
            {options.map((option, i) => {
              const Icon = option.Icon
              const isSelected = selectedIndex === i
              return (
                <button
                  key={option.key}
                  ref={(el) => option.setRefElement(el)}
                  type="button"
                  role="option"
                  aria-selected={isSelected}
                  tabIndex={-1}
                  onMouseEnter={() => setHighlightedIndex(i)}
                  onClick={() => selectOptionAndCleanUp(option)}
                  style={isSelected ? selectedOptionStyle : optionStyle}
                >
                  <Icon style={{ flexShrink: 0, height: "1rem", width: "1rem" }} />
                  <span style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                    {option.title}
                  </span>
                </button>
              )
            })}
          </div>,
          anchorElementRef.current,
        )
      }}
    />
    <Dialog open={pendingDialog !== null} onOpenChange={(open) => { if (!open) setPendingDialog(null) }}>
      <DialogContent
        // O teclado virtual do MathLive (`.ML__keyboard`) é montado em
        // `document.body`, fora da árvore do Dialog. Enquanto ele estiver
        // aberto (`<body data-math-keyboard-open>`), TODO clique fora é
        // ignorado — assim o usuário não fecha acidentalmente o diálogo
        // ao tocar numa tecla. Também tratamos cliques no próprio overlay
        // do teclado como "dentro".
        onPointerDownOutside={(e) => {
          const target = e.target as HTMLElement | null
          if (document.body.hasAttribute("data-math-keyboard-open")) e.preventDefault()
          else if (target?.closest(".ML__keyboard, .ML__virtual-keyboard, math-field")) e.preventDefault()
        }}
        onInteractOutside={(e) => {
          const target = e.target as HTMLElement | null
          if (document.body.hasAttribute("data-math-keyboard-open")) e.preventDefault()
          else if (target?.closest(".ML__keyboard, .ML__virtual-keyboard, math-field")) e.preventDefault()
        }}
        onFocusOutside={(e) => {
          const target = e.target as HTMLElement | null
          if (document.body.hasAttribute("data-math-keyboard-open")) e.preventDefault()
          else if (target?.closest(".ML__keyboard, .ML__virtual-keyboard, math-field")) e.preventDefault()
        }}
        onEscapeKeyDown={(e) => {
          // Esc deve primeiro fechar o teclado, não o diálogo.
          if (document.body.hasAttribute("data-math-keyboard-open")) e.preventDefault()
        }}
      >
        <DialogHeader>
          <DialogTitle>{pendingDialog?.title}</DialogTitle>
        </DialogHeader>
        {pendingDialog?.render({ activeEditor: editor, onClose: () => setPendingDialog(null) })}
      </DialogContent>
    </Dialog>
    </>
  )
}
