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
          const videoNode = $createVideoNode({
            ...payload,
            isNew: true, // Flag to show editor automatically on new videos
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
