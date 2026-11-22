import type { CodingDraftScalar, CodingDraftTest } from './authoring-draft-contracts';
import { parseCodingAuthoringDraft } from './authoring-draft-schema';
import type { CodingAuthoringDefinition, CodingScalarValue, CodingTest } from './contracts';
import { CodingValidationError } from './errors';
import { parseCodingAuthoringDefinition, parseCodingExecutionConfiguration } from './schemas';

/** Validates an author-only draft explicitly; this never persists or publishes it. */
export function compileCodingAuthoringDraft(value: unknown): CodingAuthoringDefinition {
  const draft = parseCodingAuthoringDraft(value);
  if (draft.limits.memoryLimitBytes !== '') fail('limits.memoryLimitBytes', 'Configurable RAM limits are unavailable; the draft value was preserved.');
  const signature = draft.functionSignature;
  if (draft.exerciseType === 'function' && !signature) fail('functionSignature', 'A function requires its complete signature.');
  const tests: CodingTest[] = draft.tests.map((test, index) => {
    const path = `tests.${index}`;
    const base = {
      id: test.id,
      ...(test.name.trim() ? { name: test.name } : {}),
      visibility: test.visibility,
      weightUnits: integer(test.weightUnits, `${path}.weightUnits`),
    };
    if (test.kind === 'stdio') return stdio(test, base, path);
    if (!signature) return fail('functionSignature', 'A function requires its complete signature.');
    return { ...base, kind: 'function', function: signature, cases: test.cases.map((testCase, caseIndex) => {
      const casePath = `${path}.cases.${caseIndex}`;
      if (testCase.expected === null) fail(`${casePath}.expected`, 'A case requires its expected value.');
      return {
        id: testCase.id,
        inputs: testCase.inputs.map((input, inputIndex) => scalar(input, `${casePath}.inputs.${inputIndex}`)),
        expected: scalar(testCase.expected, `${casePath}.expected`),
      };
    }) };
  });
  const execution = parseCodingExecutionConfiguration({
    environment: draft.environment,
    toolchain: draft.toolchain,
    limits: {
      timeLimitMs: integer(draft.limits.timeLimitMs, 'limits.timeLimitMs'),
      outputLimitBytes: integer(draft.limits.outputLimitBytes, 'limits.outputLimitBytes'),
      maxFileCount: integer(draft.limits.maxFileCount, 'limits.maxFileCount'),
    },
    entryPoint: { path: draft.entryPoint.path, ...(signature ? { symbol: signature.name } : {}) },
  });
  return parseCodingAuthoringDefinition({
    schemaVersion: 1,
    type: 'coding-assignment',
    exerciseType: draft.exerciseType,
    problem: draft.problem,
    validationSolutions: draft.validationSolutions,
    environment: execution.environment,
    toolchain: execution.toolchain,
    files: draft.files,
    tests,
    limits: execution.limits,
    entryPoint: execution.entryPoint,
    allowLearnerCreatedFiles: draft.allowLearnerCreatedFiles,
  });
}

function stdio(
  test: Extract<CodingDraftTest, { kind: 'stdio' }>,
  base: Pick<CodingTest, 'id' | 'name' | 'visibility' | 'weightUnits'>,
  path: string,
): CodingTest {
  if (test.expected.stdout === null) fail(`${path}.expected.stdout`, 'A test requires expected stdout; empty output is allowed.');
  return {
    ...base, kind: 'stdio', stdin: test.stdin,
    expected: {
      stdout: test.expected.stdout,
      ...(test.expected.stderr === null ? {} : { stderr: test.expected.stderr }),
      ...(test.expected.exitCode === '' ? {} : { exitCode: integer(test.expected.exitCode, `${path}.expected.exitCode`) }),
    },
  };
}

function scalar(buffer: CodingDraftScalar, path: string): CodingScalarValue {
  switch (buffer.type) {
    case 'string': return { type: 'string', value: buffer.text };
    case 'integer': return { type: 'integer', value: integer(buffer.text, path) };
    case 'float':
      if (!/^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$/.test(buffer.text)) fail(path, 'Expected a complete decimal.');
      return { type: 'float', value: buffer.text };
    case 'boolean':
      if (buffer.text !== 'true' && buffer.text !== 'false') fail(path, 'Expected true or false.');
      return { type: 'boolean', value: buffer.text === 'true' };
  }
}

function integer(text: string, path: string): number {
  const value = Number(text);
  if (!/^-?(?:0|[1-9]\d*)$/.test(text) || !Number.isSafeInteger(value)) fail(path, 'Expected a complete safe integer.');
  return value;
}

function fail(path: string, message: string): never {
  throw new CodingValidationError('Coding draft publication', [{ path, message }]);
}
