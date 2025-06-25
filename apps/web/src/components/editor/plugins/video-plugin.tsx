"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_VIDEO_COMMAND } from "./floating-content-insert-plugin"
import { $createVideoNode } from "../nodes/video-node"
import type { VideoData } from "../nodes/video-node"

export function VideoPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_VIDEO_COMMAND,
      (payload: VideoData) => {
        editor.update(() => {
          // Adicione um flag para indicar que este é um vídeo recém-inserido
          const videoNode = $createVideoNode({
            ...payload,
            isNew: true, // Este flag será usado para mostrar o controle de tamanho automaticamente
          })
          $insertNodes([videoNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
