/**
 * Floating action menu for CollapsibleContainerNode.
 *
 * Keeps settings discoverable after insertion and offers safe removal
 * (including Backspace/Delete protection via confirmation).
 */
"use client"

import * as React from "react"
import { useCallback, useEffect, useRef, useState } from "react"
import { createPortal } from "react-dom"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { useLexicalEditable } from "@lexical/react/useLexicalEditable"
import { $findMatchingParent } from "@lexical/utils"
import {
  $getNodeByKey,
  $getSelection,
  $isNodeSelection,
  $isRangeSelection,
  type NodeKey,
} from "lexical"
import { ChevronDown, PanelTopOpen, Trash2 } from "lucide-react"
import { cn } from "@game-guild/ui/lib/utils"
import { DeleteConfirmDialog } from "@game-guild/lexical-surface/dialogs/delete-confirm-dialog"
import ColorPicker from "../toolbar/color-picker"
import { useNodesDeleteProtection } from "../shared/use-node-delete-protection"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from "@game-guild/ui/components/dropdown-menu"
import {
  $isCollapsibleContainerNode,
  CollapsibleContainerNode,
} from "./collapsible-container-node"
import { UPDATE_COLLAPSIBLE_STYLE_COMMAND } from "./collapsible-plugin"

type CollapsibleState = {
  nodeKey: NodeKey
  open: boolean
  borderAlwaysVisible: boolean
  borderColor: string | null
}

export function CollapsibleActionMenuPlugin({
  anchorElem,
}: {
  anchorElem: HTMLElement
}): React.ReactNode {
  const [editor] = useLexicalComposerContext()
  const isEditable = useLexicalEditable()
  const [containerEl, setContainerEl] = useState<HTMLElement | null>(null)
  const [pos, setPos] = useState<{ top: number; left: number } | null>(null)
  const [state, setState] = useState<CollapsibleState | null>(null)
  const trackedNodeKeysRef = useRef<Set<NodeKey>>(new Set())
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)
  const [pendingDeleteNodeKey, setPendingDeleteNodeKey] = useState<NodeKey | null>(null)

  useEffect(() => {
    return editor.registerMutationListener(CollapsibleContainerNode, (mutations) => {
      const next = new Set(trackedNodeKeysRef.current)
      for (const [nodeKey, mutation] of mutations) {
        if (mutation === "destroyed") {
          next.delete(nodeKey)
        } else {
          next.add(nodeKey)
        }
      }
      trackedNodeKeysRef.current = next
    })
  }, [editor])

  useNodesDeleteProtection({
    enabled: isEditable,
    getNodeKeys: () => trackedNodeKeysRef.current,
    onRequestDelete: (nodeKey) => {
      setPendingDeleteNodeKey(nodeKey)
      setConfirmDeleteOpen(true)
    },
  })

  useEffect(() => {
    const update = () => {
      editor.getEditorState().read(() => {
        const selection = $getSelection()
        let container: CollapsibleContainerNode | null = null

        if ($isRangeSelection(selection)) {
          const parent = $findMatchingParent(
            selection.anchor.getNode(),
            $isCollapsibleContainerNode,
          )
          container = $isCollapsibleContainerNode(parent) ? parent : null
        } else if ($isNodeSelection(selection)) {
          for (const node of selection.getNodes()) {
            if ($isCollapsibleContainerNode(node)) {
              container = node
              break
            }
            const parent = $findMatchingParent(node, $isCollapsibleContainerNode)
            if ($isCollapsibleContainerNode(parent)) {
              container = parent
              break
            }
          }
        }

        if (!container) {
          setContainerEl(null)
          setState(null)
          return
        }

        const dom = editor.getElementByKey(container.getKey())
        if (!dom) {
          setContainerEl(null)
          setState(null)
          return
        }

        setContainerEl(dom as HTMLElement)
        setState({
          nodeKey: container.getKey(),
          open: container.getOpen(),
          borderAlwaysVisible: container.getBorderAlwaysVisible(),
          borderColor: container.getBorderColor(),
        })
      })
    }

    update()
    return editor.registerUpdateListener(update)
  }, [editor])

  useEffect(() => {
    if (!containerEl) {
      setPos(null)
      return
    }

    const compute = () => {
      const targetRect = containerEl.getBoundingClientRect()
      const anchorRect = anchorElem.getBoundingClientRect()
      setPos({
        top: targetRect.top - anchorRect.top + 4,
        left: targetRect.right - anchorRect.left - 28,
      })
    }

    compute()
    const ro = new ResizeObserver(compute)
    ro.observe(containerEl)
    window.addEventListener("scroll", compute, true)
    window.addEventListener("resize", compute)
    return () => {
      ro.disconnect()
      window.removeEventListener("scroll", compute, true)
      window.removeEventListener("resize", compute)
    }
  }, [containerEl, anchorElem])

  const updateStyle = useCallback(
    (payload: {
      borderAlwaysVisible?: boolean
      borderColor?: string | null
    }) => {
      if (!state) return
      editor.dispatchCommand(UPDATE_COLLAPSIBLE_STYLE_COMMAND, {
        nodeKey: state.nodeKey,
        ...payload,
      })
    },
    [editor, state],
  )

  const toggleOpen = useCallback(() => {
    if (!state) return
    editor.update(() => {
      const node = $getNodeByKey(state.nodeKey)
      if ($isCollapsibleContainerNode(node)) {
        node.toggleOpen()
      }
    })
  }, [editor, state])

  const requestDeleteCurrent = useCallback(() => {
    if (!state) return
    setPendingDeleteNodeKey(state.nodeKey)
    setConfirmDeleteOpen(true)
  }, [state])

  const confirmDelete = useCallback(() => {
    const key = pendingDeleteNodeKey
    if (!key) return
    editor.update(() => {
      const node = $getNodeByKey(key)
      if ($isCollapsibleContainerNode(node)) {
        node.remove()
      }
    })
    setPendingDeleteNodeKey(null)
    setConfirmDeleteOpen(false)
  }, [editor, pendingDeleteNodeKey])

  if (!isEditable || !containerEl || !pos || !state) return null

  return (
    <>
      {createPortal(
        <div
          className="absolute z-40"
          style={{ top: pos.top, left: pos.left }}
          onMouseDown={(e) => e.preventDefault()}
        >
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                aria-label="Collapsible settings"
                className={cn(
                  "inline-flex h-6 items-center justify-center gap-1 rounded px-1.5",
                  "border border-gray-300 dark:border-gray-700",
                  "bg-white/90 dark:bg-gray-800/90 text-gray-700 dark:text-gray-200",
                  "shadow-sm hover:bg-gray-100 dark:hover:bg-gray-700",
                )}
              >
                <PanelTopOpen className="h-3.5 w-3.5" />
                <ChevronDown className="h-3.5 w-3.5" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-64" onCloseAutoFocus={(e) => e.preventDefault()}>
              <DropdownMenuItem onSelect={toggleOpen}>
                {state.open ? "Collapse" : "Expand"}
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onSelect={() =>
                  updateStyle({ borderAlwaysVisible: !state.borderAlwaysVisible })
                }
              >
                Always show border
                {state.borderAlwaysVisible && (
                  <span className="ml-auto text-blue-600">✓</span>
                )}
              </DropdownMenuItem>
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>Border color</DropdownMenuSubTrigger>
                <DropdownMenuSubContent className="p-3" onFocusOutside={(e) => { const t = (e as any).detail?.originalEvent?.target; if (t instanceof Element && t.closest("[contenteditable=\"true\"]")) e.preventDefault(); }}>
                  <ColorPicker
                    color={state.borderColor ?? "#d1d5db"}
                    onChange={(next) =>
                      updateStyle({
                        borderColor:
                          typeof next === "string" && next.trim() !== ""
                            ? next
                            : null,
                      })
                    }
                  />
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onSelect={() => updateStyle({ borderColor: null })}>
                    Use default border color
                  </DropdownMenuItem>
                </DropdownMenuSubContent>
              </DropdownMenuSub>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onSelect={requestDeleteCurrent}
                className="text-red-600 focus:text-red-600"
              >
                <Trash2 className="mr-2 h-3.5 w-3.5" />
                Remove collapsible
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>,
        anchorElem,
      )}

      <DeleteConfirmDialog
        open={confirmDeleteOpen}
        onOpenChange={setConfirmDeleteOpen}
        title="Remove collapsible container?"
        itemName="this collapsible block"
        itemType="collapsible"
        onConfirm={confirmDelete}
        confirmText="Remove"
      />
    </>
  )
}
