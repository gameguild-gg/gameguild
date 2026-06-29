/**
 * ComponentPickerPlugin — slash (`/`) typeahead menu adapted from
 * facebook/lexical playground `ComponentPickerPlugin/index.tsx`.
 *
 * Wave A surfaces these options only:
 *   Paragraph, Heading 1–3, Bulleted/Numbered/Check List, Quote, Code,
 *   Horizontal Rule.
 * Image/Table/Equation/Excalidraw/Poll/Layout/Sticky/Date/Embeds are
 * intentionally excluded — they belong to Wave B and will plug in as
 * additional `ComponentPickerOption`s alongside the Wave A set.
 *
 * Our embed block menu (the legacy "/"-menu) is moved to `//` in
 * `block-insert-menu-plugin.tsx`, so the two pickers no longer collide.
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
import { cn } from "@/lib/utils"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
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
import { INSERT_STICKY_COMMAND } from "../sticky"
import { INSERT_ADMONITION_LEXICAL_COMMAND } from "../admonition"
import { INSERT_BUTTON_LEXICAL_COMMAND } from "../button"
import { INSERT_MERMAID_LEXICAL_COMMAND } from "../mermaid"
import { AlertCircle as AdmonitionIcon, MousePointerClick as ButtonIcon, GitBranch as MermaidIcon } from "lucide-react"

type DialogRender = (opts: { activeEditor: LexicalEditor; onClose: () => void }) => React.ReactNode

type IconCmp = React.ComponentType<{ className?: string }>

class ComponentPickerOption extends MenuOption {
  readonly title: string
  readonly Icon: IconCmp
  readonly keywords: string[]
  readonly onSelect: () => void
  readonly dialog?: { title: string; render: DialogRender }

  constructor(
    title: string,
    options: {
      Icon: IconCmp
      keywords?: string[]
      onSelect?: () => void
      dialog?: { title: string; render: DialogRender }
    },
  ) {
    super(title)
    this.title = title
    this.Icon = options.Icon
    this.keywords = options.keywords ?? []
    this.onSelect = (options.onSelect ?? (() => {})).bind(this)
    this.dialog = options.dialog
  }
}

function getBaseOptions(editor: LexicalEditor): ComponentPickerOption[] {
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
    }),
    new ComponentPickerOption("Bulleted List", {
      Icon: BulletedListIcon,
      keywords: ["bulleted list", "unordered list", "ul"],
      onSelect: () => editor.dispatchCommand(INSERT_UNORDERED_LIST_COMMAND, undefined),
    }),
    new ComponentPickerOption("Check List", {
      Icon: CheckListIcon,
      keywords: ["check list", "todo list"],
      onSelect: () => editor.dispatchCommand(INSERT_CHECK_LIST_COMMAND, undefined),
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
    }),
    new ComponentPickerOption("Excalidraw", {
      Icon: ExcalidrawIcon,
      keywords: ["excalidraw", "diagram", "drawing", "sketch"],
      onSelect: () => editor.dispatchCommand(INSERT_EXCALIDRAW_COMMAND, undefined),
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
    }),
    new ComponentPickerOption("Sticky Note", {
      Icon: StickyIcon,
      keywords: ["sticky", "note", "postit", "memo"],
      onSelect: () => editor.dispatchCommand(INSERT_STICKY_COMMAND, undefined),
    }),
    new ComponentPickerOption("Admonition", {
      Icon: AdmonitionIcon,
      keywords: ["admonition", "callout", "note", "warning", "info", "tip", "alert"],
      onSelect: () => editor.dispatchCommand(INSERT_ADMONITION_LEXICAL_COMMAND, undefined),
    }),
    new ComponentPickerOption("Button", {
      Icon: ButtonIcon,
      keywords: ["button", "link", "action", "cta", "download"],
      onSelect: () => editor.dispatchCommand(INSERT_BUTTON_LEXICAL_COMMAND, undefined),
    }),
    new ComponentPickerOption("Mermaid Diagram", {
      Icon: MermaidIcon,
      keywords: ["mermaid", "diagram", "flowchart", "chart", "graph", "sequence", "gantt", "class"],
      onSelect: () => editor.dispatchCommand(INSERT_MERMAID_LEXICAL_COMMAND, undefined),
    }),
  ]
}

export default function ComponentPickerPlugin() {
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
    const baseOptions = getBaseOptions(editor)
    if (!queryString) {
      return baseOptions
    }
    const regex = new RegExp(queryString, "i")
    return baseOptions.filter(
      (option) =>
        regex.test(option.title) || option.keywords.some((keyword) => regex.test(keyword)),
    )
  }, [editor, queryString])

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
      anchorClassName="z-[60]"
      menuRenderFn={(anchorElementRef, { selectedIndex, selectOptionAndCleanUp, setHighlightedIndex }) => {
        if (!anchorElementRef.current || options.length === 0) {
          return null
        }
        return createPortal(
          <div
            role="listbox"
            className={cn(
              "z-50 min-w-[220px] max-h-[360px] overflow-y-auto rounded-md p-1 shadow-2xl",
              "border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900",
            )}
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
                  className={cn(
                    "flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-left text-sm",
                    isSelected
                      ? "bg-blue-600 text-white"
                      : "text-gray-800 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-800",
                  )}
                >
                  <Icon className="w-4 h-4 shrink-0" />
                  <span className="truncate">{option.title}</span>
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
