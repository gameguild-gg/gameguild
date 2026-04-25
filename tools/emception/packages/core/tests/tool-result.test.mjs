import assert from 'node:assert/strict';
import { test } from 'node:test';
import { EmceptionError } from '../dist/errors.js';
import { assertToolResult, isToolResult } from '../dist/runtime/tool-result.js';

const ok = { exitCode: 0, stdout: '', stderr: '', durationMs: 0, timedOut: false };

test('assertToolResult accepts a minimal valid result', () => {
    const r = assertToolResult(ok);
    assert.equal(r.exitCode, 0);
});

test('assertToolResult accepts result with optional signal', () => {
    const r = assertToolResult({ ...ok, signal: 'SIGTERM', timedOut: true, exitCode: 124 });
    assert.equal(r.signal, 'SIGTERM');
});

test('assertToolResult rejects null', () => {
    assert.throws(() => assertToolResult(null), EmceptionError);
});

test('assertToolResult rejects non-object', () => {
    assert.throws(() => assertToolResult('hi'), EmceptionError);
});

test('assertToolResult rejects missing exitCode', () => {
    assert.throws(() => assertToolResult({ ...ok, exitCode: undefined }), /exitCode/);
});

test('assertToolResult rejects NaN durationMs', () => {
    assert.throws(() => assertToolResult({ ...ok, durationMs: NaN }), /durationMs/);
});

test('assertToolResult rejects negative durationMs', () => {
    assert.throws(() => assertToolResult({ ...ok, durationMs: -1 }), /durationMs/);
});

test('assertToolResult rejects non-string stdout', () => {
    assert.throws(() => assertToolResult({ ...ok, stdout: null }), /stdout/);
});

test('assertToolResult rejects non-boolean timedOut', () => {
    assert.throws(() => assertToolResult({ ...ok, timedOut: 'yes' }), /timedOut/);
});

test('assertToolResult rejects non-string signal when present', () => {
    assert.throws(() => assertToolResult({ ...ok, signal: 9 }), /signal/);
});

test('assertToolResult includes context label in message', () => {
    assert.throws(
        () => assertToolResult(null, 'clang'),
        /\(clang\)/,
    );
});

test('isToolResult is a non-throwing predicate', () => {
    assert.equal(isToolResult(ok), true);
    assert.equal(isToolResult({ ...ok, durationMs: -5 }), false);
    assert.equal(isToolResult(null), false);
});
