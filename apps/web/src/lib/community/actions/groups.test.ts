import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  postApiSocialGroups: vi.fn(),
  putApiSocialGroups: vi.fn(),
  getApiSocialGroupsMembers: vi.fn(),
  postApiSocialGroupsMembers: vi.fn(),
  postApiSocialGroupsMembersApprove: vi.fn(),
  postApiSocialGroupsMembersReject: vi.fn(),
  putApiSocialGroupsMembersRole: vi.fn(),
  deleteApiSocialGroupsMembers: vi.fn(),
  postApiSocialGroupsArchive: vi.fn(),
  SocialGroupsSocialGroupsModule: vi.fn(),
  redirect: vi.fn((href: string) => {
    throw new Error(`redirect:${href}`);
  }),
  revalidatePath: vi.fn(),
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    SocialGroupsSocialGroupsModule: mocks.SocialGroupsSocialGroupsModule,
  },
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('next/navigation', () => ({
  redirect: mocks.redirect,
}));

import {
  addCommunityGroupMember,
  approveCommunityGroupMember,
  archiveCommunityGroup,
  changeCommunityGroupMemberRole,
  createCommunityGroup,
  rejectCommunityGroupMember,
  removeCommunityGroupMember,
  updateCommunityGroup,
} from './groups';

describe('createCommunityGroup', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({
      user: { id: 'admin-1', email: 'admin@game-guild.com' },
      tenantId: 'tenant-1',
    });
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({ request: vi.fn() });
    mocks.SocialGroupsSocialGroupsModule.mockImplementation(function SocialGroupsSocialGroupsModule() {
      return {
      postApiSocialGroups: mocks.postApiSocialGroups,
      putApiSocialGroups: mocks.putApiSocialGroups,
      getApiSocialGroupsMembers: mocks.getApiSocialGroupsMembers,
      postApiSocialGroupsMembers: mocks.postApiSocialGroupsMembers,
      postApiSocialGroupsMembersApprove: mocks.postApiSocialGroupsMembersApprove,
      postApiSocialGroupsMembersReject: mocks.postApiSocialGroupsMembersReject,
      putApiSocialGroupsMembersRole: mocks.putApiSocialGroupsMembersRole,
      deleteApiSocialGroupsMembers: mocks.deleteApiSocialGroupsMembers,
      postApiSocialGroupsArchive: mocks.postApiSocialGroupsArchive,
      };
    });
    mocks.postApiSocialGroups.mockResolvedValue({ ok: true, data: { id: 'group-1' } });
    mocks.putApiSocialGroups.mockResolvedValue({ ok: true, data: { id: 'group-1' } });
    mocks.postApiSocialGroupsMembers.mockResolvedValue({ ok: true, data: { id: 'membership-1' } });
    mocks.postApiSocialGroupsMembersApprove.mockResolvedValue({ ok: true, data: undefined });
    mocks.postApiSocialGroupsMembersReject.mockResolvedValue({ ok: true, data: undefined });
    mocks.putApiSocialGroupsMembersRole.mockResolvedValue({ ok: true, data: undefined });
    mocks.deleteApiSocialGroupsMembers.mockResolvedValue({ ok: true, data: undefined });
    mocks.postApiSocialGroupsArchive.mockResolvedValue({ ok: true, data: undefined });
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
      ownerId: 'admin-1',
      tenantId: 'tenant-1',
      name: 'Pixel Art Mentors',
      slug: 'pixel-art-mentors',
      description: 'Mentor-led critiques',
      type: 'StudyGroup',
      visibility: 'InviteOnly',
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/console/community');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/console/community/members');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/console/community/members/groups');
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
      ownerId: 'admin-1',
      tenantId: 'tenant-1',
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

  it('requires an authenticated owner before creating a group', async () => {
    mocks.auth.mockResolvedValue(null);
    const formData = new FormData();
    formData.set('name', 'Anonymous Group');

    await expect(createCommunityGroup(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups?error=Authentication+is+required+to+create+a+group.',
    );

    expect(mocks.postApiSocialGroups).not.toHaveBeenCalled();
  });

  it('updates group settings from the detail page', async () => {
    const formData = new FormData();
    formData.set('groupId', 'group-1');
    formData.set('name', 'Mentor Guild');
    formData.set('description', 'Updated operating notes');
    formData.set('type', 'ProjectTeam');
    formData.set('visibility', 'Private');

    await expect(updateCommunityGroup(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups/group-1?message=Updated+Mentor+Guild.',
    );

    expect(mocks.putApiSocialGroups).toHaveBeenCalledWith('group-1', {
      name: 'Mentor Guild',
      slug: 'mentor-guild',
      description: 'Updated operating notes',
      type: 'ProjectTeam',
      visibility: 'Private',
    });
  });

  it('adds, approves, rejects, changes role, removes, and archives group members through the real social group API', async () => {
    const addForm = new FormData();
    addForm.set('groupId', 'group-1');
    addForm.set('userId', 'user-1');
    addForm.set('role', 'Moderator');

    await expect(addCommunityGroupMember(addForm)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups/group-1?message=Added+member+to+group.',
    );
    expect(mocks.postApiSocialGroupsMembers).toHaveBeenCalledWith('group-1', {
      userId: 'user-1',
      requestedRole: 'Moderator',
    });

    const approveForm = new FormData();
    approveForm.set('groupId', 'group-1');
    approveForm.set('userId', 'user-1');
    approveForm.set('approvedByUserId', 'admin-1');
    await expect(approveCommunityGroupMember(approveForm)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups/group-1?message=Approved+group+member.',
    );
    expect(mocks.postApiSocialGroupsMembersApprove).toHaveBeenCalledWith('group-1', 'user-1', {
      approvedByUserId: 'admin-1',
    });

    const rejectForm = new FormData();
    rejectForm.set('groupId', 'group-1');
    rejectForm.set('userId', 'user-2');
    await expect(rejectCommunityGroupMember(rejectForm)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups/group-1?message=Rejected+group+request.',
    );
    expect(mocks.postApiSocialGroupsMembersReject).toHaveBeenCalledWith('group-1', 'user-2');

    const roleForm = new FormData();
    roleForm.set('groupId', 'group-1');
    roleForm.set('userId', 'user-1');
    roleForm.set('role', 'Admin');
    await expect(changeCommunityGroupMemberRole(roleForm)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups/group-1?message=Updated+member+role+to+Admin.',
    );
    expect(mocks.putApiSocialGroupsMembersRole).toHaveBeenCalledWith('group-1', 'user-1', { role: 'Admin' });

    const removeForm = new FormData();
    removeForm.set('groupId', 'group-1');
    removeForm.set('userId', 'user-1');
    await expect(removeCommunityGroupMember(removeForm)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups/group-1?message=Removed+group+member.',
    );
    expect(mocks.deleteApiSocialGroupsMembers).toHaveBeenCalledWith('group-1', 'user-1');

    const archiveForm = new FormData();
    archiveForm.set('groupId', 'group-1');
    await expect(archiveCommunityGroup(archiveForm)).rejects.toThrow(
      'redirect:/dashboard/community/members/groups?message=Archived+group.',
    );
    expect(mocks.postApiSocialGroupsArchive).toHaveBeenCalledWith('group-1');
  });
});
