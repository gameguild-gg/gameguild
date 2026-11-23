import { createEmception } from '@gameguild/emception-browser';
import { generateDoctestHarness, runTests, withWorkspaceOverlay } from 'emception/testing';
import { resolveBuild, ToolchainPreset, type TestPlan } from 'emception';
import { buildAssessmentExecutionPlan } from '../../../packages/infrastructure/ui-emception/src/assessment/plan';
import { createAssessmentWorkspaceConfig, type CodingLanguage } from '../../../packages/infrastructure/ui-emception/src/assessment/presets';
import type { CodingAssessmentDefinition, CodingAssessmentParameter } from '../../../packages/infrastructure/ui-emception/src/assessment/types';
import { composeCodingNativeFunctionSupport } from '../../../packages/features/coding/src/native-function-support';
import type { CodingFunctionTest, CodingScalarType, CodingScalarValue } from '../../../packages/features/coding/src/contracts';
import { CODE_PREPARATION_BUDGET, executeBudgetedCode, validateCodeExecutionBudget, type CodeBudgetedOperation, type CodeExecutionBudget } from './execution-budget.js';
import { CODE_TOOLCHAIN_V3 } from './toolchain-binding.mjs';

const EXECUTION_BUDGET = Object.freeze({ timeLimitMs: 30_000, outputLimitBytes: 2_000_000 });

interface PublishedExecutionConfiguration {
  readonly environment: { readonly language: 'c' | 'cpp'; readonly runtime: 'wasi' | 'sdl3' | 'raylib' | 'allegro5' };
  readonly limits: CodeExecutionBudget & { readonly maxFileCount: number };
  readonly entryPoint: { readonly path: string; readonly symbol?: string };
}

interface CodeWorkerRequest {
  definition: CodingAssessmentDefinition & {
    readonly Environment: CodingAssessmentDefinition['Environment'] & { readonly Runtime?: unknown };
    readonly Execution?: unknown;
  };
  files: Record<string, { content: string; encoding: 'text' }>;
}

/** Runs only in a fresh, server-owned Chromium context. This is never a learner API. */
export async function executeCodeAssessment(request: CodeWorkerRequest): Promise<boolean[]> {
  const definition = request.definition;
  const configuration = readExecutionConfiguration(definition);
  const budget = configuration?.limits ?? EXECUTION_BUDGET;
  const canonical = configuration ? normalizeWorkerPath : (path: string) => '/home/user/' + path.replace(/^\/(?:home\/user|user)\//, '');
  if (configuration) {
    const entry = Object.entries(definition.Data.Files).find(([path]) => canonical(path) === configuration.entryPoint.path)?.[1];
    if (!entry || entry.Visibility !== 'Public' || entry.Encoding !== 'text') throw new Error('Code entry point must identify a public text source file.');
  }
  const files = Object.fromEntries(Object.entries(definition.Data.Files).map(([path, file]) => [
    canonical(path), { encoding: file.Encoding ?? 'text', content: file.Content },
  ]));
  for (const [path, file] of Object.entries(request.files)) {
    files[canonical(path)] = file;
  }
  if (configuration && Object.keys(files).length > configuration.limits.maxFileCount) throw new Error('Code workspace exceeds its frozen file-count budget.');
  const language = configuration ? nativePreset(configuration) : legacyPreset(definition.Environment.Language);
  const workspace = createAssessmentWorkspaceConfig(language, files);
  const sources = Object.keys(files).filter((path) => /\.(?:c|cc|cpp|cxx)$/i.test(path));
  if (configuration) sources.sort((left, right) => Number(right === configuration.entryPoint.path) - Number(left === configuration.entryPoint.path));
  const build = resolveBuild({ preset: workspace.compile.toolchain,
    callsite: { sources, output: workspace.compile.output } });
  if (build.toolchain === ToolchainPreset.CMake || build.toolchain === ToolchainPreset.Python) {
    throw new Error('This Code adapter requires a native compilation preset.');
  }
  const execution = composeOfficialFunctionPlan(definition, configuration, canonical);
  const api = await createEmception({ tty: 'none', manifestUrl: '/cdn/manifest.json' });
  let disposed = false;
  const dispose = () => {
    if (disposed) return;
    disposed = true;
    api.dispose();
  };
  try {
    let failed = false;
    let infrastructureFailure: unknown;
    let rejectFailure: (error: unknown) => void = () => undefined;
    const failure = new Promise<never>((_resolve, reject) => { rejectFailure = reject; });
    const protect = async (execute: CodeBudgetedOperation['execute'], preparing = false) => {
      if (failed) throw infrastructureFailure;
      try {
        return await executeBudgetedCode(budget, { execute, dispose }, preparing ? CODE_PREPARATION_BUDGET : undefined);
      } catch (error) {
        failed = true;
        infrastructureFailure = error;
        rejectFailure(error);
        throw error;
      }
    };
    const compile = api.compileAndRun.bind(api);
    let compiling = false;
    api.compileAndRun = (source, options) => protect(async (onOutput, onPhase) => {
      compiling = true;
      try {
        const result = await compile(source, { ...options, stdout: onOutput, stderr: onOutput,
          onPhase: phase => { onPhase(phase); options?.onPhase?.(phase); },
        });
        if (result.phase !== 'compile' && result.phase !== 'link' && result.phase !== 'run') {
          throw new Error('Code runtime did not report a valid execution phase.');
        }
        return result;
      } finally {
        compiling = false;
      }
    }, true);
    const run = api.run.bind(api);
    // The source-text facade delegates to api.run. Its outer compile guard
    // already observes the streams/stages; a nested student timer is incorrect.
    api.run = (command, argv, options) => {
      if (failed) return Promise.reject(infrastructureFailure);
      if (compiling) return run(command, argv, options);
      return protect(onOutput => run(command, argv, { ...options, stdout: onOutput, stderr: onOutput }));
    };
    await api.workspace.setBuild(build);
    for (const [path, file] of Object.entries(files)) {
      const data = file.encoding === 'base64'
        ? Uint8Array.from(atob(file.content), (character) => character.charCodeAt(0))
        : file.content;
      await api.workspace.writeFile(path, data);
    }
    // Build paths must refer to exactly the persisted workspace files. Header files
    // are available to includes, but are not compiled as translation units.
    const plan: TestPlan = {
      ...execution.plan,
      build,
      timeoutMsPerCase: budget.timeLimitMs,
    };
    // A terminated runtime cannot restore its scratch VFS. Failure must settle
    // independently of pending overlay cleanup; the host then closes the context.
    const report = await Promise.race([failure, withWorkspaceOverlay(api.workspace, execution.overlay,
      () => runTests(api, plan))]);
    // The reusable preview engine represents errors as failed cases. Official
    // grading must distinguish a failed student test from unavailable execution.
    if (failed) throw infrastructureFailure;
    if (report.cases.length !== plan.cases.length) throw new Error('Incomplete Code test report.');
    return report.cases.map((test) => test.passed);
  } finally {
    dispose();
  }
}

function composeOfficialFunctionPlan(
  definition: CodingAssessmentDefinition,
  configuration: PublishedExecutionConfiguration | undefined,
  canonical: (path: string) => string,
) {
  const execution = buildAssessmentExecutionPlan(definition, 'full');
  const tests = [...(definition.Tests?.Public ?? []), ...(definition.Tests?.Private ?? [])];
  const generated: { path: string; content: string }[] = [];
  const replaced = new Set<string>();
  const cases = execution.plan.cases.map((planCase, index) => {
    const test = tests[index];
    if (!configuration || test?.kind !== 'functional') return planCase;
    if (configuration.environment.runtime !== 'wasi') throw new TypeError('Native function tests require the WASI runtime.');
    const source: Pick<CodingFunctionTest, 'function' | 'cases'> = {
      function: {
        name: test.Function.FunctionName,
        parameters: (test.Function.Parameters ?? []).map(parameter => ({ name: parameter.Name, type: parameter.Type })),
        returnType: test.Function.ReturnType.Type,
      },
      cases: test.Cases.map((item, position) => ({
        id: `case-${position}`, inputs: item.Inputs.map(nativeScalar), expected: nativeScalar(item.Expected),
      })),
    };
    const language = configuration.environment.language;
    const file = language === 'c'
      ? composeCodingNativeFunctionSupport({ language, index, test: source })
      : composeCodingNativeFunctionSupport({ language, index, test: source,
        createCppHarness: bridgeSymbol => generateDoctestHarness({
          functionName: bridgeSymbol,
          parameters: source.function.parameters.map(parameter => ({ name: parameter.name, type: harnessType(parameter.type), content: undefined })),
          returnType: { type: harnessType(source.function.returnType), content: undefined },
        }, source.cases.map(item => ({
          Inputs: item.inputs.map(value => ({ type: harnessType(value.type), content: value.value })),
          Expected: { type: harnessType(item.expected.type), content: item.expected.value },
        })), { index, name: bridgeSymbol }).source,
      });
    if (planCase.kind !== 'doctest') throw new Error('Code function test mapping is inconsistent.');
    for (const path of planCase.sourceFiles) replaced.add(path);
    generated.push(file);
    return { ...planCase, sourceFiles: [file.path] };
  });
  return {
    plan: { ...execution.plan, cases },
    overlay: [...execution.overlay.filter(file => !replaced.has(file.path)).map(file => ({ ...file, path: canonical(file.path) })), ...generated],
  };
}

function nativeScalar(parameter: CodingAssessmentParameter): CodingScalarValue {
  const value = parameter.Content;
  if (parameter.Type === 'integer' && typeof value === 'number') return { type: 'integer', value };
  if (parameter.Type === 'float' && typeof value === 'string') return { type: 'float', value };
  if (parameter.Type === 'boolean' && typeof value === 'boolean') return { type: 'boolean', value };
  if (parameter.Type === 'string' && typeof value === 'string') return { type: 'string', value };
  throw new TypeError('Native function value does not match its declared type.');
}

function harnessType(type: CodingScalarType): 'Integer' | 'Float' | 'Boolean' | 'String' {
  switch (type) {
    case 'integer': return 'Integer';
    case 'float': return 'Float';
    case 'boolean': return 'Boolean';
    case 'string': return 'String';
  }
}

function readExecutionConfiguration(definition: CodeWorkerRequest['definition']): PublishedExecutionConfiguration | undefined {
  const raw = definition.Execution;
  const environment = definition.Environment;
  if (raw == null) {
    if (environment.Runtime != null) throw new Error('Explicit Code runtime requires its frozen execution configuration.');
    return undefined;
  }
  if (!record(raw) || !knownKeys(raw, ['Limits', 'EntryPoint', 'Toolchain'])
    || !record(raw.Limits) || !knownKeys(raw.Limits, ['TimeLimitMs', 'OutputLimitBytes', 'MaxFileCount', 'MemoryLimitBytes'])
    || !record(raw.EntryPoint) || !knownKeys(raw.EntryPoint, ['Path', 'Symbol'])
    || !record(raw.Toolchain) || !knownKeys(raw.Toolchain, ['Key', 'Version', 'RuntimeAbi', 'ArtifactDigest'])) throw new Error('Incomplete or unsupported Code execution configuration.');
  const language = environment.Language;
  const runtime = environment.Runtime;
  if ((language !== 'c' && language !== 'cpp')
    || (runtime !== 'wasi' && runtime !== 'sdl3' && runtime !== 'raylib' && runtime !== 'allegro5')
    || (language === 'c' && runtime !== 'wasi')) throw new Error('Unsupported explicit Code language/runtime.');
  if (environment.Tools !== (runtime === 'wasi' ? 'clang' : 'emcc')
    || (runtime === 'wasi' ? environment.LibBundle != null : environment.LibBundle !== runtime)) throw new Error('Code tools/library do not match its explicit runtime.');
  const toolchain = raw.Toolchain;
  if (toolchain.Key !== `${language}-emception` || toolchain.Version !== CODE_TOOLCHAIN_V3.artifactVersion
    || toolchain.RuntimeAbi !== CODE_TOOLCHAIN_V3.runtimeAbi
    || toolchain.ArtifactDigest !== `sha256:${CODE_TOOLCHAIN_V3.toolchainLockHash}`) throw new Error('Unsupported frozen Code toolchain identity.');
  const { TimeLimitMs: timeLimitMs, OutputLimitBytes: outputLimitBytes, MaxFileCount: maxFileCount } = raw.Limits;
  if (typeof timeLimitMs !== 'number' || typeof outputLimitBytes !== 'number'
    || typeof maxFileCount !== 'number' || !Number.isSafeInteger(maxFileCount) || maxFileCount < 1 || maxFileCount > 100
    || raw.Limits.MemoryLimitBytes != null) throw new Error('Invalid or unsupported Code execution limits.');
  validateCodeExecutionBudget({ timeLimitMs, outputLimitBytes });
  const { Path: path, Symbol: symbol } = raw.EntryPoint;
  if (typeof path !== 'string' || !/\.(?:c|cc|cpp|cxx)$/.test(path)
    || (symbol != null && (typeof symbol !== 'string' || !/^[A-Za-z_][A-Za-z0-9_]{0,127}$/.test(symbol)))) throw new Error('Invalid Code entry point.');
  return {
    environment: { language, runtime },
    limits: { timeLimitMs, outputLimitBytes, maxFileCount },
    entryPoint: { path: normalizeWorkerPath(path), ...(symbol == null ? {} : { symbol }) },
  };
}

// Deliberately mirror the published path contract without adding feature code
// to the independently fingerprinted grading artifact (CS-ADR-032).
function normalizeWorkerPath(value: string): string {
  if (typeof value !== 'string' || value !== value.trim()) throw new TypeError('Invalid Code workspace path.');
  let relative: string;
  if (value.startsWith('/home/user/')) relative = value.slice('/home/user/'.length);
  else if (value.startsWith('/user/')) relative = value.slice('/user/'.length);
  else if (value.startsWith('/')) throw new TypeError('Code path must be inside the learner workspace.');
  else relative = value;
  const segments = relative.split('/');
  if (relative.length === 0 || relative.length > 240 || relative.includes('\\') || relative.includes(':')
    || [...relative].some(character => character.charCodeAt(0) < 32)
    || segments.some(segment => segment === '' || segment === '.' || segment === '..') || segments[0] === '.gameguild') throw new TypeError('Invalid or reserved Code workspace path.');
  return `/home/user/${relative}`;
}

function nativePreset(configuration: PublishedExecutionConfiguration): CodingLanguage {
  switch (configuration.environment.runtime) {
    case 'wasi': return configuration.environment.language;
    case 'sdl3': return 'sdl-cpp';
    case 'raylib': return 'raylib-cpp';
    case 'allegro5': return 'allegro-cpp';
  }
}

function legacyPreset(language: string | undefined): CodingLanguage {
  if (language === 'c' || language === 'cpp' || language === 'sdl-cpp' || language === 'raylib-cpp' || language === 'allegro-cpp') return language;
  // Retained labels were case-insensitive in Courses, but the old preset
  // lookup used exact keys and resolved a missing key to the C++ default.
  const historicalLabel = language?.toLowerCase();
  if (historicalLabel === 'c' || historicalLabel === 'cpp' || historicalLabel === 'sdl-cpp'
    || historicalLabel === 'raylib-cpp' || historicalLabel === 'allegro-cpp') return 'cpp';
  throw new Error('This Code adapter requires a supported native compilation preset.');
}

function record(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function knownKeys(value: Record<string, unknown>, allowed: readonly string[]): boolean {
  return Object.keys(value).every(key => allowed.includes(key));
}
