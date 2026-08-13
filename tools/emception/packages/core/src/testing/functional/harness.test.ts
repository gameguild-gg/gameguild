// Unit tests for the FunctionalTest doctest harness generator.
//
// Verifies:
//   (a) mapCppType for all 4 v1 types.
//   (b) serializeCppLiteral for all 4 v1 types.
//   (c) throws on Array/Dictionary in both mapCppType and serializeCppLiteral.
//   (d) generated source includes the doctest include, <string>, the
//       `extern "C"` forward decl with correct types, the TEST_CASE block,
//       and the CHECK macro with correctly serialized arguments.
//   (e) escapeCppString handles `"`, `\`, newline, tab.
//   (f) integration smoke (gated on `EMCEPTION_SMOKE`): writes the harness
//       + a stub student function into an emception workspace and runs
//       `runTests` to prove the harness compiles and the doctest.h path
//       resolves.
//
// Run: `node --test packages/core/src/testing/functional/harness.test.ts`
// (Node 24 strips types).
//
// The integration smoke is marked `{ skip: !process.env.EMCEPTION_SMOKE }`
// because it boots the emception runtime (browser/worker adapter). The
// orchestrator runs it explicitly via `EMCEPTION_SMOKE=1 node --test ...`
// when a runtime is available.

import assert from 'node:assert/strict';
import test from 'node:test';

import {
    escapeCppString,
    generateDoctestHarness,
    mapCppType,
    serializeCppLiteral,
    type FunctionParameter,
} from './harness.ts';

// --- (a) mapCppType ---------------------------------------------------------

test('mapCppType: Integer → int', () => {
    assert.equal(mapCppType('Integer'), 'int');
});

test('mapCppType: Float → double', () => {
    assert.equal(mapCppType('Float'), 'double');
});

test('mapCppType: Boolean → bool', () => {
    assert.equal(mapCppType('Boolean'), 'bool');
});

test('mapCppType: String → std::string', () => {
    assert.equal(mapCppType('String'), 'std::string');
});

// --- (c) mapCppType throws on Array/Dictionary ------------------------------

test('mapCppType: Array throws v1-not-supported', () => {
    assert.throws(
        () => mapCppType('Array'),
        { message: 'Array/Dictionary parameter types not supported in v1' },
    );
});

test('mapCppType: Dictionary throws v1-not-supported', () => {
    assert.throws(
        () => mapCppType('Dictionary'),
        { message: 'Array/Dictionary parameter types not supported in v1' },
    );
});

// --- (b) serializeCppLiteral ------------------------------------------------

test('serializeCppLiteral: String → quoted with escaping', () => {
    const p: FunctionParameter = { type: 'String', content: 'hi' };
    assert.equal(serializeCppLiteral(p), '"hi"');
});

test('serializeCppLiteral: Boolean true → true', () => {
    const p: FunctionParameter = { type: 'Boolean', content: true };
    assert.equal(serializeCppLiteral(p), 'true');
});

test('serializeCppLiteral: Boolean false → false', () => {
    const p: FunctionParameter = { type: 'Boolean', content: false };
    assert.equal(serializeCppLiteral(p), 'false');
});

test('serializeCppLiteral: Integer → String(content)', () => {
    const p: FunctionParameter = { type: 'Integer', content: 42 };
    assert.equal(serializeCppLiteral(p), '42');
});

test('serializeCppLiteral: Float → String(content)', () => {
    const p: FunctionParameter = { type: 'Float', content: 3.14 };
    assert.equal(serializeCppLiteral(p), '3.14');
});

// --- (c) serializeCppLiteral throws on Array/Dictionary ---------------------

test('serializeCppLiteral: Array throws v1-not-supported', () => {
    const p: FunctionParameter = { type: 'Array', content: [1, 2, 3] };
    assert.throws(
        () => serializeCppLiteral(p),
        { message: 'Array/Dictionary parameter types not supported in v1' },
    );
});

test('serializeCppLiteral: Dictionary throws v1-not-supported', () => {
    const p: FunctionParameter = { type: 'Dictionary', content: { a: 1 } };
    assert.throws(
        () => serializeCppLiteral(p),
        { message: 'Array/Dictionary parameter types not supported in v1' },
    );
});

// --- (e) escapeCppString ----------------------------------------------------

test('escapeCppString: escapes double-quote', () => {
    assert.equal(escapeCppString('a"b'), 'a\\"b');
});

test('escapeCppString: escapes backslash', () => {
    assert.equal(escapeCppString('a\\b'), 'a\\\\b');
});

test('escapeCppString: escapes newline', () => {
    assert.equal(escapeCppString('a\nb'), 'a\\nb');
});

test('escapeCppString: escapes tab', () => {
    assert.equal(escapeCppString('a\tb'), 'a\\tb');
});

test('escapeCppString: backslash escaped BEFORE quote (ordering matters)', () => {
    // `"\` should become `"\\"` — backslash first, then quote — not `"\"` which
    // would escape the closing quote and break the literal.
    assert.equal(escapeCppString('"\\'), '\\"\\\\');
});

test('escapeCppString: leaves other characters alone', () => {
    assert.equal(escapeCppString('hello world 123'), 'hello world 123');
});

// --- (d) generateDoctestHarness — full shape --------------------------------

test('generateDoctestHarness: produces correct shape for int add(int, int)', () => {
    const out = generateDoctestHarness(
        {
            functionName: 'add',
            parameters: [
                { name: 'a', type: 'Integer', content: 0 },
                { name: 'b', type: 'Integer', content: 0 },
            ],
            returnType: { type: 'Integer', content: 0 },
        },
        [{ Inputs: [
            { type: 'Integer', content: 2 },
            { type: 'Integer', content: 3 },
        ], Expected: { type: 'Integer', content: 5 } }],
        { index: 0 },
    );

    assert.equal(out.filename, 'functional_0_test.cpp');

    // (d1) doctest include
    assert.match(out.source, /^#include "doctest\.h"$/m);
    // (d2) <string> include
    assert.match(out.source, /^#include <string>$/m);
    // (d3) extern "C" forward decl with types only (no param names)
    assert.match(out.source, /^extern "C" int add\(int, int\);$/m);
    // (d4) TEST_CASE block named "<index>:<functionName>"
    assert.match(out.source, /^TEST_CASE\("0:add"\) \{$/m);
    // (d5) CHECK macro with serialized args + expected literal
    assert.match(out.source, /^    CHECK\(add\(2, 3\) == 5\);$/m);
});

test('generateDoctestHarness: multi-case group emits ONE decl + N CHECK lines', () => {
    const out = generateDoctestHarness(
        {
            functionName: 'add',
            parameters: [
                { name: 'a', type: 'Integer', content: 0 },
                { name: 'b', type: 'Integer', content: 0 },
            ],
            returnType: { type: 'Integer', content: 0 },
        },
        [
            { Inputs: [{ type: 'Integer', content: 2 }, { type: 'Integer', content: 3 }], Expected: { type: 'Integer', content: 5 } },
            { Inputs: [{ type: 'Integer', content: 10 }, { type: 'Integer', content: 20 }], Expected: { type: 'Integer', content: 30 } },
        ],
        { index: 0 },
    );

    // ONE extern "C" forward decl — not duplicated per case
    const declMatches = out.source.match(/^extern "C" int add\(int, int\);$/gm);
    assert.equal(declMatches?.length, 1, 'extern "C" decl emitted exactly once');

    // ONE TEST_CASE block
    const testCaseMatches = out.source.match(/^TEST_CASE\("0:add"\) \{$/gm);
    assert.equal(testCaseMatches?.length, 1, 'TEST_CASE block emitted exactly once');

    // N CHECK lines (one per case), in the order the cases were supplied
    assert.match(out.source, /^    CHECK\(add\(2, 3\) == 5\);$/m);
    assert.match(out.source, /^    CHECK\(add\(10, 20\) == 30\);$/m);
    const checkMatches = out.source.match(/^    CHECK\(add\(/gm);
    assert.equal(checkMatches?.length, 2, '2 CHECK lines for 2 cases');
});

test('generateDoctestHarness: options.name overrides TEST_CASE label', () => {
    const out = generateDoctestHarness(
        {
            functionName: 'add',
            parameters: [
                { name: 'a', type: 'Integer', content: 0 },
                { name: 'b', type: 'Integer', content: 0 },
            ],
            returnType: { type: 'Integer', content: 0 },
        },
        [{ Inputs: [
            { type: 'Integer', content: 2 },
            { type: 'Integer', content: 3 },
        ], Expected: { type: 'Integer', content: 5 } }],
        { index: 7, name: 'add-basics' },
    );
    assert.equal(out.filename, 'functional_7_test.cpp', 'index still seeds filename');
    assert.match(out.source, /TEST_CASE\("7:add-basics"\)/, 'label falls back to options.name');
});

test('generateDoctestHarness: defaults index=0 when options omitted', () => {
    const out = generateDoctestHarness(
        {
            functionName: 'one',
            parameters: [],
            returnType: { type: 'Integer', content: 0 },
        },
        [{ Inputs: [], Expected: { type: 'Integer', content: 7 } }],
    );
    assert.equal(out.filename, 'functional_0_test.cpp');
    assert.match(out.source, /TEST_CASE\("0:one"\)/);
});

test('generateDoctestHarness: String returnType → std::string decl + quoted result', () => {
    const out = generateDoctestHarness(
        {
            functionName: 'greet',
            parameters: [{ name: 'who', type: 'String', content: '' }],
            returnType: { type: 'String', content: '' },
        },
        [{ Inputs: [{ type: 'String', content: 'world' }], Expected: { type: 'String', content: 'hello world' } }],
        { index: 1 },
    );

    assert.match(out.source, /^extern "C" std::string greet\(std::string\);$/m);
    assert.match(out.source, /^    CHECK\(greet\("world"\) == "hello world"\);$/m);
});

test('generateDoctestHarness: Boolean returnType + literal', () => {
    const out = generateDoctestHarness(
        {
            functionName: 'isEven',
            parameters: [{ name: 'n', type: 'Integer', content: 0 }],
            returnType: { type: 'Boolean', content: false },
        },
        [{ Inputs: [{ type: 'Integer', content: 4 }], Expected: { type: 'Boolean', content: true } }],
        { index: 2 },
    );

    assert.match(out.source, /^extern "C" bool isEven\(int\);$/m);
    assert.match(out.source, /^    CHECK\(isEven\(4\) == true\);$/m);
});

test('generateDoctestHarness: Float returnType + double args', () => {
    const out = generateDoctestHarness(
        {
            functionName: 'avg',
            parameters: [
                { name: 'x', type: 'Float', content: 0 },
                { name: 'y', type: 'Float', content: 0 },
            ],
            returnType: { type: 'Float', content: 0 },
        },
        [{ Inputs: [
            { type: 'Float', content: 1.5 },
            { type: 'Float', content: 2.5 },
        ], Expected: { type: 'Float', content: 2 } }],
        { index: 4 },
    );

    assert.match(out.source, /^extern "C" double avg\(double, double\);$/m);
    assert.match(out.source, /^    CHECK\(avg\(1\.5, 2\.5\) == 2\);$/m);
});

test('generateDoctestHarness: re-throws on Array param (does not generate)', () => {
    assert.throws(
        () =>
            generateDoctestHarness(
                {
                    functionName: 'bad',
                    parameters: [{ name: 'xs', type: 'Array', content: [1] }],
                    returnType: { type: 'Integer', content: 0 },
                },
                [{ Inputs: [{ type: 'Array', content: [1] }], Expected: { type: 'Integer', content: 0 } }],
                { index: 0 },
            ),
        { message: 'Array/Dictionary parameter types not supported in v1' },
    );
});

test('generateDoctestHarness: re-throws on Array returnType', () => {
    assert.throws(
        () =>
            generateDoctestHarness(
                {
                    functionName: 'bad',
                    parameters: [],
                    returnType: { type: 'Array', content: [] },
                },
                [{ Inputs: [], Expected: { type: 'Array', content: [] } }],
                { index: 0 },
            ),
        { message: 'Array/Dictionary parameter types not supported in v1' },
    );
});

test('generateDoctestHarness: throws when cases array is empty (M5 guard)', () => {
    assert.throws(
        () =>
            generateDoctestHarness(
                {
                    functionName: 'add',
                    parameters: [{ name: 'a', type: 'Integer', content: 0 }],
                    returnType: { type: 'Integer', content: 0 },
                },
                [],
                { index: 0 },
            ),
        { message: 'FunctionalTestGroup requires \u22651 case' },
    );
});

test('generateDoctestHarness: string content with quotes/backslash/newline/tab is escaped in literal', () => {
    const out = generateDoctestHarness(
        {
            functionName: 'echo',
            parameters: [{ name: 's', type: 'String', content: '' }],
            returnType: { type: 'String', content: '' },
        },
        [{ Inputs: [{ type: 'String', content: 'a"\\\n\tb' }], Expected: { type: 'String', content: 'x' } }],
        { index: 0 },
    );
    // Escaped form: a\"\n\tb  — within the surrounding double quotes the
    // backslash sequence are literal C++ escapes (\, ", \n, \t).
    assert.match(out.source, /echo\("a\\"\\\\\\n\\tb"\) == "x"/);
});

// --- (f) Integration smoke — gated on EMCEPTION_SMOKE ----------------------
//
// Boots a real emception runtime (browser/worker adapter) to verify the
// generated harness compiles and `doctest.h` resolves in the sysroot. Skipped
// by default — the orchestrator runs it explicitly when a runtime is present.
//
// Two skip gates:
//   1. `EMCEPTION_SMOKE=1` must be set (user opt-in).
//   2. A browser-like runtime must be available. The browser adapter
//      transitively imports `*.py?raw` (the emscripten subprocess shim) which
//      Node's loader rejects with `ERR_UNKNOWN_FILE_EXTENSION`. Running this
//      under raw Node previously produced a phantom pass because the test
//      object used `run` instead of `fn` (Node's test runner never executed
//      the body). Both are fixed below: `fn` is used so the body runs, and
//      the smoke is skipped when the host is Node-only.

const IS_NODE = typeof process === 'object' && !!process.versions?.node && typeof window === 'undefined';

test({
    name: 'generateDoctestHarness: integration smoke — harness compiles + runs in emception',
    async fn() {
        const { createEmception } = await import('@gameguild/emception-browser');
        const em = await createEmception({
            manifestUrl: process.env.EMCEPTION_MANIFEST_URL ?? '/cdn/manifest.json',
            tty: 'none',
        });
        try {
            // Multi-case group: signature add(int, int) → int with 2 cases.
            // Proves: (a) the harness compiles in the real worker, (b) the
            // doctest.h path resolves, (c) both CHECK lines execute and pass.
            const harness = generateDoctestHarness(
                {
                    functionName: 'add',
                    parameters: [
                        { name: 'a', type: 'Integer', content: 0 },
                        { name: 'b', type: 'Integer', content: 0 },
                    ],
                    returnType: { type: 'Integer', content: 0 },
                },
                [
                    { Inputs: [{ type: 'Integer', content: 2 }, { type: 'Integer', content: 3 }], Expected: { type: 'Integer', content: 5 } },
                    { Inputs: [{ type: 'Integer', content: 10 }, { type: 'Integer', content: 20 }], Expected: { type: 'Integer', content: 30 } },
                ],
                { index: 0 },
            );

            await em.writeFile('/home/user/solution.cpp', [
                'extern "C" int add(int a, int b) { return a + b; }',
            ].join('\n'));
            await em.writeFile(`/home/user/${harness.filename}`, harness.source);

            const report = await em.runTests({
                build: { sources: ['/home/user/solution.cpp'] },
                cases: [{
                    kind: 'doctest',
                    sourceFiles: [`/home/user/${harness.filename}`],
                    name: 'add-smoke',
                }],
            });

            assert.equal(report.passed, 1, `expected 1 pass, got ${report.passed}`);
            assert.equal(report.failed, 0);
            const failed = report.cases.find((c) => !c.passed);
            assert.equal(failed, undefined, `case failed: ${failed?.diagnostic ?? '<no diagnostic>'}`);
        } finally {
            em.dispose();
        }
    },
    skip: (!process.env.EMCEPTION_SMOKE || IS_NODE)
        ? 'set EMCEPTION_SMOKE=1 in a browser test runner (Playwright/etc); raw Node cannot host the browser adapter'
        : undefined,
});
