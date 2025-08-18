"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_AUDIO_COMMAND } from "./floating-content-insert-plugin"
import { $createAudioNode } from "../nodes/audio-node"
import type { AudioData } from "../nodes/audio-node"

export function AudioPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_AUDIO_COMMAND,
      (payload: AudioData) => {
        editor.update(() => {
          // Adicione um flag para indicar que este é um áudio recém-inserido
          const audioNode = $createAudioNode({
            ...payload,
            isNew: true, // Este flag será usado para mostrar o controle de tamanho automaticamente
          })
          $insertNodes([audioNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
