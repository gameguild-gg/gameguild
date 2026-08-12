// Facade contract test for the browser EmceptionAPI.
//
// Locks the contract that wrapWorkerClient(...).compileAndRun(...) and
// .runTests(...) must NOT throw the historical "not yet supported" stub
// errors, and that runTests resolves a TestReport-shaped object. The
// underlying WorkerClient is mocked so the test runs without spinning up
// a real WASM worker; a separate smoke test exercises the real worker
// under EMCEPTION_SMOKE=1.

import assert from 'node:assert/strict';
import test from 'node:test';

import { ToolchainPreset, type EmceptionAPI, type TestPlan } from 'emception';
import { wrapWorkerClient } from '../createEmception.js';
import type { WorkerClient } from '../worker-client.js';

interface StubClient {
    run: (tool: string, argv: string[], opts?: unknown) => Promise<{ exitCode: number; stdout: string; stderr: string; durationMs: number; timedOut: boolean }>;
    getFile: (path: string) => Promise<Uint8Array | null>;
    writeFile: (path: string, data: Uint8Array) => Promise<void>;
    listDir: (path: string) => Promise<string[]>;
    resetVfs: () => Promise<void>;
    boot: (manifestUrl: string) => Promise<void>;
    terminate: () => void;
}

/** Minimal WorkerClient stub satisfying the surface wrap() touches. */
function makeStubClient(overrides: Partial<StubClient> = {}): WorkerClient {
    // Structural stub: matches the public method surface of WorkerClient
    // without the private orch/io fields. Tests can use the cast because
    // wrap() only invokes the public methods.
    return {
        run: async () => ({ exitCode: 0, stdout: 'hi\n', stderr: '', durationMs: 1, timedOut: false }),
        getFile: async () => null,
        writeFile: async () => {},
        listDir: async () => [],
        resetVfs: async () => {},
        boot: async () => {},
        terminate: () => {},
        ...overrides,
    } as unknown as WorkerClient;
}

test('facade.compileAndRun: does NOT throw "not yet supported"', async () => {
    const api = wrapWorkerClient(makeStubClient());
    let threw: unknown;
    let result: unknown;
    try {
        result = await api.compileAndRun('int main(){return 0;}');
    } catch (err) {
        threw = err;
    }
    assert.equal(threw, undefined, `compileAndRun threw unexpectedly: ${threw instanceof Error ? threw.message : String(threw)}`);
    assert.ok(result, 'compileAndRun returned undefined');
});

test('facade.runTests: does NOT throw "not yet supported"; resolves a TestReport', async () => {
    const api = wrapWorkerClient(makeStubClient());
    const plan: TestPlan = {
        build: { toolchain: ToolchainPreset.CPP },
        cases: [{ kind: 'stdio', expectedStdout: 'hi' }],
    };
    let report;
    try {
        report = await api.runTests(plan);
    } catch (err) {
        assert.fail(`runTests should not throw, got: ${(err as Error).message}`);
    }
    assert.ok(report, 'runTests returned undefined');
    assert.equal(typeof report.passed, 'number', 'TestReport.passed must be number');
    assert.equal(typeof report.failed, 'number', 'TestReport.failed must be number');
    assert.equal(typeof report.totalDurationMs, 'number', 'TestReport.totalDurationMs must be number');
    assert.ok(Array.isArray(report.cases), 'TestReport.cases must be array');
    assert.equal(report.passed + report.failed, report.cases.length, 'passed + failed must equal cases.length');
});

test('facade.compileAndRun: returns a ToolResult-shaped value', async () => {
    const api = wrapWorkerClient(makeStubClient());
    const result = await api.compileAndRun('int main(){return 0;}') as { exitCode: number; stdout: string; stderr: string; durationMs: number; timedOut: boolean };
    assert.equal(typeof result.exitCode, 'number');
    assert.equal(typeof result.stdout, 'string');
    assert.equal(typeof result.stderr, 'string');
    assert.equal(typeof result.durationMs, 'number');
    assert.equal(typeof result.timedOut, 'boolean');
});

test('facade preserves workspace/run/on/dispose surface', () => {
    const api: EmceptionAPI = wrapWorkerClient(makeStubClient());
    assert.ok(api.workspace, 'workspace missing');
    assert.equal(typeof api.run, 'function');
    assert.equal(typeof api.compileAndRun, 'function');
    assert.equal(typeof api.runTests, 'function');
    assert.equal(typeof api.on, 'function');
    assert.equal(typeof api.dispose, 'function');
});
