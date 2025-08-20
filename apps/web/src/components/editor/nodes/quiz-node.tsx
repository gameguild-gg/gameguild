"use client"

import { useState, useEffect, createContext, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { Pencil } from "lucide-react"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import type { JSX } from "react/jsx-runtime"

import { Button } from "@/components/ui/button"
import { QuizDisplay } from "@/components/editor/extras/quiz/quiz-display"
import { QuizWrapper } from "@/components/editor/extras/quiz/quiz-wrapper"
import { useQuizLogic } from "@/hooks/editor/use-quiz-logic"
import { ContentEditMenu } from "@/components/ui/content-edit-menu"
import { QuizSettingsDialog } from "./quiz/quiz-settings-dialog"

// Adicionar no topo do arquivo, após os imports
const EditorLoadingContext = createContext<boolean>(false)

export const EditorLoadingProvider = EditorLoadingContext.Provider
export const useEditorLoading = () => useContext(EditorLoadingContext)

export type QuestionType =
  | "multiple-choice"
  | "true-false"
  | "fill-blank"
  | "short-answer"
  | "essay"
  | "matching"
  | "ordering"
  | "rating"

export interface QuizAnswer {
  id: string
  text: string
  isCorrect: boolean
}

export interface MatchingPair {
  id: string
  left: string
  right: string
}

export interface OrderingItem {
  id: string
  text: string
  order: number
}

export interface FillBlankAlternative {
  id: string
  words: string[] // Array of words for each blank position
  isCorrect: boolean
}

export interface QuizData {
  question: string
  questionType: QuestionType
  answers: QuizAnswer[]
  correctFeedback?: string
  incorrectFeedback?: string
  allowRetry: boolean
  backgroundColor?: string
  // For fill-in-the-blank
  blanks?: string[]
  fillBlankMode?: "text" | "multiple-choice"
  fillBlankAlternatives?: FillBlankAlternative[] // New structure for alternatives
  // For matching questions
  matchingPairs?: MatchingPair[]
  // For ordering questions
  orderingItems?: OrderingItem[]
  // For rating questions
  ratingScale?: { min: number; max: number; step: number }
  correctRating?: number
}

export interface SerializedQuizNode extends SerializedLexicalNode {
  type: "quiz"
  data: QuizData
  version: 1
}

export class QuizNode extends DecoratorNode<JSX.Element> {
  __data: QuizData

  static getType(): string {
    return "quiz"
  }

  static clone(node: QuizNode): QuizNode {
    return new QuizNode(node.__data, node.__key)
  }

  constructor(data: QuizData, key?: string) {
    super(key)
    this.__data = {
      ...data,
      correctFeedback: data.correctFeedback || "",
      incorrectFeedback: data.incorrectFeedback || "",
      allowRetry: data.allowRetry !== undefined ? data.allowRetry : true,
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: QuizData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedQuizNode {
    return {
      type: "quiz",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedQuizNode): QuizNode {
    return new QuizNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <QuizComponent data={this.__data} nodeKey={this.__key} />
  }
}

interface QuizComponentProps {
  data: QuizData
  nodeKey: string
}

function QuizComponent({ data, nodeKey }: QuizComponentProps) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useEditorLoading()

  const [isEditing, setIsEditing] = useState(false)

  const {
    selectedAnswers,
    setSelectedAnswers,
    showFeedback,
    setShowFeedback,
    isCorrect,
    setIsCorrect,
    resetQuiz,
    checkAnswers,
    toggleAnswer,
  } = useQuizLogic({
    answers: data.answers || [],
    allowRetry: data.allowRetry !== undefined ? data.allowRetry : true,
    correctFeedback: data.correctFeedback || "",
    incorrectFeedback: data.incorrectFeedback || "",
  })

  useEffect(() => {
    resetQuiz()
  }, [data, resetQuiz])

  const handleSave = (newData: QuizData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof QuizNode) {
        node.setData(newData)
      }
    })
  }

  if (!data.question) {
    return (
      <div className="my-6 p-8 border-2 border-dashed border-gray-300 rounded-xl text-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 rounded-full bg-blue-100 flex items-center justify-center">
            <Pencil className="w-6 h-6 text-blue-600" />
          </div>
          <div>
            <h3 className="font-medium text-gray-900">Configure Quiz</h3>
            <p className="text-sm text-gray-500 mt-1">Click to set up your quiz question and answers</p>
          </div>
          <Button onClick={() => setIsEditing(true)}>Configure Quiz</Button>
        </div>

        <QuizSettingsDialog isOpen={isEditing} onClose={() => setIsEditing(false)} data={data} onSave={handleSave} />
      </div>
    )
  }

  return (
    <>
      <div className="relative group">
        <QuizWrapper backgroundColor={data.backgroundColor}>
          <ContentEditMenu
            options={[
              {
                id: "edit",
                icon: <Pencil className="h-4 w-4" />,
                label: "Edit Quiz",
                action: () => setIsEditing(true),
              },
            ]}
          />

          <QuizDisplay
            question={data.question}
            questionType={data.questionType}
            answers={data.answers || []}
            selectedAnswers={selectedAnswers}
            setSelectedAnswers={setSelectedAnswers}
            showFeedback={showFeedback}
            isCorrect={isCorrect}
            correctFeedback={data.correctFeedback || ""}
            incorrectFeedback={data.incorrectFeedback || ""}
            allowRetry={data.allowRetry !== undefined ? data.allowRetry : true}
            checkAnswers={checkAnswers}
            toggleAnswer={toggleAnswer}
            blanks={data.blanks}
            fillBlankMode={data.fillBlankMode}
            fillBlankAlternatives={data.fillBlankAlternatives}
            ratingScale={data.ratingScale}
            correctRating={data.correctRating}
          />
        </QuizWrapper>
      </div>

      <QuizSettingsDialog isOpen={isEditing} onClose={() => setIsEditing(false)} data={data} onSave={handleSave} />
    </>
  )
}

export function $createQuizNode(): QuizNode {
  return new QuizNode({
    question: "",
    questionType: "multiple-choice",
    answers: [
      { id: "1", text: "", isCorrect: false },
      { id: "2", text: "", isCorrect: false },
    ],
    allowRetry: true,
    fillBlankMode: "text",
    fillBlankAlternatives: [],
    backgroundColor: "white",
  })
}
