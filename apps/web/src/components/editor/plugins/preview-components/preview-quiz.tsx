"use client"

import type { SerializedQuizNode } from "../../nodes/quiz-node"
import { useQuizAnswers } from "@/components/editor/nodes/quiz/hooks/use-quiz-answers"
import { QuizWrapper } from "@/components/editor/extras/quiz/quiz-wrapper"
import { QuizDisplay } from "@/components/editor/extras/quiz/quiz-display"

export function PreviewQuiz({ node }: { node: SerializedQuizNode }) {
  const { selectedAnswers, setSelectedAnswers, showFeedback, isCorrect, checkAnswers, resetQuiz } = useQuizAnswers({
    data: node.data,
  })

  if (!node?.data) {
    console.error("Invalid quiz node structure:", node)
    return null
  }

  return (
    <QuizWrapper backgroundColor={node.data.backgroundColor}>
      <QuizDisplay
        data={node.data}
        selectedAnswers={selectedAnswers}
        setSelectedAnswers={setSelectedAnswers}
        showFeedback={showFeedback}
        isCorrect={isCorrect}
        checkAnswers={checkAnswers}
        resetQuiz={resetQuiz}
      />
    </QuizWrapper>
  )
}

