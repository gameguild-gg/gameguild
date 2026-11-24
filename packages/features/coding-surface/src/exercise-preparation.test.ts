import { describe, expect, it } from 'vitest';
import { prepareCodingLearnerPreview, prepareCodingValidationSolution } from './exercise-preparation';
import { exerciseValidationFixture } from './testing/exercise-validation-fixtures';

describe('UX04-B protected preparation and strict learner projection', () => {
  it.each(['function', 'standard-io'] as const)('prepares a %s solution copy without changing starter bytes', (family) => {
    const draft = exerciseValidationFixture(family);
    const before = structuredClone(draft);
    const candidate = prepareCodingValidationSolution(draft, 'reference');
    expect(candidate.files[0]?.content).toContain('a + b');
    expect(candidate.tests).toHaveLength(2);
    expect(candidate.files[1]?.content).toBe('// PRIVATE-FILE-CANARY');
    expect(candidate).not.toHaveProperty('validationSolutions');
    candidate.files[0]!.content = 'mutation';
    expect(draft).toEqual(before);
  });

  it.each(['function', 'standard-io'] as const)('projects edited %s data, never protected cases/solutions/files', (family) => {
    const draft = exerciseValidationFixture(family);
    draft.problem.statement = 'A non-default edited statement.';
    draft.problem.examples[0]!.explanation = 'A revised explanation.';
    draft.limits.timeLimitMs = '7000';
    draft.files[0]!.content += '\n// edited starter';
    const preview = prepareCodingLearnerPreview(draft);
    expect(preview.problem).toEqual(draft.problem);
    expect(preview.limits.timeLimitMs).toBe(7000);
    expect(preview.files[0]?.content).toContain('edited starter');
    expect(preview.files).toHaveLength(1);
    expect(preview.publicTests).toHaveLength(1);
    expect(preview).not.toHaveProperty('validationSolutions');
    expect(preview).not.toHaveProperty('tests');
    expect(JSON.stringify(preview)).not.toMatch(/PRIVATE-|REFERENCE-CANARY|INCORRECT-CANARY/);
    preview.files[0]!.content = 'learner scratch';
    expect(draft.files[0]?.content).not.toBe('learner scratch');
  });

  it('rejects unknown or protected-file replacement targets before execution', () => {
    const draft = exerciseValidationFixture();
    expect(() => prepareCodingValidationSolution(draft, 'unknown')).toThrow('Select a solution with at least one source replacement.');
    draft.validationSolutions[0]!.files[0]!.fileId = 'private-file';
    draft.validationSolutions[0]!.files[0]!.path = '/home/user/private.h';
    expect(() => prepareCodingValidationSolution(draft, 'reference')).toThrow('A solution may replace only its identified public editable text file.');
  });

  it('does not invent an oracle or reference solution for an incomplete exercise', () => {
    const draft = exerciseValidationFixture('standard-io');
    const test = draft.tests[0];
    if (test?.kind !== 'stdio') throw new Error('Missing I/O fixture.');
    test.expected.stdout = null;
    expect(() => prepareCodingLearnerPreview(draft)).toThrow(/stdout/i);
    expect(() => prepareCodingValidationSolution(draft, 'reference')).toThrow(/stdout/i);
  });
});
