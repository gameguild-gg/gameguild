import { NextRequest } from 'next/server';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ getRequestAuthContext: vi.fn(), fetch: vi.fn() }));
vi.mock('@/auth', () => ({ getRequestAuthContext: mocks.getRequestAuthContext }));

import { GET, POST } from './route';

function listResponse(): Response {
  return new Response(
    JSON.stringify({
      items: [
        {
          id: 'b1000000-0000-0000-0000-000000000001',
          name: 'Launch week',
          key: 'launch-week',
          status: 'Active',
          timeZoneId: 'America/Sao_Paulo',
          currency: 'USD',
          codeCount: 12,
          attributedCustomers: 7,
          customerAcquisitionCost: 14.28,
        },
      ],
      totalCount: 1,
    }),
    { status: 200, headers: { 'content-type': 'application/json' } },
  );
}

describe('promo campaigns dashboard proxy', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal('fetch', mocks.fetch);
    vi.stubEnv('API_URL', 'http://localhost:8080');
    mocks.getRequestAuthContext.mockResolvedValue({
      session: { user: { id: 'admin' } },
      token: 'admin-token',
      tenantId: '50000000-0000-0000-0000-000000000490',
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
  });

  it('rejects unauthenticated callers before any upstream call', async () => {
    mocks.getRequestAuthContext.mockResolvedValue({ token: null });
    const response = await GET(new NextRequest('http://localhost/api/billing/promo-campaigns'));
    expect(response.status).toBe(401);
    expect(mocks.fetch).not.toHaveBeenCalled();
  });

  it('lists campaigns with paging parameters and forwards the session token', async () => {
    mocks.fetch.mockResolvedValue(listResponse());
    const response = await GET(new NextRequest('http://localhost/api/billing/promo-campaigns?searchTerm=launch'));
    expect(response.status).toBe(200);
    const requested = mocks.fetch.mock.calls[0]?.[0] as string;
    expect(requested).toContain('/api/v1/promo-campaigns?');
    expect(requested).toContain('take=50');
    expect(mocks.fetch.mock.calls[0]?.[1]).toMatchObject({
      headers: { authorization: 'Bearer admin-token' },
    });
    await expect(response.json()).resolves.toMatchObject({ items: [{ key: 'launch-week' }] });
  });

  it('forwards performance reports for one campaign id', async () => {
    mocks.fetch.mockResolvedValue(
      new Response(JSON.stringify({ campaignId: 'launch-week', totalUses: 9, customerAcquisitionCost: 12.5 }), {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }),
    );
    const campaignId = 'b1000000-0000-0000-0000-000000000001';
    const response = await GET(new NextRequest(`http://localhost/api/billing/promo-campaigns?campaignId=${campaignId}`));
    expect(response.status).toBe(200);
    const requested = mocks.fetch.mock.calls[0]?.[0] as string;
    expect(requested).toBe(`http://localhost:8080/api/v1/promo-campaigns/${campaignId}/performance`);
  });

  it('propagates upstream rejection status codes', async () => {
    mocks.fetch.mockResolvedValue(new Response('forbidden', { status: 403 }));
    const response = await GET(new NextRequest('http://localhost/api/billing/promo-campaigns'));
    expect(response.status).toBe(403);
  });

  it('issues lifecycle transitions against the colon action route', async () => {
    mocks.fetch.mockResolvedValue(new Response('{}', { status: 200 }));
    const campaignId = 'b1000000-0000-0000-0000-000000000001';
    const request = new NextRequest('http://localhost/api/billing/promo-campaigns', {
      method: 'POST',
      body: JSON.stringify({ campaignId, action: 'pause' }),
    });
    const response = await POST(request);
    expect(response.status).toBe(200);
    expect(mocks.fetch.mock.calls[0]?.[0]).toBe(`http://localhost:8080/api/v1/promo-campaigns/${campaignId}:pause`);
    expect(mocks.fetch.mock.calls[0]?.[1]).toMatchObject({ method: 'POST' });
  });

  it('rejects unsupported lifecycle actions without an upstream call', async () => {
    const request = new NextRequest('http://localhost/api/billing/promo-campaigns', {
      method: 'POST',
      body: JSON.stringify({ campaignId: 'x', action: 'delete' }),
    });
    const response = await POST(request);
    expect(response.status).toBe(400);
    expect(mocks.fetch).not.toHaveBeenCalled();
  });
});
