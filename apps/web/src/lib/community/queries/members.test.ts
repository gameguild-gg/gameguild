import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  getProfileByHandle: vi.fn(),
  getProfileByUserId: vi.fn(),
  getUserFeed: vi.fn(),
  getSocialGroups: vi.fn(),
  getPublicCourseCatalog: vi.fn(),
  getMarketingLeads: vi.fn(),
  getBlogPosts: vi.fn(),
  clientRequest: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    SocialProfilesModule: class {
      getApiSocialProfiles = mocks.getProfileByHandle;
      getApiSocialProfilesUsers = mocks.getProfileByUserId;
    },
    SocialFeedModule: class {
      getApiSocialFeedUsers = mocks.getUserFeed;
    },
    SocialGroupsSocialgroupsModule: class {
      getApiSocialGroups = mocks.getSocialGroups;
    },
    ContentMarketingleadsModule: class {
      getMarketingLeads = mocks.getMarketingLeads;
    },
    SocialBlogPostsModule: class {
      getApiSocialBlog = mocks.getBlogPosts;
    },
  },
}));

vi.mock('@/lib/courses/services/course.service', () => ({
  getPublicCourseCatalog: mocks.getPublicCourseCatalog,
}));

const { getCommunityFeed, getCommunityStats, getGroups, getMember, getMemberAccessDirectory, getMemberProject, getMembers, getPublicMemberProfile, getSupportTickets } = await import('./members');

describe('community member queries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useRealTimers();
    process.env.AUTH_SECRET = 'test-secret';
    mocks.auth.mockResolvedValue({
      user: {
        id: '00000000-0000-0000-0000-000000000111',
        email: 'member@example.com',
        name: 'Signed In Member',
      },
    });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({ request: mocks.clientRequest });
    mocks.getPublicCourseCatalog.mockResolvedValue({
      success: true,
      source: 'api',
      data: [
        {
          id: 'course-1',
          title: 'Live AI Gameplay Course',
          slug: 'live-ai-gameplay-course',
          description: 'Live API-backed course.',
          thumbnail: 'https://example.com/course.jpg',
        },
      ],
    });
    mocks.getMarketingLeads.mockResolvedValue({ ok: true, data: [] });
    mocks.getBlogPosts.mockResolvedValue({ ok: true, data: [] });
    mocks.clientRequest.mockResolvedValue({ ok: true, data: { items: [], totalCount: 0 } });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('maps a public social profile into the member page view model', async () => {
    mocks.getProfileByHandle.mockResolvedValue({
      ok: true,
      data: {
        id: 'profile-1',
        userId: '00000000-0000-0000-0000-000000000123',
        handle: 'ada-dev',
        displayName: 'Ada Developer',
        headline: 'Gameplay engineer',
        bio: 'Builds accessible game tools.',
        location: 'Remote',
        websiteUrl: 'https://example.com',
        avatarUrl: 'https://cdn.example.com/avatar.png',
        bannerUrl: 'https://cdn.example.com/banner.png',
        followerCount: 12,
        followingCount: 5,
        postCount: 7,
        projectCount: 2,
        skills: [
          { name: 'Godot', proficiency: 'Expert' },
          { name: 'TypeScript', proficiency: 'Advanced' },
        ],
        portfolioItems: [
          {
            id: 'project-pinned',
            title: 'Moon Runner',
            description: 'Fast arcade prototype.',
            url: 'https://example.com/moon',
            isPinned: true,
          },
          {
            id: 'project-tools',
            title: 'Level Tools',
            description: 'Editor helpers.',
            url: 'https://example.com/tools',
            isPinned: false,
          },
        ],
      },
    });

    const profile = await getPublicMemberProfile('ada-dev');

    expect(mocks.getProfileByHandle).toHaveBeenCalledWith('ada-dev');
    expect(profile?.displayName).toBe('Ada Developer');
    expect(profile?.headline).toBe('Gameplay engineer');
    expect(profile?.featuredProject?.name).toBe('Moon Runner');
    expect(profile?.portfolioProjects).toHaveLength(1);
    expect(profile?.technicalSkills).toEqual([
      { name: 'Godot', level: 100 },
      { name: 'TypeScript', level: 80 },
    ]);
    expect(profile?.stats.followers).toBe(12);
  });

  it('resolves a member project by portfolio item slug or id', async () => {
    mocks.getProfileByHandle.mockResolvedValue({
      ok: true,
      data: {
        id: 'profile-1',
        userId: '00000000-0000-0000-0000-000000000123',
        handle: 'ada-dev',
        displayName: 'Ada Developer',
        portfolioItems: [{ id: 'project-1', title: 'Moon Runner', description: 'Arcade prototype.', url: 'https://example.com/moon' }],
        skills: [],
      },
    });

    const result = await getMemberProject('ada-dev', 'moon-runner');

    expect(result?.member.displayName).toBe('Ada Developer');
    expect(result?.project.title).toBe('Moon Runner');
    expect(result?.project.url).toBe('https://example.com/moon');
  });

  it('uses the authenticated social feed API for live feed sections', async () => {
    mocks.getUserFeed.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'feed-1',
          userId: '00000000-0000-0000-0000-000000000111',
          contentId: '00000000-0000-0000-0000-000000000222',
          contentType: 'ProjectUpdate',
          authorId: '00000000-0000-0000-0000-000000000333',
          relevanceScore: 8.5,
          reason: 'Recommended',
          isRead: false,
          isHidden: false,
          contentCreatedAt: '2026-06-01T10:00:00Z',
          createdAt: '2026-06-01T10:05:00Z',
        },
      ],
    });

    const feed = await getCommunityFeed('discover');

    expect(mocks.getUserFeed).toHaveBeenCalledWith('00000000-0000-0000-0000-000000000111', {
      skip: 0,
      take: 20,
      includeRead: true,
    });
    expect(feed.requiresSignIn).toBe(false);
    expect(feed.items).toEqual([
      expect.objectContaining({
        id: 'feed-1',
        title: 'Project update',
        reason: 'Recommended',
      }),
    ]);
  });

  it('feeds live public courses into discovery when the viewer is signed out', async () => {
    mocks.auth.mockResolvedValue(null);

    const feed = await getCommunityFeed('discover');

    expect(feed.requiresSignIn).toBe(false);
    expect(mocks.getPublicCourseCatalog).toHaveBeenCalled();
    expect(feed.items).toEqual([
      expect.objectContaining({
        title: 'Live AI Gameplay Course',
        contentType: 'Course',
        href: '/courses/live-ai-gameplay-course',
        actionLabel: 'View course',
      }),
    ]);
  });

  it('loads community groups from the generated social groups API', async () => {
    mocks.getSocialGroups.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'group-1',
          name: 'Arcade Builders',
          description: 'Teams shipping arcade projects.',
          visibility: 'Public',
          memberCount: 14,
          createdAt: '2026-06-01T00:00:00Z',
        },
      ],
    });

    const result = await getGroups({ limit: 10, search: 'arcade' });

    expect(mocks.getSocialGroups).toHaveBeenCalledWith({
      search: 'arcade',
      skip: 0,
      take: 10,
    });
    expect(result).toEqual({
      total: 1,
      groups: [
        {
          id: 'group-1',
          name: 'Arcade Builders',
          description: 'Teams shipping arcade projects.',
          memberCount: 14,
          pendingMemberCount: 0,
          createdAt: '2026-06-01T00:00:00Z',
          isPublic: true,
          type: 'InterestCommunity',
          visibility: 'Public',
          status: 'Active',
        },
      ],
    });
  });

  it('builds community stats from live users, groups, support leads, and blog posts', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-06-14T12:00:00.000Z'));
    mocks.clientRequest.mockResolvedValue({
      ok: true,
      data: {
        totalCount: 4,
        items: [
          {
            id: 'user-active',
            email: 'active@example.com',
            isActive: true,
            createdAt: '2026-06-01T00:00:00.000Z',
            lastSeenAt: '2026-06-13T12:00:00.000Z',
          },
          {
            id: 'user-old',
            email: 'old@example.com',
            isActive: true,
            createdAt: '2026-05-01T00:00:00.000Z',
            lastSeenAt: '2026-04-01T00:00:00.000Z',
          },
          {
            id: 'user-disabled',
            email: 'disabled@example.com',
            isActive: false,
            createdAt: '2026-06-02T00:00:00.000Z',
            lastSeenAt: '2026-06-13T12:00:00.000Z',
          },
        ],
      },
    });
    mocks.getSocialGroups.mockResolvedValue({
      ok: true,
      data: [{ id: 'group-1' }, { id: 'group-2' }],
    });
    mocks.getMarketingLeads.mockResolvedValue({
      ok: true,
      data: [{ id: 'ticket-1' }, { id: 'ticket-2' }, { id: 'ticket-3' }],
    });
    mocks.getBlogPosts.mockResolvedValue({
      ok: true,
      data: [{ id: 'post-1' }],
    });

    const stats = await getCommunityStats();

    expect(mocks.clientRequest).toHaveBeenCalledWith({
      method: 'GET',
      path: '/v1/users',
      params: { limit: 500 },
      requiresAuth: true,
    });
    expect(mocks.getSocialGroups).toHaveBeenCalledWith({ skip: 0, take: 500 });
    expect(mocks.getMarketingLeads).toHaveBeenCalledWith({
      source: 'contact',
      topic: 'support',
      status: 'new',
      skip: 0,
      take: 500,
    });
    expect(mocks.getBlogPosts).toHaveBeenCalledWith({ skip: 0, take: 500 });
    expect(stats).toEqual({
      totalMembers: 4,
      activeMembers: 1,
      newMembersThisMonth: 2,
      totalGroups: 2,
      openTickets: 3,
      totalPosts: 1,
    });
  });

  it('keeps the signed-in member in stats when the user directory cannot be listed', async () => {
    mocks.clientRequest.mockResolvedValue({
      ok: false,
      error: { message: 'Forbidden' },
    });
    mocks.getSocialGroups.mockResolvedValue({ ok: true, data: [] });
    mocks.getMarketingLeads.mockResolvedValue({ ok: true, data: [] });
    mocks.getBlogPosts.mockResolvedValue({ ok: true, data: [] });

    const stats = await getCommunityStats();

    expect(stats.totalMembers).toBe(1);
    expect(stats.activeMembers).toBe(1);
  });

  it('keeps the signed-in member in stats when another community endpoint throws', async () => {
    mocks.clientRequest.mockResolvedValue({
      ok: false,
      error: { message: 'Forbidden' },
    });
    mocks.getSocialGroups.mockRejectedValue(new Error('Groups unavailable'));

    const stats = await getCommunityStats();

    expect(stats.totalMembers).toBe(1);
    expect(stats.activeMembers).toBe(1);
    expect(stats.totalGroups).toBe(0);
  });

  it('falls back to the signed-in member when getMembers cannot list the directory', async () => {
    mocks.clientRequest.mockResolvedValue({
      ok: false,
      error: { message: 'Forbidden' },
    });

    const result = await getMembers({ limit: 50 });

    expect(result.total).toBe(1);
    expect(result.members[0]).toMatchObject({
      id: '00000000-0000-0000-0000-000000000111',
      email: 'member@example.com',
      displayName: 'Signed In Member',
      status: 'active',
    });
  });

  it('falls back to the signed-in member when getMembers throws', async () => {
    mocks.clientRequest.mockRejectedValue(new Error('Users unavailable'));

    const result = await getMembers({ limit: 50 });

    expect(result.total).toBe(1);
    expect(result.members[0]?.id).toBe('00000000-0000-0000-0000-000000000111');
  });

  it('returns an identity member when optional profile enrichment throws', async () => {
    mocks.clientRequest.mockImplementation(async ({ path }: { path: string }) => {
      if (path === '/v1/users/00000000-0000-0000-0000-000000000123') {
        return {
          ok: true,
          data: {
            id: '00000000-0000-0000-0000-000000000123',
            email: 'ada@example.com',
            name: 'Ada Developer',
            isActive: true,
            createdAt: '2026-06-01T00:00:00.000Z',
          },
        };
      }

      if (path === '/v1/users/00000000-0000-0000-0000-000000000123/profile') {
        throw new Error('Identity profile is not provisioned');
      }

      throw new Error(`Unexpected path ${path}`);
    });
    mocks.getProfileByUserId.mockRejectedValue(new Error('Social profile is not provisioned'));

    const member = await getMember('00000000-0000-0000-0000-000000000123');

    expect(member).toMatchObject({
      id: '00000000-0000-0000-0000-000000000123',
      displayName: 'Ada Developer',
      email: 'ada@example.com',
      skills: [],
      portfolioItems: [],
    });
  });

  it('returns the signed-in member when the identity detail endpoint rejects self access', async () => {
    mocks.clientRequest.mockResolvedValue({
      ok: false,
      error: { message: 'Forbidden', status: 403 },
    });
    mocks.getProfileByUserId.mockResolvedValue({
      ok: false,
      error: { message: 'Profile not found', status: 404 },
    });

    const member = await getMember('00000000-0000-0000-0000-000000000111');

    expect(member).toMatchObject({
      id: '00000000-0000-0000-0000-000000000111',
      displayName: 'Signed In Member',
      email: 'member@example.com',
      skills: [],
      portfolioItems: [],
    });
  });

  it('builds role-management rows with memberships and super-admin state', async () => {
    mocks.clientRequest.mockImplementation(async ({ path }: { path: string }) => {
      if (path === '/v1/users') {
        return {
          ok: true,
          data: {
            totalCount: 1,
            items: [
              {
                id: '00000000-0000-0000-0000-000000000111',
                email: 'member@example.com',
                name: 'Signed In Member',
                isActive: true,
                createdAt: '2026-06-01T00:00:00.000Z',
              },
            ],
          },
        };
      }

      if (path === '/v1/users/00000000-0000-0000-0000-000000000111/memberships') {
        return {
          ok: true,
          data: {
            memberships: [
              {
                tenantId: '10000000-0000-0000-0000-000000000000',
                tenantName: 'GameGuild Platform',
                role: 'SystemAdmin',
                isActive: true,
              },
            ],
          },
        };
      }

      throw new Error(`Unexpected path ${path}`);
    });

    const directory = await getMemberAccessDirectory({ limit: 50 });

    expect(directory.total).toBe(1);
    expect(directory.members[0]).toMatchObject({
      role: 'SystemAdmin',
      isSuperAdmin: true,
      isCurrentUser: true,
      primaryMembership: {
        tenantName: 'GameGuild Platform',
      },
    });
  });

  it('wires the tenant provider into the community API client', async () => {
    mocks.clientRequest.mockResolvedValue({ ok: true, data: { items: [], totalCount: 0 } });
    mocks.getSocialGroups.mockResolvedValue({ ok: true, data: [] });

    await getMemberAccessDirectory({ limit: 1 });

    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: expect.any(String),
      auth: { getAccessToken: expect.any(Function) },
      tenant: { getTenantId: expect.any(Function) },
    });
  });

  it('maps support contact leads into community support tickets', async () => {
    mocks.getMarketingLeads.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'lead-1',
          source: 'contact',
          status: 'new',
          name: 'Riley Producer',
          email: 'riley@example.com',
          topic: 'support',
          plan: 'enterprise',
          message: 'Production login is blocked\nWe need help before class.',
          createdAt: '2026-06-01T10:00:00.000Z',
          updatedAt: '2026-06-01T11:00:00.000Z',
        },
      ],
    });

    const result = await getSupportTickets({ limit: 10, status: 'open', priority: 'critical' });

    expect(mocks.getMarketingLeads).toHaveBeenCalledWith({
      source: 'contact',
      topic: 'support',
      status: 'new',
      skip: 0,
      take: 10,
    });
    expect(result).toEqual({
      total: 1,
      tickets: [
        {
          id: 'lead-1',
          subject: 'Production login is blocked',
          status: 'open',
          priority: 'critical',
          createdBy: {
            id: 'riley@example.com',
            username: 'riley-producer',
          },
          createdAt: '2026-06-01T10:00:00.000Z',
          updatedAt: '2026-06-01T11:00:00.000Z',
        },
      ],
    });
  });
});
