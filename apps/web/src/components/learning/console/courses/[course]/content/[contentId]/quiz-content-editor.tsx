"use client";

import { useCallback, useMemo, useState } from "react";
import { QuizCollectionEditor } from "@game-guild/quiz-surface/editor";
import type { ContentGradingDefinition } from "@game-guild/grading";
import { sumGradedItemPoints } from "@game-guild/grading";
import {
  disableQuizContentGrading,
  enableQuizContentGrading,
  parseQuizContentDocument,
  quizContentItemsToDocument,
  quizDocumentToContentItems,
  readQuizContentGrading,
  serializeQuizContentDocument,
  updateQuizContentGrading,
  type QuizContentDocument,
  type QuizContentItem,
} from "@game-guild/quiz-content";
import { Badge } from "@game-guild/ui/components/badge";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import { Switch } from "@game-guild/ui/components/switch";

type QuizEditorMode = "edit" | "preview";

interface QuizContentEditorProps {
  initialContent: Record<string, unknown> | null | undefined;
  onChange: (content: Record<string, unknown>) => void;
  mode?: QuizEditorMode;
}

export function QuizContentEditor({
  initialContent,
  onChange,
  mode = "edit",
}: QuizContentEditorProps) {
  const initialDocument = useMemo(
    () => parseQuizContentDocument(initialContent).document,
    [initialContent],
  );
  const [document, setDocument] = useState<QuizContentDocument>(initialDocument);
  const quizItems = useMemo(
    () => quizDocumentToContentItems(document),
    [document],
  );
  const gradingConfig = useMemo(
    () => readQuizContentGrading(document),
    [document],
  );
  const gradedItemCount = Object.keys(gradingConfig.items).length;
  const gradedPoints = sumGradedItemPoints(gradingConfig);

  const commitDocument = useCallback(
    (nextDocument: QuizContentDocument) => {
      const serialized = serializeQuizContentDocument(nextDocument);
      setDocument(serialized);
      onChange({ ...serialized });
    },
    [onChange],
  );

  const handleQuizItemsChange = useCallback(
    (items: QuizContentItem[]) => {
      commitDocument(
        quizContentItemsToDocument({ items, grading: document.grading }),
      );
    },
    [commitDocument, document.grading],
  );

  const updateGrading = useCallback(
    (
      updater: (current: ContentGradingDefinition) => ContentGradingDefinition,
    ) => {
      commitDocument(updateQuizContentGrading(document, updater));
    },
    [commitDocument, document],
  );

  const handleGradingEnabledChange = useCallback(
    (enabled: boolean) => {
      commitDocument(
        enabled
          ? enableQuizContentGrading(document)
          : disableQuizContentGrading(document),
      );
    },
    [commitDocument, document],
  );

  const handleMaxScoreChange = useCallback(
    (value: string) => {
      const maxScore = Math.max(1, Number(value) || 1);
      updateGrading((current) => {
        return {
          ...current,
          score: {
            ...current.score,
            maxScore,
            passingScore:
              current.score.passingScore === undefined
                ? undefined
                : Math.min(current.score.passingScore, maxScore),
          },
        };
      });
    },
    [updateGrading],
  );

  const handlePassingScoreChange = useCallback(
    (value: string) => {
      const passingScore = value.trim()
        ? Math.max(0, Number(value) || 0)
        : undefined;
      updateGrading((current) => {
        return {
          ...current,
          score: {
            ...current.score,
            passingScore:
              passingScore === undefined
                ? undefined
                : Math.min(passingScore, current.score.maxScore),
          },
        };
      });
    },
    [updateGrading],
  );

  if (mode === "preview") {
    return (
      <QuizCollectionEditor
        items={quizItems}
        onChange={handleQuizItemsChange}
        submissionMode={
          gradingConfig.enabled ? "server-graded" : "local-practice"
        }
        readOnly={true}
      />
    );
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label>Quiz editor</Label>
        <p className="text-muted-foreground text-xs">
          Use the quiz block editor to build your questions.
        </p>
      </div>

      <div className="rounded-lg border border-gray-200 p-4 dark:border-gray-700">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="space-y-1">
            <Label htmlFor="quiz-grading-enabled">Grading</Label>
            <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
              <Badge
                variant={gradingConfig.enabled ? "secondary" : "outline"}
              >
                {gradingConfig.enabled
                  ? `${gradedItemCount} items`
                  : "Off"}
              </Badge>
              {gradingConfig.enabled && (
                <span>{gradedPoints} configured pts</span>
              )}
            </div>
          </div>
          <Switch
            id="quiz-grading-enabled"
            checked={gradingConfig.enabled}
            onCheckedChange={handleGradingEnabledChange}
          />
        </div>

        {gradingConfig.enabled && (
          <div className="mt-4 grid gap-3 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="quiz-max-score">Max score</Label>
              <Input
                id="quiz-max-score"
                type="number"
                min={1}
                value={gradingConfig.score.maxScore}
                onChange={(event) =>
                  handleMaxScoreChange(event.currentTarget.value)
                }
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="quiz-passing-score">Passing score</Label>
              <Input
                id="quiz-passing-score"
                type="number"
                min={0}
                value={gradingConfig.score.passingScore ?? ""}
                onChange={(event) =>
                  handlePassingScoreChange(event.currentTarget.value)
                }
              />
            </div>
          </div>
        )}

        {gradingConfig.enabled && (
          <div className="mt-3 flex flex-wrap gap-2">
            <Badge variant="outline">Assessment</Badge>
          </div>
        )}
      </div>

      <QuizCollectionEditor
        items={quizItems}
        onChange={handleQuizItemsChange}
        submissionMode={
          gradingConfig.enabled ? "server-graded" : "local-practice"
        }
      />
    </div>
  );
}
