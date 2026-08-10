import { describe, expect, it } from 'vitest';
import {
  CONTENT_GRADING_STORAGE_KEY,
  readContentGradingConfig,
  writeContentGradingConfig,
} from './index';

const grading = {
  enabled: true,
  schemaVersion: 1,
  validationMode: 'public',
  gradebook: {
    maxScore: 1,
    official: false,
  },
  policy: {
    feedbackMode: 'immediate',
  },
  items: {
    '1': {
      contentBlockId: '1',
      points: 1,
      gradingKind: 'deterministic',
    },
  },
} as const;

describe('content storage grading metadata', () => {
  it('writes grading beside existing content body data', () => {
    const body = writeContentGradingConfig(
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
      validationMode: 'public',
      gradebook: { maxScore: 1 },
    });
    expect(body.order).toEqual([['1', 'quiz']]);
  });

  it('reads grading from object body', () => {
    const body = writeContentGradingConfig({ order: [], blocks: {} }, grading);

    expect(readContentGradingConfig(body)?.enabled).toBe(true);
    expect(readContentGradingConfig(JSON.stringify(body))).toBeNull();
  });

  it('removes grading when disabled', () => {
    const body = writeContentGradingConfig({ order: [], blocks: {}, grading }, null);

    expect(body).toEqual({ order: [], blocks: {} });
  });
});
