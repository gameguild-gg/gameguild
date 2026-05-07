"use client"

import type { SerializedQuizNode } from "../../nodes/quiz-node"
import { QuizDisplay } from "@/components/block-content-editor/extras/quiz/quiz-display"
import { QuizWrapper } from "@/components/block-content-editor/extras/quiz/quiz-wrapper"

export function PreviewQuiz({ node }: { node: SerializedQuizNode }) {
  if (!node?.entry) {
    console.error("Invalid quiz node structure:", node)
    return null
  }

  return (
    <QuizWrapper>
      <QuizDisplay entry={node.entry} />
    </QuizWrapper>
  )
}

