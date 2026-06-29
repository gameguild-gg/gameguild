"use client"

import { useEffect, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $wrapNodeInElement } from "@lexical/utils"
import {
  $createParagraphNode,
  $insertNodes,
  $isRootOrShadowRoot,
  COMMAND_PRIORITY_EDITOR,
  createCommand,
  type LexicalCommand,
} from "lexical"
import { $createVegaLiteLexicalNode, VegaLiteLexicalNode } from "./vega-lite-node"
import { VegaLiteEditor } from "../../extras/vega-lite/vega-lite-editor"
import type { VegaLiteData } from "../../nodes/vega-lite-node"

export const INSERT_VEGA_LITE_LEXICAL_COMMAND: LexicalCommand<void> = createCommand(
  "INSERT_VEGA_LITE_LEXICAL_COMMAND",
)

const DEFAULT_VEGA_SPEC = `{
  "$schema": "https://vega.github.io/schema/vega-lite/v5.json",
  "description": "A simple bar chart with embedded data.",
  "data": {
    "values": [
      {"category": "A", "value": 28},
      {"category": "B", "value": 55},
      {"category": "C", "value": 43},
      {"category": "D", "value": 91},
      {"category": "E", "value": 81},
      {"category": "F", "value": 53},
      {"category": "G", "value": 19},
      {"category": "H", "value": 87}
    ]
  },
  "mark": "bar",
  "encoding": {
    "x": {"field": "category", "type": "nominal"},
    "y": {"field": "value", "type": "quantitative"}
  }
}`

export function VegaLitePlugin() {
  const [editor] = useLexicalComposerContext()
  const [isModalOpen, setModalOpen] = useState(false)

  useEffect(() => {
    if (!editor.hasNodes([VegaLiteLexicalNode])) {
      throw new Error("VegaLitePlugin: VegaLiteLexicalNode not registered on editor")
    }
    return editor.registerCommand<void>(
      INSERT_VEGA_LITE_LEXICAL_COMMAND,
      () => {
        setModalOpen(true)
        return true
      },
      COMMAND_PRIORITY_EDITOR,
    )
  }, [editor])

  const handleSave = (data: VegaLiteData) => {
    editor.update(() => {
      const node = $createVegaLiteLexicalNode(data.spec)
      node.setTitle(data.title || "")
      node.setCaption(data.caption || "")
      node.setSize(data.size ?? 100)
      node.setTheme(data.theme || "default")
      node.setThemeMode(data.themeMode || "system")
      node.setLayout(data.layout || "rectangular")
      node.setData(data.data || {})

      $insertNodes([node])
      if ($isRootOrShadowRoot(node.getParentOrThrow())) {
        $wrapNodeInElement(node, $createParagraphNode).selectEnd()
      }
    })
    setModalOpen(false)
  }

  const handleCancel = () => {
    setModalOpen(false)
  }

  return isModalOpen ? (
    <VegaLiteEditor
      initialData={{
        spec: DEFAULT_VEGA_SPEC,
        title: "",
        caption: "",
        size: 100,
        theme: "default",
        themeMode: "system",
        layout: "rectangular",
        data: {},
      }}
      onSave={handleSave}
      onCancel={handleCancel}
    />
  ) : null
}
