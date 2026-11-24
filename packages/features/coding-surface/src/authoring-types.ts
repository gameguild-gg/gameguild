import type { CodingAuthoringDraft } from '@game-guild/coding';

export const CODING_AUTHORING_TASKS = ['problem', 'environment', 'starter', 'tests'] as const;
export type CodingAuthoringTask = (typeof CODING_AUTHORING_TASKS)[number];
export type CodingAuthoringIdentityKind = 'example' | 'file' | 'test' | 'case';

/** The host supplies draft ownership and stable identities; editing has no runtime port. */
export interface CodingAuthoringSurfaceProps {
  readonly contextKey: string;
  readonly draft: CodingAuthoringDraft;
  readonly createId: (kind: CodingAuthoringIdentityKind) => string;
  readonly onChange: (draft: CodingAuthoringDraft) => void;
  readonly initialTask?: CodingAuthoringTask;
  readonly disabled?: boolean;
}
