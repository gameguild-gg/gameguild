"use client"

import type { SerializedQuizNode } from "../../nodes/quiz-node"
import { QuizWrapper, QuizDisplay } from "@/components/editor/extras/quiz"

export function PreviewQuiz({ node }: { node: SerializedQuizNode }) {
  if (!node?.entry) {
    console.error("Invalid quiz node structure:", node)
    return null
  }

  return (
    <QuizWrapper backgroundColor={node.entry.settings.backgroundColor}>
      <QuizDisplay entry={node.entry} />
    </QuizWrapper>
  )
}

