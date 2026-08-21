import assert from 'node:assert/strict';
import test from 'node:test';

import { ToolchainPreset } from 'emception';
import { wrapWorkerClient } from '../dist/createEmception.js';

function makeStubClient(overrides = {}) {
  return {
    run: async () => ({ exitCode: 0, stdout: 'hi\n', stderr: '', durationMs: 1, timedOut: false }),
    getFile: async () => null,
    writeFile: async () => {},
    listDir: async () => [],
    resetVfs: async () => {},
    boot: async () => {},
    terminate: () => {},
    ...overrides,
  };
}

test('facade.compileAndRun returns a ToolResult-shaped value', async () => {
  const api = wrapWorkerClient(makeStubClient());
  const result = await api.compileAndRun('int main(){return 0;}');
  assert.equal(typeof result.exitCode, 'number');
  assert.equal(typeof result.stdout, 'string');
  assert.equal(typeof result.stderr, 'string');
  assert.equal(typeof result.durationMs, 'number');
  assert.equal(typeof result.timedOut, 'boolean');
});

test('facade.runTests resolves a TestReport-shaped value', async () => {
  const api = wrapWorkerClient(makeStubClient());
  const report = await api.runTests({
    build: { toolchain: ToolchainPreset.CPP },
    cases: [{ kind: 'stdio', expectedStdout: 'hi' }],
  });
  assert.equal(typeof report.passed, 'number');
  assert.equal(typeof report.failed, 'number');
  assert.equal(typeof report.totalDurationMs, 'number');
  assert.ok(Array.isArray(report.cases));
  assert.equal(report.passed + report.failed, report.cases.length);
});

test('facade preserves the core API surface', () => {
  const api = wrapWorkerClient(makeStubClient());
  assert.ok(api.workspace);
  assert.equal(typeof api.run, 'function');
  assert.equal(typeof api.compileAndRun, 'function');
  assert.equal(typeof api.runTests, 'function');
  assert.equal(typeof api.on, 'function');
  assert.equal(typeof api.dispose, 'function');
});
