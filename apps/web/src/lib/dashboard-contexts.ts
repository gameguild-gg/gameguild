import { getRequestAuthContext } from '@/auth';
import { createServerClient } from '@game-guild/client';

export type DashboardContextType = 'Workspace' | 'Team' | 'Project' | 'Operations';

export interface DashboardContextSummary {
  type: DashboardContextType;
  id: string | null;
  name: string;
  route: string;
}

export interface DashboardContexts {
  contexts: DashboardContextSummary[];
  capabilities: string[];
  counts: DashboardWorkspaceCounts;
  navigation: DashboardNavigationGroup[];
}

export interface DashboardNavigationItem {
  title: string;
  route: string | null;
  children: DashboardNavigationItem[];
}

export interface DashboardNavigationGroup {
  label: string;
  items: DashboardNavigationItem[];
}

export interface DashboardWorkspaceCounts {
  teams: number;
  projects: number;
  pendingTasks: number;
  invitations: number;
}

const safeManagementContext: DashboardContexts = {
  contexts: [],
  capabilities: [],
  counts: { teams: 0, projects: 0, pendingTasks: 0, invitations: 0 },
  navigation: [],
};

export function hasAnyDashboardCapability(
  capabilities: readonly string[],
  ...allowedPrefixes: string[]
): boolean {
  return capabilities.some((capability) =>
    allowedPrefixes.some((prefix) => capability.startsWith(prefix)),
  );
}

export async function getDashboardContexts(): Promise<DashboardContexts> {
  const requestAuth = await getRequestAuthContext();
  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: async () => requestAuth.token },
    tenant: { getTenantId: async () => requestAuth.tenantId },
  });

  try {
    const result = await client.request<{ capabilities?: unknown }>({
      method: 'GET',
      path: '/v1/access/capabilities',
    });

    if (!result.ok || !result.data) return safeManagementContext;

    const capabilities = Array.isArray(result.data.capabilities)
      ? result.data.capabilities.filter((capability): capability is string => typeof capability === 'string')
      : [];

    return {
      contexts: capabilities.length > 0
        ? [{ type: 'Operations', id: null, name: 'Operations', route: '/dashboard' }]
        : [],
      capabilities,
      counts: safeManagementContext.counts,
      navigation: safeManagementContext.navigation,
    };
  } catch {
    return safeManagementContext;
  }
}
