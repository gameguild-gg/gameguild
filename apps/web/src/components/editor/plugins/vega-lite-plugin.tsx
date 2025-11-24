"use client"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodeToNearestRoot } from "@lexical/utils"
import { COMMAND_PRIORITY_EDITOR } from "lexical"
import { useEffect } from "react"
import type { JSX } from "react/jsx-runtime"

import { $createVegaLiteNode, VegaLiteNode, type VegaLiteData } from "../nodes/vega-lite-node"
import { INSERT_VEGA_LITE_COMMAND } from "./floating-content-insert-plugin"

export function VegaLitePlugin(): JSX.Element | null {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor.hasNodes([VegaLiteNode])) {
      throw new Error("VegaLitePlugin: VegaLiteNode not registered on editor")
    }

    return editor.registerCommand<VegaLiteData>(
      INSERT_VEGA_LITE_COMMAND,
      (payload) => {
        const vegaLiteNode = $createVegaLiteNode(payload)
        $insertNodeToNearestRoot(vegaLiteNode)
        return true
      },
      COMMAND_PRIORITY_EDITOR,
    )
  }, [editor])

  return null
}