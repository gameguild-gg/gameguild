import { describe, expect, it } from 'vitest';
import {
  buildStructuredSubmissionPayload,
  buildQuizGradingItemsFromBlocks,
  createQuizGradingConfig,
  gradeDeterministicSubmission,
  gradeQuizAnswer,
  syncQuizGradingConfig,
} from './index';

describe('quiz grading helpers', () => {
  it('builds grading items from quiz blocks', () => {
    const items = buildQuizGradingItemsFromBlocks([
      {
        id: '1',
        type: 'quiz',
        data: { type: 'TRUE_FALSE', points: 2, correctAnswer: true },
      },
      {
        id: '2',
        type: 'markdown',
        data: { markdown: 'intro' },
      },
      {
        id: '3',
        type: 'quiz',
        data: { type: 'ESSAY', points: 5 },
      },
    ]);

    expect(items).toEqual({
      '1': {
        contentBlockId: '1',
        points: 2,
        gradingKind: 'deterministic',
      },
      '3': {
        contentBlockId: '3',
        points: 5,
        gradingKind: 'manual',
      },
    });
  });

  it('creates a public non-official quiz grading config', () => {
    const config = createQuizGradingConfig([
      {
        id: '1',
        type: 'quiz',
        data: { type: 'TRUE_FALSE', correctAnswer: true },
      },
    ]);

    expect(config.enabled).toBe(true);
    expect(config.validationMode).toBe('public');
    expect(config.gradebook.official).toBe(false);
    expect(config.gradebook.maxScore).toBe(1);
  });

  it('syncs items after quiz blocks change', () => {
    const config = createQuizGradingConfig([
      {
        id: '1',
        type: 'quiz',
        data: { type: 'TRUE_FALSE', correctAnswer: true },
      },
    ]);

    const synced = syncQuizGradingConfig([
      {
        id: '2',
        type: 'quiz',
        data: { type: 'SHORT_ANSWER', points: 3, acceptedAnswers: ['Tokyo'] },
      },
    ], config);

    expect(Object.keys(synced.items)).toEqual(['2']);
    expect(synced.items['2']?.points).toBe(3);
  });

  it('grades deterministic public quiz answers', () => {
    expect(
      gradeQuizAnswer(
        { type: 'SINGLE_CHOICE', points: 2, correctOptionId: 'a' },
        { selectedOptionIds: ['a'] },
      ),
    ).toMatchObject({ status: 'graded', isCorrect: true, score: 2 });

    expect(
      gradeQuizAnswer(
        { type: 'MULTIPLE_CHOICE', correctOptionIds: ['a', 'c'] },
        { selectedOptionIds: ['a', 'b'] },
      ),
    ).toMatchObject({ status: 'graded', isCorrect: false, score: 0 });

    expect(
      gradeQuizAnswer(
        { type: 'SHORT_ANSWER', acceptedAnswers: ['Tokyo'] },
        { textAnswers: { main: 'tokyo' } },
      ),
    ).toMatchObject({ status: 'graded', isCorrect: true, score: 1 });
  });

  it('leaves formula grading unsupported for Part 2 execution', () => {
    expect(
      gradeQuizAnswer(
        { type: 'FORMULA', formula: 'x + y' },
        { textAnswers: { main: 'x+y' } },
      ),
    ).toMatchObject({ status: 'unsupported', score: null });
  });

  it('builds structured submission payloads without retaining mutable answer references', () => {
    const answer = {
      selectedOptionIds: ['a'],
      textAnswers: { main: 'Tokyo' },
    };
    const payload = buildStructuredSubmissionPayload({ question_1: answer });

    answer.selectedOptionIds.push('b');
    answer.textAnswers.main = 'Kyoto';

    expect(payload).toEqual({
      answers: {
        question_1: {
          selectedOptionIds: ['a'],
          textAnswers: { main: 'Tokyo' },
        },
      },
    });
  });

  it('keeps protected deterministic submission execution deferred to Part 2', () => {
    const grading = createQuizGradingConfig([
      {
        id: '1',
        type: 'quiz',
        data: { type: 'TRUE_FALSE', correctAnswer: true },
      },
    ], { validationMode: 'protected' });

    expect(gradeDeterministicSubmission({ grading, payload: { answers: {} } })).toMatchObject({
      status: 'unsupported',
      score: null,
    });
  });
});
