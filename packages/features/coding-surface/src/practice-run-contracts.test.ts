import { describe, expect, expectTypeOf, it } from 'vitest';
import type { CodingPracticeRunOptions, CodingPracticeRunRequest, CodingPracticeRunResult, CodingPracticeRunner } from './practice-run-types';
import type { CodingLocalExecutionOptions } from './local-execution';
import { runCodingPractice } from './practice-runner';
import type { CodingWorkbenchSession, CodingWorkbenchState } from './workbench-session-types';

describe('UX05-A2 practice execution contracts', () => {
  it('resolves public cases by identity, not caller-supplied expected answers', () => {
    const request = { contextKey: 'tenant.actor.revision.attempt', source: 'public', id: 'public:sample' } satisfies CodingPracticeRunRequest;
    expectTypeOf<keyof Extract<CodingPracticeRunRequest, { source: 'public' }>>().toEqualTypeOf<'contextKey' | 'source' | 'id'>();
    expect(request).not.toHaveProperty('expected');
  });

  it('has an observation state without a correctness boolean or estimate', () => {
    const observed = { status: 'observed' } satisfies CodingPracticeRunResult['comparison'];
    expect(observed.status).toBe('observed');
    expectTypeOf<keyof typeof observed>().toEqualTypeOf<'status'>();
    expectTypeOf<CodingPracticeRunResult['academic']>().toEqualTypeOf<false>();
    expectTypeOf<CodingPracticeRunner>().parameter(0).toEqualTypeOf<CodingPracticeRunOptions>();
    expectTypeOf<CodingPracticeRunner>().returns.toEqualTypeOf<Promise<CodingPracticeRunResult>>();
    expectTypeOf(runCodingPractice).toEqualTypeOf<CodingPracticeRunner>();
  });

  it('requires context and run identity without official submission or grading ports', () => {
    expectTypeOf<keyof CodingPracticeRunOptions>().toEqualTypeOf<
      'contextKey' | 'executionId' | 'delivery' | 'request' | 'manifestUrl' | 'signal' | 'createRuntime'
    >();
    expectTypeOf<CodingPracticeRunResult['selection']>().toEqualTypeOf<
      { readonly source: 'public'; readonly id: string } | { readonly source: 'personal'; readonly id: string }
    >();
  });

  it('shares a runtime lease without giving it an oracle, score or host command', () => {
    expectTypeOf<keyof CodingLocalExecutionOptions>().toEqualTypeOf<'definition' | 'manifestUrl' | 'signal' | 'createRuntime' | 'requireSuccessfulBuild'>();
  });

  it('adds practice to the existing exclusive owner, not a parallel execution service', () => {
    expectTypeOf<CodingWorkbenchSession['runPractice']>().toEqualTypeOf<(request: CodingPracticeRunRequest) => Promise<CodingPracticeRunResult>>();
    expectTypeOf<CodingWorkbenchSession['invalidatePractice']>().toEqualTypeOf<() => Promise<void>>();
    expectTypeOf<Extract<CodingWorkbenchState['practice'], { status: 'completed' }>['stale']>().toEqualTypeOf<boolean>();
  });
});
