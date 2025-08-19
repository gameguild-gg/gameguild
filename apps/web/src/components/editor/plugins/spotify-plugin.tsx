"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_SPOTIFY_COMMAND } from "./floating-content-insert-plugin"
import { $createSpotifyNode } from "../nodes/spotify-node"
import type { SpotifyData } from "../nodes/spotify-node"

export function SpotifyPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_SPOTIFY_COMMAND,
      (payload: SpotifyData) => {
        editor.update(() => {
          // Add a flag to indicate that this is a newly inserted Spotify embed
          const spotifyNode = $createSpotifyNode({
            ...payload,
            isNew: true, // This flag will be used to automatically show the size control
          })
          $insertNodes([spotifyNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
