"use client"

import type { SerializedQuizNode } from "../../nodes/quiz-node"
import { useQuizLogic } from "@/hooks/editor/use-quiz-logic"
import { QuizWrapper } from "@/components/editor/extras/quiz/quiz-wrapper"
import { QuizDisplay } from "@/components/editor/extras/quiz/quiz-display"
import { useEffect, useRef, useMemo } from "react"

// Função para migrar dados antigos para o novo formato
function migrateFillBlankData(nodeData: any) {
  // Se já tem fillBlankFields, não precisa migrar
  if (nodeData.fillBlankFields) {
    return nodeData.fillBlankFields
  }

  // Se tem dados antigos, migrar para o novo formato
  if (nodeData.questionType === 'fill-blank' && nodeData.fillBlankAlternatives) {
    const blankCount = (nodeData.question.match(/___/g) || []).length
    
    return Array.from({ length: blankCount }, (_, index) => ({
      id: `blank-${index}`,
      position: index,
      expectedWords: [], // Não há expectedWords no formato antigo
      alternatives: nodeData.fillBlankAlternatives
        .filter((alt: any) => alt.isCorrect)
        .map((alt: any) => ({
          id: alt.id,
          words: alt.words,
          isCorrect: alt.isCorrect
        }))
    }))
  }

  return []
}

export function PreviewQuiz({ node }: { node: SerializedQuizNode }) {
  const previousAnswersRef = useRef<string>("")
  const previousQuestionRef = useRef<string>("")
  
  // Migrar dados antigos para o novo formato
  const migratedFillBlankFields = useMemo(() => 
    migrateFillBlankData(node.data), 
    [node.data]
  )
  
  const quizLogic = useQuizLogic({
    answers: node.data?.answers || [],
    allowRetry: node.data?.allowRetry !== undefined ? node.data?.allowRetry : true,
    correctFeedback: node.data?.correctFeedback || "",
    incorrectFeedback: node.data?.incorrectFeedback || "",
    questionType: node.data?.questionType,
    fillBlankFields: migratedFillBlankFields,
  })

  const {
    question,
    questionType,
    answers,
    correctFeedback,
    incorrectFeedback,
    allowRetry,
    backgroundColor,
    ratingScale,
    correctRating,
  } = node.data

  const { selectedAnswers, setSelectedAnswers, showFeedback, isCorrect, checkAnswers, toggleAnswer, resetQuiz } =
    quizLogic

  useEffect(() => {
    // Só resetar o quiz quando as respostas ou perguntas mudarem realmente
    const currentAnswers = JSON.stringify(answers)
    const currentQuestion = question
    
    if (currentAnswers !== previousAnswersRef.current || currentQuestion !== previousQuestionRef.current) {
      resetQuiz()
      previousAnswersRef.current = currentAnswers
      previousQuestionRef.current = currentQuestion
    }
  }, [answers, question, resetQuiz])

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
        fillBlankFields={migratedFillBlankFields}
        ratingScale={ratingScale}
        correctRating={correctRating}
      />
    </QuizWrapper>
  )
}
