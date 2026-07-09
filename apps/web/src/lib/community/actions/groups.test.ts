import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  postApiSocialGroups: vi.fn(),
  SocialGroupsSocialgroupsModule: vi.fn(),
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    SocialGroupsSocialgroupsModule: mocks.SocialGroupsSocialgroupsModule,
  },
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('next/navigation', () => ({
  redirect: mocks.redirect,
}));

import { createCommunityGroup } from './groups';

describe('createCommunityGroup', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({ request: vi.fn() });
    mocks.SocialGroupsSocialgroupsModule.mockReturnValue({
      postApiSocialGroups: mocks.postApiSocialGroups,
    });
    mocks.postApiSocialGroups.mockResolvedValue({ ok: true, data: { id: 'group-1' } });
  });

  it('creates a community group and revalidates community routes', async () => {
    const formData = new FormData();
    formData.set('name', 'Pixel Art Mentors');
    formData.set('description', 'Mentor-led critiques');
    formData.set('type', 'StudyGroup');
    formData.set('visibility', 'InviteOnly');

    await expect(createCommunityGroup(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups?message=Created+Pixel+Art+Mentors.',
    );

    expect(mocks.postApiSocialGroups).toHaveBeenCalledWith({
      name: 'Pixel Art Mentors',
      slug: 'pixel-art-mentors',
      description: 'Mentor-led critiques',
      type: 'StudyGroup',
      visibility: 'InviteOnly',
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/community');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/community/members');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/community/members/groups');
  });

  it('redirects with validation errors before calling the API', async () => {
    const formData = new FormData();

    await expect(createCommunityGroup(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups?error=Group+name+is+required.',
    );

    expect(mocks.postApiSocialGroups).not.toHaveBeenCalled();
  });

  it('defaults unknown type and visibility values to safe public interest groups', async () => {
    const formData = new FormData();
    formData.set('name', 'General');
    formData.set('type', 'BadType');
    formData.set('visibility', 'BadVisibility');

    await expect(createCommunityGroup(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups?message=Created+General.',
    );

    expect(mocks.postApiSocialGroups).toHaveBeenCalledWith({
      name: 'General',
      slug: 'general',
      description: null,
      type: 'InterestCommunity',
      visibility: 'Public',
    });
  });

  it('redirects with the API error when group creation fails', async () => {
    mocks.postApiSocialGroups.mockResolvedValue({ ok: false, error: { message: 'Slug already exists.' } });
    const formData = new FormData();
    formData.set('name', 'Pixel Art Mentors');

    await expect(createCommunityGroup(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups?error=Slug+already+exists.',
    );

    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });
});
