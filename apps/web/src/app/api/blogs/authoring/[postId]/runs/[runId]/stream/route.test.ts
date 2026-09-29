import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getRequestAuthContext: vi.fn(),
}));

vi.mock('@/auth', () => ({ getRequestAuthContext: mocks.getRequestAuthContext }));

type RouteHandler = (request: Request, context: { params: Promise<Record<string, string>> }) => Promise<Response>;

let GET: RouteHandler;

beforeEach(async () => {
  vi.resetModules();
  vi.clearAllMocks();
  mocks.getRequestAuthContext.mockResolvedValue({ token: 'tok', tenantId: 'tenant-1', session: {} });
  ({ GET } = (await import('./route')) as { GET: RouteHandler });
});

function routeContext(params: Record<string, string>) {
  return { params: Promise.resolve(params) };
}

function sseResponse() {
  return new Response('id: 0\nevent: status\ndata: {"sequence":0}\n\n', {
    status: 200,
    headers: { 'Content-Type': 'text/event-stream' },
  });
}

describe('blog copilot SSE proxy route', () => {
  it('returns 401 without calling the API when unauthenticated', async () => {
    mocks.getRequestAuthContext.mockResolvedValue({ token: null, tenantId: null, session: null });
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);

    const response = await GET(new Request('http://localhost/api/blogs/authoring/p1/runs/r1/stream'), routeContext({ postId: 'p1', runId: 'r1' }));

    expect(response.status).toBe(401);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('forwards Authorization and X-Tenant-Id headers to the backend stream URL', async () => {
    const fetchMock = vi.fn().mockResolvedValue(sseResponse());
    vi.stubGlobal('fetch', fetchMock);

    await GET(new Request('http://localhost/api/blogs/authoring/p1/runs/r1/stream'), routeContext({ postId: 'p1', runId: 'r1' }));

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/social/blog/posts/p1/ai/runs/r1/stream');
    expect(new Headers(init.headers).get('Authorization')).toBe('Bearer tok');
    expect(new Headers(init.headers).get('X-Tenant-Id')).toBe('tenant-1');
    expect(new Headers(init.headers).get('Accept')).toBe('text/event-stream');
  });

  it('passes through Last-Event-ID header', async () => {
    const fetchMock = vi.fn().mockResolvedValue(sseResponse());
    vi.stubGlobal('fetch', fetchMock);

    await GET(
      new Request('http://localhost/api/blogs/authoring/p1/runs/r1/stream', {
        headers: { 'Last-Event-ID': '42' },
      }),
      routeContext({ postId: 'p1', runId: 'r1' }),
    );

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(new Headers(init.headers).get('Last-Event-ID')).toBe('42');
  });

  it('streams the backend body back as text/event-stream', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(sseResponse()));

    const response = await GET(new Request('http://localhost/api/blogs/authoring/p1/runs/r1/stream'), routeContext({ postId: 'p1', runId: 'r1' }));

    expect(response.headers.get('Content-Type')).toBe('text/event-stream');
    expect(response.headers.get('Cache-Control')).toBe('no-cache, no-transform');
    expect(await response.text()).toContain('data: {"sequence":0}');
  });

  it('maps backend failure to AI_STREAM_UNAVAILABLE with the backend status', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('nope', { status: 403 })));

    const response = await GET(new Request('http://localhost/api/blogs/authoring/p1/runs/r1/stream'), routeContext({ postId: 'p1', runId: 'r1' }));

    expect(response.status).toBe(403);
    const body = (await response.json()) as { code: string };
    expect(body.code).toBe('AI_STREAM_UNAVAILABLE');
  });
});
