import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import {
  listSessions,
  terminateSession,
  terminateOtherSessions,
  terminateAllSessions,
  SessionTerminationError,
} from '../../src/runtime/auth/extended-operations.js';
import { AuthServiceUnavailableError } from '../../src/runtime/auth/errors.js';
import { AuthSessionsModule } from '../../src/generated/modules/auth-sessions.gen.js';

const API = 'https://api.gameguild.gg';
const TOKEN = 'owned-session-bearer';
const ID = '11111111-1111-4111-8111-111111111111';
const ROW = {
  id: ID,
  createdAt: '2026-11-04T01:00:00+00:00',
  lastUsedAt: '2026-11-04T04:00:00+00:00',
  isCurrent: true,
  ipAddress: '198.51.100.8',
  deviceInfo: { userAgent: 'Test Browser', browser: 'Test' },
};
const fetchMock = vi.fn<typeof fetch>();

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal('fetch', fetchMock);
});
afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe('session operations through generated authenticated contracts', () => {
  it('maps the native activity timestamp and nested user agent to the public session type', async () => {
    const generated = vi.spyOn(AuthSessionsModule.prototype, 'getAuthSessions');
    fetchMock.mockResolvedValueOnce(Response.json([ROW]));
    await expect(listSessions(API, TOKEN)).resolves.toEqual([
      { id: ID, createdAt: ROW.createdAt, lastActiveAt: ROW.lastUsedAt, isCurrent: true, ipAddress: ROW.ipAddress, userAgent: 'Test Browser' },
    ]);
    expect(generated).toHaveBeenCalledOnce();
    const [url, options] = fetchMock.mock.calls[0];
    expect(url).toBe(`${API}/v1/auth/sessions`);
    expect(options?.method).toBe('GET');
    expect(new Headers(options?.headers).get('Authorization')).toBe(`Bearer ${TOKEN}`);
    expect(options?.redirect).toBe('error');
    expect(options?.body).toBeUndefined();
  });

  it('retains a validated legacy envelope without inventing nullable device/IP fields', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({ sessions: [{ ...ROW, ipAddress: null, deviceInfo: { userAgent: null } }] }));
    await expect(listSessions(API, TOKEN)).resolves.toEqual([{ id: ID, createdAt: ROW.createdAt, lastActiveAt: ROW.lastUsedAt, isCurrent: true }]);
  });

  it('accepts the nullable device and location objects emitted by the native session DTO', async () => {
    fetchMock.mockResolvedValueOnce(Response.json([{ ...ROW, deviceInfo: null, location: null }]));
    await expect(listSessions(API, TOKEN)).resolves.toEqual([
      { id: ID, createdAt: ROW.createdAt, lastActiveAt: ROW.lastUsedAt, isCurrent: true, ipAddress: ROW.ipAddress },
    ]);
  });

  it.each([{ body: [] }, { body: { sessions: [] } }])('accepts valid empty collections: %j', async ({ body }) => {
    fetchMock.mockResolvedValueOnce(Response.json(body));
    await expect(listSessions(API, TOKEN)).resolves.toEqual([]);
  });

  it.each([
    { body: {} },
    { body: null },
    { body: { sessions: {} } },
    { body: [{ ...ROW, id: undefined }] },
    { body: [{ ...ROW, id: 'invalid' }] },
    { body: [{ ...ROW, createdAt: undefined }] },
    { body: [{ ...ROW, lastUsedAt: undefined }] },
    { body: [{ ...ROW, isCurrent: undefined }] },
    { body: [{ ...ROW, isCurrent: 'true' }] },
    { body: [{ ...ROW, lastUsedAt: 'yesterday' }] },
    { body: [{ ...ROW, deviceInfo: { userAgent: 42 } }] },
    { body: [ROW, { ...ROW, id: 'invalid' }] },
    { body: [{ ...ROW, location: 'invalid' }] },
  ])('validates the entire list despite the generated array cast: %j', async ({ body }) => {
    fetchMock.mockResolvedValueOnce(Response.json(body));
    await expect(listSessions(API, TOKEN)).rejects.toThrow(AuthServiceUnavailableError);
  });

  it.each([401, 403, 429, 500])('preserves the HTTP %i list fallback without retrying', async (status) => {
    fetchMock.mockResolvedValueOnce(Response.json({}, { status }));
    await expect(listSessions(API, TOKEN)).resolves.toEqual([]);
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it.each(['../users/other', '../../sessions:terminate-all', `${ID}?all=true`, `${ID}#fragment`, '%2e%2e%2fusers', '', 'session-123'])(
    'rejects a non-UUID DELETE target before network access: %j',
    async (id) => {
      fetchMock.mockResolvedValue(Response.json({ message: 'Must not reach network' }));
      await expect(terminateSession(API, id, TOKEN)).rejects.toThrow(SessionTerminationError);
      expect(fetchMock).not.toHaveBeenCalled();
    },
  );

  const mutations = [
    {
      name: 'single',
      path: `/v1/auth/sessions/${ID}`,
      method: 'DELETE',
      generated: 'deleteAuthSessions' as const,
      invoke: () => terminateSession(API, ID, TOKEN),
      response: { message: 'Session terminated' },
    },
    {
      name: 'others',
      path: '/v1/auth/sessions:terminate-others',
      method: 'POST',
      generated: 'postAuthSessionsTerminateOthers' as const,
      invoke: () => terminateOtherSessions(API, TOKEN),
      response: { message: 'Other sessions terminated', terminatedCount: 0 },
    },
    {
      name: 'all',
      path: '/v1/auth/sessions:terminate-all',
      method: 'POST',
      generated: 'postAuthSessionsTerminateAll' as const,
      invoke: () => terminateAllSessions(API, TOKEN),
      response: { message: 'All sessions terminated', terminatedCount: 3 },
    },
  ];

  for (const operation of mutations) {
    it(`${operation.name} sends the actor bearer through the generated method without caller identity in a body`, async () => {
      const generated = vi.spyOn(AuthSessionsModule.prototype, operation.generated);
      fetchMock.mockResolvedValueOnce(Response.json(operation.response));
      await expect(operation.invoke()).resolves.toBeUndefined();
      expect(generated).toHaveBeenCalledOnce();
      expect(fetchMock).toHaveBeenCalledOnce();
      const [url, options] = fetchMock.mock.calls[0];
      expect(url).toBe(`${API}${operation.path}`);
      expect(options?.method).toBe(operation.method);
      expect(new Headers(options?.headers).get('Authorization')).toBe(`Bearer ${TOKEN}`);
      expect(options?.body).toBeUndefined();
      expect(options?.redirect).toBe('error');
    });

    it.each([400, 401, 403, 500])(`${operation.name} rejects HTTP %i without refreshing or retrying`, async (status) => {
      fetchMock.mockResolvedValueOnce(Response.json({ detail: 'Denied' }, { status }));
      await expect(operation.invoke()).rejects.toThrow(SessionTerminationError);
      expect(fetchMock).toHaveBeenCalledOnce();
    });

    it(`${operation.name} rejects an unvalidated successful acknowledgement`, async () => {
      fetchMock.mockResolvedValueOnce(Response.json({}));
      await expect(operation.invoke()).rejects.toThrow(SessionTerminationError);
    });
  }

  it.each([{ count: -1 }, { count: 1.5 }, { count: 'all' }, { count: undefined }])('rejects an invalid bulk termination count: %j', async ({ count }) => {
    fetchMock.mockResolvedValueOnce(Response.json({ message: 'Done', terminatedCount: count }));
    await expect(terminateOtherSessions(API, TOKEN)).rejects.toThrow(SessionTerminationError);
  });

  it('accepts the nullable message declared by the native single-session acknowledgement', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({ message: null }));
    await expect(terminateSession(API, ID, TOKEN)).resolves.toBeUndefined();
  });

  it.each(['', '   '])('never sends a missing actor bearer: %j', async (token) => {
    await expect(listSessions(API, token)).resolves.toEqual([]);
    await expect(terminateSession(API, ID, token)).rejects.toThrow(SessionTerminationError);
    await expect(terminateOtherSessions(API, token)).rejects.toThrow(SessionTerminationError);
    await expect(terminateAllSessions(API, token)).rejects.toThrow(SessionTerminationError);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  for (const operation of [
    ...mutations.map((item) => ({ ...item, error: SessionTerminationError })),
    { name: 'list', invoke: () => listSessions(API, TOKEN), error: AuthServiceUnavailableError },
  ]) {
    it(`${operation.name} wraps network failure without diagnostic payload logging`, async () => {
      const log = vi.spyOn(console, 'log').mockImplementation(() => undefined),
        group = vi.spyOn(console, 'groupCollapsed').mockImplementation(() => undefined);
      fetchMock.mockRejectedValueOnce(new Error(`Network rejected ${TOKEN}`));
      const error: unknown = await operation.invoke().catch((error: unknown) => error);
      expect(error).toBeInstanceOf(operation.error);
      if (!(error instanceof Error)) throw new Error('Expected a session error');
      expect(error.message).not.toContain(TOKEN);
      expect(log).not.toHaveBeenCalled();
      expect(group).not.toHaveBeenCalled();
    });
    it(`${operation.name} bounds an adapter that ignores abort`, async () => {
      vi.useFakeTimers();
      fetchMock.mockImplementation(() => new Promise<Response>(() => undefined));
      const settled = expect(operation.invoke()).rejects.toThrow(operation.error);
      await vi.advanceTimersByTimeAsync(5_000);
      await settled;
      expect(fetchMock.mock.calls[0][1]?.signal?.aborted).toBe(true);
    });
  }
});
