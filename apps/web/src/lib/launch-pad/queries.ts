import { getToken } from '@/auth';
import { cache } from 'react';

export type LaunchPlanStatus = 'Draft' | 'Preparing' | 'Ready' | 'Launched' | 'Paused' | number;

export interface LaunchChecklistItem {
  id: string;
  title: string;
  category: string;
  isRequired: boolean;
  isComplete: boolean;
  completedAt?: string | null;
}

export interface LaunchProjectSummary {
  id: string;
  title?: string | null;
  name?: string | null;
  slug?: string | null;
  status?: string | number | null;
  visibility?: string | number | null;
}

export interface LaunchPlan {
  id: string;
  projectId: string;
  project?: LaunchProjectSummary | null;
  name: string;
  positioning?: string | null;
  targetLaunchAt?: string | null;
  launchedAt?: string | null;
  status: LaunchPlanStatus;
  readinessPercent?: number;
  channels?: string[] | null;
  checklistItems?: LaunchChecklistItem[] | null;
}

export interface LaunchProjectOption {
  id: string;
  title: string;
  slug?: string | null;
  status?: string | number | null;
}

async function launchPadApiGet<T>(path: string, revalidate = 30): Promise<T | null> {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  const token = await getToken();
  const response = await fetch(`${apiUrl}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    next: { revalidate },
  });

  if (response.status === 401 || response.status === 403 || response.status === 404) return null;

  if (!response.ok) {
    console.error(`[launchPadApiGet] ${path} failed with ${response.status}`);
    return null;
  }

  return (await response.json()) as T;
}

export const getLaunchPadDashboard = cache(async (): Promise<LaunchPlan[]> => {
  return (await launchPadApiGet<LaunchPlan[]>('/v1/launch-pad', 30)) ?? [];
});

export const getLaunchProjectOptions = cache(async (): Promise<LaunchProjectOption[]> => {
  const projects = await launchPadApiGet<LaunchProjectSummary[]>('/v1/projects?take=50&sortBy=UpdatedAt&sortDirection=DESC', 60);

  return (projects ?? []).map((project) => ({
    id: project.id,
    title: project.title ?? project.name ?? project.slug ?? project.id,
    slug: project.slug,
    status: project.status,
  }));
});

export function normalizeLaunchStatus(status: LaunchPlanStatus): string {
  if (typeof status === 'string') return status;

  return ['Draft', 'Preparing', 'Ready', 'Launched', 'Paused'][status] ?? 'Preparing';
}

export function getPlanReadiness(plan: LaunchPlan): number {
  if (typeof plan.readinessPercent === 'number') return Math.max(0, Math.min(100, Math.round(plan.readinessPercent)));

  const items = plan.checklistItems ?? [];
  if (items.length === 0) return 0;

  return Math.round((items.filter((item) => item.isComplete).length / items.length) * 100);
}
