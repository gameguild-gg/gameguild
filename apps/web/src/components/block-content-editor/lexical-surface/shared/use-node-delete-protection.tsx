/**
 * useNodeDeleteProtection — hook reutilizável para proteger qualquer
 * decorator node Lexical contra exclusão acidental por Backspace/Delete.
 *
 * Detecta os 4 cenários em que o usuário poderia perder o nó:
 *  1. `NodeSelection` contendo o nó.
 *  2. `RangeSelection` não-colapsada que inclui o nó (ou seu pai).
 *  3. `RangeSelection` colapsada + Backspace no início do top-level
 *     imediatamente após o nó.
 *  4. `RangeSelection` colapsada + Delete no fim do top-level
 *     imediatamente antes do nó.
 *
 * Quando qualquer um desses ocorre, intercepta a tecla, evita a deleção
 * e chama `onRequestDelete` (use isso para abrir um diálogo de
 * confirmação).
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
    const wouldAffectThisNode = (isBackspace: boolean): boolean => {
      const selection = $getSelection()
      // 1) NodeSelection contendo este nó.
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
    const handler = (isBackspace: boolean) => (event: KeyboardEvent | null) => {
      let shouldBlock = false
      editor.getEditorState().read(() => {
        shouldBlock = wouldAffectThisNode(isBackspace)
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
