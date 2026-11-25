import { CODING_TOOLCHAIN, type CodingLearnerDelivery } from '@game-guild/coding';
import { codingContractFixture } from '@game-guild/coding/testing';
import type { BrowserEmceptionAPI } from '@gameguild/emception-browser';
import type { ToolResult } from 'emception';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { CodingPracticeRunOptions, CodingPracticeRunRequest } from './practice-run-types';
import { runCodingPractice } from './practice-runner';
import type { CodingRuntimeSession, CodingRuntimeSessionFactory } from './runtime-types';

const recordId = '00000000000040008000000000000001';
const prefix = `\u001eCGP:${recordId}:`;
beforeEach(() => vi.spyOn(globalThis.crypto, 'randomUUID').mockReturnValue('00000000-0000-4000-8000-000000000001'));
afterEach(() => {
  vi.useRealTimers();
  vi.restoreAllMocks();
});

describe('personal/public execution through one bounded public runtime', () => {
  it('observes I/O with no oracle and does not turn exit zero into a pass', async () => {
    const f = setup();
    const result = await runCodingPractice(f.options);
    expect(result).toMatchObject({
      contextKey: 'actor.revision.attempt',
      executionId: 7,
      academic: false,
      comparison: { status: 'observed' },
      observation: { stdout: 'hello\n', stderr: '', exitCode: 0, returnValue: null },
    });
    expect(result.comparison).not.toHaveProperty('checks');
    expect(result).not.toHaveProperty('score');
    expect(f.api.compileAndRun).toHaveBeenCalledTimes(1);
    expect(f.api.runTests).not.toHaveBeenCalled();
    expect(f.dispose).toHaveBeenCalledTimes(1);
    expect(f.whenDisposed).toHaveBeenCalledTimes(1);
  });

  it('retains exact stdin/current file bytes without introducing teacher tests', async () => {
    const f = setup();
    f.options.delivery.files[0]!.content = 'CURRENT CODE';
    await runCodingPractice(f.options);
    expect(f.files.get(f.options.delivery.files[0]!.path)).toEqual(new TextEncoder().encode('CURRENT CODE'));
    expect(f.api.compileAndRun).toHaveBeenCalledWith(undefined, expect.objectContaining({ stdin: 'Ada\n' }));
    expect(JSON.stringify([...f.files.values()])).not.toContain('private');
  });

  it('compares an explicitly empty stdout, independently from absent stderr/exit expectations', async () => {
    const f = setup();
    f.options.request = stdio({ stdout: '', stderr: null, exitCode: null });
    const result = await runCodingPractice(f.options);
    expect(result.comparison).toEqual({ status: 'failed', checks: [{ field: 'stdout', expected: '', actual: 'hello\n', matches: false }] });
    f.api.compileAndRun.mockImplementationOnce(async (_source, options) => acknowledge(options, { stdout: '' }));
    expect((await runCodingPractice(f.options)).comparison).toEqual({
      status: 'passed',
      checks: [{ field: 'stdout', expected: '', actual: '', matches: true }],
    });
  });

  it('compares only explicit stderr and exit code without silently adding stdout', async () => {
    const f = setup();
    f.options.request = stdio({ stdout: null, stderr: '', exitCode: '0' });
    expect((await runCodingPractice(f.options)).comparison).toEqual({
      status: 'passed',
      checks: [
        { field: 'stderr', expected: '', actual: '', matches: true },
        { field: 'exitCode', expected: '0', actual: '0', matches: true },
      ],
    });
  });

  it('resolves public oracles from the immutable delivery identity', async () => {
    const f = setup();
    const test = f.options.delivery.publicTests[0]!;
    if (test.kind !== 'stdio') throw new Error('Expected stdio fixture.');
    f.options.request = { contextKey: f.options.contextKey, source: 'public', id: `public:${test.id}` };
    f.api.compileAndRun.mockImplementationOnce(async (_source, options) => acknowledge(options, { stdout: test.expected.stdout }));
    const result = await runCodingPractice(f.options);
    expect(result.selection).toEqual({ source: 'public', id: `public:${test.id}` });
    expect(result.comparison.status).toBe('passed');
  });

  it.each(['context', 'unknown public', 'private payload', 'invalid exit'] as const)('rejects %s before runtime acquisition', async (defect) => {
    const f = setup();
    if (defect === 'context') f.options.request = { ...f.options.request, contextKey: 'other-actor' };
    if (defect === 'unknown public') f.options.request = { contextKey: f.options.contextKey, source: 'public', id: 'private:case' };
    if (defect === 'private payload') Object.assign(f.options.delivery, { validationSolutions: [] });
    if (defect === 'invalid exit') f.options.request = stdio({ stdout: null, stderr: null, exitCode: '-' });
    await expect(runCodingPractice(f.options)).rejects.toBeInstanceOf(Error);
    expect(f.createRuntime).not.toHaveBeenCalled();
  });

  it.each(['c', 'cpp'] as const)('observes a typed %s function return without any fabricated expected value', async (language) => {
    const f = setup('function', language);
    const result = await runCodingPractice(f.options);
    expect(result.comparison).toEqual({ status: 'observed' });
    expect(result.observation.returnValue).toEqual({ type: 'integer', value: 5 });
    const compile = f.api.compileAndRun.mock.calls[0]?.[1];
    const sources = compile?.build && 'sources' in compile.build ? compile.build.sources : undefined;
    expect(sources).toContain(`/tmp/gameguild-practice-call.${language === 'c' ? 'c' : 'cpp'}`);
    expect(f.source()).not.toMatch(/doctest|CHECK|expected|passed/);
    expect(f.api.workspace.readFile.mock.calls.some(([path]) => path === '/tmp/gameguild-practice-return')).toBe(false);
    expect(result.observation.stderr).toBe('');
    expect(f.files.has(`/tmp/gameguild-practice-call.${language === 'c' ? 'c' : 'cpp'}`)).toBe(false);
  });

  it('compares the actual typed return only when the learner supplies an expectation', async () => {
    const f = setup('function');
    f.options.request = functionInput('2', '3', { type: 'integer', text: '4' });
    expect((await runCodingPractice(f.options)).comparison).toEqual({
      status: 'failed',
      checks: [{ field: 'returnValue', expected: '4', actual: '5', matches: false }],
    });
  });

  it.each([
    ['boolean', '0', { type: 'boolean', value: false }],
    ['boolean', '1', { type: 'boolean', value: true }],
    ['float', '1.25', { type: 'float', value: '1.25' }],
    ['string', '', { type: 'string', value: '' }],
    ['string', 'hello\n"%s', { type: 'string', value: 'hello\n"%s' }],
  ] as const)('observes the native %s representation %j without coercing an input buffer', async (type, bytes, expected) => {
    const f = setup('function');
    const publicTest = f.options.delivery.publicTests[0];
    if (publicTest?.kind !== 'function') throw new Error('Function sample required.');
    publicTest.function.returnType = type;
    for (const value of publicTest.cases)
      value.expected = type === 'boolean' ? { type, value: false } : type === 'string' ? { type, value: '' } : { type, value: '0.0' };
    f.returnBytes = type === 'string' ? Array.from(new TextEncoder().encode(bytes), (byte) => byte.toString(16).padStart(2, '0')).join('') : bytes;
    expect((await runCodingPractice(f.options)).observation.returnValue).toEqual(expected);
  });

  it.each(['-', '3e', '2147483648', '1; injected()'] as const)('retains but refuses non-executable integer input %s', async (text) => {
    const f = setup('function');
    f.options.request = functionInput(text, '3', null);
    const before = structuredClone(f.options.request);
    await expect(runCodingPractice(f.options)).rejects.toBeInstanceOf(Error);
    expect(f.options.request).toEqual(before);
    expect(f.createRuntime).not.toHaveBeenCalled();
  });

  it('rejects an incomplete expected value before acquiring a runtime', async () => {
    const f = setup('function');
    f.options.request = functionInput('2', '3', { type: 'integer', text: '-' });
    await expect(runCodingPractice(f.options)).rejects.toBeInstanceOf(Error);
    expect(f.createRuntime).not.toHaveBeenCalled();
  });

  it.each(['compile', 'link', 'missing phase', 'missing start', 'timeout', 'missing return'] as const)(
    'reports %s as an error, never a comparison verdict',
    async (failure) => {
      const f = setup(failure === 'missing return' ? 'function' : 'standard-io');
      f.api.compileAndRun.mockImplementationOnce(async (_source, options) => {
        if (failure !== 'missing start') {
          options?.onPhase?.('run');
          options?.onProgramStart?.();
        }
        return {
          ...toolResult(),
          ...(failure === 'compile' || failure === 'link' ? { phase: failure, exitCode: 1, stderr: 'broken build' } : {}),
          ...(failure === 'missing phase' ? { phase: undefined } : {}),
          ...(failure === 'timeout' ? { timedOut: true } : {}),
        };
      });
      await expect(runCodingPractice(f.options)).rejects.toBeInstanceOf(Error);
      expect(f.dispose).toHaveBeenCalled();
      expect(f.whenDisposed).toHaveBeenCalled();
    },
  );

  it('rejects over-budget return bytes instead of truncating a string into an answer', async () => {
    const f = setup('function');
    f.options.delivery.limits.outputLimitBytes = 100;
    f.returnBytes = '1'.repeat(101);
    await expect(runCodingPractice(f.options)).rejects.toMatchObject({ message: expect.stringContaining('output') });
    expect(f.dispose).toHaveBeenCalled();
  });

  it('cancels/drains pending acquisition before returning to the owner', async () => {
    const f = setup();
    const acquired = deferred<BrowserEmceptionAPI>();
    const cleaned = deferred<void>();
    f.acquire.mockReturnValueOnce(acquired.promise);
    f.whenDisposed.mockReturnValueOnce(cleaned.promise);
    const abort = new AbortController();
    const running = runCodingPractice({ ...f.options, signal: abort.signal }).catch((cause: unknown) => cause);
    await vi.waitFor(() => expect(f.acquire).toHaveBeenCalled());
    abort.abort(new Error('cancelled by edit'));
    acquired.resolve(f.api);
    await vi.waitFor(() => expect(f.dispose).toHaveBeenCalled());
    expect(f.api.compileAndRun).not.toHaveBeenCalled();
    cleaned.resolve();
    expect(await running).toMatchObject({ message: 'cancelled by edit' });
  });

  it('preserves cleanup failures as errors even after a successful observation', async () => {
    const f = setup();
    f.whenDisposed.mockRejectedValueOnce(new Error('cleanup failed'));
    await expect(runCodingPractice(f.options)).rejects.toBeInstanceOf(AggregateError);
  });

  it('removes only the framed return and preserves unrelated stdout/stderr exactly', async () => {
    const f = setup('function');
    f.api.compileAndRun.mockImplementationOnce(async (_source, options) =>
      acknowledge(options, { stdout: 'student log\n', stderr: `before${prefix}5\u001fafter\n` }),
    );
    const result = await runCodingPractice(f.options);
    expect(result.observation).toMatchObject({ stdout: 'student log\n', stderr: 'beforeafter\n', returnValue: { type: 'integer', value: 5 } });
    expect(JSON.stringify(result)).not.toContain(recordId);
  });

  it.each(['missing', 'duplicate', 'unterminated', 'invalid numeric'] as const)('refuses a %s return frame without a verdict', async (defect) => {
    const f = setup('function');
    const frame = `${prefix}5\u001f`;
    const stderr =
      defect === 'missing' ? 'no record' : defect === 'duplicate' ? frame + frame : defect === 'unterminated' ? `${prefix}5` : `${prefix}not-a-number\u001f`;
    f.api.compileAndRun.mockImplementationOnce(async (_source, options) => acknowledge(options, { stderr }));
    await expect(runCodingPractice(f.options)).rejects.toBeInstanceOf(Error);
    expect(f.dispose).toHaveBeenCalled();
  });
});

function stdio(expected: { stdout: string | null; stderr: string | null; exitCode: string | null }): CodingPracticeRunRequest {
  return {
    contextKey: 'actor.revision.attempt',
    source: 'personal',
    value: { id: 'mine', source: 'personal', name: 'My case', kind: 'stdio', stdin: 'Ada\n', expected },
  };
}

function functionInput(left: string, right: string, expected: { type: 'integer'; text: string } | null): CodingPracticeRunRequest {
  return {
    contextKey: 'actor.revision.attempt',
    source: 'personal',
    value: {
      id: 'mine',
      source: 'personal',
      name: 'My function',
      kind: 'function',
      inputs: [
        { type: 'integer', text: left },
        { type: 'integer', text: right },
      ],
      expected,
    },
  };
}

function setup(family: 'standard-io' | 'function' = 'standard-io', language: 'c' | 'cpp' = 'cpp') {
  const files = new Map<string, Uint8Array>();
  const forbidden = vi.fn((): never => {
    throw new Error('Unexpected runtime path.');
  });
  const dispose = vi.fn();
  const whenDisposed = vi.fn<CodingRuntimeSession['whenDisposed']>().mockResolvedValue();
  const delivery: CodingLearnerDelivery = structuredClone(
    family === 'function' ? codingContractFixture.functionLearnerDelivery : codingContractFixture.learnerDelivery,
  );
  delivery.environment = { language, runtime: 'wasi' };
  delivery.toolchain = { ...CODING_TOOLCHAIN, key: `${language}-emception` };
  delivery.limits = { timeLimitMs: 5000, outputLimitBytes: 4096, maxFileCount: 20 };
  const api = {
    workspace: {
      list: forbidden,
      switch: forbidden,
      reset: forbidden,
      listFiles: forbidden,
      setVisibility: forbidden,
      getBuild: forbidden,
      exportZip: forbidden,
      importZip: forbidden,
      readFile: vi.fn<BrowserEmceptionAPI['workspace']['readFile']>(async (path) => files.get(path) ?? null),
      writeFile: vi.fn<BrowserEmceptionAPI['workspace']['writeFile']>(async (path, content) => {
        files.set(path, typeof content === 'string' ? new TextEncoder().encode(content) : content);
      }),
      deleteFile: vi.fn<BrowserEmceptionAPI['workspace']['deleteFile']>(async (path) => {
        files.delete(path);
      }),
      setBuild: vi.fn<BrowserEmceptionAPI['workspace']['setBuild']>().mockResolvedValue(),
    },
    compileAndRun: vi.fn<BrowserEmceptionAPI['compileAndRun']>(async (_source, options): Promise<ToolResult> => {
      const result = acknowledge(options, { stdout: family === 'function' ? '' : 'hello\n' });
      return family === 'function' ? { ...result, stderr: `${prefix}${fixture.returnBytes}\u001f` } : result;
    }),
    run: forbidden,
    runTests: forbidden,
    on: vi.fn<BrowserEmceptionAPI['on']>().mockReturnValue(vi.fn()),
    dispose: forbidden,
    canvas: { build: forbidden, start: forbidden, buildAndStart: forbidden, stop: forbidden },
  } satisfies BrowserEmceptionAPI;
  const acquire = vi.fn<CodingRuntimeSession['acquire']>().mockResolvedValue(api);
  const createRuntime = vi.fn<CodingRuntimeSessionFactory>(() => ({ acquire, dispose, whenDisposed, getState: () => ({ status: 'idle' }) }));
  const options: { -readonly [K in keyof CodingPracticeRunOptions]: CodingPracticeRunOptions[K] } = {
    contextKey: 'actor.revision.attempt',
    executionId: 7,
    delivery,
    request: family === 'function' ? functionInput('2', '3', null) : stdio({ stdout: null, stderr: null, exitCode: null }),
    manifestUrl: '/emception/manifest.json',
    signal: new AbortController().signal,
    createRuntime,
  };
  let observedSource = '';
  api.workspace.writeFile.mockImplementation(async (path, content) => {
    if (path.startsWith('/tmp/gameguild-practice-call.')) observedSource = typeof content === 'string' ? content : new TextDecoder().decode(content);
    files.set(path, typeof content === 'string' ? new TextEncoder().encode(content) : content);
  });
  const fixture = { api, options, createRuntime, acquire, dispose, whenDisposed, files, returnBytes: '5', source: () => observedSource };
  return fixture;
}

function acknowledge(options: Parameters<BrowserEmceptionAPI['compileAndRun']>[1], result: Partial<ToolResult> = {}): ToolResult {
  options?.onPhase?.('run');
  options?.onProgramStart?.();
  return { ...toolResult(), ...result };
}

function toolResult(): ToolResult {
  return { stdout: '', stderr: '', exitCode: 0, timedOut: false, durationMs: 1, phase: 'run' };
}

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  const promise = new Promise<T>((done) => {
    resolve = done;
  });
  return { promise, resolve };
}
