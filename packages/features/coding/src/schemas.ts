import { z } from 'zod';
import {
  CODING_AUTHORING_SCHEMA_VERSION,
  CODING_CONTENT_TYPE,
  CODING_DELIVERY_SCHEMA_VERSION,
  CODING_EXECUTION_REPORT_SCHEMA_VERSION,
  CODING_SUBMISSION_SCHEMA_VERSION,
  CODING_TOOLCHAIN,
  MAX_CODING_DEFINITION_BYTES,
  MAX_CODING_FILE_CONTENT_BYTES,
  MAX_CODING_FILES,
  MAX_CODING_SUBMISSION_BYTES,
  MAX_CODING_TESTS,
  MAX_CODING_TIME_LIMIT_MS,
  MAX_CODING_OUTPUT_LIMIT_BYTES,
  type CodingAuthoringDefinition,
  type CodingExecutionReport,
  type CodingExecutionConfiguration,
  type CodingLearnerDelivery,
  type CodingProblem,
  type CodingSubmission,
} from './contracts';
import { CodingValidationError } from './errors';
import { normalizeCodingPath } from './paths';

const identifierPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/;
const toolchainPartPattern = /^[A-Za-z0-9][A-Za-z0-9._+:/-]{0,127}$/;
const functionNamePattern = /^[A-Za-z_][A-Za-z0-9_]{0,127}$/;
const decimalPattern = /^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?$/;
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
    artifactDigest: z.templateLiteral(['sha256:', z.string().regex(sha256Pattern)]),
  })
  .strict();

const fileEncodingSchema = z.enum(['text', 'base64']);
const visibilitySchema = z.enum(['public', 'private']);
const exerciseTypeSchema = z.enum(['function', 'standard-io']);
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
    memoryLimitBytes: positiveSafeInteger.optional(),
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

const environmentSchema = z.object({
  language: z.enum(['c', 'cpp']),
  runtime: z.enum(['wasi', 'sdl3', 'raylib', 'allegro5']),
}).strict();

const executionConfigurationSchema = z.object({
  environment: environmentSchema,
  toolchain: toolchainSchema,
  limits: limitsSchema.superRefine((limits, context) => {
    if (limits.memoryLimitBytes !== undefined) context.addIssue({ code: 'custom', path: ['memoryLimitBytes'], message: 'Configurable RAM limits are unavailable.' });
    if (limits.timeLimitMs > MAX_CODING_TIME_LIMIT_MS) context.addIssue({ code: 'custom', path: ['timeLimitMs'], message: 'Time limit exceeds the supported execution budget.' });
    if (limits.outputLimitBytes > MAX_CODING_OUTPUT_LIMIT_BYTES) context.addIssue({ code: 'custom', path: ['outputLimitBytes'], message: 'Output limit exceeds the supported execution budget.' });
  }).transform(({ memoryLimitBytes, ...limits }) => {
    void memoryLimitBytes;
    return limits;
  }),
  entryPoint: entryPointSchema.refine(entry => /\.(?:c|cc|cpp|cxx)$/.test(entry.path), 'Entry point must be a native source file.'),
}).strict().superRefine((configuration, context) => {
  const { environment, toolchain } = configuration;
  if (environment.language === 'c' && environment.runtime !== 'wasi') {
    context.addIssue({ code: 'custom', path: ['environment'], message: 'This language/runtime combination is unavailable.' });
  }
  if (toolchain.key !== `${environment.language}-emception` || toolchain.version !== CODING_TOOLCHAIN.version
    || toolchain.runtimeAbi !== CODING_TOOLCHAIN.runtimeAbi || toolchain.artifactDigest !== CODING_TOOLCHAIN.artifactDigest) {
    context.addIssue({ code: 'custom', path: ['toolchain'], message: 'Execution requires the exact supported toolchain identity.' });
  }
});

const problemSchema = z.object({
  statement: boundedText.refine(value => value.trim().length > 0, 'A published problem needs a statement.'),
  examples: z.array(z.object({
    id: identifier, input: boundedText, output: boundedText, explanation: boundedText,
  }).strict()).max(100),
  constraints: z.array(boundedText.refine(value => value.trim().length > 0, 'A constraint must not be blank.')).max(100),
}).strict().superRefine((problem, context) => {
  addDuplicateIssues(problem.examples.map(example => example.id), context, ['examples'], 'example ID');
});

const validationSolutionsSchema = z.array(z.object({
  id: identifier,
  name: z.string().min(1).max(256).refine(value => value.trim().length > 0, 'A solution needs a name.'),
  role: z.enum(['reference', 'incorrect']),
  files: z.array(z.object({ fileId: identifier, path: pathSchema, content: boundedText }).strict()).min(1).max(MAX_CODING_FILES),
}).strict()).max(100);

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
    exerciseType: exerciseTypeSchema,
    environment: environmentSchema.optional(),
    problem: problemSchema.optional(),
    validationSolutions: validationSolutionsSchema.optional(),
    toolchain: toolchainSchema,
    files: z.array(authoringFileSchema).min(1).max(MAX_CODING_FILES),
    tests: z.array(testSchema).min(1).max(MAX_CODING_TESTS),
    limits: limitsSchema,
    entryPoint: entryPointSchema,
    allowLearnerCreatedFiles: z.boolean(),
  })
  .strict()
  .superRefine(validateAuthoringDefinition);

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
    exerciseType: exerciseTypeSchema,
    environment: environmentSchema.optional(),
    problem: problemSchema.optional(),
    toolchain: toolchainSchema,
    files: z.array(learnerFileSchema).min(1).max(MAX_CODING_FILES),
    publicTests: z.array(learnerTestSchema).max(MAX_CODING_TESTS),
    limits: limitsSchema,
    entryPoint: entryPointSchema,
    allowLearnerCreatedFiles: z.boolean(),
  })
  .strict()
  .superRefine(validateLearnerDelivery);

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

export function parseCodingAuthoringDefinition(value: unknown): CodingAuthoringDefinition {
  return parse(authoringSchema, value, 'Coding authoring definition');
}

export function parseCodingExecutionConfiguration(value: unknown): CodingExecutionConfiguration {
  return parse(executionConfigurationSchema, value, 'Coding execution configuration');
}

export function parseCodingLearnerDelivery(value: unknown): CodingLearnerDelivery {
  return parse(learnerDeliverySchema, value, 'Coding learner delivery');
}

export function parseCodingSubmission(value: unknown): CodingSubmission {
  return parse(submissionSchema, value, 'Coding submission');
}

export function parseCodingExecutionReport(value: unknown): CodingExecutionReport {
  return parse(executionReportSchema, value, 'Coding execution report');
}

export function parseCodingProblem(value: unknown): CodingProblem {
  return parse(problemSchema, value, 'Coding problem');
}

function parse<T>(schema: z.ZodType<T>, value: unknown, label: string): T {
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

function validateAuthoringDefinition(
  definition: {
    exerciseType: z.infer<typeof exerciseTypeSchema>;
    environment?: z.infer<typeof environmentSchema>;
    toolchain: z.infer<typeof toolchainSchema>;
    files: z.infer<typeof authoringFileSchema>[];
    tests: z.infer<typeof testSchema>[];
    limits: z.infer<typeof limitsSchema>;
    entryPoint: z.infer<typeof entryPointSchema>;
    problem?: z.infer<typeof problemSchema>;
    validationSolutions?: z.infer<typeof validationSolutionsSchema>;
  },
  context: z.RefinementCtx,
): void {
  validateExplicitExecution(definition, context);
  addDuplicateIssues(definition.files.map(({ id }) => id), context, ['files'], 'file ID');
  addDuplicateIssues(definition.files.map(({ path }) => path), context, ['files'], 'file path');
  addDuplicateIssues(definition.tests.map(({ id }) => id), context, ['tests'], 'test ID');
  const solutions = definition.validationSolutions ?? [];
  if (solutions.length > 0 && definition.problem === undefined) {
    context.addIssue({ code: 'custom', message: 'Protected solutions require their published problem.', path: ['problem'] });
  }
  if (definition.problem !== undefined || definition.environment !== undefined) {
    const first = definition.tests[0];
    const signature = first?.kind === 'function' ? JSON.stringify(first.function) : undefined;
    if (definition.tests.some(test => definition.exerciseType === 'function'
      ? test.kind !== 'function' || JSON.stringify(test.function) !== signature
      : test.kind !== 'stdio')) {
      context.addIssue({ code: 'custom', message: 'A published problem requires one exercise family and coherent signature.', path: ['tests'] });
    }
    if (definition.exerciseType === 'function' ? first?.kind !== 'function' || definition.entryPoint.symbol !== first.function.name
      : definition.entryPoint.symbol !== undefined) {
      context.addIssue({ code: 'custom', message: 'The entry point must match the published exercise signature.', path: ['entryPoint'] });
    }
  }
  addDuplicateIssues(solutions.map(solution => solution.id), context, ['validationSolutions'], 'solution ID');
  solutions.forEach((solution, index) => {
    addDuplicateIssues(solution.files.map(file => file.fileId), context, ['validationSolutions', index, 'files'], 'solution file ID');
    solution.files.forEach((replacement, fileIndex) => {
      const file = definition.files.find(authored => authored.id === replacement.fileId);
      if (!file || file.path !== replacement.path || !file.mutable || file.visibility !== 'public' || file.encoding !== 'text') {
        context.addIssue({ code: 'custom', message: 'A solution may replace only its identified public editable text file.',
          path: ['validationSolutions', index, 'files', fileIndex] });
      }
    });
  });
  if (definition.files.length > definition.limits.maxFileCount) {
    context.addIssue({ code: 'custom', message: 'Authored files exceed limits.maxFileCount.', path: ['limits', 'maxFileCount'] });
  }
  if (byteLength(JSON.stringify(definition)) > MAX_CODING_DEFINITION_BYTES) {
    context.addIssue({ code: 'custom', message: 'Coding definition exceeds its byte budget.' });
  }
  const entryPoint = definition.files.find(({ path }) => path === definition.entryPoint.path);
  if (!entryPoint || entryPoint.encoding !== 'text' || entryPoint.visibility !== 'public') {
    context.addIssue({ code: 'custom', message: 'Entry point must reference a public text file.', path: ['entryPoint', 'path'] });
  }
}

function validateLearnerDelivery(
  delivery: {
    environment?: z.infer<typeof environmentSchema>;
    toolchain: z.infer<typeof toolchainSchema>;
    files: z.infer<typeof learnerFileSchema>[];
    publicTests: z.infer<typeof learnerTestSchema>[];
    limits: z.infer<typeof limitsSchema>;
    entryPoint: z.infer<typeof entryPointSchema>;
  },
  context: z.RefinementCtx,
): void {
  validateExplicitExecution(delivery, context);
  addDuplicateIssues(delivery.files.map(({ id }) => id), context, ['files'], 'file ID');
  addDuplicateIssues(delivery.files.map(({ path }) => path), context, ['files'], 'file path');
  addDuplicateIssues(delivery.publicTests.map(({ id }) => id), context, ['publicTests'], 'test ID');
  if (delivery.files.length > delivery.limits.maxFileCount) {
    context.addIssue({ code: 'custom', message: 'Learner files exceed limits.maxFileCount.', path: ['limits', 'maxFileCount'] });
  }
  const entryPoint = delivery.files.find(({ path }) => path === delivery.entryPoint.path);
  if (!entryPoint || entryPoint.encoding !== 'text') {
    context.addIssue({ code: 'custom', message: 'Entry point must reference a learner-visible text file.', path: ['entryPoint', 'path'] });
  }
}

function validateExplicitExecution(value: {
  environment?: z.infer<typeof environmentSchema>;
  toolchain: z.infer<typeof toolchainSchema>;
  limits: z.infer<typeof limitsSchema>;
  entryPoint: z.infer<typeof entryPointSchema>;
}, context: z.RefinementCtx): void {
  if (value.environment === undefined) return;
  const result = executionConfigurationSchema.safeParse({
    environment: value.environment, toolchain: value.toolchain, limits: value.limits, entryPoint: value.entryPoint,
  });
  if (!result.success) result.error.issues.forEach(issue => context.addIssue({ code: 'custom', path: issue.path, message: issue.message }));
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
