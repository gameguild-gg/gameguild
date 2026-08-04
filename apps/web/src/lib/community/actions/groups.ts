'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import type { SocialGroupsSocialGroupMemberRole, SocialGroupsSocialGroupType, SocialGroupsSocialGroupVisibility } from '@game-guild/client';
import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';

const DASHBOARD_GROUPS_PATH = '/dashboard/community/members/groups';
const GROUP_TYPES = new Set<SocialGroupsSocialGroupType>(['StudyGroup', 'ProjectTeam', 'InterestCommunity', 'CourseCohort', 'Institution', 'GameJamTeam']);
const GROUP_VISIBILITIES = new Set<SocialGroupsSocialGroupVisibility>(['Public', 'Private', 'InviteOnly']);
const GROUP_ROLES = new Set<SocialGroupsSocialGroupMemberRole>(['Owner', 'Admin', 'Moderator', 'Member']);

function buildGroupsHref(params: { message?: string; error?: string }) {
  const searchParams = new URLSearchParams();
  if (params.message) searchParams.set('message', params.message);
  if (params.error) searchParams.set('error', params.error);

  const suffix = searchParams.toString();
  return `${DASHBOARD_GROUPS_PATH}${suffix ? `?${suffix}` : ''}`;
}

function buildGroupHref(groupId: string, params: { message?: string; error?: string }) {
  const searchParams = new URLSearchParams();
  if (params.message) searchParams.set('message', params.message);
  if (params.error) searchParams.set('error', params.error);

  const suffix = searchParams.toString();
  return `${DASHBOARD_GROUPS_PATH}/${groupId}${suffix ? `?${suffix}` : ''}`;
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
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
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

  const session = await auth().catch(() => null);
  const ownerId = session?.user?.id?.trim();
  if (!ownerId) {
    redirect(buildGroupsHref({ error: 'Authentication is required to create a group.' }));
  }

  const client = createClient();
  const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
  const result = await socialGroups.postApiSocialGroups({
    ownerId,
    tenantId: session?.tenantId || undefined,
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

export async function updateCommunityGroup(formData: FormData) {
  const groupId = readText(formData, 'groupId');
  const name = readText(formData, 'name');
  const description = readText(formData, 'description');
  const rawType = readText(formData, 'type') as SocialGroupsSocialGroupType;
  const rawVisibility = readText(formData, 'visibility') as SocialGroupsSocialGroupVisibility;
  const type = GROUP_TYPES.has(rawType) ? rawType : 'InterestCommunity';
  const visibility = GROUP_VISIBILITIES.has(rawVisibility) ? rawVisibility : 'Public';

  if (!groupId || !name) {
    redirect(buildGroupsHref({ error: 'Group id and name are required.' }));
  }

  const client = createClient();
  const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
  const result = await socialGroups.putApiSocialGroups(groupId, {
    name,
    slug: slugify(name),
    description: description || null,
    type,
    visibility,
  });

  if (!result.ok) {
    redirect(buildGroupHref(groupId, { error: result.error.message }));
  }

  revalidatePath('/dashboard/community');
  revalidatePath('/dashboard/community/members');
  revalidatePath(DASHBOARD_GROUPS_PATH);
  revalidatePath(`${DASHBOARD_GROUPS_PATH}/${groupId}`);
  redirect(buildGroupHref(groupId, { message: `Updated ${name}.` }));
}

export async function archiveCommunityGroup(formData: FormData) {
  const groupId = readText(formData, 'groupId');

  if (!groupId) {
    redirect(buildGroupsHref({ error: 'Group id is required.' }));
  }

  const client = createClient();
  const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
  const result = await socialGroups.postApiSocialGroupsArchive(groupId);

  if (!result.ok) {
    redirect(buildGroupHref(groupId, { error: result.error.message }));
  }

  revalidatePath('/dashboard/community');
  revalidatePath('/dashboard/community/members');
  revalidatePath(DASHBOARD_GROUPS_PATH);
  revalidatePath(`${DASHBOARD_GROUPS_PATH}/${groupId}`);
  redirect(buildGroupsHref({ message: 'Archived group.' }));
}

export async function addCommunityGroupMember(formData: FormData) {
  const groupId = readText(formData, 'groupId');
  const userId = readText(formData, 'userId');
  const rawRole = readText(formData, 'role') as SocialGroupsSocialGroupMemberRole;
  const role = GROUP_ROLES.has(rawRole) ? rawRole : 'Member';

  if (!groupId || !userId) {
    redirect(buildGroupsHref({ error: 'Group and user are required.' }));
  }

  const client = createClient();
  const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
  const result = await socialGroups.postApiSocialGroupsMembers(groupId, {
    userId,
    requestedRole: role,
  });

  if (!result.ok) {
    redirect(buildGroupHref(groupId, { error: result.error.message }));
  }

  revalidatePath('/dashboard/community');
  revalidatePath(DASHBOARD_GROUPS_PATH);
  revalidatePath(`${DASHBOARD_GROUPS_PATH}/${groupId}`);
  redirect(buildGroupHref(groupId, { message: 'Added member to group.' }));
}

export async function approveCommunityGroupMember(formData: FormData) {
  const groupId = readText(formData, 'groupId');
  const userId = readText(formData, 'userId');
  const approvedByUserId = readText(formData, 'approvedByUserId');

  if (!groupId || !userId) {
    redirect(buildGroupsHref({ error: 'Group and user are required.' }));
  }

  const client = createClient();
  const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
  const result = await socialGroups.postApiSocialGroupsMembersApprove(groupId, userId, {
    approvedByUserId: approvedByUserId || undefined,
  });

  if (!result.ok) {
    redirect(buildGroupHref(groupId, { error: result.error.message }));
  }

  revalidatePath(DASHBOARD_GROUPS_PATH);
  revalidatePath(`${DASHBOARD_GROUPS_PATH}/${groupId}`);
  redirect(buildGroupHref(groupId, { message: 'Approved group member.' }));
}

export async function rejectCommunityGroupMember(formData: FormData) {
  const groupId = readText(formData, 'groupId');
  const userId = readText(formData, 'userId');

  if (!groupId || !userId) {
    redirect(buildGroupsHref({ error: 'Group and user are required.' }));
  }

  const client = createClient();
  const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
  const result = await socialGroups.postApiSocialGroupsMembersReject(groupId, userId);

  if (!result.ok) {
    redirect(buildGroupHref(groupId, { error: result.error.message }));
  }

  revalidatePath(DASHBOARD_GROUPS_PATH);
  revalidatePath(`${DASHBOARD_GROUPS_PATH}/${groupId}`);
  redirect(buildGroupHref(groupId, { message: 'Rejected group request.' }));
}

export async function changeCommunityGroupMemberRole(formData: FormData) {
  const groupId = readText(formData, 'groupId');
  const userId = readText(formData, 'userId');
  const rawRole = readText(formData, 'role') as SocialGroupsSocialGroupMemberRole;
  const role = GROUP_ROLES.has(rawRole) ? rawRole : 'Member';

  if (!groupId || !userId) {
    redirect(buildGroupsHref({ error: 'Group and user are required.' }));
  }

  const client = createClient();
  const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
  const result = await socialGroups.putApiSocialGroupsMembersRole(groupId, userId, { role });

  if (!result.ok) {
    redirect(buildGroupHref(groupId, { error: result.error.message }));
  }

  revalidatePath(DASHBOARD_GROUPS_PATH);
  revalidatePath(`${DASHBOARD_GROUPS_PATH}/${groupId}`);
  redirect(buildGroupHref(groupId, { message: `Updated member role to ${role}.` }));
}

export async function removeCommunityGroupMember(formData: FormData) {
  const groupId = readText(formData, 'groupId');
  const userId = readText(formData, 'userId');

  if (!groupId || !userId) {
    redirect(buildGroupsHref({ error: 'Group and user are required.' }));
  }

  const client = createClient();
  const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
  const result = await socialGroups.deleteApiSocialGroupsMembers(groupId, userId);

  if (!result.ok) {
    redirect(buildGroupHref(groupId, { error: result.error.message }));
  }

  revalidatePath(DASHBOARD_GROUPS_PATH);
  revalidatePath(`${DASHBOARD_GROUPS_PATH}/${groupId}`);
  redirect(buildGroupHref(groupId, { message: 'Removed group member.' }));
}
