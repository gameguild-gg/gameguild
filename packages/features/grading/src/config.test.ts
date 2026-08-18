import { describe, expect, it } from 'vitest';
import {
  GradingConfigValidationError,
  createDisabledGradingDefinition,
  normalizeGradingDefinition,
  sumGradedItemPoints,
  validateGradingDefinition,
} from './index';

describe('grading definition contracts', () => {
  it('keeps disabled grading inert', () => {
    expect(createDisabledGradingDefinition()).toEqual({
      enabled: false,
      schemaVersion: 1,
      score: {
        maxScore: 0,
      },
      attempts: {},
      feedback: {},
      presentation: {},
      items: {},
    });
  });

  it('normalizes enabled grading without placement concerns', () => {
    const definition = normalizeGradingDefinition({
      enabled: true,
      score: {
        maxScore: 10,
      },
      items: {
        q1: {
          contentBlockId: 'q1',
          points: 10,
          gradingKind: 'deterministic',
        },
      },
    });

    expect(definition.score.maxScore).toBe(10);
    expect(definition.items.q1?.gradingKind).toBe('deterministic');
    expect(definition).not.toHaveProperty('outcome');
  });

  it('normalizes scoring, feedback and presentation policies', () => {
    const definition = validateGradingDefinition({
      enabled: true,
      schemaVersion: 1,
      score: {
        maxScore: 10,
        passingScore: 7,
      },
      attempts: {},
      feedback: {
        mode: 'after-submit',
      },
      presentation: {
        mode: 'single-step',
      },
      items: {
        q1: {
          contentBlockId: 'q1',
          points: 4,
          gradingKind: 'deterministic',
        },
        q2: {
          contentBlockId: 'q2',
          points: 6,
          gradingKind: 'manual',
        },
      },
    });

    expect(definition.feedback.mode).toBe('after-submit');
    expect(definition.presentation.mode).toBe('single-step');
    expect(sumGradedItemPoints(definition)).toBe(10);
  });

  it('rejects enabled grading without a positive max score', () => {
    expect(() =>
      validateGradingDefinition({
        enabled: true,
        score: {
          maxScore: 0,
        },
        items: {},
      }),
    ).toThrow(GradingConfigValidationError);
  });
});
