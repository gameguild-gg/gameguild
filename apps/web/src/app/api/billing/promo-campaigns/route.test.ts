import { NextRequest } from 'next/server';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getRequestAuthContext: vi.fn(),
  getPromoCampaignsForGetPromoCampaigns: vi.fn(),
  getPromoCampaignsPerformance: vi.fn(),
  postPromoCampaignsActivate: vi.fn(),
  postPromoCampaignsPause: vi.fn(),
  postPromoCampaignsComplete: vi.fn(),
  postPromoCampaignsCancel: vi.fn(),
}));

vi.mock('@/auth', () => ({ getRequestAuthContext: mocks.getRequestAuthContext }));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    CommerceProductsPromoCampaignsModule: class {
      getPromoCampaignsForGetPromoCampaigns = mocks.getPromoCampaignsForGetPromoCampaigns;
      getPromoCampaignsPerformance = mocks.getPromoCampaignsPerformance;
      postPromoCampaignsActivate = mocks.postPromoCampaignsActivate;
      postPromoCampaignsPause = mocks.postPromoCampaignsPause;
      postPromoCampaignsComplete = mocks.postPromoCampaignsComplete;
      postPromoCampaignsCancel = mocks.postPromoCampaignsCancel;
    },
  },
}));

import { GET, POST } from './route';

function apiOk(data: unknown) {
  return { ok: true as const, data };
}

function apiError(status: number) {
  return { ok: false as const, error: { name: 'ApiError' as const, status, code: 'ERROR' as const, message: 'rejected' } };
}

const campaign = {
  id: 'b1000000-0000-0000-0000-000000000001',
  name: 'Launch week',
  key: 'launch-week',
  status: 'Active',
  timeZoneId: 'America/Sao_Paulo',
  currency: 'USD',
  codeCount: 12,
  attributedCustomers: 7,
  customerAcquisitionCost: 14.28,
};

describe('promo campaigns dashboard proxy (issue #1217: served /v1 prefix via the generated client)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getRequestAuthContext.mockResolvedValue({
      session: { user: { id: 'admin' } },
      token: 'admin-token',
      tenantId: '50000000-0000-0000-0000-000000000490',
    });
  });

  it('rejects unauthenticated callers before any upstream call', async () => {
    mocks.getRequestAuthContext.mockResolvedValue({ token: null });
    const response = await GET(new NextRequest('http://localhost/api/billing/promo-campaigns'));
    expect(response.status).toBe(401);
    expect(mocks.getPromoCampaignsForGetPromoCampaigns).not.toHaveBeenCalled();
  });

  it('lists campaigns through the generated list method with paging parameters', async () => {
    mocks.getPromoCampaignsForGetPromoCampaigns.mockResolvedValue(apiOk({ items: [campaign], totalCount: 1 }));

    const response = await GET(new NextRequest('http://localhost/api/billing/promo-campaigns?searchTerm=launch'));

    expect(response.status).toBe(200);
    expect(mocks.getPromoCampaignsForGetPromoCampaigns).toHaveBeenCalledWith({
      skip: 0,
      take: 50,
      searchTerm: 'launch',
    });
    await expect(response.json()).resolves.toMatchObject({ items: [{ key: 'launch-week' }] });
  });

  it('forwards a typed status filter and drops one the contract does not know', async () => {
    mocks.getPromoCampaignsForGetPromoCampaigns.mockResolvedValue(apiOk({ items: [] }));

    await GET(new NextRequest('http://localhost/api/billing/promo-campaigns?status=Paused'));
    expect(mocks.getPromoCampaignsForGetPromoCampaigns).toHaveBeenLastCalledWith({ skip: 0, take: 50, status: 'Paused' });

    await GET(new NextRequest('http://localhost/api/billing/promo-campaigns?status=NotAStatus'));
    expect(mocks.getPromoCampaignsForGetPromoCampaigns).toHaveBeenLastCalledWith({ skip: 0, take: 50 });
  });

  it('forwards performance reports through the generated performance method', async () => {
    mocks.getPromoCampaignsPerformance.mockResolvedValue(
      apiOk({ campaignId: campaign.id, totalUses: 9, customerAcquisitionCost: 12.5 }),
    );

    const response = await GET(
      new NextRequest(`http://localhost/api/billing/promo-campaigns?campaignId=${campaign.id}&from=2026-10-01&to=2026-10-31`),
    );

    expect(response.status).toBe(200);
    expect(mocks.getPromoCampaignsPerformance).toHaveBeenCalledWith(campaign.id, {
      from: '2026-10-01',
      to: '2026-10-31',
    });
    await expect(response.json()).resolves.toMatchObject({ totalUses: 9 });
  });

  it('propagates upstream rejection status codes', async () => {
    mocks.getPromoCampaignsForGetPromoCampaigns.mockResolvedValue(apiError(403));

    const response = await GET(new NextRequest('http://localhost/api/billing/promo-campaigns'));

    expect(response.status).toBe(403);
    await expect(response.json()).resolves.toEqual({ error: 'rejected' });
  });

  it('surfaces a generated-client contract-drift throw as a 502 gateway failure', async () => {
    mocks.getPromoCampaignsForGetPromoCampaigns.mockRejectedValue(new Error('Response validation failed'));

    const response = await GET(new NextRequest('http://localhost/api/billing/promo-campaigns'));

    expect(response.status).toBe(502);
  });

  it.each([
    ['activate', mocks.postPromoCampaignsActivate],
    ['pause', mocks.postPromoCampaignsPause],
    ['complete', mocks.postPromoCampaignsComplete],
    ['cancel', mocks.postPromoCampaignsCancel],
  ] as const)('issues the %s lifecycle transition through its generated method', async (action, method) => {
    method.mockResolvedValue(apiOk({ ...campaign, status: 'Paused' }));

    const response = await POST(
      new NextRequest('http://localhost/api/billing/promo-campaigns', {
        method: 'POST',
        body: JSON.stringify({ campaignId: campaign.id, action }),
      }),
    );

    expect(response.status).toBe(200);
    expect(method).toHaveBeenCalledWith(campaign.id);
  });

  it('propagates a state-machine rejection status', async () => {
    mocks.postPromoCampaignsCancel.mockResolvedValue(apiError(409));

    const response = await POST(
      new NextRequest('http://localhost/api/billing/promo-campaigns', {
        method: 'POST',
        body: JSON.stringify({ campaignId: campaign.id, action: 'cancel' }),
      }),
    );

    expect(response.status).toBe(409);
  });

  it('rejects unsupported lifecycle actions without an upstream call', async () => {
    const response = await POST(
      new NextRequest('http://localhost/api/billing/promo-campaigns', {
        method: 'POST',
        body: JSON.stringify({ campaignId: 'x', action: 'delete' }),
      }),
    );
    expect(response.status).toBe(400);
    expect(mocks.postPromoCampaignsActivate).not.toHaveBeenCalled();
    expect(mocks.postPromoCampaignsPause).not.toHaveBeenCalled();
    expect(mocks.postPromoCampaignsComplete).not.toHaveBeenCalled();
    expect(mocks.postPromoCampaignsCancel).not.toHaveBeenCalled();
  });
});
