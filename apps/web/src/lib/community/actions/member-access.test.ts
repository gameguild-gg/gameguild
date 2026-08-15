import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  usersGetUsers: vi.fn(),
  usersPostUsers: vi.fn(),
  usersMembershipsPost: vi.fn(),
  UsersModule: vi.fn(),
  UsersMembershipsModule: vi.fn(),
  request: vi.fn(),
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
    UsersModule: mocks.UsersModule,
    UsersMembershipsModule: mocks.UsersMembershipsModule,
  },
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('next/navigation', () => ({
  redirect: mocks.redirect,
}));

import { acceptCurrentUserInvite, cancelPlatformInvite, invitePlatformUser, resendPlatformInvite, updateMemberAccessRole } from './member-access';

describe('updateMemberAccessRole', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.createServerClient.mockReturnValue({ request: mocks.request });
    mocks.request.mockResolvedValue({ ok: true, data: { success: true } });
    mocks.UsersModule.mockImplementation(function UsersModule() {
      return { getUsers: mocks.usersGetUsers, postUsers: mocks.usersPostUsers };
    });
    mocks.UsersMembershipsModule.mockImplementation(function UsersMembershipsModule() {
      return { postUsersMemberships: mocks.usersMembershipsPost };
    });
    mocks.usersPostUsers.mockResolvedValue({ ok: true, data: { id: 'user-1', email: 'learner@game-guild.com' } });
    mocks.usersGetUsers.mockResolvedValue({ ok: true, data: { items: [] } });
    mocks.usersMembershipsPost.mockResolvedValue({ ok: true, data: { success: true, memberId: 'member-1' } });
  });

  it('patches the user membership role and revalidates the dashboard routes', async () => {
    const formData = new FormData();
    formData.set('userId', 'user-1');
    formData.set('tenantId', 'tenant-1');
    formData.set('role', 'SystemAdmin');

    await expect(updateMemberAccessRole(formData)).rejects.toThrow(
      'redirect:/dashboard/platform/roles?message=Updated+member+role+to+SystemAdmin.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'PATCH',
      path: '/v1/users/user-1/memberships/tenant-1/role',
      body: { role: 'SystemAdmin' },
      requiresAuth: true,
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/community');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/community/members/users');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/platform/roles');
  });

  it('redirects with an error when required fields are missing', async () => {
    const formData = new FormData();
    formData.set('userId', 'user-1');

    await expect(updateMemberAccessRole(formData)).rejects.toThrow(
      'redirect:/dashboard/platform/roles?error=User%2C+tenant%2C+and+role+are+required+to+update+access.',
    );

    expect(mocks.request).not.toHaveBeenCalled();
  });

  it('trims submitted identifiers and redirects with an API error when the role update fails', async () => {
    mocks.request.mockResolvedValue({
      ok: false,
      error: { message: 'Only another super admin can grant this role.' },
    });
    const formData = new FormData();
    formData.set('userId', ' user-2 ');
    formData.set('tenantId', ' tenant-2 ');
    formData.set('role', ' SystemAdmin ');

    await expect(updateMemberAccessRole(formData)).rejects.toThrow(
      'redirect:/dashboard/platform/roles?error=Only+another+super+admin+can+grant+this+role.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'PATCH',
      path: '/v1/users/user-2/memberships/tenant-2/role',
      body: { role: 'SystemAdmin' },
      requiresAuth: true,
    });
    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it('redirects with the rejection message when the API returns success false', async () => {
    mocks.request.mockResolvedValue({
      ok: true,
      data: { success: false, message: 'Cannot demote the only super admin.' },
    });
    const formData = new FormData();
    formData.set('userId', 'user-owner');
    formData.set('tenantId', 'tenant-1');
    formData.set('role', 'Member');

    await expect(updateMemberAccessRole(formData)).rejects.toThrow(
      'redirect:/dashboard/platform/roles?error=Cannot+demote+the+only+super+admin.',
    );

    expect(mocks.revalidatePath).not.toHaveBeenCalled();
  });

  it('creates a user and assigns the selected tenant role for platform invites', async () => {
    const formData = new FormData();
    formData.set('email', ' learner@game-guild.com ');
    formData.set('name', ' Learner One ');
    formData.set('tenantId', ' tenant-1 ');
    formData.set('role', ' Moderator ');
    formData.set('invitedByEmail', ' admin@game-guild.com ');

    await expect(invitePlatformUser(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/users?message=Invited+Learner+One+as+Moderator.',
    );

    expect(mocks.usersPostUsers).toHaveBeenCalledWith({
      email: 'learner@game-guild.com',
      name: 'Learner One',
      phoneNumber: null,
    });
    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/v1/users/user-1/memberships',
      requiresAuth: true,
      body: {
        tenantId: 'tenant-1',
        role: 'Moderator',
        invitedByEmail: 'admin@game-guild.com',
        requiresAcceptance: true,
        inviteeEmail: 'learner@game-guild.com',
        inviteeName: 'Learner One',
      },
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/community/members/users');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/platform/roles');
  });

  it('reuses an existing user when inviting an email already present in the directory', async () => {
    mocks.usersGetUsers.mockResolvedValue({
      ok: true,
      data: { items: [{ id: 'existing-user', email: 'learner@game-guild.com', name: 'Existing Learner' }] },
    });
    const formData = new FormData();
    formData.set('email', 'learner@game-guild.com');
    formData.set('tenantId', 'tenant-1');

    await expect(invitePlatformUser(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/users?message=Invited+learner+as+Member.',
    );

    expect(mocks.usersPostUsers).not.toHaveBeenCalled();
    expect(mocks.request).toHaveBeenCalledWith(expect.objectContaining({
      path: '/v1/users/existing-user/memberships',
    }));
  });

  it('requires email and tenant before creating invited users', async () => {
    const formData = new FormData();
    formData.set('email', 'not-an-email');

    await expect(invitePlatformUser(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/users?error=A+valid+email+and+workspace+are+required+to+invite+a+user.',
    );

    expect(mocks.usersPostUsers).not.toHaveBeenCalled();
    expect(mocks.usersMembershipsPost).not.toHaveBeenCalled();
  });

  it('redirects with the API error when invited user creation fails', async () => {
    mocks.usersPostUsers.mockResolvedValue({ ok: false, error: { message: 'Email already exists.' } });
    const formData = new FormData();
    formData.set('email', 'learner@game-guild.com');
    formData.set('tenantId', 'tenant-1');

    await expect(invitePlatformUser(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/users?error=Email+already+exists.',
    );

    expect(mocks.usersMembershipsPost).not.toHaveBeenCalled();
  });

  it('redirects with the API error when membership assignment fails', async () => {
    mocks.request.mockResolvedValue({ ok: false, error: { message: 'Tenant not found.' } });
    const formData = new FormData();
    formData.set('email', 'learner@game-guild.com');
    formData.set('name', 'Learner One');
    formData.set('tenantId', 'tenant-1');

    await expect(invitePlatformUser(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/users?error=Tenant+not+found.',
    );
  });

  it('resends a pending platform invite through the membership invite endpoint', async () => {
    const formData = new FormData();
    formData.set('userId', 'user-1');
    formData.set('tenantId', 'tenant-1');

    await expect(resendPlatformInvite(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/users?message=Invite+resent.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/v1/users/user-1/memberships/tenant-1/invite:resend',
      body: {},
      requiresAuth: true,
    });
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/community/members/users');
  });

  it('cancels a pending platform invite through the membership invite endpoint', async () => {
    const formData = new FormData();
    formData.set('userId', 'user-1');
    formData.set('tenantId', 'tenant-1');

    await expect(cancelPlatformInvite(formData)).rejects.toThrow(
      'redirect:/dashboard/community/members/users?message=Invite+cancelled.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/v1/users/user-1/memberships/tenant-1/invite:cancel',
      body: {},
      requiresAuth: true,
    });
  });

  it('accepts a pending platform invite through the membership invite endpoint', async () => {
    const formData = new FormData();
    formData.set('userId', 'user-1');
    formData.set('tenantId', 'tenant-1');

    await expect(acceptCurrentUserInvite(formData)).rejects.toThrow(
      'redirect:/my/invitations?message=Invitation+accepted.+Your+workspace+access+is+now+active.',
    );

    expect(mocks.request).toHaveBeenCalledWith({
      method: 'POST',
      path: '/v1/users/user-1/memberships/tenant-1/invite:accept',
      body: {},
      requiresAuth: true,
    });
  });
});
