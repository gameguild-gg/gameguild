"use client";

import { useCallback, useMemo, useState } from "react";
import { QuizCollectionEditor } from "@game-guild/quiz-surface/editor";
import { sumQuizItemPoints } from "@game-guild/grading-adapter-quiz";
import {
  disableQuizContentGrading,
  enableQuizContentGrading,
  parseQuizContentDocument,
  quizContentItemsToDocument,
  quizDocumentToContentItems,
  quizDocumentToGradingItems,
  readQuizContentGrading,
  serializeQuizContentDocument,
  type QuizContentDocument,
  type QuizContentItem,
} from "@game-guild/quiz-content";
import { Badge } from "@game-guild/ui/components/badge";
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
  const gradingEnabled = gradingConfig !== null;
  const gradedItemCount = gradingConfig ? Object.keys(gradingConfig.items).length : 0;
  const gradedPoints = useMemo(
    () => sumQuizItemPoints(quizDocumentToGradingItems(document)),
    [document],
  );

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

  if (mode === "preview") {
    return (
      <QuizCollectionEditor
        items={quizItems}
        onChange={handleQuizItemsChange}
        submissionMode={
          gradingEnabled ? "server-graded" : "local-practice"
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
                variant={gradingEnabled ? "secondary" : "outline"}
              >
                {gradingEnabled
                  ? `${gradedItemCount} items`
                  : "Off"}
              </Badge>
              {gradingEnabled && (
                <span>{gradedPoints} configured pts</span>
              )}
            </div>
          </div>
          <Switch
            id="quiz-grading-enabled"
            checked={gradingEnabled}
            onCheckedChange={handleGradingEnabledChange}
          />
        </div>

        {gradingEnabled && (
          <div className="mt-3 flex flex-wrap gap-2">
            <Badge variant="outline">Assessment</Badge>
          </div>
        )}
      </div>

      <QuizCollectionEditor
        items={quizItems}
        onChange={handleQuizItemsChange}
        submissionMode={
          gradingEnabled ? "server-graded" : "local-practice"
        }
      />
    </div>
  );
}
