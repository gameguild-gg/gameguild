import { describe, expect, it } from 'vitest';
import fixtures from '../fixtures/coding-authoring-drafts.json';
import {
  codingDraftOperations,
  compileCodingAuthoringDraft,
  parseCodingAuthoringDraft,
  parseCodingAuthoringDefinition,
  parseCodingSubmission,
  projectCodingDefinitionForLearner,
  validateCodingSubmissionAgainstDefinition,
  type CodingAuthoringDraft,
} from './index';

function draft(family: 'function' | 'standard-io' = 'function'): CodingAuthoringDraft {
  const value = parseCodingAuthoringDraft(fixtures[family]);
  if (family === 'standard-io') {
    value.limits.timeLimitMs = '5000';
    value.tests = value.tests.slice(0, 1);
  }
  return value;
}

describe('UX02-C: explicit publication and immutable projection', () => {
  it.each(['function', 'standard-io'] as const)('compiles the %s draft without changing its identities or input buffers', family => {
    const source = draft(family);
    const before = structuredClone(source);
    const definition = compileCodingAuthoringDraft(source);
    expect(parseCodingAuthoringDefinition(definition)).toEqual(definition);
    expect(definition.problem).toEqual(source.problem);
    expect(definition.validationSolutions).toEqual(source.validationSolutions.map(solution => ({
      ...solution, files: solution.files.map(file => ({ ...file, path: '/home/user/main.cpp' })),
    })));
    expect(definition.tests.map(test => test.id)).toEqual(source.tests.map(test => test.id));
    expect(definition.files[0]!.id).toBe('main-file');
    expect(definition.entryPoint.path).toBe('/home/user/main.cpp');
    expect(definition.limits.timeLimitMs).toBe(5000);
    expect(source).toEqual(before);
  });

  it('preserves explicit empty stdout, but never invents a missing expectation', () => {
    const source = draft('standard-io');
    expect(compileCodingAuthoringDraft(source).tests[0]).toMatchObject({ kind: 'stdio', expected: { stdout: '' } });
    const test = source.tests[0];
    if (test?.kind !== 'stdio') throw new Error('Expected stdio fixture.');
    test.expected.stdout = null;
    expect(() => compileCodingAuthoringDraft(source)).toThrow(/stdout/);
    expect(parseCodingAuthoringDraft(source).tests).toEqual(source.tests);
  });

  it.each(['statement', 'signature', 'scalar', 'arity', 'weight', 'limit', 'entry', 'constraint'] as const)('rejects an incomplete %s while leaving the recoverable draft unchanged', mutation => {
      const source = draft();
      const test = source.tests[0];
      if (test?.kind !== 'function') throw new Error('Expected function fixture.');
      if (mutation === 'statement') source.problem.statement = ' ';
      if (mutation === 'signature') source.functionSignature = null;
      if (mutation === 'scalar') test.cases[0]!.inputs[0]!.text = '2abc';
      if (mutation === 'arity') test.cases[0]!.inputs.pop();
      if (mutation === 'weight') test.weightUnits = '1.5';
      if (mutation === 'limit') source.limits.timeLimitMs = '';
      if (mutation === 'entry') source.entryPoint.path = '../secret';
      if (mutation === 'constraint') source.problem.constraints = [' '];
      const before = structuredClone(source);
      expect(() => compileCodingAuthoringDraft(source)).toThrow();
      expect(source).toEqual(before);
    });

  it('parses scalar buffers exactly, never coercing truthy strings or unsafe integers', () => {
    const source = draft();
    source.functionSignature = { name: 'check', parameters: [{ name: 'flag', type: 'boolean' }], returnType: 'float' };
    source.tests = [{ id: 'typed', name: '', visibility: 'public', weightUnits: '1', kind: 'function', cases: [{
      id: 'typed-case', inputs: [{ type: 'boolean', text: 'false' }], expected: { type: 'float', text: '1.234567890123456789' },
    }] }];
    expect(compileCodingAuthoringDraft(source).tests[0]).toMatchObject({ cases: [{
      inputs: [{ type: 'boolean', value: false }], expected: { type: 'float', value: '1.234567890123456789' },
    }] });
    const test = source.tests[0];
    if (test?.kind !== 'function') throw new Error('Expected function fixture.');
    test.cases[0]!.inputs[0]!.text = 'False-ish';
    expect(() => compileCodingAuthoringDraft(source)).toThrow();
    source.functionSignature.parameters[0]!.type = 'integer';
    test.cases[0]!.inputs[0] = { type: 'integer', text: '9007199254740993' };
    expect(() => compileCodingAuthoringDraft(source)).toThrow();
  });

  it('keeps protected reference replacements out of learner delivery and starter files', () => {
    const definition = compileCodingAuthoringDraft(draft());
    const delivery = projectCodingDefinitionForLearner(definition);
    expect(delivery.problem).toEqual(definition.problem);
    expect(JSON.stringify(delivery)).not.toContain('SYNTHETIC-TEACHER-REFERENCE');
    expect(Object.hasOwn(delivery, 'validationSolutions')).toBe(false);
    expect(definition.files[0]!.content).toBe('int sum(int a, int b) { return 0; }');
  });

  it('reopening a published definition retains problem and protected references', () => {
    const source = draft();
    const definition = compileCodingAuthoringDraft(source);
    const reopened = codingDraftOperations.fromDefinition(definition, source.environment);
    expect(reopened.problem).toEqual(source.problem);
    expect(reopened.validationSolutions).toEqual(definition.validationSolutions);
  });

  it('an edited source cannot change an old revision delivery or submitted-file binding', () => {
    const source = draft();
    const frozen = compileCodingAuthoringDraft(source);
    const oldBytes = JSON.stringify(frozen);
    const response = parseCodingSubmission({ schemaVersion: 1, files: [{ fileId: 'main-file', path: 'main.cpp', encoding: 'text', content: 'learner answer' }] });
    source.problem.statement = 'A different exercise';
    source.files[0]!.path = 'next.cpp';
    source.entryPoint.path = 'next.cpp';
    source.validationSolutions = [];
    const current = compileCodingAuthoringDraft(source);
    expect(JSON.stringify(frozen)).toBe(oldBytes);
    expect(projectCodingDefinitionForLearner(frozen).problem!.statement).toBe('Return the sum of two integers.');
    expect(validateCodingSubmissionAgainstDefinition(response, frozen)).toEqual(response);
    expect(() => validateCodingSubmissionAgainstDefinition(response, current)).toThrow();
    expect(current.files[0]!.id).toBe(frozen.files[0]!.id);
  });
});
