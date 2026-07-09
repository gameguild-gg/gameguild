'use server';

import { getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import type { SocialGroupsSocialGroupType, SocialGroupsSocialGroupVisibility } from '@game-guild/client';
import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';

const DASHBOARD_GROUPS_PATH = '/dashboard/community/members/groups';
const GROUP_TYPES = new Set<SocialGroupsSocialGroupType>(['StudyGroup', 'ProjectTeam', 'InterestCommunity', 'CourseCohort', 'Institution', 'GameJamTeam']);
const GROUP_VISIBILITIES = new Set<SocialGroupsSocialGroupVisibility>(['Public', 'Private', 'InviteOnly']);

function buildGroupsHref(params: { message?: string; error?: string }) {
  const searchParams = new URLSearchParams();
  if (params.message) searchParams.set('message', params.message);
  if (params.error) searchParams.set('error', params.error);

  const suffix = searchParams.toString();
  return `${DASHBOARD_GROUPS_PATH}${suffix ? `?${suffix}` : ''}`;
}

function readText(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function slugify(value: string) {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function createClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

export async function createCommunityGroup(formData: FormData) {
  const name = readText(formData, 'name');
  const description = readText(formData, 'description');
  const rawType = readText(formData, 'type') as SocialGroupsSocialGroupType;
  const rawVisibility = readText(formData, 'visibility') as SocialGroupsSocialGroupVisibility;
  const type = GROUP_TYPES.has(rawType) ? rawType : 'InterestCommunity';
  const visibility = GROUP_VISIBILITIES.has(rawVisibility) ? rawVisibility : 'Public';

  if (!name) {
    redirect(buildGroupsHref({ error: 'Group name is required.' }));
  }

  const client = createClient();
  const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
  const result = await socialGroups.postApiSocialGroups({
    name,
    slug: slugify(name),
    description: description || null,
    type,
    visibility,
  });

  if (!result.ok) {
    redirect(buildGroupsHref({ error: result.error.message }));
  }

  revalidatePath('/dashboard/community');
  revalidatePath('/dashboard/community/members');
  revalidatePath(DASHBOARD_GROUPS_PATH);
  redirect(buildGroupsHref({ message: `Created ${name}.` }));
}
