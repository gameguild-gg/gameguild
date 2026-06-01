/**
 * CollapsiblePlugin — registers `INSERT_COLLAPSIBLE_COMMAND`, escape
 * arrow-key handlers, Enter-to-jump-from-title-to-content, and structural
 * transforms keeping `Container > [Title, Content]` invariant.
 *
 * Ported from `lexical-playground/src/plugins/CollapsibleExtension`.
 */
"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $findMatchingParent, $insertNodeToNearestRoot, mergeRegister } from "@lexical/utils"
import {
  $createParagraphNode,
  $getNodeByKey,
  $getSelection,
  $isElementNode,
  $isRangeSelection,
  COMMAND_PRIORITY_LOW,
  createCommand,
  INSERT_PARAGRAPH_COMMAND,
  KEY_ARROW_DOWN_COMMAND,
  KEY_ARROW_LEFT_COMMAND,
  KEY_ARROW_RIGHT_COMMAND,
  KEY_ARROW_UP_COMMAND,
  type LexicalCommand,
  type LexicalNode,
  type NodeKey,
} from "lexical"
import {
  $createCollapsibleContainerNode,
  $isCollapsibleContainerNode,
  CollapsibleContainerNode,
} from "./collapsible-container-node"
import {
  $createCollapsibleContentNode,
  $isCollapsibleContentNode,
  CollapsibleContentNode,
} from "./collapsible-content-node"
import {
  $createCollapsibleTitleNode,
  $isCollapsibleTitleNode,
  CollapsibleTitleNode,
} from "./collapsible-title-node"

export const INSERT_COLLAPSIBLE_COMMAND: LexicalCommand<void> =
  createCommand<void>("INSERT_COLLAPSIBLE_COMMAND")

export const UPDATE_COLLAPSIBLE_STYLE_COMMAND: LexicalCommand<{
  nodeKey: NodeKey
  borderAlwaysVisible?: boolean
  borderColor?: string | null
}> = createCommand("UPDATE_COLLAPSIBLE_STYLE_COMMAND")

const $onEscapeUp = () => {
  const selection = $getSelection()
  if (
    $isRangeSelection(selection) &&
    selection.isCollapsed() &&
    selection.anchor.offset === 0
  ) {
    const container = $findMatchingParent(
      selection.anchor.getNode(),
      $isCollapsibleContainerNode,
    )
    if ($isCollapsibleContainerNode(container)) {
      const parent = container.getParent()
      if (
        parent !== null &&
        parent.getFirstChild() === container &&
        selection.anchor.key === container.getFirstDescendant()?.getKey()
      ) {
        container.insertBefore($createParagraphNode())
      }
    }
  }
  return false
}

const $onEscapeDown = () => {
  const selection = $getSelection()
  if ($isRangeSelection(selection) && selection.isCollapsed()) {
    const container = $findMatchingParent(
      selection.anchor.getNode(),
      $isCollapsibleContainerNode,
    )
    if ($isCollapsibleContainerNode(container)) {
      const parent = container.getParent()
      if (parent !== null && parent.getLastChild() === container) {
        const titleParagraph = container.getFirstDescendant()
        const contentParagraph = container.getLastDescendant()
        if (
          (contentParagraph !== null &&
            selection.anchor.key === contentParagraph.getKey() &&
            selection.anchor.offset === contentParagraph.getTextContentSize()) ||
          (titleParagraph !== null &&
            selection.anchor.key === titleParagraph.getKey() &&
            selection.anchor.offset === titleParagraph.getTextContentSize())
        ) {
          container.insertAfter($createParagraphNode())
        }
      }
    }
  }
  return false
}

export function CollapsiblePlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (
      !editor.hasNodes([
        CollapsibleContainerNode,
        CollapsibleTitleNode,
        CollapsibleContentNode,
      ])
    ) {
      throw new Error(
        "CollapsiblePlugin: Collapsible nodes not registered on editor",
      )
    }
    return mergeRegister(
      editor.registerCommand(KEY_ARROW_UP_COMMAND, $onEscapeUp, COMMAND_PRIORITY_LOW),
      editor.registerCommand(KEY_ARROW_LEFT_COMMAND, $onEscapeUp, COMMAND_PRIORITY_LOW),
      editor.registerCommand(KEY_ARROW_DOWN_COMMAND, $onEscapeDown, COMMAND_PRIORITY_LOW),
      editor.registerCommand(KEY_ARROW_RIGHT_COMMAND, $onEscapeDown, COMMAND_PRIORITY_LOW),
      editor.registerCommand(
        INSERT_PARAGRAPH_COMMAND,
        () => {
          const selection = $getSelection()
          if (!$isRangeSelection(selection)) return false
          const titleNode = $findMatchingParent(
            selection.anchor.getNode(),
            (node: LexicalNode) => $isCollapsibleTitleNode(node),
          )
          if ($isCollapsibleTitleNode(titleNode)) {
            const container = titleNode.getParent()
            if (container && $isCollapsibleContainerNode(container)) {
              if (!container.getOpen()) {
                container.toggleOpen()
              }
              titleNode.getNextSibling()?.selectEnd()
              return true
            }
          }
          return false
        },
        COMMAND_PRIORITY_LOW,
      ),
      editor.registerCommand(
        INSERT_COLLAPSIBLE_COMMAND,
        () => {
          editor.update(() => {
            const title = $createCollapsibleTitleNode()
            const paragraph = $createParagraphNode()
            $insertNodeToNearestRoot(
              $createCollapsibleContainerNode(true).append(
                title.append(paragraph),
                $createCollapsibleContentNode().append($createParagraphNode()),
              ),
            )
            paragraph.select()
          })
          return true
        },
        COMMAND_PRIORITY_LOW,
      ),
      editor.registerCommand(
        UPDATE_COLLAPSIBLE_STYLE_COMMAND,
        ({ nodeKey, borderAlwaysVisible, borderColor }) => {
          editor.update(() => {
            const target = $getNodeByKey(nodeKey)
            if (!$isCollapsibleContainerNode(target)) return
            if (typeof borderAlwaysVisible === "boolean") {
              target.setBorderAlwaysVisible(borderAlwaysVisible)
            }
            if (borderColor !== undefined) {
              target.setBorderColor(borderColor)
            }
          })
          return true
        },
        COMMAND_PRIORITY_LOW,
      ),
      editor.registerNodeTransform(CollapsibleContentNode, (node) => {
        const parent = node.getParent()
        if (!$isCollapsibleContainerNode(parent)) {
          for (const child of node.getChildren<LexicalNode>()) {
            node.insertBefore(child)
          }
          node.remove()
          return
        }
        if (node.isEmpty()) {
          node.append($createParagraphNode())
        }
      }),
      editor.registerNodeTransform(CollapsibleTitleNode, (node) => {
        const parent = node.getParent()
        if (!$isCollapsibleContainerNode(parent)) {
          node.replace($createParagraphNode().append(...node.getChildren<LexicalNode>()))
          return
        }
        if (node.isEmpty()) {
          node.remove()
          return
        }
        // ensure title content is wrapped in a paragraph-like element
        const firstChild = node.getFirstChild()
        if (!$isElementNode(firstChild)) {
          const paragraph = $createParagraphNode()
          for (const child of node.getChildren<LexicalNode>()) {
            paragraph.append(child)
          }
          node.append(paragraph)
        }
      }),
      editor.registerNodeTransform(CollapsibleContainerNode, (node) => {
        const children = node.getChildren<LexicalNode>()
        if (
          children.length !== 2 ||
          !$isCollapsibleTitleNode(children[0]) ||
          !$isCollapsibleContentNode(children[1])
        ) {
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
