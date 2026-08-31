'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { revalidatePath } from 'next/cache';

export interface EconomyActionResult<T = never> {
  data?: T;
  message: string;
  success: boolean;
}

function getApiUrl() {
  return process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
}

async function createEconomyModule() {
  const session = await auth().catch(() => null);
  if (!session || typeof session === 'function' || !session.user?.id) return null;

  const client = createServerClient({
    baseUrl: getApiUrl(),
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => session.tenantId ?? null },
  });

  return new GeneratedApi.EconomyModule(client);
}

async function createAdRewardsModule() {
  const session = await auth().catch(() => null);
  if (!session || typeof session === 'function' || !session.user?.id) return null;

  const client = createServerClient({
    baseUrl: getApiUrl(),
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => session.tenantId ?? null },
  });

  return new GeneratedApi.EconomyAdRewardsModule(client);
}

async function createSpecializedEconomyModules() {
  const session = await auth().catch(() => null);
  if (!session || typeof session === 'function' || !session.user?.id) return null;

  const client = createServerClient({
    baseUrl: getApiUrl(),
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => session.tenantId ?? null },
  });

  return {
    bounties: new GeneratedApi.EconomyBountiesModule(client),
    kyc: new GeneratedApi.EconomyKycModule(client),
  };
}

function failure(message: string): EconomyActionResult {
  return { success: false, message };
}

function refreshEconomy() {
  revalidatePath('/', 'layout');
}

export async function submitPayoutRequestAction(
  hardCoinUnits: number,
  idempotencyKey: string,
): Promise<EconomyActionResult> {
  if (!Number.isSafeInteger(hardCoinUnits) || hardCoinUnits <= 0) {
    return failure('Enter a whole, positive HardCoin amount.');
  }
  if (!idempotencyKey.trim()) return failure('The payout request could not be identified safely.');

  const economy = await createEconomyModule();
  if (!economy) return failure('Sign in before submitting a payout request.');

  const result = await economy.postEconomyPayoutRequests({
    hardCoinUnits,
    idempotencyKey: idempotencyKey.trim(),
  });
  if (!result.ok) return failure(result.error.message || 'The payout request was not accepted.');

  refreshEconomy();
  return { success: true, message: 'Payout request recorded. No value has been reserved or dispatched.' };
}

export async function cancelPayoutRequestAction(requestId: string): Promise<EconomyActionResult> {
  if (!requestId.trim()) return failure('The payout request ID is required.');

  const economy = await createEconomyModule();
  if (!economy) return failure('Sign in before cancelling a payout request.');

  const result = await economy.postEconomyPayoutRequestsCancel(requestId);
  if (!result.ok) return failure(result.error.message || 'The payout request could not be cancelled.');

  refreshEconomy();
  return { success: true, message: 'Payout request cancelled.' };
}

export async function convertHardToSoftAction(
  principalHardCoinUnits: number,
  idempotencyKey: string,
): Promise<EconomyActionResult> {
  if (!Number.isSafeInteger(principalHardCoinUnits) || principalHardCoinUnits <= 0) {
    return failure('Enter a whole, positive HardCoin amount.');
  }
  if (!idempotencyKey.trim()) return failure('The conversion could not be identified safely.');

  const economy = await createEconomyModule();
  if (!economy) return failure('Sign in before converting HardCoin.');

  const result = await economy.postEconomyConversionsHardToSoft({
    principalHardCoinUnits,
    idempotencyKey: idempotencyKey.trim(),
  });
  if (!result.ok) return failure(result.error.message || 'The conversion is currently unavailable.');

  refreshEconomy();
  return { success: true, message: 'Conversion recorded in the Economy journal.' };
}

export async function createTopUpAction(
  hardCoinUnits: number,
  idempotencyKey: string,
): Promise<EconomyActionResult<{
  clientSecret?: string | null;
  publishableKey?: string | null;
  status?: string | null;
  topUpId?: string | null;
}>> {
  if (!Number.isSafeInteger(hardCoinUnits) || hardCoinUnits <= 0) {
    return failure('Enter a whole, positive HardCoin amount.');
  }
  if (!idempotencyKey.trim()) return failure('The top-up could not be identified safely.');

  const economy = await createEconomyModule();
  if (!economy) return failure('Sign in before starting a top-up.');

  const result = await economy.postEconomyTopUps({
    hardCoinUnits,
    idempotencyKey: idempotencyKey.trim(),
  });
  if (!result.ok) return failure(result.error.message || 'The top-up is currently unavailable.');

  refreshEconomy();
  return {
    success: true,
    message: 'Top-up created. Stripe confirmation is still required.',
    data: {
      clientSecret: result.data.clientSecret,
      publishableKey: result.data.publishableKey,
      status: result.data.status,
      topUpId: result.data.topUpId,
    },
  };
}

export async function createTransferAction(
  recipientUserId: string,
  amountUnits: number,
  currency: 'HardCoin' | 'SoftCoin',
  transferType: 'Tip' | 'Gift' | 'CreatorSupport',
  idempotencyKey: string,
): Promise<EconomyActionResult> {
  if (!recipientUserId.trim()) return failure('Choose a valid recipient.');
  if (!Number.isSafeInteger(amountUnits) || amountUnits <= 0) {
    return failure('Enter a whole, positive transfer amount.');
  }
  if (!idempotencyKey.trim()) return failure('The transfer could not be identified safely.');

  const economy = await createEconomyModule();
  if (!economy) return failure('Sign in before sending a transfer.');

  const result = await economy.postEconomyTransfers({
    recipientUserId: recipientUserId.trim(),
    amountUnits,
    currency,
    transferType,
    idempotencyKey: idempotencyKey.trim(),
  });
  if (!result.ok) return failure(result.error.message || 'The transfer is currently unavailable.');

  refreshEconomy();
  return { success: true, message: 'Transfer recorded in the Economy journal.' };
}

export async function startAdRewardSessionAction(
  network: string,
  creativeId: string,
  requiredDurationSeconds: number,
  idempotencyKey: string,
): Promise<EconomyActionResult<{ sessionId?: string; signedToken?: string | null }>> {
  if (!network.trim() || !creativeId.trim()) return failure('The ad network and creative are required.');
  if (!Number.isSafeInteger(requiredDurationSeconds) || requiredDurationSeconds <= 0) {
    return failure('The rewarded experience duration is invalid.');
  }
  if (!idempotencyKey.trim()) return failure('The ad session could not be identified safely.');

  const adRewards = await createAdRewardsModule();
  if (!adRewards) return failure('Sign in before starting an ad reward session.');

  const result = await adRewards.postEconomyAdRewardsSessions({
    network: network.trim(),
    creativeId: creativeId.trim(),
    requiredDurationSeconds,
    idempotencyKey: idempotencyKey.trim(),
  });
  if (!result.ok) return failure(result.error.message || 'The ad reward session is currently unavailable.');

  refreshEconomy();
  return {
    success: true,
    message: 'Ad reward session created. Browser callbacks cannot issue value.',
    data: {
      sessionId: result.data.claims?.sessionId,
      signedToken: result.data.token?.value,
    },
  };
}

export async function startKycOnboardingAction(
  idempotencyKey: string,
): Promise<EconomyActionResult<{ state?: string; updatedAt?: string }>> {
  if (!idempotencyKey.trim()) return failure('The KYC onboarding could not be identified safely.');
  const modules = await createSpecializedEconomyModules();
  if (!modules) return failure('Sign in before starting KYC onboarding.');

  const result = await modules.kyc.postEconomyKycOnboarding({ idempotencyKey: idempotencyKey.trim() });
  if (!result.ok) return failure(result.error.message || 'KYC onboarding is currently unavailable.');

  refreshEconomy();
  return {
    success: true,
    message: 'KYC onboarding is ready.',
    data: { state: result.data.state, updatedAt: result.data.updatedAt },
  };
}

export async function createKycAccessTokenAction(
  lifetimeSeconds = 600,
): Promise<EconomyActionResult<{ externalUserId?: string | null; token?: string | null }>> {
  if (!Number.isSafeInteger(lifetimeSeconds) || lifetimeSeconds < 60 || lifetimeSeconds > 900) {
    return failure('The KYC access token lifetime must be between 60 and 900 seconds.');
  }
  const modules = await createSpecializedEconomyModules();
  if (!modules) return failure('Sign in before opening KYC verification.');

  const result = await modules.kyc.postEconomyKycAccessToken({ lifetimeSeconds });
  if (!result.ok) return failure(result.error.message || 'KYC verification is currently unavailable.');
  return { success: true, message: 'KYC access token issued.', data: result.data };
}

export async function createPayoutOnboardingAction(): Promise<EconomyActionResult<{ onboardingUri?: string | null }>> {
  const economy = await createEconomyModule();
  if (!economy) return failure('Sign in before opening payout onboarding.');

  const result = await economy.postEconomyPayoutsOnboarding();
  if (!result.ok) return failure(result.error.message || 'Payout onboarding is currently unavailable.');
  refreshEconomy();
  return {
    success: true,
    message: result.data.onboardingUri ? 'Payout onboarding link created.' : 'Payout account refreshed.',
    data: { onboardingUri: result.data.onboardingUri },
  };
}

export interface CreateBountyIntent {
  amountUnits: number;
  currency: 'HardCoin' | 'SoftCoin';
  expiresAt: string;
  idempotencyKey: string;
  minimumReputation: number;
  requiresInstructorVerification: boolean;
  requiresPrerequisite: boolean;
}

export async function createBountyAction(input: CreateBountyIntent): Promise<EconomyActionResult> {
  if (!Number.isSafeInteger(input.amountUnits) || input.amountUnits <= 0) {
    return failure('Enter a whole, positive bounty amount.');
  }
  if (!input.idempotencyKey.trim() || !input.expiresAt.trim()) {
    return failure('Bounty expiry and idempotency key are required.');
  }
  const modules = await createSpecializedEconomyModules();
  if (!modules) return failure('Sign in before creating a bounty.');

  const result = await modules.bounties.postEconomyBounties({
    ...input,
    idempotencyKey: input.idempotencyKey.trim(),
  });
  if (!result.ok) return failure(result.error.message || 'The bounty is currently unavailable.');
  refreshEconomy();
  return { success: true, message: 'Bounty posted with durable escrow.' };
}

async function completeBountyAction(
  bountyId: string,
  idempotencyKey: string,
  operation: 'claim' | 'reclaim',
): Promise<EconomyActionResult> {
  if (!bountyId.trim() || !idempotencyKey.trim()) return failure('Bounty and idempotency key are required.');
  const modules = await createSpecializedEconomyModules();
  if (!modules) return failure(`Sign in before attempting to ${operation} a bounty.`);

  const result = operation === 'claim'
    ? await modules.bounties.postEconomyBountiesClaim(bountyId.trim(), { idempotencyKey: idempotencyKey.trim() })
    : await modules.bounties.postEconomyBountiesReclaim(bountyId.trim(), { idempotencyKey: idempotencyKey.trim() });
  if (!result.ok) {
    return failure(result.error.message || `The bounty could not be ${operation}ed.`);
  } else {
    refreshEconomy();
    return { success: true, message: operation === 'claim' ? 'Bounty claimed.' : 'Bounty reclaimed.' };
  }
}

export async function claimBountyAction(bountyId: string, idempotencyKey: string) {
  return completeBountyAction(bountyId, idempotencyKey, 'claim');
}

export async function reclaimBountyAction(bountyId: string, idempotencyKey: string) {
  return completeBountyAction(bountyId, idempotencyKey, 'reclaim');
}

export interface CompleteAdRewardIntent {
  completedAt: string;
  creativeId: string;
  network: string;
  playbackDuration: string;
  sessionId: string;
  signedToken: string;
  startedAt: string;
  visibleDuration: string;
}

export async function completeAdRewardSessionAction(
  input: CompleteAdRewardIntent,
  idempotencyKey: string,
): Promise<EconomyActionResult> {
  if (!input.sessionId.trim() || !input.signedToken.trim() || !idempotencyKey.trim()) {
    return failure('The signed ad session and idempotency key are required.');
  }
  const adRewards = await createAdRewardsModule();
  if (!adRewards) return failure('Sign in before completing an ad reward session.');

  const result = await adRewards.postEconomyAdRewardsSessionsComplete(input.sessionId.trim(), {
    idempotencyKey: idempotencyKey.trim(),
    token: input.signedToken,
    playback: {
      completedAt: input.completedAt,
      focusLoss: '00:00:00',
      milestones: [25, 50, 75, 100],
      playbackDuration: input.playbackDuration,
      startedAt: input.startedAt,
      visibleDuration: input.visibleDuration,
    },
    providerProof: {
      completedAt: input.completedAt,
      creativeId: input.creativeId,
      network: input.network,
      sessionId: input.sessionId,
    },
  });
  if (!result.ok) return failure(result.error.message || 'The ad reward proof was not accepted.');
  refreshEconomy();
  return {
    success: true,
    message: result.data.state === 'PendingProviderReport'
      ? 'Reward deferred until a durable provider report is reconciled.'
      : 'Ad reward evidence recorded.',
  };
}
