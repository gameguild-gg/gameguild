"use client"

import type { SerializedQuizNode } from "../../nodes/quiz-node"
import { QuizDisplay } from "@/components/block-content-editor/extras/quiz/quiz-display"
import type { QuizSubmissionMode } from "@/components/block-content-editor/extras/quiz/hooks/use-quiz-answers"
import { QuizWrapper } from "@/components/block-content-editor/extras/quiz/quiz-wrapper"

export function PreviewQuiz({
  node,
  submissionMode,
}: {
  node: SerializedQuizNode
  submissionMode?: QuizSubmissionMode
}) {
  if (!node?.entry) {
    console.error("Invalid quiz node structure:", node)
    return null
  }

  return (
    <QuizWrapper>
      <QuizDisplay entry={node.entry} submissionMode={submissionMode} />
    </QuizWrapper>
  )
}
