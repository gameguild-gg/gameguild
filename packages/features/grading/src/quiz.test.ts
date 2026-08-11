import { describe, expect, it } from 'vitest';
import {
  buildQuizGradingItemsFromBlocks,
  buildQuizStructuredAnswerPayload,
  createQuizGradingDefinition,
  extractQuizAnswerKeyFromBlocks,
  gradeDeterministicQuizSubmission,
  gradeQuizAnswer,
  redactQuizBlocks,
  syncQuizGradingDefinition,
} from './index';

describe('quiz grading adapter', () => {
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

  it('creates feedback-only quiz grading by default', () => {
    const definition = createQuizGradingDefinition([
      {
        id: '1',
        type: 'quiz',
        data: { type: 'TRUE_FALSE', correctAnswer: true },
      },
    ]);

    expect(definition.enabled).toBe(true);
    expect(definition.outcome).toEqual({
      uses: ['feedback'],
      gradebook: null,
    });
    expect(definition.score.maxScore).toBe(1);
  });

  it('creates gradebook-bound quiz grading when requested', () => {
    const definition = createQuizGradingDefinition(
      [
        {
          id: '1',
          type: 'quiz',
          data: { type: 'TRUE_FALSE', points: 3, correctAnswer: true },
        },
      ],
      {
        uses: ['feedback', 'gradebook'],
        groupId: 'group-1',
        weight: 30,
        includeInFinalGrade: false,
      },
    );

    expect(definition.outcome).toMatchObject({
      uses: ['feedback', 'gradebook'],
      gradebook: {
        groupId: 'group-1',
        weight: 30,
        includeInFinalGrade: false,
      },
    });
    expect(definition.score.maxScore).toBe(3);
  });

  it('syncs items after quiz blocks change', () => {
    const definition = createQuizGradingDefinition([
      {
        id: '1',
        type: 'quiz',
        data: { type: 'TRUE_FALSE', correctAnswer: true },
      },
    ]);

    const synced = syncQuizGradingDefinition([
      {
        id: '2',
        type: 'quiz',
        data: { type: 'SHORT_ANSWER', points: 3, acceptedAnswers: ['Tokyo'] },
      },
    ], definition);

    expect(Object.keys(synced.items)).toEqual(['2']);
    expect(synced.items['2']?.points).toBe(3);
  });

  it('builds structured answer payloads without retaining mutable answer references', () => {
    const answer = {
      selectedOptionIds: ['a'],
      textAnswers: { main: 'Tokyo' },
    };
    const payload = buildQuizStructuredAnswerPayload({ question_1: answer });

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

  it('redacts answer-key fields from learner quiz payloads', () => {
    const redacted = redactQuizBlocks(
      [
        {
          id: 'q1',
          type: 'quiz',
          data: {
            type: 'SINGLE_CHOICE',
            stem: 'Pick one',
            correctOptionId: 'a',
            options: [{ id: 'a', text: 'A' }],
          },
        },
      ],
      createQuizGradingDefinition([]),
    );

    expect(redacted[0]?.data).toEqual({
      type: 'SINGLE_CHOICE',
      stem: 'Pick one',
      options: [{ id: 'a', text: 'A' }],
    });
  });

  it('grades deterministic submissions from a server-owned answer key', () => {
    const blocks = [
      {
        id: 'q1',
        type: 'quiz',
        data: { type: 'SINGLE_CHOICE', points: 2, correctOptionId: 'a' },
      },
      {
        id: 'q2',
        type: 'quiz',
        data: { type: 'TRUE_FALSE', points: 1, correctAnswer: false },
      },
    ];
    const grading = createQuizGradingDefinition(blocks, {
      maxScore: 3,
      passingScore: 2,
    });
    const answerKey = extractQuizAnswerKeyFromBlocks(blocks, grading);

    expect(
      gradeDeterministicQuizSubmission({
        grading,
        answerKey,
        payload: buildQuizStructuredAnswerPayload({
          q1: { selectedOptionIds: ['a'] },
          q2: { selectedOptionIds: ['false'] },
        }),
      }),
    ).toMatchObject({
      status: 'graded',
      score: 3,
      maxScore: 3,
      passed: true,
    });

    expect(
      gradeDeterministicQuizSubmission({
        grading,
        answerKey,
        payload: {
          answers: {
            q1: { selectedOptionIds: ['a'] },
            q2: { selectedOptionIds: ['true'] },
          },
          score: 999,
          isCorrect: true,
        } as never,
      }),
    ).toMatchObject({
      status: 'graded',
      score: 2,
      maxScore: 3,
      passed: true,
    });
  });

  it('keeps unsupported question types out of deterministic server grading', () => {
    expect(
      gradeQuizAnswer(
        { type: 'FORMULA', formula: 'x + y' },
        { textAnswers: { main: 'x+y' } },
      ),
    ).toMatchObject({ status: 'unsupported', score: null });
  });
});
