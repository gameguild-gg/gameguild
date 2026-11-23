import { CODING_TOOLCHAIN, type CodingToolchainIdentity } from '@game-guild/coding';
import type { BrowserEmceptionAPI } from '@gameguild/emception-browser';
import type { FSManifest } from 'emception';
import type { CodingRuntimeFailureReason, CodingRuntimeSessionFactory, CodingRuntimeSessionState } from './runtime-types';

export const createCodingRuntimeSession: CodingRuntimeSessionFactory = (options) => {
  const toolchain = { ...options.toolchain };
  const manifestUrl = options.manifestUrl;
  const loadModule = options.loadModule ?? (() => import('@gameguild/emception-browser'));
  const readManifest = options.readManifest ?? fetchManifest;
  let state: CodingRuntimeSessionState = { status: 'idle' };
  let runtime: BrowserEmceptionAPI | null = null;
  let pending: Promise<BrowserEmceptionAPI> | null = null;
  let controller: AbortController | null = null;
  let disposed = false;

  function assertActive() {
    if (disposed) throw new Error('The coding runtime session is disposed.');
  }

  return {
    getState: () => ({ ...state }),
    acquire() {
      if (disposed) return Promise.reject(new Error('The coding runtime session is disposed.'));
      if (runtime) return Promise.resolve(runtime);
      if (pending) return pending;
      state = { status: 'loading' };
      const attemptController = new AbortController();
      controller = attemptController;
      let reason: CodingRuntimeFailureReason = 'unsupported-toolchain';
      let onAbort: () => void = () => undefined;
      const cancelled = new Promise<never>((_resolve, reject) => {
        onAbort = () => reject(new Error('The coding runtime session is disposed.'));
        attemptController.signal.addEventListener('abort', onAbort, { once: true });
      });
      const initialize = async () => {
        if (!isSupportedToolchain(toolchain)) throw new Error('Unsupported coding toolchain.');
        reason = 'runtime-unavailable';
        const module = await loadModule();
        assertActive();
        const metadata = await readManifest(manifestUrl, attemptController.signal);
        assertActive();
        reason = 'incompatible-manifest';
        const manifest = module.parseManifest(metadata, { expectedRuntimeAbi: toolchain.runtimeAbi });
        if (!matchesToolchain(manifest, toolchain)) throw new Error('Incompatible coding runtime assets.');
        reason = 'runtime-unavailable';
        const created = await module.createEmception({ manifestUrl, tty: 'none' });
        if (disposed) {
          closeRuntime(created);
          assertActive();
        }
        runtime = created;
        state = { status: 'ready' };
        return created;
      };

      // Disposal settles immediately even if import/boot cannot be aborted.
      // initialize still owns and closes any instance that arrives afterwards.
      pending = Promise.race([initialize(), cancelled])
        .catch((cause: unknown) => {
          if (disposed) throw new Error('The coding runtime session is disposed.', { cause });
          state = { status: 'error', reason };
          throw new Error(`Coding runtime acquisition failed: ${reason}.`, { cause });
        })
        .finally(() => {
          attemptController.signal.removeEventListener('abort', onAbort);
          controller = null;
          pending = null;
        });
      return pending;
    },
    dispose() {
      if (disposed) return;
      disposed = true;
      state = { status: 'disposed' };
      controller?.abort();
      const owned = runtime;
      runtime = null;
      if (owned) closeRuntime(owned);
    },
  };
};

function isSupportedToolchain(toolchain: CodingToolchainIdentity): boolean {
  return (toolchain.key === 'c-emception' || toolchain.key === 'cpp-emception')
    && toolchain.version === CODING_TOOLCHAIN.version
    && toolchain.runtimeAbi === CODING_TOOLCHAIN.runtimeAbi
    && toolchain.artifactDigest === CODING_TOOLCHAIN.artifactDigest;
}

function matchesToolchain(manifest: FSManifest, toolchain: CodingToolchainIdentity): boolean {
  return manifest.schemaVersion === 2
    && manifest.artifactVersion === toolchain.version
    && manifest.runtimeAbi === toolchain.runtimeAbi
    && `sha256:${manifest.toolchainLockHash}` === toolchain.artifactDigest
    && /^[a-f0-9]{64}$/.test(manifest.buildReceiptHash ?? '')
    && /^[a-f0-9]{64}$/.test(manifest.buildFingerprint);
}

async function fetchManifest(url: string, signal: AbortSignal): Promise<unknown> {
  const response = await fetch(url, { signal });
  if (!response.ok) throw new Error('Coding runtime metadata could not be loaded.');
  return response.json();
}

function closeRuntime(runtime: BrowserEmceptionAPI) {
  try {
    runtime.canvas.stop();
  } finally {
    runtime.dispose();
  }
}
