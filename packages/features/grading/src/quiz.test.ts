import { describe, expect, it } from 'vitest';
import {
  buildQuizGradingItemsFromBlocks,
  buildQuizStructuredAnswerPayload,
  createQuizGradingDefinition,
  extractQuizAnswerKeyFromBlocks,
  gradeDeterministicQuizSubmission,
  gradeQuizAnswer,
  getQuizQuestionGradingKind,
  QUIZ_ANSWER_KEY_INVENTORY,
  quizGradingTestVectors,
  redactQuizBlocks,
  syncQuizGradingDefinition,
} from './index';

describe('quiz grading adapter', () => {
  const knownQuizQuestionTypes = [
    'SINGLE_CHOICE',
    'MULTIPLE_CHOICE',
    'TRUE_FALSE',
    'FILL_IN_THE_BLANK',
    'SHORT_ANSWER',
    'ESSAY',
    'MATCHING',
    'ORDERING',
    'CATEGORIZATION',
    'RATING',
    'NUMERIC',
    'FORMULA',
    'HOTSPOT',
    'HIGHLIGHT',
  ] as const;

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

  it('does not mark unsupported quiz types as deterministic', () => {
    const items = buildQuizGradingItemsFromBlocks([
      {
        id: 'numeric',
        type: 'quiz',
        data: { type: 'NUMERIC', points: 2, formula: 'x + y' },
      },
      {
        id: 'formula',
        type: 'quiz',
        data: { type: 'FORMULA', points: 2, formula: 'x + y' },
      },
      {
        id: 'rating-open',
        type: 'quiz',
        data: { type: 'RATING', points: 1 },
      },
      {
        id: 'rating-keyed',
        type: 'quiz',
        data: { type: 'RATING', points: 1, correctRating: 5 },
      },
    ]);

    expect(items.numeric?.gradingKind).toBe('unsupported');
    expect(items.formula?.gradingKind).toBe('unsupported');
    expect(items['rating-open']?.gradingKind).toBe('unsupported');
    expect(items['rating-keyed']?.gradingKind).toBe('deterministic');
    expect(getQuizQuestionGradingKind('FORMULA')).toBe('unsupported');
  });

  it('does not mark incomplete deterministic quiz definitions as deterministic', () => {
    const items = buildQuizGradingItemsFromBlocks([
      {
        id: 'single-missing-key',
        type: 'quiz',
        data: { type: 'SINGLE_CHOICE', options: [{ id: 'a', text: 'A' }] },
      },
      {
        id: 'multiple-empty-key',
        type: 'quiz',
        data: { type: 'MULTIPLE_CHOICE', options: [{ id: 'a', text: 'A' }], correctOptionIds: [] },
      },
      {
        id: 'fill-empty-text',
        type: 'quiz',
        data: {
          type: 'FILL_IN_THE_BLANK',
          blanks: [{ id: 'b1', input: { type: 'TEXT', acceptedAnswers: [] } }],
        },
      },
      {
        id: 'matching-missing-right',
        type: 'quiz',
        data: { type: 'MATCHING', pairs: [{ id: 'fr', left: 'France' }] },
      },
      {
        id: 'ordering-duplicate-position',
        type: 'quiz',
        data: {
          type: 'ORDERING',
          items: [
            { id: 'a', text: 'A', correctPosition: 0 },
            { id: 'b', text: 'B', correctPosition: 0 },
          ],
        },
      },
      {
        id: 'categorization-missing-category',
        type: 'quiz',
        data: {
          type: 'CATEGORIZATION',
          categories: [{ id: 'c1', name: 'One' }],
          items: [{ id: 'i1', text: 'Item', correctCategoryIds: ['missing'] }],
        },
      },
      {
        id: 'hotspot-empty-zones',
        type: 'quiz',
        data: {
          type: 'HOTSPOT',
          imageWidth: 100,
          imageHeight: 100,
          hotspots: [{ id: 'h1', x: 50, y: 50, zones: [] }],
        },
      },
      {
        id: 'highlight-empty-spans',
        type: 'quiz',
        data: { type: 'HIGHLIGHT', plainText: 'Answer', highlights: [] },
      },
    ]);

    for (const item of Object.values(items)) {
      expect(item.gradingKind).toBe('unsupported');
    }
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

  it('normalizes structured answer payloads and drops tampered fields', () => {
    const grading = createQuizGradingDefinition([
      {
        id: 'q1',
        type: 'quiz',
        data: { type: 'SINGLE_CHOICE', correctOptionId: 'a' },
      },
    ]);

    const payload = buildQuizStructuredAnswerPayload({
      q1: {
        selectedOptionIds: ['a', 'a'],
        textAnswers: {
          main: 'Tokyo',
          correctAnswer: 'Tokyo',
          isCorrect: 'true',
          score: '999',
        },
        categorizations: {
          item1: ['cat1', 'cat1'],
          answerKey: ['cat-secret'],
        },
        ordering: ['step-1', 'step-1'],
        rating: 4,
        score: 999,
        isCorrect: true,
      },
      injected: {
        selectedOptionIds: ['secret'],
      },
    }, grading);

    expect(payload).toEqual({
      answers: {
        q1: {
          selectedOptionIds: ['a'],
          textAnswers: { main: 'Tokyo' },
          categorizations: { item1: ['cat1'] },
          ordering: ['step-1'],
          rating: 4,
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
        {
          id: 'q2',
          type: 'quiz',
          data: {
            type: 'MULTIPLE_CHOICE',
            stem: 'Pick many',
            correctOptionIds: ['a', 'c'],
            options: [
              { id: 'a', text: 'A' },
              { id: 'b', text: 'B' },
              { id: 'c', text: 'C' },
            ],
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
    expect(redacted[1]?.data).toEqual({
      type: 'MULTIPLE_CHOICE',
      stem: 'Pick many',
      options: [
        { id: 'a', text: 'A' },
        { id: 'b', text: 'B' },
        { id: 'c', text: 'C' },
      ],
    });
  });

  it('redacts quiz payloads by question type instead of field names only', () => {
    const redacted = redactQuizBlocks(
      [
        {
          id: 'fill',
          type: 'quiz',
          data: {
            type: 'FILL_IN_THE_BLANK',
            stem: 'Capital: ___',
            blanks: [
              {
                id: 'dropdown',
                position: 0,
                input: { type: 'DROPDOWN', options: ['Paris', 'Rome'] },
              },
              {
                id: 'wordbank',
                position: 1,
                input: { type: 'WORDBANK', words: ['alpha', 'beta'] },
              },
              {
                id: 'number',
                position: 2,
                input: { type: 'NUMBER', correctValue: 10, tolerance: 1, unit: 'kg' },
              },
            ],
          },
        },
        {
          id: 'matching',
          type: 'quiz',
          data: {
            type: 'MATCHING',
            stem: 'Match',
            pairs: [{ id: 'fr', left: 'France', right: 'Paris' }],
            distractors: ['Rome'],
          },
        },
        {
          id: 'ordering',
          type: 'quiz',
          data: {
            type: 'ORDERING',
            stem: 'Order',
            items: [{ id: 'a', text: 'A', correctPosition: 0 }],
          },
        },
        {
          id: 'categorization',
          type: 'quiz',
          data: {
            type: 'CATEGORIZATION',
            stem: 'Categorize',
            categories: [{ id: 'c1', name: 'Category' }],
            items: [{ id: 'i1', text: 'Item', correctCategoryIds: ['c1'] }],
          },
        },
        {
          id: 'formula',
          type: 'quiz',
          data: {
            type: 'FORMULA',
            stem: 'Formula',
            variables: [{ id: 'x', name: 'x', min: 1, max: 2, decimals: 0 }],
            formula: 'x * 2',
            toleranceType: 'absolute',
            tolerance: 0,
          },
        },
        {
          id: 'hotspot',
          type: 'quiz',
          data: {
            type: 'HOTSPOT',
            stem: 'Click',
            imageAssetUri: 'asset://7776453f-1123-4f56-8abc-1234567890ab',
            imageWidth: 100,
            imageHeight: 100,
            hotspots: [{ id: 'h1', x: 50, y: 50, zones: [{ radius: 10, label: 'Hit' }] }],
          },
        },
        {
          id: 'highlight',
          type: 'quiz',
          data: {
            type: 'HIGHLIGHT',
            stem: 'Highlight',
            sourceText: 'The __answer__',
            plainText: 'The answer',
            highlights: [{ start: 4, end: 10 }],
          },
        },
      ],
      createQuizGradingDefinition([]),
    );

    expect(redacted.find((block) => block.id === 'fill')?.data).toMatchObject({
      blanks: [
        { input: { type: 'DROPDOWN', options: ['Rome', 'Paris'] } },
        { input: { type: 'WORDBANK', words: ['beta', 'alpha'] } },
        { input: { type: 'NUMBER', unit: 'kg' } },
      ],
    });
    expect(redacted.find((block) => block.id === 'matching')?.data).toEqual({
      type: 'MATCHING',
      stem: 'Match',
      pairs: [{ id: 'fr', left: 'France' }],
      rightOptions: ['Rome', 'Paris'],
    });
    expect(redacted.find((block) => block.id === 'ordering')?.data).toEqual({
      type: 'ORDERING',
      stem: 'Order',
      items: [{ id: 'a', text: 'A' }],
    });
    expect(redacted.find((block) => block.id === 'categorization')?.data).toEqual({
      type: 'CATEGORIZATION',
      stem: 'Categorize',
      categories: [{ id: 'c1', name: 'Category' }],
      items: [{ id: 'i1', text: 'Item' }],
    });
    expect(redacted.find((block) => block.id === 'formula')?.data).toEqual({
      type: 'FORMULA',
      stem: 'Formula',
      variables: [{ id: 'x', name: 'x', min: 1, max: 2, decimals: 0 }],
    });
    expect(redacted.find((block) => block.id === 'hotspot')?.data).toEqual({
      type: 'HOTSPOT',
      stem: 'Click',
      imageAssetUri: 'asset://7776453f-1123-4f56-8abc-1234567890ab',
      imageWidth: 100,
      imageHeight: 100,
    });
    expect(redacted.find((block) => block.id === 'highlight')?.data).toEqual({
      type: 'HIGHLIGHT',
      stem: 'Highlight',
      plainText: 'The answer',
    });

    expect(JSON.stringify(redacted)).not.toContain('correctPosition');
    expect(JSON.stringify(redacted)).not.toContain('correctCategoryIds');
    expect(JSON.stringify(redacted)).not.toContain('hotspots');
    expect(JSON.stringify(redacted)).not.toContain('highlights');
    expect(JSON.stringify(redacted)).not.toContain('sourceText');
  });

  it('extracts minimal server-owned answer keys', () => {
    const blocks = [
      {
        id: 'single',
        type: 'quiz',
        data: {
          type: 'SINGLE_CHOICE',
          stem: 'Pick one',
          correctOptionId: 'a',
          options: [{ id: 'a', text: 'A' }],
        },
      },
      {
        id: 'matching',
        type: 'quiz',
        data: {
          type: 'MATCHING',
          stem: 'Match',
          pairs: [{ id: 'fr', left: 'France', right: 'Paris' }],
        },
      },
      {
        id: 'fill',
        type: 'quiz',
        data: {
          type: 'FILL_IN_THE_BLANK',
          stem: 'Capital: ___',
          blanks: [{ id: 'b1', input: { type: 'DROPDOWN', options: ['Paris', 'Rome'] } }],
        },
      },
    ];
    const grading = createQuizGradingDefinition(blocks);
    const answerKey = extractQuizAnswerKeyFromBlocks(blocks, grading);

    expect(answerKey.items.single).toEqual({
      type: 'SINGLE_CHOICE',
      correctOptionId: 'a',
    });
    expect(answerKey.items.matching).toEqual({
      type: 'MATCHING',
      pairs: [{ id: 'fr', right: 'Paris' }],
    });
    expect(answerKey.items.fill).toEqual({
      type: 'FILL_IN_THE_BLANK',
      blanks: [{ id: 'b1', input: { type: 'DROPDOWN', options: ['Paris'] } }],
    });
    expect(JSON.stringify(answerKey.items.matching)).not.toContain('France');
    expect(JSON.stringify(answerKey.items.single)).not.toContain('options');
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

  it('exports backend-facing quiz test vectors', () => {
    const inventoryTypes = QUIZ_ANSWER_KEY_INVENTORY.map(({ type }) => type);
    expect([...new Set(inventoryTypes)].sort()).toEqual([...knownQuizQuestionTypes].sort());
    expect(QUIZ_ANSWER_KEY_INVENTORY).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ type: 'SINGLE_CHOICE', gradingSupport: 'deterministic' }),
        expect.objectContaining({ type: 'ESSAY', gradingSupport: 'manual' }),
        expect.objectContaining({ type: 'FORMULA', gradingSupport: 'unsupported' }),
      ]),
    );

    for (const vector of quizGradingTestVectors) {
      expect(vector.contentType).toBe('quiz');
      expect(vector.answerKey.items).toBeDefined();
      expect(vector.learnerPayload).toBeDefined();
      expect(vector.learnerSubmission.answers).toBeDefined();
    }
    expect(quizGradingTestVectors.map(({ expectedGradeResult }) => expectedGradeResult.status)).toEqual(
      expect.arrayContaining(['graded', 'pending', 'unsupported']),
    );
  });
});
