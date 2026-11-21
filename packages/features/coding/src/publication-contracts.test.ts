import { describe, expect, it } from 'vitest';
import { codingContractFixture } from './testing/fixtures';
import { parseCodingAuthoringDefinition, parseCodingLearnerDelivery, parseCodingProblem } from './schemas';
import type { CodingAuthoringDefinition } from './contracts';

function publishedDefinition(): CodingAuthoringDefinition {
  return {
    ...structuredClone(codingContractFixture.authoring),
    tests: structuredClone(codingContractFixture.authoring.tests.filter(test => test.kind === 'stdio')),
    problem: { statement: 'Print the input.', examples: [{ id: 'example-1', input: '', output: '', explanation: '' }], constraints: [] },
    validationSolutions: [{ id: 'reference', name: 'Reference', role: 'reference', files: [{
      fileId: codingContractFixture.authoring.files[0]!.id,
      path: codingContractFixture.authoring.files[0]!.path,
      content: '// SYNTHETIC-PROTECTED-SOLUTION',
    }] }],
  };
}

describe('UX02-C: publishable metadata contracts', () => {
  it('retains the exact historical shape when publication metadata is absent', () => {
    const old = codingContractFixture.authoring;
    expect(parseCodingAuthoringDefinition(old)).toEqual(old);
    expect(Object.hasOwn(parseCodingAuthoringDefinition(old), 'problem')).toBe(false);
    expect(Object.hasOwn(parseCodingAuthoringDefinition(old), 'validationSolutions')).toBe(false);
  });

  it('accepts structured problems and author-only replacements, cloning the caller', () => {
    const source = publishedDefinition();
    const parsed = parseCodingAuthoringDefinition(source);
    expect(parsed).toEqual(source);
    parsed.problem!.statement = 'Changed';
    parsed.validationSolutions![0]!.files[0]!.content = 'Changed';
    expect(source.problem!.statement).toBe('Print the input.');
    expect(source.validationSolutions![0]!.files[0]!.content).toBe('// SYNTHETIC-PROTECTED-SOLUTION');
  });

  it.each([
    { statement: '   ', examples: [], constraints: [] },
    { statement: 'Prompt', examples: [], constraints: [''] },
    { statement: 'Prompt', examples: [], constraints: [], secret: 'not a problem field' },
    { statement: 'Prompt', examples: [{ id: 'duplicate', input: '', output: '', explanation: '' }, { id: 'duplicate', input: '', output: '', explanation: '' }], constraints: [] },
  ])('rejects incomplete or injected problem fields', problem => {
    expect(() => parseCodingProblem(problem)).toThrow();
  });

  it.each(['path', 'identity', 'private', 'readonly', 'duplicate', 'empty'] as const)('rejects an invalid reference replacement: %s', mutation => {
    const definition = publishedDefinition();
    const solution = definition.validationSolutions![0]!;
    if (mutation === 'path') solution.files[0]!.path = '/home/user/other.cpp';
    if (mutation === 'identity') solution.files[0]!.fileId = 'another-file';
    if (mutation === 'private') { definition.files[0]!.visibility = 'private'; definition.files[0]!.mutable = false; }
    if (mutation === 'readonly') definition.files[0]!.mutable = false;
    if (mutation === 'duplicate') solution.files.push(structuredClone(solution.files[0]!));
    if (mutation === 'empty') solution.files = [];
    expect(() => parseCodingAuthoringDefinition(definition)).toThrow();
  });

  it('rejects protected replacements on a learner payload, even an empty list', () => {
    expect(() => parseCodingLearnerDelivery({ ...codingContractFixture.learnerDelivery, validationSolutions: [] })).toThrow();
    expect(parseCodingLearnerDelivery({ ...codingContractFixture.learnerDelivery, problem: publishedDefinition().problem }).problem)
      .toEqual(publishedDefinition().problem);
  });

  it('requires one coherent signature and family for a structured published problem', () => {
    const definition = publishedDefinition();
    definition.tests = [structuredClone(codingContractFixture.functionAuthoring.tests[0]!)];
    expect(() => parseCodingAuthoringDefinition(definition)).toThrow();
    definition.exerciseType = 'function';
    const test = definition.tests[0];
    if (test?.kind !== 'function') throw new Error('Expected function fixture.');
    definition.entryPoint.symbol = test.function.name;
    const different = structuredClone(test);
    different.id = 'other-signature';
    different.function.name = 'other';
    definition.tests.push(different);
    expect(() => parseCodingAuthoringDefinition(definition)).toThrow();
  });
});
