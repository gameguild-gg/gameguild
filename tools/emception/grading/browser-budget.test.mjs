import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import { createRequire, registerHooks } from 'node:module';

const sourceUrl = new URL('./browser.ts', import.meta.url).href;
const functionSupportSpecifier = '../../../packages/features/coding/src/native-function-support';
const functionSupportUrl = new URL(`${functionSupportSpecifier}.ts`, sourceUrl).href;
const require = createRequire(new URL('../package.json', import.meta.url));
const typescript = require('typescript');
const fixtureKey = '__codingWorkerBudgetFixture';
const mockModules = new Map([
  ['@gameguild/emception-browser', `export const createEmception = async () => { globalThis.${fixtureKey}.boots++; return globalThis.${fixtureKey}.api; };`],
  ['emception/testing', `export const runTests = (...args) => globalThis.${fixtureKey}.runTests(...args);
    export const withWorkspaceOverlay = (...args) => globalThis.${fixtureKey}.withOverlay(...args);
    export const generateDoctestHarness = (signature, cases) => ({filename: 'unused.cpp', source: 'public harness: ' + JSON.stringify({signature, cases})});`],
  ['emception', `export const ToolchainPreset = {CMake: 'cmake', Python: 'python'};
    export const resolveBuild = input => { globalThis.${fixtureKey}.build = input; return {toolchain: input.preset, ...input.callsite}; };`],
  ['../../../packages/infrastructure/ui-emception/src/assessment/plan', `export const buildAssessmentExecutionPlan = definition => {
    const tests = [...(definition.Tests?.Public ?? []), ...(definition.Tests?.Private ?? [])];
    if (!tests.length) return {plan: {cases: [{}]}, overlay: []};
    return {plan: {cases: tests.map((test, index) => ({kind: 'doctest', name: test.Name, weight: test.Weight, sourceFiles: ['/home/user/retained-' + index + '.cpp']}))},
      overlay: tests.map((_test, index) => ({path: '/home/user/retained-' + index + '.cpp', content: 'retained harness'}))};
  };`],
  ['../../../packages/infrastructure/ui-emception/src/assessment/presets', `export const createAssessmentWorkspaceConfig = language => { globalThis.${fixtureKey}.language = language; return {compile: {toolchain: language}}; };`],
]);
const hooks = registerHooks({
  resolve(specifier, context, next) {
    if (context.parentURL === sourceUrl) {
      if (specifier === functionSupportSpecifier) return { url: functionSupportUrl, shortCircuit: true };
      if (specifier === './execution-budget.js') {
        return { url: new URL('./execution-budget.ts', import.meta.url).href, shortCircuit: true };
      }
      if (mockModules.has(specifier)) {
        return { url: `data:text/javascript,${encodeURIComponent(mockModules.get(specifier))}`, shortCircuit: true };
      }
    }
    if (context.parentURL === functionSupportUrl && specifier === './contracts') {
      return { url: new URL('../../../packages/features/coding/src/contracts.ts', sourceUrl).href, shortCircuit: true };
    }
    return next(specifier, context);
  },
});
const { executeCodeAssessment } = await import('./browser.ts');
hooks.deregister();

function assertWorkerFeatureImports(text) {
  const source = typescript.createSourceFile('browser.ts', text,
    typescript.ScriptTarget.Latest, true);
  const featuresRoot = new URL('../../../packages/features/', import.meta.url).href;
  for (const statement of source.statements) {
    if (!typescript.isImportDeclaration(statement) || statement.importClause?.isTypeOnly) continue;
    const specifier = statement.moduleSpecifier.text;
    if (specifier === functionSupportSpecifier) continue;
    assert.ok(!specifier.startsWith('@game-guild/')
      && !new URL(specifier, sourceUrl).href.startsWith(featuresRoot), `Feature runtime import: ${specifier}`);
  }
}

test('worker: only the approved pure function composer may be a feature runtime dependency', () => {
  assertWorkerFeatureImports(readFileSync(new URL(sourceUrl), 'utf8'));
  const pure = typescript.createSourceFile('native-function-support.ts', readFileSync(new URL(functionSupportUrl), 'utf8'),
    typescript.ScriptTarget.Latest, true);
  for (const statement of pure.statements) {
    if (!typescript.isImportDeclaration(statement) || statement.importClause?.isTypeOnly) continue;
    assert.equal(statement.moduleSpecifier.text, './contracts');
  }
  const contracts = typescript.createSourceFile('contracts.ts',
    readFileSync(new URL('../../../packages/features/coding/src/contracts.ts', sourceUrl), 'utf8'),
    typescript.ScriptTarget.Latest, true);
  assert.equal(contracts.statements.some(typescript.isImportDeclaration), false);
});

test('worker: import guard rejects whole-domain and surface feature dependencies', () => {
  for (const specifier of ['@game-guild/coding', '@game-guild/coding-surface', '../../../packages/features/coding/src/index']) {
    assert.throws(() => assertWorkerFeatureImports(`import {anything} from '${specifier}';`), /Feature runtime import/);
  }
  assert.doesNotThrow(() => assertWorkerFeatureImports(`import {composeCodingNativeFunctionSupport} from '${functionSupportSpecifier}';`));
});

const request = {
  definition: { Environment: { Language: 'cpp' }, Data: { Files: { 'main.cpp': { Encoding: 'text', Content: 'int main(){}' } } } },
  files: {},
};
const result = { stdout: '', stderr: '', timedOut: false, exitCode: 0, durationMs: 1, phase: 'run' };

function setup(compile, automaticPhases = true) {
  let disposed = 0;
  let invoked = 0;
  const fixture = {
    boots: 0,
    api: {
      compileAndRun: async (...args) => {
        invoked++;
        const observed = await compile(...args);
        if (automaticPhases) {
          args[1]?.onPhase?.('compile');
          if (observed.phase === 'link' || observed.phase === 'run') args[1]?.onPhase?.('link');
          if (observed.phase === 'run') args[1]?.onPhase?.('run');
        }
        return observed;
      },
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

function functionRequest(language = 'cpp') {
  const input = configuredRequest();
  input.definition.ExerciseType = 'function';
  input.definition.Environment.Language = language;
  input.definition.Execution.Toolchain.Key = `${language}-emception`;
  const group = {
    kind: 'functional', Name: 'public function', Weight: 1.25,
    Function: { FunctionName: 'sum', Parameters: [{ Name: 'left', Type: 'integer' }, { Name: 'right', Type: 'integer' }], ReturnType: { Type: 'integer' } },
    Cases: [{ Inputs: [{ Type: 'integer', Content: 2 }, { Type: 'integer', Content: 3 }], Expected: { Type: 'integer', Content: 5 } }],
  };
  input.definition.Tests = { Public: [group], Private: [{ ...structuredClone(group), Name: 'private function', Weight: 2.5 }] };
  return input;
}

for (const language of ['c', 'cpp']) {
  test(`function support: official ${language} regenerates transient helpers and retains group receipts`, async () => {
    const state = setup(async () => result);
    let overlay;
    state.fixture.withOverlay = async (_workspace, files, operation) => { overlay = files; return operation(); };
    state.fixture.runTests = async (_api, plan) => {
      assert.deepEqual(plan.cases.map(item => item.name), ['public function', 'private function']);
      assert.deepEqual(plan.cases.map(item => item.weight), [1.25, 2.5]);
      assert.deepEqual(plan.cases.map(item => item.sourceFiles), [
        [`/tmp/gameguild-function-0.${language === 'c' ? 'c' : 'cpp'}`],
        [`/tmp/gameguild-function-1.${language === 'c' ? 'c' : 'cpp'}`],
      ]);
      return { cases: [{ passed: true }, { passed: false }] };
    };
    assert.deepEqual(await executeCodeAssessment(functionRequest(language)), [true, false]);
    assert.equal(overlay.length, 2);
    assert.ok(overlay.every(file => file.path.startsWith('/tmp/gameguild-function-')));
    assert.ok(overlay.every(file => file.content.includes('__attribute__((weak)) int main(void)')));
    assert.ok(overlay.every(file => file.content.includes('sum(int,int) __attribute__((weak))')));
    assert.equal(overlay.some(file => file.content.includes('retained harness')), false);
    assert.equal(state.disposed(), 1);
  });
}

test('function support: invalid frozen function values fail before runtime boot', async () => {
  const state = setup(async () => result);
  const input = functionRequest();
  input.definition.Tests.Public[0].Cases[0].Expected.Content = '5; injected()';
  await assert.rejects(executeCodeAssessment(input), /function.*value|literal|integer/i);
  assert.equal(state.fixture.boots, 0);
});

test('function support: graphics execution is not silently treated as WASI function testing', async () => {
  const state = setup(async () => result);
  const input = functionRequest();
  Object.assign(input.definition.Environment, { Runtime: 'sdl3', Tools: 'emcc', LibBundle: 'sdl3' });
  await assert.rejects(executeCodeAssessment(input), /WASI/);
  assert.equal(state.fixture.boots, 0);
});

test('function support: historical definitions retain their original execution mapper', async () => {
  const state = setup(async () => result);
  const configured = functionRequest();
  const input = { ...configured, definition: { ...configured.definition, Execution: undefined, Environment: { Language: 'cpp' } } };
  let paths;
  state.fixture.withOverlay = async (_workspace, files, operation) => { paths = files.map(file => file.path); return operation(); };
  state.fixture.runTests = async () => ({ cases: [{ passed: true }, { passed: false }] });
  assert.deepEqual(await executeCodeAssessment(input), [true, false]);
  assert.deepEqual(paths, ['/home/user/retained-0.cpp', '/home/user/retained-1.cpp']);
});

for (const phase of [undefined, null, 'unknown', 1]) {
  test(`phase: official worker refuses missing/invalid execution metadata ${String(phase)}`, async () => {
    const state = setup(async () => ({ ...result, phase }));
    await assert.rejects(executeCodeAssessment(request), /execution phase/);
    assert.equal(state.disposed(), 1);
  });
}

for (const phase of ['compile', 'link']) {
  test(`phase: official worker preserves ${phase} as a student failure, not unavailable execution`, async () => {
    const state = setup(async () => ({ ...result, exitCode: 1, phase }));
    state.fixture.runTests = async api => {
      const observation = await api.compileAndRun(undefined, {});
      assert.equal(observation.phase, phase);
      return { cases: [{ passed: false }] };
    };
    assert.deepEqual(await executeCodeAssessment(request), [false]);
    assert.equal(state.disposed(), 1);
  });
}

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

test('WB-B worker: preserves timeout and source ordering without excluding other translation units', async () => {
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

test('phase budgets worker: a hung compiler uses the infrastructure deadline, not the exercise deadline', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  const state = setup(() => new Promise(() => undefined));
  const pending = executeCodeAssessment(configuredRequest());
  const rejected = assert.rejects(pending, /preparation.*budget/);
  await new Promise(setImmediate);
  context.mock.timers.tick(1379);
  await new Promise(setImmediate);
  const disposedAtLimit = state.disposed();
  context.mock.timers.tick(120_000 - 1379);
  await rejected;
  assert.equal(disposedAtLimit, 0);
  assert.equal(state.disposed(), 1);
});

test('phase budgets worker: preparation does not spend the frozen student allowance', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  const state = setup(async (_source, options) => {
    options.onPhase('compile'); context.mock.timers.tick(2000);
    options.onPhase('link'); context.mock.timers.tick(2000);
    options.onPhase('run'); context.mock.timers.tick(1378);
    return result;
  }, false);
  assert.deepEqual(await executeCodeAssessment(configuredRequest()), [true]);
  assert.equal(state.disposed(), 1);
});

test('phase budgets worker: a hung student is cancelled relative to the run start', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  const state = setup((_source, options) => {
    options.onPhase('compile'); context.mock.timers.tick(2000);
    options.onPhase('link'); options.onPhase('run');
    return new Promise(() => undefined);
  }, false);
  const pending = executeCodeAssessment(configuredRequest());
  const rejected = assert.rejects(pending, /runtime.*budget/);
  await new Promise(setImmediate);
  context.mock.timers.tick(1378);
  const beforeDeadline = state.disposed();
  context.mock.timers.tick(1);
  await rejected;
  assert.equal(beforeDeadline, 0);
  assert.equal(state.disposed(), 1);
});

test('phase budgets worker: fallback raw compiler calls are not charged a nested student budget', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  const state = setup(async (_source, options) => {
    options.onPhase('compile');
    await state.fixture.api.run('clang', [], options);
    options.onPhase('link'); options.onPhase('run');
    return result;
  }, false);
  state.fixture.api.run = async () => { context.mock.timers.tick(2000); return result; };
  assert.deepEqual(await executeCodeAssessment(configuredRequest()), [true]);
  assert.equal(state.disposed(), 1);
});

test('phase budgets worker: missing lifecycle metadata fails closed', async () => {
  const state = setup(async () => result, false);
  await assert.rejects(executeCodeAssessment(configuredRequest()), /execution phase/);
  assert.equal(state.disposed(), 1);
});

test('phase budgets worker: failed pending compilation cannot let a later raw tool bypass cancellation', async context => {
  context.mock.timers.enable({ apis: ['Date', 'setTimeout'], now: 0 });
  const state = setup((_source, options) => {
    options.onPhase('compile');
    return new Promise(() => undefined);
  }, false);
  let rawCalls = 0;
  state.fixture.api.run = async () => { rawCalls++; return result; };
  state.fixture.runTests = async api => {
    try { await api.compileAndRun(undefined, {}); } catch { /* The engine records the failed case. */ }
    try { await api.run('clang-query', []); } catch { /* Further tools must remain cancelled. */ }
    return { cases: [{ passed: false }] };
  };
  const rejected = assert.rejects(executeCodeAssessment(configuredRequest()), /preparation budget/);
  await new Promise(setImmediate);
  context.mock.timers.tick(120_000);
  await rejected;
  await new Promise(setImmediate);
  assert.equal(rawCalls, 0);
  assert.equal(state.disposed(), 1);
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
