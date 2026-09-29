import { describe, expect, it } from 'vitest';

import {
  blogEditorReducer,
  buildAutosavePayload,
  createBlogEditorState,
  isPayloadEmpty,
  type BlogEditorDraft,
} from './editor-state';

const draft = (): BlogEditorDraft => ({
  title: 'Hello',
  content: '# hi',
  jsonBody: null,
  excerpt: '',
  tags: ['games'],
  metaTitle: '',
  metaDescription: '',
  ogImageUrl: '',
  canonicalUrlOverride: '',
  twitterCard: 'summary_large_image',
  structuredDataOverride: '',
  allowComments: true,
  format: 'Markdown',
});

const state = () => createBlogEditorState(draft(), 3);

describe('blogEditorReducer', () => {
  it('edit patches the draft and clears transient error state', () => {
    const errored = blogEditorReducer(state(), { type: 'error', message: 'boom' });
    const next = blogEditorReducer(errored, { type: 'edit', patch: { title: 'Hello!' } });

    expect(next.draft.title).toBe('Hello!');
    expect(next.status).toBe('saved');
    expect(next.lastError).toBeNull();
  });

  it('edit during conflict is a no-op until reload-latest resolves it', () => {
    const conflicted = blogEditorReducer(state(), {
      type: 'conflict',
      expectedRevision: 3,
      currentRevision: 5,
      message: 'stale',
    });

    const edited = blogEditorReducer(conflicted, { type: 'edit', patch: { title: 'Local' } });
    expect(edited.draft.title).toBe('Hello');
    expect(edited.status).toBe('conflict');
    expect(edited.conflict).toEqual({ expectedRevision: 3, currentRevision: 5 });

    const reloaded = blogEditorReducer(edited, {
      type: 'reload-latest',
      post: { revision: 5, draft: { ...draft(), title: 'Server wins' } },
    });
    expect(reloaded.status).toBe('saved');
    expect(reloaded.draft.title).toBe('Server wins');
    expect(reloaded.revision).toBe(5);
    expect(reloaded.conflict).toBeNull();
  });

  it('saved stores the returned revision and timestamp', () => {
    const next = blogEditorReducer(state(), { type: 'saved', revision: 4, savedAt: '2026-01-01T00:00:00Z' });

    expect(next.revision).toBe(4);
    expect(next.status).toBe('saved');
    expect(next.lastSavedAt).toBe('2026-01-01T00:00:00Z');
  });
});

describe('buildAutosavePayload', () => {
  it('omits untouched fields and includes only changed ones', () => {
    const payload = buildAutosavePayload({ ...draft(), title: 'Hello world', excerpt: 'new' }, draft());

    expect(payload).toEqual({ title: 'Hello world', excerpt: 'new' });
  });

  it('sends null-equivalents as explicit values so clearing a field persists', () => {
    const payload = buildAutosavePayload({ ...draft(), excerpt: '' }, { ...draft(), excerpt: 'old' });

    expect(payload).toEqual({ excerpt: '' });
  });

  it('detects jsonBody and tags changes by value', () => {
    const withJson = buildAutosavePayload({ ...draft(), jsonBody: '{"root":{}}' }, draft());
    expect(withJson).toEqual({ jsonBody: '{"root":{}}' });

    const withTags = buildAutosavePayload({ ...draft(), tags: ['games', 'dev'] }, draft());
    expect(withTags).toEqual({ tags: ['games', 'dev'] });
  });

  it('isPayloadEmpty guards no-op autosaves', () => {
    expect(isPayloadEmpty(buildAutosavePayload(draft(), draft()))).toBe(true);
    expect(isPayloadEmpty(buildAutosavePayload({ ...draft(), title: 'x' }, draft()))).toBe(false);
  });
});
