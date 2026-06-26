/**
 * CodeActionMenuPlugin — small floating menu (copy button + language
 * label) shown when the mouse hovers a `<code>` block.
 *
 * Simplified port of `lexical-playground/src/plugins/CodeActionMenuPlugin`:
 * only the copy + language label are kept (Prettier formatting is not
 * wired since we don't ship Prettier client-side).
 */
"use client"

import * as React from "react"
import { useCallback, useEffect, useMemo, useRef, useState } from "react"
import { createPortal } from "react-dom"
import { $isCodeNode, CodeNode } from "@lexical/code"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import {
  $getNearestNodeFromDOMNode,
  $getNodeByKey,
  $getSelection,
  $createNodeSelection,
  $setSelection,
  COPY_COMMAND,
  isHTMLElement,
  type NodeKey,
} from "lexical"
import { cn } from "@/lib/utils"
import { Copy, Check, Trash2 } from "lucide-react"
import { useNodesDeleteProtection } from "../shared/use-node-delete-protection"
import { DeleteConfirmDialog } from "../../extras/dialogs/delete-confirm-dialog"

const CODE_PADDING = 8

type Position = { top: string; right: string }

function getMouseInfo(event: MouseEvent): { codeDOMNode: HTMLElement | null; isOutside: boolean } {
  const target = event.target
  if (isHTMLElement(target)) {
    const codeDOMNode = target.closest<HTMLElement>("code")
    const isOutside = !(
      codeDOMNode ||
      target.closest<HTMLElement>("div.lexical-code-action-menu")
    )
    return { codeDOMNode, isOutside }
  }
  return { codeDOMNode: null, isOutside: true }
}

function debounce<T extends (...args: unknown[]) => void>(fn: T, ms: number) {
  let id: number | null = null
  const wrapped = (...args: Parameters<T>) => {
    if (id !== null) window.clearTimeout(id)
    id = window.setTimeout(() => fn(...args), ms)
  }
  wrapped.cancel = () => {
    if (id !== null) window.clearTimeout(id)
  }
  return wrapped
}

function CodeActionMenuContainer({ anchorElem }: { anchorElem: HTMLElement }) {
  const [editor] = useLexicalComposerContext()
  const [lang, setLang] = useState("")
  const [isShown, setShown] = useState(false)
  const [shouldListen, setShouldListen] = useState(false)
  const [position, setPosition] = useState<Position>({ right: "0", top: "0" })
  const [copied, setCopied] = useState(false)
  const [pendingDeleteKey, setPendingDeleteKey] = useState<NodeKey | null>(null)
  const codeSetRef = useRef<Set<string>>(new Set())
  const codeDOMNodeRef = useRef<HTMLElement | null>(null)

  const onMouseMove = useCallback(
    (event: MouseEvent) => {
      const { codeDOMNode, isOutside } = getMouseInfo(event)
      if (isOutside) {
        setShown(false)
        return
      }
      if (!codeDOMNode) return
      codeDOMNodeRef.current = codeDOMNode

      let language = ""
      let found = false
      editor.update(() => {
        const node = $getNearestNodeFromDOMNode(codeDOMNode)
        if ($isCodeNode(node)) {
          language = node.getLanguage() ?? ""
          found = true
        }
      })
      if (found) {
        const { y: anchorY, right: anchorRight } = anchorElem.getBoundingClientRect()
        const { y, right } = codeDOMNode.getBoundingClientRect()
        setLang(language)
        setShown(true)
        setPosition({
          right: `${anchorRight - right + CODE_PADDING}px`,
          top: `${y - anchorY}px`,
        })
      }
    },
    [anchorElem, editor],
  )

  useEffect(() => {
    if (!shouldListen) return
    const debounced = debounce(onMouseMove as (...args: unknown[]) => void, 50)
    document.addEventListener("mousemove", debounced)
    return () => {
      setShown(false)
      debounced.cancel()
      document.removeEventListener("mousemove", debounced)
    }
  }, [shouldListen, onMouseMove])

  useEffect(() => {
    return editor.registerMutationListener(
      CodeNode,
      (mutations) => {
        editor.getEditorState().read(() => {
          for (const [key, type] of mutations) {
            if (type === "created") codeSetRef.current.add(key)
            else if (type === "destroyed") codeSetRef.current.delete(key)
          }
        })
        setShouldListen(codeSetRef.current.size > 0)
      },
      { skipInitialization: false },
    )
  }, [editor])

  const onCopy = useCallback(() => {
    const dom = codeDOMNodeRef.current
    if (!dom) return
    editor.update(() => {
      const node = $getNearestNodeFromDOMNode(dom)
      if (!$isCodeNode(node)) return
      const selection = $createNodeSelection()
      selection.add(node.getKey())
      $setSelection(selection)
      const ce = new ClipboardEvent("copy")
      editor.dispatchCommand(COPY_COMMAND, ce)
      $setSelection($getSelection())
    })
    void navigator.clipboard
      .writeText(dom.innerText)
      .then(() => {
        setCopied(true)
        window.setTimeout(() => setCopied(false), 1200)
      })
      .catch(() => {
        /* clipboard may be denied */
      })
  }, [editor])

  // Proteção contra exclusão acidental: ao tentar apagar um CodeNode,
  // intercepta a tecla e abre dialog de confirmação.
  const getNodeKeys = useCallback(() => codeSetRef.current, [])
  useNodesDeleteProtection({
    getNodeKeys,
    onRequestDelete: (key) => setPendingDeleteKey(key),
  })

  const onRequestDeleteHovered = useCallback(() => {
    const dom = codeDOMNodeRef.current
    if (!dom) return
    editor.read(() => {
      const node = $getNearestNodeFromDOMNode(dom)
      if ($isCodeNode(node)) setPendingDeleteKey(node.getKey())
    })
  }, [editor])

  const confirmDelete = useCallback(() => {
    if (pendingDeleteKey === null) return
    const key = pendingDeleteKey
    editor.update(() => {
      const node = $getNodeByKey(key)
      if (node) node.remove()
    })
    setPendingDeleteKey(null)
  }, [editor, pendingDeleteKey])

  const menuOverlay = useMemo(() => {
    if (!isShown) return null
    return (
      <div
        className={cn(
          "lexical-code-action-menu absolute z-30 flex items-center gap-2 px-2 py-1 rounded shadow",
          "border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900",
          "text-xs text-gray-700 dark:text-gray-300",
        )}
        style={{ top: position.top, right: position.right }}
      >
        <span className="font-mono">{lang || "(no language)"}</span>
        <button
          type="button"
          onClick={onCopy}
          className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          {copied ? <Check className="w-3 h-3" /> : <Copy className="w-3 h-3" />}
          {copied ? "Copied" : "Copy"}
        </button>
        <button
          type="button"
          onClick={onRequestDeleteHovered}
          className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded hover:bg-red-100 dark:hover:bg-red-900/40 text-red-600 dark:text-red-400"
        >
          <Trash2 className="w-3 h-3" />
          Delete
        </button>
      </div>
    )
  }, [isShown, position, lang, onCopy, onRequestDeleteHovered, copied])

  return (
    <>
      {menuOverlay}
      <DeleteConfirmDialog
        open={pendingDeleteKey !== null}
        onOpenChange={(o) => {
          if (!o) setPendingDeleteKey(null)
        }}
        title="Remove code block?"
        itemName="this code block"
        itemType="code block"
        confirmText="Remove"
        onConfirm={confirmDelete}
      />
    </>
  )
}

export function CodeActionMenuPlugin({
  anchorElem,
}: {
  anchorElem?: HTMLElement
} = {}): React.ReactPortal | null {
  const [resolvedAnchor, setResolvedAnchor] = useState<HTMLElement | null>(
    anchorElem ?? null,
  )
  useEffect(() => {
    if (!anchorElem && typeof document !== "undefined") setResolvedAnchor(document.body)
  }, [anchorElem])
  if (!resolvedAnchor) return null
  return createPortal(<CodeActionMenuContainer anchorElem={resolvedAnchor} />, resolvedAnchor)
}
