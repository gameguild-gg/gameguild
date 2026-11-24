import type { CodingAuthoringDraft, CodingLearnerDelivery } from '@game-guild/coding';
import { describe, expect, expectTypeOf, it } from 'vitest';
import type { CodingExerciseValidationOptions, CodingExerciseValidationSession, CodingSolutionCheck } from './exercise-validation-types';
import type { CodingSurfaceProps } from './types';
import type { CodingWorkbenchSession } from './workbench-session-types';
import type { CodingLearnerPreviewProps } from './authoring-learner-preview';

describe('UX04-B exercise preparation contracts', () => {
  it('binds local solution checks to an explicit draft and ownership barrier', () => {
    expectTypeOf<CodingExerciseValidationOptions['draft']>().toEqualTypeOf<CodingAuthoringDraft>();
    expectTypeOf<CodingExerciseValidationOptions['beforeExecution']>().toEqualTypeOf<(() => Promise<void>) | undefined>();
    expectTypeOf<CodingExerciseValidationSession['update']>().toEqualTypeOf<(draft: CodingAuthoringDraft) => Promise<void>>();
    expectTypeOf<Extract<keyof CodingSolutionCheck, 'score' | 'grade' | 'submission'>>().toEqualTypeOf<never>();
    expectTypeOf<Extract<keyof CodingExerciseValidationOptions, 'onSubmit' | 'onGrade' | 'storage'>>().toEqualTypeOf<never>();
    expectTypeOf<CodingSurfaceProps['onWorkbenchReady']>().toEqualTypeOf<((session: CodingWorkbenchSession) => void) | undefined>();
  });

  it('uses only the existing redacted learner contract for the solving composition', () => {
    type LearnerProps = Extract<CodingSurfaceProps, { mode: 'learner' }>;
    expectTypeOf<LearnerProps['definition']>().toEqualTypeOf<CodingLearnerDelivery>();
    expectTypeOf<CodingLearnerPreviewProps['delivery']>().toEqualTypeOf<CodingLearnerDelivery>();
    expectTypeOf<Extract<keyof CodingLearnerPreviewProps, 'draft' | 'solutions' | 'onSubmit' | 'storage'>>().toEqualTypeOf<never>();
    expectTypeOf<CodingSurfaceProps['beforeExecution']>().toEqualTypeOf<Promise<void> | undefined>();
    const error: CodingSolutionCheck = { solutionId: 'wrong', name: 'Wrong', role: 'incorrect', status: 'error', message: 'Compiler unavailable.' };
    expect(error.status).toBe('error');
    expect(error).not.toHaveProperty('report');
  });
});
