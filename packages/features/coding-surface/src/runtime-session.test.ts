// @vitest-environment node
import { CODING_TOOLCHAIN } from '@game-guild/coding';
import { parseManifest, type BrowserEmceptionAPI } from '@gameguild/emception-browser';
import type { ReleaseFSManifest } from 'emception';
import { describe, expect, it, vi } from 'vitest';
import { createCodingRuntimeSession } from './runtime-session';
import type { CodingRuntimeModule, CodingRuntimeSessionOptions } from './runtime-types';

describe('lazy coding runtime session', () => {
  it('does not import, fetch or boot when created or inspected', () => {
    const fixture = setup();
    const session = createCodingRuntimeSession(fixture.options);
    expect(session.getState()).toEqual({ status: 'idle' });
    expect(session.getState()).toEqual({ status: 'idle' });
    expect(fixture.loadModule).not.toHaveBeenCalled();
    expect(fixture.readManifest).not.toHaveBeenCalled();
    expect(fixture.createEmception).not.toHaveBeenCalled();
  });

  it('shares acquisition and reuses only the runtime owned by this session', async () => {
    const fixture = setup();
    const session = createCodingRuntimeSession(fixture.options);
    const first = session.acquire();
    const second = session.acquire();
    expect(first).toBe(second);
    expect(session.getState()).toEqual({ status: 'loading' });
    expect(await first).toBe(fixture.api);
    expect(await session.acquire()).toBe(fixture.api);
    expect(session.getState()).toEqual({ status: 'ready' });
    expect(fixture.loadModule).toHaveBeenCalledTimes(1);
    expect(fixture.readManifest).toHaveBeenCalledExactlyOnceWith('/emception/manifest.json', expect.any(AbortSignal));
    expect(fixture.createEmception).toHaveBeenCalledExactlyOnceWith({ manifestUrl: '/emception/manifest.json', tty: 'none' });
    session.dispose();
  });

  it('never shares a runtime across distinct surface sessions', async () => {
    const left = setup();
    const right = setup();
    const first = createCodingRuntimeSession(left.options);
    const second = createCodingRuntimeSession(right.options);
    expect(await first.acquire()).not.toBe(await second.acquire());
    first.dispose();
    expect(left.api.dispose).toHaveBeenCalledTimes(1);
    expect(right.api.dispose).not.toHaveBeenCalled();
    second.dispose();
  });

  it('accepts the frozen C identity without substituting the C++ identity', async () => {
    const fixture = setup();
    const session = createCodingRuntimeSession({ ...fixture.options, toolchain: { ...fixture.options.toolchain, key: 'c-emception' } });
    expect(await session.acquire()).toBe(fixture.api);
    expect(session.getState()).toEqual({ status: 'ready' });
    session.dispose();
  });

  it('reports a failed module import without fetching metadata or creating a worker', async () => {
    const fixture = setup();
    fixture.loadModule.mockRejectedValueOnce(new Error('synthetic import failure'));
    const session = createCodingRuntimeSession(fixture.options);
    await expect(session.acquire()).rejects.toMatchObject({ message: 'Coding runtime acquisition failed: runtime-unavailable.' });
    expect(session.getState()).toEqual({ status: 'error', reason: 'runtime-unavailable' });
    expect(fixture.readManifest).not.toHaveBeenCalled();
    expect(fixture.createEmception).not.toHaveBeenCalled();
    session.dispose();
  });

  it('uses the default fetch port only on explicit acquisition', async () => {
    const fixture = setup();
    const fetch = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify(manifest()), { status: 200 }));
    const session = createCodingRuntimeSession({ ...fixture.options, readManifest: undefined });
    try {
      expect(fetch).not.toHaveBeenCalled();
      expect(await session.acquire()).toBe(fixture.api);
      expect(fetch).toHaveBeenCalledExactlyOnceWith('/emception/manifest.json', { signal: expect.any(AbortSignal) });
      expect(fixture.readManifest).not.toHaveBeenCalled();
    } finally {
      session.dispose();
      fetch.mockRestore();
    }
  });

  it('rejects an HTTP failure without parsing its body or starting a worker', async () => {
    const fixture = setup();
    const response = new Response(JSON.stringify(manifest()), { status: 503 });
    const parse = vi.spyOn(response, 'json');
    const fetch = vi.spyOn(globalThis, 'fetch').mockResolvedValue(response);
    const session = createCodingRuntimeSession({ ...fixture.options, readManifest: undefined });
    try {
      await expect(session.acquire()).rejects.toMatchObject({ message: 'Coding runtime acquisition failed: runtime-unavailable.' });
      expect(parse).not.toHaveBeenCalled();
      expect(fixture.createEmception).not.toHaveBeenCalled();
      expect(session.getState()).toEqual({ status: 'error', reason: 'runtime-unavailable' });
    } finally {
      session.dispose();
      fetch.mockRestore();
    }
  });

  it('does not let callers mutate its state snapshot', () => {
    const fixture = setup();
    const session = createCodingRuntimeSession(fixture.options);
    Object.assign(session.getState(), { status: 'ready' });
    expect(session.getState()).toEqual({ status: 'idle' });
    session.dispose();
  });

  it.each([
    { key: 'other-emception' }, { version: '4.4.0' }, { runtimeAbi: 'unknown-abi' }, { artifactDigest: 'sha256:wrong' as const },
  ])('rejects an unsupported frozen identity before loading anything: %o', async (change) => {
    const fixture = setup();
    const session = createCodingRuntimeSession({ ...fixture.options, toolchain: { ...fixture.options.toolchain, ...change } });
    await expect(session.acquire()).rejects.toThrow();
    expect(session.getState()).toEqual({ status: 'error', reason: 'unsupported-toolchain' });
    expect(fixture.loadModule).not.toHaveBeenCalled();
    expect(fixture.readManifest).not.toHaveBeenCalled();
    expect(fixture.createEmception).not.toHaveBeenCalled();
    session.dispose();
  });

  it.each([
    { artifactVersion: '4.5.1' }, { runtimeAbi: 'wrong-abi' }, { toolchainLockHash: 'b'.repeat(64) },
    { buildReceiptHash: undefined }, { buildReceiptHash: 'bad' }, { buildFingerprint: 'bad' },
    { schemaVersion: undefined, version: 1 },
  ])('rejects incompatible/legacy manifest before starting a worker: %o', async (change) => {
    const fixture = setup();
    fixture.readManifest.mockResolvedValue({ ...manifest(), ...change });
    const session = createCodingRuntimeSession(fixture.options);
    await expect(session.acquire()).rejects.toThrow();
    expect(session.getState()).toEqual({ status: 'error', reason: 'incompatible-manifest' });
    expect(fixture.createEmception).not.toHaveBeenCalled();
    session.dispose();
  });

  it('allows an explicit retry after metadata loading fails, without automatic retries', async () => {
    const fixture = setup();
    fixture.readManifest.mockRejectedValueOnce(new Error('synthetic network failure'));
    const session = createCodingRuntimeSession(fixture.options);
    await expect(session.acquire()).rejects.toThrow();
    expect(session.getState()).toEqual({ status: 'error', reason: 'runtime-unavailable' });
    expect(fixture.readManifest).toHaveBeenCalledTimes(1);
    expect(fixture.createEmception).not.toHaveBeenCalled();
    expect(await session.acquire()).toBe(fixture.api);
    expect(fixture.readManifest).toHaveBeenCalledTimes(2);
    session.dispose();
  });

  it('never reports ready after the public runtime rejects boot', async () => {
    const fixture = setup();
    fixture.createEmception.mockRejectedValueOnce(new Error('synthetic boot failure'));
    const session = createCodingRuntimeSession(fixture.options);
    await expect(session.acquire()).rejects.toThrow();
    expect(session.getState()).toEqual({ status: 'error', reason: 'runtime-unavailable' });
    expect(fixture.api.dispose).not.toHaveBeenCalled();
    session.dispose();
  });

  it('retains the original boot failure as cause without putting it in UI state', async () => {
    const fixture = setup();
    const failure = new Error('synthetic transport detail');
    fixture.createEmception.mockRejectedValueOnce(failure);
    const session = createCodingRuntimeSession(fixture.options);
    await expect(session.acquire()).rejects.toMatchObject({
      message: 'Coding runtime acquisition failed: runtime-unavailable.', cause: failure,
    });
    expect(session.getState()).toEqual({ status: 'error', reason: 'runtime-unavailable' });
    expect(JSON.stringify(session.getState())).not.toContain('synthetic transport detail');
    session.dispose();
  });

  it('does not fetch or create a runtime if disposed while the module loads', async () => {
    const fixture = setup();
    const loading = deferred<CodingRuntimeModule>();
    fixture.loadModule.mockReturnValue(loading.promise);
    const session = createCodingRuntimeSession(fixture.options);
    const acquired = session.acquire();
    const rejected = expect(acquired).rejects.toMatchObject({ message: 'The coding runtime session is disposed.' });
    session.dispose();
    await rejected;
    loading.resolve(fixture.module);
    await drainMicrotasks();
    expect(fixture.readManifest).not.toHaveBeenCalled();
    expect(fixture.createEmception).not.toHaveBeenCalled();
    expect(session.getState()).toEqual({ status: 'disposed' });
  });

  it('aborts a pending metadata request and never boots from its late response', async () => {
    const fixture = setup();
    const metadata = deferred<unknown>();
    fixture.readManifest.mockReturnValue(metadata.promise);
    const session = createCodingRuntimeSession(fixture.options);
    const acquired = session.acquire();
    const rejected = expect(acquired).rejects.toMatchObject({ message: 'The coding runtime session is disposed.' });
    await drainMicrotasks();
    const signal = fixture.readManifest.mock.calls[0]?.[1];
    expect(signal?.aborted).toBe(false);
    session.dispose();
    await rejected;
    expect(signal?.aborted).toBe(true);
    metadata.resolve(manifest());
    await drainMicrotasks();
    expect(fixture.createEmception).not.toHaveBeenCalled();
    expect(session.getState()).toEqual({ status: 'disposed' });
  });

  it('settles disposal immediately and closes a late boot result exactly once', async () => {
    const fixture = setup();
    const boot = deferred<BrowserEmceptionAPI>();
    fixture.createEmception.mockReturnValue(boot.promise);
    const session = createCodingRuntimeSession(fixture.options);
    const acquired = session.acquire();
    const rejected = expect(acquired).rejects.toMatchObject({ message: 'The coding runtime session is disposed.' });
    await drainMicrotasks();
    expect(fixture.createEmception).toHaveBeenCalledTimes(1);
    session.dispose();
    session.dispose();
    await rejected;
    expect(fixture.api.dispose).not.toHaveBeenCalled();
    boot.resolve(fixture.api);
    await drainMicrotasks();
    expect(fixture.api.canvas.stop).toHaveBeenCalledTimes(1);
    expect(fixture.api.dispose).toHaveBeenCalledTimes(1);
    expect(session.getState()).toEqual({ status: 'disposed' });
    await expect(session.acquire()).rejects.toMatchObject({ message: 'The coding runtime session is disposed.' });
    expect(fixture.createEmception).toHaveBeenCalledTimes(1);
  });

  it('stops the canvas before disposing a ready runtime and remains disposed', async () => {
    const fixture = setup();
    const session = createCodingRuntimeSession(fixture.options);
    await session.acquire();
    session.dispose();
    session.dispose();
    expect(fixture.api.canvas.stop).toHaveBeenCalledTimes(1);
    expect(fixture.api.dispose).toHaveBeenCalledTimes(1);
    expect(fixture.api.canvas.stop.mock.invocationCallOrder[0]).toBeLessThan(fixture.api.dispose.mock.invocationCallOrder[0]);
    await expect(session.acquire()).rejects.toMatchObject({ message: 'The coding runtime session is disposed.' });
  });

  it('still disposes the worker if canvas teardown throws', async () => {
    const fixture = setup();
    fixture.api.canvas.stop.mockImplementation(() => { throw new Error('synthetic cleanup failure'); });
    const session = createCodingRuntimeSession(fixture.options);
    await session.acquire();
    expect(() => session.dispose()).toThrow('synthetic cleanup failure');
    expect(fixture.api.dispose).toHaveBeenCalledTimes(1);
    expect(session.getState()).toEqual({ status: 'disposed' });
    session.dispose();
  });

  it('captures its identity and URL instead of following mutable caller options', async () => {
    const fixture = setup();
    const options = { ...fixture.options, toolchain: { ...fixture.options.toolchain } };
    const session = createCodingRuntimeSession(options);
    options.toolchain.version = '4.4.0';
    options.manifestUrl = '/other/manifest.json';
    await session.acquire();
    expect(fixture.readManifest.mock.calls[0]?.[0]).toBe('/emception/manifest.json');
    expect(fixture.createEmception).toHaveBeenCalledExactlyOnceWith({ manifestUrl: '/emception/manifest.json', tty: 'none' });
    session.dispose();
  });
});

function manifest(): ReleaseFSManifest {
  return {
    schemaVersion: 2, version: 2, artifactVersion: CODING_TOOLCHAIN.version, runtimeAbi: CODING_TOOLCHAIN.runtimeAbi,
    toolchainLockHash: CODING_TOOLCHAIN.artifactDigest.slice('sha256:'.length),
    buildReceiptHash: 'a'.repeat(64), buildFingerprint: 'c'.repeat(64), patchSetVersion: 'fixture',
    generated: '2026-11-22T00:00:00Z', baseUrl: '/emception/',
    toolVersions: { pythonMajorMinor: '3.14', pythonMajorMinorCompact: '314' },
    profiles: {}, files: {}, bundles: {},
  };
}

function setup() {
  const fail = () => { throw new Error('This lifecycle test must not execute code or inspect files.'); };
  const api = {
    workspace: {
      list: fail, switch: fail, reset: fail, readFile: fail, writeFile: fail, deleteFile: fail,
      listFiles: fail, setVisibility: fail, getBuild: fail, setBuild: fail, exportZip: fail, importZip: fail,
    },
    run: fail, compileAndRun: fail, runTests: fail, on: fail, dispose: vi.fn(),
    canvas: { build: fail, start: fail, buildAndStart: fail, stop: vi.fn() },
  } satisfies BrowserEmceptionAPI;
  const createEmception = vi.fn<CodingRuntimeModule['createEmception']>().mockResolvedValue(api);
  const module: CodingRuntimeModule = { parseManifest, createEmception };
  const loadModule = vi.fn<NonNullable<CodingRuntimeSessionOptions['loadModule']>>().mockResolvedValue(module);
  const readManifest = vi.fn<NonNullable<CodingRuntimeSessionOptions['readManifest']>>().mockResolvedValue(manifest());
  const options: CodingRuntimeSessionOptions = {
    toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN }, manifestUrl: '/emception/manifest.json', loadModule, readManifest,
  };
  return { api, module, createEmception, loadModule, readManifest, options };
}

function deferred<T>() {
  let resolve: (value: T) => void = () => undefined;
  const promise = new Promise<T>((complete) => { resolve = complete; });
  return { promise, resolve };
}

async function drainMicrotasks() {
  for (let index = 0; index < 12; index++) await Promise.resolve();
}
