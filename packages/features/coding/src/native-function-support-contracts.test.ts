import { expectTypeOf, it } from 'vitest';
import { composeCodingNativeFunctionSupport } from './native-function-support';
import type {
  CodingNativeFunctionSupportComposer,
  CodingNativeFunctionSupportFile,
  CodingNativeFunctionSupportRequest,
} from './native-function-support-types';

it('defines a pure native source-composition port, not a runtime or grading port', () => {
  expectTypeOf(composeCodingNativeFunctionSupport).toEqualTypeOf<CodingNativeFunctionSupportComposer>();
  expectTypeOf<ReturnType<CodingNativeFunctionSupportComposer>>().toEqualTypeOf<CodingNativeFunctionSupportFile>();
  expectTypeOf<Extract<CodingNativeFunctionSupportRequest, { language: 'cpp' }>['createCppHarness']>()
    .toEqualTypeOf<(bridgeSymbol: string) => string>();
  expectTypeOf<keyof Extract<CodingNativeFunctionSupportRequest, { language: 'c' }>>()
    .toEqualTypeOf<'language' | 'index' | 'test'>();
});
