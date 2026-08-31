import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(async () => 'token'),
  createServerClient: vi.fn((config: unknown) => config),
  payout: vi.fn(),
  cancelPayout: vi.fn(),
  conversion: vi.fn(),
  topUp: vi.fn(),
  transfer: vi.fn(),
  payoutOnboarding: vi.fn(),
  startAdReward: vi.fn(),
  completeAdReward: vi.fn(),
  kycOnboarding: vi.fn(),
  kycToken: vi.fn(),
  createBounty: vi.fn(),
  claimBounty: vi.fn(),
  reclaimBounty: vi.fn(),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth, getToken: mocks.getToken }));
vi.mock('next/cache', () => ({ revalidatePath: mocks.revalidatePath }));
vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    EconomyModule: class {
      postEconomyPayoutRequests = mocks.payout;
      postEconomyPayoutRequestsCancel = mocks.cancelPayout;
      postEconomyConversionsHardToSoft = mocks.conversion;
      postEconomyTopUps = mocks.topUp;
      postEconomyTransfers = mocks.transfer;
      postEconomyPayoutsOnboarding = mocks.payoutOnboarding;
    },
    EconomyAdRewardsModule: class {
      postEconomyAdRewardsSessions = mocks.startAdReward;
      postEconomyAdRewardsSessionsComplete = mocks.completeAdReward;
    },
    EconomyBountiesModule: class {
      postEconomyBounties = mocks.createBounty;
      postEconomyBountiesClaim = mocks.claimBounty;
      postEconomyBountiesReclaim = mocks.reclaimBounty;
    },
    EconomyKycModule: class {
      postEconomyKycOnboarding = mocks.kycOnboarding;
      postEconomyKycAccessToken = mocks.kycToken;
    },
  },
}));

import {
  cancelPayoutRequestAction,
  claimBountyAction,
  completeAdRewardSessionAction,
  convertHardToSoftAction,
  createBountyAction,
  createKycAccessTokenAction,
  createPayoutOnboardingAction,
  createTopUpAction,
  createTransferAction,
  reclaimBountyAction,
  startAdRewardSessionAction,
  startKycOnboardingAction,
  submitPayoutRequestAction,
} from './actions';

const ok = (data: unknown = {}) => ({ ok: true, data });
const fail = (message?: string | null) => ({ ok: false, error: { message } });
const bountyIntent = {
  amountUnits: 100,
  currency: 'HardCoin' as const,
  expiresAt: '2026-09-01T00:00:00.000Z',
  idempotencyKey: 'bounty-key',
  minimumReputation: 0,
  requiresInstructorVerification: false,
  requiresPrerequisite: false,
};
const adCompletion = {
  completedAt: '2026-08-30T00:01:00.000Z',
  creativeId: 'creative',
  network: 'google-ad-manager',
  playbackDuration: '00:01:00',
  sessionId: 'session',
  signedToken: 'signed',
  startedAt: '2026-08-30T00:00:00.000Z',
  visibleDuration: '00:01:00',
};

describe('Economy self-service actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'actor-1' }, tenantId: 'tenant-1' });
    mocks.payout.mockResolvedValue(ok());
    mocks.cancelPayout.mockResolvedValue(ok());
    mocks.conversion.mockResolvedValue(ok());
    mocks.topUp.mockResolvedValue(ok({ topUpId: 'top-up', clientSecret: 'secret', publishableKey: 'pk', status: 'Pending' }));
    mocks.transfer.mockResolvedValue(ok());
    mocks.payoutOnboarding.mockResolvedValue(ok({ onboardingUri: 'https://stripe.test/onboard' }));
    mocks.startAdReward.mockResolvedValue(ok({ claims: { sessionId: 'session' }, token: { value: 'signed' } }));
    mocks.completeAdReward.mockResolvedValue(ok({ state: 'PendingProviderReport' }));
    mocks.kycOnboarding.mockResolvedValue(ok({ state: 'ApplicantPending', updatedAt: 'now' }));
    mocks.kycToken.mockResolvedValue(ok({ token: 'kyc-token', externalUserId: 'subject' }));
    mocks.createBounty.mockResolvedValue(ok());
    mocks.claimBounty.mockResolvedValue(ok());
    mocks.reclaimBounty.mockResolvedValue(ok());
    delete process.env.API_URL;
    delete process.env.NEXT_PUBLIC_API_URL;
  });

  it('submits every self-service business intent without browser-owned authority', async () => {
    process.env.API_URL = 'https://api.internal';
    await expect(submitPayoutRequestAction(500, ' payout-key ')).resolves.toMatchObject({ success: true });
    await expect(cancelPayoutRequestAction(' payout ')).resolves.toMatchObject({ success: true });
    await expect(convertHardToSoftAction(250, ' conversion-key ')).resolves.toMatchObject({ success: true });
    await expect(createTopUpAction(1500, ' top-up-key ')).resolves.toMatchObject({
      success: true,
      data: { topUpId: 'top-up', clientSecret: 'secret', publishableKey: 'pk', status: 'Pending' },
    });
    await expect(createTransferAction(' recipient ', 25, 'HardCoin', 'Tip', ' transfer-key ')).resolves.toMatchObject({ success: true });
    await expect(createPayoutOnboardingAction()).resolves.toMatchObject({ success: true, data: { onboardingUri: 'https://stripe.test/onboard' } });
    await expect(startAdRewardSessionAction(' google-ad-manager ', ' creative ', 30, ' ad-key ')).resolves.toMatchObject({
      success: true, data: { sessionId: 'session', signedToken: 'signed' },
    });
    await expect(startKycOnboardingAction(' kyc-key ')).resolves.toMatchObject({ success: true });
    await expect(createKycAccessTokenAction()).resolves.toMatchObject({ success: true, data: { token: 'kyc-token' } });
    await expect(createBountyAction({ ...bountyIntent, idempotencyKey: ' bounty-key ' })).resolves.toMatchObject({ success: true });
    await expect(claimBountyAction(' bounty ', ' claim-key ')).resolves.toMatchObject({ success: true });
    await expect(reclaimBountyAction(' bounty ', ' reclaim-key ')).resolves.toMatchObject({ success: true });
    await expect(completeAdRewardSessionAction(adCompletion, ' completion-key ')).resolves.toEqual({
      success: true, message: 'Reward deferred until a durable provider report is reconciled.',
    });

    expect(mocks.payout).toHaveBeenCalledWith({ hardCoinUnits: 500, idempotencyKey: 'payout-key' });
    expect(mocks.cancelPayout).toHaveBeenCalledWith(' payout ');
    expect(mocks.conversion).toHaveBeenCalledWith({ principalHardCoinUnits: 250, idempotencyKey: 'conversion-key' });
    expect(mocks.topUp).toHaveBeenCalledWith({ hardCoinUnits: 1500, idempotencyKey: 'top-up-key' });
    expect(mocks.transfer).toHaveBeenCalledWith({ recipientUserId: 'recipient', amountUnits: 25, currency: 'HardCoin', transferType: 'Tip', idempotencyKey: 'transfer-key' });
    expect(mocks.startAdReward).toHaveBeenCalledWith({ network: 'google-ad-manager', creativeId: 'creative', requiredDurationSeconds: 30, idempotencyKey: 'ad-key' });
    expect(mocks.completeAdReward).toHaveBeenCalledWith('session', expect.objectContaining({ token: 'signed' }));
    expect(mocks.revalidatePath).toHaveBeenCalled();
    const config = mocks.createServerClient.mock.calls[0][0] as {
      auth: { getAccessToken: () => Promise<string> };
      baseUrl: string;
      tenant: { getTenantId: () => Promise<string | null> };
    };
    expect(config.baseUrl).toBe('https://api.internal');
    await expect(config.auth.getAccessToken()).resolves.toBe('token');
    await expect(config.tenant.getTenantId()).resolves.toBe('tenant-1');
  });

  it('covers alternate successful provider states', async () => {
    mocks.payoutOnboarding.mockResolvedValueOnce(ok({ onboardingUri: null }));
    mocks.completeAdReward.mockResolvedValueOnce(ok({ state: 'Posted' }));
    mocks.startAdReward.mockResolvedValueOnce(ok({ claims: null, token: null }));
    await expect(createPayoutOnboardingAction()).resolves.toEqual({
      success: true, message: 'Payout account refreshed.', data: { onboardingUri: null },
    });
    await expect(completeAdRewardSessionAction(adCompletion, 'key')).resolves.toEqual({ success: true, message: 'Ad reward evidence recorded.' });
    await expect(startAdRewardSessionAction('network', 'creative', 30, 'key')).resolves.toMatchObject({ data: { sessionId: undefined, signedToken: undefined } });
  });

  it('rejects every malformed intent before contacting the API', async () => {
    const invalidResults = await Promise.all([
      submitPayoutRequestAction(0, 'key'), submitPayoutRequestAction(1.5, 'key'), submitPayoutRequestAction(1, ' '),
      cancelPayoutRequestAction(' '),
      convertHardToSoftAction(0, 'key'), convertHardToSoftAction(1.5, 'key'), convertHardToSoftAction(1, ' '),
      createTopUpAction(0, 'key'), createTopUpAction(1.5, 'key'), createTopUpAction(1, ' '),
      createTransferAction(' ', 1, 'HardCoin', 'Tip', 'key'),
      createTransferAction('recipient', 0, 'HardCoin', 'Tip', 'key'),
      createTransferAction('recipient', 1.5, 'HardCoin', 'Tip', 'key'),
      createTransferAction('recipient', 1, 'HardCoin', 'Tip', ' '),
      startAdRewardSessionAction(' ', 'creative', 30, 'key'),
      startAdRewardSessionAction('network', ' ', 30, 'key'),
      startAdRewardSessionAction('network', 'creative', 0, 'key'),
      startAdRewardSessionAction('network', 'creative', 1.5, 'key'),
      startAdRewardSessionAction('network', 'creative', 30, ' '),
      startKycOnboardingAction(' '),
      createKycAccessTokenAction(59), createKycAccessTokenAction(901), createKycAccessTokenAction(60.5),
      createBountyAction({ ...bountyIntent, amountUnits: 0 }), createBountyAction({ ...bountyIntent, amountUnits: 1.5 }),
      createBountyAction({ ...bountyIntent, idempotencyKey: ' ' }), createBountyAction({ ...bountyIntent, expiresAt: ' ' }),
      claimBountyAction(' ', 'key'), claimBountyAction('bounty', ' '),
      reclaimBountyAction(' ', 'key'), reclaimBountyAction('bounty', ' '),
      completeAdRewardSessionAction({ ...adCompletion, sessionId: ' ' }, 'key'),
      completeAdRewardSessionAction({ ...adCompletion, signedToken: ' ' }, 'key'),
      completeAdRewardSessionAction(adCompletion, ' '),
    ]);
    expect(invalidResults.every((result) => !result.success)).toBe(true);
    expect(mocks.createServerClient).not.toHaveBeenCalled();
  });

  it('fails closed for every unauthenticated module family', async () => {
    for (const session of [null, () => undefined, { user: {} }]) {
      mocks.auth.mockResolvedValueOnce(session);
      await expect(submitPayoutRequestAction(1, 'key')).resolves.toMatchObject({ success: false });
    }
    mocks.auth.mockRejectedValueOnce(new Error('auth down'));
    await expect(submitPayoutRequestAction(1, 'key')).resolves.toMatchObject({ success: false });
    const unauthenticatedEconomyActions = [
      () => cancelPayoutRequestAction('request'),
      () => convertHardToSoftAction(1, 'key'),
      () => createTopUpAction(1, 'key'),
      () => createTransferAction('recipient', 1, 'HardCoin', 'Tip', 'key'),
      () => createPayoutOnboardingAction(),
    ];
    for (const invoke of unauthenticatedEconomyActions) {
      mocks.auth.mockResolvedValueOnce(null);
      await expect(invoke()).resolves.toMatchObject({ success: false });
    }
    for (const session of [null, () => undefined, { user: {} }]) {
      mocks.auth.mockResolvedValueOnce(session);
      await expect(startAdRewardSessionAction('network', 'creative', 30, 'key')).resolves.toMatchObject({ success: false });
    }
    mocks.auth.mockRejectedValueOnce(new Error('auth down'));
    await expect(startAdRewardSessionAction('network', 'creative', 30, 'key')).resolves.toMatchObject({ success: false });
    for (const session of [null, () => undefined, { user: {} }]) {
      mocks.auth.mockResolvedValueOnce(session);
      await expect(startKycOnboardingAction('key')).resolves.toMatchObject({ success: false });
    }
    mocks.auth.mockRejectedValueOnce(new Error('auth down'));
    await expect(startKycOnboardingAction('key')).resolves.toMatchObject({ success: false });
    mocks.auth.mockResolvedValueOnce(null);
    await expect(createKycAccessTokenAction()).resolves.toMatchObject({ success: false });
    mocks.auth.mockResolvedValueOnce(null);
    await expect(createBountyAction(bountyIntent)).resolves.toMatchObject({ success: false });
    mocks.auth.mockResolvedValueOnce(null);
    await expect(claimBountyAction('bounty', 'key')).resolves.toMatchObject({ success: false });
    mocks.auth.mockResolvedValueOnce(null);
    await expect(reclaimBountyAction('bounty', 'key')).resolves.toMatchObject({ success: false });
    mocks.auth.mockResolvedValueOnce(null);
    await expect(completeAdRewardSessionAction(adCompletion, 'key')).resolves.toMatchObject({ success: false });
  });

  it('binds access-token and tenant callbacks for every generated module family', async () => {
    await startAdRewardSessionAction('network', 'creative', 30, 'key');
    let config = mocks.createServerClient.mock.calls.at(-1)?.[0] as {
      auth: { getAccessToken: () => Promise<string> };
      tenant: { getTenantId: () => Promise<string | null> };
    };
    await expect(config.auth.getAccessToken()).resolves.toBe('token');
    await expect(config.tenant.getTenantId()).resolves.toBe('tenant-1');

    await startKycOnboardingAction('key');
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    await expect(config.auth.getAccessToken()).resolves.toBe('token');
    await expect(config.tenant.getTenantId()).resolves.toBe('tenant-1');

    mocks.auth.mockResolvedValueOnce({ user: { id: 'actor' }, tenantId: null });
    await startAdRewardSessionAction('network', 'creative', 30, 'key');
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    await expect(config.tenant.getTenantId()).resolves.toBeNull();

    mocks.auth.mockResolvedValueOnce({ user: { id: 'actor' }, tenantId: null });
    await startKycOnboardingAction('key');
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    await expect(config.tenant.getTenantId()).resolves.toBeNull();
  });

  it('uses public and local API fallbacks and null tenant context safely', async () => {
    process.env.NEXT_PUBLIC_API_URL = 'https://api.public';
    mocks.auth.mockResolvedValueOnce({ user: { id: 'actor' }, tenantId: null });
    await createTopUpAction(1, 'key');
    let config = mocks.createServerClient.mock.calls.at(-1)?.[0] as { baseUrl: string; tenant: { getTenantId: () => Promise<string | null> } };
    expect(config.baseUrl).toBe('https://api.public');
    await expect(config.tenant.getTenantId()).resolves.toBeNull();
    delete process.env.NEXT_PUBLIC_API_URL;
    await createTopUpAction(1, 'key');
    config = mocks.createServerClient.mock.calls.at(-1)?.[0] as typeof config;
    expect(config.baseUrl).toBe('http://localhost:8080');
  });

  it('returns provider diagnostics and safe fallbacks for every action', async () => {
    const cases: Array<[ReturnType<typeof vi.fn>, () => Promise<unknown>]> = [
      [mocks.payout, () => submitPayoutRequestAction(1, 'key')],
      [mocks.cancelPayout, () => cancelPayoutRequestAction('request')],
      [mocks.conversion, () => convertHardToSoftAction(1, 'key')],
      [mocks.topUp, () => createTopUpAction(1, 'key')],
      [mocks.transfer, () => createTransferAction('recipient', 1, 'HardCoin', 'Tip', 'key')],
      [mocks.payoutOnboarding, () => createPayoutOnboardingAction()],
      [mocks.startAdReward, () => startAdRewardSessionAction('network', 'creative', 30, 'key')],
      [mocks.kycOnboarding, () => startKycOnboardingAction('key')],
      [mocks.kycToken, () => createKycAccessTokenAction()],
      [mocks.createBounty, () => createBountyAction(bountyIntent)],
      [mocks.claimBounty, () => claimBountyAction('bounty', 'key')],
      [mocks.reclaimBounty, () => reclaimBountyAction('bounty', 'key')],
      [mocks.completeAdReward, () => completeAdRewardSessionAction(adCompletion, 'key')],
    ];
    for (const [provider, invoke] of cases) {
      provider.mockResolvedValueOnce(fail('provider error'));
      await expect(invoke()).resolves.toEqual({ success: false, message: 'provider error' });
      provider.mockResolvedValueOnce(fail(null));
      await expect(invoke()).resolves.toMatchObject({ success: false });
    }
  });
});
