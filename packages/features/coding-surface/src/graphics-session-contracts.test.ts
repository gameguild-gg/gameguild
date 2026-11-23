import type { CodingLearnerDelivery } from '@game-guild/coding';
import { describe, expectTypeOf, it } from 'vitest';
import type {
  CodingGraphicsSession,
  CodingGraphicsSessionFactory,
  CodingGraphicsSessionOptions,
  CodingGraphicsSessionState,
} from './graphics-session-types';
import type { CodingRuntimeSessionFactory } from './runtime-types';

describe('local graphics contract', () => {
  it('exports an owned factory with public runtime and redacted delivery ports', () => {
    expectTypeOf<typeof import('./index').createCodingGraphicsSession>().toEqualTypeOf<CodingGraphicsSessionFactory>();
    expectTypeOf<CodingGraphicsSessionOptions['delivery']>().toEqualTypeOf<CodingLearnerDelivery>();
    expectTypeOf<CodingGraphicsSessionOptions['createRuntime']>().toEqualTypeOf<CodingRuntimeSessionFactory | undefined>();
    expectTypeOf<keyof CodingGraphicsSessionOptions>().toEqualTypeOf<'candidateKey' | 'delivery' | 'manifestUrl' | 'createRuntime'>();
  });

  it('acknowledges cleanup independently of a diagnostic run', () => {
    expectTypeOf<CodingGraphicsSession['run']>().parameter(0).toEqualTypeOf<HTMLCanvasElement>();
    expectTypeOf<CodingGraphicsSession['run']>().returns.toEqualTypeOf<Promise<void>>();
    expectTypeOf<CodingGraphicsSession['stop']>().returns.toEqualTypeOf<Promise<void>>();
    expectTypeOf<CodingGraphicsSession['dispose']>().returns.toEqualTypeOf<Promise<void>>();
    expectTypeOf<keyof CodingGraphicsSession>().toEqualTypeOf<'getState' | 'run' | 'stop' | 'dispose'>();
  });

  it('has no successful verdict/score and requires a reason on failure', () => {
    expectTypeOf<keyof CodingGraphicsSessionState>().toEqualTypeOf<'status' | 'candidateKey' | 'output'>();
    expectTypeOf<Extract<CodingGraphicsSessionState, { status: 'failed' }>['reason']>().toEqualTypeOf<
      'unsupported-context' | 'runtime-unavailable' | 'compile-failed' | 'link-failed' | 'preparation-timeout'
      | 'execution-timeout' | 'output-limit' | 'runtime-failed' | 'cleanup-failed'
    >();
    expectTypeOf<CodingGraphicsSessionState['output']>().toEqualTypeOf<{
      readonly stdout: string; readonly stderr: string; readonly truncated: boolean;
    }>();
  });
});
