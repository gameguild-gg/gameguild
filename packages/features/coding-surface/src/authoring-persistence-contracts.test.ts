import { codingContractFixture } from '@game-guild/coding/testing';
import { codingDraftOperations } from '@game-guild/coding';
import { describe, expect, expectTypeOf, it } from 'vitest';
import type {
  CodingAuthoringScope,
  CodingDraftLoadResult,
  CodingDraftPersistencePort,
  CodingDraftRecovery,
  CodingDraftSaveResult,
  CodingDraftSessionState,
  CodingDraftSessionFactory,
  CodingDraftSnapshot,
} from './authoring-persistence-types';

describe('coding draft persistence port contract', () => {
  it('implements the public session factory contract', () => {
    expectTypeOf<typeof import('./index').createCodingDraftSession>().toEqualTypeOf<CodingDraftSessionFactory>();
  });

  it('represents incomplete author-only saves without an executable definition or runtime', async () => {
    const scope: CodingAuthoringScope = { tenantId: 'tenant', actorId: 'teacher', courseId: 'course', contentId: 'content' };
    const draft = codingDraftOperations.fromDefinition(codingContractFixture.functionAuthoring, { language: 'cpp', runtime: 'wasi' });
    draft.functionSignature = null;
    draft.validationSolutions = [{ id: 'reference', name: '', role: 'reference', files: [] }];
    const snapshot: CodingDraftSnapshot = { revision: 3, basePublishedVersion: 2, draft };
    const load: CodingDraftLoadResult = { status: 'loaded', snapshot };
    const saved: CodingDraftSaveResult = { status: 'saved', snapshot: { ...snapshot, revision: 4 } };
    const port: CodingDraftPersistencePort = { scope, load: async () => load, save: async () => saved };
    const checkpoint: CodingDraftRecovery = { kind: 'coding-authoring-memory-recovery', scope, snapshot };
    const state: CodingDraftSessionState = { snapshot, draft, status: 'unsaved', error: null, currentRevision: null };

    expect(await port.load()).toEqual(load);
    expect(await port.save({ expectedRevision: 3, draft })).toEqual(saved);
    expect(checkpoint.snapshot.draft.functionSignature).toBeNull();
    expect(state.status).toBe('unsaved');
    expect(Object.keys(port)).toEqual(['scope', 'load', 'save']);
  });

  it('keeps conflicts, authorization and infrastructure failures distinct from saved', () => {
    const results: CodingDraftSaveResult[] = [
      { status: 'conflict', currentRevision: 9 },
      { status: 'conflict', currentRevision: null },
      { status: 'error', reason: 'forbidden' },
      { status: 'error', reason: 'unavailable' },
      { status: 'error', reason: 'invalid-data' },
    ];
    expect(results.map((result) => result.status)).toEqual(['conflict', 'conflict', 'error', 'error', 'error']);
    expect(JSON.stringify(results)).not.toContain('draft');
  });
});
