import { CODING_TOOLCHAIN } from '@game-guild/coding';
import type { CreateEmceptionOptions, ParseManifestOptions } from '@gameguild/emception-browser';
import { describe, expect, expectTypeOf, it } from 'vitest';
import type {
  CodingRuntimeModule,
  CodingRuntimeSession,
  CodingRuntimeSessionOptions,
  CodingRuntimeSessionState,
  CodingRuntimeSessionFactory,
} from './runtime-types';

describe('coding runtime port contract', () => {
  it('exposes its factory through the owned public entry point', () => {
    expectTypeOf<typeof import('./index').createCodingRuntimeSession>().toEqualTypeOf<CodingRuntimeSessionFactory>();
  });
  it('uses only the existing public browser configuration and parser', () => {
    expectTypeOf<Parameters<CodingRuntimeModule['createEmception']>[0]>().toEqualTypeOf<CreateEmceptionOptions>();
    expectTypeOf<Parameters<CodingRuntimeModule['parseManifest']>[1]>().toEqualTypeOf<ParseManifestOptions | undefined>();
  });

  it('separates failed acquisition from a ready or disposed session', () => {
    const states: CodingRuntimeSessionState[] = [
      { status: 'idle' }, { status: 'loading' }, { status: 'ready' }, { status: 'disposed' },
      { status: 'error', reason: 'unsupported-toolchain' },
      { status: 'error', reason: 'incompatible-manifest' },
      { status: 'error', reason: 'runtime-unavailable' },
    ];
    expect(states.filter((state) => state.status === 'error').map((state) => state.reason)).toEqual([
      'unsupported-toolchain', 'incompatible-manifest', 'runtime-unavailable',
    ]);
    expectTypeOf<CodingRuntimeSession['getState']>().returns.toEqualTypeOf<CodingRuntimeSessionState>();
  });

  it('requires an explicit frozen identity without receiving teacher or academic data', () => {
    const options: CodingRuntimeSessionOptions = {
      toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
      manifestUrl: '/emception/manifest.json',
    };
    expect(Object.keys(options)).toEqual(['toolchain', 'manifestUrl']);
    expectTypeOf<keyof CodingRuntimeSession>().toEqualTypeOf<'getState' | 'acquire' | 'dispose' | 'whenDisposed'>();
    expectTypeOf<CodingRuntimeSession['whenDisposed']>().returns.toEqualTypeOf<Promise<void>>();
  });
});
