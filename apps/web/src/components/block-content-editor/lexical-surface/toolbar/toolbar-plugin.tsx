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
import { $isCodeNode } from "@lexical/code"
import { $isLinkNode, TOGGLE_LINK_COMMAND } from "@lexical/link"
import { $isListNode, ListNode } from "@lexical/list"
import { INSERT_HORIZONTAL_RULE_COMMAND } from "@lexical/react/LexicalHorizontalRuleNode"
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
  $getSelection,
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
import { cn } from "@/lib/utils"
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
  Heading1Icon,
  Heading2Icon,
  Heading3Icon,
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

// ─── constants ──────────────────────────────────────────────────────────────

const FONT_FAMILY_OPTIONS: ReadonlyArray<readonly [string, string]> = [
  ["Arial", "Arial"],
  ["Courier New", "Courier New"],
  ["Georgia", "Georgia"],
  ["Times New Roman", "Times New Roman"],
  ["Trebuchet MS", "Trebuchet MS"],
  ["Verdana", "Verdana"],
]

const FONT_SIZE_OPTIONS: ReadonlyArray<readonly [string, string]> = [
  ["10px", "10"],
  ["11px", "11"],
  ["12px", "12"],
  ["13px", "13"],
  ["14px", "14"],
  ["15px", "15"],
  ["16px", "16"],
  ["17px", "17"],
  ["18px", "18"],
  ["19px", "19"],
  ["20px", "20"],
  ["24px", "24"],
  ["28px", "28"],
  ["32px", "32"],
  ["48px", "48"],
  ["72px", "72"],
]

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
      buttonClassName="min-w-[120px]"
      buttonAriaLabel="Formatting options for text style"
    >
      <DropDownItem active={blockType === "paragraph"} onClick={() => formatParagraph(editor)}>
        <ParagraphIcon className="w-4 h-4" /> Normal
      </DropDownItem>
      <DropDownItem active={blockType === "h1"} onClick={() => formatHeading(editor, blockType, "h1")}>
        <Heading1Icon className="w-4 h-4" /> Heading 1
      </DropDownItem>
      <DropDownItem active={blockType === "h2"} onClick={() => formatHeading(editor, blockType, "h2")}>
        <Heading2Icon className="w-4 h-4" /> Heading 2
      </DropDownItem>
      <DropDownItem active={blockType === "h3"} onClick={() => formatHeading(editor, blockType, "h3")}>
        <Heading3Icon className="w-4 h-4" /> Heading 3
      </DropDownItem>
      <DropDownItem active={blockType === "bullet"} onClick={() => formatBulletList(editor, blockType)}>
        <BulletedListIcon className="w-4 h-4" /> Bullet List
      </DropDownItem>
      <DropDownItem active={blockType === "number"} onClick={() => formatNumberedList(editor, blockType)}>
        <NumberedListIcon className="w-4 h-4" /> Numbered List
      </DropDownItem>
      <DropDownItem active={blockType === "check"} onClick={() => formatCheckList(editor, blockType)}>
        <CheckListIcon className="w-4 h-4" /> Check List
      </DropDownItem>
      <DropDownItem active={blockType === "quote"} onClick={() => formatQuote(editor, blockType)}>
        <QuoteIcon className="w-4 h-4" /> Quote
      </DropDownItem>
      <DropDownItem active={blockType === "code"} onClick={() => formatCode(editor, blockType)}>
        <CodeBlockIcon className="w-4 h-4" /> Code Block
      </DropDownItem>
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
  const options = style === "font-family" ? FONT_FAMILY_OPTIONS : FONT_SIZE_OPTIONS
  const display = style === "font-size" ? value.replace(/px$/, "") : value

  const handleClick = useCallback(
    (option: string) => {
      editor.update(() => {
        $addUpdateTag(SKIP_SELECTION_FOCUS_TAG)
        const selection = $getSelection()
        if (selection !== null) {
          $patchStyleText(selection, { [style]: option })
        }
      })
    },
    [editor, style],
  )

  return (
    <DropDown
      disabled={disabled}
      buttonLabel={display}
      buttonClassName={style === "font-family" ? "min-w-[110px]" : "min-w-[60px]"}
      buttonAriaLabel={
        style === "font-family" ? "Formatting options for font family" : "Formatting options for font size"
      }
    >
      {options.map(([option, label]) => (
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
      buttonClassName="min-w-[120px]"
      buttonAriaLabel="Formatting options for text alignment"
    >
      <DropDownItem onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "left")}>
        <AlignLeftIcon className="w-4 h-4" /> Left Align
      </DropDownItem>
      <DropDownItem onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "center")}>
        <AlignCenterIcon className="w-4 h-4" /> Center Align
      </DropDownItem>
      <DropDownItem onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "right")}>
        <AlignRightIcon className="w-4 h-4" /> Right Align
      </DropDownItem>
      <DropDownItem onClick={() => editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "justify")}>
        <AlignJustifyIcon className="w-4 h-4" /> Justify Align
      </DropDownItem>
      <DropDownDivider />
      <DropDownItem onClick={() => editor.dispatchCommand(OUTDENT_CONTENT_COMMAND, undefined)}>
        <OutdentIcon className="w-4 h-4" /> Outdent
      </DropDownItem>
      <DropDownItem onClick={() => editor.dispatchCommand(INDENT_CONTENT_COMMAND, undefined)}>
        <IndentIcon className="w-4 h-4" /> Indent
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

export default function ToolbarPlugin({
  editor,
  activeEditor,
  setActiveEditor,
  setIsLinkEditMode,
}: {
  editor: LexicalEditor
  activeEditor: LexicalEditor
  setActiveEditor: Dispatch<LexicalEditor>
  setIsLinkEditMode: Dispatch<boolean>
}) {
  const [, setSelectedElementKey] = useState<string | null>(null)
  const [isEditable, setIsEditable] = useState(() => editor.isEditable())
  const { toolbarState, updateToolbarState } = useToolbarState()

  const dispatchToolbarCommand = <T extends LexicalCommand<unknown>>(
    command: T,
    payload: CommandPayloadType<T> | undefined = undefined,
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
        $getSelectionStyleValueForProperty(selection, "font-family", "Arial"),
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
      updateToolbarState(
        "fontSize",
        $getSelectionStyleValueForProperty(selection, "font-size", `${DEFAULT_FONT_SIZE}px`),
      )
      updateToolbarState("isLowercase", selection.hasFormat("lowercase"))
      updateToolbarState("isUppercase", selection.hasFormat("uppercase"))
      updateToolbarState("isCapitalize", selection.hasFormat("capitalize"))
    }

    if ($isNodeSelection(selection)) {
      const nodes = selection.getNodes()
      for (const selectedNode of nodes) {
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
        "bg-white dark:bg-gray-900 sticky top-0 z-10",
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
      <FontDropDown
        disabled={!isEditable}
        style="font-size"
        value={toolbarState.fontSize}
        editor={activeEditor}
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

      <PageSettingsDropDown disabled={!isEditable} />

      <DropDown
        disabled={!isEditable}
        buttonLabel="Insert"
        buttonIcon={<InsertIcon className="w-4 h-4" />}
        buttonAriaLabel="Insert specialized editor node"
      >
        <DropDownItem onClick={() => dispatchToolbarCommand(INSERT_HORIZONTAL_RULE_COMMAND)}>
          <HorizontalRuleIcon className="w-4 h-4" /> Horizontal Rule
        </DropDownItem>
      </DropDown>

      <Divider />

      <ElementFormatDropdown
        disabled={!isEditable}
        value={toolbarState.elementFormat}
        editor={activeEditor}
        isRTL={toolbarState.isRTL}
      />
    </div>
  )
}
