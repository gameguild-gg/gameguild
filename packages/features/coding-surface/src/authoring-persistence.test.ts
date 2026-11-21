import { codingDraftOperations, parseCodingAuthoringDraft } from '@game-guild/coding';
import { codingContractFixture } from '@game-guild/coding/testing';
import sharedDrafts from '../../coding/fixtures/coding-authoring-drafts.json';
import { describe, expect, it, vi } from 'vitest';
import { createCodingDraftSession } from './authoring-persistence';
import type {
  CodingAuthoringScope,
  CodingDraftPersistencePort,
  CodingDraftSaveResult,
  CodingDraftSession,
  CodingDraftSnapshot,
} from './authoring-persistence-types';

const scope: CodingAuthoringScope = { tenantId: 'tenant', actorId: 'teacher', courseId: 'course', contentId: 'content' };

function snapshot(): CodingDraftSnapshot {
  const draft = codingDraftOperations.fromDefinition(codingContractFixture.functionAuthoring, { language: 'cpp', runtime: 'wasi' });
  draft.problem.statement = 'Incomplete exercise';
  draft.functionSignature = null;
  draft.validationSolutions = [{ id: 'reference', name: '', role: 'reference', files: [{ fileId: 'main', path: '', content: 'PRIVATE-REFERENCE' }] }];
  return { revision: 4, basePublishedVersion: 2, draft };
}

function setup() {
  const initial = snapshot();
  const save = vi.fn<CodingDraftPersistencePort['save']>().mockImplementation(async (request) => ({
    status: 'saved',
    snapshot: { ...initial, revision: request.expectedRevision + 1, draft: structuredClone(request.draft) },
  }));
  const load = vi.fn<CodingDraftPersistencePort['load']>().mockResolvedValue({ status: 'loaded', snapshot: initial });
  const port: CodingDraftPersistencePort = { scope, save, load };
  const session = createCodingDraftSession(port, initial);
  return { session, port, save, load, initial };
}

function edit(session: CodingDraftSession, statement = 'Edited incomplete exercise') {
  const draft = session.getState().draft;
  draft.problem.statement = statement;
  session.edit(draft);
  return draft;
}

function deferred<T>() {
  let complete: (value: T) => void = () => {
    throw new Error('Deferred promise was not initialized.');
  };
  const promise = new Promise<T>((resolve) => {
    complete = resolve;
  });
  return { promise, resolve: (value: T) => complete(value) };
}

describe('author-only coding draft persistence', () => {
  it.each(['function', 'standard-io'] as const)('[CS-REF-07/22] preserves the shared %s backend fixture through save and reopen', async (family) => {
    const draft = parseCodingAuthoringDraft(sharedDrafts[family]);
    let stored: CodingDraftSnapshot = { revision: 4, basePublishedVersion: 2, draft };
    const port: CodingDraftPersistencePort = {
      scope,
      load: async () => ({ status: 'loaded', snapshot: structuredClone(stored) }),
      save: async (request) => {
        stored = { ...stored, revision: request.expectedRevision + 1, draft: structuredClone(request.draft) };
        return { status: 'saved', snapshot: structuredClone(stored) };
      },
    };
    const session = createCodingDraftSession(port, stored);
    draft.problem.statement += ' Still being edited.';
    session.edit(draft);
    await session.save();
    const reopened = await port.load();
    expect(reopened).toEqual({ status: 'loaded', snapshot: { revision: 5, basePublishedVersion: 2, draft } });
    expect(session.getState()).toMatchObject({ status: 'saved', draft });
  });

  it('clones initial state and edits without saving, runtime boot or browser storage', () => {
    const storage = vi.spyOn(Storage.prototype, 'setItem');
    try {
      const { session, initial, save, load } = setup();
      initial.draft.problem.statement = 'external mutation';
      expect(session.getState().draft.problem.statement).toBe('Incomplete exercise');
      const edited = edit(session);
      edited.problem.statement = 'another external mutation';
      const state = session.getState();
      state.draft.validationSolutions.length = 0;
      expect(session.getState()).toMatchObject({ status: 'unsaved', error: null, snapshot: { revision: 4 } });
      expect(session.getState().draft.validationSolutions[0].files[0].content).toBe('PRIVATE-REFERENCE');
      expect(save).not.toHaveBeenCalled();
      expect(load).not.toHaveBeenCalled();
      expect(storage).not.toHaveBeenCalled();
    } finally {
      storage.mockRestore();
    }
  });

  it('displays saving until the exact acknowledged write, then retains new edits made in flight', async () => {
    const { session, save } = setup();
    const pending = deferred<CodingDraftSaveResult>();
    save.mockReturnValue(pending.promise);
    const sent = edit(session, 'Sent draft');
    const first = session.save();
    const duplicate = session.save();
    expect(session.getState().status).toBe('saving');
    expect(save).toHaveBeenCalledExactlyOnceWith({ expectedRevision: 4, draft: sent });
    edit(session, 'Newer local draft');
    pending.resolve({ status: 'saved', snapshot: { revision: 5, basePublishedVersion: 2, draft: sent } });
    await first;
    await duplicate;
    expect(session.getState()).toMatchObject({
      status: 'unsaved',
      snapshot: { revision: 5, draft: { problem: { statement: 'Sent draft' } } },
      draft: { problem: { statement: 'Newer local draft' } },
    });
  });

  it('roundtrips incomplete and protected data and uses the next acknowledged revision', async () => {
    const { session, save } = setup();
    const draft = edit(session);
    draft.limits.timeLimitMs = 'not a number';
    session.edit(draft);
    await session.save();
    expect(session.getState()).toMatchObject({ status: 'saved', snapshot: { revision: 5 }, draft });
    edit(session, 'Second save');
    await session.save();
    expect(save.mock.calls[1][0].expectedRevision).toBe(5);
    expect(session.getState().snapshot.revision).toBe(6);
  });

  it.each(['forbidden', 'unavailable', 'invalid-data'] as const)('preserves unsaved work on %s', async (reason) => {
    const { session, save } = setup();
    const draft = edit(session);
    save.mockResolvedValue({ status: 'error', reason });
    await session.save();
    expect(session.getState()).toMatchObject({ status: 'error', error: reason, snapshot: { revision: 4 }, draft });
    expect(session.checkpoint().snapshot.draft).toEqual(draft);
  });

  it('contains transport rejection without exposing the rejected private payload as error text', async () => {
    const { session, save } = setup();
    const draft = edit(session);
    save.mockRejectedValue(new Error('PRIVATE-REFERENCE'));
    await session.save();
    expect(session.getState()).toMatchObject({ status: 'error', error: 'unavailable', draft });
  });

  it('locks a conflict against automatic retries and retains subsequent local edits', async () => {
    const { session, save } = setup();
    edit(session);
    save.mockResolvedValue({ status: 'conflict', currentRevision: 9 });
    await session.save();
    edit(session, 'Preserved after conflict');
    await session.save();
    expect(save).toHaveBeenCalledTimes(1);
    expect(session.getState()).toMatchObject({
      status: 'conflict',
      currentRevision: 9,
      snapshot: { revision: 4 },
      draft: { problem: { statement: 'Preserved after conflict' } },
    });
  });

  it('keeps the conflict lock when loading the other author revision fails', async () => {
    const { session, save, load } = setup();
    edit(session);
    save.mockResolvedValue({ status: 'conflict', currentRevision: 9 });
    await session.save();
    load.mockResolvedValue({ status: 'error', reason: 'unavailable' });
    await session.reload({ discardLocalChanges: true });
    await session.save();
    expect(save).toHaveBeenCalledTimes(1);
    expect(session.getState()).toMatchObject({ status: 'error', error: 'unavailable', currentRevision: 9, snapshot: { revision: 4 } });
  });

  it.each(['revision', 'publication', 'body'] as const)('rejects a non-matching %s acknowledgement', async (invalid) => {
    const { session, save, initial } = setup();
    const draft = edit(session);
    const ack = { ...initial, revision: 5, draft: structuredClone(draft) };
    if (invalid === 'revision') ack.revision = 7;
    if (invalid === 'publication') ack.basePublishedVersion = 3;
    if (invalid === 'body') ack.draft.problem.statement = 'Not the sent body';
    save.mockResolvedValue({ status: 'saved', snapshot: ack });
    await session.save();
    expect(session.getState()).toMatchObject({ status: 'error', error: 'invalid-data', snapshot: { revision: 4 }, draft });
  });

  it('recovers an independent memory checkpoint only for its original owner and revisions', () => {
    const { session, port, initial } = setup();
    const draft = edit(session);
    const recovery = session.checkpoint();
    const reopened = createCodingDraftSession(port, initial);
    reopened.restore(recovery);
    recovery.snapshot.draft.validationSolutions.length = 0;
    expect(reopened.getState()).toMatchObject({ status: 'unsaved', draft });
    expect(reopened.getState().snapshot.draft).toEqual(initial.draft);
    expect(reopened.checkpoint().kind).toBe('coding-authoring-memory-recovery');
  });

  it('allows recovery callbacks without depending on a mutable this receiver', () => {
    const { session, port, initial } = setup();
    const draft = edit(session);
    const reopened = createCodingDraftSession(port, initial);
    const restore = reopened.restore;
    restore(session.checkpoint());
    expect(reopened.getState().draft).toEqual(draft);
  });

  it.each(['tenantId', 'actorId', 'courseId', 'contentId'] as const)('denies recovery across %s', (field) => {
    const { session, initial, port } = setup();
    edit(session);
    const otherScope = { ...scope, [field]: 'other' };
    const other = createCodingDraftSession({ ...port, scope: otherScope }, initial);
    expect(() => other.restore(session.checkpoint())).toThrow(/scope/i);
    expect(other.getState()).toMatchObject({ status: 'saved', draft: initial.draft });
  });

  it.each(['revision', 'basePublishedVersion'] as const)('denies stale %s recovery without losing the current draft', (field) => {
    const { session, initial, port } = setup();
    edit(session);
    const other = createCodingDraftSession(port, { ...initial, [field]: initial[field] + 1 });
    expect(() => other.restore(session.checkpoint())).toThrow(/revision/i);
    expect(other.getState().draft).toEqual(initial.draft);
  });

  it('reopens acknowledged server data only after explicit discard, preserving a separate checkpoint', async () => {
    const { session, initial, load } = setup();
    const draft = edit(session);
    const backup = session.checkpoint();
    const latest = { ...structuredClone(initial), revision: 9 };
    latest.draft.problem.statement = 'Other teacher edit';
    load.mockResolvedValue({ status: 'loaded', snapshot: latest });
    await session.reload({ discardLocalChanges: true });
    expect(session.getState()).toMatchObject({ status: 'saved', snapshot: latest, draft: latest.draft });
    expect(backup.snapshot.draft).toEqual(draft);
    expect(() => session.restore(backup)).toThrow(/revision/i);
  });

  it('retains work if reload fails, is stale, or new edits occur during the load', async () => {
    const { session, load, initial } = setup();
    const draft = edit(session);
    load.mockResolvedValueOnce({ status: 'error', reason: 'forbidden' });
    await session.reload({ discardLocalChanges: true });
    expect(session.getState()).toMatchObject({ status: 'error', error: 'forbidden', draft });
    load.mockResolvedValueOnce({ status: 'loaded', snapshot: { ...initial, revision: 3 } });
    await session.reload({ discardLocalChanges: true });
    expect(session.getState()).toMatchObject({ status: 'error', error: 'invalid-data', draft });
    const pending = deferred<Awaited<ReturnType<CodingDraftPersistencePort['load']>>>();
    load.mockReturnValueOnce(pending.promise);
    const loading = session.reload({ discardLocalChanges: true });
    expect(session.getState().status).toBe('loading');
    edit(session, 'New edit while loading');
    pending.resolve({ status: 'loaded', snapshot: { ...initial, revision: 9 } });
    await loading;
    expect(session.getState()).toMatchObject({
      status: 'conflict',
      currentRevision: 9,
      snapshot: { revision: 4 },
      draft: { problem: { statement: 'New edit while loading' } },
    });
  });

  it('rejects a changed server body with an unchanged revision and classifies malformed acks as invalid data', async () => {
    const { session, save, load, initial } = setup();
    const local = edit(session);
    const altered = structuredClone(initial);
    altered.draft.problem.statement = 'Changed without a revision';
    load.mockResolvedValue({ status: 'loaded', snapshot: altered });
    await session.reload({ discardLocalChanges: true });
    expect(session.getState()).toMatchObject({ status: 'error', error: 'invalid-data', draft: local });
    save.mockResolvedValue({ status: 'saved', snapshot: { ...initial, revision: Number.NaN } });
    await session.save();
    expect(session.getState()).toMatchObject({ status: 'error', error: 'invalid-data', snapshot: { revision: 4 }, draft: local });
  });

  it('does not revive a disposed actor session with a late write acknowledgement', async () => {
    const { session, save, initial } = setup();
    const pending = deferred<CodingDraftSaveResult>();
    save.mockReturnValueOnce(pending.promise);
    const draft = edit(session);
    const saving = session.save();
    session.dispose();
    pending.resolve({ status: 'saved', snapshot: { ...initial, revision: 5, draft } });
    await saving;
    expect(() => session.getState()).toThrow(/closed/i);
    expect(() => session.checkpoint()).toThrow(/closed/i);
    expect(() => session.edit(draft)).toThrow(/closed/i);
    expect(() => session.restore({ kind: 'coding-authoring-memory-recovery', scope, snapshot: initial })).toThrow(/closed/i);
  });

  it('rejects malformed initial scopes and snapshot revisions before any host operation', () => {
    const { port, initial } = setup();
    expect(() => createCodingDraftSession({ ...port, scope: { ...scope, actorId: ' ' } }, initial)).toThrow(/scope/i);
    expect(() => createCodingDraftSession(port, { ...initial, revision: Number.NaN })).toThrow(/revision/i);
    expect(() => createCodingDraftSession(port, { ...initial, basePublishedVersion: -1 })).toThrow(/revision/i);
    expect(() => parseCodingAuthoringDraft({ ...initial.draft, score: 100 })).toThrow();
  });
});
