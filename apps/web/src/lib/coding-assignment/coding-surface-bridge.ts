import {
  CODING_AUTHORING_SCHEMA_VERSION,
  CODING_CONTENT_TYPE,
  parseCodingAuthoringDefinition,
  projectCodingDefinitionForLearner,
  type CodingAuthoringDefinition,
  type CodingExerciseType,
  type CodingFunctionTest,
  type CodingScalarValue,
  type CodingStdioTest,
  type CodingSubmission,
  type CodingTest,
} from '@game-guild/coding';
import type {
  CodingAssignmentContent,
  FunctionParameterType,
  FunctionParameterValue,
  Test,
} from './types';

const TOOLCHAIN_DIGEST = 'sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37' as const;

export function codingAssignmentToAuthoringDefinition(
  content: CodingAssignmentContent,
): CodingAuthoringDefinition {
  return parseCodingAuthoringDefinition(codingAssignmentToDraftDefinition(content));
}

/** Builds an authoring workspace before the draft satisfies publication rules. */
export function codingAssignmentToDraftDefinition(
  content: CodingAssignmentContent,
): CodingAuthoringDefinition {
  const files = Object.entries(content.Data.Files).map(([path, file]) => ({
    id: file.Id ?? stableIdentifier('file', path),
    path,
    encoding: file.Encoding,
    content: file.Content,
    mutable: file.Modifiable,
    visibility: file.Visibility === 'Private' ? 'private' as const : 'public' as const,
  }));
  const tests = [
    ...content.Tests.Public.map((test, index) => toCanonicalTest(test, 'public', index)),
    ...content.Tests.Private.map((test, index) => toCanonicalTest(test, 'private', index)),
  ];
  const entryPoint = files.find((file) => /(^|\/)main\.(?:c|cc|cpp|cxx)$/i.test(file.path) && file.visibility === 'public' && file.encoding === 'text')
    ?? files.find((file) => file.visibility === 'public' && file.encoding === 'text');
  const draftEntryPoint = entryPoint ?? files[0];
  const metadata = {
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
      toolchain: toolchainIdentity(content.Environment.Language),
      files: [],
      tests,
      limits: defaultLimits(),
      entryPoint: { path: '/home/user/main.cpp' },
      allowLearnerCreatedFiles: content.Environment.AllowStudentCreateFiles,
    };
  }

  return {
    schemaVersion: CODING_AUTHORING_SCHEMA_VERSION,
    type: CODING_CONTENT_TYPE,
    exerciseType: explicitExerciseType(content.ExerciseType),
    ...metadata,
    toolchain: toolchainIdentity(content.Environment.Language),
    files,
    tests,
    limits: defaultLimits(),
    entryPoint: { path: draftEntryPoint.path, ...(content.ExerciseType === 'function' ? { symbol: firstFunctionName(content) } : {}) },
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

function toCanonicalTest(test: Test, visibility: 'public' | 'private', index: number): CodingTest {
  const common = {
    id: test.Id ?? stableIdentifier('test', `${visibility}:${index}:${JSON.stringify(test)}`),
    name: test.Name ?? undefined,
    visibility,
    weightUnits: Math.max(1, Math.round((test.Weight ?? 1) * 100)),
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
      inputs: testCase.Inputs.map((input) => scalarValue(input.Type, input.Content)),
      expected: scalarValue(testCase.Expected.Type, testCase.Expected.Content),
    })),
  } satisfies CodingFunctionTest;
}

function scalarValue(type: FunctionParameterType, value: FunctionParameterValue): CodingScalarValue {
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
    memoryLimitBytes: 134_217_728,
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
