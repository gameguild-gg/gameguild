import type { CodingExecutionConfiguration, CodingExerciseType } from '@game-guild/coding';
import { describe, expectTypeOf, it } from 'vitest';
import type {
  CodingCapabilityUnavailableReason,
  CodingLocalCapability,
  CodingLocalTestCapability,
  CodingRuntimeCapabilities,
  CodingRuntimeCapabilityRequest,
  CodingRuntimeCapabilityResolver,
} from './runtime-capability-types';
import type { CodingSurfaceMode } from './types';

describe('coding runtime capability contract', () => {
  it('exposes the resolver through the owned public entry point', () => {
    expectTypeOf<typeof import('./index').resolveCodingRuntimeCapabilities>().toEqualTypeOf<CodingRuntimeCapabilityResolver>();
  });

  it('uses existing family, frozen environment and host-authorized mode contracts', () => {
    expectTypeOf<CodingRuntimeCapabilityRequest['exerciseType']>().toEqualTypeOf<CodingExerciseType>();
    expectTypeOf<CodingRuntimeCapabilityRequest['execution']>().toEqualTypeOf<Pick<CodingExecutionConfiguration, 'environment' | 'toolchain'>>();
    expectTypeOf<CodingRuntimeCapabilityRequest['mode']>().toEqualTypeOf<CodingSurfaceMode>();
    expectTypeOf<Parameters<CodingRuntimeCapabilityResolver>>().toEqualTypeOf<[CodingRuntimeCapabilityRequest]>();
    expectTypeOf<ReturnType<CodingRuntimeCapabilityResolver>>().toEqualTypeOf<CodingRuntimeCapabilities>();
  });

  it('requires a reason for unavailable operations and a scope for available tests', () => {
    expectTypeOf<Extract<CodingLocalCapability, { status: 'unavailable' }>['reason']>().toEqualTypeOf<CodingCapabilityUnavailableReason>();
    expectTypeOf<Extract<CodingLocalTestCapability, { status: 'available' }>['scope']>().toEqualTypeOf<'public' | 'full'>();
    expectTypeOf<keyof Extract<CodingLocalTestCapability, { status: 'unavailable' }>>().toEqualTypeOf<'status' | 'reason'>();
  });

  it('receives no cases, private references, credentials or academic command ports', () => {
    expectTypeOf<keyof CodingRuntimeCapabilityRequest>().toEqualTypeOf<
      'exerciseType' | 'execution' | 'mode' | 'readOnly' | 'allowLearnerCreatedFiles' | 'graphicsPanelVisible'
    >();
    expectTypeOf<keyof CodingRuntimeCapabilities>().toEqualTypeOf<
      'program' | 'tests' | 'interactiveInput' | 'graphics' | 'createFiles' | 'graphicsTarget'
    >();
  });
});
