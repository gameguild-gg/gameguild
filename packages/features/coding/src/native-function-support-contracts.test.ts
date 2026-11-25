import { expectTypeOf, it } from 'vitest';
import { composeCodingNativeFunctionObservation, composeCodingNativeFunctionSupport } from './native-function-support';
import type {
  CodingNativeFunctionSupportComposer,
  CodingNativeFunctionSupportFile,
  CodingNativeFunctionSupportRequest,
  CodingNativeFunctionObservationComposer,
  CodingNativeFunctionObservationRequest,
  CodingNativeFunctionObservationFile,
} from './native-function-support-types';

it('defines a pure native source-composition port, not a runtime or grading port', () => {
  expectTypeOf(composeCodingNativeFunctionSupport).toEqualTypeOf<CodingNativeFunctionSupportComposer>();
  expectTypeOf<ReturnType<CodingNativeFunctionSupportComposer>>().toEqualTypeOf<CodingNativeFunctionSupportFile>();
  expectTypeOf<Extract<CodingNativeFunctionSupportRequest, { language: 'cpp' }>['createCppHarness']>().toEqualTypeOf<(bridgeSymbol: string) => string>();
  expectTypeOf<keyof Extract<CodingNativeFunctionSupportRequest, { language: 'c' }>>().toEqualTypeOf<'language' | 'index' | 'test'>();
});

it('defines observation without a fabricated expected answer or runtime dependency', () => {
  expectTypeOf(composeCodingNativeFunctionObservation).toEqualTypeOf<CodingNativeFunctionObservationComposer>();
  expectTypeOf<keyof CodingNativeFunctionObservationRequest>().toEqualTypeOf<'language' | 'signature' | 'inputs' | 'outputLimitBytes' | 'recordId'>();
  expectTypeOf<ReturnType<CodingNativeFunctionObservationComposer>>().toEqualTypeOf<CodingNativeFunctionObservationFile>();
});
