import { parseCodingAuthoringDraft, type CodingAuthoringDraft } from '@game-guild/coding';
import { describe, expect, expectTypeOf, it } from 'vitest';
import { CODING_AUTHORING_TASKS, type CodingAuthoringIdentityKind, type CodingAuthoringSurfaceProps } from './authoring-types';
import drafts from '../../coding/fixtures/coding-authoring-drafts.json';

describe('UX04-A authoring presentation contract', () => {
  it('uses the accepted author-only draft and a synchronous controlled edit event', () => {
    expectTypeOf<CodingAuthoringSurfaceProps['draft']>().toEqualTypeOf<CodingAuthoringDraft>();
    expectTypeOf<CodingAuthoringSurfaceProps['onChange']>().toEqualTypeOf<(draft: CodingAuthoringDraft) => void>();
    expectTypeOf<CodingAuthoringSurfaceProps['createId']>().toEqualTypeOf<(kind: CodingAuthoringIdentityKind) => string>();
    expectTypeOf<Extract<keyof CodingAuthoringSurfaceProps, 'api' | 'storage' | 'onSubmit' | 'onGrade' | 'runtime'>>().toEqualTypeOf<never>();
    expect(CODING_AUTHORING_TASKS).toEqual(['problem', 'environment', 'starter', 'tests', 'validate']);
  });

  it.each(['function', 'standard-io'] as const)('accepts incomplete %s data without inventing another persisted model', (family) => {
    const draft = parseCodingAuthoringDraft(drafts[family]);
    draft.problem.statement = '';
    draft.tests = [];
    draft.files = [];
    draft.entryPoint.path = '';
    draft.limits.timeLimitMs = '-';
    if (family === 'function') draft.functionSignature = null;
    const changes: CodingAuthoringDraft[] = [];
    const props: CodingAuthoringSurfaceProps = {
      contextKey: 'author:course:content',
      draft,
      createId: (kind) => `${kind}-1`,
      onChange: (next) => {
        changes.push(next);
      },
    };
    props.onChange(parseCodingAuthoringDraft(props.draft));
    expect(changes[0]).toEqual(draft);
    expect(changes[0]).not.toBe(draft);
  });
});
