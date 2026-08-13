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
  COMMAND_PRIORITY_CRITICAL,
  type LexicalNode,
} from "lexical"
import { $getClipboardDataFromSelection, $insertDataTransferForRichText } from "@lexical/clipboard"

let clipboardCache: { text: string; html?: string } | null = null

export function ContextMenuPlugin(): React.JSX.Element {
  const [editor] = useLexicalComposerContext()

  React.useEffect(() => {
    return editor.registerCommand(
      COPY_COMMAND,
      () => {
        editor.read(() => {
          const data = $getClipboardDataFromSelection()
          if (data) {
            clipboardCache = {
              text: data["text/plain"] || "",
              html: data["text/html"] || undefined,
            }
          }
        })
        return false
      },
      COMMAND_PRIORITY_CRITICAL
    )
  }, [editor])

  React.useEffect(() => {
    return editor.registerCommand(
      CUT_COMMAND,
      () => {
        editor.read(() => {
          const data = $getClipboardDataFromSelection()
          if (data) {
            clipboardCache = {
              text: data["text/plain"] || "",
              html: data["text/html"] || undefined,
            }
          }
        })
        return false
      },
      COMMAND_PRIORITY_CRITICAL
    )
  }, [editor])

  React.useEffect(() => {
    return editor.registerRootListener((rootElement, prevRootElement) => {
      const onContextMenu = (e: MouseEvent) => {
        if (rootElement && e.target === rootElement && rootElement.lastElementChild) {
          e.preventDefault()
          e.stopPropagation()
          const newEvent = new MouseEvent("contextmenu", {
            bubbles: true,
            cancelable: true,
            clientX: e.clientX,
            clientY: e.clientY,
          })
          rootElement.lastElementChild.dispatchEvent(newEvent)
        }
      }

      if (prevRootElement !== null) {
        prevRootElement.removeEventListener("contextmenu", onContextMenu)
      }
      if (rootElement !== null) {
        rootElement.addEventListener("contextmenu", onContextMenu, true)
      }
    })
  }, [editor])

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
        $onSelect: () => {
          const data = $getClipboardDataFromSelection()
          if (!data) return
          void (async () => {
            try {
              if (navigator.clipboard?.write) {
                const items: Record<string, Blob> = {
                  "text/plain": new Blob([data["text/plain"] || ""], { type: "text/plain" }),
                }
                if (data["text/html"]) {
                  items["text/html"] = new Blob([data["text/html"]], { type: "text/html" })
                }
                await navigator.clipboard.write([new ClipboardItem(items)])
              } else {
                await navigator.clipboard.writeText(data["text/plain"] || "")
              }
            } catch (err) {
              await navigator.clipboard.writeText(data["text/plain"] || "").catch(() => { })
            }
          })()
          const selection = $getSelection()
          if ($isRangeSelection(selection)) {
            selection.removeText()
          } else if ($isNodeSelection(selection)) {
            selection.getNodes().forEach(n => n.remove())
          }
        },
        disabled: false,
      }),
      new NodeContextMenuOption("Copy", {
        $onSelect: () => {
          const data = $getClipboardDataFromSelection()
          if (!data) return
          clipboardCache = {
            text: data["text/plain"] || "",
            html: data["text/html"] || undefined,
          }
          void (async () => {
            try {
              if (navigator.clipboard?.write) {
                const items: Record<string, Blob> = {
                  "text/plain": new Blob([data["text/plain"] || ""], { type: "text/plain" }),
                }
                if (data["text/html"]) {
                  items["text/html"] = new Blob([data["text/html"]], { type: "text/html" })
                }
                await navigator.clipboard.write([new ClipboardItem(items)])
              } else {
                await navigator.clipboard.writeText(data["text/plain"] || "")
              }
            } catch (err) {
              await navigator.clipboard.writeText(data["text/plain"] || "").catch(() => { })
            }
          })()
        },
        disabled: false,
      }),
      new NodeContextMenuOption("Paste", {
        $onSelect: () => {
          editor.focus()

          const pasteFromCacheAsync = () => {
            setTimeout(() => {
              const cache = clipboardCache
              if (cache) {
                editor.update(() => {
                  const selection = $getSelection()
                  if (selection) {
                    const data = new DataTransfer()
                    data.setData("text/plain", cache.text)
                    if (cache.html) {
                      data.setData("text/html", cache.html)
                    }
                    $insertDataTransferForRichText(data, selection, editor)
                  }
                })
                editor.focus()
              } else {
                alert("Para colar do mundo externo neste navegador, utilize o atalho de teclado Ctrl+V.")
              }
            }, 0)
          }

          if (navigator.clipboard && typeof navigator.clipboard.read === "function") {
            navigator.clipboard.read().then(async (items) => {
              const data = new DataTransfer()
              const item = items[0]
              if (!item) return
              for (const type of item.types) {
                const dataString = await (await item.getType(type)).text()
                data.setData(type, dataString)
              }
              editor.update(() => {
                const selection = $getSelection()
                if (selection) {
                  $insertDataTransferForRichText(data, selection, editor)
                }
              })
              editor.focus()
            }).catch(() => {
              pasteFromCacheAsync()
            })
          } else if (navigator.clipboard && typeof navigator.clipboard.readText === "function") {
            navigator.clipboard.readText().then((text) => {
              editor.update(() => {
                const selection = $getSelection()
                if (selection) {
                  const data = new DataTransfer()
                  data.setData("text/plain", text)
                  $insertDataTransferForRichText(data, selection, editor)
                }
              })
              editor.focus()
            }).catch(() => {
              pasteFromCacheAsync()
            })
          } else {
            pasteFromCacheAsync()
          }
        },
        disabled: false,
      }),
      new NodeContextMenuOption("Paste as Plain Text", {
        $onSelect: () => {
          editor.focus()

          const pasteFromCacheAsync = () => {
            setTimeout(() => {
              const cache = clipboardCache
              if (cache) {
                editor.update(() => {
                  const selection = $getSelection()
                  if (selection) {
                    const data = new DataTransfer()
                    data.setData("text/plain", cache.text)
                    $insertDataTransferForRichText(data, selection, editor)
                  }
                })
                editor.focus()
              } else {
                alert("Para colar do mundo externo neste navegador, utilize o atalho de teclado Ctrl+V.")
              }
            }, 0)
          }

          if (navigator.clipboard && typeof navigator.clipboard.readText === "function") {
            navigator.clipboard.readText().then((text) => {
              editor.update(() => {
                const selection = $getSelection()
                if (selection) {
                  const data = new DataTransfer()
                  data.setData("text/plain", text)
                  $insertDataTransferForRichText(data, selection, editor)
                }
              })
              editor.focus()
            }).catch(() => {
              pasteFromCacheAsync()
            })
          } else {
            pasteFromCacheAsync()
          }
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
      className="z-50 min-w-[180px] rounded-md p-1 shadow-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 empty:hidden"
      itemClassName="flex items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-left text-gray-800 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-800 cursor-pointer"
      separatorClassName="my-1 h-px bg-gray-200 dark:bg-gray-700"
      items={items}
    />
  )
}
