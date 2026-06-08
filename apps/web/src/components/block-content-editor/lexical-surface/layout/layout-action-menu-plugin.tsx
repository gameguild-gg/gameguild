/**
 * Floating action menu for LayoutContainerNode.
 *
 * Allows re-opening layout settings anytime (template, border visibility,
 * border style, border color) and removing the block with confirmation.
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
import { AlertTriangle, ChevronDown, LayoutGrid, Trash2 } from "lucide-react"
import { cn } from "@/lib/utils"
import { BaseConfirmDialog } from "../../extras/dialogs/base-confirm-dialog"
import { DeleteConfirmDialog } from "../../extras/dialogs/delete-confirm-dialog"
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
} from "@/components/ui/dropdown-menu"
import {
  $isLayoutContainerNode,
  LayoutContainerNode,
} from "./layout-container-node"
import {
  LAYOUT_TEMPLATES,
  UPDATE_LAYOUT_BORDER_COMMAND,
  UPDATE_LAYOUT_COMMAND,
} from "./layout-plugin"

type LayoutState = {
  nodeKey: NodeKey
  template: string
  borderAlwaysVisible: boolean
  borderColor: string | null
}

type PendingReduceColumnsChange = {
  nodeKey: NodeKey
  template: string
  fromCount: number
  toCount: number
}

function getTemplateColumnsCount(template: string): number {
  return template.trim().split(/\s+/).length
}

export function LayoutActionMenuPlugin({
  anchorElem,
}: {
  anchorElem: HTMLElement
}): React.ReactNode {
  const [editor] = useLexicalComposerContext()
  const isEditable = useLexicalEditable()
  const [layoutEl, setLayoutEl] = useState<HTMLElement | null>(null)
  const [pos, setPos] = useState<{ top: number; left: number } | null>(null)
  const [layoutState, setLayoutState] = useState<LayoutState | null>(null)
  const trackedNodeKeysRef = useRef<Set<NodeKey>>(new Set())
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)
  const [pendingDeleteNodeKey, setPendingDeleteNodeKey] = useState<NodeKey | null>(null)
  const [confirmReduceColumnsOpen, setConfirmReduceColumnsOpen] = useState(false)
  const [pendingReduceColumnsChange, setPendingReduceColumnsChange] =
    useState<PendingReduceColumnsChange | null>(null)

  useEffect(() => {
    return editor.registerMutationListener(LayoutContainerNode, (mutations) => {
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
        let container: LayoutContainerNode | null = null

        if ($isRangeSelection(selection)) {
          const parent = $findMatchingParent(
            selection.anchor.getNode(),
            $isLayoutContainerNode,
          )
          container = $isLayoutContainerNode(parent) ? parent : null
        } else if ($isNodeSelection(selection)) {
          for (const node of selection.getNodes()) {
            if ($isLayoutContainerNode(node)) {
              container = node
              break
            }
            const parent = $findMatchingParent(node, $isLayoutContainerNode)
            if ($isLayoutContainerNode(parent)) {
              container = parent
              break
            }
          }
        }

        if (!container) {
          setLayoutEl(null)
          setLayoutState(null)
          return
        }

        const dom = editor.getElementByKey(container.getKey())
        if (!dom) {
          setLayoutEl(null)
          setLayoutState(null)
          return
        }

        setLayoutEl(dom as HTMLElement)
        setLayoutState({
          nodeKey: container.getKey(),
          template: container.getTemplateColumns(),
          borderAlwaysVisible: container.getBorderAlwaysVisible(),
          borderColor: container.getBorderColor(),
        })
      })
    }

    update()
    return editor.registerUpdateListener(update)
  }, [editor])

  useEffect(() => {
    if (!layoutEl) {
      setPos(null)
      return
    }
    const compute = () => {
      const targetRect = layoutEl.getBoundingClientRect()
      const anchorRect = anchorElem.getBoundingClientRect()
      setPos({
        top: targetRect.top - anchorRect.top + 4,
        left: targetRect.right - anchorRect.left - 28,
      })
    }

    compute()
    const ro = new ResizeObserver(compute)
    ro.observe(layoutEl)
    window.addEventListener("scroll", compute, true)
    window.addEventListener("resize", compute)
    return () => {
      ro.disconnect()
      window.removeEventListener("scroll", compute, true)
      window.removeEventListener("resize", compute)
    }
  }, [layoutEl, anchorElem])

  const updateLayoutTemplate = useCallback(
    (template: string) => {
      if (!layoutState) return

      const currentColumns = getTemplateColumnsCount(layoutState.template)
      const nextColumns = getTemplateColumnsCount(template)

      if (nextColumns < currentColumns) {
        setPendingReduceColumnsChange({
          nodeKey: layoutState.nodeKey,
          template,
          fromCount: currentColumns,
          toCount: nextColumns,
        })
        setConfirmReduceColumnsOpen(true)
        return
      }

      editor.dispatchCommand(UPDATE_LAYOUT_COMMAND, {
        nodeKey: layoutState.nodeKey,
        template,
      })
    },
    [editor, layoutState],
  )

  const confirmReduceColumnsChange = useCallback(() => {
    if (!pendingReduceColumnsChange) return
    editor.dispatchCommand(UPDATE_LAYOUT_COMMAND, {
      nodeKey: pendingReduceColumnsChange.nodeKey,
      template: pendingReduceColumnsChange.template,
    })
    setConfirmReduceColumnsOpen(false)
    setPendingReduceColumnsChange(null)
  }, [editor, pendingReduceColumnsChange])

  const onReduceColumnsDialogOpenChange = useCallback((open: boolean) => {
    setConfirmReduceColumnsOpen(open)
    if (!open) {
      setPendingReduceColumnsChange(null)
    }
  }, [])

  const updateBorder = useCallback(
    (payload: {
      borderAlwaysVisible?: boolean
      borderColor?: string | null
    }) => {
      if (!layoutState) return
      editor.dispatchCommand(UPDATE_LAYOUT_BORDER_COMMAND, {
        nodeKey: layoutState.nodeKey,
        ...payload,
      })
    },
    [editor, layoutState],
  )

  const requestDeleteCurrent = useCallback(() => {
    if (!layoutState) return
    setPendingDeleteNodeKey(layoutState.nodeKey)
    setConfirmDeleteOpen(true)
  }, [layoutState])

  const confirmDelete = useCallback(() => {
    const key = pendingDeleteNodeKey
    if (!key) return
    editor.update(() => {
      const node = $getNodeByKey(key)
      if ($isLayoutContainerNode(node)) {
        node.remove()
      }
    })
    setConfirmDeleteOpen(false)
    setPendingDeleteNodeKey(null)
  }, [editor, pendingDeleteNodeKey])

  if (!isEditable || !layoutEl || !pos || !layoutState) return null

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
                aria-label="Layout settings"
                className={cn(
                  "inline-flex h-6 items-center justify-center gap-1 rounded px-1.5",
                  "border border-gray-300 dark:border-gray-700",
                  "bg-white/90 dark:bg-gray-800/90 text-gray-700 dark:text-gray-200",
                  "shadow-sm hover:bg-gray-100 dark:hover:bg-gray-700",
                )}
              >
                <LayoutGrid className="h-3.5 w-3.5" />
                <ChevronDown className="h-3.5 w-3.5" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-64">
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>Columns layout</DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  {LAYOUT_TEMPLATES.map(({ value, label }) => (
                    <DropdownMenuItem
                      key={value}
                      onSelect={() => updateLayoutTemplate(value)}
                    >
                      {label}
                      {layoutState.template === value && (
                        <span className="ml-auto text-blue-600">✓</span>
                      )}
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuSubContent>
              </DropdownMenuSub>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onSelect={() =>
                  updateBorder({
                    borderAlwaysVisible: !layoutState.borderAlwaysVisible,
                  })
                }
              >
                Always show border
                {layoutState.borderAlwaysVisible && (
                  <span className="ml-auto text-blue-600">✓</span>
                )}
              </DropdownMenuItem>
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>Border color</DropdownMenuSubTrigger>
                <DropdownMenuSubContent className="p-3" onFocusOutside={(e) => { const t = (e as any).detail?.originalEvent?.target; if (t instanceof Element && t.closest("[contenteditable=\"true\"]")) e.preventDefault(); }}>
                  <ColorPicker
                    color={layoutState.borderColor ?? "#9ca3af"}
                    onChange={(next) =>
                      updateBorder({
                        borderColor:
                          typeof next === "string" && next.trim() !== ""
                            ? next
                            : null,
                      })
                    }
                  />
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onSelect={() => updateBorder({ borderColor: null })}
                  >
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
                Remove layout
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>,
        anchorElem,
      )}

      <DeleteConfirmDialog
        open={confirmDeleteOpen}
        onOpenChange={setConfirmDeleteOpen}
        title="Remove columns layout?"
        itemName="this columns layout"
        itemType="layout"
        onConfirm={confirmDelete}
        confirmText="Remove"
      />

      <BaseConfirmDialog
        open={confirmReduceColumnsOpen}
        onOpenChange={onReduceColumnsDialogOpenChange}
        title="Reduce number of columns?"
        description={
          pendingReduceColumnsChange
            ? `This change will reduce the layout from ${pendingReduceColumnsChange.fromCount} to ${pendingReduceColumnsChange.toCount} columns. Content in removed columns may be lost.`
            : "Reducing columns can remove content from discarded columns."
        }
        onConfirm={confirmReduceColumnsChange}
        confirmText="Apply change"
        cancelText="Cancel"
        confirmButtonClass="bg-orange-600 text-white hover:bg-orange-700 dark:bg-orange-700 dark:hover:bg-orange-800"
        icon={
          <div className="w-12 h-12 rounded-full bg-orange-100 dark:bg-orange-900/20 flex items-center justify-center">
            <AlertTriangle className="w-6 h-6 text-orange-600 dark:text-orange-400" />
          </div>
        }
      />
    </>
  )
}
