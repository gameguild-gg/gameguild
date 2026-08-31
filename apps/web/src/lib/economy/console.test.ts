import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  contexts: vi.fn(),
  hasCapability: vi.fn(),
  forbidden: vi.fn(() => { throw new Error('forbidden'); }),
  api: vi.fn(),
  auth: vi.fn(),
  getToken: vi.fn(async () => 'token'),
  serverConfig: null as null | { auth: { getAccessToken: () => Promise<string> }; baseUrl: string; tenant: { getTenantId: () => Promise<string | null> } },
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));
vi.mock('@/lib/dashboard-contexts', () => ({
  getDashboardContexts: mocks.contexts,
  hasAnyDashboardCapability: mocks.hasCapability,
}));
vi.mock('next/navigation', () => ({ forbidden: mocks.forbidden }));
vi.mock('@game-guild/client', () => {
  class ApiModule {
    constructor() {
      return new Proxy(this, {
        get: (target, property) => property in target ? target[property as keyof ApiModule] : (...args: unknown[]) => mocks.api(String(property), args),
      });
    }
  }
  return {
    createServerClient: vi.fn((config) => { mocks.serverConfig = config; return {}; }),
    GeneratedApi: {
      EconomyAdministrationModule: ApiModule,
      AuthStepUpModule: ApiModule,
      EconomyComplianceAdministrationModule: ApiModule,
      EconomyComplianceHoldAdministrationModule: ApiModule,
      EconomyLegacyMigrationAdministrationModule: ApiModule,
      EconomyRiskReviewAdministrationModule: ApiModule,
      EconomyTreasuryAdministrationModule: ApiModule,
    },
  };
});

import {
  economyConsoleSurfaces,
  createEconomyConsoleModules,
  getEconomyConsoleData,
  requireEconomyConsoleSurface,
  type EconomyConsoleSurface,
} from './console';

describe('Economy console access and readers', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'operator' }, tenantId: 'tenant-1' });
    mocks.serverConfig = null;
    mocks.contexts.mockResolvedValue({ capabilities: ['Economy.ReadOperations'] });
    mocks.hasCapability.mockReturnValue(true);
    mocks.api.mockResolvedValue({ ok: true, data: { items: [{ id: 'record-1', status: 'Ready' }] } });
  });

  it('requires the exact capability assigned to a console surface', async () => {
    await expect(requireEconomyConsoleSurface('policies')).resolves.toEqual({ capabilities: ['Economy.ReadOperations'] });
    expect(mocks.hasCapability).toHaveBeenCalledWith(['Economy.ReadOperations'], economyConsoleSurfaces.policies.capability);

    mocks.hasCapability.mockReturnValueOnce(false);
    await expect(requireEconomyConsoleSurface('treasury')).rejects.toThrow('forbidden');
  });

  it('loads every operational surface through its generated tenant-scoped client', async () => {
    for (const surface of Object.keys(economyConsoleSurfaces) as EconomyConsoleSurface[]) {
      const data = await getEconomyConsoleData(surface);
      expect(data.issue).toBeNull();
      if (surface === 'payout-reviews') expect(data.records).toEqual([]);
      else expect(data.records.length).toBeGreaterThan(0);
    }
  });

  it('normalizes arrays, singleton records, and client-safe errors', async () => {
    mocks.api
      .mockResolvedValueOnce({ ok: false, error: { message: 'policy store unavailable' } })
      .mockResolvedValueOnce({ ok: true, data: [{ id: 'health' }, null, 'unsafe'] })
      .mockResolvedValueOnce({ ok: true, data: { id: 'reserve' } });

    const data = await getEconomyConsoleData('readiness');

    expect(data.issue).toContain('policy store unavailable');
    expect(data.records).toEqual([{ id: 'health' }, { id: 'reserve' }]);

    mocks.api.mockResolvedValueOnce({ ok: false, error: { message: null } });
    expect((await getEconomyConsoleData('payout-operations')).issue).toBe('Operations: unavailable');
    mocks.api.mockResolvedValueOnce({ ok: true, data: 'unsafe' });
    expect((await getEconomyConsoleData('payout-operations')).records).toEqual([]);
  });

  it('creates generated clients with fail-closed tenant and API URL fallbacks', async () => {
    process.env.API_URL = 'https://private.example';
    await createEconomyConsoleModules();
    expect(mocks.serverConfig?.baseUrl).toBe('https://private.example');
    await expect(mocks.serverConfig?.auth.getAccessToken()).resolves.toBe('token');
    await expect(mocks.serverConfig?.tenant.getTenantId()).resolves.toBe('tenant-1');
    delete process.env.API_URL;

    process.env.NEXT_PUBLIC_API_URL = 'https://public.example';
    mocks.auth.mockResolvedValueOnce({ user: { id: 'operator' }, tenantId: null });
    await createEconomyConsoleModules();
    expect(mocks.serverConfig?.baseUrl).toBe('https://public.example');
    await expect(mocks.serverConfig?.tenant.getTenantId()).resolves.toBeNull();
    delete process.env.NEXT_PUBLIC_API_URL;

    mocks.auth.mockResolvedValueOnce(() => undefined);
    await createEconomyConsoleModules();
    expect(mocks.serverConfig?.baseUrl).toBe('http://localhost:8080');
    await expect(mocks.serverConfig?.tenant.getTenantId()).resolves.toBeNull();
    mocks.auth.mockRejectedValueOnce(new Error('auth unavailable'));
    await createEconomyConsoleModules();
    await expect(mocks.serverConfig?.tenant.getTenantId()).resolves.toBeNull();
  });
});
