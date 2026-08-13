/**
 * LayoutPlugin — registers the `INSERT_LAYOUT_COMMAND` and
 * `UPDATE_LAYOUT_COMMAND` plus the structural transforms that keep
 * `LayoutContainerNode > LayoutItemNode*` invariant.
 *
 * Ported from `lexical-playground/src/plugins/LayoutExtension`.
 */
"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $findMatchingParent, $insertNodeToNearestRoot, mergeRegister } from "@lexical/utils"
import {
  $createParagraphNode,
  $getNodeByKey,
  $getSelection,
  $isRangeSelection,
  COMMAND_PRIORITY_EDITOR,
  COMMAND_PRIORITY_LOW,
  createCommand,
  KEY_ARROW_DOWN_COMMAND,
  KEY_ARROW_LEFT_COMMAND,
  KEY_ARROW_RIGHT_COMMAND,
  KEY_ARROW_UP_COMMAND,
  type ElementNode,
  type LexicalCommand,
  type LexicalNode,
  type NodeKey,
} from "lexical"
import {
  $createLayoutContainerNode,
  $isLayoutContainerNode,
  LayoutContainerNode,
} from "./layout-container-node"
import {
  $createLayoutItemNode,
  $isLayoutItemNode,
  LayoutItemNode,
} from "./layout-item-node"

export const INSERT_LAYOUT_COMMAND: LexicalCommand<string> =
  createCommand<string>("INSERT_LAYOUT_COMMAND")

export const LAYOUT_TEMPLATES = [
  { label: "2 columns (equal width)", value: "1fr 1fr" },
  { label: "2 columns (25% - 75%)", value: "1fr 3fr" },
  { label: "3 columns (equal width)", value: "1fr 1fr 1fr" },
  { label: "3 columns (25% - 50% - 25%)", value: "1fr 2fr 1fr" },
  { label: "4 columns (equal width)", value: "1fr 1fr 1fr 1fr" },
] as const

export const UPDATE_LAYOUT_COMMAND: LexicalCommand<{
  template: string
  nodeKey: NodeKey
}> = createCommand("UPDATE_LAYOUT_COMMAND")

export const UPDATE_LAYOUT_BORDER_COMMAND: LexicalCommand<{
  nodeKey: NodeKey
  borderAlwaysVisible?: boolean
  borderColor?: string | null
}> = createCommand("UPDATE_LAYOUT_BORDER_COMMAND")

function getItemsCountFromTemplate(template: string): number {
  return template.trim().split(/\s+/).length
}

const $onEscape = (before: boolean) => {
  const selection = $getSelection()
  if (
    $isRangeSelection(selection) &&
    selection.isCollapsed() &&
    selection.anchor.offset === 0
  ) {
    const container = $findMatchingParent(
      selection.anchor.getNode(),
      $isLayoutContainerNode,
    )
    if ($isLayoutContainerNode(container)) {
      const parent = container.getParent<ElementNode>()
      const child =
        parent &&
        (before ? parent.getFirstChild<LexicalNode>() : parent.getLastChild<LexicalNode>())
      const descendant = before
        ? container.getFirstDescendant<LexicalNode>()?.getKey()
        : container.getLastDescendant<LexicalNode>()?.getKey()

      if (
        parent !== null &&
        child === container &&
        selection.anchor.key === descendant
      ) {
        if (before) {
          container.insertBefore($createParagraphNode())
        } else {
          container.insertAfter($createParagraphNode())
        }
      }
    }
  }
  return false
}

const $fillLayoutItemIfEmpty = (node: LayoutItemNode) => {
  if (node.isEmpty()) {
    node.append($createParagraphNode())
  }
}

const $removeIsolatedLayoutItem = (node: LayoutItemNode): boolean => {
  const parent = node.getParent<ElementNode>()
  if (!$isLayoutContainerNode(parent)) {
    const children = node.getChildren<LexicalNode>()
    for (const child of children) {
      node.insertBefore(child)
    }
    node.remove()
    return true
  }
  return false
}

export function LayoutPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([LayoutContainerNode, LayoutItemNode])) {
      throw new Error(
        "LayoutPlugin: LayoutContainerNode and LayoutItemNode not registered on editor",
      )
    }
    return mergeRegister(
      editor.registerCommand(KEY_ARROW_DOWN_COMMAND, () => $onEscape(false), COMMAND_PRIORITY_LOW),
      editor.registerCommand(KEY_ARROW_RIGHT_COMMAND, () => $onEscape(false), COMMAND_PRIORITY_LOW),
      editor.registerCommand(KEY_ARROW_UP_COMMAND, () => $onEscape(true), COMMAND_PRIORITY_LOW),
      editor.registerCommand(KEY_ARROW_LEFT_COMMAND, () => $onEscape(true), COMMAND_PRIORITY_LOW),
      editor.registerCommand(
        INSERT_LAYOUT_COMMAND,
        (template) => {
          editor.update(() => {
            const container = $createLayoutContainerNode(template)
            const itemsCount = getItemsCountFromTemplate(template)
            for (let i = 0; i < itemsCount; i++) {
              container.append($createLayoutItemNode().append($createParagraphNode()))
            }
            $insertNodeToNearestRoot(container)
            container.selectStart()
          })
          return true
        },
        COMMAND_PRIORITY_EDITOR,
      ),
      editor.registerCommand(
        UPDATE_LAYOUT_COMMAND,
        ({ template, nodeKey }) => {
          editor.update(() => {
            const container = $getNodeByKey<LexicalNode>(nodeKey)
            if (!$isLayoutContainerNode(container)) return
            const itemsCount = getItemsCountFromTemplate(template)
            const prevItemsCount = getItemsCountFromTemplate(container.getTemplateColumns())
            if (itemsCount > prevItemsCount) {
              for (let i = prevItemsCount; i < itemsCount; i++) {
                container.append($createLayoutItemNode().append($createParagraphNode()))
              }
            } else if (itemsCount < prevItemsCount) {
              for (let i = prevItemsCount - 1; i >= itemsCount; i--) {
                const layoutItem = container.getChildAtIndex<LexicalNode>(i)
                if ($isLayoutItemNode(layoutItem)) {
                  layoutItem.remove()
                }
              }
            }
            container.setTemplateColumns(template)
          })
          return true
        },
        COMMAND_PRIORITY_EDITOR,
      ),
      editor.registerCommand(
        UPDATE_LAYOUT_BORDER_COMMAND,
        ({ nodeKey, borderAlwaysVisible, borderColor }) => {
          editor.update(() => {
            const container = $getNodeByKey<LexicalNode>(nodeKey)
            if (!$isLayoutContainerNode(container)) return
            if (typeof borderAlwaysVisible === "boolean") {
              container.setBorderAlwaysVisible(borderAlwaysVisible)
            }
            if (borderColor !== undefined) {
              container.setBorderColor(borderColor)
            }
          })
          return true
        },
        COMMAND_PRIORITY_EDITOR,
      ),
      editor.registerNodeTransform(LayoutItemNode, (node) => {
        const removed = $removeIsolatedLayoutItem(node)
        if (!removed) {
          $fillLayoutItemIfEmpty(node)
        }
      }),
      editor.registerNodeTransform(LayoutContainerNode, (node) => {
        const children = node.getChildren<LexicalNode>()
        if (!children.every($isLayoutItemNode)) {
          for (const child of children) {
            node.insertBefore(child)
          }
          node.remove()
        }
      }),
    )
  }, [editor])

  return null
}
