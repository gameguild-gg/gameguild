import { CODING_TOOLCHAIN } from '@game-guild/coding';
import { codingContractFixture } from '@game-guild/coding/testing';
import type { BrowserEmceptionAPI } from '@gameguild/emception-browser';
import type { ToolResult } from 'emception';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { runCodingPreview } from './preview-runner';
import type { CodingRuntimeSession, CodingRuntimeSessionFactory } from './runtime-types';

afterEach(() => vi.useRealTimers());

describe('bounded preview uses the public test engine', () => {
  it('passes/fails actual expectations and disposes its runtime', async () => {
    const fixture = setup();
    const result = await runCodingPreview(fixture.options);
    expect(result).toMatchObject({ academic: false, scope: 'public', report: { passed: 1, failed: 0 } });
    expect(fixture.api.runTests).not.toHaveBeenCalled();
    expect(fixture.disposed).toHaveBeenCalledTimes(1);
    fixture.api.compileAndRun.mockImplementationOnce(async (_source, options) => {
      options?.onPhase?.('run');
      options?.onProgramStart?.();
      return toolResult('wrong\n');
    });
    const wrong = await runCodingPreview(fixture.options);
    expect(wrong.report).toMatchObject({ passed: 0, failed: 1 });
  });

  it('uses the existing native function mapper rather than main-file verdicts', async () => {
    const fixture = setup();
    fixture.options.definition = {
      ...structuredClone(codingContractFixture.functionAuthoring),
      environment: { language: 'cpp', runtime: 'wasi' },
      toolchain: { ...CODING_TOOLCHAIN, key: 'cpp-emception' },
      limits: fixture.options.definition.limits,
    };
    fixture.api.compileAndRun.mockImplementation(async (_source, options) => {
      options?.onPhase?.('run');
      options?.onProgramStart?.();
      expect(options?.build).toHaveProperty('sources');
      expect(fixture.writes.some(([path]) => path.startsWith('/tmp/gameguild-function-'))).toBe(true);
      return toolResult(
        '[doctest] test cases: 1 | 1 passed | 0 failed | 0 skipped\n[doctest] assertions: 1 | 1 passed | 0 failed |\n[doctest] Status: SUCCESS!\n',
      );
    });
    const result = await runCodingPreview(fixture.options);
    expect(result.report.passed).toBe(1);
  });

  it('never turns an adapter exception into a numeric wrong-answer estimate', async () => {
    const fixture = setup();
    fixture.api.compileAndRun.mockRejectedValueOnce(new Error('transport unavailable'));
    await expect(runCodingPreview(fixture.options)).rejects.toMatchObject({ message: 'transport unavailable' });
    expect(fixture.disposed).toHaveBeenCalled();
  });

  it('rejects missing phase evidence rather than passing an expected empty answer', async () => {
    const fixture = setup();
    fixture.api.compileAndRun.mockResolvedValueOnce({ ...toolResult(), phase: undefined });
    await expect(runCodingPreview(fixture.options)).rejects.toMatchObject({ message: expect.stringContaining('phase') });
  });

  it('requires the program-start callback that establishes the student execution deadline', async () => {
    const fixture = setup();
    fixture.api.compileAndRun.mockResolvedValueOnce(toolResult('ready\n'));
    await expect(runCodingPreview(fixture.options)).rejects.toMatchObject({ message: expect.stringContaining('program-start') });
  });

  it('does not accept the early run stage as proof of actual program start', async () => {
    const fixture = setup();
    fixture.api.compileAndRun.mockImplementationOnce(async (_source, options) => {
      options?.onPhase?.('run');
      return toolResult('ready\n');
    });
    await expect(runCodingPreview(fixture.options)).rejects.toMatchObject({ message: expect.stringContaining('program-start') });
  });

  it('keeps WASM loading and compilation in preparation until actual program start', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    fixture.options.definition.limits.timeLimitMs = 10;
    const preparing = deferred<void>();
    const prepared = deferred<void>();
    fixture.api.compileAndRun.mockImplementationOnce(async (_source, options) => {
      options?.onPhase?.('run');
      preparing.resolve();
      await prepared.promise;
      options?.onProgramStart?.();
      return toolResult('ready\n');
    });
    const running = runCodingPreview(fixture.options).catch((cause: unknown) => cause);
    await preparing.promise;
    try {
      await vi.advanceTimersByTimeAsync(11);
      expect(fixture.disposed).not.toHaveBeenCalled();
    } finally {
      prepared.resolve();
    }
    expect(await running).toMatchObject({ academic: false, report: { passed: 1, failed: 0 } });
    expect(vi.getTimerCount()).toBe(0);
  });

  it.each(['compile', 'link'] as const)('does not certify a broken %s stage as a detected incorrect solution', async (phase) => {
    const fixture = setup();
    fixture.api.compileAndRun.mockResolvedValueOnce({ ...toolResult(), phase, exitCode: 1, stderr: 'invalid source' });
    await expect(runCodingPreview({ ...fixture.options, requireSuccessfulBuild: true })).rejects.toMatchObject({
      message: `Solution ${phase === 'compile' ? 'compilation' : 'linking'} failed (exit 1).\ninvalid source`,
    });
    expect(fixture.disposed).toHaveBeenCalled();
  });

  it('bounds streaming UTF-8 output and stops before returning a test verdict', async () => {
    const fixture = setup();
    fixture.options.definition.limits.outputLimitBytes = 3;
    fixture.api.compileAndRun.mockImplementation(async (_source, options) => {
      if (typeof options?.stdout === 'function') await options.stdout(new TextEncoder().encode('\u00e9\u00e9'));
      return toolResult();
    });
    await expect(runCodingPreview(fixture.options)).rejects.toMatchObject({ message: expect.stringContaining('output-limit') });
    expect(fixture.disposed).toHaveBeenCalled();
  });

  it('separates bounded preparation from the published program deadline', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    fixture.options.definition.limits.timeLimitMs = 10;
    const started = deferred<void>();
    let begin: (() => void) | undefined;
    fixture.api.compileAndRun.mockImplementation(async (_source, options) => {
      begin = () => {
        options?.onPhase?.('run');
        options?.onProgramStart?.();
        started.resolve();
      };
      await fixture.closed.promise;
      return toolResult();
    });
    const running = runCodingPreview(fixture.options).catch((error: unknown) => error);
    await drain();
    await vi.advanceTimersByTimeAsync(30_000);
    expect(fixture.disposed).not.toHaveBeenCalled();
    begin?.();
    await started.promise;
    await vi.advanceTimersByTimeAsync(11);
    expect(await running).toMatchObject({ message: expect.stringContaining('execution-timeout') });
    expect(vi.getTimerCount()).toBe(0);
  });

  it('does not charge post-execution overlay restoration to the finished student program', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    fixture.options.definition.limits.timeLimitMs = 10;
    fixture.options.definition.files.push({
      id: 'overlay-fixture',
      path: '/home/user/support.h',
      content: '// protected support',
      encoding: 'text',
      visibility: 'private',
      mutable: false,
    });
    const restoring = deferred<void>();
    const restored = deferred<void>();
    fixture.api.workspace.readFile.mockImplementation(async (path) => {
      for (const [written, content] of [...fixture.writes].reverse()) {
        if (written === path) return typeof content === 'string' ? new TextEncoder().encode(content) : content;
      }
      return null;
    });
    fixture.api.workspace.deleteFile.mockImplementation(async () => {
      restoring.resolve();
      await restored.promise;
    });
    const running = runCodingPreview({ ...fixture.options, mode: 'author-test' }).catch((cause: unknown) => cause);
    await Promise.race([
      restoring.promise,
      running.then((outcome) => {
        throw new Error('Execution ended before restoration.', { cause: outcome });
      }),
    ]);
    try {
      await vi.advanceTimersByTimeAsync(11);
      expect(fixture.disposed).not.toHaveBeenCalled();
    } finally {
      restored.resolve();
    }
    expect(await running).toMatchObject({ academic: false, report: { passed: 1, failed: 0 } });
    expect(fixture.disposed).toHaveBeenCalledTimes(1);
    expect(vi.getTimerCount()).toBe(0);
  });

  it('ends the student timer at public exit even while compileAndRun is restoring native scratch', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    fixture.options.definition.limits.timeLimitMs = 10;
    const exited = deferred<void>();
    const restored = deferred<void>();
    fixture.api.compileAndRun.mockImplementation(async (_source, options) => {
      options?.onPhase?.('run');
      options?.onProgramStart?.();
      const listener = fixture.api.on.mock.calls.find(([event]) => event === 'exit')?.[1];
      listener?.({ exitCode: 0, durationMs: 1 });
      exited.resolve();
      await restored.promise;
      return toolResult('ready\n');
    });
    const running = runCodingPreview(fixture.options).catch((cause: unknown) => cause);
    await exited.promise;
    try {
      await vi.advanceTimersByTimeAsync(11);
      expect(fixture.disposed).not.toHaveBeenCalled();
    } finally {
      restored.resolve();
    }
    expect(await running).toMatchObject({ academic: false, report: { passed: 1, failed: 0 } });
    expect(fixture.unsubscribe).toHaveBeenCalledTimes(1);
  });

  it('cancels and waits for actual pending acquisition ownership', async () => {
    const fixture = setup();
    const acquired = deferred<BrowserEmceptionAPI>();
    const cleanup = deferred<void>();
    fixture.acquire.mockReturnValueOnce(acquired.promise);
    fixture.whenDisposed.mockReturnValueOnce(cleanup.promise);
    const abort = new AbortController();
    const running = runCodingPreview({ ...fixture.options, signal: abort.signal }).catch((error: unknown) => error);
    abort.abort(new Error('cancelled by user'));
    let settled = false;
    void running.then(() => {
      settled = true;
    });
    await Promise.resolve();
    expect(settled).toBe(false);
    acquired.resolve(fixture.api);
    cleanup.resolve();
    expect(await running).toMatchObject({ message: 'cancelled by user' });
    expect(fixture.api.compileAndRun).not.toHaveBeenCalled();
  });

  it('does not conceal a failed runtime disposal', async () => {
    const fixture = setup();
    fixture.whenDisposed.mockRejectedValueOnce(new Error('worker still owned'));
    await expect(runCodingPreview(fixture.options)).rejects.toMatchObject({ message: expect.stringContaining('cleanup failed') });
  });

  it.each(['sdl3', 'raylib', 'allegro5'] as const)('rejects unsupported %s test execution before acquiring resources', async (runtime) => {
    const fixture = setup();
    fixture.options.definition.environment = { language: 'cpp', runtime };
    await expect(runCodingPreview(fixture.options)).rejects.toMatchObject({ message: expect.stringContaining('unavailable') });
    expect(fixture.createRuntime).not.toHaveBeenCalled();
  });
});

function setup() {
  const forbidden = vi.fn((): never => {
    throw new Error('Unexpected runtime path.');
  });
  const closed = deferred<void>();
  const unsubscribe = vi.fn();
  const writes: [string, Uint8Array | string][] = [];
  const api = {
    workspace: {
      list: forbidden,
      switch: forbidden,
      reset: forbidden,
      readFile: vi.fn<BrowserEmceptionAPI['workspace']['readFile']>().mockResolvedValue(null),
      writeFile: vi.fn<BrowserEmceptionAPI['workspace']['writeFile']>(async (path, content) => {
        writes.push([path, content]);
      }),
      deleteFile: vi.fn<BrowserEmceptionAPI['workspace']['deleteFile']>().mockResolvedValue(),
      listFiles: forbidden,
      setVisibility: forbidden,
      getBuild: forbidden,
      setBuild: vi.fn<BrowserEmceptionAPI['workspace']['setBuild']>().mockResolvedValue(),
      exportZip: forbidden,
      importZip: forbidden,
    },
    run: forbidden,
    compileAndRun: vi.fn<BrowserEmceptionAPI['compileAndRun']>(async (_source, options) => {
      options?.onPhase?.('run');
      options?.onProgramStart?.();
      return toolResult('ready\n');
    }),
    runTests: forbidden,
    on: vi.fn<BrowserEmceptionAPI['on']>().mockReturnValue(unsubscribe),
    dispose: forbidden,
    canvas: { build: forbidden, start: forbidden, buildAndStart: forbidden, stop: forbidden },
  } satisfies BrowserEmceptionAPI;
  const acquire = vi.fn<CodingRuntimeSession['acquire']>().mockResolvedValue(api);
  const disposed = vi.fn(() => {
    closed.resolve();
  });
  const whenDisposed = vi.fn<CodingRuntimeSession['whenDisposed']>().mockResolvedValue();
  const createRuntime = vi.fn<CodingRuntimeSessionFactory>(() => ({ acquire, getState: () => ({ status: 'idle' }), dispose: disposed, whenDisposed }));
  const definition = structuredClone(codingContractFixture.authoring);
  definition.toolchain = { ...CODING_TOOLCHAIN, key: 'cpp-emception' };
  definition.environment = { language: 'cpp', runtime: 'wasi' };
  definition.limits = { timeLimitMs: 30_000, outputLimitBytes: 2_000_000, maxFileCount: 100 };
  definition.tests = definition.tests.filter((test) => test.kind === 'stdio');
  const options = { definition, mode: 'learner' as const, manifestUrl: '/emception/manifest.json', signal: new AbortController().signal, createRuntime };
  return { api, closed, writes, acquire, disposed, whenDisposed, createRuntime, options, unsubscribe };
}

function toolResult(stdout = ''): ToolResult {
  return { stdout, stderr: '', exitCode: 0, timedOut: false, durationMs: 1, phase: 'run' };
}

function deferred<T>() {
  let resolve!: (value: T | PromiseLike<T>) => void;
  const promise = new Promise<T>((done) => {
    resolve = done;
  });
  return { promise, resolve };
}

async function drain() {
  for (let index = 0; index < 40; index++) await Promise.resolve();
}
