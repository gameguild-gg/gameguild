// Test-engine coverage — stdio handler, stdio-file handler,
// custom handler, and the plan-level `runTests` orchestrator (abort
// signal, redactHidden, sequencing, exception wrapping). The clang-query
// and doctest handlers are covered separately in engine-handlers.test.mjs.

import assert from 'node:assert/strict';
import test from 'node:test';

import { runTests } from '../dist/index.js';

/** Minimal EmceptionAPI stub mirroring the engine-handlers test helper. */
function makeApi({ compileImpl, runImpl, listFilesImpl, readFileImpl } = {}) {
    return {
        workspace: {
            list: async () => [],
            switch: async () => { },
            reset: async () => { },
            readFile: readFileImpl ?? (async () => null),
            writeFile: async () => { },
            listFiles: listFilesImpl ?? (async () => []),
            setVisibility: async () => { },
            getBuild: async () => ({}),
            setBuild: async () => { },
            exportZip: async () => new Blob([]),
            importZip: async () => { },
        },
        run: runImpl ?? (async () => {
            throw new Error('run() not stubbed');
        }),
        compileAndRun: compileImpl ?? (async () => {
            throw new Error('compileAndRun() not stubbed');
        }),
        runTests: async () => { throw new Error('not used'); },
        on: () => () => { },
        dispose: () => { },
    };
}

// ---------------------------------------------------------------------------
// stdio handler
// ---------------------------------------------------------------------------

test('stdio: matching stdout → passes; forwards build + stdin', async () => {
    let captured;
    const api = makeApi({
        compileImpl: async (_src, opts) => {
            captured = opts;
            return { exitCode: 0, stdout: 'hi\n', stderr: '', durationMs: 2, timedOut: false };
        },
    });
    const report = await runTests(api, {
        build: { std: 'c++20', sources: ['main.cpp'] },
        cases: [{ kind: 'stdio', stdin: 'in\n', expectedStdout: 'hi\n' }],
    });
    assert.equal(report.passed, 1);
    assert.equal(report.failed, 0);
    assert.equal(captured.stdin, 'in\n');
    assert.equal(captured.stdout, 'capture');
    assert.equal(captured.stderr, 'capture');
    assert.deepEqual(captured.build, { std: 'c++20', sources: ['main.cpp'] });
});

test('stdio: regex stdout match', async () => {
    const api = makeApi({
        compileImpl: async () => ({ exitCode: 0, stdout: 'answer = 42\n', stderr: '', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        cases: [{ kind: 'stdio', expectedStdout: /answer = \d+/ }],
    });
    assert.equal(report.passed, 1);
});

test('stdio: stdout mismatch → fails with diff diagnostic', async () => {
    const api = makeApi({
        compileImpl: async () => ({ exitCode: 0, stdout: 'nope\n', stderr: '', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        cases: [{ kind: 'stdio', expectedStdout: 'hi\n', name: 'greet' }],
    });
    assert.equal(report.failed, 1);
    assert.equal(report.cases[0].name, 'greet');
    assert.match(report.cases[0].diagnostic, /stdout mismatch/);
    assert.match(report.cases[0].diagnostic, /"hi\\n"/);
    assert.match(report.cases[0].diagnostic, /"nope\\n"/);
});

test('stdio: expectedExit + expectedStderr both checked', async () => {
    const api = makeApi({
        compileImpl: async () => ({ exitCode: 1, stdout: '', stderr: 'oops\n', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        cases: [{
            kind: 'stdio',
            expectedStdout: '',
            expectedStderr: /oops/,
            expectedExit: 1,
        }],
    });
    assert.equal(report.passed, 1);
});

test('stdio: timed-out result → fails even when stdout matches', async () => {
    const api = makeApi({
        compileImpl: async () => ({ exitCode: 0, stdout: '', stderr: '', durationMs: 1000, timedOut: true }),
    });
    const report = await runTests(api, {
        timeoutMsPerCase: 50,
        cases: [{ kind: 'stdio', expectedStdout: '' }],
    });
    assert.equal(report.failed, 1);
    assert.match(report.cases[0].diagnostic, /Timed out/);
});

// ---------------------------------------------------------------------------
// stdio-file handler
// ---------------------------------------------------------------------------

test('stdio-file: reads fixtures from workspace + uses content as stdin', async () => {
    const enc = new TextEncoder();
    let stdinSeen;
    const api = makeApi({
        readFileImpl: async (path) => {
            if (path === 'in/01.txt') return enc.encode('5 7\n');
            if (path === 'out/01.txt') return enc.encode('12\n');
            return null;
        },
        compileImpl: async (_src, opts) => {
            stdinSeen = opts.stdin;
            return { exitCode: 0, stdout: '12\n', stderr: '', durationMs: 1, timedOut: false };
        },
    });
    const report = await runTests(api, {
        cases: [{ kind: 'stdio-file', inFile: 'in/01.txt', expectedOutFile: 'out/01.txt' }],
    });
    assert.equal(report.passed, 1);
    assert.equal(stdinSeen, '5 7\n');
});

test('stdio-file: missing input fixture → fails fast', async () => {
    const api = makeApi({
        readFileImpl: async () => null,
    });
    const report = await runTests(api, {
        cases: [{ kind: 'stdio-file', inFile: 'in/missing.txt', expectedOutFile: 'out/01.txt' }],
    });
    assert.equal(report.failed, 1);
    assert.match(report.cases[0].diagnostic, /Input fixture not found.*missing\.txt/);
});

// ---------------------------------------------------------------------------
// custom handler
// ---------------------------------------------------------------------------

test('custom: receives api + result name defaults to "custom"', async () => {
    let calledWith;
    const api = makeApi();
    const report = await runTests(api, {
        cases: [{
            kind: 'custom',
            run: async (em) => {
                calledWith = em;
                return { name: undefined, passed: true, durationMs: 3 };
            },
        }],
    });
    assert.equal(calledWith, api);
    assert.equal(report.passed, 1);
    assert.equal(report.cases[0].name, 'custom');
});

test('custom: handler exception → wrapped as failed case (engine never throws)', async () => {
    const api = makeApi();
    const report = await runTests(api, {
        cases: [{
            kind: 'custom',
            name: 'boom',
            run: async () => { throw new Error('handler exploded'); },
        }],
    });
    assert.equal(report.failed, 1);
    assert.equal(report.cases[0].name, 'boom');
    assert.match(report.cases[0].diagnostic, /handler exploded/);
});

// ---------------------------------------------------------------------------
// runTests orchestrator: abort, redactor, sequencing
// ---------------------------------------------------------------------------

test('runTests: AbortSignal between cases short-circuits remaining as failures', async () => {
    let calls = 0;
    const ac = new AbortController();
    const api = makeApi({
        compileImpl: async () => {
            calls += 1;
            if (calls === 1) ac.abort();
            return { exitCode: 0, stdout: '', stderr: '', durationMs: 1, timedOut: false };
        },
    });
    const report = await runTests(api, {
        cases: [
            { kind: 'stdio', expectedStdout: '', name: 'first' },
            { kind: 'stdio', expectedStdout: '', name: 'second' },
            { kind: 'stdio', expectedStdout: '', name: 'third' },
        ],
    }, { signal: ac.signal });
    assert.equal(calls, 1, 'only first case actually executes compileAndRun');
    assert.equal(report.cases.length, 3);
    assert.equal(report.cases[0].passed, true);
    assert.equal(report.cases[1].passed, false);
    assert.equal(report.cases[2].passed, false);
    assert.match(report.cases[1].diagnostic, /Aborted/);
});

test('runTests: redactHidden masks hidden + solution paths in diagnostics', async () => {
    const api = makeApi({
        listFilesImpl: async () => [
            { path: 'main.cpp', visibility: 'public' },
            { path: 'tests/grader.cpp', visibility: 'hidden' },
            { path: 'solution.cpp', visibility: 'solution' },
        ],
        compileImpl: async () => ({
            exitCode: 1,
            stdout: '',
            stderr: 'failure in tests/grader.cpp and solution.cpp',
            // The diagnostic text built by stdio handler embeds stderr
            // via JSON.stringify, so we need stdout mismatch to surface
            // a diagnostic mentioning the sensitive paths.
            durationMs: 1,
            timedOut: false,
        }),
    });
    const report = await runTests(api, {
        redactHidden: true,
        cases: [{
            kind: 'stdio',
            expectedStdout: 'expected',
            expectedStderr: 'whatever',
            name: 'redact-me',
        }],
    });
    assert.equal(report.failed, 1);
    const diag = report.cases[0].diagnostic;
    assert.doesNotMatch(diag, /tests\/grader\.cpp/);
    assert.doesNotMatch(diag, /solution\.cpp/);
    assert.match(diag, /<hidden>/);
});

test('runTests: aggregates totalDurationMs and per-case pass/fail counts', async () => {
    const api = makeApi({
        compileImpl: async () => ({ exitCode: 0, stdout: 'x', stderr: '', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        cases: [
            { kind: 'stdio', expectedStdout: 'x' },
            { kind: 'stdio', expectedStdout: 'y' },
            { kind: 'stdio', expectedStdout: 'x' },
        ],
    });
    assert.equal(report.passed, 2);
    assert.equal(report.failed, 1);
    assert.equal(report.cases.length, 3);
    assert.ok(report.totalDurationMs >= 0);
});
