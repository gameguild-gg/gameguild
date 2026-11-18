import { z } from 'zod';
import {
  CODING_AUTHORING_SCHEMA_VERSION,
  CODING_CONTENT_TYPE,
  CODING_DELIVERY_SCHEMA_VERSION,
  CODING_EXECUTION_REPORT_SCHEMA_VERSION,
  CODING_SUBMISSION_SCHEMA_VERSION,
  MAX_CODING_DEFINITION_BYTES,
  MAX_CODING_FILE_CONTENT_BYTES,
  MAX_CODING_FILES,
  MAX_CODING_SUBMISSION_BYTES,
  MAX_CODING_TESTS,
  type CodingAuthoringDefinitionV1,
  type CodingExecutionReportV1,
  type CodingLearnerDeliveryV1,
  type CodingSubmissionV1,
} from './contracts';
import { CodingValidationError } from './errors';
import { normalizeCodingPath } from './paths';

const identifierPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/;
const toolchainPartPattern = /^[A-Za-z0-9][A-Za-z0-9._+:/-]{0,127}$/;
const functionNamePattern = /^[A-Za-z_][A-Za-z0-9_]{0,127}$/;
const decimalPattern = /^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$/;
const sha256DigestPattern = /^sha256:[0-9a-f]{64}$/;
const sha256Pattern = /^[0-9a-f]{64}$/;

const identifier = z.string().regex(identifierPattern, 'Expected a stable identifier.');
const boundedText = z.string().refine((value) => byteLength(value) <= MAX_CODING_FILE_CONTENT_BYTES, 'Text exceeds its UTF-8 byte budget.');
const positiveSafeInteger = z.number().int().positive().max(Number.MAX_SAFE_INTEGER);
const nonNegativeSafeInteger = z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER);

const toolchainSchema = z
  .object({
    key: z.string().regex(toolchainPartPattern),
    version: z.string().regex(toolchainPartPattern),
    runtimeAbi: z.string().regex(toolchainPartPattern),
    artifactDigest: z.string().regex(sha256DigestPattern),
  })
  .strict();

const fileEncodingSchema = z.enum(['text', 'base64']);
const visibilitySchema = z.enum(['public', 'private']);
const pathSchema = z.string().transform((value, context) => {
  try {
    return normalizeCodingPath(value);
  } catch (error) {
    context.addIssue({
      code: 'custom',
      message: error instanceof Error ? error.message : 'Invalid coding path.',
    });
    return z.NEVER;
  }
});

const authoringFileSchema = z
  .object({
    id: identifier,
    path: pathSchema,
    encoding: fileEncodingSchema,
    content: boundedText,
    mutable: z.boolean(),
    visibility: visibilitySchema,
  })
  .strict()
  .superRefine((file, context) => {
    if (file.mutable && (file.visibility !== 'public' || file.encoding !== 'text')) {
      context.addIssue({
        code: 'custom',
        message: 'Mutable files must be public text files.',
        path: ['mutable'],
      });
    }
    if (file.encoding === 'base64' && !isBase64(file.content)) {
      context.addIssue({
        code: 'custom',
        message: 'Base64 file content is malformed.',
        path: ['content'],
      });
    }
  });

const limitsSchema = z
  .object({
    timeLimitMs: positiveSafeInteger,
    memoryLimitBytes: positiveSafeInteger,
    outputLimitBytes: positiveSafeInteger,
    maxFileCount: positiveSafeInteger.max(MAX_CODING_FILES),
  })
  .strict();

const entryPointSchema = z
  .object({
    path: pathSchema,
    symbol: z.string().regex(functionNamePattern).optional(),
  })
  .strict();

const scalarTypeSchema = z.enum(['string', 'boolean', 'integer', 'float']);
const scalarValueSchema = z.discriminatedUnion('type', [
  z.object({ type: z.literal('string'), value: z.string() }).strict(),
  z.object({ type: z.literal('boolean'), value: z.boolean() }).strict(),
  z.object({ type: z.literal('integer'), value: z.number().int().safe() }).strict(),
  z.object({ type: z.literal('float'), value: z.string().regex(decimalPattern) }).strict(),
]);

const testBase = {
  id: identifier,
  name: z.string().min(1).max(256).optional(),
  visibility: visibilitySchema,
  weightUnits: positiveSafeInteger,
};

const stdioTestSchema = z
  .object({
    ...testBase,
    kind: z.literal('stdio'),
    stdin: boundedText,
    expected: z
      .object({
        stdout: boundedText,
        stderr: boundedText.optional(),
        exitCode: z.number().int().safe().optional(),
      })
      .strict(),
  })
  .strict();

const functionParameterSchema = z
  .object({
    name: z.string().regex(functionNamePattern),
    type: scalarTypeSchema,
  })
  .strict();

const functionCaseSchema = z
  .object({
    id: identifier,
    inputs: z.array(scalarValueSchema),
    expected: scalarValueSchema,
  })
  .strict();

const functionTestObjectSchema = z
  .object({
    ...testBase,
    kind: z.literal('function'),
    function: z
      .object({
        name: z.string().regex(functionNamePattern),
        parameters: z.array(functionParameterSchema),
        returnType: scalarTypeSchema,
      })
      .strict(),
    cases: z.array(functionCaseSchema).min(1),
  })
  .strict();

const functionTestSchema = functionTestObjectSchema.superRefine(validateFunctionTest);

function validateFunctionTest(test: Pick<z.infer<typeof functionTestObjectSchema>, 'function' | 'cases'>, context: z.RefinementCtx): void {
  const parameterNames = test.function.parameters.map(({ name }) => name);
  addDuplicateIssues(parameterNames, context, ['function', 'parameters'], 'parameter name');
  const caseIds = test.cases.map(({ id }) => id);
  addDuplicateIssues(caseIds, context, ['cases'], 'case ID');
  test.cases.forEach((testCase, caseIndex) => {
    if (testCase.inputs.length !== test.function.parameters.length) {
      context.addIssue({
        code: 'custom',
        message: 'Case input count must match the function parameter count.',
        path: ['cases', caseIndex, 'inputs'],
      });
      return;
    }
    testCase.inputs.forEach((input, inputIndex) => {
      if (input.type !== test.function.parameters[inputIndex]?.type) {
        context.addIssue({
          code: 'custom',
          message: 'Case input type must match its function parameter type.',
          path: ['cases', caseIndex, 'inputs', inputIndex, 'type'],
        });
      }
    });
    if (testCase.expected.type !== test.function.returnType) {
      context.addIssue({
        code: 'custom',
        message: 'Case expected type must match the function return type.',
        path: ['cases', caseIndex, 'expected', 'type'],
      });
    }
  });
}

const testSchema = z.discriminatedUnion('kind', [stdioTestSchema, functionTestSchema]);

const authoringSchema = z
  .object({
    schemaVersion: z.literal(CODING_AUTHORING_SCHEMA_VERSION),
    type: z.literal(CODING_CONTENT_TYPE),
    toolchain: toolchainSchema,
    files: z.array(authoringFileSchema).min(1).max(MAX_CODING_FILES),
    tests: z.array(testSchema).min(1).max(MAX_CODING_TESTS),
    limits: limitsSchema,
    entryPoint: entryPointSchema,
    allowLearnerCreatedFiles: z.boolean(),
  })
  .strict()
  .superRefine((definition, context) => {
    addDuplicateIssues(
      definition.files.map(({ id }) => id),
      context,
      ['files'],
      'file ID',
    );
    addDuplicateIssues(
      definition.files.map(({ path }) => path),
      context,
      ['files'],
      'file path',
    );
    addDuplicateIssues(
      definition.tests.map(({ id }) => id),
      context,
      ['tests'],
      'test ID',
    );
    if (definition.files.length > definition.limits.maxFileCount) {
      context.addIssue({
        code: 'custom',
        message: 'Authored files exceed limits.maxFileCount.',
        path: ['limits', 'maxFileCount'],
      });
    }
    if (byteLength(JSON.stringify(definition)) > MAX_CODING_DEFINITION_BYTES) {
      context.addIssue({
        code: 'custom',
        message: 'Coding definition exceeds its byte budget.',
      });
    }
    const entryPoint = definition.files.find(({ path }) => path === definition.entryPoint.path);
    if (!entryPoint || entryPoint.encoding !== 'text' || entryPoint.visibility !== 'public') {
      context.addIssue({
        code: 'custom',
        message: 'Entry point must reference a public text file.',
        path: ['entryPoint', 'path'],
      });
    }
  });

const learnerFileSchema = z
  .object({
    id: identifier,
    path: pathSchema,
    encoding: fileEncodingSchema,
    content: boundedText,
    mutable: z.boolean(),
  })
  .strict()
  .superRefine((file, context) => {
    if (file.mutable && file.encoding !== 'text') {
      context.addIssue({
        code: 'custom',
        message: 'Mutable learner files must be text files.',
        path: ['mutable'],
      });
    }
  });

const learnerStdioTestSchema = stdioTestSchema.omit({ visibility: true });
const learnerFunctionTestObjectSchema = functionTestObjectSchema.omit({ visibility: true });
const learnerFunctionTestSchema = learnerFunctionTestObjectSchema.superRefine(validateFunctionTest);
const learnerTestSchema = z.discriminatedUnion('kind', [learnerStdioTestSchema, learnerFunctionTestSchema]);

const learnerDeliverySchema = z
  .object({
    schemaVersion: z.literal(CODING_DELIVERY_SCHEMA_VERSION),
    type: z.literal(CODING_CONTENT_TYPE),
    toolchain: toolchainSchema,
    files: z.array(learnerFileSchema).min(1).max(MAX_CODING_FILES),
    publicTests: z.array(learnerTestSchema).max(MAX_CODING_TESTS),
    limits: limitsSchema,
    entryPoint: entryPointSchema,
    allowLearnerCreatedFiles: z.boolean(),
  })
  .strict()
  .superRefine((delivery, context) => {
    addDuplicateIssues(
      delivery.files.map(({ id }) => id),
      context,
      ['files'],
      'file ID',
    );
    addDuplicateIssues(
      delivery.files.map(({ path }) => path),
      context,
      ['files'],
      'file path',
    );
    addDuplicateIssues(
      delivery.publicTests.map(({ id }) => id),
      context,
      ['publicTests'],
      'test ID',
    );
    if (delivery.files.length > delivery.limits.maxFileCount) {
      context.addIssue({
        code: 'custom',
        message: 'Learner files exceed limits.maxFileCount.',
        path: ['limits', 'maxFileCount'],
      });
    }
    const entryPoint = delivery.files.find(({ path }) => path === delivery.entryPoint.path);
    if (!entryPoint || entryPoint.encoding !== 'text') {
      context.addIssue({
        code: 'custom',
        message: 'Entry point must reference a learner-visible text file.',
        path: ['entryPoint', 'path'],
      });
    }
  });

const submissionFileSchema = z
  .object({
    fileId: identifier,
    path: pathSchema,
    encoding: z.literal('text'),
    content: boundedText,
  })
  .strict();

const submissionSchema = z
  .object({
    schemaVersion: z.literal(CODING_SUBMISSION_SCHEMA_VERSION),
    files: z.array(submissionFileSchema).max(MAX_CODING_FILES),
  })
  .strict()
  .superRefine((submission, context) => {
    addDuplicateIssues(
      submission.files.map(({ fileId }) => fileId),
      context,
      ['files'],
      'file ID',
    );
    addDuplicateIssues(
      submission.files.map(({ path }) => path),
      context,
      ['files'],
      'file path',
    );
    if (byteLength(JSON.stringify(submission)) > MAX_CODING_SUBMISSION_BYTES) {
      context.addIssue({ code: 'custom', message: 'Submission exceeds its byte budget.' });
    }
  });

const testOutcomeSchema = z
  .object({
    testId: identifier,
    state: z.enum(['passed', 'failed', 'error', 'not-run']),
    durationMs: nonNegativeSafeInteger,
    evidenceRef: z.string().min(1).max(512).optional(),
    feedback: z.string().max(4_000).optional(),
  })
  .strict();

const executionReportSchema = z
  .object({
    schemaVersion: z.literal(CODING_EXECUTION_REPORT_SCHEMA_VERSION),
    reportId: identifier,
    executionState: z.enum(['completed', 'partial', 'executor-error', 'cancelled', 'timed-out']),
    toolchain: toolchainSchema,
    receiptHash: z.string().regex(sha256Pattern).optional(),
    outcomes: z.array(testOutcomeSchema).max(MAX_CODING_TESTS),
    failureCode: identifier.optional(),
  })
  .strict()
  .superRefine((report, context) => {
    addDuplicateIssues(
      report.outcomes.map(({ testId }) => testId),
      context,
      ['outcomes'],
      'test outcome ID',
    );
    if ((report.executionState === 'completed' || report.executionState === 'partial') && !report.receiptHash) {
      context.addIssue({
        code: 'custom',
        message: 'Completed or partial reports require a receiptHash.',
        path: ['receiptHash'],
      });
    }
    if (report.executionState === 'completed' && report.outcomes.some(({ state }) => state === 'error' || state === 'not-run')) {
      context.addIssue({
        code: 'custom',
        message: 'A completed report may contain only passed or failed outcomes.',
        path: ['outcomes'],
      });
    }
    if (['executor-error', 'cancelled', 'timed-out'].includes(report.executionState) && !report.failureCode) {
      context.addIssue({
        code: 'custom',
        message: 'Non-result execution states require a failureCode.',
        path: ['failureCode'],
      });
    }
  });

export function parseCodingAuthoringDefinition(value: unknown): CodingAuthoringDefinitionV1 {
  return parse(authoringSchema, value, 'Coding authoring definition') as CodingAuthoringDefinitionV1;
}

export function parseCodingLearnerDelivery(value: unknown): CodingLearnerDeliveryV1 {
  return parse(learnerDeliverySchema, value, 'Coding learner delivery') as CodingLearnerDeliveryV1;
}

export function parseCodingSubmission(value: unknown): CodingSubmissionV1 {
  return parse(submissionSchema, value, 'Coding submission') as CodingSubmissionV1;
}

export function parseCodingExecutionReport(value: unknown): CodingExecutionReportV1 {
  return parse(executionReportSchema, value, 'Coding execution report') as CodingExecutionReportV1;
}

function parse(schema: z.ZodType, value: unknown, label: string): unknown {
  const result = schema.safeParse(value);
  if (result.success) return structuredClone(result.data);
  throw new CodingValidationError(
    label,
    result.error.issues.map((issue) => ({
      path: issue.path.length > 0 ? issue.path.join('.') : '$',
      message: issue.message,
    })),
  );
}

function addDuplicateIssues(values: readonly string[], context: z.RefinementCtx, path: PropertyKey[], label: string): void {
  const seen = new Set<string>();
  values.forEach((value, index) => {
    if (seen.has(value)) {
      context.addIssue({
        code: 'custom',
        message: `Duplicate ${label}: ${value}.`,
        path: [...path, index],
      });
    }
    seen.add(value);
  });
}

function isBase64(value: string): boolean {
  if (value.length === 0) return true;
  if (value.length % 4 !== 0 || !/^[A-Za-z0-9+/]*={0,2}$/.test(value)) return false;
  const padding = value.endsWith('==') ? 2 : value.endsWith('=') ? 1 : 0;
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
  if (padding === 2) return alphabet.indexOf(value.at(-3) ?? '') % 16 === 0;
  if (padding === 1) return alphabet.indexOf(value.at(-2) ?? '') % 4 === 0;
  return true;
}

function byteLength(value: string): number {
  return new TextEncoder().encode(value).byteLength;
}
