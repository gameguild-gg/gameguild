import { CODING_TOOLCHAIN } from '@game-guild/coding';
import type {
  CodingCapabilityUnavailableReason,
  CodingLocalCapability,
  CodingRuntimeCapabilityRequest,
  CodingRuntimeCapabilityResolver,
} from './runtime-capability-types';

export const resolveCodingRuntimeCapabilities: CodingRuntimeCapabilityResolver = request => {
  assertKnownContext(request);
  const { environment } = request.execution;
  const requiresGraphics = environment.runtime === 'sdl3'
    || environment.runtime === 'raylib' || environment.runtime === 'allegro5';
  const graphicsTarget = { required: requiresGraphics, visible: requiresGraphics && request.graphicsPanelVisible };
  const createFiles = fileCreation(request);
  const blocked = executionBlocker(request);
  if (blocked) {
    return {
      program: unavailable(blocked), tests: unavailable(blocked),
      interactiveInput: unavailable(blocked), graphics: unavailable(blocked),
      createFiles, graphicsTarget,
    };
  }

  return {
    program: available(),
    tests: request.exerciseType === 'function'
      ? unavailable('function-abi-unavailable')
      : requiresGraphics
        ? unavailable('graphics-tests-unavailable')
        : { status: 'available', scope: request.mode === 'learner' ? 'public' : 'full' },
    interactiveInput: environment.runtime === 'wasi' && environment.language === 'cpp'
      ? available() : unavailable('interactive-input-unverified'),
    graphics: requiresGraphics ? available() : unavailable('graphics-not-required'),
    createFiles,
    graphicsTarget,
  };
};

function assertKnownContext(request: CodingRuntimeCapabilityRequest): void {
  if (request.mode !== 'author' && request.mode !== 'author-test'
    && request.mode !== 'learner' && request.mode !== 'grader') {
    throw new TypeError('Unknown coding surface mode.');
  }
  if (request.exerciseType !== 'function' && request.exerciseType !== 'standard-io') {
    throw new TypeError('Unknown coding exercise family.');
  }
  if (typeof request.readOnly !== 'boolean' || typeof request.allowLearnerCreatedFiles !== 'boolean'
    || typeof request.graphicsPanelVisible !== 'boolean') {
    throw new TypeError('Coding read-only restriction, file policy and panel visibility must be explicit booleans.');
  }
}

function executionBlocker(request: CodingRuntimeCapabilityRequest): CodingCapabilityUnavailableReason | undefined {
  if (request.mode === 'author') return 'authoring-only';
  const { environment, toolchain } = request.execution;
  if (toolchain.key !== `${environment.language}-emception`
    || toolchain.version !== CODING_TOOLCHAIN.version
    || toolchain.runtimeAbi !== CODING_TOOLCHAIN.runtimeAbi
    || toolchain.artifactDigest !== CODING_TOOLCHAIN.artifactDigest) return 'incompatible-toolchain';
  if ((environment.language !== 'c' && environment.language !== 'cpp')
    || (environment.runtime !== 'wasi' && environment.runtime !== 'sdl3'
      && environment.runtime !== 'raylib' && environment.runtime !== 'allegro5')
    || (environment.language === 'c' && environment.runtime !== 'wasi')) return 'unsupported-environment';
  // Public exports/presets alone are not evidence for enabling an execution path.
  if (environment.runtime === 'raylib' || environment.runtime === 'allegro5') return 'runtime-unverified';
  return undefined;
}

function fileCreation(request: CodingRuntimeCapabilityRequest): CodingLocalCapability {
  if (request.readOnly) return unavailable('read-only-context');
  if (request.mode === 'author') return available();
  if (request.mode !== 'learner') return unavailable('read-only-context');
  return request.allowLearnerCreatedFiles ? available() : unavailable('assignment-disallows-files');
}

function available(): CodingLocalCapability {
  return { status: 'available' };
}

function unavailable(reason: CodingCapabilityUnavailableReason): Extract<CodingLocalCapability, { status: 'unavailable' }> {
  return { status: 'unavailable', reason };
}
