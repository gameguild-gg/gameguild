// BootHandshake state machine. Pure logic, no transport.
//
// Run via:
//   node --test tools/emception/packages/core/tests/boot-handshake.test.mjs

import assert from 'node:assert/strict';
import test from 'node:test';

import {
    BootCancelledError,
    BootError,
    BootHandshake,
} from '../dist/index.js';

test('initial state is idle', () => {
    const bh = new BootHandshake();
    assert.equal(bh.currentState, 'idle');
    assert.equal(bh.isBooted, false);
    assert.equal(bh.isSettled, false);
});

test('start transitions to booting and returns a Promise', () => {
    const bh = new BootHandshake();
    const p = bh.start();
    assert.equal(bh.currentState, 'booting');
    assert.ok(p instanceof Promise);
    bh.cancel();
    return p.catch(() => { });
});

test('succeed resolves the boot promise and reaches booted state', async () => {
    const bh = new BootHandshake();
    const p = bh.start();
    bh.succeed();
    await p;
    assert.equal(bh.currentState, 'booted');
    assert.equal(bh.isBooted, true);
    assert.equal(bh.isSettled, true);
});

test('fail with string wraps in BootError and rejects', async () => {
    const bh = new BootHandshake();
    const p = bh.start();
    bh.fail('worker exploded');
    await assert.rejects(p, BootError);
    assert.equal(bh.currentState, 'failed');
    assert.equal(bh.isSettled, true);
    assert.equal(bh.isBooted, false);
});

test('fail with Error preserves the original error instance', async () => {
    const bh = new BootHandshake();
    const p = bh.start();
    const e = new Error('custom');
    bh.fail(e);
    await assert.rejects(p, /custom/);
    try {
        await p;
    } catch (caught) {
        assert.equal(caught, e);
    }
});

test('cancel rejects with BootCancelledError by default', async () => {
    const bh = new BootHandshake();
    const p = bh.start();
    bh.cancel();
    await assert.rejects(p, BootCancelledError);
    assert.equal(bh.currentState, 'cancelled');
});

test('cancel with custom reason propagates that reason', async () => {
    const bh = new BootHandshake();
    const p = bh.start();
    const reason = new Error('parent disposed');
    bh.cancel(reason);
    await assert.rejects(p, /parent disposed/);
});

test('start twice without resolution throws BootError', () => {
    const bh = new BootHandshake();
    const p = bh.start();
    assert.throws(() => bh.start(), BootError);
    bh.cancel();
    return p.catch(() => { });
});

test('start after success throws BootError (single-shot)', async () => {
    const bh = new BootHandshake();
    const p = bh.start();
    bh.succeed();
    await p;
    assert.throws(() => bh.start(), BootError);
});

test('succeed/fail/cancel are no-ops after the first terminal call', async () => {
    const bh = new BootHandshake();
    const p = bh.start();
    bh.succeed();
    bh.fail('late'); // no-op
    bh.cancel(); // no-op
    await p;
    assert.equal(bh.currentState, 'booted');
});

test('succeed before start is a no-op (idle stays idle)', () => {
    const bh = new BootHandshake();
    bh.succeed();
    assert.equal(bh.currentState, 'idle');
});

test('fail before start is a no-op', () => {
    const bh = new BootHandshake();
    bh.fail('whatever');
    assert.equal(bh.currentState, 'idle');
});

test('cancel before start is a no-op', () => {
    const bh = new BootHandshake();
    bh.cancel();
    assert.equal(bh.currentState, 'idle');
});
