import { codingDraftOperations, parseCodingAuthoringDraft } from '@game-guild/coding';
import { codingContractFixture } from '@game-guild/coding/testing';
import type { CodingAuthoringScope, CodingDraftPersistencePort } from '@game-guild/coding-surface';
import type { AuthoringContentPayload } from '@/lib/learning/authoring';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { createLearningCodingDraftPersistence } from './learning-draft-persistence';
import sharedDrafts from '../../../../../packages/features/coding/fixtures/coding-authoring-drafts.json';

const mocks = vi.hoisted(() => ({ auth: vi.fn() }));
vi.mock('@/auth', () => ({ getRequestAuthContext: mocks.auth }));

const scope: CodingAuthoringScope = { tenantId: 'local-tenant', actorId: 'teacher', courseId: 'course', contentId: 'content' };

function draft() {
  const value = codingDraftOperations.fromDefinition(codingContractFixture.functionAuthoring, { language: 'cpp', runtime: 'wasi' });
  value.functionSignature = null;
  value.validationSolutions = [{ id: 'reference', name: '', role: 'reference', files: [{ fileId: 'main', path: '', content: 'PRIVATE-REFERENCE' }] }];
  return value;
}

function payload(): AuthoringContentPayload {
  return {
    title: 'Exercise',
    slug: 'exercise',
    description: 'Host metadata',
    type: 'Code',
    body: null,
    jsonBody: null,
    lessonFormat: null,
    activitySettings: { gradingEnabled: true },
    isRequired: true,
    estimatedMinutesSource: 'Auto',
    visibility: 'Private',
  };
}

function response(overrides: Record<string, unknown> = {}) {
  return {
    id: 'server-draft',
    programId: 'course',
    contentId: 'content',
    revision: 4,
    basePublishedVersion: 2,
    payload: { ...payload(), jsonBody: draft() },
    ...overrides,
  };
}

function createPort(getPayload = payload, revision = 4): CodingDraftPersistencePort {
  return createLearningCodingDraftPersistence({ scope, getPayload: () => ({ revision, payload: getPayload() }) });
}

function answer(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

afterEach(() => {
  vi.restoreAllMocks();
});

describe('Learning coding draft persistence boundary', () => {
  it.each(['function', 'standard-io'] as const)('[CS-REF-06/07/22] preserves the shared %s fixture across the Learning request boundary', async (family) => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    const candidate = parseCodingAuthoringDraft(sharedDrafts[family]);
    const candidatePayload = { ...payload(), jsonBody: candidate };
    const fetch = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(answer(response({ payload: candidatePayload })))
      .mockResolvedValueOnce(answer(response({ revision: 5, payload: candidatePayload })));
    const port = createPort();
    expect(await port.load()).toEqual({ status: 'loaded', snapshot: { revision: 4, basePublishedVersion: 2, draft: candidate } });
    expect(await port.save({ expectedRevision: 4, draft: candidate })).toEqual({
      status: 'saved',
      snapshot: { revision: 5, basePublishedVersion: 2, draft: candidate },
    });
    expect(fetch.mock.calls[1][1]?.body).toBe(JSON.stringify({ revision: 4, payload: candidatePayload }));
  });

  it('loads incomplete protected authoring JSON through the existing authenticated authoring action', async () => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    const fetch = vi.spyOn(globalThis, 'fetch').mockResolvedValue(answer(response()));
    const port = createPort();
    expect(await port.load()).toEqual({ status: 'loaded', snapshot: { revision: 4, basePublishedVersion: 2, draft: draft() } });
    expect(fetch).toHaveBeenCalledExactlyOnceWith(
      expect.stringContaining('/v1/courses/course/content/content/authoring'),
      expect.objectContaining({
        cache: 'no-store',
        headers: { Authorization: 'Bearer server-token', 'X-Tenant-Id': 'server-tenant' },
      }),
    );
  });

  it('saves only to the shared draft at the expected revision, reading current host metadata at write time', async () => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    let metadata = payload();
    const port = createPort(() => metadata);
    metadata = { ...metadata, title: 'Changed title', description: 'Changed description' };
    const candidate = draft();
    const fetch = vi.spyOn(globalThis, 'fetch').mockResolvedValue(answer(response({ revision: 5, payload: { ...metadata, jsonBody: candidate } })));
    expect(await port.save({ expectedRevision: 4, draft: candidate })).toEqual({
      status: 'saved',
      snapshot: { revision: 5, basePublishedVersion: 2, draft: candidate },
    });
    const call = fetch.mock.calls[0];
    expect(call[0]).not.toContain('/coding-assignment');
    expect(call[0]).not.toContain('/publish');
    expect(call[1]?.body).toBe(JSON.stringify({ revision: 4, payload: { ...metadata, jsonBody: candidate } }));
    expect(call[1]?.headers).toEqual({ Authorization: 'Bearer server-token', 'X-Tenant-Id': 'server-tenant', 'Content-Type': 'application/json' });
    expect(candidate.validationSolutions[0].files[0].content).toBe('PRIVATE-REFERENCE');
    expect(metadata.jsonBody).toBeNull();
  });

  it.each([401, 403] as const)('does not turn HTTP %i into a loaded or saved document', async (status) => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    vi.spyOn(globalThis, 'fetch').mockImplementation(async () => answer({ detail: 'PRIVATE-REFERENCE' }, status));
    const port = createPort();
    expect(await port.load()).toEqual({ status: 'error', reason: 'forbidden' });
    expect(await port.save({ expectedRevision: 4, draft: draft() })).toEqual({ status: 'error', reason: 'forbidden' });
  });

  it('does not call the API without authenticated context or trust the local isolation scope as credentials', async () => {
    mocks.auth.mockResolvedValue({ token: null, tenantId: null });
    const fetch = vi.spyOn(globalThis, 'fetch');
    const port = createPort();
    expect(await port.load()).toEqual({ status: 'error', reason: 'forbidden' });
    expect(await port.save({ expectedRevision: 4, draft: draft() })).toEqual({ status: 'error', reason: 'forbidden' });
    expect(fetch).not.toHaveBeenCalled();
  });

  it.each([9, null])('preserves a revision conflict with current revision %s without retrying', async (currentRevision) => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    const fetch = vi.spyOn(globalThis, 'fetch').mockResolvedValue(answer({ currentRevision, detail: 'PRIVATE-REFERENCE' }, 409));
    expect(await createPort().save({ expectedRevision: 4, draft: draft() })).toEqual({ status: 'conflict', currentRevision });
    expect(fetch).toHaveBeenCalledTimes(1);
  });

  it.each<[string, Record<string, unknown>]>([
    ['other course', { programId: 'other' }],
    ['other content', { contentId: 'other' }],
    ['invalid revision', { revision: '4' }],
    ['invalid base', { basePublishedVersion: -1 }],
    ['wrong content type', { payload: { type: 'Lesson', jsonBody: draft() } }],
    ['canonical definition, not an authoring draft', { payload: { type: 'Code', jsonBody: codingContractFixture.functionAuthoring } }],
    ['missing body', { payload: { type: 'Code', jsonBody: null } }],
  ])('rejects a successful HTTP response for %s', async (_name, invalid) => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(answer(response(invalid)));
    expect(await createPort().load()).toEqual({ status: 'error', reason: 'invalid-data' });
  });

  it('rejects an ack for another revision or body instead of claiming Saved', async () => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    const wrongDraft = draft();
    wrongDraft.problem.statement = 'Someone else';
    vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(answer(response({ revision: 9 })))
      .mockResolvedValueOnce(answer(response({ revision: 5, payload: { ...payload(), jsonBody: wrongDraft } })));
    const port = createPort();
    expect(await port.save({ expectedRevision: 4, draft: draft() })).toEqual({ status: 'error', reason: 'invalid-data' });
    expect(await port.save({ expectedRevision: 4, draft: draft() })).toEqual({ status: 'error', reason: 'invalid-data' });
  });

  it('contains fetch, malformed JSON, metadata and serialization failures without logging private content', async () => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    const log = vi.spyOn(console, 'error');
    vi.spyOn(globalThis, 'fetch')
      .mockRejectedValueOnce(new Error('PRIVATE-REFERENCE'))
      .mockResolvedValueOnce(new Response('not-json', { status: 200 }));
    const port = createPort();
    expect(await port.load()).toEqual({ status: 'error', reason: 'unavailable' });
    expect(await port.load()).toEqual({ status: 'error', reason: 'invalid-data' });
    const broken = createPort(() => {
      throw new Error('PRIVATE-REFERENCE');
    });
    expect(await broken.save({ expectedRevision: 4, draft: draft() })).toEqual({ status: 'error', reason: 'invalid-data' });
    expect(log).not.toHaveBeenCalled();
  });

  it('rejects incomplete isolation identities instead of making a route with undefined IDs', () => {
    const incomplete = { ...scope };
    Reflect.deleteProperty(incomplete, 'contentId');
    expect(() => createLearningCodingDraftPersistence({ scope: incomplete, getPayload: () => ({ revision: 4, payload: payload() }) })).toThrow(/scope/i);
  });

  it('rejects non-JSON metadata before invoking the server action', async () => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    const fetch = vi.spyOn(globalThis, 'fetch');
    const metadata = payload();
    const cycle: Record<string, unknown> = {};
    cycle.self = cycle;
    metadata.activitySettings = cycle;
    expect(await createPort(() => metadata).save({ expectedRevision: 4, draft: draft() })).toEqual({ status: 'error', reason: 'invalid-data' });
    expect(fetch).not.toHaveBeenCalled();
  });

  it('cannot overwrite another author metadata after reloading a newer coding revision', async () => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    const latestPayload = { ...payload(), title: 'Other teacher title', jsonBody: draft() };
    const fetch = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(answer(response({ revision: 9, payload: latestPayload })))
      .mockResolvedValueOnce(answer(response({ revision: 10 })));
    const port = createPort();
    expect(await port.load()).toMatchObject({ status: 'loaded', snapshot: { revision: 9 } });
    expect(await port.save({ expectedRevision: 9, draft: draft() })).toEqual({ status: 'error', reason: 'invalid-data' });
    expect(fetch).toHaveBeenCalledTimes(1);
  });

  it('writes again only after the host coordinator adopts the acknowledged revision and metadata', async () => {
    mocks.auth.mockResolvedValue({ token: 'server-token', tenantId: 'server-tenant' });
    let hostDraft = { revision: 4, payload: payload() };
    const port = createLearningCodingDraftPersistence({ scope, getPayload: () => hostDraft });
    const fetch = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(answer(response({ revision: 5 })))
      .mockResolvedValueOnce(answer(response({ revision: 6 })));
    const first = await port.save({ expectedRevision: 4, draft: draft() });
    expect(first).toMatchObject({ status: 'saved', snapshot: { revision: 5 } });
    hostDraft = { revision: 5, payload: { ...hostDraft.payload, title: 'Latest coordinator title' } };
    expect(await port.save({ expectedRevision: 5, draft: draft() })).toMatchObject({ status: 'saved', snapshot: { revision: 6 } });
    expect(fetch.mock.calls[1][1]?.body).toBe(JSON.stringify({ revision: 5, payload: { ...hostDraft.payload, jsonBody: draft() } }));
  });
});
