import { getToken } from '@/auth';
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

const safeWorkspaceContext: DashboardContexts = {
  contexts: [
    { type: 'Workspace', id: null, name: 'Workspace', route: '/dashboard' },
  ],
  capabilities: [],
  counts: { teams: 0, projects: 0, pendingTasks: 0, invitations: 0 },
  navigation: [
    {
      label: 'Overview',
      items: [
        { title: 'Dashboard', route: '/dashboard', children: [] },
        { title: 'Invitations', route: '/dashboard/invitations', children: [] },
      ],
    },
  ],
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
  const apiUrl =
    process.env.API_URL ||
    process.env.NEXT_PUBLIC_API_URL ||
    'http://localhost:8080';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });

  try {
    const result = await client.request<DashboardContexts>({
      method: 'GET',
      path: '/v1/dashboard/contexts',
    });

    if (!result.ok || !result.data) return safeWorkspaceContext;

    return {
      contexts: Array.isArray(result.data.contexts)
        ? result.data.contexts
        : safeWorkspaceContext.contexts,
      capabilities: Array.isArray(result.data.capabilities)
        ? result.data.capabilities
        : [],
      counts:
        result.data.counts && typeof result.data.counts === 'object'
          ? result.data.counts
          : safeWorkspaceContext.counts,
      navigation: Array.isArray(result.data.navigation)
        ? result.data.navigation
        : safeWorkspaceContext.navigation,
    };
  } catch {
    return safeWorkspaceContext;
  }
}
