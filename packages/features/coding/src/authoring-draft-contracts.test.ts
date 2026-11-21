import { describe, expect, it } from 'vitest';
import fixtures from '../fixtures/coding-authoring-drafts.json';
import {
  CODING_DRAFT_TYPE,
  parseCodingAuthoringDefinition,
  parseCodingAuthoringDraft,
  parseCodingLearnerDelivery,
  projectCodingDefinitionForLearner,
  type CodingAuthoringDraft,
} from './index';

describe('UX02-A: incomplete author-only draft contract', () => {
  it.each(['function', 'standard-io'] as const)('roundtrips the shared %s fixture without changing fields', family => {
    const parsed = parseCodingAuthoringDraft(fixtures[family]);
    expect(parseCodingAuthoringDraft(JSON.parse(JSON.stringify(parsed)))).toEqual(fixtures[family]);
    expect(parsed).not.toBe(fixtures[family]);
  });

  it('retains incomplete statement, path, signature, numeric buffers and cases', () => {
    const draft = functionDraft();
    draft.problem.statement = '';
    draft.files[0]!.path = '';
    draft.entryPoint.path = '';
    draft.functionSignature = { name: '', parameters: [{ name: '', type: 'integer' }], returnType: 'integer' };
    draft.limits.timeLimitMs = 'later';
    draft.tests = [{ id: 'new-test', name: '', visibility: 'private', weightUnits: '', kind: 'function', cases: [
      { id: 'new-case', inputs: [{ type: 'integer', text: '-' }], expected: null },
    ] }];
    expect(parseCodingAuthoringDraft(draft)).toEqual(draft);
  });

  it('distinguishes no stdout expectation from intentional empty output', () => {
    const draft = parseCodingAuthoringDraft(fixtures['standard-io']);
    const [empty, absent] = draft.tests;
    expect(empty?.kind === 'stdio' && empty.expected.stdout).toBe('');
    expect(absent?.kind === 'stdio' && absent.expected.stdout).toBeNull();
    expect(parseCodingAuthoringDraft(JSON.parse(JSON.stringify(draft)))).toEqual(draft);
  });

  it('never treats a draft as an executable definition or learner delivery', () => {
    const draft = functionDraft();
    expect(() => parseCodingAuthoringDefinition(draft)).toThrow();
    expect(() => parseCodingLearnerDelivery(draft)).toThrow();
    expect(() => Reflect.apply(projectCodingDefinitionForLearner, undefined, [draft])).toThrow();
  });

  it.each([
    { schemaVersion: 2 },
    { exerciseType: 'project' },
    { validationResult: { passed: true } },
    { learnerScore: 100 },
    { files: [{ ...fixtures.function.files[0], id: '<script>' }] },
    { toolchain: { ...fixtures.function.toolchain, artifactDigest: 'unverified' } },
    { limits: { ...fixtures.function.limits, timeLimitMs: Number.NaN } },
  ])('rejects malformed shapes, identities and injected state: %j', patch => {
    expect(() => parseCodingAuthoringDraft({ ...fixtures.function, ...patch })).toThrow();
  });

  it('rejects duplicate stable identities and mixed-family cases', () => {
    const draft = functionDraft();
    draft.files.push(structuredClone(draft.files[0]!));
    expect(() => parseCodingAuthoringDraft(draft)).toThrow('Duplicate draft identifier');
    expect(() => parseCodingAuthoringDraft({ ...fixtures.function, tests: fixtures['standard-io'].tests })).toThrow('exercise family');
    expect(() => parseCodingAuthoringDraft({ ...fixtures['standard-io'], functionSignature: fixtures.function.functionSignature })).toThrow('no function signature');
  });

  it('rejects duplicate case, example and protected-solution file identities', () => {
    const draft = functionDraft();
    draft.problem.examples.push(structuredClone(draft.problem.examples[0]!));
    expect(() => parseCodingAuthoringDraft(draft)).toThrow('Duplicate draft identifier');
    const solution = functionDraft().validationSolutions[0]!;
    solution.files.push(structuredClone(solution.files[0]!));
    expect(() => parseCodingAuthoringDraft({ ...fixtures.function, validationSolutions: [solution] })).toThrow('Duplicate draft identifier');
    const test = functionDraft().tests[0];
    if (test?.kind !== 'function') throw new Error('Fixture must have functional cases.');
    test.cases.push(structuredClone(test.cases[0]!));
    expect(() => parseCodingAuthoringDraft({ ...fixtures.function, tests: [test] })).toThrow('Duplicate draft identifier');
  });

  it('enforces file-count, per-buffer and aggregate UTF-8 budgets', () => {
    expect(() => parseCodingAuthoringDraft({ ...fixtures.function, files: Array.from({ length: 101 }, (_, index) => ({
      ...fixtures.function.files[0], id: `file-${index}`,
    })) })).toThrow();
    expect(() => parseCodingAuthoringDraft({ ...fixtures.function, problem: {
      ...fixtures.function.problem, statement: '\u00e9'.repeat(1_000_001),
    } })).toThrow('byte budget');
    const draft = functionDraft();
    draft.files = Array.from({ length: 6 }, (_, index) => ({ ...draft.files[0]!, id: `file-${index}`, content: 'x'.repeat(1_800_000) }));
    expect(() => parseCodingAuthoringDraft(draft)).toThrow('total byte budget');
  });

  it('clones protected references independently of mutable starter files', () => {
    const draft = functionDraft();
    const restored = parseCodingAuthoringDraft(draft);
    restored.validationSolutions[0]!.files[0]!.content = 'edited protected solution';
    restored.files[0]!.content = 'edited starter';
    expect(draft.validationSolutions[0]!.files[0]!.content).toContain('SYNTHETIC-TEACHER-REFERENCE');
    expect(draft.files[0]!.content).toBe('int sum(int a, int b) { return 0; }');
  });

  it('exposes a distinct authoring discriminant', () => {
    expect(CODING_DRAFT_TYPE).toBe('coding-authoring-draft');
    expect(functionDraft().type).toBe(CODING_DRAFT_TYPE);
  });
});

function functionDraft(): CodingAuthoringDraft {
  return parseCodingAuthoringDraft(fixtures.function);
}
