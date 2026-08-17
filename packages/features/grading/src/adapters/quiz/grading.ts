import {
  evaluateQuizAnswer,
  fromStructuredGradingAnswer,
  type QuizEntryType,
  type QuizPracticeEntry,
  type QuizStructuredAnswer,
} from '@game-guild/quiz';
import { sumGradedItemPoints } from '../../config';
import type {
  GradeItemResult,
  GradeResult,
  GradeSubmissionArgs,
  StructuredAnswer,
} from '../../types';
import { buildQuizStructuredAnswerPayload } from './structured-answer';
import {
  asQuizQuestion,
  getQuizQuestionType,
  normalizeQuestionPoints,
} from './utils';

export function gradeQuizAnswer(entry: unknown, answer: StructuredAnswer, points?: number): GradeItemResult {
  const question = asQuizQuestion(entry);
  const maxScore = points ?? normalizeQuestionPoints(question?.points);
  const type = getQuizQuestionType(question);
  if (!question || !type) return unsupported(maxScore);
  if (type === 'NUMERIC' || type === 'FORMULA') return unsupported(maxScore);

  const result = evaluateQuizAnswer(
    question as unknown as QuizPracticeEntry,
    fromStructuredGradingAnswer(type as QuizEntryType, answer as QuizStructuredAnswer),
  );
  switch (result.status) {
    case 'correct':
      return graded(true, maxScore);
    case 'incorrect':
      return graded(false, maxScore);
    case 'pending':
      return pending(maxScore);
    case 'unsupported':
      return unsupported(maxScore);
  }
}

export function gradeDeterministicQuizSubmission(args: GradeSubmissionArgs): GradeResult {
  const { grading, payload, answerKey } = args;
  if (!grading.enabled || !answerKey) {
    return {
      status: 'unsupported',
      score: null,
      maxScore: grading.score.maxScore,
    };
  }

  // Re-normalize inside the grading function so callers cannot bypass the
  // whitelist by constructing `StructuredAnswerPayload` by hand.
  const normalizedPayload = buildQuizStructuredAnswerPayload(payload, grading);
  const itemResults = Object.entries(grading.items).map(([itemId, item]) => {
    if (item.gradingKind !== 'deterministic') {
      return {
        contentBlockId: item.contentBlockId,
        status: item.gradingKind === 'manual' ? 'pending' : 'unsupported',
        score: null,
        maxScore: item.points,
      } satisfies GradeItemResult;
    }

    // The answer key is server-owned and addressed by graded item id. Learner
    // answers are addressed by content block id.
    const result = gradeQuizAnswer(answerKey.items[itemId], normalizedPayload.answers[item.contentBlockId] ?? {}, item.points);
    return {
      ...result,
      contentBlockId: item.contentBlockId,
    };
  });
  const gradedItems = itemResults.filter((item) => item.status === 'graded');
  const pendingItems = itemResults.filter((item) => item.status === 'pending');
  const unsupportedItems = itemResults.filter((item) => item.status === 'unsupported');
  const score = gradedItems.reduce((sum, item) => sum + (item.score ?? 0), 0);
  const maxScore = sumGradedItemPoints(grading);

  if (pendingItems.length > 0) {
    return {
      status: 'pending',
      score: null,
      maxScore,
      items: itemResults,
    };
  }

  if (unsupportedItems.length > 0) {
    return {
      status: 'unsupported',
      score: null,
      maxScore,
      items: itemResults,
    };
  }

  return {
    status: 'graded',
    score,
    maxScore,
    passed: grading.score.passingScore === undefined ? undefined : score >= grading.score.passingScore,
    items: itemResults,
  };
}

function graded(isCorrect: boolean, maxScore: number): GradeItemResult {
  return {
    contentBlockId: '',
    status: 'graded',
    score: isCorrect ? maxScore : 0,
    maxScore,
    isCorrect,
  };
}

function pending(maxScore: number): GradeItemResult {
  return {
    contentBlockId: '',
    status: 'pending',
    score: null,
    maxScore,
  };
}

function unsupported(maxScore: number): GradeItemResult {
  return {
    contentBlockId: '',
    status: 'unsupported',
    score: null,
    maxScore,
  };
}
