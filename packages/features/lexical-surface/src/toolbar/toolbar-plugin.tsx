/**
 * Top toolbar for our LexicalSurface. Ported from facebook/lexical
 * playground `ToolbarPlugin/index.tsx` with these Wave A adjustments:
 *
 * - Tailwind classes throughout (no playground CSS imports).
 * - Icons via `lucide-react` (mapped in `../icons`).
 * - Removed code-prism / code-shiki language & theme dropdowns.
 * - Removed Wave B items from the Insert dropdown (image, table, poll,
 *   layout, sticky, equation, excalidraw, page-break, embeds, GIF).
 * - Removed font-size +/- input pair in favour of a fixed size
 *   dropdown (matches our reduced toolbar density).
 * - Removed lower/upper/capitalize/highlight/sub/sup from "additional
 *   styles" submenu (kept on the bus by `ToolbarContext` for Wave B
 *   restoration).
 */
"use client"

import * as React from "react"
import { Dispatch, useCallback, useEffect, useState } from "react"
import {
  $isCodeNode,
  CODE_LANGUAGE_FRIENDLY_NAME_MAP,
  CodeNode,
  getCodeLanguageOptions,
  getLanguageFriendlyName,
} from "@lexical/code"
import { $isLinkNode, TOGGLE_LINK_COMMAND } from "@lexical/link"
import { $isListNode, ListNode } from "@lexical/list"
import { INSERT_DIVIDER_LEXICAL_COMMAND } from "../divider"
import { $isHeadingNode } from "@lexical/rich-text"
import {
  $getSelectionStyleValueForProperty,
  $isParentElementRTL,
  $patchStyleText,
} from "@lexical/selection"
import { $isTableNode, $isTableSelection } from "@lexical/table"
import {
  $findMatchingParent,
  $getNearestNodeOfType,
  $isEditorIsNestedEditor,
  IS_APPLE,
  mergeRegister,
} from "@lexical/utils"
import {
  $addUpdateTag,
  $getNodeByKey,
  $getSelection,
  $createTextNode,
  $isElementNode,
  $isNodeSelection,
  $isRangeSelection,
  $isRootOrShadowRoot,
  CAN_REDO_COMMAND,
  CAN_UNDO_COMMAND,
  COMMAND_PRIORITY_CRITICAL,
  CommandPayloadType,
  ElementFormatType,
  FORMAT_ELEMENT_COMMAND,
  FORMAT_TEXT_COMMAND,
  HISTORIC_TAG,
  INDENT_CONTENT_COMMAND,
  LexicalCommand,
  LexicalEditor,
  LexicalNode,
  OUTDENT_CONTENT_COMMAND,
  REDO_COMMAND,
  SELECTION_CHANGE_COMMAND,
  SKIP_DOM_SELECTION_TAG,
  SKIP_SELECTION_FOCUS_TAG,
  TextFormatType,
  UNDO_COMMAND,
} from "lexical"
import { cn } from "@game-guild/ui/lib/utils"
import {
  AlignCenterIcon,
  AlignJustifyIcon,
  AlignLeftIcon,
  AlignRightIcon,
  BgColorIcon,
  BoldIcon,
  BulletedListIcon,
  CheckListIcon,
  ClearFormatIcon,
  CodeBlockIcon,
  CodeInlineIcon,
  CaseIcon,
  CapitalizeIcon,
  HighlightIcon,
  LowercaseIcon,
  UppercaseIcon,
  SubscriptIcon,
  SuperscriptIcon,
  Heading1Icon,
  Heading2Icon,
  Heading3Icon,
  Heading4Icon,
  Heading5Icon,
  Heading6Icon,
  HorizontalRuleIcon,
  IndentIcon,
  InsertIcon,
  ItalicIcon,
  LinkIcon,
  NumberedListIcon,
  OutdentIcon,
  ParagraphIcon,
  QuoteIcon,
  RedoIcon,
  StrikethroughIcon,
  TextColorIcon,
  UnderlineIcon,
  UndoIcon,
  StickyIcon,
} from "../icons"
import { DropDown, DropDownDivider, DropDownItem } from "./dropdown"
import { DropdownColorPicker } from "./dropdown-color-picker"
import { PageSettingsDropDown } from "./page-settings-dropdown"
import { getSelectedNode } from "./get-selected-node"
import {
  blockTypeToBlockName,
  DEFAULT_FONT_SIZE,
  useToolbarState,
} from "./toolbar-context"
import {
  clearFormatting,
  formatBulletList,
  formatCheckList,
  formatCode,
  formatHeading,
  formatNumberedList,
  formatParagraph,
  formatQuote,
  isKeyboardInput,
} from "./utils"
import { SHORTCUTS } from "../shortcuts/shortcuts"
import { $isEquationNode } from "../equation/equation-node"
import { InsertEquationDialog } from "../equation"
import { InsertTableDialog } from "../table"
import { INSERT_EXCALIDRAW_COMMAND } from "../excalidraw"
import { InsertLayoutDialog } from "../layout"
import { INSERT_COLLAPSIBLE_COMMAND } from "../collapsible"
import { INSERT_STICKY_COMMAND } from "../sticky"
import { INSERT_ADMONITION_LEXICAL_COMMAND } from "../admonition"
import { INSERT_BUTTON_LEXICAL_COMMAND } from "../button"
import { INSERT_MERMAID_LEXICAL_COMMAND } from "../mermaid"
import { INSERT_VEGA_LITE_LEXICAL_COMMAND } from "../vega-lite"
import { INSERT_MEDIA_LEXICAL_COMMAND } from "../media"
import { EmojiPickerPanel } from "../emoji"
import { Sigma as EquationIcon, Pencil as ExcalidrawIcon, Smile as EmojiIcon, Table as TableIcon, Columns as ColumnsIcon, PanelTopOpen as CollapsibleIcon, AlertCircle as AdmonitionToolbarIcon, MousePointerClick as ButtonToolbarIcon, GitBranch as MermaidToolbarIcon, BarChart3 as VegaToolbarIcon, Film as MediaToolbarIcon } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog"
import { Popover, PopoverContent, PopoverTrigger } from "@game-guild/ui/components/popover"
// ─── constants ──────────────────────────────────────────────────────────────

const CODE_FONT_FAMILY_VALUE =
  "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace"
const CODE_FONT_FAMILY_LABEL = "Monospace"

type FontGroup = "Sans-serif" | "Serif" | "Display" | "Monospace" | "Accessibility"

interface FontOption {
  /** CSS `font-family` value applied to the text. */
  value: string
  /** Label displayed in the dropdown. */
  label: string
  /** Category for visual grouping. */
  group: FontGroup
  /** Short note displayed below the name (e.g., for accessibility fonts). */
  hint?: string
  /** When true, ensures the Google Fonts CSS is loaded. */
  webFont?: boolean
}

const FONT_FAMILY_OPTIONS: ReadonlyArray<FontOption> = [
  // Sans-serif (common proportional)
  { value: "Arial", label: "Arial", group: "Sans-serif" },
  { value: "Helvetica", label: "Helvetica", group: "Sans-serif" },
  { value: "Verdana", label: "Verdana", group: "Sans-serif" },
  { value: "Tahoma", label: "Tahoma", group: "Sans-serif" },
  { value: "Trebuchet MS", label: "Trebuchet MS", group: "Sans-serif" },
  { value: "Calibri", label: "Calibri", group: "Sans-serif" },
  { value: "Segoe UI", label: "Segoe UI", group: "Sans-serif" },
  { value: "system-ui", label: "System UI", group: "Sans-serif" },
  // Serif
  { value: "Times New Roman", label: "Times New Roman", group: "Serif" },
  { value: "Georgia", label: "Georgia", group: "Serif" },
  { value: "Garamond", label: "Garamond", group: "Serif" },
  { value: "Palatino", label: "Palatino", group: "Serif" },
  // Display
  { value: "Comic Sans MS", label: "Comic Sans MS", group: "Display" },
  { value: "Impact", label: "Impact", group: "Display" },
  // Monospace
  { value: CODE_FONT_FAMILY_VALUE, label: CODE_FONT_FAMILY_LABEL, group: "Monospace" },
  { value: "Courier New", label: "Courier New", group: "Monospace" },
  { value: "Consolas", label: "Consolas", group: "Monospace" },
  { value: "Menlo", label: "Menlo", group: "Monospace" },
  { value: "Monaco", label: "Monaco", group: "Monospace" },
  { value: "Source Code Pro", label: "Source Code Pro", group: "Monospace", webFont: true },
  // Accessibility — recommended for dyslexia / low vision / autism.
  // Dynamically loaded from Google Fonts when mounting the toolbar.
  {
    value: "'Atkinson Hyperlegible', Arial, sans-serif",
    label: "Atkinson Hyperlegible",
    group: "Accessibility",
    hint: "Low Vision (Braille Institute)",
    webFont: true,
  },
  {
    value: "Lexend, Arial, sans-serif",
    label: "Lexend",
    group: "Accessibility",
    hint: "Reading / Dyslexia",
    webFont: true,
  },
  {
    value: "'Lexend Deca', Arial, sans-serif",
    label: "Lexend Deca",
    group: "Accessibility",
    hint: "Fluent Reading",
    webFont: true,
  },
  {
    value: "'OpenDyslexic', 'Comic Sans MS', sans-serif",
    label: "OpenDyslexic",
    group: "Accessibility",
    hint: "Dyslexia",
    webFont: true,
  },
  {
    value: "'Andika', Arial, sans-serif",
    label: "Andika",
    group: "Accessibility",
    hint: "Literacy / Autism",
    webFont: true,
  },
  {
    value: "'Nunito', Arial, sans-serif",
    label: "Nunito",
    group: "Accessibility",
    hint: "High Readability",
    webFont: true,
  },
]

// only URL Google Fonts CSS since we don't want to ship the font files ourselves or load
const ACCESSIBILITY_FONTS_HREF =
  "https://fonts.googleapis.com/css2?" +
  [
    "family=Atkinson+Hyperlegible:ital,wght@0,400;0,700;1,400;1,700",
    "family=Lexend:wght@300;400;500;600;700",
    "family=Lexend+Deca:wght@300;400;500;600;700",
    "family=Andika:ital,wght@0,400;0,700;1,400;1,700",
    "family=Nunito:ital,wght@0,400;0,700;1,400;1,700",
    "family=Source+Code+Pro:wght@400;500;600;700",
    "display=swap",
  ].join("&")

const OPENDYSLEXIC_CSS_HREF =
  "https://cdn.jsdelivr.net/npm/open-dyslexic@1.0.3/open-dyslexic-regular.css"

function ensureToolbarWebFontsLoaded(): void {
  if (typeof document === "undefined") return
  const head = document.head
  const ensure = (href: string, id: string) => {
    if (document.getElementById(id)) return
    const link = document.createElement("link")
    link.id = id
    link.rel = "stylesheet"
    link.href = href
    head.appendChild(link)
  }
  ensure(ACCESSIBILITY_FONTS_HREF, "lexical-toolbar-google-fonts")
  ensure(OPENDYSLEXIC_CSS_HREF, "lexical-toolbar-opendyslexic")
}

const FONT_SIZE_OPTIONS: ReadonlyArray<readonly [string, string]> = [
  ["8px", "8"],
  ["9px", "9"],
  ["10px", "10"],
  ["11px", "11"],
  ["12px", "12"],
  ["14px", "14"],
  ["18px", "18"],
  ["24px", "24"],
  ["30px", "30"],
  ["36px", "36"],
  ["48px", "48"],
  ["60px", "60"],
  ["72px", "72"],
  ["96px", "96"],
]

const MIN_FONT_SIZE = 8
const MAX_FONT_SIZE = 400

const ELEMENT_FORMAT_OPTIONS: Record<
  Exclude<ElementFormatType, "">,
  { Icon: React.ComponentType<{ className?: string }>; IconRTL: React.ComponentType<{ className?: string }>; name: string }
> = {
  center: { Icon: AlignCenterIcon, IconRTL: AlignCenterIcon, name: "Center Align" },
  end: { Icon: AlignRightIcon, IconRTL: AlignLeftIcon, name: "End Align" },
  justify: { Icon: AlignJustifyIcon, IconRTL: AlignJustifyIcon, name: "Justify Align" },
  left: { Icon: AlignLeftIcon, IconRTL: AlignLeftIcon, name: "Left Align" },
  right: { Icon: AlignRightIcon, IconRTL: AlignRightIcon, name: "Right Align" },
  start: { Icon: AlignLeftIcon, IconRTL: AlignRightIcon, name: "Start Align" },
}

// ─── sub-components ─────────────────────────────────────────────────────────

function Divider() {
  return <div className="w-px h-5 mx-1 bg-gray-200 dark:bg-gray-700 self-center" aria-hidden />
}

// Set/replace a CSS property in an inline `style` string. Used para aplicar
// font-family / font-size diretamente no CodeNode (ElementNode), já que os
// CodeHighlightNode filhos são recriados pelo registerCodeHighlighting e
// perdem styles inline.
function upsertCssProperty(prev: string, prop: string, value: string): string {
  const decls = (prev || "")
    .split(";")
    .map((d) => d.trim())
    .filter(Boolean)
    .filter((d) => {
      const idx = d.indexOf(":")
      if (idx < 0) return false
      return d.slice(0, idx).trim().toLowerCase() !== prop.toLowerCase()
    })
  decls.push(`${prop}: ${value}`)
  return decls.join("; ")
}

// Procura um CodeNode ancestral cobrindo a seleção atual.
function $getEnclosingCodeNode(): CodeNode | null {
  const selection = $getSelection()
  if (selection === null) return null
  const nodes =
    $isRangeSelection(selection) || $isNodeSelection(selection)
      ? selection.getNodes()
      : []
  for (const n of nodes) {
    const code = $findMatchingParent(n, $isCodeNode)
    if (code) return code as CodeNode
    if ($isCodeNode(n)) return n as CodeNode
  }
  return null
}

function ToolbarButton({
  active,
  disabled,
  onClick,
  title,
  ariaLabel,
  children,
}: {
  active?: boolean
  disabled?: boolean
  onClick: (e: React.MouseEvent<HTMLButtonElement>) => void
  title?: string
  ariaLabel: string
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      title={title}
      aria-label={ariaLabel}
      className={cn(
        "inline-flex items-center justify-center w-8 h-8 rounded text-gray-700 dark:text-gray-200",
        "hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-40 disabled:pointer-events-none",
        active && "bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300",
      )}
    >
      {children}
    </button>
  )
}

function BlockFormatDropDown({
  editor,
  blockType,
  disabled,
}: {
  editor: LexicalEditor
  blockType: keyof typeof blockTypeToBlockName
  disabled?: boolean
}) {
  return (
    <DropDown
      disabled={disabled}
      buttonLabel={blockTypeToBlockName[blockType]}
      buttonClassName="w-[140px] truncate justify-start"
      buttonAriaLabel="Formatting options for text style"
    >
      <DropDownItem active={blockType === "paragraph"} onClick={() => formatParagraph(editor)} shortcut={SHORTCUTS.NORMAL}>
        <ParagraphIcon className="w-4 h-4" /> Normal
      </DropDownItem>
      <DropDownItem active={blockType === "h1"} onClick={() => formatHeading(editor, blockType, "h1")} shortcut={SHORTCUTS.HEADING1}>
        <Heading1Icon className="w-4 h-4" /> Heading 1
      </DropDownItem>
      <DropDownItem active={blockType === "h2"} onClick={() => formatHeading(editor, blockType, "h2")} shortcut={SHORTCUTS.HEADING2}>
        <Heading2Icon className="w-4 h-4" /> Heading 2
      </DropDownItem>
      <DropDownItem active={blockType === "h3"} onClick={() => formatHeading(editor, blockType, "h3")} shortcut={SHORTCUTS.HEADING3}>
        <Heading3Icon className="w-4 h-4" /> Heading 3
      </DropDownItem>
      <DropDownItem active={blockType === "h4"} onClick={() => formatHeading(editor, blockType, "h4")}>
        <Heading4Icon className="w-4 h-4" /> Heading 4
      </DropDownItem>
      <DropDownItem active={blockType === "h5"} onClick={() => formatHeading(editor, blockType, "h5")}>
        <Heading5Icon className="w-4 h-4" /> Heading 5
      </DropDownItem>
      <DropDownItem active={blockType === "h6"} onClick={() => formatHeading(editor, blockType, "h6")}>
        <Heading6Icon className="w-4 h-4" /> Heading 6
      </DropDownItem>
      <DropDownItem active={blockType === "bullet"} onClick={() => formatBulletList(editor, blockType)} shortcut={SHORTCUTS.BULLET_LIST}>
        <BulletedListIcon className="w-4 h-4" /> Bullet List
      </DropDownItem>
      <DropDownItem active={blockType === "number"} onClick={() => formatNumberedList(editor, blockType)} shortcut={SHORTCUTS.NUMBERED_LIST}>
        <NumberedListIcon className="w-4 h-4" /> Numbered List
      </DropDownItem>
      <DropDownItem active={blockType === "check"} onClick={() => formatCheckList(editor, blockType)} shortcut={SHORTCUTS.CHECK_LIST}>
        <CheckListIcon className="w-4 h-4" /> Check List
      </DropDownItem>
      <DropDownItem active={blockType === "quote"} onClick={() => formatQuote(editor, blockType)} shortcut={SHORTCUTS.QUOTE}>
        <QuoteIcon className="w-4 h-4" /> Quote
      </DropDownItem>
      <DropDownItem active={blockType === "code"} onClick={() => formatCode(editor, blockType)} shortcut={SHORTCUTS.CODE_BLOCK}>
        <CodeBlockIcon className="w-4 h-4" /> Code Block
      </DropDownItem>
    </DropDown>
  )
}

function CodeLanguageDropDown({
  editor,
  language,
  codeNodeKey,
  disabled,
}: {
  editor: LexicalEditor
  language: string
  codeNodeKey: string
  disabled?: boolean
}) {
  const options = React.useMemo(() => getCodeLanguageOptions(), [])
  const friendly =
    CODE_LANGUAGE_FRIENDLY_NAME_MAP[language] ??
    (language ? getLanguageFriendlyName(language) : "Plain Text")

  const onSelect = (value: string) => {
    editor.update(() => {
      const node = $getNodeByKey(codeNodeKey)
      if (node && $isCodeNode(node)) node.setLanguage(value)
    })
  }

  return (
    <DropDown
      disabled={disabled}
      buttonLabel={friendly}
      buttonClassName="w-[140px] truncate justify-start"
      buttonAriaLabel="Select code language"
    >
      {options.map(([value, label]) => (
        <DropDownItem
          key={value || "plain"}
          active={value === language}
          onClick={() => onSelect(value)}
        >
          {label}
        </DropDownItem>
      ))}
    </DropDown>
  )
}

function FontDropDown({
  editor,
  value,
  style,
  disabled,
}: {
  editor: LexicalEditor
  value: string
  style: "font-family" | "font-size"
  disabled?: boolean
}) {
  const isFontFamily = style === "font-family"
  // Garante que as web fonts (Google Fonts + OpenDyslexic) estejam
  // carregadas para o preview de cada item.
  React.useEffect(() => {
    if (isFontFamily) ensureToolbarWebFontsLoaded()
  }, [isFontFamily])

  const display = isFontFamily
    ? (FONT_FAMILY_OPTIONS.find((o) => o.value === value)?.label ?? value)
    : value.replace(/px$/, "")

  const handleClick = useCallback(
    (option: string) => {
      editor.update(() => {
        $addUpdateTag(SKIP_SELECTION_FOCUS_TAG)
        const selection = $getSelection()
        if (selection === null) return
        // Em blocos de código, o registerCodeHighlighting recria os
        // CodeHighlightNode descartando styles inline. Aplicamos o
        // style no CodeNode pai e os spans filhos herdam via CSS.
        const codeNode = $getEnclosingCodeNode()
        if (codeNode) {
          codeNode.setStyle(upsertCssProperty(codeNode.getStyle(), style, option))
          return
        }
        $patchStyleText(selection, { [style]: option })
      })
    },
    [editor, style],
  )

  // Estilo de botão dedicado para a fonte selecionada (preview no toolbar).
  const buttonLabelStyle: React.CSSProperties | undefined = isFontFamily
    ? { fontFamily: value }
    : undefined

  if (!isFontFamily) {
    return (
      <DropDown
        disabled={disabled}
        buttonLabel={display}
        buttonClassName="min-w-[60px]"
        buttonAriaLabel="Formatting options for font size"
      >
        {FONT_SIZE_OPTIONS.map(([option, label]) => (
          <DropDownItem
            key={option}
            active={value === option}
            onClick={() => handleClick(option)}
          >
            {label}
          </DropDownItem>
        ))}
      </DropDown>
    )
  }

  // Agrupa as fontes preservando a ordem dos grupos definidos em FONT_FAMILY_OPTIONS.
  const groups: FontGroup[] = []
  for (const opt of FONT_FAMILY_OPTIONS) {
    if (!groups.includes(opt.group)) groups.push(opt.group)
  }

  return (
    <DropDown
      disabled={disabled}
      buttonLabel={display}
      buttonLabelStyle={buttonLabelStyle}
      buttonClassName="w-[140px] truncate justify-start"
      buttonAriaLabel="Formatting options for font family"
    >
      {groups.flatMap((group, gi) => {
        const items = FONT_FAMILY_OPTIONS.filter((o) => o.group === group)
        return [
          gi > 0 ? <DropDownDivider key={`div-${group}`} /> : null,
          <div
            key={`hdr-${group}`}
            className="px-2 pt-1 pb-0.5 text-[10px] uppercase tracking-wide text-gray-500 dark:text-gray-400"
          >
            {group}
          </div>,
          ...items.map((opt) => (
            <DropDownItem
              key={opt.value}
              active={value === opt.value}
              closeOnClick={false}
              onClick={() => handleClick(opt.value)}
            >
              <span className="flex flex-col min-w-0">
                <span className="truncate" style={{ fontFamily: opt.value }}>
                  {opt.label}
                </span>
                {opt.hint && (
                  <span className="text-[10px] text-gray-500 dark:text-gray-400 truncate">
                    {opt.hint}
                  </span>
                )}
              </span>
            </DropDownItem>
          )),
        ]
      })}
    </DropDown>
  )
}

function FontSizeStepper({
  editor,
  value,
  disabled,
}: {
  editor: LexicalEditor
  value: string
  disabled?: boolean
}) {
  const currentNumber = React.useMemo(() => {
    const parsed = parseInt(String(value).replace(/px$/, ""), 10)
    return Number.isFinite(parsed) ? parsed : 15
  }, [value])
  const [inputValue, setInputValue] = React.useState<string>(String(currentNumber))

  React.useEffect(() => {
    setInputValue(String(currentNumber))
  }, [currentNumber])

  const applySize = useCallback(
    (px: number) => {
      const clamped = Math.max(MIN_FONT_SIZE, Math.min(MAX_FONT_SIZE, Math.round(px)))
      editor.update(() => {
        $addUpdateTag(SKIP_SELECTION_FOCUS_TAG)
        const selection = $getSelection()
        if (selection === null) return
        // Caso especial: equação selecionada (NodeSelection com EquationNode).
        // `$patchStyleText` só funciona para RangeSelection — para o decorator
        // node, ajustamos a propriedade `fontSize` (em em) diretamente.
        if ($isNodeSelection(selection)) {
          const nodes = selection.getNodes()
          let handled = false
          for (const n of nodes) {
            if ($isEquationNode(n)) {
              n.setFontSize(clamped / DEFAULT_FONT_SIZE)
              handled = true
            }
          }
          if (handled) return
        }
        // Em blocos de código, aplicar font-size no CodeNode pai (ver comentário
        // em FontDropDown.handleClick).
        const codeNode = $getEnclosingCodeNode()
        if (codeNode) {
          codeNode.setStyle(
            upsertCssProperty(codeNode.getStyle(), "font-size", `${clamped}px`),
          )
          return
        }
        $patchStyleText(selection, { "font-size": `${clamped}px` })
      })
    },
    [editor],
  )

  const commitInput = useCallback(() => {
    const parsed = parseInt(inputValue, 10)
    if (Number.isFinite(parsed)) {
      applySize(parsed)
    } else {
      setInputValue(String(currentNumber))
    }
  }, [applySize, currentNumber, inputValue])

  return (
    <div className="inline-flex items-center gap-0.5">
      <button
        type="button"
        disabled={disabled || currentNumber <= MIN_FONT_SIZE}
        onClick={() => applySize(currentNumber - 1)}
        title="Decrease font size"
        aria-label="Decrease font size"
        className="inline-flex items-center justify-center w-7 h-8 rounded text-sm hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-40 disabled:pointer-events-none"
      >
        −
      </button>
      <div className="inline-flex items-center h-8 rounded border border-gray-200 dark:border-gray-700">
        <input
          type="text"
          inputMode="numeric"
          disabled={disabled}
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value.replace(/[^0-9]/g, ""))}
          onBlur={commitInput}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault()
              commitInput()
              ;(e.target as HTMLInputElement).blur()
            }
          }}
          aria-label="Font size"
          className="w-9 h-full bg-transparent text-sm text-center outline-none tabular-nums"
        />
        <DropDown
          buttonAriaLabel="Choose font size"
          buttonClassName="!h-7 !px-1 !rounded-none"
          showChevron={true}
        >
          {FONT_SIZE_OPTIONS.map(([option, label]) => (
            <DropDownItem
              key={option}
              active={value === option}
              onClick={() => applySize(parseInt(label, 10))}
            >
              {label}
            </DropDownItem>
          ))}
        </DropDown>
      </div>
      <button
        type="button"
        disabled={disabled || currentNumber >= MAX_FONT_SIZE}
        onClick={() => applySize(currentNumber + 1)}
        title="Increase font size"
        aria-label="Increase font size"
        className="inline-flex items-center justify-center w-7 h-8 rounded text-sm hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-40 disabled:pointer-events-none"
      >
        +
      </button>
    </div>
  )
}

function ElementFormatDropdown({
  editor,
  value,
  isRTL,
  disabled,
}: {
  editor: LexicalEditor
  value: ElementFormatType
  isRTL: boolean
  disabled?: boolean
}) {
  const formatOption = ELEMENT_FORMAT_OPTIONS[value || "left"]
  const Icon = isRTL ? formatOption.IconRTL : formatOption.Icon

  return (
    <DropDown
      disabled={disabled}
      buttonLabel={formatOption.name}
      buttonIcon={<Icon className="w-4 h-4" />}
      buttonClassName="w-[140px] truncate justify-start"
      buttonAriaLabel="Formatting options for text alignment"
    >
      <DropDownItem onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "left")} shortcut={SHORTCUTS.LEFT_ALIGN}>
        <AlignLeftIcon className="w-4 h-4" /> Left Align
      </DropDownItem>
      <DropDownItem onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "center")} shortcut={SHORTCUTS.CENTER_ALIGN}>
        <AlignCenterIcon className="w-4 h-4" /> Center Align
      </DropDownItem>
      <DropDownItem onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "right")} shortcut={SHORTCUTS.RIGHT_ALIGN}>
        <AlignRightIcon className="w-4 h-4" /> Right Align
      </DropDownItem>
      <DropDownItem onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "justify")} shortcut={SHORTCUTS.JUSTIFY_ALIGN}>
        <AlignJustifyIcon className="w-4 h-4" /> Justify Align
      </DropDownItem>
      <DropDownDivider />
      <DropDownItem onClick={() => editor.dispatchCommand(OUTDENT_CONTENT_COMMAND, undefined)} shortcut={SHORTCUTS.OUTDENT}>
        <OutdentIcon className="w-4 h-4" /> Outdent
      </DropDownItem>
      <DropDownItem onClick={() => editor.dispatchCommand(INDENT_CONTENT_COMMAND, undefined)} shortcut={SHORTCUTS.INDENT}>
        <IndentIcon className="w-4 h-4" /> Indent
      </DropDownItem>
    </DropDown>
  )
}

function CaseFormatDropDown({
  editor,
  disabled,
  isLowercase,
  isUppercase,
  isCapitalize,
  isStrikethrough,
  isSubscript,
  isSuperscript,
  isHighlight,
}: {
  editor: LexicalEditor
  disabled?: boolean
  isLowercase: boolean
  isUppercase: boolean
  isCapitalize: boolean
  isStrikethrough: boolean
  isSubscript: boolean
  isSuperscript: boolean
  isHighlight: boolean
}) {
  const dispatch = (payload: TextFormatType) =>
    editor.dispatchCommand(FORMAT_TEXT_COMMAND, payload)
  const clear = () => {
    editor.update(() => {
      const selection = $getSelection()
      if (selection !== null) {
        clearFormatting(editor)
      }
    })
  }
  return (
    <DropDown
      disabled={disabled}
      buttonIcon={<CaseIcon className="w-4 h-4" />}
      buttonAriaLabel="Formatting options for additional text styles"
    >
      <DropDownItem active={isLowercase} onClick={() => dispatch("lowercase")} shortcut={SHORTCUTS.LOWERCASE}>
        <LowercaseIcon className="w-4 h-4" /> Lowercase
      </DropDownItem>
      <DropDownItem active={isUppercase} onClick={() => dispatch("uppercase")} shortcut={SHORTCUTS.UPPERCASE}>
        <UppercaseIcon className="w-4 h-4" /> Uppercase
      </DropDownItem>
      <DropDownItem active={isCapitalize} onClick={() => dispatch("capitalize")} shortcut={SHORTCUTS.CAPITALIZE}>
        <CapitalizeIcon className="w-4 h-4" /> Capitalize
      </DropDownItem>
      <DropDownItem active={isStrikethrough} onClick={() => dispatch("strikethrough")} shortcut={SHORTCUTS.STRIKETHROUGH}>
        <StrikethroughIcon className="w-4 h-4" /> Strikethrough
      </DropDownItem>
      <DropDownItem active={isSubscript} onClick={() => dispatch("subscript")} shortcut={SHORTCUTS.SUBSCRIPT}>
        <SubscriptIcon className="w-4 h-4" /> Subscript
      </DropDownItem>
      <DropDownItem active={isSuperscript} onClick={() => dispatch("superscript")} shortcut={SHORTCUTS.SUPERSCRIPT}>
        <SuperscriptIcon className="w-4 h-4" /> Superscript
      </DropDownItem>
      <DropDownItem active={isHighlight} onClick={() => dispatch("highlight")}>
        <HighlightIcon className="w-4 h-4" /> Highlight
      </DropDownItem>
      <DropDownItem onClick={clear} shortcut={SHORTCUTS.CLEAR_FORMATTING}>
        <ClearFormatIcon className="w-4 h-4" /> Clear Formatting
      </DropDownItem>
    </DropDown>
  )
}

function $findTopLevelElement(node: LexicalNode) {
  let topLevelElement =
    node.getKey() === "root"
      ? node
      : $findMatchingParent(node, (e) => {
          const parent = e.getParent()
          return parent !== null && $isRootOrShadowRoot(parent)
        })

  if (topLevelElement === null) {
    topLevelElement = node.getTopLevelElementOrThrow()
  }
  return topLevelElement
}

// ─── main plugin ────────────────────────────────────────────────────────────

function EmojiPickerPopover({
  editor,
  disabled,
}: {
  editor: LexicalEditor
  disabled?: boolean
}) {
  const [open, setOpen] = useState(false)

  const insert = useCallback(
    (emoji: string) => {
      editor.update(() => {
        $addUpdateTag(SKIP_SELECTION_FOCUS_TAG)
        const selection = $getSelection()
        if ($isRangeSelection(selection)) {
          selection.insertNodes([$createTextNode(emoji)])
        }
      })
      setOpen(false)
    },
    [editor],
  )

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          disabled={disabled}
          title="Insert emoji"
          aria-label="Insert emoji"
          className="inline-flex h-8 items-center justify-center gap-1 rounded px-2 text-sm text-gray-800 hover:bg-gray-100 disabled:pointer-events-none disabled:opacity-40 dark:text-gray-100 dark:hover:bg-gray-800"
        >
          <EmojiIcon className="h-4 w-4" />
          <span className="hidden sm:inline">Emoji</span>
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        sideOffset={4}
        className="w-auto p-2"
        onOpenAutoFocus={(e) => {
          // Mantém o foco no input interno do panel.
          e.preventDefault()
        }}
      >
        <EmojiPickerPanel onSelect={insert} />
      </PopoverContent>
    </Popover>
  )
}

export default function ToolbarPlugin({
  editor,
  activeEditor,
  setActiveEditor,
  setIsLinkEditMode,
  features,
}: {
  editor: LexicalEditor
  activeEditor: LexicalEditor
  setActiveEditor: Dispatch<LexicalEditor>
  setIsLinkEditMode: Dispatch<boolean>
  features?: {
    pageLayout?: boolean
  }
}) {
  const [selectedElementKey, setSelectedElementKey] = useState<string | null>(null)
  const [isEditable, setIsEditable] = useState(() => editor.isEditable())
  const [insertDialog, setInsertDialog] = useState<"equation" | "table" | "layout" | null>(null)
  const { toolbarState, updateToolbarState } = useToolbarState()

  const dispatchToolbarCommand = <T extends LexicalCommand<any>>(
    command: T,
    payload?: CommandPayloadType<T>,
    skipRefocus: boolean = false,
  ) => {
    activeEditor.update(() => {
      if (skipRefocus) {
        $addUpdateTag(SKIP_DOM_SELECTION_TAG)
      }
      activeEditor.dispatchCommand(command, payload as CommandPayloadType<T>)
    })
  }

  const dispatchFormatTextCommand = (payload: TextFormatType, skipRefocus: boolean = false) =>
    dispatchToolbarCommand(FORMAT_TEXT_COMMAND, payload, skipRefocus)

  const $handleHeadingNode = useCallback(
    (selectedElement: LexicalNode) => {
      const type = $isHeadingNode(selectedElement)
        ? selectedElement.getTag()
        : selectedElement.getType()
      if (type in blockTypeToBlockName) {
        updateToolbarState("blockType", type as keyof typeof blockTypeToBlockName)
      }
    },
    [updateToolbarState],
  )

  const $updateToolbar = useCallback(() => {
    const selection = $getSelection()
    if ($isRangeSelection(selection)) {
      if (activeEditor !== editor && $isEditorIsNestedEditor(activeEditor)) {
        const rootElement = activeEditor.getRootElement()
        updateToolbarState(
          "isImageCaption",
          !!rootElement?.parentElement?.classList.contains("image-caption-container"),
        )
      } else {
        updateToolbarState("isImageCaption", false)
      }

      const anchorNode = selection.anchor.getNode()
      const element = $findTopLevelElement(anchorNode)
      const elementKey = element.getKey()
      const elementDOM = activeEditor.getElementByKey(elementKey)

      updateToolbarState("isRTL", $isParentElementRTL(selection))

      const node = getSelectedNode(selection)
      const parent = node.getParent()
      const isLink = $isLinkNode(parent) || $isLinkNode(node)
      updateToolbarState("isLink", isLink)

      const tableNode = $findMatchingParent(node, $isTableNode)
      updateToolbarState("rootType", $isTableNode(tableNode) ? "table" : "root")

      if (elementDOM !== null) {
        setSelectedElementKey(elementKey)
        if ($isListNode(element)) {
          const parentList = $getNearestNodeOfType<ListNode>(anchorNode, ListNode)
          const type = parentList ? parentList.getListType() : element.getListType()
          updateToolbarState("blockType", type)
        } else {
          $handleHeadingNode(element)
          if ($isCodeNode(element)) {
            updateToolbarState("blockType", "code")
            updateToolbarState(
              "codeLanguage",
              element.getLanguage() ?? "",
            )
          }
        }
      }

      const elementIsCode = $isCodeNode(element)
      // Estilos efetivos definidos no próprio CodeNode (font-family/size)
      // — ver comentário em FontDropDown.handleClick.
      const codeStyleObj: Record<string, string> = {}
      if (elementIsCode) {
        const raw = (element as CodeNode).getStyle() || ""
        for (const decl of raw.split(";")) {
          const idx = decl.indexOf(":")
          if (idx > 0) {
            const k = decl.slice(0, idx).trim().toLowerCase()
            const v = decl.slice(idx + 1).trim()
            if (k) codeStyleObj[k] = v
          }
        }
      }
      updateToolbarState(
        "fontColor",
        $getSelectionStyleValueForProperty(selection, "color", "#000"),
      )
      updateToolbarState(
        "bgColor",
        $getSelectionStyleValueForProperty(selection, "background-color", "#fff"),
      )
      updateToolbarState(
        "fontFamily",
        elementIsCode
          ? (codeStyleObj["font-family"] ?? CODE_FONT_FAMILY_VALUE)
          : $getSelectionStyleValueForProperty(selection, "font-family", "Arial"),
      )

      let matchingParent
      if ($isLinkNode(parent)) {
        matchingParent = $findMatchingParent(
          node,
          (parentNode) => $isElementNode(parentNode) && !parentNode.isInline(),
        )
      }

      updateToolbarState(
        "elementFormat",
        $isElementNode(matchingParent)
          ? matchingParent.getFormatType()
          : $isElementNode(node)
            ? node.getFormatType()
            : parent?.getFormatType() || "left",
      )
    }

    if ($isRangeSelection(selection) || $isTableSelection(selection)) {
      updateToolbarState("isBold", selection.hasFormat("bold"))
      updateToolbarState("isItalic", selection.hasFormat("italic"))
      updateToolbarState("isUnderline", selection.hasFormat("underline"))
      updateToolbarState("isStrikethrough", selection.hasFormat("strikethrough"))
      updateToolbarState("isSubscript", selection.hasFormat("subscript"))
      updateToolbarState("isSuperscript", selection.hasFormat("superscript"))
      updateToolbarState("isHighlight", selection.hasFormat("highlight"))
      updateToolbarState("isCode", selection.hasFormat("code"))
      // Para blocos de código, lemos o font-size do CodeNode pai.
      const codeAncestor = $getEnclosingCodeNode()
      let codeFontSize: string | null = null
      if (codeAncestor) {
        const raw = codeAncestor.getStyle() || ""
        for (const decl of raw.split(";")) {
          const idx = decl.indexOf(":")
          if (idx > 0 && decl.slice(0, idx).trim().toLowerCase() === "font-size") {
            codeFontSize = decl.slice(idx + 1).trim()
            break
          }
        }
      }
      updateToolbarState(
        "fontSize",
        codeAncestor
          ? (codeFontSize ?? `${DEFAULT_FONT_SIZE}px`)
          : $getSelectionStyleValueForProperty(selection, "font-size", `${DEFAULT_FONT_SIZE}px`),
      )
      updateToolbarState("isLowercase", selection.hasFormat("lowercase"))
      updateToolbarState("isUppercase", selection.hasFormat("uppercase"))
      updateToolbarState("isCapitalize", selection.hasFormat("capitalize"))
    }

    if ($isNodeSelection(selection)) {
      const nodes = selection.getNodes()
      for (const selectedNode of nodes) {
        if ($isEquationNode(selectedNode)) {
          updateToolbarState(
            "fontSize",
            `${Math.round(selectedNode.getFontSize() * DEFAULT_FONT_SIZE)}px`,
          )
        }
        const parentList = $getNearestNodeOfType<ListNode>(selectedNode, ListNode)
        if (parentList) {
          updateToolbarState("blockType", parentList.getListType())
        } else {
          const selectedElement = $findTopLevelElement(selectedNode)
          $handleHeadingNode(selectedElement)
          if ($isElementNode(selectedElement)) {
            updateToolbarState("elementFormat", selectedElement.getFormatType())
          }
        }
      }
    }
  }, [activeEditor, editor, updateToolbarState, $handleHeadingNode])

  useEffect(() => {
    return editor.registerCommand(
      SELECTION_CHANGE_COMMAND,
      (_payload, newEditor) => {
        setActiveEditor(newEditor)
        $updateToolbar()
        return false
      },
      COMMAND_PRIORITY_CRITICAL,
    )
  }, [editor, $updateToolbar, setActiveEditor])

  useEffect(() => {
    activeEditor.getEditorState().read(
      () => {
        $updateToolbar()
      },
      { editor: activeEditor },
    )
  }, [activeEditor, $updateToolbar])

  useEffect(() => {
    return mergeRegister(
      editor.registerEditableListener((editable) => setIsEditable(editable)),
      activeEditor.registerUpdateListener(({ editorState }) => {
        editorState.read(
          () => {
            $updateToolbar()
          },
          { editor: activeEditor },
        )
      }),
      activeEditor.registerCommand<boolean>(
        CAN_UNDO_COMMAND,
        (payload) => {
          updateToolbarState("canUndo", payload)
          return false
        },
        COMMAND_PRIORITY_CRITICAL,
      ),
      activeEditor.registerCommand<boolean>(
        CAN_REDO_COMMAND,
        (payload) => {
          updateToolbarState("canRedo", payload)
          return false
        },
        COMMAND_PRIORITY_CRITICAL,
      ),
    )
  }, [$updateToolbar, activeEditor, editor, updateToolbarState])

  const applyStyleText = useCallback(
    (styles: Record<string, string>, skipHistoryStack?: boolean, skipRefocus: boolean = false) => {
      activeEditor.update(
        () => {
          if (skipRefocus) {
            $addUpdateTag(SKIP_DOM_SELECTION_TAG)
          }
          const selection = $getSelection()
          if (selection !== null) {
            $patchStyleText(selection, styles)
          }
        },
        skipHistoryStack ? { tag: HISTORIC_TAG } : {},
      )
    },
    [activeEditor],
  )

  const onFontColorSelect = useCallback(
    (value: string, skipHistoryStack: boolean, skipRefocus: boolean) => {
      applyStyleText({ color: value }, skipHistoryStack, skipRefocus)
    },
    [applyStyleText],
  )

  const onBgColorSelect = useCallback(
    (value: string, skipHistoryStack: boolean, skipRefocus: boolean) => {
      applyStyleText({ "background-color": value }, skipHistoryStack, skipRefocus)
    },
    [applyStyleText],
  )

  const insertLink = useCallback(() => {
    if (!toolbarState.isLink) {
      setIsLinkEditMode(true)
      activeEditor.dispatchCommand(TOGGLE_LINK_COMMAND, "https://")
    } else {
      setIsLinkEditMode(false)
      activeEditor.dispatchCommand(TOGGLE_LINK_COMMAND, null)
    }
  }, [activeEditor, setIsLinkEditMode, toolbarState.isLink])

  return (
    <div
      className={cn(
        "flex flex-wrap items-center gap-0.5 p-1 rounded-t",
        "border-b border-gray-200 dark:border-gray-700",
        "bg-white dark:bg-gray-900 lexical-toolbar",
      )}
      role="toolbar"
      aria-label="Editor toolbar"
    >
      <ToolbarButton
        disabled={!toolbarState.canUndo || !isEditable}
        onClick={(e) => dispatchToolbarCommand(UNDO_COMMAND, undefined, isKeyboardInput(e))}
        title={IS_APPLE ? "Undo (⌘Z)" : "Undo (Ctrl+Z)"}
        ariaLabel="Undo"
      >
        <UndoIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton
        disabled={!toolbarState.canRedo || !isEditable}
        onClick={(e) => dispatchToolbarCommand(REDO_COMMAND, undefined, isKeyboardInput(e))}
        title={IS_APPLE ? "Redo (⇧⌘Z)" : "Redo (Ctrl+Y)"}
        ariaLabel="Redo"
      >
        <RedoIcon className="w-4 h-4" />
      </ToolbarButton>

      <Divider />

      {toolbarState.blockType in blockTypeToBlockName && activeEditor === editor && (
        <>
          <BlockFormatDropDown
            disabled={!isEditable}
            blockType={toolbarState.blockType}
            editor={activeEditor}
          />
          <Divider />
        </>
      )}

      <FontDropDown
        disabled={!isEditable}
        style="font-family"
        value={toolbarState.fontFamily}
        editor={activeEditor}
      />
      <FontSizeStepper
        disabled={!isEditable}
        value={toolbarState.fontSize}
        editor={activeEditor}
      />

      <Divider />

      <CaseFormatDropDown
        editor={activeEditor}
        disabled={!isEditable}
        isLowercase={toolbarState.isLowercase}
        isUppercase={toolbarState.isUppercase}
        isCapitalize={toolbarState.isCapitalize}
        isStrikethrough={toolbarState.isStrikethrough}
        isSubscript={toolbarState.isSubscript}
        isSuperscript={toolbarState.isSuperscript}
        isHighlight={toolbarState.isHighlight}
      />

      <Divider />

      <ToolbarButton
        active={toolbarState.isBold}
        disabled={!isEditable}
        onClick={(e) => dispatchFormatTextCommand("bold", isKeyboardInput(e))}
        title="Bold"
        ariaLabel="Format Bold"
      >
        <BoldIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton
        active={toolbarState.isItalic}
        disabled={!isEditable}
        onClick={(e) => dispatchFormatTextCommand("italic", isKeyboardInput(e))}
        title="Italic"
        ariaLabel="Format Italic"
      >
        <ItalicIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton
        active={toolbarState.isUnderline}
        disabled={!isEditable}
        onClick={(e) => dispatchFormatTextCommand("underline", isKeyboardInput(e))}
        title="Underline"
        ariaLabel="Format Underline"
      >
        <UnderlineIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton
        active={toolbarState.isStrikethrough}
        disabled={!isEditable}
        onClick={(e) => dispatchFormatTextCommand("strikethrough", isKeyboardInput(e))}
        title="Strikethrough"
        ariaLabel="Format Strikethrough"
      >
        <StrikethroughIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton
        active={toolbarState.isSubscript}
        disabled={!isEditable}
        onClick={(e) => dispatchFormatTextCommand("subscript", isKeyboardInput(e))}
        title="Subscript"
        ariaLabel="Format Subscript"
      >
        <SubscriptIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton
        active={toolbarState.isSuperscript}
        disabled={!isEditable}
        onClick={(e) => dispatchFormatTextCommand("superscript", isKeyboardInput(e))}
        title="Superscript"
        ariaLabel="Format Superscript"
      >
        <SuperscriptIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton
        active={toolbarState.isHighlight}
        disabled={!isEditable}
        onClick={(e) => dispatchFormatTextCommand("highlight", isKeyboardInput(e))}
        title="Highlight"
        ariaLabel="Format Highlight"
      >
        <HighlightIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton
        active={toolbarState.isCode}
        disabled={!isEditable}
        onClick={(e) => dispatchFormatTextCommand("code", isKeyboardInput(e))}
        title="Inline Code"
        ariaLabel="Format Inline Code"
      >
        <CodeInlineIcon className="w-4 h-4" />
      </ToolbarButton>
      <ToolbarButton
        active={toolbarState.isLink}
        disabled={!isEditable}
        onClick={insertLink}
        title="Insert Link"
        ariaLabel="Insert Link"
      >
        <LinkIcon className="w-4 h-4" />
      </ToolbarButton>

      <Divider />

      <DropdownColorPicker
        disabled={!isEditable}
        buttonAriaLabel="Formatting text color"
        buttonIcon={<TextColorIcon className="w-4 h-4" />}
        color={toolbarState.fontColor}
        onChange={onFontColorSelect}
        title="Text color"
      />
      <DropdownColorPicker
        disabled={!isEditable}
        buttonAriaLabel="Formatting background color"
        buttonIcon={<BgColorIcon className="w-4 h-4" />}
        color={toolbarState.bgColor}
        onChange={onBgColorSelect}
        title="Background color"
      />
      <ToolbarButton
        disabled={!isEditable}
        onClick={(e) => clearFormatting(activeEditor, isKeyboardInput(e))}
        title="Clear formatting"
        ariaLabel="Clear all text formatting"
      >
        <ClearFormatIcon className="w-4 h-4" />
      </ToolbarButton>

      <Divider />

      {features?.pageLayout !== false && <PageSettingsDropDown disabled={!isEditable} />}

      <DropDown
        disabled={!isEditable}
        buttonLabel="Insert"
        buttonIcon={<InsertIcon className="w-4 h-4" />}
        buttonAriaLabel="Insert document feature"
      >
          <DropDownItem onClick={() => dispatchToolbarCommand(INSERT_DIVIDER_LEXICAL_COMMAND)}>
            <HorizontalRuleIcon className="w-4 h-4" /> Horizontal Rule
          </DropDownItem>
          <DropDownItem onClick={() => setInsertDialog("equation")}>
            <EquationIcon className="w-4 h-4" /> Equation
          </DropDownItem>
          <DropDownItem onClick={() => setInsertDialog("table")}>
            <TableIcon className="w-4 h-4" /> Table
          </DropDownItem>
          <DropDownItem
            onClick={() => dispatchToolbarCommand(INSERT_EXCALIDRAW_COMMAND)}
          >
            <ExcalidrawIcon className="w-4 h-4" /> Excalidraw
          </DropDownItem>
          <DropDownItem onClick={() => setInsertDialog("layout")}>
            <ColumnsIcon className="w-4 h-4" /> Columns Layout
          </DropDownItem>
          <DropDownItem
            onClick={() => dispatchToolbarCommand(INSERT_COLLAPSIBLE_COMMAND)}
          >
            <CollapsibleIcon className="w-4 h-4" /> Collapsible container
          </DropDownItem>
          <DropDownItem
            onClick={() => dispatchToolbarCommand(INSERT_STICKY_COMMAND)}
          >
            <StickyIcon className="w-4 h-4" /> Sticky Note
          </DropDownItem>
          <DropDownItem
            onClick={() => dispatchToolbarCommand(INSERT_ADMONITION_LEXICAL_COMMAND)}
          >
            <AdmonitionToolbarIcon className="w-4 h-4" /> Admonition
          </DropDownItem>
          <DropDownItem
            onClick={() => dispatchToolbarCommand(INSERT_BUTTON_LEXICAL_COMMAND)}
          >
            <ButtonToolbarIcon className="w-4 h-4" /> Button
          </DropDownItem>
          <DropDownItem
            onClick={() => dispatchToolbarCommand(INSERT_MERMAID_LEXICAL_COMMAND)}
          >
            <MermaidToolbarIcon className="w-4 h-4" /> Mermaid Diagram
          </DropDownItem>
          <DropDownItem
            onClick={() => dispatchToolbarCommand(INSERT_VEGA_LITE_LEXICAL_COMMAND)}
          >
            <VegaToolbarIcon className="w-4 h-4" /> Vega-Lite Chart
          </DropDownItem>
          <DropDownItem
            onClick={() => dispatchToolbarCommand(INSERT_MEDIA_LEXICAL_COMMAND, { mediaType: "image" })}
          >
            <MediaToolbarIcon className="w-4 h-4" /> Media Block
          </DropDownItem>
      </DropDown>

      <EmojiPickerPopover editor={activeEditor} disabled={!isEditable} />

      <Divider />

      <ElementFormatDropdown
        disabled={!isEditable}
        value={toolbarState.elementFormat}
        editor={activeEditor}
        isRTL={toolbarState.isRTL}
      />

      {/* Área contextual: dropdowns dependentes do bloco selecionado.
          Ficam no final do toolbar (segunda linha quando não cabe na
          primeira) para não empurrar os controles principais. */}
      {toolbarState.blockType === "code" && selectedElementKey && activeEditor === editor && (
        <>
          <Divider />
          <CodeLanguageDropDown
            disabled={!isEditable}
            language={toolbarState.codeLanguage}
            editor={activeEditor}
            codeNodeKey={selectedElementKey}
          />
        </>
      )}

      {/* Diálogos disparados pelo menu Insert. */}
      <Dialog
        open={insertDialog !== null}
        onOpenChange={(open) => {
          if (!open) setInsertDialog(null)
        }}
      >
        <DialogContent
          className={insertDialog === "equation" ? "sm:max-w-[720px]" : undefined}
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
            if (document.body.hasAttribute("data-math-keyboard-open")) e.preventDefault()
          }}
        >
          <DialogHeader>
            <DialogTitle>
              {insertDialog === "equation"
                ? "Insert Equation"
                : insertDialog === "table"
                  ? "Insert Table"
                  : "Insert Columns Layout"}
            </DialogTitle>
          </DialogHeader>
          {insertDialog === "equation" && (
            <InsertEquationDialog
              activeEditor={activeEditor}
              onClose={() => setInsertDialog(null)}
            />
          )}
          {insertDialog === "table" && (
            <InsertTableDialog
              activeEditor={activeEditor}
              onClose={() => setInsertDialog(null)}
            />
          )}
          {insertDialog === "layout" && (
            <InsertLayoutDialog
              activeEditor={activeEditor}
              onClose={() => setInsertDialog(null)}
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
