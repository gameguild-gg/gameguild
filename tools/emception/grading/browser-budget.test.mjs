import assert from 'node:assert/strict';
import test from 'node:test';
import { registerHooks } from 'node:module';

const sourceUrl = new URL('./browser.ts', import.meta.url).href;
const fixtureKey = '__codingWorkerBudgetFixture';
const mockModules = new Map([
  ['@gameguild/emception-browser', `export const createEmception = async () => globalThis.${fixtureKey}.api;`],
  ['emception/testing', `export const runTests = (...args) => globalThis.${fixtureKey}.runTests(...args);
    export const withWorkspaceOverlay = (...args) => globalThis.${fixtureKey}.withOverlay(...args);`],
  ['emception', `export const ToolchainPreset = {CMake: 'cmake', Python: 'python'};
    export const resolveBuild = input => ({toolchain: 'cpp', ...input.callsite});`],
  ['../../../packages/infrastructure/ui-emception/src/assessment/plan', `export const buildAssessmentExecutionPlan = () => ({plan: {cases: [{}]}, overlay: []});`],
  ['../../../packages/infrastructure/ui-emception/src/assessment/presets', `export const createAssessmentWorkspaceConfig = () => ({compile: {toolchain: 'cpp'}});`],
]);
const hooks = registerHooks({
  resolve(specifier, context, next) {
    if (context.parentURL === sourceUrl) {
      if (specifier === './execution-budget.js') {
        return { url: new URL('./execution-budget.ts', import.meta.url).href, shortCircuit: true };
      }
      if (mockModules.has(specifier)) {
        return { url: `data:text/javascript,${encodeURIComponent(mockModules.get(specifier))}`, shortCircuit: true };
      }
    }
    return next(specifier, context);
  },
});
const { executeCodeAssessment } = await import('./browser.ts');
hooks.deregister();

const request = {
  definition: { Environment: { Language: 'cpp' }, Data: { Files: { 'main.cpp': { Encoding: 'text', Content: 'int main(){}' } } } },
  files: {},
};
const result = { stdout: '', stderr: '', timedOut: false, exitCode: 0, durationMs: 1 };

function setup(compile) {
  let disposed = 0;
  let invoked = 0;
  const fixture = {
    api: {
      compileAndRun: async (...args) => { invoked++; return compile(...args); },
      run: async () => result,
      workspace: { setBuild: async () => undefined, writeFile: async () => undefined },
      dispose: () => { disposed++; },
    },
    runTests: async api => {
      try { await api.compileAndRun(undefined, { stdout: 'capture', stderr: 'capture' }); }
      catch { /* Match the public test engine, which represents errors as failed cases. */ }
      return { cases: [{ passed: true }] };
    },
    withOverlay: async (_workspace, _overlay, operation) => operation(),
  };
  globalThis[fixtureKey] = fixture;
  return { fixture, disposed: () => disposed, invoked: () => invoked };
}

test('worker: streamed byte overflow is unavailable, not a successful student result', async () => {
  const state = setup(async (_source, options) => {
    if (typeof options?.stdout === 'function') options.stdout(new Uint8Array(2_000_001));
    return result;
  });
  await assert.rejects(executeCodeAssessment(request), /output budget/);
  assert.equal(state.disposed(), 1);
});

test('worker: stream failure settles even when terminated-runtime overlay cleanup is stuck', async () => {
  const state = setup(async (_source, options) => {
    if (typeof options?.stderr === 'function') options.stderr(new Uint8Array(2_000_001));
    return result;
  });
  state.fixture.withOverlay = async (_workspace, _overlay, operation) => {
    await operation();
    return new Promise(() => undefined);
  };
  const pending = executeCodeAssessment(request);
  const observation = await Promise.race([
    pending.then(() => 'resolved', error => error.message),
    new Promise(resolve => { setTimeout(() => resolve('test deadline'), 250); }),
  ]);
  assert.equal(observation, 'Code execution exceeded its output budget.');
  assert.equal(state.disposed(), 1);
});

test('worker: no further case runs after a failed/disposed runtime', async () => {
  const state = setup(async () => ({ ...result, timedOut: true }));
  state.fixture.runTests = async api => {
    for (let index = 0; index < 2; index++) {
      try { await api.compileAndRun(undefined, { stdout: 'capture', stderr: 'capture' }); }
      catch { /* The official boundary must still stop subsequent compiler calls. */ }
    }
    return { cases: [{ passed: false }] };
  };
  await assert.rejects(executeCodeAssessment(request), /runtime.*budget/);
  assert.equal(state.invoked(), 1);
  assert.equal(state.disposed(), 1);
});

test('worker: successful result retains the existing boolean receipt and one disposal', async () => {
  const state = setup(async () => result);
  assert.deepEqual(await executeCodeAssessment(request), [true]);
  assert.equal(state.disposed(), 1);
});

test.after(() => { delete globalThis[fixtureKey]; });
