"use client"

import { useState, useEffect } from "react"
import { X, BookOpen, Save, RotateCcw } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { Textarea } from "@/components/ui/textarea"
import { QuizDisplay } from "@/components/editor/extras/quiz/quiz-display"
import { QuizWrapper } from "@/components/editor/extras/quiz/quiz-wrapper"
import { QuizTypeSelector } from "./quiz-type-selector"
import type { QuizData, QuestionType, QuizAnswer, FillBlankAlternative } from "../quiz-node"

interface QuizSettingsDialogProps {
  isOpen: boolean
  onClose: () => void
  data: QuizData
  onSave: (data: QuizData) => void
}

export function QuizSettingsDialog({ isOpen, onClose, data, onSave }: QuizSettingsDialogProps) {
  const [showTypeSelector, setShowTypeSelector] = useState(!data.question)
  const [question, setQuestion] = useState(data.question)
  const [questionType, setQuestionType] = useState<QuestionType>(data.questionType || "multiple-choice")
  const [answers, setAnswers] = useState<QuizAnswer[]>(data.answers || [])
  const [correctFeedback, setCorrectFeedback] = useState(data.correctFeedback || "")
  const [incorrectFeedback, setIncorrectFeedback] = useState(data.incorrectFeedback || "")
  const [allowRetry, setAllowRetry] = useState(data.allowRetry !== undefined ? data.allowRetry : true)
  const [backgroundColor, setBackgroundColor] = useState(data.backgroundColor || "white")
  const [blanks, setBlanks] = useState<string[]>(data.blanks || [])
  const [fillBlankMode, setFillBlankMode] = useState<"text" | "multiple-choice">(data.fillBlankMode || "text")
  const [fillBlankAlternatives, setFillBlankAlternatives] = useState<FillBlankAlternative[]>(
    data.fillBlankAlternatives || [],
  )
  const [ratingScale, setRatingScale] = useState(data.ratingScale || { min: 1, max: 5, step: 1 })
  const [correctRating, setCorrectRating] = useState(data.correctRating || 3)

  const [previewSelectedAnswers, setPreviewSelectedAnswers] = useState<string[]>([])
  const [previewShowFeedback, setPreviewShowFeedback] = useState(false)
  const [previewIsCorrect, setPreviewIsCorrect] = useState(false)

  useEffect(() => {
    if (isOpen) {
      setShowTypeSelector(!data.question)
      setQuestion(data.question)
      setQuestionType(data.questionType || "multiple-choice")
      setAnswers(data.answers || [])
      setCorrectFeedback(data.correctFeedback || "")
      setIncorrectFeedback(data.incorrectFeedback || "")
      setAllowRetry(data.allowRetry !== undefined ? data.allowRetry : true)
      setBackgroundColor(data.backgroundColor || "white")
      setBlanks(data.blanks || [])
      setFillBlankMode(data.fillBlankMode || "text")
      setFillBlankAlternatives(data.fillBlankAlternatives || [])
      setRatingScale(data.ratingScale || { min: 1, max: 5, step: 1 })
      setCorrectRating(data.correctRating || 3)

      setPreviewSelectedAnswers([])
      setPreviewShowFeedback(false)
      setPreviewIsCorrect(false)
    }
  }, [isOpen, data])

  const handleTypeSelect = (template: any) => {
    setQuestionType(template.type)
    setQuestion(template.defaultData.questions[0].question)

    // Set answers based on template
    if (template.defaultData.questions[0].options) {
      setAnswers(
        template.defaultData.questions[0].options.map((option: string, index: number) => ({
          id: (index + 1).toString(),
          text: option,
          isCorrect: index === template.defaultData.questions[0].correctAnswer,
        })),
      )
    } else if (template.type === "true-false") {
      setAnswers([
        { id: "true", text: "True", isCorrect: template.defaultData.questions[0].correctAnswer === true },
        { id: "false", text: "False", isCorrect: template.defaultData.questions[0].correctAnswer === false },
      ])
    } else if (template.type === "fill-blank") {
      setBlanks([template.defaultData.questions[0].correctAnswer])
    }

    setShowTypeSelector(false)
  }

  const addAnswer = () => {
    const newAnswer: QuizAnswer = {
      id: Math.random().toString(36).substring(7),
      text: "",
      isCorrect: false,
    }
    setAnswers([...answers, newAnswer])
  }

  const updateAnswer = (id: string, text: string) => {
    setAnswers(answers.map((a) => (a.id === id ? { ...a, text } : a)))
  }

  const toggleCorrect = (id: string) => {
    setAnswers(answers.map((a) => (a.id === id ? { ...a, isCorrect: !a.isCorrect } : a)))
  }

  const removeAnswer = (id: string) => {
    setAnswers(answers.filter((a) => a.id !== id))
  }

  const handleSave = () => {
    const quizData: QuizData = {
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
    }
    onSave(quizData)
    onClose()
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-gray-900 border dark:border-gray-700 rounded-lg shadow-2xl w-full max-w-7xl h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-900">
          <div className="flex items-center gap-2">
            <BookOpen className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Quiz Builder</h2>
          </div>
          <Button variant="ghost" size="sm" onClick={onClose} className="hover:bg-gray-100 dark:hover:bg-gray-800">
            <X className="h-4 w-4" />
          </Button>
        </div>

        {showTypeSelector && (
          <QuizTypeSelector onSelect={handleTypeSelect} onCancel={() => setShowTypeSelector(false)} />
        )}

        {/* Main Content - only show when not selecting type */}
        {!showTypeSelector && (
          <>
            {/* Settings Bar */}
            <div className="flex items-center gap-4 p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
              <div className="flex items-center gap-2">
                <span className="text-sm text-gray-600 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                  Type: <span className="font-medium text-gray-800 dark:text-gray-200">{questionType}</span>
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowTypeSelector(true)}
                  className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800"
                >
                  Change Type
                </Button>
              </div>
            </div>

            {/* Two Column Layout */}
            <div className="flex-1 flex min-h-0">
              {/* Left Panel - Configuration */}
              <div className="w-1/2 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-white dark:bg-gray-900">
                <div className="p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
                  <h3 className="font-medium text-gray-800 dark:text-gray-200">Configuration</h3>
                </div>

                <div className="flex-1 overflow-y-auto p-6 space-y-6">
                  {/* Question Input */}
                  <div className="space-y-2">
                    <Label className="text-base font-medium">Question</Label>
                    <Textarea
                      placeholder="Enter your question here..."
                      value={question}
                      onChange={(e) => setQuestion(e.target.value)}
                      rows={3}
                      className="resize-none"
                    />
                  </div>

                  {/* Question Type Specific Configuration */}
                  {questionType === "multiple-choice" && (
                    <div className="space-y-3">
                      <Label className="text-sm font-medium">Answer Options</Label>
                      {answers.map((answer) => (
                        <div key={answer.id} className="flex items-center gap-2">
                          <input
                            type="checkbox"
                            checked={answer.isCorrect}
                            onChange={() => toggleCorrect(answer.id)}
                            className="w-4 h-4 rounded"
                          />
                          <Input
                            placeholder="Enter answer"
                            value={answer.text}
                            onChange={(e) => updateAnswer(answer.id, e.target.value)}
                            className="flex-1"
                          />
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => removeAnswer(answer.id)}
                            disabled={answers.length <= 2}
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        </div>
                      ))}
                      <Button variant="outline" size="sm" onClick={addAnswer}>
                        Add Answer
                      </Button>
                    </div>
                  )}

                  {questionType === "true-false" && (
                    <div className="space-y-3">
                      <Label className="text-sm font-medium">Correct Answer</Label>
                      <Select
                        value={answers.find((a) => a.isCorrect)?.id || ""}
                        onValueChange={(value) => {
                          setAnswers(answers.map((a) => ({ ...a, isCorrect: a.id === value })))
                        }}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select correct answer" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="true">True</SelectItem>
                          <SelectItem value="false">False</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  )}

                  {/* Feedback Settings */}
                  <div className="space-y-4">
                    <Label className="text-base font-medium">Feedback Messages</Label>
                    <div className="space-y-3">
                      <div>
                        <Label className="text-sm">Correct Answer Feedback</Label>
                        <Input
                          placeholder="Great job! That's correct!"
                          value={correctFeedback}
                          onChange={(e) => setCorrectFeedback(e.target.value)}
                        />
                      </div>
                      <div>
                        <Label className="text-sm">Incorrect Answer Feedback</Label>
                        <Input
                          placeholder="Not quite right. Try again!"
                          value={incorrectFeedback}
                          onChange={(e) => setIncorrectFeedback(e.target.value)}
                        />
                      </div>
                    </div>
                  </div>

                  {/* Settings */}
                  <div className="space-y-4">
                    <Label className="text-base font-medium">Settings</Label>
                    <div className="space-y-3">
                      <div className="flex items-center justify-between">
                        <Label className="text-sm">Allow Retry</Label>
                        <Switch checked={allowRetry} onCheckedChange={setAllowRetry} />
                      </div>
                      <div>
                        <Label className="text-sm">Background Color</Label>
                        <Select value={backgroundColor} onValueChange={setBackgroundColor}>
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="white">White</SelectItem>
                            <SelectItem value="blue">Blue</SelectItem>
                            <SelectItem value="green">Green</SelectItem>
                            <SelectItem value="purple">Purple</SelectItem>
                            <SelectItem value="orange">Orange</SelectItem>
                            <SelectItem value="gray">Gray</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              {/* Right Panel - Live Preview */}
              <div className="w-1/2 flex flex-col bg-white dark:bg-gray-900">
                <div className="p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850 flex items-center justify-between">
                  <h3 className="font-medium text-gray-800 dark:text-gray-200">Live Preview</h3>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setPreviewSelectedAnswers([])
                      setPreviewShowFeedback(false)
                      setPreviewIsCorrect(false)
                    }}
                    className="flex items-center gap-1 text-xs"
                  >
                    <RotateCcw className="h-3 w-3" />
                    Reset
                  </Button>
                </div>
                <div className="flex-1 p-4 overflow-auto bg-gray-50 dark:bg-gray-900">
                  <QuizWrapper backgroundColor={backgroundColor}>
                    <QuizDisplay
                      question={question || "Your question will appear here..."}
                      questionType={questionType}
                      answers={answers}
                      selectedAnswers={previewSelectedAnswers}
                      setSelectedAnswers={setPreviewSelectedAnswers}
                      showFeedback={previewShowFeedback}
                      isCorrect={previewIsCorrect}
                      correctFeedback={correctFeedback}
                      incorrectFeedback={incorrectFeedback}
                      allowRetry={allowRetry}
                      checkAnswers={() => {
                        const correctAnswerIds = answers.filter((a) => a.isCorrect).map((a) => a.id)
                        const isCorrect =
                          previewSelectedAnswers.length > 0 &&
                          previewSelectedAnswers.every((id) => correctAnswerIds.includes(id)) &&
                          correctAnswerIds.every((id) => previewSelectedAnswers.includes(id))

                        setPreviewIsCorrect(isCorrect)
                        setPreviewShowFeedback(true)
                      }}
                      toggleAnswer={(answerId: string) => {
                        if (questionType === "multiple-choice") {
                          setPreviewSelectedAnswers((prev) =>
                            prev.includes(answerId) ? prev.filter((id) => id !== answerId) : [...prev, answerId],
                          )
                        } else {
                          setPreviewSelectedAnswers([answerId])
                        }
                        setPreviewShowFeedback(false)
                      }}
                      blanks={blanks}
                      fillBlankMode={fillBlankMode}
                      fillBlankAlternatives={fillBlankAlternatives}
                      ratingScale={ratingScale}
                      correctRating={correctRating}
                    />
                  </QuizWrapper>
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="p-4 border-t border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
              <div className="flex gap-2 justify-end">
                <Button
                  variant="outline"
                  onClick={onClose}
                  className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleSave}
                  className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600"
                  disabled={!question.trim()}
                >
                  <Save className="h-4 w-4" />
                  Save Quiz
                </Button>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
