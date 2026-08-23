'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { revalidatePath } from 'next/cache';

export interface EconomyPayoutReviewActionResult {
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

function failure(message: string): EconomyPayoutReviewActionResult {
  return { success: false, message };
}

export async function reviewPayoutRequestAction(
  requestId: string,
  outcome: 'approve' | 'reject',
  reason: string,
): Promise<EconomyPayoutReviewActionResult> {
  if (!requestId.trim()) return failure('The payout request ID is required.');
  if (!reason.trim()) return failure('An immutable decision reason is required.');

  const economy = await createEconomyModule();
  if (!economy) return failure('Sign in with wallet-administration permission before reviewing payouts.');

  const body = { reason: reason.trim() };
  const result = outcome === 'approve'
    ? await economy.postAdminEconomyPayoutRequestsApprove(requestId, body)
    : await economy.postAdminEconomyPayoutRequestsReject(requestId, body);

  if (!result.ok) return failure(result.error.message || 'The payout review decision was not accepted.');

  revalidatePath('/', 'layout');
  return {
    success: true,
    message: outcome === 'approve'
      ? 'Approval recorded. A second independent tenant administrator is still required before a final approval.'
      : 'Rejection recorded with an immutable reason. No value was dispatched.',
  };
}
