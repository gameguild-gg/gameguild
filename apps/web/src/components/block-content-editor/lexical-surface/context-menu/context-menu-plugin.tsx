/**
 * ContextMenuPlugin — right-click contextual menu on nodes (cut, copy,
 * paste, remove link, delete). Adapted from
 * `lexical-playground/src/plugins/ContextMenuPlugin/index.tsx` with
 * Tailwind styling instead of playground CSS.
 */
"use client"

import * as React from "react"
import { useMemo } from "react"
import { $isLinkNode, TOGGLE_LINK_COMMAND } from "@lexical/link"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import {
  NodeContextMenuOption,
  NodeContextMenuPlugin as LexicalNodeContextMenuPlugin,
  NodeContextMenuSeparator,
} from "@lexical/react/LexicalNodeContextMenuPlugin"
import {
  $getSelection,
  $isDecoratorNode,
  $isNodeSelection,
  $isRangeSelection,
  COPY_COMMAND,
  CUT_COMMAND,
  PASTE_COMMAND,
  type LexicalNode,
} from "lexical"

export function ContextMenuPlugin(): React.JSX.Element {
  const [editor] = useLexicalComposerContext()

  const items = useMemo(
    () => [
      new NodeContextMenuOption("Remove Link", {
        $onSelect: () => {
          editor.dispatchCommand(TOGGLE_LINK_COMMAND, null)
        },
        $showOn: (node: LexicalNode) => $isLinkNode(node.getParent()),
        disabled: false,
      }),
      new NodeContextMenuSeparator({
        $showOn: (node: LexicalNode) => $isLinkNode(node.getParent()),
      }),
      new NodeContextMenuOption("Cut", {
        $onSelect: () => editor.dispatchCommand(CUT_COMMAND, null),
        disabled: false,
      }),
      new NodeContextMenuOption("Copy", {
        $onSelect: () => editor.dispatchCommand(COPY_COMMAND, null),
        disabled: false,
      }),
      new NodeContextMenuOption("Paste", {
        $onSelect: () => {
          void (async () => {
            try {
              const items = await navigator.clipboard.read()
              const item = items[0]
              if (!item) return
              const data = new DataTransfer()
              for (const type of item.types) {
                const blob = await item.getType(type)
                const text = await blob.text()
                data.setData(type, text)
              }
              const event = new ClipboardEvent("paste", { clipboardData: data })
              editor.dispatchCommand(PASTE_COMMAND, event)
            } catch (err) {
              console.error("Paste failed:", err)
            }
          })()
        },
        disabled: false,
      }),
      new NodeContextMenuOption("Paste as Plain Text", {
        $onSelect: () => {
          void (async () => {
            try {
              const text = await navigator.clipboard.readText()
              const data = new DataTransfer()
              data.setData("text/plain", text)
              const event = new ClipboardEvent("paste", { clipboardData: data })
              editor.dispatchCommand(PASTE_COMMAND, event)
            } catch (err) {
              console.error("Paste as plain text failed:", err)
            }
          })()
        },
        disabled: false,
      }),
      new NodeContextMenuSeparator(),
      new NodeContextMenuOption("Delete Node", {
        $onSelect: () => {
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            const currentNode = selection.anchor.getNode()
            const ancestor = currentNode.getParents().at(-2)
            ancestor?.remove()
          } else if ($isNodeSelection(selection)) {
            selection.getNodes().forEach((node) => {
              if ($isDecoratorNode(node)) node.remove()
            })
          }
        },
        disabled: false,
      }),
    ],
    [editor],
  )

  return (
    <LexicalNodeContextMenuPlugin
      className="z-50 min-w-[180px] rounded-md p-1 shadow-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900"
      itemClassName="flex items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-left text-gray-800 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-800 cursor-pointer"
      separatorClassName="my-1 h-px bg-gray-200 dark:bg-gray-700"
      items={items}
    />
  )
}
