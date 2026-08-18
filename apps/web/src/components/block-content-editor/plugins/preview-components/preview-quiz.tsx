"use client"

import { useEffect, useState } from "react"
import {
  createEmptyQuizAnswer,
  toQuizLearnerEntry,
  type QuizAnswer,
} from "@game-guild/quiz"
import {
  QuizPlayer,
  QuizPracticePlayer,
  QuizWrapper,
  type QuizSubmissionMode,
  type QuizSubmissionResult,
} from "@game-guild/quiz-surface/player"
import type { QuizBlockView } from "@game-guild/quiz-content"

export function PreviewQuiz({
  node,
  submissionMode,
}: {
  node: QuizBlockView
  submissionMode?: QuizSubmissionMode
}) {
  const [answer, setAnswer] = useState<QuizAnswer>(() => createEmptyQuizAnswer(node.data.type))
  const [submissionResult, setSubmissionResult] = useState<QuizSubmissionResult>({ status: "idle" })

  useEffect(() => {
    setAnswer(createEmptyQuizAnswer(node.data.type))
    setSubmissionResult({ status: "idle" })
  }, [node.data.type])

  if (!node?.data) {
    console.error("Invalid quiz node structure:", node)
    return null
  }

  return (
    <QuizWrapper>
      {submissionMode === "server-graded" ? (
        <QuizPlayer
          entry={toQuizLearnerEntry(node.data)}
          answer={answer}
          onAnswerChange={setAnswer}
          onSubmit={() => setSubmissionResult({ status: "pending", feedback: "Answer submitted." })}
          submissionResult={submissionResult}
        />
      ) : (
        <QuizPracticePlayer entry={node.data} answer={answer} onAnswerChange={setAnswer} />
      )}
    </QuizWrapper>
  )
}
