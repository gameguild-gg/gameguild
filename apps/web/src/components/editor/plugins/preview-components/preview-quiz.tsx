"use client"

import type { SerializedQuizNode } from "../../nodes/quiz-node"
import { useQuizLogic } from "@/hooks/editor/use-quiz-logic"
import { QuizWrapper } from "../../ui/quiz/quiz-wrapper"
import { QuizDisplay } from "../../ui/quiz/quiz-display"
import { Button } from "@/components/ui/button"
import { RotateCcw } from 'lucide-react'

export function PreviewQuiz({ node }: { node: SerializedQuizNode }) {
  const quizLogic = useQuizLogic({
    answers: node.data?.answers,
    allowRetry: node.data?.allowRetry,
    correctFeedback: node.data?.correctFeedback,
    incorrectFeedback: node.data?.incorrectFeedback,
  })

  if (!node?.data) {
    console.error("Invalid quiz node structure:", node)
    return null
  }

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

  const { selectedAnswers, showFeedback, isCorrect, checkAnswers, toggleAnswer, resetQuiz } = quizLogic

  return (
    <QuizWrapper backgroundColor={backgroundColor}>
      <QuizDisplay
        question={question}
        questionType={questionType || "multiple-choice"}
        answers={answers}
        selectedAnswers={selectedAnswers}
        setSelectedAnswers={() => {}} // Dummy function
        showFeedback={showFeedback}
        isCorrect={isCorrect}
        correctFeedback={correctFeedback ?? ""}
        incorrectFeedback={incorrectFeedback ?? ""}
        allowRetry={allowRetry}
        checkAnswers={checkAnswers}
        toggleAnswer={toggleAnswer}
        blanks={blanks}
        fillBlankMode={fillBlankMode}
        fillBlankAlternatives={fillBlankAlternatives}
        ratingScale={ratingScale}
        correctRating={correctRating}
      />
      {showFeedback && !allowRetry && (
        <div className="flex justify-end mt-4 pt-3 border-t border-border/50">
          <Button
            variant="outline"
            size="sm"
            onClick={resetQuiz}
            className="flex items-center gap-2 text-muted-foreground hover:text-foreground transition-colors bg-transparent"
          >
            <RotateCcw className="h-4 w-4" />
            Try Again
          </Button>
        </div>
      )}
    </QuizWrapper>
  )
}
