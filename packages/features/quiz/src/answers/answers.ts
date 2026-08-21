import {
  QuizEntryType,
  type HighlightSpan,
  type SerializedRichTextPayload,
} from "../questions/question-types";

export interface SingleChoiceAnswer {
  type: QuizEntryType.SingleChoice;
  optionId: string | null;
}

export interface MultipleChoiceAnswer {
  type: QuizEntryType.MultipleChoice;
  optionIds: string[];
}

export interface TrueFalseAnswer {
  type: QuizEntryType.TrueFalse;
  value: boolean | null;
}

export interface FillInTheBlankAnswer {
  type: QuizEntryType.FillInTheBlank;
  values: Record<string, string>;
}

export interface ShortAnswer {
  type: QuizEntryType.ShortAnswer;
  value: string;
}

export interface EssayAnswer {
  type: QuizEntryType.Essay;
  richText: SerializedRichTextPayload;
  plainText: string;
}

export interface MatchingAnswer {
  type: QuizEntryType.Matching;
  matches: Record<string, string>;
}

export interface OrderingAnswer {
  type: QuizEntryType.Ordering;
  itemIds: string[];
}

export interface CategorizationAnswer {
  type: QuizEntryType.Categorization;
  categoryIdsByItem: Record<string, string[]>;
}

export interface RatingAnswer {
  type: QuizEntryType.Rating;
  value: number | null;
}

export interface NumericAnswer {
  type: QuizEntryType.Numeric;
  value: string;
}

export interface FormulaAnswer {
  type: QuizEntryType.Formula;
  expression: string;
}

export interface HotspotAnswer {
  type: QuizEntryType.Hotspot;
  point: { x: number; y: number } | null;
}

export interface HighlightAnswer {
  type: QuizEntryType.Highlight;
  spans: HighlightSpan[];
}

export type QuizAnswer =
  | SingleChoiceAnswer
  | MultipleChoiceAnswer
  | TrueFalseAnswer
  | FillInTheBlankAnswer
  | ShortAnswer
  | EssayAnswer
  | MatchingAnswer
  | OrderingAnswer
  | CategorizationAnswer
  | RatingAnswer
  | NumericAnswer
  | FormulaAnswer
  | HotspotAnswer
  | HighlightAnswer;

export interface QuizStructuredAnswer {
  selectedOptionIds?: string[];
  textAnswers?: Record<string, string>;
  categorizations?: Record<string, string[]>;
  ordering?: string[];
  rating?: number;
}

export function createEmptyQuizAnswer(type: QuizEntryType): QuizAnswer {
  switch (type) {
    case QuizEntryType.SingleChoice:
      return { type, optionId: null };
    case QuizEntryType.MultipleChoice:
      return { type, optionIds: [] };
    case QuizEntryType.TrueFalse:
      return { type, value: null };
    case QuizEntryType.FillInTheBlank:
      return { type, values: {} };
    case QuizEntryType.ShortAnswer:
      return { type, value: "" };
    case QuizEntryType.Essay:
      return { type, richText: null, plainText: "" };
    case QuizEntryType.Matching:
      return { type, matches: {} };
    case QuizEntryType.Ordering:
      return { type, itemIds: [] };
    case QuizEntryType.Categorization:
      return { type, categoryIdsByItem: {} };
    case QuizEntryType.Rating:
      return { type, value: null };
    case QuizEntryType.Numeric:
      return { type, value: "" };
    case QuizEntryType.Formula:
      return { type, expression: "" };
    case QuizEntryType.Hotspot:
      return { type, point: null };
    case QuizEntryType.Highlight:
      return { type, spans: [] };
  }
}

export function toStructuredGradingAnswer(answer: QuizAnswer): QuizStructuredAnswer {
  switch (answer.type) {
    case QuizEntryType.SingleChoice:
      return answer.optionId ? { selectedOptionIds: [answer.optionId] } : {};
    case QuizEntryType.MultipleChoice:
      return answer.optionIds.length ? { selectedOptionIds: [...answer.optionIds] } : {};
    case QuizEntryType.TrueFalse:
      return answer.value === null
        ? {}
        : { selectedOptionIds: [answer.value ? "true" : "false"] };
    case QuizEntryType.FillInTheBlank:
      return nonEmptyRecord(answer.values, "textAnswers");
    case QuizEntryType.ShortAnswer:
      return answer.value ? { textAnswers: { main: answer.value } } : {};
    case QuizEntryType.Essay:
      return {
        textAnswers: {
          main: answer.richText ? JSON.stringify(answer.richText) : "",
          main_plain: answer.plainText,
        },
      };
    case QuizEntryType.Matching:
      return {
        selectedOptionIds: Object.entries(answer.matches).map(([left, right]) => `${left}:${right}`),
      };
    case QuizEntryType.Ordering:
      return answer.itemIds.length ? { ordering: [...answer.itemIds] } : {};
    case QuizEntryType.Categorization:
      return nonEmptyRecord(answer.categoryIdsByItem, "categorizations");
    case QuizEntryType.Rating:
      return answer.value === null ? {} : { rating: answer.value };
    case QuizEntryType.Numeric:
      return answer.value ? { textAnswers: { main: answer.value } } : {};
    case QuizEntryType.Formula:
      return answer.expression ? { textAnswers: { main: answer.expression } } : {};
    case QuizEntryType.Hotspot:
      return answer.point
        ? {
            textAnswers: {
              hotspot_x: String(answer.point.x),
              hotspot_y: String(answer.point.y),
            },
          }
        : {};
    case QuizEntryType.Highlight:
      return answer.spans.length
        ? { textAnswers: { highlight_spans: JSON.stringify(answer.spans) } }
        : {};
  }
}

export function fromStructuredGradingAnswer(
  type: QuizEntryType,
  answer: QuizStructuredAnswer,
): QuizAnswer {
  switch (type) {
    case QuizEntryType.SingleChoice:
      return { type, optionId: answer.selectedOptionIds?.[0] ?? null };
    case QuizEntryType.MultipleChoice:
      return { type, optionIds: [...(answer.selectedOptionIds ?? [])] };
    case QuizEntryType.TrueFalse: {
      const value = answer.selectedOptionIds?.[0];
      return { type, value: value === "true" ? true : value === "false" ? false : null };
    }
    case QuizEntryType.FillInTheBlank:
      return { type, values: { ...answer.textAnswers } };
    case QuizEntryType.ShortAnswer:
      return { type, value: answer.textAnswers?.main ?? "" };
    case QuizEntryType.Essay:
      return {
        type,
        richText: parseRichText(answer.textAnswers?.main),
        plainText: answer.textAnswers?.main_plain ?? "",
      };
    case QuizEntryType.Matching:
      return {
        type,
        matches: Object.fromEntries(
          (answer.selectedOptionIds ?? []).flatMap((value) => {
            const separator = value.indexOf(":");
            return separator > 0 ? [[value.slice(0, separator), value.slice(separator + 1)]] : [];
          }),
        ),
      };
    case QuizEntryType.Ordering:
      return { type, itemIds: [...(answer.ordering ?? [])] };
    case QuizEntryType.Categorization:
      return {
        type,
        categoryIdsByItem: Object.fromEntries(
          Object.entries(answer.categorizations ?? {}).map(([key, value]) => [key, [...value]]),
        ),
      };
    case QuizEntryType.Rating:
      return { type, value: answer.rating ?? null };
    case QuizEntryType.Numeric:
      return { type, value: answer.textAnswers?.main ?? "" };
    case QuizEntryType.Formula:
      return { type, expression: answer.textAnswers?.main ?? "" };
    case QuizEntryType.Hotspot: {
      const x = Number(answer.textAnswers?.hotspot_x);
      const y = Number(answer.textAnswers?.hotspot_y);
      return { type, point: Number.isFinite(x) && Number.isFinite(y) ? { x, y } : null };
    }
    case QuizEntryType.Highlight:
      return { type, spans: parseHighlightSpans(answer.textAnswers?.highlight_spans) };
  }
}

export function normalizeQuizAnswer(type: QuizEntryType, value: unknown): QuizAnswer {
  if (!value || typeof value !== "object" || (value as { type?: unknown }).type !== type) {
    return createEmptyQuizAnswer(type);
  }

  const candidate = value as Record<string, unknown>;
  switch (type) {
    case QuizEntryType.SingleChoice:
      return { type, optionId: stringOrNull(candidate.optionId) };
    case QuizEntryType.MultipleChoice:
      return { type, optionIds: stringArray(candidate.optionIds) };
    case QuizEntryType.TrueFalse:
      return { type, value: typeof candidate.value === "boolean" ? candidate.value : null };
    case QuizEntryType.FillInTheBlank:
      return { type, values: stringRecord(candidate.values) };
    case QuizEntryType.ShortAnswer:
      return { type, value: stringOrEmpty(candidate.value) };
    case QuizEntryType.Essay:
      return {
        type,
        richText: objectOrNull(candidate.richText),
        plainText: stringOrEmpty(candidate.plainText),
      };
    case QuizEntryType.Matching:
      return { type, matches: stringRecord(candidate.matches) };
    case QuizEntryType.Ordering:
      return { type, itemIds: stringArray(candidate.itemIds) };
    case QuizEntryType.Categorization:
      return { type, categoryIdsByItem: stringArrayRecord(candidate.categoryIdsByItem) };
    case QuizEntryType.Rating:
      return { type, value: finiteNumberOrNull(candidate.value) };
    case QuizEntryType.Numeric:
      return { type, value: stringOrEmpty(candidate.value) };
    case QuizEntryType.Formula:
      return { type, expression: stringOrEmpty(candidate.expression) };
    case QuizEntryType.Hotspot:
      return { type, point: pointOrNull(candidate.point) };
    case QuizEntryType.Highlight:
      return { type, spans: highlightSpans(candidate.spans) };
  }
}

function nonEmptyRecord(
  value: Record<string, string> | Record<string, string[]>,
  key: "textAnswers" | "categorizations",
): QuizStructuredAnswer {
  if (!Object.keys(value).length) return {};
  return key === "textAnswers"
    ? { textAnswers: value as Record<string, string> }
    : { categorizations: value as Record<string, string[]> };
}

function parseRichText(value: string | undefined): SerializedRichTextPayload {
  if (!value) return null;
  try {
    return objectOrNull(JSON.parse(value));
  } catch {
    return null;
  }
}

function parseHighlightSpans(value: string | undefined): HighlightSpan[] {
  if (!value) return [];
  try {
    return highlightSpans(JSON.parse(value));
  } catch {
    return [];
  }
}

function stringOrEmpty(value: unknown): string {
  return typeof value === "string" ? value : "";
}

function stringOrNull(value: unknown): string | null {
  return typeof value === "string" ? value : null;
}

function stringArray(value: unknown): string[] {
  return Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : [];
}

function stringRecord(value: unknown): Record<string, string> {
  if (!value || typeof value !== "object" || Array.isArray(value)) return {};
  return Object.fromEntries(
    Object.entries(value).filter((entry): entry is [string, string] => typeof entry[1] === "string"),
  );
}

function stringArrayRecord(value: unknown): Record<string, string[]> {
  if (!value || typeof value !== "object" || Array.isArray(value)) return {};
  return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, stringArray(item)]));
}

function objectOrNull(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : null;
}

function finiteNumberOrNull(value: unknown): number | null {
  return typeof value === "number" && Number.isFinite(value) ? value : null;
}

function pointOrNull(value: unknown): HotspotAnswer["point"] {
  if (!value || typeof value !== "object") return null;
  const point = value as { x?: unknown; y?: unknown };
  return typeof point.x === "number" && Number.isFinite(point.x) &&
    typeof point.y === "number" && Number.isFinite(point.y)
    ? { x: point.x, y: point.y }
    : null;
}

function highlightSpans(value: unknown): HighlightSpan[] {
  if (!Array.isArray(value)) return [];
  return value.flatMap((span) => {
    if (!span || typeof span !== "object") return [];
    const { start, end } = span as { start?: unknown; end?: unknown };
    return typeof start === "number" && Number.isFinite(start) &&
      typeof end === "number" && Number.isFinite(end) && start >= 0 && end > start
      ? [{ start, end }]
      : [];
  });
}
