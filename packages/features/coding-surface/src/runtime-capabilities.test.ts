import { CODING_TOOLCHAIN } from '@game-guild/coding';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { resolveCodingRuntimeCapabilities } from './runtime-capabilities';
import type { CodingRuntimeCapabilityRequest } from './runtime-capability-types';

const base: CodingRuntimeCapabilityRequest = {
  exerciseType: 'standard-io',
  execution: {
    environment: { language: 'cpp', runtime: 'wasi' },
    toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
  },
  mode: 'learner',
  readOnly: false,
  allowLearnerCreatedFiles: false,
  graphicsPanelVisible: false,
};

function request(overrides: Partial<CodingRuntimeCapabilityRequest> = {}): CodingRuntimeCapabilityRequest {
  return { ...structuredClone(base), ...overrides };
}

const unavailable = (reason: string) => ({ status: 'unavailable', reason });

afterEach(() => vi.unstubAllGlobals());

describe('UX03-A: local runtime capability eligibility', () => {
  it('offers independent WASI program, public batch tests and proven C++ interactive input', () => {
    const result = resolveCodingRuntimeCapabilities(request());
    expect(result.program).toEqual({ status: 'available' });
    expect(result.tests).toEqual({ status: 'available', scope: 'public' });
    expect(result.interactiveInput).toEqual({ status: 'available' });
    expect(result.graphics).toEqual(unavailable('graphics-not-required'));
    expect(result.graphicsTarget).toEqual({ required: false, visible: false });
    expect(result.createFiles).toEqual(unavailable('assignment-disallows-files'));
  });

  it('does not advertise unverified C interactive input based on its batch support', () => {
    const input = request();
    input.execution.environment.language = 'c';
    input.execution.toolchain.key = 'c-emception';
    const result = resolveCodingRuntimeCapabilities(input);
    expect(result.program).toEqual({ status: 'available' });
    expect(result.tests).toEqual({ status: 'available', scope: 'public' });
    expect(result.interactiveInput).toEqual(unavailable('interactive-input-unverified'));
  });

  it('keeps authoring operations offline while permitting starter file creation', () => {
    const result = resolveCodingRuntimeCapabilities(request({ mode: 'author' }));
    for (const operation of [result.program, result.tests, result.interactiveInput, result.graphics]) {
      expect(operation).toEqual(unavailable('authoring-only'));
    }
    expect(result.tests).not.toHaveProperty('scope');
    expect(result.createFiles).toEqual({ status: 'available' });
  });

  it.each(['author-test', 'grader'] as const)('offers full local diagnostics but no file creation in %s', mode => {
    const result = resolveCodingRuntimeCapabilities(request({ mode, allowLearnerCreatedFiles: true }));
    expect(result.program).toEqual({ status: 'available' });
    expect(result.tests).toEqual({ status: 'available', scope: 'full' });
    expect(result.createFiles).toEqual(unavailable('read-only-context'));
    expect(Object.keys(result)).toEqual([
      'program', 'tests', 'interactiveInput', 'graphics', 'createFiles', 'graphicsTarget',
    ]);
  });

  it('allows learner-created files only when the existing assignment setting permits them', () => {
    expect(resolveCodingRuntimeCapabilities(request({ allowLearnerCreatedFiles: true })).createFiles)
      .toEqual({ status: 'available' });
    expect(resolveCodingRuntimeCapabilities(request()).createFiles)
      .toEqual(unavailable('assignment-disallows-files'));
  });

  it.each(['author', 'learner'] as const)('honors host read-only file restrictions in %s mode', mode => {
    const result = resolveCodingRuntimeCapabilities(request({ mode, readOnly: true, allowLearnerCreatedFiles: true }));
    expect(result.createFiles).toEqual(unavailable('read-only-context'));
    if (mode === 'learner') expect(result.program).toEqual({ status: 'available' });
  });

  it('offers the proven native function test mapper separately from a program run', () => {
    const result = resolveCodingRuntimeCapabilities(request({ exerciseType: 'function' }));
    expect(result.program).toEqual({ status: 'available' });
    expect(result.tests).toEqual({ status: 'available', scope: 'public' });
  });

  it('routes a proven SDL3 program to graphics, not the native batch test path', () => {
    const input = request({ graphicsPanelVisible: true });
    input.execution.environment.runtime = 'sdl3';
    const result = resolveCodingRuntimeCapabilities(input);
    expect(result.program).toEqual({ status: 'available' });
    expect(result.graphics).toEqual({ status: 'available' });
    expect(result.graphicsTarget).toEqual({ required: true, visible: true });
    expect(result.tests).toEqual(unavailable('graphics-tests-unavailable'));
    expect(result.interactiveInput).toEqual(unavailable('interactive-input-unverified'));
  });

  it('retains the required canvas when its optional presentation panel is hidden', () => {
    const input = request({ graphicsPanelVisible: true });
    input.execution.environment.runtime = 'sdl3';
    const visible = resolveCodingRuntimeCapabilities(input);
    const hidden = resolveCodingRuntimeCapabilities({ ...input, graphicsPanelVisible: false });
    expect(hidden.graphicsTarget).toEqual({ required: true, visible: false });
    expect(hidden.program).toEqual(visible.program);
    expect(hidden.graphics).toEqual(visible.graphics);
    expect(hidden.tests).toEqual(visible.tests);
  });

  it('does not turn a WASI runtime into graphics by opening a panel', () => {
    const result = resolveCodingRuntimeCapabilities(request({ graphicsPanelVisible: true }));
    expect(result.graphicsTarget).toEqual({ required: false, visible: false });
    expect(result.graphics).toEqual(unavailable('graphics-not-required'));
  });

  it('does not derive graphics support from the exercise family', () => {
    const input = request({ exerciseType: 'function', graphicsPanelVisible: true });
    input.execution.environment.runtime = 'sdl3';
    const result = resolveCodingRuntimeCapabilities(input);
    expect(result.graphics).toEqual({ status: 'available' });
    expect(result.graphicsTarget.required).toBe(true);
    expect(result.tests).toEqual(unavailable('graphics-tests-unavailable'));
  });

  it.each(['raylib', 'allegro5'] as const)('does not promote the unverified %s runtime from its preset alone', runtime => {
    const input = request();
    input.execution.environment.runtime = runtime;
    const result = resolveCodingRuntimeCapabilities(input);
    for (const operation of [result.program, result.tests, result.interactiveInput, result.graphics]) {
      expect(operation).toEqual(unavailable('runtime-unverified'));
    }
    expect(result.graphicsTarget.required).toBe(true);
  });

  it('refuses a C graphics environment instead of silently selecting C++', () => {
    const input = request({ allowLearnerCreatedFiles: true });
    input.execution.environment = { language: 'c', runtime: 'sdl3' };
    input.execution.toolchain.key = 'c-emception';
    const result = resolveCodingRuntimeCapabilities(input);
    for (const operation of [result.program, result.tests, result.interactiveInput, result.graphics]) {
      expect(operation).toEqual(unavailable('unsupported-environment'));
    }
    expect(result.createFiles).toEqual({ status: 'available' });
  });

  it.each([
    ['key', 'python-cpp-emception'],
    ['version', '4.4.0'],
    ['runtimeAbi', 'another-abi'],
    ['artifactDigest', `sha256:${'a'.repeat(64)}`],
  ])('rejects a mismatched frozen %s without booting a fallback', (field, value) => {
    const input = request();
    Reflect.set(input.execution.toolchain, field, value);
    const result = resolveCodingRuntimeCapabilities(input);
    for (const operation of [result.program, result.tests, result.interactiveInput, result.graphics]) {
      expect(operation).toEqual(unavailable('incompatible-toolchain'));
    }
  });

  it('rejects a toolchain key that differs from the explicitly selected language', () => {
    const input = request();
    input.execution.environment.language = 'c';
    expect(resolveCodingRuntimeCapabilities(input).program).toEqual(unavailable('incompatible-toolchain'));
  });

  it('does not require runtime eligibility for offline authoring', () => {
    const input = request({ mode: 'author' });
    input.execution.toolchain.version = 'unavailable-version';
    const result = resolveCodingRuntimeCapabilities(input);
    expect(result.program).toEqual(unavailable('authoring-only'));
    expect(result.createFiles).toEqual({ status: 'available' });
  });

  it.each([
    ['mode', 'unexpected-mode'],
    ['exerciseType', 'project'],
    ['readOnly', 'false'],
    ['allowLearnerCreatedFiles', 'true'],
    ['graphicsPanelVisible', 'true'],
  ])('fails closed on malformed %s instead of widening scope', (field, value) => {
    const input = request();
    Reflect.set(input, field, value);
    expect(() => resolveCodingRuntimeCapabilities(input)).toThrow(TypeError);
  });

  it('refuses an unknown runtime without inferring a WASI default', () => {
    const input = request();
    Reflect.set(input.execution.environment, 'runtime', 'unknown');
    expect(resolveCodingRuntimeCapabilities(input).program).toEqual(unavailable('unsupported-environment'));
  });

  it('performs no import/worker/network boot and leaves frozen input unchanged', () => {
    const fetch = vi.fn();
    const Worker = vi.fn();
    vi.stubGlobal('fetch', fetch);
    vi.stubGlobal('Worker', Worker);
    const input = request();
    const before = structuredClone(input);
    Object.freeze(input.execution.environment);
    Object.freeze(input.execution.toolchain);
    Object.freeze(input.execution);
    Object.freeze(input);
    resolveCodingRuntimeCapabilities(input);
    expect(input).toEqual(before);
    expect(fetch).not.toHaveBeenCalled();
    expect(Worker).not.toHaveBeenCalled();
  });

  it('returns independent data without mutable global operation state', () => {
    const first = resolveCodingRuntimeCapabilities(request());
    Reflect.set(first.program, 'status', 'unavailable');
    Reflect.set(first.tests, 'scope', 'full');
    expect(resolveCodingRuntimeCapabilities(request()).program).toEqual({ status: 'available' });
    expect(resolveCodingRuntimeCapabilities(request()).tests).toEqual({ status: 'available', scope: 'public' });
  });
});
