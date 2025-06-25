"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_IMAGE_COMMAND } from "./floating-content-insert-plugin"
import { $createImageNode } from "../nodes/image-node"
import type { ImageData } from "../nodes/image-node"

export function ImagePlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_IMAGE_COMMAND,
      (payload: ImageData) => {
        editor.update(() => {
          const imageNode = $createImageNode(payload)
          $insertNodes([imageNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
