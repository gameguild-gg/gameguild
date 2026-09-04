import {
  addScoreValues,
  scoreValueByRatio,
  ZERO_SCORE_VALUE,
  type GradeItemResultV1,
  type GradeResultV1,
  type ScoreValue,
} from "@game-guild/grading";
import {
  evaluateQuizAnswer,
  QuizEntryType,
  type QuizAnswer,
  type QuizEvaluationContext,
} from "@game-guild/quiz";
import {
  QUIZ_AUTOMATED_REVIEW_HANDLER,
  QUIZ_DETERMINISTIC_ALGORITHM,
  type QuizGradingItemInputV1,
} from "./contracts";
import {
  classifyQuizReviewCapability,
  getQuizItemMaxScore,
  sumQuizItemPoints,
} from "./items";

export function evaluateDeterministicQuizItem(
  item: QuizGradingItemInputV1,
  answer: QuizAnswer | undefined,
  context: QuizEvaluationContext = {},
): GradeItemResultV1 {
  const maxScore = getQuizItemMaxScore(item.entry);
  if (classifyQuizReviewCapability(item.entry) !== "automated-review") {
    return unresolvedItem(item.itemId, maxScore, "pending");
  }
  if (!answer) return gradedItem(item.itemId, ZERO_SCORE_VALUE, maxScore);

  const partialScore = evaluatePartialCredit(item, answer, maxScore);
  if (partialScore) return gradedItem(item.itemId, partialScore, maxScore);

  const evaluation = evaluateQuizAnswer(item.entry, answer, context);
  switch (evaluation.status) {
    case "correct":
      return gradedItem(item.itemId, maxScore, maxScore);
    case "incorrect":
      return gradedItem(item.itemId, ZERO_SCORE_VALUE, maxScore, evaluation.reason);
    case "pending":
      return unresolvedItem(item.itemId, maxScore, "pending", evaluation.reason);
    case "unsupported":
      return unresolvedItem(item.itemId, maxScore, "unsupported", evaluation.reason);
  }
}

export function evaluateDeterministicQuiz(
  items: readonly QuizGradingItemInputV1[],
  answers: Readonly<Record<string, QuizAnswer>>,
  contexts: Readonly<Record<string, QuizEvaluationContext>> = {},
): GradeResultV1 {
  const results = items.map((item) => (
    evaluateDeterministicQuizItem(item, answers[item.itemId], contexts[item.itemId])
  ));
  const scored = results.flatMap((item) => item.score === null ? [] : [item.score]);
  const unresolved = results.some((item) => item.state !== "graded");
  return {
    schemaVersion: 1,
    state: unresolved ? "partial" : "final",
    score: unresolved ? null : addScoreValues(scored),
    maxScore: sumQuizItemPoints(items),
    items: results,
    evidenceRefs: [],
  };
}

function evaluatePartialCredit(
  item: QuizGradingItemInputV1,
  answer: QuizAnswer,
  maxScore: ScoreValue,
): ScoreValue | null {
  if (item.entry.type === QuizEntryType.Matching && item.entry.allowPartialCredit) {
    if (answer.type !== item.entry.type || item.entry.pairs.length === 0) return ZERO_SCORE_VALUE;
    const correct = item.entry.pairs.filter((pair) => answer.matches[pair.id] === pair.right).length;
    return scoreValueByRatio(maxScore, BigInt(correct), BigInt(item.entry.pairs.length));
  }
  if (item.entry.type === QuizEntryType.Ordering && item.entry.allowPartialCredit) {
    if (answer.type !== item.entry.type || item.entry.items.length === 0) return ZERO_SCORE_VALUE;
    const expected = [...item.entry.items]
      .sort((left, right) => left.correctPosition - right.correctPosition)
      .map(({ id }) => id);
    const correct = expected.filter((itemId, index) => answer.itemIds[index] === itemId).length;
    return scoreValueByRatio(maxScore, BigInt(correct), BigInt(expected.length));
  }
  return null;
}

function gradedItem(
  itemId: string,
  score: ScoreValue,
  maxScore: ScoreValue,
  feedback?: string,
): GradeItemResultV1 {
  return {
    itemId,
    state: "graded",
    score,
    maxScore,
    ...(feedback ? { feedback } : {}),
    evidenceRefs: [],
    reviewMethod: "AutomatedReview",
    handlerKey: QUIZ_AUTOMATED_REVIEW_HANDLER.key,
    handlerVersion: QUIZ_AUTOMATED_REVIEW_HANDLER.version,
    algorithmVersion: QUIZ_DETERMINISTIC_ALGORITHM.version,
  };
}

function unresolvedItem(
  itemId: string,
  maxScore: ScoreValue,
  state: "pending" | "unsupported",
  feedback?: string,
): GradeItemResultV1 {
  return {
    itemId,
    state,
    score: null,
    maxScore,
    ...(feedback ? { feedback } : {}),
    evidenceRefs: [],
    reviewMethod: "AutomatedReview",
    handlerKey: QUIZ_AUTOMATED_REVIEW_HANDLER.key,
    handlerVersion: QUIZ_AUTOMATED_REVIEW_HANDLER.version,
    algorithmVersion: QUIZ_DETERMINISTIC_ALGORITHM.version,
  };
}
