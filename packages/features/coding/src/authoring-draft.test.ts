import { describe, expect, it } from 'vitest';
import fixtures from '../fixtures/coding-authoring-drafts.json';
import * as coding from './index';
import { codingContractFixture } from './testing';
import type { CodingDraftOperations } from './authoring-draft-contracts';

describe('UX02-A: explicit authoring draft transformations', () => {
  it('exposes the previously specified pure operations port', () => {
    expect(Reflect.get(coding, 'codingDraftOperations')).toMatchObject({
      fromDefinition: expect.any(Function), planChange: expect.any(Function),
      resolveChange: expect.any(Function), duplicateTest: expect.any(Function),
    });
  });

  it('imports a coherent function without changing file, test or case identities', () => {
    const definition = structuredClone(codingContractFixture.functionAuthoring);
    const original = structuredClone(definition);
    const draft = operations().fromDefinition(definition, { language: 'cpp', runtime: 'wasi' });
    expect(draft.functionSignature).toEqual({ name: 'sum', parameters: [
      { name: 'left', type: 'integer' }, { name: 'right', type: 'integer' },
    ], returnType: 'integer' });
    expect(draft.files).toEqual(definition.files);
    expect(draft.tests[0]).toEqual({
      id: 'function-public', name: 'Public addition cases', visibility: 'public', weightUnits: '50', kind: 'function',
      cases: [{ id: 'function-public-positive', inputs: [{ type: 'integer', text: '2' }, { type: 'integer', text: '3' }], expected: { type: 'integer', text: '5' } }],
    });
    expect(draft.problem).toEqual({ statement: '', examples: [], constraints: [] });
    draft.files[0]!.content = 'independent edit';
    expect(definition).toEqual(original);
  });

  it('imports standard I/O without treating an empty expectation or zero exit code as missing', () => {
    const definition = structuredClone(codingContractFixture.authoring);
    definition.tests = [{ kind: 'stdio', id: 'empty', visibility: 'public', weightUnits: 1, stdin: '', expected: { stdout: '', exitCode: 0 } }];
    const draft = operations().fromDefinition(definition, { language: 'cpp', runtime: 'wasi' });
    expect(draft.functionSignature).toBeNull();
    expect(draft.tests[0]).toMatchObject({ id: 'empty', expected: { stdout: '', stderr: null, exitCode: '0' } });
  });

  it('keeps false and exact decimal spellings when importing typed values', () => {
    const definition = structuredClone(codingContractFixture.functionAuthoring);
    definition.entryPoint.symbol = 'flag';
    definition.tests = [{ kind: 'function', id: 'flag-test', visibility: 'public', weightUnits: 1,
      function: { name: 'flag', parameters: [{ name: 'enabled', type: 'boolean' }], returnType: 'float' },
      cases: [{ id: 'false-case', inputs: [{ type: 'boolean', value: false }], expected: { type: 'float', value: '-0.2500' } }],
    }];
    const draft = operations().fromDefinition(definition, { language: 'cpp', runtime: 'wasi' });
    expect(draft.tests[0]).toMatchObject({ cases: [{ inputs: [{ type: 'boolean', text: 'false' }], expected: { type: 'float', text: '-0.2500' } }] });
  });

  it('refuses implicit reinterpretation of historical mixed kinds or signatures', () => {
    expect(() => operations().fromDefinition(codingContractFixture.authoring, { language: 'cpp', runtime: 'wasi' })).toThrow('mixed');
    const definition = structuredClone(codingContractFixture.functionAuthoring);
    const second = definition.tests[1];
    if (second?.kind !== 'function') throw new Error('Expected a function fixture.');
    second.function.name = 'different';
    expect(() => operations().fromDefinition(definition, { language: 'cpp', runtime: 'wasi' })).toThrow('signature');
    expect(second.function.name).toBe('different');
  });

  it('reports family change impact without modifying source, files, problem or protected solutions', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    const before = structuredClone(draft);
    const plan = operations().planChange(draft, { kind: 'family', exerciseType: 'standard-io' });
    expect(plan.impact.removedTestIds).toEqual(['addition']);
    expect(plan.impact.filesToReview).toEqual(['main-file']);
    expect(plan.candidate).toMatchObject({ exerciseType: 'standard-io', tests: [], functionSignature: null });
    expect(plan.candidate.problem).toEqual(draft.problem);
    expect(plan.candidate.files).toEqual(draft.files);
    expect(plan.candidate.validationSolutions).toEqual(draft.validationSolutions);
    expect(draft).toEqual(before);
    expect(operations().resolveChange(draft, plan, 'cancel')).toEqual(before);
    expect(operations().resolveChange(draft, plan, 'confirm')).toEqual(plan.candidate);
  });

  it('does not lose data on no-op changes or cancellation after another edit', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    const noOp = operations().planChange(draft, { kind: 'family', exerciseType: 'function' });
    expect(noOp.candidate).toEqual(draft);
    expect(noOp.impact).toMatchObject({ removedTestIds: [], changedCaseIds: [], filesToReview: [] });
    const plan = operations().planChange(draft, { kind: 'family', exerciseType: 'standard-io' });
    draft.problem.statement = 'A newer edit';
    expect(() => operations().resolveChange(draft, plan, 'confirm')).toThrow('stale');
    expect(operations().resolveChange(draft, plan, 'cancel')).toEqual(draft);
  });

  it('treats reordered object properties as the same environment and signature', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    const environment = operations().planChange(draft, { kind: 'environment', environment: { runtime: 'wasi', language: 'cpp' } });
    const signature = operations().planChange(draft, { kind: 'signature', signature: {
      returnType: 'integer', parameters: [{ type: 'integer', name: 'a' }, { type: 'integer', name: 'b' }], name: 'sum',
    } });
    expect(environment.impact).toEqual({ removedTestIds: [], changedCaseIds: [], filesToReview: [] });
    expect(signature.impact).toEqual({ removedTestIds: [], changedCaseIds: [], filesToReview: [] });
    expect(environment.candidate).toEqual(draft);
    expect(signature.candidate).toEqual(draft);
  });

  it('preserves compatible typed values across parameter reorder and clears incompatible expectations', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    const plan = operations().planChange(draft, { kind: 'signature', signature: {
      name: 'sum', parameters: [{ name: 'b', type: 'integer' }, { name: 'a', type: 'integer' }, { name: 'label', type: 'string' }], returnType: 'string',
    } });
    expect(plan.impact.changedCaseIds).toEqual(['positive']);
    const test = plan.candidate.tests[0];
    expect(test?.kind === 'function' && test.cases).toEqual([
      { id: 'positive', inputs: [{ type: 'integer', text: '3' }, { type: 'integer', text: '2' }, { type: 'string', text: '' }], expected: null },
    ]);
    expect(operations().resolveChange(draft, plan, 'cancel')).toEqual(fixtures.function);
  });

  it('preserves incomplete values by compatible identity but never guesses duplicate parameter bindings', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    const test = draft.tests[0];
    if (test?.kind !== 'function' || !draft.functionSignature) throw new Error('Expected a function draft.');
    test.cases[0]!.inputs[0]!.text = '-';
    const reorder = operations().planChange(draft, { kind: 'signature', signature: {
      name: 'sum', parameters: [{ name: 'a', type: 'integer' }], returnType: 'integer',
    } });
    expect(reorder.candidate.tests[0]).toMatchObject({ cases: [{ inputs: [{ type: 'integer', text: '-' }] }] });
    draft.functionSignature.parameters[1]!.name = 'a';
    const ambiguous = operations().planChange(draft, { kind: 'signature', signature: {
      name: 'sum', parameters: [{ name: 'a', type: 'integer' }], returnType: 'integer',
    } });
    expect(ambiguous.candidate.tests[0]).toMatchObject({ cases: [{ inputs: [{ type: 'integer', text: '' }] }] });
  });

  it('does not bind a duplicate parameter name even when its duplicate has another type', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    if (!draft.functionSignature) throw new Error('Expected a function signature.');
    draft.functionSignature.parameters[1] = { name: 'a', type: 'string' };
    const plan = operations().planChange(draft, { kind: 'signature', signature: {
      name: 'sum', parameters: [{ name: 'a', type: 'integer' }], returnType: 'integer',
    } });
    expect(plan.candidate.tests[0]).toMatchObject({ cases: [{ inputs: [{ type: 'integer', text: '' }] }] });
  });

  it('rejects function setup on standard I/O rather than creating hidden mixed state', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures['standard-io']);
    expect(() => operations().planChange(draft, { kind: 'signature', signature: {
      name: 'sum', parameters: [], returnType: 'integer',
    } })).toThrow('function');
  });

  it('keeps code and tests while marking an environment change for explicit review', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    const plan = operations().planChange(draft, { kind: 'environment', environment: { language: 'cpp', runtime: 'sdl3' } });
    expect(plan.candidate.files).toEqual(draft.files);
    expect(plan.candidate.tests).toEqual(draft.tests);
    expect(plan.impact.filesToReview).toEqual(['main-file']);
    expect(plan.candidate.exerciseType).toBe('function');
    expect(plan.candidate.environment).toEqual({ language: 'cpp', runtime: 'sdl3' });
    expect(plan.candidate.toolchain).toEqual(draft.toolchain);
    expect(operations().resolveChange(draft, plan, 'cancel')).toEqual(draft);
  });

  it('keeps toolchain identity separate from selected language and runtime', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    const plan = operations().planChange(draft, { kind: 'toolchain', toolchain: { ...draft.toolchain, version: '4.5.3' } });
    expect(plan.candidate.toolchain.version).toBe('4.5.3');
    expect(plan.candidate.environment).toEqual(draft.environment);
    expect(plan.candidate.exerciseType).toBe('function');
    expect(plan.impact.filesToReview).toEqual(['main-file']);
  });

  it('duplicates a functional group with caller-owned fresh IDs and independent buffers', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    const result = operations().duplicateTest(draft, 'addition', 'addition-copy', ['positive-copy']);
    expect(result.tests.map(test => test.id)).toEqual(['addition', 'addition-copy']);
    const copy = result.tests[1];
    expect(copy).toMatchObject({ id: 'addition-copy', name: 'Addition (copy)', visibility: 'public', kind: 'function', cases: [{ id: 'positive-copy' }] });
    if (copy?.kind !== 'function') throw new Error('Expected duplicated function.');
    copy.cases[0]!.inputs[0]!.text = '99';
    expect(draft).toEqual(fixtures.function);
    expect(result.tests[0]).toEqual(draft.tests[0]);
  });

  it('duplicates incomplete I/O cases without replacing missing output with empty output', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures['standard-io']);
    const copied = operations().duplicateTest(draft, 'incomplete', 'incomplete-copy', []);
    expect(copied.tests[2]).toMatchObject({ id: 'incomplete-copy', visibility: 'private', expected: { stdout: null, exitCode: 'not yet' } });
  });

  it('rejects missing cases, reused IDs and malformed duplicate identities', () => {
    const draft = coding.parseCodingAuthoringDraft(fixtures.function);
    expect(() => operations().duplicateTest(draft, 'absent', 'copy', [])).toThrow('does not exist');
    expect(() => operations().duplicateTest(draft, 'addition', 'addition', ['copy'])).toThrow('fresh ID');
    expect(() => operations().duplicateTest(draft, 'addition', 'copy', [])).toThrow('one fresh ID');
    expect(() => operations().duplicateTest(draft, 'addition', 'copy', ['positive'])).toThrow('one fresh ID');
    expect(() => operations().duplicateTest(draft, 'addition', '../invalid', ['copy'])).toThrow('Coding authoring draft');
  });
});

function operations(): CodingDraftOperations {
  const port: unknown = Reflect.get(coding, 'codingDraftOperations');
  if (!isOperations(port)) throw new Error('The specified coding draft operations are not exported.');
  return port;
}

function isOperations(port: unknown): port is CodingDraftOperations {
  return typeof port === 'object' && port !== null
    && 'fromDefinition' in port && typeof port.fromDefinition === 'function'
    && 'planChange' in port && typeof port.planChange === 'function'
    && 'resolveChange' in port && typeof port.resolveChange === 'function'
    && 'duplicateTest' in port && typeof port.duplicateTest === 'function';
}
