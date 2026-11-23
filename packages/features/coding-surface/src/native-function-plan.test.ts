import { codingContractFixture } from '@game-guild/coding/testing';
import { projectCodingDefinitionForLearner } from '@game-guild/coding';
import { describe, expect, it } from 'vitest';
import { buildCodingExecutionPlan } from './execution-plan';
import { createCodingSurfaceSession } from './session';

function definition(language: 'c' | 'cpp' = 'cpp') {
  const source = structuredClone(codingContractFixture.functionAuthoring);
  source.environment = { language, runtime: 'wasi' };
  source.limits = { timeLimitMs: 30_000, outputLimitBytes: 2_000_000, maxFileCount: 100 };
  source.toolchain.key = `${language}-emception`;
  source.files = [{ id: 'solution', path: `/home/user/solution.${language === 'c' ? 'c' : 'cpp'}`, encoding: 'text',
    visibility: 'public', mutable: true, content: 'int sum(int left,int right){return left+right;}' }];
  source.entryPoint.path = source.files[0]!.path;
  return source;
}

describe('native function preview composition', () => {
  it.each(['c', 'cpp'] as const)('composes %s support without changing submitted sources or result order', language => {
    const source = definition(language);
    const before = structuredClone(source);
    const execution = buildCodingExecutionPlan(source, 'full');
    expect(execution.plan.build?.sources).toEqual([source.entryPoint.path]);
    expect(execution.overlay.map(file => file.path)).toEqual([
      `/tmp/gameguild-function-0.${language === 'c' ? 'c' : 'cpp'}`,
      `/tmp/gameguild-function-1.${language === 'c' ? 'c' : 'cpp'}`,
    ]);
    expect(execution.plan.cases.map(item => item.name)).toEqual(['Public addition cases', 'Private boundary cases']);
    expect(execution.overlay[0]?.content).toContain('__attribute__((weak)) int main(void)');
    expect(execution.overlay[0]?.content).toContain('sum(int,int) __attribute__((weak))');
    if (language === 'cpp') expect(execution.overlay[0]?.content).toContain('extern "C" int gameguild_function_0');
    else expect(execution.overlay[0]?.content).not.toContain('extern "C"');
    expect(source).toEqual(before);
  });

  it('never reads private cases or support files when building learner public tests', () => {
    const source = definition();
    const hidden = source.tests.find(test => test.visibility === 'private');
    if (!hidden || hidden.kind !== 'function') throw new Error('Missing private fixture.');
    hidden.name = 'PRIVATE_CASE_CANARY';
    source.files.push({ id: 'private-file', path: '/home/user/private_canary.cpp', encoding: 'text',
      visibility: 'private', mutable: false, content: 'PRIVATE_FILE_CANARY' });
    const delivery = projectCodingDefinitionForLearner(source);
    const authoredPublic = buildCodingExecutionPlan(source, 'public');
    const learner = buildCodingExecutionPlan(delivery, 'public');
    expect(learner).toEqual(authoredPublic);
    expect(JSON.stringify(learner)).not.toMatch(/private_canary|PRIVATE_FILE_CANARY|PRIVATE_CASE_CANARY/);
    expect(learner.plan.cases).toHaveLength(1);
  });

  it('makes headers available without compiling them as translation units', () => {
    const source = definition();
    source.files.push({ id: 'header', path: '/home/user/helpers.h', encoding: 'text', visibility: 'public', mutable: true, content: '// header' });
    expect(buildCodingExecutionPlan(source, 'public').plan.build?.sources).toEqual([source.entryPoint.path]);
  });

  it('refuses function tests in an unverified graphics environment', () => {
    const source = definition();
    source.environment = { language: 'cpp', runtime: 'sdl3' };
    expect(() => buildCodingExecutionPlan(source, 'public')).toThrow(/WASI/);
  });

  it('does not infer C++ for an unknown native toolchain without an environment', () => {
    const source = definition();
    delete source.environment;
    source.toolchain.key = 'unknown-cpp';
    expect(() => buildCodingExecutionPlan(source, 'public')).toThrow(/toolchain/);
  });

  it('does not compose C support for a mismatched C++ toolchain', () => {
    const source = definition();
    source.environment = { language: 'c', runtime: 'wasi' };
    expect(() => buildCodingExecutionPlan(source, 'public')).toThrow(/toolchain/);
  });

  it('restores or removes temporary support after a preview failure', async () => {
    const source = definition();
    const bytes = new TextEncoder().encode('preexisting temporary file');
    const files = new Map<string, Uint8Array>([['/tmp/gameguild-function-0.cpp', bytes]]);
    let observed = false;
    const failure = new Error('synthetic execution failure');
    const session = createCodingSurfaceSession({ definition: source, mode: 'author-test', controller: {
      syncWorkspace: async () => undefined,
      getFiles: async () => [],
      api: {
        workspace: {
          readFile: async path => files.get(path) ?? null,
          writeFile: async (path, content) => { files.set(path, typeof content === 'string' ? new TextEncoder().encode(content) : content); },
          deleteFile: async path => { files.delete(path); },
        },
        runTests: async () => {
          observed = files.has('/tmp/gameguild-function-1.cpp');
          throw failure;
        },
      },
    } });
    await expect(session.run()).rejects.toBe(failure);
    expect(observed).toBe(true);
    expect(session.status).toBe('failed');
    expect([...files.keys()]).toEqual(['/tmp/gameguild-function-0.cpp']);
    // The external SDK can return a typed array from a different JS realm.
    expect(Array.from(files.get('/tmp/gameguild-function-0.cpp') ?? [])).toEqual(Array.from(bytes));
  });
});
