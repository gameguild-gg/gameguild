import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  createServerClient: vi.fn((config: unknown) => config),
  getToken: vi.fn(async () => 'access-token'),
  wallet: vi.fn(),
  transactions: vi.fn(),
  capabilities: vi.fn(),
  payoutRequests: vi.fn(),
  payoutOperations: vi.fn(),
  payoutOperation: vi.fn(),
  payoutAccount: vi.fn(),
  topUps: vi.fn(),
  topUp: vi.fn(),
  kyc: vi.fn(),
  bounties: vi.fn(),
  bounty: vi.fn(),
  adReward: vi.fn(),
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
      getEconomyWallet = mocks.wallet;
      getEconomyWalletTransactions = mocks.transactions;
      getEconomyCapabilities = mocks.capabilities;
      getEconomyPayoutRequests = mocks.payoutRequests;
      getEconomyPayoutsForGetEconomyPayouts = mocks.payoutOperations;
      getEconomyPayoutsForGetEconomyPayoutsByOperationId = mocks.payoutOperation;
      getEconomyPayoutsAccount = mocks.payoutAccount;
      getEconomyTopUpsForGetEconomyTopUps = mocks.topUps;
      getEconomyTopUpsForGetEconomyTopUpsByTopUpId = mocks.topUp;
    },
    EconomyAdRewardsModule: class { getEconomyAdRewardsSessions = mocks.adReward; },
    EconomyBountiesModule: class {
      getEconomyBountiesForGetEconomyBounties = mocks.bounties;
      getEconomyBountiesForGetEconomyBountiesByBountyId = mocks.bounty;
    },
    EconomyKycModule: class { getEconomyKycStatus = mocks.kyc; },
  },
}));

import {
  getAdRewardSession,
  getEconomyBountiesData,
  getEconomyBounty,
  getEconomyKycData,
  getEconomyPayoutOperation,
  getEconomyPayoutsData,
  getEconomyTopUp,
  getEconomyTopUpsData,
  getEconomyWorkspaceData,
} from './queries';

const ok = (data: unknown) => ({ ok: true, data });
const fail = (message?: string | null) => ({ ok: false, error: { message } });

describe('Economy server queries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'actor' }, tenantId: 'tenant' });
    mocks.wallet.mockResolvedValue(ok({ availableHardToSpend: 10 }));
    mocks.transactions.mockResolvedValue(ok([{ journalEntryId: 'entry' }]));
    mocks.capabilities.mockResolvedValue(ok([{ capability: 'Payout', state: 'Disabled' }]));
    mocks.payoutRequests.mockResolvedValue(ok([{ id: 'request' }]));
    mocks.payoutOperations.mockResolvedValue(ok([{ id: 'operation' }]));
    mocks.payoutOperation.mockResolvedValue(ok({ id: 'operation' }));
    mocks.payoutAccount.mockResolvedValue(ok({ state: 'Pending' }));
    mocks.topUps.mockResolvedValue(ok([{ topUpId: 'top-up' }]));
    mocks.topUp.mockResolvedValue(ok({ topUpId: 'top-up' }));
    mocks.kyc.mockResolvedValue(ok({ result: 'Approved' }));
    mocks.bounties.mockResolvedValue(ok([{ id: { value: 'bounty' } }]));
    mocks.bounty.mockResolvedValue(ok({ id: { value: 'bounty' } }));
    mocks.adReward.mockResolvedValue(ok({ sessionId: 'session' }));
    delete process.env.API_URL;
    delete process.env.NEXT_PUBLIC_API_URL;
  });

  it('loads the complete workspace snapshot and authenticated client context', async () => {
    process.env.API_URL = 'https://api.internal';
    const result = await getEconomyWorkspaceData();

    expect(result).toMatchObject({ issue: null, wallet: { availableHardToSpend: 10 } });
    expect(result.transactions).toHaveLength(1);
    const config = mocks.createServerClient.mock.calls[0][0] as {
      auth: { getAccessToken: () => Promise<string> };
      baseUrl: string;
      tenant: { getTenantId: () => Promise<string | null> };
    };
    expect(config.baseUrl).toBe('https://api.internal');
    await expect(config.auth.getAccessToken()).resolves.toBe('access-token');
    await expect(config.tenant.getTenantId()).resolves.toBe('tenant');
  });

  it('falls back through public and local API URLs and handles missing tenant sessions', async () => {
    mocks.auth.mockResolvedValueOnce(() => undefined);
    await getEconomyWorkspaceData();
    let workspaceConfig = mocks.createServerClient.mock.calls.at(-1)?.[0] as { tenant: { getTenantId: () => Promise<string | null> } };
    await expect(workspaceConfig.tenant.getTenantId()).resolves.toBeNull();
    mocks.auth.mockRejectedValueOnce(new Error('auth down'));
    await getEconomyWorkspaceData();
    workspaceConfig = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof workspaceConfig;
    await expect(workspaceConfig.tenant.getTenantId()).resolves.toBeNull();

    mocks.auth.mockResolvedValueOnce({ user: { id: 'actor' }, tenantId: null });
    await getEconomyWorkspaceData();
    workspaceConfig = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof workspaceConfig;
    await expect(workspaceConfig.tenant.getTenantId()).resolves.toBeNull();

    process.env.NEXT_PUBLIC_API_URL = 'https://api.public';
    mocks.auth.mockResolvedValueOnce(null);
    await getEconomyTopUpsData();
    let config = mocks.createServerClient.mock.calls.at(-1)?.[0] as { baseUrl: string; tenant: { getTenantId: () => Promise<string | null> } };
    expect(config.baseUrl).toBe('https://api.public');
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    delete process.env.NEXT_PUBLIC_API_URL;
    mocks.auth.mockResolvedValueOnce(() => undefined);
    await getEconomyKycData();
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    expect(config.baseUrl).toBe('http://localhost:8080');
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    mocks.auth.mockResolvedValueOnce({ user: { id: 'actor' }, tenantId: null });
    await getEconomyBountiesData();
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    mocks.auth.mockRejectedValueOnce(new Error('auth down'));
    await getEconomyTopUpsData();
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    await expect(config.tenant.getTenantId()).resolves.toBeNull();
  });

  it('returns every successful specialized snapshot and detail', async () => {
    await expect(getEconomyTopUpsData()).resolves.toEqual({ topUps: [{ topUpId: 'top-up' }], issue: null });
    await expect(getEconomyTopUp(' top-up ')).resolves.toEqual({ topUpId: 'top-up' });
    await expect(getEconomyKycData()).resolves.toEqual({ status: { result: 'Approved' }, issue: null });
    await expect(getEconomyPayoutsData()).resolves.toMatchObject({ issue: null, account: { state: 'Pending' } });
    await expect(getEconomyBountiesData()).resolves.toEqual({ bounties: [{ id: { value: 'bounty' } }], issue: null });
    await expect(getEconomyBounty(' bounty ')).resolves.toEqual({ id: { value: 'bounty' } });
    await expect(getEconomyPayoutOperation(' operation ')).resolves.toEqual({ id: 'operation' });
    await expect(getAdRewardSession(' session ')).resolves.toEqual({ sessionId: 'session' });
    const config = mocks.createServerClient.mock.calls.at(-1)?.[0] as {
      auth: { getAccessToken: () => Promise<string> };
      tenant: { getTenantId: () => Promise<string | null> };
    };
    await expect(config.auth.getAccessToken()).resolves.toBe('access-token');
    await expect(config.tenant.getTenantId()).resolves.toBe('tenant');
  });

  it('fails closed with combined diagnostics and empty safe-read values', async () => {
    mocks.wallet.mockResolvedValue(fail('wallet down'));
    mocks.transactions.mockResolvedValue(fail(null));
    mocks.capabilities.mockResolvedValue(fail('capabilities down'));
    mocks.payoutRequests.mockResolvedValue(fail('requests down'));
    mocks.payoutOperations.mockResolvedValue(fail('operations down'));

    const result = await getEconomyWorkspaceData();
    expect(result).toEqual({
      wallet: null,
      transactions: [],
      capabilities: [],
      payoutRequests: [],
      payoutOperations: [],
      issue: 'Wallet: wallet down · Transactions: unavailable · Capabilities: capabilities down · Payout requests: requests down · Payout operations: operations down',
    });
  });

  it('returns safe values for failed list and detail queries', async () => {
    mocks.topUps.mockResolvedValue(fail(null));
    mocks.topUp.mockResolvedValue(fail('missing'));
    mocks.kyc.mockResolvedValue(fail(null));
    mocks.bounties.mockResolvedValue(fail('down'));
    mocks.bounty.mockResolvedValue(fail('missing'));
    mocks.adReward.mockResolvedValue(fail('missing'));
    mocks.payoutOperation.mockResolvedValue(fail('missing'));

    await expect(getEconomyTopUpsData()).resolves.toEqual({ topUps: [], issue: 'Top-ups: unavailable' });
    await expect(getEconomyTopUp('top-up')).resolves.toBeNull();
    await expect(getEconomyKycData()).resolves.toEqual({ status: null, issue: 'KYC: unavailable' });
    await expect(getEconomyBountiesData()).resolves.toEqual({ bounties: [], issue: 'Bounties: down' });
    await expect(getEconomyBounty('bounty')).resolves.toBeNull();
    await expect(getEconomyPayoutOperation('operation')).resolves.toBeNull();
    await expect(getAdRewardSession('session')).resolves.toBeNull();
  });

  it('combines failed payout diagnostics and rejects empty detail identifiers', async () => {
    mocks.payoutAccount.mockResolvedValue(fail(null));
    mocks.payoutRequests.mockResolvedValue(fail('requests down'));
    mocks.payoutOperations.mockResolvedValue(fail('operations down'));

    await expect(getEconomyPayoutsData()).resolves.toEqual({
      account: null,
      requests: [],
      operations: [],
      issue: 'Payout account: unavailable | Payout requests: requests down | Payout operations: operations down',
    });
    await expect(getEconomyTopUp(' ')).resolves.toBeNull();
    await expect(getEconomyBounty(' ')).resolves.toBeNull();
    await expect(getEconomyPayoutOperation(' ')).resolves.toBeNull();
    await expect(getAdRewardSession(' ')).resolves.toBeNull();
  });
});
