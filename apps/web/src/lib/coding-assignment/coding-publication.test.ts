import { describe, expect, it } from 'vitest';
import fixtures from '../../../../../packages/features/coding/fixtures/coding-publication-cases.json';
import drafts from '../../../../../packages/features/coding/fixtures/coding-authoring-drafts.json';
import { compileCodingAuthoringDraft } from '@game-guild/coding';
import { codingAssignmentToAuthoringDefinition, codingAssignmentToLearnerDelivery, codingAuthoringDefinitionToAssignmentContent } from './coding-surface-bridge';
import { narrowCodingAssignmentContent, type BundleFileMeta, type CodingAssignmentContent } from './types';

function content(family: 'function' | 'standard-io' | 'legacy'): CodingAssignmentContent {
  const result = narrowCodingAssignmentContent(structuredClone(fixtures[family]));
  if (!result) throw new Error('The shared wire fixture must be accepted.');
  return result;
}

function configuredContent(family: 'function' | 'standard-io'): CodingAssignmentContent {
  const source = content(family);
  return {
    ...source,
    Environment: { ...source.Environment, Runtime: 'wasi' },
    Execution: {
      Limits: { TimeLimitMs: 4321, OutputLimitBytes: 12345, MaxFileCount: 7 },
      EntryPoint: { Path: '/home/user/answer.cpp', ...(family === 'function' ? { Symbol: 'sum' } : {}) },
      Toolchain: { Key: 'cpp-emception', Version: '4.5.2', RuntimeAbi: 'emception-browser-v1',
        ArtifactDigest: 'sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37' },
    },
    Data: { Files: Object.fromEntries<BundleFileMeta>([
      ...Object.entries(source.Data.Files).map(([path, file]): [string, BundleFileMeta] => [path.replace('/main.cpp', '/answer.cpp'), file]),
      ['/home/user/main.cpp', { Id: 'decoy-file', Content: '// not the entry point', Encoding: 'text', Visibility: 'Public', Modifiable: false }],
    ]) },
    ValidationSolutions: source.ValidationSolutions?.map(solution => ({ ...solution,
      Files: solution.Files.map(file => ({ ...file, Path: file.Path.replace('/main.cpp', '/answer.cpp') })),
    })),
  };
}

describe('UX02-C: published coding wire parity', () => {
  it('WB-B: publishes a compiled draft without inventing grading or execution configuration', () => {
    const definition = compileCodingAuthoringDraft(drafts.function);
    const wire = codingAuthoringDefinitionToAssignmentContent(definition, { MaxScore: 25 });
    expect(wire.Grading).toEqual({ MaxScore: 25 });
    expect(wire.Execution?.Limits).toEqual({ TimeLimitMs: 5000, OutputLimitBytes: 1048576, MaxFileCount: 100 });
    expect(codingAssignmentToAuthoringDefinition(wire)).toEqual(definition);
  });

  it.each(['function', 'standard-io'] as const)('WB-B: %s keeps its exact configuration through narrowing, projection and publication', family => {
    const source = configuredContent(family);
    const narrowed = narrowCodingAssignmentContent(source);
    expect(narrowed?.Execution).toEqual(source.Execution);
    const definition = codingAssignmentToAuthoringDefinition(source);
    expect(definition.environment).toEqual({ language: 'cpp', runtime: 'wasi' });
    expect(definition.limits).toEqual({ timeLimitMs: 4321, outputLimitBytes: 12345, maxFileCount: 7 });
    expect(definition.entryPoint.path).toBe('/home/user/answer.cpp');
    expect(codingAssignmentToLearnerDelivery(source).limits).toEqual(definition.limits);
    const published = codingAuthoringDefinitionToAssignmentContent(definition, { MaxScore: 25 });
    expect(published.Execution).toEqual(source.Execution);
    expect(published.Environment).toEqual(source.Environment);
    expect(published.Grading.MaxScore).toBe(25);
    expect(codingAssignmentToAuthoringDefinition(published)).toEqual(definition);
    expect(JSON.stringify(codingAssignmentToLearnerDelivery(published))).not.toContain('SYNTHETIC-');
  });

  it('WB-B: normalizes camelCase configuration without losing the frozen values', () => {
    const source = configuredContent('function');
    const execution = source.Execution!;
    const narrowed = narrowCodingAssignmentContent({ ...source, Execution: undefined,
      execution: { limits: { timeLimitMs: 4321, outputLimitBytes: 12345, maxFileCount: 7 },
        entryPoint: { path: '/home/user/answer.cpp', symbol: 'sum' },
        toolchain: { key: execution.Toolchain.Key, version: execution.Toolchain.Version, runtimeAbi: execution.Toolchain.RuntimeAbi, artifactDigest: execution.Toolchain.ArtifactDigest } },
    });
    expect(narrowed?.Execution).toEqual(execution);
  });

  it('WB-B: rejects malformed explicit configuration instead of treating it as legacy data', () => {
    const source = configuredContent('function');
    const execution = source.Execution!;
    const bad: unknown[] = [
      { ...source, Execution: {} },
      { ...source, Execution: undefined },
      { ...source, Environment: { ...source.Environment, Runtime: undefined } },
      { ...source, Environment: { ...source.Environment, Tools: 'unknown' } },
      { ...source, Execution: { ...execution, Extra: 'not supported' } },
      { ...source, Execution: { ...execution, Limits: { ...execution.Limits, MemoryLimitBytes: 134217728 } } },
      { ...source, Execution: { ...execution, Limits: { ...execution.Limits, TimeLimitMs: 30001 } } },
      { ...source, Execution: { ...execution, Toolchain: { ...execution.Toolchain, Version: 'future' } } },
    ];
    for (const candidate of bad) expect(narrowCodingAssignmentContent(candidate)).toBeNull();
  });

  it.each([0, -1, 0.001, 1.005, Number.NaN, Number.POSITIVE_INFINITY])('WB-B: rejects a lossy explicit test weight %s', weight => {
    const source = configuredContent('function');
    const changed = { ...source, Tests: { ...source.Tests, Public: source.Tests.Public.map(test => ({ ...test, Weight: weight })) } };
    expect(() => codingAssignmentToAuthoringDefinition(changed)).toThrow();
  });

  it('WB-B: rejects type-coerced function values in explicitly configured content', () => {
    const source = configuredContent('function');
    const changed = { ...source, Tests: { ...source.Tests, Public: source.Tests.Public.map(test => test.kind === 'functional'
      ? { ...test, Cases: test.Cases.map(testCase => ({ ...testCase, Inputs: testCase.Inputs.map(input => ({ ...input, Content: '2' })) })) }
      : test) } };
    expect(() => codingAssignmentToAuthoringDefinition(changed)).toThrow();
  });

  it.each(['dropped-test', 'weight', 'stdin', 'stderr', 'exit-code'] as const)('WB-B: explicit configuration without problem metadata rejects lossy %s narrowing', mutation => {
    const source = configuredContent('standard-io');
    const test = source.Tests.Public[0];
    const altered = mutation === 'dropped-test' ? { ...test, kind: 'unsupported' }
      : { ...test, ...({ weight: { Weight: '1' }, stdin: { Stdin: 3 }, stderr: { Stderr: false }, 'exit-code': { ExitCode: '0' } }[mutation]) };
    expect(narrowCodingAssignmentContent({ ...source, Problem: undefined, Tests: { ...source.Tests, Private: [altered] } })).toBeNull();
  });

  it('WB-B: legacy reads do not advertise an invented RAM limit or add new wire fields', () => {
    const source = content('legacy');
    expect(narrowCodingAssignmentContent(source)).toEqual(source);
    expect(codingAssignmentToAuthoringDefinition(source).limits.memoryLimitBytes).toBeUndefined();
    expect(Object.hasOwn(source, 'Execution')).toBe(false);
  });

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
