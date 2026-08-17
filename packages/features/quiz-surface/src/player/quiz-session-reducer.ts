import { createEmptyQuizAnswer, type QuizAnswer, type QuizEntryType } from "@game-guild/quiz";

export type QuizSessionPhase = "answering" | "submitting" | "submitted";

export interface QuizSessionState {
  answer: QuizAnswer;
  promptVariables?: Record<string, number>;
  phase: QuizSessionPhase;
}

export type QuizSessionAction =
  | { type: "replace-answer"; answer: QuizAnswer; promptVariables?: Record<string, number> }
  | { type: "set-prompt-variables"; promptVariables: Record<string, number> }
  | { type: "submit" }
  | { type: "submitted" }
  | { type: "reset"; questionType: QuizEntryType };

export function createQuizSessionState(questionType: QuizEntryType): QuizSessionState {
  return { answer: createEmptyQuizAnswer(questionType), phase: "answering" };
}

export function quizSessionReducer(
  state: QuizSessionState,
  action: QuizSessionAction,
): QuizSessionState {
  switch (action.type) {
    case "replace-answer":
      return {
        ...state,
        answer: action.answer,
        promptVariables: action.promptVariables ?? state.promptVariables,
      };
    case "set-prompt-variables":
      return { ...state, promptVariables: action.promptVariables };
    case "submit":
      return { ...state, phase: "submitting" };
    case "submitted":
      return { ...state, phase: "submitted" };
    case "reset":
      return createQuizSessionState(action.questionType);
  }
}
