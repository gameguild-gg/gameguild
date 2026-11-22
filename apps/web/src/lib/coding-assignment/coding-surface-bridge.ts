import {
  CODING_AUTHORING_SCHEMA_VERSION,
  CODING_CONTENT_TYPE,
  parseCodingAuthoringDefinition,
  projectCodingDefinitionForLearner,
  type CodingAuthoringDefinition,
  type CodingExerciseType,
  type CodingExecutionConfiguration,
  type CodingFunctionTest,
  type CodingScalarValue,
  type CodingStdioTest,
  type CodingSubmission,
  type CodingTest,
} from '@game-guild/coding';
import type {
  CodingAssignmentContent,
  BundleFileMeta,
  FunctionParameterType,
  FunctionParameterValue,
  GradingConfig,
  Test,
} from './types';
import { readWireExecutionConfiguration, writeWireExecutionConfiguration } from './execution-configuration';

const TOOLCHAIN_DIGEST = 'sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37' as const;

export function codingAuthoringDefinitionToAssignmentContent(
  definition: CodingAuthoringDefinition,
  grading: GradingConfig,
): CodingAssignmentContent {
  const source = parseCodingAuthoringDefinition(definition);
  if (!source.environment) throw new TypeError('Publishing requires an explicit coding environment.');
  if (!Number.isFinite(grading.MaxScore) || grading.MaxScore <= 0) throw new TypeError('Publishing requires a valid host-owned grading configuration.');
  const configuration: CodingExecutionConfiguration = {
    environment: source.environment, toolchain: source.toolchain, limits: source.limits, entryPoint: source.entryPoint,
  };
  const wire = writeWireExecutionConfiguration(configuration);
  return {
    Type: 'coding-assignment', Version: 1, ExerciseType: source.exerciseType,
    ...(source.problem === undefined ? {} : { Problem: {
      Statement: source.problem.statement, Constraints: [...source.problem.constraints],
      Examples: source.problem.examples.map(example => ({ Id: example.id, Input: example.input, Output: example.output, Explanation: example.explanation })),
    } }),
    ...(source.validationSolutions === undefined ? {} : { ValidationSolutions: source.validationSolutions.map(solution => ({
      Id: solution.id, Name: solution.name, Role: solution.role,
      Files: solution.files.map(file => ({ FileId: file.fileId, Path: file.path, Content: file.content })),
    })) }),
    Environment: { ...wire.Environment, AllowStudentCreateFiles: source.allowLearnerCreatedFiles },
    Execution: wire.Execution,
    Data: { Files: Object.fromEntries<BundleFileMeta>(source.files.map((file): [string, BundleFileMeta] => [file.path, {
      Id: file.id, Content: file.content, Encoding: file.encoding, Modifiable: file.mutable, Visibility: file.visibility === 'private' ? 'Private' : 'Public',
    }])) },
    Tests: { Public: source.tests.filter(test => test.visibility === 'public').map(toWireTest), Private: source.tests.filter(test => test.visibility === 'private').map(toWireTest) },
    Grading: { MaxScore: grading.MaxScore },
  };
}

export function codingAssignmentToAuthoringDefinition(
  content: CodingAssignmentContent,
): CodingAuthoringDefinition {
  return parseCodingAuthoringDefinition(codingAssignmentToDraftDefinition(content));
}

/** Builds an authoring workspace before the draft satisfies publication rules. */
export function codingAssignmentToDraftDefinition(
  content: CodingAssignmentContent,
): CodingAuthoringDefinition {
  const execution = readWireExecutionConfiguration(content.Environment, content.Execution);
  const files = Object.entries(content.Data.Files).map(([path, file]) => ({
    id: file.Id ?? stableIdentifier('file', path),
    path,
    encoding: file.Encoding,
    content: file.Content,
    mutable: file.Modifiable,
    visibility: file.Visibility === 'Private' ? 'private' as const : 'public' as const,
  }));
  const tests = [
    ...content.Tests.Public.map((test, index) => toCanonicalTest(test, 'public', index, execution !== undefined)),
    ...content.Tests.Private.map((test, index) => toCanonicalTest(test, 'private', index, execution !== undefined)),
  ];
  const entryPoint = files.find((file) => /(^|\/)main\.(?:c|cc|cpp|cxx)$/i.test(file.path) && file.visibility === 'public' && file.encoding === 'text')
    ?? files.find((file) => file.visibility === 'public' && file.encoding === 'text');
  const draftEntryPoint = entryPoint ?? files[0];
  const metadata = {
    ...(execution === undefined ? {} : { environment: execution.environment }),
    ...(content.Problem == null ? {} : { problem: {
      statement: content.Problem.Statement,
      examples: content.Problem.Examples.map(example => ({ id: example.Id, input: example.Input, output: example.Output, explanation: example.Explanation })),
      constraints: [...content.Problem.Constraints],
    } }),
    ...(content.ValidationSolutions == null ? {} : { validationSolutions: content.ValidationSolutions.map(solution => ({
      id: solution.Id, name: solution.Name, role: solution.Role,
      files: solution.Files.map(file => ({ fileId: file.FileId, path: file.Path, content: file.Content })),
    })) }),
  };
  if (!draftEntryPoint) {
    return {
      schemaVersion: CODING_AUTHORING_SCHEMA_VERSION,
      type: CODING_CONTENT_TYPE,
      exerciseType: explicitExerciseType(content.ExerciseType),
      ...metadata,
      toolchain: execution?.toolchain ?? toolchainIdentity(content.Environment.Language),
      files: [],
      tests,
      limits: execution?.limits ?? defaultLimits(),
      entryPoint: execution?.entryPoint ?? { path: '/home/user/main.cpp' },
      allowLearnerCreatedFiles: content.Environment.AllowStudentCreateFiles,
    };
  }

  return {
    schemaVersion: CODING_AUTHORING_SCHEMA_VERSION,
    type: CODING_CONTENT_TYPE,
    exerciseType: explicitExerciseType(content.ExerciseType),
    ...metadata,
    toolchain: execution?.toolchain ?? toolchainIdentity(content.Environment.Language),
    files,
    tests,
    limits: execution?.limits ?? defaultLimits(),
    entryPoint: execution?.entryPoint ?? { path: draftEntryPoint.path, ...(content.ExerciseType === 'function' ? { symbol: firstFunctionName(content) } : {}) },
    allowLearnerCreatedFiles: content.Environment.AllowStudentCreateFiles,
  };
}

export function codingAssignmentToLearnerDelivery(content: CodingAssignmentContent) {
  return projectCodingDefinitionForLearner(codingAssignmentToAuthoringDefinition(content));
}

export function applySubmissionToDefinition(
  definition: CodingAuthoringDefinition,
  submissionFiles: readonly { path: string; content: string }[],
): CodingAuthoringDefinition {
  const submitted = new Map(submissionFiles.map((file) => [canonicalPath(file.path), file.content]));
  const authoredPaths = new Set(definition.files.map((file) => canonicalPath(file.path)));
  const learnerCreatedFiles = definition.allowLearnerCreatedFiles
    ? [...submitted.entries()]
        .filter(([path]) => !authoredPaths.has(path))
        .map(([path, content]) => ({
          id: stableIdentifier('learner-file', path),
          path,
          encoding: 'text' as const,
          content,
          mutable: true,
          visibility: 'public' as const,
        }))
    : [];
  return parseCodingAuthoringDefinition({
    ...definition,
    files: [
      ...definition.files.map((file) => file.visibility === 'public' && submitted.has(canonicalPath(file.path))
        ? { ...file, content: submitted.get(canonicalPath(file.path))! }
        : file),
      ...learnerCreatedFiles,
    ],
  });
}

export function codingSubmissionToFileRecord(submission: CodingSubmission): Record<string, { content: string; encoding: 'text' }> {
  return Object.fromEntries(submission.files.map((file) => [file.path, { content: file.content, encoding: 'text' as const }]));
}

function explicitExerciseType(value: CodingAssignmentContent['ExerciseType']): CodingExerciseType {
  if (value !== 'function' && value !== 'standard-io') {
    throw new TypeError('Coding assignment exercise type is required.');
  }
  return value;
}

function toWireTest(test: CodingTest): Test {
  const weight = test.weightUnits / 100;
  if (Math.round(weight * 100) !== test.weightUnits) throw new TypeError('Coding test weight cannot roundtrip through the wire contract.');
  const base = { Id: test.id, Weight: weight, ...(test.name === undefined ? {} : { Name: test.name }) };
  if (test.kind === 'stdio') return {
    ...base, kind: 'standard', Stdin: test.stdin, Stdout: test.expected.stdout,
    ...(test.expected.stderr === undefined ? {} : { Stderr: test.expected.stderr }),
    ...(test.expected.exitCode === undefined ? {} : { ExitCode: test.expected.exitCode }),
  };
  return {
    ...base, kind: 'functional',
    Function: { FunctionName: test.function.name, Parameters: test.function.parameters.map(parameter => ({ Name: parameter.name, Type: parameter.type })), ReturnType: { Type: test.function.returnType } },
    Cases: test.cases.map(testCase => ({ Id: testCase.id, Inputs: testCase.inputs.map(value => ({ Type: value.type, Content: value.value })), Expected: { Type: testCase.expected.type, Content: testCase.expected.value } })),
  };
}

function toCanonicalTest(test: Test, visibility: 'public' | 'private', index: number, explicit: boolean): CodingTest {
  const weight = test.Weight ?? 1;
  const units = Math.round(weight * 100);
  if (explicit && (!Number.isSafeInteger(units) || units <= 0 || units / 100 !== weight)) {
    throw new TypeError('Explicit coding test weights must roundtrip without rounding or clamping.');
  }
  const common = {
    id: test.Id ?? stableIdentifier('test', `${visibility}:${index}:${JSON.stringify(test)}`),
    name: test.Name ?? undefined,
    visibility,
    weightUnits: explicit ? units : Math.max(1, units),
  };
  if (test.kind === 'standard') {
    return {
      ...common,
      kind: 'stdio',
      stdin: test.Stdin ?? '',
      expected: {
        stdout: test.Stdout,
        ...(test.Stderr == null ? {} : { stderr: test.Stderr }),
        ...(test.ExitCode == null ? {} : { exitCode: test.ExitCode }),
      },
    } satisfies CodingStdioTest;
  }
  return {
    ...common,
    kind: 'function',
    function: {
      name: test.Function.FunctionName,
      parameters: test.Function.Parameters.map((parameter) => ({ name: parameter.Name, type: parameter.Type })),
      returnType: test.Function.ReturnType.Type,
    },
    cases: test.Cases.map((testCase, caseIndex) => ({
      id: testCase.Id ?? stableIdentifier('case', `${common.id}:${caseIndex}`),
      inputs: testCase.Inputs.map((input) => scalarValue(input.Type, input.Content, explicit)),
      expected: scalarValue(testCase.Expected.Type, testCase.Expected.Content, explicit),
    })),
  } satisfies CodingFunctionTest;
}

function scalarValue(type: FunctionParameterType, value: FunctionParameterValue, explicit: boolean): CodingScalarValue {
  if (explicit) {
    if (type === 'string' && typeof value === 'string') return { type, value };
    if (type === 'boolean' && typeof value === 'boolean') return { type, value };
    if (type === 'integer' && typeof value === 'number') return { type, value };
    if (type === 'float' && typeof value === 'string') return { type, value };
    throw new TypeError('Explicit coding function values must match their declared type.');
  }
  switch (type) {
    case 'string': return { type, value: String(value) };
    case 'boolean': return { type, value: Boolean(value) };
    case 'integer': return { type, value: Number(value) };
    case 'float': return { type, value: String(value) };
  }
}

function firstFunctionName(content: CodingAssignmentContent): string | undefined {
  return [...content.Tests.Public, ...content.Tests.Private].find((test) => test.kind === 'functional')?.Function.FunctionName;
}

function toolchainKey(language: string): string {
  return `${language || 'cpp'}-emception`;
}

function toolchainIdentity(language: string) {
  return {
    key: toolchainKey(language),
    version: '4.5.2',
    runtimeAbi: 'emception-browser-v1',
    artifactDigest: TOOLCHAIN_DIGEST,
  };
}

function defaultLimits() {
  return {
    timeLimitMs: 5_000,
    outputLimitBytes: 1_048_576,
    maxFileCount: 100,
  };
}

function stableIdentifier(prefix: string, source: string): string {
  let hash = 2_166_136_261;
  for (let index = 0; index < source.length; index += 1) {
    hash ^= source.charCodeAt(index);
    hash = Math.imul(hash, 16_777_619);
  }
  return `${prefix}-${(hash >>> 0).toString(16).padStart(8, '0')}`;
}

function canonicalPath(path: string): string {
  if (path.startsWith('/home/user/')) return path;
  if (path.startsWith('/user/')) return `/home/user/${path.slice('/user/'.length)}`;
  return `/home/user/${path.replace(/^\/+/, '')}`;
}
