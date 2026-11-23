import {
  composeCodingNativeFunctionSupport,
  type CodingAuthoringDefinition,
  type CodingFunctionTest,
  type CodingLearnerDelivery,
  type CodingScalarValue,
  type CodingTest,
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
  const language = definition.environment?.language ?? (definition.toolchain.key === 'c-emception' ? 'c' : 'cpp');
  if (tests.some(test => test.kind === 'function')) {
    if (definition.environment?.runtime && definition.environment.runtime !== 'wasi') {
      throw new TypeError('Native function tests require the WASI runtime.');
    }
    if (definition.toolchain.key !== `${language}-emception`) {
      throw new TypeError('Native function tests require a matching C/C++ toolchain.');
    }
  }
  const overlay: WorkspaceOverlayFile[] = [];
  const cases = tests.map((test, index) => toPlanCase(test, index, overlay, language));
  const sources = definition.files
    .filter((file) => file.encoding === 'text')
    .filter((file) => !('visibility' in file) || scope === 'full' || file.visibility === 'public')
    .filter((file) => /\.(?:c|cc|cpp|cxx)$/i.test(file.path))
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

function toPlanCase(test: CodingTest | CodingLearnerDelivery['publicTests'][number], index: number, overlay: WorkspaceOverlayFile[], language: 'c' | 'cpp'): TestCase {
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
  return functionCase(test, index, overlay, language);
}

function functionCase(test: CodingFunctionTest | Omit<CodingFunctionTest, 'visibility'>, index: number, overlay: WorkspaceOverlayFile[], language: 'c' | 'cpp'): TestCase {
  const source = language === 'c'
    ? composeCodingNativeFunctionSupport({ language, index, test })
    : composeCodingNativeFunctionSupport({ language, index, test,
      createCppHarness: bridgeSymbol => cppHarness(test, index, bridgeSymbol),
    });
  overlay.push(source);
  return { kind: 'doctest', sourceFiles: [source.path], weight: test.weightUnits, name: test.name };
}

function cppHarness(test: CodingFunctionTest | Omit<CodingFunctionTest, 'visibility'>, index: number, bridgeSymbol: string): string {
  const signature: FunctionalTestSignature = {
    functionName: bridgeSymbol,
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
  return generateDoctestHarness(signature, cases, { index, name: bridgeSymbol }).source;
}

function harnessType(type: CodingFunctionTest['function']['returnType']): FunctionParameter['type'] {
  switch (type) {
    case 'integer': return 'Integer';
    case 'float': return 'Float';
    case 'boolean': return 'Boolean';
    case 'string': return 'String';
  }
}

function harnessValue(value: CodingScalarValue): FunctionParameter {
  return { type: harnessType(value.type), content: value.value };
}
