// =============================================================================
// COMMUNITY MEMBER QUERIES
// =============================================================================

import { cookies } from 'next/headers';
import { createServerClient, decodeJWT, GeneratedApi, SessionStore, resolveCookieOptions } from '@game-guild/client';
import type {
  IdentityUsersUser,
  IdentityUsersUserProfile,
  SocialProfilesProfilePortfolioItem,
  SocialProfilesProfileSkill,
  SocialProfilesSocialProfile,
} from '@game-guild/client';

export interface MemberSummary {
  id: string;
  username: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  role: 'admin' | 'moderator' | 'member';
  status: 'active' | 'inactive' | 'banned';
  joinedAt: string;
  lastActiveAt: string;
}

export interface MemberGroup {
  id: string;
  name: string;
  description: string;
  memberCount: number;
  createdAt: string;
  isPublic: boolean;
}

export interface SupportTicket {
  id: string;
  subject: string;
  status: 'open' | 'in-progress' | 'resolved' | 'closed';
  priority: 'low' | 'medium' | 'high' | 'critical';
  createdBy: { id: string; username: string };
  assignedTo?: { id: string; username: string };
  createdAt: string;
  updatedAt: string;
}

export interface CommunityStats {
  totalMembers: number;
  activeMembers: number;
  newMembersThisMonth: number;
  totalGroups: number;
  openTickets: number;
  totalPosts: number;
}

export interface MemberDetail extends MemberSummary {
  handle?: string;
  headline?: string;
  bio?: string;
  location?: string;
  website?: string;
  bannerUrl?: string;
  timezone?: string;
  language?: string;
  phoneNumber?: string;
  updatedAt?: string;
  availabilityStatus?: SocialProfilesSocialProfile['availabilityStatus'];
  completenessScore?: number;
  followerCount?: number;
  followingCount?: number;
  postCount?: number;
  projectCount?: number;
  skills: SocialProfilesProfileSkill[];
  portfolioItems: SocialProfilesProfilePortfolioItem[];
}

// Paged result shape returned by the API
interface UsersPagedResult {
  items?: IdentityUsersUser[] | null;
  totalCount?: number;
  hasNextPage?: boolean;
}

async function getAccessToken(): Promise<string | null> {
  const secret = process.env.AUTH_SECRET || process.env.NEXTAUTH_SECRET || '';
  if (!secret) return null;

  const cookieStore = await cookies();
  const isSecure = process.env.NEXTAUTH_URL?.startsWith('https') ?? false;
  const cookieOptions = resolveCookieOptions({ name: '__gg' }, isSecure);
  const sessionStore = new SessionStore(cookieOptions);

  const encrypted = sessionStore.read((name) => cookieStore.get(name)?.value);
  if (!encrypted) return null;

  const payload = await decodeJWT({ token: encrypted, secret });
  return (payload as { accessToken?: string } | null)?.accessToken ?? null;
}

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken },
  });
}

function mapUserToMember(user: IdentityUsersUser): MemberSummary {
  return {
    id: user.id ?? '',
    username: user.name ?? user.email?.split('@')[0] ?? 'unknown',
    displayName: user.name ?? 'Unknown',
    email: user.email ?? '',
    role: 'member',
    status: user.isActive ? 'active' : 'inactive',
    joinedAt: user.createdAt ?? new Date().toISOString(),
    lastActiveAt: user.lastSeenAt ?? user.updatedAt ?? user.createdAt ?? new Date().toISOString(),
  };
}

/**
 * Fetch community overview statistics.
 */
export async function getCommunityStats(): Promise<CommunityStats> {
  try {
    const client = getApiClient();
    const result = await client.request<UsersPagedResult>({
      method: 'GET',
      path: '/v1/users',
      params: { limit: 1 },
      requiresAuth: true,
    });

    if (result.ok) {
      const totalMembers = result.data.totalCount ?? 0;
      return {
        totalMembers,
        activeMembers: totalMembers, // API doesn't split by active yet
        newMembersThisMonth: 0,
        totalGroups: 0,
        openTickets: 0,
        totalPosts: 0,
      };
    }
  } catch {
    // Fall through to defaults
  }

  return {
    totalMembers: 0,
    activeMembers: 0,
    newMembersThisMonth: 0,
    totalGroups: 0,
    openTickets: 0,
    totalPosts: 0,
  };
}

/**
 * Fetch paginated member list from the API.
 */
export async function getMembers(options?: {
  page?: number;
  limit?: number;
  search?: string;
  role?: string;
  status?: string;
}): Promise<{ members: MemberSummary[]; total: number }> {
  try {
    const client = getApiClient();
    const result = await client.request<UsersPagedResult>({
      method: 'GET',
      path: '/v1/users',
      params: {
        limit: options?.limit ?? 20,
        q: options?.search || undefined,
        status: options?.status || undefined,
      },
      requiresAuth: true,
    });

    if (result.ok) {
      const users = result.data.items ?? [];
      return {
        members: users.map(mapUserToMember),
        total: result.data.totalCount ?? users.length,
      };
    }
  } catch {
    // Fall through to empty result
  }

  return { members: [], total: 0 };
}

/**
 * Fetch a single member by ID, including profile data.
 */
export async function getMember(userId: string): Promise<MemberDetail | null> {
  try {
    const client = getApiClient();
    const socialProfiles = new GeneratedApi.SocialProfilesModule(client);
    const [userResult, profileResult, socialProfileResult] = await Promise.all([
      client.request<IdentityUsersUser>({
        method: 'GET',
        path: `/v1/users/${userId}`,
        requiresAuth: true,
      }),
      client.request<IdentityUsersUserProfile>({
        method: 'GET',
        path: `/v1/users/${userId}/profile`,
        requiresAuth: true,
      }),
      socialProfiles.getApiSocialProfilesUsers(userId),
    ]);

    if (!userResult.ok) return null;

    const user = userResult.data;
    const profile = profileResult.ok ? profileResult.data : null;
    const socialProfile = socialProfileResult.ok ? socialProfileResult.data : null;
    const summary = mapUserToMember(user);

    return {
      ...summary,
      username: socialProfile?.handle ?? summary.username,
      handle: socialProfile?.handle ?? undefined,
      displayName: socialProfile?.displayName ?? profile?.displayName ?? user.name ?? 'Unknown',
      avatarUrl: socialProfile?.avatarUrl ?? profile?.avatarUrl ?? undefined,
      headline: socialProfile?.headline ?? undefined,
      bio: socialProfile?.bio ?? profile?.bio ?? undefined,
      location: socialProfile?.location ?? profile?.location ?? undefined,
      website: socialProfile?.websiteUrl ?? profile?.website ?? undefined,
      bannerUrl: socialProfile?.bannerUrl ?? profile?.bannerUrl ?? undefined,
      timezone: socialProfile?.timeZone ?? profile?.timeZone ?? undefined,
      language: profile?.language ?? undefined,
      phoneNumber: user.phoneNumber ?? undefined,
      updatedAt: user.updatedAt ?? undefined,
      availabilityStatus: socialProfile?.availabilityStatus,
      completenessScore: socialProfile?.completenessScore,
      followerCount: socialProfile?.followerCount,
      followingCount: socialProfile?.followingCount,
      postCount: socialProfile?.postCount,
      projectCount: socialProfile?.projectCount,
      skills: socialProfile?.showSkills === false ? [] : socialProfile?.skills ?? [],
      portfolioItems: socialProfile?.showPortfolio === false ? [] : socialProfile?.portfolioItems ?? [],
    };
  } catch {
    return null;
  }
}

/**
 * Fetch paginated groups list.
 */
export async function getGroups(options?: { page?: number; limit?: number; search?: string }): Promise<{ groups: MemberGroup[]; total: number }> {
  void options;
  // TODO: Wire to groups API when available
  return { groups: [], total: 0 };
}

/**
 * Fetch support tickets.
 */
export async function getSupportTickets(options?: {
  page?: number;
  limit?: number;
  status?: string;
  priority?: string;
}): Promise<{ tickets: SupportTicket[]; total: number }> {
  void options;
  // TODO: Wire to support tickets API when available
  return { tickets: [], total: 0 };
}
