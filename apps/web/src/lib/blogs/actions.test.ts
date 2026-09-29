import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getRequestAuthContext: vi.fn(),
}));

vi.mock('@/auth', () => ({ getRequestAuthContext: mocks.getRequestAuthContext }));
vi.mock('react', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react')>()),
  cache: <T extends (...args: never[]) => unknown>(fn: T) => fn,
}));

import { updateDraft } from './actions';

function fetchResponding(status: number, body: unknown) {
  return vi.fn().mockResolvedValue(
    new Response(JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    }),
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  mocks.getRequestAuthContext.mockResolvedValue({ token: 'tok', tenantId: 'tenant-1', session: {} });
});

describe('blog action error mapping', () => {
  it('maps 409 ProblemDetails to a typed revision-conflict error', async () => {
    vi.stubGlobal(
      'fetch',
      fetchResponding(409, {
        title: 'Revision conflict',
        detail: 'The post was edited by someone else.',
        expectedRevision: 7,
        currentRevision: 9,
      }),
    );

    const result = await updateDraft('p1', { revision: 7, title: 'New title' });

    expect(result.success).toBe(false);
    if (result.success) return;
    expect(result.status).toBe(409);
    expect(result).toMatchObject({
      code: 'revision-conflict',
      expectedRevision: 7,
      currentRevision: 9,
    });
  });

  it('maps other error statuses to a generic error with the API message', async () => {
    vi.stubGlobal('fetch', fetchResponding(403, { title: 'Forbidden', detail: 'Primary author only.' }));

    const result = await updateDraft('p1', { revision: 3 });

    expect(result.success).toBe(false);
    if (result.success) return;
    expect(result.status).toBe(403);
    expect(result.error).toBe('Primary author only.');
    expect('code' in result && result.code).toBeUndefined();
  });

  it('returns 401 without calling fetch when unauthenticated', async () => {
    mocks.getRequestAuthContext.mockResolvedValue({ token: null, tenantId: null, session: null });
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);

    const result = await updateDraft('p1', { revision: 1 });

    expect(result.success).toBe(false);
    if (result.success) return;
    expect(result.status).toBe(401);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('returns success payload on 200', async () => {
    vi.stubGlobal('fetch', fetchResponding(200, { id: 'p1', revision: 8 }));

    const result = await updateDraft('p1', { revision: 7, title: 'New title' });

    expect(result).toEqual({ success: true, data: { id: 'p1', revision: 8 } });
  });
});
