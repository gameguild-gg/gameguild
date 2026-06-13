// =============================================================================
// COMMUNITY MEMBER QUERIES
// =============================================================================

import { cookies } from 'next/headers';
import { createServerClient, decodeJWT, GeneratedApi, SessionStore, resolveCookieOptions } from '@game-guild/client';
import { getCourseShowcase, PUBLIC_COURSE_SNAPSHOT } from '@/lib/courses/public-programs';
import type {
  IdentityUsersUser,
  IdentityUsersUserProfile,
  SocialFeedFeedItem,
  SocialFeedFeedItemReason,
  SocialGroupsSocialGroup,
  SocialProfilesProfilePortfolioItem,
  SocialProfilesProfileSkillProficiency,
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

export interface PublicMemberProfile {
  userId: string;
  username: string;
  displayName: string;
  initials: string;
  joinDate: Date;
  headline?: string;
  bio?: string;
  location?: string;
  website?: string;
  avatarUrl?: string;
  bannerUrl?: string;
  featuredProject?: PublicMemberProjectSummary;
  portfolioProjects: PublicMemberProjectSummary[];
  technicalSkills: { name: string; level: number }[];
  toolsSkills: { name: string; level: number }[];
  activities: { action: string; item: string; time: string; type: string }[];
  stats: {
    followers: number;
    following: number;
    posts: number;
    projects: number;
  };
}

export interface PublicMemberProjectSummary {
  id?: string;
  slug: string;
  title: string;
  name: string;
  description?: string;
  tech: string;
  rating: number;
  url?: string;
  imageUrl?: string;
  isPinned: boolean;
}

export type CommunityFeedKind = 'following' | 'discover' | 'trending';

export interface CommunityFeedItem {
  id: string;
  title: string;
  contentType: string;
  contentId: string;
  authorId: string;
  reason: string;
  relevanceScore: number;
  isRead: boolean;
  createdAt: string;
  summary?: string;
  href?: string;
  imageUrl?: string;
  actionLabel?: string;
}

export interface CommunityFeedResult {
  kind: CommunityFeedKind;
  requiresSignIn: boolean;
  items: CommunityFeedItem[];
}

// Paged result shape returned by the API
interface UsersPagedResult {
  items?: IdentityUsersUser[] | null;
  totalCount?: number;
  hasNextPage?: boolean;
}

async function getSessionClaims(): Promise<{ accessToken: string | null; userId: string | null }> {
  const secret = process.env.AUTH_SECRET || process.env.NEXTAUTH_SECRET || '';
  if (!secret) return { accessToken: null, userId: null };

  const cookieStore = await cookies();
  const isSecure = process.env.NEXTAUTH_URL?.startsWith('https') ?? false;
  const cookieOptions = resolveCookieOptions({ name: '__gg' }, isSecure);
  const sessionStore = new SessionStore(cookieOptions);

  const encrypted = sessionStore.read((name) => cookieStore.get(name)?.value);
  if (!encrypted) return { accessToken: null, userId: null };

  const payload = await decodeJWT({ token: encrypted, secret });
  const claims = payload as { accessToken?: string; sub?: string; userId?: string; nameid?: string } | null;

  return {
    accessToken: claims?.accessToken ?? null,
    userId: claims?.sub ?? claims?.userId ?? claims?.nameid ?? null,
  };
}

async function getAccessToken(): Promise<string | null> {
  return (await getSessionClaims()).accessToken;
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

function getInitials(displayName: string) {
  return (
    displayName
      .split(/[\s_-]+/)
      .map((part) => part[0])
      .filter(Boolean)
      .join('')
      .slice(0, 2)
      .toUpperCase() || 'U'
  );
}

function slugify(value: string) {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function proficiencyToLevel(proficiency?: SocialProfilesProfileSkillProficiency) {
  switch (proficiency) {
    case 'Expert':
      return 100;
    case 'Advanced':
      return 80;
    case 'Intermediate':
      return 60;
    case 'Beginner':
      return 35;
    default:
      return 50;
  }
}

function portfolioTech(item: SocialProfilesProfilePortfolioItem) {
  if (item.projectId) return 'Project';
  if (!item.url) return 'Portfolio';

  try {
    return new URL(item.url).hostname.replace(/^www\./, '');
  } catch {
    return 'Portfolio';
  }
}

function mapPortfolioItem(item: SocialProfilesProfilePortfolioItem): PublicMemberProjectSummary {
  const title = item.title?.trim() || 'Untitled project';

  return {
    id: item.id,
    slug: slugify(title || item.id || 'project'),
    title,
    name: title,
    description: item.description ?? undefined,
    tech: portfolioTech(item),
    rating: 0,
    url: item.url ?? undefined,
    imageUrl: item.imageUrl ?? undefined,
    isPinned: item.isPinned ?? false,
  };
}

function mapSocialProfileToPublicMember(profile: SocialProfilesSocialProfile): PublicMemberProfile {
  const displayName = profile.displayName?.trim() || profile.handle?.trim() || 'Community member';
  const portfolio = [...(profile.portfolioItems ?? [])]
    .sort((left, right) => {
      if ((left.isPinned ?? false) !== (right.isPinned ?? false)) return left.isPinned ? -1 : 1;
      return (left.displayOrder ?? 0) - (right.displayOrder ?? 0);
    })
    .map(mapPortfolioItem);
  const featuredProject = portfolio.find((project) => project.isPinned) ?? portfolio[0];

  return {
    userId: profile.userId ?? '',
    username: profile.handle ?? profile.userId ?? 'member',
    displayName,
    initials: getInitials(displayName),
    joinDate: new Date(profile.verifiedAt ?? '2026-01-01T00:00:00.000Z'),
    headline: profile.headline ?? undefined,
    bio: profile.bio ?? undefined,
    location: profile.location ?? undefined,
    website: profile.websiteUrl ?? undefined,
    avatarUrl: profile.avatarUrl ?? undefined,
    bannerUrl: profile.bannerUrl ?? undefined,
    featuredProject,
    portfolioProjects: featuredProject ? portfolio.filter((project) => project.id !== featuredProject.id || project.slug !== featuredProject.slug) : portfolio,
    technicalSkills: (profile.showSkills === false ? [] : (profile.skills ?? [])).map((skill) => ({
      name: skill.name ?? 'Skill',
      level: proficiencyToLevel(skill.proficiency),
    })),
    toolsSkills: [],
    activities:
      profile.showActivity === false
        ? []
        : [
            {
              action: 'updated',
              item: 'their community profile',
              time: 'Recently',
              type: 'profile',
            },
          ],
    stats: {
      followers: profile.followerCount ?? 0,
      following: profile.followingCount ?? 0,
      posts: profile.postCount ?? 0,
      projects: profile.projectCount ?? portfolio.length,
    },
  };
}

function reasonMatches(kind: CommunityFeedKind, reason?: SocialFeedFeedItemReason) {
  if (!reason) return true;

  switch (kind) {
    case 'following':
      return reason === 'Following';
    case 'trending':
      return reason === 'Trending' || reason === 'Liked';
    case 'discover':
      return reason === 'Recommended' || reason === 'InNetwork' || reason === 'Mentioned' || reason === 'Replied';
    default:
      return true;
  }
}

function titleFromContentType(contentType?: string) {
  return (contentType || 'Community update')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .toLowerCase()
    .replace(/^\w/, (letter) => letter.toUpperCase());
}

function mapFeedItem(item: SocialFeedFeedItem): CommunityFeedItem {
  return {
    id: item.id ?? '',
    title: titleFromContentType(item.contentType),
    contentType: item.contentType ?? 'Community',
    contentId: item.contentId ?? '',
    authorId: item.authorId ?? '',
    reason: item.reason ?? 'Recommended',
    relevanceScore: item.relevanceScore ?? 0,
    isRead: item.isRead ?? false,
    createdAt: item.contentCreatedAt ?? item.createdAt ?? new Date(0).toISOString(),
  };
}

function getCourseFeedReason(kind: CommunityFeedKind) {
  switch (kind) {
    case 'trending':
      return 'Trending course';
    case 'discover':
      return 'Recommended course';
    case 'following':
    default:
      return 'Course';
  }
}

function getCourseFeedItems(kind: CommunityFeedKind, take = 6): CommunityFeedItem[] {
  if (kind === 'following') {
    return [];
  }

  return PUBLIC_COURSE_SNAPSHOT.slice(0, take).map((course, index) => {
    const slug = String(course.slug ?? course.id ?? `course-${index + 1}`);
    const showcase = getCourseShowcase(slug);
    const thumbnail = typeof course.thumbnail === 'string' ? course.thumbnail : undefined;

    return {
      id: `course-${slug}`,
      title: course.title ?? 'GameGuild course',
      contentType: 'Course',
      contentId: slug,
      authorId: 'gameguild-learning',
      reason: getCourseFeedReason(kind),
      relevanceScore: Math.max(0, 10 - index),
      isRead: false,
      createdAt: '2026-01-01T00:00:00.000Z',
      summary: showcase?.headline ?? course.description ?? 'Explore a GameGuild course landing page.',
      href: `/courses/${slug}`,
      imageUrl: thumbnail,
      actionLabel: 'View course',
    };
  });
}

export async function getPublicMemberProfile(member: string): Promise<PublicMemberProfile | null> {
  try {
    const client = getApiClient();
    const socialProfiles = new GeneratedApi.SocialProfilesModule(client);
    const result = await socialProfiles.getApiSocialProfiles(member.replace(/^@/, ''));

    if (!result.ok || !result.data) return null;

    return mapSocialProfileToPublicMember(result.data);
  } catch {
    return null;
  }
}

export async function getMemberProject(
  member: string,
  project: string,
): Promise<{
  member: PublicMemberProfile;
  project: PublicMemberProjectSummary;
} | null> {
  const profile = await getPublicMemberProfile(member);
  if (!profile) return null;

  const normalizedProject = slugify(project);
  const projects = [profile.featuredProject, ...profile.portfolioProjects].filter((item): item is PublicMemberProjectSummary => Boolean(item));
  const selected = projects.find((item) => item.id === project || item.slug === normalizedProject);

  return selected ? { member: profile, project: selected } : null;
}

export async function getCommunityFeed(kind: CommunityFeedKind, options?: { take?: number; includeRead?: boolean }): Promise<CommunityFeedResult> {
  const session = await getSessionClaims();
  if (!session.userId) {
    if (kind !== 'following') {
      return {
        kind,
        requiresSignIn: false,
        items: getCourseFeedItems(kind, options?.take ?? 6),
      };
    }

    return { kind, requiresSignIn: true, items: [] };
  }

  try {
    const client = getApiClient();
    const socialFeed = new GeneratedApi.SocialFeedModule(client);
    const result = await socialFeed.getApiSocialFeedUsers(session.userId, {
      skip: 0,
      take: options?.take ?? 20,
      includeRead: options?.includeRead ?? true,
    });

    if (!result.ok) {
      return {
        kind,
        requiresSignIn: false,
        items: getCourseFeedItems(kind, options?.take ?? 6),
      };
    }

    const items = (result.data ?? []).filter((item) => reasonMatches(kind, item.reason)).map(mapFeedItem);

    return {
      kind,
      requiresSignIn: false,
      items: items.length > 0 ? items : getCourseFeedItems(kind, options?.take ?? 6),
    };
  } catch {
    return {
      kind,
      requiresSignIn: false,
      items: getCourseFeedItems(kind, options?.take ?? 6),
    };
  }
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
      skills: socialProfile?.showSkills === false ? [] : (socialProfile?.skills ?? []),
      portfolioItems: socialProfile?.showPortfolio === false ? [] : (socialProfile?.portfolioItems ?? []),
    };
  } catch {
    return null;
  }
}

/**
 * Fetch paginated groups list.
 */
export async function getGroups(options?: { page?: number; limit?: number; search?: string }): Promise<{ groups: MemberGroup[]; total: number }> {
  try {
    const limit = options?.limit ?? 20;
    const client = getApiClient();
    const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
    const result = await socialGroups.getApiSocialGroups({
      search: options?.search || undefined,
      skip: Math.max(0, ((options?.page ?? 1) - 1) * limit),
      take: limit,
    });

    if (!result.ok) return { groups: [], total: 0 };

    const groups = (result.data ?? []).map(mapSocialGroup);
    return { groups, total: groups.length };
  } catch {
    return { groups: [], total: 0 };
  }
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
  return { tickets: [], total: 0 };
}

function mapSocialGroup(group: SocialGroupsSocialGroup): MemberGroup {
  return {
    id: group.id ?? '',
    name: group.name ?? 'Untitled group',
    description: group.description ?? '',
    memberCount: group.memberCount ?? 0,
    createdAt: group.createdAt ?? new Date(0).toISOString(),
    isPublic: group.visibility === 'Public',
  };
}
