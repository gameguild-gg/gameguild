// Phase 7.1 / 7.2 — NodeRuntimeAdapter smoke tests.

import test from 'node:test';
import assert from 'node:assert/strict';
import os from 'node:os';
import path from 'node:path';
import fs from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { MessageChannel, MessagePort } from 'node:worker_threads';

import { createNodeRuntimeAdapter } from '../dist/index.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

test('adapter has stable identity + capability flags', () => {
    const adapter = createNodeRuntimeAdapter();
    assert.equal(adapter.name, 'node');
    assert.equal(adapter.hasSharedArrayBuffer(), typeof SharedArrayBuffer !== 'undefined');
});

test('transferable detects MessagePort, ArrayBuffer, and SAB', () => {
    const adapter = createNodeRuntimeAdapter();
    const { port1, port2 } = new MessageChannel();
    const ab = new ArrayBuffer(8);
    const sab = typeof SharedArrayBuffer !== 'undefined' ? new SharedArrayBuffer(8) : null;

    const t1 = adapter.transferable(port1);
    assert.equal(t1.length, 1);
    assert.ok(t1[0] instanceof MessagePort);

    const t2 = adapter.transferable({ buffer: ab, nested: { other: 1 } });
    assert.ok(t2.includes(ab));

    if (sab) {
        const t3 = adapter.transferable([sab, 42, 'noise']);
        assert.ok(t3.includes(sab));
    }

    // Closing ports avoids dangling handles.
    port1.close();
    port2.close();
});

test('transferable returns [] for plain values', () => {
    const adapter = createNodeRuntimeAdapter();
    assert.deepEqual(adapter.transferable(undefined), []);
    assert.deepEqual(adapter.transferable(null), []);
    assert.deepEqual(adapter.transferable('hello'), []);
    assert.deepEqual(adapter.transferable({ a: 1, b: [2, 3] }), []);
});

test('spawnWorker without workerEntry → RuntimeFeatureUnavailableError', async () => {
    const adapter = createNodeRuntimeAdapter();
    await assert.rejects(
        () => adapter.spawnWorker(),
        (err) => err instanceof Error && /workerEntry/.test(err.message),
    );
});

test('spawnWorker round-trip: messages flow + terminate cleans up', async () => {
    // Inline worker entry: echoes any inbound message back, prefixed with 'echo:'.
    const tmpDir = await fs.mkdtemp(path.join(os.tmpdir(), 'em-node-adapter-'));
    const entryPath = path.join(tmpDir, 'echo-worker.mjs');
    await fs.writeFile(
        entryPath,
        "import { parentPort } from 'node:worker_threads';\n" +
        "parentPort.on('message', (msg) => parentPort.postMessage('echo:' + String(msg)));\n",
    );

    const adapter = createNodeRuntimeAdapter({ workerEntry: entryPath });
    const handle = await adapter.spawnWorker({ name: 'test-echo' });
    try {
        const got = await new Promise((resolve) => {
            handle.addEventListener('message', (ev) => resolve(ev.data));
            handle.postMessage('hi');
        });
        assert.equal(got, 'echo:hi');
    } finally {
        await handle.terminate();
        await fs.rm(tmpDir, { recursive: true, force: true });
    }
});

test('spawnWorker: removeEventListener actually unsubscribes', async () => {
    const tmpDir = await fs.mkdtemp(path.join(os.tmpdir(), 'em-node-adapter-'));
    const entryPath = path.join(tmpDir, 'tick-worker.mjs');
    await fs.writeFile(
        entryPath,
        "import { parentPort } from 'node:worker_threads';\n" +
        "parentPort.on('message', () => { parentPort.postMessage('a'); parentPort.postMessage('b'); });\n",
    );

    const adapter = createNodeRuntimeAdapter({ workerEntry: entryPath });
    const handle = await adapter.spawnWorker();
    try {
        let count = 0;
        const listener = () => { count += 1; };
        handle.addEventListener('message', listener);
        // Wait one tick after posting so both messages dispatch.
        await new Promise((r) => {
            handle.addEventListener('message', function settle(ev) {
                if (ev.data === 'b') {
                    handle.removeEventListener('message', settle);
                    r(undefined);
                }
            });
            handle.postMessage('go');
        });
        assert.equal(count, 2, 'listener should see both echoed messages');

        handle.removeEventListener('message', listener);
        await new Promise((r) => {
            handle.addEventListener('message', function settle2(ev) {
                if (ev.data === 'b') {
                    handle.removeEventListener('message', settle2);
                    r(undefined);
                }
            });
            handle.postMessage('go-again');
        });
        assert.equal(count, 2, 'listener should not fire again after removal');
    } finally {
        await handle.terminate();
        await fs.rm(tmpDir, { recursive: true, force: true });
    }
});

test('openWorkspaceStore: kind=fs creates a manager + handle', async () => {
    const root = await fs.mkdtemp(path.join(os.tmpdir(), 'em-node-store-'));
    try {
        const adapter = createNodeRuntimeAdapter({ fsRoot: root });
        const store = await adapter.openWorkspaceStore({ name: 'demo' });
        try {
            assert.equal(store.name, 'demo');
            assert.equal(store.kind, 'fs');
            const resource = store.resource;
            assert.ok(resource && typeof resource === 'object');
            assert.ok(resource.manager, 'resource.manager exists');
            assert.ok(resource.handle, 'resource.handle exists (workspace pre-opened)');
            assert.equal(resource.root, root);
            // The handle should expose the standard WorkspaceHandle surface.
            await resource.handle.writeFile('hello.txt', 'world');
            const bytes = await resource.handle.readFile('hello.txt');
            assert.equal(new TextDecoder().decode(bytes), 'world');
        } finally {
            await store.close();
        }
    } finally {
        await fs.rm(root, { recursive: true, force: true });
    }
});

test('openWorkspaceStore: rejects unsupported kinds', async () => {
    const adapter = createNodeRuntimeAdapter();
    await assert.rejects(
        () => adapter.openWorkspaceStore({ name: 'x', kind: 'idb' }),
        (err) => err instanceof Error && /not supported/.test(err.message),
    );
});

test('loadManifest delegates to the Phase 7.6 loader (path)', async () => {
    const tmpDir = await fs.mkdtemp(path.join(os.tmpdir(), 'em-node-manifest-'));
    const manifestPath = path.join(tmpDir, 'manifest.json');
    const manifest = { bundles: { foo: { entries: [] } } };
    await fs.writeFile(manifestPath, JSON.stringify(manifest));
    try {
        const adapter = createNodeRuntimeAdapter();
        const loaded = await adapter.loadManifest({ path: manifestPath });
        assert.deepEqual(loaded, manifest);
    } finally {
        await fs.rm(tmpDir, { recursive: true, force: true });
    }
});
