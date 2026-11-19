import type {
  CodingAuthoringDefinition,
  CodingFunctionTest,
  CodingLearnerDelivery,
  CodingScalarValue,
  CodingTest,
} from '@game-guild/coding';
import type { TestCase, TestPlan } from 'emception';
import {
  generateDoctestHarness,
  type FunctionParameter,
  type FunctionalTestCase,
  type FunctionalTestSignature,
  type WorkspaceOverlayFile,
} from 'emception/testing';

export interface CodingExecutionPlan {
  readonly plan: TestPlan;
  readonly overlay: readonly WorkspaceOverlayFile[];
}

export function buildCodingExecutionPlan(
  definition: CodingAuthoringDefinition | CodingLearnerDelivery,
  scope: 'public' | 'full',
): CodingExecutionPlan {
  if (scope === 'full' && !('tests' in definition)) {
    throw new TypeError('Full coding previews require an authoring definition.');
  }
  const tests: readonly (CodingTest | CodingLearnerDelivery['publicTests'][number])[] = 'tests' in definition
    ? definition.tests.filter((test) => scope === 'full' || test.visibility === 'public')
    : definition.publicTests;
  const overlay: WorkspaceOverlayFile[] = [];
  const cases = tests.map((test, index) => toPlanCase(test, index, overlay));
  const sources = definition.files
    .filter((file) => file.encoding === 'text')
    .filter((file) => !('visibility' in file) || scope === 'full' || file.visibility === 'public')
    .map((file) => file.path);

  if ('tests' in definition && scope === 'full') {
    for (const file of definition.files) {
      if (file.visibility === 'private' && file.encoding === 'text') {
        overlay.push({ path: file.path, content: file.content });
      }
    }
  }
  return { plan: { build: { sources }, cases }, overlay };
}

function toPlanCase(test: CodingTest | CodingLearnerDelivery['publicTests'][number], index: number, overlay: WorkspaceOverlayFile[]): TestCase {
  if (test.kind === 'stdio') {
    return {
      kind: 'stdio',
      stdin: test.stdin,
      expectedStdout: test.expected.stdout,
      expectedStderr: test.expected.stderr,
      expectedExit: test.expected.exitCode,
      weight: test.weightUnits,
      name: test.name,
    };
  }
  return functionCase(test, index, overlay);
}

function functionCase(test: CodingFunctionTest | Omit<CodingFunctionTest, 'visibility'>, index: number, overlay: WorkspaceOverlayFile[]): TestCase {
  const signature: FunctionalTestSignature = {
    functionName: test.function.name,
    parameters: test.function.parameters.map((parameter) => ({
      name: parameter.name,
      type: harnessType(parameter.type),
      content: undefined,
    })),
    returnType: { type: harnessType(test.function.returnType), content: undefined },
  };
  const cases: FunctionalTestCase[] = test.cases.map((testCase) => ({
    Inputs: testCase.inputs.map(harnessValue),
    Expected: harnessValue(testCase.expected),
  }));
  const harness = generateDoctestHarness(signature, cases, { index, name: test.name });
  const path = `/home/user/${harness.filename}`;
  overlay.push({ path, content: harness.source });
  return { kind: 'doctest', sourceFiles: [path], weight: test.weightUnits, name: test.name };
}

function harnessType(type: CodingFunctionTest['function']['returnType']): FunctionParameter['type'] {
  return `${type.charAt(0).toUpperCase()}${type.slice(1)}` as FunctionParameter['type'];
}

function harnessValue(value: CodingScalarValue): FunctionParameter {
  return { type: harnessType(value.type), content: value.value };
}
