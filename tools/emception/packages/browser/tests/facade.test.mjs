import assert from 'node:assert/strict';
import test from 'node:test';

import { ToolchainPreset } from 'emception';
import { wrapWorkerClient } from '../dist/createEmception.js';

function makeStubClient(overrides = {}) {
  return {
    run: async () => ({ exitCode: 0, stdout: 'hi\n', stderr: '', durationMs: 1, timedOut: false }),
    getFile: async () => null,
    writeFile: async () => {},
    deleteFile: async () => {},
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

test('facade.runTests compiles persisted plan sources without replacing them with main.cpp', async () => {
  const writes = [];
  const runs = [];
  const api = wrapWorkerClient(makeStubClient({
    writeFile: async (path, content) => { writes.push({ path, content }); },
    run: async (cmd, argv) => {
      runs.push({ cmd, argv });
      return { exitCode: 0, stdout: '5\n', stderr: '', durationMs: 1, timedOut: false };
    },
  }));

  const report = await api.runTests({
    build: {
      toolchain: ToolchainPreset.CPP,
      sources: ['/user/main.cpp', '/home/user/functional_0_test.cpp'],
    },
    cases: [{ kind: 'stdio', expectedStdout: '5\n' }],
  });

  assert.equal(report.passed, 1);
  assert.deepEqual(writes, [], 'existing workspace files must not be overwritten');
  assert.deepEqual(
    runs.filter(({ cmd }) => cmd === 'clang').map(({ argv }) => argv.at(-1)),
    ['/user/main.cpp', '/home/user/functional_0_test.cpp'],
  );
  const link = runs.find(({ cmd }) => cmd === 'wasm-ld');
  assert.ok(link, 'links all compiled source objects');
  assert.equal(link.argv.filter((value) => /^\/tmp\/emception-build-\d+-\d+\.o$/.test(value)).length, 2);
});

test('facade isolates native scratch outputs between sequential test runs', async () => {
  const runs = [];
  const api = wrapWorkerClient(makeStubClient({
    run: async (cmd, argv) => {
      runs.push({ cmd, argv });
      return { exitCode: 0, stdout: '5\n', stderr: '', durationMs: 1, timedOut: false };
    },
  }));

  const plan = {
    build: {
      toolchain: ToolchainPreset.CPP,
      sources: ['/user/main.cpp'],
    },
    cases: [{ kind: 'stdio', expectedStdout: '5\n' }],
  };

  await api.runTests(plan);
  await api.runTests(plan);

  const links = runs.filter(({ cmd }) => cmd === 'wasm-ld');
  assert.equal(links.length, 2);
  const outputs = links.map(({ argv }) => argv[argv.indexOf('-o') + 1]);
  assert.notEqual(outputs[0], outputs[1], 'each invocation must use a fresh WASM output path');
});

test('facade removes an explicit native output before relinking it', async () => {
  const deletedPaths = [];
  const api = wrapWorkerClient(makeStubClient({
    deleteFile: async (path) => { deletedPaths.push(path); },
  }));

  await api.compileAndRun(undefined, {
    build: {
      toolchain: ToolchainPreset.CPP,
      sources: ['/user/main.cpp'],
      output: '/user/program.wasm',
    },
  });

  assert.equal(deletedPaths[0], '/user/program.wasm');
  assert.ok(
    deletedPaths.some((path) => /^\/tmp\/emception-build-\d+-0\.o$/.test(path)),
    'private object files are cleaned after execution',
  );
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
  assert.equal(typeof api.workspace.deleteFile, 'function');
});

test('facade forwards workspace deletion to the Worker client', async () => {
  const deletedPaths = [];
  const api = wrapWorkerClient(makeStubClient({
    deleteFile: async (path) => { deletedPaths.push(path); },
  }));

  await api.workspace.deleteFile('/home/user/private-test.cpp');

  assert.deepEqual(deletedPaths, ['/home/user/private-test.cpp']);
});

for (const mode of ['source', 'files']) {
  for (const terminalPhase of ['compile', 'link', 'run']) {
    test(`phase: facade exposes ${terminalPhase} for ${mode} without running later stages`, async () => {
      const calls = [];
      const phaseFor = { clang: 'compile', 'wasm-ld': 'link', 'wasi-run': 'run' };
      const api = wrapWorkerClient(makeStubClient({
        run: async (command) => {
          calls.push(command);
          return { exitCode: phaseFor[command] === terminalPhase ? 1 : 0,
            stdout: '', stderr: '', durationMs: 1, timedOut: false };
        },
      }));
      const result = mode === 'source'
        ? await api.compileAndRun('int main(){return 1;}')
        : await api.compileAndRun(undefined, { build: {
            toolchain: ToolchainPreset.CPP, sources: ['/home/user/solution.cpp', '/home/user/helper.cpp'],
          } });
      assert.equal(result.phase, terminalPhase);
      assert.equal(result.exitCode, 1);
      assert.equal(typeof result.stdout, 'string');
      assert.equal(typeof result.stderr, 'string');
      assert.equal(result.timedOut, false);
      assert.equal(phaseFor[calls.at(-1)], terminalPhase);
      if (terminalPhase !== 'run') assert.ok(!calls.includes('wasi-run'));
      if (terminalPhase === 'compile') assert.ok(!calls.includes('wasm-ld'));
    });
  }
}

test('phase: raw facade.run retains its original result without inventing a pipeline stage', async () => {
  const api = wrapWorkerClient(makeStubClient());
  const result = await api.run('clang', ['--version']);
  assert.equal(result.phase, undefined);
  assert.equal(result.stdout, 'hi\n');
});
