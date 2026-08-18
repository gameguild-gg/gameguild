"use client";

import { useEffect, useMemo, useState } from "react";
import {
  QuizEntryType,
  createEmptyQuizAnswer,
  toQuizLearnerEntry,
  validateQuizAuthoringEntry,
  type QuizAnswer,
  type QuizEntry,
} from "@game-guild/quiz";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import { Switch } from "@game-guild/ui/components/switch";
import { Textarea } from "@game-guild/ui/components/textarea";
import { FormProvider, useForm } from "react-hook-form";
import { FileText, Save, Users } from "lucide-react";
import { QuizPlayer, type QuizSubmissionResult } from "../player/quiz-player";
import { QuizPracticePlayer } from "../player/quiz-practice-player";
import { QuizWrapper } from "../shared/quiz-wrapper";
import { QuizAssetsBoundary } from "../shared/quiz-assets-boundary";
import { quizEditorRegistry, quizQuestionLabels } from "./editor-registry";
import { QuizTypeSelector } from "./question-type-selector";

export interface QuizEditorSurfaceProps {
  value: QuizEntry;
  onChange: (entry: QuizEntry) => void;
  onCommit: (entry: QuizEntry) => void;
  onCancel: () => void;
  submissionMode?: "local-practice" | "server-graded";
}

export function QuizEditorSurface({
  value,
  onChange,
  onCommit,
  onCancel,
  submissionMode = "local-practice",
}: QuizEditorSurfaceProps) {
  const [selectingType, setSelectingType] = useState(!value.stem);
  const [previewAnswer, setPreviewAnswer] = useState<QuizAnswer>(() => createEmptyQuizAnswer(value.type));
  const [previewRevision, setPreviewRevision] = useState(0);
  const [submissionResult, setSubmissionResult] =
    useState<QuizSubmissionResult>({ status: "idle" });
  const form = useForm<QuizEntry>({ defaultValues: value });
  const currentEntry = form.watch() as QuizEntry;
  const Editor = quizEditorRegistry[currentEntry.type];
  const issues = useMemo(() => validateQuizAuthoringEntry(currentEntry), [currentEntry]);

  useEffect(() => {
    const subscription = form.watch((next) => {
      const nextEntry = next as QuizEntry;
      onChange(nextEntry);
      setPreviewAnswer(createEmptyQuizAnswer(nextEntry.type));
      setSubmissionResult({ status: "idle" });
      setPreviewRevision((current) => current + 1);
    });
    return () => subscription.unsubscribe();
  }, [form, onChange]);

  const selectType = (entry: QuizEntry) => {
    form.reset(entry);
    setPreviewAnswer(createEmptyQuizAnswer(entry.type));
    setSubmissionResult({ status: "idle" });
    setPreviewRevision((current) => current + 1);
    setSelectingType(false);
    onChange(entry);
  };

  return (
    <QuizAssetsBoundary>
      <div className="flex min-h-0 flex-1 flex-col bg-background text-foreground">
        {selectingType ? (
          <div className="flex-1 overflow-y-auto">
            <QuizTypeSelector
              onSelect={selectType}
              onCancel={() => {
                if (value.stem) {
                  setSelectingType(false);
                } else {
                  onCancel();
                }
              }}
            />
          </div>
        ) : (
          <FormProvider {...form}>
            <form
              className="flex min-h-0 flex-1 flex-col"
              onSubmit={form.handleSubmit((entry) => onCommit(entry))}
            >
              <div className="flex shrink-0 items-center gap-4 border-b border-gray-200 bg-gray-50 p-4 dark:border-gray-800 dark:bg-gray-900">
                <span className="rounded bg-gray-100 px-2 py-1 text-sm text-gray-600 dark:bg-gray-800 dark:text-gray-400">
                  Type:{" "}
                  <span className="font-medium text-gray-800 dark:text-gray-200">
                    {quizQuestionLabels[currentEntry.type]}
                  </span>
                </span>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="border-gray-300 dark:border-gray-600"
                  onClick={() => setSelectingType(true)}
                >
                  Change type
                </Button>
              </div>

              <div className="grid min-h-0 flex-1 grid-rows-2 lg:grid-cols-2 lg:grid-rows-1">
                <section className="flex min-h-0 flex-col border-b border-gray-200 bg-white lg:border-r lg:border-b-0 dark:border-gray-800 dark:bg-gray-900">
                  <header className="flex shrink-0 items-center gap-2 border-b border-gray-200 bg-gray-50 p-4 font-medium text-gray-800 dark:border-gray-800 dark:bg-gray-900 dark:text-gray-200">
                    <FileText className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    Configuration
                  </header>
                  <div className="flex-1 space-y-6 overflow-y-auto bg-white p-6 dark:bg-gray-950">
                    <div className="space-y-2">
                      <Label htmlFor="quiz-stem" className="text-sm font-medium">
                        Question
                      </Label>
                      <Textarea
                        id="quiz-stem"
                        rows={3}
                        className="resize-none"
                        placeholder="Enter your question here..."
                        {...form.register("stem")}
                      />
                    </div>

                    <Editor />

                    <div className="space-y-4">
                      <Label className="text-sm font-medium">Feedback messages</Label>
                      <div className="space-y-3">
                        <div>
                          <Label className="text-xs text-gray-600 dark:text-gray-400">
                            Correct answer feedback
                          </Label>
                          <Input
                            className="mt-1"
                            placeholder="Great job! That's correct!"
                            {...form.register("feedback.correct")}
                          />
                        </div>
                        <div>
                          <Label className="text-xs text-gray-600 dark:text-gray-400">
                            Incorrect answer feedback
                          </Label>
                          <Input
                            className="mt-1"
                            placeholder="Not quite right. Try again!"
                            {...form.register("feedback.incorrect")}
                          />
                        </div>
                      </div>
                    </div>

                    <div className="space-y-3">
                      <Label className="text-sm font-medium">Settings</Label>
                      <SettingToggle
                        label="Show feedback"
                        description="Show correct or incorrect result after submission"
                        checked={currentEntry.settings.showFeedback ?? true}
                        onCheckedChange={(checked) => {
                          form.setValue("settings.showFeedback", checked);
                          form.setValue("settings.showCorrectAnswer", checked);
                        }}
                      />
                      <SettingToggle
                        label="Show correct answer"
                        description="Reveal the correct answer after submission"
                        checked={currentEntry.settings.showCorrectAnswer ?? true}
                        onCheckedChange={(checked) => form.setValue("settings.showCorrectAnswer", checked)}
                      />
                      <SettingToggle
                        label="Allow retry"
                        checked={currentEntry.settings.allowRetry}
                        onCheckedChange={(checked) => form.setValue("settings.allowRetry", checked)}
                      />
                    </div>
                  </div>
                </section>

                <section className="flex min-h-0 flex-col bg-white dark:bg-gray-900">
                  <header className="flex shrink-0 items-center gap-2 border-b border-gray-200 bg-gray-50 p-4 font-medium text-gray-800 dark:border-gray-800 dark:bg-gray-900 dark:text-gray-200">
                    <Users className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    Live preview
                  </header>
                  <div className="flex-1 overflow-y-auto bg-white p-4 dark:bg-gray-950">
                    <QuizWrapper>
                      {currentEntry.stem ? (
                        submissionMode === "server-graded" ? (
                          <QuizPlayer
                            key={`server-${previewRevision}`}
                            entry={toQuizLearnerEntry(currentEntry)}
                            answer={previewAnswer.type === currentEntry.type
                              ? previewAnswer
                              : createEmptyQuizAnswer(currentEntry.type)}
                            onAnswerChange={setPreviewAnswer}
                            onSubmit={() =>
                              setSubmissionResult({ status: "pending" })
                            }
                            submissionResult={submissionResult}
                          />
                        ) : (
                          <QuizPracticePlayer
                            key={`practice-${previewRevision}`}
                            entry={currentEntry}
                            answer={previewAnswer.type === currentEntry.type
                              ? previewAnswer
                              : createEmptyQuizAnswer(currentEntry.type)}
                            onAnswerChange={setPreviewAnswer}
                          />
                        )
                      ) : (
                        <div className="space-y-2 py-10 text-center">
                          <div className="text-lg font-medium text-gray-400 dark:text-gray-500">
                            Your question will appear here...
                          </div>
                          <div className="text-sm text-gray-500 italic">
                            Preview updates while you edit
                          </div>
                        </div>
                      )}
                    </QuizWrapper>
                  </div>
                </section>
              </div>

              <div className="flex shrink-0 items-center justify-between gap-3 border-t border-gray-200 bg-gray-50 p-4 dark:border-gray-800 dark:bg-gray-900">
                <div className="text-sm text-destructive" aria-live="polite">
                  {issues[0]?.message}
                </div>
                <div className="flex gap-2">
                  <Button type="button" variant="outline" onClick={onCancel}>Cancel</Button>
                  <Button type="submit" disabled={!currentEntry.stem.trim() || issues.length > 0}>
                    <Save className="h-4 w-4" />
                    Save quiz
                  </Button>
                </div>
              </div>
            </form>
          </FormProvider>
        )}
      </div>
    </QuizAssetsBoundary>
  );
}

function SettingToggle({
  label,
  description,
  checked,
  onCheckedChange,
}: {
  label: string;
  description?: string;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
}) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-lg border bg-gray-50 p-3 dark:bg-gray-800/50">
      <div className="min-w-0">
        <Label className="text-sm">{label}</Label>
        {description && (
          <p className="text-xs text-gray-500 dark:text-gray-400">
            {description}
          </p>
        )}
      </div>
      <Switch checked={checked} onCheckedChange={onCheckedChange} />
    </div>
  );
}
