import { describe, expect, it } from 'vitest';
import {
  GradingConfigValidationError,
  createDisabledGradingDefinition,
  isGradebookBound,
  normalizeGradingDefinition,
  sumGradedItemPoints,
  validateGradingDefinition,
} from './index';

describe('grading definition contracts', () => {
  it('keeps disabled grading inert', () => {
    expect(createDisabledGradingDefinition()).toEqual({
      enabled: false,
      schemaVersion: 1,
      outcome: {
        uses: ['feedback'],
        gradebook: null,
      },
      score: {
        maxScore: 0,
      },
      attempts: {},
      feedback: {},
      presentation: {},
      items: {},
    });
  });

  it('keeps feedback-only grading out of the gradebook', () => {
    const definition = normalizeGradingDefinition({
      enabled: true,
      outcome: {
        uses: ['feedback'],
        gradebook: {
          groupId: 'ignored',
          includeInFinalGrade: true,
        },
      },
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

    expect(definition.outcome.uses).toEqual(['feedback']);
    expect(definition.outcome.gradebook).toBeNull();
    expect(isGradebookBound(definition)).toBe(false);
  });

  it('normalizes gradebook placement when the result should count there', () => {
    const definition = validateGradingDefinition({
      enabled: true,
      schemaVersion: 1,
      outcome: {
        uses: ['feedback', 'gradebook', 'gradebook'],
        gradebook: {
          groupId: 'group-1',
          weight: 20,
          required: false,
          includeInFinalGrade: true,
        },
      },
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

    expect(definition.outcome.uses).toEqual(['feedback', 'gradebook']);
    expect(definition.outcome.gradebook).toEqual({
      groupId: 'group-1',
      weight: 20,
      required: false,
      includeInFinalGrade: true,
    });
    expect(isGradebookBound(definition)).toBe(true);
    expect(sumGradedItemPoints(definition)).toBe(10);
  });

  it('rejects unsupported result uses', () => {
    expect(() =>
      validateGradingDefinition({
        enabled: true,
        outcome: {
          uses: ['analytics'],
        },
        score: {
          maxScore: 1,
        },
        items: {},
      }),
    ).toThrow(GradingConfigValidationError);
  });

  it('rejects enabled grading without a positive max score', () => {
    expect(() =>
      validateGradingDefinition({
        enabled: true,
        outcome: {
          uses: ['feedback'],
        },
        score: {
          maxScore: 0,
        },
        items: {},
      }),
    ).toThrow(GradingConfigValidationError);
  });
});
