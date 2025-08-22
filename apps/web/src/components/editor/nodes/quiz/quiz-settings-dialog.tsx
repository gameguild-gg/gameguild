"use client"

import { useState, useEffect } from "react"
import { X, BookOpen, Save, RotateCcw, Plus, Trash2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { Textarea } from "@/components/ui/textarea"
import { QuizDisplay } from "@/components/editor/extras/quiz/quiz-display"
import { QuizWrapper } from "@/components/editor/extras/quiz/quiz-wrapper"
import { QuizTypeSelector } from "./quiz-type-selector"
import type { QuizData, QuestionType, QuizAnswer, FillBlankField, FillBlankAlternative } from "../quiz-node"

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
  const [fillBlankFields, setFillBlankFields] = useState<FillBlankField[]>(data.fillBlankFields || [])
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
      setFillBlankFields(data.fillBlankFields || [])
      setRatingScale(data.ratingScale || { min: 1, max: 5, step: 1 })
      setCorrectRating(data.correctRating || 3)

      setPreviewSelectedAnswers([])
      setPreviewShowFeedback(false)
      setPreviewIsCorrect(false)
    }
  }, [isOpen, data])

  // Auto-detect blanks when question changes for fill-blank type
  useEffect(() => {
    if (questionType === "fill-blank" && question) {
      const blanks = question.split("___")
      if (blanks.length > 1) {
        // Create or update fill blank fields
        const newFields: FillBlankField[] = []
        for (let i = 0; i < blanks.length - 1; i++) {
          // Check if we already have a field for this position
          const existingField = fillBlankFields.find(field => field.position === i)
          if (existingField) {
            // Preserve existing field data
            newFields.push(existingField)
          } else {
            // Create new field with default values
            newFields.push({
              id: Math.random().toString(36).substring(7),
              position: i,
              expectedWords: [""],
              alternatives: []
            })
          }
        }
        
        // Only update if the number of fields changed
        if (newFields.length !== fillBlankFields.length) {
          setFillBlankFields(newFields)
        }
      } else {
        // No blanks detected, clear fields
        if (fillBlankFields.length > 0) {
          setFillBlankFields([])
        }
      }
    }
  }, [question, questionType, fillBlankFields])

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
      // Parse the question to find blanks and create fields
      const questionText = template.defaultData.questions[0].question
      const blanks = questionText.split("___")
      const newFields: FillBlankField[] = []
      
      for (let i = 0; i < blanks.length - 1; i++) {
        newFields.push({
          id: Math.random().toString(36).substring(7),
          position: i,
          expectedWords: [template.defaultData.questions[0].correctAnswer || ""],
          alternatives: []
        })
      }
      
      setFillBlankFields(newFields)
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

  // Fill-blank specific functions
  const addFillBlankField = () => {
    const newField: FillBlankField = {
      id: Math.random().toString(36).substring(7),
      position: fillBlankFields.length,
      expectedWords: [""],
      alternatives: []
    }
    setFillBlankFields([...fillBlankFields, newField])
  }

  const removeFillBlankField = (id: string) => {
    setFillBlankFields(fillBlankFields.filter(field => field.id !== id))
  }

  const updateFillBlankFieldWords = (id: string, words: string[]) => {
    setFillBlankFields(fillBlankFields.map(field => 
      field.id === id ? { ...field, expectedWords: words } : field
    ))
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
      fillBlankFields: questionType === "fill-blank" ? fillBlankFields : undefined,
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
                      placeholder="Enter your question here... Use ___ to create blanks"
                      value={question}
                      onChange={(e) => setQuestion(e.target.value)}
                      rows={3}
                      className="resize-none"
                    />
                    {questionType === "fill-blank" && (
                      <div className="text-sm text-gray-600 bg-blue-50 p-3 rounded-lg">
                        <p className="font-medium mb-2">💡 Dica para Fill-in-the-Blank:</p>
                        <p>Use <code className="bg-gray-200 px-1 rounded">___</code> para criar espaços em branco.</p>
                        <p>Exemplo: "A capital do Brasil é ___ e fica no estado de ___."</p>
                      </div>
                    )}
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

                  {/* Fill-blank Configuration */}
                  {questionType === "fill-blank" && (
                    <div className="space-y-4">
                      <div className="flex items-center justify-between">
                        <Label className="text-base font-medium">Blank Fields Configuration</Label>
                        <Button variant="outline" size="sm" onClick={addFillBlankField}>
                          <Plus className="h-4 w-4 mr-2" />
                          Add Blank
                        </Button>
                      </div>
                      
                      {fillBlankFields.length === 0 && (
                        <div className="text-center py-8 text-gray-500 bg-gray-50 rounded-lg">
                          <p>No blanks detected in the question.</p>
                          <p className="text-sm">Add <code className="bg-gray-200 px-1 rounded">___</code> to your question to create blanks.</p>
                        </div>
                      )}

                      {fillBlankFields.map((field, index) => (
                        <div key={field.id} className="border border-gray-200 rounded-lg p-4 space-y-3">
                          <div className="flex items-center justify-between">
                            <Label className="text-sm font-medium">Blank #{index + 1}</Label>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => removeFillBlankField(field.id)}
                              className="text-red-600 hover:text-red-700 hover:bg-red-50"
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                          
                          <div className="space-y-2">
                            <Label className="text-xs text-gray-600">Expected Words</Label>
                            <div className="text-xs text-gray-500 mb-2">
                              You can include any characters including commas, spaces, and punctuation. Separate multiple acceptable answers with line breaks.
                            </div>
                            <Textarea
                              placeholder="e.g., word1&#10;word2&#10;phrase with spaces, punctuation!&#10;another answer"
                              value={field.expectedWords.join("\n")}
                              onChange={(e) => {
                                // Split by line breaks to allow multiple acceptable answers
                                const words = e.target.value.split("\n").map(w => w.trim()).filter(w => w)
                                updateFillBlankFieldWords(field.id, words)
                              }}
                              rows={4}
                              className="resize-none"
                            />
                          </div>
                        </div>
                      ))}
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
                        if (questionType === "fill-blank") {
                          // Check fill-blank answers
                          const isCorrect = fillBlankFields.every((field, index) => {
                            const userAnswer = previewSelectedAnswers[index] || ""
                            if (!userAnswer.trim()) return false
                            
                            // Check if user answer matches any of the expected words
                            return field.expectedWords.some(word => 
                              word.toLowerCase().trim() === userAnswer.toLowerCase().trim()
                            )
                          })
                          setPreviewIsCorrect(isCorrect)
                        } else {
                          // Check other question types
                          const correctAnswerIds = answers.filter((a) => a.isCorrect).map((a) => a.id)
                          const isCorrect =
                            previewSelectedAnswers.length > 0 &&
                            previewSelectedAnswers.every((id) => correctAnswerIds.includes(id)) &&
                            correctAnswerIds.every((id) => previewSelectedAnswers.includes(id))
                          setPreviewIsCorrect(isCorrect)
                        }
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
                      fillBlankFields={fillBlankFields}
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
