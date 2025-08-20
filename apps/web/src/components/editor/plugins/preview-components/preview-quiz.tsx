"use client"

import type { SerializedQuizNode } from "../../nodes/quiz-node"
import { useQuizLogic } from "@/hooks/editor/use-quiz-logic"
import { QuizWrapper } from "@/components/editor/extras/quiz/quiz-wrapper"
import { QuizDisplay } from "@/components/editor/extras/quiz/quiz-display"
import { useEffect } from "react"

export function PreviewQuiz({ node }: { node: SerializedQuizNode }) {
  const quizLogic = useQuizLogic({
    answers: node.data?.answers || [],
    allowRetry: node.data?.allowRetry !== undefined ? node.data.allowRetry : true,
    correctFeedback: node.data?.correctFeedback || "",
    incorrectFeedback: node.data?.incorrectFeedback || "",
  })

  const {
    question,
    questionType,
    answers,
    correctFeedback,
    incorrectFeedback,
    allowRetry,
    backgroundColor,
    blanks,
    fillBlankMode,
    fillBlankAlternatives,
    ratingScale,
    correctRating,
  } = node.data

  const { selectedAnswers, setSelectedAnswers, showFeedback, isCorrect, checkAnswers, toggleAnswer, resetQuiz } =
    quizLogic

  useEffect(() => {
    resetQuiz()
  }, [node.data, resetQuiz])

  if (!node?.data) {
    console.error("Invalid quiz node structure:", node)
    return null
  }

  return (
    <QuizWrapper backgroundColor={backgroundColor}>
      <QuizDisplay
        question={question}
        questionType={questionType || "multiple-choice"}
        answers={answers || []}
        selectedAnswers={selectedAnswers}
        setSelectedAnswers={setSelectedAnswers}
        showFeedback={showFeedback}
        isCorrect={isCorrect}
        correctFeedback={correctFeedback || ""}
        incorrectFeedback={incorrectFeedback || ""}
        allowRetry={allowRetry !== undefined ? allowRetry : true}
        checkAnswers={checkAnswers}
        toggleAnswer={toggleAnswer}
        resetQuiz={resetQuiz}
        blanks={blanks}
        fillBlankMode={fillBlankMode}
        fillBlankAlternatives={fillBlankAlternatives}
        ratingScale={ratingScale}
        correctRating={correctRating}
      />
    </QuizWrapper>
  )
}
