import { CODING_TOOLCHAIN, parseCodingLearnerDelivery, type CodingLearnerDelivery } from '@game-guild/coding';
import type { BrowserEmceptionAPI, CanvasArtifact, CanvasSession } from '@gameguild/emception-browser';
import { ToolchainPreset, type ToolResult } from 'emception';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createCodingGraphicsSession } from './graphics-session';
import type { CodingGraphicsSessionOptions } from './graphics-session-types';
import type { CodingRuntimeSession, CodingRuntimeSessionFactory } from './runtime-types';

afterEach(() => { vi.useRealTimers(); document.body.replaceChildren(); });

describe('local graphical execution', () => {
  it('does not acquire a runtime while inspecting an immutable candidate', async () => {
    const fixture = setup();
    const session = createCodingGraphicsSession(fixture.options);
    expect(session.getState()).toEqual({ status: 'idle', candidateKey: 'candidate-a', output: { stdout: '', stderr: '', truncated: false } });
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    await session.dispose();
    expect(session.getState().status).toBe('disposed');
    expect(fixture.createRuntime).not.toHaveBeenCalled();
  });

  it('renders only through the public canvas API, never the WASI/test path', async () => {
    const fixture = setup();
    const session = createCodingGraphicsSession(fixture.options);
    await session.run(fixture.canvas);
    expect(session.getState().status).toBe('running');
    expect(fixture.createRuntime).toHaveBeenCalledExactlyOnceWith({ toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN }, manifestUrl: '/emception/manifest.json' });
    expect(fixture.api.workspace.writeFile).toHaveBeenCalledWith('/home/user/main.cpp', 'int main(){return 0;}');
    expect(fixture.api.canvas.build).toHaveBeenCalledWith(expect.objectContaining({ toolchain: ToolchainPreset.SDL_CPP, sourcePath: '/home/user/main.cpp', cwd: '/home/user' }));
    expect(fixture.api.canvas.start).toHaveBeenCalledWith(fixture.artifact, expect.objectContaining({ canvas: fixture.canvas }));
    await session.dispose();
    expect(fixture.stopCanvas).toHaveBeenCalledTimes(1);
    expect(fixture.disposeRuntime).toHaveBeenCalledTimes(1);
  });

  it('freezes exact source bytes, candidate identity and manifest URL', async () => {
    const fixture = setup();
    const delivery = structuredClone(fixture.options.delivery);
    const options = { ...fixture.options, delivery };
    const session = createCodingGraphicsSession(options);
    delivery.files[0].content = 'WRONG SOURCE';
    delivery.toolchain.version = 'wrong';
    options.candidateKey = 'candidate-b';
    options.manifestUrl = '/wrong.json';
    await session.run(fixture.canvas);
    expect(session.getState().candidateKey).toBe('candidate-a');
    expect(fixture.api.workspace.writeFile).toHaveBeenCalledWith('/home/user/main.cpp', 'int main(){return 0;}');
    expect(fixture.createRuntime.mock.calls[0]?.[0].manifestUrl).toBe('/emception/manifest.json');
    await session.dispose();
  });

  it('writes headers/assets without compiling them as native source units', async () => {
    const fixture = setup();
    const delivery = structuredClone(fixture.options.delivery);
    delivery.files.unshift({ id: 'header', path: '/home/user/start.h', encoding: 'text', content: '// initial editor file', mutable: false });
    delivery.files.push({ id: 'asset', path: '/home/user/icon.bin', encoding: 'base64', content: 'AQID', mutable: false });
    const session = createCodingGraphicsSession({ ...fixture.options, delivery });
    await session.run(fixture.canvas);
    expect(fixture.api.workspace.writeFile).toHaveBeenCalledWith('/home/user/icon.bin', new Uint8Array([1, 2, 3]));
    expect(fixture.api.canvas.build.mock.calls[0]?.[0].sourcePath).toBe('/home/user/main.cpp');
    await session.dispose();
  });

  it.each(['wasi', 'raylib', 'allegro5'] as const)('rejects unimplemented canvas environments before boot: %s', async runtime => {
    const fixture = setup();
    const delivery = { ...fixture.options.delivery, environment: { language: 'cpp' as const, runtime } };
    const session = createCodingGraphicsSession({ ...fixture.options, delivery });
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining('unsupported-context') });
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    expect(session.getState()).toMatchObject({ status: 'failed', reason: 'unsupported-context' });
    await session.dispose();
  });

  it('does not silently drop additional native translation units', async () => {
    const fixture = setup();
    const delivery = structuredClone(fixture.options.delivery);
    delivery.files.push({ ...delivery.files[0], id: 'second', path: '/home/user/other.cpp' });
    const session = createCodingGraphicsSession({ ...fixture.options, delivery });
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining('unsupported-context') });
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('does not accept an unknown frozen identity just because SDL was selected', () => {
    const fixture = setup();
    const delivery = structuredClone(fixture.options.delivery);
    delivery.toolchain.version = '4.4.0';
    expect(() => createCodingGraphicsSession({ ...fixture.options, delivery })).toThrow();
    expect(fixture.createRuntime).not.toHaveBeenCalled();
  });

  it.each(['detached', 'different-id', 'duplicate'] as const)('rejects an invalid SDL target before boot: %s', async kind => {
    const fixture = setup();
    if (kind === 'detached') fixture.canvas.remove();
    if (kind === 'different-id') fixture.canvas.id = 'other-target';
    if (kind === 'duplicate') document.body.append(fixture.canvas.cloneNode());
    const session = createCodingGraphicsSession(fixture.options);
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining('unsupported-context') });
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    await session.dispose();
  });

  it('rejects author-only payloads instead of putting private canaries in the VFS', () => {
    const fixture = setup();
    const delivery = { ...fixture.options.delivery, validationSolutions: [{ secret: 'PRIVATE_CANARY' }] };
    expect(() => createCodingGraphicsSession({ ...fixture.options, delivery })).toThrow();
    expect(fixture.createRuntime).not.toHaveBeenCalled();
    expect(fixture.api.workspace.writeFile).not.toHaveBeenCalled();
  });

  it('keeps bounded stdout/stderr independent, not a fake terminal', async () => {
    const fixture = setup();
    const session = createCodingGraphicsSession(fixture.options);
    await session.run(fixture.canvas);
    fixture.api.canvas.start.mock.calls[0]?.[1].onStdout?.('a'.repeat(60_000));
    fixture.api.canvas.start.mock.calls[0]?.[1].onStderr?.('b'.repeat(60_000));
    const state = session.getState();
    expect(state.output.stdout.length + state.output.stderr.length).toBeLessThanOrEqual(100_000);
    expect(state.output.stderr).toContain('bbb');
    expect(state.output.stdout).not.toContain('b');
    expect(state.output.truncated).toBe(true);
    Object.assign(state.output, { stderr: 'mutated' });
    expect(session.getState().output.stderr).not.toBe('mutated');
    await session.dispose();
  });

  it.each(['compile', 'link'] as const)('reports %s failure without starting a canvas or a wrong-answer verdict', async phase => {
    const fixture = setup();
    const bad = { ...toolResult(), exitCode: 1, stderr: `${phase} diagnostic` };
    fixture.api.canvas.build.mockResolvedValue(phase === 'compile' ? { phase, compile: bad } : { phase, compile: toolResult(), link: bad });
    const session = createCodingGraphicsSession(fixture.options);
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining(`${phase}-failed`) });
    expect(session.getState()).toMatchObject({ status: 'failed', reason: `${phase}-failed` });
    expect(fixture.api.canvas.start).not.toHaveBeenCalled();
    expect(fixture.disposeRuntime).toHaveBeenCalledTimes(1);
    await session.dispose();
  });

  it('rejects concurrent runs and settles disposal exactly once', async () => {
    const fixture = setup();
    const session = createCodingGraphicsSession(fixture.options);
    await session.run(fixture.canvas);
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining('already active') });
    await Promise.all([session.dispose(), session.dispose()]);
    expect(fixture.stopCanvas).toHaveBeenCalledTimes(1);
    expect(fixture.disposeRuntime).toHaveBeenCalledTimes(1);
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining('disposed') });
  });

  it('waits for late runtime ownership before allowing a restart', async () => {
    const fixture = setup();
    const acquired = deferred<BrowserEmceptionAPI>();
    const released = deferred<void>();
    fixture.acquire.mockReturnValueOnce(acquired.promise);
    fixture.whenDisposed.mockReturnValueOnce(released.promise);
    const session = createCodingGraphicsSession(fixture.options);
    const started = session.run(fixture.canvas);
    const cancelled = started.then(() => { throw new Error('A cancelled run must reject.'); }, (error: unknown) => error);
    await drain();
    const finished = vi.fn();
    const stop = session.stop().then(finished);
    expect(await cancelled).toMatchObject({ message: expect.stringContaining('cancelled') });
    expect(session.getState().status).toBe('stopping');
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining('already active') });
    acquired.resolve(fixture.api);
    await drain();
    expect(finished).not.toHaveBeenCalled();
    released.resolve();
    await stop;
    expect(session.getState().status).toBe('stopped');
    expect(fixture.api.canvas.build).not.toHaveBeenCalled();
    await session.run(fixture.canvas);
    expect(fixture.createRuntime).toHaveBeenCalledTimes(2);
    await session.dispose();
  });

  it('stops a canvas that arrives after cancellation without publishing its output/state', async () => {
    const fixture = setup();
    const starting = deferred<CanvasSession>();
    fixture.api.canvas.start.mockReturnValueOnce(starting.promise);
    const session = createCodingGraphicsSession(fixture.options);
    const started = session.run(fixture.canvas);
    const cancelled = started.then(() => { throw new Error('A cancelled run must reject.'); }, (error: unknown) => error);
    await drain();
    const stopping = session.stop();
    expect(await cancelled).toMatchObject({ message: expect.stringContaining('cancelled') });
    fixture.api.canvas.start.mock.calls[0]?.[1].onStdout?.('late PRIVATE_CANARY');
    starting.resolve(fixture.canvasSession);
    await stopping;
    expect(fixture.stopCanvas).toHaveBeenCalledTimes(1);
    expect(session.getState()).toMatchObject({ status: 'stopped', output: { stdout: '', stderr: '' } });
    await session.dispose();
  });

  it('uses separate bounded preparation and student timers', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    const building = deferred<CanvasArtifact>();
    fixture.api.canvas.build.mockReturnValueOnce(building.promise);
    const delivery = { ...fixture.options.delivery, limits: { ...fixture.options.delivery.limits, timeLimitMs: 1000 } };
    const session = createCodingGraphicsSession({ ...fixture.options, delivery });
    const started = session.run(fixture.canvas).then(() => undefined, (error: unknown) => error);
    await drain();
    await vi.advanceTimersByTimeAsync(10_000);
    expect(session.getState().status).toBe('building');
    building.resolve(fixture.artifact);
    expect(await started).toBeUndefined();
    await vi.advanceTimersByTimeAsync(999);
    expect(session.getState().status).toBe('running');
    await vi.advanceTimersByTimeAsync(1);
    await drain();
    expect(session.getState()).toMatchObject({ status: 'failed', reason: 'execution-timeout' });
    expect(fixture.stopCanvas).toHaveBeenCalledTimes(1);
    expect(fixture.disposeRuntime).toHaveBeenCalledTimes(1);
    await session.dispose();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('cancels a stalled build without starting a late canvas', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    const building = deferred<CanvasArtifact>();
    fixture.api.canvas.build.mockReturnValueOnce(building.promise);
    const session = createCodingGraphicsSession(fixture.options);
    const started = session.run(fixture.canvas);
    const rejected = started.then(() => { throw new Error('A timed-out run must reject.'); }, (error: unknown) => error);
    await drain();
    await vi.advanceTimersByTimeAsync(120_000);
    expect(await rejected).toMatchObject({ message: expect.stringContaining('preparation-timeout') });
    expect(fixture.disposeRuntime).toHaveBeenCalledTimes(1);
    building.resolve(fixture.artifact);
    await session.dispose();
    expect(fixture.api.canvas.start).not.toHaveBeenCalled();
    expect(vi.getTimerCount()).toBe(0);
  });

  it('counts UTF-8 output bytes across both channels and disposes on overflow', async () => {
    const fixture = setup();
    const delivery = { ...fixture.options.delivery, limits: { ...fixture.options.delivery.limits, outputLimitBytes: 5 } };
    const session = createCodingGraphicsSession({ ...fixture.options, delivery });
    await session.run(fixture.canvas);
    fixture.api.canvas.start.mock.calls[0]?.[1].onStdout?.('\u00e9\u00e9');
    fixture.api.canvas.start.mock.calls[0]?.[1].onStderr?.('\u00e9');
    await drain();
    expect(session.getState()).toMatchObject({ status: 'failed', reason: 'output-limit' });
    expect(fixture.stopCanvas).toHaveBeenCalledTimes(1);
    expect(fixture.disposeRuntime).toHaveBeenCalledTimes(1);
    await session.dispose();
  });

  it('allows retry after a failed build without retaining old files/runtime', async () => {
    const fixture = setup();
    fixture.api.canvas.build.mockResolvedValueOnce({ phase: 'compile', compile: { ...toolResult(), exitCode: 1 } });
    const session = createCodingGraphicsSession(fixture.options);
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining('compile-failed') });
    await session.run(fixture.canvas);
    expect(fixture.createRuntime).toHaveBeenCalledTimes(2);
    expect(session.getState().status).toBe('running');
    await session.dispose();
  });

  it('retains a cleanup failure and never declares the session reusable', async () => {
    const fixture = setup();
    fixture.whenDisposed.mockRejectedValue(new Error('synthetic release failure'));
    const session = createCodingGraphicsSession(fixture.options);
    await session.run(fixture.canvas);
    await expect(session.stop()).rejects.toMatchObject({ message: expect.stringContaining('cleanup-failed') });
    expect(session.getState()).toMatchObject({ status: 'failed', reason: 'cleanup-failed' });
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining('cleanup-failed') });
    await expect(session.dispose()).rejects.toMatchObject({ message: expect.stringContaining('cleanup-failed') });
  });

  it('reports a runtime-factory failure without leaving a loading state', async () => {
    const fixture = setup();
    fixture.createRuntime.mockImplementation(() => { throw new Error('synthetic factory failure'); });
    const session = createCodingGraphicsSession(fixture.options);
    await expect(session.run(fixture.canvas)).rejects.toMatchObject({ message: expect.stringContaining('runtime-unavailable') });
    expect(session.getState()).toMatchObject({ status: 'failed', reason: 'runtime-unavailable' });
    await session.dispose();
  });

  it('does not erase failures while closing a canvas that arrives late', async () => {
    const fixture = setup();
    const starting = deferred<CanvasSession>();
    fixture.api.canvas.start.mockReturnValueOnce(starting.promise);
    fixture.stopCanvas.mockImplementation(() => { throw new Error('late cleanup failed'); });
    const session = createCodingGraphicsSession(fixture.options);
    const cancelled = session.run(fixture.canvas).then(() => undefined, (error: unknown) => error);
    await drain();
    const stopped = session.stop().then(() => undefined, (error: unknown) => error);
    expect(await cancelled).toMatchObject({ message: expect.stringContaining('cancelled') });
    starting.resolve(fixture.canvasSession);
    expect(await stopped).toMatchObject({ message: expect.stringContaining('cleanup-failed') });
    expect(session.getState()).toMatchObject({ status: 'failed', reason: 'cleanup-failed' });
    await expect(session.dispose()).rejects.toMatchObject({ message: expect.stringContaining('cleanup-failed') });
  });

  it('checks the shared preparation deadline even when a busy loop delays timers', async () => {
    vi.useFakeTimers();
    const fixture = setup();
    const building = deferred<CanvasArtifact>();
    fixture.api.canvas.build.mockReturnValueOnce(building.promise);
    const session = createCodingGraphicsSession(fixture.options);
    const started = session.run(fixture.canvas).then(() => undefined, (error: unknown) => error);
    await drain();
    vi.setSystemTime(Date.now() + 120_001);
    building.resolve(fixture.artifact);
    expect(await started).toMatchObject({ message: expect.stringContaining('preparation-timeout') });
    expect(fixture.api.canvas.start).not.toHaveBeenCalled();
    await session.dispose();
    expect(vi.getTimerCount()).toBe(0);
  });
});

function toolResult(): ToolResult { return { exitCode: 0, stdout: '', stderr: '', durationMs: 1, timedOut: false }; }

function setup() {
  const fail = () => { throw new Error('Forbidden non-canvas path in this test.'); };
  const artifact: CanvasArtifact = { phase: 'ready', compile: toolResult(), link: toolResult(), runtimeProfile: 'sdl3-runtime', runtimePath: '/sdl3.mjs', wasmPath: '/tmp/local.wasm', runtimeGlue: new Uint8Array(), wasm: new Uint8Array() };
  const stopCanvas = vi.fn();
  const canvasSession: CanvasSession = { runtimeProfile: 'sdl3-runtime', stop: stopCanvas };
  const api = {
    workspace: {
      list: fail, switch: fail, reset: fail, readFile: fail, writeFile: vi.fn<BrowserEmceptionAPI['workspace']['writeFile']>().mockResolvedValue(undefined), deleteFile: fail,
      listFiles: fail, setVisibility: fail, getBuild: fail, setBuild: fail, exportZip: fail, importZip: fail,
    },
    run: fail, compileAndRun: fail, runTests: fail, on: fail, dispose: fail,
    canvas: {
      build: vi.fn<BrowserEmceptionAPI['canvas']['build']>().mockResolvedValue(artifact),
      start: vi.fn<BrowserEmceptionAPI['canvas']['start']>().mockResolvedValue(canvasSession),
      buildAndStart: fail, stop: fail,
    },
  } satisfies BrowserEmceptionAPI;
  const acquire = vi.fn<CodingRuntimeSession['acquire']>().mockResolvedValue(api);
  const disposeRuntime = vi.fn();
  const whenDisposed = vi.fn<CodingRuntimeSession['whenDisposed']>().mockResolvedValue(undefined);
  const createRuntime = vi.fn<CodingRuntimeSessionFactory>().mockImplementation(() => ({
    getState: () => ({ status: 'idle' }), acquire, dispose: disposeRuntime, whenDisposed,
  }));
  const delivery: CodingLearnerDelivery = parseCodingLearnerDelivery({
    schemaVersion: 1, type: 'coding-assignment', exerciseType: 'standard-io',
    environment: { language: 'cpp', runtime: 'sdl3' }, toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
    files: [{ id: 'main', path: '/home/user/main.cpp', encoding: 'text', content: 'int main(){return 0;}', mutable: true }],
    publicTests: [], limits: { timeLimitMs: 30_000, outputLimitBytes: 2_000_000, maxFileCount: 16 },
    entryPoint: { path: '/home/user/main.cpp' }, allowLearnerCreatedFiles: false,
  });
  const options: CodingGraphicsSessionOptions = { candidateKey: 'candidate-a', delivery, manifestUrl: '/emception/manifest.json', createRuntime };
  const canvas = document.createElement('canvas');
  canvas.id = 'canvas';
  document.body.replaceChildren(canvas);
  return { options, api, artifact, canvasSession, stopCanvas, createRuntime, acquire, disposeRuntime, whenDisposed, canvas };
}

function deferred<T>() {
  let resolve: (value: T | PromiseLike<T>) => void = () => undefined;
  const promise = new Promise<T>(done => { resolve = done; });
  return { resolve, promise };
}

async function drain() { for (let index = 0; index < 20; index++) await Promise.resolve(); }
