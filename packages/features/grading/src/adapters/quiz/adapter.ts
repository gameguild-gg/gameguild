import type { GradingAdapter } from '../types';
import { extractQuizAnswerKeyFromBlocks } from './answer-key';
import { buildQuizGradingItemsFromBlocks } from './items';
import { redactQuizBlocks, redactQuizBlockStorage } from './redaction';
import { buildQuizStructuredAnswerPayload } from './structured-answer';
import type { QuizBlockLike, QuizBlockStorageLike } from './types';
import { toQuizBlocks } from './utils';

export const quizGradingAdapter: GradingAdapter<readonly QuizBlockLike[] | QuizBlockStorageLike> = {
  contentType: 'quiz',
  extractItems(payload) {
    return buildQuizGradingItemsFromBlocks(toQuizBlocks(payload));
  },
  extractAnswerKey(payload, grading) {
    return extractQuizAnswerKeyFromBlocks(toQuizBlocks(payload), grading);
  },
  redactLearnerPayload(payload, grading) {
    if (Array.isArray(payload)) return redactQuizBlocks(payload, grading);
    return redactQuizBlockStorage(payload as QuizBlockStorageLike, grading);
  },
  buildStructuredAnswerPayload(input, grading) {
    return buildQuizStructuredAnswerPayload(input, grading);
  },
};
