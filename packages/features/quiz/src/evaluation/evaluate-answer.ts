import type { QuizAnswer } from "../answers/answers";
import type { QuizPracticeEntry } from "../contracts/contracts";
import { evaluateFormula, validateFormula } from "../formula/formula-expression";
import { FillBlankInputType, QuizEntryType, type FillBlankInput } from "../questions/question-types";

export type QuizEvaluationResult =
  | { status: "correct" }
  | { status: "incorrect"; reason?: string }
  | { status: "pending"; reason?: string }
  | { status: "unsupported"; reason?: string };

export interface QuizEvaluationContext {
  formulaPrompts?: readonly Record<string, number>[];
}

export function evaluateQuizAnswer(
  entry: QuizPracticeEntry,
  answer: QuizAnswer,
  context: QuizEvaluationContext = {},
): QuizEvaluationResult {
  if (entry.type !== answer.type) return incorrect("Answer type does not match question type");

  switch (entry.type) {
    case QuizEntryType.SingleChoice:
      return result(answer.type === entry.type && answer.optionId === entry.correctOptionId);

    case QuizEntryType.MultipleChoice:
      return result(
        answer.type === entry.type &&
        sameStringSet(answer.optionIds, entry.correctOptionIds) &&
        entry.correctOptionIds.length > 0,
      );

    case QuizEntryType.TrueFalse:
      return result(answer.type === entry.type && answer.value === entry.correctAnswer);

    case QuizEntryType.FillInTheBlank:
      return result(
        answer.type === entry.type &&
        entry.blanks.length > 0 &&
        entry.blanks.every((blank) => gradeBlank(blank.input, answer.values[blank.id] ?? "")),
      );

    case QuizEntryType.ShortAnswer:
      return result(
        answer.type === entry.type &&
        matchesAcceptedAnswer(answer.value, entry.acceptedAnswers, entry.caseSensitive === true),
      );

    case QuizEntryType.Essay: {
      if (answer.type !== entry.type) return incorrect();
      const expected = entry.correctAnswerPlain?.trim();
      if (!expected) return { status: "pending", reason: "Essay requires manual grading" };
      if (answer.plainText.trim().toLocaleLowerCase() !== expected.toLocaleLowerCase()) {
        return incorrect();
      }
      if (entry.requireFormatting) {
        return sameFormatting(entry.correctAnswer, answer.richText)
          ? { status: "correct" }
          : incorrect();
      }
      return { status: "correct" };
    }

    case QuizEntryType.Matching:
      return result(
        answer.type === entry.type &&
        entry.pairs.length > 0 &&
        Object.keys(answer.matches).length === entry.pairs.length &&
        entry.pairs.every((pair) => answer.matches[pair.id] === pair.right),
      );

    case QuizEntryType.Ordering: {
      if (answer.type !== entry.type) return incorrect();
      const expected = [...entry.items]
        .sort((left, right) => left.correctPosition - right.correctPosition)
        .map((item) => item.id);
      return result(sameStringArray(answer.itemIds, expected) && expected.length > 0);
    }

    case QuizEntryType.Categorization:
      return result(
        answer.type === entry.type &&
        entry.items.length > 0 &&
        entry.items.every((item) =>
          sameStringSet(answer.categoryIdsByItem[item.id] ?? [], item.correctCategoryIds),
        ),
      );

    case QuizEntryType.Rating:
      if (answer.type !== entry.type || answer.value === null) return incorrect();
      return entry.correctRating === undefined
        ? { status: "correct" }
        : result(answer.value === entry.correctRating);

    case QuizEntryType.Numeric: {
      if (answer.type !== entry.type) return incorrect();
      const prompt = context.formulaPrompts?.[0];
      if (!prompt) return { status: "unsupported", reason: "Numeric prompt is missing" };
      const value = Number.parseFloat(answer.value.trim());
      if (!Number.isFinite(value)) return incorrect();
      try {
        const expected = evaluateFormula(entry.formula, prompt);
        return result(withinTolerance(value, expected, entry.toleranceType, entry.tolerance));
      } catch {
        return incorrect("Formula evaluation failed");
      }
    }

    case QuizEntryType.Formula: {
      if (answer.type !== entry.type || !answer.expression.trim()) return incorrect();
      const variableNames = entry.variables.map((variable) => variable.name).filter(Boolean);
      if (validateFormula(answer.expression, variableNames)) return incorrect("Formula is invalid");
      const prompts = context.formulaPrompts ?? [];
      if (!prompts.length) return { status: "unsupported", reason: "Formula prompts are missing" };
      try {
        return result(prompts.every((prompt) => {
          const actual = evaluateFormula(answer.expression, prompt);
          const expected = evaluateFormula(entry.formula, prompt);
          return withinTolerance(actual, expected, entry.toleranceType, entry.tolerance);
        }));
      } catch {
        return incorrect("Formula evaluation failed");
      }
    }

    case QuizEntryType.Hotspot: {
      if (answer.type !== entry.type || !answer.point) return incorrect();
      const { x, y } = answer.point;
      return result(entry.hotspots.some((hotspot) => {
        const radius = Math.max(0, ...hotspot.zones.map((zone) => zone.radius));
        const dx = ((x - hotspot.x) / 100) * entry.imageWidth;
        const dy = ((y - hotspot.y) / 100) * entry.imageHeight;
        return Math.sqrt(dx * dx + dy * dy) <= (radius / 100) * entry.imageWidth;
      }));
    }

    case QuizEntryType.Highlight:
      return result(
        answer.type === entry.type &&
        answer.spans.length > 0 &&
        entry.highlights.length > 0 &&
        entry.highlights.every((expected) => answer.spans.some((span) => overlaps(span, expected))) &&
        answer.spans.every((span) => entry.highlights.some((expected) => overlaps(span, expected))),
      );
  }
}

function gradeBlank(
  input: FillBlankInput,
  rawValue: string,
): boolean {
  const value = rawValue.trim();
  if (!value) return false;
  switch (input.type) {
    case FillBlankInputType.Text:
      return matchesAcceptedAnswer(value, input.acceptedAnswers, input.caseSensitive === true);
    case FillBlankInputType.Number: {
      let numeric = value;
      if (input.unit) {
        numeric = numeric.replace(new RegExp(`\\s*${escapeRegExp(input.unit)}\\s*$`), "").trim();
        if (input.requireUnit && numeric === value) return false;
      }
      const parsed = Number.parseFloat(numeric);
      if (!Number.isFinite(parsed) || (!input.allowNegative && parsed < 0)) return false;
      if (input.requiredPrecision !== undefined) {
        const decimals = numeric.includes(".") ? numeric.split(".")[1]?.length ?? 0 : 0;
        if (decimals !== input.requiredPrecision) return false;
      }
      return Math.abs(parsed - input.correctValue) <= (input.tolerance ?? 0);
    }
    case FillBlankInputType.Dropdown:
      return value === input.options[0];
    case FillBlankInputType.WordBank:
      return value === input.words[0];
  }
}

function result(correct: boolean): QuizEvaluationResult {
  return correct ? { status: "correct" } : { status: "incorrect" };
}

function incorrect(reason?: string): QuizEvaluationResult {
  return reason ? { status: "incorrect", reason } : { status: "incorrect" };
}

function matchesAcceptedAnswer(value: string, accepted: readonly string[], caseSensitive: boolean): boolean {
  const normalized = value.trim();
  if (!normalized) return false;
  return accepted.some((candidate) => caseSensitive
    ? normalized === candidate.trim()
    : normalized.toLocaleLowerCase() === candidate.trim().toLocaleLowerCase());
}

function withinTolerance(
  value: number,
  expected: number,
  type: "absolute" | "percentage",
  tolerance: number,
): boolean {
  const threshold = type === "percentage" ? Math.abs(expected) * (tolerance / 100) : tolerance;
  return Math.abs(value - expected) <= threshold;
}

function sameFormatting(expected: unknown, actual: unknown): boolean {
  const normalize = (value: unknown): unknown => {
    if (!value || typeof value !== "object") return null;
    const root = (value as { root?: { children?: unknown[] } }).root;
    if (!Array.isArray(root?.children)) return null;
    const node = (current: unknown): unknown => {
      if (!current || typeof current !== "object") return null;
      const source = current as Record<string, unknown>;
      return {
        type: source.type,
        format: source.format,
        tag: source.tag,
        listType: source.listType,
        children: Array.isArray(source.children) ? source.children.map(node) : undefined,
      };
    };
    return root.children.map(node);
  };
  const left = normalize(expected);
  const right = normalize(actual);
  return left !== null && right !== null && JSON.stringify(left) === JSON.stringify(right);
}

function sameStringSet(left: readonly string[], right: readonly string[]): boolean {
  const leftSet = new Set(left);
  const rightSet = new Set(right);
  return leftSet.size === rightSet.size && Array.from(leftSet).every((value) => rightSet.has(value));
}

function sameStringArray(left: readonly string[], right: readonly string[]): boolean {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function overlaps(
  left: { start: number; end: number },
  right: { start: number; end: number },
): boolean {
  return left.start < right.end && left.end > right.start;
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
