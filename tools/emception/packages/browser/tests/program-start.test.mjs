import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { registerHooks } from 'node:module';
import test from 'node:test';
import { wrapWorkerClient } from '../dist/createEmception.js';

// Node loads the real raw Python asset as text; no runtime logic is replaced.
const hooks = registerHooks({
  load(url, context, nextLoad) {
    if (url.endsWith('/subprocess_shim.py?raw')) {
      return { format: 'module', source: `export default ${JSON.stringify(readFileSync(new URL(url), 'utf8'))}`, shortCircuit: true };
    }
    return nextLoad(url, context);
  },
});
let ToolRunner;
try {
  ({ ToolRunner } = await import('../dist/tool-runner.js'));
} finally {
  hooks.deregister();
}

function client(events) {
  return {
    async run(command, _argv, options) {
      events.push(command);
      if (command === 'wasi-run') options.onProgramStart?.();
      else assert.equal(options.onProgramStart, undefined);
      return { exitCode: 0, stdout: '', stderr: '' };
    },
    async writeFile() {},
    async deleteFile() {},
    terminate() {},
  };
}

for (const mode of ['source', 'files']) {
  test(`program-start: facade forwards only the native execution observer for ${mode}`, async () => {
    const events = [];
    const api = wrapWorkerClient(client(events));
    const source = mode === 'source' ? 'int main(){return 0;}' : undefined;
    const result = await api.compileAndRun(source, {
      ...(mode === 'files' ? { build: { sources: ['/home/user/main.cpp'] } } : {}),
      onProgramStart: () => events.push('program-start'),
    });
    assert.equal(result.phase, 'run');
    assert.deepEqual(events, ['clang', 'wasm-ld', 'wasi-run', 'program-start']);
  });
}

test('program-start: direct browser run forwards the optional observer', async () => {
  const events = [];
  const api = wrapWorkerClient(client(events));
  await api.run('wasi-run', ['program.wasm'], { onProgramStart: () => events.push('program-start') });
  assert.deepEqual(events, ['wasi-run', 'program-start']);
});

// Real WASM: constructor sets an exported mutable global to 7; main returns it.
function nativeFixture() {
  const name = (value) => [...new TextEncoder().encode(value)];
  const exportOf = (value, kind, index) => [value.length, ...name(value), kind, index];
  const section = (id, payload) => [id, payload.length, ...payload];
  return Uint8Array.from([
    0,
    97,
    115,
    109,
    1,
    0,
    0,
    0,
    ...section(1, [2, 96, 0, 0, 96, 0, 1, 127]),
    ...section(3, [2, 0, 1]),
    ...section(6, [1, 127, 1, 65, 0, 11]),
    ...section(7, [3, ...exportOf('__wasm_call_ctors', 0, 0), ...exportOf('main', 0, 1), ...exportOf('observed', 3, 0)]),
    ...section(10, [2, 6, 0, 65, 7, 36, 0, 11, 4, 0, 35, 0, 11]),
  ]);
}

test('program-start: actual native start occurs after instantiation but before constructors', async (t) => {
  const instantiate = WebAssembly.instantiate;
  let instance;
  let starts = 0;
  t.mock.method(WebAssembly, 'instantiate', async (...args) => {
    instance = await instantiate(...args);
    return instance;
  });
  const runner = new ToolRunner({ fetchFile: async () => nativeFixture() });
  const result = await runner.run('wasi-run', ['wasi-run', '/program.wasm'], {
    onProgramStart() {
      starts++;
      assert.ok(instance instanceof WebAssembly.Instance);
      assert.equal(instance.exports.observed.value, 0, 'constructors must not run during preparation');
    },
  });
  assert.equal(starts, 1);
  assert.equal(result.exitCode, 7);
  assert.equal(instance.exports.observed.value, 7);
});

test('program-start: existing native callers retain constructor and result behavior', async () => {
  const runner = new ToolRunner({ fetchFile: async () => nativeFixture() });
  const result = await runner.run('wasi-run', ['wasi-run', '/program.wasm']);
  assert.deepEqual(result, { exitCode: 7, stdout: '', stderr: '' });
});

test('program-start: failed module preparation cannot acknowledge program entry', async () => {
  let starts = 0;
  const runner = new ToolRunner({ fetchFile: async () => Uint8Array.from([1, 2, 3]) });
  const result = await runner.run('wasi-run', ['wasi-run', '/invalid.wasm'], { onProgramStart: () => starts++ });
  assert.equal(starts, 0);
  assert.notEqual(result.exitCode, 0);
});
