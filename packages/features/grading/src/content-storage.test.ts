import { describe, expect, it } from 'vitest';
import {
  CONTENT_GRADING_STORAGE_KEY,
  type ContentGradingDefinition,
  readContentGradingDefinition,
  writeContentGradingDefinition,
} from './index';

const grading: ContentGradingDefinition = {
  enabled: true,
  schemaVersion: 1,
  score: {
    maxScore: 1,
  },
  attempts: {},
  feedback: {
    mode: 'immediate',
  },
  presentation: {
    mode: 'continuous',
  },
  items: {
    '1': {
      contentBlockId: '1',
      points: 1,
      gradingKind: 'deterministic',
    },
  },
};

describe('content storage grading metadata', () => {
  it('writes grading beside existing content body data', () => {
    const body = writeContentGradingDefinition(
      {
        order: [['1', 'quiz']],
        blocks: {
          '1': { type: 'TRUE_FALSE', correctAnswer: true },
        },
      },
      grading,
    );

    expect(body[CONTENT_GRADING_STORAGE_KEY]).toMatchObject({
      enabled: true,
      score: { maxScore: 1 },
    });
    expect(body.order).toEqual([['1', 'quiz']]);
  });

  it('reads grading from object body', () => {
    const body = writeContentGradingDefinition({ order: [], blocks: {} }, grading);

    expect(readContentGradingDefinition(body)?.enabled).toBe(true);
    expect(readContentGradingDefinition(JSON.stringify(body))).toBeNull();
  });

  it('removes grading when disabled', () => {
    const body = writeContentGradingDefinition({ order: [], blocks: {}, grading }, null);

    expect(body).toEqual({ order: [], blocks: {} });
  });
});
