import { expectTypeOf, it } from 'vitest';
import type { CodingSolvingLayoutProps, CodingSolvingView } from './solving-layout';
import type { CodingPracticeResultsProps } from './practice-results';
import type { CodingPracticeExecutionState } from './practice-run-types';

it('defines shared controlled views without persistence, runtime or host ports', () => {
  expectTypeOf<keyof CodingSolvingLayoutProps>().toEqualTypeOf<'problem' | 'editor' | 'cases' | 'results' | 'view' | 'onViewChange'>();
  expectTypeOf<CodingSolvingView>().toEqualTypeOf<'problem' | 'code' | 'cases' | 'results'>();
  expectTypeOf<keyof CodingPracticeResultsProps>().toEqualTypeOf<'state'>();
  expectTypeOf<CodingPracticeResultsProps['state']>().toEqualTypeOf<CodingPracticeExecutionState>();
});
