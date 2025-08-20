"use client"

import { useState, useEffect } from "react"
import { X, BookOpen, CheckCircle, Circle, Square, Type, FileText, Star } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { Textarea } from "@/components/ui/textarea"
import { QuizDisplay } from "@/components/editor/extras/quiz/quiz-display"
import { QuizWrapper } from "@/components/editor/extras/quiz/quiz-wrapper"
import type { QuizData, QuestionType, QuizAnswer, FillBlankAlternative } from "../quiz-node"

interface QuizSettingsDialogProps {
  isOpen: boolean
  onClose: () => void
  data: QuizData
  onSave: (data: QuizData) => void
}

const QUIZ_TYPES = [
  {
    id: "multiple-choice",
    label: "Multiple Choice",
    icon: CheckCircle,
    description: "Select one or more correct answers",
  },
  { id: "true-false", label: "True/False", icon: Circle, description: "Simple true or false question" },
  { id: "fill-blank", label: "Fill in the Blank", icon: Square, description: "Complete the missing words" },
  { id: "short-answer", label: "Short Answer", icon: Type, description: "Brief text response" },
  { id: "essay", label: "Essay", icon: FileText, description: "Long-form written response" },
  { id: "rating", label: "Rating Scale", icon: Star, description: "Rate on a numerical scale" },
] as const

export function QuizSettingsDialog({ isOpen, onClose, data, onSave }: QuizSettingsDialogProps) {
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

  useEffect(() => {
    if (isOpen) {
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
    }
  }, [isOpen, data])

  const handleQuestionTypeChange = (newType: QuestionType) => {
    setQuestionType(newType)

    switch (newType) {
      case "true-false":
        setAnswers([
          { id: "true", text: "True", isCorrect: false },
          { id: "false", text: "False", isCorrect: false },
        ])
        break
      case "multiple-choice":
        if (answers.length === 0) {
          setAnswers([
            { id: "1", text: "", isCorrect: false },
            { id: "2", text: "", isCorrect: false },
          ])
        }
        break
      case "fill-blank":
        setBlanks([""])
        setFillBlankAlternatives([])
        break
      case "rating":
        setRatingScale({ min: 1, max: 5, step: 1 })
        setCorrectRating(3)
        break
      default:
        if (answers.length === 0) {
          setAnswers([{ id: "1", text: "", isCorrect: true }])
        }
    }
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

  const renderQuestionTypeConfig = () => {
    switch (questionType) {
      case "multiple-choice":
        return (
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
        )

      case "true-false":
        return (
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
        )

      case "fill-blank":
        return (
          <div className="space-y-4">
            <div>
              <Label className="text-sm font-medium">Fill Mode</Label>
              <Select
                value={fillBlankMode}
                onValueChange={(value: "text" | "multiple-choice") => setFillBlankMode(value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="text">Free Text</SelectItem>
                  <SelectItem value="multiple-choice">Multiple Choice</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {fillBlankMode === "text" && (
              <div className="space-y-2">
                <Label className="text-sm font-medium">Correct Answers (use ___ in question)</Label>
                {blanks.map((blank, index) => (
                  <Input
                    key={index}
                    placeholder={`Answer for blank ${index + 1}`}
                    value={blank}
                    onChange={(e) => {
                      const newBlanks = [...blanks]
                      newBlanks[index] = e.target.value
                      setBlanks(newBlanks)
                    }}
                  />
                ))}
              </div>
            )}
          </div>
        )

      case "rating":
        return (
          <div className="space-y-4">
            <div className="grid grid-cols-3 gap-3">
              <div>
                <Label className="text-sm font-medium">Min</Label>
                <Input
                  type="number"
                  value={ratingScale.min}
                  onChange={(e) => setRatingScale({ ...ratingScale, min: Number.parseInt(e.target.value) || 1 })}
                />
              </div>
              <div>
                <Label className="text-sm font-medium">Max</Label>
                <Input
                  type="number"
                  value={ratingScale.max}
                  onChange={(e) => setRatingScale({ ...ratingScale, max: Number.parseInt(e.target.value) || 5 })}
                />
              </div>
              <div>
                <Label className="text-sm font-medium">Correct</Label>
                <Input
                  type="number"
                  value={correctRating}
                  min={ratingScale.min}
                  max={ratingScale.max}
                  onChange={(e) => setCorrectRating(Number.parseInt(e.target.value) || 3)}
                />
              </div>
            </div>
          </div>
        )

      default:
        return (
          <div className="space-y-3">
            <Label className="text-sm font-medium">Sample Answer (optional)</Label>
            <Textarea
              placeholder="Enter a sample correct answer"
              value={answers[0]?.text || ""}
              onChange={(e) => setAnswers([{ id: "1", text: e.target.value, isCorrect: true }])}
              rows={3}
            />
          </div>
        )
    }
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl shadow-xl w-[95vw] max-w-7xl h-[90vh] flex flex-col">
        <div className="flex items-center justify-between p-6 border-b">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-blue-100">
              <BookOpen className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <h2 className="text-xl font-semibold">Quiz Builder</h2>
              <p className="text-sm text-muted-foreground">Create and configure your quiz</p>
            </div>
          </div>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-5 w-5" />
          </Button>
        </div>

        <div className="flex-1 flex overflow-hidden">
          {/* Left Column - Configuration */}
          <div className="w-1/2 border-r overflow-y-auto">
            <div className="p-6 space-y-6">
              {/* Quiz Type Selection */}
              <div className="space-y-3">
                <Label className="text-base font-medium">Quiz Type</Label>
                <div className="grid grid-cols-2 gap-3">
                  {QUIZ_TYPES.map((type) => {
                    const Icon = type.icon
                    return (
                      <button
                        key={type.id}
                        onClick={() => handleQuestionTypeChange(type.id as QuestionType)}
                        className={`p-4 rounded-lg border-2 text-left transition-all ${
                          questionType === type.id
                            ? "border-blue-500 bg-blue-50"
                            : "border-gray-200 hover:border-gray-300"
                        }`}
                      >
                        <div className="flex items-center gap-3 mb-2">
                          <Icon className="w-5 h-5 text-blue-600" />
                          <span className="font-medium">{type.label}</span>
                        </div>
                        <p className="text-sm text-muted-foreground">{type.description}</p>
                      </button>
                    )
                  })}
                </div>
              </div>

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
              {renderQuestionTypeConfig()}

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

          {/* Right Column - Live Preview */}
          <div className="w-1/2 overflow-y-auto bg-gray-50">
            <div className="p-6">
              <div className="mb-4">
                <Label className="text-base font-medium">Live Preview</Label>
                <p className="text-sm text-muted-foreground">See how your quiz will look</p>
              </div>

              <QuizWrapper backgroundColor={backgroundColor}>
                <QuizDisplay
                  question={question || "Your question will appear here..."}
                  questionType={questionType}
                  answers={answers}
                  selectedAnswers={[]}
                  setSelectedAnswers={() => {}}
                  showFeedback={false}
                  isCorrect={false}
                  correctFeedback={correctFeedback}
                  incorrectFeedback={incorrectFeedback}
                  allowRetry={allowRetry}
                  checkAnswers={() => {}}
                  toggleAnswer={() => {}}
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

        <div className="flex items-center justify-end gap-3 p-6 border-t bg-gray-50">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={!question.trim()}>
            Save Quiz
          </Button>
        </div>
      </div>
    </div>
  )
}
