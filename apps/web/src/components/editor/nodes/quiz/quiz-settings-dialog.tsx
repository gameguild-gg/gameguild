/**
 * Simplified Quiz Settings Dialog with React Hook Form
 * Cleaner implementation with proper form management
 */

"use client"

import { useState, useEffect } from "react"
import { useForm, FormProvider } from "react-hook-form"
import { X, BookOpen, Save, RotateCcw, FileText, Users } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { Textarea } from "@/components/ui/textarea"
import { QuizWrapper } from "@/components/editor/extras/quiz/quiz-wrapper"
import { QuizDisplay } from "@/components/editor/extras/quiz/quiz-display"
import { QuizTypeSelector } from "./quiz-type-selector"
import { MultipleChoiceEditor } from "./editors/multiple-choice-editor"
import { TrueFalseEditor } from "./editors/true-false-editor"
import { FillBlankEditor } from "./editors/fill-blank-editor"
import { RatingEditor } from "./editors/rating-editor"
import { useQuizAnswers } from "./hooks/use-quiz-answers"
import type { QuizData } from "../../nodes/quiz-node"

interface QuizSettingsDialogProps {
  isOpen: boolean
  onClose: () => void
  data: QuizData
  onSave: (data: QuizData) => void
}

export function QuizSettingsDialog({ isOpen, onClose, data, onSave }: QuizSettingsDialogProps) {
  const [showTypeSelector, setShowTypeSelector] = useState(!data.question)

  const form = useForm<QuizData>({
    defaultValues: data,
  })

  const { register, watch, setValue, handleSubmit, reset } = form
  const questionType = watch("questionType")
  const question = watch("question")
  const backgroundColor = watch("backgroundColor")

  // Quiz answers hook for preview testing
  const {
    selectedAnswers,
    setSelectedAnswers,
    showFeedback,
    isCorrect,
    checkAnswers,
    resetQuiz,
  } = useQuizAnswers({
    data: {
      question: question,
      questionType: questionType,
      answers: watch("answers") || [],
      correctFeedback: watch("correctFeedback") || "",
      incorrectFeedback: watch("incorrectFeedback") || "",
      allowRetry: watch("allowRetry"),
      backgroundColor: backgroundColor,
      fillBlankFields: watch("fillBlankFields"),
      ratingScale: watch("ratingScale"),
      correctRating: watch("correctRating"),
    },
  })

  // Reset form when dialog opens
  useEffect(() => {
    if (isOpen) {
      reset(data)
      setShowTypeSelector(!data.question)
      document.body.style.overflow = "hidden"
      document.body.style.pointerEvents = "none"
    }
    return () => {
      document.body.style.overflow = ""
      document.body.style.pointerEvents = ""
    }
  }, [isOpen, data, reset])

  const handleTypeSelect = (template: any) => {
    setValue("questionType", template.type)
    setValue("question", template.defaultData.questions[0].question)

    if (template.defaultData.questions[0].options) {
      setValue(
        "answers",
        template.defaultData.questions[0].options.map((option: string, index: number) => ({
          id: (index + 1).toString(),
          text: option,
          isCorrect: index === template.defaultData.questions[0].correctAnswer,
        }))
      )
    } else if (template.type === "true-false") {
      setValue("answers", [
        { id: "true", text: "True", isCorrect: template.defaultData.questions[0].correctAnswer === true },
        { id: "false", text: "False", isCorrect: template.defaultData.questions[0].correctAnswer === false },
      ])
    }

    setShowTypeSelector(false)
  }

  const onSubmit = (formData: QuizData) => {
    document.body.style.overflow = ""
    document.body.style.pointerEvents = ""
    onSave(formData)
    onClose()
  }

  const handleClose = () => {
    document.body.style.overflow = ""
    document.body.style.pointerEvents = ""
    onClose()
  }

  if (!isOpen) return null

  return (
    <div
      className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
      style={{ pointerEvents: "auto" }}
      onClick={handleClose}
    >
      <div
        className="bg-white dark:bg-gray-900 border dark:border-gray-700 shadow-2xl w-full max-w-7xl h-[90vh] flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <BookOpen className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Quiz Builder</h2>
          </div>
          <Button variant="ghost" size="sm" onClick={handleClose} className="hover:bg-gray-100 dark:hover:bg-gray-800">
            <X className="h-4 w-4" />
          </Button>
        </div>

        {showTypeSelector && (
          <QuizTypeSelector onSelect={handleTypeSelect} onCancel={() => setShowTypeSelector(false)} />
        )}

        {!showTypeSelector && (
          <FormProvider {...form}>
            <form onSubmit={handleSubmit(onSubmit)} className="flex-1 flex flex-col min-h-0">
              {/* Settings Bar */}
              <div className="flex items-center gap-4 p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                <div className="flex items-center gap-2">
                  <span className="text-sm text-gray-600 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                    Type: <span className="font-medium text-gray-800 dark:text-gray-200">{questionType}</span>
                  </span>
                  <Button
                    type="button"
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
                <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col bg-white dark:bg-gray-900">
                  <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                    <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                      <FileText className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                      Configuration
                    </h3>
                  </div>

                  <div className="flex-1 overflow-y-auto p-6 space-y-6 bg-white dark:bg-gray-950">
                    {/* Question Input */}
                    <div className="space-y-2">
                      <Label className="text-sm font-medium">Question</Label>
                      <Textarea
                        placeholder="Enter your question here..."
                        {...register("question", { required: true })}
                        rows={3}
                        className="resize-none"
                      />
                    </div>

                    {/* Question Type Specific Editors */}
                    {questionType === "multiple-choice" && <MultipleChoiceEditor />}
                    {questionType === "true-false" && <TrueFalseEditor />}
                    {questionType === "fill-blank" && <FillBlankEditor />}
                    {questionType === "rating" && <RatingEditor />}

                    {/* Feedback Messages */}
                    <div className="space-y-4">
                      <Label className="text-sm font-medium">Feedback Messages</Label>
                      <div className="space-y-3">
                        <div>
                          <Label className="text-xs text-gray-600">Correct Answer Feedback</Label>
                          <Input
                            placeholder="Great job! That's correct!"
                            {...register("correctFeedback")}
                            className="mt-1"
                          />
                        </div>
                        <div>
                          <Label className="text-xs text-gray-600">Incorrect Answer Feedback</Label>
                          <Input
                            placeholder="Not quite right. Try again!"
                            {...register("incorrectFeedback")}
                            className="mt-1"
                          />
                        </div>
                      </div>
                    </div>

                    {/* Settings */}
                    <div className="space-y-4">
                      <Label className="text-sm font-medium">Settings</Label>
                      <div className="space-y-3">
                        <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
                          <Label className="text-sm">Allow Retry</Label>
                          <Switch
                            checked={watch("allowRetry")}
                            onCheckedChange={(checked) => setValue("allowRetry", checked)}
                          />
                        </div>
                        <div>
                          <Label className="text-xs text-gray-600">Background Color</Label>
                          <Select
                            value={watch("backgroundColor")}
                            onValueChange={(value) => setValue("backgroundColor", value)}
                          >
                            <SelectTrigger className="mt-1">
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
                  <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                    <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                      <Users className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                      Live Preview
                    </h3>
                  </div>
                  <div className="flex-1 p-4 overflow-auto bg-white dark:bg-gray-950">
                    <QuizWrapper backgroundColor={backgroundColor}>
                      {question ? (
                        <QuizDisplay
                          data={{
                            question: question,
                            questionType: questionType,
                            answers: watch("answers") || [],
                            correctFeedback: watch("correctFeedback") || "",
                            incorrectFeedback: watch("incorrectFeedback") || "",
                            allowRetry: watch("allowRetry"),
                            backgroundColor: backgroundColor,
                            fillBlankFields: watch("fillBlankFields"),
                            ratingScale: watch("ratingScale"),
                            correctRating: watch("correctRating"),
                          }}
                          selectedAnswers={selectedAnswers}
                          setSelectedAnswers={setSelectedAnswers}
                          showFeedback={showFeedback}
                          isCorrect={isCorrect}
                          checkAnswers={checkAnswers}
                          resetQuiz={resetQuiz}
                        />
                      ) : (
                        <div className="space-y-4">
                          <div className="text-lg font-medium text-gray-400">Your question will appear here...</div>
                          <div className="text-sm text-gray-500 italic">Preview will update as you edit</div>
                        </div>
                      )}
                    </QuizWrapper>
                  </div>
                </div>
              </div>

              {/* Footer */}
              <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                <div className="flex gap-2 justify-end">
                  <Button type="button" variant="outline" onClick={handleClose}>
                    Cancel
                  </Button>
                  <Button type="submit" className="flex items-center gap-2" disabled={!question?.trim()}>
                    <Save className="h-4 w-4" />
                    Save Quiz
                  </Button>
                </div>
              </div>
            </form>
          </FormProvider>
        )}
      </div>
    </div>
  )
}
