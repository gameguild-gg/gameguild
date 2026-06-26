/**
 * FloatingTextFormatToolbarPlugin — ported from facebook/lexical
 * playground. Wave A: limited to bold/italic/underline/strikethrough/
 * inline-code/link buttons. (Sub/sup, lowercase/uppercase/capitalize,
 * and comment-strip are dropped from the bubble; they can still be
 * applied via the top toolbar's "additional styles" when we ship it
 * in a later wave.)
 */
"use client"

import * as React from "react"
import { Dispatch, useCallback, useEffect, useRef, useState } from "react"
import { createPortal } from "react-dom"
import { useMergeRefs } from "@floating-ui/react"
import { $isCodeHighlightNode } from "@lexical/code"
import { $isLinkNode, TOGGLE_LINK_COMMAND } from "@lexical/link"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { mergeRegister } from "@lexical/utils"
import { $getSelectionStyleValueForProperty, $patchStyleText } from "@lexical/selection"
import {
  $getSelection,
  $isParagraphNode,
  $isRangeSelection,
  $isTextNode,
  COMMAND_PRIORITY_LOW,
  FORMAT_TEXT_COMMAND,
  getDOMSelection,
  LexicalEditor,
  SELECTION_CHANGE_COMMAND,
} from "lexical"
import { cn } from "@/lib/utils"
import { DEFAULT_FONT_SIZE } from "../toolbar/toolbar-context"
import {
  BoldIcon,
  CodeInlineIcon,
  ItalicIcon,
  LinkIcon,
  StrikethroughIcon,
  UnderlineIcon,
  HighlightIcon,
  SubscriptIcon,
  SuperscriptIcon,
} from "../icons"
import { getSelectedNode } from "../toolbar/get-selected-node"
import { getDOMRangeRect, setFloatingElemPosition } from "./use-floating-position"

const MIN_FONT_SIZE = 8
const MAX_FONT_SIZE = 400

function BubbleButton({
  active,
  onClick,
  title,
  ariaLabel,
  disabled,
  children,
}: {
  active?: boolean
  onClick: () => void
  title: string
  ariaLabel: string
  disabled?: boolean
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      aria-label={ariaLabel}
      disabled={disabled}
      className={cn(
        "inline-flex items-center justify-center w-8 h-8 rounded",
        "text-gray-800 dark:text-white",
        "hover:bg-gray-100 dark:hover:bg-white/10",
        active && "bg-gray-200 dark:bg-white/20",
        disabled && "opacity-40 pointer-events-none",
      )}
    >
      {children}
    </button>
  )
}

function BubbleFontSizeStepper({
  editor,
  fontSize,
}: {
  editor: LexicalEditor
  fontSize: string
}) {
  const currentNumber = React.useMemo(() => {
    const parsed = parseInt(String(fontSize).replace(/px$/, ""), 10)
    return Number.isFinite(parsed) ? parsed : DEFAULT_FONT_SIZE
  }, [fontSize])
  const [inputValue, setInputValue] = useState<string>(String(currentNumber))

  useEffect(() => {
    setInputValue(String(currentNumber))
  }, [currentNumber])

  const applySize = useCallback(
    (px: number) => {
      const clamped = Math.max(MIN_FONT_SIZE, Math.min(MAX_FONT_SIZE, Math.round(px)))
      editor.update(() => {
        const selection = $getSelection()
        if (!$isRangeSelection(selection)) return
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
    <div className="inline-flex items-center gap-0.5 px-0.5">
      <button
        type="button"
        disabled={currentNumber <= MIN_FONT_SIZE}
        onClick={() => applySize(currentNumber - 1)}
        title="Decrease font size"
        aria-label="Decrease font size"
        className="inline-flex items-center justify-center w-6 h-7 rounded text-sm text-gray-800 dark:text-white hover:bg-gray-100 dark:hover:bg-white/10 disabled:opacity-40 disabled:pointer-events-none"
      >
        −
      </button>
      <input
        type="text"
        inputMode="numeric"
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
        className="w-8 h-7 bg-transparent text-sm text-center outline-none tabular-nums text-gray-800 dark:text-white rounded border border-gray-200 dark:border-white/20"
      />
      <button
        type="button"
        disabled={currentNumber >= MAX_FONT_SIZE}
        onClick={() => applySize(currentNumber + 1)}
        title="Increase font size"
        aria-label="Increase font size"
        className="inline-flex items-center justify-center w-6 h-7 rounded text-sm text-gray-800 dark:text-white hover:bg-gray-100 dark:hover:bg-white/10 disabled:opacity-40 disabled:pointer-events-none"
      >
        +
      </button>
    </div>
  )
}

function BubbleDivider() {
  return <div className="w-px h-5 mx-0.5 bg-gray-200 dark:bg-white/20" />
}

function TextFormatFloatingToolbar({
  editor,
  anchorElem,
  isLink,
  isBold,
  isItalic,
  isUnderline,
  isCode,
  isStrikethrough,
  isSubscript,
  isSuperscript,
  isHighlight,
  fontSize,
  setIsLinkEditMode,
  ref,
}: {
  editor: LexicalEditor
  anchorElem: HTMLElement
  isBold: boolean
  isCode: boolean
  isItalic: boolean
  isLink: boolean
  isStrikethrough: boolean
  isUnderline: boolean
  isSubscript: boolean
  isSuperscript: boolean
  isHighlight: boolean
  fontSize: string
  setIsLinkEditMode: Dispatch<boolean>
  ref?: React.Ref<HTMLDivElement | null>
}) {
  const popupRef = useRef<HTMLDivElement | null>(null)
  const mergedRef = useMergeRefs([popupRef, ref])

  const insertLink = useCallback(() => {
    if (!isLink) {
      setIsLinkEditMode(true)
      editor.dispatchCommand(TOGGLE_LINK_COMMAND, "https://")
    } else {
      setIsLinkEditMode(false)
      editor.dispatchCommand(TOGGLE_LINK_COMMAND, null)
    }
  }, [editor, isLink, setIsLinkEditMode])

  // Hide while a drag is in progress (upstream parity).
  useEffect(() => {
    function onMouseMove(e: MouseEvent) {
      if (popupRef.current && (e.buttons === 1 || e.buttons === 3)) {
        if (popupRef.current.style.pointerEvents !== "none") {
          const el = document.elementFromPoint(e.clientX, e.clientY)
          if (!popupRef.current.contains(el)) {
            popupRef.current.style.pointerEvents = "none"
          }
        }
      }
    }
    function onMouseUp() {
      if (popupRef.current && popupRef.current.style.pointerEvents !== "auto") {
        popupRef.current.style.pointerEvents = "auto"
      }
    }
    document.addEventListener("mousemove", onMouseMove)
    document.addEventListener("mouseup", onMouseUp)
    return () => {
      document.removeEventListener("mousemove", onMouseMove)
      document.removeEventListener("mouseup", onMouseUp)
    }
  }, [])

  const $updateTextFormatFloatingToolbar = useCallback(() => {
    const selection = $getSelection()
    const popupElem = popupRef.current
    const nativeSelection = getDOMSelection(editor._window)

    if (popupElem === null) {
      return
    }

    const rootElement = editor.getRootElement()
    if (
      selection !== null &&
      nativeSelection !== null &&
      !nativeSelection.isCollapsed &&
      rootElement !== null &&
      rootElement.contains(nativeSelection.anchorNode)
    ) {
      const rangeRect = getDOMRangeRect(nativeSelection, rootElement)
      setFloatingElemPosition(rangeRect, popupElem, anchorElem, isLink)
    }
  }, [editor, anchorElem, isLink])

  useEffect(() => {
    const scrollerElem = anchorElem.parentElement
    const update = () => {
      editor.getEditorState().read(() => {
        $updateTextFormatFloatingToolbar()
      })
    }
    window.addEventListener("resize", update)
    scrollerElem?.addEventListener("scroll", update)
    return () => {
      window.removeEventListener("resize", update)
      scrollerElem?.removeEventListener("scroll", update)
    }
  }, [editor, $updateTextFormatFloatingToolbar, anchorElem])

  useEffect(() => {
    editor.getEditorState().read(() => {
      $updateTextFormatFloatingToolbar()
    })
    return mergeRegister(
      editor.registerUpdateListener(({ editorState }) => {
        editorState.read(() => {
          $updateTextFormatFloatingToolbar()
        })
      }),
      editor.registerCommand(
        SELECTION_CHANGE_COMMAND,
        () => {
          $updateTextFormatFloatingToolbar()
          return false
        },
        COMMAND_PRIORITY_LOW,
      ),
    )
  }, [editor, $updateTextFormatFloatingToolbar])

  if (!editor.isEditable()) {
    return null
  }

  return (
    <div
      ref={mergedRef}
      className={cn(
        "absolute top-0 left-0 flex items-center gap-0.5 p-1 rounded-md shadow-lg",
        "bg-white text-gray-900 border border-gray-200",
        "dark:bg-gray-900 dark:text-white dark:border-gray-700",
        "opacity-0 will-change-transform pointer-events-auto z-50",
      )}
      style={{ transform: "translate(-10000px, -10000px)" }}
    >
      <BubbleFontSizeStepper editor={editor} fontSize={fontSize} />
      <BubbleDivider />
      <BubbleButton
        active={isBold}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "bold")}
        title="Bold"
        ariaLabel="Format text as bold"
      >
        <BoldIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isItalic}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "italic")}
        title="Italic"
        ariaLabel="Format text as italic"
      >
        <ItalicIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isUnderline}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "underline")}
        title="Underline"
        ariaLabel="Format text as underlined"
      >
        <UnderlineIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isStrikethrough}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "strikethrough")}
        title="Strikethrough"
        ariaLabel="Format text with a strikethrough"
      >
        <StrikethroughIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isSubscript}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "subscript")}
        title="Subscript"
        ariaLabel="Format text as subscript"
      >
        <SubscriptIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isSuperscript}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "superscript")}
        title="Superscript"
        ariaLabel="Format text as superscript"
      >
        <SuperscriptIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isHighlight}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "highlight")}
        title="Highlight"
        ariaLabel="Format text as highlighted"
      >
        <HighlightIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isCode}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "code")}
        title="Inline code"
        ariaLabel="Insert inline code"
      >
        <CodeInlineIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isLink}
        onClick={insertLink}
        title="Insert link"
        ariaLabel="Insert link"
      >
        <LinkIcon className="w-4 h-4" />
      </BubbleButton>
    </div>
  )
}

function useFloatingTextFormatToolbar(
  editor: LexicalEditor,
  anchorElem: HTMLElement,
  setIsLinkEditMode: Dispatch<boolean>,
) {
  const [isText, setIsText] = useState(false)
  const [isLink, setIsLink] = useState(false)
  const [isBold, setIsBold] = useState(false)
  const [isItalic, setIsItalic] = useState(false)
  const [isUnderline, setIsUnderline] = useState(false)
  const [isStrikethrough, setIsStrikethrough] = useState(false)
  const [isSubscript, setIsSubscript] = useState(false)
  const [isSuperscript, setIsSuperscript] = useState(false)
  const [isHighlight, setIsHighlight] = useState(false)
  const [isCode, setIsCode] = useState(false)
  const [fontSize, setFontSize] = useState<string>(`${DEFAULT_FONT_SIZE}px`)

  const ref = useRef<HTMLDivElement | null>(null)

  const updatePopup = useCallback(() => {
    // If the focus/selection is inside the bubble itself (e.g., font-size input),
    // we don't recalculate visibility — otherwise the bubble disappears as soon as the user clicks the input.
    const active = document.activeElement
    if (ref.current && active && ref.current.contains(active)) {
      return
    }
    editor.getEditorState().read(() => {
      if (editor.isComposing()) {
        return
      }
      const selection = $getSelection()
      const nativeSelection = getDOMSelection(editor._window)
      const rootElement = editor.getRootElement()

      // When the native selection is inside the bubble (input focused),
      // we ignore it — the editor's selection is preserved in Lexical's state.
      if (
        nativeSelection !== null &&
        ref.current &&
        ref.current.contains(nativeSelection.anchorNode as Node | null)
      ) {
        return
      }

      if (
        nativeSelection !== null &&
        (!$isRangeSelection(selection) ||
          rootElement === null ||
          !rootElement.contains(nativeSelection.anchorNode))
      ) {
        setIsText(false)
        return
      }
      if (!$isRangeSelection(selection)) {
        return
      }

      const node = getSelectedNode(selection)
      setIsBold(selection.hasFormat("bold"))
      setIsItalic(selection.hasFormat("italic"))
      setIsUnderline(selection.hasFormat("underline"))
      setIsStrikethrough(selection.hasFormat("strikethrough"))
      setIsSubscript(selection.hasFormat("subscript"))
      setIsSuperscript(selection.hasFormat("superscript"))
      setIsHighlight(selection.hasFormat("highlight"))
      setIsCode(selection.hasFormat("code"))
      setFontSize(
        $getSelectionStyleValueForProperty(selection, "font-size", `${DEFAULT_FONT_SIZE}px`),
      )

      const parent = node.getParent()
      setIsLink($isLinkNode(parent) || $isLinkNode(node))

      if (
        !$isCodeHighlightNode(selection.anchor.getNode()) &&
        selection.getTextContent() !== ""
      ) {
        setIsText($isTextNode(node) || $isParagraphNode(node))
      } else {
        setIsText(false)
      }

      const rawTextContent = selection.getTextContent().replace(/\n/g, "")
      if (!selection.isCollapsed() && rawTextContent === "") {
        setIsText(false)
      }
    })
  }, [editor])

  useEffect(() => {
    document.addEventListener("selectionchange", updatePopup)
    return () => {
      document.removeEventListener("selectionchange", updatePopup)
    }
  }, [updatePopup])

  useEffect(() => {
    const onDragStart = () => {
      if (ref.current) {
        ref.current.style.display = "none"
      }
    }
    const onDragEnd = () => {
      if (ref.current && ref.current.style.display === "none") {
        ref.current.style.display = "block"
      }
    }
    document.addEventListener("dragstart", onDragStart, true)
    document.addEventListener("dragend", onDragEnd, true)
    document.addEventListener("drop", onDragEnd, true)
    return () => {
      document.removeEventListener("dragstart", onDragStart, true)
      document.removeEventListener("dragend", onDragEnd, true)
      document.removeEventListener("drop", onDragEnd, true)
    }
  }, [])

  useEffect(() => {
    return mergeRegister(
      editor.registerUpdateListener(() => {
        updatePopup()
      }),
      editor.registerRootListener(() => {
        if (editor.getRootElement() === null) {
          setIsText(false)
        }
      }),
    )
  }, [editor, updatePopup])

  if (!isText || isLink) {
    return null
  }

  return createPortal(
    <TextFormatFloatingToolbar
      editor={editor}
      anchorElem={anchorElem}
      ref={ref}
      isLink={isLink}
      isBold={isBold}
      isItalic={isItalic}
      isStrikethrough={isStrikethrough}
      isUnderline={isUnderline}
      isCode={isCode}
      isSubscript={isSubscript}
      isSuperscript={isSuperscript}
      isHighlight={isHighlight}
      fontSize={fontSize}
      setIsLinkEditMode={setIsLinkEditMode}
    />,
    anchorElem,
  )
}

export default function FloatingTextFormatToolbarPlugin({
  anchorElem,
  setIsLinkEditMode,
}: {
  anchorElem: HTMLElement
  setIsLinkEditMode: Dispatch<boolean>
}) {
  const [editor] = useLexicalComposerContext()
  return useFloatingTextFormatToolbar(editor, anchorElem, setIsLinkEditMode)
}
