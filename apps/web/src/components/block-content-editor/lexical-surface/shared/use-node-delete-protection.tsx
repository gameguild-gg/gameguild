/**
 * useNodeDeleteProtection — reusable hook to protect any
 * decorator node Lexical against accidental deletion via Backspace/Delete.
 *
 * Detects the 4 scenarios in which the user could lose the node:
 *  1. `NodeSelection` containing the node.
 *  2. `RangeSelection` not collapsed that includes the node (or its parent).
 *  3. `RangeSelection` collapsed + Backspace immediately after the node.
 *  4. `RangeSelection` collapsed + Delete immediately before the node.
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
  $isElementNode,
  $isNodeSelection,
  $isRangeSelection,
  $isRootOrShadowRoot,
  $isTextNode,
  COMMAND_PRIORITY_HIGH,
  KEY_BACKSPACE_COMMAND,
  KEY_DELETE_COMMAND,
  type LexicalNode,
  type NodeKey,
} from "lexical"

function $isSameOrDescendant(node: LexicalNode | null, ancestor: LexicalNode): boolean {
  let current: LexicalNode | null = node
  while (current) {
    if (current.getKey() === ancestor.getKey()) {
      return true
    }
    current = current.getParent()
  }
  return false
}

function $getTopLevelInRootOrShadow(node: LexicalNode): LexicalNode | null {
  let current: LexicalNode | null = node
  while (current) {
    const parent: LexicalNode | null = current.getParent()
    if (!parent) {
      return current
    }
    if ($isRootOrShadowRoot(parent)) {
      return current
    }
    current = parent
  }
  return null
}

function $getDeepestFirstDescendant(node: LexicalNode): LexicalNode {
  let current = node
  while ($isElementNode(current) && current.getChildrenSize() > 0) {
    const child = current.getChildAtIndex(0)
    if (!child) break
    current = child
  }
  return current
}

function $getDeepestLastDescendant(node: LexicalNode): LexicalNode {
  let current = node
  while ($isElementNode(current) && current.getChildrenSize() > 0) {
    const child = current.getChildAtIndex(current.getChildrenSize() - 1)
    if (!child) break
    current = child
  }
  return current
}

function $getPreviousNodeFrom(node: LexicalNode): LexicalNode | null {
  let current: LexicalNode | null = node
  while (current) {
    const previous = current.getPreviousSibling()
    if (previous) {
      return $getDeepestLastDescendant(previous)
    }
    current = current.getParent()
  }
  return null
}

function $getNextNodeFrom(node: LexicalNode): LexicalNode | null {
  let current: LexicalNode | null = node
  while (current) {
    const next = current.getNextSibling()
    if (next) {
      return $getDeepestFirstDescendant(next)
    }
    current = current.getParent()
  }
  return null
}

function $getAdjacentNodeFromCollapsedSelection(isBackspace: boolean): LexicalNode | null {
  const selection = $getSelection()
  if (!$isRangeSelection(selection) || !selection.isCollapsed()) {
    return null
  }

  const anchor = selection.anchor
  const anchorNode = anchor.getNode()

  if (anchor.type === "text") {
    if (!$isTextNode(anchorNode)) {
      return null
    }
    const offset = anchor.offset
    const textLength = anchorNode.getTextContentSize()
    if (isBackspace) {
      if (offset !== 0) {
        return null
      }
      return $getPreviousNodeFrom(anchorNode)
    }
    if (offset !== textLength) {
      return null
    }
    return $getNextNodeFrom(anchorNode)
  }

  if ($isElementNode(anchorNode)) {
    const offset = anchor.offset
    const childrenSize = anchorNode.getChildrenSize()

    if (isBackspace) {
      if (offset > 0) {
        const childBefore = anchorNode.getChildAtIndex(offset - 1)
        return childBefore ? $getDeepestLastDescendant(childBefore) : null
      }
      return $getPreviousNodeFrom(anchorNode)
    }

    if (offset < childrenSize) {
      const childAfter = anchorNode.getChildAtIndex(offset)
      return childAfter ? $getDeepestFirstDescendant(childAfter) : null
    }
    return $getNextNodeFrom(anchorNode)
  }

  return isBackspace ? $getPreviousNodeFrom(anchorNode) : $getNextNodeFrom(anchorNode)
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
    // Range collapsed: checa o nó imediatamente adjacente ao caret.
    const targetNode = $getNodeByKey(nodeKey)
    if (!targetNode) return false
    const adjacentNode = $getAdjacentNodeFromCollapsedSelection(isBackspace)
    if (!adjacentNode) return false
    // IMPORTANT: only trigger when the node being deleted is exactly the
    // adjacent node (or an internal descendant). Do not trigger when only a
    // container/ancestor is adjacent, otherwise lines below may incorrectly
    // open the confirmation dialog.
    if (!$isSameOrDescendant(adjacentNode, targetNode)) {
      return false
    }

    // Backspace nuance for block decorator nodes inside paragraphs:
    // only trigger when the caret is in the same top-level container
    // as the protected node. This avoids false positives on the second
    // line below a node while preserving confirmation on the first line
    // that is structurally merged with the node container.
    if (isBackspace) {
      const anchorTop = $getTopLevelInRootOrShadow(selection.anchor.getNode())
      const targetTop = $getTopLevelInRootOrShadow(targetNode)
      if (!anchorTop || !targetTop) return false
      if (anchorTop.getKey() !== targetTop.getKey()) return false
    }

    return true
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
