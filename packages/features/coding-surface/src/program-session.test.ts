import { CODING_TOOLCHAIN, parseCodingLearnerDelivery, type CodingLearnerDelivery } from '@game-guild/coding';
import type { BrowserEmceptionAPI, BrowserRunOptions } from '@gameguild/emception-browser';
import type { ToolResult } from 'emception';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createCodingProgramSession } from './program-session';
import type { CodingProgramSessionOptions } from './program-session-types';
import type { CodingRuntimeSession, CodingRuntimeSessionFactory } from './runtime-types';

afterEach(() => vi.useRealTimers());

describe('public text execution and input', () => {
  it('constructs and inspects the candidate without booting a runtime', async () => {
    const fixture = setup();
    const session = createCodingProgramSession(fixture.options);
    expect(session.getState()).toEqual({ status: 'idle', candidateKey: 'candidate-a', waitingForInput: false, output: { stdout: '', stderr: '', truncated: false } });
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    await session.dispose();
    expect(session.getState().status).toBe('disposed');
    expect(fixture.createRuntime).not.toHaveBeenCalled();
  });

  it.each(['', 'two lines\nnext', 'caf\u00e9\n'])('sends exact finite batch bytes and EOF: %j', async text => {
    const fixture = setup();
    fixture.execute.mockImplementation(async options => {
      const bytes = await readAll(options);
      expect(new TextDecoder().decode(Uint8Array.from(bytes))).toBe(text);
      emit(options, 'stdout', text);
      return result();
    });
    const session = createCodingProgramSession(fixture.options);
    await session.run({ kind: 'batch', text });
    expect(session.getState()).toMatchObject({ status: 'completed', exitCode: 0, output: { stdout: text, stderr: '' }, waitingForInput: false });
    expect(fixture.api.run.mock.calls.map(call => call[0])).toEqual(['clang', 'wasm-ld', 'wasi-run']);
    expect(fixture.disposeRuntime).toHaveBeenCalledTimes(1);
    await session.dispose();
  });

  it('accepts input only after the real program requests it, then delivers more input and EOF', async () => {
    const fixture = setup();
    fixture.execute.mockImplementation(async options => {
      emit(options, 'stdout', 'name? ');
      const bytes = await readAll(options);
      emit(options, 'stdout', new TextDecoder().decode(Uint8Array.from(bytes)));
      return result();
    });
    const session = createCodingProgramSession(fixture.options);
    expect(() => session.writeInput('too early')).toThrow();
    const running = session.run({ kind: 'interactive' });
    await vi.waitFor(() => expect(session.getState()).toMatchObject({ status: 'running', waitingForInput: true, output: { stdout: 'name? ' } }));
    session.writeInput('Ada');
    await vi.waitFor(() => expect(session.getState().waitingForInput).toBe(true));
    session.writeInput('\n');
    session.endInput();
    await running;
    expect(session.getState()).toMatchObject({ status: 'completed', output: { stdout: 'name? Ada\n' }, waitingForInput: false });
    expect(() => session.writeInput('after EOF')).toThrow();
    await session.dispose();
  });

  it('freezes source bytes and identity while keeping stdout/stderr separate', async () => {
    const fixture = setup();
    const session = createCodingProgramSession(fixture.options);
    fixture.options.delivery.files[0].content = 'changed after capture';
    fixture.execute.mockImplementation(async options => { emit(options, 'stdout', 'out'); emit(options, 'stderr', 'err'); return result(7); });
    await session.run({ kind: 'batch', text: '' });
    expect(fixture.api.workspace.writeFile).toHaveBeenCalledWith('/home/user/main.cpp', 'int main(){return 0;}');
    expect(session.getState()).toMatchObject({ status: 'completed', exitCode: 7, output: { stdout: 'out', stderr: 'err' } });
    const snapshot = session.getState();
    Reflect.set(snapshot.output, 'stdout', 'mutated');
    expect(session.getState().output.stdout).toBe('out');
    expect(session.getState()).not.toHaveProperty('score');
    expect(session.getState()).not.toHaveProperty('passed');
    await session.dispose();
  });

  it('writes public headers and binary assets without compiling them as translation units', async () => {
    const fixture = setup();
    fixture.options.delivery.files.push({ id: 'header', path: '/home/user/helpers.h', content: '// helper', mutable: false, encoding: 'text' });
    fixture.options.delivery.files.push({ id: 'asset', path: '/home/user/data.bin', content: 'AQID', mutable: false, encoding: 'base64' });
    const session = createCodingProgramSession(fixture.options);
    await session.run({ kind: 'batch', text: '' });
    expect(fixture.api.workspace.writeFile).toHaveBeenCalledWith('/home/user/data.bin', new Uint8Array([1, 2, 3]));
    expect(fixture.api.run).toHaveBeenCalledTimes(3);
    await session.dispose();
  });

  it.each(['compile', 'link'] as const)('keeps %s errors distinct from program exit or wrong answer', async phase => {
    const fixture = setup();
    fixture.api.run.mockImplementation(async cmd => cmd === (phase === 'compile' ? 'clang' : 'wasm-ld') ? result(2) : result());
    const session = createCodingProgramSession(fixture.options);
    await expect(session.run({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringContaining(`${phase}-failed`) });
    expect(session.getState()).toMatchObject({ status: 'failed', reason: `${phase}-failed` });
    expect(fixture.execute).not.toHaveBeenCalled();
    expect(fixture.disposeRuntime).toHaveBeenCalledTimes(1);
    await session.dispose();
  });

  it.each(['sdl3', 'raylib', 'allegro5'] as const)('rejects graphics in the text executor before boot: %s', async runtime => {
    const fixture = setup();
    fixture.options.delivery.environment = { language: 'cpp', runtime };
    const session = createCodingProgramSession(fixture.options);
    await expect(session.run({ kind: 'interactive' })).rejects.toMatchObject({ message: expect.stringContaining('unsupported-context') });
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('never silently drops a second native source', async () => {
    const fixture = setup();
    fixture.options.delivery.files.push({ ...fixture.options.delivery.files[0], id: 'second', path: '/home/user/extra.cpp' });
    const session = createCodingProgramSession(fixture.options);
    await expect(session.run({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringContaining('unsupported-context') });
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('rejects private authoring payloads before putting anything in the VFS', () => {
    const fixture = setup();
    const delivery = { ...fixture.options.delivery, validationSolutions: [{ secret: 'PRIVATE_CANARY' }] };
    expect(() => createCodingProgramSession({ ...fixture.options, delivery })).toThrow();
    expect(fixture.createRuntime).not.toHaveBeenCalled();
  });

  it.each(['', 'a'.repeat(257), 'line\nbreak'])('requires a bounded explicit candidate key', candidateKey => {
    const fixture = setup();
    expect(() => createCodingProgramSession({ ...fixture.options, candidateKey })).toThrow();
    expect(fixture.createRuntime).not.toHaveBeenCalled();
  });

  it('cancels pending input, ignores old output and acknowledges actual ownership before restart', async () => {
    const fixture = setup();
    const drained = deferred<void>();
    fixture.whenDisposed.mockReturnValueOnce(drained.promise);
    let staleOptions: BrowserRunOptions | undefined;
    fixture.execute.mockImplementation(async options => { staleOptions = options; await readAll(options); return result(); });
    const session = createCodingProgramSession(fixture.options);
    const running = session.run({ kind: 'interactive' }).catch((error: unknown) => error);
    await vi.waitFor(() => expect(session.getState().waitingForInput).toBe(true));
    const stopping = session.stop();
    emit(staleOptions, 'stdout', 'stale output');
    await expect(session.run({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringContaining('already active') });
    expect(session.getState().output.stdout).toBe('');
    drained.resolve();
    await stopping;
    expect(await running).toBeInstanceOf(Error);
    expect(session.getState().status).toBe('stopped');
    fixture.execute.mockResolvedValue(result());
    await session.run({ kind: 'batch', text: '' });
    expect(session.getState().status).toBe('completed');
    await session.dispose();
  });

  it('waits for an in-flight acquisition to drain and does not execute a late runtime', async () => {
    const fixture = setup();
    const acquiring = deferred<BrowserEmceptionAPI>();
    fixture.acquire.mockReturnValueOnce(acquiring.promise);
    const session = createCodingProgramSession(fixture.options);
    const running = session.run({ kind: 'batch', text: '' }).catch((error: unknown) => error);
    const stopped = session.stop();
    acquiring.resolve(fixture.api);
    await stopped;
    expect(await running).toBeInstanceOf(Error);
    expect(fixture.api.run).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('bounds interactive input and cancels instead of retaining an unbounded input queue', async () => {
    const fixture = setup();
    fixture.execute.mockImplementation(async options => { await readAll(options); return result(); });
    const session = createCodingProgramSession(fixture.options);
    const running = session.run({ kind: 'interactive' }).catch((error: unknown) => error);
    await vi.waitFor(() => expect(session.getState().waitingForInput).toBe(true));
    expect(() => session.writeInput('a'.repeat(65_537))).toThrow();
    expect(await running).toMatchObject({ message: expect.stringContaining('input-limit') });
    expect(session.getState()).toMatchObject({ status: 'failed', reason: 'input-limit' });
    await session.dispose();
  });

  it('rejects an oversized batch before boot', async () => {
    const fixture = setup();
    const session = createCodingProgramSession(fixture.options);
    await expect(session.run({ kind: 'batch', text: 'a'.repeat(65_537) })).rejects.toMatchObject({ message: expect.stringContaining('input-limit') });
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('enforces UTF-8 output bytes across both channels', async () => {
    const fixture = setup();
    fixture.options.delivery.limits.outputLimitBytes = 3;
    fixture.execute.mockImplementation(async options => { emit(options, 'stdout', '\u00e9'); emit(options, 'stderr', '\u00e9'); return result(); });
    const session = createCodingProgramSession(fixture.options);
    await expect(session.run({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringContaining('output-limit') });
    expect(session.getState()).toMatchObject({ status: 'failed', reason: 'output-limit' });
    expect(fixture.disposeRuntime).toHaveBeenCalledTimes(1);
    await session.dispose();
  });

  it('bounds visible output independently of the larger actual program output budget', async () => {
    const fixture = setup();
    fixture.execute.mockImplementation(async options => { emit(options, 'stdout', 'a'.repeat(60_000)); emit(options, 'stderr', 'b'.repeat(60_000)); return result(); });
    const session = createCodingProgramSession(fixture.options);
    await session.run({ kind: 'batch', text: '' });
    const { output } = session.getState();
    expect(output.stdout.length + output.stderr.length).toBeLessThanOrEqual(100_000);
    expect(output.truncated).toBe(true);
    expect(output.stderr).toBe('b'.repeat(60_000));
    await session.dispose();
  });

  it('does not accept interactive writes during a batch program', async () => {
    const fixture = setup();
    const completion = deferred<ToolResult>();
    fixture.execute.mockReturnValue(completion.promise);
    const session = createCodingProgramSession(fixture.options);
    const running = session.run({ kind: 'batch', text: '' });
    await vi.waitFor(() => expect(session.getState().status).toBe('running'));
    expect(() => session.writeInput('unexpected')).toThrow();
    expect(() => session.endInput()).toThrow();
    completion.resolve(result());
    await running;
    await session.dispose();
  });

  it('gives compilation a separate bounded budget and starts the student timer at execution', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    const compile = deferred<ToolResult>();
    fixture.api.run.mockReturnValueOnce(compile.promise);
    fixture.execute.mockImplementation(async options => { await readAll(options); return result(); });
    const session = createCodingProgramSession(fixture.options);
    const running = session.run({ kind: 'interactive' }).catch((error: unknown) => error);
    await drain();
    await vi.advanceTimersByTimeAsync(31_000);
    expect(session.getState().status).toBe('building');
    compile.resolve(result());
    await drain();
    expect(session.getState().status).toBe('running');
    await vi.advanceTimersByTimeAsync(30_001);
    expect(await running).toMatchObject({ message: expect.stringContaining('execution-timeout') });
    await session.dispose();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('times out a stalled preparation and waits for late ownership cleanup', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    const acquiring = deferred<BrowserEmceptionAPI>();
    fixture.acquire.mockReturnValueOnce(acquiring.promise);
    const session = createCodingProgramSession(fixture.options);
    const running = session.run({ kind: 'batch', text: '' }).catch((error: unknown) => error);
    await vi.advanceTimersByTimeAsync(120_001);
    expect(await running).toMatchObject({ message: expect.stringContaining('preparation-timeout') });
    acquiring.resolve(fixture.api);
    await session.dispose();
    expect(fixture.api.run).not.toHaveBeenCalled();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('rejects factory failure and surfaces a cleanup failure without allowing restart', async () => {
    const fixture = setup();
    fixture.createRuntime.mockImplementationOnce(() => { throw new Error('factory failed'); });
    const session = createCodingProgramSession(fixture.options);
    await expect(session.run({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringContaining('runtime-unavailable') });
    fixture.whenDisposed.mockRejectedValueOnce(new Error('cleanup failed'));
    await expect(session.run({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringContaining('cleanup-failed') });
    await expect(session.run({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringContaining('cleanup-failed') });
    await expect(session.dispose()).rejects.toMatchObject({ message: expect.stringContaining('cleanup-failed') });
  });

  it('decodes split UTF-8 chunks without mixing the channel decoders', async () => {
    const fixture = setup();
    fixture.execute.mockImplementation(async options => {
      if (typeof options?.stdout !== 'function' || typeof options.stderr !== 'function') throw new Error('Missing output callbacks.');
      await options.stdout(new Uint8Array([0xc3]));
      await options.stderr(new Uint8Array([0xc2, 0xa3]));
      await options.stdout(new Uint8Array([0xa9]));
      return result();
    });
    const session = createCodingProgramSession(fixture.options);
    await session.run({ kind: 'batch', text: '' });
    expect(session.getState().output).toEqual({ stdout: '\u00e9', stderr: '\u00a3', truncated: false });
    await session.dispose();
  });

  it('checks the elapsed preparation deadline even when the browser delays timer dispatch', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    const compile = deferred<ToolResult>();
    fixture.api.run.mockReturnValueOnce(compile.promise);
    const session = createCodingProgramSession(fixture.options);
    const running = session.run({ kind: 'batch', text: '' }).catch((error: unknown) => error);
    await drain();
    vi.setSystemTime(Date.now() + 120_001);
    compile.resolve(result());
    expect(await running).toMatchObject({ message: expect.stringContaining('preparation-timeout') });
    expect(fixture.execute).not.toHaveBeenCalled();
    await session.dispose();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('waits for a cancelled run to settle, not just the runtime disposal method', async () => {
    const fixture = setup();
    const finished = deferred<ToolResult>();
    fixture.execute.mockReturnValueOnce(finished.promise);
    const session = createCodingProgramSession(fixture.options);
    const running = session.run({ kind: 'batch', text: '' }).catch((error: unknown) => error);
    await vi.waitFor(() => expect(session.getState().status).toBe('running'));
    let settled = false;
    const stopping = session.stop().then(() => { settled = true; });
    await drain();
    expect(settled).toBe(false);
    await expect(session.run({ kind: 'batch', text: '' })).rejects.toMatchObject({ message: expect.stringContaining('already active') });
    finished.resolve(result());
    await stopping;
    expect(await running).toBeInstanceOf(Error);
    await session.dispose();
  });
});

function result(exitCode = 0): ToolResult { return { exitCode, stdout: '', stderr: '', durationMs: 1, timedOut: false }; }

function emit(options: BrowserRunOptions | undefined, channel: 'stdout' | 'stderr', text: string) {
  const sink = options?.[channel];
  if (typeof sink !== 'function') throw new Error('Missing public output sink.');
  void sink(new TextEncoder().encode(text));
}

async function readAll(options: BrowserRunOptions | undefined) {
  const stdin = options?.stdin;
  if (typeof stdin !== 'function') throw new Error('Missing public interactive input callback.');
  const bytes: number[] = [];
  while (true) {
    const value = await stdin();
    if (value === null || value === -1) return bytes;
    if (typeof value !== 'number') throw new Error('The browser input callback must return individual bytes.');
    bytes.push(value);
  }
}

function setup() {
  const forbidden = () => { throw new Error('Unexpected test, graphics or academic execution.'); };
  const execute = vi.fn<(options?: BrowserRunOptions) => Promise<ToolResult>>().mockResolvedValue(result());
  const api = {
    workspace: {
      list: forbidden, switch: forbidden, reset: forbidden, readFile: forbidden,
      writeFile: vi.fn<BrowserEmceptionAPI['workspace']['writeFile']>().mockResolvedValue(undefined), deleteFile: forbidden,
      listFiles: forbidden, setVisibility: forbidden, getBuild: forbidden, setBuild: forbidden, exportZip: forbidden, importZip: forbidden,
    },
    run: vi.fn<BrowserEmceptionAPI['run']>().mockImplementation(async (cmd, _argv, options) => cmd === 'wasi-run' ? execute(options) : result()),
    compileAndRun: forbidden, runTests: forbidden, on: forbidden, dispose: forbidden,
    canvas: { build: forbidden, start: forbidden, buildAndStart: forbidden, stop: forbidden },
  } satisfies BrowserEmceptionAPI;
  const acquire = vi.fn<CodingRuntimeSession['acquire']>().mockResolvedValue(api);
  const disposeRuntime = vi.fn();
  const whenDisposed = vi.fn<CodingRuntimeSession['whenDisposed']>().mockResolvedValue(undefined);
  const createRuntime = vi.fn<CodingRuntimeSessionFactory>().mockImplementation(() => ({
    getState: () => ({ status: 'idle' }), acquire, dispose: disposeRuntime, whenDisposed,
  }));
  const delivery: CodingLearnerDelivery = parseCodingLearnerDelivery({
    schemaVersion: 1, type: 'coding-assignment', exerciseType: 'standard-io',
    environment: { language: 'cpp', runtime: 'wasi' }, toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
    files: [{ id: 'main', path: '/home/user/main.cpp', encoding: 'text', content: 'int main(){return 0;}', mutable: true }],
    publicTests: [], limits: { timeLimitMs: 30_000, outputLimitBytes: 2_000_000, maxFileCount: 16 },
    entryPoint: { path: '/home/user/main.cpp' }, allowLearnerCreatedFiles: false,
  });
  const options: CodingProgramSessionOptions = { candidateKey: 'candidate-a', delivery, manifestUrl: '/emception/manifest.json', createRuntime };
  return { api, execute, acquire, disposeRuntime, whenDisposed, createRuntime, options };
}

function deferred<T>() {
  let resolve: (value: T | PromiseLike<T>) => void = () => undefined;
  const promise = new Promise<T>(done => { resolve = done; });
  return { resolve, promise };
}

async function drain() { for (let index = 0; index < 40; index++) await Promise.resolve(); }
