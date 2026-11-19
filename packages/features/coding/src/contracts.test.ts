import { describe, expect, it } from 'vitest';
import { codingContractFixture } from './testing';
import { parseCodingAuthoringDefinition, parseCodingExecutionReport, parseCodingLearnerDelivery, parseCodingSubmission } from './schemas';
import { normalizeCodingPath } from './paths';
import { projectCodingDefinitionForLearner, validateCodingSubmissionAgainstDefinition } from './projection';

describe('coding contracts', () => {
  it('normalizes paths and preserves the canonical form on roundtrip', () => {
    const parsed = parseCodingAuthoringDefinition(codingContractFixture.authoring);

    expect(parsed.files.map(({ path }) => path)).toEqual(['/home/user/main.cpp', '/home/user/include/read_only.h', '/home/user/private/hidden.cpp']);
    expect(parseCodingAuthoringDefinition(JSON.parse(JSON.stringify(parsed)))).toEqual(parsed);
  });

  it.each(['../secret', '/etc/passwd', 'C:/secret', 'a\\b', 'a//b', '.gameguild/receipt'])('rejects unsafe path %s', (path) =>
    expect(() => normalizeCodingPath(path)).toThrow(),
  );

  it('fails closed for unknown versions, unknown fields, path alias collisions, and fractional weights', () => {
    expect(() => parseCodingAuthoringDefinition(codingContractFixture.invalid.unknownSchemaVersion)).toThrow();
    expect(() =>
      parseCodingAuthoringDefinition({
        ...codingContractFixture.authoring,
        exerciseType: undefined,
      }),
    ).toThrow();
    expect(() =>
      parseCodingAuthoringDefinition({
        ...codingContractFixture.authoring,
        unknown: true,
      }),
    ).toThrow(/Unrecognized key/);
    expect(() =>
      parseCodingAuthoringDefinition({
        ...codingContractFixture.authoring,
        files: codingContractFixture.authoring.files.map((file, index) => ({
          ...file,
          path: codingContractFixture.invalid.aliasedPaths[index] ?? file.path,
        })),
      }),
    ).toThrow(/Duplicate file path/);
    expect(() =>
      parseCodingAuthoringDefinition({
        ...codingContractFixture.authoring,
        tests: codingContractFixture.authoring.tests.map((test, index) =>
          index === 0 ? { ...test, weightUnits: codingContractFixture.invalid.nonIntegerWeight } : test,
        ),
      }),
    ).toThrow();
  });

  it('validates functional signatures and canonical float values', () => {
    const functional = codingContractFixture.authoring.tests[1]!;
    if (functional.kind !== 'function') throw new Error('Fixture is not functional.');
    expect(() =>
      parseCodingAuthoringDefinition({
        ...codingContractFixture.authoring,
        tests: [
          codingContractFixture.authoring.tests[0]!,
          {
            ...functional,
            cases: [
              {
                ...functional.cases[0]!,
                inputs: [{ type: 'string', value: 'wrong' }, functional.cases[0]!.inputs[1]!],
              },
            ],
          },
        ],
      }),
    ).toThrow(/input type/);
  });

  it('projects a learner-safe definition without private files, tests, or visibility fields', () => {
    const learner = projectCodingDefinitionForLearner(codingContractFixture.authoring);
    const serialized = JSON.stringify(learner);

    expect(learner).toEqual(codingContractFixture.learnerDelivery);
    expect(serialized).not.toContain('private-harness');
    expect(serialized).not.toContain('private-function');
    expect(serialized).not.toContain('private-case-1');
    expect(serialized).not.toContain('visibility');
    expect(parseCodingLearnerDelivery(learner)).toEqual(learner);
  });

  it('roundtrips the explicit exercise type without inferring it from tests or toolchain', () => {
    const parsed = parseCodingAuthoringDefinition(codingContractFixture.authoring);
    const learner = projectCodingDefinitionForLearner(parsed);

    expect(parsed.exerciseType).toBe('standard-io');
    expect(learner.exerciseType).toBe('standard-io');
    expect(parseCodingLearnerDelivery(JSON.parse(JSON.stringify(learner)))).toEqual(learner);
    expect(() => parseCodingAuthoringDefinition({ ...parsed, schemaVersion: 2 })).toThrow();
  });

  it('accepts only mutable public files and explicitly allowed learner files', () => {
    const submission = parseCodingSubmission(codingContractFixture.submission);
    expect(validateCodingSubmissionAgainstDefinition(submission, codingContractFixture.authoring)).toEqual(submission);

    const immutable = parseCodingSubmission({
      schemaVersion: 1,
      files: [
        {
          fileId: 'readonly-header',
          path: 'include/read_only.h',
          encoding: 'text',
          content: 'changed',
        },
      ],
    });
    expect(() => validateCodingSubmissionAgainstDefinition(immutable, codingContractFixture.authoring)).toThrow(/cannot replace/);

    const forged = { ...codingContractFixture.submission, score: 100 };
    expect(() => parseCodingSubmission(forged)).toThrow(/Unrecognized key/);
  });

  it('distinguishes completed, partial, and infrastructure execution states', () => {
    expect(parseCodingExecutionReport(codingContractFixture.executionReport).executionState).toBe('completed');
    expect(() =>
      parseCodingExecutionReport({
        ...codingContractFixture.executionReport,
        executionState: 'completed',
        outcomes: [
          {
            ...codingContractFixture.executionReport.outcomes[0],
            state: 'error',
          },
        ],
      }),
    ).toThrow(/completed report/);
    expect(() =>
      parseCodingExecutionReport({
        ...codingContractFixture.executionReport,
        executionState: 'executor-error',
        receiptHash: undefined,
        outcomes: [],
        failureCode: undefined,
      }),
    ).toThrow(/failureCode/);
  });
});
