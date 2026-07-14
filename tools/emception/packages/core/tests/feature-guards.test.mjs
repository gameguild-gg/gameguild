import assert from 'node:assert/strict';
import { test } from 'node:test';
import {
    CanvasUnavailableError,
    RuntimeFeatureUnavailableError,
} from '../dist/errors.js';
import {
    assertCanvasUnsupported,
    assertNoBrowserOnlyFeatures,
    assertXtermStdinUnsupported,
    looksLikeCanvas,
    looksLikeXtermTerminal,
} from '../dist/runtime/feature-guards.js';

test('looksLikeCanvas accepts an OffscreenCanvas-shaped object', () => {
    assert.equal(looksLikeCanvas({ width: 800, height: 600, getContext: () => null }), true);
});

test('looksLikeCanvas accepts an HTMLCanvasElement-shaped object', () => {
    assert.equal(
        looksLikeCanvas({ width: 800, height: 600, transferControlToOffscreen: () => ({}) }),
        true,
    );
});

test('looksLikeCanvas rejects plain objects without canvas methods', () => {
    assert.equal(looksLikeCanvas({ width: 1, height: 2 }), false);
    assert.equal(looksLikeCanvas({ width: '800', height: 600, getContext: () => null }), false);
    assert.equal(looksLikeCanvas(null), false);
    assert.equal(looksLikeCanvas('hi'), false);
});

test('looksLikeXtermTerminal accepts a Terminal-shaped object', () => {
    assert.equal(
        looksLikeXtermTerminal({ onData: () => undefined, write: () => undefined }),
        true,
    );
});

test('looksLikeXtermTerminal rejects shapes without both methods', () => {
    assert.equal(looksLikeXtermTerminal({ write: () => undefined }), false);
    assert.equal(looksLikeXtermTerminal({ onData: () => undefined }), false);
    assert.equal(looksLikeXtermTerminal(null), false);
});

test('assertCanvasUnsupported is a no-op when canvas is undefined / null', () => {
    assert.doesNotThrow(() => assertCanvasUnsupported(undefined, 'node'));
    assert.doesNotThrow(() => assertCanvasUnsupported(null, 'node'));
});

test('assertCanvasUnsupported throws CanvasUnavailableError for canvas objects', () => {
    assert.throws(
        () => assertCanvasUnsupported({ width: 800, height: 600, getContext: () => null }, 'node'),
        CanvasUnavailableError,
    );
});

test('assertCanvasUnsupported error mentions @emception/browser', () => {
    try {
        assertCanvasUnsupported(
            { width: 800, height: 600, getContext: () => null },
            'node',
        );
        assert.fail('expected throw');
    } catch (err) {
        assert.match(String(err.message), /@emception\/browser/);
        assert.match(String(err.message), /node/);
    }
});

test('assertCanvasUnsupported throws RuntimeFeatureUnavailableError for non-canvas objects', () => {
    assert.throws(() => assertCanvasUnsupported({}, 'node'), RuntimeFeatureUnavailableError);
});

test('assertXtermStdinUnsupported is a no-op for null / string / Uint8Array / function / "none"', () => {
    assert.doesNotThrow(() => assertXtermStdinUnsupported(undefined, 'node'));
    assert.doesNotThrow(() => assertXtermStdinUnsupported('hello', 'node'));
    assert.doesNotThrow(() => assertXtermStdinUnsupported(new Uint8Array(0), 'node'));
    assert.doesNotThrow(() => assertXtermStdinUnsupported(() => null, 'node'));
    assert.doesNotThrow(() => assertXtermStdinUnsupported('none', 'node'));
});

test('assertXtermStdinUnsupported allows async-iterable / ReadableStream-like values', () => {
    // We do NOT special-case these; they're accepted because they don't
    // structurally match xterm's `Terminal` interface.
    const asyncIter = { [Symbol.asyncIterator]: () => ({ next: async () => ({ done: true }) }) };
    assert.doesNotThrow(() => assertXtermStdinUnsupported(asyncIter, 'node'));
});

test('assertXtermStdinUnsupported throws on xterm-shaped stdin', () => {
    assert.throws(
        () =>
            assertXtermStdinUnsupported(
                { onData: () => undefined, write: () => undefined },
                'node',
            ),
        RuntimeFeatureUnavailableError,
    );
});

test('assertXtermStdinUnsupported error mentions @emception/xterm and @emception/browser', () => {
    try {
        assertXtermStdinUnsupported(
            { onData: () => undefined, write: () => undefined },
            'node',
        );
        assert.fail('expected throw');
    } catch (err) {
        assert.match(String(err.message), /@emception\/browser/);
        assert.match(String(err.message), /@emception\/xterm/);
    }
});

test('assertNoBrowserOnlyFeatures runs both guards', () => {
    assert.doesNotThrow(() => assertNoBrowserOnlyFeatures(undefined, 'node'));
    assert.doesNotThrow(() => assertNoBrowserOnlyFeatures({}, 'node'));
    assert.doesNotThrow(() => assertNoBrowserOnlyFeatures({ stdin: 'hi' }, 'node'));
    assert.throws(
        () => assertNoBrowserOnlyFeatures({ canvas: { width: 1, height: 1, getContext: () => null } }, 'node'),
        CanvasUnavailableError,
    );
    assert.throws(
        () => assertNoBrowserOnlyFeatures({ stdin: { onData: () => undefined, write: () => undefined } }, 'node'),
        RuntimeFeatureUnavailableError,
    );
});
