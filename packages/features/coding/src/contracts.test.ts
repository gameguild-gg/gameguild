import { describe, expect, it } from 'vitest';
import { codingContractFixtureV1 } from './testing';
import { parseCodingAuthoringDefinition, parseCodingExecutionReport, parseCodingLearnerDelivery, parseCodingSubmission } from './schemas';
import { normalizeCodingPath } from './paths';
import { projectCodingDefinitionForLearner, validateCodingSubmissionAgainstDefinition } from './projection';

describe('coding contracts', () => {
  it('normalizes paths and preserves the canonical form on roundtrip', () => {
    const parsed = parseCodingAuthoringDefinition(codingContractFixtureV1.authoring);

    expect(parsed.files.map(({ path }) => path)).toEqual(['/home/user/main.cpp', '/home/user/include/read_only.h', '/home/user/private/hidden.cpp']);
    expect(parseCodingAuthoringDefinition(JSON.parse(JSON.stringify(parsed)))).toEqual(parsed);
  });

  it.each(['../secret', '/etc/passwd', 'C:/secret', 'a\\b', 'a//b', '.gameguild/receipt'])('rejects unsafe path %s', (path) =>
    expect(() => normalizeCodingPath(path)).toThrow(),
  );

  it('fails closed for unknown versions, unknown fields, path alias collisions, and fractional weights', () => {
    expect(() => parseCodingAuthoringDefinition(codingContractFixtureV1.invalid.unknownSchemaVersion)).toThrow();
    expect(() =>
      parseCodingAuthoringDefinition({
        ...codingContractFixtureV1.authoring,
        unknown: true,
      }),
    ).toThrow(/Unrecognized key/);
    expect(() =>
      parseCodingAuthoringDefinition({
        ...codingContractFixtureV1.authoring,
        files: codingContractFixtureV1.authoring.files.map((file, index) => ({
          ...file,
          path: codingContractFixtureV1.invalid.aliasedPaths[index] ?? file.path,
        })),
      }),
    ).toThrow(/Duplicate file path/);
    expect(() =>
      parseCodingAuthoringDefinition({
        ...codingContractFixtureV1.authoring,
        tests: codingContractFixtureV1.authoring.tests.map((test, index) =>
          index === 0 ? { ...test, weightUnits: codingContractFixtureV1.invalid.nonIntegerWeight } : test,
        ),
      }),
    ).toThrow();
  });

  it('validates functional signatures and canonical float values', () => {
    const functional = codingContractFixtureV1.authoring.tests[1]!;
    if (functional.kind !== 'function') throw new Error('Fixture is not functional.');
    expect(() =>
      parseCodingAuthoringDefinition({
        ...codingContractFixtureV1.authoring,
        tests: [
          codingContractFixtureV1.authoring.tests[0]!,
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
    const learner = projectCodingDefinitionForLearner(codingContractFixtureV1.authoring);
    const serialized = JSON.stringify(learner);

    expect(learner).toEqual(codingContractFixtureV1.learnerDelivery);
    expect(serialized).not.toContain('private-harness');
    expect(serialized).not.toContain('private-function');
    expect(serialized).not.toContain('private-case-1');
    expect(serialized).not.toContain('visibility');
    expect(parseCodingLearnerDelivery(learner)).toEqual(learner);
  });

  it('accepts only mutable public files and explicitly allowed learner files', () => {
    const submission = parseCodingSubmission(codingContractFixtureV1.submission);
    expect(validateCodingSubmissionAgainstDefinition(submission, codingContractFixtureV1.authoring)).toEqual(submission);

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
    expect(() => validateCodingSubmissionAgainstDefinition(immutable, codingContractFixtureV1.authoring)).toThrow(/cannot replace/);

    const forged = { ...codingContractFixtureV1.submission, score: 100 };
    expect(() => parseCodingSubmission(forged)).toThrow(/Unrecognized key/);
  });

  it('distinguishes completed, partial, and infrastructure execution states', () => {
    expect(parseCodingExecutionReport(codingContractFixtureV1.executionReport).executionState).toBe('completed');
    expect(() =>
      parseCodingExecutionReport({
        ...codingContractFixtureV1.executionReport,
        executionState: 'completed',
        outcomes: [
          {
            ...codingContractFixtureV1.executionReport.outcomes[0],
            state: 'error',
          },
        ],
      }),
    ).toThrow(/completed report/);
    expect(() =>
      parseCodingExecutionReport({
        ...codingContractFixtureV1.executionReport,
        executionState: 'executor-error',
        receiptHash: undefined,
        outcomes: [],
        failureCode: undefined,
      }),
    ).toThrow(/failureCode/);
  });
});
