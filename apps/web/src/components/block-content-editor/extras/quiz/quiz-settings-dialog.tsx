/**
 * Quiz Settings Dialog
 * Full-screen dialog for editing quiz questions
 */

"use client"

import { useState, useEffect } from "react"
import { useForm, FormProvider } from "react-hook-form"
import { BookOpen, Save, FileText, Users, RotateCcw } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Textarea } from "@/components/ui/textarea"
import { QuizWrapper } from "./quiz-wrapper"
import { QuizTypeSelector } from "./quiz-type-selector"
import { QuizRenderer } from "./renderers/quiz-renderer"
import { QuizFeedback } from "./quiz-feedback"
import { useQuizAnswers } from "./hooks/use-quiz-answers"
import { useEditorSettings } from "@/components/block-content-editor/extras/settings-menu"
import { BlockEditorShell } from "@/components/block-content-editor/extras/block-editor-shell"
import {
  type QuizEntry,
  QuizEntryType,
  createEmptyAnswerState,
} from "./types"

// Editors
import { SingleChoiceEditor } from "./editors/single-choice-editor"
import { MultipleChoiceEditor } from "./editors/multiple-choice-editor"
import { TrueFalseEditor } from "./editors/true-false-editor"
import { FillBlankEditor } from "./editors/fill-blank-editor"
import { ShortAnswerEditor } from "./editors/short-answer-editor"
import { EssayEditor } from "./editors/essay-editor"
import { MatchingEditor } from "./editors/matching-editor"
import { OrderingEditor } from "./editors/ordering-editor"
import { CategorizationEditor } from "./editors/categorization-editor"
import { RatingEditor } from "./editors/rating-editor"
import { NumericEditor } from "./editors/numeric-editor"
import { FormulaEditor } from "./editors/formula-editor"
import { HotspotEditor } from "./editors/hotspot-editor"
import { HighlightEditor } from "./editors/highlight-editor"

interface QuizSettingsDialogProps {
  isOpen: boolean
  onClose: () => void
  entry: QuizEntry
  onSave: (entry: QuizEntry) => void
}

function getEditorComponent(type: QuizEntryType) {
  switch (type) {
    case QuizEntryType.SingleChoice:
      return SingleChoiceEditor
    case QuizEntryType.MultipleChoice:
      return MultipleChoiceEditor
    case QuizEntryType.TrueFalse:
      return TrueFalseEditor
    case QuizEntryType.FillInTheBlank:
      return FillBlankEditor
    case QuizEntryType.ShortAnswer:
      return ShortAnswerEditor
    case QuizEntryType.Essay:
      return EssayEditor
    case QuizEntryType.Matching:
      return MatchingEditor
    case QuizEntryType.Ordering:
      return OrderingEditor
    case QuizEntryType.Categorization:
      return CategorizationEditor
    case QuizEntryType.Rating:
      return RatingEditor
    case QuizEntryType.Numeric:
      return NumericEditor
    case QuizEntryType.Formula:
      return FormulaEditor
    case QuizEntryType.Hotspot:
      return HotspotEditor
    case QuizEntryType.Highlight:
      return HighlightEditor
    default:
      return null
  }
}

function getTypeLabel(type: QuizEntryType): string {
  const labels: Record<QuizEntryType, string> = {
    [QuizEntryType.SingleChoice]: "Single Choice",
    [QuizEntryType.MultipleChoice]: "Multiple Choice",
    [QuizEntryType.TrueFalse]: "True/False",
    [QuizEntryType.FillInTheBlank]: "Fill in the Blank",
    [QuizEntryType.ShortAnswer]: "Short Answer",
    [QuizEntryType.Essay]: "Essay",
    [QuizEntryType.Matching]: "Matching",
    [QuizEntryType.Ordering]: "Ordering",
    [QuizEntryType.Categorization]: "Categorization",
    [QuizEntryType.Rating]: "Rating",
    [QuizEntryType.Numeric]: "Numeric",
    [QuizEntryType.Formula]: "Formula",
    [QuizEntryType.Hotspot]: "Hotspot",
    [QuizEntryType.Highlight]: "Highlight",
  }
  return labels[type] || type
}

export function QuizSettingsDialog({ isOpen, onClose, entry, onSave }: QuizSettingsDialogProps) {
  const [showTypeSelector, setShowTypeSelector] = useState(!entry.stem)
  const settings = useEditorSettings("quiz")

  const form = useForm<QuizEntry>({
    defaultValues: entry,
  })

  const { watch, setValue, handleSubmit, reset } = form
  const currentEntry = watch()
  const stem = watch("stem")

  // Quiz answers hook for preview testing
  const {
    answerState,
    updateAnswerState,
    showFeedback,
    isCorrect,
    checkAnswers,
    resetQuiz,
  } = useQuizAnswers({ entry: currentEntry })

  // Reset form when dialog opens
  useEffect(() => {
    if (isOpen) {
      reset(entry)
      setShowTypeSelector(!entry.stem)
    }
  }, [isOpen, entry, reset])

  const handleTypeSelect = (newEntry: QuizEntry) => {
    reset(newEntry)
    setShowTypeSelector(false)
  }

  const onSubmit = (formData: QuizEntry) => {
    onSave(formData)
    onClose()
  }

  const handleClose = () => {
    onClose()
  }

  if (!isOpen) return null

  const EditorComponent = getEditorComponent(currentEntry.type)

  return (
    <BlockEditorShell
      settings={settings}
      includeMonacoTheme={false}
      onClose={handleClose}
      icon={<BookOpen className="h-5 w-5 text-blue-600 dark:text-blue-400" />}
      title="Quiz Builder"
    >
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
                    Type: <span className="font-medium text-gray-800 dark:text-gray-200">{getTypeLabel(currentEntry.type)}</span>
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setShowTypeSelector(true)}
                    className="border-gray-300 dark:border-gray-600"
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
                        value={stem}
                        onChange={(e) => setValue("stem", e.target.value)}
                        rows={3}
                        className="resize-none"
                      />
                    </div>

                    {/* Question Type Specific Editor */}
                    {EditorComponent && <EditorComponent />}

                    {/* Feedback Messages */}
                    <div className="space-y-4">
                      <Label className="text-sm font-medium">Feedback Messages</Label>
                      <div className="space-y-3">
                        <div>
                          <Label className="text-xs text-gray-600 dark:text-gray-400">Correct Answer Feedback</Label>
                          <Input
                            placeholder="Great job! That's correct!"
                            value={currentEntry.feedback?.correct || ""}
                            onChange={(e) => setValue("feedback.correct", e.target.value)}
                            className="mt-1"
                          />
                        </div>
                        <div>
                          <Label className="text-xs text-gray-600 dark:text-gray-400">Incorrect Answer Feedback</Label>
                          <Input
                            placeholder="Not quite right. Try again!"
                            value={currentEntry.feedback?.incorrect || ""}
                            onChange={(e) => setValue("feedback.incorrect", e.target.value)}
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
                          <div>
                            <Label className="text-sm">Show Feedback</Label>
                            <p className="text-xs text-gray-500 dark:text-gray-400">Show correct/incorrect result after submission</p>
                          </div>
                          <Switch
                            checked={currentEntry.settings.showFeedback ?? true}
                            onCheckedChange={(checked) => {
                              setValue("settings.showFeedback", checked)
                              setValue("settings.showCorrectAnswer", checked)
                            }}
                          />
                        </div>
                        <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
                          <div>
                            <Label className="text-sm">Show Correct Answer</Label>
                            <p className="text-xs text-gray-500 dark:text-gray-400">Reveal the correct answer after submission</p>
                          </div>
                          <Switch
                            checked={currentEntry.settings.showCorrectAnswer ?? true}
                            onCheckedChange={(checked) => setValue("settings.showCorrectAnswer", checked)}
                          />
                        </div>
                        <div className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border">
                          <Label className="text-sm">Allow Retry</Label>
                          <Switch
                            checked={currentEntry.settings.allowRetry}
                            onCheckedChange={(checked) => setValue("settings.allowRetry", checked)}
                          />
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
                    <QuizWrapper>
                      {stem ? (
                        <div className="space-y-4">
                          {/* Question text - hide for fill-blank */}
                          {currentEntry.type !== QuizEntryType.FillInTheBlank && (
                            <div className="text-lg font-medium">{stem}</div>
                          )}

                          <QuizRenderer
                            entry={currentEntry}
                            answerState={answerState}
                            onAnswerChange={updateAnswerState}
                            disabled={false}
                            showFeedback={showFeedback}
                          />

                          {/* Submit button - h-12 matches Feedback/Submitted height */}
                          {!showFeedback && (
                            <button
                              type="button"
                              onClick={checkAnswers}
                              className="w-full h-12 bg-blue-600 hover:bg-blue-700 text-white font-semibold px-6 rounded-lg transition-colors"
                            >
                              Submit Answer
                            </button>
                          )}

                          {/* Feedback */}
                          {showFeedback && (currentEntry.settings.showFeedback ?? true) && (
                            <QuizFeedback
                              isCorrect={isCorrect}
                              correctFeedback={currentEntry.feedback?.correct || ""}
                              incorrectFeedback={currentEntry.feedback?.incorrect || ""}
                              allowRetry={currentEntry.settings.allowRetry}
                              onRetry={resetQuiz}
                              showRetryButton={true}
                            />
                          )}

                          {/* Submitted without feedback - h-12 matches Submit button height */}
                          {showFeedback && !(currentEntry.settings.showFeedback ?? true) && (
                            <div className="flex items-center justify-between gap-3 rounded-lg px-4 h-12 py-0 text-sm border-l-4 bg-blue-50 dark:bg-blue-950/20 text-blue-700 dark:text-blue-400 border-blue-500">
                              <span className="font-medium">Answer submitted.</span>
                              {currentEntry.settings.allowRetry ? (
                                <button
                                  type="button"
                                  onClick={resetQuiz}
                                  className="shrink-0 flex items-center gap-1.5 text-xs font-medium border border-blue-300 dark:border-blue-600 text-blue-700 dark:text-blue-400 hover:bg-blue-100 dark:hover:bg-blue-950/30 py-1.5 px-3 rounded-md transition-colors"
                                >
                                  <RotateCcw className="h-3 w-3" />
                                  Try Again
                                </button>
                              ) : (
                                <button
                                  type="button"
                                  onClick={resetQuiz}
                                  className="shrink-0 bg-gray-600 hover:bg-gray-700 text-white font-medium py-1.5 px-4 rounded-md transition-colors duration-200 text-sm"
                                >
                                  Reset Quiz
                                </button>
                              )}
                            </div>
                          )}
                        </div>
                      ) : (
                        <div className="space-y-4">
                          <div className="text-lg font-medium text-gray-400 dark:text-gray-500">Your question will appear here...</div>
                          <div className="text-sm text-gray-500 italic">Preview will update as you edit</div>
                        </div>
                      )}
                    </QuizWrapper>
                  </div>
                </div>
              </div>

              {/* Footer */}
              <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 rounded-b-lg">
                <div className="flex gap-2 justify-end">
                  <Button type="button" variant="outline" onClick={handleClose}>
                    Cancel
                  </Button>
                  <Button type="submit" className="flex items-center gap-2" disabled={!stem?.trim()}>
                    <Save className="h-4 w-4" />
                    Save Quiz
                  </Button>
                </div>
              </div>
            </form>
          </FormProvider>
        )}
    </BlockEditorShell>
  )
}
