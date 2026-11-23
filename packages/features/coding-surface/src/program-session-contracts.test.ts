import type { CodingLearnerDelivery } from '@game-guild/coding';
import { describe, expectTypeOf, it } from 'vitest';
import type { CodingProgramInput, CodingProgramSession, CodingProgramSessionFactory, CodingProgramSessionOptions, CodingProgramSessionState } from './program-session-types';
import type { CodingRuntimeSessionFactory } from './runtime-types';

describe('owned text execution contract', () => {
  it('accepts only a public candidate and existing lazy runtime port', () => {
    expectTypeOf<typeof import('./index').createCodingProgramSession>().toEqualTypeOf<CodingProgramSessionFactory>();
    expectTypeOf<CodingProgramSessionFactory>().parameter(0).toEqualTypeOf<CodingProgramSessionOptions>();
    expectTypeOf<CodingProgramSessionOptions['delivery']>().toEqualTypeOf<CodingLearnerDelivery>();
    expectTypeOf<CodingProgramSessionOptions['createRuntime']>().toEqualTypeOf<CodingRuntimeSessionFactory | undefined>();
    expectTypeOf<keyof CodingProgramSessionOptions>().toEqualTypeOf<'candidateKey' | 'delivery' | 'manifestUrl' | 'createRuntime'>();
  });

  it('separates finite batch input from interactive input and teardown', () => {
    expectTypeOf<CodingProgramInput>().toEqualTypeOf<{ readonly kind: 'batch'; readonly text: string } | { readonly kind: 'interactive' }>();
    expectTypeOf<CodingProgramSession['run']>().parameter(0).toEqualTypeOf<CodingProgramInput>();
    expectTypeOf<CodingProgramSession['run']>().returns.toEqualTypeOf<Promise<void>>();
    expectTypeOf<CodingProgramSession['stop']>().returns.toEqualTypeOf<Promise<void>>();
    expectTypeOf<CodingProgramSession['dispose']>().returns.toEqualTypeOf<Promise<void>>();
    expectTypeOf<keyof CodingProgramSession>().toEqualTypeOf<'getState' | 'run' | 'writeInput' | 'endInput' | 'stop' | 'dispose'>();
  });

  it('represents input demand and exit status without any academic verdict', () => {
    expectTypeOf<keyof CodingProgramSessionState>().toEqualTypeOf<'candidateKey' | 'output' | 'waitingForInput' | 'status'>();
    expectTypeOf<Extract<CodingProgramSessionState, { status: 'completed' }>['exitCode']>().toEqualTypeOf<number>();
    expectTypeOf<Extract<CodingProgramSessionState, { status: 'failed' }>['reason']>().not.toEqualTypeOf<string>();
  });
});
