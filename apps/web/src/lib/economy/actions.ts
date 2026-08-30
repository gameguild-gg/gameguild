'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { revalidatePath } from 'next/cache';

export interface EconomyActionResult {
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
