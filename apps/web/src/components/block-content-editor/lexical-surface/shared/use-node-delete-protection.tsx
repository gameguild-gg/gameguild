/**
 * useNodeDeleteProtection — reusable hook to protect any
 * decorator node Lexical against accidental deletion via Backspace/Delete.
 *
 * Detects the 4 scenarios in which the user could lose the node:
 *  1. `NodeSelection` containing the node.
 *  2. `RangeSelection` not collapsed that includes the node (or its parent).
 *  3. `RangeSelection` collapsed + Backspace at the start of the top-level
 *     immediately after the node.
 *  4. `RangeSelection` collapsed + Delete at the end of the top-level
 *     immediately before the node.
 *
 * When any of these occur, it intercepts the key, prevents deletion,
 * and calls `onRequestDelete` (use this to open a confirmation dialog).
 */
"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { mergeRegister } from "@lexical/utils"
import {
  $getNodeByKey,
  $getSelection,
  $isNodeSelection,
  $isRangeSelection,
  COMMAND_PRIORITY_HIGH,
  KEY_BACKSPACE_COMMAND,
  KEY_DELETE_COMMAND,
  type LexicalNode,
  type NodeKey,
} from "lexical"

function $getTopLevel(node: LexicalNode): LexicalNode | null {
  let n: LexicalNode | null = node
  while (n && n.getParent() && n.getParent()!.getKey() !== "root") {
    n = n.getParent()
  }
  return n
}

/**
 * Reusable predicate: given a `nodeKey` and the type of key pressed
 * (`isBackspace`), returns `true` if this deletion would affect the node.
 *
 * Should be called within `editor.read()` / `editor.update()`.
 */
export function $wouldDeletionAffectNodeKey(nodeKey: NodeKey, isBackspace: boolean): boolean {
  const selection = $getSelection()
  // 1) NodeSelection containing this node.
  if ($isNodeSelection(selection)) {
    return selection.getNodes().some((n) => n.getKey() === nodeKey)
  }
  // 2/3/4) RangeSelection.
  if ($isRangeSelection(selection)) {
    if (!selection.isCollapsed()) {
      const nodes = selection.getNodes()
      if (nodes.some((n) => n.getKey() === nodeKey)) return true
      for (const n of nodes) {
        const children = (
          "getChildren" in n && typeof (n as { getChildren?: unknown }).getChildren === "function"
            ? (n as unknown as { getChildren: () => Array<{ getKey: () => string }> }).getChildren()
            : []
        )
        if (children.some((c) => c.getKey() === nodeKey)) return true
      }
      return false
    }
    // Range collapsed: checa adjacência top-level.
    const targetNode = $getNodeByKey(nodeKey)
    if (!targetNode) return false
    const anchor = selection.anchor.getNode()
    const top = $getTopLevel(anchor)
    const targetTop = $getTopLevel(targetNode)
    if (!top || !targetTop) return false
    const offset = selection.anchor.offset
    if (isBackspace) {
      if (offset !== 0) return false
      return top.getPreviousSibling()?.getKey() === targetTop.getKey()
    } else {
      const textLen =
        "getTextContentSize" in anchor
          ? (anchor as unknown as { getTextContentSize: () => number }).getTextContentSize()
          : 0
      if (offset !== textLen) return false
      return top.getNextSibling()?.getKey() === targetTop.getKey()
    }
  }
  return false
}

export function useNodeDeleteProtection({
  nodeKey,
  enabled = true,
  onRequestDelete,
}: {
  nodeKey: NodeKey
  enabled?: boolean
  onRequestDelete: () => void
}) {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!enabled) return
    const handler = (isBackspace: boolean) => (event: KeyboardEvent | null) => {
      let shouldBlock = false
      editor.read(() => {
        shouldBlock = $wouldDeletionAffectNodeKey(nodeKey, isBackspace)
      })
      if (!shouldBlock) return false
      event?.preventDefault()
      onRequestDelete()
      return true
    }
    return mergeRegister(
      editor.registerCommand(KEY_BACKSPACE_COMMAND, handler(true), COMMAND_PRIORITY_HIGH),
      editor.registerCommand(KEY_DELETE_COMMAND, handler(false), COMMAND_PRIORITY_HIGH),
    )
  }, [editor, nodeKey, enabled, onRequestDelete])
}

/**
 * Variant to protect a dynamic set of nodes (e.g., all `CodeNode` tracked via mutation listener). `getNodeKeys` is called
 * on each keypress to get the current list; `onRequestDelete` receives the
 * key of the node that would be affected.
 */
export function useNodesDeleteProtection({
  getNodeKeys,
  enabled = true,
  onRequestDelete,
}: {
  getNodeKeys: () => Iterable<NodeKey>
  enabled?: boolean
  onRequestDelete: (nodeKey: NodeKey) => void
}) {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!enabled) return
    const handler = (isBackspace: boolean) => (event: KeyboardEvent | null) => {
      let affected: NodeKey | null = null
      editor.read(() => {
        for (const key of getNodeKeys()) {
          if ($wouldDeletionAffectNodeKey(key, isBackspace)) {
            affected = key
            break
          }
        }
      })
      if (affected === null) return false
      event?.preventDefault()
      onRequestDelete(affected)
      return true
    }
    return mergeRegister(
      editor.registerCommand(KEY_BACKSPACE_COMMAND, handler(true), COMMAND_PRIORITY_HIGH),
      editor.registerCommand(KEY_DELETE_COMMAND, handler(false), COMMAND_PRIORITY_HIGH),
    )
  }, [editor, enabled, getNodeKeys, onRequestDelete])
}
