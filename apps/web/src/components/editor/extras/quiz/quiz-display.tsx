"use client"

import { QuizFeedback } from "./quiz-feedback"
import { QuizRenderer } from "../../nodes/quiz/renderers/quiz-renderer"
import type { QuizData } from "../../nodes/quiz-node"

interface QuizDisplayProps {
  data: QuizData
  selectedAnswers: string[]
  setSelectedAnswers: (answers: string[]) => void
  showFeedback: boolean
  isCorrect: boolean
  checkAnswers: () => void
  resetQuiz?: () => void
}

export function QuizDisplay({
  data,
  selectedAnswers,
  setSelectedAnswers,
  showFeedback,
  isCorrect,
  checkAnswers,
  resetQuiz,
}: QuizDisplayProps) {

  return (
    <div className="space-y-4">
      {/* Question text - hide for fill-blank as it's rendered inline */}
      {data.questionType !== "fill-blank" && <div className="text-lg font-medium">{data.question}</div>}

      {/* Render appropriate question type */}
      <QuizRenderer
        data={data}
        selectedAnswers={selectedAnswers}
        onAnswerChange={setSelectedAnswers}
        disabled={false}
        showFeedback={showFeedback}
      />

      {/* Submit button */}
      {!showFeedback && (
        <button
          onClick={checkAnswers}
          className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-3 px-6 rounded-lg transition-colors duration-200 shadow-sm hover:shadow-md"
        >
          Submit Answer
        </button>
      )}

      {/* Feedback */}
      {showFeedback && (
        <QuizFeedback
          isCorrect={isCorrect}
          correctFeedback={data.correctFeedback || ""}
          incorrectFeedback={data.incorrectFeedback || ""}
          allowRetry={data.allowRetry}
          onRetry={resetQuiz}
          showRetryButton={data.allowRetry && !!resetQuiz}
        />
      )}
    </div>
  )
}
