'use server';

import { getToken } from '@/auth';
import type { LaunchPlan } from './queries';
import { revalidatePath } from 'next/cache';

type ActionResult<T> = { success: true; data: T } | { success: false; error: string };

const defaultChecklist = [
  { title: 'Landing page copy and media approved', category: 'Storefront', isRequired: true },
  { title: 'Release build or playable package smoke tested', category: 'Quality', isRequired: true },
  { title: 'Distribution channels and launch date confirmed', category: 'Distribution', isRequired: true },
  { title: 'Support intake and known-issues process ready', category: 'Support', isRequired: true },
  { title: 'Launch metrics and post-launch review prepared', category: 'Analytics', isRequired: true },
];

async function launchPadApiRequest<T>(path: string, init: RequestInit): Promise<ActionResult<T>> {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  const token = await getToken();
  const response = await fetch(`${apiUrl}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init.headers ?? {}),
    },
  });

  if (!response.ok) {
    let error = response.statusText || 'Launch Pad request failed.';
    try {
      const body = await response.json() as { detail?: string; message?: string; description?: string; code?: string };
      error = body.detail || body.message || body.description || body.code || error;
    } catch {
      // Keep the HTTP status text when no JSON error payload is available.
    }

    return { success: false, error };
  }

  return { success: true, data: await response.json() as T };
}

function parseChecklist(raw: string) {
  const parsed = raw
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line) => {
      const [category, ...titleParts] = line.includes(':') ? line.split(':') : ['Readiness', line];
      return {
        category: category.trim() || 'Readiness',
        title: titleParts.join(':').trim() || line,
        isRequired: true,
      };
    });

  return parsed.length > 0 ? parsed : defaultChecklist;
}

export async function createLaunchPlan(formData: FormData): Promise<void> {
  const projectId = String(formData.get('projectId') ?? '').trim();
  const name = String(formData.get('name') ?? '').trim();
  const positioning = String(formData.get('positioning') ?? '').trim();
  const targetLaunchAt = String(formData.get('targetLaunchAt') ?? '').trim();
  const channels = formData.getAll('channels').map(String).map((channel) => channel.trim()).filter(Boolean);
  const checklist = parseChecklist(String(formData.get('checklist') ?? ''));

  if (!projectId || !name) return;

  const result = await launchPadApiRequest<LaunchPlan>('/v1/launch-pad', {
    method: 'POST',
    body: JSON.stringify({
      projectId,
      name,
      positioning: positioning || null,
      targetLaunchAt: targetLaunchAt ? new Date(targetLaunchAt).toISOString() : null,
      channels,
      checklistItems: checklist,
    }),
  });

  if (result.success) revalidatePath('/dashboard/launch-pad');
}

export async function completeLaunchChecklistItem(formData: FormData): Promise<void> {
  const planId = String(formData.get('planId') ?? '').trim();
  const itemId = String(formData.get('itemId') ?? '').trim();
  if (!planId || !itemId) return;

  const result = await launchPadApiRequest<LaunchPlan>(`/v1/launch-pad/${planId}/checklist/${itemId}:complete`, {
    method: 'POST',
    body: JSON.stringify({}),
  });

  if (result.success) revalidatePath('/dashboard/launch-pad');
}

export async function publishLaunchPlan(formData: FormData): Promise<void> {
  const planId = String(formData.get('planId') ?? '').trim();
  if (!planId) return;

  const result = await launchPadApiRequest<LaunchPlan>(`/v1/launch-pad/${planId}:publish`, {
    method: 'POST',
    body: JSON.stringify({}),
  });

  if (result.success) revalidatePath('/dashboard/launch-pad');
}
