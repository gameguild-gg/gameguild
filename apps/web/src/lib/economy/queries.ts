import { auth, getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type APIControllersEconomySelfServiceCapability,
  type EconomyContractsEconomyWalletSummary,
  type EconomyContractsEconomyWalletTransaction,
  type EconomyPayoutsQueriesEconomyPayoutInput,
  type EconomyPayoutsQueriesEconomyPayoutOperation,
} from '@game-guild/client';
import { cache } from 'react';

export interface EconomyWorkspaceData {
  capabilities: APIControllersEconomySelfServiceCapability[];
  issue: string | null;
  payoutOperations: EconomyPayoutsQueriesEconomyPayoutOperation[];
  payoutRequests: EconomyPayoutsQueriesEconomyPayoutInput[];
  transactions: EconomyContractsEconomyWalletTransaction[];
  wallet: EconomyContractsEconomyWalletSummary | null;
}

function getApiUrl() {
  return process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
}

async function createEconomyModule() {
  const session = await auth().catch(() => null);
  const client = createServerClient({
    baseUrl: getApiUrl(),
    auth: { getAccessToken: () => getToken() },
    tenant: {
      getTenantId: async () =>
        session && typeof session !== 'function' ? (session.tenantId ?? null) : null,
    },
  });

  return new GeneratedApi.EconomyModule(client);
}

function resultIssue(label: string, result: { ok: boolean; error?: { message?: string | null } }) {
  return result.ok ? null : `${label}: ${result.error?.message || 'unavailable'}`;
}

export const getEconomyWorkspaceData = cache(async (): Promise<EconomyWorkspaceData> => {
  const economy = await createEconomyModule();
  const [wallet, transactions, capabilities, payoutRequests, payoutOperations] = await Promise.all([
    economy.getEconomyWallet(),
    economy.getEconomyWalletTransactions({ take: 50 }),
    economy.getEconomyCapabilities(),
    economy.getEconomyPayoutRequests({ take: 50 }),
    economy.getEconomyPayoutsForGetEconomyPayouts({ take: 50 }),
  ]);

  const issues = [
    resultIssue('Wallet', wallet),
    resultIssue('Transactions', transactions),
    resultIssue('Capabilities', capabilities),
    resultIssue('Payout requests', payoutRequests),
    resultIssue('Payout operations', payoutOperations),
  ].filter((issue): issue is string => issue !== null);

  return {
    wallet: wallet.ok ? wallet.data : null,
    transactions: transactions.ok ? transactions.data : [],
    capabilities: capabilities.ok ? capabilities.data : [],
    payoutRequests: payoutRequests.ok ? payoutRequests.data : [],
    payoutOperations: payoutOperations.ok ? payoutOperations.data : [],
    issue: issues.length ? issues.join(' · ') : null,
  };
});
