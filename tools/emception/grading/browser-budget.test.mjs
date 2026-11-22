import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import { createRequire, registerHooks } from 'node:module';

const sourceUrl = new URL('./browser.ts', import.meta.url).href;
const require = createRequire(new URL('../package.json', import.meta.url));
const typescript = require('typescript');
const fixtureKey = '__codingWorkerBudgetFixture';
const mockModules = new Map([
  ['@gameguild/emception-browser', `export const createEmception = async () => { globalThis.${fixtureKey}.boots++; return globalThis.${fixtureKey}.api; };`],
  ['emception/testing', `export const runTests = (...args) => globalThis.${fixtureKey}.runTests(...args);
    export const withWorkspaceOverlay = (...args) => globalThis.${fixtureKey}.withOverlay(...args);`],
  ['emception', `export const ToolchainPreset = {CMake: 'cmake', Python: 'python'};
    export const resolveBuild = input => { globalThis.${fixtureKey}.build = input; return {toolchain: input.preset, ...input.callsite}; };`],
  ['../../../packages/infrastructure/ui-emception/src/assessment/plan', `export const buildAssessmentExecutionPlan = () => ({plan: {cases: [{}]}, overlay: []});`],
  ['../../../packages/infrastructure/ui-emception/src/assessment/presets', `export const createAssessmentWorkspaceConfig = language => { globalThis.${fixtureKey}.language = language; return {compile: {toolchain: language}}; };`],
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

test('worker: artifact entry does not acquire feature implementation dependencies', () => {
  const source = typescript.createSourceFile('browser.ts', readFileSync(new URL(sourceUrl), 'utf8'),
    typescript.ScriptTarget.Latest, true);
  const featuresRoot = new URL('../../../packages/features/', import.meta.url).href;
  for (const statement of source.statements) {
    if (!typescript.isImportDeclaration(statement) || statement.importClause?.isTypeOnly) continue;
    const specifier = statement.moduleSpecifier.text;
    assert.ok(!specifier.startsWith('@game-guild/')
      && !new URL(specifier, sourceUrl).href.startsWith(featuresRoot), `Feature runtime import: ${specifier}`);
  }
});

const request = {
  definition: { Environment: { Language: 'cpp' }, Data: { Files: { 'main.cpp': { Encoding: 'text', Content: 'int main(){}' } } } },
  files: {},
};
const result = { stdout: '', stderr: '', timedOut: false, exitCode: 0, durationMs: 1 };

function setup(compile) {
  let disposed = 0;
  let invoked = 0;
  const fixture = {
    boots: 0,
    api: {
      compileAndRun: async (...args) => { invoked++; return compile(...args); },
      run: async () => result,
      workspace: { setBuild: async () => undefined, writeFile: async () => undefined },
      dispose: () => { disposed++; },
    },
    runTests: async (api, plan) => {
      fixture.plan = plan;
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

for (const language of ['CPP', 'C', 'Sdl-Cpp', 'Raylib-Cpp', 'Allegro-Cpp']) {
  test(`worker: retains the historical default preset for legacy ${language}`, async () => {
    const state = setup(async () => result);
    await executeCodeAssessment({ ...request, definition: { ...request.definition, Environment: { Language: language } } });
    assert.equal(state.fixture.language, 'cpp');
  });
}

function configuredRequest() {
  return {
    definition: {
      ...request.definition, ExerciseType: 'standard-io',
      Environment: { Language: 'cpp', Runtime: 'wasi', Tools: 'clang', AllowStudentCreateFiles: true },
      Execution: {
        Limits: { TimeLimitMs: 1379, OutputLimitBytes: 65, MaxFileCount: 7 },
        EntryPoint: { Path: '/home/user/solution.cpp' },
        Toolchain: { Key: 'cpp-emception', Version: '4.5.2', RuntimeAbi: 'emception-browser-v1',
          ArtifactDigest: 'sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37' },
      },
      Data: { Files: {
        'main.cpp': { Encoding: 'text', Visibility: 'Public', Content: '// decoy' },
        'solution.cpp': { Encoding: 'text', Visibility: 'Public', Content: 'int main(){}' },
      } },
    },
    files: {},
  };
}

test('WB-B worker: forwards exact published timeout and explicit source before a decoy main', async () => {
  const state = setup(async () => result);
  assert.deepEqual(await executeCodeAssessment(configuredRequest()), [true]);
  assert.equal(state.fixture.plan.timeoutMsPerCase, 1379);
  assert.deepEqual(state.fixture.build.callsite.sources, ['/home/user/solution.cpp', '/home/user/main.cpp']);
  assert.equal(state.disposed(), 1);
});

for (const [language, runtime, tools, bundle, preset] of [
  ['cpp', 'wasi', 'clang', null, 'cpp'],
  ['c', 'wasi', 'clang', null, 'c'],
  ['cpp', 'sdl3', 'emcc', 'sdl3', 'sdl-cpp'],
  ['cpp', 'raylib', 'emcc', 'raylib', 'raylib-cpp'],
  ['cpp', 'allegro5', 'emcc', 'allegro5', 'allegro-cpp'],
]) {
  test(`WB-B worker: explicit ${language}/${runtime} selects its native preset`, async () => {
    const state = setup(async () => result);
    const input = configuredRequest();
    input.definition.Environment = { Language: language, Runtime: runtime, Tools: tools, LibBundle: bundle };
    input.definition.Execution.Toolchain.Key = `${language}-emception`;
    await executeCodeAssessment(input);
    assert.equal(state.fixture.language, preset);
    assert.equal(state.fixture.build.preset, preset);
  });
}

test('WB-B worker: enforces the frozen output budget, not its larger legacy default', async () => {
  const state = setup(async (_source, options) => {
    options.stdout(new Uint8Array(66));
    return result;
  });
  await assert.rejects(executeCodeAssessment(configuredRequest()), /output budget/);
  assert.equal(state.disposed(), 1);
});

test('WB-B worker: a hung compiler is cancelled at the frozen time limit', async context => {
  context.mock.timers.enable({ apis: ['setTimeout'] });
  const state = setup(() => new Promise(() => undefined));
  const pending = executeCodeAssessment(configuredRequest());
  const rejected = assert.rejects(pending, /runtime.*budget/);
  await new Promise(setImmediate);
  context.mock.timers.tick(1379);
  await new Promise(setImmediate);
  const disposedAtLimit = state.disposed();
  context.mock.timers.tick(30_000);
  await rejected;
  assert.equal(disposedAtLimit, 1);
});

for (const mutation of ['runtime', 'tools', 'version', 'memory', 'missing-execution', 'missing-file', 'private-entry', 'merged-file-count',
  'extra-execution', 'extra-limits', 'extra-entry-point', 'extra-toolchain', 'language', 'language-runtime', 'bundle',
  'toolchain-key', 'toolchain-abi', 'toolchain-digest', 'time-type', 'time-max', 'output-type', 'output-max', 'file-count-type',
  'file-count-max', 'entry-extension', 'entry-traversal', 'entry-reserved', 'entry-symbol']) {
  test(`WB-B worker: rejects ${mutation} before runtime boot`, async () => {
    const state = setup(async () => result);
    const input = configuredRequest();
    if (mutation === 'runtime') input.definition.Environment.Runtime = 'unknown';
    if (mutation === 'tools') input.definition.Environment.Tools = 'unknown';
    if (mutation === 'version') input.definition.Execution.Toolchain.Version = '999';
    if (mutation === 'memory') input.definition.Execution.Limits.MemoryLimitBytes = 128;
    if (mutation === 'missing-execution') delete input.definition.Execution;
    if (mutation === 'missing-file') input.definition.Execution.EntryPoint.Path = '/home/user/missing.cpp';
    if (mutation === 'private-entry') input.definition.Data.Files['solution.cpp'].Visibility = 'Private';
    if (mutation === 'extra-execution') input.definition.Execution.FutureSetting = true;
    if (mutation === 'extra-limits') input.definition.Execution.Limits.FutureSetting = true;
    if (mutation === 'extra-entry-point') input.definition.Execution.EntryPoint.FutureSetting = true;
    if (mutation === 'extra-toolchain') input.definition.Execution.Toolchain.FutureSetting = true;
    if (mutation === 'language') input.definition.Environment.Language = 'python';
    if (mutation === 'language-runtime') {
      input.definition.Environment = { Language: 'c', Runtime: 'sdl3', Tools: 'emcc', LibBundle: 'sdl3' };
      input.definition.Execution.Toolchain.Key = 'c-emception';
    }
    if (mutation === 'bundle') input.definition.Environment.LibBundle = 'sdl3';
    if (mutation === 'toolchain-key') input.definition.Execution.Toolchain.Key = 'other';
    if (mutation === 'toolchain-abi') input.definition.Execution.Toolchain.RuntimeAbi = 'other';
    if (mutation === 'toolchain-digest') input.definition.Execution.Toolchain.ArtifactDigest = 'sha256:' + '0'.repeat(64);
    if (mutation === 'time-type') input.definition.Execution.Limits.TimeLimitMs = '1379';
    if (mutation === 'time-max') input.definition.Execution.Limits.TimeLimitMs = 30_001;
    if (mutation === 'output-type') input.definition.Execution.Limits.OutputLimitBytes = null;
    if (mutation === 'output-max') input.definition.Execution.Limits.OutputLimitBytes = 2_000_001;
    if (mutation === 'file-count-type') input.definition.Execution.Limits.MaxFileCount = 7.5;
    if (mutation === 'file-count-max') input.definition.Execution.Limits.MaxFileCount = 101;
    if (mutation === 'entry-extension') input.definition.Execution.EntryPoint.Path = 'solution.CPP';
    if (mutation === 'entry-traversal') input.definition.Execution.EntryPoint.Path = '../solution.cpp';
    if (mutation === 'entry-reserved') input.definition.Execution.EntryPoint.Path = '.gameguild/solution.cpp';
    if (mutation === 'entry-symbol') input.definition.Execution.EntryPoint.Symbol = 'main();';
    if (mutation === 'merged-file-count') {
      input.definition.Execution.Limits.MaxFileCount = 2;
      input.files['new.cpp'] = { encoding: 'text', content: '// extra' };
    }
    await assert.rejects(executeCodeAssessment(input));
    assert.equal(state.fixture.boots, 0);
    assert.equal(state.invoked(), 0);
  });
}

test.after(() => { delete globalThis[fixtureKey]; });
