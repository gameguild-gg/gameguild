import { describe, expect, expectTypeOf, it } from 'vitest';
import type {
  CodingLearnerCasesProps,
  CodingLearnerPracticeIntent,
  CodingLearnerPracticeOperations,
  CodingLearnerPracticeState,
  CodingPersonalCase,
} from './learner-practice-types';

const emptyOutput = {
  id: 'mine-1',
  name: 'Empty output',
  source: 'personal',
  kind: 'stdio',
  stdin: '',
  expected: { stdout: '', stderr: null, exitCode: null },
} satisfies CodingPersonalCase;
const noOracle = { ...emptyOutput, expected: { stdout: null, stderr: null, exitCode: null } } satisfies CodingPersonalCase;
const incomplete = {
  id: 'mine-2',
  name: 'Incomplete input',
  source: 'personal',
  kind: 'function',
  inputs: [{ type: 'integer', text: '-' }],
  expected: null,
} satisfies CodingPersonalCase;

describe('UX05-A1 learner practice contracts', () => {
  it('exposes only controlled offline editing ports', () => {
    expectTypeOf<keyof CodingLearnerCasesProps>().toEqualTypeOf<'state' | 'onChange' | 'createId' | 'disabled'>();
    expectTypeOf<keyof CodingLearnerPracticeOperations>().toEqualTypeOf<'create' | 'reduce'>();
    expectTypeOf<CodingLearnerPracticeOperations['reduce']>().parameter(1).toEqualTypeOf<CodingLearnerPracticeIntent>();
    expectTypeOf<CodingLearnerPracticeOperations['create']>().returns.toEqualTypeOf<CodingLearnerPracticeState>();
  });

  it('keeps a missing expectation distinct from an intentionally empty one', () => {
    expect(emptyOutput.expected.stdout).toBe('');
    expect(noOracle.expected.stdout).toBeNull();
    for (const value of [emptyOutput, noOracle, incomplete]) {
      for (const forbidden of ['weightUnits', 'visibility', 'score', 'passed', 'submissionId', 'validationSolutions']) {
        expect(value).not.toHaveProperty(forbidden);
      }
    }
  });

  it('retains incomplete typed input without pretending it is executable', () => {
    expect(incomplete.inputs).toEqual([{ type: 'integer', text: '-' }]);
    expect(incomplete.expected).toBeNull();
    expectTypeOf<CodingPersonalCase['source']>().toEqualTypeOf<'personal'>();
  });
});
