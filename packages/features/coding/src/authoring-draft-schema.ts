import { z } from 'zod';
import {
  CODING_DRAFT_TYPE,
  type CodingAuthoringDraft,
} from './authoring-draft-contracts';
import {
  MAX_CODING_DEFINITION_BYTES,
  MAX_CODING_FILES,
  MAX_CODING_FILE_CONTENT_BYTES,
  MAX_CODING_TESTS,
} from './contracts';
import { CodingValidationError } from './errors';

const identifier = z.string().regex(/^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/);
const buffer = z.string().refine(text => new TextEncoder().encode(text).byteLength <= MAX_CODING_FILE_CONTENT_BYTES,
  'Draft text exceeds its byte budget.');
const scalarType = z.enum(['string', 'boolean', 'integer', 'float']);
const scalar = z.object({ type: scalarType, text: buffer }).strict();
const signature = z.object({
  name: buffer,
  parameters: z.array(z.object({ name: buffer, type: scalarType }).strict()).max(100),
  returnType: scalarType,
}).strict();
const testBase = {
  id: identifier,
  name: buffer,
  visibility: z.enum(['public', 'private']),
  weightUnits: buffer,
};
const functionCase = z.object({
  id: identifier,
  inputs: z.array(scalar).max(100),
  expected: scalar.nullable(),
}).strict();
const draftSchema = z.object({
  type: z.literal(CODING_DRAFT_TYPE),
  schemaVersion: z.literal(1),
  exerciseType: z.enum(['function', 'standard-io']),
  environment: z.object({ language: z.enum(['c', 'cpp']), runtime: z.enum(['wasi', 'sdl3', 'raylib', 'allegro5']) }).strict(),
  problem: z.object({
    statement: buffer,
    examples: z.array(z.object({ id: identifier, input: buffer, output: buffer, explanation: buffer }).strict()).max(100),
    constraints: z.array(buffer).max(100),
  }).strict(),
  toolchain: z.object({
    key: z.string().regex(/^[A-Za-z0-9][A-Za-z0-9._+:/-]{0,127}$/),
    version: z.string().regex(/^[A-Za-z0-9][A-Za-z0-9._+:/-]{0,127}$/),
    runtimeAbi: z.string().regex(/^[A-Za-z0-9][A-Za-z0-9._+:/-]{0,127}$/),
    artifactDigest: z.templateLiteral(['sha256:', z.string().regex(/^[0-9a-f]{64}$/)]),
  }).strict(),
  files: z.array(z.object({
    id: identifier,
    path: buffer,
    encoding: z.enum(['text', 'base64']),
    content: buffer,
    mutable: z.boolean(),
    visibility: z.enum(['public', 'private']),
  }).strict()).max(MAX_CODING_FILES),
  entryPoint: z.object({ path: buffer }).strict(),
  functionSignature: signature.nullable(),
  tests: z.array(z.discriminatedUnion('kind', [
    z.object({ ...testBase, kind: z.literal('stdio'), stdin: buffer,
      expected: z.object({ stdout: buffer.nullable(), stderr: buffer.nullable(), exitCode: buffer }).strict() }).strict(),
    z.object({ ...testBase, kind: z.literal('function'), cases: z.array(functionCase).max(MAX_CODING_TESTS) }).strict(),
  ])).max(MAX_CODING_TESTS),
  validationSolutions: z.array(z.object({
    id: identifier,
    name: buffer,
    role: z.enum(['reference', 'incorrect']),
    files: z.array(z.object({ fileId: identifier, path: buffer, content: buffer }).strict()).max(MAX_CODING_FILES),
  }).strict()).max(100),
  limits: z.object({ timeLimitMs: buffer, memoryLimitBytes: buffer, outputLimitBytes: buffer, maxFileCount: buffer }).strict(),
  allowLearnerCreatedFiles: z.boolean(),
}).strict().superRefine((draft, context) => {
  const unique = (ids: readonly string[], path: PropertyKey[]) => {
    const seen = new Set<string>();
    ids.forEach((id, index) => {
      if (seen.has(id)) context.addIssue({ code: 'custom', message: 'Duplicate draft identifier.', path: [...path, index] });
      seen.add(id);
    });
  };
  unique(draft.files.map(file => file.id), ['files']);
  unique(draft.tests.map(test => test.id), ['tests']);
  unique(draft.problem.examples.map(example => example.id), ['problem', 'examples']);
  unique(draft.validationSolutions.map(solution => solution.id), ['validationSolutions']);
  draft.tests.forEach((test, index) => {
    if (test.kind === 'function') unique(test.cases.map(testCase => testCase.id), ['tests', index, 'cases']);
    if ((draft.exerciseType === 'function') !== (test.kind === 'function')) {
      context.addIssue({ code: 'custom', message: 'Draft cases must belong to its exercise family.', path: ['tests', index, 'kind'] });
    }
  });
  draft.validationSolutions.forEach((solution, index) => unique(solution.files.map(file => file.fileId), ['validationSolutions', index, 'files']));
  if (draft.exerciseType === 'standard-io' && draft.functionSignature !== null) {
    context.addIssue({ code: 'custom', message: 'Standard I/O has no function signature.', path: ['functionSignature'] });
  }
  if (new TextEncoder().encode(JSON.stringify(draft)).byteLength > MAX_CODING_DEFINITION_BYTES) {
    context.addIssue({ code: 'custom', message: 'Draft exceeds its total byte budget.' });
  }
});

export function parseCodingAuthoringDraft(value: unknown): CodingAuthoringDraft {
  const result = draftSchema.safeParse(value);
  if (!result.success) throw new CodingValidationError('Coding authoring draft', result.error.issues.map(issue => ({
    path: issue.path.length ? issue.path.join('.') : '$', message: issue.message,
  })));
  return structuredClone(result.data);
}
