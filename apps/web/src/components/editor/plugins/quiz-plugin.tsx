"use client"

import { useEffect } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $insertNodes } from "lexical"
import { INSERT_QUIZ_COMMAND } from "./floating-content-insert-plugin"
import { $createQuizNode } from "../nodes/quiz-node"

export function QuizPlugin() {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!editor) return

    return editor.registerCommand(
      INSERT_QUIZ_COMMAND,
      () => {
        editor.update(() => {
          const quizNode = $createQuizNode()
          $insertNodes([quizNode])
        })
        return true
      },
      1,
    )
  }, [editor])

  return null
}
