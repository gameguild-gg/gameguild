import { describe, expect, it } from 'vitest';
import fixtures from '../../../../../packages/features/coding/fixtures/coding-publication-cases.json';
import { codingAssignmentToAuthoringDefinition, codingAssignmentToLearnerDelivery } from './coding-surface-bridge';
import { narrowCodingAssignmentContent, type CodingAssignmentContent } from './types';

function content(family: 'function' | 'standard-io' | 'legacy'): CodingAssignmentContent {
  const result = narrowCodingAssignmentContent(structuredClone(fixtures[family]));
  if (!result) throw new Error('The shared wire fixture must be accepted.');
  return result;
}

describe('UX02-C: published coding wire parity', () => {
  it.each(['function', 'standard-io'] as const)('preserves %s problem, protected solutions and stable IDs', family => {
    const wire = content(family);
    const definition = codingAssignmentToAuthoringDefinition(wire);
    expect(definition.problem?.statement).toBe(fixtures[family].Problem.Statement);
    expect(definition.problem?.examples[0]?.id).toBe('example-1');
    expect(definition.validationSolutions?.[0]?.files[0]?.content).toContain('SYNTHETIC-TEACHER-REFERENCE');
    expect(definition.files[0]?.id).toBe('main-file');
    expect(definition.tests.map(test => test.id)).toEqual(family === 'function' ? ['addition-public', 'addition-private'] : ['echo-public', 'echo-private']);
    const test = definition.tests[0];
    if (test?.kind === 'function') expect(test.cases[0]?.id).toBe('positive-case');
    const delivery = codingAssignmentToLearnerDelivery(wire);
    expect(delivery.problem).toEqual(definition.problem);
    expect(JSON.stringify(delivery)).not.toContain('SYNTHETIC-');
    expect(Object.hasOwn(delivery, 'validationSolutions')).toBe(false);
  });

  it('does not derive a new test identity when its expectation changes', () => {
    const before = codingAssignmentToAuthoringDefinition(content('standard-io'));
    const changed = structuredClone(fixtures['standard-io']);
    changed.Tests.Public[0]!.Stdout = 'another result';
    const narrowed = narrowCodingAssignmentContent(changed);
    if (!narrowed) throw new Error('The changed fixture must be accepted.');
    const after = codingAssignmentToAuthoringDefinition(narrowed);
    expect(before.tests[0]?.id).toBe('echo-public');
    expect(after.tests[0]?.id).toBe('echo-public');
    expect(before.tests[0]).not.toEqual(after.tests[0]);
  });

  it('retains the historical definition without inventing problem or solution fields', () => {
    const definition = codingAssignmentToAuthoringDefinition(content('legacy'));
    expect(Object.hasOwn(definition, 'problem')).toBe(false);
    expect(Object.hasOwn(definition, 'validationSolutions')).toBe(false);
    expect(definition.files[0]?.id).toMatch(/^file-/);
    expect(definition.tests[0]?.id).toMatch(/^test-/);
  });

  it('preserves file-map keys even when they resemble JavaScript object properties', () => {
    const source = fixtures.function;
    const result = narrowCodingAssignmentContent({ ...source, Data: { Files: Object.fromEntries([
      ...Object.entries(source.Data.Files),
      ['__proto__', { ...source.Data.Files['/home/user/main.cpp'], Id: 'support-file', Modifiable: false }],
    ]) } });
    expect(result).not.toBeNull();
    expect(Object.hasOwn(result!.Data.Files, '__proto__')).toBe(true);
    expect(result!.Data.Files['__proto__']?.Id).toBe('support-file');
  });

  it('normalizes new camelCase metadata without turning solution file arrays into maps', () => {
    const raw = fixtures.function;
    const result = narrowCodingAssignmentContent({
      ...raw,
      Problem: undefined,
      ValidationSolutions: undefined,
      problem: { statement: 'A different problem', examples: [{ id: 'example-2', input: '', output: '', explanation: '' }], constraints: [] },
      validationSolutions: [{ id: 'reference', name: 'Reference', role: 'reference', files: [{ fileId: 'main-file', path: '/home/user/main.cpp', content: '// PRIVATE-REFERENCE' }] }],
    });
    if (!result) throw new Error('CamelCase fields must be accepted.');
    expect(result.Problem?.Statement).toBe('A different problem');
    expect(result.Problem?.Examples[0]?.Id).toBe('example-2');
    expect(result.ValidationSolutions?.[0]?.Files).toEqual([{ FileId: 'main-file', Path: '/home/user/main.cpp', Content: '// PRIVATE-REFERENCE' }]);
  });

  it.each(['problem', 'problem-extra', 'reference', 'reference-extra', 'file-id', 'test-id', 'case-id', 'unknown-test'] as const)('fails closed for malformed %s rather than dropping the field', mutation => {
    const raw: Record<string, unknown> = structuredClone(fixtures.function);
    if (mutation === 'problem') raw.Problem = { Statement: 3, Examples: [], Constraints: [] };
    if (mutation === 'problem-extra') raw.Problem = { ...fixtures.function.Problem, PrivateSolution: 'must not be silently discarded' };
    if (mutation === 'reference') raw.ValidationSolutions = [{ ...fixtures.function.ValidationSolutions[0], Role: 'grader' }];
    if (mutation === 'reference-extra') raw.ValidationSolutions = [{ ...fixtures.function.ValidationSolutions[0], Score: 100 }];
    if (mutation === 'file-id') raw.Data = { Files: { 'main.cpp': { ...fixtures.function.Data.Files['/home/user/main.cpp'], Id: 3 } } };
    if (mutation === 'test-id') raw.Tests = { ...fixtures.function.Tests, Public: [{ ...fixtures.function.Tests.Public[0], Id: 3 }] };
    if (mutation === 'case-id') raw.Tests = { ...fixtures.function.Tests, Public: [{ ...fixtures.function.Tests.Public[0], Cases: [{ ...fixtures.function.Tests.Public[0]!.Cases[0], Id: 3 }] }] };
    if (mutation === 'unknown-test') raw.Tests = { ...fixtures.function.Tests, Public: [{ ...fixtures.function.Tests.Public[0], kind: 'unknown' }] };
    expect(narrowCodingAssignmentContent(raw)).toBeNull();
  });
});
