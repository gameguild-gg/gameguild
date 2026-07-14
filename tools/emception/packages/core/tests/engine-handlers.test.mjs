// Engine handler verification — clang-query and
// doctest, exercised against a fake EmceptionAPI. The runtime-agnostic
// matcher engine and console parser were unit-tested separately; this
// file proves the engine routes inputs/outputs correctly.

import assert from 'node:assert/strict';
import test from 'node:test';

import { runTests } from '../dist/index.js';

/** Minimal AST-dump JSON: a TU with one C++ class named "LinkedList". */
const SAMPLE_AST = JSON.stringify({
    kind: 'TranslationUnitDecl',
    inner: [
        {
            kind: 'CXXRecordDecl',
            name: 'LinkedList',
            inner: [
                { kind: 'CXXMethodDecl', name: 'push' },
                { kind: 'CXXMethodDecl', name: 'pop' },
            ],
        },
    ],
});

const SAMPLE_DOCTEST_PASS = [
    '===============================================================================',
    '[doctest] test cases:      2 |      2 passed |      0 failed | 0 skipped',
    '[doctest] assertions:      4 |      4 passed |      0 failed |',
    '[doctest] Status: SUCCESS!',
    '',
].join('\n');

const SAMPLE_DOCTEST_FAIL = [
    'src/list_test.cpp:12:',
    'TEST CASE:  push appends',
    '',
    'src/list_test.cpp:15: ERROR: CHECK( list.size() == 1 ) is NOT correct!',
    '  values: CHECK( 0 == 1 )',
    '',
    '===============================================================================',
    '[doctest] test cases:      2 |      1 passed |      1 failed | 0 skipped',
    '[doctest] assertions:      4 |      3 passed |      1 failed |',
    '[doctest] Status: FAILURE!',
    '',
].join('\n');

/** Build a minimum EmceptionAPI stub. */
function makeApi({
    runImpl,
    compileImpl,
} = {}) {
    return {
        workspace: {
            list: async () => [],
            switch: async () => { },
            reset: async () => { },
            readFile: async () => null,
            writeFile: async () => { },
            listFiles: async () => [],
            setVisibility: async () => { },
            getBuild: async () => ({}),
            setBuild: async () => { },
            exportZip: async () => new Blob([]),
            importZip: async () => { },
        },
        run: runImpl ?? (async () => {
            throw new Error('run() not stubbed for this test');
        }),
        compileAndRun: compileImpl ?? (async () => {
            throw new Error('compileAndRun() not stubbed for this test');
        }),
        runTests: async () => { throw new Error('not used'); },
        on: () => () => { },
        dispose: () => { },
    };
}

test('clang-query: matcher hits → passes when expect=found', async () => {
    let capturedCmd, capturedArgv;
    const api = makeApi({
        runImpl: async (cmd, argv) => {
            capturedCmd = cmd;
            capturedArgv = argv;
            return { exitCode: 0, stdout: SAMPLE_AST, stderr: '', durationMs: 1, timedOut: false };
        },
    });
    const report = await runTests(api, {
        build: { sources: ['list.cpp'], std: 'c++20', includePaths: ['inc'], defines: { B: '2', A: true } },
        cases: [{ kind: 'clang-query', matcher: 'cxxRecordDecl(hasName("LinkedList"))', expect: 'found' }],
    });
    assert.equal(capturedCmd, 'clang');
    assert.deepEqual(capturedArgv.slice(0, 3), ['-Xclang', '-ast-dump=json', '-fsyntax-only']);
    assert.ok(capturedArgv.includes('-std=c++20'));
    assert.ok(capturedArgv.includes('-Iinc'));
    // Defines are sorted alphabetically.
    const dA = capturedArgv.indexOf('-DA');
    const dB = capturedArgv.indexOf('-DB=2');
    assert.ok(dA >= 0 && dB > dA, 'defines emitted in sorted order');
    assert.ok(capturedArgv.includes('list.cpp'));
    assert.equal(report.passed, 1);
    assert.equal(report.failed, 0);
});

test('clang-query: no hits + expect=found → fails with descriptive diagnostic', async () => {
    const api = makeApi({
        runImpl: async () => ({ exitCode: 0, stdout: SAMPLE_AST, stderr: '', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        build: { sources: ['list.cpp'] },
        cases: [{ kind: 'clang-query', matcher: 'recordDecl(hasName("Missing"))', expect: 'found' }],
    });
    assert.equal(report.failed, 1);
    assert.match(report.cases[0].diagnostic, /expected at least one match, found 0/);
});

test('clang-query: minCount expectation', async () => {
    const api = makeApi({
        runImpl: async () => ({ exitCode: 0, stdout: SAMPLE_AST, stderr: '', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        build: { sources: ['list.cpp'] },
        cases: [
            { kind: 'clang-query', name: 'two methods', matcher: 'cxxMethodDecl()', expect: { minCount: 2 } },
            { kind: 'clang-query', name: 'three methods', matcher: 'cxxMethodDecl()', expect: { minCount: 3 } },
        ],
    });
    assert.equal(report.passed, 1);
    assert.equal(report.failed, 1);
    assert.equal(report.cases[0].name, 'two methods');
    assert.equal(report.cases[1].name, 'three methods');
    assert.match(report.cases[1].diagnostic, /expected at least 3 match\(es\), found 2/);
});

test('clang-query: empty sources → fails fast with helpful message', async () => {
    const api = makeApi({ runImpl: async () => { throw new Error('should not be called'); } });
    const report = await runTests(api, {
        build: { sources: [] },
        cases: [{ kind: 'clang-query', matcher: 'recordDecl()', expect: 'found' }],
    });
    assert.equal(report.failed, 1);
    assert.match(report.cases[0].diagnostic, /plan\.build\.sources is empty/);
});

test('clang-query: ast-dump non-zero exit surfaces stderr', async () => {
    const api = makeApi({
        runImpl: async () => ({ exitCode: 1, stdout: '', stderr: 'fatal: oops', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        build: { sources: ['list.cpp'] },
        cases: [{ kind: 'clang-query', matcher: 'recordDecl()', expect: 'found' }],
    });
    assert.equal(report.failed, 1);
    assert.match(report.cases[0].diagnostic, /ast-dump failed \(exit 1\)/);
    assert.match(report.cases[0].diagnostic, /fatal: oops/);
});

test('clang-query: malformed AST JSON surfaces parse error', async () => {
    const api = makeApi({
        runImpl: async () => ({ exitCode: 0, stdout: 'not json {', stderr: '', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        build: { sources: ['list.cpp'] },
        cases: [{ kind: 'clang-query', matcher: 'recordDecl()', expect: 'found' }],
    });
    assert.equal(report.failed, 1);
    assert.match(report.cases[0].diagnostic, /JSON parse failed/);
});

test('doctest: SUCCESS summary → passes', async () => {
    const api = makeApi({
        compileImpl: async () => ({ exitCode: 0, stdout: SAMPLE_DOCTEST_PASS, stderr: '', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        build: { sources: ['main.cpp'] },
        cases: [{ kind: 'doctest', sourceFiles: ['list_test.cpp'] }],
    });
    assert.equal(report.passed, 1);
    assert.equal(report.failed, 0);
});

test('doctest: FAILURE summary → fails with extracted assertion details', async () => {
    const api = makeApi({
        compileImpl: async () => ({ exitCode: 1, stdout: SAMPLE_DOCTEST_FAIL, stderr: '', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        build: { sources: ['main.cpp'] },
        cases: [{ kind: 'doctest', sourceFiles: ['list_test.cpp'] }],
    });
    assert.equal(report.failed, 1);
    const d = report.cases[0].diagnostic;
    assert.match(d, /1\/2 test cases failed/);
    assert.match(d, /1\/4 assertions failed/);
    assert.match(d, /push appends/);
    assert.match(d, /list\.size\(\) == 1/);
});

test('doctest: missing summary → reported as crash, not failure', async () => {
    const api = makeApi({
        compileImpl: async () => ({ exitCode: 139, stdout: 'segfault somewhere', stderr: 'ABORT', durationMs: 1, timedOut: false }),
    });
    const report = await runTests(api, {
        build: { sources: ['main.cpp'] },
        cases: [{ kind: 'doctest', sourceFiles: ['list_test.cpp'] }],
    });
    assert.equal(report.failed, 1);
    assert.match(report.cases[0].diagnostic, /crashed before printing summary \(exit 139\)/);
    assert.match(report.cases[0].diagnostic, /ABORT/);
});

test('doctest: timeout → distinct diagnostic', async () => {
    const api = makeApi({
        compileImpl: async () => ({ exitCode: 0, stdout: '', stderr: '', durationMs: 5000, timedOut: true }),
    });
    const report = await runTests(api, {
        build: { sources: ['main.cpp'] },
        timeoutMsPerCase: 100,
        cases: [{ kind: 'doctest', sourceFiles: ['t.cpp'] }],
    });
    assert.equal(report.failed, 1);
    assert.match(report.cases[0].diagnostic, /timed out after 100ms/);
});

test('doctest: empty source set → fails fast', async () => {
    const api = makeApi({});
    const report = await runTests(api, {
        cases: [{ kind: 'doctest', sourceFiles: [] }],
    });
    assert.equal(report.failed, 1);
    assert.match(report.cases[0].diagnostic, /no source files/);
});
