import {
  buildQuizStructuredAnswerPayload,
  createQuizGradingDefinition,
  extractQuizAnswerKeyFromBlocks,
  redactQuizBlocks,
  type QuizBlockLike,
} from '../adapters/quiz';
import type {
  AnswerKey,
  ContentGradingDefinition,
  GradeResult,
  StructuredAnswerPayload,
} from '../types';

export interface QuizGradingTestVector {
  name: string;
  contentType: 'quiz';
  authoringPayload: readonly QuizBlockLike[];
  gradingDefinition: ContentGradingDefinition;
  answerKey: AnswerKey;
  learnerPayload: readonly QuizBlockLike[];
  learnerSubmission: StructuredAnswerPayload;
  expectedGradeResult: Pick<GradeResult, 'status' | 'score' | 'maxScore' | 'passed'>;
}

const deterministicQuizBlocks = [
  {
    id: 'q-single',
    type: 'quiz',
    data: {
      type: 'SINGLE_CHOICE',
      stem: 'Pick the engine.',
      points: 2,
      options: [
        { id: 'godot', text: 'Godot' },
        { id: 'inkscape', text: 'Inkscape' },
      ],
      correctOptionId: 'godot',
      settings: { allowRetry: true, showFeedback: true, showCorrectAnswer: true },
    },
  },
  {
    id: 'q-order',
    type: 'quiz',
    data: {
      type: 'ORDERING',
      stem: 'Order the production steps.',
      points: 3,
      items: [
        { id: 'prototype', text: 'Prototype', correctPosition: 0 },
        { id: 'playtest', text: 'Playtest', correctPosition: 1 },
        { id: 'ship', text: 'Ship', correctPosition: 2 },
      ],
      settings: { allowRetry: true, showFeedback: true, showCorrectAnswer: true },
    },
  },
] as const satisfies readonly QuizBlockLike[];

const deterministicDefinition = createQuizGradingDefinition(deterministicQuizBlocks, {
  maxScore: 5,
  passingScore: 4,
});

export const deterministicQuizVector: QuizGradingTestVector = {
  name: 'deterministic quiz submission',
  contentType: 'quiz',
  authoringPayload: deterministicQuizBlocks,
  gradingDefinition: deterministicDefinition,
  answerKey: extractQuizAnswerKeyFromBlocks(deterministicQuizBlocks, deterministicDefinition),
  learnerPayload: redactQuizBlocks(deterministicQuizBlocks, deterministicDefinition),
  learnerSubmission: buildQuizStructuredAnswerPayload({
    'q-single': { selectedOptionIds: ['godot'] },
    'q-order': { ordering: ['prototype', 'playtest', 'ship'] },
  }, deterministicDefinition),
  expectedGradeResult: {
    status: 'graded',
    score: 5,
    maxScore: 5,
    passed: true,
  },
};

const unsupportedQuizBlocks = [
  {
    id: 'q-formula',
    type: 'quiz',
    data: {
      type: 'FORMULA',
      stem: 'Find the formula.',
      points: 4,
      variables: [{ id: 'x', name: 'x', min: 1, max: 10, decimals: 0 }],
      formula: 'x * 2',
      toleranceType: 'absolute',
      tolerance: 0,
      decimalPlaces: 0,
      settings: { allowRetry: true, showFeedback: true, showCorrectAnswer: true },
    },
  },
] as const satisfies readonly QuizBlockLike[];

const unsupportedDefinition = createQuizGradingDefinition(unsupportedQuizBlocks, {
  maxScore: 4,
});

export const unsupportedQuizVector: QuizGradingTestVector = {
  name: 'unsupported formula quiz submission',
  contentType: 'quiz',
  authoringPayload: unsupportedQuizBlocks,
  gradingDefinition: unsupportedDefinition,
  answerKey: extractQuizAnswerKeyFromBlocks(unsupportedQuizBlocks, unsupportedDefinition),
  learnerPayload: redactQuizBlocks(unsupportedQuizBlocks, unsupportedDefinition),
  learnerSubmission: buildQuizStructuredAnswerPayload({
    'q-formula': {
      textAnswers: {
        main: 'x * 2',
        formula_values: JSON.stringify({ x: 3 }),
      },
    },
  }, unsupportedDefinition),
  expectedGradeResult: {
    status: 'unsupported',
    score: null,
    maxScore: 4,
  },
};

const manualQuizBlocks = [
  {
    id: 'q-essay',
    type: 'quiz',
    data: {
      type: 'ESSAY',
      stem: 'Explain why iteration matters in game production.',
      points: 6,
      minWordCount: 80,
      maxWordCount: 240,
      showWordCount: true,
      correctAnswerPlain: 'A strong answer explains feedback loops, playtesting, and scoped changes.',
      settings: { allowRetry: false, showFeedback: false, showCorrectAnswer: false },
    },
  },
] as const satisfies readonly QuizBlockLike[];

const manualDefinition = createQuizGradingDefinition(manualQuizBlocks, {
  maxScore: 6,
  feedbackMode: 'manual',
});

export const manualQuizVector: QuizGradingTestVector = {
  name: 'manual essay quiz submission',
  contentType: 'quiz',
  authoringPayload: manualQuizBlocks,
  gradingDefinition: manualDefinition,
  answerKey: extractQuizAnswerKeyFromBlocks(manualQuizBlocks, manualDefinition),
  learnerPayload: redactQuizBlocks(manualQuizBlocks, manualDefinition),
  learnerSubmission: buildQuizStructuredAnswerPayload({
    'q-essay': {
      textAnswers: {
        main: 'Iteration matters because prototypes need player feedback before production scope hardens.',
      },
    },
  }, manualDefinition),
  expectedGradeResult: {
    status: 'pending',
    score: null,
    maxScore: 6,
  },
};

export const quizGradingTestVectors = [
  deterministicQuizVector,
  unsupportedQuizVector,
  manualQuizVector,
] as const;
