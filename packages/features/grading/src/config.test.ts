import { describe, expect, it } from 'vitest';
import {
  GradingConfigValidationError,
  assertPublicIsNotOfficial,
  createDisabledGradingConfig,
  isOfficialGrade,
  normalizeGradingConfig,
  sumGradedItemPoints,
  validateGradingConfig,
} from './index';

describe('grading config contracts', () => {
  it('keeps disabled grading inert', () => {
    expect(createDisabledGradingConfig()).toEqual({
      enabled: false,
      schemaVersion: 1,
      validationMode: 'public',
      gradebook: {
        maxScore: 0,
        official: false,
      },
      policy: {},
      items: {},
    });
  });

  it('normalizes public grading as non-official', () => {
    const config = normalizeGradingConfig({
      enabled: true,
      validationMode: 'public',
      gradebook: {
        maxScore: 10,
        official: true,
      },
      items: {
        q1: {
          contentBlockId: 'q1',
          points: 10,
          gradingKind: 'deterministic',
        },
      },
    });

    expect(config.gradebook.official).toBe(false);
    expect(isOfficialGrade(config)).toBe(false);
  });

  it('rejects explicit public official grading', () => {
    expect(() =>
      assertPublicIsNotOfficial({
        enabled: true,
        schemaVersion: 1,
        validationMode: 'public',
        gradebook: {
          maxScore: 10,
          official: true,
        },
        policy: {},
        items: {},
      }),
    ).toThrow(GradingConfigValidationError);
  });

  it('treats protected official grading as official', () => {
    const config = validateGradingConfig({
      enabled: true,
      schemaVersion: 1,
      validationMode: 'protected',
      gradebook: {
        maxScore: 10,
        official: true,
      },
      policy: {},
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

    expect(isOfficialGrade(config)).toBe(true);
    expect(sumGradedItemPoints(config)).toBe(10);
  });

  it('rejects enabled grading without a positive max score', () => {
    expect(() =>
      validateGradingConfig({
        enabled: true,
        validationMode: 'protected',
        gradebook: {
          maxScore: 0,
        },
        policy: {},
        items: {},
      }),
    ).toThrow(GradingConfigValidationError);
  });
});
