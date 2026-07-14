// RequestCorrelator validated end-to-end against real
// node:worker_threads MessageChannels. NO MOCKS: every test wires two real
// MessagePorts so the correlator drives an actual cross-port hop, exactly the
// shape the eventual worker-orchestrator extraction will use.
//
// Run via:
//   node --test tools/emception/packages/core/tests/request-correlator.test.mjs

import assert from 'node:assert/strict';
import test from 'node:test';
import { MessageChannel } from 'node:worker_threads';

import {
    CorrelatorDisposedError,
    RequestCorrelator,
} from '../dist/index.js';

/** Helper: run an "echo server" on `port2` that replies to every {id, payload}. */
function startEchoServer(port) {
    port.on('message', (msg) => {
        if (msg && typeof msg.id === 'number') {
            // Real cross-thread (-ish) hop via MessageChannel.
            port.postMessage({ id: msg.id, payload: `echo:${msg.payload}` });
        }
    });
}

test('allocate yields monotonically increasing ids starting at 1', () => {
    const c = new RequestCorrelator();
    const a = c.allocate();
    const b = c.allocate();
    const d = c.allocate();
    assert.equal(a.id, 1);
    assert.equal(b.id, 2);
    assert.equal(d.id, 3);
    c.dispose();
    // Drain the rejected promises so node:test does not flag unhandled rejections.
    return Promise.allSettled([a.promise, b.promise, d.promise]);
});

test('startId option respected', () => {
    const c = new RequestCorrelator({ startId: 100 });
    const a = c.allocate();
    assert.equal(a.id, 100);
    c.dispose();
    return a.promise.catch(() => { });
});

test('complete resolves the matching promise with the value', async () => {
    const c = new RequestCorrelator();
    const { id, promise } = c.allocate();
    const ok = c.complete(id, { hello: 'world' });
    assert.equal(ok, true);
    assert.deepEqual(await promise, { hello: 'world' });
});

test('complete returns false for unknown id (stale response)', () => {
    const c = new RequestCorrelator();
    assert.equal(c.complete(9999, 'whatever'), false);
});

test('fail rejects the matching promise with the given reason', async () => {
    const c = new RequestCorrelator();
    const { id, promise } = c.allocate();
    const reason = new Error('boom');
    const ok = c.fail(id, reason);
    assert.equal(ok, true);
    await assert.rejects(promise, /boom/);
});

test('pendingCount reflects live entries', () => {
    const c = new RequestCorrelator();
    assert.equal(c.pendingCount, 0);
    const a = c.allocate();
    const b = c.allocate();
    assert.equal(c.pendingCount, 2);
    c.complete(a.id, 'x');
    assert.equal(c.pendingCount, 1);
    c.fail(b.id, new Error('x'));
    assert.equal(c.pendingCount, 0);
    return b.promise.catch(() => { });
});

test('dispose rejects every outstanding request with CorrelatorDisposedError', async () => {
    const c = new RequestCorrelator();
    const { promise: p1 } = c.allocate();
    const { promise: p2 } = c.allocate();
    c.dispose();
    await assert.rejects(p1, CorrelatorDisposedError);
    await assert.rejects(p2, CorrelatorDisposedError);
    assert.equal(c.isDisposed, true);
});

test('dispose with custom reason propagates that reason', async () => {
    const c = new RequestCorrelator();
    const { promise } = c.allocate();
    const custom = new Error('shutdown');
    c.dispose(custom);
    await assert.rejects(promise, /shutdown/);
});

test('allocate after dispose throws CorrelatorDisposedError', () => {
    const c = new RequestCorrelator();
    c.dispose();
    assert.throws(() => c.allocate(), CorrelatorDisposedError);
});

test('complete and fail after dispose return false', async () => {
    const c = new RequestCorrelator();
    const { id, promise } = c.allocate();
    // Drain the rejection eagerly.
    promise.catch(() => { });
    c.dispose();
    assert.equal(c.complete(id, 'x'), false);
    assert.equal(c.fail(id, new Error('x')), false);
});

test('dispose is idempotent', () => {
    const c = new RequestCorrelator();
    c.dispose();
    c.dispose();
    assert.equal(c.isDisposed, true);
});

test('end-to-end with real MessageChannel: 5 concurrent echoes complete in order of resolution', async () => {
    const channel = new MessageChannel();
    const c = new RequestCorrelator();

    startEchoServer(channel.port2);
    channel.port1.on('message', (msg) => {
        c.complete(msg.id, msg.payload);
    });
    const sends = ['a', 'b', 'c', 'd', 'e'].map((payload) => {
        const { id, promise } = c.allocate();
        channel.port1.postMessage({ id, payload });
        return promise;
    });

    const results = await Promise.all(sends);
    assert.deepEqual(results, ['echo:a', 'echo:b', 'echo:c', 'echo:d', 'echo:e']);
    channel.port1.close();
    channel.port2.close();
});

test('end-to-end with real MessageChannel: stale response after complete is dropped silently', async () => {
    const channel = new MessageChannel();
    const c = new RequestCorrelator();
    let staleHandled = null;

    channel.port2.on('message', (msg) => {
        // Server sends two responses for the same id (simulates a misbehaving worker).
        channel.port2.postMessage({ id: msg.id, payload: 'first' });
        channel.port2.postMessage({ id: msg.id, payload: 'second' });
    });
    channel.port1.on('message', (msg) => {
        const ok = c.complete(msg.id, msg.payload);
        if (!ok) staleHandled = msg.payload;
    });
    const { id, promise } = c.allocate();
    channel.port1.postMessage({ id, ping: true });
    const result = await promise;
    assert.equal(result, 'first');

    // Wait one macrotask so the stale message lands.
    await new Promise((r) => setImmediate(r));
    assert.equal(staleHandled, 'second');

    channel.port1.close();
    channel.port2.close();
});

test('end-to-end with real MessageChannel: dispose mid-flight rejects in-flight request', async () => {
    const channel = new MessageChannel();
    const c = new RequestCorrelator();
    // Server NEVER responds, simulating a hung worker.
    channel.port2.on('message', () => { });
    channel.port1.on('message', (msg) => c.complete(msg.id, msg.payload));
    const { id, promise } = c.allocate();
    channel.port1.postMessage({ id, payload: 'hang' });

    // Dispose after letting the message fly.
    await new Promise((r) => setImmediate(r));
    c.dispose();

    await assert.rejects(promise, CorrelatorDisposedError);
    channel.port1.close();
    channel.port2.close();
});
