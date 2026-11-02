import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getRequestAuthContext: vi.fn(),
  verifyTurnstileToken: vi.fn(),
  forwardedIpHeaders: vi.fn(),
}));

vi.mock('@/auth', () => ({ getRequestAuthContext: mocks.getRequestAuthContext }));
vi.mock('@/lib/antibot/turnstile', () => ({ verifyTurnstileToken: mocks.verifyTurnstileToken }));
vi.mock('@/lib/server/forwarded-headers', () => ({ forwardedIpHeaders: mocks.forwardedIpHeaders }));
vi.mock('react', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react')>()),
  cache: <T extends (...args: never[]) => unknown>(fn: T) => fn,
}));

import { addComment, createPost, updateDraft } from './actions';

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
  mocks.verifyTurnstileToken.mockResolvedValue({ success: true });
  mocks.forwardedIpHeaders.mockResolvedValue({});
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

describe('addComment turnstile gate', () => {
  it('returns a typed failure before any API call when the token fails verification', async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: 'verify-failed' });

    const result = await addComment('p1', { content: 'hi', turnstileToken: 'stale' });

    expect(result).toEqual({
      success: false,
      error: 'Bot check failed. Please refresh and try again.',
      status: 403,
    });
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith('stale');
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('verifies a missing token and fails closed before any API call', async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    mocks.verifyTurnstileToken.mockResolvedValue({ success: false, reason: 'missing-token' });

    const result = await addComment('p1', { content: 'hi' });

    expect(result).toMatchObject({ success: false, status: 403 });
    expect(mocks.verifyTurnstileToken).toHaveBeenCalledWith(undefined);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('forwards the comment once with the token stripped and the client IP attached', async () => {
    mocks.forwardedIpHeaders.mockResolvedValue({ 'X-Forwarded-For': '203.0.113.7' });
    const fetchMock = fetchResponding(200, { id: 'c1', content: 'hi' });
    vi.stubGlobal('fetch', fetchMock);

    const result = await addComment('p1', { content: 'hi', parentCommentId: null, turnstileToken: 'fresh' });

    expect(result).toEqual({ success: true, data: { id: 'c1', content: 'hi' } });
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [, init] = fetchMock.mock.calls[0];
    const body = JSON.parse(init.body as string);
    expect(body).toEqual({ content: 'hi', parentCommentId: null });
    expect(body).not.toHaveProperty('turnstileToken');
    expect(init.headers['X-Forwarded-For']).toBe('203.0.113.7');
  });
});

describe('createPost', () => {
  it('resolves editUrl server-side from the author profile', async () => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ id: 'p1', slug: 'devlog-2', primaryAuthorId: 'u1' }), {
            status: 201,
            headers: { 'Content-Type': 'application/json' },
          }),
        )
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ handle: 'jane' }), {
            status: 200,
            headers: { 'Content-Type': 'application/json' },
          }),
        ),
    );

    const result = await createPost({ title: 'DevLog 2', format: 'Markdown' });

    expect(result).toEqual({
      success: true,
      data: { id: 'p1', slug: 'devlog-2', primaryAuthorId: 'u1' },
      editUrl: '/blogs/jane/devlog-2/edit',
    });
  });

  it('returns editUrl null when handle resolution fails', async () => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValueOnce(
          new Response(JSON.stringify({ id: 'p1', slug: 'devlog-2', primaryAuthorId: 'u1' }), {
            status: 201,
            headers: { 'Content-Type': 'application/json' },
          }),
        )
        .mockResolvedValueOnce(new Response('not found', { status: 404 })),
    );

    const result = await createPost({ title: 'DevLog 2', format: 'Markdown' });

    expect(result).toEqual({
      success: true,
      data: { id: 'p1', slug: 'devlog-2', primaryAuthorId: 'u1' },
      editUrl: null,
    });
  });
});
