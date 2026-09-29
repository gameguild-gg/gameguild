import type { BlogContentFormat } from './types';

/**
 * Editable surface of a blog post as held by the editor. Mirrors the fields
 * `UpdateBlogPostDraftInput` can touch; `format` is locked after create and
 * never sent for update.
 */
export interface BlogEditorDraft {
  title: string;
  content: string;
  jsonBody: string | null;
  excerpt: string;
  tags: string[];
  metaTitle: string;
  metaDescription: string;
  ogImageUrl: string;
  canonicalUrlOverride: string;
  twitterCard: string;
  structuredDataOverride: string;
  allowComments: boolean;
  format: BlogContentFormat;
}

export interface BlogEditorState {
  draft: BlogEditorDraft;
  revision: number;
  status: 'saved' | 'saving' | 'error' | 'conflict';
  lastSavedAt: string | null;
  lastError: string | null;
  conflict: { expectedRevision: number; currentRevision: number } | null;
}

export type BlogEditorAction =
  | { type: 'edit'; patch: Partial<Omit<BlogEditorDraft, 'format'>> }
  | { type: 'saving' }
  | { type: 'saved'; revision: number; savedAt: string }
  | { type: 'error'; message: string }
  | {
      type: 'conflict';
      expectedRevision: number;
      currentRevision: number;
      message: string;
    }
  | { type: 'reload-latest'; post: { revision: number; draft: BlogEditorDraft } };

export function createBlogEditorState(
  initial: BlogEditorDraft,
  revision: number,
): BlogEditorState {
  return {
    draft: initial,
    revision,
    status: 'saved',
    lastSavedAt: null,
    lastError: null,
    conflict: null,
  };
}

export function blogEditorReducer(state: BlogEditorState, action: BlogEditorAction): BlogEditorState {
  switch (action.type) {
    case 'edit':
      if (state.status === 'conflict') return state;
      return {
        ...state,
        status: state.status === 'error' ? 'saved' : state.status,
        draft: { ...state.draft, ...action.patch },
        conflict: null,
        lastError: null,
      };
    case 'saving':
      return { ...state, status: 'saving', lastError: null };
    case 'saved':
      return { ...state, revision: action.revision, status: 'saved', lastSavedAt: action.savedAt, lastError: null, conflict: null };
    case 'error':
      return { ...state, status: 'error', lastError: action.message };
    case 'conflict':
      return {
        ...state,
        status: 'conflict',
        conflict: { expectedRevision: action.expectedRevision, currentRevision: action.currentRevision },
        lastError: action.message,
      };
    case 'reload-latest':
      return {
        draft: action.post.draft,
        revision: action.post.revision,
        status: 'saved',
        lastSavedAt: state.lastSavedAt,
        lastError: null,
        conflict: null,
      };
  }
}

const UNTOUCHED = Symbol('untouched');

function changed<T>(current: T, initial: T): T | typeof UNTOUCHED {
  return current === initial ? UNTOUCHED : current;
}

/**
 * Shape the autosave payload per `UpdateBlogPostDraftCommand` semantics:
 * `null` = untouched (field left as-is server-side), a value = set.
 * Fields equal to their last-saved values are omitted entirely.
 */
export function buildAutosavePayload(
  draft: BlogEditorDraft,
  baseline: BlogEditorDraft,
): Partial<Record<string, unknown>> {
  const payload: Record<string, unknown> = {};

  const stringFields = [
    'title',
    'content',
    'excerpt',
    'metaTitle',
    'metaDescription',
    'ogImageUrl',
    'canonicalUrlOverride',
    'twitterCard',
    'structuredDataOverride',
  ] as const;
  for (const field of stringFields) {
    const value = changed(draft[field], baseline[field]);
    if (value !== UNTOUCHED) payload[field] = value;
  }

  if (changed(draft.jsonBody, baseline.jsonBody) !== UNTOUCHED) {
    payload.jsonBody = draft.jsonBody;
  }
  if (changed(draft.allowComments, baseline.allowComments) !== UNTOUCHED) {
    payload.allowComments = draft.allowComments;
  }
  if (JSON.stringify(draft.tags) !== JSON.stringify(baseline.tags)) {
    payload.tags = draft.tags;
  }

  return payload;
}

export function isPayloadEmpty(payload: Partial<Record<string, unknown>>): boolean {
  return Object.keys(payload).length === 0;
}
