// RpcChannel validated end-to-end against real
// node:worker_threads MessageChannels. NO MOCKS.

import assert from 'node:assert/strict';
import test from 'node:test';
import { MessageChannel } from 'node:worker_threads';

import {
    CorrelatorDisposedError,
    RpcChannel,
    messagePortTransport,
} from '../dist/index.js';

/** Server: replies with `{type:'response', id, payload}` for every request,
 *  optionally emits a `{type:'note', text}` notification before responding. */
function startEchoServer(port, opts = {}) {
    port.on('message', (msg) => {
        if (!msg || typeof msg !== 'object') return;
        if (msg.type === 'request') {
            if (opts.notifyEach) {
                port.postMessage({ type: 'note', text: `pre:${msg.payload}` });
            }
            port.postMessage({ type: 'response', id: msg.id, payload: `echo:${msg.payload}` });
        } else if (msg.type === 'fire') {
            port.postMessage({ type: 'note', text: `fired:${msg.payload}` });
        }
    });
}

/** Build a MessageChannel pair. */
function pair() {
    const ch = new MessageChannel();
    return ch;
}

const RESPONSE_ID = (m) => (m && m.type === 'response' ? m.id : undefined);

test('request returns the matching response payload', async () => {
    const ch = pair();
    startEchoServer(ch.port2);
    const rpc = new RpcChannel(messagePortTransport(ch.port1), { responseId: RESPONSE_ID });
    const reply = await rpc.request((id) => ({ type: 'request', id, payload: 'a' }));
    assert.equal(reply.payload, 'echo:a');
    assert.equal(reply.type, 'response');
    await rpc.dispose();
    ch.port2.close();
});

test('5 concurrent requests all resolve with their matching response', async () => {
    const ch = pair();
    startEchoServer(ch.port2);
    const rpc = new RpcChannel(messagePortTransport(ch.port1), { responseId: RESPONSE_ID });
    const replies = await Promise.all(
        ['a', 'b', 'c', 'd', 'e'].map((p) =>
            rpc.request((id) => ({ type: 'request', id, payload: p })),
        ),
    );
    assert.deepEqual(
        replies.map((r) => r.payload),
        ['echo:a', 'echo:b', 'echo:c', 'echo:d', 'echo:e'],
    );
    await rpc.dispose();
    ch.port2.close();
});

test('notifications (no response id) route to onNotification, not the correlator', async () => {
    const ch = pair();
    startEchoServer(ch.port2, { notifyEach: true });
    const notes = [];
    const rpc = new RpcChannel(messagePortTransport(ch.port1), {
        responseId: RESPONSE_ID,
        onNotification: (m) => notes.push(m),
    });
    const reply = await rpc.request((id) => ({ type: 'request', id, payload: 'x' }));
    assert.equal(reply.payload, 'echo:x');
    assert.equal(notes.length, 1);
    assert.equal(notes[0].text, 'pre:x');
    await rpc.dispose();
    ch.port2.close();
});

test('inFlightCount tracks pending requests', async () => {
    const ch = pair();
    ch.port2.on('message', () => { /* never reply */ });
    const rpc = new RpcChannel(messagePortTransport(ch.port1), { responseId: RESPONSE_ID });
    assert.equal(rpc.inFlightCount, 0);
    const p1 = rpc.request((id) => ({ type: 'request', id, payload: 'p1' }));
    const p2 = rpc.request((id) => ({ type: 'request', id, payload: 'p2' }));
    assert.equal(rpc.inFlightCount, 2);
    const settled = Promise.all([p1.catch((e) => e), p2.catch((e) => e)]);
    await rpc.dispose();
    const [e1, e2] = await settled;
    assert.ok(e1 instanceof CorrelatorDisposedError);
    assert.ok(e2 instanceof CorrelatorDisposedError);
    assert.equal(rpc.inFlightCount, 0);
    ch.port2.close();
});

test('dispose rejects in-flight request and marks isDisposed', async () => {
    const ch = pair();
    ch.port2.on('message', () => { /* never reply */ });
    const rpc = new RpcChannel(messagePortTransport(ch.port1), { responseId: RESPONSE_ID });
    const p = rpc.request((id) => ({ type: 'request', id, payload: 'hang' }));
    const settled = p.catch((e) => e);
    await rpc.dispose();
    const err = await settled;
    assert.ok(err instanceof CorrelatorDisposedError);
    assert.equal(rpc.isDisposed, true);
    ch.port2.close();
});

test('request after dispose rejects with CorrelatorDisposedError', async () => {
    const ch = pair();
    startEchoServer(ch.port2);
    const rpc = new RpcChannel(messagePortTransport(ch.port1), { responseId: RESPONSE_ID });
    await rpc.dispose();
    await assert.rejects(
        () => rpc.request((id) => ({ type: 'request', id, payload: 'x' })),
        CorrelatorDisposedError,
    );
    ch.port2.close();
});

test('notify after dispose throws CorrelatorDisposedError', async () => {
    const ch = pair();
    startEchoServer(ch.port2);
    const rpc = new RpcChannel(messagePortTransport(ch.port1), { responseId: RESPONSE_ID });
    await rpc.dispose();
    assert.throws(
        () => rpc.notify({ type: 'fire', payload: 'x' }),
        CorrelatorDisposedError,
    );
    ch.port2.close();
});

test('notify is fire-and-forget (no correlation, no Promise)', async () => {
    const ch = pair();
    const notes = [];
    startEchoServer(ch.port2);
    const rpc = new RpcChannel(messagePortTransport(ch.port1), {
        responseId: RESPONSE_ID,
        onNotification: (m) => notes.push(m),
    });
    rpc.notify({ type: 'fire', payload: 'noresp' });
    // Wait long enough for the message-port round-trip to settle on macOS.
    await new Promise((r) => setTimeout(r, 20));
    assert.equal(notes.length, 1);
    assert.equal(notes[0].text, 'fired:noresp');
    assert.equal(rpc.inFlightCount, 0);
    await rpc.dispose();
    ch.port2.removeAllListeners('message');
    ch.port2.close();
});

test('reportTransportError routes through the supplied handler', async () => {
    const ch = pair();
    startEchoServer(ch.port2);
    const errs = [];
    const rpc = new RpcChannel(messagePortTransport(ch.port1), {
        responseId: RESPONSE_ID,
        onTransportError: (e) => errs.push(e),
    });
    const e = new Error('boom');
    rpc.reportTransportError(e);
    assert.equal(errs.length, 1);
    assert.equal(errs[0], e);
    await rpc.dispose();
    ch.port2.close();
});

test('dispose is idempotent', async () => {
    const ch = pair();
    startEchoServer(ch.port2);
    const rpc = new RpcChannel(messagePortTransport(ch.port1), { responseId: RESPONSE_ID });
    await rpc.dispose();
    await rpc.dispose();
    assert.equal(rpc.isDisposed, true);
    ch.port2.close();
});

test('startId option is honored for the underlying correlator', async () => {
    const ch = pair();
    let seenId = null;
    ch.port2.on('message', (msg) => {
        if (msg && msg.type === 'request') {
            seenId = msg.id;
            ch.port2.postMessage({ type: 'response', id: msg.id, payload: 'ok' });
        }
    });
    const rpc = new RpcChannel(messagePortTransport(ch.port1), {
        responseId: RESPONSE_ID,
        startId: 500,
    });
    await rpc.request((id) => ({ type: 'request', id, payload: 'p' }));
    assert.equal(seenId, 500);
    await rpc.dispose();
    ch.port2.close();
});
