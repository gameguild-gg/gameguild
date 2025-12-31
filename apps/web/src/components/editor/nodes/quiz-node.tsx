"use client"

import { useState, useEffect, createContext, useContext, useRef } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { Pencil } from "lucide-react"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import type { JSX } from "react/jsx-runtime"

import { Button } from "@/components/ui/button"
import { QuizDisplay } from "@/components/editor/extras/quiz/quiz-display"
import { QuizWrapper } from "@/components/editor/extras/quiz/quiz-wrapper"
import { useQuizAnswers } from "@/components/editor/nodes/quiz/hooks/use-quiz-answers"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"
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
  | "categorization"

export interface QuizAnswer {
  id: string
  text: string
  isCorrect: boolean
  categoryIds?: string[] // For categorization type
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

export interface FillBlankField {
  id: string
  position: number // Position in the question text
  expectedWords: string[] // Array of acceptable words for this blank
  alternatives: FillBlankAlternative[] // Alternative word sets for this blank
}

// Using QuizQuestion types from ./quiz/types.ts
export type { QuizQuestion } from "./quiz/types"

// Legacy interface for backward compatibility with existing serialized data
export interface QuizData {
  question: string
  questionType: QuestionType
  answers: QuizAnswer[]
  correctFeedback?: string
  incorrectFeedback?: string
  allowRetry: boolean
  backgroundColor?: string
  fillBlankFields?: FillBlankField[]
  matchingPairs?: MatchingPair[]
  orderingItems?: OrderingItem[]
  ratingScale?: { min: number; max: number; step: number }
  correctRating?: number
  // For categorization
  categories?: Array<{
    id: string
    name: string
    description?: string
  }>
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
  const [isEditing, setIsEditing] = useState(false)
  const [hasAutoOpened, setHasAutoOpened] = useState(false)

  const { selectedAnswers, setSelectedAnswers, showFeedback, isCorrect, checkAnswers, resetQuiz } = useQuizAnswers({
    data,
  })

  // Auto-open editor for new quiz
  useEffect(() => {
    const isNewQuiz = !data.question || data.question.trim() === ""
    if (isNewQuiz && !hasAutoOpened) {
      setIsEditing(true)
      setHasAutoOpened(true)
    }
  }, [data.question, hasAutoOpened])

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
      <div className="my-8 relative group">
        <div className="relative">
          <QuizWrapper backgroundColor={data.backgroundColor}>
            <QuizDisplay
              data={data}
              selectedAnswers={selectedAnswers}
              setSelectedAnswers={setSelectedAnswers}
              showFeedback={showFeedback}
              isCorrect={isCorrect}
              checkAnswers={checkAnswers}
              resetQuiz={resetQuiz}
            />
          </QuizWrapper>

          {/* Edit menu */}
          <ContentEditMenu
            options={[
              {
                id: "edit",
                icon: <Pencil className="h-4 w-4" />,
                label: "Edit Quiz",
                action: () => setIsEditing(true),
              },
            ]}
            className="opacity-100"
          />
        </div>
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
    backgroundColor: "white",
  })
}
