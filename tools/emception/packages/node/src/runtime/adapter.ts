// Phase 7.1 / 7.2 — Node `RuntimeAdapter` skeleton.
//
// Wires the runtime-agnostic `RuntimeAdapter` interface from `@emception/core`
// against Node primitives:
//
//   - `spawnWorker`           → `node:worker_threads.Worker` (with an
//                                 EventTarget-shaped facade so core can use
//                                 the same `addEventListener('message', ...)`
//                                 API as the browser's `Worker`).
//   - `loadManifest`          → delegates to the existing Phase 7.6 loader.
//   - `openWorkspaceStore`    → opens an `FsWorkspaceManager` rooted at
//                                 `<fsRoot>` (default `<os.tmpdir()>/emception`)
//                                 and returns it as the store handle's
//                                 `resource`.
//   - `transferable(value)`   → detects `MessagePort` / `ArrayBuffer` /
//                                 `SharedArrayBuffer`, the only types Node's
//                                 worker_threads structured-clone honors as
//                                 transferables.
//   - `hasSharedArrayBuffer`  → always true on Node 16+ (the supported floor).
//
// The actual worker entry script (the WASM toolchain bringup) is not part of
// this commit — it lands in a follow-up. `spawnWorker` therefore requires the
// caller to supply `{ workerEntry }` either at adapter construction or per-
// spawn. If no entry is configured, `spawnWorker` throws a descriptive error
// rather than silently spawning an empty worker.
//
// All deps are Node built-ins; no third-party packages added.

import os from 'node:os';
import path from 'node:path';
import {
    MessageChannel,
    MessagePort,
    Worker as NodeWorker,
} from 'node:worker_threads';

import {
    BuildConfigError,
    RuntimeFeatureUnavailableError,
    type FSManifest,
    type ManifestSource,
    type RuntimeAdapter,
    type SpawnWorkerOptions,
    type WorkerHandle,
    type WorkspaceStoreHandle,
    type WorkspaceStoreOptions,
} from '@emception/core';

import type { WorkspaceManager } from '@emception/core';
import { createFsWorkspaceManager } from '../workspace/store-fs.js';
import { loadManifest } from './manifest.js';

/**
 * Local alias for the DOM `Transferable` type so this file compiles under
 * `lib: ['esnext']` (no DOM). Structurally a subset of the browser
 * `Transferable` union, so assigning `Transferable[]` here to a method
 * declared with the DOM-typed `Transferable[]` (in `@emception/core`)
 * remains covariant-compatible.
 */
type Transferable = MessagePort | ArrayBuffer | SharedArrayBuffer;

export interface NodeRuntimeAdapterOptions {
    /**
     * URL or filesystem path to the worker entry module. Required for
     * `spawnWorker` to succeed. Both `.mjs` and `.cjs` are supported by
     * `node:worker_threads.Worker`.
     */
    workerEntry?: string | URL;
    /**
     * Default fs root for workspace stores (when callers don't pass `fsRoot`
     * in `openWorkspaceStore`). Defaults to `<os.tmpdir()>/emception`.
     */
    fsRoot?: string;
}

/**
 * Build a Node-side `RuntimeAdapter`. Pure factory — no I/O happens until
 * one of the returned methods is called.
 */
export function createNodeRuntimeAdapter(
    opts: NodeRuntimeAdapterOptions = {},
): RuntimeAdapter {
    const defaultFsRoot = opts.fsRoot ?? path.join(os.tmpdir(), 'emception');

    return {
        name: 'node',

        async spawnWorker(spawnOpts: SpawnWorkerOptions = {}): Promise<WorkerHandle> {
            if (!opts.workerEntry) {
                throw new RuntimeFeatureUnavailableError(
                    'createNodeRuntimeAdapter: spawnWorker requires { workerEntry } ' +
                    'to point at the toolchain worker module.',
                );
            }
            const entry = typeof opts.workerEntry === 'string'
                ? toFileUrl(opts.workerEntry)
                : opts.workerEntry;
            const worker = new NodeWorker(entry, {
                name: spawnOpts.name,
                workerData: spawnOpts.workspaceRoot
                    ? { workspaceRoot: spawnOpts.workspaceRoot }
                    : undefined,
            });
            return wrapNodeWorker(worker);
        },

        async loadManifest(source: ManifestSource): Promise<FSManifest> {
            return loadManifest(source);
        },

        async openWorkspaceStore(
            storeOpts: WorkspaceStoreOptions,
        ): Promise<WorkspaceStoreHandle> {
            const kind = storeOpts.kind ?? 'fs';
            if (kind !== 'fs') {
                throw new BuildConfigError(
                    `createNodeRuntimeAdapter: workspace store kind '${kind}' not ` +
                    `supported by the Node adapter (use 'fs').`,
                );
            }
            const root = storeOpts.fsRoot ?? defaultFsRoot;
            const manager = await createFsWorkspaceManager({ root });
            // Pre-open the named workspace so the worker just calls
            // `resource.activeHandle` and skips the lookup. Tolerant of
            // managers without an `open` method (forward-compat).
            const handle = typeof (manager as { open?: unknown }).open === 'function'
                ? await (manager as WorkspaceManager).open({ name: storeOpts.name })
                : null;
            return {
                name: storeOpts.name,
                kind: 'fs',
                resource: { manager, handle, root },
                async close() {
                    await disposeManager(manager);
                },
            };
        },

        transferable(value: unknown): Transferable[] {
            const out: Transferable[] = [];
            for (const v of walk(value)) {
                if (v instanceof MessagePort) out.push(v);
                else if (v instanceof ArrayBuffer) out.push(v);
                else if (typeof SharedArrayBuffer !== 'undefined' && v instanceof SharedArrayBuffer) {
                    // SAB is shared, not transferred — but core's adapter contract
                    // returns an array of "things to hand to the worker"; SABs
                    // belong here so the postMessage call doesn't drop them.
                    out.push(v);
                }
            }
            return out;
        },

        hasSharedArrayBuffer(): boolean {
            return typeof SharedArrayBuffer !== 'undefined';
        },
    };
}

/**
 * Wrap a `node:worker_threads.Worker` so it satisfies the EventTarget-shaped
 * `WorkerHandle` interface that `@emception/core` consumes (which mirrors the
 * browser `Worker` API).
 */
export function wrapNodeWorker(worker: NodeWorker): WorkerHandle {
    type Listener = (ev: any) => void;
    // Track wrappers so `removeEventListener` can find the original Node
    // listener registered via `worker.on(...)`. Without this, removal would
    // silently no-op because each call to addEventListener creates a new
    // closure.
    const messageWrappers = new WeakMap<Listener, (data: unknown) => void>();
    const errorWrappers = new WeakMap<Listener, (err: Error) => void>();

    return {
        postMessage(message: unknown, transfer?: Transferable[]): void {
            if (transfer && transfer.length > 0) {
                worker.postMessage(message, transfer as unknown as readonly any[]);
            } else {
                worker.postMessage(message);
            }
        },
        terminate(): Promise<void> {
            return worker.terminate().then(() => undefined);
        },
        addEventListener(type: 'message' | 'error', listener: Listener): void {
            if (type === 'message') {
                const wrap = (data: unknown) => listener({ data });
                messageWrappers.set(listener, wrap);
                worker.on('message', wrap);
            } else {
                const wrap = (err: Error) => listener({ error: err, message: err.message });
                errorWrappers.set(listener, wrap);
                worker.on('error', wrap);
            }
        },
        removeEventListener(type: 'message' | 'error', listener: Listener): void {
            if (type === 'message') {
                const wrap = messageWrappers.get(listener);
                if (wrap) {
                    worker.off('message', wrap);
                    messageWrappers.delete(listener);
                }
            } else {
                const wrap = errorWrappers.get(listener);
                if (wrap) {
                    worker.off('error', wrap);
                    errorWrappers.delete(listener);
                }
            }
        },
    };
}

/** Re-export so tests can construct paired ports without importing node:worker_threads. */
export { MessageChannel, MessagePort };

// ─────────────────────────── helpers ───────────────────────────

function toFileUrl(p: string): URL {
    if (p.startsWith('file://')) return new URL(p);
    return new URL(`file://${path.resolve(p)}`);
}

/**
 * Iterate over the candidate values inside `value` that may carry
 * transferables. We intentionally keep this shallow — one level of array /
 * plain-object — to match the browser adapter and avoid pathological deep
 * traversal of user payloads.
 */
function* walk(value: unknown): Iterable<unknown> {
    if (value == null) return;
    yield value;
    if (Array.isArray(value)) {
        for (const v of value) yield v;
        return;
    }
    if (typeof value === 'object') {
        for (const v of Object.values(value as Record<string, unknown>)) yield v;
    }
}

async function disposeManager(manager: WorkspaceManager): Promise<void> {
    // WorkspaceManager exposes dispose() per its contract; tolerate
    // adapters that haven't grown one yet so this stays forward-compatible.
    const m = manager as unknown as { dispose?: () => unknown };
    if (typeof m.dispose === 'function') await m.dispose();
}
