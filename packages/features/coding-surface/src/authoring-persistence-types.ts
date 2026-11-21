import type { CodingAuthoringDraft } from '@game-guild/coding';

/** Local isolation identity supplied by the authenticated host, never an authorization grant. */
export interface CodingAuthoringScope {
  readonly tenantId: string;
  readonly actorId: string;
  readonly courseId: string;
  readonly contentId: string;
}

export interface CodingDraftSnapshot {
  readonly revision: number;
  readonly basePublishedVersion: number;
  readonly draft: CodingAuthoringDraft;
}

export type CodingDraftFailureReason = 'forbidden' | 'unavailable' | 'invalid-data';
export interface CodingDraftFailure {
  readonly status: 'error';
  readonly reason: CodingDraftFailureReason;
}

export type CodingDraftLoadResult = { readonly status: 'loaded'; readonly snapshot: CodingDraftSnapshot } | CodingDraftFailure;
export type CodingDraftSaveResult =
  | { readonly status: 'saved'; readonly snapshot: CodingDraftSnapshot }
  | { readonly status: 'conflict'; readonly currentRevision: number | null }
  | CodingDraftFailure;

/** The host binds authorized Learning actions and owns the surrounding lesson payload. */
export interface CodingDraftPersistencePort {
  readonly scope: CodingAuthoringScope;
  load(): Promise<CodingDraftLoadResult>;
  save(request: { readonly expectedRevision: number; readonly draft: CodingAuthoringDraft }): Promise<CodingDraftSaveResult>;
}

/** Never persist this author-only value in learner/browser storage or logs. */
export interface CodingDraftRecovery {
  readonly kind: 'coding-authoring-memory-recovery';
  readonly scope: CodingAuthoringScope;
  readonly snapshot: CodingDraftSnapshot;
}

export interface CodingDraftSessionState {
  readonly snapshot: CodingDraftSnapshot;
  readonly draft: CodingAuthoringDraft;
  readonly status: 'saved' | 'unsaved' | 'saving' | 'loading' | 'conflict' | 'error';
  readonly error: CodingDraftFailureReason | null;
  readonly currentRevision: number | null;
}

export interface CodingDraftSession {
  getState(): CodingDraftSessionState;
  edit(draft: CodingAuthoringDraft): void;
  save(): Promise<void>;
  reload(options: { readonly discardLocalChanges: true }): Promise<void>;
  checkpoint(): CodingDraftRecovery;
  restore(recovery: CodingDraftRecovery): void;
  dispose(): void;
}

export interface CodingDraftSessionFactory {
  (port: CodingDraftPersistencePort, snapshot: CodingDraftSnapshot): CodingDraftSession;
}
