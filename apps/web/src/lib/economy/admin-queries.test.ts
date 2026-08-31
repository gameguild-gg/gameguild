import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(async () => 'token'),
  createServerClient: vi.fn((config: unknown) => config),
  requests: vi.fn(),
  audit: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth, getToken: mocks.getToken }));
vi.mock('react', async (importOriginal) => ({
  ...(await importOriginal<typeof import('react')>()),
  cache: <T extends (...args: never[]) => unknown>(fn: T) => fn,
}));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    EconomyModule: class {
      getAdminEconomyPayoutRequests = mocks.requests;
      getAdminEconomyPayoutRequestsAudit = mocks.audit;
    },
  },
}));

import { getEconomyPayoutReviewWorkspaceData } from './admin-queries';

describe('Payout review server queries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'reviewer' }, tenantId: 'tenant' });
    mocks.requests.mockResolvedValue({ ok: true, data: [{ id: 'one' }, { id: undefined }, { id: 'two' }] });
    mocks.audit.mockImplementation(async (id: string) => id === 'one'
      ? { ok: true, data: [{ id: 'audit-one' }] }
      : { ok: false, error: { message: 'audit down' } });
    delete process.env.API_URL;
    delete process.env.NEXT_PUBLIC_API_URL;
  });

  it('loads queue audits, ignores missing IDs, and combines audit diagnostics', async () => {
    process.env.API_URL = 'https://api.internal';
    const result = await getEconomyPayoutReviewWorkspaceData();
    expect(result).toEqual({
      requests: [{ id: 'one' }, { id: undefined }, { id: 'two' }],
      reviewAudits: { one: [{ id: 'audit-one' }] },
      issue: 'Audit two: audit down',
    });
    expect(mocks.audit).toHaveBeenCalledTimes(2);
    const config = mocks.createServerClient.mock.calls[0][0] as {
      auth: { getAccessToken: () => Promise<string> };
      baseUrl: string;
      tenant: { getTenantId: () => Promise<string | null> };
    };
    expect(config.baseUrl).toBe('https://api.internal');
    await expect(config.auth.getAccessToken()).resolves.toBe('token');
    await expect(config.tenant.getTenantId()).resolves.toBe('tenant');
  });

  it('returns a healthy queue when every audit succeeds', async () => {
    mocks.requests.mockResolvedValueOnce({ ok: true, data: [{ id: 'one' }] });
    mocks.audit.mockResolvedValueOnce({ ok: true, data: [] });
    await expect(getEconomyPayoutReviewWorkspaceData()).resolves.toEqual({
      requests: [{ id: 'one' }],
      reviewAudits: { one: [] },
      issue: null,
    });
  });

  it('fails closed when the queue is unavailable with provider or fallback diagnostics', async () => {
    mocks.requests.mockResolvedValueOnce({ ok: false, error: { message: 'queue down' } });
    await expect(getEconomyPayoutReviewWorkspaceData()).resolves.toEqual({ requests: [], reviewAudits: {}, issue: 'queue down' });
    mocks.requests.mockResolvedValueOnce({ ok: false, error: { message: null } });
    await expect(getEconomyPayoutReviewWorkspaceData()).resolves.toEqual({
      requests: [], reviewAudits: {}, issue: 'The payout review queue is unavailable.',
    });
  });

  it('uses fallback URLs and never derives tenant authority from a request', async () => {
    process.env.NEXT_PUBLIC_API_URL = 'https://api.public';
    mocks.auth.mockResolvedValueOnce(null);
    await getEconomyPayoutReviewWorkspaceData();
    let config = mocks.createServerClient.mock.calls.at(-1)?.[0] as { baseUrl: string; tenant: { getTenantId: () => Promise<string | null> } };
    expect(config.baseUrl).toBe('https://api.public');
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    delete process.env.NEXT_PUBLIC_API_URL;
    mocks.auth.mockResolvedValueOnce(() => undefined);
    await getEconomyPayoutReviewWorkspaceData();
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    expect(config.baseUrl).toBe('http://localhost:8080');
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    mocks.auth.mockResolvedValueOnce({ user: { id: 'reviewer' }, tenantId: null });
    await getEconomyPayoutReviewWorkspaceData();
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    mocks.auth.mockRejectedValueOnce(new Error('auth down'));
    await getEconomyPayoutReviewWorkspaceData();
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    await expect(config.tenant.getTenantId()).resolves.toBeNull();
  });

  it('uses a safe audit fallback when the provider omits its message', async () => {
    mocks.requests.mockResolvedValueOnce({ ok: true, data: [{ id: 'one' }] });
    mocks.audit.mockResolvedValueOnce({ ok: false, error: { message: null } });
    await expect(getEconomyPayoutReviewWorkspaceData()).resolves.toMatchObject({ issue: 'Audit one: unavailable' });
  });
});
