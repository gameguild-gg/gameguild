"use client";

import { useCallback, useEffect, useState } from "react";
import {
  DEFAULT_QUIZ_EDITOR_MODAL_SIZE,
  getQuizEditorModalSize,
  setQuizEditorModalSize,
  subscribeToQuizEditorPreferences,
  type QuizEditorModalSize,
} from "./quiz-editor-preferences";

export interface QuizEditorSettings {
  modalSize: QuizEditorModalSize;
  setModalSize: (modalSize: QuizEditorModalSize) => Promise<void>;
}

export function useQuizEditorSettings(): QuizEditorSettings {
  const [modalSize, setModalSizeState] = useState<QuizEditorModalSize>(
    DEFAULT_QUIZ_EDITOR_MODAL_SIZE,
  );

  useEffect(() => {
    let active = true;
    const load = async () => {
      const next = await getQuizEditorModalSize();
      if (active) setModalSizeState(next);
    };
    void load();
    const unsubscribe = subscribeToQuizEditorPreferences(() => void load());
    return () => {
      active = false;
      unsubscribe();
    };
  }, []);

  const setModalSize = useCallback(async (next: QuizEditorModalSize) => {
    setModalSizeState(next);
    await setQuizEditorModalSize(next);
  }, []);

  return { modalSize, setModalSize };
}
