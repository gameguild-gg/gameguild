// =============================================================================
// COMMUNITY MEMBER QUERIES
// =============================================================================

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { getCourseShowcase } from '@/lib/courses/public-programs';
import { getPublicCourseCatalog } from '@/lib/courses/services/course.service';
import type {
  IdentityTenantsGetUserMembershipsOutput,
  IdentityTenantsUserMembership,
  IdentityUsersUser,
  IdentityUsersUserProfileDto,
  SocialFeedFeedItem,
  SocialFeedFeedItemReason,
  SocialGroupsSocialGroup,
  SocialGroupsSocialGroupMember,
  SocialGroupsSocialGroupMemberRole,
  SocialGroupsSocialGroupMembershipStatus,
  SocialGroupsSocialGroupStatus,
  SocialGroupsSocialGroupType,
  SocialGroupsSocialGroupVisibility,
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
  pendingMemberCount: number;
  createdAt: string;
  isPublic: boolean;
  type: SocialGroupsSocialGroupType;
  visibility: SocialGroupsSocialGroupVisibility;
  status: SocialGroupsSocialGroupStatus;
}

export interface MemberGroupMember {
  id: string;
  userId: string;
  displayName: string;
  email: string;
  role: SocialGroupsSocialGroupMemberRole;
  status: SocialGroupsSocialGroupMembershipStatus;
  requestedAt: string;
  joinedAt: string | null;
  approvedByUserId?: string | null;
  removedAt?: string | null;
}

export interface MembershipInviteFields {
  inviteStatus?: string | null;
  invitedByEmail?: string | null;
  inviteeEmail?: string | null;
  inviteeName?: string | null;
  invitedAt?: string | null;
  lastInviteSentAt?: string | null;
  acceptedAt?: string | null;
  cancelledAt?: string | null;
  inviteResendCount?: number | null;
}

export type MemberAccessMembership = IdentityTenantsUserMembership & MembershipInviteFields;

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

export const COMMUNITY_ACCESS_ROLES = [
  {
    value: 'Member',
    label: 'Member',
    description: 'Can access the community and learning surfaces.',
  },
  {
    value: 'Moderator',
    label: 'Moderator',
    description: 'Can moderate member activity and support queues.',
  },
  {
    value: 'TenantAdmin',
    label: 'Platform admin',
    description: 'Can manage users, content, and tenant operations.',
  },
  {
    value: 'SystemAdmin',
    label: 'Super admin',
    description: 'Full platform-management authority.',
  },
] as const;

export interface MemberAccessRow {
  member: MemberSummary;
  memberships: MemberAccessMembership[];
  primaryMembership: MemberAccessMembership | null;
  role: string;
  isSuperAdmin: boolean;
  isCurrentUser: boolean;
  membershipLoadError?: string | null;
}

export interface MemberAccessDirectory {
  members: MemberAccessRow[];
  total: number;
  currentUserId: string | null;
  error?: string | null;
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

function isDateInCurrentMonth(value?: string | null, now = new Date()) {
  if (!value) return false;

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return false;

  return date.getUTCFullYear() === now.getUTCFullYear() && date.getUTCMonth() === now.getUTCMonth();
}

function isRecentlyActive(user: IdentityUsersUser, now = new Date()) {
  if (user.isActive === false) return false;
  if (!user.lastSeenAt) return user.isActive === true;

  const lastSeenAt = new Date(user.lastSeenAt).getTime();
  if (Number.isNaN(lastSeenAt)) return user.isActive === true;

  const thirtyDaysMs = 30 * 24 * 60 * 60 * 1000;
  return now.getTime() - lastSeenAt <= thirtyDaysMs;
}

async function getAccessToken(): Promise<string | null> {
  return getToken();
}

async function getSessionClaims(): Promise<{ userId: string | null }> {
  const session = await auth().catch(() => null);

  return {
    userId: session?.user?.id ?? null,
  };
}

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken },
    tenant: { getTenantId: async () => (await auth().catch(() => null))?.tenantId ?? null },
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

function mapSessionToMember(
  sessionUser: { id?: string | null; email?: string | null; name?: string | null } | null | undefined,
): MemberSummary | null {
  if (!sessionUser?.id) return null;

  const now = new Date().toISOString();

  return mapUserToMember({
    id: sessionUser.id,
    email: sessionUser.email ?? '',
    name: sessionUser.name ?? sessionUser.email?.split('@')[0] ?? 'GameGuild member',
    isActive: true,
    createdAt: now,
    updatedAt: now,
    lastSeenAt: now,
  });
}

function mapSummaryToMemberDetail(summary: MemberSummary): MemberDetail {
  return {
    ...summary,
    skills: [],
    portfolioItems: [],
  };
}

function isSuperAdminRole(role?: string | null) {
  return role === 'SystemAdmin' || role === 'Admin';
}

function selectPrimaryMembership(memberships: MemberAccessMembership[]) {
  return (
    memberships.find((membership) => membership.isActive && isSuperAdminRole(membership.role)) ??
    memberships.find((membership) => membership.isActive && membership.role === 'TenantAdmin') ??
    memberships.find((membership) => membership.isActive) ??
    memberships[0] ??
    null
  );
}

async function getUserMemberships(
  client: ReturnType<typeof getApiClient>,
  userId: string,
): Promise<{ memberships: MemberAccessMembership[]; error?: string | null }> {
  const result = await client.request<IdentityTenantsGetUserMembershipsOutput>({
    method: 'GET',
    path: `/v1/users/${userId}/memberships`,
    params: { includeInactive: true },
    requiresAuth: true,
  });

  if (!result.ok) {
    return { memberships: [], error: result.error.message };
  }

  return { memberships: (result.data?.memberships ?? []) as MemberAccessMembership[], error: null };
}

export async function getPendingMemberInvitations(userId: string): Promise<{
  invitations: MemberAccessMembership[];
  error?: string | null;
}> {
  if (!userId) return { invitations: [], error: 'A signed-in user is required to load invitations.' };

  const result = await getUserMemberships(getApiClient(), userId);
  return {
    invitations: result.memberships.filter(
      (membership) => !membership.isActive && membership.inviteStatus === 'Pending',
    ),
    error: result.error,
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

function normalizeLeadStatus(status?: string | null): SupportTicket['status'] {
  switch (status?.trim().toLowerCase()) {
    case 'reviewed':
      return 'in-progress';
    case 'archived':
      return 'closed';
    case 'new':
    default:
      return 'open';
  }
}

function statusToLeadStatus(status?: string): string | undefined {
  switch (status) {
    case 'open':
      return 'new';
    case 'in-progress':
    case 'resolved':
      return 'reviewed';
    case 'closed':
      return 'archived';
    default:
      return undefined;
  }
}

function inferTicketPriority(lead: GeneratedApi.ContentPagesMarketingLead): SupportTicket['priority'] {
  const message = `${lead.plan ?? ''} ${lead.message ?? ''}`.toLowerCase();

  if (/\b(critical|urgent|outage|security|blocked|broken|down)\b/.test(message)) {
    return 'critical';
  }

  if (/\b(enterprise|billing|payment|production|deadline|cannot access)\b/.test(message)) {
    return 'high';
  }

  if (message.trim().length === 0) {
    return 'low';
  }

  return 'medium';
}

function leadUsername(lead: GeneratedApi.ContentPagesMarketingLead) {
  if (lead.name?.trim()) return slugify(lead.name);
  return lead.email?.split('@')[0] ?? 'unknown';
}

function mapLeadToSupportTicket(lead: GeneratedApi.ContentPagesMarketingLead): SupportTicket {
  const createdAt = lead.createdAt ?? new Date(0).toISOString();
  const subject = lead.message?.trim()
    ? lead.message.trim().split(/\r?\n/)[0]!.slice(0, 120)
    : `${lead.topic ?? 'Support'} request from ${lead.email ?? 'unknown contact'}`;

  return {
    id: lead.id ?? `${lead.email ?? 'support'}-${createdAt}`,
    subject,
    status: normalizeLeadStatus(lead.status),
    priority: inferTicketPriority(lead),
    createdBy: {
      id: lead.email ?? lead.id ?? 'unknown',
      username: leadUsername(lead),
    },
    createdAt,
    updatedAt: lead.updatedAt ?? createdAt,
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

async function getCourseFeedItems(kind: CommunityFeedKind, take = 6): Promise<CommunityFeedItem[]> {
  if (kind === 'following') {
    return [];
  }

  const catalog = await getPublicCourseCatalog();
  if (!catalog.success || catalog.data.length === 0) {
    return [];
  }

  return catalog.data.slice(0, take).map((course, index) => {
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
        items: await getCourseFeedItems(kind, options?.take ?? 6),
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
        items: await getCourseFeedItems(kind, options?.take ?? 6),
      };
    }

    const items = (result.data ?? []).filter((item) => reasonMatches(kind, item.reason)).map(mapFeedItem);

    return {
      kind,
      requiresSignIn: false,
      items: items.length > 0 ? items : await getCourseFeedItems(kind, options?.take ?? 6),
    };
  } catch {
    return {
      kind,
      requiresSignIn: false,
      items: await getCourseFeedItems(kind, options?.take ?? 6),
    };
  }
}

/**
 * Fetch community overview statistics.
 */
export async function getCommunityStats(): Promise<CommunityStats> {
  try {
    const client = getApiClient();
    const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
    const marketingLeads = new GeneratedApi.ContentMarketingleadsModule(client);
    const blogPosts = new GeneratedApi.SocialBlogPostsModule(client);
    const now = new Date();

    const [session, usersResult, groupsResult, supportResult, postsResult] = await Promise.all([
      auth().catch(() => null),
      client.request<UsersPagedResult>({
        method: 'GET',
        path: '/v1/users',
        params: { limit: 500 },
        requiresAuth: true,
      }),
      socialGroups.getApiSocialGroups({ skip: 0, take: 500 }),
      marketingLeads.getMarketingLeads({
        source: 'contact',
        topic: 'support',
        status: 'new',
        skip: 0,
        take: 500,
      }),
      blogPosts.getApiSocialBlog({ skip: 0, take: 500 }),
    ]);

    const fallbackMemberCount = session?.user?.id ? 1 : 0;
    const users = usersResult.ok ? usersResult.data.items ?? [] : [];

    return {
      totalMembers: usersResult.ok ? usersResult.data.totalCount ?? users.length : fallbackMemberCount,
      activeMembers: usersResult.ok ? users.filter((user) => isRecentlyActive(user, now)).length : fallbackMemberCount,
      newMembersThisMonth: users.filter((user) => isDateInCurrentMonth(user.createdAt, now)).length,
      totalGroups: groupsResult.ok ? (groupsResult.data ?? []).length : 0,
      openTickets: supportResult.ok ? (supportResult.data ?? []).length : 0,
      totalPosts: postsResult.ok ? (postsResult.data ?? []).length : 0,
    };
  } catch {
    const session = await auth().catch(() => null);
    const fallbackMemberCount = session?.user?.id ? 1 : 0;

    return {
      totalMembers: fallbackMemberCount,
      activeMembers: fallbackMemberCount,
      newMembersThisMonth: 0,
      totalGroups: 0,
      openTickets: 0,
      totalPosts: 0,
    };
  }
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
    const [session, result] = await Promise.all([
      auth().catch(() => null),
      client.request<UsersPagedResult>({
        method: 'GET',
        path: '/v1/users',
        params: {
          limit: options?.limit ?? 20,
          q: options?.search || undefined,
          status: options?.status || undefined,
        },
        requiresAuth: true,
      }),
    ]);

    if (result.ok) {
      const users = result.data.items ?? [];
      return {
        members: users.map(mapUserToMember),
        total: result.data.totalCount ?? users.length,
      };
    }

    const sessionMember = mapSessionToMember(session?.user);
    if (sessionMember) {
      return { members: [sessionMember], total: 1 };
    }
  } catch {
    const session = await auth().catch(() => null);
    const sessionMember = mapSessionToMember(session?.user);
    if (sessionMember) {
      return { members: [sessionMember], total: 1 };
    }
  }

  return { members: [], total: 0 };
}

export async function getMemberAccessDirectory(options?: {
  limit?: number;
  search?: string;
  status?: string;
}): Promise<MemberAccessDirectory> {
  try {
    const client = getApiClient();
    const session = await auth().catch(() => null);
    const result = await client.request<UsersPagedResult>({
      method: 'GET',
      path: '/v1/users',
      params: {
        limit: options?.limit ?? 50,
        q: options?.search || undefined,
        status: options?.status || undefined,
      },
      requiresAuth: true,
    });

    if (!result.ok) {
      const sessionMember = mapSessionToMember(session?.user);
      return {
        members: sessionMember
          ? [
              {
                member: sessionMember,
                memberships: [],
                primaryMembership: null,
                role: 'Member',
                isSuperAdmin: false,
                isCurrentUser: true,
                membershipLoadError: result.error.message,
              },
            ]
          : [],
        total: sessionMember ? 1 : 0,
        currentUserId: session?.user?.id ?? null,
        error: result.error.message,
      };
    }

    const users = result.data.items ?? [];
    const rows = await Promise.all(
      users.map(async (user) => {
        const member = mapUserToMember(user);
        const membershipResult = member.id ? await getUserMemberships(client, member.id) : { memberships: [], error: 'User id is missing.' };
        const primaryMembership = selectPrimaryMembership(membershipResult.memberships);
        const role = primaryMembership?.role ?? member.role;

        return {
          member,
          memberships: membershipResult.memberships,
          primaryMembership,
          role,
          isSuperAdmin: isSuperAdminRole(role),
          isCurrentUser: member.id === session?.user?.id,
          membershipLoadError: membershipResult.error ?? null,
        } satisfies MemberAccessRow;
      }),
    );

    return {
      members: rows,
      total: result.data.totalCount ?? users.length,
      currentUserId: session?.user?.id ?? null,
      error: null,
    };
  } catch (error) {
    const session = await auth().catch(() => null);
    const sessionMember = mapSessionToMember(session?.user);
    return {
      members: sessionMember
        ? [
            {
              member: sessionMember,
              memberships: [],
              primaryMembership: null,
              role: 'Member',
              isSuperAdmin: false,
              isCurrentUser: true,
              membershipLoadError: error instanceof Error ? error.message : 'Users could not be loaded.',
            },
          ]
        : [],
      total: sessionMember ? 1 : 0,
      currentUserId: session?.user?.id ?? null,
      error: error instanceof Error ? error.message : 'Users could not be loaded.',
    };
  }
}

/**
 * Fetch a single member by ID, including profile data.
 */
export async function getMember(userId: string): Promise<MemberDetail | null> {
  try {
    const client = getApiClient();
    const userResult = await client.request<IdentityUsersUser>({
      method: 'GET',
      path: `/v1/users/${userId}`,
      requiresAuth: true,
    });

    if (!userResult.ok) {
      const session = await auth().catch(() => null);
      const sessionMember = mapSessionToMember(session?.user);

      return sessionMember?.id === userId ? mapSummaryToMemberDetail(sessionMember) : null;
    }

    const socialProfiles = new GeneratedApi.SocialProfilesModule(client);
    const [profileResult, socialProfileResult] = await Promise.allSettled([
      client.request<IdentityUsersUserProfileDto>({
        method: 'GET',
        path: `/v1/users/${userId}/profile`,
        requiresAuth: true,
      }),
      socialProfiles.getApiSocialProfilesUsers(userId),
    ]);

    const user = userResult.data;
    const profile = profileResult.status === 'fulfilled' && profileResult.value.ok ? profileResult.value.data : null;
    const socialProfile = socialProfileResult.status === 'fulfilled' && socialProfileResult.value.ok ? socialProfileResult.value.data : null;
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

export async function getGroup(groupId: string): Promise<{ group: MemberGroup | null; error?: string | null }> {
  try {
    const client = getApiClient();
    const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
    const result = await socialGroups.getApiSocialGroups1(groupId);

    if (!result.ok) return { group: null, error: result.error.message };

    return { group: mapSocialGroup(result.data), error: null };
  } catch (error) {
    return { group: null, error: error instanceof Error ? error.message : 'Group could not be loaded.' };
  }
}

export async function getGroupMembers(
  groupId: string,
  options?: { status?: SocialGroupsSocialGroupMembershipStatus; limit?: number },
): Promise<{ members: MemberGroupMember[]; error?: string | null }> {
  try {
    const client = getApiClient();
    const socialGroups = new GeneratedApi.SocialGroupsSocialgroupsModule(client);
    const [membersResult, directory] = await Promise.all([
      socialGroups.getApiSocialGroupsMembers(groupId, {
        status: options?.status,
        skip: 0,
        take: options?.limit ?? 200,
      }),
      getMemberAccessDirectory({ limit: 500 }),
    ]);

    if (!membersResult.ok) return { members: [], error: membersResult.error.message };

    const usersById = new Map(directory.members.map((row) => [row.member.id, row.member]));
    return {
      members: (membersResult.data ?? []).map((member) => mapSocialGroupMember(member, usersById.get(member.userId ?? ''))),
      error: directory.error ?? null,
    };
  } catch (error) {
    return { members: [], error: error instanceof Error ? error.message : 'Group members could not be loaded.' };
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
  try {
    const limit = options?.limit ?? 20;
    const client = getApiClient();
    const marketingLeads = new GeneratedApi.ContentMarketingleadsModule(client);
    const result = await marketingLeads.getMarketingLeads({
      source: 'contact',
      topic: 'support',
      status: statusToLeadStatus(options?.status),
      skip: Math.max(0, ((options?.page ?? 1) - 1) * limit),
      take: limit,
    });

    if (!result.ok) return { tickets: [], total: 0 };

    const tickets = (result.data ?? [])
      .map(mapLeadToSupportTicket)
      .filter((ticket) => !options?.priority || ticket.priority === options.priority);

    return { tickets, total: tickets.length };
  } catch {
    return { tickets: [], total: 0 };
  }
}

function mapSocialGroup(group: SocialGroupsSocialGroup): MemberGroup {
  return {
    id: group.id ?? '',
    name: group.name ?? 'Untitled group',
    description: group.description ?? '',
    memberCount: group.memberCount ?? 0,
    pendingMemberCount: group.pendingMemberCount ?? 0,
    createdAt: group.createdAt ?? new Date(0).toISOString(),
    isPublic: group.visibility === 'Public',
    type: group.type ?? 'InterestCommunity',
    visibility: group.visibility ?? 'Public',
    status: group.status ?? 'Active',
  };
}

function mapSocialGroupMember(member: SocialGroupsSocialGroupMember, user?: MemberSummary): MemberGroupMember {
  const userId = member.userId ?? '';

  return {
    id: member.id ?? `${member.groupId ?? 'group'}-${userId}`,
    userId,
    displayName: user?.displayName ?? user?.username ?? userId,
    email: user?.email ?? '',
    role: member.role ?? 'Member',
    status: member.status ?? 'Pending',
    requestedAt: member.requestedAt ?? new Date(0).toISOString(),
    joinedAt: member.joinedAt ?? null,
    approvedByUserId: member.approvedByUserId ?? null,
    removedAt: member.removedAt ?? null,
  };
}
