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
import {
  BoldIcon,
  CodeInlineIcon,
  ItalicIcon,
  LinkIcon,
  StrikethroughIcon,
  UnderlineIcon,
} from "../icons"
import { getSelectedNode } from "../toolbar/get-selected-node"
import { getDOMRangeRect, setFloatingElemPosition } from "./use-floating-position"

function BubbleButton({
  active,
  onClick,
  title,
  ariaLabel,
  children,
}: {
  active?: boolean
  onClick: () => void
  title: string
  ariaLabel: string
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      aria-label={ariaLabel}
      className={cn(
        "inline-flex items-center justify-center w-8 h-8 rounded text-white",
        "hover:bg-white/10",
        active && "bg-white/20",
      )}
    >
      {children}
    </button>
  )
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
        "bg-gray-900 dark:bg-gray-800 text-white",
        "opacity-0 will-change-transform pointer-events-auto z-50",
      )}
      style={{ transform: "translate(-10000px, -10000px)" }}
    >
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
  const [isCode, setIsCode] = useState(false)

  const updatePopup = useCallback(() => {
    editor.getEditorState().read(() => {
      if (editor.isComposing()) {
        return
      }
      const selection = $getSelection()
      const nativeSelection = getDOMSelection(editor._window)
      const rootElement = editor.getRootElement()

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
      setIsCode(selection.hasFormat("code"))

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

  const ref = useRef<HTMLDivElement | null>(null)
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
