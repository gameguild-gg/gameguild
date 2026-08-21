"use client";

import { useEffect, useReducer } from "react";
import type { QuizEntryType } from "@game-guild/quiz";
import {
  createQuizSessionState,
  quizSessionReducer,
} from "./quiz-session-reducer";

export function useQuizSession(questionType: QuizEntryType) {
  const [state, dispatch] = useReducer(quizSessionReducer, questionType, createQuizSessionState);

  useEffect(() => {
    if (state.answer.type !== questionType) dispatch({ type: "reset", questionType });
  }, [questionType, state.answer.type]);

  return { state, dispatch };
}
