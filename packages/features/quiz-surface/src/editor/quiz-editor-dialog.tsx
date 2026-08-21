"use client";

import { useCallback, useState } from "react";
import type { QuizEntry } from "@game-guild/quiz";
import { BookOpen } from "lucide-react";
import { QuizEditorShell } from "./chrome/quiz-editor-shell";
import {
  useQuizEditorSettings,
  type QuizEditorSettings,
} from "./chrome/use-quiz-editor-settings";
import { QuizEditorSurface } from "./quiz-editor-surface";

export interface QuizEditorDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  value: QuizEntry;
  onCommit: (entry: QuizEntry) => void;
  submissionMode?: "local-practice" | "server-graded";
}

export function QuizEditorDialog({
  open,
  onOpenChange,
  value,
  onCommit,
  submissionMode = "local-practice",
}: QuizEditorDialogProps) {
  const settings = useQuizEditorSettings();

  if (!open) return null;
  return (
    <QuizEditorSession
      value={value}
      onOpenChange={onOpenChange}
      onCommit={onCommit}
      submissionMode={submissionMode}
      settings={settings}
    />
  );
}

function QuizEditorSession({
  value,
  onOpenChange,
  onCommit,
  submissionMode,
  settings,
}: Omit<QuizEditorDialogProps, "open"> & {
  settings: QuizEditorSettings;
}) {
  const [draft, setDraft] = useState(value);
  const close = useCallback(() => onOpenChange(false), [onOpenChange]);

  return (
    <QuizEditorShell
      title="Quiz Builder"
      icon={<BookOpen className="h-5 w-5 text-blue-600 dark:text-blue-400" />}
      settings={settings}
      onClose={close}
    >
      <QuizEditorSurface
        value={draft}
        onChange={setDraft}
        onCancel={close}
        submissionMode={submissionMode}
        onCommit={(entry) => {
          onCommit(entry);
          close();
        }}
      />
    </QuizEditorShell>
  );
}
