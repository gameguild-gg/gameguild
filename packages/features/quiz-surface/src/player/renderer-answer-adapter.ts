import {
  QuizEntryType,
  type QuizAnswer,
  type QuizStructuredAnswer,
  fromStructuredGradingAnswer,
  toStructuredGradingAnswer,
} from "@game-guild/quiz";

/** Internal adapter for the existing question controls. Public state remains QuizAnswer. */
export interface RendererAnswerState {
  selectedOptionIds: string[];
  textAnswers: Record<string, string>;
  categorizations: Record<string, string[]>;
  ordering: string[];
  rating?: number;
}

export function toRendererAnswerState(
  answer: QuizAnswer,
  promptVariables?: Record<string, number>,
): RendererAnswerState {
  const structured = toStructuredGradingAnswer(answer);
  return {
    selectedOptionIds: structured.selectedOptionIds ?? [],
    textAnswers: {
      ...(structured.textAnswers ?? {}),
      ...(promptVariables ? { formula_values: JSON.stringify(promptVariables) } : {}),
    },
    categorizations: structured.categorizations ?? {},
    ordering: structured.ordering ?? [],
    ...(structured.rating === undefined ? {} : { rating: structured.rating }),
  };
}

export function applyRendererAnswerUpdate(
  answer: QuizAnswer,
  update: Partial<RendererAnswerState>,
): { answer: QuizAnswer; promptVariables?: Record<string, number> } {
  const current = toStructuredGradingAnswer(answer);
  const structured: QuizStructuredAnswer = {
    selectedOptionIds: update.selectedOptionIds ?? current.selectedOptionIds,
    textAnswers: update.textAnswers ?? current.textAnswers,
    categorizations: update.categorizations ?? current.categorizations,
    ordering: update.ordering ?? current.ordering,
    rating: update.rating ?? current.rating,
  };
  return {
    answer: fromStructuredGradingAnswer(answer.type, structured),
    promptVariables: parsePromptVariables(update.textAnswers?.formula_values),
  };
}

export function isAnswerForType(answer: QuizAnswer, type: QuizEntryType): boolean {
  return answer.type === type;
}

function parsePromptVariables(value: string | undefined): Record<string, number> | undefined {
  if (!value) return undefined;
  try {
    const parsed = JSON.parse(value) as unknown;
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) return undefined;
    const entries = Object.entries(parsed).filter(
      (entry): entry is [string, number] => typeof entry[1] === "number" && Number.isFinite(entry[1]),
    );
    return entries.length ? Object.fromEntries(entries) : undefined;
  } catch {
    return undefined;
  }
}
