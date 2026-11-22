import { describe, expect, it } from 'vitest';
import { CODING_EXECUTION_CAPABILITIES, CODING_TOOLCHAIN, parseCodingExecutionConfiguration } from './index';

const configuration = () => ({
  environment: { language: 'cpp', runtime: 'wasi' },
  toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
  entryPoint: { path: 'solution.cpp', symbol: 'sum' },
  limits: { timeLimitMs: 4321, outputLimitBytes: 12345, maxFileCount: 7 },
});

describe('WB-B execution configuration contract', () => {
  it('preserves exact limits and identity, normalizing only the workspace path', () => {
    const source = configuration();
    expect(parseCodingExecutionConfiguration(source)).toEqual({ ...source, entryPoint: { path: '/home/user/solution.cpp', symbol: 'sum' } });
    expect(source.entryPoint.path).toBe('solution.cpp');
  });

  it('declares RAM unavailable and rejects configured RAM instead of dropping it', () => {
    expect(CODING_EXECUTION_CAPABILITIES.memoryLimit.available).toBe(false);
    const source = configuration();
    expect(() => parseCodingExecutionConfiguration({ ...source, limits: { ...source.limits, memoryLimitBytes: 134217728 } })).toThrow(/RAM/);
  });

  it.each([
    { timeLimitMs: 0 }, { timeLimitMs: 30001 }, { timeLimitMs: 1.5 },
    { outputLimitBytes: 0 }, { outputLimitBytes: 2000001 }, { maxFileCount: 101 },
  ])('rejects unsupported limits %j', limits => {
    const source = configuration();
    expect(() => parseCodingExecutionConfiguration({ ...source, limits: { ...source.limits, ...limits } })).toThrow();
  });

  it.each([
    { key: 'other-emception' }, { version: 'future' }, { runtimeAbi: 'other' }, { artifactDigest: `sha256:${'f'.repeat(64)}` },
  ])('rejects unknown toolchain identity %j', identity => {
    const source = configuration();
    expect(() => parseCodingExecutionConfiguration({ ...source, toolchain: { ...source.toolchain, ...identity } })).toThrow(/toolchain/);
  });

  it('rejects unsupported runtime combinations and non-source entry points', () => {
    expect(() => parseCodingExecutionConfiguration({ ...configuration(), environment: { language: 'c', runtime: 'sdl3' } })).toThrow();
    expect(() => parseCodingExecutionConfiguration({ ...configuration(), entryPoint: { path: '../secret.cpp' } })).toThrow();
    expect(() => parseCodingExecutionConfiguration({ ...configuration(), entryPoint: { path: 'image.png' } })).toThrow();
  });
});
