import { parseCodingExecutionConfiguration, type CodingExecutionConfiguration as ExecutionConfiguration } from '@game-guild/coding';
import type { CodingEnvironment, CodingExecutionConfiguration } from './types';

export function readWireExecutionConfiguration(environment: unknown, execution: unknown): ExecutionConfiguration | undefined {
  if (!record(environment)) throw new TypeError('Coding environment is required.');
  if (execution == null) {
    if (environment.Runtime != null) throw new TypeError('Explicit runtime requires its complete execution configuration.');
    return undefined;
  }
  if (!record(execution) || !keys(execution, ['Limits', 'EntryPoint', 'Toolchain'])
    || !record(execution.Limits) || !keys(execution.Limits, ['TimeLimitMs', 'OutputLimitBytes', 'MaxFileCount', 'MemoryLimitBytes'])
    || !record(execution.EntryPoint) || !keys(execution.EntryPoint, ['Path', 'Symbol'])
    || !record(execution.Toolchain) || !keys(execution.Toolchain, ['Key', 'Version', 'RuntimeAbi', 'ArtifactDigest'])) {
    throw new TypeError('Coding execution configuration is incomplete or contains unknown fields.');
  }
  const limits = execution.Limits;
  const entry = execution.EntryPoint;
  const toolchain = execution.Toolchain;
  const configuration = parseCodingExecutionConfiguration({
    environment: { language: environment.Language, runtime: environment.Runtime },
    toolchain: { key: toolchain.Key, version: toolchain.Version, runtimeAbi: toolchain.RuntimeAbi, artifactDigest: toolchain.ArtifactDigest },
    limits: { timeLimitMs: limits.TimeLimitMs, outputLimitBytes: limits.OutputLimitBytes, maxFileCount: limits.MaxFileCount,
      ...(limits.MemoryLimitBytes == null ? {} : { memoryLimitBytes: limits.MemoryLimitBytes }) },
    entryPoint: { path: entry.Path, ...(entry.Symbol == null ? {} : { symbol: entry.Symbol }) },
  });
  const runtime = configuration.environment.runtime;
  if (environment.Tools !== (runtime === 'wasi' ? 'clang' : 'emcc')
    || (runtime === 'wasi' ? environment.LibBundle != null : environment.LibBundle !== runtime)) {
    throw new TypeError('Coding environment tools/library do not match its explicit runtime.');
  }
  return configuration;
}

export function writeWireExecutionConfiguration(configuration: ExecutionConfiguration): {
  Environment: Omit<CodingEnvironment, 'AllowStudentCreateFiles'>;
  Execution: CodingExecutionConfiguration;
} {
  const source = parseCodingExecutionConfiguration(configuration);
  const { environment, toolchain, limits, entryPoint } = source;
  return {
    Environment: { Language: environment.language, Runtime: environment.runtime,
      Tools: environment.runtime === 'wasi' ? 'clang' : 'emcc', LibBundle: environment.runtime === 'wasi' ? null : environment.runtime },
    Execution: {
      Limits: { TimeLimitMs: limits.timeLimitMs, OutputLimitBytes: limits.outputLimitBytes, MaxFileCount: limits.maxFileCount },
      EntryPoint: { Path: entryPoint.path, ...(entryPoint.symbol === undefined ? {} : { Symbol: entryPoint.symbol }) },
      Toolchain: { Key: toolchain.key, Version: toolchain.version, RuntimeAbi: toolchain.runtimeAbi, ArtifactDigest: toolchain.artifactDigest },
    },
  };
}

function record(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function keys(value: Record<string, unknown>, allowed: readonly string[]): boolean {
  return Object.keys(value).every(key => allowed.includes(key));
}
