import { auth, getToken } from '@/auth';
import { getDashboardContexts, hasAnyDashboardCapability } from '@/lib/dashboard-contexts';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { forbidden } from 'next/navigation';

export const economyConsoleSurfaces = {
  readiness: { capability: 'Economy.ReadOperations', label: 'Readiness' },
  'payout-reviews': { capability: 'Economy.ReviewPayouts', label: 'Payout review' },
  'payout-operations': { capability: 'Economy.OperatePayouts', label: 'Payout operations' },
  'risk-reviews': { capability: 'Economy.OperateCompliance', label: 'Risk review' },
  'financial-crime': { capability: 'Economy.OperateCompliance', label: 'Financial Crime' },
  'trust-safety': { capability: 'Economy.OperateCompliance', label: 'Trust & Safety' },
  policies: { capability: 'Economy.ManagePolicies', label: 'Policies' },
  reserves: { capability: 'Economy.ManageReserves', label: 'Reserve & custody' },
  ledger: { capability: 'Economy.OperateLedger', label: 'Ledger & anchors' },
  'kill-switches': { capability: 'Economy.ManageKillSwitches', label: 'Kill switches' },
  'ad-rewards': { capability: 'Economy.OperateAdRewards', label: 'Ad rewards' },
  marketplace: { capability: 'Economy.OperateMarketplace', label: 'Marketplace' },
  bounties: { capability: 'Economy.OperateBounties', label: 'Bounties' },
  treasury: { capability: 'Economy.OperateTreasury', label: 'Treasury' },
  'legacy-migration': { capability: 'Economy.ManageLegacyMigration', label: 'Legacy migration' },
} as const;

export type EconomyConsoleSurface = keyof typeof economyConsoleSurfaces;

export async function requireEconomyConsoleSurface(surface: EconomyConsoleSurface) {
  const contexts = await getDashboardContexts();
  const capability = economyConsoleSurfaces[surface].capability;
  if (!hasAnyDashboardCapability(contexts.capabilities, capability)) forbidden();
  return contexts;
}

function getApiUrl() {
  return process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
}

export async function createEconomyConsoleModules() {
  const session = await auth().catch(() => null);
  const client = createServerClient({
    baseUrl: getApiUrl(),
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => session && typeof session !== 'function' ? (session.tenantId ?? null) : null },
  });
  return {
    admin: new GeneratedApi.EconomyAdministrationModule(client),
    authStepUp: new GeneratedApi.AuthStepUpModule(client),
    compliance: new GeneratedApi.EconomyComplianceAdministrationModule(client),
    holds: new GeneratedApi.EconomyComplianceHoldAdministrationModule(client),
    legacy: new GeneratedApi.EconomyLegacyMigrationAdministrationModule(client),
    risk: new GeneratedApi.EconomyRiskReviewAdministrationModule(client),
    treasury: new GeneratedApi.EconomyTreasuryAdministrationModule(client),
  };
}

interface ApiResult {
  data?: unknown;
  error?: { message?: string | null };
  ok: boolean;
}

function records(data: unknown): Array<Record<string, unknown>> {
  if (Array.isArray(data)) return data.filter((item): item is Record<string, unknown> => Boolean(item) && typeof item === 'object');
  if (!data || typeof data !== 'object') return [];
  const value = data as Record<string, unknown>;
  if (Array.isArray(value.items)) return records(value.items);
  return [value];
}

export interface EconomyConsoleData {
  issue: string | null;
  records: Array<Record<string, unknown>>;
  sections: Array<{ label: string; records: Array<Record<string, unknown>> }>;
}

export async function getEconomyConsoleData(surface: EconomyConsoleSurface): Promise<EconomyConsoleData> {
  const modules = await createEconomyConsoleModules();
  let requests: Array<[string, Promise<ApiResult>]>;
  switch (surface) {
    case 'readiness':
      requests = [
        ['Capabilities', modules.admin.getAdminEconomyCapabilitiesConfiguration({ limit: 100 })],
        ['Ledger health', modules.admin.getAdminEconomyLedgerHealth()],
        ['Active reserve', modules.admin.getAdminEconomyReservesActive()],
      ];
      break;
    case 'payout-operations':
      requests = [['Operations', modules.admin.getAdminEconomyPayoutRequestsOperationsForGetAdminEconomyPayoutRequestsOperations({ take: 100 })]];
      break;
    case 'risk-reviews':
      requests = [
        ['Reviews', modules.risk.getAdminEconomyRiskReviewsForGetAdminEconomyRiskReviews({ limit: 100 })],
        ['Compliance holds', modules.holds.getAdminEconomyComplianceHoldsForGetAdminEconomyComplianceHolds({ limit: 100 })],
      ];
      break;
    case 'financial-crime':
      requests = [['Cases', modules.compliance.getAdminEconomyComplianceFinancialCrimeCasesForGetAdminEconomyComplianceFinancialCrimeCases({ take: 100 })]];
      break;
    case 'trust-safety':
      requests = [['Appeals', modules.compliance.getAdminEconomyComplianceTrustSafetyAppeals({ take: 100 })]];
      break;
    case 'policies':
      requests = [['Policies', modules.admin.getAdminEconomyPoliciesForGetAdminEconomyPolicies({ limit: 100 })]];
      break;
    case 'reserves':
      requests = [
        ['Active head', modules.admin.getAdminEconomyReservesActive()],
        ['Liabilities', modules.admin.getAdminEconomyReservesLiabilities()],
        ['Proposals', modules.admin.getAdminEconomyReservesProposalsForGetAdminEconomyReservesProposals({ limit: 100 })],
        ['Custody', modules.admin.getAdminEconomyCustodyObservationsForGetAdminEconomyCustodyObservations({ limit: 100 })],
      ];
      break;
    case 'ledger':
      requests = [
        ['Health', modules.admin.getAdminEconomyLedgerHealth()],
        ['Verification runs', modules.admin.getAdminEconomyLedgerVerificationRunsForGetAdminEconomyLedgerVerificationRuns({ limit: 100 })],
        ['Anchors', modules.admin.getAdminEconomyLedgerAnchorsForGetAdminEconomyLedgerAnchors({ limit: 100 })],
        ['Projection generations', modules.admin.getAdminEconomyLedgerProjectionGenerationsForGetAdminEconomyLedgerProjectionGenerations({ limit: 100 })],
      ];
      break;
    case 'kill-switches':
      requests = [['Capability epochs', modules.admin.getAdminEconomyCapabilitiesConfiguration({ limit: 100 })]];
      break;
    case 'ad-rewards':
      requests = [
        ['Sessions', modules.admin.getAdminEconomyAdRewardsSessionsForGetAdminEconomyAdRewardsSessions({ limit: 100 })],
        ['Pending claims', modules.admin.getAdminEconomyAdRewardsPendingClaims({ limit: 100 })],
        ['Reports', modules.admin.getAdminEconomyAdRewardsReports({ limit: 100 })],
        ['Reconciliations', modules.admin.getAdminEconomyAdRewardsReconciliations({ limit: 100 })],
      ];
      break;
    case 'marketplace':
      requests = [
        ['Settlements', modules.admin.getAdminEconomyMarketplaceSettlementsForGetAdminEconomyMarketplaceSettlements({ limit: 100 })],
        ['Refunds', modules.admin.getAdminEconomyMarketplaceRefundsForGetAdminEconomyMarketplaceRefunds({ limit: 100 })],
        ['Outbox', modules.admin.getAdminEconomyMarketplaceOutbox({ limit: 100 })],
      ];
      break;
    case 'bounties':
      requests = [['Expired bounties', modules.admin.getAdminEconomyBountiesExpired()]];
      break;
    case 'treasury':
      requests = [['Withdrawals', modules.treasury.getAdminEconomyTreasuryWithdrawalsForGetAdminEconomyTreasuryWithdrawals({ limit: 100 })]];
      break;
    case 'legacy-migration':
      requests = [['Batches', modules.legacy.getAdminEconomyLegacyMigrationBatchesForGetAdminEconomyLegacyMigrationBatches({ limit: 100 })]];
      break;
    default:
      requests = [];
  }

  const resolved = await Promise.all(requests.map(async ([label, request]) => [label, await request] as const));
  const issues = resolved.filter(([, result]) => !result.ok).map(([label, result]) => `${label}: ${result.error?.message || 'unavailable'}`);
  const sections = resolved.filter(([, result]) => result.ok).map(([label, result]) => ({ label, records: records(result.data) }));
  return { issue: issues.length ? issues.join(' · ') : null, records: sections.flatMap((section) => section.records), sections };
}
